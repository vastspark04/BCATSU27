using System.Collections;
using UnityEngine;

public class CarrierCatapult : MonoBehaviour
{
	public int catapultDesignation;

	public Transform catapultTransform;

	public float length;

	public float preLaunchLength;

	public float prelaunchRollTime;

	public float prelaunchWaitTime;

	public float launchTime;

	public AnimationCurve launchCurve;

	public float returnTime;

	public float triggerRadius;

	public RotationToggle deflectorRotator;

	private Vector3 readyPos;

	private Vector3 launchStartPos;

	private Vector3 launchTargetPos;

	private Transform planeHookTransform;

	private Rigidbody planeRb;

	private Rigidbody catRb;

	public Rigidbody parentRb;

	public AudioSource audioSource;

	public AudioClip latchSound;

	public AudioClip launchStartSound;

	public AudioClip launchReleaseSound;

	private ParticleSystem[] ps;

	private Vector3 catLocalPos;

	private bool awoken;

	private CatapultHook hook;

	private Transform catParent;

	private Vector3 catPosition;

	private FlightInfo flightInfo;

	private Vector3 lastVelocity;

	private float t;

	private ConfigurableJoint catJoint;

	public float sqrRadius { get; private set; }

	public bool catapultReady { get; private set; }

	public bool hooked { get; private set; }

	public Vector3 catapultPosition => WorldPos(catPosition);

	private void OnDrawGizmos()
	{
		if ((bool)catapultTransform)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(catapultTransform.position, catapultTransform.position + length * catapultTransform.forward);
			Gizmos.DrawWireSphere(catapultTransform.position, triggerRadius);
			Gizmos.color = Color.red;
			Gizmos.DrawLine(catapultTransform.position, catapultTransform.position + preLaunchLength * catapultTransform.forward);
		}
	}

	private void Awake()
	{
		catParent = catapultTransform.parent;
		readyPos = LocalPos(catapultTransform.position);
		launchTargetPos = LocalPos(catapultTransform.position + length * catapultTransform.forward);
		launchStartPos = LocalPos(catapultTransform.position + preLaunchLength * catapultTransform.forward);
		ps = catapultTransform.GetComponentsInChildren<ParticleSystem>();
		sqrRadius = triggerRadius * triggerRadius;
		if (!parentRb)
		{
			parentRb = GetComponentInParent<Rigidbody>();
		}
		awoken = true;
	}

	private void Start()
	{
		catapultReady = true;
		catapultTransform.localPosition = catLocalPos;
	}

	private void OnEnable()
	{
		catapultTransform.localPosition = catLocalPos;
		if (awoken)
		{
			catLocalPos = catapultTransform.localPosition;
		}
	}

	public void Hook(CatapultHook hook)
	{
		hooked = true;
		this.hook = hook;
		planeHookTransform = hook.hookForcePointTransform;
		planeRb = hook.rb;
		flightInfo = hook.rb.GetComponentInChildren<FlightInfo>();
		if ((bool)flightInfo)
		{
			flightInfo.PauseGCalculations();
		}
		StartCoroutine(CatapultRoutine());
	}

	private IEnumerator CatapultRoutine()
	{
		yield return new WaitForFixedUpdate();
		catapultReady = false;
		audioSource.PlayOneShot(latchSound);
		catPosition = readyPos;
		catapultTransform.position = WorldPos(readyPos);
		CreateJoint();
		FloatingOrigin.instance.AddRigidbody(catRb);
		yield return ReadyRoutine();
		if ((bool)hook)
		{
			audioSource.PlayOneShot(launchStartSound);
			yield return LaunchRoutine();
		}
		yield return new WaitForSeconds(1f);
		yield return ReturnRoutine();
		catapultReady = true;
	}

	private void OnDestroy()
	{
		if (catapultTransform != null)
		{
			Object.Destroy(catapultTransform.gameObject);
		}
	}

	private void SetCatPosition(float t)
	{
		catPosition = Vector3.Lerp(readyPos, launchTargetPos, t);
		if ((bool)catRb)
		{
			catRb.MovePosition(WorldPos(catPosition));
		}
		else
		{
			catapultTransform.position = WorldPos(catPosition);
		}
	}

	private IEnumerator ReadyRoutine()
	{
		float t3 = 0f;
		while (t3 < 1f)
		{
			t3 += Time.fixedDeltaTime;
			catPosition = readyPos;
			catRb.MovePosition(WorldPos(readyPos));
			yield return new WaitForFixedUpdate();
		}
		t3 = 0f;
		while (t3 < 1f)
		{
			catPosition = Vector3.Lerp(readyPos, launchStartPos, t3);
			Vector3 position = WorldPos(catPosition);
			catRb.MovePosition(position);
			t3 += 1f / prelaunchRollTime * Time.fixedDeltaTime;
			yield return new WaitForFixedUpdate();
		}
		deflectorRotator.SetDeployed();
		t3 = 0f;
		while (t3 < prelaunchWaitTime)
		{
			catPosition = launchStartPos;
			catRb.MovePosition(WorldPos(catPosition));
			t3 += Time.fixedDeltaTime;
			yield return new WaitForFixedUpdate();
		}
		if (!hook)
		{
			yield break;
		}
		VehicleMaster vm = hook.GetComponentInParent<VehicleMaster>();
		while ((bool)hook)
		{
			bool flag = true;
			for (int i = 0; i < hook.engines.Length && flag; i++)
			{
				if (!hook.engines[i].startedUp || hook.engines[i].finalThrottle < 0.66f)
				{
					flag = false;
				}
			}
			if ((bool)vm && (bool)vm.wingFolder && (vm.wingFolder.deployed || vm.wingFolder.transforms[0].currentT > 0.001f))
			{
				flag = false;
			}
			if (flag)
			{
				break;
			}
			catPosition = launchStartPos;
			catRb.MovePosition(WorldPos(catPosition));
			yield return new WaitForFixedUpdate();
		}
	}

	private IEnumerator LaunchRoutine()
	{
		t = 0f;
		ps.SetEmission(emit: true);
		Vector3 position = catRb.position;
		catRb.MovePosition(WorldPos(launchStartPos));
		lastVelocity = (catRb.position - position) / Time.fixedDeltaTime;
		float tDelta = 1f / launchTime * Time.fixedDeltaTime;
		while (t + tDelta < 1f)
		{
			FloatingOrigin.instance.AddQueuedFixedUpdateAction(LaunchQueuedUpdate);
			yield return new WaitForFixedUpdate();
		}
		ps.SetEmission(emit: false);
	}

	private void LaunchQueuedUpdate()
	{
		float num = 1f / launchTime * Time.fixedDeltaTime;
		catPosition = Vector3.Lerp(launchStartPos, launchTargetPos, launchCurve.Evaluate(t));
		Vector3 vector = WorldPos(catPosition);
		Vector3 position = catRb.position;
		catRb.MovePosition(vector);
		if ((bool)flightInfo)
		{
			Vector3 vector2 = (vector - position) / Time.fixedDeltaTime;
			Vector3 vector3 = (vector2 - lastVelocity) / Time.fixedDeltaTime;
			lastVelocity = vector2;
			vector3 = Vector3.ClampMagnitude(vector3, 98.100006f);
			flightInfo.OverrideRecordedAcceleration(vector3);
			catRb.velocity = vector2;
		}
		t += num;
		if (t + num >= 1f)
		{
			EndLaunch();
		}
		else if (t + 2f * num >= 1f)
		{
			catJoint.xMotion = ConfigurableJointMotion.Free;
			catJoint.yMotion = ConfigurableJointMotion.Free;
			catJoint.zMotion = ConfigurableJointMotion.Free;
			catJoint.angularYMotion = ConfigurableJointMotion.Free;
		}
	}

	private void EndLaunch()
	{
		DestroyJoint();
		float num = 1f / launchTime * 5f * Time.fixedDeltaTime;
		Vector3 vector = (WorldPos(launchTargetPos) - WorldPos(Vector3.Lerp(launchStartPos, launchTargetPos, launchCurve.Evaluate(1f - num)))) / (5f * Time.fixedDeltaTime);
		Debug.Log("catVel: " + vector.magnitude);
		Vector3 velocity = vector + parentRb.velocity;
		if ((bool)planeRb)
		{
			planeRb.velocity = velocity;
			if (planeRb.isKinematic)
			{
				KinematicPlane component = planeRb.GetComponent<KinematicPlane>();
				if ((bool)component)
				{
					component.SetVelocity(velocity);
				}
			}
		}
		if ((bool)flightInfo && (flightInfo.GetComponent<Actor>() == FlightSceneManager.instance.playerActor || ((bool)flightInfo.GetComponent<AIPilot>() && VTScenario.isScenarioHost)))
		{
			flightInfo.UnpauseGCalculations();
		}
		flightInfo = null;
		audioSource.PlayOneShot(launchReleaseSound);
		planeRb = null;
		planeHookTransform = null;
		hooked = false;
	}

	private IEnumerator ReturnRoutine()
	{
		deflectorRotator.SetDefault();
		float t = 0f;
		while (t < 1f)
		{
			catapultTransform.position = Vector3.Lerp(WorldPos(launchTargetPos), WorldPos(readyPos), t);
			t += 1f / returnTime * Time.fixedDeltaTime;
			yield return new WaitForFixedUpdate();
		}
	}

	private void CreateJoint()
	{
		Quaternion rotation = planeRb.transform.rotation;
		planeRb.transform.rotation = catapultTransform.rotation;
		catapultTransform.parent = null;
		catRb = catapultTransform.gameObject.AddComponent<Rigidbody>();
		catRb.isKinematic = true;
		catLocalPos = catapultTransform.localPosition;
		catRb.interpolation = RigidbodyInterpolation.Interpolate;
		catJoint = catapultTransform.gameObject.AddComponent<ConfigurableJoint>();
		catJoint.autoConfigureConnectedAnchor = false;
		catJoint.connectedBody = planeRb;
		catJoint.connectedAnchor = planeRb.transform.InverseTransformPoint(planeHookTransform.position);
		catJoint.anchor = Vector3.zero;
		catJoint.projectionMode = JointProjectionMode.PositionAndRotation;
		catJoint.xMotion = ConfigurableJointMotion.Locked;
		catJoint.yMotion = ConfigurableJointMotion.Locked;
		catJoint.zMotion = ConfigurableJointMotion.Locked;
		catJoint.angularXMotion = ConfigurableJointMotion.Free;
		catJoint.angularZMotion = ConfigurableJointMotion.Free;
		catJoint.angularYMotion = ConfigurableJointMotion.Free;
		planeRb.transform.rotation = rotation;
	}

	private void DestroyJoint()
	{
		Object.Destroy(catJoint);
		if ((bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.RemoveRigidbody(catRb);
		}
		Object.Destroy(catRb);
		catapultTransform.parent = catParent;
	}

	private Vector3 WorldPos(Vector3 localPos)
	{
		return catParent.TransformPoint(localPos);
	}

	private Vector3 LocalPos(Vector3 worldPos)
	{
		return catParent.InverseTransformPoint(worldPos);
	}
}
