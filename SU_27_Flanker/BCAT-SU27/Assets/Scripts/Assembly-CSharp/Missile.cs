using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class Missile : MonoBehaviour, ISetLowPoly
{
	public enum GuidanceModes
	{
		Radar,
		Heat,
		Optical,
		Bomb,
		GPS,
		AntiRad
	}

	public enum NavModes
	{
		LeadTime,
		ViewAngle,
		Proportional,
		Custom
	}

	[Serializable]
	public struct LaunchEvent
	{
		public float delay;

		public UnityEvent launchEvent;
	}

	private class QSMissile
	{
		public GameObject missileObj;

		public Missile missile;

		public QSMissileInfo info;
	}

	private struct QSMissileInfo
	{
		public Vector3D globalPos;

		public Vector3 velocity;

		public Vector3 angularVelocity;

		public Vector3D targetGlobalPos;

		public int targetActorID;

		public Vector3D targeterGlobalPos;

		public int targeterActorID;

		public Vector3D launcherGlobalPos;

		public int launcherActorID;

		public float elapsedTime;

		public ConfigNode config;

		public Vector3D estTarPosD;

		public Vector3 estTarVel;

		public Vector3 directionFired;

		public int actorID;

		private bool radarTWSLock;

		public QSMissileInfo(Missile m)
		{
			actorID = m.actor.actorID;
			globalPos = VTMapManager.WorldToGlobalPoint(m.transform.position);
			targetGlobalPos = default(Vector3D);
			targetActorID = -1;
			targeterActorID = -1;
			targeterGlobalPos = default(Vector3D);
			velocity = Vector3.zero;
			angularVelocity = Vector3.zero;
			directionFired = m.directionFired;
			if ((bool)m.rb)
			{
				velocity = m.rb.velocity;
				angularVelocity = m.rb.angularVelocity;
			}
			elapsedTime = Time.time - m.timeFired;
			config = new ConfigNode("config");
			estTarPosD = VTMapManager.WorldToGlobalPoint(m.estTargetPos);
			estTarVel = m.estTargetVel;
			radarTWSLock = false;
			launcherGlobalPos = Vector3D.zero;
			launcherActorID = -1;
			if ((bool)m.launcherRB)
			{
				Actor component = m.launcherRB.GetComponent<Actor>();
				if ((bool)component)
				{
					if ((bool)component.unitSpawn)
					{
						launcherActorID = component.unitSpawn.unitID;
					}
					else
					{
						launcherActorID = -2;
						launcherGlobalPos = VTMapManager.WorldToGlobalPoint(component.position);
					}
				}
			}
			switch (m.guidanceMode)
			{
			case GuidanceModes.Radar:
			{
				if (m.radarLock == null || !(m.radarLock.lockingRadar != null))
				{
					break;
				}
				Actor myActor = m.radarLock.lockingRadar.myActor;
				if ((bool)myActor)
				{
					if (myActor == m.actor)
					{
						targeterActorID = -3;
					}
					else if ((bool)myActor.unitSpawn)
					{
						targeterActorID = myActor.unitSpawn.unitID;
					}
					else
					{
						targeterGlobalPos = VTMapManager.WorldToGlobalPoint(myActor.position);
						targeterActorID = -2;
					}
				}
				if (m.radarLock.locked)
				{
					Actor actor = m.radarLock.actor;
					targetGlobalPos = VTMapManager.WorldToGlobalPoint(actor.position);
					if ((bool)actor.unitSpawn)
					{
						targetActorID = actor.unitSpawn.unitID;
					}
					else
					{
						targetActorID = -2;
					}
					break;
				}
				LockingRadar.AdvLockData advLockData = m.twsData;
				if (advLockData == null)
				{
					advLockData = m.radarLock.lockingRadar.GetTWSLockUpdate(m.radarLock);
				}
				if (advLockData != null)
				{
					radarTWSLock = true;
					QuicksaveManager.SaveActorIdentifier(advLockData.actor, out targetActorID, out targetGlobalPos, out var _);
				}
				break;
			}
			case GuidanceModes.Optical:
				if ((bool)m.opticalTargetActor)
				{
					targetGlobalPos = VTMapManager.WorldToGlobalPoint(m.opticalTargetActor.position);
					if ((bool)m.opticalTargetActor.unitSpawn)
					{
						targetActorID = m.opticalTargetActor.unitSpawn.unitID;
					}
					else
					{
						targetActorID = -2;
					}
				}
				if ((bool)m.opticalTargeter)
				{
					Actor actor2 = m.opticalTargeter.actor;
					if ((bool)actor2)
					{
						QuicksaveManager.SaveActorIdentifier(actor2, out targeterActorID, out targeterGlobalPos, out var _);
					}
				}
				break;
			case GuidanceModes.GPS:
				if (m.gpsTarget != null)
				{
					targetGlobalPos = VTMapManager.WorldToGlobalPoint(m.gpsTarget.worldPosition);
					targetActorID = -2;
				}
				break;
			case GuidanceModes.AntiRad:
				if ((bool)m.antiRadTargetActor)
				{
					targetGlobalPos = VTMapManager.WorldToGlobalPoint(m.antiRadTargetActor.position);
					if ((bool)m.antiRadTargetActor.unitSpawn)
					{
						targetActorID = m.antiRadTargetActor.unitSpawn.unitID;
					}
					else
					{
						targetActorID = -2;
					}
					ConfigNode configNode = new ConfigNode("antiRadRWR");
					config.AddNode(configNode);
					m.antiRadRWR.OnQuicksave(configNode);
				}
				break;
			}
			if ((bool)m.guidanceUnit)
			{
				ConfigNode configNode2 = new ConfigNode("guidanceUnit");
				config.AddNode(configNode2);
				m.guidanceUnit.SaveToQuicksaveNode(configNode2);
			}
			IQSMissileComponent[] componentsInChildrenImplementing = m.gameObject.GetComponentsInChildrenImplementing<IQSMissileComponent>(includeInactive: true);
			if (componentsInChildrenImplementing.Length != 0)
			{
				ConfigNode configNode3 = new ConfigNode("missileComponents");
				for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
				{
					componentsInChildrenImplementing[i].OnQuicksavedMissile(configNode3, elapsedTime);
				}
				config.AddNode(configNode3);
			}
		}

		public void ApplyToMissile(Missile m)
		{
			Debug.Log("Quickloading missile: " + m.gameObject.name);
			m.actor.actorID = actorID;
			m.QL_GetReferences();
			m.QS_SetTargetData(VTMapManager.GlobalToWorldPoint(estTarPosD), estTarVel);
			m.directionFired = directionFired;
			m.transform.position = VTMapManager.GlobalToWorldPoint(globalPos);
			if ((bool)m.rb)
			{
				m.rb.position = m.transform.position;
				m.rb.velocity = velocity;
				m.rb.angularVelocity = angularVelocity;
			}
			m.SetStartTime(elapsedTime);
			Debug.Log(" - elapsedTime: " + elapsedTime);
			Debug.Log(" - new timeFired: " + m.timeFired);
			Actor targetActor = GetTargetActor(launcherActorID, launcherGlobalPos);
			if ((bool)targetActor)
			{
				m.launcherRB = targetActor.GetComponent<Rigidbody>();
			}
			switch (m.guidanceMode)
			{
			case GuidanceModes.Radar:
				Debug.Log(" - Radar missile");
				if (targetActorID != -1)
				{
					Actor targetActor3 = GetTargetActor(targetActorID, targetGlobalPos);
					if ((bool)targetActor3)
					{
						Debug.Log(" - radar target: " + targetActor3.actorName);
						if (targeterActorID == -3)
						{
							Debug.Log(" - forcing lock with missile's radar");
							m.lockingRadar.ForceLock(targetActor3, out var lockData);
							m.SetRadarLock(lockData);
							break;
						}
						Actor targetActor4 = GetTargetActor(targeterActorID, targeterGlobalPos);
						if ((bool)targetActor4)
						{
							Debug.Log(" - locking radar actor found.");
							LockingRadar lockingRadar = null;
							if ((bool)targetActor4.weaponManager && (bool)targetActor4.weaponManager.lockingRadar)
							{
								lockingRadar = targetActor4.weaponManager.lockingRadar;
							}
							else
							{
								LockingRadar[] componentsInChildren = targetActor4.GetComponentsInChildren<LockingRadar>();
								foreach (LockingRadar lockingRadar2 in componentsInChildren)
								{
									if (!lockingRadar2.isMissile)
									{
										lockingRadar = lockingRadar2;
										break;
									}
								}
							}
							if ((bool)lockingRadar)
							{
								if (radarTWSLock)
								{
									Debug.Log(" - TWS lock");
									RadarLockData radarLockData = new RadarLockData();
									radarLockData.actor = targetActor3;
									radarLockData.lockingRadar = lockingRadar;
									if ((bool)lockingRadar.radar)
									{
										radarLockData.radarSymbol = lockingRadar.radar.radarSymbol;
									}
									else
									{
										Debug.Log(" - locking radar had no \"radar\": " + UIUtils.GetHierarchyString(lockingRadar.gameObject));
										m.radarLock.radarSymbol = "?";
									}
									radarLockData.locked = false;
									m.SetRadarLock(radarLockData);
									lockingRadar.UpdateTWSLock(targetActor3);
									LockingRadar.AdvLockData tWSLockUpdate = lockingRadar.GetTWSLockUpdate(m.radarLock);
									if (tWSLockUpdate != null)
									{
										Debug.Log(" - - TWS Data successful!");
										m.QS_SetTWSLock(tWSLockUpdate);
									}
									else
									{
										Debug.Log(" - - TWS Data unsuccessful...");
									}
								}
								else if (!lockingRadar.IsLocked() || !(lockingRadar.currentLock.actor == targetActor3))
								{
									lockingRadar.ForceLock(targetActor3, out var lockData2);
									m.SetRadarLock(lockData2);
									Debug.Log(" - - forced radar lock on target");
								}
							}
							else
							{
								Debug.Log(" - could not find locking radar in children.");
							}
						}
						else
						{
							Debug.Log(" - locking radar actor not found.");
						}
					}
					else
					{
						Debug.Log(" - could not quickload radar target actor...");
					}
				}
				else
				{
					Debug.Log(" - no radar target saved.");
				}
				break;
			case GuidanceModes.Optical:
			{
				Debug.Log(" - Optical missile ");
				OpticalTargeter opticalTargeter = null;
				Actor actor = QuicksaveManager.RetrieveActor(targeterActorID, targeterGlobalPos, -1);
				if ((bool)actor)
				{
					opticalTargeter = ((!actor.weaponManager || !actor.weaponManager.opticalTargeter) ? actor.GetComponentInChildren<OpticalTargeter>(includeInactive: true) : actor.weaponManager.opticalTargeter);
					if ((bool)opticalTargeter)
					{
						Debug.Log(" - Targeter found: " + (opticalTargeter.actor ? opticalTargeter.actor.actorName : opticalTargeter.gameObject.name));
					}
					else
					{
						Debug.Log(" - No targeter found.");
					}
				}
				Actor targetActor2 = GetTargetActor(targetActorID, targetGlobalPos);
				Transform transform = null;
				if ((bool)opticalTargeter)
				{
					transform = opticalTargeter.lockTransform;
				}
				else if (m.opticalFAF && (bool)targetActor2)
				{
					transform = targetActor2.transform;
				}
				if ((bool)transform)
				{
					m.SetOpticalTarget(transform, targetActor2, opticalTargeter);
				}
				break;
			}
			case GuidanceModes.GPS:
				if (targetActorID == -2)
				{
					m.SetGPSTarget(new GPSTarget(VTMapManager.GlobalToWorldPoint(targetGlobalPos), "ql ", 0));
				}
				break;
			case GuidanceModes.AntiRad:
				Debug.Log(" - AntiRad missile");
				m.antiRadTargetActor = GetTargetActor(targetActorID, targetGlobalPos);
				if ((bool)m.antiRadTargetActor)
				{
					Debug.Log(" - anti rad target found.");
					if (config.HasNode("antiRadRWR"))
					{
						Debug.Log(" - - quickloading missile's RWR data");
						m.antiRadRWR.enabled = true;
						m.antiRadRWR.OnQuickload(config.GetNode("antiRadRWR"));
					}
					m.timeUpdatedAntirad = Time.time;
				}
				else
				{
					Debug.Log(" - anti rad target NOT FOUND");
				}
				break;
			}
			if ((bool)m.guidanceUnit && config.HasNode("guidanceUnit"))
			{
				m.guidanceUnit.LoadFromQuicksaveNode(m, config.GetNode("guidanceUnit"));
			}
			if (config.HasNode("missileComponents"))
			{
				IQSMissileComponent[] componentsInChildrenImplementing = m.gameObject.GetComponentsInChildrenImplementing<IQSMissileComponent>(includeInactive: true);
				ConfigNode node = config.GetNode("missileComponents");
				for (int j = 0; j < componentsInChildrenImplementing.Length; j++)
				{
					componentsInChildrenImplementing[j].OnQuickloadedMissile(node, elapsedTime);
				}
			}
			m.QL_ResumeMissile(elapsedTime);
		}

		private Actor GetTargetActor(int unitID, Vector3D targetGPos)
		{
			if (unitID >= 0)
			{
				return GetUnitActor(unitID);
			}
			if (unitID == -2)
			{
				return GetActorAtPosition(targetGPos);
			}
			return null;
		}

		private Actor GetUnitActor(int unitID)
		{
			return VTScenario.current.units.GetUnit(unitID).spawnedUnit.actor;
		}

		private Actor GetActorAtPosition(Vector3D globalPos)
		{
			Vector3 vector = VTMapManager.GlobalToWorldPoint(globalPos);
			Actor result = null;
			float num = 250000f;
			foreach (Actor allActor in TargetManager.instance.allActors)
			{
				float sqrMagnitude = (allActor.position - vector).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = allActor;
				}
			}
			return result;
		}
	}

	public bool isLocal = true;

	public static List<Missile> allFiredMissiles = new List<Missile>();

	public Actor launchedByActor;

	private Actor _actor;

	[Header("Components")]
	public Transform edgeTransform;

	public MissileGuidanceUnit guidanceUnit;

	public Transform exhaustTransform;

	public GameObject hiddenMissileObject;

	public GameObject highPolyModel;

	public GameObject lowPolyModel;

	[Header("Rigidbody")]
	public float mass;

	public float angularDrag;

	[Header("Guidance")]
	public GuidanceModes guidanceMode;

	public bool insBackup = true;

	public NavModes navMode;

	public float leadTimeMultiplier = 1f;

	[Header("Thrust")]
	public bool launchWarn = true;

	public float boostThrust;

	public float boostTime;

	public float cruiseThrust;

	public float cruiseTime;

	public float thrustDelay;

	public float initialKick;

	[Header("Torque")]
	public float maxTorque;

	private float torqueMultiplier;

	public float maxTorqueSpeed = 300f;

	public float torqueRampUpRate = 1f;

	public float torqueDelay;

	public float minTorqueSpeed = 100f;

	public float torqueToPrograde;

	[Header("Steering")]
	public float maxAoA;

	public float steerMult;

	public float steerIntegral;

	public float maxSteerIntegralAccum;

	public float maxLeadTime = 8f;

	public bool squareInput;

	public float maxBallisticOffset = 100f;

	public float minBallisticCalcSpeed;

	private Vector3 steerAccum;

	[Header("Roll")]
	public bool flyRoll;

	public float rollTorque;

	public float rollUpBias = 0.4f;

	[Header("Decoupling")]
	public Vector2 torqueKickOnLaunch;

	public float decoupleSpeed;

	public float railLength;

	[Header("Damage")]
	public float explodeRadius;

	public float explodeDamage;

	public float proxyDetonateRange;

	public ExplosionManager.ExplosionTypes explosionType = ExplosionManager.ExplosionTypes.Aerial;

	[HideInInspector]
	public Rigidbody launcherRB;

	private ParticleSystem[] ps;

	private Light exhaustLight;

	private AudioSource audioSource;

	[Header("Radar")]
	public float pitbullRange;

	private bool active;

	public LockingRadar lockingRadar;

	private float radarLostTime = -1f;

	[Header("Optical")]
	public float opticalFOV = 60f;

	public bool opticalFAF;

	public bool opticalLOAL;

	private float lastOLOALSearchTime;

	private Transform opticalTarget;

	private OpticalTargeter opticalTargeter;

	protected FixedPoint staticOpticalTargetLock;

	[Header("IR")]
	public HeatSeeker heatSeeker;

	private GPSTarget gpsTarget;

	private bool gpsTargetAcquired;

	[Header("AntiRad")]
	public ModuleRWR antiRadRWR;

	public Actor antiRadTargetActor;

	[Header("Launch Events")]
	public List<LaunchEvent> launchEvents;

	public UnityEvent OnDetonate;

	private Hitbox[] hitboxes;

	private Collider[] colliders;

	private SimpleDrag simpleDrag;

	private HeatEmitter heatEmitter;

	public bool debugMissile;

	private Transform debugCamTf;

	private float debugGs;

	private Vector3 debugLastVelocity;

	public PlayerInfo sourcePlayer;

	private float thrustHeat;

	private Collider directHitCollider;

	private Vector3 finalTorque;

	private Vector3 lastProportionalAccel;

	private bool missed;

	private Vector3 explosionNormal = Vector3.up;

	private bool detonated;

	public bool debugExplosion;

	private float currentSpeed;

	protected float lastTargetDistance = float.MaxValue;

	private bool hasLOALInitialTarget;

	private FixedPoint loalInitialTarget;

	private Vector3 estTargetAccel;

	private float timeUpdatedAntirad;

	private const int GROUND_TARGET_VEL_COUNT = 10;

	private Vector3[] groundTargetVels = new Vector3[10];

	private float lastGroundTargetVelTime;

	private float groundTargetVelInterval = 0.5f;

	private int currGTVIdx;

	private int gtvAvailCount;

	public float radarLockFov = 2f;

	private bool startedRadarLook;

	private Vector3 lastRadarLookDir;

	private static List<Actor> opticalLoalTgtBuffer = new List<Actor>();

	private LockingRadar datalinkOnlyRadar;

	private Vector3 directionFired;

	private Transform firedRailTf;

	private Vector3 firedRailOffset;

	private ChaffCountermeasure chaffModule;

	private bool missileResumed;

	private static List<QSMissile> quicksavedMissiles = new List<QSMissile>();

	private static List<Missile> quickloadedMissiles = new List<Missile>();

	public Actor actor
	{
		get
		{
			if (!_actor)
			{
				_actor = GetComponent<Actor>();
			}
			return _actor;
		}
		private set
		{
			_actor = value;
		}
	}

	public Vector3 decoupleDirection
	{
		get
		{
			if (!overrideDecoupleDirTf)
			{
				return -base.transform.up;
			}
			return -overrideDecoupleDirTf.up;
		}
	}

	public Transform overrideDecoupleDirTf { get; set; }

	public Rigidbody rb { get; private set; }

	public bool fired { get; private set; }

	public float timeFired { get; private set; }

	public float timeToImpact { get; private set; }

	public RadarLockData radarLock { get; private set; }

	public LockingRadar.AdvLockData twsData { get; private set; }

	public bool isPitbull => active;

	public LockingRadar surrogateRadar
	{
		get
		{
			if (!active && radarLock != null && radarLock.locked && (bool)radarLock.lockingRadar)
			{
				return radarLock.lockingRadar;
			}
			return null;
		}
	}

	public Actor opticalTargetActor { get; private set; }

	public bool hasTarget
	{
		get
		{
			switch (guidanceMode)
			{
			case GuidanceModes.Radar:
				if (missed)
				{
					return false;
				}
				if (!datalinkOnlyActor)
				{
					if (radarLock != null)
					{
						if (!radarLock.locked || !(radarLock.actor != null))
						{
							return twsData != null;
						}
						return true;
					}
					return false;
				}
				return true;
			case GuidanceModes.Optical:
				if (!opticalFAF)
				{
					return opticalTarget != null;
				}
				return true;
			case GuidanceModes.Heat:
				if (heatSeeker != null)
				{
					return heatSeeker.seekerLock > 0.5f;
				}
				return false;
			case GuidanceModes.GPS:
				return gpsTargetAcquired;
			case GuidanceModes.AntiRad:
				return true;
			default:
				return false;
			}
		}
	}

	private float AIRSPEED_HEAT_MULT => VTOLVRConstants.MISSILE_AIRSPEED_HEAT_MULT;

	private float THRUST_HEAT_MULT => VTOLVRConstants.MISSILE_THRUST_HEAT_MULT;

	private float COOLDOWN_SQRSPEED_DIV => VTOLVRConstants.MISSILE_COOLDOWN_SQRSPEED_DIV;

	public Vector3 estTargetPos { get; private set; }

	public Vector3 estTargetVel { get; private set; }

	public Actor datalinkOnlyActor { get; private set; }

	public event UnityAction<Missile> OnMissileDetonated;

	public event UnityAction OnFired;

	public static void DestroyAllFiredMissiles()
	{
		foreach (Missile allFiredMissile in allFiredMissiles)
		{
			if ((bool)allFiredMissile && (bool)allFiredMissile.gameObject)
			{
				UnityEngine.Object.Destroy(allFiredMissile.gameObject);
			}
		}
	}

	public void SetLowPoly()
	{
		if ((bool)highPolyModel)
		{
			UnityEngine.Object.Destroy(highPolyModel);
		}
		if ((bool)lowPolyModel)
		{
			lowPolyModel.SetActive(value: true);
		}
	}

	public void SetOpticalTarget(Transform tgt, Actor actr = null, OpticalTargeter targeter = null)
	{
		opticalTargeter = targeter;
		if (opticalFAF)
		{
			if ((bool)actr)
			{
				opticalTarget = actr.transform;
				staticOpticalTargetLock = new FixedPoint(actr.position);
				estTargetPos = actr.position;
				estTargetVel = actr.velocity;
			}
			else
			{
				opticalTarget = null;
				staticOpticalTargetLock = new FixedPoint(tgt.position);
			}
		}
		else
		{
			opticalTarget = tgt;
		}
		opticalTargetActor = actr;
	}

	public void SetGPSTarget(GPSTarget target)
	{
		if (target != null)
		{
			gpsTarget = target;
			gpsTargetAcquired = true;
		}
	}

	private void Awake()
	{
		if ((bool)exhaustTransform)
		{
			ps = exhaustTransform.GetComponentsInChildren<ParticleSystem>();
			ps.SetEmissionAndActive(emit: false);
			exhaustLight = exhaustTransform.GetComponentInChildren<Light>();
		}
		if ((bool)exhaustLight)
		{
			exhaustLight.enabled = false;
		}
		audioSource = GetComponent<AudioSource>();
		if (!lockingRadar)
		{
			lockingRadar = GetComponentInChildren<LockingRadar>(includeInactive: true);
		}
		if ((bool)lockingRadar)
		{
			lockingRadar.enabled = false;
		}
		Wing[] componentsInChildren = GetComponentsInChildren<Wing>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		OmniWing[] componentsInChildren2 = GetComponentsInChildren<OmniWing>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].enabled = false;
		}
		hitboxes = GetComponentsInChildren<Hitbox>();
		Hitbox[] array = hitboxes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: false);
		}
		colliders = GetComponentsInChildren<Collider>(includeInactive: true);
		Collider[] array2 = colliders;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = false;
		}
		simpleDrag = GetComponentInChildren<SimpleDrag>();
		simpleDrag.enabled = false;
		heatEmitter = base.gameObject.GetComponent<HeatEmitter>();
		if (!heatEmitter)
		{
			heatEmitter = base.gameObject.AddComponent<HeatEmitter>();
		}
		SolidBooster[] componentsInChildren3 = GetComponentsInChildren<SolidBooster>(includeInactive: true);
		foreach (SolidBooster solidBooster in componentsInChildren3)
		{
			if (!solidBooster.heatEmitter)
			{
				solidBooster.heatEmitter = heatEmitter;
			}
		}
		heatEmitter.fwdAspect = true;
		if ((bool)antiRadRWR)
		{
			antiRadRWR.enabled = false;
			antiRadRWR.overridePersistTime = 10f;
		}
		Health component = GetComponent<Health>();
		if ((bool)component)
		{
			component.OnDeath.AddListener(Detonate);
		}
		base.enabled = false;
	}

	private void UpdateHeat()
	{
		float num = currentSpeed * currentSpeed;
		float num2 = thrustHeat + 0.5f * AIRSPEED_HEAT_MULT * num;
		heatEmitter.AddHeat(num2 * Time.deltaTime);
		heatEmitter.cooldownRate = Mathf.Clamp(num / COOLDOWN_SQRSPEED_DIV, 100f, 500f);
	}

	private void LateUpdate()
	{
		if (!fired)
		{
			return;
		}
		currentSpeed = rb.velocity.magnitude;
		UpdateHeat();
		if (isLocal)
		{
			if (guidanceMode == GuidanceModes.Radar)
			{
				if (radarLock != null && (bool)radarLock.actor)
				{
					if (active)
					{
						if (!radarLock.locked)
						{
							active = false;
						}
					}
					else if (!missed && (bool)lockingRadar && Vector3.Distance(radarLock.actor.position, base.transform.position) < pitbullRange && lockingRadar.TransferLock(radarLock))
					{
						active = true;
						radarLock = lockingRadar.currentLock;
						startedRadarLook = false;
						chaffModule = radarLock.actor.GetChaffModule();
					}
					radarLostTime = -1f;
				}
				else if ((bool)datalinkOnlyActor)
				{
					if (!active && !missed && (estTargetPos - base.transform.position).magnitude < pitbullRange && lockingRadar.TryPitbullLockActor(datalinkOnlyActor, out var lockData))
					{
						radarLock = lockData;
						active = true;
						startedRadarLook = false;
						chaffModule = radarLock.actor.GetChaffModule();
						Debug.Log("Datalink only pitbull lock SUCCESS");
					}
					radarLostTime = -1f;
				}
				else if (!missed)
				{
					if (radarLostTime > 0f && Time.time - radarLostTime > 3f)
					{
						if (debugMissile)
						{
							Debug.Log("No radar lock, delayed detonating. ID: " + base.gameObject.GetInstanceID());
						}
						missed = true;
						finalTorque = Vector3.zero;
						StartCoroutine(DelayedDetonate(UnityEngine.Random.Range(2f, 6f)));
					}
					else
					{
						radarLostTime = Time.time;
					}
				}
			}
			for (int i = 0; i < hitboxes.Length; i++)
			{
				hitboxes[i].gameObject.layer = 2;
			}
			bool flag = false;
			if (Time.time - timeFired > thrustDelay + 1f && Physics.Raycast(base.transform.position - rb.velocity * Time.fixedDeltaTime, rb.velocity, out var hitInfo, currentSpeed * Time.fixedDeltaTime * 3f, 1025))
			{
				Hitbox component = hitInfo.collider.GetComponent<Hitbox>();
				if (!component || !component.actor || component.actor.finalCombatRole != Actor.Roles.Missile || component.actor.team != actor.team)
				{
					Vector3 position = hitInfo.point + hitInfo.normal * 0.1f;
					if (hitInfo.collider.gameObject.layer == 0)
					{
						position.y += 0.5f;
					}
					base.transform.position = position;
					if ((bool)component && (bool)component.actor && component.actor.finalCombatRole == Actor.Roles.Ship)
					{
						explosionNormal = Vector3.up;
					}
					else
					{
						explosionNormal = hitInfo.normal;
					}
					flag = true;
					directHitCollider = hitInfo.collider;
				}
			}
			if (!flag && (bool)WaterPhysics.instance && base.transform.position.y < WaterPhysics.instance.height)
			{
				BulletHitManager.instance.CreateSplash(base.transform.position, rb.velocity);
				flag = true;
			}
			if (!flag && (bool)VTCustomMapManager.instance && !VTCustomMapManager.instance.mapGenerator.IsChunkColliderEnabled(base.transform.position))
			{
				float altitude = WaterPhysics.GetAltitude(base.transform.position);
				float heightmapAltitude = VTCustomMapManager.instance.mapGenerator.GetHeightmapAltitude(base.transform.position);
				if (altitude < heightmapAltitude - 15f)
				{
					Debug.Log("Missile " + base.gameObject.name + " flew below terrain height in non-collision area");
					flag = true;
				}
			}
			if (flag)
			{
				Detonate(directHitCollider);
			}
			for (int j = 0; j < hitboxes.Length; j++)
			{
				hitboxes[j].gameObject.layer = 10;
			}
			CheckMiss();
			if (!missed)
			{
				SteerToTarget();
				UpdateTimeToImpact();
			}
		}
		if (debugMissile && (bool)debugCamTf)
		{
			debugCamTf.transform.position = base.transform.position + -15f * rb.velocity.normalized + 2f * Vector3.up;
			debugCamTf.LookAt(base.transform.position);
			Debug.DrawLine(base.transform.position, estTargetPos, Color.magenta);
		}
	}

	private bool TargetInProxyRange(out float dist)
	{
		Vector3 position = base.transform.position;
		Vector3 vector = estTargetPos;
		float num = proxyDetonateRange * proxyDetonateRange;
		if ((position - vector).sqrMagnitude < 16f * num)
		{
			Vector3 b = base.transform.position + rb.velocity * Time.fixedDeltaTime;
			Vector3 b2 = estTargetPos + estTargetVel * Time.fixedDeltaTime;
			float num2 = float.MaxValue;
			for (float num3 = 0f; num3 <= 1f; num3 += 0.02f)
			{
				Vector3 vector2 = Vector3.Lerp(position, b, num3);
				float sqrMagnitude = (Vector3.Lerp(vector, b2, num3) - vector2).sqrMagnitude;
				if (sqrMagnitude > num2)
				{
					dist = -1f;
					return false;
				}
				if (sqrMagnitude < num)
				{
					dist = Mathf.Sqrt(sqrMagnitude);
					return true;
				}
				num2 = sqrMagnitude;
			}
		}
		dist = -1f;
		return false;
	}

	private void FixedUpdate()
	{
		if (isLocal && fired)
		{
			if (TargetInProxyRange(out var dist))
			{
				base.transform.position = estTargetPos - rb.velocity.normalized * dist;
				Detonate();
			}
			rb.AddRelativeTorque(finalTorque);
			if (torqueToPrograde > 0f)
			{
				rb.AddTorque((0f - torqueToPrograde) * currentSpeed * 0.00125f * Vector3.Cross(rb.velocity, base.transform.forward));
			}
			if (debugMissile)
			{
				debugGs = (rb.velocity - debugLastVelocity).sqrMagnitude / (9.81f * Time.fixedDeltaTime);
				debugLastVelocity = rb.velocity;
			}
		}
	}

	private void SteerToTarget()
	{
		if (guidanceMode == GuidanceModes.Bomb)
		{
			finalTorque = Vector3.zero;
			return;
		}
		UpdateTargetData();
		if (!(Time.time - timeFired > thrustDelay))
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		switch (navMode)
		{
		case NavModes.LeadTime:
			vector = GuidedPoint();
			break;
		case NavModes.ViewAngle:
			vector = BallisticPoint(estTargetPos, base.transform.position, currentSpeed);
			break;
		case NavModes.Proportional:
			vector = ProportionalTargetPoint();
			break;
		case NavModes.Custom:
			vector = guidanceUnit.GetGuidedPoint();
			break;
		}
		float num = Vector3.Angle(rb.transform.forward, rb.velocity);
		Vector3 vector2 = rb.velocity;
		if (navMode == NavModes.ViewAngle)
		{
			vector2 = ViewAngleTargetDirection();
		}
		else if (lastTargetDistance < 1600f && navMode != NavModes.Proportional)
		{
			vector2 = rb.velocity;
		}
		vector2.Normalize();
		Vector3 vector3 = vector - base.transform.position;
		Vector3 current = estTargetPos - base.transform.position;
		if (guidanceMode == GuidanceModes.Radar)
		{
			if (active && radarLock != null && radarLock.locked)
			{
				vector3 = Vector3.RotateTowards(current, vector3, lockingRadar.fov / 2f * ((float)Math.PI / 180f) * 0.75f, 0f);
			}
		}
		else if (guidanceMode == GuidanceModes.Optical)
		{
			vector3 = Vector3.RotateTowards(current, vector3, opticalFOV / 2f * ((float)Math.PI / 180f) * 0.75f, 0f);
		}
		else if (guidanceMode == GuidanceModes.Heat)
		{
			vector3 = Vector3.RotateTowards(current, vector3, heatSeeker.gimbalFOV / 2f * ((float)Math.PI / 180f) * 0.75f, 0f);
		}
		vector3.Normalize();
		float num2 = Vector3.Angle(vector2, vector3) * Mathf.Clamp01(2f - 2f * (num / Mathf.Max(maxAoA, 0.0001f)));
		float num3 = Mathf.Clamp01(rb.velocity.sqrMagnitude / (maxTorqueSpeed * maxTorqueSpeed));
		if (steerIntegral > 0f)
		{
			Vector3 vector4 = vector3 - vector2;
			steerAccum += steerIntegral * vector4 * Time.deltaTime;
			steerAccum = Vector3.ClampMagnitude(steerAccum, maxSteerIntegralAccum);
			vector3 += steerAccum;
		}
		Vector3 direction = -Vector3.Cross(vector3, vector2);
		direction = rb.transform.InverseTransformDirection(direction);
		float num4 = Mathf.Clamp((squareInput ? (0.25f * num2 * num2) : num2) * steerMult * num3 * torqueMultiplier, 0f, maxTorque);
		direction.z = 0f;
		finalTorque = direction.normalized * num4;
		if (flyRoll)
		{
			Vector3 normalized = Vector3.ProjectOnPlane(vector3, vector2).normalized;
			normalized += rollUpBias * Vector3.up;
			normalized.Normalize();
			Vector3 vector5 = Vector3.Cross(base.transform.up, normalized) * (rollTorque * num3);
			vector5 = base.transform.InverseTransformVector(vector5);
			finalTorque.z += vector5.z * torqueMultiplier;
		}
	}

	private bool CheckSightLine(Vector3 targetPos)
	{
		return !Physics.Linecast(base.transform.position, targetPos, 1);
	}

	private Vector3 ViewAngleTargetDirection()
	{
		Vector3 vector = estTargetPos - base.transform.position;
		float magnitude = Vector3.ProjectOnPlane(estTargetVel, vector).magnitude;
		float angle = Mathf.Min(leadTimeMultiplier * 0.12f * magnitude, 30f);
		Vector3 axis = -Vector3.Cross(vector, estTargetVel);
		return Quaternion.AngleAxis(angle, axis) * rb.velocity;
	}

	private Vector3 ProportionalTargetPoint()
	{
		Vector3 lhs = estTargetPos - base.transform.position;
		Vector3 vector = 1f * leadTimeMultiplier * estTargetVel - rb.velocity;
		Vector3 vector2 = Vector3.Cross(lhs, -vector) / lhs.sqrMagnitude;
		Vector3 vector3 = Vector3.Cross(vector, -vector2);
		return base.transform.position + base.transform.forward * 100f + vector3;
	}

	private void CheckMiss()
	{
		if (guidanceMode == GuidanceModes.Bomb || guidanceMode == GuidanceModes.GPS || guidanceMode == GuidanceModes.AntiRad || opticalLOAL || missed)
		{
			return;
		}
		if (Time.time - timeFired > 3f + thrustDelay && Vector3.Dot(rb.velocity, estTargetPos - rb.position) < 0f)
		{
			if (debugMissile)
			{
				Debug.Log("Missile miss due to target behind missile.", base.gameObject);
			}
			missed = true;
		}
		if (Time.time - timeFired > thrustDelay + boostTime + cruiseTime && Vector3.Dot(rb.velocity - estTargetVel, estTargetPos - rb.position) < 0f)
		{
			if (debugMissile)
			{
				Debug.Log("Missile miss due to negative closing rate after cruise phase");
			}
			missed = true;
		}
		if (missed)
		{
			radarLock = null;
			maxTorque = 0f;
			finalTorque = Vector3.zero;
			StartCoroutine(DelayedDetonate(UnityEngine.Random.Range(0.5f, 3f)));
		}
	}

	private IEnumerator RailRoutine(Transform parentTf, Rigidbody parentRb)
	{
		Vector3 localDir = parentTf.InverseTransformDirection(base.transform.forward);
		Vector3 origin = parentTf.InverseTransformPoint(base.transform.position);
		Vector3 localUp = parentTf.InverseTransformDirection(base.transform.up);
		float dist = 0f;
		float accel = boostThrust / rb.mass;
		float localSpeed = 0f;
		rb.isKinematic = true;
		rb.interpolation = RigidbodyInterpolation.None;
		Vector3 localPos = origin;
		while ((localPos - origin).sqrMagnitude < railLength * railLength)
		{
			localSpeed += accel * Time.deltaTime;
			dist += localSpeed * Time.deltaTime;
			localPos = origin + localDir * dist;
			base.transform.position = parentTf.TransformPoint(localPos);
			base.transform.rotation = Quaternion.LookRotation(parentTf.TransformDirection(localDir), parentTf.TransformDirection(localUp));
			rb.position = base.transform.position;
			rb.rotation = base.transform.rotation;
			torqueMultiplier = 0f;
			yield return null;
		}
		torqueMultiplier = 0f;
		rb.isKinematic = false;
		rb.interpolation = RigidbodyInterpolation.Extrapolate;
		rb.velocity = parentRb.GetPointVelocity(base.transform.position) + localSpeed * parentTf.TransformDirection(localDir);
		rb.angularVelocity = Vector3.zero;
	}

	private IEnumerator DelayedDetonate(float time)
	{
		if (guidanceMode != GuidanceModes.Optical && guidanceMode != GuidanceModes.Bomb)
		{
			yield return new WaitForSeconds(time);
			Detonate();
		}
	}

	public void Detonate()
	{
		Detonate(null);
	}

	private void Detonate(Collider directHit)
	{
		if (detonated)
		{
			return;
		}
		detonated = true;
		Vector3 sourceVelocity = Vector3.one;
		if ((bool)rb)
		{
			sourceVelocity = rb.velocity;
		}
		else
		{
			Debug.LogError("Missile tried to detonate without a rigidbody!");
		}
		for (int i = 0; i < colliders.Length; i++)
		{
			if ((bool)colliders[i])
			{
				colliders[i].enabled = false;
			}
		}
		if (explosionType == ExplosionManager.ExplosionTypes.Aerial)
		{
			explosionNormal = sourceVelocity;
		}
		ExplosionManager.instance.CreateExplosionEffect(explosionType, base.transform.position, explosionNormal);
		if (isLocal)
		{
			ExplosionManager.instance.CreateDamageExplosion(base.transform.position, explodeRadius, explodeDamage, actor, sourceVelocity, directHit, debugExplosion, sourcePlayer);
		}
		OnDetonate?.Invoke();
		this.OnMissileDetonated?.Invoke(this);
		if ((bool)exhaustTransform)
		{
			ps.SetEmission(emit: false);
			exhaustTransform.parent = null;
			FloatingOriginTransform floatingOriginTransform = exhaustTransform.gameObject.GetComponent<FloatingOriginTransform>();
			if (!floatingOriginTransform)
			{
				floatingOriginTransform = exhaustTransform.gameObject.AddComponent<FloatingOriginTransform>();
			}
			floatingOriginTransform.shiftParticles = false;
			UnityEngine.Object.Destroy(exhaustTransform.gameObject, ps.GetLongestLife());
			if ((bool)exhaustLight)
			{
				exhaustLight.enabled = false;
			}
		}
		if (VTOLMPUtils.IsMultiplayer())
		{
			base.gameObject.SetActive(value: false);
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	protected virtual Vector3 GuidedPoint()
	{
		Vector3 targetPosition = BallisticLeadTargetPoint(estTargetPos, estTargetVel, rb.position, rb.velocity, Mathf.Max(minBallisticCalcSpeed, currentSpeed), leadTimeMultiplier, maxBallisticOffset, maxLeadTime, estTargetAccel);
		return ApplyCM(targetPosition);
	}

	protected virtual void UpdateTimeToImpact()
	{
		Vector3 onNormal = estTargetPos - base.transform.position;
		float magnitude = Vector3.Project(rb.velocity - estTargetVel, onNormal).magnitude;
		float magnitude2 = onNormal.magnitude;
		timeToImpact = magnitude2 / magnitude;
	}

	public static Vector3 BallisticLeadTargetPoint(Vector3 estTargetPos, Vector3 estTargetVel, Vector3 missilePosition, Vector3 missileVelocity, float currentSpeed, float leadTimeMultiplier, float maxBallisticOffset, float maxLeadTime)
	{
		return BallisticLeadTargetPoint(estTargetPos, estTargetVel, missilePosition, missileVelocity, currentSpeed, leadTimeMultiplier, maxBallisticOffset, maxLeadTime, Vector3.zero);
	}

	public static Vector3 BallisticLeadTargetPoint(Vector3 estTargetPos, Vector3 estTargetVel, Vector3 missilePosition, Vector3 missileVelocity, float currentSpeed, float leadTimeMultiplier, float maxBallisticOffset, float maxLeadTime, Vector3 estTargetAccel)
	{
		float b = 200f;
		float num = 0f;
		float num2 = Vector3.Distance(estTargetPos, missilePosition);
		Vector3 vector = Mathf.Max(currentSpeed, b) * missileVelocity.normalized;
		num = num2 / (estTargetVel - vector).magnitude;
		num = Mathf.Clamp(leadTimeMultiplier * num, 0f, maxLeadTime);
		Vector3 vector2 = estTargetPos + estTargetVel * num;
		if (num2 < 1000f)
		{
			vector2 += 0.5f * num * num * estTargetAccel;
		}
		Vector3 result = vector2;
		if (maxBallisticOffset > 0f)
		{
			float num3 = Mathf.Lerp(3f, 1f, maxBallisticOffset / 90f);
			result = BallisticPoint(vector2, missilePosition, currentSpeed * num3);
		}
		return result;
	}

	public static Vector3 GetLeadPoint(Vector3 targetPosition, Vector3 targetVelocity, Vector3 missilePosition, Vector3 missileVel, float maxLeadTime = -1f)
	{
		float num = Vector3.Distance(targetPosition, missilePosition);
		Vector3 vector = Mathf.Max(missileVel.magnitude, 200f) * missileVel.normalized;
		float num2 = 1f / ((targetVelocity - vector).magnitude / num);
		if (maxLeadTime > 0f)
		{
			num2 = Mathf.Min(num2, maxLeadTime);
		}
		return targetPosition + targetVelocity * num2;
	}

	public void SetLOALInitialTarget(Vector3 pos)
	{
		hasLOALInitialTarget = true;
		loalInitialTarget = new FixedPoint(pos + UnityEngine.Random.insideUnitSphere * 150f);
	}

	private bool CheckCanSeePoint(Vector3 pt)
	{
		if (Physics.Linecast(base.transform.position, pt, out var hitInfo, 1) && (hitInfo.point - pt).sqrMagnitude > 25f)
		{
			if ((bool)opticalTargetActor && hitInfo.collider.GetComponentInParent<Actor>() == opticalTargetActor)
			{
				return true;
			}
			return false;
		}
		return true;
	}

	private Vector3 GetAvgGroundTargetVel(Vector3 currVel)
	{
		if (Time.time - lastGroundTargetVelTime > groundTargetVelInterval)
		{
			groundTargetVels[currGTVIdx] = currVel;
			currGTVIdx = (currGTVIdx + 1) % 10;
			lastGroundTargetVelTime = Time.time;
			gtvAvailCount = Mathf.Min(gtvAvailCount + 1, 10);
		}
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < gtvAvailCount; i++)
		{
			zero += groundTargetVels[i];
		}
		return zero / gtvAvailCount;
	}

	private void UpdateTargetData()
	{
		Vector3 vector = estTargetVel;
		float num = Time.deltaTime;
		if (num == 0f)
		{
			num = Time.fixedDeltaTime;
		}
		switch (guidanceMode)
		{
		case GuidanceModes.Radar:
			if (radarLock != null)
			{
				if (radarLock.locked)
				{
					if (Radar.ADV_RADAR && (bool)chaffModule)
					{
						if (!startedRadarLook)
						{
							startedRadarLook = true;
							lastRadarLookDir = radarLock.actor.position - radarLock.lockingRadar.referenceTransform.position;
						}
						if (chaffModule.GetAdvChaffAffectedPos(radarLock.lockingRadar.referenceTransform.position, lastRadarLookDir, radarLockFov, out var affectedPos, out var affectedVel))
						{
							estTargetPos = affectedPos;
							estTargetVel = affectedVel;
							lastRadarLookDir = estTargetPos - radarLock.lockingRadar.referenceTransform.position;
							break;
						}
						if (!missed)
						{
							StartCoroutine(DelayedDetonate(timeToImpact));
						}
						missed = true;
						EstimateTarget();
						Debug.Log(base.gameObject.name + " lost radar target tracking due to countermeasures.");
					}
					else
					{
						estTargetPos = radarLock.actor.position;
						estTargetVel = radarLock.actor.velocity;
					}
					break;
				}
				twsData = radarLock.lockingRadar.GetTWSLockUpdate(radarLock);
				if (twsData != null)
				{
					if (twsData.lockStatus != LockingRadar.AdvLockData.LockStatus.Standby)
					{
						estTargetPos = twsData.position;
						estTargetVel = twsData.velocity;
						twsData.lockStatus = LockingRadar.AdvLockData.LockStatus.Received;
					}
					else
					{
						EstimateTarget();
					}
					break;
				}
				if (!missed)
				{
					StartCoroutine(DelayedDetonate(UnityEngine.Random.Range(10f, 20f)));
				}
				missed = true;
				Debug.Log(base.gameObject.name + " lost radar target tracking.");
				EstimateTarget();
			}
			else if ((bool)datalinkOnlyRadar)
			{
				twsData = datalinkOnlyRadar.GetTWSLockUpdate(datalinkOnlyActor);
				if (twsData != null)
				{
					if (twsData.lockStatus != LockingRadar.AdvLockData.LockStatus.Standby)
					{
						estTargetPos = twsData.position;
						estTargetVel = twsData.velocity;
						twsData.lockStatus = LockingRadar.AdvLockData.LockStatus.Received;
					}
					else
					{
						EstimateTarget();
					}
					break;
				}
				if (!missed)
				{
					StartCoroutine(DelayedDetonate(UnityEngine.Random.Range(10f, 20f)));
				}
				missed = true;
				Debug.Log(base.gameObject.name + " lost radar target tracking.");
				EstimateTarget();
			}
			else if (insBackup)
			{
				EstimateTarget();
			}
			else
			{
				estTargetPos = base.transform.position + base.transform.forward * 1000f;
				estTargetVel = Vector3.zero;
			}
			break;
		case GuidanceModes.Optical:
			if (opticalTarget != null)
			{
				if (opticalFAF)
				{
					if (!CheckCanSeePoint(opticalTarget.position))
					{
						if (insBackup)
						{
							EstimateTarget();
							break;
						}
						estTargetPos = base.transform.position + base.transform.forward * 10f;
						estTargetVel = Vector3.zero;
						break;
					}
					Vector3 vector2 = estTargetPos;
					estTargetPos = opticalTarget.position;
					Vector3 currVel = (opticalTargeter ? opticalTargeter.targetVelocity : ((estTargetPos - vector2) / num));
					if ((bool)opticalTargetActor)
					{
						currVel = opticalTargetActor.velocity;
					}
					estTargetVel = GetAvgGroundTargetVel(currVel);
				}
				else if ((bool)opticalTargeter && (opticalTargeter.laserOccluded || !(Vector3.Angle(opticalTarget.position - base.transform.position, base.transform.forward) < opticalFOV / 2f)))
				{
					if (insBackup)
					{
						EstimateTarget();
					}
					else if (hasLOALInitialTarget)
					{
						estTargetPos = loalInitialTarget.point;
						estTargetVel = Vector3.zero;
					}
					else
					{
						estTargetPos = base.transform.position + base.transform.forward * 10f;
						estTargetVel = Vector3.zero;
					}
				}
				else
				{
					Vector3 vector3 = estTargetPos;
					estTargetPos = opticalTarget.position;
					Vector3 currVel2 = (opticalTargeter ? opticalTargeter.targetVelocity : ((estTargetPos - vector3) / num));
					if ((bool)opticalTargetActor)
					{
						currVel2 = opticalTargetActor.velocity;
					}
					estTargetVel = GetAvgGroundTargetVel(currVel2);
				}
			}
			else if (opticalFAF)
			{
				if (opticalLOAL)
				{
					if (CheckCanSeePoint(staticOpticalTargetLock.point))
					{
						estTargetPos = staticOpticalTargetLock.point;
						estTargetVel = Vector3.zero;
					}
					else
					{
						estTargetPos = base.transform.position + directionFired.normalized * 1000f;
						estTargetVel = Vector3.zero;
					}
					if (!(Time.time - lastOLOALSearchTime > 1f))
					{
						break;
					}
					float num3 = 500f;
					Vector3 direction = staticOpticalTargetLock.point - base.transform.position;
					float maxRadius = direction.magnitude + num3;
					int roleMask = Actor.GetRoleMask(Actor.Roles.Ground, Actor.Roles.GroundArmor);
					List<Actor> list = opticalLoalTgtBuffer;
					TargetManager.instance.GetAllOpticalTargetsInView(this.actor, 1f, 100f, maxRadius, roleMask, base.transform.position, direction, list);
					float num4 = num3 * num3;
					Actor actor = null;
					for (int i = 0; i < list.Count; i++)
					{
						float sqrMagnitude = (list[i].position - staticOpticalTargetLock.point).sqrMagnitude;
						if (sqrMagnitude < num4)
						{
							actor = list[i];
							num4 = sqrMagnitude;
						}
					}
					if ((bool)actor)
					{
						SetOpticalTarget(actor.transform, actor);
					}
					lastOLOALSearchTime = Time.time;
				}
				else
				{
					estTargetPos = staticOpticalTargetLock.point;
					estTargetVel = Vector3.zero;
				}
			}
			else if (insBackup)
			{
				EstimateTarget();
			}
			else
			{
				estTargetPos = base.transform.position + base.transform.forward * 10f;
				estTargetVel = Vector3.zero;
			}
			break;
		case GuidanceModes.Heat:
			if (heatSeeker.seekerLock > 0f)
			{
				estTargetPos = heatSeeker.targetPosition;
				estTargetVel = heatSeeker.targetVelocity;
			}
			else
			{
				EstimateTarget();
			}
			break;
		case GuidanceModes.GPS:
			if (gpsTargetAcquired)
			{
				estTargetPos = gpsTarget.worldPosition;
				estTargetVel = Vector3.zero;
			}
			else
			{
				EstimateTarget();
			}
			break;
		case GuidanceModes.AntiRad:
		{
			bool flag = false;
			int num2 = 0;
			while (!flag && num2 < antiRadRWR.contacts.Length)
			{
				ModuleRWR.RWRContact rWRContact = antiRadRWR.contacts[num2];
				if (rWRContact != null && rWRContact.active && rWRContact.radarActor == antiRadTargetActor)
				{
					estTargetPos = rWRContact.detectedPosition + (Time.time - rWRContact.GetTimeDetected()) * estTargetVel;
					estTargetVel = rWRContact.radarActor.velocity;
					flag = true;
					timeUpdatedAntirad = Time.time;
				}
				num2++;
			}
			if (!flag)
			{
				if (insBackup || Time.time - timeUpdatedAntirad < 2f)
				{
					EstimateTarget();
					break;
				}
				estTargetPos = base.transform.position + rb.velocity * 0.5f;
				estTargetVel = Vector3.zero;
			}
			break;
		}
		}
		estTargetAccel = (estTargetVel - vector) / num;
		lastTargetDistance = Vector3.Distance(base.transform.position, estTargetPos);
	}

	private void EstimateTarget()
	{
		estTargetPos += estTargetVel * Time.deltaTime;
	}

	public static Vector3 BallisticPoint(Vector3 targetPosition, Vector3 missilePosition, float speed)
	{
		Vector3 up = Vector3.up;
		Vector3 vector = Vector3.ProjectOnPlane(targetPosition - missilePosition, up);
		float num = speed * speed;
		float num2 = num * num;
		float magnitude = Physics.gravity.magnitude;
		float num3 = targetPosition.y - missilePosition.y;
		float sqrMagnitude = vector.sqrMagnitude;
		float num4 = Mathf.Sqrt(sqrMagnitude);
		float num5 = ((1 == 0) ? 1 : (-1));
		float num6 = num + num5 * Mathf.Sqrt(num2 - magnitude * (magnitude * sqrMagnitude + 2f * num3 * num));
		float num7 = magnitude * num4;
		float num8 = Mathf.Atan(num6 / num7);
		Vector3 vector2 = (float.IsNaN(num8) ? (Quaternion.AngleAxis(45f, Vector3.Cross(vector, up)) * vector) : (Quaternion.AngleAxis(num8 * 57.29578f, Vector3.Cross(vector, up)) * vector));
		return missilePosition + vector2.normalized * (targetPosition - missilePosition).magnitude;
	}

	public void SetRadarLock(RadarLockData lockData)
	{
		radarLock = lockData;
	}

	public void SetDataLinkOnly(LockingRadar lr, Actor datalinkTarget, Vector3 initialTargetPos)
	{
		estTargetPos = initialTargetPos;
		datalinkOnlyRadar = lr;
		datalinkOnlyActor = datalinkTarget;
	}

	public void Fire()
	{
		if (fired)
		{
			Debug.LogError("Missile.Fire() was called but this missile was already fired!", base.gameObject);
			return;
		}
		base.enabled = true;
		actor = GetComponent<Actor>();
		rb = base.gameObject.AddComponent<Rigidbody>();
		allFiredMissiles.Add(this);
		if ((bool)heatEmitter)
		{
			heatEmitter.actor = actor;
		}
		if ((bool)simpleDrag)
		{
			simpleDrag.enabled = true;
		}
		if ((bool)lockingRadar)
		{
			lockingRadar.enabled = true;
			lockingRadar.myActor = actor;
		}
		if (!actor)
		{
			Debug.LogWarning("Missile was fired without an actor!");
		}
		if ((bool)CameraFollowMe.instance)
		{
			CameraFollowMe.instance.AddTarget(actor);
		}
		timeFired = Time.time;
		if ((bool)launcherRB)
		{
			directionFired = launcherRB.transform.forward;
		}
		else
		{
			directionFired = base.transform.forward;
		}
		if (!launcherRB)
		{
			Rigidbody[] componentsInParent = GetComponentsInParent<Rigidbody>();
			foreach (Rigidbody rigidbody in componentsInParent)
			{
				if (rigidbody.GetInstanceID() != rb.GetInstanceID())
				{
					launcherRB = rigidbody;
				}
			}
		}
		base.gameObject.AddComponent<FloatingOriginTransform>().SetRigidbody(rb);
		base.transform.parent = null;
		Hitbox[] array = hitboxes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: true);
		}
		Collider[] array2 = colliders;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = true;
		}
		rb.mass = mass;
		if (isLocal)
		{
			rb.angularDrag = angularDrag;
			rb.isKinematic = false;
			if ((bool)launcherRB)
			{
				rb.velocity = launcherRB.velocity;
			}
			rb.velocity += initialKick * base.transform.forward;
			rb.velocity += decoupleSpeed * decoupleDirection;
			torqueMultiplier = 0f;
			Vector3 torque = UnityEngine.Random.Range(torqueKickOnLaunch.x, torqueKickOnLaunch.y) * UnityEngine.Random.onUnitSphere;
			torque.z *= 0.25f;
			rb.AddRelativeTorque(torque, ForceMode.Impulse);
			if (guidanceMode == GuidanceModes.Radar && radarLock != null && radarLock.locked && (bool)radarLock.actor)
			{
				MissileDetector component = radarLock.actor.GetComponent<MissileDetector>();
				if ((bool)component)
				{
					component.AddMissile(this);
				}
				chaffModule = radarLock.actor.GetChaffModule();
			}
		}
		else
		{
			rb.isKinematic = true;
		}
		fired = true;
		if (isLocal)
		{
			Wing[] componentsInChildren = GetComponentsInChildren<Wing>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = true;
			}
			OmniWing[] componentsInChildren2 = GetComponentsInChildren<OmniWing>();
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				componentsInChildren2[i].enabled = true;
			}
		}
		IParentRBDependent[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IParentRBDependent>(includeInactive: true);
		for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
		{
			componentsInChildrenImplementing[i].SetParentRigidbody(rb);
		}
		FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
		if (isLocal && railLength > 0f && (bool)launcherRB)
		{
			StartCoroutine(RailRoutine(launcherRB.transform, launcherRB));
		}
		if ((bool)launcherRB && isLocal)
		{
			firedRailTf = launcherRB.transform;
			firedRailOffset = firedRailTf.InverseTransformPoint(base.transform.position);
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			FloatingOrigin.instance.AddQueuedFixedUpdateAction(FixRbPosition);
		}
		else
		{
			rb.interpolation = RigidbodyInterpolation.Interpolate;
		}
		if (debugMissile)
		{
			Camera camera = new GameObject().AddComponent<Camera>();
			camera.transform.parent = base.transform;
			camera.transform.localPosition = new Vector3(0f, 2f, -15f);
			camera.transform.localRotation = Quaternion.identity;
			camera.fieldOfView = 40f;
			camera.depth = 10f;
			camera.stereoTargetEye = StereoTargetEyeMask.None;
			camera.nearClipPlane = 1f;
			camera.farClipPlane = 15000f;
			camera.transform.rotation = Quaternion.LookRotation(camera.transform.forward);
			camera.rect = new Rect(0.64f, 0.36f, 0.32f, 0.32f);
			debugCamTf = camera.transform;
		}
		StartCoroutine(ThrustRoutine());
		this.OnFired?.Invoke();
	}

	private void FixRbPosition()
	{
		if ((bool)firedRailTf && (bool)rb)
		{
			Vector3 vector = firedRailTf.TransformPoint(firedRailOffset) - rb.position;
			rb.position += vector;
		}
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		estTargetPos += offset;
	}

	private void OnDestroy()
	{
		if (fired)
		{
			FloatingOrigin.instance.OnOriginShift -= FloatingOrigin_instance_OnOriginShift;
			allFiredMissiles.Remove(this);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(base.transform.position, base.transform.position + railLength * base.transform.forward);
	}

	private IEnumerator ThrustRoutine(bool resumed = false, float elapsedTime = 0f)
	{
		if (launchEvents != null)
		{
			for (int i = 0; i < launchEvents.Count; i++)
			{
				StartCoroutine(LaunchEventRoutine(launchEvents[i], resumed, elapsedTime));
			}
		}
		if (elapsedTime < thrustDelay)
		{
			yield return new WaitForSeconds(thrustDelay - elapsedTime);
		}
		if (launchWarn && elapsedTime < thrustDelay + 3f)
		{
			MissileDetector.AnnounceMissileLaunch(this);
		}
		StartCoroutine(TorqueRampUpRoutine(torqueDelay - Mathf.Max(0f, elapsedTime - thrustDelay)));
		if (resumed && !(elapsedTime < thrustDelay + boostTime + cruiseTime))
		{
			yield break;
		}
		if ((bool)exhaustTransform)
		{
			ps.SetEmissionAndActive(emit: true);
		}
		if ((bool)exhaustLight && elapsedTime - thrustDelay < 1f)
		{
			exhaustLight.enabled = true;
			StartCoroutine(DisableLightAfterLaunchRoutine());
		}
		if ((bool)audioSource)
		{
			audioSource.Play();
		}
		if (navMode == NavModes.Custom && !guidanceUnit.guidanceEnabled)
		{
			guidanceUnit.BeginGuidance(this);
		}
		float finalBoostTime = boostTime;
		if (resumed)
		{
			finalBoostTime = boostTime - (elapsedTime - thrustDelay);
		}
		float t2 = Time.time;
		thrustHeat = THRUST_HEAT_MULT * boostThrust;
		while (Time.time - t2 < finalBoostTime)
		{
			if (isLocal)
			{
				rb.AddRelativeForce(boostThrust * Vector3.forward);
			}
			yield return new WaitForFixedUpdate();
		}
		float finalCruiseTime = cruiseTime;
		if (resumed)
		{
			finalCruiseTime = cruiseTime - (elapsedTime - thrustDelay - boostTime);
		}
		t2 = Time.time;
		thrustHeat = THRUST_HEAT_MULT * cruiseThrust;
		while (Time.time - t2 < finalCruiseTime)
		{
			if (isLocal)
			{
				rb.AddRelativeForce(cruiseThrust * Vector3.forward);
			}
			yield return new WaitForFixedUpdate();
		}
		thrustHeat = 0f;
		if ((bool)exhaustTransform)
		{
			ParticleSystem[] array = ps;
			foreach (ParticleSystem particleSystem in array)
			{
				StartCoroutine(RampDownParticles(particleSystem));
			}
		}
		if ((bool)audioSource)
		{
			audioSource.Stop();
		}
	}

	private IEnumerator DisableLightAfterLaunchRoutine()
	{
		yield return new WaitForSeconds(1f);
		exhaustLight.enabled = false;
	}

	private IEnumerator TorqueRampUpRoutine(float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		while (rb.velocity.sqrMagnitude < minTorqueSpeed * minTorqueSpeed)
		{
			yield return null;
		}
		while (torqueMultiplier < 1f)
		{
			torqueMultiplier = Mathf.MoveTowards(torqueMultiplier, 1f, torqueRampUpRate * Time.fixedDeltaTime);
			yield return new WaitForFixedUpdate();
		}
		torqueMultiplier = 1f;
	}

	private IEnumerator RampDownParticles(ParticleSystem ps)
	{
		float lerpRate = ((ps.main.simulationSpace == ParticleSystemSimulationSpace.World) ? 0.3f : 5f);
		ParticleSystem.EmissionModule em = ps.emission;
		float currEmis = em.rateOverTime.constant;
		ParticleSystem.MainModule main = ps.main;
		float size = main.startSize.constant;
		while ((bool)ps && currEmis > 50f)
		{
			currEmis = Mathf.Lerp(currEmis, 0f, lerpRate * Time.deltaTime);
			em.rateOverTime = new ParticleSystem.MinMaxCurve(currEmis);
			size = Mathf.Lerp(size, 0.01f, lerpRate * Time.deltaTime);
			main.startSize = new ParticleSystem.MinMaxCurve(size);
			if ((bool)exhaustLight)
			{
				exhaustLight.intensity = Mathf.Lerp(exhaustLight.intensity, 0f, lerpRate * Time.deltaTime);
			}
			yield return null;
		}
		em.enabled = false;
		if ((bool)exhaustLight)
		{
			exhaustLight.enabled = false;
		}
	}

	private Vector3 ApplyCM(Vector3 targetPosition)
	{
		if (guidanceMode == GuidanceModes.Radar && !Radar.ADV_RADAR && (bool)chaffModule)
		{
			return ApplyChaffDiversion(targetPosition);
		}
		return targetPosition;
	}

	private Vector3 ApplyChaffDiversion(Vector3 targetPosition)
	{
		float num = chaffModule.GetMagnitude() * 2f;
		if (num > 0f)
		{
			float num2 = Time.time - timeFired;
			float num3 = 0.5f;
			float num4 = VectorUtils.FullRangePerlinNoise(num2 * num3, 0f) * num;
			float num5 = VectorUtils.FullRangePerlinNoise(num2 * num3, 1f) * num;
			targetPosition += num4 * base.transform.right;
			targetPosition += num5 * base.transform.up;
		}
		return targetPosition;
	}

	public void SetAoALimiter(float aoa)
	{
		maxAoA = aoa;
	}

	public void SetMaxTorqueSpeed(float speed)
	{
		maxTorqueSpeed = speed;
	}

	public void SetAngularDrag(float angDrag)
	{
		if (fired && (bool)rb)
		{
			rb.angularDrag = angDrag;
		}
		else
		{
			angularDrag = angDrag;
		}
	}

	private IEnumerator LaunchEventRoutine(LaunchEvent e, bool resumed, float elapsedTime)
	{
		float num = e.delay;
		if (resumed)
		{
			num -= elapsedTime;
		}
		if (num > 0f || !resumed)
		{
			yield return new WaitForSeconds(num);
			e.launchEvent.Invoke();
		}
	}

	public void SetStartTime(float elapsedTime)
	{
		timeFired = Time.time - elapsedTime;
	}

	public void QL_GetReferences()
	{
		actor = GetComponent<Actor>();
		actor.SetMissile(this);
		rb = GetComponent<Rigidbody>();
		actor.SetParentRigidbody(rb);
		if ((bool)exhaustTransform)
		{
			ps = exhaustTransform.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
			ps.SetEmissionAndActive(emit: false);
			exhaustLight = exhaustTransform.GetComponentInChildren<Light>(includeInactive: true);
		}
		audioSource = GetComponent<AudioSource>();
		lockingRadar = GetComponentInChildren<LockingRadar>(includeInactive: true);
		hitboxes = GetComponentsInChildren<Hitbox>(includeInactive: true);
		colliders = GetComponentsInChildren<Collider>(includeInactive: true);
		simpleDrag = GetComponentInChildren<SimpleDrag>(includeInactive: true);
		heatEmitter = base.gameObject.GetComponent<HeatEmitter>();
		heatEmitter.fwdAspect = true;
		if ((bool)heatEmitter)
		{
			heatEmitter.actor = actor;
		}
		if ((bool)simpleDrag)
		{
			simpleDrag.enabled = true;
		}
		if ((bool)lockingRadar)
		{
			lockingRadar.enabled = true;
			lockingRadar.myActor = actor;
		}
		base.gameObject.GetComponent<FloatingOriginTransform>().SetRigidbody(rb);
		base.transform.parent = null;
	}

	public void QL_ResumeMissile(float elapsedTime)
	{
		base.enabled = true;
		missileResumed = true;
		allFiredMissiles.Add(this);
		if ((bool)CameraFollowMe.instance)
		{
			CameraFollowMe.instance.AddTarget(actor);
		}
		Hitbox[] array = hitboxes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: true);
		}
		Collider[] array2 = colliders;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = true;
		}
		rb.mass = mass;
		rb.angularDrag = angularDrag;
		rb.isKinematic = false;
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		if (guidanceMode == GuidanceModes.Radar && radarLock != null && radarLock.locked && (bool)radarLock.actor)
		{
			MissileDetector component = radarLock.actor.GetComponent<MissileDetector>();
			if ((bool)component)
			{
				component.AddMissile(this);
			}
			chaffModule = radarLock.actor.GetChaffModule();
		}
		fired = true;
		Wing[] componentsInChildren = GetComponentsInChildren<Wing>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = true;
		}
		OmniWing[] componentsInChildren2 = GetComponentsInChildren<OmniWing>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].enabled = true;
		}
		IParentRBDependent[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IParentRBDependent>(includeInactive: true);
		for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
		{
			componentsInChildrenImplementing[i].SetParentRigidbody(rb);
		}
		FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		StartCoroutine(ThrustRoutine(resumed: true, elapsedTime));
		if (guidanceMode == GuidanceModes.Radar)
		{
			string text = "Resuming Radar missile " + base.gameObject.name;
			text += "\n radarLock: ";
			if (radarLock != null)
			{
				text = text + "\n\t actor: " + (radarLock.actor ? radarLock.actor.actorName : "null");
				text = text + "\n\t lockingRadar: " + (radarLock.lockingRadar ? UIUtils.GetHierarchyString(radarLock.lockingRadar.gameObject) : "null");
				text = text + "\n\t radarSymbol: " + radarLock.radarSymbol;
			}
			else
			{
				text += "null";
			}
			Debug.Log(text);
		}
	}

	public void QS_SetTargetData(Vector3 position, Vector3 vel)
	{
		estTargetPos = position;
		estTargetVel = vel;
	}

	public void QS_SetTWSLock(LockingRadar.AdvLockData data)
	{
		twsData = data;
	}

	public static void QuicksaveMissiles()
	{
		quickloadedMissiles.Clear();
		foreach (QSMissile quicksavedMissile in quicksavedMissiles)
		{
			if ((bool)quicksavedMissile.missileObj)
			{
				UnityEngine.Object.Destroy(quicksavedMissile.missileObj);
			}
		}
		quicksavedMissiles.Clear();
		foreach (Missile allFiredMissile in allFiredMissiles)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(allFiredMissile.gameObject);
			gameObject.name = allFiredMissile.gameObject.name;
			QSMissile qSMissile = new QSMissile
			{
				missileObj = gameObject,
				missile = gameObject.GetComponent<Missile>(),
				info = new QSMissileInfo(allFiredMissile)
			};
			qSMissile.missile.actor.actorID = allFiredMissile.actor.actorID;
			quicksavedMissiles.Add(qSMissile);
			Rigidbody component = gameObject.GetComponent<Rigidbody>();
			if ((bool)component)
			{
				component.interpolation = RigidbodyInterpolation.None;
			}
			gameObject.SetActive(value: false);
		}
	}

	public static void QuickloadMissiles()
	{
		quickloadedMissiles.Clear();
		if (quicksavedMissiles == null)
		{
			return;
		}
		for (int i = 0; i < quicksavedMissiles.Count; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(quicksavedMissiles[i].missileObj);
			gameObject.name = quicksavedMissiles[i].missileObj.name;
			gameObject.gameObject.SetActive(value: true);
			Missile component = gameObject.GetComponent<Missile>();
			try
			{
				quicksavedMissiles[i].info.ApplyToMissile(component);
				quickloadedMissiles.Add(component);
			}
			catch (Exception ex)
			{
				Debug.LogError("Error quickloading missile! \n" + ex);
				QuicksaveManager.instance.IndicateError();
				UnityEngine.Object.Destroy(gameObject);
			}
		}
	}

	public static Missile GetQuicksavedMissile(int actorID)
	{
		Debug.Log("GetQuicksavedMissile(" + actorID + ")");
		foreach (Missile quickloadedMissile in quickloadedMissiles)
		{
			if ((bool)quickloadedMissile && quickloadedMissile.actor.actorID == actorID)
			{
				Debug.Log(" - Returning " + quickloadedMissile.actor.actorName);
				return quickloadedMissile;
			}
		}
		Debug.Log(" - Returning null.");
		return null;
	}
}
