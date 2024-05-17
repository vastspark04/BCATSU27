using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VTNetworking;
using VTOLVR.Multiplayer;

public class Actor : MonoBehaviour, IQSVehicleComponent, IParentRBDependent
{
	[Flags]
	public enum Roles
	{
		None = 0,
		Ground = 2,
		GroundArmor = 4,
		Air = 8,
		Ship = 0x10,
		Missile = 0x100
	}

	[Serializable]
	public struct ActorRolesSelection
	{
		public bool ground;

		public bool groundArmor;

		public bool air;

		public bool ship;

		public bool missile;

		public int bitmask
		{
			get
			{
				int num = 0;
				if (ground)
				{
					num |= 2;
				}
				if (groundArmor)
				{
					num |= 4;
				}
				if (air)
				{
					num |= 8;
				}
				if (ship)
				{
					num |= 0x10;
				}
				if (missile)
				{
					num |= 0x100;
				}
				return num;
			}
		}

		public static ActorRolesSelection any
		{
			get
			{
				ActorRolesSelection result = default(ActorRolesSelection);
				result.ground = true;
				result.groundArmor = true;
				result.air = true;
				return result;
			}
		}
	}

	public struct Designation
	{
		public PhoneticLetters letter;

		public int num1;

		public int num2;

		private int toID => (int)letter * 100000 + num1 * 1000 + num2;

		public Designation(PhoneticLetters letter, int num1, int num2)
		{
			this.letter = letter;
			this.num1 = num1;
			this.num2 = num2;
		}

		public override string ToString()
		{
			return $"{letter} {num1}-{num2}";
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Designation designation))
			{
				return false;
			}
			if (letter == designation.letter && num1 == designation.num1)
			{
				return num2 == designation.num2;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (int)(letter + num1 * 100 + num2 * 10000);
		}

		public static bool operator ==(Designation a, Designation b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(Designation a, Designation b)
		{
			return !a.Equals(b);
		}

		public int CompareTo(Designation other)
		{
			return toID.CompareTo(other.toID);
		}
	}

	public struct GetActorInfo
	{
		public Actor actor;

		public float sqrDist;
	}

	public string actorName;

	public Teams team;

	private Designation _des;

	[Tooltip("We're using this to disallow targeting static things such as placeable rearming points on the TGP and TSD")]
	public bool opticalTargetable = true;

	public Actor parentActor;

	public bool discovered = true;

	public bool permanentDiscovery = true;

	public AIUnitSpawn.InitialDetectionModes detectionMode;

	private List<Actor> childActors = new List<Actor>();

	private bool _gotUs;

	private UnitSpawn _us;

	private int _actorID = -1;

	private static int nextActorID;

	public bool overrideCombatTarget;

	public Roles overriddenCombatRole;

	private Missile missile;

	private bool _gotFlightInfo;

	private FlightInfo _fi;

	private RadarCrossSection radarCrossSection;

	public int mapPriority;

	private bool _gotHealth;

	private Health h;

	private List<Radar> radars;

	private LockingRadar[] lockingRadars;

	private bool gotChaffModule;

	private ChaffCountermeasure chaffModule;

	public Roles role;

	private Rigidbody rb;

	private GroundUnitMover mover;

	private bool gotTf;

	private Transform _myTf;

	public bool drawIcon = true;

	public UnitIconManager.MapIconTypes iconType;

	public float iconScale = 1f;

	public Vector3 iconOffset;

	[HideInInspector]
	public float iconRotation = 999f;

	public bool useIconRotation;

	public Transform iconRotationReference;

	private bool recordVelocity;

	private Vector3 lastPos;

	private Vector3 recordedVelocity;

	public bool fixedVelocityUpdate = true;

	public bool customVelocity;

	private bool useRBVelocity;

	private Vector3 _lastKnownVelByAllied;

	private Vector3 _lastKnownVelByEnemy;

	private FixedPoint _lastKnownPosByAllied;

	private FixedPoint _lastKnownPosByEnemy;

	private float _lastKnownPosTimeByAllied;

	private float _lastKnownPosTimeByEnemy;

	private float _physRadius = -1f;

	public float overridePhysicalRadius = -1f;

	private bool isMultiplayer;

	private bool gotWm;

	private WeaponManager _wm;

	[HideInInspector]
	public Actor currentlyTargetingActor;

	private float timeTookOff = -1f;

	private bool autoUnoccupyParking = true;

	[HideInInspector]
	public bool hideDeathLog;

	private IntVector2 currGrid;

	private static int subdivFrameStart = 0;

	private const int subdivFrameInterval = 10;

	public List<ModuleRWR> rwrs = new List<ModuleRWR>();

	private static Dictionary<IntVector2, List<Actor>> subdivs = new Dictionary<IntVector2, List<Actor>>();

	public const float SUBDIV_GRID_SIZE = 1000f;

	public Designation designation
	{
		get
		{
			return _des;
		}
		set
		{
			if (_des != value)
			{
				_des = value;
				this.OnSetDesignation?.Invoke(_des);
			}
		}
	}

	public UnitSpawn unitSpawn
	{
		get
		{
			if (!_gotUs)
			{
				if (!_us)
				{
					_us = GetComponent<UnitSpawn>();
				}
				_gotUs = true;
			}
			return _us;
		}
		set
		{
			_us = value;
			if ((bool)_us)
			{
				_gotUs = true;
			}
		}
	}

	public AirportManager.ParkingSpace parkingNode { get; set; }

	public int actorID
	{
		get
		{
			if (_actorID == -1)
			{
				_actorID = nextActorID++;
			}
			return _actorID;
		}
		set
		{
			_actorID = value;
		}
	}

	public Roles finalCombatRole
	{
		get
		{
			if (overrideCombatTarget)
			{
				return overriddenCombatRole;
			}
			return role;
		}
	}

	public FlightInfo flightInfo
	{
		get
		{
			if (!_gotFlightInfo)
			{
				_fi = GetComponent<FlightInfo>();
				_gotFlightInfo = true;
			}
			return _fi;
		}
		set
		{
			_fi = value;
			if ((bool)_fi)
			{
				_gotFlightInfo = true;
			}
		}
	}

	public Health health
	{
		get
		{
			if (!_gotHealth)
			{
				if (!h)
				{
					h = GetComponent<Health>();
				}
				_gotHealth = true;
			}
			return h;
		}
	}

	public bool alive
	{
		get
		{
			if ((bool)h && h.normalizedHealth <= 0f)
			{
				return false;
			}
			return true;
		}
	}

	public float healthMinDamage
	{
		get
		{
			if ((bool)h)
			{
				return h.minDamage;
			}
			return 0f;
		}
	}

	public bool hasRadar
	{
		get
		{
			if (radars == null)
			{
				GetRadars();
			}
			for (int i = 0; i < radars.Count; i++)
			{
				if ((bool)radars[i] && radars[i].radarEnabled)
				{
					return true;
				}
			}
			return false;
		}
	}

	public string location { get; private set; }

	public bool isPlayer
	{
		get
		{
			if (FlightSceneManager.instance != null)
			{
				return FlightSceneManager.instance.playerActor == this;
			}
			return false;
		}
	}

	private Transform myTransform
	{
		get
		{
			if (!gotTf)
			{
				gotTf = true;
				_myTf = base.transform;
			}
			return _myTf;
		}
	}

	public Vector3 velocity
	{
		get
		{
			if ((bool)parentActor)
			{
				return parentActor.velocity;
			}
			if (customVelocity)
			{
				return recordedVelocity;
			}
			if (useRBVelocity)
			{
				return rb.velocity;
			}
			return recordedVelocity;
		}
	}

	public Vector3 position => myTransform.TransformPoint(iconOffset);

	public Vector3 worldCenterOfMass
	{
		get
		{
			if (!rb)
			{
				return position;
			}
			return rb.worldCenterOfMass;
		}
	}

	public float physicalRadius
	{
		get
		{
			if (_physRadius < 0f)
			{
				CalcPhysRad();
			}
			return _physRadius;
		}
	}

	public WeaponManager weaponManager
	{
		get
		{
			if (!gotWm)
			{
				_wm = GetComponent<WeaponManager>();
				gotWm = true;
			}
			return _wm;
		}
	}

	public GunTurretAI gunTurretAI { get; private set; }

	public bool detectedByEnemy { get; private set; }

	public bool detectedByAllied { get; private set; }

	public Actor killedByActor { get; private set; }

	public event Action<Designation> OnSetDesignation;

	public event Action<Teams> OnSetTeam;

	public static event UnityAction<Actor> OnActorKilled;

	public event UnityAction OnThisActorKilled;

	public static int GetRoleMask(params Roles[] roles)
	{
		int num = 0;
		for (int i = 0; i < roles.Length; i++)
		{
			num |= (int)roles[i];
		}
		return num;
	}

	public Missile GetMissile()
	{
		return missile;
	}

	public void SetMissile(Missile m)
	{
		missile = m;
	}

	public float GetRadarCrossSection(Vector3 viewDir)
	{
		if ((bool)radarCrossSection)
		{
			return radarCrossSection.GetCrossSection(viewDir);
		}
		if ((bool)missile)
		{
			return 1f;
		}
		return 40f;
	}

	public List<Radar> GetRadars()
	{
		if (radars == null)
		{
			radars = new List<Radar>();
			Radar[] componentsInChildren = GetComponentsInChildren<Radar>(includeInactive: true);
			foreach (Radar radar in componentsInChildren)
			{
				if (!radar.isMissile || role == Roles.Missile)
				{
					radars.Add(radar);
				}
			}
		}
		return radars;
	}

	public LockingRadar[] GetLockingRadars()
	{
		if (lockingRadars == null)
		{
			lockingRadars = GetComponentsInChildren<LockingRadar>();
		}
		return lockingRadars;
	}

	public ChaffCountermeasure GetChaffModule()
	{
		if (!gotChaffModule)
		{
			chaffModule = GetComponentInChildren<ChaffCountermeasure>();
			gotChaffModule = true;
		}
		return chaffModule;
	}

	public float LastSeenTime(Teams viewingTeam)
	{
		if (viewingTeam == Teams.Allied)
		{
			return _lastKnownPosTimeByAllied;
		}
		return _lastKnownPosTimeByEnemy;
	}

	public Vector3 LastKnownVelocity(Teams viewingTeam, float leewayTime = -1f)
	{
		float num = (isMultiplayer ? VTNetworkManager.GetNetworkTimestamp() : Time.time);
		if (viewingTeam == Teams.Allied)
		{
			if (leewayTime > 0f && num - _lastKnownPosTimeByAllied < leewayTime)
			{
				_lastKnownVelByAllied = velocity;
				return velocity;
			}
			return _lastKnownVelByAllied;
		}
		if (leewayTime > 0f && num - _lastKnownPosTimeByEnemy < leewayTime)
		{
			_lastKnownVelByEnemy = velocity;
			return velocity;
		}
		return _lastKnownVelByEnemy;
	}

	public Vector3 LastKnownPosition(Teams viewingTeam, float leewayTime = -1f)
	{
		float num = (isMultiplayer ? VTNetworkManager.GetNetworkTimestamp() : Time.time);
		if (viewingTeam == Teams.Allied)
		{
			if (leewayTime > 0f && num - _lastKnownPosTimeByAllied < leewayTime)
			{
				_lastKnownPosByAllied.point = position;
				return position;
			}
			return _lastKnownPosByAllied.point;
		}
		if (leewayTime > 0f && num - _lastKnownPosTimeByEnemy < leewayTime)
		{
			_lastKnownPosByEnemy.point = position;
			return position;
		}
		return _lastKnownPosByEnemy.point;
	}

	public void UpdateKnownPosition(Actor viewingActor, bool mpBroadcast = true, float mpTimestamp = -1f)
	{
		float num = (isMultiplayer ? VTNetworkManager.GetNetworkTimestamp() : Time.time);
		Teams teams = viewingActor.team;
		float num2 = ((teams == Teams.Allied) ? _lastKnownPosTimeByAllied : _lastKnownPosTimeByEnemy);
		if (mpTimestamp > 0f)
		{
			if (mpTimestamp < num2)
			{
				return;
			}
			num = mpTimestamp;
		}
		if (teams == Teams.Allied)
		{
			_lastKnownPosTimeByAllied = num;
			_lastKnownPosByAllied.point = position;
			_lastKnownVelByAllied = velocity;
		}
		else
		{
			_lastKnownPosTimeByEnemy = num;
			_lastKnownPosByEnemy.point = position;
			_lastKnownVelByEnemy = velocity;
		}
		if (!isMultiplayer)
		{
			return;
		}
		if (mpBroadcast && (VTScenario.isScenarioHost || viewingActor == FlightSceneManager.instance.playerActor))
		{
			VTOLMPDataLinkManager.instance.ReportKnownPosition(this, teams);
		}
		if (!discovered)
		{
			PlayerInfo localPlayerInfo = VTOLMPLobbyManager.localPlayerInfo;
			if (localPlayerInfo != null && localPlayerInfo.chosenTeam && localPlayerInfo.team == teams)
			{
				DiscoverActor();
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (drawIcon)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(base.transform.TransformPoint(iconOffset), iconScale * Vector3.one);
		}
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(position, physicalRadius);
	}

	private void OnValidate()
	{
		if (overridePhysicalRadius > 0f)
		{
			_physRadius = overridePhysicalRadius;
		}
		else
		{
			_physRadius = -1f;
		}
	}

	private void CalcPhysRad()
	{
		if (overridePhysicalRadius > 0f)
		{
			_physRadius = overridePhysicalRadius;
			return;
		}
		if (role == Roles.Missile)
		{
			_physRadius = 1f;
			return;
		}
		AIPilot component = GetComponent<AIPilot>();
		if ((bool)component)
		{
			_physRadius = component.parkingSize;
			return;
		}
		Debug.LogFormat(base.gameObject, "actor.physicalRadius was retrieved but not defined. default to 15. ({0})", base.gameObject.name);
		_physRadius = 15f;
	}

	private void Awake()
	{
		if (!h)
		{
			h = GetComponent<Health>();
		}
		if ((bool)h)
		{
			h.OnDeath.AddListener(H_OnDeath);
		}
		if ((bool)parentActor)
		{
			parentActor.childActors.Add(this);
		}
	}

	private void Start()
	{
		isMultiplayer = VTOLMPUtils.IsMultiplayer();
		TargetManager.instance.RegisterActor(this);
		if (iconRotationReference == null)
		{
			iconRotationReference = base.transform;
		}
		if ((bool)parentActor)
		{
			discovered = parentActor.discovered;
			permanentDiscovery = parentActor.permanentDiscovery;
			recordVelocity = false;
		}
		if (drawIcon && discovered && alive)
		{
			UnitIconManager.instance.RegisterIcon(this, 0.07f * iconScale, iconOffset);
		}
		lastPos = base.transform.position;
		if ((bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
		}
		rb = GetComponentInParent<Rigidbody>();
		mover = base.gameObject.GetComponentInParentImplementing<GroundUnitMover>();
		if (!rb && !mover)
		{
			recordVelocity = true;
		}
		gunTurretAI = GetComponentInChildren<GunTurretAI>();
		radarCrossSection = GetComponent<RadarCrossSection>();
	}

	public void SetTeam(Teams team)
	{
		if (!VTOLMPUtils.IsMultiplayer())
		{
			throw new NotImplementedException("Actor.SetTeam is only implemented for multiplayer!");
		}
		PlayerInfo localPlayerInfo = VTOLMPLobbyManager.localPlayerInfo;
		switch (iconType)
		{
		case UnitIconManager.MapIconTypes.EnemyAir:
		case UnitIconManager.MapIconTypes.FriendlyAir:
			iconType = ((team == localPlayerInfo.team) ? UnitIconManager.MapIconTypes.FriendlyAir : UnitIconManager.MapIconTypes.EnemyAir);
			break;
		case UnitIconManager.MapIconTypes.EnemyGround:
		case UnitIconManager.MapIconTypes.FriendlyGround:
			iconType = ((team == localPlayerInfo.team) ? UnitIconManager.MapIconTypes.FriendlyGround : UnitIconManager.MapIconTypes.EnemyGround);
			break;
		}
		Debug.Log($"{base.name}.SetTeam({team})");
		TargetManager.instance.UnregisterActor(this);
		UnitIconManager.instance.UnregisterIcon(this);
		this.team = team;
		TargetManager.instance.RegisterActor(this);
		if (drawIcon && discovered)
		{
			UnitIconManager.instance.RegisterIcon(this, 0.07f * iconScale, iconOffset);
		}
		foreach (Actor childActor in childActors)
		{
			childActor.SetTeam(team);
		}
		this.OnSetTeam?.Invoke(team);
	}

	public void EnableIcon()
	{
		drawIcon = true;
		if (discovered)
		{
			UnitIconManager.instance.RegisterIcon(this, 0.07f * iconScale, iconOffset);
		}
	}

	public void DisableIcon()
	{
		drawIcon = false;
		UnitIconManager.instance.UnregisterIcon(this);
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		lastPos += offset;
	}

	public void DiscoverActor()
	{
		if ((bool)parentActor)
		{
			parentActor.DiscoverActor();
		}
		else if (!discovered)
		{
			discovered = true;
			if (drawIcon)
			{
				UnitIconManager.instance.RegisterIcon(this, 0.07f * iconScale, iconOffset);
			}
			if (!permanentDiscovery)
			{
				StartCoroutine(TimedDiscoveryRoutine());
			}
		}
	}

	public void DiscoverOnLocalPlayerTeamChosen()
	{
		StartCoroutine(DiscoverOnLocalPlayerTeamChosenRoutine());
	}

	private IEnumerator DiscoverOnLocalPlayerTeamChosenRoutine()
	{
		PlayerInfo localPlayer = VTOLMPLobbyManager.localPlayerInfo;
		while (!localPlayer.chosenTeam)
		{
			yield return null;
		}
		if (localPlayer.team == team)
		{
			DiscoverActor();
		}
	}

	public void DetectActor(Teams detectedByTeam, Actor byActor = null)
	{
		if (detectedByTeam == Teams.Allied && !detectedByAllied)
		{
			detectedByAllied = true;
			TargetManager.instance.detectedByAllies.Add(this);
		}
		else if (detectedByTeam == Teams.Enemy && !detectedByEnemy)
		{
			detectedByEnemy = true;
			TargetManager.instance.detectedByEnemies.Add(this);
		}
		if (byActor != null)
		{
			UpdateKnownPosition(byActor);
		}
		if ((bool)parentActor)
		{
			parentActor.DetectActor(detectedByTeam, byActor);
		}
	}

	private void Update()
	{
		if (!customVelocity)
		{
			if ((bool)rb)
			{
				recordVelocity = rb.isKinematic;
				useRBVelocity = !recordVelocity;
			}
			else
			{
				useRBVelocity = false;
			}
			if (!fixedVelocityUpdate && recordVelocity)
			{
				float num = ((Time.deltaTime == 0f) ? Time.fixedDeltaTime : Time.deltaTime);
				recordedVelocity = (myTransform.position - lastPos) / num;
				lastPos = base.transform.position;
			}
		}
		if (useIconRotation)
		{
			iconRotation = Mathf.Repeat(iconRotationReference.eulerAngles.y, 360f);
		}
		if (autoUnoccupyParking && parkingNode != null && parkingNode.parkingNode != null && (bool)flightInfo && !flightInfo.isLanded && (parkingNode.transform.position - position).sqrMagnitude > 4000000f)
		{
			if (timeTookOff < 0f)
			{
				timeTookOff = Time.time;
			}
			else if (Time.time - timeTookOff > 30f)
			{
				parkingNode.UnOccupyParking(this);
				parkingNode = null;
			}
		}
		else
		{
			timeTookOff = -1f;
		}
	}

	public void SetAutoUnoccupyParking(bool b)
	{
		autoUnoccupyParking = b;
	}

	private void FixedUpdate()
	{
		if (recordVelocity && !parentActor && fixedVelocityUpdate && !customVelocity)
		{
			recordedVelocity = (myTransform.position - lastPos) / Time.fixedDeltaTime;
			lastPos = myTransform.position;
		}
	}

	public VTNetEntity GetNetEntity()
	{
		VTNetEntity component = GetComponent<VTNetEntity>();
		if (!component && (bool)parentActor)
		{
			return parentActor.GetNetEntity();
		}
		return component;
	}

	private void H_OnDeath()
	{
		if ((bool)TargetManager.instance)
		{
			TargetManager.instance.UnregisterActor(this);
		}
		if ((bool)UnitIconManager.instance && drawIcon)
		{
			UnitIconManager.instance.UnregisterIcon(this);
		}
		string arg = (h.killedByActor ? h.killedByActor.DebugName() : "Environment");
		string message = $"{this.DebugName()} was killed by {arg}. {(string.IsNullOrEmpty(h.killMessage) ? string.Empty : h.killMessage)}";
		if (!hideDeathLog)
		{
			if (!isMultiplayer)
			{
				FlightLogger.Log(message);
			}
			else
			{
				Debug.Log(message);
			}
		}
		else
		{
			Debug.Log(message);
		}
		killedByActor = h.killedByActor;
		if (parkingNode != null)
		{
			parkingNode.UnOccupyParking(this);
		}
		if (Actor.OnActorKilled != null)
		{
			Actor.OnActorKilled(this);
		}
		this.OnThisActorKilled?.Invoke();
	}

	private IEnumerator TimedDiscoveryRoutine()
	{
		yield return new WaitForSeconds(30f);
		if (discovered && !permanentDiscovery && (bool)UnitIconManager.instance)
		{
			UnitIconManager.instance.UnregisterIcon(this);
			discovered = false;
		}
	}

	private void OnEnable()
	{
		StartCoroutine(LocationRoutine());
		StartCoroutine(SubdivRoutine());
	}

	private IEnumerator SubdivRoutine()
	{
		int num = subdivFrameStart;
		subdivFrameStart = (subdivFrameStart + 1) % 10;
		currGrid = GetSubdivGrid(position);
		AddActorToDiv(this, currGrid);
		while (base.enabled && alive)
		{
			IntVector2 subdivGrid = GetSubdivGrid(position);
			if (currGrid != subdivGrid)
			{
				RemoveActorFromDiv(this, currGrid);
				currGrid = subdivGrid;
				AddActorToDiv(this, subdivGrid);
			}
			for (int i = num; i < 10; i++)
			{
				yield return null;
			}
			num = 0;
		}
	}

	private void OnDisable()
	{
		if (discovered && !permanentDiscovery && (bool)UnitIconManager.instance)
		{
			UnitIconManager.instance.UnregisterIcon(this);
			discovered = false;
		}
		RemoveActorFromDiv(this, currGrid);
	}

	private void OnDestroy()
	{
		if ((bool)TargetManager.instance)
		{
			TargetManager.instance.UnregisterActor(this);
		}
		if ((bool)UnitIconManager.instance)
		{
			UnitIconManager.instance.UnregisterIcon(this);
		}
	}

	private IEnumerator LocationRoutine()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 1f));
		WaitForSeconds wait = new WaitForSeconds(1f);
		while (Location.locations == null)
		{
			yield return null;
		}
		while (base.enabled)
		{
			yield return null;
			Location closest = null;
			float closestSqrDist = float.MaxValue;
			int count = Location.locations.Count;
			for (int i = 0; i < count; i++)
			{
				Location location = Location.locations[i];
				float sqrMagnitude = (location.transform.position - base.transform.position).sqrMagnitude;
				if (sqrMagnitude < closestSqrDist && sqrMagnitude < location.sqrRadius)
				{
					closest = location;
					closestSqrDist = sqrMagnitude;
				}
				yield return null;
			}
			if ((bool)closest)
			{
				this.location = closest.locationName;
			}
			else
			{
				this.location = "Unknown";
			}
			yield return wait;
		}
	}

	public void SetCustomVelocity(Vector3 vel)
	{
		customVelocity = true;
		recordVelocity = false;
		recordedVelocity = vel;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode("Actor_" + base.gameObject.name);
		qsNode.AddNode(configNode);
		configNode.SetValue("detectedByAllied", detectedByAllied);
		configNode.SetValue("detectedByEnemy", detectedByEnemy);
		configNode.SetValue("actorID", actorID);
		configNode.SetValue("autoUnoccupyParking", autoUnoccupyParking);
		if (parkingNode != null)
		{
			configNode.AddNode(AirportManager.SaveParkingSpaceToConfigNode("parkingSpace", parkingNode));
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = "Actor_" + base.gameObject.name;
		if (!qsNode.HasNode(text))
		{
			return;
		}
		ConfigNode node = qsNode.GetNode(text);
		actorID = node.GetValue<int>("actorID");
		if (node.GetValue<bool>("detectedByAllied"))
		{
			DetectActor(Teams.Allied);
		}
		if (node.GetValue<bool>("detectedByEnemy"))
		{
			DetectActor(Teams.Enemy);
		}
		autoUnoccupyParking = node.GetValue<bool>("autoUnoccupyParking");
		ConfigNode node2 = node.GetNode("parkingSpace");
		if (node2 == null)
		{
			return;
		}
		Debug.LogFormat("- {0} quickloading parking space", this.DebugName());
		AirportManager.ParkingSpace parkingSpace = AirportManager.RetrieveParkingSpaceFromConfigNode(node2);
		if (parkingSpace.occupiedBy != this)
		{
			if (parkingSpace.occupiedBy != null)
			{
				Debug.LogFormat("- parking space was occupied by wrong actor ({0}). Overriding it.", parkingSpace.occupiedBy.DebugName());
			}
			parkingSpace.UnOccupyParking(parkingSpace.occupiedBy);
		}
		parkingSpace.OccupyParking(this);
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		this.rb = rb;
		if (!rb)
		{
			useRBVelocity = false;
		}
	}

	public static IntVector2 GetSubdivGrid(Vector3 worldPos)
	{
		return new IntVector2(Mathf.RoundToInt((worldPos.x + (float)FloatingOrigin.accumOffset.x) / 1000f), Mathf.RoundToInt((worldPos.z + (float)FloatingOrigin.accumOffset.z) / 1000f));
	}

	private static void AddActorToDiv(Actor a, IntVector2 grid)
	{
		if (!subdivs.TryGetValue(grid, out var value))
		{
			value = new List<Actor>();
			subdivs.Add(grid, value);
		}
		value.Add(a);
	}

	private static void RemoveActorFromDiv(Actor a, IntVector2 grid)
	{
		if (subdivs.TryGetValue(grid, out var value))
		{
			value.Remove(a);
		}
	}

	public static void GetActorsInRadius(Vector3 origin, float radius, Teams requesterTeam, TeamOptions teamOption, List<Actor> outList, bool clearList = true, bool sphericalRadius = true)
	{
		if (clearList)
		{
			outList.Clear();
		}
		IntVector2 subdivGrid = GetSubdivGrid(origin);
		int num = Mathf.FloorToInt(radius / 1000f) + 1;
		float num2 = radius * radius;
		Teams teams = Teams.Allied;
		if (teamOption != TeamOptions.BothTeams)
		{
			teams = ((teamOption == TeamOptions.SameTeam) ? requesterTeam : ((requesterTeam == Teams.Allied) ? Teams.Enemy : Teams.Allied));
		}
		for (int i = subdivGrid.x - num; i <= subdivGrid.x + num; i++)
		{
			for (int j = subdivGrid.y - num; j <= subdivGrid.y + num; j++)
			{
				IntVector2 key = new IntVector2(i, j);
				if (!subdivs.TryGetValue(key, out var value))
				{
					continue;
				}
				for (int k = 0; k < value.Count; k++)
				{
					Actor actor = value[k];
					if ((teamOption == TeamOptions.BothTeams || actor.team == teams) && actor.alive)
					{
						Vector3 vector = actor.position - origin;
						if (!sphericalRadius)
						{
							vector.y = 0f;
						}
						if (vector.sqrMagnitude < num2)
						{
							outList.Add(actor);
						}
					}
				}
			}
		}
	}

	public static void GetActorsInRadius(Vector3 origin, float radius, Teams requesterTeam, TeamOptions teamOption, List<GetActorInfo> outList, bool clearList = true, bool sphericalRadius = true)
	{
		if (clearList)
		{
			outList.Clear();
		}
		IntVector2 subdivGrid = GetSubdivGrid(origin);
		int num = Mathf.FloorToInt(radius / 1000f) + 1;
		float num2 = radius * radius;
		Teams teams = Teams.Allied;
		if (teamOption != TeamOptions.BothTeams)
		{
			teams = ((teamOption == TeamOptions.SameTeam) ? requesterTeam : ((requesterTeam == Teams.Allied) ? Teams.Enemy : Teams.Allied));
		}
		for (int i = subdivGrid.x - num; i <= subdivGrid.x + num; i++)
		{
			for (int j = subdivGrid.y - num; j <= subdivGrid.y + num; j++)
			{
				IntVector2 key = new IntVector2(i, j);
				if (!subdivs.TryGetValue(key, out var value))
				{
					continue;
				}
				for (int k = 0; k < value.Count; k++)
				{
					Actor actor = value[k];
					if ((teamOption == TeamOptions.BothTeams || actor.team == teams) && actor.alive)
					{
						Vector3 vector = actor.position - origin;
						if (!sphericalRadius)
						{
							vector.y = 0f;
						}
						float sqrMagnitude = vector.sqrMagnitude;
						if (sqrMagnitude < num2)
						{
							outList.Add(new GetActorInfo
							{
								actor = actor,
								sqrDist = sqrMagnitude
							});
						}
					}
				}
			}
		}
	}
}
