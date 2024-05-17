using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class AIPilot : MonoBehaviour, IEngageEnemies, IQSVehicleComponent
{
	public enum CommandStates
	{
		Navigation,
		Orbit,
		Combat,
		Evade,
		Park,
		Taxi,
		Land,
		AirRefuel,
		FollowLeader,
		Override
	}

	public enum CombatRoles
	{
		Fighter,
		FighterAttack,
		Attack,
		Bomber
	}

	private enum TakeOffStates
	{
		None,
		TaxiToRunway,
		Preparing,
		RunningUp,
		Climbing
	}

	private enum CTOStates
	{
		None,
		WaitAuthorization,
		WaitTaxiClearance,
		TaxiToCat,
		PrepareTakeOff,
		Launching,
		Ascending
	}

	private enum LandingStates
	{
		None,
		WaitAuthorization,
		FlyToStarting,
		FlyToRunway,
		StoppingOnRunway,
		Taxiing,
		Aborting,
		Bolter
	}

	public enum CarpetBombResumeStates
	{
		None,
		Calculation,
		SetupSurfaceAttack,
		FlyingToLineUp,
		Bombing,
		PostBombing
	}

	private enum RefTfLocks
	{
		None,
		AirbaseTaxi,
		Taxi,
		Combat,
		PostPadTaxi
	}

	private enum LandOnPadStates
	{
		None,
		PreApproach,
		Approach,
		Transition,
		RailLanding,
		Taxiing
	}

	[Serializable]
	private class EvadeTargetInfo
	{
		public Actor actor;

		public bool isHeatMissile;

		public bool isRadarMissile;

		public bool isUnknownMissile;

		public bool isFiringPlane;

		public bool isFiringGroundUnit;

		public float startTime;

		public float evadeDuration;

		public bool isMissile
		{
			get
			{
				if (!isHeatMissile && !isRadarMissile)
				{
					return isUnknownMissile;
				}
				return true;
			}
		}

		public Vector3 position => actor.position;

		public Vector3 velocity => actor.velocity;

		public void Reset()
		{
			actor = null;
			isHeatMissile = false;
			isRadarMissile = false;
			isUnknownMissile = false;
			isFiringPlane = false;
			isFiringGroundUnit = false;
			startTime = Time.time;
			evadeDuration = 5f;
		}

		public ConfigNode SaveToConfigNode(string nodeName)
		{
			ConfigNode configNode = new ConfigNode(nodeName);
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(actor, "actor"));
			configNode.SetValue("isHeatMissile", isHeatMissile);
			configNode.SetValue("isRadarMissile", isRadarMissile);
			configNode.SetValue("isUnknownMissile", isUnknownMissile);
			configNode.SetValue("isFiringPlane", isFiringPlane);
			configNode.SetValue("isFiringGroundUnit", isFiringGroundUnit);
			return configNode;
		}

		public static EvadeTargetInfo LoadFromConfigNode(ConfigNode node)
		{
			return new EvadeTargetInfo
			{
				actor = QuicksaveManager.RetrieveActorFromNode(node.GetNode("actor")),
				isHeatMissile = node.GetValue<bool>("isHeatMissile"),
				isRadarMissile = node.GetValue<bool>("isRadarMissile"),
				isUnknownMissile = node.GetValue<bool>("isUnknownMissile"),
				isFiringPlane = node.GetValue<bool>("isFiringPlane"),
				isFiringGroundUnit = node.GetValue<bool>("isFiringGroundUnit")
			};
		}
	}

	private bool gotActor;

	private Actor _a;

	private CommandStates _cState;

	private bool cancelOverride;

	public CombatRoles combatRole = CombatRoles.FighterAttack;

	public WingmanVoiceProfile voiceProfile;

	public float initialSpeed;

	public bool startLanded;

	public bool followPlayerOnStart;

	public float takeOffInputLimiter = 0.5f;

	public float takeOffSpeed = 80f;

	private Rigidbody rb;

	[Header("Flight Parameters")]
	public float defaultAltitude;

	public float minAltitude;

	public float minAltClimbThresh = 250f;

	public float maxAltitude = 4000f;

	public float navSpeed;

	public float maxSpeed;

	public float minCombatSpeed = 120f;

	public float taxiSpeed = 10f;

	private float defaultTaxiSpeed;

	public float carrierTaxiSpeed = 2f;

	public float orbitRadius;

	public bool orbitClockwise;

	public float navLeadDistance = 1000f;

	[Tooltip("If greater than 0, pilot will be forced to return to objective area if it exceeds this distance")]
	public float forceReturnDistance = -1f;

	public MinMax cmDeployCount = new MinMax(1f, 6f);

	public MinMax cmDeployInterval = new MinMax(0.25f, 1.5f);

	public MinMax cmDeployCooldown = new MinMax(1f, 3f);

	public float bombXCorrectionMul = 1f;

	public bool autoRefuel = true;

	private bool rearmAfterLanding;

	private bool takeOffAfterLanding;

	public float obstacleCheckAheadTime = 3f;

	public bool allowPlayerCommands = true;

	private bool returningToObjective;

	[Header("Behavior Targets")]
	public FollowPath navPath;

	public Transform orbitTransform;

	public Runway targetRunway;

	public FollowPath taxiPath;

	public AirFormationLeader formationLeader;

	private AirFormationLeader previousFormationLeader;

	private Transform formationBreakTf;

	[Tooltip("To set random inputs on death")]
	public AeroController aeroController;

	private Coroutine landingRoutine;

	private List<Collider> vesselColliders;

	private bool collidersAreDebris;

	private AirFormationLeader _formationComponent;

	private AirFormationLeader _lastFormationLeader;

	private Transform formationTransform;

	private EvadeTargetInfo evadeTarget = new EvadeTargetInfo();

	[Header("Onboard Components")]
	public float parkingSize = 5f;

	public AutoPilot autoPilot;

	public KinematicPlane kPlane;

	public VisualTargetFinder targetFinder;

	public Radar detectionRadar;

	public LockingRadar lockingRadar;

	public MissileDetector rwr;

	public GearAnimator gearAnimator;

	public CatapultHook catHook;

	public Tailhook tailHook;

	public RotationToggle wingRotator;

	public RefuelPort refuelPort;

	public Transform taxiSteerReferenceTf;

	public Transform taxiCollisionDetectTf;

	private Transform[] taxiCollisionDetectTfs;

	public ExteriorLightsController extLightsCtrlr;

	public ModuleRWR moduleRWR;

	[Space]
	public RefuelPlane targetRefuelPlane;

	private const bool debug = false;

	private string debugString = string.Empty;

	private string fixedUpdateDebugString = string.Empty;

	private Countermeasure[] cms;

	[HideInInspector]
	public WeaponManager wm;

	private float leadHelpRand;

	private float maxThrust;

	[Header("VTEvent Vars")]
	public bool vt_radarEnabled;

	private Actor overrideAttackTarget;

	private Actor attackTarget;

	private Coroutine combatRoutine;

	private List<BDCoroutine> combatDecisionRoutines = new List<BDCoroutine>();

	private List<Actor> priorityTargets = new List<Actor>();

	private List<Actor> nonTargets = new List<Actor>();

	private List<Actor> designatedTargets = new List<Actor>();

	public AIWing aiWing;

	[Header("VTOL Settings")]
	public bool isVtol;

	public VTOLAutoPilot vtolAp;

	public PID3 vtolLandingPID;

	public TiltController tiltController;

	public bool retractTiltAfterLand = true;

	[Header("Short Take Off Capability")]
	public bool sto_capable;

	public float sto_runningTilt = 65f;

	public float sto_takeOffTilt = 45f;

	public float sto_takeOffPitch = 20f;

	public float sto_takeOffSpeed = 30f;

	[Header("Radio")]
	public bool doRadioComms;

	private float _deploySafetyTime;

	private Vector3 _deploySafetyDir;

	private Vector3 _deploySafetyUp;

	private float _deploySafetyDuration = 1.5f;

	private bool wasFormation;

	private float timeBrokeFormation = -1000f;

	private bool catapulting;

	private CommandStates queuedCommand = CommandStates.Override;

	private RefuelPlane queuedRefuelPlane;

	private Health health;

	private PID formationFwdPID = new PID(0.21f, 1f, -0.15f, -0.05f, 0.05f);

	[Header("Configured Fields")]
	public bool allowEvasiveManeuvers = true;

	public float landingGlideSlope = 3.5f;

	public float landingSpeed = 78f;

	public float landingStartDistance = 5000f;

	public float gunAirMaxRange = 800f;

	public float gunGroundMaxRange = 1250f;

	[Range(25f, 80f)]
	public float gunRunAngle = 30f;

	public float gunRunStartAltitude = 1500f;

	public float gunRunMinAltitude = 100f;

	public float gunRunSpeed = 200f;

	public float missileEvadeDistance = 1200f;

	public PID landingHorizPID;

	public PID landingVertPID;

	public bool useLandingTouchdownPID;

	public PID landingTouchdownPID;

	public float landingTouchdownSpeed = -0.3f;

	private RaySpringDamper rearSusp;

	private Transform myTransform;

	private Transform headLookTransform;

	private bool crashedIntoTerrain;

	private float twr;

	private Transform fallbackOrbitTf;

	private Coroutine initialLandRoutine;

	private Coroutine initialFlyRoutine;

	private WaitForSeconds fiveSecWait = new WaitForSeconds(5f);

	private Coroutine checkForEmptyTanksRoutine;

	private WaitForSeconds jettisonDelayWait = new WaitForSeconds(0.2f);

	private TakeOffStates takeOffState;

	private float takeOffRaiser;

	public float takeOffRaiserRotateRate = 20f;

	public float takeOffRasierClimbBegin = 90f;

	public float takeOffRaiserClimbingRate = 20f;

	public AICarrierSpawn currentCarrier;

	private int _currCarrSpawnIdx = -1;

	private CTOStates ctoState;

	private WaitForSeconds oneSecWait = new WaitForSeconds(1f);

	private bool isAirbaseNavigating;

	private List<AirbaseNavNode> currentNavTransforms;

	private bool rtbAvailable = true;

	private AIPilotObstacleChecker obstChecker;

	private bool isRefuelPlane;

	private RefuelPlane myRefuelPlane;

	private bool m_playerCommsRadarEnabled = true;

	private bool startTimeParking;

	private float timeParked;

	private bool navNodes_wasLanded = true;

	private float lastTgtUpdateTime;

	private bool isExitingRefuel;

	private int lastFormationIdx;

	private bool climbingAboveMinAlt;

	private float obstacleCheckTime;

	private float obstacleCheckInterval = 2f;

	private Vector3 obstacleNormal;

	private bool avoidingObstacle;

	private bool altCheckAhead;

	private const float FORMATION_MIN_ALT = 50f;

	private const float FORMATION_CLIMB_THRESH = 80f;

	private bool obst2Mode;

	public float planarTurnaroundP;

	public float planarTurnaroundBase;

	public float planarTurnaroundMaxAngle = 45f;

	private float maxCollisionDetectTime = 10f;

	private float collisionDetectTime = -1f;

	private float collisionDetectCooldown = -1f;

	private float minTaxiCollisionDetectTime = 1f;

	private int taxiCollisionTfIdx;

	private bool completedRefuel;

	private Transform refuelQueueFormationTf;

	public float formationP = 4f;

	public float formationD = 2f;

	public float maxFormationOffsetAdjust = 15f;

	private float formationLeadDist = 1000f;

	private float formationSqrDist;

	private bool waitingForLeaderOrbit;

	public float formationRightAdjust = 50f;

	public float debug_formationDivRejoinDiv = 100f;

	private const float FAR_FLIGHT_THRESH = 4000f;

	private const float FAR_FLIGHT_THRESH_SQRD = 16000000f;

	private bool firingCms;

	private bool firingFlares;

	public CountermeasureManager cmm;

	private static WaitForSeconds[] randWaits;

	private LandingStates landingState;

	public float pidDebugMul = 1f;

	private Transform rearmParkingNodeTf;

	private AICarrierSpawn rearmCarrierSpawn;

	private FollowPath commandedASMPath;

	private AntiShipGuidance.ASMTerminalBehaviors commandedASMMode;

	private Actor lastMergeTarget;

	private Dictionary<string, bool> balanceDict = new Dictionary<string, bool>();

	private float r_carpetBomb_heading;

	private float r_carpetBomb_distInterval;

	private float r_carpetBomb_radius;

	private float r_carpetBomb_altitude;

	private HPEquipBombRack r_carpetBomb_bombEquip;

	private FixedPoint r_carpetBomb_wpt;

	private CarpetBombResumeStates r_carpetBomb_state;

	private bool _jettisonedTanks;

	private RefTfLocks refTfLockID;

	private LandOnPadStates landOnPadState;

	private AICarrierSpawn landOnPadCSpawn;

	private int apLandOnPadIdx = -1;

	private Transform landOnPadTf;

	private FixedPoint landOnPadStartApproachPt;

	private float landOnPadInHeading = -1f;

	private static List<Actor> landOnPadActorBuffer = new List<Actor>();

	private float landOnPadCurveT = -1f;

	private BezierCurveD5 landOnPadCurve;

	private float landOnPadStartSpeed;

	private WaitForFixedUpdate fixedWait = new WaitForFixedUpdate();

	private bool vtolManeuvering;

	private bool isTakingOffVtol;

	private float vtoHeading;

	private float vtoTgtAltitude;

	private bool landedJoint;

	private Vector3 landedRot;

	private Vector3 landedPos;

	private Transform landedParentTf;

	private Actor landedParentActor;

	private MovingPlatform landedJointPlatform;

	private bool quickloadLanded;

	private FixedPoint quickloadLandedPoint;

	private Vector3 quickloadVelocity;

	private Vector3 quickloadAngularVelocity;

	public Actor actor
	{
		get
		{
			if (!gotActor)
			{
				gotActor = true;
				_a = GetComponent<Actor>();
			}
			return _a;
		}
	}

	public CommandStates commandState
	{
		get
		{
			return _cState;
		}
		set
		{
			if (_cState != value)
			{
				_cState = value;
			}
		}
	}

	private FlightInfo flightInfo => autoPilot.flightInfo;

	public AirportManager.ParkingSpace landingParkingSpace { get; private set; }

	public AIAircraftSpawn aiSpawn { get; private set; }

	public AirFormationLeader formationComponent
	{
		get
		{
			if (!_formationComponent)
			{
				_formationComponent = GetComponent<AirFormationLeader>();
				if (!_formationComponent)
				{
					_formationComponent = base.gameObject.AddComponent<AirFormationLeader>();
					_formationComponent.actor = actor;
				}
			}
			return _formationComponent;
		}
	}

	public bool autoEngageEnemies { get; private set; }

	public FuelTank fuelTank { get; private set; }

	public bool isAlive { get; private set; }

	public bool hasOverrideAttackTarget => overrideAttackTarget != null;

	private bool isDeploySafety => Time.time - _deploySafetyTime < _deploySafetyDuration;

	public float thrustToWeightRatio => twr;

	public int currentCarrierSpawnIdx
	{
		get
		{
			return _currCarrSpawnIdx;
		}
		set
		{
			_currCarrSpawnIdx = value;
			Debug.LogFormat("{0} set carrier spawn idx {1}", actor.DebugName(), value);
		}
	}

	public AirbaseNavNode taxiingToNavNode
	{
		get
		{
			if (currentNavTransforms != null && currentNavTransforms.Count > 0)
			{
				return currentNavTransforms[0];
			}
			return null;
		}
	}

	public bool playerComms_radarEnabled
	{
		get
		{
			return m_playerCommsRadarEnabled;
		}
		set
		{
			m_playerCommsRadarEnabled = value;
		}
	}

	private static WaitForSeconds randThreatWait => randWaits[UnityEngine.Random.Range(0, randWaits.Length)];

	public bool rearming { get; private set; }

	public event Action OnExploded;

	public event Action OnCollisionDeath;

	public void SetRearmAfterLanding(bool rearm)
	{
		rearmAfterLanding = rearm;
	}

	public void SetTakeOffAfterLanding(bool b)
	{
		takeOffAfterLanding = b;
	}

	public void AddPriorityTarget(Actor a)
	{
		if (a != null && !priorityTargets.Contains(a))
		{
			priorityTargets.Add(a);
		}
	}

	public void ClearPriorityTargets()
	{
		Debug.Log("Clearing priority targets");
		priorityTargets.Clear();
	}

	public void AddNonTarget(Actor a)
	{
		if (a != null && !nonTargets.Contains(a))
		{
			nonTargets.Add(a);
		}
		if (attackTarget == a)
		{
			StopCombat();
		}
	}

	public void ClearNonTargets()
	{
		nonTargets.Clear();
	}

	public void AddDesignatedTarget(Actor a)
	{
		if (a != null && !designatedTargets.Contains(a))
		{
			designatedTargets.Add(a);
		}
		if ((bool)attackTarget && !designatedTargets.Contains(attackTarget))
		{
			StopCombat();
		}
	}

	public void ClearDesignatedTargets()
	{
		designatedTargets.Clear();
	}

	[ContextMenu("Auto Set Taxi Reference Tf")]
	public void AutoSetTaxiReferenceTf()
	{
		taxiSteerReferenceTf = GetComponent<WheelsController>().steeringTransform.parent;
	}

	[ContextMenu("Set Autopilot Wheel Reference")]
	public void SetAutopilotWheelRef()
	{
		GetComponent<AutoPilot>().wheelSteerReferenceTf = taxiSteerReferenceTf;
	}

	private void Awake()
	{
		obstacleCheckTime = UnityEngine.Random.Range(Time.time, Time.time + obstacleCheckInterval);
		isAlive = true;
		aiSpawn = GetComponent<AIAircraftSpawn>();
		if (!aeroController)
		{
			aeroController = GetComponent<AeroController>();
		}
		vesselColliders = new List<Collider>();
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>(includeInactive: true);
		foreach (Collider collider in componentsInChildren)
		{
			if (collider.gameObject.layer == 8)
			{
				vesselColliders.Add(collider);
			}
		}
		defaultTaxiSpeed = taxiSpeed;
		if (startLanded)
		{
			initialSpeed = 0f;
		}
		navNodes_wasLanded = startLanded;
		autoPilot.referenceTransform = base.transform;
		myTransform = base.transform;
		headLookTransform = new GameObject("HeadLookTransform").transform;
		headLookTransform.parent = base.transform;
		headLookTransform.localPosition = Vector3.zero;
		fuelTank = GetComponent<FuelTank>();
		wm = GetComponent<WeaponManager>();
		Health component = GetComponent<Health>();
		if ((bool)component)
		{
			component.OnDeath.AddListener(OnDeath);
			health = component;
		}
		cms = base.gameObject.GetComponentsInChildrenImplementing<Countermeasure>();
		formationFwdPID.updateMode = UpdateModes.Dynamic;
		if (!taxiCollisionDetectTf)
		{
			taxiCollisionDetectTfs = new Transform[1] { base.transform };
		}
		else
		{
			taxiCollisionDetectTfs = taxiCollisionDetectTf.GetComponentsInChildren<Transform>();
		}
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetAllLights(0);
		}
	}

	private void SetCollidersToVessel()
	{
		if (!collidersAreDebris)
		{
			return;
		}
		foreach (Collider vesselCollider in vesselColliders)
		{
			if ((bool)vesselCollider)
			{
				vesselCollider.gameObject.layer = 8;
			}
		}
		collidersAreDebris = false;
	}

	private void SetCollidersForTaxi()
	{
		if (collidersAreDebris)
		{
			return;
		}
		foreach (Collider vesselCollider in vesselColliders)
		{
			if ((bool)vesselCollider)
			{
				vesselCollider.gameObject.layer = 9;
			}
		}
		collidersAreDebris = true;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireSphere(base.transform.position, parkingSize);
	}

	private void Start()
	{
		myRefuelPlane = GetComponent<RefuelPlane>();
		isRefuelPlane = myRefuelPlane != null;
		leadHelpRand = UnityEngine.Random.Range(0f, 10f);
		autoPilot.inputLimiter = 0.5f;
		maxThrust = 0f;
		foreach (ModuleEngine engine in autoPilot.engines)
		{
			if (engine.includeInTWR)
			{
				maxThrust += engine.maxThrust * engine.abThrustMult;
			}
		}
		if ((bool)targetFinder)
		{
			switch (combatRole)
			{
			case CombatRoles.Attack:
				targetFinder.targetsToFind.air = true;
				targetFinder.targetsToFind.ground = true;
				targetFinder.targetsToFind.groundArmor = true;
				targetFinder.targetsToFind.missile = false;
				targetFinder.targetsToFind.ship = true;
				break;
			case CombatRoles.Bomber:
				targetFinder.targetsToFind.air = false;
				targetFinder.targetsToFind.ground = true;
				targetFinder.targetsToFind.groundArmor = true;
				targetFinder.targetsToFind.missile = false;
				targetFinder.targetsToFind.ship = true;
				break;
			case CombatRoles.Fighter:
				targetFinder.targetsToFind.air = true;
				targetFinder.targetsToFind.ground = false;
				targetFinder.targetsToFind.groundArmor = false;
				targetFinder.targetsToFind.missile = false;
				targetFinder.targetsToFind.ship = false;
				break;
			case CombatRoles.FighterAttack:
				targetFinder.targetsToFind.air = true;
				targetFinder.targetsToFind.ground = true;
				targetFinder.targetsToFind.groundArmor = true;
				targetFinder.targetsToFind.missile = false;
				targetFinder.targetsToFind.ship = true;
				break;
			}
		}
		if (followPlayerOnStart)
		{
			commandState = CommandStates.FollowLeader;
			formationLeader = FlightSceneManager.instance.playerActor.GetComponent<AirFormationLeader>();
		}
		if (!GetComponent<LODBase>())
		{
			base.gameObject.AddComponent<LODBase>();
		}
		FindRearSuspension();
		if (!orbitTransform)
		{
			Debug.LogWarning("AI Pilot has no default orbit transform. Creating one at its spawnpoint.");
			orbitTransform = new GameObject("fallbackOrbitTf").transform;
			orbitTransform.gameObject.AddComponent<FloatingOriginTransform>();
			Vector3 vector = (orbitClockwise ? myTransform.right : (-myTransform.right)) * orbitRadius;
			orbitTransform.position = base.transform.position + vector;
			fallbackOrbitTf = orbitTransform;
		}
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetLandingLights(0);
			extLightsCtrlr.SetNavLights(0);
			extLightsCtrlr.SetStrobeLights(0);
		}
	}

	private void OnDestroy()
	{
		if ((bool)fallbackOrbitTf && Application.isPlaying)
		{
			UnityEngine.Object.Destroy(fallbackOrbitTf.gameObject);
		}
	}

	private void OnDeath()
	{
		isAlive = false;
		if ((bool)aiWing)
		{
			aiWing.pilots.Remove(this);
			aiWing.UpdateLeader();
		}
		if ((bool)attackTarget)
		{
			if ((bool)aiWing)
			{
				aiWing.ReportDisengageTarget(attackTarget);
			}
			attackTarget = null;
		}
		Disarm();
		if ((bool)kPlane)
		{
			kPlane.SetToDynamic();
			actor.customVelocity = false;
			kPlane.enabled = false;
		}
		if ((bool)autoPilot)
		{
			foreach (ModuleEngine engine in autoPilot.engines)
			{
				engine.FailEngine();
			}
			autoPilot.enabled = false;
		}
		if ((bool)aeroController)
		{
			aeroController.SetRandomInputs();
		}
		StopCombat();
		StopAllCoroutines();
		if (crashedIntoTerrain)
		{
			return;
		}
		if (aiSpawn is AIAWACSSpawn && actor.team == Teams.Allied)
		{
			AIAWACSSpawn aIAWACSSpawn = (AIAWACSSpawn)aiSpawn;
			if (aIAWACSSpawn.commsEnabled)
			{
				aIAWACSSpawn.awacsVoiceProfile.ReportGoingDown();
			}
		}
		else
		{
			PlayRadioMessage(WingmanVoiceProfile.Messages.ShotDown, 10f, UnityEngine.Random.Range(0.5f, 1.5f));
		}
	}

	private IEnumerator DestroyAfterDeathRoutine()
	{
		float delay = UnityEngine.Random.Range(5f, 15f);
		float time = Time.time;
		while (Time.time - time < delay && !(myTransform.position.y < WaterPhysics.instance.height))
		{
			yield return null;
		}
		ExplosionManager.instance.CreateExplosionEffect(ExplosionManager.ExplosionTypes.Aerial, myTransform.position, rb ? rb.velocity : base.transform.forward);
		this.OnExploded?.Invoke();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void OnEnable()
	{
		if (VTScenarioEditor.isLoadingPreviewThumbnails)
		{
			return;
		}
		if (!rb)
		{
			rb = GetComponent<Rigidbody>();
		}
		if (((bool)aiSpawn && !aiSpawn.unitSpawner) || ((bool)aiSpawn && !aiSpawn.unitSpawner.spawned))
		{
			return;
		}
		StartCoroutine(ThreatScanRoutine());
		checkForEmptyTanksRoutine = StartCoroutine(CheckForEmptyTanksRoutine());
		StartCoroutine(CheckIfNeedRefuelRoutine());
		if (startLanded)
		{
			initialSpeed = 0f;
			initialLandRoutine = StartCoroutine(InitialLandRoutine());
		}
		else
		{
			if ((bool)aiSpawn)
			{
				initialSpeed = aiSpawn.initialSpeed;
			}
			initialFlyRoutine = StartCoroutine(InitialFlyRoutine());
		}
		navNodes_wasLanded = startLanded;
	}

	private IEnumerator CheckForEmptyTanksRoutine()
	{
		if (!wm)
		{
			yield break;
		}
		List<HPEquipDropTank> droptanks = new List<HPEquipDropTank>();
		yield return fiveSecWait;
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip && equip is HPEquipDropTank)
			{
				droptanks.Add((HPEquipDropTank)equip);
			}
		}
		if (droptanks.Count == 0)
		{
			yield break;
		}
		while (base.enabled)
		{
			if (flightInfo.radarAltitude > 100f)
			{
				foreach (HPEquipDropTank item in droptanks)
				{
					if ((bool)item)
					{
						if (item.fuelTank.fuelFraction == 0f)
						{
							JettisonTanks();
							yield break;
						}
						continue;
					}
					yield break;
				}
			}
			yield return fiveSecWait;
		}
	}

	private IEnumerator InitialLandRoutine()
	{
		commandState = CommandStates.Park;
		kPlane.SetToKinematic();
		kPlane.enabled = false;
		rb.interpolation = RigidbodyInterpolation.None;
		GridPlatoon componentInParent = GetComponentInParent<GridPlatoon>();
		UnitSpawn uSpawn = base.gameObject.GetComponentImplementing<UnitSpawn>();
		FixedPoint startPt;
		if ((bool)componentInParent && (bool)LevelBuilder.fetch)
		{
			startPt = new FixedPoint(LevelBuilder.fetch.GridToPosition(componentInParent.spawnInGrid) + base.transform.localPosition);
		}
		else
		{
			yield return null;
			startPt = new FixedPoint(base.transform.position);
		}
		Vector3[] array = new Vector3[3];
		int num = 0;
		float height = 0f;
		RaySpringDamper[] componentsInChildren = GetComponentsInChildren<RaySpringDamper>();
		foreach (RaySpringDamper raySpringDamper in componentsInChildren)
		{
			float magnitude = Vector3.Project(myTransform.position - (raySpringDamper.transform.position - raySpringDamper.suspensionDistance * 0.95f * raySpringDamper.transform.up), myTransform.up).magnitude;
			height = Mathf.Max(magnitude, height);
			if (num < 3)
			{
				array[num] = raySpringDamper.transform.position - raySpringDamper.suspensionDistance * raySpringDamper.transform.up;
				num++;
			}
		}
		Plane plane = new Plane(array[0], array[1], array[2]);
		Vector3 up2 = plane.normal * Mathf.Sign(Vector3.Dot(plane.normal, Vector3.up));
		up2 = Quaternion.FromToRotation(up2, myTransform.up) * Vector3.up;
		Vector3 fwd = Vector3.Cross(up2, -myTransform.right);
		Quaternion rotation = Quaternion.LookRotation(fwd, up2);
		while (!FlightSceneManager.isFlightReady)
		{
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			rb.rotation = rotation;
			commandState = CommandStates.Park;
			yield return fixedWait;
		}
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		bool placed = false;
		RaycastHit hit = default(RaycastHit);
		Vector3 velocity = Vector3.zero;
		while (!placed)
		{
			Vector3 vector = startPt.point;
			if (quickloadLanded)
			{
				vector = quickloadLandedPoint.point;
			}
			else if ((bool)uSpawn && (bool)uSpawn.unitSpawner)
			{
				vector = uSpawn.unitSpawner.transform.position;
			}
			if (Physics.Raycast(vector + 3f * Vector3.up, Vector3.down, out hit, 100f, 1, QueryTriggerInteraction.Ignore))
			{
				if (WaterPhysics.GetAltitude(hit.point) > 0f)
				{
					placed = true;
				}
				MovingPlatform component = hit.collider.GetComponent<MovingPlatform>();
				if ((bool)component)
				{
					velocity = component.GetVelocity(hit.point);
				}
			}
			if (quickloadLanded)
			{
				velocity = quickloadVelocity;
			}
			rb.velocity = velocity;
			rb.angularVelocity = Vector3.zero;
			rb.rotation = rotation;
			yield return new WaitForFixedUpdate();
		}
		Debug.Log("Initially landing AIPilot " + base.gameObject.name);
		startPt.point = hit.point + height * Vector3.up;
		Vector3 vector3 = (rb.position = (base.transform.position = startPt.point));
		rb.rotation = Quaternion.LookRotation(fwd, up2);
		rb.velocity = velocity;
		rb.rotation = rotation;
		Debug.DrawLine(hit.point, startPt.point, Color.magenta);
		kPlane.enabled = true;
		kPlane.SetVelocity(velocity);
		kPlane.SetToDynamic();
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		while (!flightInfo.isLanded)
		{
			yield return null;
		}
		if (takeOffAfterLanding && (bool)aiSpawn && !isAirbaseNavigating)
		{
			aiSpawn.TakeOff();
		}
	}

	private IEnumerator InitialFlyRoutine(bool forceTilt = true)
	{
		bool wasQuickloaded = ((bool)aiSpawn && aiSpawn.qsSpawned) || (!aiSpawn && QuicksaveManager.isQuickload);
		if (!wasQuickloaded)
		{
			kPlane.enabled = false;
		}
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		yield return null;
		if (forceTilt && isVtol && (bool)tiltController)
		{
			tiltController.SetTiltImmediate(90f);
		}
		if (initialSpeed < 50f)
		{
			initialSpeed = 150f;
		}
		Vector3 velocity = initialSpeed * myTransform.forward;
		if (wasQuickloaded)
		{
			_ = quickloadVelocity;
		}
		else
		{
			if ((bool)aiSpawn)
			{
				initialSpeed = aiSpawn.initialSpeed;
				velocity = initialSpeed * myTransform.forward;
			}
			if ((bool)kPlane)
			{
				kPlane.enabled = true;
				if (isTakingOffVtol)
				{
					kPlane.SetToDynamic();
					rb.velocity = velocity;
					rb.angularVelocity = quickloadAngularVelocity;
				}
				else
				{
					kPlane.SetToKinematic();
					kPlane.SetVelocity(velocity);
				}
			}
		}
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		yield return null;
		if (!wasQuickloaded && (bool)gearAnimator)
		{
			gearAnimator.RetractImmediate();
		}
	}

	private IEnumerator TakeOffCatapultRoutine(CarrierCatapult myCatapult)
	{
		catHook.SetState(1);
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetNavLights(1);
		}
		bool wasBehindCatapult = false;
		while (!catHook.hooked)
		{
			bool flag = Vector3.Dot(catHook.transform.position - myCatapult.transform.position, myCatapult.transform.forward) > 0f;
			if (!flag)
			{
				wasBehindCatapult = true;
			}
			if ((bool)myCatapult && commandState == CommandStates.Park && wasBehindCatapult && flag)
			{
				myTransform.rotation = myCatapult.transform.rotation;
				Vector3 vector = myCatapult.transform.position - catHook.hookForcePointTransform.position;
				vector.y = 0f;
				myTransform.position += vector;
			}
			yield return null;
		}
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetStrobeLights(1);
		}
		catHook.SetState(0);
		commandState = CommandStates.Override;
		autoPilot.SetFlaps(1f);
		autoPilot.targetPosition = myTransform.position + catHook.catapult.transform.forward * 9000f;
		if ((bool)wingRotator)
		{
			wingRotator.SetDefault();
		}
		autoPilot.targetSpeed = 0f;
		yield return new WaitForSeconds(8f);
		catapulting = true;
		if ((bool)actor.parkingNode)
		{
			actor.parkingNode.UnOccupyParking(actor);
		}
		autoPilot.steerMode = AutoPilot.SteerModes.Stable;
		autoPilot.inputLimiter = 1f;
		while (flightInfo.isLanded)
		{
			autoPilot.targetSpeed = maxSpeed;
			autoPilot.targetPosition = autoPilot.referenceTransform.position + autoPilot.referenceTransform.forward * 100f + 35f * Vector3.up;
			yield return fixedWait;
		}
		autoPilot.inputLimiter = 1f;
		if ((bool)aiSpawn)
		{
			aiSpawn.unitSpawner.spawnFlags.Remove("carrier");
		}
		if ((bool)kPlane)
		{
			kPlane.enabled = true;
			kPlane.SetToKinematic();
		}
		StartCoroutine(RetractGearDelayed(1f));
		float climbT = 0f;
		Vector3 projFwd = Vector3.ProjectOnPlane(myTransform.forward, Vector3.up).normalized;
		while (flightInfo.radarAltitude < minAltitude)
		{
			Vector3 a = projFwd * 100f + new Vector3(0f, 5f, 0f);
			Vector3 limitedClimbDirectionForSpeed = GetLimitedClimbDirectionForSpeed(projFwd * 100f + new Vector3(0f, 55f, 0f));
			a = Vector3.Slerp(a, limitedClimbDirectionForSpeed, climbT);
			climbT += 0.5f * Time.deltaTime;
			autoPilot.targetPosition = autoPilot.referenceTransform.position + a;
			autoPilot.inputLimiter = 1f;
			autoPilot.targetSpeed = maxSpeed;
			if (autoPilot.currentSpeed < 100f)
			{
				autoPilot.SetFlaps(1f);
			}
			else if (autoPilot.currentSpeed < 150f)
			{
				autoPilot.SetFlaps(0.5f);
			}
			else
			{
				autoPilot.SetFlaps(0f);
			}
			yield return null;
		}
		if (commandState == CommandStates.Orbit && (bool)formationLeader)
		{
			commandState = CommandStates.FollowLeader;
		}
		else
		{
			ApplyQueuedCommand();
		}
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetAllLights(0);
		}
	}

	private IEnumerator RetractGearDelayed(float delay)
	{
		yield return new WaitForSeconds(delay);
		if ((bool)gearAnimator)
		{
			gearAnimator.Retract();
		}
	}

	public void LandAtAirport(AirportManager airport)
	{
		if (landOnPadState != 0 || landingState != 0)
		{
			Debug.LogErrorFormat("{0} was commanded to LandAtAirport but is already landing! Aborting command.", actor.DebugName());
			return;
		}
		if (airport.team != actor.team)
		{
			Debug.LogError(actor.DebugName() + " was commanded to land at airport of the opposite team!");
			return;
		}
		StopCombat();
		bool flag = isVtol && twr > 1.05f;
		AirportManager.LandingRequestResponse landingRequestResponse = airport.RequestLanding2(actor, rb.mass, parkingSize, (!flag) ? AirportManager.VTOLLandingModes.NoVTOL : AirportManager.VTOLLandingModes.VTOLAllowed);
		if (landingRequestResponse.accepted && ((bool)landingRequestResponse.runway || flag))
		{
			landingRequestResponse.parkingSpace.OccupyParking(actor);
			actor.SetAutoUnoccupyParking(b: false);
			if (flag && (bool)landingRequestResponse.landingPad)
			{
				Debug.Log(" - request accepted for vertical landing");
				StartCoroutine(LandOnPadRoutine(LandOnPadStates.None, landingRequestResponse.landingPad, shutoffEngine: false, resetAtParking: true, landingRequestResponse.parkingSpace));
				return;
			}
			Debug.Log(" - request accepted for runway landing");
			targetRunway = landingRequestResponse.runway;
			landingParkingSpace = landingRequestResponse.parkingSpace;
			orbitTransform = targetRunway.landingQueueOrbitTf;
			orbitRadius = targetRunway.landingQueueRadius - 500f;
			defaultAltitude = targetRunway.GetFinalQueueAltitude();
			autoEngageEnemies = false;
			commandState = CommandStates.Land;
		}
		else if (!landingRequestResponse.incompatible && isVtol && !flag)
		{
			float num = rb.mass;
			for (int i = 0; i < wm.equipCount; i++)
			{
				HPEquippable equip = wm.GetEquip(i);
				if ((bool)equip && equip.jettisonable)
				{
					float num2 = 0f;
					IMassObject[] componentsInChildrenImplementing = equip.gameObject.GetComponentsInChildrenImplementing<IMassObject>();
					foreach (IMassObject massObject in componentsInChildrenImplementing)
					{
						num2 += massObject.GetMass();
					}
					num -= num2;
				}
			}
			if (maxThrust / (num * 9.81f) > 1.05f)
			{
				landingRequestResponse = airport.RequestLanding2(actor, rb.mass, parkingSize, AirportManager.VTOLLandingModes.ForceVTOL);
				if (landingRequestResponse.accepted)
				{
					landingRequestResponse.parkingSpace.OccupyParking(actor);
					StartCoroutine(JettisonUntilTWR(1.05f));
					StartCoroutine(LandOnPadRoutine(LandOnPadStates.None, landingRequestResponse.landingPad, shutoffEngine: false, resetAtParking: true, landingRequestResponse.parkingSpace));
				}
				else
				{
					StartCoroutine(WaitForFreeLandingPad(airport, delegate
					{
						LandAtAirport(airport);
					}));
					OrbitTransform(airport.transform);
				}
			}
			else
			{
				Debug.LogErrorFormat("Pilot {0} tried to land at an airport but it could not find a runway, and will not have a high enough TWR to land vertically.", aiSpawn ? aiSpawn.unitSpawner.GetUIDisplayName() : actor.actorName);
			}
		}
		else if (landingRequestResponse.incompatible)
		{
			Debug.LogErrorFormat("Pilot {0} tried to land at an airport, but that airport will never be able to accomodate it.", aiSpawn ? aiSpawn.unitSpawner.GetUIDisplayName() : actor.actorName);
		}
		else
		{
			Debug.Log(" - request denied for some reason: \n" + landingRequestResponse.ToString());
		}
	}

	public void LandAtAirport_old(AirportManager airport)
	{
		StopCombat();
		if (isVtol && twr > 1.05f)
		{
			int carrierSpawnIdx;
			Transform obj = airport.RequestLandingPad(actor, out carrierSpawnIdx);
			currentCarrierSpawnIdx = carrierSpawnIdx;
			if (obj != null)
			{
				return;
			}
		}
		Runway runway = airport.RequestLanding(myTransform, rb.mass);
		if (!runway)
		{
			if (isVtol && (bool)wm)
			{
				if (twr <= 1.05f)
				{
					float num = rb.mass;
					for (int i = 0; i < wm.equipCount; i++)
					{
						HPEquippable equip = wm.GetEquip(i);
						if ((bool)equip && equip.jettisonable)
						{
							float num2 = 0f;
							IMassObject[] componentsInChildrenImplementing = equip.gameObject.GetComponentsInChildrenImplementing<IMassObject>();
							foreach (IMassObject massObject in componentsInChildrenImplementing)
							{
								num2 += massObject.GetMass();
							}
							num -= num2;
						}
					}
					if (maxThrust / (num * 9.81f) > 1.05f)
					{
						int carrierSpawnIdx2;
						Transform obj2 = airport.RequestLandingPad(actor, out carrierSpawnIdx2);
						_currCarrSpawnIdx = carrierSpawnIdx2;
						if (obj2 != null)
						{
							StartCoroutine(JettisonUntilTWR(1.05f));
							return;
						}
						StartCoroutine(WaitForFreeLandingPad(airport, delegate
						{
							LandAtAirport(airport);
						}));
						OrbitTransform(airport.transform);
					}
					else
					{
						Debug.LogErrorFormat("Pilot {0} tried to land at an airport but it could not find a runway, and will not have a high enough TWR to land vertically.", aiSpawn ? aiSpawn.unitSpawner.GetUIDisplayName() : actor.actorName);
					}
				}
				else
				{
					StartCoroutine(WaitForFreeLandingPad(airport, delegate
					{
						LandAtAirport(airport);
					}));
					OrbitTransform(airport.transform);
				}
			}
			else
			{
				Debug.LogErrorFormat("Pilot {0} tried to land at an airport but it could not find a runway, and is not a VTOL.", aiSpawn ? aiSpawn.unitSpawner.GetUIDisplayName() : actor.actorName);
			}
		}
		else
		{
			targetRunway = runway;
			orbitTransform = runway.landingQueueOrbitTf;
			orbitRadius = runway.landingQueueRadius - 500f;
			defaultAltitude = runway.GetFinalQueueAltitude();
			autoEngageEnemies = false;
			commandState = CommandStates.Land;
		}
	}

	private IEnumerator WaitForFreeLandingPad(AirportManager ap, Action OnFreePadFound)
	{
		Debug.LogFormat("{0} is waiting for a free landing pad.", aiSpawn ? aiSpawn.unitSpawner.GetUIDisplayName() : actor.actorName);
		while (!ap.HasFreeLandingPads())
		{
			yield return null;
		}
		OnFreePadFound?.Invoke();
		Debug.LogFormat("{0} waited for a free landing pad and has now found one.", aiSpawn ? aiSpawn.unitSpawner.GetUIDisplayName() : actor.actorName);
	}

	private IEnumerator JettisonUntilTWR(float twrTarget)
	{
		MassUpdater ma = GetComponent<MassUpdater>();
		int i = wm.equipCount - 1;
		while (i >= 0 && twr < twrTarget)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip && equip.jettisonable)
			{
				equip.markedForJettison = true;
				wm.JettisonMarkedItems();
				yield return new WaitForFixedUpdate();
				yield return null;
				ma.UpdateMassObjects();
			}
			yield return jettisonDelayWait;
			i--;
		}
	}

	public void TakeOffCatapult(CarrierCatapult c)
	{
		StartCoroutine(TakeOffCatapultRoutine(c));
	}

	public bool TakeOff(Runway runway)
	{
		if (!flightInfo.isLanded)
		{
			if (landingState == LandingStates.None)
			{
				return false;
			}
			takeOffAfterLanding = true;
		}
		if (takeOffState != 0)
		{
			Debug.LogErrorFormat("{0} was commanded to take off but it is already taking off! (takeOffState == {1})", actor.DebugName(), takeOffState);
			return false;
		}
		targetRunway = runway;
		if (sto_capable && targetRunway.shortTakeOff)
		{
			StartCoroutine(ShortTakeOffRoutine(skipTaxi: false));
		}
		else
		{
			StartCoroutine(TakeOffRoutine());
		}
		return true;
	}

	private IEnumerator TakeOffRoutine(TakeOffStates resumeAtState = TakeOffStates.None)
	{
		Debug.LogFormat("{0} is beginning TakeOffRoutine", actor.DebugName());
		if (resumeAtState == TakeOffStates.None || resumeAtState == TakeOffStates.TaxiToRunway)
		{
			takeOffState = TakeOffStates.TaxiToRunway;
			yield return StartCoroutine(TO_TaxiToRunway());
			resumeAtState = TakeOffStates.None;
		}
		if (resumeAtState == TakeOffStates.None || resumeAtState == TakeOffStates.Preparing)
		{
			takeOffState = TakeOffStates.Preparing;
			yield return StartCoroutine(TO_Preparing());
			resumeAtState = TakeOffStates.None;
		}
		if (resumeAtState == TakeOffStates.None || resumeAtState == TakeOffStates.RunningUp)
		{
			takeOffState = TakeOffStates.RunningUp;
			yield return StartCoroutine(TO_RunningUp());
			resumeAtState = TakeOffStates.None;
		}
		if (resumeAtState == TakeOffStates.None || resumeAtState == TakeOffStates.Climbing)
		{
			takeOffState = TakeOffStates.Climbing;
			yield return StartCoroutine(TO_Climbing());
		}
		takeOffState = TakeOffStates.None;
	}

	private IEnumerator TO_TaxiToRunway()
	{
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetNavLights(1);
		}
		while ((bool)aiSpawn && (bool)aiSpawn.passengerBay && aiSpawn.passengerBay.IsExpectingUnits())
		{
			commandState = CommandStates.Override;
			yield return null;
		}
		bool readyToTakeoff = false;
		Debug.Log(base.gameObject.name + " awaiting ready to take off.", base.gameObject);
		while ((bool)targetRunway && !readyToTakeoff)
		{
			_ = taxiSteerReferenceTf.position - targetRunway.transform.position;
			bool flag = targetRunway.clearanceBounds.Contains(targetRunway.transform.InverseTransformPoint(myTransform.position));
			bool flag2 = Vector3.Angle(myTransform.forward, targetRunway.transform.forward) < 3f;
			bool flag3 = true;
			readyToTakeoff = !isAirbaseNavigating && flag2 && flag3 && flag;
			if (!isAirbaseNavigating && !taxiPath && flag && (!flag3 || !flag2))
			{
				autoPilot.targetPosition = targetRunway.transform.position + targetRunway.transform.forward * 4000f;
				autoPilot.targetSpeed = taxiSpeed;
			}
			commandState = CommandStates.Override;
			yield return null;
		}
		Debug.Log(base.gameObject.name + " ready to take off.  Preparing aircraft.");
		if ((bool)actor.parkingNode)
		{
			actor.parkingNode.UnOccupyParking(actor);
		}
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetStrobeLights(1);
		}
		commandState = CommandStates.Override;
	}

	private IEnumerator TO_Preparing()
	{
		if ((bool)wingRotator)
		{
			wingRotator.SetDefault();
			float t = Time.time;
			while (Time.time - t < 2f)
			{
				commandState = CommandStates.Override;
				autoPilot.targetSpeed = 0f;
				if ((bool)tiltController && tiltController.currentTilt != 90f)
				{
					float tiltImmediate = Mathf.MoveTowards(tiltController.currentTilt, 90f, tiltController.tiltSpeed * Time.deltaTime);
					tiltController.SetTiltImmediate(tiltImmediate);
				}
				yield return null;
			}
		}
		if ((bool)tiltController)
		{
			while (tiltController.currentTilt != 90f)
			{
				float tiltImmediate2 = Mathf.MoveTowards(tiltController.currentTilt, 90f, tiltController.tiltSpeed * Time.deltaTime);
				tiltController.SetTiltImmediate(tiltImmediate2);
				autoPilot.targetSpeed = 0f;
				autoPilot.targetPosition = myTransform.position + myTransform.forward * 100f;
				yield return null;
			}
		}
		autoPilot.SetFlaps(1f);
		commandState = CommandStates.Override;
	}

	private IEnumerator TO_RunningUp()
	{
		Debug.Log(base.gameObject.name + " running up engines for takeoff.");
		while (flightInfo.airspeed < takeOffSpeed)
		{
			commandState = CommandStates.Override;
			autoPilot.targetSpeed = maxSpeed;
			autoPilot.targetPosition = myTransform.position + 1000f * targetRunway.transform.forward;
			autoPilot.SetOverrideRollTarget(Vector3.up);
			autoPilot.inputLimiter = takeOffInputLimiter;
			yield return null;
		}
		takeOffRaiser = Mathf.Max(0f, (myTransform.forward * 1000f).y);
		while (flightInfo.isLanded)
		{
			Vector3 direction = 1000f * targetRunway.transform.forward;
			direction.y = Mathf.Max(direction.y, 0f) + takeOffRaiser;
			direction = GetLimitedClimbDirectionForSpeed(direction);
			autoPilot.targetPosition = myTransform.position + direction;
			autoPilot.SetOverrideRollTarget(Vector3.up);
			autoPilot.inputLimiter = takeOffInputLimiter;
			if (takeOffRaiser < 200f)
			{
				takeOffRaiser += takeOffRaiserRotateRate * Time.deltaTime;
			}
			commandState = CommandStates.Override;
			yield return null;
		}
	}

	private IEnumerator TO_Climbing()
	{
		if ((bool)kPlane)
		{
			kPlane.enabled = true;
			kPlane.SetToKinematic();
		}
		takeOffAfterLanding = false;
		targetRunway.UnregisterUsageRequest(actor);
		StartCoroutine(RetractGearDelayed(2f));
		float liftOffSpeed = Mathf.Max(rb.velocity.y, 0.05f);
		while (flightInfo.radarAltitude < minAltitude)
		{
			Vector3 vector = Vector3.ProjectOnPlane(myTransform.forward, flightInfo.surfaceNormal);
			if (vector.y < 0f)
			{
				vector.y = 0f;
			}
			vector.Normalize();
			Vector3 direction = vector * 1000f + new Vector3(0f, takeOffRaiser, 0f);
			if (takeOffRaiser < 200f)
			{
				takeOffRaiser += takeOffRaiserClimbingRate * Time.deltaTime;
			}
			direction = GetLimitedClimbDirectionForSpeed(direction);
			autoPilot.targetPosition = autoPilot.referenceTransform.position + direction;
			autoPilot.inputLimiter = 1f;
			autoPilot.targetSpeed = maxSpeed;
			if (autoPilot.currentSpeed < 100f)
			{
				autoPilot.SetFlaps(1f);
			}
			else if (autoPilot.currentSpeed < 150f)
			{
				autoPilot.SetFlaps(0.5f);
			}
			else
			{
				autoPilot.SetFlaps(0f);
			}
			commandState = CommandStates.Override;
			Vector3 velocity = kPlane.velocity;
			if (velocity.y < liftOffSpeed)
			{
				velocity.y = liftOffSpeed;
				kPlane.SetVelocity(velocity);
			}
			yield return null;
		}
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetAllLights(0);
		}
		autoPilot.SetFlaps(0f);
		ApplyQueuedCommand();
	}

	private IEnumerator ShortTakeOffRoutine(bool skipTaxi)
	{
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetAllLights(0);
		}
		Debug.LogFormat("{0}: Short take off routine (skipTaxi = {1})", actor.DebugName(), skipTaxi);
		if (!skipTaxi)
		{
			yield return StartCoroutine(TO_TaxiToRunway());
		}
		yield return StartCoroutine(STO_PrepareTakeoff());
		yield return StartCoroutine(STO_TakingOff());
	}

	private IEnumerator STO_PrepareTakeoff()
	{
		float currTilt = tiltController.currentTilt;
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetNavLights(1);
			extLightsCtrlr.SetStrobeLights(1);
		}
		if ((bool)wingRotator)
		{
			wingRotator.SetDefault();
		}
		while (Vector3.Angle(Vector3.ProjectOnPlane(myTransform.forward, Vector3.up), Vector3.ProjectOnPlane(targetRunway.transform.forward, Vector3.up)) > 0.1f)
		{
			autoPilot.steerMode = AutoPilot.SteerModes.Aim;
			autoPilot.targetSpeed = 0.5f;
			autoPilot.targetPosition = myTransform.position + targetRunway.transform.forward * 100f - myTransform.forward * 90f;
			yield return null;
		}
		while (currTilt != sto_runningTilt)
		{
			currTilt = Mathf.MoveTowards(currTilt, sto_runningTilt, tiltController.tiltSpeed * Time.deltaTime);
			tiltController.SetTiltImmediate(currTilt);
			autoPilot.targetSpeed = 0f;
			autoPilot.targetPosition = myTransform.position + myTransform.forward * 100f;
			yield return null;
		}
		while (flightInfo.surfaceSpeed > 0.05f)
		{
			autoPilot.targetSpeed = 0f;
			autoPilot.targetPosition = myTransform.position + myTransform.forward * 100f;
			yield return null;
		}
		bool rClear = targetRunway.IsRunwayClear(actor);
		while (!rClear)
		{
			float rT = 0f;
			while (Time.time - rT < 3f)
			{
				autoPilot.targetSpeed = 0f;
				autoPilot.targetPosition = myTransform.position + myTransform.forward * 100f;
				yield return null;
			}
			rClear = targetRunway.IsRunwayClear(actor);
			yield return null;
		}
		float t = Time.time;
		float maxThrottle = 1f;
		if ((bool)vtolAp)
		{
			maxThrottle = vtolAp.hoverMaxThrottle;
		}
		while (Time.time - t < 3f)
		{
			autoPilot.OverrideSetBrakes(1f);
			autoPilot.OverrideSetThrottle(maxThrottle);
			yield return null;
		}
	}

	private RaySpringDamper GetFrontSuspension()
	{
		if ((bool)flightInfo.wheelsController)
		{
			return flightInfo.wheelsController.suspensions[0];
		}
		if (flightInfo.suspensions != null)
		{
			return flightInfo.suspensions[0];
		}
		return null;
	}

	private IEnumerator STO_TakingOff()
	{
		float maxThrottle = 1f;
		if ((bool)vtolAp)
		{
			maxThrottle = vtolAp.hoverMaxThrottle;
		}
		takeOffAfterLanding = false;
		float currTilt = tiltController.currentTilt;
		Vector3 takeOffVector = Quaternion.AngleAxis(0f - sto_takeOffPitch, targetRunway.transform.right) * targetRunway.transform.forward;
		bool removedRunwayUser = false;
		RaySpringDamper frontSusp = GetFrontSuspension();
		float finalTakeOffSpeed = sto_takeOffSpeed;
		while (flightInfo.isLanded)
		{
			autoPilot.steerMode = AutoPilot.SteerModes.Aim;
			autoPilot.inputLimiter = 1f;
			float target;
			if (autoPilot.currentSpeed < sto_takeOffSpeed && Physics.Raycast(frontSusp.transform.position + rb.velocity * 2f, Vector3.down, frontSusp.suspensionDistance * 1.5f, 1))
			{
				target = sto_runningTilt;
				autoPilot.targetPosition = myTransform.position + targetRunway.transform.forward * 100f;
			}
			else
			{
				target = sto_takeOffTilt;
				autoPilot.targetPosition = myTransform.position + takeOffVector * 100f + 100f * Vector3.up;
			}
			finalTakeOffSpeed = Mathf.Max(sto_takeOffSpeed, autoPilot.currentSpeed);
			currTilt = Mathf.MoveTowards(currTilt, target, tiltController.tiltSpeed * Time.deltaTime);
			tiltController.SetTiltImmediate(currTilt);
			autoPilot.targetSpeed = maxSpeed;
			autoPilot.OverrideSetThrottle(maxThrottle);
			yield return fixedWait;
		}
		while (currTilt < 90f || flightInfo.airspeed < sto_takeOffSpeed)
		{
			autoPilot.steerMode = AutoPilot.SteerModes.Aim;
			autoPilot.inputLimiter = 1f;
			float target2 = Mathf.Lerp(sto_takeOffTilt, 90f, Mathf.InverseLerp(finalTakeOffSpeed + 10f, takeOffSpeed, autoPilot.currentSpeed));
			autoPilot.targetPosition = myTransform.position + takeOffVector * 100f;
			if (!removedRunwayUser)
			{
				targetRunway.UnregisterUsageRequest(actor);
				removedRunwayUser = true;
			}
			currTilt = Mathf.MoveTowards(currTilt, target2, tiltController.tiltSpeed * Time.deltaTime);
			tiltController.SetTiltImmediate(currTilt);
			autoPilot.targetSpeed = maxSpeed;
			autoPilot.OverrideSetThrottle(maxThrottle);
			yield return null;
		}
		currentCarrier = null;
		currentCarrierSpawnIdx = -1;
		gearAnimator.Retract();
		kPlane.enabled = true;
		kPlane.SetToKinematic();
		ApplyQueuedCommand();
		if ((bool)actor.parkingNode)
		{
			actor.parkingNode.UnOccupyParking(actor);
		}
		actor.SetAutoUnoccupyParking(b: true);
		if ((bool)extLightsCtrlr)
		{
			yield return fiveSecWait;
			extLightsCtrlr.SetAllLights(0);
		}
	}

	public void TakeOffCarrier(AICarrierSpawn carrier, int spawnIdx)
	{
		if (commandState != CommandStates.Override)
		{
			if (carrier.usesCatapults)
			{
				StartCoroutine(TakeOffCarrierRoutine(carrier, spawnIdx));
				return;
			}
			if ((bool)carrier.spawnPoints[spawnIdx].stoPath && sto_capable)
			{
				StartCoroutine(CarrierSTORoutine(carrier, carrier.spawnPoints[spawnIdx].stoPath));
				return;
			}
			Runway takeoffRunway;
			List<AirbaseNavNode> takeoffPath = carrier.airportManager.navigation.GetTakeoffPath(myTransform.position, myTransform.forward, out takeoffRunway, 0f);
			taxiSpeed = carrierTaxiSpeed;
			TaxiAirbaseNav(takeoffPath, takeoffRunway);
			TakeOff(takeoffRunway);
		}
	}

	private IEnumerator CarrierSTORoutine(AICarrierSpawn carrier, FollowPath stoPath)
	{
		commandState = CommandStates.Override;
		Runway runway = carrier.airportManager.runways[0];
		runway.RegisterUsageRequest(actor);
		taxiSpeed = carrierTaxiSpeed;
		autoPilot.enabled = true;
		while (!runway.IsRunwayUsageAuthorized(actor))
		{
			autoPilot.targetSpeed = 0f;
			yield return null;
		}
		while (!runway.IsRunwayClear(actor))
		{
			autoPilot.targetSpeed = 0f;
			yield return null;
		}
		bool enginesOff = true;
		while (enginesOff)
		{
			enginesOff = false;
			foreach (ModuleEngine engine in autoPilot.engines)
			{
				if (!engine.startedUp)
				{
					enginesOff = true;
					break;
				}
			}
			if (enginesOff && (bool)extLightsCtrlr)
			{
				extLightsCtrlr.SetStrobeLights(1);
			}
			autoPilot.targetSpeed = 0f;
			yield return null;
		}
		Debug.LogFormat(stoPath.gameObject, "{0} taxiing to STO runway: {1}", actor.DebugName(), stoPath.gameObject.name);
		while (!TaxiNavManual(stoPath, 2f, 0.99f) && Vector3.Dot(Vector3.ProjectOnPlane(myTransform.forward, runway.transform.up).normalized, runway.transform.forward) < 0.99f)
		{
			yield return null;
		}
		while (flightInfo.surfaceSpeed > 0.01f)
		{
			autoPilot.targetPosition = autoPilot.wheelSteerReferenceTf.position + runway.transform.forward;
			autoPilot.targetSpeed = 0f;
			yield return null;
		}
		CSVPushback csvPush = carrier.GetComponentInChildren<CSVPushback>();
		if ((bool)csvPush)
		{
			if (csvPush.NeedsPushback(flightInfo.wheelsController.suspensions[0].transform))
			{
				csvPush.Pushback(flightInfo.wheelsController.suspensions[0].transform);
				while (!csvPush.isHooked)
				{
					autoPilot.targetSpeed = 0f;
					yield return null;
				}
			}
			else
			{
				csvPush = null;
			}
		}
		if ((bool)csvPush)
		{
			yield return StartCoroutine(PushbackRoutine(carrier, runway, csvPush, null));
		}
		if ((bool)csvPush)
		{
			csvPush.ResetPushback();
			while (!csvPush.isReset)
			{
				yield return null;
			}
		}
		autoPilot.enabled = true;
		targetRunway = runway;
		StartCoroutine(ShortTakeOffRoutine(skipTaxi: true));
	}

	private IEnumerator PushbackRoutine(AICarrierSpawn carrier, Runway runway, CSVPushback csvPush, Action onComplete)
	{
		Vector3 targetPos = carrier.transform.InverseTransformPoint(runway.transform.position) + new Vector3(0f, aiSpawn.heightFromSurface, 0f);
		Vector3 movePos = carrier.transform.InverseTransformPoint(myTransform.position);
		Vector3 localPos = carrier.transform.InverseTransformPoint(myTransform.position);
		Vector3 localPBUp = carrier.transform.InverseTransformDirection(myTransform.up);
		_ = localPos;
		while ((bool)csvPush)
		{
			movePos = Vector3.MoveTowards(movePos, targetPos, 3f * Time.fixedDeltaTime);
			Vector3 vector = localPos;
			localPos = Vector3.Lerp(localPos, movePos, 4f * Time.fixedDeltaTime);
			Vector3 position = carrier.shipMover.rb.position + carrier.shipMover.rb.rotation * localPos;
			rb.MovePosition(position);
			Vector3 vector2 = (localPos - vector) / Time.fixedDeltaTime;
			Vector3 velocity = carrier.shipMover.velocity + carrier.shipMover.rb.rotation * vector2;
			rb.velocity = velocity;
			Vector3 vector3 = carrier.shipMover.rb.rotation * localPBUp;
			Vector3 forward = Vector3.ProjectOnPlane(runway.transform.forward, vector3);
			rb.MoveRotation(Quaternion.LookRotation(forward, vector3));
			if ((localPos - targetPos).sqrMagnitude < 0.01f)
			{
				break;
			}
			autoPilot.enabled = false;
			yield return fixedWait;
		}
		rb.velocity = carrier.shipMover.rb.velocity;
		onComplete?.Invoke();
	}

	private IEnumerator TakeOffCarrierRoutine(AICarrierSpawn carrier, int spawnIdx, CTOStates resumeState = CTOStates.None)
	{
		currentCarrier = carrier;
		currentCarrierSpawnIdx = spawnIdx;
		CarrierCatapult catapult = carrier.spawnPoints[spawnIdx].catapult;
		FollowPath takeOffPath = carrier.spawnPoints[spawnIdx].catapultPath;
		yield return null;
		catHook.SetState(1);
		commandState = CommandStates.Override;
		if (resumeState == CTOStates.None || resumeState == CTOStates.WaitAuthorization)
		{
			resumeState = CTOStates.None;
			ctoState = CTOStates.WaitAuthorization;
			yield return StartCoroutine(CTO_WaitAuthorization(carrier));
		}
		if (resumeState == CTOStates.None || resumeState == CTOStates.WaitTaxiClearance)
		{
			resumeState = CTOStates.None;
			ctoState = CTOStates.WaitTaxiClearance;
			yield return StartCoroutine(CTO_WaitTaxiClearance(carrier));
		}
		if (resumeState == CTOStates.None || resumeState == CTOStates.TaxiToCat)
		{
			resumeState = CTOStates.None;
			ctoState = CTOStates.TaxiToCat;
			yield return StartCoroutine(CTO_TaxiToCatapult(carrier, catapult, takeOffPath));
		}
		if (resumeState == CTOStates.PrepareTakeOff || resumeState == CTOStates.Launching)
		{
			SetPlaneToCatapult(catapult);
			resumeState = CTOStates.None;
		}
		if (resumeState == CTOStates.None || resumeState == CTOStates.PrepareTakeOff)
		{
			resumeState = CTOStates.None;
			ctoState = CTOStates.PrepareTakeOff;
			yield return StartCoroutine(CTO_PrepareTakeOff());
		}
		if (resumeState == CTOStates.None || resumeState == CTOStates.Launching)
		{
			resumeState = CTOStates.None;
			ctoState = CTOStates.Launching;
			yield return StartCoroutine(CTO_Launching(carrier));
		}
		if (resumeState == CTOStates.None || resumeState == CTOStates.Ascending)
		{
			resumeState = CTOStates.None;
			ctoState = CTOStates.Ascending;
			yield return StartCoroutine(CTO_Ascending());
		}
		catHook.SetState(0);
		ctoState = CTOStates.None;
	}

	private IEnumerator CTO_WaitAuthorization(AICarrierSpawn carrier)
	{
		autoPilot.enabled = true;
		while ((bool)aiSpawn && (bool)aiSpawn.passengerBay && aiSpawn.passengerBay.IsExpectingUnits())
		{
			commandState = CommandStates.Override;
			autoPilot.targetSpeed = 0f;
			yield return null;
		}
		bool enginesOff = true;
		while (enginesOff)
		{
			enginesOff = false;
			foreach (ModuleEngine engine in autoPilot.engines)
			{
				if (!engine.startedUp)
				{
					enginesOff = true;
					break;
				}
			}
			if (enginesOff && (bool)extLightsCtrlr)
			{
				extLightsCtrlr.SetStrobeLights(1);
			}
			autoPilot.targetSpeed = 0f;
			yield return null;
		}
		carrier.RegisterAITakeoffRequest(aiSpawn);
		while (!carrier.IsAuthorizedForTakeoff(actor))
		{
			commandState = CommandStates.Override;
			yield return oneSecWait;
		}
	}

	private IEnumerator CTO_WaitTaxiClearance(AICarrierSpawn carrier)
	{
		taxiSpeed = carrierTaxiSpeed;
		if (!carrier.airportManager.reserveRunwayForCarrierTakeOff)
		{
			yield break;
		}
		carrier.runway.RegisterUsageRequest(actor);
		Debug.Log(actor.actorName + " awaiting clearance for carrier takeoff.");
		bool waitingForClearance = true;
		while (waitingForClearance)
		{
			Actor authorizedUser = carrier.runway.GetAuthorizedUser();
			if (authorizedUser == null || authorizedUser == actor || authorizedUser.flightInfo.isLanded)
			{
				waitingForClearance = false;
			}
			commandState = CommandStates.Override;
			yield return null;
		}
	}

	private IEnumerator CTO_TaxiToCatapult(AICarrierSpawn carrier, CarrierCatapult catapult, FollowPath takeOffPath)
	{
		Debug.Log("Carrier unit taking off");
		Coroutine taxiRoutine = StartCoroutine(TaxiNavRoutine(takeOffPath, 2f));
		catHook.SetState(1);
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetNavLights(1);
		}
		bool wasBehindCat = false;
		while (!catHook.hooked)
		{
			bool flag = Vector3.Dot(catHook.transform.position - catapult.transform.position, catapult.transform.forward) > 0f;
			if (!flag)
			{
				wasBehindCat = true;
			}
			if ((bool)catapult && wasBehindCat && flag)
			{
				SetPlaneToCatapult(catapult);
			}
			commandState = CommandStates.Override;
			yield return null;
		}
		if (taxiRoutine != null)
		{
			StopCoroutine(taxiRoutine);
		}
	}

	private void SetPlaneToCatapult(CarrierCatapult catapult)
	{
		myTransform.rotation = catapult.transform.rotation;
		Vector3 vector = catapult.transform.position - catHook.hookForcePointTransform.position;
		vector.y = 0f;
		myTransform.position += vector;
		rb.position = myTransform.position;
		rb.velocity = catapult.parentRb.velocity;
		rb.angularVelocity = Vector3.zero;
		catHook.SetState(1);
	}

	private IEnumerator CTO_PrepareTakeOff()
	{
		while (!catHook.catapult)
		{
			yield return null;
		}
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetStrobeLights(1);
		}
		catHook.SetState(0);
		commandState = CommandStates.Override;
		autoPilot.SetFlaps(1f);
		autoPilot.targetPosition = myTransform.position + catHook.catapult.transform.forward * 9000f;
		if ((bool)wingRotator)
		{
			wingRotator.SetDefault();
		}
		autoPilot.targetSpeed = 0f;
		float t = Time.time;
		while (Time.time - t < 8f)
		{
			autoPilot.targetSpeed = 0f;
			commandState = CommandStates.Override;
			yield return null;
		}
	}

	private IEnumerator CTO_Launching(AICarrierSpawn carrier)
	{
		catapulting = true;
		if ((bool)actor.parkingNode)
		{
			actor.parkingNode.UnOccupyParking(actor);
		}
		autoPilot.steerMode = AutoPilot.SteerModes.Stable;
		autoPilot.inputLimiter = 1f;
		while (flightInfo.isLanded)
		{
			autoPilot.targetSpeed = maxSpeed;
			autoPilot.targetPosition = autoPilot.referenceTransform.position + autoPilot.referenceTransform.forward * 100f + 35f * Vector3.up;
			commandState = CommandStates.Override;
			yield return null;
		}
		if (carrier.airportManager.reserveRunwayForCarrierTakeOff)
		{
			carrier.runway.UnregisterUsageRequest(actor);
		}
		autoPilot.inputLimiter = 1f;
		if ((bool)aiSpawn)
		{
			aiSpawn.unitSpawner.spawnFlags.Remove("carrier");
		}
		if ((bool)kPlane)
		{
			kPlane.enabled = true;
			kPlane.SetToKinematic();
		}
	}

	private IEnumerator CTO_Ascending()
	{
		StartCoroutine(RetractGearDelayed(1f));
		float climbT = 0f;
		Vector3 projFwd = Vector3.ProjectOnPlane(myTransform.forward, Vector3.up).normalized;
		while (flightInfo.radarAltitude < minAltitude)
		{
			Vector3 a = projFwd * 100f + new Vector3(0f, 5f, 0f);
			Vector3 limitedClimbDirectionForSpeed = GetLimitedClimbDirectionForSpeed(projFwd * 100f + new Vector3(0f, 55f, 0f));
			a = Vector3.Slerp(a, limitedClimbDirectionForSpeed, climbT);
			climbT += 0.5f * Time.deltaTime;
			autoPilot.targetPosition = autoPilot.referenceTransform.position + a;
			autoPilot.inputLimiter = 1f;
			autoPilot.targetSpeed = maxSpeed;
			if (autoPilot.currentSpeed < 100f)
			{
				autoPilot.SetFlaps(1f);
			}
			else if (autoPilot.currentSpeed < 150f)
			{
				autoPilot.SetFlaps(0.5f);
			}
			else
			{
				autoPilot.SetFlaps(0f);
			}
			commandState = CommandStates.Override;
			yield return null;
		}
		autoPilot.SetFlaps(0f);
		ApplyQueuedCommand();
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetAllLights(0);
		}
		currentCarrier = null;
		currentCarrierSpawnIdx = -1;
	}

	private void AddDebugLine(string l)
	{
	}

	private void AddFixedDebugLine(string l)
	{
	}

	private IEnumerator AirbaseNavTaxiRoutine(List<AirbaseNavNode> transforms, Runway takeoffRunway)
	{
		Debug.LogFormat(base.gameObject, "{0} is starting AirbaseNavTaxiRoutine", actor.DebugName());
		if (transforms == null || transforms.Count < 1)
		{
			yield break;
		}
		if (currentNavTransforms != null)
		{
			Debug.LogErrorFormat(base.gameObject, "{0} tried to set the current nav transforms but it was already set!!", actor.DebugName());
			yield break;
		}
		currentNavTransforms = new List<AirbaseNavNode>();
		foreach (AirbaseNavNode transform in transforms)
		{
			currentNavTransforms.Add(transform);
		}
		commandState = CommandStates.Override;
		isAirbaseNavigating = true;
		SetReferenceTransform(taxiSteerReferenceTf, RefTfLocks.AirbaseTaxi);
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetNavLights(1);
		}
		Debug.Log(base.gameObject.name + " beginning airbase navigation. (" + currentNavTransforms.Count + ") nodes.");
		bool hasRequestedTakeoff = false;
		float sqrDistThresh = 9f;
		float rearSqrDistThresh = 1296f;
		FixedPoint prevPosition = new FixedPoint(base.transform.position);
		if (currentNavTransforms[0].nodeType == AirbaseNavNode.NodeTypes.Parking && landingState != LandingStates.Taxiing)
		{
			prevPosition = new FixedPoint(currentNavTransforms[0].transform.position);
			currentNavTransforms.RemoveAt(0);
		}
		bool enginesOff = true;
		while (enginesOff)
		{
			enginesOff = false;
			foreach (ModuleEngine engine in autoPilot.engines)
			{
				if (!engine.startedUp)
				{
					enginesOff = true;
					break;
				}
			}
			autoPilot.targetSpeed = 0f;
			yield return null;
		}
		while (currentNavTransforms.Count > 0)
		{
			AirbaseNavNode nextNode = currentNavTransforms[0];
			Vector3 pathDirection = (nextNode.position - prevPosition.point).normalized;
			bool skip = false;
			bool waitRunway = false;
			float sqrMagnitude;
			while (((sqrMagnitude = (taxiSteerReferenceTf.position - nextNode.position).sqrMagnitude) > sqrDistThresh && !skip) || waitRunway)
			{
				waitRunway = false;
				if (Vector3.Dot(taxiSteerReferenceTf.forward, nextNode.position - taxiSteerReferenceTf.position) > 0f)
				{
					autoPilot.targetPosition = nextNode.position;
				}
				else
				{
					Vector3 targetPosition = Vector3.Project(taxiSteerReferenceTf.position - prevPosition.point, pathDirection) + prevPosition.point + pathDirection * 5f;
					autoPilot.targetPosition = targetPosition;
					if (sqrMagnitude < rearSqrDistThresh)
					{
						skip = true;
					}
				}
				autoPilot.inputLimiter = 1f;
				autoPilot.steerMode = AutoPilot.SteerModes.Aim;
				float b = 15f;
				Vector3 normalized = PlanarDirection(nextNode.position - myTransform.position).normalized;
				float a = Mathf.Lerp(2f, b, Mathf.Pow(Vector3.Dot(PlanarDirection(myTransform.forward).normalized, normalized), 4f));
				float b2;
				if (currentNavTransforms.Count > 1)
				{
					AirbaseNavNode airbaseNavNode = currentNavTransforms[1];
					Vector3 normalized2 = PlanarDirection(airbaseNavNode.position - nextNode.position).normalized;
					float num = Vector3.Angle(normalized, normalized2);
					float a2 = Mathf.Lerp(4f, b, Mathf.Pow(1f - Mathf.Clamp01(num / 70f), 2f));
					if (airbaseNavNode.isReservedForPath)
					{
						a2 = 4f;
					}
					b2 = Mathf.Lerp(a2, b, Mathf.Clamp01(sqrMagnitude / 10000f));
				}
				else
				{
					b2 = Mathf.Lerp(4f, b, Mathf.Clamp01(sqrMagnitude / 10000f));
				}
				autoPilot.targetSpeed = Mathf.Min(a, b2);
				SetThrottleLimiterDisallowAB();
				if ((bool)takeoffRunway && takeoffRunway.shortHoldTriggerBounds.Contains(takeoffRunway.transform.InverseTransformPoint(nextNode.transform.position)))
				{
					if (!hasRequestedTakeoff && takeoffRunway.shortHoldTriggerBounds.Contains(takeoffRunway.transform.InverseTransformPoint(actor.position)))
					{
						takeoffRunway.RegisterUsageRequest(actor);
						hasRequestedTakeoff = true;
						Debug.Log(base.gameObject.name + " is holding short of runway " + takeoffRunway.gameObject.name);
					}
					if (!takeoffRunway.IsRunwayUsageAuthorized(actor))
					{
						waitRunway = true;
						autoPilot.targetSpeed = Mathf.Clamp((sqrMagnitude - 25f) / 25f, 0f, autoPilot.targetSpeed);
						autoPilot.targetSpeed = Mathf.Lerp(autoPilot.targetSpeed, 0f, 1f - Mathf.Clamp01((sqrMagnitude - sqrDistThresh) / VTOLVRConstants.AIRBASE_NAV_DECCEL_DIV));
					}
				}
				if (TaxiCollisionDetected(myTransform.forward))
				{
					autoPilot.targetSpeed = 0f;
				}
				if ((bool)nextNode.hangarDoor && !nextNode.hangarDoor.isFullyOpen)
				{
					autoPilot.targetSpeed = 0f;
				}
				if (isVtol && (bool)tiltController)
				{
					tiltController.PadInput(Vector3.up);
				}
				float num2 = nextNode.ReserveForPath(this);
				if (num2 > 0f)
				{
					float num3 = num2 * num2;
					autoPilot.targetSpeed = Mathf.Lerp(autoPilot.targetSpeed, 0f, 1f - Mathf.Clamp01((sqrMagnitude - num3) / VTOLVRConstants.AIRBASE_NAV_DECCEL_DIV));
				}
				if (takeoffRunway != null)
				{
					Vector3 vector = taxiSteerReferenceTf.position - takeoffRunway.transform.position;
					bool flag = Vector3.Dot(vector, takeoffRunway.transform.forward) > 0f;
					bool flag2 = Vector3.SqrMagnitude(Vector3.Project(vector, takeoffRunway.transform.right)) < 100f;
					if (Vector3.Angle(myTransform.forward, takeoffRunway.transform.forward) < 10f && flag && flag2)
					{
						autoPilot.throttleLimiter = 1f;
						break;
					}
				}
				commandState = CommandStates.Override;
				yield return null;
			}
			_ = currentNavTransforms[0];
			currentNavTransforms.RemoveAt(0);
		}
		if (takeoffRunway != null)
		{
			if ((bool)extLightsCtrlr)
			{
				extLightsCtrlr.SetStrobeLights(1);
			}
			while (Vector3.Angle(Vector3.ProjectOnPlane(myTransform.forward, takeoffRunway.transform.up), takeoffRunway.transform.forward) > 4f)
			{
				Vector3 targetPosition2 = Vector3.Project(taxiSteerReferenceTf.position - takeoffRunway.transform.position, takeoffRunway.transform.forward) + takeoffRunway.transform.position + takeoffRunway.transform.forward * 5f;
				autoPilot.steerMode = AutoPilot.SteerModes.Aim;
				autoPilot.targetSpeed = taxiSpeed;
				autoPilot.targetPosition = targetPosition2;
				commandState = CommandStates.Override;
				yield return null;
			}
		}
		else if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetAllLights(0);
		}
		autoPilot.targetSpeed = 0f;
		commandState = CommandStates.Park;
		Debug.Log(base.gameObject.name + " completed airbase navigation.");
		isAirbaseNavigating = false;
		currentNavTransforms = null;
		UnlockReferenceTransform(RefTfLocks.AirbaseTaxi);
	}

	public void TaxiAirbaseNav(List<AirbaseNavNode> navTfs, Runway tgtRunway)
	{
		StartCoroutine(AirbaseNavTaxiRoutine(navTfs, tgtRunway));
	}

	public void Taxi(FollowPath path)
	{
		taxiPath = path;
		commandState = CommandStates.Taxi;
		SetReferenceTransform(taxiSteerReferenceTf, RefTfLocks.Taxi);
	}

	public void OrbitPlayer()
	{
		OrbitTransform(FlightSceneManager.instance.playerActor.transform);
	}

	public void OrbitTransform(Transform tf)
	{
		if (NotReadyForFlightCommand())
		{
			queuedCommand = CommandStates.Orbit;
		}
		else
		{
			commandState = CommandStates.Orbit;
		}
		orbitTransform = tf;
		navPath = null;
	}

	public void SetFallbackOrbitTransform(Transform tf)
	{
		orbitTransform = tf;
	}

	public void FormOnPlayer()
	{
		if ((bool)FlightSceneManager.instance && (bool)FlightSceneManager.instance.playerActor)
		{
			if (NotReadyForFlightCommand())
			{
				queuedCommand = CommandStates.FollowLeader;
			}
			else
			{
				commandState = CommandStates.FollowLeader;
			}
			formationLeader = FlightSceneManager.instance.playerActor.GetComponent<AirFormationLeader>();
		}
		else
		{
			Debug.Log(base.gameObject.name + " tried to form on player, but player doesn't exist!", base.gameObject);
		}
	}

	public void FormOnPilot(AIPilot pilot)
	{
		if (!(pilot == this))
		{
			if (NotReadyForFlightCommand())
			{
				queuedCommand = CommandStates.FollowLeader;
			}
			else
			{
				commandState = CommandStates.FollowLeader;
			}
			formationLeader = pilot.formationComponent;
		}
	}

	public void FormOnPilot(AirFormationLeader leader)
	{
		if (!(leader == formationComponent))
		{
			if (NotReadyForFlightCommand())
			{
				queuedCommand = CommandStates.FollowLeader;
			}
			else
			{
				commandState = CommandStates.FollowLeader;
			}
			formationLeader = leader;
		}
	}

	public void SetNavSpeed(float speed)
	{
		navSpeed = speed;
	}

	public void FlyNavPath(FollowPath path)
	{
		if (NotReadyForFlightCommand())
		{
			queuedCommand = CommandStates.Navigation;
		}
		else
		{
			commandState = CommandStates.Navigation;
		}
		navPath = path;
	}

	public void GoRefuel(RefuelPlane rp)
	{
		if ((bool)refuelPort && (bool)fuelTank && fuelTank.fuelFraction < 0.95f)
		{
			if (NotReadyForFlightCommand())
			{
				queuedCommand = CommandStates.AirRefuel;
				queuedRefuelPlane = rp;
			}
			else
			{
				commandState = CommandStates.AirRefuel;
				targetRefuelPlane = rp;
			}
			completedRefuel = false;
		}
	}

	public void OrderAttackTarget(Actor target, bool allowSnapshot = true)
	{
		if ((bool)target && target.alive)
		{
			if (allowSnapshot && TrySnapshotAttack(target))
			{
				Debug.LogFormat("{0} firing snapshot attack on {1}!", actor.actorName, target.actorName);
			}
			else
			{
				ResetAttackTarget();
				KillCombatDecisionRoutines();
				overrideAttackTarget = target;
				UpdateTargets();
			}
		}
	}

	private bool TrySnapshotAttack(Actor target)
	{
		Actor.Roles roles = target.role;
		if (target.overrideCombatTarget)
		{
			roles = target.overriddenCombatRole;
		}
		if ((roles == Actor.Roles.Ground || roles == Actor.Roles.GroundArmor) && wm.availableWeaponTypes.agm && SwitchToGroundMissile(target, requireFAF: true))
		{
			DynamicLaunchZone.LaunchParams dynamicLaunchParams = wm.currentEquip.dlz.GetDynamicLaunchParams(actor.velocity, target.position, target.velocity);
			float magnitude = (target.position - myTransform.position).magnitude;
			if (magnitude > dynamicLaunchParams.minLaunchRange && magnitude < dynamicLaunchParams.maxLaunchRange)
			{
				StartCoroutine(SnapshotAGMRoutine(target, FailedSnapshotOrder));
				return true;
			}
		}
		return false;
	}

	private void FailedSnapshotOrder(Actor target)
	{
		OrderAttackTarget(target, allowSnapshot: false);
	}

	private IEnumerator SnapshotAGMRoutine(Actor target, Action<Actor> onFailed)
	{
		bool num = wm.opticalTargeter.Lock(target.position);
		float attemptMaxTime = 3f;
		if (num && wm.opticalTargeter.lockedActor == target)
		{
			OpticalMissileLauncher ml = ((HPEquipOpticalML)wm.currentEquip).oml;
			bool fired = false;
			float attemptTime = 0f;
			UnityEngine.Random.Range(0.75f, 1f);
			Mathf.Sign(UnityEngine.Random.Range(-1f, 1f));
			while ((bool)target && target.alive && attemptTime < attemptMaxTime && !fired)
			{
				ml.boresightFOVFraction = 1f;
				if (ml.targetLocked)
				{
					bool flag = false;
					if ((bool)aiWing && aiWing.GetNumMissilesOnTarget(target) >= aiWing.maxMissilePerTarget)
					{
						flag = true;
					}
					if (!flag && (bool)target && target.alive)
					{
						if (wm.opticalTargeter.Lock(wm.opticalTargeter.cameraTransform.position, target.position - wm.opticalTargeter.cameraTransform.position))
						{
							wm.SingleFire();
							yield return null;
							fired = true;
							if ((bool)aiWing)
							{
								aiWing.ReportMissileOnTarget(actor, target, wm.lastFiredMissile);
							}
							StartCoroutine(CallMissileResultRoutine(target, wm.lastFiredMissile, WingmanVoiceProfile.Messages.Shack, WingmanVoiceProfile.Messages.GroundMiss));
							PlayRadioMessage(WingmanVoiceProfile.Messages.Rifle);
						}
						else
						{
							attemptTime = attemptMaxTime;
						}
					}
					else
					{
						attemptTime = attemptMaxTime;
					}
				}
				attemptTime += Time.deltaTime;
				yield return null;
			}
			if (!fired)
			{
				onFailed?.Invoke(target);
			}
		}
		else
		{
			onFailed?.Invoke(target);
		}
	}

	public void CancelAttackOrder()
	{
		if ((bool)overrideAttackTarget)
		{
			ResetAttackTarget();
			KillCombatDecisionRoutines();
			if (overrideAttackTarget != null)
			{
				Debug.Log(actor.actorName + " overrideAttackTarget nulled from CancelAttackOrder()");
				overrideAttackTarget = null;
			}
		}
	}

	public bool CommandGoRefuel()
	{
		if (!refuelPort || fuelTank.fuelFraction > 0.95f)
		{
			return false;
		}
		RefuelPlane refuelPlane = FindRefuelTanker();
		if ((bool)refuelPlane)
		{
			if (commandState == CommandStates.Combat)
			{
				StopCombat();
				commandState = CommandStates.Orbit;
			}
			GoRefuel(refuelPlane);
			return true;
		}
		return false;
	}

	private IEnumerator CheckIfNeedRefuelRoutine()
	{
		if (!autoRefuel || !fuelTank)
		{
			yield break;
		}
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 10f));
		while (base.enabled && autoRefuel)
		{
			yield return fiveSecWait;
			yield return fiveSecWait;
			if (!autoRefuel || flightInfo.isLanded || !(fuelTank.fuelFraction < 0.22f))
			{
				continue;
			}
			RefuelPlane refuelPlane = null;
			if ((bool)refuelPort)
			{
				refuelPlane = FindRefuelTanker();
			}
			if ((bool)refuelPlane)
			{
				bool flag = true;
				if (landingState != 0 && landOnPadState != 0)
				{
					flag = false;
				}
				if (flag)
				{
					Debug.LogFormat("{0}: Going to refuel at tanker.", actor.DebugName());
					if (commandState == CommandStates.Combat)
					{
						StopCombat();
						commandState = CommandStates.Orbit;
					}
					GoRefuel(refuelPlane);
					PlayRadioMessage(WingmanVoiceProfile.Messages.LowFuel, 4f);
					while (fuelTank.fuelFraction < 0.5f)
					{
						yield return null;
					}
				}
				else
				{
					Debug.LogFormat("{0}: Already landing. Not going to tanker.", actor.DebugName());
					yield return fiveSecWait;
					yield return fiveSecWait;
					yield return fiveSecWait;
					yield return fiveSecWait;
				}
			}
			else if (rtbAvailable && (bool)aiSpawn && aiSpawn.autoRTB)
			{
				Debug.LogFormat("{0}: Tanker not available, trying RTB to refuel.", actor.DebugName());
				if (landingState == LandingStates.None && landOnPadState == LandOnPadStates.None)
				{
					if (rtbAvailable = aiSpawn.CommandRTB())
					{
						rearmAfterLanding = true;
						if (aiSpawn is AIAWACSSpawn && actor.team == Teams.Allied)
						{
							AIAWACSSpawn aIAWACSSpawn = (AIAWACSSpawn)aiSpawn;
							if (aIAWACSSpawn.commsEnabled)
							{
								aIAWACSSpawn.awacsVoiceProfile.ReportRTB();
							}
						}
						else
						{
							PlayRadioMessage(WingmanVoiceProfile.Messages.ReturningToBase);
						}
						while (fuelTank.fuelFraction < 0.5f)
						{
							yield return null;
						}
					}
					else
					{
						Debug.LogFormat("{0}: RTB for refuel not available.", actor.DebugName());
					}
				}
				else
				{
					Debug.LogFormat("{0}: Already landing.", actor.DebugName());
					while (landingState != 0 || landOnPadState != 0)
					{
						yield return null;
					}
					while (flightInfo.isLanded)
					{
						yield return fiveSecWait;
					}
					Debug.LogFormat("{0}: Finished landing and took off. Resuming fuel check.", actor.DebugName());
				}
			}
			else
			{
				Debug.LogFormat("{0}: Not configured to RTB or RTB not available.", actor.DebugName());
				yield return fiveSecWait;
				yield return fiveSecWait;
				yield return fiveSecWait;
				yield return fiveSecWait;
			}
		}
	}

	private RefuelPlane FindRefuelTanker()
	{
		List<Actor> list = ((actor.team == Teams.Allied) ? TargetManager.instance.alliedUnits : TargetManager.instance.enemyUnits);
		Debug.LogFormat("{0} is looking to refuel.", actor.DebugName());
		float num = float.MaxValue;
		RefuelPlane refuelPlane = null;
		foreach (Actor item in list)
		{
			RefuelPlane component;
			if (item.role == Actor.Roles.Air && (bool)item.flightInfo && !item.flightInfo.isLanded && item != actor && (component = item.GetComponent<RefuelPlane>()) != null && (item.position - myTransform.position).sqrMagnitude < num)
			{
				refuelPlane = component;
			}
		}
		if ((bool)refuelPlane)
		{
			Debug.LogFormat("{0} found tanker: {1}", actor.DebugName(), refuelPlane.actor.DebugName());
		}
		else
		{
			Debug.LogFormat("{0} did not find a tanker.", actor.DebugName());
		}
		return refuelPlane;
	}

	private bool NotReadyForFlightCommand()
	{
		if (!flightInfo.isLanded && commandState != CommandStates.Override)
		{
			return commandState == CommandStates.Park;
		}
		return true;
	}

	private void ApplyQueuedCommand()
	{
		if (queuedCommand == CommandStates.Override)
		{
			if ((bool)formationLeader)
			{
				commandState = CommandStates.FollowLeader;
			}
			else if ((bool)navPath)
			{
				commandState = CommandStates.Navigation;
			}
			else
			{
				commandState = CommandStates.Orbit;
			}
		}
		else if (queuedCommand == CommandStates.AirRefuel)
		{
			commandState = CommandStates.AirRefuel;
			targetRefuelPlane = queuedRefuelPlane;
			completedRefuel = false;
		}
		else
		{
			commandState = queuedCommand;
		}
		if ((bool)aiSpawn)
		{
			autoEngageEnemies = aiSpawn.engageEnemies;
		}
		cancelOverride = false;
		queuedCommand = CommandStates.Override;
	}

	private void Update()
	{
		if (!isAlive)
		{
			return;
		}
		if (isRefuelPlane && (bool)extLightsCtrlr && (commandState == CommandStates.Orbit || commandState == CommandStates.Navigation))
		{
			extLightsCtrlr.SetNavLights(myRefuelPlane.hasTargetRefuelPort ? 1 : 0);
		}
		twr = maxThrust / (rb.mass * 9.81f);
		UpdateRadar();
		if (commandState != CommandStates.Combat && autoEngageEnemies && (flightInfo.radarAltitude > minAltitude || (commandState == CommandStates.FollowLeader && flightInfo.radarAltitude > 50f)) && (commandState != CommandStates.AirRefuel || !refuelPort || refuelPort.fuelTank.fuelFraction > 0.5f))
		{
			UpdateTargets();
		}
		UpdateCommand();
		if (commandState != CommandStates.Combat)
		{
			if (flightInfo.isLanded && flightInfo.airspeed < 20f && landingState != LandingStates.StoppingOnRunway)
			{
				autoPilot.steerMode = AutoPilot.SteerModes.Aim;
			}
			else if (!vtolManeuvering && landingState != LandingStates.FlyToRunway)
			{
				autoPilot.steerMode = AutoPilot.SteerModes.Stable;
			}
			if (!vtolManeuvering)
			{
				SafetyOverrides();
			}
		}
		if (!catHook)
		{
			return;
		}
		if (flightInfo.isLanded && (bool)currentCarrier)
		{
			if (!catHook.deployed)
			{
				catHook.Extend();
			}
		}
		else if (catHook.deployed)
		{
			catHook.Retract();
		}
	}

	private void UpdateRadar()
	{
		if ((bool)detectionRadar)
		{
			bool flag = vt_radarEnabled && playerComms_radarEnabled;
			if (flightInfo.isLanded || !isAlive || commandState == CommandStates.AirRefuel || landingState != 0 || takeOffState != 0 || ctoState != 0)
			{
				flag = false;
			}
			if (!flag && detectionRadar.radarEnabled && (bool)lockingRadar)
			{
				lockingRadar.Unlock();
			}
			detectionRadar.radarEnabled = flag;
		}
	}

	private void LateUpdate()
	{
		if (!isAlive || !FlightSceneManager.isFlightReady || (VTOLMPUtils.IsMultiplayer() && !VTOLMPLobbyManager.isLobbyHost))
		{
			return;
		}
		if (WaterPhysics.GetAltitude(myTransform.position) < 0f)
		{
			crashedIntoTerrain = true;
			health.invincible = false;
			health.Damage(10000f, myTransform.position, Health.DamageTypes.Impact, null, "Crashed into water.");
			this.OnCollisionDeath?.Invoke();
		}
		else if (rb.isKinematic && autoPilot.currentSpeed > 30f && Physics.Linecast(myTransform.position, myTransform.position + kPlane.velocity * Time.deltaTime, 1))
		{
			crashedIntoTerrain = true;
			health.invincible = false;
			health.Damage(10000f, myTransform.position, Health.DamageTypes.Impact, null, "Crashed into terrain.");
			this.OnCollisionDeath?.Invoke();
		}
		if (commandState == CommandStates.Park || commandState == CommandStates.Override)
		{
			if (!landedJoint && !startTimeParking && Time.time - timeParked > 2f && (bool)rearSusp && rearSusp.isTouching && flightInfo.surfaceSpeed < 0.1f && autoPilot.targetSpeed < 0.01f && !rb.isKinematic && (bool)rearSusp.touchingCollider)
			{
				CreateLandedJoint(rearSusp.touchingCollider.transform);
			}
			if (startTimeParking && flightInfo.surfaceSpeed < 0.1f)
			{
				startTimeParking = false;
				timeParked = Time.time;
			}
		}
		if ((commandState != CommandStates.Override && commandState != CommandStates.Park) || (commandState == CommandStates.Override && (autoPilot.targetSpeed > 0.01f || autoPilot.engines[0].inputThrottle > 0.2f)))
		{
			startTimeParking = true;
			if (landedJoint)
			{
				DestroyLandedJoint();
			}
		}
		if (landedJoint)
		{
			UpdateLandedJoint();
		}
		if (flightInfo.isLanded)
		{
			SetCollidersForTaxi();
		}
		else
		{
			SetCollidersToVessel();
		}
		if (actor.team == Teams.Allied)
		{
			bool flag = (bool)formationComponent && formationComponent.HasFollowers() && (bool)aiWing && aiWing.isPlayerWing;
			bool flag2 = isRefuelPlane && myRefuelPlane.hasTargetRefuelPort;
			bool flag3 = !flightInfo.isLanded && !avoidingObstacle && !climbingAboveMinAlt && commandState != CommandStates.Override;
			autoPilot.gentleSteer = flag3 && (flag || flag2);
		}
	}

	private void DebugLog(string s)
	{
	}

	private void DebugLogFormat(string s, params object[] args)
	{
	}

	public static bool IsNonTarget(List<Actor> nonTargets, Actor tgt)
	{
		if (nonTargets.Contains(tgt))
		{
			return true;
		}
		if ((bool)tgt.parentActor)
		{
			return IsNonTarget(nonTargets, tgt.parentActor);
		}
		return false;
	}

	private void UpdateTargets()
	{
		if (!wm)
		{
			return;
		}
		if (attackTarget != null)
		{
			if ((bool)aiWing)
			{
				aiWing.ReportDisengageTarget(attackTarget);
			}
			attackTarget = null;
			this.actor.currentlyTargetingActor = null;
		}
		if (commandState == CommandStates.Evade || commandState == CommandStates.AirRefuel || commandState == CommandStates.Land || commandState == CommandStates.Override || (!overrideAttackTarget && Time.time - lastTgtUpdateTime < 1f))
		{
			return;
		}
		lastTgtUpdateTime = Time.time;
		if ((bool)aiWing)
		{
			attackTarget = aiWing.RequestTarget(this, priorityTargets, nonTargets);
		}
		else
		{
			if ((bool)targetFinder && (bool)targetFinder.attackingTarget)
			{
				int count = targetFinder.targetsSeen.Count;
				int num = UnityEngine.Random.Range(0, count);
				int num2 = 0;
				while (num2 < count)
				{
					Actor tgt = targetFinder.targetsSeen[num];
					if (!IsNonTarget(nonTargets, tgt))
					{
						attackTarget = tgt;
						num2 = count;
					}
					num2++;
					num = (num + 1) % count;
				}
				if (combatRole == CombatRoles.FighterAttack || combatRole == CombatRoles.Fighter)
				{
					if (attackTarget.finalCombatRole != Actor.Roles.Air)
					{
						for (int i = 0; i < targetFinder.targetsSeen.Count; i++)
						{
							Actor actor = targetFinder.targetsSeen[i];
							if (actor.finalCombatRole == Actor.Roles.Air && actor.team != this.actor.team)
							{
								attackTarget = actor;
								break;
							}
						}
					}
				}
				else if (combatRole == CombatRoles.Attack && (attackTarget.finalCombatRole != Actor.Roles.Ground || attackTarget.finalCombatRole != Actor.Roles.GroundArmor))
				{
					for (int j = 0; j < targetFinder.targetsSeen.Count; j++)
					{
						Actor actor2 = targetFinder.targetsSeen[j];
						if ((actor2.finalCombatRole == Actor.Roles.Ground || actor2.finalCombatRole == Actor.Roles.GroundArmor) && actor2.team != this.actor.team)
						{
							attackTarget = actor2;
							break;
						}
					}
				}
			}
			if ((!attackTarget || ((combatRole == CombatRoles.FighterAttack || combatRole == CombatRoles.Fighter) && attackTarget.finalCombatRole != Actor.Roles.Air)) && (bool)detectionRadar && detectionRadar.radarEnabled && detectionRadar.detectedUnits.Count > 0)
			{
				float num3 = float.MaxValue;
				for (int k = 0; k < detectionRadar.detectedUnits.Count; k++)
				{
					Actor actor3 = detectionRadar.detectedUnits[k];
					if ((bool)actor3 && actor3.alive && actor3.finalCombatRole == Actor.Roles.Air && actor3.team != this.actor.team && !IsNonTarget(nonTargets, actor3))
					{
						float sqrMagnitude = (actor3.position - myTransform.position).sqrMagnitude;
						if (sqrMagnitude < num3)
						{
							attackTarget = actor3;
							num3 = sqrMagnitude;
						}
					}
				}
			}
			if (!attackTarget && (bool)moduleRWR)
			{
				for (int l = 0; l < moduleRWR.maxContacts; l++)
				{
					ModuleRWR.RWRContact rWRContact = moduleRWR.contacts[l];
					if (rWRContact.active && rWRContact.radarActor.team != this.actor.team && rWRContact.radarActor.finalCombatRole != Actor.Roles.Missile && !nonTargets.Contains(rWRContact.radarActor))
					{
						if (rWRContact.radarActor.finalCombatRole == Actor.Roles.Air && VectorTo(rWRContact.radarActor.position).sqrMagnitude < wm.maxAntiAirRange * wm.maxAntiAirRange && (combatRole == CombatRoles.Fighter || wm.availableWeaponTypes.aam))
						{
							attackTarget = rWRContact.radarActor;
							break;
						}
						if (rWRContact.radarActor.finalCombatRole != Actor.Roles.Air && (combatRole == CombatRoles.Attack || wm.availableWeaponTypes.antirad))
						{
							attackTarget = rWRContact.radarActor;
							break;
						}
					}
				}
			}
		}
		if (!overrideAttackTarget && designatedTargets.Count > 0)
		{
			Actor actor4 = null;
			designatedTargets.RemoveAll((Actor x) => !x || !x.alive);
			float num4 = float.MaxValue;
			foreach (Actor designatedTarget in designatedTargets)
			{
				if ((bool)designatedTarget && designatedTarget.alive && designatedTarget.gameObject.activeInHierarchy && designatedTarget.finalCombatRole != 0 && (designatedTarget.position - myTransform.position).sqrMagnitude < num4)
				{
					actor4 = designatedTarget;
				}
			}
			if (actor4 != null)
			{
				attackTarget = actor4;
			}
		}
		if (!overrideAttackTarget && (bool)aiWing)
		{
			Actor actor5 = aiWing.RequestDesignatedTarget(this);
			if ((bool)actor5)
			{
				attackTarget = actor5;
			}
		}
		if ((bool)overrideAttackTarget)
		{
			if (overrideAttackTarget.alive)
			{
				attackTarget = overrideAttackTarget;
			}
			else
			{
				PlayRadioMessage(WingmanVoiceProfile.Messages.AttackOrderComplete, 5f, 1f);
				Debug.Log(this.actor.actorName + " overrideAttackTarget nulled because it is dead.");
				overrideAttackTarget = null;
			}
		}
		if (!attackTarget)
		{
			return;
		}
		if (attackTarget.parentActor != null && attackTarget.parentActor.alive && attackTarget.parentActor.finalCombatRole == Actor.Roles.Ship && wm.availableWeaponTypes.antiShip)
		{
			if (attackTarget == overrideAttackTarget)
			{
				overrideAttackTarget = attackTarget.parentActor;
			}
			attackTarget = attackTarget.parentActor;
		}
		if ((bool)aiWing)
		{
			aiWing.ReportEngageTarget(attackTarget);
		}
		if (commandState != CommandStates.Combat)
		{
			PlayRadioMessage(WingmanVoiceProfile.Messages.EngagingTargets, 20f, 1.5f);
		}
		commandState = CommandStates.Combat;
		this.actor.currentlyTargetingActor = attackTarget;
	}

	public void SetCommand(int command)
	{
		commandState = (CommandStates)command;
	}

	public void CommandCancelOverride()
	{
		if (commandState == CommandStates.Override)
		{
			cancelOverride = true;
		}
	}

	public void StopCombat()
	{
		if (combatRoutine != null)
		{
			StopCoroutine(combatRoutine);
			combatRoutine = null;
			KillCombatDecisionRoutines();
			if ((bool)attackTarget)
			{
				ResetAttackTarget();
			}
			Disarm();
			if (overrideAttackTarget != null && commandState != CommandStates.Evade)
			{
				Debug.Log(actor.actorName + " overrideAttackTarget nulled from StopCombat()");
				overrideAttackTarget = null;
			}
		}
		if (commandState == CommandStates.Combat)
		{
			ApplyQueuedCommand();
		}
	}

	public void StopFollowingLeader()
	{
		if (commandState == CommandStates.FollowLeader)
		{
			if ((bool)formationLeader)
			{
				formationLeader.UnregisterFollower(this);
			}
			formationTransform = null;
			ApplyQueuedCommand();
		}
	}

	private void UpdateCommand()
	{
		autoPilot.throttleLimiter = 1f;
		if ((bool)fuelTank && fuelTank.fuelFraction < 0.2f)
		{
			autoPilot.throttleLimiter = 0.69f;
		}
		if (commandState != CommandStates.FollowLeader && (bool)formationTransform)
		{
			if ((bool)formationLeader)
			{
				formationLeader.UnregisterFollower(this);
			}
			formationTransform = null;
		}
		if (commandState != CommandStates.Combat && combatRoutine != null)
		{
			StopCombat();
		}
		isExitingRefuel = false;
		if ((bool)targetRefuelPlane && commandState != CommandStates.AirRefuel)
		{
			FlyRefuel(exitingRefuel: true);
			isExitingRefuel = true;
			return;
		}
		switch (commandState)
		{
		case CommandStates.Navigation:
			if (!navPath)
			{
				commandState = CommandStates.Orbit;
				UpdateCommand();
			}
			else
			{
				FlyNav(-1f);
				autoPilot.targetSpeed = navSpeed;
			}
			break;
		case CommandStates.Orbit:
			autoPilot.inputLimiter = 0.5f;
			FlyOrbit(orbitTransform, orbitRadius, navSpeed, defaultAltitude, orbitClockwise);
			break;
		case CommandStates.Combat:
			if (combatRoutine == null)
			{
				combatRoutine = StartCoroutine(CombatRoutine());
			}
			break;
		case CommandStates.Evade:
			if (!evadeTarget.actor)
			{
				if (queuedCommand != CommandStates.Override)
				{
					commandState = queuedCommand;
					queuedCommand = CommandStates.Override;
				}
				else if ((bool)wm && wm.availableWeaponTypes.HasAny() && (autoEngageEnemies || (bool)attackTarget || (bool)overrideAttackTarget))
				{
					commandState = CommandStates.Combat;
				}
				else
				{
					ApplyQueuedCommand();
				}
				UpdateCommand();
			}
			else
			{
				autoPilot.inputLimiter = 1f;
				EvadeThreat();
			}
			break;
		case CommandStates.Taxi:
			if (!taxiPath)
			{
				Debug.LogError("AI pilot has no taxi path.");
				commandState = CommandStates.Park;
				UpdateCommand();
			}
			else
			{
				TaxiNav(taxiPath, 2f);
			}
			break;
		case CommandStates.Park:
			if (!vtolManeuvering)
			{
				autoPilot.targetSpeed = 0f;
				autoPilot.targetPosition = myTransform.position + myTransform.forward * 1000f;
			}
			break;
		case CommandStates.Land:
			if (!targetRunway)
			{
				Debug.LogError("AI Pilot has no target runway for landing.");
				commandState = CommandStates.Orbit;
				UpdateCommand();
			}
			else if (landingRoutine == null)
			{
				landingRoutine = StartCoroutine(LandingRoutine());
			}
			break;
		case CommandStates.AirRefuel:
			if (!targetRefuelPlane)
			{
				Debug.LogError("AI Pilot has no target refuel plane.");
				commandState = CommandStates.Orbit;
				UpdateCommand();
			}
			else if (FlyRefuel())
			{
				queuedRefuelPlane = null;
				ApplyQueuedCommand();
				if ((bool)extLightsCtrlr)
				{
					extLightsCtrlr.SetAllLights(0);
				}
			}
			break;
		case CommandStates.FollowLeader:
			if (!formationLeader || !formationLeader.gameObject.activeSelf || !formationLeader.actor.alive || (formationLeader.actor.isPlayer && FlightSceneManager.instance.playerHasEjected))
			{
				formationLeader = null;
				ApplyQueuedCommand();
				UpdateCommand();
				break;
			}
			if (!formationTransform || formationLeader != _lastFormationLeader)
			{
				if ((bool)_lastFormationLeader)
				{
					_lastFormationLeader.UnregisterFollower(this);
				}
				formationTransform = formationLeader.RegisterFollower(this, out lastFormationIdx);
				_lastFormationLeader = formationLeader;
			}
			FlyFormation(formationTransform, formationLeader.actor.velocity);
			break;
		}
	}

	private void BeginDeploySafety()
	{
		_deploySafetyTime = Time.time;
		_deploySafetyDir = rb.velocity + 5f * myTransform.up;
		_deploySafetyUp = myTransform.up;
	}

	private void SafetyOverrides(bool ignoreOverride = false, float overrideMinAlt = -1f, bool doObstCheck2 = true)
	{
		if (commandState == CommandStates.Park || commandState == CommandStates.Taxi || (!ignoreOverride && commandState == CommandStates.Override))
		{
			return;
		}
		Vector3 current = Vector3.ProjectOnPlane(rb.velocity, Vector3.up);
		if (!flightInfo.isLanded)
		{
			UnlockReferenceTransform(RefTfLocks.Taxi);
		}
		if (commandState != CommandStates.Land && commandState != 0)
		{
			if (forceReturnDistance > 0f)
			{
				Transform transform = orbitTransform;
				if (commandState == CommandStates.FollowLeader)
				{
					transform = formationLeader.transform;
				}
				else if (commandState == CommandStates.AirRefuel && (bool)targetRefuelPlane)
				{
					transform = targetRefuelPlane.transform;
				}
				float num = Vector3.Distance(myTransform.position, transform.position);
				if (returningToObjective)
				{
					autoPilot.targetPosition = transform.position;
					autoPilot.targetSpeed = maxSpeed;
					if (num < forceReturnDistance * 0.9f)
					{
						returningToObjective = false;
					}
				}
				else if (num > forceReturnDistance)
				{
					returningToObjective = true;
				}
			}
			AltitudeSafety(overrideMinAlt, doObstCheck2);
		}
		Vector3 vector = autoPilot.targetPosition - myTransform.position;
		if (Vector3.Dot(vector, myTransform.forward) < 0f)
		{
			vector = Vector3.RotateTowards(current, vector, (float)Math.PI / 4f, float.MaxValue);
			vector = Vector3.RotateTowards(Vector3.up, vector, (float)Math.PI / 2f, float.MaxValue);
		}
		float radarAltitude = flightInfo.radarAltitude;
		if (radarAltitude > maxAltitude * 0.9f)
		{
			float t = (radarAltitude - maxAltitude * 0.9f) / (maxAltitude * 0.2f);
			float num2 = Mathf.Lerp(180f, 0f, t);
			vector = Vector3.RotateTowards(Vector3.down, vector, num2 * ((float)Math.PI / 180f), float.MaxValue);
		}
		if (isDeploySafety)
		{
			autoPilot.SetOverrideRollTarget(_deploySafetyUp);
			_deploySafetyDir += 5f * Time.deltaTime * _deploySafetyUp;
			vector = _deploySafetyDir;
		}
		autoPilot.targetPosition = myTransform.position + vector.normalized * 1000f;
		FormationBreakSafety();
	}

	private void FormationBreakSafety()
	{
		if (commandState == CommandStates.FollowLeader)
		{
			if (wasFormation && formationLeader != previousFormationLeader)
			{
				if ((bool)previousFormationLeader)
				{
					timeBrokeFormation = Time.time;
					formationBreakTf = previousFormationLeader.transform;
				}
				previousFormationLeader = formationLeader;
			}
			wasFormation = true;
		}
		else
		{
			if (wasFormation)
			{
				if (commandState == CommandStates.AirRefuel)
				{
					if (VectorTo(targetRefuelPlane.refuelPositionTransform.position).sqrMagnitude > 10000f)
					{
						timeBrokeFormation = Time.time;
						if ((bool)formationLeader)
						{
							formationBreakTf = formationLeader.transform;
						}
					}
				}
				else
				{
					timeBrokeFormation = Time.time;
					if ((bool)formationLeader)
					{
						formationBreakTf = formationLeader.transform;
					}
				}
			}
			wasFormation = false;
		}
		if (!isExitingRefuel && Time.time - timeBrokeFormation < 4f && (bool)formationBreakTf)
		{
			Vector3 normalized = (myTransform.position - formationBreakTf.position).normalized;
			Vector3 vector = formationBreakTf.forward * 1000f + normalized * 250f;
			autoPilot.targetPosition = autoPilot.referenceTransform.position + vector.normalized * 1000f;
		}
	}

	private void AltitudeSafety(float overrideMinAlt = -1f, bool doObstCheck2 = true)
	{
		float sweptRadarAltitude = flightInfo.sweptRadarAltitude;
		flightInfo.fwdSweepRadarAlt = true;
		Vector3 vector = base.transform.forward * 1000f;
		vector.y = 0f;
		float hardMinimum = minAltitude;
		float num = minAltClimbThresh;
		if (overrideMinAlt > 0f)
		{
			hardMinimum = overrideMinAlt;
			num = Mathf.Min(100f, overrideMinAlt / 2f);
		}
		if (commandState == CommandStates.FollowLeader && !waitingForLeaderOrbit)
		{
			float num2 = Mathf.Max(500f, minAltitude - 50f);
			if (formationSqrDist < num2 * num2)
			{
				hardMinimum = 50f;
				num = 80f;
			}
			doObstCheck2 = false;
		}
		if (landingState == LandingStates.FlyToRunway)
		{
			hardMinimum = 0f;
		}
		float minAltAtAttitudeAndSpeed = GetMinAltAtAttitudeAndSpeed(hardMinimum);
		if (sweptRadarAltitude < minAltAtAttitudeAndSpeed)
		{
			climbingAboveMinAlt = true;
		}
		if (doObstCheck2)
		{
			ObstacleCheck2();
		}
		ObstacleCheck();
		if (!climbingAboveMinAlt)
		{
			altCheckAhead = !altCheckAhead;
		}
		if (!avoidingObstacle && climbingAboveMinAlt)
		{
			if (catapulting)
			{
				autoPilot.inputLimiter = 0.65f;
			}
			if (Vector3.Dot(flightInfo.surfaceNormal, myTransform.forward) < 0f)
			{
				autoPilot.inputLimiter = 1f;
			}
			Vector3 target = Vector3.ProjectOnPlane(autoPilot.targetPosition - myTransform.position, Vector3.up);
			Vector3 vector2 = vector;
			if (sweptRadarAltitude > 50f)
			{
				vector2 = Vector3.RotateTowards(vector, target, 0.08726646f, 0f);
			}
			if (!flightInfo.isLanded || autoPilot.currentSpeed > minCombatSpeed || catapulting)
			{
				Vector3 lhs = Vector3.up;
				if (flightInfo.radarAltitude < flightInfo.altitudeASL - 1f)
				{
					lhs = flightInfo.surfaceNormal;
				}
				vector2 = Quaternion.AngleAxis(-50f, Vector3.Cross(lhs, vector2)) * vector2;
				if (sweptRadarAltitude > 100f || catapulting)
				{
					vector2 = GetLimitedClimbDirectionForSpeed(vector2);
				}
			}
			if (flightInfo.isLanded && autoPilot.currentSpeed < minCombatSpeed && (bool)targetRunway)
			{
				vector2 = targetRunway.transform.forward * 1000f + 20f * Vector3.up;
			}
			if (commandState == CommandStates.FollowLeader && (bool)formationLeader && SteerAvoidActor(formationLeader.actor, out var avoidDirection))
			{
				vector2 = Vector3.RotateTowards(vector2, avoidDirection, (float)Math.PI / 4f, 0f);
			}
			autoPilot.targetPosition = myTransform.position + vector2;
			if (sweptRadarAltitude > minAltAtAttitudeAndSpeed + num)
			{
				climbingAboveMinAlt = false;
				if (commandState == CommandStates.Combat && (bool)attackTarget && sweptRadarAltitude < gunRunStartAltitude && attackTarget.transform.position.y - WaterPhysics.instance.height < 100f)
				{
					climbingAboveMinAlt = true;
				}
			}
		}
		else
		{
			if (autoPilot.targetPosition.y < WaterPhysics.instance.height + 50f && sweptRadarAltitude < 130f && Vector3.Dot(autoPilot.targetPosition - myTransform.position, Vector3.up) < 0f)
			{
				Vector3 targetPosition = autoPilot.targetPosition;
				targetPosition.y = WaterPhysics.instance.height + 50f;
				autoPilot.targetPosition = targetPosition;
			}
			catapulting = false;
		}
	}

	private void ApplyPlanarTurnAround(float dotLim = 0f)
	{
		Vector3 normalized = (autoPilot.targetPosition - myTransform.position).normalized;
		normalized = ApplyPlanarTurnAround(normalized, dotLim);
		autoPilot.targetPosition = myTransform.position + normalized;
	}

	private Vector3 ApplyPlanarTurnAround(Vector3 tgtVec, float dotLim)
	{
		Vector3 target = tgtVec;
		target.y = 0f;
		Vector3 forward = myTransform.forward;
		forward.y = 0f;
		if (Vector3.Dot(target.normalized, forward.normalized) < dotLim)
		{
			tgtVec = Vector3.RotateTowards(forward, target, planarTurnaroundMaxAngle * ((float)Math.PI / 180f), float.MaxValue).normalized * 1000f;
			tgtVec += (planarTurnaroundBase - rb.velocity.y * planarTurnaroundP) * Vector3.up;
		}
		return tgtVec.normalized * 1000f;
	}

	private void ObstacleCheck()
	{
		if (avoidingObstacle || Time.time - obstacleCheckTime > obstacleCheckInterval)
		{
			Ray ray = new Ray(myTransform.position, rb.velocity * obstacleCheckAheadTime + 0.5f * obstacleCheckAheadTime * obstacleCheckAheadTime * flightInfo.acceleration);
			avoidingObstacle = Physics.SphereCast(ray, 20f, out var hitInfo, flightInfo.airspeed * obstacleCheckAheadTime, 1);
			if (avoidingObstacle)
			{
				obstacleNormal = hitInfo.normal;
			}
			obstacleCheckTime = Time.time;
		}
		if (avoidingObstacle)
		{
			autoPilot.targetPosition = myTransform.position + obstacleNormal * 100f + new Vector3(0f, 100f, 0f);
			autoPilot.inputLimiter = 1f;
			if (Time.time - obstacleCheckTime > obstacleCheckInterval)
			{
				avoidingObstacle = false;
			}
		}
	}

	private void ObstacleCheck2()
	{
		avoidingObstacle = false;
		if ((bool)obstChecker && obstChecker.CheckAvoidObstacles(autoPilot.targetPosition, out var correctedPosition))
		{
			avoidingObstacle = true;
			autoPilot.targetPosition = correctedPosition;
		}
		float sweptRadarAltitude = flightInfo.sweptRadarAltitude;
		if (sweptRadarAltitude > minAltitude * 6f)
		{
			return;
		}
		float num = flightInfo.altitudeASL - sweptRadarAltitude;
		for (float num2 = 0.25f; num2 <= 1.01f; num2 += 0.25f)
		{
			float num3 = 8f * num2;
			Vector3 vector = (autoPilot.targetPosition - myTransform.position).normalized * (autoPilot.currentSpeed * num3);
			float num4 = WaterPhysics.GetAltitude(myTransform.position + vector) - num;
			float num5 = minAltitude - num4;
			if (num5 > 0f)
			{
				vector += new Vector3(0f, num5, 0f);
				autoPilot.targetPosition = myTransform.position + vector;
				avoidingObstacle = true;
			}
		}
	}

	private void FlyNav(float leadDistance)
	{
		bool flag = false;
		if (!navPath)
		{
			flag = true;
		}
		else
		{
			if (flightInfo.radarAltitude < minAltitude)
			{
				autoPilot.inputLimiter = 1f;
			}
			else
			{
				autoPilot.inputLimiter = 1f;
			}
			Vector3 vector;
			if (leadDistance < 0f)
			{
				float closestTimeWorld = navPath.GetClosestTimeWorld(myTransform.position);
				float magnitude = (navPath.GetWorldPoint(closestTimeWorld) - myTransform.position).magnitude;
				leadDistance = Mathf.Lerp(navLeadDistance, 4f * navLeadDistance, 2f * magnitude / navLeadDistance);
				float num = leadDistance / navPath.GetApproximateLength();
				float t = closestTimeWorld + num;
				vector = navPath.GetWorldPoint(t);
				autoPilot.targetPosition = vector;
			}
			else
			{
				vector = navPath.GetFollowPoint(myTransform.position, leadDistance, 4, lastFormationIdx);
				autoPilot.targetPosition = vector;
			}
			ApplyPlanarTurnAround();
			if (!navPath.loop && (vector - navPath.pointTransforms[navPath.pointTransforms.Length - 1].position).sqrMagnitude < 100f)
			{
				flag = true;
			}
		}
		if (flag)
		{
			navPath = null;
			ApplyQueuedCommand();
		}
	}

	private void TaxiNav(FollowPath path, float leadDistance)
	{
		if (isVtol && (bool)tiltController)
		{
			tiltController.PadInput(Vector3.up);
		}
		bool flag = false;
		foreach (ModuleEngine engine in autoPilot.engines)
		{
			if (!engine.startedUp)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			autoPilot.targetSpeed = 0f;
			return;
		}
		Transform transform = (autoPilot.wheelSteerReferenceTf ? autoPilot.wheelSteerReferenceTf : taxiSteerReferenceTf);
		autoPilot.inputLimiter = 1f;
		autoPilot.targetPosition = path.GetFollowPoint(transform.position, leadDistance, out var currentT);
		autoPilot.targetSpeed = taxiSpeed;
		SetThrottleLimiterDisallowAB();
		Vector3 dir = path.GetFollowPoint(transform.position, 10f) - autoPilot.referenceTransform.position;
		if (TaxiCollisionDetected(dir))
		{
			autoPilot.targetSpeed = 0f;
		}
		if (currentT >= 0.999f)
		{
			commandState = CommandStates.Park;
			autoPilot.throttleLimiter = 1f;
		}
	}

	private bool TaxiNavManual(FollowPath path, float leadDistance, float endThresh = 0.999f)
	{
		if (isVtol && (bool)tiltController)
		{
			tiltController.PadInput(Vector3.up);
		}
		Transform transform = (autoPilot.wheelSteerReferenceTf ? autoPilot.wheelSteerReferenceTf : taxiSteerReferenceTf);
		autoPilot.inputLimiter = 1f;
		autoPilot.targetPosition = path.GetFollowPoint(transform.position, leadDistance, out var currentT);
		autoPilot.targetSpeed = taxiSpeed;
		SetThrottleLimiterDisallowAB();
		Vector3 dir = path.GetFollowPoint(transform.position, 10f) - autoPilot.referenceTransform.position;
		if (TaxiCollisionDetected(dir))
		{
			autoPilot.targetSpeed = 0f;
		}
		return currentT >= endThresh;
	}

	private IEnumerator TaxiNavRoutine(FollowPath path, float leadDistance)
	{
		Transform wheelTf = (autoPilot.wheelSteerReferenceTf ? autoPilot.wheelSteerReferenceTf : taxiSteerReferenceTf);
		while (true)
		{
			if (isVtol && (bool)tiltController)
			{
				tiltController.PadInput(Vector3.up);
			}
			autoPilot.targetPosition = path.GetFollowPoint(wheelTf.position, leadDistance, out var currentT);
			autoPilot.inputLimiter = 1f;
			autoPilot.targetSpeed = taxiSpeed;
			SetThrottleLimiterDisallowAB();
			Vector3 dir = path.GetFollowPoint(wheelTf.position, 10f) - wheelTf.position;
			if (TaxiCollisionDetected(dir))
			{
				autoPilot.targetSpeed = 0f;
			}
			if (currentT >= 0.999f)
			{
				break;
			}
			yield return null;
		}
		autoPilot.targetSpeed = 0f;
	}

	private bool TaxiCollisionDetected(Vector3 dir)
	{
		dir.y = 0f;
		dir = Vector3.RotateTowards(myTransform.forward, dir, (float)Math.PI / 4f, 0f);
		if (collisionDetectCooldown > 0f)
		{
			if (!(Time.time - collisionDetectCooldown > maxCollisionDetectTime))
			{
				return false;
			}
			collisionDetectCooldown = -1f;
			collisionDetectTime = -1f;
		}
		if (Time.time - collisionDetectTime < minTaxiCollisionDetectTime)
		{
			taxiCollisionTfIdx = -1;
			return true;
		}
		bool flag = taxiCollisionTfIdx == -1;
		int num = taxiCollisionTfIdx;
		taxiCollisionTfIdx = (taxiCollisionTfIdx + 1) % taxiCollisionDetectTfs.Length;
		for (num = ((!flag) ? num : 0); num < taxiCollisionDetectTfs.Length; num++)
		{
			if (Physics.Raycast(new Ray(taxiCollisionDetectTfs[num].position, dir), out var hitInfo, 24f, 9473))
			{
				AIPilot aIPilot = null;
				Actor componentInParent = hitInfo.collider.GetComponentInParent<Actor>();
				if ((bool)componentInParent)
				{
					aIPilot = componentInParent.GetComponent<AIPilot>();
				}
				bool flag2 = false;
				bool flag3 = true;
				if ((bool)componentInParent && componentInParent.isPlayer)
				{
					flag3 = true;
					flag2 = true;
				}
				else if ((bool)aIPilot && Vector3.Dot(aIPilot.transform.forward, myTransform.forward) < 0f && aIPilot.GetInstanceID() < GetInstanceID())
				{
					flag3 = false;
				}
				if (flag3)
				{
					if (collisionDetectTime < 0f || flag2)
					{
						collisionDetectTime = Time.time;
					}
					else if (Time.time - collisionDetectTime > maxCollisionDetectTime)
					{
						collisionDetectCooldown = Time.time;
						return false;
					}
					return true;
				}
			}
			if (!flag)
			{
				break;
			}
		}
		if (flag)
		{
			collisionDetectTime = -1f;
		}
		collisionDetectCooldown = -1f;
		return false;
	}

	private void FlyOrbit(Vector3 flightCenter, float radius, float speed, float altitude, bool clockwise)
	{
		Vector3 up = Vector3.up;
		Vector3 vector = Vector3.ProjectOnPlane(myTransform.position - flightCenter, up).normalized * radius;
		Vector3 vector2 = Quaternion.AngleAxis(clockwise ? 15f : (-15f), up) * vector;
		Vector3 vector3 = Vector3.Project(rb.velocity, up);
		Vector3 vector4 = flightCenter + vector2;
		vector4.y = WaterPhysics.instance.height + altitude;
		Vector3 vector5 = vector4 - myTransform.position;
		float sqrMagnitude = Vector3.ProjectOnPlane(vector5, Vector3.up).sqrMagnitude;
		vector5 = ((!(sqrMagnitude < 16000000f)) ? (vector5.normalized * 1000f) : (vector5.normalized * 1000f - vector3 * 0.25f + (altitude - flightInfo.altitudeASL) * 0.25f * Vector3.up));
		vector5 = ApplyPlanarTurnAround(vector5, 0.9f);
		vector5 = GetLimitedClimbDirectionForSpeed(vector5);
		vector5 = GetLevelFarFlightDirection(vector5, sqrMagnitude) * 1000f;
		vector4 = myTransform.position + vector5;
		autoPilot.targetPosition = vector4;
		autoPilot.targetSpeed = speed;
	}

	private void FlyOrbit(Transform centerTf, float radius, float speed, float altitude, bool clockwise)
	{
		FlyOrbit(centerTf.position, radius, speed, altitude, clockwise);
	}

	private bool FlyRefuel(bool exitingRefuel = false)
	{
		RefuelPlane refuelPlane = targetRefuelPlane;
		if (!refuelPlane.IsAIPilotReady())
		{
			targetRefuelPlane = null;
			refuelQueueFormationTf = null;
			Debug.Log("refuel plane not ready.  breaking out.");
			return true;
		}
		if (refuelPlane == null)
		{
			refuelQueueFormationTf = null;
			return true;
		}
		float sqrMagnitude = VectorTo(refuelPlane.transform.position).sqrMagnitude;
		if ((bool)extLightsCtrlr && sqrMagnitude < 1000000f)
		{
			extLightsCtrlr.SetNavLights(1);
		}
		if (!exitingRefuel && !completedRefuel)
		{
			if (sqrMagnitude > 1000000f || !refuelPlane.RequestRefuelReservation(refuelPort))
			{
				if (formationLeader == refuelPlane.aiPilot.formationComponent && formationTransform != null)
				{
					FlyFormation(formationTransform, formationLeader.actor.velocity);
				}
				else
				{
					if (refuelQueueFormationTf == null)
					{
						refuelQueueFormationTf = refuelPlane.aiPilot.formationComponent.RegisterFollower(this);
						lastFormationIdx = -1;
					}
					FlyFormation(refuelQueueFormationTf, refuelPlane.actor.velocity);
				}
				return false;
			}
			if (refuelQueueFormationTf != null)
			{
				refuelPlane.aiPilot.formationComponent.UnregisterFollower(this);
				refuelQueueFormationTf = null;
			}
		}
		Vector3 velocity = refuelPlane.actor.velocity;
		float magnitude = velocity.magnitude;
		Vector3 position = refuelPlane.refuelPositionTransform.position;
		float sqrMagnitude2 = (position - refuelPort.transform.position).sqrMagnitude;
		Vector3 vector = Vector3.ProjectOnPlane(Vector3.down * 72f, refuelPlane.refuelPositionTransform.up);
		float targetSpeed;
		if (refuelPort.fuelTank.fuelFraction > 0.99f || exitingRefuel || completedRefuel)
		{
			completedRefuel = true;
			position += refuelPlane.actor.velocity.normalized * 500f + vector;
			targetSpeed = magnitude - 10f;
			if (sqrMagnitude2 > 10000f)
			{
				targetRefuelPlane.CancelReservation(refuelPort);
				targetRefuelPlane = null;
				if (refuelPort.open)
				{
					refuelPort.Close();
				}
				extLightsCtrlr.SetNavLights(0);
				return true;
			}
			if (sqrMagnitude2 > 2500f && refuelPort.open)
			{
				refuelPort.Close();
			}
		}
		else if (sqrMagnitude2 < 1000000f)
		{
			targetSpeed = magnitude;
			float current = 0f - refuelPort.transform.InverseTransformPoint(position).z;
			float value = formationFwdPID.Evaluate(current, 0f);
			targetSpeed += Mathf.Clamp(value, -30f, 30f);
			position += refuelPlane.actor.velocity.normalized * 500f + vector;
			Vector3 vector2 = Vector3.ProjectOnPlane(refuelPlane.refuelPositionTransform.position - refuelPort.transform.position, velocity);
			Vector3 vector3 = Vector3.ProjectOnPlane(velocity - kPlane.velocity, velocity);
			vector2 = Vector3.ClampMagnitude(vector2 * formationP, maxFormationOffsetAdjust) + vector3 * formationD;
			position += vector2;
			if (sqrMagnitude2 < 2500f && !refuelPort.open)
			{
				refuelPort.Open();
			}
			autoPilot.inputLimiter = 1f;
		}
		else
		{
			targetSpeed = Mathf.Lerp(maxSpeed, magnitude + 15f, 2250000f / sqrMagnitude2);
		}
		autoPilot.targetPosition = position;
		autoPilot.targetSpeed = targetSpeed;
		return false;
	}

	private void FlyFormation(Transform formationTf, Vector3 leaderVelocity)
	{
		float magnitude = leaderVelocity.magnitude;
		if (magnitude < minCombatSpeed)
		{
			FlyOrbit(formationTf, orbitRadius, navSpeed, WaterPhysics.GetAltitude(formationTf.position) + minAltitude + minAltClimbThresh, orbitClockwise);
			waitingForLeaderOrbit = true;
			return;
		}
		waitingForLeaderOrbit = false;
		Vector3 vector = formationTf.position;
		Vector3 vector2 = vector - myTransform.position;
		vector2.y = 0f;
		float num = (formationSqrDist = vector2.sqrMagnitude);
		float num2 = VectorUtils.SignedAngle(Vector3.ProjectOnPlane(Vector3.up, formationTf.forward), formationTf.up, Vector3.Cross(Vector3.up, formationTf.forward));
		Vector3 vector3 = 0.035f * formationRightAdjust * num2 * formationTf.right;
		float num3;
		if (num < 1000000f)
		{
			num3 = magnitude;
			float current = 0f - myTransform.InverseTransformPoint(vector).z;
			float value = formationFwdPID.Evaluate(current, 0f);
			num3 += Mathf.Clamp(value, -30f, 30f);
			vector += leaderVelocity.normalized * formationLeadDist + vector3;
			Vector3 vector4 = Vector3.ProjectOnPlane(formationTf.position - base.transform.position, leaderVelocity);
			Vector3 vector5 = Vector3.ProjectOnPlane(kPlane.velocity, leaderVelocity);
			vector4 = Vector3.ClampMagnitude(vector4 * formationP, maxFormationOffsetAdjust) - vector5 * formationD;
			vector += vector4;
			if (WaterPhysics.GetAltitude(vector) < 50f)
			{
				Vector3 vector6 = vector - myTransform.position;
				vector6.y = 0f;
				vector6 = vector6.normalized * formationLeadDist;
				vector = myTransform.position + vector6;
				vector.y = WaterPhysics.instance.height + 50f + 80f;
			}
			if ((bool)formationLeader)
			{
				if (Vector3.Dot(myTransform.position - formationLeader.actor.position, formationTf.position - formationLeader.actor.position) < 0f || Vector3.Dot(myTransform.forward, formationLeader.actor.transform.forward) < 0f)
				{
					vector += 70f * Vector3.Project(myTransform.position - formationLeader.actor.position, formationLeader.actor.transform.up).normalized;
				}
				if (SteerAvoidActor(formationLeader.actor, out var avoidDirection, 900f))
				{
					Vector3 vector7 = vector - myTransform.position;
					vector7 = avoidDirection * formationLeadDist;
					vector = myTransform.position + vector7;
				}
			}
			autoPilot.inputLimiter = 1f;
		}
		else
		{
			Vector3 vector8 = leaderVelocity.normalized * Mathf.Clamp(num / (debug_formationDivRejoinDiv * debug_formationDivRejoinDiv) - 1000f, formationLeadDist, 8000f);
			Vector3 vector9 = vector + vector8;
			Vector3 limitedClimbDirectionForSpeed = GetLimitedClimbDirectionForSpeed(vector9 - myTransform.position);
			limitedClimbDirectionForSpeed = GetLevelFarFlightDirection(limitedClimbDirectionForSpeed, (formationTf.position - myTransform.position).sqrMagnitude);
			vector = myTransform.position + limitedClimbDirectionForSpeed * 1000f;
			num3 = Mathf.Lerp(maxSpeed, magnitude + 10f, 1000000f / num);
			if (WaterPhysics.GetAltitude(vector) < minAltitude)
			{
				Vector3 vector10 = vector - myTransform.position;
				vector10.y = 0f;
				vector10 = vector10.normalized * formationLeadDist;
				vector = myTransform.position + vector10;
				vector.y = WaterPhysics.instance.height + minAltitude + minAltClimbThresh;
			}
		}
		autoPilot.targetPosition = vector;
		autoPilot.targetSpeed = num3;
	}

	private Vector3 GetLimitedClimbDirectionForSpeed(Vector3 direction)
	{
		if (Vector3.Dot(direction, Vector3.up) < 0f)
		{
			return direction;
		}
		Vector3 current = (direction - direction.y * Vector3.up).normalized * 1000f;
		float num = Mathf.Clamp(autoPilot.currentSpeed * 0.07f * twr, 5f, 90f);
		return Vector3.RotateTowards(current, direction, num * ((float)Math.PI / 180f), 0f);
	}

	private Vector3 GetLevelFarFlightDirection(Vector3 direction, float sqrDist)
	{
		Vector3 vector = direction;
		vector.y = 0f;
		if (direction.y > 0f)
		{
			float num = Mathf.Min(direction.normalized.y * 100f, 20f);
			vector = Vector3.RotateTowards(vector, Vector3.up, num * ((float)Math.PI / 180f), 0f);
		}
		vector.Normalize();
		float num2 = 16000000f;
		float t = 1f - Mathf.Clamp01((sqrDist - num2) / num2);
		return Vector3.Slerp(vector, direction.normalized, t);
	}

	private IEnumerator WingmanDefeatedMsgDelayed()
	{
		yield return oneSecWait;
		if (isAlive)
		{
			PlayRadioMessage(WingmanVoiceProfile.Messages.DefeatedMissile);
		}
	}

	private void EvadeThreat()
	{
		float num = Vector3.SqrMagnitude(evadeTarget.position - myTransform.position);
		if (num > missileEvadeDistance * missileEvadeDistance)
		{
			evadeTarget.Reset();
			return;
		}
		if (Time.time - evadeTarget.startTime > evadeTarget.evadeDuration)
		{
			if (!evadeTarget.isMissile)
			{
				evadeTarget.Reset();
				return;
			}
			_ = rb.velocity - evadeTarget.velocity;
		}
		if (evadeTarget.isMissile)
		{
			Missile missile = (evadeTarget.actor ? evadeTarget.actor.GetMissile() : null);
			if (!missile || !missile.hasTarget || Vector3.Dot(missile.actor.velocity.normalized, (myTransform.position - missile.actor.position).normalized) < 0f)
			{
				if (doRadioComms)
				{
					StartCoroutine(WingmanDefeatedMsgDelayed());
				}
				evadeTarget.Reset();
				return;
			}
			if (!firingCms)
			{
				bool chaff = evadeTarget.isRadarMissile || evadeTarget.isUnknownMissile;
				bool flares = evadeTarget.isHeatMissile || evadeTarget.isUnknownMissile;
				StartCoroutine(CountermeasureRoutine(flares, chaff));
			}
			if (evadeTarget.isHeatMissile)
			{
				if (num > 25000000f)
				{
					autoPilot.inputLimiter = 0.5f;
				}
				if (num > 160000f)
				{
					autoPilot.throttleLimiter = 0.25f;
				}
				else
				{
					autoPilot.throttleLimiter = 1f;
				}
			}
			float num2 = Vector3.Dot(evadeTarget.velocity - actor.velocity, (myTransform.position - evadeTarget.position).normalized);
			float num3 = num / (num2 * num2);
			autoPilot.targetSpeed = maxSpeed;
			if (num3 < 9f)
			{
				Vector3 vector = Vector3.Cross(evadeTarget.position - myTransform.position, rb.velocity).normalized;
				if (Vector3.Dot(vector, myTransform.up) < 0f)
				{
					vector = -vector;
				}
				autoPilot.targetPosition = myTransform.position + 50f * rb.velocity.normalized + 100f * vector;
				if (!evadeTarget.isHeatMissile)
				{
					autoPilot.inputLimiter = 1f;
				}
				return;
			}
			Vector3 vector2 = evadeTarget.position - myTransform.position;
			if (evadeTarget.isRadarMissile)
			{
				RadarLockData radarLock = evadeTarget.actor.GetMissile().radarLock;
				if (radarLock != null && (bool)radarLock.lockingRadar)
				{
					vector2 = radarLock.lockingRadar.transform.position - myTransform.position;
				}
			}
			int instanceID = GetInstanceID();
			Vector3 vector3 = Vector3.Cross(Vector3.up, vector2);
			if (!evadeTarget.isRadarMissile)
			{
				if (Mathf.RoundToInt((Time.time + (float)instanceID) / 10f) % 2 == 0)
				{
					vector3 = -vector3;
				}
			}
			else
			{
				float num4 = Mathf.Sign(Vector3.Dot(myTransform.forward, vector3));
				vector3 *= num4;
			}
			Vector3 rhs = Vector3.ProjectOnPlane(vector3, Vector3.up);
			Vector3 vector4 = Vector3.Cross(vector2, rhs).normalized;
			if (vector4.y < 0f)
			{
				vector4 = -vector4;
			}
			rhs = rhs.normalized * 100f + vector4 * Mathf.Lerp(40f, (0f - flightInfo.sweptRadarAltitude) / 5f, Mathf.PerlinNoise(GetInstanceID(), Time.time / 5f));
			autoPilot.targetPosition = myTransform.position + rhs;
		}
		else if (evadeTarget.isFiringPlane)
		{
			int num5 = Mathf.RoundToInt(Mathf.Repeat(evadeTarget.startTime * (float)evadeTarget.actor.GetInstanceID(), 10f));
			if (num5 > 5)
			{
				autoPilot.targetPosition = myTransform.position + 100f * myTransform.forward - 400f * evadeTarget.actor.transform.up;
			}
			else
			{
				int num6 = ((num5 % 2 != 0) ? 1 : (-1));
				autoPilot.targetPosition = myTransform.position + 100f * myTransform.forward + 100f * myTransform.up + num6 * 15 * myTransform.right;
			}
			autoPilot.targetSpeed = maxSpeed;
		}
		else if (evadeTarget.isFiringGroundUnit)
		{
			Vector3 forward = myTransform.forward;
			forward.y = 0f;
			Vector3 vector5 = myTransform.position - evadeTarget.position;
			vector5.y = 0f;
			Vector3 onNormal = Vector3.Cross(Vector3.up, vector5);
			float num7 = Vector3.Angle(forward, vector5);
			Vector3 vector6 = Vector3.Project(forward, onNormal);
			if (num7 > 30f)
			{
				vector6 = -vector6;
			}
			autoPilot.targetPosition = myTransform.position + forward.normalized * 500f + 150f * Vector3.up;
			autoPilot.targetSpeed = maxSpeed;
		}
	}

	private IEnumerator CountermeasureRoutine(bool flares = true, bool chaff = true)
	{
		firingCms = true;
		firingFlares = flares;
		int cmCount = Mathf.RoundToInt(cmDeployCount.Random());
		for (int i = 0; i < cmCount; i++)
		{
			yield return new WaitForSeconds(cmDeployInterval.Random());
			if ((bool)cmm)
			{
				cmm.SetChaff(chaff ? 1 : 0);
				cmm.SetFlare(flares ? 1 : 0);
				cmm.FireSingleCM();
				continue;
			}
			for (int j = 0; j < cms.Length; j++)
			{
				if ((cms[j] is FlareCountermeasure && flares) || (cms[j] is ChaffCountermeasure && chaff))
				{
					cms[j].FireCM();
				}
			}
		}
		yield return new WaitForSeconds(cmDeployCooldown.Random());
		firingCms = false;
		firingFlares = false;
	}

	public void CountermeasureProgram(bool flares, bool chaff, int count, float interval)
	{
		if (isAlive && !flightInfo.isLanded)
		{
			StartCoroutine(CountermeasureProgramRoutine(flares, chaff, count, interval));
		}
	}

	private IEnumerator CountermeasureProgramRoutine(bool flares, bool chaff, int count, float interval)
	{
		WaitForSeconds wait = new WaitForSeconds(interval);
		for (int i = 0; i < count; i++)
		{
			if (!isAlive)
			{
				break;
			}
			if (flightInfo.isLanded)
			{
				break;
			}
			if ((bool)cmm)
			{
				cmm.SetChaff(chaff ? 1 : 0);
				cmm.SetFlare(flares ? 1 : 0);
				cmm.FireSingleCM();
			}
			else
			{
				for (int j = 0; j < cms.Length; j++)
				{
					if ((cms[j] is FlareCountermeasure && flares) || (cms[j] is ChaffCountermeasure && chaff))
					{
						cms[j].FireCM();
					}
				}
			}
			yield return wait;
		}
	}

	public void FireFlares()
	{
		if (!firingCms)
		{
			StartCoroutine(CountermeasureRoutine(flares: true, chaff: false));
		}
	}

	public void FireChaff()
	{
		if (!firingCms)
		{
			StartCoroutine(CountermeasureRoutine(flares: false));
		}
	}

	private IEnumerator ThreatScanRoutine()
	{
		if (randWaits == null)
		{
			randWaits = new WaitForSeconds[5]
			{
				new WaitForSeconds(0.2f),
				new WaitForSeconds(0.41f),
				new WaitForSeconds(0.63f),
				new WaitForSeconds(0.87f),
				new WaitForSeconds(1f)
			};
		}
		int cardinalDir = UnityEngine.Random.Range(0, 4);
		yield return null;
		while (base.enabled)
		{
			while (flightInfo.isLanded)
			{
				yield return null;
			}
			yield return randThreatWait;
			if (commandState != CommandStates.Evade && (bool)rwr && rwr.missileDetected)
			{
				float num = missileEvadeDistance * missileEvadeDistance;
				if ((rwr.nearestThreat.transform.position - myTransform.position).sqrMagnitude < num)
				{
					if (allowEvasiveManeuvers)
					{
						evadeTarget.Reset();
						evadeTarget.actor = rwr.nearestThreat.actor;
						evadeTarget.evadeDuration = 3f;
						evadeTarget.isRadarMissile = true;
						if (commandState != CommandStates.Evade)
						{
							queuedCommand = commandState;
						}
						commandState = CommandStates.Evade;
						PlayRadioMessage(WingmanVoiceProfile.Messages.DefendingMissile);
					}
					else if (!firingCms)
					{
						FireChaff();
					}
					continue;
				}
			}
			cardinalDir = (cardinalDir + 1) % 4;
			Vector3 direction = Quaternion.AngleAxis(cardinalDir * 90, myTransform.up) * myTransform.forward;
			TargetManager.ThreatScanResults threatScanResults = TargetManager.instance.AirThreatScan(actor, direction, 3500f, 0f, moduleRWR);
			if (!threatScanResults.actor)
			{
				continue;
			}
			Vector3 vector = VectorTo(threatScanResults.actor.position);
			if (threatScanResults.isMissile)
			{
				if (commandState != CommandStates.Evade)
				{
					if (Vector3.Dot((rb.velocity - threatScanResults.actor.velocity).normalized, vector.normalized) > 0.99939f)
					{
						evadeTarget.Reset();
						evadeTarget.actor = threatScanResults.actor;
						if (evadeTarget.actor.GetMissile().guidanceMode == Missile.GuidanceModes.Heat)
						{
							evadeTarget.isHeatMissile = true;
						}
						else if (evadeTarget.actor.GetMissile().guidanceMode == Missile.GuidanceModes.Radar)
						{
							evadeTarget.isRadarMissile = true;
						}
						else
						{
							evadeTarget.isUnknownMissile = true;
						}
						if (allowEvasiveManeuvers && commandState != CommandStates.Override)
						{
							if (evadeTarget.isHeatMissile || evadeTarget.isRadarMissile)
							{
								if (commandState != CommandStates.Evade)
								{
									queuedCommand = commandState;
								}
								commandState = CommandStates.Evade;
								PlayRadioMessage(WingmanVoiceProfile.Messages.DefendingMissile);
							}
						}
						else if (evadeTarget.isHeatMissile)
						{
							FireFlares();
						}
						else if (evadeTarget.isRadarMissile)
						{
							FireChaff();
						}
						else if (evadeTarget.isUnknownMissile)
						{
							StartCoroutine(CountermeasureRoutine());
						}
					}
					if (doRadioComms)
					{
						Missile missile = threatScanResults.actor.GetMissile();
						if ((bool)missile && missile.hasTarget && (missile.estTargetPos - FlightSceneManager.instance.playerActor.position).sqrMagnitude < 64f)
						{
							PlayRadioMessage(WingmanVoiceProfile.Messages.MissileOnPlayer);
						}
					}
				}
			}
			else
			{
				if (allowEvasiveManeuvers && threatScanResults.firingGun && Vector3.Dot(vector.normalized, threatScanResults.shootingDirection) < -0.9f)
				{
					evadeTarget.Reset();
					evadeTarget.actor = threatScanResults.actor;
					if (WaterPhysics.GetAltitude(threatScanResults.actor.position) > 100f)
					{
						evadeTarget.isFiringPlane = true;
					}
					else
					{
						evadeTarget.isFiringGroundUnit = true;
					}
					evadeTarget.evadeDuration = 3f;
					if (commandState != CommandStates.Evade)
					{
						queuedCommand = commandState;
					}
					commandState = CommandStates.Evade;
				}
				if (doRadioComms)
				{
					Actor playerActor = FlightSceneManager.instance.playerActor;
					if (threatScanResults.actor.finalCombatRole == Actor.Roles.Air && (playerActor.position - threatScanResults.actor.position).sqrMagnitude < 4000000f && Vector3.Angle(threatScanResults.actor.velocity, playerActor.velocity) < 30f && Vector3.Angle(threatScanResults.actor.position - playerActor.position, playerActor.transform.forward) > 140f)
					{
						PlayRadioMessage(WingmanVoiceProfile.Messages.BanditOnYourSix);
					}
				}
			}
			if ((bool)aiWing && threatScanResults.actor.finalCombatRole != Actor.Roles.Missile)
			{
				aiWing.ReportTarget(actor, threatScanResults.actor, AIWing.DetectionMethods.Visual);
			}
		}
	}

	private void OnAuthorizeLanding(int tfID)
	{
		if (tfID == myTransform.GetInstanceID())
		{
			commandState = CommandStates.Land;
		}
	}

	private IEnumerator LandingRoutine(LandingStates resumeState = LandingStates.None)
	{
		Runway landingRunway = targetRunway;
		commandState = CommandStates.Override;
		if (resumeState == LandingStates.None || resumeState == LandingStates.WaitAuthorization)
		{
			landingState = LandingStates.WaitAuthorization;
			resumeState = LandingStates.None;
			yield return StartCoroutine(Land_WaitAuthorization(landingRunway));
		}
		if (resumeState == LandingStates.None || resumeState == LandingStates.FlyToStarting)
		{
			landingState = LandingStates.FlyToStarting;
			resumeState = LandingStates.None;
			yield return StartCoroutine(Land_FlyToStarting(landingRunway));
		}
		if (resumeState == LandingStates.None || resumeState == LandingStates.FlyToRunway)
		{
			landingState = LandingStates.FlyToRunway;
			resumeState = LandingStates.None;
			yield return StartCoroutine(Land_FlyToRunway(landingRunway));
		}
		actor.SetAutoUnoccupyParking(b: true);
		if ((landingState == LandingStates.Aborting && resumeState == LandingStates.None) || resumeState == LandingStates.Aborting)
		{
			landingState = LandingStates.Aborting;
			resumeState = LandingStates.None;
			yield return StartCoroutine(Land_Aborting(landingRunway));
			yield break;
		}
		if (resumeState == LandingStates.None || resumeState == LandingStates.StoppingOnRunway)
		{
			landingState = LandingStates.StoppingOnRunway;
			resumeState = LandingStates.None;
			yield return StartCoroutine(Land_StoppingOnRunway(landingRunway));
		}
		if ((landingState == LandingStates.Bolter && resumeState == LandingStates.None) || resumeState == LandingStates.Bolter)
		{
			landingState = LandingStates.Bolter;
			resumeState = LandingStates.None;
			yield return StartCoroutine(Land_Bolter(landingRunway));
		}
		else
		{
			landingState = LandingStates.Taxiing;
			resumeState = LandingStates.None;
			yield return StartCoroutine(Land_Taxiing(landingRunway));
		}
		landingState = LandingStates.None;
		landingRoutine = null;
	}

	private IEnumerator Land_WaitAuthorization(Runway landingRunway)
	{
		bool authorized = false;
		bool requested = false;
		while (!authorized)
		{
			orbitTransform = landingRunway.landingQueueOrbitTf;
			defaultAltitude = landingRunway.GetFinalQueueAltitude();
			orbitRadius = landingRunway.landingQueueRadius - 500f;
			commandState = CommandStates.Override;
			FlyOrbit(landingRunway.landingQueueOrbitTf, landingRunway.landingQueueRadius - 500f, 150f, landingRunway.GetFinalQueueAltitude(), clockwise: false);
			if ((orbitTransform.position - myTransform.position).sqrMagnitude > orbitRadius * orbitRadius * 2f)
			{
				autoPilot.targetSpeed = navSpeed;
			}
			else
			{
				autoPilot.targetSpeed = 150f;
				if (!requested)
				{
					requested = true;
					landingRunway.RegisterUsageRequest(actor);
				}
			}
			if (requested && landingRunway.IsRunwayUsageAuthorized(actor))
			{
				authorized = true;
			}
			if ((bool)extLightsCtrlr && VectorTo(orbitTransform.position).sqrMagnitude < 1.5f * (orbitRadius * orbitRadius))
			{
				extLightsCtrlr.SetNavLights(1);
			}
			SafetyOverrides(ignoreOverride: true);
			yield return null;
		}
	}

	private IEnumerator Land_FlyToStarting(Runway landingRunway)
	{
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetNavLights(1);
			extLightsCtrlr.SetStrobeLights(1);
		}
		commandState = CommandStates.Override;
		Vector3 vector = new Vector3(0f, 0f, (0f - landingStartDistance) * Mathf.Clamp(autoPilot.currentSpeed / 150f, 1f, 1.6f));
		vector = Quaternion.AngleAxis(landingGlideSlope, Vector3.right) * vector;
		autoPilot.targetSpeed = 150f;
		FixedPoint worldStartPoint = new FixedPoint(landingRunway.transform.TransformPoint(vector));
		float num = kPlane.GetTurningRadius(150f) * 3f;
		Debug.Log("AIPilot FlyToStarting turnRad = " + num);
		Vector3 vector2 = worldStartPoint.point - landingRunway.transform.forward * 1000f - landingRunway.transform.right * num;
		Vector3 vector3 = worldStartPoint.point - landingRunway.transform.forward * 1000f + landingRunway.transform.right * num;
		float radarAltitudeAtPosition = GetRadarAltitudeAtPosition(vector2);
		float radarAltitudeAtPosition2 = GetRadarAltitudeAtPosition(vector3);
		FixedPoint finalTurnInPt = new FixedPoint((radarAltitudeAtPosition > radarAltitudeAtPosition2) ? vector2 : vector3);
		while (true)
		{
			Vector3 vector4 = myTransform.position - worldStartPoint.point;
			autoPilot.targetPosition = finalTurnInPt.point;
			autoPilot.inputLimiter = 0.7f;
			if (!(Vector3.Dot(vector4.normalized, -landingRunway.transform.forward) > 0f))
			{
				ApplyPlanarTurnAround(0.9f);
				SafetyOverrides(ignoreOverride: true);
				commandState = CommandStates.Override;
				yield return null;
				continue;
			}
			break;
		}
	}

	private IEnumerator Land_FlyToRunway(Runway landingRunway)
	{
		FindRearSuspension();
		landingRunway.ShowLandingLightObjects(actor);
		AICarrierSpawn cSpawn = null;
		if (landingRunway.airport.isCarrier)
		{
			cSpawn = landingRunway.airport.carrierSpawn;
			if ((bool)cSpawn)
			{
				cSpawn.BeginLandingMode(this);
			}
		}
		if ((bool)extLightsCtrlr && !landingRunway.airport.isCarrier && EnvironmentManager.CurrentSunBrightness < 0.8f)
		{
			extLightsCtrlr.SetLandingLights(1);
		}
		float timeClearanceChecked = 0f;
		float clearanceCheckInterval = 1f;
		bool clearanceAbort = false;
		bool touchedDown = false;
		float baseAngle = -1f;
		float startDescentRate = 0f;
		float touchdownAngle = -1f;
		landingHorizPID.updateMode = UpdateModes.Fixed;
		landingVertPID.updateMode = UpdateModes.Fixed;
		while (!touchedDown)
		{
			commandState = CommandStates.Override;
			if ((bool)cSpawn && !cSpawn.actor.alive)
			{
				Debug.LogFormat("{0} was trying to land on a sinking carrier.  Aborting!", actor.DebugName());
				landingState = LandingStates.Aborting;
				break;
			}
			float magnitude = (myTransform.position - landingRunway.transform.position).magnitude;
			float num = ((!rearSusp) ? (myTransform.position - myTransform.up * 2f - landingRunway.transform.position).y : (rearSusp.transform.position + rearSusp.transform.up * (0f - rearSusp.suspensionDistance) - landingRunway.transform.position).y);
			if (num < 8f && !landingRunway.arrestor)
			{
				autoPilot.steerMode = AutoPilot.SteerModes.Aim;
				float angle;
				if (useLandingTouchdownPID)
				{
					if (baseAngle < 0f)
					{
						baseAngle = Vector3.Angle(Vector3.ProjectOnPlane(myTransform.forward, Vector3.up), myTransform.forward);
						_ = flightInfo.verticalSpeed / (0f - num);
						startDescentRate = flightInfo.verticalSpeed;
					}
					float target = Mathf.Lerp(startDescentRate, landingTouchdownSpeed, Mathf.InverseLerp(8f, 0.2f, num));
					angle = baseAngle + landingTouchdownPID.Evaluate(Mathf.Min(0f, flightInfo.verticalSpeed), target);
				}
				else
				{
					angle = landingVertPID.Evaluate(Mathf.Min(0f, flightInfo.verticalSpeed), 0f - num) * 0.333f;
				}
				Vector3 vector = Quaternion.AngleAxis(angle, -landingRunway.transform.right) * landingRunway.transform.forward * 1000f;
				autoPilot.targetPosition = autoPilot.referenceTransform.position + vector;
			}
			else
			{
				autoPilot.steerMode = AutoPilot.SteerModes.Stable;
				autoPilot.targetPosition = landingRunway.GetGuidedLandingPoint(myTransform.position, rb.velocity, landingGlideSlope, landingHorizPID, landingVertPID);
				ApplyPlanarTurnAround(0.9f);
			}
			autoPilot.inputLimiter = 1f;
			float t = Mathf.Clamp01(1f - (magnitude - 2500f) / 6500f);
			autoPilot.targetSpeed = Mathf.Lerp(Mathf.Min(navSpeed, 220f), landingSpeed, t);
			if (num <= 0.5f || flightInfo.wheelsController.landed)
			{
				if (!landingRunway.arrestor && autoPilot.currentSpeed > 50f)
				{
					if (touchdownAngle < 0f)
					{
						touchdownAngle = Vector3.Angle(Vector3.ProjectOnPlane(myTransform.forward, Vector3.up), myTransform.forward);
					}
					autoPilot.steerMode = AutoPilot.SteerModes.Aim;
					autoPilot.targetPosition = myTransform.position + Quaternion.AngleAxis(touchdownAngle, -landingRunway.transform.right) * (landingRunway.transform.forward * 1000f);
					autoPilot.SetOverrideRollTarget(Vector3.up);
					autoPilot.targetSpeed = autoPilot.currentSpeed - 5f;
					if (rb.isKinematic)
					{
						kPlane.SetToDynamic();
						autoPilot.SetFlaps(0f);
					}
					commandState = CommandStates.Override;
					yield return null;
					continue;
				}
				break;
			}
			if (magnitude < 500f || num < 15f)
			{
				if (Time.time - timeClearanceChecked > clearanceCheckInterval)
				{
					timeClearanceChecked = Time.time;
					clearanceAbort = !landingRunway.IsRunwayClear(actor);
				}
				if (autoPilot.currentSpeed > landingSpeed + 15f || clearanceAbort)
				{
					landingState = LandingStates.Aborting;
					break;
				}
			}
			else if ((magnitude < 2000f || autoPilot.currentSpeed < 120f) && gearAnimator.targetState != 0)
			{
				if ((bool)gearAnimator)
				{
					Debug.Log(base.gameObject.name + " deploying gear.");
					gearAnimator.Extend();
				}
				if ((bool)tailHook && landingRunway.arrestor)
				{
					tailHook.ExtendHook();
				}
			}
			if (autoPilot.currentSpeed < 100f)
			{
				autoPilot.SetFlaps(1f);
			}
			else if (autoPilot.currentSpeed < 150f)
			{
				autoPilot.SetFlaps(0.5f);
			}
			yield return fixedWait;
		}
	}

	private IEnumerator Land_Aborting(Runway landingRunway)
	{
		bool retracted = false;
		Vector3 abortVector = Quaternion.AngleAxis(-25f, landingRunway.transform.right) * (landingRunway.transform.forward * 10000f);
		while (flightInfo.radarAltitude < minAltitude + minAltClimbThresh)
		{
			commandState = CommandStates.Override;
			autoPilot.targetSpeed = 200f;
			autoPilot.inputLimiter = 1f;
			if (!retracted && flightInfo.radarAltitude > 100f)
			{
				retracted = true;
				autoPilot.SetFlaps(0f);
				if ((bool)gearAnimator)
				{
					gearAnimator.Retract();
				}
				if ((bool)tailHook)
				{
					tailHook.RetractHook();
				}
			}
			ObstacleCheck();
			if (!avoidingObstacle)
			{
				Vector3 limitedClimbDirectionForSpeed = GetLimitedClimbDirectionForSpeed(abortVector);
				autoPilot.targetPosition = autoPilot.referenceTransform.position + limitedClimbDirectionForSpeed;
			}
			yield return null;
		}
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetLandingLights(0);
		}
		landingRunway.UnregisterUsageRequest(actor);
		landingRunway.HideLightObjects(actor);
		landingRoutine = null;
		if (!retracted)
		{
			autoPilot.SetFlaps(0f);
			if ((bool)gearAnimator)
			{
				gearAnimator.Retract();
			}
			if ((bool)tailHook)
			{
				tailHook.RetractHook();
			}
		}
		landingState = LandingStates.None;
		landingRoutine = null;
		AICarrierSpawn aICarrierSpawn = null;
		if (landingRunway.airport.isCarrier)
		{
			aICarrierSpawn = landingRunway.airport.carrierSpawn;
		}
		if ((bool)aICarrierSpawn && !aICarrierSpawn.actor.alive)
		{
			landingState = LandingStates.None;
			landingRoutine = null;
			ApplyQueuedCommand();
		}
		else
		{
			LandAtAirport(landingRunway.airport);
		}
	}

	private IEnumerator Land_StoppingOnRunway(Runway landingRunway)
	{
		if ((bool)kPlane && rb.isKinematic)
		{
			kPlane.SetToDynamic();
		}
		Vector3 aimDir2 = 100f * myTransform.forward;
		aimDir2 = Vector3.ProjectOnPlane(aimDir2, landingRunway.transform.right);
		while (!flightInfo.isLanded)
		{
			autoPilot.steerMode = AutoPilot.SteerModes.Aim;
			autoPilot.targetPosition = autoPilot.referenceTransform.position + aimDir2;
			autoPilot.inputLimiter = 1f;
			autoPilot.targetSpeed = 0f;
			commandState = CommandStates.Override;
			yield return null;
		}
		commandState = CommandStates.Override;
		if (landingRunway.arrestor)
		{
			commandState = CommandStates.Override;
			bool testingForCatch = true;
			while (testingForCatch)
			{
				yield return null;
				commandState = CommandStates.Override;
				Vector3 targetPosition = autoPilot.referenceTransform.position + 2000f * landingRunway.transform.forward + 20f * Vector3.up;
				SetAutopilotParams(targetPosition, maxSpeed, 1f, allowAB: false);
				if (flightInfo.surfaceSpeed < 10f)
				{
					testingForCatch = false;
					float t = Time.time;
					while (Time.time - t < 2f)
					{
						commandState = CommandStates.Override;
						autoPilot.targetPosition = autoPilot.referenceTransform.position + 2000f * landingRunway.transform.forward;
						autoPilot.targetSpeed = 0f;
						yield return null;
					}
					tailHook.RetractHook();
					taxiSpeed = carrierTaxiSpeed;
				}
				else if (!flightInfo.isLanded)
				{
					testingForCatch = false;
					autoPilot.throttleLimiter = 1f;
				}
			}
			if (!flightInfo.isLanded)
			{
				landingState = LandingStates.Bolter;
				yield break;
			}
		}
		else
		{
			taxiSpeed = defaultTaxiSpeed;
		}
		while (autoPilot.currentSpeed > taxiSpeed)
		{
			autoPilot.steerMode = AutoPilot.SteerModes.Aim;
			autoPilot.targetPosition = autoPilot.referenceTransform.position + aimDir2;
			autoPilot.targetSpeed = taxiSpeed - 1f;
			aimDir2 = Vector3.RotateTowards(aimDir2, landingRunway.transform.forward, 0.12217305f * Time.deltaTime, 0f);
			yield return null;
		}
		landingState = LandingStates.Taxiing;
	}

	private IEnumerator Land_Taxiing(Runway landingRunway)
	{
		AICarrierSpawn cSpawn = null;
		bool isCarrier = landingRunway.airport.isCarrier;
		if (isCarrier)
		{
			cSpawn = (currentCarrier = landingRunway.airport.carrierSpawn);
		}
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetLandingLights(0);
		}
		Transform parkingNodeTf = landingParkingSpace.parkingNode.transform;
		autoPilot.inputLimiter = 1f;
		if (isCarrier)
		{
			taxiPath = landingParkingSpace.parkingNode.carrierReturnPath;
			currentCarrierSpawnIdx = landingRunway.airport.GetCarrierSpawnIdx(parkingNodeTf);
		}
		else if (!isAirbaseNavigating)
		{
			Debug.LogFormat("{0} requesting parking path async.", actor.DebugName());
			float t2 = Time.realtimeSinceStartup;
			AirbaseNavigation.AsyncPathRequest parkingRequest = landingRunway.airport.navigation.GetParkingPathAsync(myTransform.position, myTransform.forward, landingParkingSpace.parkingNode);
			Vector3 aimDir = myTransform.position + myTransform.forward * 100f;
			while (!parkingRequest.done)
			{
				autoPilot.steerMode = AutoPilot.SteerModes.Aim;
				autoPilot.targetPosition = autoPilot.referenceTransform.position + aimDir;
				autoPilot.targetSpeed = taxiSpeed - 1f;
				aimDir = Vector3.RotateTowards(aimDir, landingRunway.transform.forward, 0.12217305f * Time.deltaTime, 0f);
				yield return null;
			}
			Debug.LogFormat("{0} received parking path directions. Request took {1} s.", actor.DebugName(), Time.realtimeSinceStartup - t2);
			StartCoroutine(AirbaseNavTaxiRoutine(parkingRequest.path, null));
		}
		autoPilot.SetFlaps(0f);
		if (isCarrier)
		{
			float t2 = Time.time;
			while (Time.time - t2 < 2f)
			{
				autoPilot.targetSpeed = 0f;
				yield return null;
			}
			if ((bool)wingRotator)
			{
				wingRotator.SetDeployed();
			}
		}
		bool hasUnregisteredUsage = false;
		while (commandState != CommandStates.Park)
		{
			if (!hasUnregisteredUsage && !landingRunway.clearanceBounds.Contains(landingRunway.transform.InverseTransformPoint(myTransform.position)))
			{
				landingRunway.HideLightObjects(actor);
				landingRunway.UnregisterUsageRequest(actor);
				hasUnregisteredUsage = true;
			}
			if (isCarrier)
			{
				TaxiNav(taxiPath, 2f);
			}
			yield return null;
		}
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetAllLights(0);
		}
		if ((bool)cSpawn)
		{
			cSpawn.FinishLanding(this);
		}
		if (ResetAtParking(cSpawn, parkingNodeTf))
		{
			yield break;
		}
		foreach (ModuleEngine engine in autoPilot.engines)
		{
			engine.SetPower(0);
		}
	}

	private IEnumerator Land_Bolter(Runway landingRunway)
	{
		autoPilot.steerMode = AutoPilot.SteerModes.Stable;
		if ((bool)kPlane)
		{
			kPlane.enabled = true;
			kPlane.SetToKinematic();
		}
		commandState = CommandStates.Orbit;
		yield return oneSecWait;
		gearAnimator.Retract();
		tailHook.RetractHook();
		yield return fiveSecWait;
		autoPilot.SetFlaps(0f);
		commandState = CommandStates.Land;
		landingRunway.UnregisterUsageRequest(actor);
		landingRunway.HideLightObjects(actor);
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetLandingLights(0);
		}
	}

	private bool ResetAtParking(AICarrierSpawn cSpawn, Transform parkingNodeTf)
	{
		if ((bool)aiSpawn && parkingNodeTf != null)
		{
			StartCoroutine(ResetAtParkingRoutine(cSpawn, parkingNodeTf));
			return true;
		}
		return false;
	}

	private IEnumerator ResetAtParkingRoutine(AICarrierSpawn cSpawn, Transform parkingNodeTf)
	{
		rearmCarrierSpawn = cSpawn;
		rearmParkingNodeTf = parkingNodeTf;
		rearming = true;
		commandState = CommandStates.Override;
		if ((bool)cSpawn)
		{
			if (!aiSpawn.unitSpawner.spawnFlags.Contains("carrier"))
			{
				aiSpawn.unitSpawner.spawnFlags.Add("carrier");
			}
			currentCarrier = cSpawn;
			foreach (CarrierSpawnPoint spawnPoint in cSpawn.spawnPoints)
			{
				if (spawnPoint.spawnTf == parkingNodeTf)
				{
					aiSpawn.takeOffPath = spawnPoint.catapultPath;
					aiSpawn.catapult = spawnPoint.catapult;
					break;
				}
			}
		}
		while (flightInfo.surfaceSpeed > 0.1f || !landedParentTf)
		{
			autoPilot.targetSpeed = 0f;
			commandState = CommandStates.Override;
			yield return null;
		}
		float num = Vector3.Dot(base.transform.position - parkingNodeTf.position, parkingNodeTf.transform.up);
		Vector3 localPtA = landedParentTf.InverseTransformPoint(myTransform.position);
		Vector3 localPtB = landedParentTf.InverseTransformPoint(parkingNodeTf.position + num * Vector3.up);
		Vector3 up = base.transform.up;
		Vector3 forward = base.transform.forward;
		Quaternion quaternion = Quaternion.FromToRotation(Vector3.ProjectOnPlane(forward, parkingNodeTf.up), parkingNodeTf.forward);
		up = quaternion * up;
		forward = quaternion * forward;
		Vector3 localParkFwd = landedParentTf.InverseTransformDirection(forward);
		Vector3 localParkUp = landedParentTf.InverseTransformDirection(up);
		float t2;
		if (false)
		{
			Vector3 localFwdA = landedParentTf.InverseTransformDirection(myTransform.forward);
			landedParentTf.InverseTransformDirection(myTransform.up);
			t2 = 0f;
			while (t2 < 1f)
			{
				t2 = Mathf.MoveTowards(t2, 1f, 0.3f * Time.deltaTime);
				Vector3 forward2 = landedParentTf.TransformDirection(Vector3.Slerp(localFwdA, localParkFwd, t2));
				Vector3 up2 = Vector3.up;
				Vector3 position = landedParentTf.TransformPoint(Vector3.Lerp(localPtA, localPtB, t2));
				myTransform.position = position;
				myTransform.rotation = Quaternion.LookRotation(forward2, up2);
				CreateLandedJoint(landedParentTf);
				commandState = CommandStates.Override;
				yield return null;
			}
		}
		else
		{
			myTransform.position = landedParentTf.TransformPoint(localPtB);
		}
		Quaternion rotation = Quaternion.LookRotation(landedParentTf.TransformDirection(localParkFwd), landedParentTf.TransformDirection(localParkUp));
		base.transform.rotation = rotation;
		CreateLandedJoint(landedParentTf);
		t2 = Time.time;
		while (Time.time - t2 < 5f)
		{
			commandState = CommandStates.Override;
			yield return null;
		}
		if (rearmAfterLanding)
		{
			if ((bool)wm)
			{
				wm.ClearEquips();
				aiSpawn.EquipLoadout();
			}
			fuelTank.SetNormFuel(1f);
			if (checkForEmptyTanksRoutine != null)
			{
				StopCoroutine(checkForEmptyTanksRoutine);
			}
			yield return null;
			GetComponent<MassUpdater>().UpdateMassObjects();
			_jettisonedTanks = false;
			checkForEmptyTanksRoutine = StartCoroutine(CheckForEmptyTanksRoutine());
			yield return fiveSecWait;
		}
		rearming = false;
		if (rearmAfterLanding || takeOffAfterLanding)
		{
			aiSpawn.TakeOff();
			rearmAfterLanding = false;
			takeOffAfterLanding = false;
			yield break;
		}
		foreach (ModuleEngine engine in autoPilot.engines)
		{
			engine.SetPower(0);
		}
		commandState = CommandStates.Park;
	}

	private void FindRearSuspension()
	{
		if ((bool)rearSusp)
		{
			return;
		}
		if (!rb)
		{
			rb = GetComponent<Rigidbody>();
		}
		if (!myTransform)
		{
			myTransform = base.transform;
		}
		if (!autoPilot || !autoPilot.flightInfo || !flightInfo.wheelsController || flightInfo.wheelsController.suspensions == null || flightInfo.wheelsController.suspensions.Length < 1)
		{
			return;
		}
		RaySpringDamper[] suspensions = flightInfo.wheelsController.suspensions;
		foreach (RaySpringDamper raySpringDamper in suspensions)
		{
			if (Vector3.Dot(raySpringDamper.transform.position - rb.transform.TransformPoint(rb.centerOfMass), myTransform.forward) < 0f)
			{
				rearSusp = raySpringDamper;
				break;
			}
		}
		if (!rearSusp)
		{
			Debug.LogFormat("{0} Landing routine: Could not find rear suspension.", actor.actorName);
		}
	}

	private bool SwitchToAirMissile(Actor tgt)
	{
		if (!wm.availableWeaponTypes.aam)
		{
			return false;
		}
		float magnitude = (tgt.position - myTransform.position).magnitude;
		bool flag = lockingRadar != null;
		int num = -1;
		float num2 = float.MaxValue;
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if (!equip || ((!(equip is HPEquipIRML) || ((HPEquipIRML)equip).GetCount() <= 0) && (!flag || !detectionRadar.radarEnabled || !(equip is HPEquipRadarML) || ((HPEquipRadarML)equip).GetCount() <= 0)))
			{
				continue;
			}
			if ((bool)equip.dlz)
			{
				DynamicLaunchZone.LaunchParams dynamicLaunchParams = equip.dlz.GetDynamicLaunchParams(rb.velocity, tgt.position, tgt.velocity);
				if (magnitude > dynamicLaunchParams.minLaunchRange)
				{
					float rangeTr = dynamicLaunchParams.rangeTr;
					float num3 = Mathf.Abs(magnitude - rangeTr);
					if (num3 < num2)
					{
						num = i;
						num2 = num3;
					}
				}
			}
			else
			{
				Debug.LogError(equip.fullName + " Does not have DLZ!");
			}
		}
		if (num >= 0)
		{
			if (!wm.isMasterArmed)
			{
				wm.ToggleMasterArmed();
			}
			wm.SetWeapon(num);
			BalanceWeaponSelection();
			return true;
		}
		return false;
	}

	private bool SwitchToGun(Actor tgt)
	{
		if (!wm.availableWeaponTypes.gun)
		{
			return false;
		}
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip && equip is HPEquipGun && ((HPEquipGun)equip).gun.currentAmmo > 0 && equip.GetWeaponDamage() >= tgt.healthMinDamage)
			{
				if (!wm.isMasterArmed)
				{
					wm.ToggleMasterArmed();
				}
				wm.SetWeapon(i);
				return true;
			}
		}
		return false;
	}

	private bool SwitchToRocket(Actor tgt)
	{
		if (!wm.availableWeaponTypes.rocket)
		{
			return false;
		}
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip && equip is RocketLauncher && equip.GetCount() > 0 && equip.GetWeaponDamage() >= tgt.healthMinDamage)
			{
				if (!wm.isMasterArmed)
				{
					wm.ToggleMasterArmed();
				}
				wm.SetWeapon(i);
				return true;
			}
		}
		return false;
	}

	private bool SwitchToGroundMissile(Actor tgt, bool requireFAF = false)
	{
		if (!wm.opticalTargeter)
		{
			return false;
		}
		if (!wm.availableWeaponTypes.agm)
		{
			return false;
		}
		float magnitude = (tgt.position - myTransform.position).magnitude;
		int num = -1;
		float num2 = float.MaxValue;
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if (!equip || !(equip is HPEquipOpticalML))
			{
				continue;
			}
			HPEquipOpticalML hPEquipOpticalML = (HPEquipOpticalML)equip;
			if ((!requireFAF || hPEquipOpticalML.ml.missilePrefab.GetComponent<Missile>().opticalFAF) && hPEquipOpticalML.ml.missileCount > 0 && equip.GetWeaponDamage() >= tgt.healthMinDamage)
			{
				float rangeTr = equip.dlz.GetDynamicLaunchParams(rb.velocity, tgt.position, tgt.velocity).rangeTr;
				float num3 = Mathf.Abs(magnitude - rangeTr);
				if (num3 < num2)
				{
					num = i;
					num2 = num3;
				}
			}
		}
		if (num >= 0)
		{
			if (!wm.isMasterArmed)
			{
				wm.ToggleMasterArmed();
			}
			wm.SetWeapon(num);
			BalanceWeaponSelection();
			return true;
		}
		return false;
	}

	private bool SwitchToAntiRadMissile(Actor tgt)
	{
		if (!moduleRWR)
		{
			return false;
		}
		if (!wm.availableWeaponTypes.antirad)
		{
			return false;
		}
		if (tgt.finalCombatRole == Actor.Roles.Air)
		{
			return false;
		}
		List<Radar> list = null;
		if (!tgt.hasRadar)
		{
			bool flag = false;
			if ((bool)tgt.parentActor)
			{
				Actor actor = tgt;
				while ((bool)actor.parentActor && !flag)
				{
					if (actor.parentActor.hasRadar)
					{
						flag = true;
						list = actor.parentActor.GetRadars();
					}
					else
					{
						actor = actor.parentActor;
					}
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		else
		{
			list = tgt.GetRadars();
		}
		bool flag2 = false;
		for (int i = 0; i < list.Count; i++)
		{
			if (flag2)
			{
				break;
			}
			Radar radar = list[i];
			if ((bool)radar && radar.radarEnabled)
			{
				if (radar.rotationRange > 359f)
				{
					flag2 = true;
				}
				else if (Vector3.Angle(myTransform.position - radar.rotationTransform.position, radar.rotationTransform.parent.forward) < radar.rotationRange / 2f)
				{
					flag2 = true;
				}
			}
		}
		if (!flag2)
		{
			return false;
		}
		float magnitude = (tgt.position - myTransform.position).magnitude;
		int num = -1;
		float num2 = float.MaxValue;
		for (int j = 0; j < wm.equipCount; j++)
		{
			HPEquippable equip = wm.GetEquip(j);
			if ((bool)equip && equip is HPEquipARML && ((HPEquipARML)equip).ml.missileCount > 0 && equip.GetWeaponDamage() >= tgt.healthMinDamage)
			{
				float rangeTr = equip.dlz.GetDynamicLaunchParams(rb.velocity, tgt.position, tgt.velocity).rangeTr;
				float num3 = Mathf.Abs(magnitude - rangeTr);
				if (num3 < num2)
				{
					num = j;
					num2 = num3;
				}
			}
		}
		if (num >= 0)
		{
			if (!wm.isMasterArmed)
			{
				wm.ToggleMasterArmed();
			}
			wm.SetWeapon(num);
			BalanceWeaponSelection();
			return true;
		}
		return false;
	}

	private bool SwitchToBomb(Actor tgt)
	{
		if (!wm.availableWeaponTypes.bomb)
		{
			return false;
		}
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip && equip is HPEquipBombRack && ((HPEquipBombRack)equip).ml.missileCount > 0 && equip.GetWeaponDamage() >= tgt.healthMinDamage)
			{
				if (!wm.isMasterArmed)
				{
					wm.ToggleMasterArmed();
				}
				wm.SetWeapon(i);
				BalanceWeaponSelection();
				return true;
			}
		}
		return false;
	}

	private bool SwitchToBomb()
	{
		if (!wm.availableWeaponTypes.bomb)
		{
			return false;
		}
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip && equip is HPEquipBombRack && ((HPEquipBombRack)equip).ml.missileCount > 0)
			{
				if (!wm.isMasterArmed)
				{
					wm.ToggleMasterArmed();
				}
				wm.SetWeapon(i);
				return true;
			}
		}
		return false;
	}

	private bool SwitchToLaserBomb(Actor tgt)
	{
		if (!wm.availableWeaponTypes.bomb)
		{
			return false;
		}
		if (!wm.opticalTargeter)
		{
			return false;
		}
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip && equip is HPEquipLaserBombRack && equip.GetCount() > 0 && equip.GetWeaponDamage() >= tgt.healthMinDamage)
			{
				if (!wm.isMasterArmed)
				{
					wm.ToggleMasterArmed();
				}
				wm.SetWeapon(i);
				BalanceWeaponSelection();
				return true;
			}
		}
		return false;
	}

	private bool SwitchToGPSBomb(Actor tgt)
	{
		if (!wm.availableWeaponTypes.bomb)
		{
			return false;
		}
		if (tgt.velocity.sqrMagnitude > 1f)
		{
			return false;
		}
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip && equip is HPEquipGPSBombRack && ((HPEquipGPSBombRack)equip).ml.missileCount > 0 && equip.GetWeaponDamage() >= tgt.healthMinDamage)
			{
				if (!wm.isMasterArmed)
				{
					wm.ToggleMasterArmed();
				}
				wm.SetWeapon(i);
				BalanceWeaponSelection();
				return true;
			}
		}
		return false;
	}

	private bool SwitchToShipMissile(Actor tgt)
	{
		if (!wm.availableWeaponTypes.antiShip)
		{
			return false;
		}
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip && equip is HPEquipASML && equip.GetCount() > 0 && equip.GetWeaponDamage() >= tgt.healthMinDamage)
			{
				if (!wm.isMasterArmed)
				{
					wm.ToggleMasterArmed();
				}
				wm.SetWeapon(i);
				BalanceWeaponSelection();
				return true;
			}
		}
		return false;
	}

	private IEnumerator DecideCombatAction(Actor target)
	{
		float num = Vector3.Distance(target.position, myTransform.position);
		autoPilot.inputLimiter = 1f;
		if (target.finalCombatRole == Actor.Roles.Air && target.velocity.sqrMagnitude > 900f)
		{
			if (SwitchToAirMissile(target))
			{
				yield return StartCombatSubroutine(CombatAirMissileRoutine(target));
			}
			else if (SwitchToGun(target))
			{
				if (num < gunAirMaxRange)
				{
					yield return StartCombatSubroutine(CombatGunAirRoutine(target));
					yield break;
				}
				if (wm.availableWeaponTypes.aam)
				{
					yield return StartCombatSubroutine(CombatExtendRoutine(-VectorTo(target.position), 10f));
					yield break;
				}
				yield return StartCombatSubroutine(CombatFlyToTargetRoutine(target, gunAirMaxRange, maxSpeed));
				yield return StartCombatSubroutine(CombatGunAirRoutine(target));
			}
			else
			{
				Vector3 direction = Vector3.RotateTowards(-rb.velocity, -target.velocity, (float)Math.PI / 2f, 0f);
				yield return StartCombatSubroutine(CombatExtendRoutine(direction, 4f));
			}
		}
		else if (target.finalCombatRole == Actor.Roles.Ship)
		{
			if (SwitchToShipMissile(target))
			{
				yield return StartCombatSubroutine(CombatAntiShipMissileRoutine(target));
			}
			else if (SwitchToAntiRadMissile(target))
			{
				yield return StartCombatSubroutine(CombatAntiRadMissileRoutine(target));
			}
			else if (SwitchToGPSBomb(target))
			{
				yield return StartCombatSubroutine(CombatGPSBombRoutine(target));
			}
			else if (SwitchToLaserBomb(target))
			{
				yield return StartCombatSubroutine(CombatLaserBombRoutine(target));
			}
			else if (SwitchToBomb(target))
			{
				if (combatRole == CombatRoles.Bomber)
				{
					float heading = UnityEngine.Random.Range(0f, 360f);
					HPEquipBombRack hPEquipBombRack = (HPEquipBombRack)wm.currentEquip;
					float num2 = EstimateBombRunDist(flightInfo.airspeed, myTransform.position.y - target.position.y, 5f);
					if (Vector3.ProjectOnPlane(target.position - myTransform.position, Vector3.up).sqrMagnitude > num2 * num2)
					{
						heading = VectorUtils.Bearing(myTransform.position, target.position);
					}
					Missile component = hPEquipBombRack.ml.missilePrefab.GetComponent<Missile>();
					float num3 = component.explodeRadius * 0.65f;
					float explodeDamage = component.explodeDamage;
					float num4 = explodeDamage * 3f;
					if ((bool)target.health)
					{
						num4 = target.health.currentHealth;
					}
					float radius = (float)Mathf.CeilToInt(num4 / explodeDamage) * num3;
					yield return StartCombatSubroutine(CarpetBombActorRoutine(hPEquipBombRack, target, heading, num3, radius, defaultAltitude, combat: true));
				}
				else
				{
					yield return StartCombatSubroutine(CombatBombRoutine(target));
				}
			}
			else
			{
				if (!(overrideAttackTarget != null) || !(target == overrideAttackTarget) || !target.unitSpawn || !(target.unitSpawn is AIUnitSpawn))
				{
					yield break;
				}
				AIUnitSpawn aIUnitSpawn = (AIUnitSpawn)target.unitSpawn;
				if (aIUnitSpawn.subUnits == null)
				{
					yield break;
				}
				foreach (Actor subUnit in aIUnitSpawn.subUnits)
				{
					if (subUnit.alive)
					{
						attackTarget = (overrideAttackTarget = subUnit);
						break;
					}
				}
			}
		}
		else if (SwitchToAntiRadMissile(target))
		{
			yield return StartCombatSubroutine(CombatAntiRadMissileRoutine(target));
		}
		else if (SwitchToGroundMissile(target))
		{
			yield return StartCombatSubroutine(CombatGroundMissileRoutine(target));
		}
		else if (SwitchToRocket(target))
		{
			yield return StartCombatSubroutine(CombatRocketAttackRoutine(target));
		}
		else if (SwitchToGPSBomb(target))
		{
			yield return StartCombatSubroutine(CombatGPSBombRoutine(target));
		}
		else if (SwitchToLaserBomb(target))
		{
			yield return StartCombatSubroutine(CombatLaserBombRoutine(target));
		}
		else if (SwitchToBomb(target))
		{
			if (combatRole == CombatRoles.Bomber)
			{
				float heading2 = UnityEngine.Random.Range(0f, 360f);
				HPEquipBombRack hPEquipBombRack2 = (HPEquipBombRack)wm.currentEquip;
				float num5 = EstimateBombRunDist(flightInfo.airspeed, myTransform.position.y - target.position.y, 5f);
				if (Vector3.ProjectOnPlane(target.position - myTransform.position, Vector3.up).sqrMagnitude > num5 * num5)
				{
					heading2 = VectorUtils.Bearing(myTransform.position, target.position);
				}
				float distInterval = hPEquipBombRack2.ml.missilePrefab.GetComponent<Missile>().explodeRadius * 0.65f;
				yield return StartCombatSubroutine(CarpetBombActorRoutine(hPEquipBombRack2, target, heading2, distInterval, 60f, defaultAltitude, combat: true));
			}
			else
			{
				yield return StartCombatSubroutine(CombatBombRoutine(target));
			}
		}
		else if (SwitchToGun(target))
		{
			yield return StartCombatSubroutine(CombatGunGroundAttackRoutine(target));
		}
	}

	private Vector3 VectorToTargetWithAngleLimit(Vector3 tgtPos, Vector3 angleAnchor, float angleDegrees)
	{
		Vector3 target = VectorTo(tgtPos);
		return Vector3.RotateTowards(VectorTo(angleAnchor), target, angleDegrees * ((float)Math.PI / 180f), 0f);
	}

	public void CommandAntiShipOnPath(FollowPath asmPath, AntiShipGuidance.ASMTerminalBehaviors tMode)
	{
		if (NotReadyForFlightCommand())
		{
			Debug.LogError(actor.DebugName() + " was commanded to anti-ship but it was busy!");
		}
		else if (wm.availableWeaponTypes.antiShip)
		{
			StopCombat();
			for (int i = 0; i < wm.equipCount; i++)
			{
				HPEquippable equip = wm.GetEquip(i);
				if ((bool)equip && equip is HPEquipASML && equip.GetCount() > 0)
				{
					if (!wm.isMasterArmed)
					{
						wm.ToggleMasterArmed();
					}
					wm.SetWeapon(i);
					BalanceWeaponSelection();
					commandState = CommandStates.Override;
					StartCoroutine(AntiShipCommandRoutine(asmPath, tMode));
					break;
				}
			}
		}
		else
		{
			Debug.LogError(actor.DebugName() + " was commanded to fire ASM on a path but it does not have ASM");
		}
	}

	private IEnumerator AntiShipCommandRoutine(FollowPath asmPath, AntiShipGuidance.ASMTerminalBehaviors tMode)
	{
		commandedASMPath = asmPath;
		commandedASMMode = tMode;
		int asmIdx = wm.currentEquip.hardpointIdx;
		wm.SetMasterArmed(armed: false);
		float currT;
		FixedPoint attackPt = new FixedPoint(asmPath.GetFollowPoint(myTransform.position, 1000f, out currT));
		float magnitude = (attackPt.point - myTransform.position).magnitude;
		if (magnitude < 3000f || (magnitude < 4000f && Vector3.Dot(myTransform.forward, attackPt.point - myTransform.position) < 0f))
		{
			Vector3 flyDir = asmPath.GetWorldTangent(currT);
			flyDir.y = 0f;
			flyDir.Normalize();
			while ((attackPt.point - myTransform.position).sqrMagnitude < 17640000f)
			{
				SetAutopilotParams(myTransform.position + flyDir * 1000f, Mathf.Lerp(navSpeed, maxSpeed, 0.5f), 1f, allowAB: true);
				SafetyOverrides();
				yield return null;
			}
		}
		string currentGroup = "AIA 1";
		if (!wm.gpsSystem.SetCurrentGroup(currentGroup))
		{
			wm.gpsSystem.CreateGroup("AIA", 1);
			wm.gpsSystem.SetCurrentGroup(currentGroup);
		}
		wm.gpsSystem.currentGroup.RemoveAllTargets();
		float num = 1500f / asmPath.GetApproximateLength();
		for (float num2 = currT + num; num2 < 1f - num / 2f; num2 += num)
		{
			Vector3 worldPoint = asmPath.GetWorldPoint(num2);
			wm.gpsSystem.currentGroup.AddTarget(new GPSTarget(worldPoint, "tgt", 0));
		}
		wm.gpsSystem.currentGroup.AddTarget(new GPSTarget(asmPath.GetWorldPoint(1f), "tgt", 0));
		wm.gpsSystem.currentGroup.isPath = true;
		wm.gpsSystem.currentGroup.currentTargetIdx = 0;
		wm.gpsSystem.TargetsChanged();
		wm.SetMasterArmed(armed: true);
		if ((bool)wm.currentEquip && !(wm.currentEquip is HPEquipASML))
		{
			Debug.Log("AIPilot ready to shoot ASM set master arm ON but currentEquip changed.  Switching back to ASML");
		}
		wm.SetWeapon(asmIdx);
		((HPEquipASML)wm.currentEquip).SetTerminalMode(tMode);
		while (Vector3.Dot((attackPt.point - myTransform.position).normalized, myTransform.forward) < 0.995f || !wm.IsLaunchAuthorized() || Mathf.Abs(flightInfo.roll) > 5f || flightInfo.playerGs > 2f || (double)flightInfo.playerGs < 0.5)
		{
			SetAutopilotParams(attackPt.point, navSpeed, 1f, allowAB: true);
			ApplyPlanarTurnAround();
			SafetyOverrides();
			yield return null;
		}
		wm.SingleFire();
		BeginDeploySafety();
		PlayRadioMessage(WingmanVoiceProfile.Messages.Bruiser, 4f);
		commandedASMPath = null;
		ApplyQueuedCommand();
	}

	private IEnumerator CombatAntiShipMissileRoutine(Actor target)
	{
		Debug.Log(actor.actorName + " beginning ASM routine.");
		if (wm.currentEquip == null || !(wm.currentEquip is HPEquipASML))
		{
			yield break;
		}
		float estimatedRange = ((HPEquipASML)wm.currentEquip).ml.missilePrefab.GetComponent<AntiShipGuidance>().estimatedRange;
		float offBoresight = ((HPEquipASML)wm.currentEquip).offBoresightLaunchAngle;
		wm.SetMasterArmed(armed: false);
		string currentGroup = "AIA 2";
		if (!wm.gpsSystem.SetCurrentGroup(currentGroup))
		{
			wm.gpsSystem.CreateGroup("AIA", 2);
			wm.gpsSystem.SetCurrentGroup(currentGroup);
		}
		wm.gpsSystem.currentGroup.RemoveAllTargets();
		if (VectorTo(target.position).magnitude < 5000f)
		{
			float distance = Mathf.Min(8000f, estimatedRange);
			float altitude = 3000f;
			yield return StartCombatSubroutine(CombatSetupSurfaceAttack(target, distance, altitude));
		}
		if (!ValidateTarget(target))
		{
			yield break;
		}
		Vector3 vector = VectorTo(target.position);
		float magnitude = vector.magnitude;
		Vector3 vector2 = target.position;
		int num = 0;
		bool flag = true;
		while (flag)
		{
			Vector3 vector3 = Quaternion.AngleAxis(UnityEngine.Random.Range(-30f, 30f), Vector3.up) * vector;
			vector2 = myTransform.position + magnitude * 0.3f * vector3.normalized;
			float num2 = UnityEngine.Random.Range(0f, 3000f);
			vector2.y = WaterPhysics.instance.height + num2;
			if (num < 4 && Physics.Linecast(myTransform.position, vector2, 1))
			{
				flag = true;
				num++;
			}
			else
			{
				flag = false;
			}
		}
		FixedPoint f_wpt1 = new FixedPoint(vector2);
		wm.gpsSystem.currentGroup.isPath = true;
		wm.gpsSystem.currentGroup.currentTargetIdx = 0;
		Vector3 vector4 = VectorToTargetWithAngleLimit(PositionAtAltitudeASL(myTransform.position + myTransform.forward * 1000f, flightInfo.altitudeASL), f_wpt1.point, offBoresight * 0.85f);
		while (ValidateTarget(target) && Vector3.Angle(rb.velocity, vector4) > 5f)
		{
			autoPilot.targetPosition = myTransform.position + vector4;
			autoPilot.targetSpeed = maxSpeed;
			SafetyOverrides();
			yield return null;
			vector4 = VectorToTargetWithAngleLimit(PositionAtAltitudeASL(myTransform.position + myTransform.forward * 1000f, flightInfo.altitudeASL), f_wpt1.point, offBoresight * 0.55f);
		}
		FixedPoint tgtPos = new FixedPoint(myTransform.position + vector4.normalized * 10000f);
		wm.SetMasterArmed(armed: true);
		float t2 = Time.time;
		while (ValidateTarget(target) && Time.time - t2 < 2f)
		{
			autoPilot.targetPosition = tgtPos.point;
			autoPilot.targetSpeed = maxSpeed;
			SafetyOverrides();
			yield return null;
		}
		if (!ValidateTarget(target) || ((wm.currentEquip == null || !(wm.currentEquip is HPEquipASML)) && !SwitchToShipMissile(target)))
		{
			yield break;
		}
		HPEquipASML asml = (HPEquipASML)wm.currentEquip;
		wm.gpsSystem.AddTarget(myTransform.position + myTransform.forward * 500f, "AI");
		wm.gpsSystem.AddTarget(f_wpt1.point, "AI");
		wm.gpsSystem.AddTarget(target.position, "AI");
		wm.gpsSystem.currentGroup.currentTargetIdx = 0;
		wm.gpsSystem.currentGroup.isPath = true;
		wm.gpsSystem.TargetsChanged();
		yield return null;
		if (asml.LaunchAuthorized())
		{
			asml.SetTerminalMode(UnityEngine.Random.Range(0, 3) switch
			{
				0 => AntiShipGuidance.ASMTerminalBehaviors.SeaSkim, 
				1 => AntiShipGuidance.ASMTerminalBehaviors.Popup, 
				_ => AntiShipGuidance.ASMTerminalBehaviors.SSEvasive, 
			});
			wm.SingleFire();
			BeginDeploySafety();
			yield return null;
			Missile firedMissile = wm.lastFiredMissile;
			if ((bool)firedMissile)
			{
				Debug.Log(actor.actorName + " fired ASM.");
				PlayRadioMessage(WingmanVoiceProfile.Messages.Bruiser, 3f);
				if ((bool)aiWing)
				{
					aiWing.ReportMissileOnTarget(actor, target, firedMissile);
				}
				if ((bool)target)
				{
					float orbitRad = 3000f;
					Vector3 vector5 = VectorTo(target.position);
					FixedPoint awayPoint = new FixedPoint(PositionAtAltitudeRadar(target.position - vector5 - vector5.normalized * orbitRad, defaultAltitude));
					yield return null;
					t2 = Time.time;
					while (Time.time - t2 < 2f)
					{
						autoPilot.targetPosition = tgtPos.point;
						autoPilot.targetSpeed = maxSpeed;
						SafetyOverrides();
						yield return null;
					}
					wm.SetMasterArmed(armed: false);
					while (ValidateTarget(target) && (bool)firedMissile)
					{
						FlyOrbit(awayPoint.point, orbitRad, navSpeed, defaultAltitude, orbitClockwise);
						SafetyOverrides();
						yield return null;
					}
				}
				else
				{
					wm.SetMasterArmed(armed: false);
				}
			}
			else
			{
				wm.SetMasterArmed(armed: false);
			}
		}
		else
		{
			Debug.Log(actor.actorName + " ASML was not launch authorized.");
		}
	}

	private IEnumerator CombatExtendRoutine(Vector3 direction, float time, bool planarTurnaround = false)
	{
		float startTime = Time.time;
		direction = direction.normalized;
		while (true)
		{
			autoPilot.steerMode = AutoPilot.SteerModes.Stable;
			autoPilot.targetPosition = myTransform.position + direction * 1000f;
			autoPilot.targetSpeed = maxSpeed;
			if (planarTurnaround)
			{
				ApplyPlanarTurnAround();
			}
			SafetyOverrides();
			if (Time.time - startTime > time)
			{
				break;
			}
			yield return null;
		}
	}

	private IEnumerator CombatFlyToTargetRoutine(Actor target, float radius, float speed)
	{
		float sqrRad = radius * radius;
		float sqrMagnitude;
		while (ValidateTarget(target) && (sqrMagnitude = (target.position - myTransform.position).sqrMagnitude) > sqrRad)
		{
			autoPilot.steerMode = AutoPilot.SteerModes.Stable;
			float num = 0f;
			float num2 = Vector3.Dot(rb.velocity - target.velocity, (target.position - myTransform.position).normalized);
			if (num2 > 0f)
			{
				num = Mathf.Sqrt(sqrMagnitude) / num2;
				num *= 0.5f;
				num = Mathf.Min(num, 3f);
			}
			Vector3 vector = target.position + target.velocity * num;
			if (sqrMagnitude > 100000000f)
			{
				vector = PositionAtAltitudeRadar(vector, defaultAltitude);
			}
			autoPilot.targetPosition = vector;
			autoPilot.targetSpeed = speed;
			SafetyOverrides();
			yield return null;
		}
	}

	private IEnumerator CombatAirMissileRoutine(Actor target)
	{
		HPEquippable eq = wm.currentEquip;
		int eqIdx = eq.hardpointIdx;
		bool isRadar = eq is HPEquipRadarML;
		InternalWeaponBay iwb = GetExternallyControlledIWB(eq);
		if (!iwb && wm.isMasterArmed)
		{
			wm.ToggleMasterArmed();
		}
		autoPilot.steerMode = AutoPilot.SteerModes.Stable;
		float breakRadius3;
		float breakAltitude4;
		float currTime5;
		while (ValidateTarget(target) && (bool)wm.lastFiredMissile)
		{
			if ((bool)wm.lastFiredMissile.heatSeeker)
			{
				if (Time.time - wm.lastFiredMissile.timeFired > 3f && (double)wm.lastFiredMissile.heatSeeker.seekerLock < 0.5)
				{
					break;
				}
			}
			else if (!wm.lastFiredMissile.hasTarget)
			{
				break;
			}
			if (isRadar)
			{
				float breakTime = UnityEngine.Random.Range(3f, 5f);
				bool breakDir4 = UnityEngine.Random.Range(-1f, 1f) > 0f;
				breakRadius3 = VectorTo(target.position).magnitude;
				breakAltitude4 = WaterPhysics.GetAltitude(myTransform.position);
				currTime5 = Time.time;
				autoPilot.inputLimiter = 0.45f;
				while (ValidateTarget(target) && Time.time - currTime5 < breakTime)
				{
					FlyOrbit(target.transform, breakRadius3, maxSpeed, breakAltitude4, breakDir4);
					SafetyOverrides();
					yield return null;
				}
				autoPilot.inputLimiter = 1f;
			}
			else
			{
				autoPilot.targetPosition = target.position - target.velocity * 4f;
				autoPilot.targetSpeed = target.velocity.magnitude - (1000f - VectorTo(target.position).magnitude) / 50f;
				SafetyOverrides();
			}
			yield return null;
		}
		Missile firedMissile3;
		if (isRadar)
		{
			while (ValidateTarget(target) && Vector3.Angle(base.transform.forward, VectorTo(target.position)) > lockingRadar.fov * 0.4f)
			{
				autoPilot.inputLimiter = TurnToMissileTargetInputLimit(target.position);
				autoPilot.targetPosition = target.position;
				autoPilot.targetSpeed = maxSpeed;
				SafetyOverrides();
				yield return null;
				if (!target || VectorTo(target.position).sqrMagnitude < gunAirMaxRange * gunAirMaxRange || !detectionRadar.radarEnabled)
				{
					yield break;
				}
			}
			lockingRadar.Unlock();
			RadarLockData lockData = null;
			while (ValidateTarget(target) && !lockingRadar.GetLock(target, out lockData))
			{
				if (!detectionRadar.radarEnabled)
				{
					yield break;
				}
				autoPilot.inputLimiter = TurnToMissileTargetInputLimit(target.position);
				autoPilot.targetPosition = target.position;
				autoPilot.targetSpeed = maxSpeed;
				SafetyOverrides();
				lockingRadar.Unlock();
				yield return null;
				if (!target || VectorTo(target.position).sqrMagnitude < gunAirMaxRange * gunAirMaxRange)
				{
					yield break;
				}
			}
			if (lockData == null || !lockData.locked)
			{
				yield break;
			}
			autoPilot.inputLimiter = 1f;
			if (!wm.isMasterArmed)
			{
				wm.ToggleMasterArmed();
				wm.SetWeapon(eqIdx);
			}
			if ((bool)iwb)
			{
				iwb.RegisterOpenReq(this);
			}
			currTime5 = 0f;
			breakAltitude4 = UnityEngine.Random.Range(1f, 2f);
			while (ValidateTarget(target) && Time.time - currTime5 < breakAltitude4)
			{
				Vector3 targetPosition = target.position + target.velocity * 3f;
				autoPilot.targetPosition = targetPosition;
				autoPilot.targetSpeed = maxSpeed;
				if (!detectionRadar.radarEnabled)
				{
					yield break;
				}
				SafetyOverrides();
				yield return null;
			}
			bool breakDir4 = false;
			breakRadius3 = 0f;
			while (ValidateTarget(target))
			{
				float magnitude = VectorTo(target.position).magnitude;
				DynamicLaunchZone.LaunchParams dynamicLaunchParams = eq.dlz.GetDynamicLaunchParams(rb.velocity, target.position, target.velocity);
				Vector3 vector = target.position + target.velocity * 3f;
				autoPilot.targetPosition = vector;
				autoPilot.targetSpeed = maxSpeed;
				autoPilot.inputLimiter = TurnToMissileTargetInputLimit(target.position);
				if (!detectionRadar.radarEnabled)
				{
					yield break;
				}
				if (magnitude < dynamicLaunchParams.minLaunchRange)
				{
					lockingRadar.Unlock();
					yield break;
				}
				if (magnitude < dynamicLaunchParams.maxLaunchRange)
				{
					if (Vector3.Angle(rb.velocity, VectorTo(vector)) < 10f)
					{
						breakDir4 = true;
						break;
					}
				}
				else if (breakRadius3 > 6f)
				{
					lockingRadar.Unlock();
					yield break;
				}
				SafetyOverrides();
				breakRadius3 += Time.deltaTime;
				yield return null;
			}
			if (ValidateTarget(target) && breakDir4 && (bool)aiWing && aiWing.GetNumMissilesOnTarget(target) >= aiWing.maxMissilePerTarget)
			{
				breakDir4 = false;
				float breakTime = Time.time;
				float waitTime2 = UnityEngine.Random.Range(1f, 2f);
				float targetDist3 = VectorTo(target.position).magnitude;
				while (ValidateTarget(target) && Time.time - breakTime < waitTime2)
				{
					autoPilot.inputLimiter = 0.4f;
					FlyOrbit(target.transform, targetDist3, maxSpeed, defaultAltitude, orbitClockwise);
					SafetyOverrides();
					yield return null;
					if (!detectionRadar.radarEnabled)
					{
						yield break;
					}
				}
				autoPilot.inputLimiter = 1f;
			}
			if (ValidateTarget(target) && breakDir4)
			{
				wm.SingleFire();
				yield return null;
				firedMissile3 = wm.lastFiredMissile;
				PlayRadioMessage(WingmanVoiceProfile.Messages.Fox3, 4f);
				StartCoroutine(CallMissileResultRoutine(target, firedMissile3, WingmanVoiceProfile.Messages.Splash, WingmanVoiceProfile.Messages.AirMiss));
				if ((bool)firedMissile3 && firedMissile3.fired)
				{
					if ((bool)aiWing)
					{
						aiWing.ReportMissileOnTarget(this.actor, target, firedMissile3);
					}
					if (firedMissile3.thrustDelay > 0.1f)
					{
						BeginDeploySafety();
					}
				}
				float targetDist3 = Time.time;
				while (ValidateTarget(target) && Time.time - targetDist3 < 1f)
				{
					autoPilot.inputLimiter = 0.45f;
					autoPilot.targetPosition = target.position;
					SafetyOverrides();
					yield return null;
				}
				if ((bool)iwb)
				{
					iwb.UnregisterOpenReq(this);
				}
				if (wm.isMasterArmed)
				{
					wm.ToggleMasterArmed();
				}
				if ((bool)firedMissile3 && firedMissile3.fired && !firedMissile3.isPitbull)
				{
					FireChaff();
					int breakDir2 = (int)Mathf.Sign(UnityEngine.Random.Range(-1f, 1f));
					while (ValidateTarget(target) && (bool)firedMissile3 && firedMissile3.hasTarget && !firedMissile3.isPitbull)
					{
						Vector3 vector2 = VectorTo(target.position);
						vector2.y = 0f;
						vector2 = Quaternion.AngleAxis(lockingRadar.fov * 0.4f * (float)breakDir2, Vector3.up) * vector2;
						autoPilot.targetPosition = base.transform.position + vector2;
						autoPilot.targetSpeed = maxSpeed;
						autoPilot.inputLimiter = 0.7f;
						SafetyOverrides();
						yield return null;
					}
					lockingRadar.Unlock();
					bool flag = true;
					if ((ValidateTarget(target) && Vector3.Dot(target.velocity, rb.velocity) > 0f) || UnityEngine.Random.Range(0f, 100f) < 25f)
					{
						flag = false;
					}
					if (flag)
					{
						if (!firingCms)
						{
							StartCoroutine(CountermeasureRoutine(flares: true, !rwr || rwr.missileDetected));
						}
						float waitTime2 = UnityEngine.Random.Range(3f, 5f);
						bool breakDir = UnityEngine.Random.Range(-1f, 1f) > 0f;
						float breakTime = (target ? VectorTo(target.position).magnitude : 8000f);
						float breakAltitude2 = WaterPhysics.GetAltitude(myTransform.position);
						float currTime3 = Time.time;
						autoPilot.inputLimiter = 0.45f;
						while (ValidateTarget(target) && Time.time - currTime3 < waitTime2)
						{
							FlyOrbit(target.transform, breakTime, maxSpeed, breakAltitude2, breakDir);
							SafetyOverrides();
							yield return null;
						}
						autoPilot.inputLimiter = 1f;
					}
				}
				else
				{
					lockingRadar.Unlock();
					autoPilot.inputLimiter = 1f;
				}
			}
			else if ((bool)iwb)
			{
				iwb.UnregisterOpenReq(this);
			}
			yield break;
		}
		if (!wm.isMasterArmed)
		{
			wm.ToggleMasterArmed();
			wm.SetWeapon(eqIdx);
		}
		if ((bool)iwb)
		{
			iwb.RegisterOpenReq(this);
		}
		IRMissileLauncher irml = ((HPEquipIRML)eq).irml;
		firedMissile3 = irml.activeMissile;
		firedMissile3.heatSeeker.headTransform = headLookTransform;
		firedMissile3.heatSeeker.SetSeekerMode(HeatSeeker.SeekerModes.HeadTrack);
		headLookTransform.localRotation = Quaternion.identity;
		breakRadius3 = Time.time;
		breakAltitude4 = UnityEngine.Random.Range(8f, 15f);
		if (!ValidateTarget(target))
		{
			yield break;
		}
		Vector3 position = target.position + target.velocity * 2f;
		Vector3 vectorToTarget = VectorTo(position);
		while (ValidateTarget(target) && (firedMissile3.heatSeeker.seekerLock < 0.75f || Vector3.Dot(myTransform.forward, vectorToTarget.normalized) < 0.98f || (firedMissile3.heatSeeker.targetPosition - target.position).sqrMagnitude > 400f) && Time.time - breakRadius3 < breakAltitude4)
		{
			yield return null;
			if (ValidateTarget(target))
			{
				position = target.position + target.velocity * 2f;
				vectorToTarget = VectorTo(position);
				autoPilot.targetPosition = position;
				Vector3 forward = headLookTransform.parent.InverseTransformPoint(target.position);
				headLookTransform.localRotation = Quaternion.RotateTowards(headLookTransform.localRotation, Quaternion.LookRotation(forward), 50f * Time.deltaTime);
				if (Vector3.Dot(target.transform.forward, myTransform.forward) > 0.85f)
				{
					float magnitude2 = target.velocity.magnitude;
					autoPilot.targetSpeed = magnitude2 - (1000f - vectorToTarget.magnitude) / 50f;
				}
				else
				{
					autoPilot.targetSpeed = maxSpeed;
				}
				SafetyOverrides();
			}
		}
		if (!ValidateTarget(target) || !(firedMissile3.heatSeeker.seekerLock >= 0.75f) || !((firedMissile3.heatSeeker.targetPosition - target.position).sqrMagnitude < 400f) || !(Vector3.Dot(myTransform.forward, vectorToTarget.normalized) >= 0.98f))
		{
			yield break;
		}
		currTime5 = VectorTo(target.position).magnitude;
		if ((bool)aiWing && aiWing.GetNumMissilesOnTarget(target) >= aiWing.maxMissilePerTarget)
		{
			float targetDist3 = Time.time;
			float currTime3 = UnityEngine.Random.Range(1f, 2f);
			while (ValidateTarget(target) && Time.time - targetDist3 < currTime3)
			{
				FlyOrbit(target.transform, currTime5, maxSpeed, defaultAltitude, orbitClockwise);
				SafetyOverrides();
				yield return null;
			}
		}
		else
		{
			if (!ValidateTarget(target))
			{
				yield break;
			}
			bool flag2 = false;
			for (int i = 0; i < TargetManager.instance.allActors.Count; i++)
			{
				Actor actor = TargetManager.instance.allActors[i];
				if (actor.team == this.actor.team && Vector3.Dot((actor.position - myTransform.position).normalized, (target.position - myTransform.position).normalized) > 0.92f)
				{
					flag2 = true;
					break;
				}
			}
			if (flag2 && currTime5 < gunAirMaxRange && SwitchToGun(target))
			{
				yield return StartCombatSubroutine(CombatGunAirRoutine(target));
				yield break;
			}
			if (!flag2)
			{
				wm.SingleFire();
				yield return null;
				firedMissile3 = wm.lastFiredMissile;
				if ((bool)firedMissile3 && firedMissile3.fired)
				{
					if ((bool)aiWing)
					{
						aiWing.ReportMissileOnTarget(this.actor, target, firedMissile3);
					}
					PlayRadioMessage(WingmanVoiceProfile.Messages.Fox2, 3f);
					StartCoroutine(CallMissileResultRoutine(target, firedMissile3, WingmanVoiceProfile.Messages.Splash, WingmanVoiceProfile.Messages.AirMiss));
				}
			}
			if (wm.isMasterArmed)
			{
				wm.ToggleMasterArmed();
			}
		}
	}

	private IEnumerator CallMissileResultRoutine(Actor target, Missile missile, WingmanVoiceProfile.Messages successMessage, WingmanVoiceProfile.Messages failMessage)
	{
		if (!doRadioComms)
		{
			yield break;
		}
		while ((bool)missile)
		{
			yield return oneSecWait;
		}
		if (actor.alive)
		{
			if (!target || !target.alive)
			{
				PlayRadioMessage(successMessage);
			}
			else if ((bool)target && target.alive)
			{
				PlayRadioMessage(failMessage);
			}
		}
	}

	private IEnumerator CallAttackResultRoutine(Actor target, float delay, WingmanVoiceProfile.Messages successMessage, WingmanVoiceProfile.Messages failMessage)
	{
		if (!doRadioComms)
		{
			yield break;
		}
		yield return new WaitForSeconds(delay);
		if (!target || !target.alive)
		{
			if (successMessage != WingmanVoiceProfile.Messages.None)
			{
				PlayRadioMessage(successMessage);
			}
		}
		else if (failMessage != WingmanVoiceProfile.Messages.None && (bool)target && target.alive)
		{
			PlayRadioMessage(failMessage);
		}
	}

	private float TurnToMissileTargetInputLimit(Vector3 targetPosition)
	{
		return Mathf.Max(0.5f, Mathf.Clamp01((autoPilot.currentSpeed - 180f) / 100f) + 15000f / VectorTo(targetPosition).magnitude);
	}

	private IEnumerator CombatGunAirRoutine(Actor target)
	{
		if (!(wm.currentEquip is HPEquipGun))
		{
			Debug.LogErrorFormat("AI Pilot {0} entered CombatGunAirRoutine but current equip was not a gun! ({1})", actor.DebugName(), wm.currentEquip ? wm.currentEquip.fullName : "null");
			yield break;
		}
		Gun gun = ((HPEquipGun)wm.currentEquip).gun;
		if (!ValidateTarget(target))
		{
			yield break;
		}
		_ = target.velocity;
		float turnTimer = 0f;
		float burstTimer = 0f;
		float burstDuration = UnityEngine.Random.Range(1f, 2.5f);
		float maxTurnTime = UnityEngine.Random.Range(5f, 8f);
		if (wm.availableWeaponTypes.aam)
		{
			maxTurnTime = UnityEngine.Random.Range(1f, 3f);
		}
		bool shootAtMerge = UnityEngine.Random.Range(0f, 100f) < 15f;
		if (target == lastMergeTarget)
		{
			shootAtMerge = true;
		}
		lastMergeTarget = target;
		Vector3 randAvoidDir = Vector3.ProjectOnPlane(UnityEngine.Random.onUnitSphere, Vector3.forward).normalized;
		bool avoidingMergeImpact = false;
		while (ValidateTarget(target))
		{
			autoPilot.steerMode = AutoPilot.SteerModes.Stable;
			Vector3 vector = target.position - gun.fireTransforms[0].position;
			float magnitude = vector.magnitude;
			autoPilot.inputLimiter = 1f;
			float t = leadHelpRand + Time.time;
			float leadHelp = AIHelper.instance.GetLeadHelp(t);
			Vector3 calculatedTargetPosition = gun.GetCalculatedTargetPosition(target, calcAccel: true);
			Vector3 vector2 = leadHelp * Vector3.ProjectOnPlane(target.velocity, vector);
			autoPilot.targetPosition = calculatedTargetPosition + vector2;
			float num = Vector3.Angle(gun.fireTransforms[0].forward, autoPilot.targetPosition - gun.fireTransforms[0].position);
			turnTimer += Time.deltaTime;
			bool flag = false;
			if (avoidingMergeImpact)
			{
				autoPilot.targetPosition = target.transform.TransformPoint(randAvoidDir * 500f);
				if (turnTimer >= maxTurnTime)
				{
					UnlockReferenceTransform(RefTfLocks.Combat);
					break;
				}
			}
			else if (magnitude < gunAirMaxRange * 1.1f && turnTimer < maxTurnTime && burstTimer < burstDuration)
			{
				if (num < 25f)
				{
					if (Vector3.Dot(myTransform.forward, target.velocity.normalized) > 0.75f && Vector3.Dot((myTransform.position - target.position).normalized, target.transform.forward) < 0f)
					{
						autoPilot.targetSpeed = target.velocity.magnitude;
					}
					else
					{
						autoPilot.targetSpeed = maxSpeed;
					}
					autoPilot.steerMode = AutoPilot.SteerModes.Aim;
					SetReferenceTransform(gun.fireTransforms[0], RefTfLocks.Combat);
					if (num < 5f)
					{
						bool flag2 = true;
						float num2 = 0.9986295f;
						if (Vector3.Dot(-target.velocity, rb.velocity.normalized) > num2 && !shootAtMerge)
						{
							flag2 = false;
						}
						if (flag2)
						{
							if (!flag && doRadioComms)
							{
								PlayRadioMessage(WingmanVoiceProfile.Messages.Guns);
								StartCoroutine(CallAttackResultRoutine(target, burstDuration + 2f, WingmanVoiceProfile.Messages.Splash, WingmanVoiceProfile.Messages.None));
							}
							wm.SingleFire();
							burstTimer += Time.deltaTime;
						}
						else
						{
							avoidingMergeImpact = true;
						}
					}
					turnTimer = 0f;
				}
			}
			else
			{
				if (!(burstTimer >= burstDuration))
				{
					if (turnTimer >= maxTurnTime)
					{
						UnlockReferenceTransform(RefTfLocks.Combat);
						yield return StartCombatSubroutine(CombatExtendRoutine(Vector3.Reflect(-vector, myTransform.forward) + 100f * Vector3.down, UnityEngine.Random.Range(1f, 4f)));
					}
					else
					{
						UnlockReferenceTransform(RefTfLocks.Combat);
					}
					break;
				}
				burstTimer += Time.deltaTime;
				if (burstTimer > burstDuration + 1f)
				{
					UnlockReferenceTransform(RefTfLocks.Combat);
					if (wm.availableWeaponTypes.aam)
					{
						yield return StartCombatSubroutine(CombatExtendRoutine(Vector3.Reflect(-vector, myTransform.forward) + 100f * Vector3.down, UnityEngine.Random.Range(5f, 8f)));
					}
					break;
				}
			}
			SafetyOverrides();
			yield return null;
			if (autoPilot.currentSpeed < minCombatSpeed)
			{
				yield return StartCombatSubroutine(CombatRegainSpeedRoutine());
			}
		}
	}

	private InternalWeaponBay GetExternallyControlledIWB(HPEquippable eq)
	{
		InternalWeaponBay result = null;
		InternalWeaponBay[] internalWeaponBays = wm.internalWeaponBays;
		foreach (InternalWeaponBay internalWeaponBay in internalWeaponBays)
		{
			if (internalWeaponBay.hardpointIdx == eq.hardpointIdx && internalWeaponBay.externallyControlled)
			{
				result = internalWeaponBay;
				break;
			}
		}
		return result;
	}

	private List<InternalWeaponBay> GetAllExternallyControlledIWBs(HPEquippable eq)
	{
		List<InternalWeaponBay> list = new List<InternalWeaponBay>();
		InternalWeaponBay[] internalWeaponBays = wm.internalWeaponBays;
		foreach (InternalWeaponBay internalWeaponBay in internalWeaponBays)
		{
			HPEquippable hPEquippable = null;
			if ((bool)(hPEquippable = wm.GetEquip(internalWeaponBay.hardpointIdx)) && hPEquippable.shortName == eq.shortName && internalWeaponBay.externallyControlled)
			{
				list.Add(internalWeaponBay);
			}
		}
		return list;
	}

	private IEnumerator CombatLaserBombRoutine(Actor target)
	{
		HPEquipLaserBombRack bombEq = (HPEquipLaserBombRack)wm.currentEquip;
		float bombSpeed = 315f;
		float num = EstimateBombRunDist(bombSpeed, defaultAltitude, 0f);
		float distance = num * 1.2f;
		GetAttackVectors(target.position, num, out var ingress, out var egress);
		yield return StartCombatSubroutine(CombatSetupSurfaceAttackDir(target, distance, defaultAltitude, ingress));
		if (!ValidateTarget(target))
		{
			yield break;
		}
		InternalWeaponBay iwb = GetExternallyControlledIWB(bombEq);
		bool bayOpen = false;
		bool doBomb = false;
		while (ValidateTarget(target))
		{
			if (!wm.opticalTargeter)
			{
				yield break;
			}
			Vector3 targetPosition = target.position + new Vector3(0f, defaultAltitude, 0f);
			autoPilot.targetPosition = targetPosition;
			autoPilot.targetSpeed = Mathf.Max(bombSpeed, navSpeed);
			ApplyPlanarTurnAround();
			if ((bool)aiWing && aiWing.GetNumMissilesOnTarget(target) >= aiWing.maxMissilePerTarget)
			{
				wm.opticalTargeter.Unlock();
				yield break;
			}
			float maxLockingDistance = wm.opticalTargeter.maxLockingDistance;
			if ((target.position - wm.opticalTargeter.cameraTransform.position).sqrMagnitude < maxLockingDistance * maxLockingDistance && wm.opticalTargeter.lockedActor != target)
			{
				wm.opticalTargeter.Lock(target.position);
			}
			float deployRadius = bombEq.GetDeployRadius(target.position);
			float magnitude = (bombEq.GetImpactPoint() - target.position).magnitude;
			if (magnitude < deployRadius)
			{
				if (!bayOpen)
				{
					bayOpen = true;
					if ((bool)iwb)
					{
						iwb.RegisterOpenReq(this);
					}
				}
				if (magnitude < deployRadius / 2f && wm.opticalTargeter.lockedActor == target && bombEq.LaunchAuthorized())
				{
					doBomb = true;
					break;
				}
			}
			SafetyOverrides();
			yield return null;
		}
		Missile missile = null;
		if (doBomb)
		{
			missile = bombEq.ml.GetNextMissile();
			wm.SingleFire();
			BeginDeploySafety();
			yield return null;
			if ((bool)aiWing)
			{
				aiWing.ReportMissileOnTarget(actor, target, missile);
			}
			PlayRadioMessage(WingmanVoiceProfile.Messages.Pickle, 2f, 0.5f);
			StartCoroutine(CallMissileResultRoutine(target, missile, WingmanVoiceProfile.Messages.Shack, WingmanVoiceProfile.Messages.GroundMiss));
			float t = Time.time;
			while (Time.time - t < _deploySafetyDuration)
			{
				autoPilot.targetPosition = myTransform.position + rb.velocity * 5000f;
				autoPilot.targetSpeed = Mathf.Max(bombSpeed, navSpeed);
				SafetyOverrides();
				yield return null;
			}
		}
		if (bayOpen && (bool)iwb)
		{
			iwb.UnregisterOpenReq(this);
		}
		if (doBomb)
		{
			if (missile.opticalFAF)
			{
				bool flag = ((bool)rwr && rwr.missileDetected) || ((bool)moduleRWR && moduleRWR.isMissileLocked);
				StartCombatSubroutine(UnlockTGPAfterDelay(2f));
				yield return StartCombatSubroutine(CombatPostMissileBrakeRoutineDir(egress, flag, flag));
			}
			else
			{
				bool clockWise = Vector3.Dot(Vector3.Cross(target.position - myTransform.position, Vector3.up), egress) < 0f;
				yield return StartCombatSubroutine(CombatWaitForOpticalMissileNonFAF(target, missile, clockWise));
			}
		}
		if ((bool)wm.opticalTargeter)
		{
			wm.opticalTargeter.Unlock();
		}
	}

	private IEnumerator UnlockTGPAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		if (actor.alive && (bool)wm.opticalTargeter)
		{
			wm.opticalTargeter.Unlock();
		}
	}

	private IEnumerator CombatGPSBombRoutine(Actor target)
	{
		HPEquipGPSBombRack bombEq = (HPEquipGPSBombRack)wm.currentEquip;
		float bombSpeed = 315f;
		float num = EstimateBombRunDist(bombSpeed, defaultAltitude, 0f);
		float distance = num * 1.2f;
		GetAttackVectors(target.position, num, out var ingress, out var egress);
		yield return StartCombatSubroutine(CombatSetupSurfaceAttackDir(target, distance, defaultAltitude, ingress));
		if (!ValidateTarget(target))
		{
			yield break;
		}
		string grpName = "GPS 3";
		wm.gpsSystem.CreateGroup("GPS", 3);
		wm.gpsSystem.SetCurrentGroup(grpName);
		wm.gpsSystem.AddTarget(target.position, "AI");
		InternalWeaponBay iwb = GetExternallyControlledIWB(bombEq);
		FixedPoint flyTgt = new FixedPoint(target.position + new Vector3(0f, defaultAltitude, 0f));
		bool bayOpened = false;
		while (ValidateTarget(target))
		{
			autoPilot.targetPosition = flyTgt.point;
			autoPilot.targetSpeed = Mathf.Max(bombSpeed, navSpeed);
			ApplyPlanarTurnAround();
			if ((bool)aiWing && aiWing.GetNumMissilesOnTarget(target) >= aiWing.maxMissilePerTarget)
			{
				wm.opticalTargeter.Unlock();
				yield break;
			}
			Vector3 vector = PlanarDirection(target.position - myTransform.position) * (bombEq.GetDeployRadius(target.position) / 2f);
			if (Vector3.Dot(bombEq.GetImpactPoint() - (target.position + vector), vector) > 0f || (bombEq.GPSTargetInOptimalRange() && (!iwb || iwb.doorState > 0.99f)))
			{
				break;
			}
			if ((bool)iwb && !bayOpened && bombEq.LaunchAuthorized())
			{
				bayOpened = true;
				iwb.RegisterOpenReq(this);
			}
			SafetyOverrides();
			yield return null;
		}
		if (ValidateTarget(target) && bombEq.GPSTargetInOptimalRange())
		{
			Debug.LogFormat("{0} dropping GPS bomb", actor.DebugName());
			Missile nextMissile = bombEq.ml.GetNextMissile();
			wm.SingleFire();
			BeginDeploySafety();
			if ((bool)aiWing)
			{
				aiWing.ReportMissileOnTarget(actor, target, nextMissile);
			}
			PlayRadioMessage(WingmanVoiceProfile.Messages.Pickle, 2f, 0.5f);
			StartCoroutine(CallMissileResultRoutine(target, nextMissile, WingmanVoiceProfile.Messages.Shack, WingmanVoiceProfile.Messages.GroundMiss));
		}
		while (isDeploySafety)
		{
			autoPilot.targetPosition = flyTgt.point;
			autoPilot.targetSpeed = Mathf.Max(bombSpeed, navSpeed);
			SafetyOverrides();
			yield return null;
		}
		if (bayOpened)
		{
			iwb.UnregisterOpenReq(this);
		}
		wm.gpsSystem.SetCurrentGroup(grpName);
		wm.gpsSystem.RemoveCurrentGroup();
		for (float t = 0f; t < 6f; t += Time.deltaTime)
		{
			autoPilot.targetPosition = PositionAtAltitudeRadar(myTransform.position + egress * 5000f, defaultAltitude);
			autoPilot.targetSpeed = maxSpeed;
			ApplyPlanarTurnAround();
			SafetyOverrides();
			yield return null;
		}
	}

	private IEnumerator CombatBombRoutine(Actor target)
	{
		FixedPoint targetPosition = new FixedPoint(target.position);
		HPEquipBombRack bomb = (HPEquipBombRack)wm.currentEquip;
		float sqrExplodeRadius = Mathf.Pow(bomb.ml.GetNextMissile().explodeRadius * 0.5f, 2f);
		InternalWeaponBay bombIwb = GetExternallyControlledIWB(bomb);
		float bombSpeed = Mathf.Lerp(navSpeed, maxSpeed, 0.5f);
		float bombAltitude = defaultAltitude;
		if (bomb.ai_maxBombAltitude > 10f)
		{
			bombAltitude = Mathf.Min(bombAltitude, bomb.ai_maxBombAltitude);
		}
		float origARF = autoPilot.angularRollFactor;
		int num = 1;
		Actor bombStartTgt = target;
		List<Actor> obj = ((this.actor.team == Teams.Allied) ? TargetManager.instance.enemyUnits : TargetManager.instance.alliedUnits);
		float num2 = 40000f;
		Vector3 zero = Vector3.zero;
		List<Actor> bombTgts = new List<Actor> { target };
		foreach (Actor item in obj)
		{
			Vector3 vector = item.position - target.position;
			if ((bool)item && (item.finalCombatRole == Actor.Roles.Ground || item.finalCombatRole == Actor.Roles.GroundArmor) && item.alive && item != target && vector.sqrMagnitude < num2)
			{
				bombTgts.Add(item);
				zero += vector;
				num++;
			}
		}
		float diveAngle = UnityEngine.Random.Range(5f, (num > 1) ? 25f : 45f);
		float num3 = EstimateBombRunDist(bombSpeed, bombAltitude, diveAngle);
		float num4 = Mathf.Clamp(num3, 3000f, 9000f);
		GetAttackVectors(target.position, num3, out var ingress, out var egress);
		if (num > 1)
		{
			Vector3 vector2 = PositionAtAltitudeRadar(target.position - zero.normalized * num4, defaultAltitude);
			float num5 = float.MaxValue;
			float num6 = float.MinValue;
			Actor actor = null;
			foreach (Actor item2 in bombTgts)
			{
				float sqrMagnitude = (item2.position - vector2).sqrMagnitude;
				if (sqrMagnitude < num5)
				{
					num5 = sqrMagnitude;
					bombStartTgt = item2;
				}
				if (sqrMagnitude > num6)
				{
					num6 = sqrMagnitude;
					actor = item2;
				}
			}
			Vector3 vector3 = zero;
			zero = actor.position - bombStartTgt.position;
			if (zero.sqrMagnitude < 1f)
			{
				zero = vector3;
			}
			_ = zero.sqrMagnitude;
			yield return StartCombatSubroutine(CombatSetupSurfaceAttackDir(target, num4, bombAltitude, zero));
			autoPilot.angularRollFactor = 0f;
			yield return StartCombatSubroutine(MultiBombRoutine(bombTgts, bomb, diveAngle, bombAltitude, bombSpeed));
			autoPilot.angularRollFactor = origARF;
			yield break;
		}
		yield return StartCombatSubroutine(CombatSetupSurfaceAttackDir(target, num4, bombAltitude, ingress));
		if (!ValidateTarget(target))
		{
			yield break;
		}
		targetPosition.point = target.position;
		bool bombed = false;
		autoPilot.inputLimiter = 1f;
		_ = myTransform.position;
		_ = targetPosition.point;
		int velSample = 0;
		autoPilot.angularRollFactor = 0f;
		bool turnToDiveComplete = false;
		bool extendComplete = false;
		float abortT = 0f;
		while (ValidateTarget(target))
		{
			if ((bool)aiWing && aiWing.GetNumMissilesOnTarget(target) >= aiWing.maxMissilePerTarget)
			{
				wm.opticalTargeter.Unlock();
				break;
			}
			float t;
			Vector3 impactPointWithLead = bomb.GetImpactPointWithLead(out t);
			Vector3 velocity = target.velocity;
			targetPosition.point = (bombStartTgt ? bombStartTgt.position : target.position) + velocity * t;
			Vector3 lhs = impactPointWithLead + rb.velocity * Time.fixedDeltaTime - targetPosition.point;
			bool impactPtPastTgt = Vector3.Dot(lhs, targetPosition.point - myTransform.position) > 0f;
			GetBombFlyTgtPos(targetPosition.point, diveAngle, bombAltitude, bombSpeed, out var flyTgtPos2, out var preDivePt);
			bool flag = false;
			Vector3 vector4 = preDivePt - targetPosition.point;
			vector4.y = 0f;
			float magnitude = vector4.magnitude;
			vector4 /= magnitude;
			float magnitude2 = PlanarVectorTo(targetPosition.point).magnitude;
			if (!extendComplete && magnitude2 < magnitude + 2000f)
			{
				flyTgtPos2 = preDivePt + vector4 * 2000f;
			}
			else if (Vector3.Dot(targetPosition.point - myTransform.position, preDivePt - myTransform.position) > 0f && Vector3.Distance(myTransform.position, preDivePt) > 2f * autoPilot.currentSpeed)
			{
				extendComplete = true;
				flyTgtPos2 = preDivePt;
			}
			else
			{
				flag = true;
				if (Vector3.Dot(myTransform.right, Vector3.Cross(Vector3.up, myTransform.forward)) > 0.7f && Vector3.Dot(rb.velocity.normalized, (flyTgtPos2 - rb.position).normalized) > 0.99f)
				{
					if (!impactPtPastTgt)
					{
						if (!turnToDiveComplete)
						{
							turnToDiveComplete = true;
							if ((bool)bombIwb)
							{
								bombIwb.RegisterOpenReq(this);
							}
						}
					}
					else
					{
						turnToDiveComplete = true;
					}
				}
				Vector3 vector5 = (flyTgtPos2 - myTransform.position).normalized * 1500f;
				if (Vector3.Dot(targetPosition.point - myTransform.position, myTransform.forward) > 0f)
				{
					Vector3 normalized = Vector3.Cross(Vector3.up, target.position - myTransform.position).normalized;
					float current3 = Vector3.Dot(normalized, impactPointWithLead - target.position) * bombXCorrectionMul;
					float num7 = landingHorizPID.Evaluate(current3, 0f);
					vector5 += num7 * normalized;
				}
				flyTgtPos2 = myTransform.position + vector5;
			}
			autoPilot.targetSpeed = maxSpeed;
			autoPilot.throttleLimiter = 1f;
			if (!bombed)
			{
				if (extendComplete)
				{
					Vector3 vector6 = impactPointWithLead - targetPosition.point;
					vector6.y = 0f;
					float sqrDist = vector6.sqrMagnitude;
					if (flag && turnToDiveComplete && Vector3.Dot(target.position - myTransform.position, rb.velocity) > 0f)
					{
						if (impactPtPastTgt)
						{
							if (bomb.GetCount() > 0 && sqrDist < sqrExplodeRadius)
							{
								wm.SingleFire();
								if ((bool)bombIwb)
								{
									bombIwb.UnregisterOpenReq(this);
								}
								bombed = true;
								_ = Time.time;
								PlayRadioMessage(WingmanVoiceProfile.Messages.Pickle, 3f);
								BeginDeploySafety();
								yield return null;
								StartCoroutine(CallMissileResultRoutine(target, wm.lastFiredMissile, WingmanVoiceProfile.Messages.Shack, WingmanVoiceProfile.Messages.GroundMiss));
								if ((bool)aiWing)
								{
									aiWing.ReportMissileOnTarget(this.actor, target, wm.lastFiredMissile);
								}
							}
							if (flightInfo.radarAltitude < GetMinAltAtAttitudeAndSpeed(minAltitude) && impactPtPastTgt)
							{
								bombed = true;
								_ = Time.time;
							}
						}
						_ = sqrDist;
					}
					autoPilot.targetSpeed = bombSpeed;
					if (autoPilot.currentSpeed > bombSpeed * 0.7f)
					{
						SetThrottleLimiterDisallowAB();
					}
					else
					{
						autoPilot.throttleLimiter = 1f;
					}
					if (sqrDist < 1000000f)
					{
						if (!firingCms)
						{
							StartCoroutine(CountermeasureRoutine(flares: true, !rwr || rwr.missileDetected));
						}
						if (velSample >= 0)
						{
							bombSpeed = autoPilot.currentSpeed;
							velSample = -1000;
						}
					}
					else if (velSample > 30)
					{
						velSample = 0;
						_ = target.velocity;
					}
					else
					{
						velSample++;
					}
					if (turnToDiveComplete && impactPtPastTgt)
					{
						abortT += Time.deltaTime;
						if (abortT > 1f)
						{
							bombed = true;
						}
					}
				}
				autoPilot.targetPosition = flyTgtPos2;
				ApplyPlanarTurnAround();
				SafetyOverrides();
				yield return null;
				flyTgtPos2 = default(Vector3);
				continue;
			}
			autoPilot.angularRollFactor = origARF;
			if (!firingCms)
			{
				StartCoroutine(CountermeasureRoutine(flares: true, !rwr || rwr.missileDetected));
			}
			yield return StartCombatSubroutine(CombatExtendRoutine(egress, UnityEngine.Random.Range(1f, 4f)));
			break;
		}
	}

	private IEnumerator CombatWaitForOpticalMissileNonFAF(Actor target, Missile m, bool clockWise = true)
	{
		if (m.opticalFAF)
		{
			yield break;
		}
		bool reLockRequired = false;
		while (ValidateTarget(target) && (bool)m && m.hasTarget)
		{
			FlyOrbit(target.transform, orbitRadius, navSpeed, defaultAltitude, clockWise);
			Vector3 target2 = autoPilot.targetPosition - myTransform.position;
			target2 = Vector3.RotateTowards(target.position - myTransform.position, target2, (float)Math.PI / 3f, float.MaxValue);
			autoPilot.targetPosition = myTransform.position + target2;
			if (wm.opticalTargeter.isGimbalLimit || wm.opticalTargeter.lockedActor != target)
			{
				reLockRequired = true;
			}
			if (reLockRequired && !wm.opticalTargeter.isGimbalLimit)
			{
				reLockRequired = !wm.opticalTargeter.Lock(target.position);
			}
			SafetyOverrides();
			yield return null;
		}
	}

	private void GetBombFlyTgtPos(Vector3 targetPosition, float diveAngle, float bombAltitude, float bombSpeed, out Vector3 flyTgtPos, out Vector3 preDivePt)
	{
		Vector3 vector = targetPosition - myTransform.position;
		vector.y = 0f;
		flyTgtPos = targetPosition;
		flyTgtPos.y += bombAltitude;
		new Plane(Vector3.down, flyTgtPos);
		Vector3 axis = Vector3.Cross(Vector3.up, vector);
		Quaternion quaternion = Quaternion.AngleAxis(diveAngle, axis);
		flyTgtPos = Vector3.Lerp(targetPosition, targetPosition + bombAltitude * Vector3.up, Mathf.Pow(Mathf.Cos(diveAngle * ((float)Math.PI / 180f)), 4f));
		Ray ray = new Ray(flyTgtPos, quaternion * -vector);
		float distance = EstimateBombRunDist(bombSpeed, bombAltitude, diveAngle);
		preDivePt = ray.GetPoint(distance) - vector.normalized * 500f;
	}

	private IEnumerator MultiBombRoutine(List<Actor> bombTgts, HPEquipBombRack bomb, float diveAngle, float bombAltitude, float bombSpeed)
	{
		float sqrExplodeRadius = bomb.ml.GetNextMissile().explodeRadius;
		sqrExplodeRadius *= sqrExplodeRadius;
		int enemyCount = bombTgts.Count;
		FixedPoint refTgtPoint = new FixedPoint(Vector3.zero);
		FixedPoint finalTgtPoint = new FixedPoint(Vector3.zero);
		bool turnToDiveComplete = false;
		bool bombingComplete = false;
		int numBombsReleased = 0;
		bool extendComplete = false;
		List<InternalWeaponBay> iwbs = GetAllExternallyControlledIWBs(bomb);
		while (true)
		{
			Vector3 vector = Vector3.zero;
			float num = float.MaxValue;
			float num2 = 0f;
			foreach (Actor bombTgt in bombTgts)
			{
				if ((bool)bombTgt && bombTgt.alive)
				{
					float sqrMagnitude = (bombTgt.position - myTransform.position).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						vector = bombTgt.velocity;
						refTgtPoint.point = bombTgt.position;
					}
					if (sqrMagnitude > num2)
					{
						num2 = sqrMagnitude;
						finalTgtPoint.point = bombTgt.position;
					}
				}
			}
			float t;
			Vector3 impactPt = bomb.GetImpactPointWithLead(out t);
			refTgtPoint.point += vector * t;
			finalTgtPoint.point += vector * t;
			GetBombFlyTgtPos(refTgtPoint.point, diveAngle, bombAltitude, bombSpeed, out var flyTgtPos2, out var preDivePt);
			bool flag = false;
			Vector3 vector2 = preDivePt - refTgtPoint.point;
			vector2.y = 0f;
			float magnitude = vector2.magnitude;
			vector2 /= magnitude;
			float magnitude2 = PlanarVectorTo(refTgtPoint.point).magnitude;
			if (!extendComplete && magnitude2 < magnitude + 2000f)
			{
				flyTgtPos2 = preDivePt + vector2 * 2000f;
			}
			else if (Vector3.Dot(refTgtPoint.point - myTransform.position, preDivePt - myTransform.position) > 0f && Vector3.Distance(myTransform.position, preDivePt) > 2f * autoPilot.currentSpeed)
			{
				extendComplete = true;
				flyTgtPos2 = preDivePt;
			}
			else
			{
				flag = true;
				if (!(Vector3.Dot(impactPt - refTgtPoint.point, refTgtPoint.point - myTransform.position) > 0f) && Vector3.Dot(myTransform.right, Vector3.Cross(Vector3.up, myTransform.forward)) > 0.7f && Vector3.Dot(rb.velocity.normalized, (flyTgtPos2 - rb.position).normalized) > 0.99f && !turnToDiveComplete)
				{
					turnToDiveComplete = true;
					foreach (InternalWeaponBay item in iwbs)
					{
						item.RegisterOpenReq(this);
					}
				}
				Vector3 normalized = Vector3.Cross(Vector3.up, refTgtPoint.point - myTransform.position).normalized;
				float current2 = Vector3.Dot(normalized, impactPt - refTgtPoint.point);
				float num3 = landingHorizPID.Evaluate(current2, 0f);
				Vector3 vector3 = (flyTgtPos2 - myTransform.position).normalized * 1500f;
				vector3 += num3 * normalized;
				flyTgtPos2 = myTransform.position + vector3;
			}
			autoPilot.targetSpeed = maxSpeed;
			autoPilot.throttleLimiter = 1f;
			if (bombingComplete)
			{
				break;
			}
			if (extendComplete)
			{
				if (flag && turnToDiveComplete && Vector3.Dot(refTgtPoint.point - myTransform.position, rb.velocity) > 0f)
				{
					if (bomb.GetCount() > 0)
					{
						bool flag2 = false;
						Actor actor = null;
						foreach (Actor bombTgt2 in bombTgts)
						{
							if (!bombTgt2 || !bombTgt2.alive)
							{
								continue;
							}
							Vector3 vector4 = bombTgt2.position + bombTgt2.velocity * t;
							Vector3 vector5 = vector4 - impactPt;
							vector5.y = 0f;
							if (Vector3.Dot(rb.velocity, vector4 - impactPt) < 0f && vector5.sqrMagnitude < sqrExplodeRadius * 0.5f)
							{
								if (!flag2)
								{
									actor = bombTgt2;
									flag2 = true;
									Debug.DrawLine(myTransform.position, vector4, Color.red, 2f);
								}
							}
							else
							{
								Debug.DrawLine(myTransform.position, vector4, Color.grey);
							}
						}
						if ((bool)actor)
						{
							bombTgts.Remove(actor);
						}
						if (flag2)
						{
							wm.SingleFire();
							if (numBombsReleased == 0)
							{
								PlayRadioMessage(WingmanVoiceProfile.Messages.Pickle, 3f);
							}
							numBombsReleased++;
							if (numBombsReleased >= enemyCount || wm.combinedCount == 0)
							{
								bombingComplete = true;
							}
							yield return null;
							bomb = (HPEquipBombRack)wm.currentEquip;
						}
						if (Vector3.Dot(finalTgtPoint.point - myTransform.position, finalTgtPoint.point - impactPt) < 0f || climbingAboveMinAlt)
						{
							bombingComplete = true;
						}
					}
					else
					{
						bombingComplete = true;
					}
				}
				autoPilot.targetSpeed = bombSpeed;
				if (autoPilot.currentSpeed > navSpeed * 0.7f)
				{
					SetThrottleLimiterDisallowAB();
				}
				else
				{
					autoPilot.throttleLimiter = 1f;
				}
			}
			autoPilot.targetPosition = flyTgtPos2;
			ApplyPlanarTurnAround();
			SafetyOverrides();
			yield return null;
			flyTgtPos2 = default(Vector3);
		}
		foreach (InternalWeaponBay item2 in iwbs)
		{
			item2.UnregisterOpenReq(this);
		}
		Vector3 direction = Mathf.Sign(UnityEngine.Random.Range(-1f, 1f)) * Vector3.Cross(Vector3.up, rb.velocity);
		if (!firingCms)
		{
			StartCoroutine(CountermeasureRoutine(flares: true, !rwr || rwr.missileDetected));
		}
		BeginDeploySafety();
		yield return StartCombatSubroutine(CombatExtendRoutine(direction, UnityEngine.Random.Range(1f, 4f)));
	}

	private IEnumerator CarpetBombActorRoutine(HPEquipBombRack bombEquip, Actor target, float heading, float distInterval, float radius, float altitude, bool combat)
	{
		int dropCount = Mathf.CeilToInt(2f * radius / distInterval);
		int numDropped = 0;
		List<InternalWeaponBay> iwbs = GetAllExternallyControlledIWBs(bombEquip);
		if (iwbs.Count == 0)
		{
			wm.SetMasterArmed(armed: false);
		}
		bool wasAutoEngage = autoEngageEnemies;
		if (!combat)
		{
			commandState = CommandStates.Override;
			autoEngageEnemies = false;
		}
		Vector3 dir = VectorUtils.BearingVector(heading);
		float num = EstimateBombRunDist(maxSpeed, altitude, 0f);
		FixedPoint startPoint = new FixedPoint(PositionAtAltitudeRadar(target.position, altitude));
		FixedPoint lineupPoint = new FixedPoint(startPoint.point - dir * (radius + 1000f + num));
		yield return StartCombatSubroutine(CombatSetupSurfaceAttackDir(lineupPoint, 1000f, altitude, dir));
		autoPilot.inputLimiter = 1f;
		while (ValidateTarget(target) && Vector3.Dot(dir, lineupPoint.point - myTransform.position) > 0f)
		{
			if (!combat)
			{
				commandState = CommandStates.Override;
			}
			autoPilot.targetPosition = lineupPoint.point;
			autoPilot.targetSpeed = maxSpeed;
			SafetyOverrides(ignoreOverride: true);
			yield return null;
		}
		if (!ValidateTarget(target))
		{
			yield break;
		}
		FixedPoint lastDropPt = new FixedPoint(myTransform.position);
		if ((bool)wm.opticalTargeter)
		{
			wm.opticalTargeter.Lock(target.position);
		}
		bool wasAllowEvasive = allowEvasiveManeuvers;
		allowEvasiveManeuvers = false;
		bool dropped = false;
		bool openedBay = false;
		Vector3 vel2 = actor.velocity;
		while (ValidateTarget(target) && Vector3.Dot(dir, startPoint.point - myTransform.position) > 0f)
		{
			if (!combat)
			{
				commandState = CommandStates.Override;
			}
			if ((bool)wm.opticalTargeter && wm.opticalTargeter.lockedActor != target)
			{
				wm.opticalTargeter.Lock(target.position);
			}
			float t2;
			Vector3 impactPointWithLead = bombEquip.GetImpactPointWithLead(out t2, checkIwb: false);
			Vector3 vector = target.position + target.velocity * t2;
			float sqrMagnitude = (impactPointWithLead - vector).sqrMagnitude;
			float num2 = Mathf.Sqrt(sqrMagnitude) / flightInfo.airspeed;
			autoPilot.targetPosition = vector + target.velocity * num2 + new Vector3(0f, altitude, 0f);
			Vector3 targetPosition = autoPilot.targetPosition;
			targetPosition.y += 4f * (altitude - flightInfo.radarAltitude);
			autoPilot.targetPosition = targetPosition;
			autoPilot.targetSpeed = maxSpeed;
			float num3 = radius + 2f * maxSpeed;
			if (!openedBay && sqrMagnitude < num3 * num3)
			{
				openedBay = true;
				if (iwbs.Count > 0)
				{
					foreach (InternalWeaponBay item in iwbs)
					{
						item.RegisterOpenReq(this);
					}
				}
				else
				{
					wm.SetMasterArmed(armed: true);
					SwitchToBomb();
				}
				if (!firingCms)
				{
					StartCoroutine(CountermeasureRoutine(flares: true, (bool)rwr && rwr.missileDetected));
				}
			}
			if (sqrMagnitude < radius * radius || (dropped && numDropped < dropCount))
			{
				if ((lastDropPt.point - myTransform.position).sqrMagnitude > distInterval * distInterval)
				{
					lastDropPt = new FixedPoint(myTransform.position);
					wm.SingleFire();
					numDropped++;
					if (!dropped)
					{
						PlayRadioMessage(WingmanVoiceProfile.Messages.Pickle, 3f);
						vel2 = (autoPilot.targetPosition - myTransform.position).normalized * 300f + new Vector3(0f, 5f, 0f);
						radius *= 1.1f;
					}
					dropped = true;
					yield return null;
					if (wm.currentEquip == null || wm.currentEquip.GetCount() == 0)
					{
						break;
					}
				}
				autoPilot.targetPosition = myTransform.position + vel2;
				vel2 += new Vector3(0f, 7f * Time.deltaTime, 0f);
			}
			else if (dropped)
			{
				break;
			}
			AltitudeSafety();
			if (firingFlares)
			{
				SetThrottleLimiterDisallowAB();
			}
			yield return null;
		}
		if (!ValidateTarget(target))
		{
			yield break;
		}
		if (!firingCms)
		{
			StartCoroutine(CountermeasureRoutine(flares: true, (bool)rwr && rwr.missileDetected));
		}
		vel2 = actor.velocity;
		float t = Time.time;
		while (Time.time - t < 2f)
		{
			if (!combat)
			{
				commandState = CommandStates.Override;
			}
			autoPilot.targetPosition = myTransform.position + vel2;
			autoPilot.targetSpeed = maxSpeed;
			AltitudeSafety();
			vel2 += new Vector3(0f, 10f * Time.deltaTime, 0f);
			yield return null;
		}
		allowEvasiveManeuvers = wasAllowEvasive;
		BeginDeploySafety();
		Disarm();
		if (!combat)
		{
			commandState = CommandStates.Orbit;
		}
		autoEngageEnemies = wasAutoEngage;
		autoPilot.throttleLimiter = 1f;
	}

	private void BalanceWeaponSelection()
	{
		if (!wm.currentEquip || !wm.isMasterArmed)
		{
			return;
		}
		string shortName = wm.currentEquip.shortName;
		if (!balanceDict.TryGetValue(shortName, out var value))
		{
			value = false;
			balanceDict.Add(shortName, value);
		}
		Vector3 vector = wm.transform.right;
		if (!value)
		{
			balanceDict[shortName] = true;
		}
		else
		{
			balanceDict[shortName] = false;
			vector = -vector;
		}
		int num = -1;
		float num2 = float.MinValue;
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip && equip.shortName == shortName)
			{
				float num3 = Vector3.Dot(equip.transform.position - myTransform.position, vector);
				if (num3 > num2)
				{
					num2 = num3;
					num = i;
				}
			}
		}
		if (num >= 0)
		{
			wm.SetWeapon(num);
		}
	}

	private IEnumerator CarpetBombRoutine(HPEquipBombRack bombEquip, FixedPoint wpt, float heading, float distInterval, float radius, float altitude, bool combat = false, CarpetBombResumeStates resumeState = CarpetBombResumeStates.None)
	{
		r_carpetBomb_heading = heading;
		r_carpetBomb_distInterval = distInterval;
		r_carpetBomb_radius = radius;
		r_carpetBomb_altitude = altitude;
		r_carpetBomb_bombEquip = bombEquip;
		r_carpetBomb_wpt = wpt;
		Vector3 dir = VectorUtils.BearingVector(heading);
		float bombDist = EstimateBombRunDist(maxSpeed, altitude, 0f);
		FixedPoint targetPoint = new FixedPoint(PositionAtAltitudeRadar(wpt.point, 0f));
		_ = actor.velocity;
		bool wasAllowEvasive = allowEvasiveManeuvers;
		bool wasAutoEngage = autoEngageEnemies;
		List<InternalWeaponBay> iwbs = GetAllExternallyControlledIWBs(bombEquip);
		if (resumeState == CarpetBombResumeStates.None || resumeState == CarpetBombResumeStates.Calculation)
		{
			r_carpetBomb_state = CarpetBombResumeStates.Calculation;
			resumeState = CarpetBombResumeStates.None;
			if (iwbs.Count == 0)
			{
				wm.SetMasterArmed(armed: false);
			}
			if (!combat)
			{
				commandState = CommandStates.Override;
				autoEngageEnemies = false;
			}
			FixedPoint altCheckStartPt = new FixedPoint(PositionAtAltitudeASL(wpt.point - dir * (bombDist * 1.25f), 0f));
			FixedPoint altCheckEndPt2 = new FixedPoint(PositionAtAltitudeASL(wpt.point, 0f));
			float magnitude = (altCheckStartPt.point - altCheckEndPt2.point).magnitude;
			int checkIntervals = Mathf.CeilToInt(magnitude / 150f);
			float maxSurfAlt2 = 0f;
			for (int i = 0; i <= checkIntervals; i++)
			{
				float t = (float)i / (float)checkIntervals;
				float altitude2 = WaterPhysics.GetAltitude(PositionAtAltitudeRadar(Vector3.Lerp(altCheckStartPt.point, altCheckEndPt2.point, t), 0f));
				maxSurfAlt2 = Mathf.Max(altitude2, maxSurfAlt2);
				autoPilot.targetPosition = myTransform.position + PlanarDirection(myTransform.forward) * 1000f;
				SafetyOverrides();
				if (!combat && cancelOverride)
				{
					ApplyQueuedCommand();
					yield break;
				}
				yield return null;
			}
			altitude += maxSurfAlt2;
			r_carpetBomb_altitude = altitude;
		}
		Vector3 worldPosition = PositionAtAltitudeASL(wpt.point, altitude);
		FixedPoint startPoint = new FixedPoint(worldPosition);
		FixedPoint lineupPoint = new FixedPoint(startPoint.point - dir * (radius + 1000f + bombDist));
		if (resumeState == CarpetBombResumeStates.None || resumeState == CarpetBombResumeStates.SetupSurfaceAttack)
		{
			r_carpetBomb_state = CarpetBombResumeStates.SetupSurfaceAttack;
			resumeState = CarpetBombResumeStates.None;
			yield return StartCombatSubroutine(CombatSetupSurfaceAttackDir(lineupPoint, 1000f, altitude, dir, aslAlt: true));
			if (!combat && cancelOverride)
			{
				ApplyQueuedCommand();
				yield break;
			}
		}
		if (resumeState == CarpetBombResumeStates.None || resumeState == CarpetBombResumeStates.FlyingToLineUp)
		{
			r_carpetBomb_state = CarpetBombResumeStates.FlyingToLineUp;
			resumeState = CarpetBombResumeStates.None;
			autoPilot.inputLimiter = 1f;
			while (Vector3.Dot(dir, lineupPoint.point - myTransform.position) > 0f)
			{
				if (!combat)
				{
					commandState = CommandStates.Override;
					if (cancelOverride)
					{
						ApplyQueuedCommand();
						yield break;
					}
				}
				autoPilot.targetPosition = lineupPoint.point;
				autoPilot.targetSpeed = maxSpeed;
				SafetyOverrides(ignoreOverride: true, 80f);
				yield return null;
			}
		}
		if (resumeState == CarpetBombResumeStates.Bombing || resumeState == CarpetBombResumeStates.None)
		{
			r_carpetBomb_state = CarpetBombResumeStates.Bombing;
			resumeState = CarpetBombResumeStates.None;
			FixedPoint altCheckEndPt2 = new FixedPoint(myTransform.position);
			if ((bool)wm.opticalTargeter)
			{
				wm.opticalTargeter.AreaLockPosition(targetPoint.point);
			}
			allowEvasiveManeuvers = false;
			bool dropped = false;
			bool openedBay = false;
			Vector3 vel2 = actor.velocity;
			while (Vector3.Dot(dir, startPoint.point - myTransform.position) > 0f)
			{
				if (!combat)
				{
					commandState = CommandStates.Override;
					if (cancelOverride)
					{
						ApplyQueuedCommand();
						yield break;
					}
				}
				autoPilot.targetPosition = startPoint.point;
				float num = Mathf.Clamp(4f * (altitude - flightInfo.altitudeASL), -50f, 50f);
				Vector3 targetPosition = autoPilot.targetPosition;
				targetPosition.y += num;
				autoPilot.targetPosition = targetPosition;
				autoPilot.targetSpeed = maxSpeed;
				Vector3 impactPoint = bombEquip.GetImpactPoint();
				float sqrMagnitude = (impactPoint - targetPoint.point).sqrMagnitude;
				if (!openedBay && sqrMagnitude < (radius + maxSpeed) * (radius + maxSpeed))
				{
					openedBay = true;
					if (iwbs.Count > 0)
					{
						if (!wm.isMasterArmed)
						{
							wm.SetMasterArmed(armed: true);
						}
						foreach (InternalWeaponBay item in iwbs)
						{
							item.RegisterOpenReq(this);
						}
					}
					else
					{
						wm.SetMasterArmed(armed: true);
						SwitchToBomb();
					}
					if (!firingCms)
					{
						StartCoroutine(CountermeasureRoutine(flares: true, (bool)rwr && rwr.missileDetected));
					}
				}
				if (sqrMagnitude < radius * radius)
				{
					if ((altCheckEndPt2.point - myTransform.position).sqrMagnitude > distInterval * distInterval)
					{
						altCheckEndPt2 = new FixedPoint(myTransform.position);
						wm.SingleFire();
						if (!dropped)
						{
							PlayRadioMessage(WingmanVoiceProfile.Messages.Pickle, 3f);
						}
						dropped = true;
					}
					autoPilot.targetPosition = myTransform.position + vel2;
					vel2 += new Vector3(0f, 7f * Time.deltaTime, 0f);
				}
				else if (dropped)
				{
					break;
				}
				AltitudeSafety(80f);
				if (!dropped)
				{
					vel2 = autoPilot.targetPosition - myTransform.position;
				}
				if (firingFlares)
				{
					SetThrottleLimiterDisallowAB();
				}
				yield return null;
				if (dropped && (wm.currentEquip == null || wm.currentEquip.GetCount() == 0))
				{
					break;
				}
			}
		}
		if (resumeState == CarpetBombResumeStates.PostBombing || resumeState == CarpetBombResumeStates.None)
		{
			r_carpetBomb_state = CarpetBombResumeStates.PostBombing;
			resumeState = CarpetBombResumeStates.None;
			if (!firingCms)
			{
				StartCoroutine(CountermeasureRoutine(flares: true, (bool)rwr && rwr.missileDetected));
			}
			Vector3 vel2 = actor.velocity;
			float maxSurfAlt2 = Time.time;
			while (Time.time - maxSurfAlt2 < 2f)
			{
				if (!combat)
				{
					commandState = CommandStates.Override;
					if (cancelOverride)
					{
						ApplyQueuedCommand();
						yield break;
					}
				}
				autoPilot.targetPosition = myTransform.position + vel2;
				autoPilot.targetSpeed = maxSpeed;
				AltitudeSafety();
				vel2 += new Vector3(0f, 10f * Time.deltaTime, 0f);
				yield return null;
			}
			allowEvasiveManeuvers = wasAllowEvasive;
			BeginDeploySafety();
			Disarm();
			if (!combat)
			{
				commandState = CommandStates.Orbit;
			}
			autoEngageEnemies = wasAutoEngage;
			autoPilot.throttleLimiter = 1f;
		}
		r_carpetBomb_state = CarpetBombResumeStates.None;
	}

	public void OrderCarpetBomb(Waypoint wpt, float heading, int count, float altitude)
	{
		if (wm.availableWeaponTypes.bomb && !flightInfo.isLanded && SwitchToBomb())
		{
			HPEquipBombRack hPEquipBombRack = (HPEquipBombRack)wm.currentEquip;
			float num = hPEquipBombRack.ml.missilePrefab.GetComponent<Missile>().explodeRadius * 0.65f;
			float radius = num * (float)count / 2f;
			StartCoroutine(CarpetBombRoutine(hPEquipBombRack, new FixedPoint(wpt.worldPosition), heading, num, radius, altitude));
		}
	}

	private float GetMinAltAtAttitudeAndSpeed(float hardMinimum)
	{
		float currentSpeed = autoPilot.currentSpeed;
		float num = kPlane.maxGCurve.Evaluate(currentSpeed) * 9.81f;
		float num2 = currentSpeed * currentSpeed / num;
		float num3 = Vector3.Dot(rb.velocity.normalized, Vector3.down);
		return Mathf.Max(num2 * num3 + hardMinimum, hardMinimum);
	}

	private float EstimateBombRunDist(float bombSpeed, float bombAltitude, float diveAngle)
	{
		return Mathf.Sqrt(2f * bombAltitude / 9.8f) * bombSpeed * 1.1f * Mathf.Cos(diveAngle * ((float)Math.PI / 180f));
	}

	private IEnumerator CombatGunGroundAttackRoutine(Actor target)
	{
		float turndownRadius = gunRunStartAltitude / Mathf.Tan((float)Math.PI / 180f * gunRunAngle);
		GetAttackVectors(target.position, turndownRadius / 2f, out var ingress, out var egress);
		if (!ValidateTarget(target))
		{
			yield break;
		}
		yield return StartCombatSubroutine(CombatSetupSurfaceAttackDir(target, turndownRadius * 1.55f, gunRunStartAltitude, ingress));
		if ((bool)target.flightInfo && !target.flightInfo.isLanded)
		{
			yield break;
		}
		autoPilot.inputLimiter = 1f;
		if (!ValidateTarget(target))
		{
			yield break;
		}
		yield return StartCombatSubroutine(CombatFlyToPositionForGroundRoutine(target, target.position, gunRunStartAltitude, turndownRadius, maxSpeed));
		if ((bool)target.flightInfo && !target.flightInfo.isLanded)
		{
			yield break;
		}
		float burstTime = 0f;
		float burstDuration = UnityEngine.Random.Range(1f, 3f);
		autoPilot.inputLimiter = 1f;
		HPEquipGun gunEquip = (HPEquipGun)wm.currentEquip;
		Transform gunTf = gunEquip.gun.fireTransforms[0];
		if (!firingCms)
		{
			StartCombatSubroutine(CountermeasureRoutine(flares: true, chaff: false));
		}
		bool radioed = false;
		while (ValidateTarget(target))
		{
			autoPilot.steerMode = AutoPilot.SteerModes.Aim;
			SetReferenceTransform(gunTf, RefTfLocks.Combat);
			Vector3 delta = VectorTo(target.position);
			float magnitude = delta.magnitude;
			Vector3 vector = target.velocity - rb.velocity;
			float num = VectorUtils.CalculateLeadTime(delta, vector, gunEquip.gun.bulletInfo.speed);
			Vector3 vector2 = target.position + num * vector - 0.5f * num * num * Physics.gravity;
			Vector3 to = VectorTo(vector2);
			SetAutopilotParams(vector2, maxSpeed, 1f, allowAB: false);
			if (magnitude < gunGroundMaxRange && Vector3.Angle(autoPilot.referenceTransform.forward, to) < 5f)
			{
				wm.SingleFire();
				burstTime += Time.deltaTime;
			}
			if (magnitude < 2000f)
			{
				if (!firingCms)
				{
					StartCoroutine(CountermeasureRoutine());
				}
				if (!radioed && doRadioComms)
				{
					radioed = true;
					PlayRadioMessage(WingmanVoiceProfile.Messages.Guns);
					StartCoroutine(CallAttackResultRoutine(target, burstDuration + 2f, WingmanVoiceProfile.Messages.Shack, WingmanVoiceProfile.Messages.None));
				}
			}
			if (burstTime > burstDuration || flightInfo.sweptRadarAltitude < GetMinAltAtAttitudeAndSpeed(gunRunMinAltitude))
			{
				autoPilot.throttleLimiter = 1f;
				break;
			}
			if (((bool)rwr && rwr.missileDetected) || flightInfo.radarAltitude > minAltitude)
			{
				SafetyOverrides(ignoreOverride: false, gunRunMinAltitude, doObstCheck2: false);
			}
			yield return null;
		}
		UnlockReferenceTransform(RefTfLocks.Combat);
		yield return StartCombatSubroutine(CombatExtendRoutine(egress, 2f));
	}

	private IEnumerator CombatRocketAttackRoutine(Actor target)
	{
		float turndownRadius = gunRunStartAltitude / Mathf.Tan((float)Math.PI / 180f * gunRunAngle);
		GetAttackVectors(target.position, turndownRadius / 2f, out var ingress, out var egress);
		yield return StartCombatSubroutine(CombatSetupSurfaceAttackDir(target, turndownRadius * 1.35f, gunRunStartAltitude, ingress));
		if (((bool)target.flightInfo && !target.flightInfo.isLanded) || !ValidateTarget(target))
		{
			yield break;
		}
		autoPilot.inputLimiter = 1f;
		yield return StartCombatSubroutine(CombatFlyToPositionForGroundRoutine(target, target.position, gunRunStartAltitude, turndownRadius, maxSpeed));
		if (((bool)target.flightInfo && !target.flightInfo.isLanded) || !ValidateTarget(target))
		{
			yield break;
		}
		float burstTime = 0f;
		autoPilot.inputLimiter = 1f;
		RocketLauncher rocketEquip = (RocketLauncher)wm.currentEquip;
		Transform aimTf = rocketEquip.GetAIAimTransform();
		float fireInterval = UnityEngine.Random.Range(0.06f, 1f);
		float burstDuration = fireInterval * UnityEngine.Random.Range(2f, 7f);
		float lastFireTime = Time.time;
		SetReferenceTransform(aimTf, RefTfLocks.Combat);
		bool fired = false;
		float estTimeToImpact = 0f;
		while (ValidateTarget(target))
		{
			autoPilot.steerMode = AutoPilot.SteerModes.Aim;
			if ((bool)wm.opticalTargeter && (!wm.opticalTargeter.locked || !wm.opticalTargeter.lockedActor == (bool)target))
			{
				wm.opticalTargeter.ForceLockActor(target);
			}
			float distToTarget = VectorTo(target.position).magnitude;
			autoPilot.targetSpeed = gunRunSpeed;
			float impactTime = rocketEquip.GetImpactTime();
			Vector3 vector = target.position + impactTime * target.velocity;
			autoPilot.targetPosition = vector;
			Vector3 to = VectorTo(vector);
			if (Vector3.Angle(autoPilot.referenceTransform.forward, to) < 5f)
			{
				Vector3 forward = aimTf.parent.InverseTransformDirection(rocketEquip.GetAimPoint() - aimTf.position);
				Vector3 upwards = aimTf.parent.InverseTransformDirection(myTransform.up);
				aimTf.localRotation = Quaternion.Slerp(aimTf.localRotation, Quaternion.LookRotation(forward, upwards), 3f * Time.deltaTime);
				if (distToTarget < gunGroundMaxRange * 1.5f && Vector3.Angle(aimTf.forward, to) < 0.5f)
				{
					if (Time.time - lastFireTime > fireInterval)
					{
						wm.SingleFire();
						lastFireTime = Time.time;
						burstTime += Time.deltaTime;
						fired = true;
						estTimeToImpact = rocketEquip.GetImpactTime();
						yield return null;
						if (!wm.currentEquip || !(wm.currentEquip is RocketLauncher))
						{
							break;
						}
						rocketEquip = (RocketLauncher)wm.currentEquip;
					}
					burstTime += Time.deltaTime;
				}
			}
			else
			{
				aimTf.rotation = myTransform.rotation;
			}
			if (distToTarget < 2000f && !firingCms)
			{
				StartCoroutine(CountermeasureRoutine());
			}
			if (burstTime > burstDuration || flightInfo.sweptRadarAltitude < gunRunMinAltitude)
			{
				break;
			}
			if ((bool)rwr && rwr.missileDetected)
			{
				SafetyOverrides();
			}
			yield return null;
		}
		UnlockReferenceTransform(RefTfLocks.Combat);
		if (fired && (bool)target)
		{
			StartCoroutine(CallAttackResultRoutine(target, estTimeToImpact + 2f, WingmanVoiceProfile.Messages.Shack, WingmanVoiceProfile.Messages.GroundMiss));
		}
		yield return StartCombatSubroutine(CombatExtendRoutine(egress, 2f));
	}

	private IEnumerator CombatSetupSurfaceAttack(Actor target, float distance, float altitude)
	{
		Vector3 vector = myTransform.position - target.position;
		vector.y = 0f;
		vector = Quaternion.AngleAxis(UnityEngine.Random.Range(-60f, 60f), Vector3.up) * vector;
		float num = target.position.y - WaterPhysics.instance.height;
		altitude += num;
		Vector3 worldPosition = PositionAtAltitudeRadar(target.position, altitude) + vector.normalized * distance;
		FixedPoint extendPoint = new FixedPoint(worldPosition);
		FixedPoint origTgtPos = new FixedPoint(target.position);
		float sqrTgtDist = distance * distance;
		float num2 = Mathf.Sign(UnityEngine.Random.Range(-1f, 1f));
		Vector3 driftVec = num2 * Vector3.Cross(Vector3.up, vector).normalized;
		float sqrMagnitude;
		while (ValidateTarget(target) && ((sqrMagnitude = VectorTo(PositionAtAltitudeASL(origTgtPos.point, altitude)).sqrMagnitude) < sqrTgtDist || sqrMagnitude > 4f * sqrTgtDist))
		{
			bool flag = autoPilot.currentSpeed > 250f || Vector3.Dot(rb.velocity.normalized, Vector3.down) > 0.268f;
			bool allowAB = autoPilot.currentSpeed < 225f || ((bool)fuelTank && fuelTank.fuelFraction > 0.5f) || maxSpeed > flightInfo.airspeed * 2f;
			Vector3 vector2 = driftVec * (sqrMagnitude / sqrTgtDist) * 500f;
			SetAutopilotParams(extendPoint.point + vector2, maxSpeed, flag ? 1f : 0.5f, allowAB);
			ApplyPlanarTurnAround();
			SafetyOverrides();
			if ((bool)target.flightInfo && !target.flightInfo.isLanded)
			{
				break;
			}
			yield return null;
		}
	}

	private void SetAutopilotParams(Vector3 targetPosition, float targetSpeed, float inputLimiter, bool allowAB)
	{
		autoPilot.targetPosition = targetPosition;
		autoPilot.targetSpeed = targetSpeed;
		autoPilot.inputLimiter = inputLimiter;
		autoPilot.throttleLimiter = 1f;
		if (!allowAB)
		{
			SetThrottleLimiterDisallowAB();
		}
	}

	private void SetThrottleLimiterDisallowAB()
	{
		if (autoPilot.engines[0].autoAB)
		{
			autoPilot.throttleLimiter = autoPilot.engines[0].autoABThreshold - 0.02f;
		}
		else
		{
			autoPilot.throttleLimiter = 1f;
		}
	}

	private IEnumerator CombatSetupSurfaceAttackDir(Actor target, float distance, float altitude, Vector3 inDirection)
	{
		inDirection.y = 0f;
		inDirection.Normalize();
		Vector3 worldPosition = PositionAtAltitudeRadar(target.position - inDirection * distance, altitude);
		FixedPoint extendPoint = new FixedPoint(worldPosition);
		while (ValidateTarget(target) && !(VectorTo(extendPoint.point).sqrMagnitude < 562500f))
		{
			Vector3 from = target.position - myTransform.position;
			from.y = 0f;
			if (from.sqrMagnitude > distance * distance && Vector3.Angle(from, inDirection) < 5f)
			{
				break;
			}
			bool flag = autoPilot.currentSpeed > 250f || flightInfo.radarAltitude < 800f || Vector3.Dot(rb.velocity.normalized, Vector3.down) > 0.268f;
			bool allowAB = !firingFlares && (autoPilot.currentSpeed < 225f || autoPilot.currentSpeed < maxSpeed / 2f || maxSpeed > 300f);
			SetAutopilotParams(extendPoint.point, maxSpeed, flag ? 1f : 0.5f, allowAB);
			if (commandState == CommandStates.Override)
			{
				AltitudeSafety();
			}
			else
			{
				SafetyOverrides();
			}
			yield return null;
		}
	}

	private IEnumerator CombatSetupSurfaceAttackDir(FixedPoint targetPos, float distance, float altitude, Vector3 inDirection, bool aslAlt = false)
	{
		inDirection.y = 0f;
		inDirection.Normalize();
		Vector3 worldPosition = ((!aslAlt) ? PositionAtAltitudeRadar(targetPos.point - inDirection * distance, altitude) : PositionAtAltitudeASL(targetPos.point - inDirection * distance, altitude));
		FixedPoint extendPoint = new FixedPoint(worldPosition);
		while (true)
		{
			Vector3 point = extendPoint.point;
			point.y = myTransform.position.y;
			if (VectorTo(point).sqrMagnitude < 250000f)
			{
				yield break;
			}
			Vector3 from = targetPos.point - myTransform.position;
			from.y = 0f;
			if (from.sqrMagnitude > distance * distance && Vector3.Angle(from, inDirection) < 5f)
			{
				yield break;
			}
			bool flag = autoPilot.currentSpeed > 250f || flightInfo.radarAltitude < 800f || Vector3.Dot(rb.velocity.normalized, Vector3.down) > 0.268f;
			bool allowAB = !firingFlares && (autoPilot.currentSpeed < 225f || autoPilot.currentSpeed < maxSpeed / 2f || maxSpeed > 300f);
			SetAutopilotParams(extendPoint.point, maxSpeed, flag ? 1f : 0.5f, allowAB);
			if (commandState == CommandStates.Override)
			{
				AltitudeSafety();
				if (cancelOverride)
				{
					break;
				}
			}
			else
			{
				SafetyOverrides();
			}
			yield return null;
		}
		ApplyQueuedCommand();
	}

	private IEnumerator CombatGroundMissileRoutine(Actor target)
	{
		wm.opticalTargeter.Unlock();
		float distToTgt = VectorTo(target.position).magnitude;
		float num = 3000f;
		if ((bool)wm.currentEquip.dlz)
		{
			DynamicLaunchZone.LaunchParams dynamicLaunchParams = wm.currentEquip.dlz.GetDynamicLaunchParams((target.position - myTransform.position).normalized * navSpeed, target.position, target.velocity);
			num = Mathf.Lerp(dynamicLaunchParams.minLaunchRange, dynamicLaunchParams.maxLaunchRange, UnityEngine.Random.Range(0.25f, 0.75f));
		}
		bool clockWise2 = Vector3.Dot(rb.velocity, Vector3.Cross(Vector3.up, myTransform.position - target.position)) > 0f;
		float attemptMaxTime = 15f;
		float attemptTime2 = 0f;
		float lastLockAttemptTime = 0f;
		float lockAttemptInterval = 1f;
		bool locked = false;
		float orbitRadius = Mathf.Clamp(distToTgt, num, wm.opticalTargeter.maxLockingDistance * 0.9f);
		if (distToTgt < num)
		{
			yield return StartCombatSubroutine(CombatSetupSurfaceAttack(target, num + 100f, defaultAltitude));
			if ((bool)target.flightInfo && !target.flightInfo.isLanded)
			{
				yield break;
			}
		}
		while (ValidateTarget(target) && (bool)wm.opticalTargeter && distToTgt > wm.opticalTargeter.maxLockingDistance)
		{
			FlyOrbit(target.transform, orbitRadius, navSpeed, defaultAltitude, clockWise2);
			SafetyOverrides();
			distToTgt = VectorTo(target.position).magnitude;
			yield return null;
		}
		if (!wm.opticalTargeter)
		{
			yield break;
		}
		FixedPoint lastTargetPosition = default(FixedPoint);
		while (ValidateTarget(target) && attemptTime2 < attemptMaxTime && !locked)
		{
			if (!wm.opticalTargeter)
			{
				yield break;
			}
			FlyOrbit(target.transform, orbitRadius, navSpeed, defaultAltitude, clockWise2);
			lastTargetPosition.point = target.position;
			if (Time.time - lastLockAttemptTime > lockAttemptInterval)
			{
				lastLockAttemptTime = Time.time;
				locked = wm.opticalTargeter.Lock(wm.opticalTargeter.cameraTransform.position, target.position - wm.opticalTargeter.cameraTransform.position);
				if (locked && (wm.opticalTargeter.lockTransform.position - target.position).sqrMagnitude > 10000f)
				{
					if (target.velocity.sqrMagnitude > 10f)
					{
						locked = false;
					}
					else
					{
						wm.opticalTargeter.AreaLockPosition(target.position);
					}
				}
			}
			SafetyOverrides();
			attemptTime2 += Time.time;
			if ((bool)target.flightInfo && !target.flightInfo.isLanded)
			{
				yield break;
			}
			yield return null;
		}
		if (locked)
		{
			OpticalMissileLauncher ml = ((HPEquipOpticalML)wm.currentEquip).oml;
			bool fired = false;
			attemptTime2 = 0f;
			float timeOnLA = 0f;
			float timeOnLALimit = UnityEngine.Random.Range(1f, 2f);
			float randRangeMult = UnityEngine.Random.Range(0.75f, 1f);
			float randLR = Mathf.Sign(UnityEngine.Random.Range(-1f, 1f));
			while (ValidateTarget(target) && attemptTime2 < attemptMaxTime && !fired)
			{
				lastTargetPosition.point = target.position;
				float magnitude = (target.position - myTransform.position).magnitude;
				float num2 = 10000f;
				if ((bool)wm.currentEquip.dlz)
				{
					num2 = wm.currentEquip.dlz.GetDynamicLaunchParams(rb.velocity, target.position, target.velocity).maxLaunchRange;
				}
				num2 *= randRangeMult;
				if (Vector3.Dot(target.position - myTransform.position, myTransform.forward) < -0.5f)
				{
					autoPilot.targetPosition = myTransform.position + randLR * 100f * Vector3.Cross(Vector3.up, myTransform.position - target.position);
					autoPilot.inputLimiter = 1f;
				}
				else
				{
					Vector3 direction = target.position + new Vector3(0f, defaultAltitude, 0f) - myTransform.position;
					direction = GetLevelFarFlightDirection(direction, num2 * num2);
					if (!(magnitude > num2))
					{
						ml.boresightFOVFraction = 0.5f;
						float num3 = 0.8f * ml.boresightFOVFraction * ml.GetNextMissile().opticalFOV / 2f;
						direction = Vector3.RotateTowards(target.position - myTransform.position, direction, num3 * ((float)Math.PI / 180f), 0f);
					}
					autoPilot.targetPosition = myTransform.position + direction;
					ApplyPlanarTurnAround();
				}
				if (ml.targetLocked && magnitude < num2)
				{
					if ((bool)aiWing && aiWing.GetNumMissilesOnTarget(target) >= aiWing.maxMissilePerTarget)
					{
						float waitStart = Time.time;
						float waitTime = UnityEngine.Random.Range(1f, 2f);
						float targetDist = VectorTo(target.position).magnitude;
						while (ValidateTarget(target) && Time.time - waitStart < waitTime)
						{
							FlyOrbit(target.transform, targetDist, maxSpeed, defaultAltitude, orbitClockwise);
							SafetyOverrides();
							yield return null;
						}
						yield break;
					}
					timeOnLA += Time.deltaTime;
					if (!ValidateTarget(target))
					{
						yield break;
					}
					if (timeOnLA > timeOnLALimit)
					{
						BeginDeploySafety();
						if (!wm.opticalTargeter.Lock(target.position))
						{
							yield break;
						}
						wm.SingleFire();
						yield return null;
						fired = true;
						if ((bool)aiWing)
						{
							aiWing.ReportMissileOnTarget(actor, target, wm.lastFiredMissile);
						}
						StartCoroutine(CallMissileResultRoutine(target, wm.lastFiredMissile, WingmanVoiceProfile.Messages.Shack, WingmanVoiceProfile.Messages.GroundMiss));
						PlayRadioMessage(WingmanVoiceProfile.Messages.Rifle);
					}
				}
				autoPilot.targetSpeed = maxSpeed;
				SafetyOverrides();
				attemptTime2 += Time.deltaTime;
				yield return null;
			}
			if (fired)
			{
				yield return StartCombatSubroutine(CombatPostMissileBreakRoutine(lastTargetPosition.point));
				clockWise2 = Vector3.Dot(rb.velocity, Vector3.Cross(Vector3.up, myTransform.position - lastTargetPosition.point)) > 0f;
				bool reLockRequired = false;
				while (ValidateTarget(target) && (bool)wm.lastFiredMissile && wm.lastFiredMissile.hasTarget)
				{
					FlyOrbit(target.transform, orbitRadius, navSpeed, defaultAltitude, clockWise2);
					if (!wm.lastFiredMissile.opticalFAF)
					{
						Vector3 target2 = autoPilot.targetPosition - myTransform.position;
						target2 = Vector3.RotateTowards(target.position - myTransform.position, target2, (float)Math.PI / 3f, float.MaxValue);
						autoPilot.targetPosition = myTransform.position + target2;
						if (wm.opticalTargeter.isGimbalLimit || wm.opticalTargeter.lockedActor != target)
						{
							reLockRequired = true;
						}
						if (reLockRequired && !wm.opticalTargeter.isGimbalLimit)
						{
							reLockRequired = !wm.opticalTargeter.Lock(target.position);
						}
					}
					SafetyOverrides();
					yield return null;
				}
			}
		}
		wm.opticalTargeter.Unlock();
	}

	private IEnumerator CombatPostMissileBreakRoutine(Vector3 targetPosition, bool flares = true, bool chaff = true)
	{
		yield return StartCombatSubroutine(CombatExtendRoutine(targetPosition - myTransform.position, UnityEngine.Random.Range(1f, 2.5f)));
		Vector3 vector = Vector3.Cross(Vector3.up, VectorTo(targetPosition));
		Vector3 direction = UnityEngine.Random.Range(-1f, 1f) * vector;
		if (!firingCms)
		{
			StartCoroutine(CountermeasureRoutine(flares, chaff));
		}
		yield return StartCombatSubroutine(CombatExtendRoutine(direction, UnityEngine.Random.Range(1f, 4f)));
	}

	private IEnumerator CombatPostMissileBrakeRoutineDir(Vector3 egressDir, bool flares = true, bool chaff = true)
	{
		Vector3 direction = PlanarDirection(rb.velocity);
		yield return StartCombatSubroutine(CombatExtendRoutine(direction, UnityEngine.Random.Range(1f, 2.5f)));
		if (!firingCms)
		{
			StartCoroutine(CountermeasureRoutine(flares, chaff));
		}
		yield return StartCombatSubroutine(CombatExtendRoutine(egressDir, UnityEngine.Random.Range(1f, 4f)));
	}

	private IEnumerator CombatAntiRadMissileRoutine(Actor target)
	{
		HPEquipARML antiRadEquip = (HPEquipARML)wm.currentEquip;
		antiRadEquip.targetActor = target;
		Missile component = antiRadEquip.ml.missilePrefab.GetComponent<Missile>();
		ModuleRWR mRWR = component.antiRadRWR;
		float num = 0f;
		int num2 = 0;
		foreach (Radar radar in target.GetRadars())
		{
			if ((bool)radar && radar.radarEnabled)
			{
				num += radar.transmissionStrength;
				num2++;
			}
		}
		if (num2 == 0)
		{
			yield break;
		}
		num /= (float)num2;
		float estRadarRangeSqr = num * 100f * mRWR.receiverSensitivity;
		if (antiRadEquip.LaunchAuthorized())
		{
			if ((bool)aiWing && aiWing.GetNumMissilesOnTarget(target) >= aiWing.maxMissilePerTarget)
			{
				wm.opticalTargeter.Unlock();
				yield break;
			}
			wm.SingleFire();
			BeginDeploySafety();
			yield return null;
			Missile lastFiredMissile = wm.lastFiredMissile;
			if ((bool)lastFiredMissile && (bool)aiWing)
			{
				aiWing.ReportMissileOnTarget(actor, target, lastFiredMissile);
			}
			PlayRadioMessage(WingmanVoiceProfile.Messages.Magnum, 5f);
			if ((bool)target)
			{
				yield return StartCombatSubroutine(CombatPostMissileBreakRoutine(target.position, flares: false));
			}
		}
		if (VectorTo(target.position).magnitude < Mathf.Lerp(antiRadEquip.dlz.launchParams.minLaunchRange, antiRadEquip.dlz.launchParams.maxLaunchRange, 0.15f))
		{
			yield return StartCombatSubroutine(CombatSetupSurfaceAttack(target, antiRadEquip.dlz.maxLaunchRange, defaultAltitude));
		}
		bool waitClockwise = UnityEngine.Random.Range(0, 10) % 2 == 0;
		while (((bool)wm.lastFiredMissile && wm.lastFiredMissile.guidanceMode == Missile.GuidanceModes.AntiRad) || ((bool)aiWing && aiWing.GetNumMissilesOnTarget(target) > 0))
		{
			if (!ValidateTarget(target))
			{
				yield break;
			}
			FlyOrbit(target.position, antiRadEquip.dlz.maxLaunchRange, navSpeed, defaultAltitude, waitClockwise);
			SafetyOverrides();
			yield return null;
		}
		float attemptMaxTime = 10f;
		bool fired = false;
		bool aborted = false;
		float startTime = Time.time;
		while (ValidateTarget(target) && Time.time - startTime < attemptMaxTime && !fired && !aborted)
		{
			float planarSqrDist = PlanarVectorTo(target.position).sqrMagnitude;
			if (planarSqrDist > estRadarRangeSqr)
			{
				startTime = Time.time;
			}
			if (Vector3.Angle(PlanarVectorTo(target.position), PlanarDirection(myTransform.forward)) < 5f)
			{
				if (antiRadEquip.LaunchAuthorized())
				{
					if (((bool)wm.lastFiredMissile && wm.lastFiredMissile.guidanceMode == Missile.GuidanceModes.AntiRad) || ((bool)aiWing && aiWing.GetNumMissilesOnTarget(target) > 1))
					{
						yield return StartCombatSubroutine(CombatPostMissileBreakRoutine(target.position, flares: false, chaff: false));
						yield break;
					}
					wm.SingleFire();
					BeginDeploySafety();
					fired = true;
					yield return null;
					Missile lastFiredMissile2 = wm.lastFiredMissile;
					if ((bool)lastFiredMissile2 && (bool)aiWing)
					{
						aiWing.ReportMissileOnTarget(actor, target, lastFiredMissile2);
					}
					PlayRadioMessage(WingmanVoiceProfile.Messages.Magnum, 5f);
				}
			}
			else
			{
				startTime = Time.time;
			}
			float minLaunchRange = antiRadEquip.dlz.launchParams.minLaunchRange;
			if (planarSqrDist < minLaunchRange * minLaunchRange)
			{
				aborted = true;
			}
			Vector3 vector = VectorToTargetWithAngleLimit(PositionAtAltitudeRadar(target.position, defaultAltitude), target.position, mRWR.antennaFov * 0.45f);
			autoPilot.targetPosition = myTransform.position + vector;
			autoPilot.targetSpeed = maxSpeed;
			SafetyOverrides();
			yield return null;
		}
		if ((fired || aborted) && (bool)target)
		{
			yield return StartCombatSubroutine(CombatPostMissileBreakRoutine(target.position, flares: false));
		}
	}

	private IEnumerator CombatRegainSpeedRoutine()
	{
		float regainSpeedTime = 0f;
		while (autoPilot.currentSpeed < minCombatSpeed * 2f)
		{
			float num = myTransform.position.y - WaterPhysics.instance.height;
			Vector3 vector;
			if ((bool)attackTarget)
			{
				Vector3 normalized = (myTransform.position - attackTarget.position).normalized;
				Vector3 forward = attackTarget.transform.forward;
				vector = ((!(Vector3.Dot(normalized, forward) > 0f)) ? (-normalized) : Vector3.Slerp((normalized - forward).normalized, myTransform.forward, Vector3.Angle(forward, normalized) / 10f));
			}
			else
			{
				vector = Vector3.ProjectOnPlane(rb.velocity, Vector3.up).normalized;
			}
			vector.y = 0f;
			Vector3 direction = vector;
			vector = Quaternion.AngleAxis(Mathf.Lerp(0f, 90f, (num - minAltitude + minAltClimbThresh) / 4000f), Vector3.Cross(Vector3.up, vector)) * vector;
			autoPilot.targetPosition = myTransform.position + vector.normalized * 1000f;
			autoPilot.targetSpeed = maxSpeed;
			regainSpeedTime += Time.deltaTime;
			if (regainSpeedTime > 10f)
			{
				yield return StartCombatSubroutine(CombatExtendRoutine(direction, UnityEngine.Random.Range(2f, 6f)));
				regainSpeedTime = 0f;
			}
			yield return null;
		}
	}

	private bool SteerAvoidActor(Actor avoidActor, out Vector3 avoidDirection, float sqrAvoidRadius = 2500f)
	{
		float num = 3f;
		float num2 = 0.08f;
		Vector3 position = myTransform.position;
		Vector3 velocity = rb.velocity;
		Vector3 position2 = avoidActor.position;
		Vector3 velocity2 = avoidActor.velocity;
		for (float num3 = num2; num3 <= num; num3 += num2)
		{
			Vector3 vector = position + velocity * num3;
			Vector3 vector2 = position2 + velocity2 * num3;
			if ((vector - vector2).sqrMagnitude < sqrAvoidRadius)
			{
				avoidDirection = Vector3.ProjectOnPlane(position - position2, velocity).normalized;
				return true;
			}
		}
		avoidDirection = Vector3.zero;
		return false;
	}

	private IEnumerator CombatRoutine()
	{
		FixedPoint lastTargetPosition = new FixedPoint(orbitTransform.position);
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetAllLights(0);
		}
		if ((bool)wm.opticalTargeter)
		{
			wm.opticalTargeter.visibleLaser = true;
		}
		while (base.enabled)
		{
			float noTargetTime = 0f;
			while (!attackTarget)
			{
				if ((!attackTarget && autoEngageEnemies) || ((bool)overrideAttackTarget && overrideAttackTarget != attackTarget))
				{
					UpdateTargets();
				}
				if (!attackTarget)
				{
					FlyOrbit(lastTargetPosition.point, orbitRadius, maxSpeed, defaultAltitude, orbitClockwise);
					SafetyOverrides();
					noTargetTime += Time.deltaTime;
					yield return null;
					if (noTargetTime > 10f || !autoEngageEnemies)
					{
						ApplyQueuedCommand();
						yield break;
					}
					if (rtbAvailable && (bool)aiSpawn && aiSpawn.autoRTB && CheckWinchester() && (rtbAvailable = aiSpawn.CommandRTB()))
					{
						rearmAfterLanding = true;
						PlayRadioMessage(WingmanVoiceProfile.Messages.ReturningToBase);
					}
				}
			}
			lastTargetPosition.point = attackTarget.position;
			BDCoroutine decideRoutine = StartCombatSubroutine(DecideCombatAction(attackTarget));
			while (ValidateTarget(attackTarget) && decideRoutine.keepWaiting)
			{
				yield return null;
			}
			if ((bool)attackTarget && attackTarget.alive && (bool)aiWing)
			{
				aiWing.ReportTarget(actor, attackTarget, AIWing.DetectionMethods.Visual);
			}
			ResetAttackTarget();
			KillCombatDecisionRoutines();
			Disarm();
			yield return null;
		}
	}

	private bool CheckWinchester()
	{
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip && equip.armable && equip.GetCount() > 0)
			{
				return false;
			}
		}
		return true;
	}

	private BDCoroutine StartCombatSubroutine(IEnumerator routine)
	{
		BDCoroutine bDCoroutine = new BDCoroutine(routine, this);
		combatDecisionRoutines.Add(bDCoroutine);
		bDCoroutine.OnCoroutineFinished += OnSubroutineFinished;
		return bDCoroutine;
	}

	private void OnSubroutineFinished(BDCoroutine br)
	{
		if (combatDecisionRoutines != null)
		{
			combatDecisionRoutines.Remove(br);
		}
	}

	private IEnumerator CombatFlyToPositionForGroundRoutine(Actor target, Vector3 worldPosition, float altitude, float radius, float speed)
	{
		FixedPoint fp = new FixedPoint(PositionAtAltitudeRadar(worldPosition, altitude));
		float sqrRad = radius * radius;
		while (ValidateTarget(target) && VectorTo(fp.point).sqrMagnitude > sqrRad)
		{
			autoPilot.targetPosition = fp.point + (altitude - flightInfo.altitudeASL) * Vector3.up;
			autoPilot.targetSpeed = speed;
			SafetyOverrides();
			if ((bool)target.flightInfo && !target.flightInfo.isLanded)
			{
				break;
			}
			yield return null;
		}
	}

	private void GetAttackVectors(Vector3 targetPos, float attackRange, out Vector3 ingress, out Vector3 egress)
	{
		if ((bool)aiWing)
		{
			aiWing.GetGroundAttackVectors(myTransform.position, targetPos, attackRange, designatedTargets, moduleRWR ? moduleRWR.GetDetectedActors(TeamOptions.OtherTeam) : null, out ingress, out egress);
		}
		else if ((bool)targetFinder)
		{
			List<List<Actor>> list = new List<List<Actor>>();
			list.Add(targetFinder.targetsSeen);
			list.Add(designatedTargets);
			if ((bool)detectionRadar)
			{
				list.Add(detectionRadar.detectedUnits);
			}
			if ((bool)moduleRWR)
			{
				list.Add(moduleRWR.GetDetectedActors(TeamOptions.OtherTeam));
			}
			AIWing.GetGroundAttackVectors(myTransform.position, targetPos, attackRange, list.ToArray(), out ingress, out egress);
		}
		else
		{
			ingress = PlanarDirection(targetPos - myTransform.position);
			egress = ingress;
		}
	}

	private void Disarm()
	{
		if (!wm)
		{
			return;
		}
		if (wm.isMasterArmed)
		{
			wm.ToggleMasterArmed();
		}
		InternalWeaponBay[] internalWeaponBays = wm.internalWeaponBays;
		foreach (InternalWeaponBay internalWeaponBay in internalWeaponBays)
		{
			if (internalWeaponBay.externallyControlled)
			{
				internalWeaponBay.UnregisterOpenReq(this);
			}
		}
		if ((bool)wm.opticalTargeter)
		{
			wm.opticalTargeter.Unlock();
			wm.opticalTargeter.visibleLaser = false;
		}
	}

	private bool ValidateTarget(Actor target)
	{
		if (target != null && target.alive)
		{
			return target == attackTarget;
		}
		return false;
	}

	private void KillCombatDecisionRoutines()
	{
		foreach (BDCoroutine combatDecisionRoutine in combatDecisionRoutines)
		{
			if (combatDecisionRoutine != null)
			{
				_ = combatDecisionRoutine.Current;
				combatDecisionRoutine.StopCoroutine();
			}
		}
		combatDecisionRoutines = new List<BDCoroutine>();
		UnlockReferenceTransform(RefTfLocks.Combat);
		if ((bool)wm && wm.internalWeaponBays != null)
		{
			InternalWeaponBay[] internalWeaponBays = wm.internalWeaponBays;
			for (int i = 0; i < internalWeaponBays.Length; i++)
			{
				internalWeaponBays[i].UnregisterOpenReq(this);
			}
		}
	}

	private void ResetAttackTarget()
	{
		if (attackTarget != null)
		{
			if ((bool)aiWing)
			{
				aiWing.ReportDisengageTarget(attackTarget);
			}
			attackTarget = null;
			actor.currentlyTargetingActor = null;
		}
	}

	private void JettisonTanks()
	{
		if (_jettisonedTanks)
		{
			return;
		}
		_jettisonedTanks = true;
		if (!wm)
		{
			return;
		}
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip)
			{
				if (equip is HPEquipDropTank)
				{
					equip.markedForJettison = true;
					BeginDeploySafety();
				}
				else
				{
					equip.markedForJettison = false;
				}
			}
		}
		wm.JettisonMarkedItems();
	}

	private Vector3 VectorTo(Vector3 position)
	{
		return position - myTransform.position;
	}

	private Vector3 PlanarVectorTo(Vector3 position)
	{
		Vector3 result = position - myTransform.position;
		result.y = 0f;
		return result;
	}

	private Vector3 PlanarDirection(Vector3 dir)
	{
		dir.y = 0f;
		return dir.normalized;
	}

	private Vector3 PositionAtAltitudeASL(Vector3 worldPos, float altitude)
	{
		worldPos.y = WaterPhysics.instance.height + altitude;
		return worldPos;
	}

	private Vector3 PositionAtAltitudeRadar(Vector3 worldPos, float altitude)
	{
		Vector3 vector = PositionAtAltitudeASL(worldPos, 0f);
		if ((bool)VTMapGenerator.fetch)
		{
			float heightmapAltitude = VTMapGenerator.fetch.GetHeightmapAltitude(worldPos);
			return vector + new Vector3(0f, heightmapAltitude + altitude, 0f);
		}
		if (Physics.Raycast(vector + new Vector3(0f, 10000f, 0f), Vector3.down, out var hitInfo, 10000f, 1))
		{
			return hitInfo.point + new Vector3(0f, altitude, 0f);
		}
		return vector + new Vector3(0f, altitude, 0f);
	}

	private float GetRadarAltitudeAtPosition(Vector3 worldPos)
	{
		Vector3 vector = PositionAtAltitudeRadar(worldPos, 0f);
		return worldPos.y - vector.y;
	}

	private void SetReferenceTransform(Transform rTf, RefTfLocks lockID)
	{
		if (refTfLockID == RefTfLocks.None)
		{
			refTfLockID = lockID;
			autoPilot.referenceTransform = rTf;
		}
	}

	private void UnlockReferenceTransform(RefTfLocks lockID)
	{
		if (refTfLockID != 0 && lockID == refTfLockID)
		{
			refTfLockID = RefTfLocks.None;
			autoPilot.referenceTransform = myTransform;
		}
	}

	public void PlayRadioMessage(WingmanVoiceProfile.Messages messageName, float cooldown = 10f, float delay = -1f)
	{
		if (doRadioComms && base.gameObject.activeInHierarchy)
		{
			if (delay > 0f)
			{
				StartCoroutine(DelayedRadioMessage(messageName, cooldown, delay));
			}
			else
			{
				CommRadioManager.instance.PlayWingmanMessage(messageName, voiceProfile, cooldown);
			}
		}
	}

	private IEnumerator DelayedRadioMessage(WingmanVoiceProfile.Messages messageName, float cooldown, float delay)
	{
		yield return new WaitForSeconds(delay);
		CommRadioManager.instance.PlayWingmanMessage(messageName, voiceProfile, cooldown);
	}

	private IEnumerator LandOnPadRoutine(LandOnPadStates resumeState, Transform padTf, bool shutoffEngine, bool resetAtParking, AirportManager.ParkingSpace parkingSpace, float inHeading = -1f, float landFacing = -1f)
	{
		if (landOnPadState != 0)
		{
			Debug.LogErrorFormat("{0}: Entered LandOnPadRoutine but landOnPadState != None.  Aborting.", actor.DebugName());
			yield break;
		}
		commandState = CommandStates.Override;
		vtolManeuvering = true;
		landOnPadTf = padTf;
		landOnPadInHeading = inHeading;
		AirportManager ap = padTf.GetComponentInParent<AirportManager>();
		if ((bool)ap)
		{
			apLandOnPadIdx = ap.landingPads.IndexOf(padTf);
		}
		else
		{
			apLandOnPadIdx = -1;
		}
		Actor padActor = padTf.GetComponentInParent<Actor>();
		AICarrierSpawn cSpawn = (landOnPadCSpawn = padTf.GetComponentInParent<AICarrierSpawn>());
		LandingPadToParkingRoute parkingRoute = padTf.GetComponent<LandingPadToParkingRoute>();
		if ((bool)parkingSpace)
		{
			parkingSpace.OccupyParking(actor);
			actor.SetAutoUnoccupyParking(b: false);
			landingParkingSpace = parkingSpace;
		}
		if (resumeState == LandOnPadStates.None || resumeState == LandOnPadStates.PreApproach)
		{
			resumeState = LandOnPadStates.None;
			landOnPadState = LandOnPadStates.PreApproach;
			yield return StartCoroutine(LandOnPadPreApproachRoutine(padTf, cSpawn, inHeading));
		}
		if (landOnPadState == LandOnPadStates.None)
		{
			actor.SetAutoUnoccupyParking(b: true);
			vtolManeuvering = false;
			ApplyQueuedCommand();
			StartCoroutine(RetryLandingAfterVTOL());
			yield break;
		}
		if (resumeState == LandOnPadStates.None || resumeState == LandOnPadStates.Approach)
		{
			resumeState = LandOnPadStates.None;
			landOnPadState = LandOnPadStates.Approach;
			bool aborted = false;
			yield return StartCoroutine(LandOnPadApproachRoutine(padTf, cSpawn, delegate
			{
				aborted = true;
			}));
			if (aborted)
			{
				landOnPadState = LandOnPadStates.None;
				actor.SetAutoUnoccupyParking(b: true);
				vtolManeuvering = false;
				ApplyQueuedCommand();
				StartCoroutine(RetryLandingAfterVTOL());
				yield break;
			}
		}
		if (resumeState == LandOnPadStates.None || resumeState == LandOnPadStates.Transition)
		{
			resumeState = LandOnPadStates.None;
			landOnPadState = LandOnPadStates.Transition;
			yield return StartCoroutine(LandOnPadTransitionRoutine(padTf, cSpawn));
		}
		if (landOnPadState == LandOnPadStates.None)
		{
			actor.SetAutoUnoccupyParking(b: true);
			vtolManeuvering = false;
			TakeOffVTOL(flightInfo.heading);
			StartCoroutine(RetryLandingAfterVTOL());
			yield break;
		}
		if (resumeState == LandOnPadStates.None || resumeState == LandOnPadStates.RailLanding)
		{
			resumeState = LandOnPadStates.None;
			landOnPadState = LandOnPadStates.RailLanding;
			yield return StartCoroutine(LandOnPadRailRoutine(padTf, padActor, inHeading, landFacing));
		}
		if (landOnPadState == LandOnPadStates.None)
		{
			actor.SetAutoUnoccupyParking(b: true);
			vtolManeuvering = false;
			TakeOffVTOL(flightInfo.heading);
			StartCoroutine(RetryLandingAfterVTOL());
			yield break;
		}
		vtolAp.enabled = false;
		autoPilot.controlThrottle = true;
		for (int i = 0; i < vtolAp.engines.Length; i++)
		{
			vtolAp.engines[i].autoAB = true;
		}
		actor.SetAutoUnoccupyParking(b: true);
		if ((bool)cSpawn && (bool)ap)
		{
			currentCarrier = cSpawn;
			currentCarrierSpawnIdx = ap.GetCarrierSpawnIdx(padTf);
		}
		float t = Time.time;
		while (Time.time - t < 5f)
		{
			autoPilot.targetSpeed = 0f;
			commandState = CommandStates.Override;
			yield return null;
		}
		vtolManeuvering = false;
		if ((bool)wingRotator && (bool)cSpawn)
		{
			wingRotator.SetDeployed();
		}
		if (retractTiltAfterLand)
		{
			while (tiltController.currentTilt < 90f)
			{
				tiltController.PadInput(Vector3.up);
				yield return null;
			}
		}
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetAllLights(0);
		}
		Transform rearmPt = padTf;
		if ((bool)parkingRoute && (resumeState == LandOnPadStates.None || resumeState == LandOnPadStates.Taxiing))
		{
			resumeState = LandOnPadStates.None;
			landOnPadState = LandOnPadStates.Taxiing;
			rearmPt = parkingRoute.parkingNode.transform;
			if (ap.isCarrier)
			{
				taxiSpeed = carrierTaxiSpeed;
			}
			yield return StartCoroutine(LandOnPadTaxiRoutine(parkingRoute));
		}
		actor.SetAutoUnoccupyParking(b: true);
		commandState = CommandStates.Park;
		if (shutoffEngine)
		{
			yield return oneSecWait;
			foreach (ModuleEngine engine in autoPilot.engines)
			{
				engine.SetPower(0);
			}
		}
		else if (resetAtParking)
		{
			ResetAtParking(cSpawn, rearmPt);
		}
		else if (takeOffAfterLanding)
		{
			aiSpawn.TakeOff();
			takeOffAfterLanding = false;
		}
		landOnPadState = LandOnPadStates.None;
	}

	private IEnumerator RetryLandingAfterVTOL()
	{
		while (vtolManeuvering)
		{
			yield return null;
		}
	}

	private IEnumerator LandOnPadPreApproachRoutine(Transform padTf, AICarrierSpawn cSpawn, float inHeading = -1f)
	{
		while (takeOffState != 0 || ctoState != 0 || isTakingOffVtol)
		{
			yield return null;
		}
		if ((bool)cSpawn)
		{
			landOnPadStartApproachPt = new FixedPoint(cSpawn.actor.position - cSpawn.transform.forward * 4000f);
		}
		else if (inHeading >= 0f)
		{
			Vector3 vector = VectorUtils.BearingVector(inHeading);
			landOnPadStartApproachPt = new FixedPoint(padTf.position - vector * 3000f);
		}
		else
		{
			Vector3 vector2 = myTransform.position - padTf.position;
			vector2.y = 0f;
			vector2.Normalize();
			Vector3 vector3 = padTf.position;
			for (float num = 0f; num < 359f; num += 30f)
			{
				Vector3 vector4 = Quaternion.AngleAxis(num, Vector3.up) * vector2;
				vector3 = padTf.position + vector4 * 3000f + new Vector3(0f, 750f, 0f);
				if (!Physics.Linecast(padTf.position + 50f * Vector3.up, vector3, 1))
				{
					num = 400f;
				}
			}
			landOnPadStartApproachPt = new FixedPoint(vector3);
		}
		float maxTerrainAlt = GetMaxTerrainAlt(padTf.position, landOnPadStartApproachPt.point);
		float y = WaterPhysics.instance.height + (maxTerrainAlt + 200f);
		Vector3 point = landOnPadStartApproachPt.point;
		point.y = y;
		landOnPadStartApproachPt = new FixedPoint(point);
		while (Vector3.Dot(rb.velocity, landOnPadStartApproachPt.point - myTransform.position) < 0f)
		{
			Vector3 point2 = landOnPadStartApproachPt.point;
			point2.y = myTransform.position.y;
			autoPilot.targetPosition = point2;
			autoPilot.targetSpeed = 260f;
			AltitudeSafety(200f);
			if ((bool)cSpawn && !cSpawn.actor.alive)
			{
				Debug.LogFormat("{0} - target carrier died while landing vertically ({1})", actor.DebugName(), landOnPadState);
				landOnPadState = LandOnPadStates.None;
				yield break;
			}
			yield return null;
		}
		while (Vector3.Dot(rb.velocity, landOnPadStartApproachPt.point - myTransform.position) > 0f)
		{
			Vector3 vector5 = landOnPadStartApproachPt.point - myTransform.position;
			Vector3 current = vector5;
			current.y = 0f;
			vector5 = Vector3.RotateTowards(current, vector5, 0.34906584f, 0f);
			autoPilot.targetPosition = myTransform.position + vector5;
			float sqrMagnitude = (padTf.position - myTransform.position).sqrMagnitude;
			autoPilot.targetSpeed = Mathf.Lerp(150f, 260f, sqrMagnitude / 8000f);
			AltitudeSafety(100f);
			if ((bool)cSpawn && !cSpawn.actor.alive)
			{
				Debug.LogFormat("{0} - target carrier died while landing vertically ({1})", actor.DebugName(), landOnPadState);
				landOnPadState = LandOnPadStates.None;
				break;
			}
			yield return null;
		}
	}

	private float GetMaxTerrainAlt(Vector3 startPt, Vector3 endPt)
	{
		float num = 0f;
		float maxDelta = (startPt - endPt).magnitude / 150f;
		for (float num2 = 0f; num2 < 1f; num2 = Mathf.MoveTowards(num2, 1f, maxDelta))
		{
			Vector3 vector = Vector3.Lerp(startPt, endPt, num2);
			RaycastHit hitInfo;
			if ((bool)VTMapGenerator.fetch)
			{
				num = Mathf.Max(num, VTMapGenerator.fetch.GetHeightmapAltitude(vector));
			}
			else if (Physics.Raycast(PositionAtAltitudeASL(vector, 0f) + new Vector3(0f, 10000f, 0f), Vector3.down, out hitInfo, 10000f, 1))
			{
				num = Mathf.Max(num, WaterPhysics.GetAltitude(hitInfo.point));
			}
		}
		return num;
	}

	private IEnumerator LandOnPadApproachRoutine(Transform padTf, AICarrierSpawn cSpawn, Action onAborted)
	{
		bool readyToLand = false;
		while (!readyToLand)
		{
			commandState = CommandStates.Override;
			vtolManeuvering = true;
			Vector3 vector = padTf.position + 150f * Vector3.up;
			autoPilot.targetPosition = vector;
			if ((bool)cSpawn && !cSpawn.actor.alive)
			{
				Debug.LogFormat("{0} - target carrier died while landing vertically ({1})", actor.DebugName(), landOnPadState);
				landOnPadState = LandOnPadStates.None;
				onAborted?.Invoke();
				break;
			}
			float magnitude = (vector - myTransform.position).magnitude;
			if (magnitude < 2000f && Vector3.Dot(vector - myTransform.position, myTransform.forward) < 0f)
			{
				Vector3 worldPos = vector + (myTransform.position - vector).normalized * 3000f;
				autoPilot.targetPosition = PositionAtAltitudeRadar(worldPos, 500f);
				autoPilot.targetSpeed = 150f;
			}
			else if (magnitude > 8000f)
			{
				autoPilot.targetSpeed = 260f;
			}
			else if (magnitude > 1700f)
			{
				autoPilot.targetSpeed = Mathf.Lerp(200f, 80f, Mathf.InverseLerp(8000f, 1700f, magnitude));
				if ((bool)extLightsCtrlr)
				{
					extLightsCtrlr.SetNavLights(1);
					extLightsCtrlr.SetStrobeLights(1);
				}
			}
			else
			{
				List<Actor> list = landOnPadActorBuffer;
				Actor.GetActorsInRadius(padTf.position, actor.physicalRadius * 1.5f, Teams.Allied, TeamOptions.BothTeams, list);
				readyToLand = true;
				foreach (Actor item in list)
				{
					if (item != actor && item.alive && item.role == Actor.Roles.Air)
					{
						readyToLand = false;
					}
				}
				if (!readyToLand)
				{
					onAborted?.Invoke();
					break;
				}
			}
			ApplyPlanarTurnAround(0.9f);
			Vector3 forward = myTransform.forward;
			Vector3 vector2 = vector - myTransform.position;
			forward.y = (vector2.y = 0f);
			AltitudeSafety(100f);
			yield return null;
		}
	}

	private IEnumerator LandOnPadTransitionRoutine(Transform padTf, AICarrierSpawn cSpawn)
	{
		while (tiltController.currentTilt > 0f)
		{
			if ((bool)cSpawn && !cSpawn.actor.alive)
			{
				Debug.LogFormat("{0} - target carrier died while landing vertically ({1})", actor.DebugName(), landOnPadState);
				landOnPadState = LandOnPadStates.None;
				break;
			}
			commandState = CommandStates.Override;
			vtolManeuvering = true;
			tiltController.PadInput(Vector3.down);
			autoPilot.targetSpeed = 0f;
			autoPilot.targetPosition = padTf.position + 150f * Vector3.up;
			yield return null;
		}
	}

	private IEnumerator LandOnPadRailRoutine(Transform padTf, Actor padActor, float inHeading, float landFacing)
	{
		kPlane.SetToDynamic();
		gearAnimator.Extend();
		Vector3 velocity = rb.velocity;
		if ((bool)padActor)
		{
			velocity -= padActor.velocity;
		}
		float startSpeed = velocity.magnitude;
		if (landOnPadCurveT < 0f)
		{
			landOnPadStartSpeed = startSpeed;
			Vector3 arg = padTf.InverseTransformPoint(myTransform.position);
			Vector3 vector = padTf.InverseTransformPoint(myTransform.position + velocity * 3f);
			Vector3 zero = Vector3.zero;
			if ((bool)aiSpawn)
			{
				zero += aiSpawn.heightFromSurface * Vector3.up;
			}
			else
			{
				RaySpringDamper raySpringDamper = flightInfo.wheelsController.suspensions[0];
				float num = 0f - base.transform.InverseTransformPoint(raySpringDamper.transform.position - raySpringDamper.suspensionDistance * raySpringDamper.transform.up).y;
				zero += num * Vector3.up;
			}
			Vector3 vector2 = 75f * Vector3.up;
			Vector3 arg2 = Vector3.Lerp(vector, vector2, 0.5f);
			if ((bool)padActor)
			{
				arg2 = vector2 + 200f * Mathf.Sign(Vector3.Dot(padTf.position - padActor.position, padActor.transform.right)) * padTf.InverseTransformDirection(padActor.transform.right);
			}
			Func<Vector3, Vector3D> func = (Vector3 pt) => new Vector3D(pt);
			landOnPadCurve = new BezierCurveD5(func(arg), func(vector), func(arg2), func(vector2), func(zero));
			landOnPadCurveT = 0f;
		}
		else
		{
			startSpeed = landOnPadStartSpeed;
			Debug.LogFormat("{0} is resuming LandOnPadRailRoutine at T: {1}", actor.DebugName(), landOnPadCurveT);
		}
		Func<float, Vector3> curvePoint = (float in_t) => padTf.TransformPoint(landOnPadCurve.GetPoint(in_t).toVector3);
		Vector3 accel = Vector3.zero;
		_ = myTransform.position;
		Vector3 prevV = rb.velocity;
		if ((bool)padActor)
		{
			prevV += padActor.velocity;
		}
		Vector3 startFwd = Vector3.ProjectOnPlane(myTransform.forward, Vector3.up).normalized;
		Quaternion tgtRot = myTransform.rotation;
		while (landOnPadCurveT < 1f && !flightInfo.isLanded)
		{
			if ((bool)padActor && !padActor.alive)
			{
				Debug.LogFormat("{0} - target carrier died while landing vertically ({1})", actor.DebugName(), landOnPadState);
				landOnPadState = LandOnPadStates.None;
				yield break;
			}
			Vector3 position = curvePoint(landOnPadCurveT);
			rb.MovePosition(position);
			Vector3 vector3 = curvePoint(landOnPadCurveT + 0.01f) - curvePoint(Mathf.Min(landOnPadCurveT, 0.99f));
			float num2 = vector3.magnitude / 0.01f;
			vector3.Normalize();
			float num3 = Mathf.Lerp(startSpeed, 2f, Mathf.Pow(landOnPadCurveT, 1.65f) * 1.05f);
			float num4 = num3 / num2;
			Vector3 vector4 = num3 * vector3;
			if ((bool)padActor)
			{
				vector4 += padActor.velocity;
			}
			rb.velocity = vector4;
			if (Time.fixedDeltaTime > 0f)
			{
				accel = (vector4 - prevV) / Time.fixedDeltaTime;
			}
			prevV = vector4;
			Vector3 a = new Vector3(0f, 9.81f, 0f) + accel;
			a.y = Mathf.Max(a.y, 0f);
			a = Vector3.Slerp(a, Vector3.up, Mathf.InverseLerp(0.97f, 0.995f, landOnPadCurveT));
			Vector3 b = ((inHeading < 0f) ? padTf.forward : Vector3.ProjectOnPlane(VectorUtils.BearingVector(inHeading), padTf.up));
			if (landFacing >= 0f)
			{
				b = Vector3.ProjectOnPlane(VectorUtils.BearingVector(landFacing), padTf.up);
			}
			Vector3 forward = Vector3.ProjectOnPlane(Vector3.Slerp(startFwd, b, Mathf.Pow(landOnPadCurveT * 1.05f, 2f)), a);
			tgtRot = Quaternion.RotateTowards(tgtRot, Quaternion.LookRotation(forward, a), 90f * Time.fixedDeltaTime);
			Quaternion rot = Quaternion.Slerp(myTransform.rotation, tgtRot, 2f * Time.fixedDeltaTime);
			rb.MoveRotation(rot);
			rb.angularVelocity = Vector3.zero;
			float num5 = Vector3.Dot(accel + new Vector3(0f, 9.81f, 0f), myTransform.up);
			float num6 = maxThrust / rb.mass;
			float a2 = num5 / num6;
			autoPilot.OverrideSetThrottle(Mathf.Min(a2, vtolAp.hoverMaxThrottle));
			landOnPadCurveT += num4 * Time.fixedDeltaTime;
			autoPilot.targetPosition = myTransform.position + myTransform.forward * 100f;
			autoPilot.steerMode = AutoPilot.SteerModes.Aim;
			yield return fixedWait;
		}
		landOnPadCurveT = -1f;
	}

	private IEnumerator LandOnPadTaxiRoutine(LandingPadToParkingRoute parkingRoute)
	{
		SetReferenceTransform(taxiSteerReferenceTf, RefTfLocks.PostPadTaxi);
		while (tiltController.currentTilt < 89f)
		{
			tiltController.PadInput(Vector3.up);
			autoPilot.OverrideSetThrottle(-1f);
			yield return null;
		}
		while (commandState != CommandStates.Park)
		{
			TaxiNav(parkingRoute.pathToParking, 1f);
			yield return null;
		}
		UnlockReferenceTransform(RefTfLocks.PostPadTaxi);
	}

	public void TemporaryLandAt(Transform landingTarget)
	{
		if (landOnPadState != 0 || landingState != 0)
		{
			Debug.LogErrorFormat("{0} was commanded to TemporaryLandAt but is already landing!", actor.DebugName());
		}
		StopCombat();
		if (landingTarget != null && isVtol && twr > 1.1f)
		{
			StartCoroutine(LandOnPadRoutine(LandOnPadStates.None, landingTarget, shutoffEngine: false, resetAtParking: false, null));
		}
	}

	public void TemporaryLandAt(Transform landingTarget, float inHeading)
	{
		TemporaryLandAt(landingTarget, inHeading, -1f);
	}

	public void TemporaryLandAt(Transform landingTarget, float inHeading, float landFacing)
	{
		if (landOnPadState != 0 || landingState != 0)
		{
			Debug.LogErrorFormat("{0} was commanded to TemporaryLandAt but is already landing!", actor.DebugName());
		}
		StopCombat();
		if (landingTarget != null && isVtol && twr > 1.1f)
		{
			StartCoroutine(LandOnPadRoutine(LandOnPadStates.None, landingTarget, shutoffEngine: false, resetAtParking: false, null, inHeading, landFacing));
		}
	}

	public void TakeOffVTOL(float heading, float targetAltitude)
	{
		if (isVtol)
		{
			if (isTakingOffVtol)
			{
				Debug.LogErrorFormat("{0}: called to take off vertically but is already taking off vertically.", actor.DebugName());
			}
			else
			{
				StartCoroutine(TakeOffVTOLRoutine(heading, targetAltitude));
			}
		}
	}

	public void TakeOffVTOL(float heading)
	{
		TakeOffVTOL(heading, 30f);
	}

	private IEnumerator DebugStringRoutine(string message, float time)
	{
		float time2 = Time.time;
		while (Time.time - time2 < time)
		{
		}
		yield break;
	}

	private IEnumerator TakeOffVTOLRoutine(float heading, float targetAltitude)
	{
		DebugLogFormat("{0} is taking off VTOL", actor.DebugName());
		commandState = CommandStates.Override;
		vtolManeuvering = true;
		isTakingOffVtol = true;
		vtoHeading = heading;
		vtoTgtAltitude = targetAltitude;
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetNavLights(1);
			extLightsCtrlr.SetStrobeLights(1);
		}
		takeOffAfterLanding = false;
		if (flightInfo.isLanded)
		{
			if ((bool)wingRotator)
			{
				wingRotator.SetDefault();
			}
			for (int e = 0; e < autoPilot.engines.Count; e++)
			{
				while (!autoPilot.engines[e].startedUp)
				{
					yield return null;
				}
			}
			while (tiltController.currentTilt > 0f)
			{
				tiltController.PadInput(new Vector3(0f, -1f, 0f));
				yield return null;
			}
			DebugLogFormat("{0} set initial tilt.", actor.DebugName());
			if ((bool)wingRotator)
			{
				while (wingRotator.transforms[0].currentT > 0.01f)
				{
					yield return null;
				}
			}
		}
		while ((bool)aiSpawn && (bool)aiSpawn.passengerBay && aiSpawn.passengerBay.IsExpectingUnits())
		{
			commandState = CommandStates.Override;
			yield return null;
		}
		if ((bool)currentCarrier)
		{
			DebugLogFormat("{0} registered takeoff unit with {1}", actor.DebugName(), currentCarrier.actor.DebugName());
			currentCarrier.RegisterVTOLTakeoffUnit(actor);
			while (!currentCarrier.IsVTOLTakeoffAuthorized(actor))
			{
				autoPilot.OverrideSetThrottle(-1f);
				autoPilot.OverrideSetBrakes(1f);
				commandState = CommandStates.Override;
				yield return null;
			}
			DebugLogFormat("{0} is authorized for VTOL takeoff", actor.DebugName());
		}
		float ascendSpeed = 10f;
		Vector3 steerDirection2 = myTransform.forward * 1000f;
		steerDirection2.y = 0f;
		Vector3 targetHeading = Quaternion.AngleAxis(heading, Vector3.up) * Vector3.forward * 1000f;
		float tiltInput = 0f;
		float startAlt = flightInfo.altitudeASL;
		Debug.LogFormat("{0} taking off vertically.  StartAlt:{1} targetAltitude:{2}", actor.DebugName(), startAlt, targetAltitude);
		while (flightInfo.altitudeASL - startAlt < targetAltitude && flightInfo.airspeed < minCombatSpeed)
		{
			commandState = CommandStates.Override;
			autoPilot.steerMode = AutoPilot.SteerModes.Aim;
			float num = 1f / twr + Mathf.Clamp((ascendSpeed - flightInfo.verticalSpeed) * 0.05f, -0.05f, 0.08f);
			Vector3 vector = Vector3.up;
			Vector3 vector2 = steerDirection2;
			if ((bool)currentCarrier)
			{
				if (flightInfo.isLanded && Vector3.Project(currentCarrier.shipMover.currentAccel, currentCarrier.transform.forward).sqrMagnitude > 0.040000003f)
				{
					num = -1f;
				}
				else
				{
					if (landedJoint)
					{
						DestroyLandedJoint();
					}
					if (currentCarrier.actor.velocity.sqrMagnitude > 1f)
					{
						vector = Vector3.up + Vector3.ClampMagnitude(Vector3.ProjectOnPlane(currentCarrier.actor.velocity - actor.velocity, Vector3.up) * 0.15f, 0.15f);
						vector2 = Vector3.ProjectOnPlane(steerDirection2, vector);
					}
				}
			}
			else if (landedJoint)
			{
				DestroyLandedJoint();
			}
			autoPilot.targetPosition = myTransform.position + vector2;
			autoPilot.SetOverrideRollTarget(vector);
			if ((bool)vtolAp)
			{
				num = Mathf.Min(num, vtolAp.hoverMaxThrottle);
			}
			autoPilot.targetSpeed = maxSpeed;
			autoPilot.OverrideSetThrottle(num);
			if (flightInfo.altitudeASL - startAlt > 15f)
			{
				steerDirection2 = Vector3.RotateTowards(target: Vector3.RotateTowards(steerDirection2, targetHeading, 0.17453292f * Time.fixedDeltaTime, 0f), current: myTransform.forward, maxRadiansDelta: (float)Math.PI / 12f, maxMagnitudeDelta: float.MaxValue);
				steerDirection2.y = 0f;
			}
			yield return fixedWait;
		}
		if ((bool)currentCarrier)
		{
			currentCarrier.UnregisterVTOLTakeoffUnit(actor);
			currentCarrier = null;
			currentCarrierSpawnIdx = -1;
		}
		gearAnimator.Retract();
		if ((bool)actor.parkingNode)
		{
			actor.parkingNode.UnOccupyParking(actor);
		}
		while (flightInfo.airspeed < minCombatSpeed)
		{
			commandState = CommandStates.Override;
			autoPilot.targetSpeed = maxSpeed;
			if (tiltController.currentTilt < 89.8f)
			{
				if ((bool)vtolAp)
				{
					autoPilot.OverrideSetThrottle(vtolAp.hoverMaxThrottle);
				}
				else
				{
					autoPilot.OverrideSetThrottle(1f);
				}
			}
			float num2 = Mathf.Max(30f, autoPilot.currentSpeed);
			tiltInput = Mathf.Clamp((num2 - tiltController.currentTilt) * 1f, -1f, 1f);
			tiltController.PadInputScaled(new Vector3(0f, tiltInput, 0f));
			float angle = Mathf.Lerp(0f, -15f, (flightInfo.airspeed - 15f) / 80f);
			steerDirection2 = Vector3.RotateTowards(myTransform.forward, steerDirection2, (float)Math.PI / 12f, float.MaxValue);
			steerDirection2.y = 0f;
			autoPilot.targetPosition = myTransform.position + Quaternion.AngleAxis(angle, Vector3.Cross(Vector3.up, steerDirection2)) * steerDirection2;
			steerDirection2 = Vector3.RotateTowards(steerDirection2, targetHeading, (float)Math.PI * 4f / 45f * Time.fixedDeltaTime, 0f);
			Vector3 velocity = rb.velocity;
			velocity.y = 0f;
			float angle2 = Mathf.Clamp(0f - VectorUtils.SignedAngle(velocity, steerDirection2, myTransform.right), -10f, 10f);
			autoPilot.SetOverrideRollTarget(Quaternion.AngleAxis(angle2, myTransform.forward) * Vector3.up);
			yield return fixedWait;
		}
		while (tiltController.currentTilt < 90f)
		{
			commandState = CommandStates.Override;
			autoPilot.targetSpeed = maxSpeed;
			steerDirection2 = Vector3.RotateTowards(myTransform.forward, steerDirection2, (float)Math.PI / 12f, float.MaxValue);
			steerDirection2.y = 0f;
			autoPilot.targetPosition = base.transform.position + Quaternion.AngleAxis(-15f, Vector3.Cross(Vector3.up, steerDirection2)) * steerDirection2;
			steerDirection2 = Vector3.RotateTowards(steerDirection2, targetHeading, (float)Math.PI * 4f / 45f * Time.fixedDeltaTime, 0f);
			autoPilot.steerMode = AutoPilot.SteerModes.Stable;
			tiltInput = Mathf.MoveTowards(tiltInput, 1f, 1f * Time.fixedDeltaTime);
			tiltController.PadInput(new Vector3(0f, tiltInput, 0f));
			if (tiltController.currentTilt < 89.8f)
			{
				if ((bool)vtolAp)
				{
					autoPilot.OverrideSetThrottle(vtolAp.hoverMaxThrottle);
				}
				else
				{
					autoPilot.OverrideSetThrottle(1f);
				}
			}
			yield return fixedWait;
		}
		kPlane.enabled = true;
		kPlane.SetToKinematic();
		vtolManeuvering = false;
		if ((bool)extLightsCtrlr)
		{
			extLightsCtrlr.SetAllLights(0);
		}
		isTakingOffVtol = false;
		ApplyQueuedCommand();
	}

	public void SetEngageEnemies(bool engage)
	{
		SetAutoEngageEnemies(engage);
	}

	public void SetAutoEngageEnemies(bool engage)
	{
		autoEngageEnemies = engage;
		if (!engage && commandState == CommandStates.Combat)
		{
			StopCombat();
		}
	}

	private void CreateLandedJoint(Transform surfaceTf)
	{
		landedParentActor = surfaceTf.GetComponentInParent<Actor>();
		landedJointPlatform = surfaceTf.GetComponent<MovingPlatform>();
		if ((bool)landedParentActor)
		{
			surfaceTf = landedParentActor.transform;
		}
		landedParentTf = surfaceTf;
		landedPos = surfaceTf.InverseTransformPoint(myTransform.position);
		landedRot = surfaceTf.InverseTransformDirection(myTransform.forward);
		kPlane.enabled = false;
		rb.isKinematic = flightInfo.isLanded;
		landedJoint = true;
		autoPilot.enabled = false;
		FlightControlComponent[] outputs = autoPilot.outputs;
		foreach (FlightControlComponent obj in outputs)
		{
			obj.SetPitchYawRoll(Vector3.zero);
			obj.SetThrottle(0f);
		}
		foreach (ModuleEngine engine in autoPilot.engines)
		{
			engine.SetThrottle(0f);
		}
		flightInfo.PauseGCalculations();
	}

	private void DestroyLandedJoint()
	{
		if (landedJoint)
		{
			landedJoint = false;
			if (flightInfo.isLanded)
			{
				rb.position = myTransform.position;
				rb.rotation = myTransform.rotation;
				Vector3 velocity = rb.velocity;
				MovingPlatform component = landedParentTf.GetComponent<MovingPlatform>();
				if ((bool)component)
				{
					velocity = component.GetVelocity(myTransform.position);
				}
				else
				{
					Rigidbody component2 = landedParentTf.GetComponent<Rigidbody>();
					if ((bool)component2)
					{
						velocity = component2.GetPointVelocity(myTransform.position);
					}
				}
				kPlane.SetToDynamic();
				kPlane.SetVelocity(velocity);
			}
			else
			{
				kPlane.enabled = true;
				kPlane.SetToKinematic();
			}
		}
		landedParentActor = null;
		landedParentTf = null;
		autoPilot.enabled = true;
		landedJointPlatform = null;
		flightInfo.UnpauseGCalculations();
	}

	private void UpdateLandedJoint()
	{
		if ((bool)landedParentTf)
		{
			myTransform.position = landedParentTf.TransformPoint(landedPos);
			myTransform.rotation = Quaternion.LookRotation(landedParentTf.TransformDirection(landedRot));
			rb.isKinematic = flightInfo.isLanded;
			rb.velocity = Vector3.zero;
			if ((bool)landedJointPlatform)
			{
				rb.velocity = landedJointPlatform.GetVelocity(rb.position);
			}
			flightInfo.OverrideRecordedAcceleration(Vector3.zero);
			kPlane.SetVelocity(rb.velocity);
		}
		else
		{
			DestroyLandedJoint();
		}
	}

	public Vector3 GetVelocity()
	{
		if (rb.isKinematic)
		{
			return kPlane.velocity;
		}
		return rb.velocity;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		if (!base.enabled)
		{
			return;
		}
		Debug.Log("Quicksaving AIPilot " + base.gameObject.name);
		ConfigNode configNode = new ConfigNode("AIPilot");
		qsNode.AddNode(configNode);
		configNode.SetValue("commandState", commandState);
		configNode.SetValue("queuedCommand", queuedCommand);
		if ((bool)formationLeader)
		{
			int num = -1;
			if (formationLeader.actor == FlightSceneManager.instance.playerActor)
			{
				num = VTScenario.current.units.GetPlayerSpawner().unitInstanceID;
			}
			else
			{
				UnitSpawn componentImplementing = formationLeader.actor.gameObject.GetComponentImplementing<UnitSpawn>();
				if ((bool)componentImplementing)
				{
					num = componentImplementing.unitID;
				}
			}
			if (num >= 0)
			{
				configNode.SetValue("formationLeaderUnit", num);
			}
		}
		if ((bool)orbitTransform)
		{
			configNode.SetValue("orbitGlobalPos", VTMapManager.WorldToGlobalPoint(orbitTransform.position));
		}
		if ((bool)navPath && navPath.scenarioPathID >= 0)
		{
			configNode.SetValue("navPathID", navPath.scenarioPathID);
		}
		if (targetRefuelPlane != null)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(targetRefuelPlane.actor, "targetRefuelPlane"));
		}
		if (queuedRefuelPlane != null)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(queuedRefuelPlane.actor, "queuedRefuelPlane"));
		}
		configNode.SetValue("kPlaneEnabled", kPlane.enabled);
		configNode.SetValue("isKinematic", rb.isKinematic);
		configNode.SetValue("velocity", rb.isKinematic ? kPlane.velocity : rb.velocity);
		configNode.SetValue("angularVelocity", rb.isKinematic ? Vector3.zero : rb.angularVelocity);
		configNode.SetValue("forceDynamic", kPlane.forceDynamic);
		configNode.SetValue("startLanded", flightInfo.isLanded);
		configNode.SetValue("quickloadLandedPoint", VTMapManager.WorldToGlobalPoint(base.transform.position));
		if ((bool)voiceProfile)
		{
			configNode.SetValue("voiceProfile", voiceProfile.name);
		}
		configNode.SetValue("takeOffState", takeOffState);
		configNode.SetValue("landingState", landingState);
		configNode.SetValue("rearmAfterLanding", rearmAfterLanding);
		configNode.SetValue("takeOffAfterLanding", takeOffAfterLanding);
		configNode.SetValue("isTakingOffVtol", isTakingOffVtol);
		configNode.AddNode(landingVertPID.SaveToNode("landingVertPID"));
		configNode.AddNode(landingHorizPID.SaveToNode("landingHorizPID"));
		if ((bool)tailHook)
		{
			configNode.SetValue("tailHookDeployed", tailHook.isDeployed);
		}
		configNode.SetValue("doRadioComms", doRadioComms);
		configNode.SetValue("defaultAltitude", defaultAltitude);
		configNode.SetValue("navSpeed", navSpeed);
		configNode.SetValue("allowPlayerCommands", allowPlayerCommands);
		configNode.SetValue("rearming", rearming);
		configNode.SetValue("landOnPadState", landOnPadState);
		if (landOnPadState != 0)
		{
			if ((bool)landOnPadCSpawn)
			{
				configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(landOnPadCSpawn.actor, "landOnPadCSpawn"));
			}
			configNode.SetValue("landOnPadIdx", apLandOnPadIdx);
			configNode.SetValue("landOnPadTf", new FixedPoint(landOnPadTf.position));
			configNode.SetValue("landOnPadStartApproachPt", landOnPadStartApproachPt);
			configNode.SetValue("landOnPadInHeading", landOnPadInHeading);
			if (landOnPadState == LandOnPadStates.RailLanding)
			{
				configNode.SetValue("landOnPadCurveT", landOnPadCurveT);
				configNode.SetValue("landOnPadCurve", landOnPadCurve);
				configNode.SetValue("landOnPadStartSpeed", landOnPadStartSpeed);
			}
		}
		if (isTakingOffVtol)
		{
			configNode.SetValue("vtoHeading", vtoHeading);
			configNode.SetValue("vtoTgtAltitude", vtoTgtAltitude);
		}
		if (landedJoint && (bool)landedParentActor)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(landedParentActor, "landedParentActor"));
			configNode.SetValue("landedRot", landedRot);
			configNode.SetValue("landedPos", landedPos);
		}
		configNode.SetValue("ctoState", ctoState);
		if (ctoState != 0)
		{
			ConfigNode configNode2 = new ConfigNode("cto");
			configNode.AddNode(configNode2);
			configNode2.AddNode(QuicksaveManager.SaveActorIdentifierToNode(currentCarrier.actor, "carrierActor"));
			configNode2.SetValue("spawnIdx", currentCarrierSpawnIdx);
		}
		if ((bool)targetRunway)
		{
			configNode.SetValue("targetRunwayGPos", VTMapManager.WorldToGlobalPoint(targetRunway.transform.position));
			if (targetRunway.airport.isCarrier)
			{
				configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(targetRunway.airport.GetComponentInParent<Actor>(), "targetRunwayActor"));
			}
			if (landingParkingSpace != null)
			{
				configNode.SetValue("landingParkingSpaceIdx", targetRunway.airport.parkingSpaces.IndexOf(landingParkingSpace));
			}
		}
		QuicksaveAirbaseNav(configNode);
		if (commandState == CommandStates.Evade && evadeTarget != null && evadeTarget.actor != null)
		{
			configNode.AddNode(evadeTarget.SaveToConfigNode("evadeTarget"));
		}
		if ((bool)wingRotator)
		{
			configNode.SetValue("wingsFolded", wingRotator.deployed);
		}
		if ((bool)overrideAttackTarget)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(overrideAttackTarget, "overrideAttackTarget"));
		}
		if ((bool)attackTarget)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(attackTarget, "attackTarget"));
		}
		foreach (Actor priorityTarget in priorityTargets)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(priorityTarget, "priorityTarget"));
		}
		foreach (Actor nonTarget in nonTargets)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(nonTarget, "nonTarget"));
		}
		foreach (Actor designatedTarget in designatedTargets)
		{
			if ((bool)designatedTarget && designatedTarget.alive)
			{
				configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(designatedTarget, "D_TARGET"));
			}
		}
		if (r_carpetBomb_state != 0 && r_carpetBomb_bombEquip != null)
		{
			configNode.SetValue("r_carpetBomb_state", r_carpetBomb_state);
			configNode.SetValue("r_carpetBomb_heading", r_carpetBomb_heading);
			configNode.SetValue("r_carpetBomb_distInterval", r_carpetBomb_distInterval);
			configNode.SetValue("r_carpetBomb_radius", r_carpetBomb_radius);
			configNode.SetValue("r_carpetBomb_altitude", r_carpetBomb_altitude);
			configNode.SetValue("r_carpetBomb_bombEquip", r_carpetBomb_bombEquip.hardpointIdx);
			configNode.SetValue("r_carpetBomb_wpt", r_carpetBomb_wpt);
		}
		else
		{
			configNode.SetValue("r_carpetBomb_state", CarpetBombResumeStates.None);
		}
		if (commandedASMPath != null)
		{
			configNode.SetValue("commandedASMPath", VTSConfigUtils.WriteObject(commandedASMPath));
			configNode.SetValue("commandedASMMode", commandedASMMode);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		if (!qsNode.HasNode("AIPilot"))
		{
			return;
		}
		Debug.Log("Quickloading AI Pilot: " + base.gameObject.name, base.gameObject);
		ConfigNode node = qsNode.GetNode("AIPilot");
		CommandStates value = node.GetValue<CommandStates>("commandState");
		if (value != CommandStates.FollowLeader)
		{
			wasFormation = false;
		}
		if (node.HasValue("formationLeaderUnit"))
		{
			int value2 = node.GetValue<int>("formationLeaderUnit");
			UnitSpawner unit = VTScenario.current.units.GetUnit(value2);
			if ((bool)unit && (bool)unit.spawnedUnit)
			{
				if (unit.spawnedUnit is PlayerSpawn)
				{
					formationLeader = FlightSceneManager.instance.playerActor.GetComponentInChildren<AirFormationLeader>();
				}
				else
				{
					formationLeader = unit.spawnedUnit.GetComponentInChildren<AirFormationLeader>();
				}
			}
		}
		if (node.HasValue("orbitGlobalPos"))
		{
			Vector3 position = VTMapManager.GlobalToWorldPoint(node.GetValue<Vector3D>("orbitGlobalPos"));
			if (!fallbackOrbitTf)
			{
				fallbackOrbitTf = new GameObject("QSFallbackOrbitTf").AddComponent<FloatingOriginTransform>().transform;
			}
			fallbackOrbitTf.position = position;
			orbitTransform = fallbackOrbitTf;
		}
		if (node.HasValue("navPathID"))
		{
			int value3 = node.GetValue<int>("navPathID");
			navPath = VTScenario.current.paths.GetPath(value3);
		}
		Vector3 value4 = node.GetValue<Vector3>("velocity");
		kPlane.enabled = node.GetValue<bool>("kPlaneEnabled");
		if (node.GetValue<bool>("forceDynamic"))
		{
			kPlane.ForceDynamic();
		}
		rb.velocity = (quickloadVelocity = value4);
		rb.angularVelocity = (quickloadAngularVelocity = node.GetValue<Vector3>("angularVelocity"));
		if (node.GetValue<bool>("isKinematic"))
		{
			kPlane.SetToKinematic();
		}
		else
		{
			kPlane.SetToDynamic();
		}
		if (rb.isKinematic)
		{
			kPlane.SetVelocity(value4);
		}
		initialSpeed = value4.magnitude;
		if (node.HasValue("voiceProfile"))
		{
			string value5 = node.GetValue("voiceProfile");
			voiceProfile = VTResources.GetWingmanVoiceProfile(value5);
		}
		if (node.HasNode("overrideAttackTarget"))
		{
			Actor actor = (overrideAttackTarget = QuicksaveManager.RetrieveActorFromNode(node.GetNode("overrideAttackTarget")));
		}
		if (node.HasNode("attackTarget"))
		{
			Actor actor2 = (attackTarget = QuicksaveManager.RetrieveActorFromNode(node.GetNode("attackTarget")));
		}
		ClearPriorityTargets();
		foreach (ConfigNode node4 in node.GetNodes("priorityTarget"))
		{
			Actor actor3 = QuicksaveManager.RetrieveActorFromNode(node4);
			if ((bool)actor3)
			{
				AddPriorityTarget(actor3);
			}
		}
		ClearNonTargets();
		foreach (ConfigNode node5 in node.GetNodes("nonTarget"))
		{
			Actor actor4 = QuicksaveManager.RetrieveActorFromNode(node5);
			if ((bool)actor4)
			{
				AddNonTarget(actor4);
			}
		}
		ClearDesignatedTargets();
		foreach (ConfigNode node6 in node.GetNodes("D_TARGET"))
		{
			Actor actor5 = QuicksaveManager.RetrieveActorFromNode(node6);
			if ((bool)actor5)
			{
				AddDesignatedTarget(actor5);
			}
		}
		quickloadLanded = node.GetValue<bool>("startLanded");
		quickloadLandedPoint = new FixedPoint(node.GetValue<Vector3D>("quickloadLandedPoint"));
		startLanded = quickloadLanded;
		navNodes_wasLanded = startLanded;
		rearmAfterLanding = node.GetValue<bool>("rearmAfterLanding");
		takeOffAfterLanding = node.GetValue<bool>("takeOffAfterLanding");
		doRadioComms = node.GetValue<bool>("doRadioComms");
		defaultAltitude = node.GetValue<float>("defaultAltitude");
		navSpeed = node.GetValue<float>("navSpeed");
		allowPlayerCommands = node.GetValue<bool>("allowPlayerCommands");
		landingVertPID.LoadFromNode(node.GetNode("landingVertPID"));
		landingHorizPID.LoadFromNode(node.GetNode("landingHorizPID"));
		if ((bool)tailHook)
		{
			tailHook.SetHook(node.GetValue<bool>("tailHookDeployed") ? 1 : 0);
		}
		base.transform.position = quickloadLandedPoint.point;
		DestroyLandedJoint();
		if (initialLandRoutine != null)
		{
			StopCoroutine(initialLandRoutine);
		}
		if (initialFlyRoutine != null)
		{
			StopCoroutine(initialFlyRoutine);
		}
		if (node.HasNode("landedParentActor"))
		{
			Actor actor6 = QuicksaveManager.RetrieveActorFromNode(node.GetNode("landedParentActor"));
			if ((bool)actor6)
			{
				CreateLandedJoint(actor6.transform);
				landedPos = node.GetValue<Vector3>("landedPos");
				landedRot = node.GetValue<Vector3>("landedRot");
				rb.interpolation = RigidbodyInterpolation.Interpolate;
			}
		}
		if (!landedJoint)
		{
			if (startLanded)
			{
				initialLandRoutine = StartCoroutine(InitialLandRoutine());
			}
			else
			{
				initialFlyRoutine = StartCoroutine(InitialFlyRoutine(forceTilt: false));
			}
		}
		if ((bool)wingRotator)
		{
			bool value6 = node.GetValue<bool>("wingsFolded");
			wingRotator.SetState(value6 ? 1 : 0);
			wingRotator.SetNormalizedRotationImmediate(value6 ? 1 : 0);
		}
		if (node.HasValue("targetRunwayGPos"))
		{
			targetRunway = GetRunway(node.GetValue<Vector3D>("targetRunwayGPos"));
		}
		if (node.HasNode("targetRunwayActor"))
		{
			Actor a = QuicksaveManager.RetrieveActorFromNode(node.GetNode("targetRunwayActor"));
			targetRunway = GetRunway(a);
		}
		TakeOffStates value7 = node.GetValue<TakeOffStates>("takeOffState");
		LandingStates value8 = node.GetValue<LandingStates>("landingState");
		CTOStates value9 = node.GetValue<CTOStates>("ctoState");
		LandOnPadStates value10 = node.GetValue<LandOnPadStates>("landOnPadState");
		bool value11 = node.GetValue<bool>("isTakingOffVtol");
		if (node.HasNode("targetRefuelPlane"))
		{
			Actor actor7 = QuicksaveManager.RetrieveActorFromNode(node.GetNode("targetRefuelPlane"));
			if ((bool)actor7)
			{
				targetRefuelPlane = actor7.GetComponentInChildren<RefuelPlane>(includeInactive: true);
			}
		}
		if (node.HasNode("queuedRefuelPlane"))
		{
			Actor actor8 = QuicksaveManager.RetrieveActorFromNode(node.GetNode("queuedRefuelPlane"));
			if ((bool)actor8)
			{
				queuedRefuelPlane = actor8.GetComponentInChildren<RefuelPlane>(includeInactive: true);
			}
		}
		queuedCommand = node.GetValue<CommandStates>("queuedCommand");
		if (node.HasNode("evadeTarget"))
		{
			ConfigNode node2 = node.GetNode("evadeTarget");
			StartCoroutine(QLEvadeTarget(node2));
		}
		if ((bool)targetRunway && node.HasValue("landingParkingSpaceIdx"))
		{
			int value12 = node.GetValue<int>("landingParkingSpaceIdx");
			if (value12 >= 0)
			{
				landingParkingSpace = targetRunway.airport.parkingSpaces[value12];
				if ((bool)landingParkingSpace.occupiedBy)
				{
					landingParkingSpace.UnOccupyParking(landingParkingSpace.occupiedBy);
				}
				landingParkingSpace.OccupyParking(this.actor);
			}
		}
		if (value == CommandStates.Taxi && landingParkingSpace != null && (bool)landingParkingSpace.parkingNode.carrierReturnPath)
		{
			taxiPath = landingParkingSpace.parkingNode.carrierReturnPath;
			Debug.Log(" - - Loaded taxi path from parking node's carrier return path (#2)");
		}
		if (value == CommandStates.Override)
		{
			Debug.Log(" - Command state is override.  Handling it.");
			bool flag = false;
			if (value7 != 0 && (bool)targetRunway)
			{
				Debug.Log(" - - Pilot was taking off.  _takeOffState: " + value7);
				List<AirbaseNavNode> list = QuickloadAirbaseNav(node);
				if (list != null)
				{
					Debug.Log(" - - Successful airbase navigation load.  Begining taxi.");
					TaxiAirbaseNav(list, targetRunway);
				}
				else
				{
					Debug.Log(" - - Unsuccessful airbase navigation load!");
				}
				StartCoroutine(TakeOffRoutine(value7));
				flag = true;
			}
			if (value9 != 0)
			{
				if (initialLandRoutine != null)
				{
					StopCoroutine(initialLandRoutine);
					if (value9 == CTOStates.Ascending)
					{
						kPlane.SetToKinematic();
					}
					else
					{
						kPlane.SetToDynamic();
						this.actor.customVelocity = false;
						rb.interpolation = RigidbodyInterpolation.Interpolate;
					}
				}
				Debug.Log(" - - Pilot was taking off from carrier. _ctoState: " + value9);
				ConfigNode node3 = node.GetNode("cto");
				AICarrierSpawn carrier = (AICarrierSpawn)QuicksaveManager.RetrieveActorFromNode(node3.GetNode("carrierActor")).unitSpawn;
				int value13 = node3.GetValue<int>("spawnIdx");
				StartCoroutine(TakeOffCarrierRoutine(carrier, value13, value9));
				flag = true;
			}
			if (value11)
			{
				float value14 = node.GetValue<float>("vtoHeading");
				float value15 = node.GetValue<float>("vtoTgtAltitude");
				TakeOffVTOL(value14, value15);
				flag = true;
			}
			if (value8 != 0)
			{
				Debug.Log(" - - Pilot was landing.  _landingState: " + value8);
				if (landingRoutine != null)
				{
					StopCoroutine(landingRoutine);
				}
				landingRoutine = StartCoroutine(LandingRoutine(value8));
				if (value8 == LandingStates.Taxiing)
				{
					List<AirbaseNavNode> list2 = QuickloadAirbaseNav(node);
					if (list2 != null)
					{
						Debug.Log(" - - Successful airbase navigation load (landing).  Begining taxi.");
						TaxiAirbaseNav(list2, null);
					}
					else if (landingParkingSpace != null && (bool)landingParkingSpace.parkingNode.carrierReturnPath)
					{
						taxiPath = landingParkingSpace.parkingNode.carrierReturnPath;
						Debug.Log(" - - Loaded taxi path from parking node's carrier return path (#1)");
					}
					else
					{
						Debug.Log(" - - Unsuccessful airbase navigation load! (landing)");
					}
					flag = true;
				}
				else
				{
					Debug.Log(" - failed to retrieve target runway. Cancelling landing quickload");
				}
			}
			if (value10 != 0)
			{
				Transform transform = null;
				Actor actor9 = QuicksaveManager.RetrieveActorFromNode(node.GetNode("landOnPadCSpawn"));
				int value16 = node.GetValue<int>("landOnPadIdx");
				FixedPoint value17 = node.GetValue<FixedPoint>("landOnPadTf");
				AirportManager airportManager = null;
				airportManager = ((!actor9) ? GetAirport(value17.globalPoint) : actor9.GetComponent<AICarrierSpawn>().airportManager);
				if ((bool)airportManager && value16 >= 0)
				{
					int value18;
					if (node.HasValue("landingParkingSpaceIdx") && (value18 = node.GetValue<int>("landingParkingSpaceIdx")) >= 0)
					{
						if ((bool)landingParkingSpace.occupiedBy)
						{
							landingParkingSpace.UnOccupyParking(landingParkingSpace.occupiedBy);
						}
						landingParkingSpace = airportManager.parkingSpaces[value18];
						transform = landingParkingSpace.landingPad;
						landingParkingSpace.OccupyParking(this.actor);
					}
					else if (value16 >= 0)
					{
						landingParkingSpace = airportManager.GetParkingSpaceFromLandingPadIdx(value16);
						if ((bool)landingParkingSpace.occupiedBy)
						{
							landingParkingSpace.UnOccupyParking(landingParkingSpace.occupiedBy);
						}
						transform = airportManager.landingPads[value16];
						airportManager.ReserveLandingPad(value16, this.actor);
					}
					if (rearmAfterLanding)
					{
						_ = 1;
					}
					else
						_ = takeOffAfterLanding;
				}
				else
				{
					float num = float.MaxValue;
					Waypoint[] waypoints = VTScenario.current.waypoints.GetWaypoints();
					foreach (Waypoint waypoint in waypoints)
					{
						float sqrMagnitude = (waypoint.worldPosition - value17.point).sqrMagnitude;
						if (sqrMagnitude < num)
						{
							num = sqrMagnitude;
							transform = waypoint.GetTransform();
						}
					}
				}
				Debug.Log(" - - Pilot was landing vertically on a pad. _landOnPadState: " + value10);
				if (transform != null)
				{
					if (value10 == LandOnPadStates.RailLanding)
					{
						landOnPadCurveT = node.GetValue<float>("landOnPadCurveT");
						landOnPadCurve = node.GetValue<BezierCurveD5>("landOnPadCurve");
						landOnPadStartSpeed = node.GetValue<float>("landOnPadStartSpeed");
					}
					landOnPadInHeading = -1f;
					ConfigNodeUtils.TryParseValue(node, "landOnPadInHeading", ref landOnPadInHeading);
					StartCoroutine(LandOnPadRoutine(value10, transform, shutoffEngine: false, airportManager != null, landingParkingSpace, landOnPadInHeading));
				}
				else
				{
					Debug.Log(" - - padTf could not be retrieved!");
				}
				flag = true;
			}
			r_carpetBomb_state = node.GetValue<CarpetBombResumeStates>("r_carpetBomb_state");
			if (r_carpetBomb_state != 0)
			{
				Debug.Log("Quickload AI resumed carpet bombing");
				int value19 = node.GetValue<int>("r_carpetBomb_bombEquip");
				float value20 = node.GetValue<float>("r_carpetBomb_heading");
				float value21 = node.GetValue<float>("r_carpetBomb_distInterval");
				float value22 = node.GetValue<float>("r_carpetBomb_radius");
				float value23 = node.GetValue<float>("r_carpetBomb_altitude");
				FixedPoint value24 = node.GetValue<FixedPoint>("r_carpetBomb_wpt");
				StartCoroutine(QL_ResumeCarpetBomb(value19, value24, value20, value21, value22, value23, combat: false, r_carpetBomb_state));
				flag = true;
			}
			if (node.HasValue("rearming") && node.GetValue<bool>("rearming"))
			{
				AICarrierSpawn cSpawn = null;
				if ((bool)targetRunway)
				{
					cSpawn = targetRunway.airport.carrierSpawn;
				}
				StartCoroutine(ResumeRearm(cSpawn));
				flag = true;
			}
			if (node.HasValue("commandedASMPath"))
			{
				commandedASMPath = VTSConfigUtils.ParseObject<FollowPath>(node.GetValue("commandedASMPath"));
				commandedASMMode = node.GetValue<AntiShipGuidance.ASMTerminalBehaviors>("commandedASMMode");
				CommandAntiShipOnPath(commandedASMPath, commandedASMMode);
				flag = true;
			}
			if (!flag)
			{
				Debug.LogFormat("AIPilot {0} override commandState was not handled properly on quickload. Defaulting to park or orbit.", aiSpawn ? aiSpawn.unitSpawner.GetUIDisplayName() : this.actor.actorName);
				if (startLanded)
				{
					commandState = CommandStates.Park;
				}
				else
				{
					commandState = CommandStates.Orbit;
				}
			}
		}
		else
		{
			commandState = value;
		}
	}

	private IEnumerator ResumeRearm(AICarrierSpawn cSpawn)
	{
		commandState = CommandStates.Override;
		autoPilot.targetSpeed = 0f;
		yield return null;
		if (!actor.parkingNode)
		{
			Debug.LogFormat("{0} resuming rearm. waiting for parking space", actor.DebugName());
			while (!actor.parkingNode)
			{
				yield return null;
			}
		}
		Debug.LogFormat("{0} resumed rearm", actor.DebugName());
		ResetAtParking(cSpawn, actor.parkingNode.transform);
	}

	private IEnumerator QL_ResumeCarpetBomb(int eqIdx, FixedPoint wp, float hdg, float distInterval, float rad, float cbAlt, bool combat, CarpetBombResumeStates resumeState)
	{
		yield return null;
		HPEquipBombRack eq = (HPEquipBombRack)wm.GetEquip(eqIdx);
		if (eq != null && eq.ml != null)
		{
			Debug.Log("did not need to wait for eq or eq.ml null check for carpet bomb resume.");
		}
		while (eq == null || eq.ml == null)
		{
			yield return null;
			if (eq == null)
			{
				eq = (HPEquipBombRack)wm.GetEquip(eqIdx);
			}
		}
		StartCoroutine(CarpetBombRoutine(eq, wp, hdg, distInterval, rad, cbAlt, combat: false, resumeState));
	}

	private void QuicksaveAirbaseNav(ConfigNode apNode)
	{
		if (!isAirbaseNavigating || currentNavTransforms == null || currentNavTransforms.Count <= 0 || !(currentNavTransforms[0] != null))
		{
			return;
		}
		Debug.Log(" - Quicksaving airbaseNav.  currentNavTransforms count: " + currentNavTransforms.Count);
		AirportManager airport = currentNavTransforms[0].GetAirport();
		if ((bool)airport)
		{
			ConfigNode configNode = new ConfigNode("airbaseNav");
			apNode.AddNode(configNode);
			List<int> list = new List<int>();
			foreach (AirbaseNavNode currentNavTransform in currentNavTransforms)
			{
				int item = airport.navigation.navNodes.IndexOf(currentNavTransform);
				list.Add(item);
			}
			configNode.SetValue("nodeIndices", list);
			configNode.SetValue("apGPos", VTMapManager.WorldToGlobalPoint(airport.transform.position));
		}
		else
		{
			Debug.Log(" - - NO AIRPORT FOUND FROM NAV NODES!");
		}
	}

	private List<AirbaseNavNode> QuickloadAirbaseNav(ConfigNode apNode)
	{
		currentNavTransforms = null;
		if (apNode.HasNode("airbaseNav"))
		{
			Debug.Log(" - Quickloading airbaseNav");
			ConfigNode node = apNode.GetNode("airbaseNav");
			List<int> value = node.GetValue<List<int>>("nodeIndices");
			Debug.Log(" - - parsed index list, length: " + value.Count);
			AirportManager airport = GetAirport(node.GetValue<Vector3D>("apGPos"));
			if ((bool)airport && (bool)airport.navigation)
			{
				Debug.Log(" - - Airbase nav found. ap: " + airport.airportName);
				List<AirbaseNavNode> list = new List<AirbaseNavNode>();
				{
					foreach (int item in value)
					{
						list.Add(airport.navigation.navNodes[item]);
					}
					return list;
				}
			}
			Debug.Log(" - - NO AIRPORT NAV FOUND!");
		}
		return null;
	}

	private IEnumerator QLEvadeTarget(ConfigNode evadeTargetNode)
	{
		Debug.LogFormat("QL: {0} was evading. Waiting for missiles to quickload.", actor.DebugName());
		while (!QuicksaveManager.hasQuickloadedMissiles)
		{
			yield return null;
		}
		evadeTarget = EvadeTargetInfo.LoadFromConfigNode(evadeTargetNode);
		if ((bool)evadeTarget.actor)
		{
			Debug.LogFormat("QL: {0} quickloaded evade target: {1}", actor.DebugName(), evadeTarget.actor.DebugName());
			commandState = CommandStates.Evade;
		}
		else
		{
			Debug.LogFormat("QL: {0} failed to quickload evade target.", actor.DebugName());
		}
	}

	private Runway GetRunway(Vector3D globalPos)
	{
		Vector3 vector = VTMapManager.GlobalToWorldPoint(globalPos);
		Runway result = null;
		float num = float.MaxValue;
		foreach (AirportManager airport in VTMapManager.fetch.airports)
		{
			Runway[] runways = airport.runways;
			foreach (Runway runway in runways)
			{
				if ((bool)runway)
				{
					float sqrMagnitude = (runway.transform.position - vector).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						result = runway;
						num = sqrMagnitude;
					}
				}
			}
		}
		return result;
	}

	private Runway GetRunway(Actor a)
	{
		if ((bool)a && (bool)a.unitSpawn && a.unitSpawn is AICarrierSpawn)
		{
			return ((AICarrierSpawn)a.unitSpawn).runway;
		}
		return null;
	}

	private AirportManager GetAirport(Vector3D globalPos)
	{
		Vector3 vector = VTMapManager.GlobalToWorldPoint(globalPos);
		AirportManager result = null;
		float num = float.MaxValue;
		foreach (AirportManager airport in VTMapManager.fetch.airports)
		{
			if ((bool)airport && airport.team == actor.team)
			{
				float sqrMagnitude = (airport.transform.position - vector).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					result = airport;
					num = sqrMagnitude;
				}
			}
		}
		return result;
	}
}
