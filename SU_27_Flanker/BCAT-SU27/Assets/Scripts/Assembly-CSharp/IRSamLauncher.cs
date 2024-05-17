using System.Collections;
using UnityEngine;

public class IRSamLauncher : MonoBehaviour, IEngageEnemies, IQSVehicleComponent
{
	public IRMissileLauncher ml;

	public string missileResourcePath;

	public VisualTargetFinder targetFinder;

	public float lockAttemptTime = 5f;

	public float lockHoldTime = 1f;

	public float fireCooldown = 10f;

	public MinMax engagementRange = new MinMax(1000f, 5500f);

	public ModuleTurret turret;

	public Health health;

	private Coroutine samRoutine;

	public Transform headLookTf;

	public bool engageEnemies = true;

	public bool isEngaging;

	private void Start()
	{
		if (!headLookTf)
		{
			headLookTf = new GameObject("headLook").transform;
			headLookTf.parent = base.transform;
			headLookTf.transform.localPosition = Vector3.zero;
		}
	}

	private void OnEnable()
	{
		samRoutine = StartCoroutine(SAMRoutine());
		if ((bool)health)
		{
			health.OnDeath.AddListener(OnDeath);
		}
	}

	private void OnDeath()
	{
		if (samRoutine != null)
		{
			StopCoroutine(samRoutine);
			ml.DisableWeapon();
		}
	}

	private void OnDisable()
	{
		if ((bool)health)
		{
			health.OnDeath.RemoveListener(OnDeath);
		}
		isEngaging = false;
	}

	private float GetMissileSimSpeed(float dist)
	{
		float num = 0f;
		float num2 = 0.5f;
		float num3 = 0f;
		float num4 = 0f;
		Missile nextMissile = ml.GetNextMissile();
		SimpleDrag component = nextMissile.GetComponent<SimpleDrag>();
		while (num3 < dist)
		{
			float num5 = 0f;
			if (num4 < nextMissile.boostTime)
			{
				num5 = nextMissile.boostThrust;
			}
			else if (num4 < nextMissile.boostTime + nextMissile.cruiseTime)
			{
				num5 = nextMissile.cruiseThrust;
			}
			num5 -= component.CalculateDragForceMagnitudeAtSeaLevel(num);
			num += num5 / nextMissile.mass * num2;
			num3 += num * num2;
			num4 += num2;
		}
		return dist / num4;
	}

	private IEnumerator SAMRoutine()
	{
		isEngaging = false;
		yield return new WaitForSeconds(Random.Range(0f, fireCooldown));
		while (base.enabled)
		{
			turret.useDeltaTime = true;
			yield return null;
			while (!engageEnemies || ml.missileCount < 1)
			{
				yield return null;
			}
			Actor actor = null;
			while (actor == null)
			{
				yield return null;
				actor = targetFinder.attackingTarget;
				if ((bool)actor)
				{
					float sqrMagnitude = (actor.position - base.transform.position).sqrMagnitude;
					if (sqrMagnitude < engagementRange.min * engagementRange.min || sqrMagnitude > engagementRange.max * engagementRange.max)
					{
						actor = null;
					}
				}
			}
			if ((bool)health && health.normalizedHealth <= 0f)
			{
				break;
			}
			if (!engageEnemies)
			{
				continue;
			}
			Actor target = actor;
			isEngaging = true;
			ml.EnableWeapon();
			if (ml.debugMissiles)
			{
				ml.activeMissile.heatSeeker.debugSeeker = true;
			}
			turret.referenceTransform = ml.activeMissile.transform;
			ml.activeMissile.heatSeeker.headTransform = headLookTf;
			ml.activeMissile.heatSeeker.SetSeekerMode(HeatSeeker.SeekerModes.HeadTrack);
			float t3 = Time.time;
			while (engageEnemies && Time.time - t3 < lockAttemptTime && (bool)target && ml.activeMissile.heatSeeker.seekerLock < 0.8f && (ml.activeMissile.heatSeeker.targetPosition - target.position).sqrMagnitude > 100f)
			{
				Vector3 vector = target.position + target.velocity * Time.deltaTime;
				turret.AimToTarget(vector);
				headLookTf.LookAt(vector);
				yield return null;
			}
			if (!engageEnemies)
			{
				ml.DisableWeapon();
				turret.ReturnTurretOneshot();
				isEngaging = false;
				continue;
			}
			t3 = Time.time;
			while ((bool)target && Time.time - t3 < lockHoldTime * 0.15f)
			{
				Vector3 vector2 = target.position + target.velocity * Time.deltaTime;
				turret.AimToTarget(vector2);
				headLookTf.LookAt(vector2);
				yield return null;
			}
			if (!engageEnemies)
			{
				turret.ReturnTurretOneshot();
				isEngaging = false;
				continue;
			}
			bool hasFireSolution = false;
			if ((bool)target)
			{
				t3 = Time.time;
				float simSpeed = GetMissileSimSpeed((target.position - base.transform.position).magnitude);
				while ((bool)target && Time.time - t3 < lockHoldTime * 0.85f)
				{
					if (SAMLauncher.GetAirToAirFireSolution(ml.hardpoints[0].position, ml.parentActor.velocity, simSpeed, target, turret, null, out var fireSolution))
					{
						turret.AimToTarget(fireSolution);
						hasFireSolution = true;
					}
					else
					{
						hasFireSolution = false;
					}
					headLookTf.LookAt(target.position + target.velocity * Time.deltaTime);
					yield return null;
				}
			}
			bool fired = false;
			if ((bool)target && hasFireSolution && ml.activeMissile.heatSeeker.seekerLock > 0.5f && (ml.activeMissile.heatSeeker.targetPosition - target.position).sqrMagnitude < 2800f && CanSeeTarget(target))
			{
				fired = ml.TryFireMissile();
			}
			if (!fired)
			{
				ml.activeMissile.heatSeeker.SetSeekerMode(HeatSeeker.SeekerModes.Caged);
			}
			turret.referenceTransform = turret.pitchTransform;
			yield return null;
			ml.DisableWeapon();
			turret.ReturnTurretOneshot();
			isEngaging = false;
			if (fired)
			{
				yield return new WaitForSeconds(fireCooldown);
			}
		}
	}

	private bool CanSeeTarget(Actor target)
	{
		return !Physics.Linecast(targetFinder.transform.position, target.position, 1);
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode(base.gameObject.name + "_IRSamLauncher").SetValue("missileCount", ml.missileCount);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(base.gameObject.name + "_IRSamLauncher");
		if (node != null)
		{
			int value = node.GetValue<int>("missileCount");
			ml.LoadCount(value);
		}
	}

	public void SetEngageEnemies(bool engage)
	{
		engageEnemies = engage;
	}
}
