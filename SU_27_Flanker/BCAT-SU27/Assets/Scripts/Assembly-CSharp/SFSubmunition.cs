using System.Collections;
using UnityEngine;

public class SFSubmunition : MonoBehaviour
{
	public Missile missile;

	public SensorFuzedCB cbu;

	public Collider col;

	public SolidBooster[] boosters;

	public float mass;

	public MinMax ejectSpeed;

	public MinMax ejectTorque;

	public Vector3 localTorqueDir;

	public float fov;

	public int skeetCount = 4;

	public float boostAlt;

	public float skeetMinAlt;

	public MinMax skeetEjectSpeed;

	public float skeetScanInterval = 0.1f;

	public Bullet.BulletInfo bulletInfo;

	public float spinTorque = 5f;

	[HideInInspector]
	public ParticleSystem fireParticleSystem;

	[HideInInspector]
	public int firePSBurstCount;

	[Header("Chute")]
	public SimpleDrag chuteDrag;

	public float chuteDeployRate;

	public Vector3 chuteScale;

	public Transform chuteTransform;

	private Coroutine chuteRoutine;

	private int skeetsFinished;

	private Teams myTeam;

	public void FireSubmunition()
	{
		StartCoroutine(SubRoutine());
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += OnExitScene;
		}
	}

	private void OnExitScene()
	{
		if ((bool)base.gameObject)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void OnDestroy()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= OnExitScene;
		}
	}

	private IEnumerator SubRoutine()
	{
		myTeam = missile.actor.team;
		fireParticleSystem.transform.parent = null;
		base.transform.parent = null;
		base.gameObject.layer = 9;
		col.enabled = true;
		Rigidbody rb = base.gameObject.AddComponent<Rigidbody>();
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		rb.mass = mass;
		rb.velocity = missile.rb.velocity;
		rb.angularDrag = 0f;
		base.gameObject.AddComponent<FloatingOriginTransform>().SetRigidbody(rb);
		chuteDrag.SetParentRigidbody(rb);
		rb.AddRelativeForce(ejectSpeed.Random() * Vector3.forward, ForceMode.VelocityChange);
		rb.AddRelativeTorque(ejectTorque.Random() * localTorqueDir, ForceMode.Impulse);
		chuteRoutine = StartCoroutine(ChuteRoutine());
		RaycastHit hitInfo;
		while (Vector3.Dot(rb.velocity.normalized, Vector3.down) < 0.9f || !Physics.Raycast(new Ray(base.transform.position, Vector3.down), out hitInfo, boostAlt, 1))
		{
			yield return null;
		}
		float surfaceAlt = WaterPhysics.GetAltitude(hitInfo.point);
		if (chuteRoutine != null)
		{
			StopCoroutine(chuteRoutine);
			chuteTransform.localScale = Vector3.zero;
			chuteDrag.enabled = false;
		}
		for (int i = 0; i < boosters.Length; i++)
		{
			boosters[i].SetParentRigidbody(rb);
			boosters[i].Fire();
		}
		float burntime = boosters[0].burnTime;
		float t = Time.time;
		while (Time.time - t < burntime)
		{
			rb.AddRelativeTorque(spinTorque * Vector3.up);
			yield return new WaitForFixedUpdate();
		}
		Vector3 vector = base.transform.forward;
		float angle = 360f / (float)skeetCount;
		for (int j = 0; j < skeetCount; j++)
		{
			StartCoroutine(SkeetRoutine(rb, vector, surfaceAlt));
			vector = Quaternion.AngleAxis(angle, base.transform.up) * vector;
		}
		while (skeetsFinished < skeetCount)
		{
			yield return null;
		}
		Object.Destroy(base.gameObject);
	}

	private IEnumerator SkeetRoutine(Rigidbody rb, Vector3 direction, float surfaceAlt)
	{
		FixedPoint skeetPoint = new FixedPoint(base.transform.position);
		Vector3 velocity = rb.velocity + skeetEjectSpeed.Random() * direction.normalized;
		bool alive = true;
		_ = Time.time;
		float lastScanTime = Time.time;
		Actor target = null;
		while (alive)
		{
			skeetPoint.point += velocity * Time.fixedDeltaTime;
			velocity += Physics.gravity * Time.fixedDeltaTime;
			if (WaterPhysics.GetAltitude(skeetPoint.point) < surfaceAlt + skeetMinAlt)
			{
				alive = false;
			}
			else if (Time.time - lastScanTime > skeetScanInterval)
			{
				target = TargetManager.instance.GetOpticalTargetFromView(null, 2f * cbu.deployAltitude, cbu.targetsToFind.bitmask, 5f, skeetPoint.point, Vector3.down, fov, random: false, allActors: false, null, updateDetection: false, myTeam);
				if ((bool)target)
				{
					alive = false;
				}
				lastScanTime = Time.time;
			}
			if (alive)
			{
				yield return new WaitForFixedUpdate();
			}
		}
		if ((bool)target)
		{
			fireParticleSystem.transform.position = skeetPoint.point;
			Vector3 vector = target.position - skeetPoint.point;
			fireParticleSystem.transform.rotation = Quaternion.LookRotation(vector);
			fireParticleSystem.Emit(firePSBurstCount);
			Actor sourceActor = null;
			if ((bool)missile)
			{
				sourceActor = missile.actor;
			}
			Bullet.FireBullet(skeetPoint.point, vector, bulletInfo, Vector3.zero, sourceActor);
		}
		else
		{
			ExplosionManager.instance.CreateExplosionEffect(ExplosionManager.ExplosionTypes.DebrisPoof, skeetPoint.point, velocity);
		}
		skeetsFinished++;
	}

	private IEnumerator ChuteRoutine()
	{
		float dragArea = chuteDrag.area;
		chuteDrag.area = 0f;
		chuteDrag.enabled = true;
		float t = 0f;
		while (t <= 1f)
		{
			t = Mathf.MoveTowards(t, 1f, chuteDeployRate * Time.deltaTime);
			chuteDrag.area = Mathf.Lerp(0f, dragArea, t);
			chuteTransform.localScale = Vector3.Lerp(Vector3.zero, chuteScale, t);
			yield return null;
		}
	}
}
