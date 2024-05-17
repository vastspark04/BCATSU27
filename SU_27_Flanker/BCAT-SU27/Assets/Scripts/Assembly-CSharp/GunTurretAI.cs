using System.Collections;
using UnityEngine;

public class GunTurretAI : MonoBehaviour, IEngageEnemies, IQSVehicleComponent
{
	public Gun gun;

	public ModuleTurret turret;

	public float nonTurretFOV = 90f;

	public float nonTurretAimSpeed = 120f;

	public VisualTargetFinder targetFinder;

	public bool requireRadarLock;

	public bool radarOnlyOnSpotted = true;

	public bool unlockWhileSearching = true;

	public float preLockOnTime = 2f;

	public LockingRadar lockingRadar;

	public float maxFiringRange;

	public float burstTime;

	public float minBurstTime = 0.5f;

	public float fireInterval;

	public float inaccuracyFactor;

	public float inaccuracyRate;

	public float leadSweep;

	public float leadSweepRate;

	public bool engageEnemies = true;

	private bool alive = true;

	private float perlinSeed;

	private WaitForSeconds fireWait;

	private Transform referenceTransform;

	private ChaffCountermeasure chaffModule;

	private Coroutine aiRoutine;

	private int iffCheckFrame;

	private int iffCheckInterval = 4;

	public bool engagingTarget { get; private set; }

	public Actor engagingActor { get; private set; }

	public bool isFiring { get; private set; }

	private RadarLockData radarLock
	{
		get
		{
			if (!lockingRadar)
			{
				return null;
			}
			return lockingRadar.currentLock;
		}
	}

	private string qsNodeName => base.gameObject.name + "_GunTurretAI";

	private void OnDrawGizmos()
	{
		if ((bool)gun && !turret)
		{
			Gizmos.color = Color.yellow;
			Gizmos.matrix = Matrix4x4.TRS(gun.transform.position, gun.transform.parent.rotation, Vector3.one);
			Gizmos.DrawFrustum(Vector3.zero, nonTurretFOV, 10f, 0.1f, 1f);
			Gizmos.matrix = Matrix4x4.identity;
		}
	}

	private void Awake()
	{
		GetComponentInParent<Health>().OnDeath.AddListener(Health_OnDeath);
	}

	private void Start()
	{
		perlinSeed = Random.Range(0f, 100f);
		if ((bool)turret)
		{
			turret.useDeltaTime = true;
		}
		referenceTransform = gun.fireTransforms[0];
		fireWait = new WaitForSeconds(fireInterval);
	}

	private void Health_OnDeath()
	{
		StopAllCoroutines();
		gun.SetFire(fire: false);
		if ((bool)lockingRadar)
		{
			lockingRadar.Unlock();
			lockingRadar.enabled = false;
			if ((bool)lockingRadar.radar)
			{
				lockingRadar.radar.radarEnabled = false;
			}
		}
		alive = false;
		engagingTarget = false;
		isFiring = false;
	}

	private void OnEnable()
	{
		aiRoutine = StartCoroutine(AIRoutine());
	}

	private void OnDisable()
	{
		gun.SetFire(fire: false);
		engagingTarget = false;
		if (aiRoutine != null)
		{
			StopCoroutine(aiRoutine);
		}
	}

	private IEnumerator AIRoutine()
	{
		if ((bool)lockingRadar && radarOnlyOnSpotted && (bool)lockingRadar.radar)
		{
			lockingRadar.radar.radarEnabled = false;
		}
		while (alive)
		{
			if (engageEnemies)
			{
				while (gun.currentAmmo < 1)
				{
					if (radarOnlyOnSpotted && (bool)lockingRadar && lockingRadar.radar.radarEnabled && unlockWhileSearching)
					{
						if (lockingRadar.IsLocked())
						{
							lockingRadar.Unlock();
						}
						lockingRadar.radar.radarEnabled = false;
					}
					yield return fireWait;
				}
				if (requireRadarLock)
				{
					while (!lockingRadar || !lockingRadar.radar || lockingRadar.radar.destroyed)
					{
						yield return fireWait;
					}
				}
				float startSearchTime = Time.time;
				while (alive && !targetFinder.attackingTarget)
				{
					if ((bool)lockingRadar && Time.time - startSearchTime > 5f)
					{
						if (unlockWhileSearching && radarLock != null && radarLock.locked)
						{
							lockingRadar.Unlock();
						}
						if (radarOnlyOnSpotted)
						{
							lockingRadar.radar.radarEnabled = false;
						}
					}
					yield return null;
				}
				Actor tgt = targetFinder.attackingTarget;
				if (!alive || (unlockWhileSearching && radarLock != null && radarLock.actor != tgt))
				{
					lockingRadar.Unlock();
					chaffModule = null;
				}
				if (!alive)
				{
					break;
				}
				if (radarOnlyOnSpotted && (bool)lockingRadar && (bool)lockingRadar.radar && !lockingRadar.radar.radarEnabled)
				{
					if (Vector3.Distance(tgt.position + tgt.velocity * preLockOnTime, base.transform.position) < maxFiringRange)
					{
						lockingRadar.radar.radarEnabled = true;
						yield return new WaitForSeconds(preLockOnTime);
					}
					else
					{
						tgt = null;
					}
				}
				if ((bool)tgt && Vector3.Distance(tgt.position, base.transform.position) < maxFiringRange)
				{
					bool flag = true;
					if (requireRadarLock)
					{
						if ((bool)lockingRadar && ((radarLock != null && radarLock.locked && radarLock.actor == tgt) || lockingRadar.GetLock(tgt)))
						{
							chaffModule = radarLock.actor.GetChaffModule();
							flag = true;
						}
						else
						{
							flag = false;
						}
					}
					if (flag)
					{
						yield return StartCoroutine(FireRoutine(tgt));
					}
					else
					{
						yield return fireWait;
					}
				}
				else
				{
					yield return fireWait;
				}
			}
			else
			{
				yield return fireWait;
			}
			yield return null;
		}
	}

	private Vector3 CalculatedTargetPosition(Actor target)
	{
		Vector3 affectedPos = target.position;
		Vector3 affectedVel = target.velocity;
		if (requireRadarLock && (bool)chaffModule)
		{
			chaffModule.GetAdvChaffAffectedPos(lockingRadar.referenceTransform.position, affectedPos - lockingRadar.referenceTransform.position, 10f, out affectedPos, out affectedVel);
		}
		Vector3 vector = gun.GetCalculatedTargetPosition(affectedPos, affectedVel) - referenceTransform.position;
		if (inaccuracyFactor > 0f)
		{
			float angle = VectorUtils.FullRangePerlinNoise(perlinSeed, Time.time * inaccuracyRate) * inaccuracyFactor;
			float angle2 = VectorUtils.FullRangePerlinNoise(perlinSeed * 2f, Time.time * inaccuracyRate) * inaccuracyFactor;
			vector = Quaternion.AngleAxis(angle, referenceTransform.up) * Quaternion.AngleAxis(angle2, referenceTransform.right) * vector;
		}
		return referenceTransform.position + vector + leadSweep * Mathf.Sin(leadSweepRate * Time.time + perlinSeed) * Time.deltaTime * target.velocity;
	}

	private IEnumerator FireRoutine(Actor targetActor)
	{
		iffCheckFrame = iffCheckInterval;
		engagingTarget = true;
		engagingActor = targetActor;
		float t = Time.time;
		float timeStartedFiring = 0f;
		while (base.enabled && alive && engageEnemies && (bool)targetActor && targetActor.alive && Time.time - t < burstTime)
		{
			if (requireRadarLock && (radarLock == null || !radarLock.locked))
			{
				isFiring = false;
				gun.SetFire(fire: false);
				break;
			}
			Vector3 vector = CalculatedTargetPosition(targetActor);
			if ((bool)turret)
			{
				turret.AimToTarget(vector);
				if (turret.TargetInRange(vector, maxFiringRange))
				{
					bool dontShoot = false;
					if (iffCheckFrame >= iffCheckInterval)
					{
						if (Physics.Raycast(gun.fireTransforms[0].position, gun.fireTransforms[0].forward, out var hitInfo, 1000f, 1024))
						{
							Hitbox component = hitInfo.collider.GetComponent<Hitbox>();
							if ((bool)component && (bool)component.actor && component.actor.team != targetActor.team)
							{
								dontShoot = true;
							}
							else
							{
								float magnitude = (targetActor.position - base.transform.position).magnitude;
								if (hitInfo.distance < magnitude * 0.7f)
								{
									dontShoot = true;
								}
							}
							if (dontShoot)
							{
								isFiring = false;
								gun.SetFire(fire: false);
								yield return new WaitForSeconds(burstTime);
							}
						}
						iffCheckFrame = 0;
					}
					else
					{
						iffCheckFrame++;
					}
					if (dontShoot)
					{
						break;
					}
					if (!isFiring)
					{
						timeStartedFiring = Time.time;
					}
					isFiring = true;
					gun.SetFire(fire: true);
				}
				else if (Time.time - timeStartedFiring > minBurstTime)
				{
					isFiring = false;
					gun.SetFire(fire: false);
				}
			}
			else
			{
				Vector3 vector2 = vector - gun.transform.position;
				if (Vector3.Angle(vector2, gun.transform.parent.forward) < nonTurretFOV / 2f && (targetActor.position - gun.transform.position).sqrMagnitude < maxFiringRange * maxFiringRange)
				{
					gun.transform.rotation = Quaternion.LookRotation(vector2);
					if (!isFiring)
					{
						timeStartedFiring = Time.time;
					}
					isFiring = true;
					gun.SetFire(fire: true);
				}
				else
				{
					isFiring = false;
					gun.SetFire(fire: false);
				}
			}
			yield return null;
		}
		isFiring = false;
		gun.SetFire(fire: false);
		yield return fireWait;
		if ((bool)turret)
		{
			turret.ReturnTurretOneshot();
		}
		engagingTarget = false;
		engagingActor = null;
	}

	public void SetEngageEnemies(bool engage)
	{
		engageEnemies = engage;
		if (radarOnlyOnSpotted && (bool)lockingRadar && (bool)lockingRadar.radar)
		{
			lockingRadar.radar.radarEnabled = false;
		}
		if (radarLock != null && radarLock.locked)
		{
			radarLock.lockingRadar.Unlock();
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode(qsNodeName);
		configNode.SetValue("engageEnemies", engageEnemies);
		configNode.SetValue("alive", alive);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(qsNodeName);
		if (node != null)
		{
			engageEnemies = node.GetValue<bool>("engageEnemies");
			alive = node.GetValue<bool>("alive");
			if (!alive && aiRoutine != null)
			{
				StopCoroutine(aiRoutine);
			}
		}
	}
}
