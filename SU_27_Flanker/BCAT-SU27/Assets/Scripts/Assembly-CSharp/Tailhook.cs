using System;
using System.Collections;
using UnityEngine;

public class Tailhook : MonoBehaviour
{
	private enum CableForceModes
	{
		Original,
		Velocity,
		Center
	}

	public Actor actor;

	public ModuleTurret hookTurret;

	public Transform hookPointTf;

	public Transform hookForcePointTf;

	public float deployAngle;

	public float defaultTurretSpeed;

	public float hookedTurretSpeed;

	private bool deployed;

	public float hookForce;

	public float minDistForce = 10f;

	public float maxDistForce = 60f;

	public AudioSource audioSource;

	public AudioClip cableCatchSound;

	public AudioClip cableBreakSound;

	private Rigidbody rb;

	private KinematicPlane kPlane;

	private bool landJointed;

	public PID3 jointPID;

	private Vector3 landJointPoint;

	private Transform landJointParent;

	public RotationToggle hookDoor;

	private FlightInfo flightInfo;

	private static CableForceModes forceMode;

	private bool isRemote;

	public bool enableHookPhysics;

	public float hookMass = 1f;

	public float hookSpring = 1f;

	public float hookDamper = 0.01f;

	public float hookBounce = 0.003f;

	public float hookLimitBounce = 0.1f;

	[Range(0f, 1f)]
	public float hookMaxRoughness = 0.15f;

	private float hookArcPos;

	private float physHookDeployAngle;

	private float hookVel;

	public int arcRayIntervals = 4;

	public Transform hookCollisionEffectTf;

	public ParticleSystem[] hookCollisionParticles;

	public MinMax hookEmitPerImpactSpeed;

	private float emitMult;

	private bool hookEmitting;

	private bool waitingForDoor;

	private Coroutine deployHookRoutine;

	private Coroutine returnHookRoutine;

	public bool isDeployed => deployed;

	public CarrierCable hookedCable { get; private set; }

	public event Action<OpticalLandingSystem.OLSData> OnReceivedOLSData;

	public event Action<int> OnExtendState;

	private void Start()
	{
		flightInfo = GetComponentInParent<FlightInfo>();
		rb = flightInfo.rb;
		kPlane = rb.GetComponent<KinematicPlane>();
		actor = rb.GetComponent<Actor>();
		if ((bool)actor && actor.isPlayer && GameSettings.CurrentSettings.GetBoolSetting("HOOK_PHYSICS"))
		{
			enableHookPhysics = true;
		}
		FreeHook();
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)hookTurret)
		{
			float magnitude = (hookTurret.pitchTransform.position - hookPointTf.position).magnitude;
			Vector3 vector = Quaternion.AngleAxis(0f - deployAngle, hookTurret.transform.right) * hookTurret.transform.forward;
			Vector3 vector2 = hookTurret.transform.position + vector * magnitude;
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(hookTurret.transform.position, vector2);
			Gizmos.DrawSphere(vector2, 0.1f);
		}
	}

	public void SendOLSData(OpticalLandingSystem.OLSData data)
	{
		if (this.OnReceivedOLSData != null)
		{
			this.OnReceivedOLSData(data);
		}
	}

	public void PlayBreakSound()
	{
		if ((bool)audioSource && (bool)cableBreakSound)
		{
			audioSource.PlayOneShot(cableBreakSound);
		}
	}

	public void SetToRemote()
	{
		isRemote = true;
	}

	private IEnumerator DeployedRoutine()
	{
		if (enableHookPhysics)
		{
			SetStartingHookArcPos();
		}
		while (deployed)
		{
			if (Input.GetKey(KeyCode.RightShift) && Input.GetKey(KeyCode.H))
			{
				enableHookPhysics = true;
			}
			yield return new WaitForFixedUpdate();
			if (waitingForDoor)
			{
				continue;
			}
			if ((bool)hookedCable)
			{
				hookTurret.yawSpeedDPS = hookedTurretSpeed;
				hookTurret.pitchSpeedDPS = hookedTurretSpeed;
				hookTurret.AimToTarget(hookedCable.transform.position);
				if (!landJointed && (flightInfo.surfaceSpeed < 1f || Vector3.Dot(flightInfo.surfaceVelocity, base.transform.position - hookedCable.transform.position) < 0f))
				{
					CreateJoint();
				}
				if (landJointed)
				{
					Vector3 force = jointPID.Evaluate(hookForcePointTf.position, landJointParent.TransformPoint(landJointPoint));
					AddForceAtPosition(force, hookForcePointTf.position);
					continue;
				}
				Vector3 vector = hookedCable.transform.position - hookForcePointTf.position;
				float magnitude = vector.magnitude;
				Vector3 vector2 = Vector3.zero;
				switch (forceMode)
				{
				case CableForceModes.Original:
					vector2 = Vector3.Slerp(vector.normalized, Vector3.Project(vector, hookedCable.transform.forward).normalized, 0.5f).normalized;
					break;
				case CableForceModes.Velocity:
					vector2 = -rb.velocity.normalized;
					break;
				case CableForceModes.Center:
					vector2 = vector.normalized;
					break;
				}
				Vector3 force2 = hookForce * Mathf.Clamp(magnitude, minDistForce, maxDistForce) * vector2;
				AddForceAtPosition(force2, hookForcePointTf.position);
				continue;
			}
			if (enableHookPhysics)
			{
				HookPhysics();
			}
			else
			{
				Vector3 targetPosition = hookTurret.transform.position + Quaternion.AngleAxis(0f - deployAngle, hookTurret.transform.right) * hookTurret.transform.forward * 10f;
				hookTurret.AimToTarget(targetPosition);
			}
			Vector3 pointVelocity = rb.GetPointVelocity(hookPointTf.position);
			int num = 32768;
			if (enableHookPhysics)
			{
				num |= 1;
			}
			if (isRemote || !Physics.Linecast(hookPointTf.position, hookPointTf.position + 1.1f * Time.fixedDeltaTime * pointVelocity, out var hitInfo, num) || hitInfo.collider.gameObject.layer != 15)
			{
				continue;
			}
			CarrierCable componentInParent = hitInfo.collider.gameObject.GetComponentInParent<CarrierCable>();
			if ((bool)componentInParent && !componentInParent.hook)
			{
				hookedCable = componentInParent;
				hookedCable.SetHook(this);
				if ((bool)audioSource)
				{
					audioSource.PlayOneShot(cableCatchSound);
				}
				FlightLogger.Log($"Tailhook caught arrestor cable: {hookedCable.name}");
			}
		}
	}

	public void RemoteSetCable(CarrierCable c)
	{
		if ((bool)hookedCable && hookedCable != c)
		{
			FreeHook();
		}
		if ((bool)c)
		{
			hookedCable = c;
			if ((bool)audioSource)
			{
				audioSource.PlayOneShot(cableCatchSound);
			}
			FlightLogger.Log($"Tailhook caught arrestor cable (remote): {hookedCable.name}");
		}
	}

	private void HookPhysics()
	{
		physHookDeployAngle = Mathf.MoveTowards(physHookDeployAngle, deployAngle, hookTurret.pitchSpeedDPS * Time.fixedDeltaTime);
		float z = hookPointTf.localPosition.z;
		float angle = hookArcPos / (float)arcRayIntervals;
		Vector3 vector = new Vector3(0f, 0f, z);
		Vector3 position = vector;
		RaycastHit hitInfo = default(RaycastHit);
		bool flag = false;
		for (int i = 0; i < arcRayIntervals; i++)
		{
			vector = Quaternion.AngleAxis(angle, Vector3.left) * vector;
			Vector3 start = hookTurret.transform.TransformPoint(position);
			Vector3 end = hookTurret.transform.TransformPoint(vector);
			if (Physics.Linecast(start, end, out hitInfo, 1))
			{
				flag = true;
				break;
			}
			position = vector;
		}
		float num = (physHookDeployAngle - hookArcPos) * hookSpring - hookVel * hookDamper;
		hookVel += num / hookMass;
		if (flag)
		{
			Vector3 vector2 = Vector3.Cross(-hookTurret.transform.right, hookTurret.pitchTransform.forward);
			Vector3 pointVelocity = rb.GetPointVelocity(hookPointTf.position);
			Vector3 vector3 = Vector3.Project(pointVelocity, vector2);
			pointVelocity -= vector3;
			float magnitude = (vector3 + pointVelocity * UnityEngine.Random.Range(0f, hookMaxRoughness)).magnitude;
			float num2 = (float)Math.PI * 2f * z * 0.0027777778f;
			float num3 = magnitude / num2;
			num3 *= Mathf.Sign(Vector3.Dot(vector3, vector2));
			float num4 = hookVel + num3;
			if (num4 > 1f)
			{
				hookVel = (0f - num4) * hookBounce;
				if (hookCollisionParticles != null)
				{
					emitMult = Mathf.Max(emitMult, num4 * hookEmitPerImpactSpeed.Random());
					emitMult = Mathf.Min(emitMult, 2f);
					if (emitMult > 0.1f)
					{
						for (int j = 0; j < hookCollisionParticles.Length; j++)
						{
							hookCollisionParticles[j].SetEmission(emit: true);
							hookCollisionParticles[j].SetEmissionRateMultiplier(emitMult);
						}
						if ((bool)hookCollisionEffectTf)
						{
							hookCollisionEffectTf.rotation = Quaternion.LookRotation(-pointVelocity);
						}
						if (!hookEmitting)
						{
							hookEmitting = true;
							StartCoroutine(HookEmittingDecayRoutine());
						}
					}
				}
			}
			float num5 = Vector3.Angle(hookTurret.transform.forward, hitInfo.point - hookTurret.transform.position);
			hookArcPos = num5 + hookVel * Time.fixedDeltaTime;
			hookArcPos = Mathf.Clamp(hookArcPos, float.Epsilon, num5 - 0.001f);
		}
		else
		{
			hookArcPos += hookVel * Time.fixedDeltaTime;
			if ((hookArcPos >= physHookDeployAngle && hookVel > 0f) || (hookArcPos <= 0f && hookVel < 0f))
			{
				hookVel = (0f - hookVel) * hookLimitBounce;
			}
			hookArcPos = Mathf.Clamp(hookArcPos, float.Epsilon, physHookDeployAngle - 0.001f);
		}
		Vector3 targetPosition = hookTurret.transform.TransformPoint(Quaternion.AngleAxis(hookArcPos, Vector3.left) * new Vector3(0f, 0f, z));
		hookTurret.AimToTargetImmediate(targetPosition);
	}

	private IEnumerator HookEmittingDecayRoutine()
	{
		while (hookEmitting && emitMult > 0.01f)
		{
			emitMult -= 4f * Time.deltaTime;
			hookCollisionParticles.SetEmissionRateMultiplier(emitMult);
			yield return null;
		}
		hookCollisionParticles.SetEmission(emit: false);
		hookEmitting = false;
	}

	private void SetStartingHookArcPos()
	{
		hookArcPos = Vector3.Angle(hookTurret.transform.forward, hookTurret.pitchTransform.forward);
		physHookDeployAngle = hookArcPos;
		hookVel = 0f;
	}

	private void AddForceAtPosition(Vector3 force, Vector3 position)
	{
		if (!rb.isKinematic)
		{
			rb.AddForceAtPosition(force, position);
		}
		else if ((bool)kPlane)
		{
			kPlane.AddForce(force);
		}
	}

	private void CreateJoint()
	{
		landJointParent = hookedCable.transform;
		landJointPoint = landJointParent.InverseTransformPoint(hookForcePointTf.position);
		landJointed = true;
	}

	public void FreeHook()
	{
		if ((bool)hookedCable)
		{
			hookedCable.SetHook(null);
			hookedCable = null;
		}
		hookTurret.yawSpeedDPS = defaultTurretSpeed;
		hookTurret.pitchSpeedDPS = defaultTurretSpeed;
	}

	public void ToggleHook()
	{
		if (deployed)
		{
			RetractHook();
		}
		else
		{
			ExtendHook();
		}
	}

	public void ExtendHook()
	{
		deployed = true;
		if (returnHookRoutine != null)
		{
			StopCoroutine(returnHookRoutine);
		}
		if (deployHookRoutine != null)
		{
			StopCoroutine(deployHookRoutine);
		}
		deployHookRoutine = StartCoroutine(DeployHookRoutine());
		this.OnExtendState?.Invoke(1);
	}

	private IEnumerator DeployHookRoutine()
	{
		if ((bool)hookDoor)
		{
			waitingForDoor = true;
			hookDoor.SetDeployed();
			while (hookDoor.transforms[0].currentT < 0.99f)
			{
				yield return null;
			}
			waitingForDoor = false;
		}
		deployHookRoutine = StartCoroutine(DeployedRoutine());
	}

	public void RetractHook()
	{
		FreeHook();
		deployed = false;
		if (returnHookRoutine != null)
		{
			StopCoroutine(returnHookRoutine);
		}
		if (deployHookRoutine != null)
		{
			StopCoroutine(deployHookRoutine);
		}
		returnHookRoutine = StartCoroutine(ReturnHookRoutine());
		if (landJointed)
		{
			landJointed = false;
		}
		this.OnExtendState?.Invoke(0);
	}

	private IEnumerator ReturnHookRoutine()
	{
		while (!hookTurret.ReturnTurret())
		{
			yield return null;
		}
		if ((bool)hookDoor)
		{
			hookDoor.SetDefault();
		}
	}

	public void SetHook(int h)
	{
		if (h > 0)
		{
			ExtendHook();
		}
		else
		{
			RetractHook();
		}
	}
}
