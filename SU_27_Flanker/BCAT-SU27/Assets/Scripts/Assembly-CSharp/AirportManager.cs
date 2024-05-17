using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class AirportManager : MonoBehaviour, IQSVehicleComponent
{
	[Serializable]
	public class ParkingSpace
	{
		public Runway[] landingRunways;

		public Transform landingPad;

		public AirbaseNavNode parkingNode;

		public ReArmingPoint[] rearmPoints;

		public MultiplayerSpawn mpSpawn;

		private Actor _occupiedBy;

		public Transform transform => parkingNode.transform;

		public float parkingSize => parkingNode.parkingSize;

		public int idx { get; set; }

		public AirportManager airport { get; set; }

		public Actor occupiedBy
		{
			get
			{
				if ((bool)mpSpawn)
				{
					_occupiedBy = mpSpawn.actor;
				}
				return _occupiedBy;
			}
			private set
			{
				if (!(_occupiedBy != value))
				{
					return;
				}
				if (_occupiedBy != null && value != null)
				{
					Debug.LogErrorFormat("Tried to set parking space occupied by {0} but it's already occupied by {1}", value.actorName, _occupiedBy.actorName);
					return;
				}
				_occupiedBy = value;
				if (value != null)
				{
					value.parkingNode = this;
				}
				if ((bool)parkingNode)
				{
					parkingNode.parkingOccupiedBy = _occupiedBy;
				}
			}
		}

		private string debugName => $"{airport.airportName} [{idx}]";

		public static implicit operator bool(ParkingSpace ps)
		{
			return ps != null;
		}

		public void UnOccupyParking(Actor a)
		{
			if (a == occupiedBy && a != null)
			{
				occupiedBy = null;
				if (a.parkingNode == this)
				{
					a.parkingNode = null;
				}
				Debug.LogFormat("{0} unoccupied its parking spot. ({1})", a.DebugName(), debugName);
			}
			else if (_occupiedBy != null)
			{
				Debug.LogErrorFormat("{0} tried to unoccupy a parking spot that was occupied by {1} ({2})", a.DebugName(), occupiedBy.DebugName(), debugName);
			}
		}

		public void OccupyParking(Actor a)
		{
			if (a == null)
			{
				Debug.LogErrorFormat("Tried to occupy parking with a null actor! ({0})", debugName);
				return;
			}
			if ((bool)mpSpawn && mpSpawn.actor != a)
			{
				Debug.LogError("An actor (" + a.DebugName() + ") occupied a spot that is assigned to a MP spawn!", transform.gameObject);
			}
			occupiedBy = a;
			Debug.LogFormat("{0} is occupying parking spot {1}", a.DebugName(), debugName);
		}
	}

	public class AirbaseSurfaceCollider : MonoBehaviour
	{
		public AirportManager airport;
	}

	public struct LandingRequestResponse
	{
		public bool accepted;

		public bool incompatible;

		public ParkingSpace parkingSpace;

		public Runway runway;

		public Transform landingPad;

		public override string ToString()
		{
			return string.Format("LandingRequestResponse:\naccepted [{0}]\nincompatible [{1}]\nparkingSpace [{2}]\nrunway [{3}]\nlandingPad [{4}]", accepted, incompatible, parkingSpace ? UIUtils.GetHierarchyString(parkingSpace.transform.gameObject) : "null", runway ? UIUtils.GetHierarchyString(runway.gameObject) : "null", landingPad ? UIUtils.GetHierarchyString(landingPad.gameObject) : "null");
		}
	}

	public enum VTOLLandingModes
	{
		NoVTOL = 1,
		ForceVTOL = 2,
		VTOLAllowed = 4,
		ForceHelipadParking = 8
	}

	public enum PlayerLandingReponses
	{
		ClearedToLand,
		UnableToLand,
		AlreadyRequested
	}

	public enum PlayerRequestStatus
	{
		None,
		DirectingToAirbase,
		WaitingForLandingClearance,
		ClearedToLand,
		TaxiToParking,
		TaxiToRunway,
		WaitingForTakeoffClearance,
		ClearedToTakeoff,
		WaitingForCatapultClearance,
		TaxiToCatapult,
		PreCatapult,
		RunUpEnginesCatapult,
		DirectingToAirbaseVertical,
		ClearedToLandVertical,
		ClearedToTakeoffVertical
	}

	private enum PlayerLandingStates
	{
		None,
		DirectingToAirbase,
		WaitingForLandingClearance,
		ClearedToLand,
		TaxiToParking
	}

	public Runway[] runways;

	public Transform[] landingPads;

	public AirbaseNavigation navigation;

	public Teams team;

	public Location location;

	public string airportName;

	public ATCVoiceProfile voiceProfile;

	public Collider[] surfaceColliders;

	public List<ParkingSpace> parkingSpaces;

	[Header("Effects")]
	public GameObject[] enableForLandingRequest;

	[Header("Carrier")]
	public bool isCarrier;

	public bool vtolOnlyLanding;

	public bool vtolOnlyTakeoff;

	public bool hasArrestor;

	public bool reserveRunwayForCarrierTakeOff;

	public bool reserveRunwayForVerticalLanding;

	public Transform carrierOlsTransform;

	public OpticalLandingSystem ols;

	public CarrierCable[] carrierCables;

	[Header("Runtime")]
	public Runway playerTakeoffRunway;

	private AICarrierSpawn _cSpawn;

	private Coroutine playerRequestRoutine;

	private PlayerRequestStatus rqStatus;

	private PlayerLandingStates pLandingState;

	private float landingClearanceSqrRadius = 144000000f;

	public float waveOffCheckDist = 250f;

	public float waveOffMinDist = 115f;

	public MinMax waveOffAoA;

	private float waveOffDotThresh = 0.99965733f;

	private float linedUpDotThresh = 0.99990255f;

	private bool lsoComplete;

	private Coroutine lsoRoutine;

	private Runway playerLandingRunway;

	private CarrierCatapult playerCat;

	private float takeoffRunwayHeading;

	public AICarrierSpawn carrierSpawn
	{
		get
		{
			if (isCarrier)
			{
				if (_cSpawn == null)
				{
					_cSpawn = GetComponentInParent<AICarrierSpawn>();
				}
				return _cSpawn;
			}
			return null;
		}
	}

	public PlayerRequestStatus playerRequestStatus
	{
		get
		{
			return rqStatus;
		}
		private set
		{
			if (value != rqStatus)
			{
				Debug.Log("Setting AirportManager.playerRequestStatus: " + value);
				rqStatus = value;
			}
		}
	}

	private Actor playerActor => FlightSceneManager.instance.playerActor;

	private string mapAirbaseNodeName => base.gameObject.name + "_AirportManager";

	private void Awake()
	{
		for (int i = 0; i < parkingSpaces.Count; i++)
		{
			parkingSpaces[i].airport = this;
			if (parkingSpaces[i].rearmPoints != null)
			{
				ReArmingPoint[] rearmPoints = parkingSpaces[i].rearmPoints;
				for (int j = 0; j < rearmPoints.Length; j++)
				{
					rearmPoints[j].parkingSpace = parkingSpaces[i];
				}
			}
			parkingSpaces[i].idx = i;
		}
		Runway[] array = runways;
		for (int j = 0; j < array.Length; j++)
		{
			array[j].airport = this;
		}
		Collider[] array2 = surfaceColliders;
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j].gameObject.AddComponent<AirbaseSurfaceCollider>().airport = this;
		}
		if (enableForLandingRequest != null)
		{
			enableForLandingRequest.SetActive(active: false);
		}
	}

	private IEnumerator CheckRunwayUsageRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(2f);
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 2f));
		bool wasUsed = false;
		while (base.enabled)
		{
			bool flag = false;
			for (int i = 0; i < runways.Length; i++)
			{
				if (flag)
				{
					break;
				}
				if (runways[i].UsageQueueCount() > 0)
				{
					flag = true;
				}
			}
			if (flag != wasUsed)
			{
				wasUsed = flag;
				enableForLandingRequest.SetActive(flag);
			}
			yield return wait;
		}
	}

	private void Start()
	{
		FlightSceneManager.instance.OnExitScene += OnExitScene;
		if (isCarrier)
		{
			if (!carrierSpawn)
			{
				Debug.LogError("Airport manager started that is flagged isCarrier but does not have an AICarrierSpawn component.");
			}
		}
		else
		{
			QuicksaveManager.instance.OnQuicksave += Instance_OnQuicksave;
			QuicksaveManager.instance.OnQuickload += Instance_OnQuickload;
		}
	}

	private void OnEnable()
	{
		StartCoroutine(CheckRunwayUsageRoutine());
		StartCoroutine(CheckForMPSpawnsRoutine());
	}

	private IEnumerator CheckForMPSpawnsRoutine()
	{
		if (!VTOLMPUtils.IsMultiplayer())
		{
			yield break;
		}
		while (!VTMapManager.fetch.mpScenarioStart)
		{
			yield return null;
		}
		while (!VTMapManager.fetch.scenarioReady)
		{
			yield return null;
		}
		List<MultiplayerSpawn> list = new List<MultiplayerSpawn>();
		foreach (UnitSpawner value in VTScenario.current.units.alliedUnits.Values)
		{
			if (value.spawnedUnit is MultiplayerSpawn)
			{
				list.Add((MultiplayerSpawn)value.spawnedUnit);
			}
		}
		foreach (UnitSpawner value2 in VTScenario.current.units.enemyUnits.Values)
		{
			if (value2.spawnedUnit is MultiplayerSpawn)
			{
				list.Add((MultiplayerSpawn)value2.spawnedUnit);
			}
		}
		foreach (MultiplayerSpawn item in list)
		{
			foreach (ParkingSpace parkingSpace in parkingSpaces)
			{
				if ((parkingSpace.transform.position - item.transform.position).sqrMagnitude < parkingSpace.parkingSize * parkingSpace.parkingSize)
				{
					parkingSpace.mpSpawn = item;
					break;
				}
			}
		}
	}

	private void OnExitScene()
	{
		Runway[] array = runways;
		foreach (Runway runway in array)
		{
			if ((bool)runway)
			{
				runway.ClearAll();
			}
		}
		if (playerRequestRoutine != null)
		{
			StopCoroutine(playerRequestRoutine);
		}
		playerLandingRunway = null;
		playerRequestStatus = PlayerRequestStatus.None;
	}

	private void OnDestroy()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= OnExitScene;
		}
	}

	private bool BitmaskCheck(VTOLLandingModes mask, VTOLLandingModes check)
	{
		return (mask & check) == check;
	}

	public LandingRequestResponse RequestLanding2(Actor requester, float mass, float parkingSize, VTOLLandingModes vtolMode)
	{
		LandingRequestResponse result = default(LandingRequestResponse);
		result.accepted = false;
		result.incompatible = true;
		Debug.LogFormat("{0} requested landing. VTOL Mode: {1}", requester.DebugName(), vtolMode);
		ParkingSpace parkingSpace = null;
		if (VTOLMPUtils.IsMultiplayer() && requester == FlightSceneManager.instance.playerActor)
		{
			foreach (ParkingSpace parkingSpace2 in parkingSpaces)
			{
				if ((bool)parkingSpace2.mpSpawn && parkingSpace2.mpSpawn.actor == requester)
				{
					parkingSpace = parkingSpace2;
					Debug.LogFormat(" - found MPSpawn parking spot: {0}", parkingSpace2.parkingNode.gameObject.name);
					break;
				}
			}
		}
		if (parkingSpace == null)
		{
			bool flag = BitmaskCheck(vtolMode, VTOLLandingModes.ForceHelipadParking);
			foreach (ParkingSpace parkingSpace3 in parkingSpaces)
			{
				if ((bool)parkingSpace3.landingPad && !BitmaskCheck(vtolMode, VTOLLandingModes.NoVTOL))
				{
					result.incompatible = false;
				}
				if (parkingSpace3.landingRunways != null && parkingSpace3.landingRunways.Length != 0 && !BitmaskCheck(vtolMode, VTOLLandingModes.ForceVTOL))
				{
					result.incompatible = false;
				}
				bool flag2 = parkingSpace3.occupiedBy == null || parkingSpace3.occupiedBy == requester;
				if (flag && (bool)parkingSpace3.landingPad && flag2)
				{
					parkingSpace = parkingSpace3;
					break;
				}
				if ((!parkingSpace || ((bool)parkingSpace && !parkingSpace.parkingNode.vtolOnly && !BitmaskCheck(vtolMode, VTOLLandingModes.NoVTOL) && parkingSpace3.parkingNode.vtolOnly)) && flag2)
				{
					if (parkingSpace3.mpSpawn != null && parkingSpace3.mpSpawn.actor != requester)
					{
						continue;
					}
					if (parkingSpace3.parkingNode.vtolOnly)
					{
						if (BitmaskCheck(vtolMode, VTOLLandingModes.NoVTOL) && !flag)
						{
							continue;
						}
					}
					else if (!parkingSpace3.landingPad && BitmaskCheck(vtolMode, VTOLLandingModes.ForceVTOL))
					{
						continue;
					}
					if (parkingSpace3.parkingSize >= parkingSize)
					{
						parkingSpace = parkingSpace3;
						Debug.LogFormat(" - found suitable parking spot: {0}", parkingSpace3.parkingNode.gameObject.name);
					}
				}
				if ((bool)parkingSpace && !result.incompatible && (BitmaskCheck(vtolMode, VTOLLandingModes.NoVTOL) || (bool)parkingSpace.landingPad))
				{
					break;
				}
			}
		}
		if (parkingSpace != null)
		{
			result.accepted = true;
			result.parkingSpace = parkingSpace;
			result.landingPad = parkingSpace.landingPad;
			if (!BitmaskCheck(vtolMode, VTOLLandingModes.ForceVTOL))
			{
				Runway runway = null;
				int num = int.MaxValue;
				if (parkingSpace.landingRunways == null || parkingSpace.landingRunways.Length == 0)
				{
					Debug.LogError("Parking space has no landing runways...", parkingSpace.parkingNode.gameObject);
				}
				for (int i = 0; i < parkingSpace.landingRunways.Length; i++)
				{
					Runway runway2 = parkingSpace.landingRunways[i];
					if (mass < runway2.maxMass && runway2.landing && runway2.UsageQueueCount() < num)
					{
						runway = runway2;
						num = runway.UsageQueueCount();
					}
				}
				if ((bool)runway)
				{
					if (runway.sharedRunways != null && runway.sharedRunways.Count > 0)
					{
						float num2 = Vector3.Dot(runway.transform.forward, (runway.transform.position - requester.position).normalized);
						foreach (Runway sharedRunway in runway.sharedRunways)
						{
							if (sharedRunway.landing)
							{
								float num3 = Vector3.Dot(sharedRunway.transform.forward, (sharedRunway.transform.position - requester.position).normalized);
								if (num3 > num2)
								{
									num2 = num3;
									runway = sharedRunway;
								}
							}
						}
					}
					Debug.Log(requester.gameObject.name + " is requesting landing.  Directing to " + runway.gameObject.name);
					result.runway = runway;
				}
			}
		}
		return result;
	}

	public Runway RequestLanding(Transform tf, float mass)
	{
		Runway runway = null;
		int num = int.MaxValue;
		for (int i = 0; i < runways.Length; i++)
		{
			if (mass < runways[i].maxMass && runways[i].landing && runways[i].UsageQueueCount() < num)
			{
				runway = runways[i];
				num = runway.UsageQueueCount();
			}
		}
		if ((bool)runway)
		{
			if (runway.sharedRunways != null && runway.sharedRunways.Count > 0)
			{
				float num2 = Vector3.Dot(runway.transform.forward, (runway.transform.position - tf.position).normalized);
				foreach (Runway sharedRunway in runway.sharedRunways)
				{
					if (sharedRunway.landing)
					{
						float num3 = Vector3.Dot(sharedRunway.transform.forward, (sharedRunway.transform.position - tf.position).normalized);
						if (num3 > num2)
						{
							num2 = num3;
							runway = sharedRunway;
						}
					}
				}
			}
			Debug.Log(tf.gameObject.name + " is requesting landing.  Directing to " + runway.gameObject.name);
		}
		return runway;
	}

	private bool PadOccupiedByActor(int idx, Actor a)
	{
		Transform transform = landingPads[idx];
		foreach (ParkingSpace parkingSpace in parkingSpaces)
		{
			if ((bool)parkingSpace.landingPad && parkingSpace.landingPad == transform)
			{
				return parkingSpace.occupiedBy == a;
			}
		}
		return false;
	}

	private bool IsPadReserved(int idx)
	{
		Transform transform = landingPads[idx];
		foreach (ParkingSpace parkingSpace in parkingSpaces)
		{
			if ((bool)parkingSpace.landingPad && parkingSpace.landingPad == transform)
			{
				return parkingSpace.occupiedBy != null;
			}
		}
		return false;
	}

	public ParkingSpace GetParkingSpaceFromLandingPadIdx(int idx)
	{
		Transform transform = landingPads[idx];
		foreach (ParkingSpace parkingSpace in parkingSpaces)
		{
			if (parkingSpace.landingPad == transform)
			{
				return parkingSpace;
			}
		}
		return null;
	}

	public int GetCarrierSpawnIdx(Transform landingPadOrParkingNode)
	{
		if ((bool)_cSpawn)
		{
			LandingPadToParkingRoute component = landingPadOrParkingNode.GetComponent<LandingPadToParkingRoute>();
			if ((bool)component)
			{
				for (int i = 0; i < _cSpawn.spawnPoints.Count; i++)
				{
					if (_cSpawn.spawnPoints[i].spawnTf == component.parkingNode.transform)
					{
						return i;
					}
				}
			}
			for (int j = 0; j < _cSpawn.spawnPoints.Count; j++)
			{
				if (_cSpawn.spawnPoints[j].spawnTf == landingPadOrParkingNode)
				{
					return j;
				}
			}
		}
		return -1;
	}

	public Transform RequestLandingPad(Actor pilot, out int carrierSpawnIdx)
	{
		carrierSpawnIdx = -1;
		Debug.LogFormat(base.gameObject, "Pilot {0} is requesting landing pad.", pilot.unitSpawn ? pilot.unitSpawn.unitSpawner.GetUIDisplayName() : pilot.actorName);
		if (landingPads == null || landingPads.Length == 0)
		{
			Debug.Log(" - no landing pads.", base.gameObject);
			return null;
		}
		for (int i = 0; i < landingPads.Length; i++)
		{
			if (PadOccupiedByActor(i, pilot))
			{
				carrierSpawnIdx = GetCarrierSpawnIdx(landingPads[i]);
				return landingPads[i];
			}
		}
		for (int j = 0; j < landingPads.Length; j++)
		{
			if (!IsPadReserved(j) && IsPadClear(j))
			{
				StartCoroutine(ReservedPadRoutine(j, pilot));
				Debug.LogFormat(" - reserved pad {0}.", j);
				carrierSpawnIdx = GetCarrierSpawnIdx(landingPads[j]);
				return landingPads[j];
			}
		}
		Debug.LogFormat(" - all pads are occupied.");
		return null;
	}

	public bool HasFreeLandingPads()
	{
		foreach (ParkingSpace parkingSpace in parkingSpaces)
		{
			if ((bool)parkingSpace.landingPad && !parkingSpace.occupiedBy)
			{
				return true;
			}
		}
		return false;
	}

	public bool ReserveLandingPad(int i, Actor pilot)
	{
		ParkingSpace parkingSpaceFromLandingPadIdx = GetParkingSpaceFromLandingPadIdx(i);
		if (!parkingSpaceFromLandingPadIdx.occupiedBy || parkingSpaceFromLandingPadIdx.occupiedBy == pilot)
		{
			parkingSpaceFromLandingPadIdx.OccupyParking(pilot);
			StartCoroutine(ReservedPadRoutine(i, pilot));
			return true;
		}
		return false;
	}

	private IEnumerator ReservedPadRoutine(int idx, Actor pilot)
	{
		ParkingSpace ps = GetParkingSpaceFromLandingPadIdx(idx);
		ps.OccupyParking(pilot);
		bool tookOff = false;
		float tookOffTime = -1f;
		while ((bool)pilot && pilot.alive && !tookOff && ps.occupiedBy == pilot)
		{
			if (!pilot.flightInfo.isLanded)
			{
				if (tookOffTime < 0f)
				{
					tookOffTime = Time.time;
				}
				else if (Time.time - tookOffTime > 5f)
				{
					tookOff = true;
				}
			}
			yield return null;
		}
		ps.UnOccupyParking(pilot);
	}

	private bool IsPadClear(int padIdx)
	{
		ParkingSpace parkingSpaceFromLandingPadIdx = GetParkingSpaceFromLandingPadIdx(padIdx);
		if ((bool)parkingSpaceFromLandingPadIdx.occupiedBy)
		{
			return false;
		}
		float num = 15f * 15f;
		foreach (Actor allActor in TargetManager.instance.allActors)
		{
			if (allActor.finalCombatRole == Actor.Roles.Air && (allActor.position - parkingSpaceFromLandingPadIdx.landingPad.position).sqrMagnitude < num)
			{
				return false;
			}
		}
		return true;
	}

	public Runway GetNextTakeoffRunway()
	{
		Runway runway = null;
		int num = int.MaxValue;
		for (int i = 0; i < runways.Length; i++)
		{
			if (runways[i].takeoff && runways[i].takeoffRequests < num)
			{
				runway = runways[i];
				num = runways[i].takeoffRequests;
			}
		}
		if ((bool)runway)
		{
			runway.takeoffRequests++;
		}
		return runway;
	}

	public void AddCustomAirportToMapManager()
	{
		if (VTMapManager.nextLaunchMode != VTMapManager.MapLaunchModes.MapEditor)
		{
			VTMapManager.fetch.airports.Add(this);
		}
		VTMapEdPrefab componentInParent = GetComponentInParent<VTMapEdPrefab>();
		if ((bool)componentInParent)
		{
			base.gameObject.name = componentInParent.GetDisplayName();
		}
	}

	public bool HasPlayerRequestedLanding()
	{
		if (playerRequestStatus != PlayerRequestStatus.ClearedToLand && playerRequestStatus != PlayerRequestStatus.DirectingToAirbase && playerRequestStatus != PlayerRequestStatus.WaitingForLandingClearance)
		{
			return playerRequestStatus == PlayerRequestStatus.TaxiToParking;
		}
		return true;
	}

	public PlayerLandingReponses PlayerRequestLanding(bool onlyParkHelipad = false)
	{
		if (vtolOnlyLanding)
		{
			if ((bool)voiceProfile)
			{
				voiceProfile.PlayUnableMsg();
			}
			return PlayerLandingReponses.UnableToLand;
		}
		if (playerRequestStatus == PlayerRequestStatus.None)
		{
			Debug.Log("Player requested landing.");
			if (playerRequestRoutine != null)
			{
				StopCoroutine(playerRequestRoutine);
			}
			playerActor.GetComponent<VehicleMaster>();
			VTOLLandingModes vTOLLandingModes = VTOLLandingModes.NoVTOL;
			if (onlyParkHelipad)
			{
				vTOLLandingModes |= VTOLLandingModes.ForceHelipadParking;
			}
			LandingRequestResponse landingRequestResponse = RequestLanding2(playerActor, playerActor.flightInfo.rb.mass, 0f, vTOLLandingModes);
			if (landingRequestResponse.accepted)
			{
				playerRequestRoutine = StartCoroutine(PlayerLandingRoutine(landingRequestResponse.runway, landingRequestResponse.parkingSpace));
				return PlayerLandingReponses.ClearedToLand;
			}
			if (landingRequestResponse.incompatible)
			{
				if ((bool)voiceProfile)
				{
					voiceProfile.PlayUnableMsg();
				}
				return PlayerLandingReponses.UnableToLand;
			}
			if ((bool)voiceProfile)
			{
				voiceProfile.PlayUnableMsg();
			}
			return PlayerLandingReponses.UnableToLand;
		}
		if (HasPlayerRequestedLanding())
		{
			if (playerRequestStatus == PlayerRequestStatus.DirectingToAirbase)
			{
				if ((bool)voiceProfile && (bool)playerLandingRunway)
				{
					Vector3 toPt = playerLandingRunway.transform.position - playerLandingRunway.transform.forward * 4000f;
					float heading = VectorUtils.Bearing(PlayerPosition(), toPt);
					voiceProfile.PlayLandingFlyHeadingMsg(heading, playerLandingRunway);
					WaypointManager.instance.currentWaypoint = base.transform;
				}
			}
			else if (playerRequestStatus == PlayerRequestStatus.WaitingForLandingClearance)
			{
				if ((bool)voiceProfile)
				{
					voiceProfile.PlayLandingPatternFullMsg();
				}
			}
			else if (playerRequestStatus == PlayerRequestStatus.ClearedToLand)
			{
				if ((bool)voiceProfile)
				{
					if (!isCarrier)
					{
						float heading2 = VectorUtils.Bearing(playerLandingRunway.transform.position, playerLandingRunway.transform.position + playerLandingRunway.transform.forward);
						voiceProfile.PlayLandingClearedForRunwayMsg(heading2, playerLandingRunway.parallelDesignation);
					}
					else
					{
						voiceProfile.PlayClearedToLandCarrierMsg();
					}
				}
			}
			else if (playerRequestStatus == PlayerRequestStatus.TaxiToParking)
			{
				if ((bool)voiceProfile)
				{
					voiceProfile.PlayTaxiToParkingMsg();
				}
			}
			else
			{
				Debug.Log("Unhandled player request status when requesting landing: " + playerRequestStatus);
			}
			return PlayerLandingReponses.AlreadyRequested;
		}
		Debug.LogError("Player requested landing but a non-landing request is in progress. Make sure prior requests are cancelled first.");
		return PlayerLandingReponses.UnableToLand;
	}

	private void UnoccupyPlayerParking()
	{
		foreach (ParkingSpace parkingSpace in parkingSpaces)
		{
			if (parkingSpace.occupiedBy == playerActor)
			{
				parkingSpace.UnOccupyParking(playerActor);
			}
		}
	}

	public void CancelPlayerRequest(bool stopLso = true)
	{
		Debug.Log("Player cancelled ATC request.");
		if (stopLso)
		{
			StopLSO();
		}
		if (playerRequestRoutine != null)
		{
			StopCoroutine(playerRequestRoutine);
		}
		playerActor.SetAutoUnoccupyParking(b: true);
		if (!playerActor.flightInfo.isLanded && (bool)playerActor.parkingNode)
		{
			playerActor.parkingNode.UnOccupyParking(playerActor);
		}
		if ((bool)playerLandingRunway)
		{
			playerLandingRunway.UnregisterUsageRequest(playerActor);
			playerLandingRunway.HideLightObjects(playerActor);
			playerLandingRunway = null;
		}
		if ((bool)playerTakeoffRunway)
		{
			playerTakeoffRunway.UnregisterUsageRequest(playerActor);
			playerTakeoffRunway = null;
		}
		if ((bool)_cSpawn)
		{
			_cSpawn.CancelPlayerTakeOff();
		}
		WaypointManager.instance.taxiNodes = null;
		playerRequestStatus = PlayerRequestStatus.None;
	}

	private bool CheckPlayerIsLandedAtAirport()
	{
		Actor actor = FlightSceneManager.instance.playerActor;
		if (actor.flightInfo.isLanded)
		{
			for (int i = 0; i < actor.flightInfo.wheelsController.suspensions.Length; i++)
			{
				RaySpringDamper raySpringDamper = actor.flightInfo.wheelsController.suspensions[i];
				if ((bool)raySpringDamper.touchingCollider && surfaceColliders.Contains(raySpringDamper.touchingCollider))
				{
					return true;
				}
			}
		}
		return false;
	}

	private IEnumerator PlayerLandingRoutine(Runway expectedRunway, ParkingSpace pSpace, PlayerLandingStates resumeState = PlayerLandingStates.None)
	{
		lsoComplete = false;
		playerLandingRunway = expectedRunway;
		if (QuicksaveManager.isQuickload)
		{
			while (!PlayerSpawn.qLoadPlayerComplete)
			{
				yield return null;
			}
		}
		if (resumeState == PlayerLandingStates.None || resumeState == PlayerLandingStates.DirectingToAirbase)
		{
			if (PlayerSqrDist() > landingClearanceSqrRadius)
			{
				Debug.Log("ATC landing routine: Directing player to airbase.");
				playerRequestStatus = PlayerRequestStatus.DirectingToAirbase;
				pLandingState = PlayerLandingStates.DirectingToAirbase;
				resumeState = PlayerLandingStates.None;
				if ((bool)voiceProfile)
				{
					Vector3 toPt = expectedRunway.transform.position - expectedRunway.transform.forward * 4000f;
					float heading = VectorUtils.Bearing(PlayerPosition(), toPt);
					voiceProfile.PlayLandingFlyHeadingMsg(heading, expectedRunway);
					WaypointManager.instance.currentWaypoint = base.transform;
				}
				while (PlayerSqrDist() > landingClearanceSqrRadius)
				{
					if (playerActor.flightInfo.isLanded)
					{
						Debug.Log("ATC landing routine: Player landed somewhere outside of the landing clearance radius.");
						if ((bool)voiceProfile)
						{
							voiceProfile.PlayLandedElseWhereMsg();
						}
						playerRequestStatus = PlayerRequestStatus.None;
						pLandingState = PlayerLandingStates.None;
						yield break;
					}
					yield return new WaitForSeconds(5f);
				}
			}
			if (WaypointManager.instance.currentWaypoint == base.transform)
			{
				WaypointManager.instance.ClearWaypoint();
			}
			bool isHelicopter = FlightSceneManager.instance.playerVehicleMaster.isHelicopter;
			VTOLLandingModes vtolMode = VTOLLandingModes.NoVTOL;
			if (isHelicopter)
			{
				vtolMode = (VTOLLandingModes)12;
			}
			LandingRequestResponse landingRequestResponse = RequestLanding2(playerActor, playerActor.flightInfo.rb.mass, 0f, vtolMode);
			if (!landingRequestResponse.accepted)
			{
				playerLandingRunway = null;
				Debug.Log("ATC landing routine: Airbase has no landing runway for player.");
				if ((bool)voiceProfile)
				{
					voiceProfile.PlayUnableMsg();
				}
				playerRequestStatus = PlayerRequestStatus.None;
				pLandingState = PlayerLandingStates.None;
				yield break;
			}
			playerLandingRunway = landingRequestResponse.runway;
			landingRequestResponse.parkingSpace.OccupyParking(playerActor);
			pSpace = landingRequestResponse.parkingSpace;
			playerLandingRunway.RegisterUsageRequestHighPriority(playerActor);
			playerActor.SetAutoUnoccupyParking(b: false);
		}
		yield return null;
		if (resumeState == PlayerLandingStates.None || resumeState == PlayerLandingStates.WaitingForLandingClearance)
		{
			resumeState = PlayerLandingStates.None;
			if (!playerLandingRunway.IsRunwayUsageAuthorized(playerActor))
			{
				Debug.Log("ATC landing routine: Player is waiting for landing clearance.");
				playerRequestStatus = PlayerRequestStatus.WaitingForLandingClearance;
				pLandingState = PlayerLandingStates.WaitingForLandingClearance;
				if ((bool)voiceProfile)
				{
					voiceProfile.PlayLandingPatternFullMsg();
				}
				while (!playerLandingRunway.IsRunwayUsageAuthorized(playerActor))
				{
					if (playerActor.flightInfo.isLanded)
					{
						Debug.Log("ATC landing routine: Player landed before getting runway usage authorization.");
						playerRequestStatus = PlayerRequestStatus.None;
						pLandingState = PlayerLandingStates.None;
						if ((bool)voiceProfile)
						{
							voiceProfile.PlayLandedBeforeClearanceMsg();
						}
						playerActor.SetAutoUnoccupyParking(b: true);
						SendPlayerTaxiToParkingRoute(pSpace);
						yield break;
					}
					yield return new WaitForSeconds(5f);
				}
			}
		}
		if (resumeState == PlayerLandingStates.None || resumeState == PlayerLandingStates.ClearedToLand)
		{
			if (resumeState == PlayerLandingStates.ClearedToLand)
			{
				while (playerActor.flightInfo.isLanded)
				{
					yield return null;
				}
			}
			resumeState = PlayerLandingStates.None;
			Debug.Log("ATC landing routine: Player is now cleared to land.");
			playerRequestStatus = PlayerRequestStatus.ClearedToLand;
			pLandingState = PlayerLandingStates.ClearedToLand;
			playerLandingRunway.ShowLandingLightObjects(playerActor);
			DashHSI ils = playerActor.GetComponentInChildren<DashHSI>();
			if ((bool)voiceProfile)
			{
				if (!isCarrier)
				{
					float heading2 = VectorUtils.Bearing(playerLandingRunway.transform.position, playerLandingRunway.transform.position + playerLandingRunway.transform.forward);
					voiceProfile.PlayLandingClearedForRunwayMsg(heading2, playerLandingRunway.parallelDesignation);
				}
				else
				{
					voiceProfile.PlayClearedToLandCarrierMsg();
				}
			}
			if ((bool)ils)
			{
				ils.ilsRunway = playerLandingRunway;
			}
			WaypointManager.instance.currentWaypoint = playerLandingRunway.transform;
			bool callTheBall = false;
			bool hasTouchedDown = false;
			bool lsoStarted = false;
			while (true)
			{
				if (isCarrier)
				{
					if (!callTheBall && PlayerReadyToCallBall(3000f, 0.9659258f))
					{
						lsoComplete = false;
						if ((bool)voiceProfile)
						{
							voiceProfile.PlayCallTheBallMsg();
						}
						FlightSceneManager.instance.playerVehicleMaster.comms.CallTheBall(delegate(bool ballCalled)
						{
							if (ballCalled)
							{
								if ((bool)voiceProfile)
								{
									voiceProfile.PlayRogerBallMsg();
									if (!lsoStarted && lsoRoutine == null && !lsoComplete)
									{
										lsoRoutine = StartCoroutine(CarrierLSODirectionRoutine(1200f));
										lsoStarted = true;
									}
								}
							}
							else if (!lsoStarted && lsoRoutine == null && !lsoComplete)
							{
								lsoRoutine = StartCoroutine(CarrierLSODirectionRoutine(999999f));
								lsoStarted = true;
							}
						}, delegate
						{
							if (lsoStarted && !lsoComplete && lsoRoutine != null)
							{
								StopCoroutine(lsoRoutine);
								lsoRoutine = StartCoroutine(LSOWaveOffRoutine());
							}
						});
						callTheBall = true;
					}
					if (!lsoStarted && (bool)voiceProfile && !lsoComplete && lsoRoutine == null && PlayerReadyToCallBall(1200f, 0.9659258f))
					{
						lsoRoutine = StartCoroutine(CarrierLSODirectionRoutine(1200f));
						lsoStarted = true;
					}
				}
				yield return null;
				if (!playerActor.alive)
				{
					Debug.Log("ATC landing routine: Player vehicle died while in ATC landing routine");
					StopLSO();
					playerRequestStatus = PlayerRequestStatus.None;
					pLandingState = PlayerLandingStates.None;
					yield break;
				}
				if (playerActor.flightInfo.altitudeASL < 0f)
				{
					Debug.Log("ATC landing routine: Player vehicle sunk while in ATC landing routine");
					StopLSO();
					playerRequestStatus = PlayerRequestStatus.None;
					pLandingState = PlayerLandingStates.None;
					yield break;
				}
				if (playerActor.flightInfo.isLanded)
				{
					if (playerActor.flightInfo.surfaceSpeed < 25f)
					{
						break;
					}
					hasTouchedDown = true;
					if (WaypointManager.instance.currentWaypoint == playerLandingRunway.transform)
					{
						WaypointManager.instance.ClearWaypoint();
					}
				}
				if (hasTouchedDown && playerActor.flightInfo.radarAltitude > 100f && playerActor.flightInfo.verticalSpeed > 10f)
				{
					Debug.Log("ATC landing routine: Player did touch & go");
					playerActor.SetAutoUnoccupyParking(b: true);
					playerLandingRunway.UnregisterUsageRequest(playerActor);
					playerLandingRunway = null;
					if ((bool)ils)
					{
						ils.ilsRunway = null;
					}
					StopLSO();
					playerRequestStatus = PlayerRequestStatus.None;
					pLandingState = PlayerLandingStates.None;
					yield break;
				}
			}
			if (playerLandingRunway.clearanceBounds.Contains(playerLandingRunway.transform.InverseTransformPoint(playerActor.position)))
			{
				Debug.Log("ATC landing routine: player landed successfully within runway bounds");
			}
			else
			{
				Debug.Log("ATC landing routine: player landed outside runway bounds");
			}
			if ((bool)ils)
			{
				ils.ilsRunway = null;
			}
			playerActor.SetAutoUnoccupyParking(b: true);
			while (playerActor.flightInfo.surfaceSpeed > 25f)
			{
				yield return null;
			}
		}
		if (resumeState == PlayerLandingStates.None || resumeState == PlayerLandingStates.TaxiToParking)
		{
			pLandingState = PlayerLandingStates.TaxiToParking;
			SendPlayerTaxiToParkingRoute(pSpace);
			StopLSO();
			while (playerLandingRunway.clearanceBounds.Contains(playerLandingRunway.transform.InverseTransformPoint(playerActor.position)))
			{
				yield return new WaitForSeconds(5f);
			}
			Debug.Log("Player has exited runway.");
		}
		playerLandingRunway.UnregisterUsageRequest(playerActor);
		playerLandingRunway.HideLightObjects(playerActor);
		playerLandingRunway = null;
		playerRequestStatus = PlayerRequestStatus.None;
	}

	private void StopLSO()
	{
		Debug.Log("LSO Stopped");
		CommRadioManager.instance.EndLiveRadio();
		lsoComplete = true;
		if (lsoRoutine != null)
		{
			StopCoroutine(lsoRoutine);
			lsoRoutine = null;
		}
	}

	private IEnumerator LSOWaveOffRoutine(bool callBolter = true)
	{
		float waveOffCheckSqr = waveOffCheckDist * waveOffCheckDist;
		Actor pActor = FlightSceneManager.instance.playerActor;
		Tailhook hook = pActor.GetComponentInChildren<Tailhook>();
		Transform finalCableTf = carrierCables[carrierCables.Length - 1].transform;
		bool cancelLanding = true;
		bool playedWOBolter = false;
		while (true)
		{
			if ((hook.hookPointTf.position - carrierOlsTransform.position).sqrMagnitude > waveOffCheckSqr)
			{
				voiceProfile.PlayLSOReturnToHolding();
				break;
			}
			if (pActor.flightInfo.isLanded)
			{
				if ((bool)hook.hookedCable)
				{
					int num = carrierCables.IndexOf(hook.hookedCable);
					FlightLogger.Log("LSO: Player caught wire " + (num + 1) + " (waved off)");
					voiceProfile.PlayLSOXwire(num);
					cancelLanding = false;
					break;
				}
				if (callBolter && !playedWOBolter && Vector3.Dot(hook.hookPointTf.position - finalCableTf.position, finalCableTf.forward) > 3f)
				{
					playedWOBolter = true;
					voiceProfile.PlayLSOBolter();
					FlightLogger.Log("LSO: Bolter! (waved off)");
				}
			}
			yield return null;
		}
		if (cancelLanding)
		{
			CancelPlayerRequest(stopLso: false);
		}
		CommRadioManager.instance.EndLiveRadio();
		yield return new WaitForSeconds(5f);
		lsoComplete = true;
		if (cancelLanding && playerRequestStatus == PlayerRequestStatus.None && !pActor.flightInfo.isLanded)
		{
			PlayerRequestLanding();
		}
		StopLSO();
	}

	private IEnumerator CarrierLSODirectionRoutine(float startDist)
	{
		lsoComplete = false;
		Actor pActor = FlightSceneManager.instance.playerActor;
		Tailhook hook = pActor.GetComponentInChildren<Tailhook>();
		float waveOffMinSqr = waveOffMinDist * waveOffMinDist;
		float waveOffCheckSqr = waveOffCheckDist * waveOffCheckDist;
		float sqrDistThresh = startDist * startDist;
		Transform finalCableTf = carrierCables[carrierCables.Length - 1].transform;
		while ((carrierOlsTransform.position - hook.hookPointTf.position).sqrMagnitude > sqrDistThresh)
		{
			yield return null;
		}
		yield return null;
		yield return null;
		CommRadioManager.instance.BeginLiveRadio(duckBgm: true);
		float lso_lastCallTime = 0f;
		float lso_callInterval = 1.75f;
		float lso_callIntervalLong = 5f;
		int lso_lastCallID = -1;
		Action<int, Action> PlayIntervalMsg = delegate(int id, Action action)
		{
			float num7 = ((lso_lastCallID != id) ? lso_callInterval : lso_callIntervalLong);
			lso_lastCallID = id;
			if (Time.time - lso_lastCallTime > num7)
			{
				lso_lastCallTime = Time.time;
				action();
			}
		};
		int aoaIdx = 0;
		int AVG_AOA_COUNT = 10;
		float avgAoaInterval = 0.2f;
		float avgAoaTime = 0f;
		float num = Mathf.Lerp(waveOffAoA.min, waveOffAoA.max, 0.5f);
		float avgAoa2 = num;
		float[] AoAs = new float[AVG_AOA_COUNT];
		for (int i = 0; i < AVG_AOA_COUNT; i++)
		{
			AoAs[i] = num;
		}
		while (true)
		{
			if (Vector3.Dot(hook.hookPointTf.position - finalCableTf.position, finalCableTf.forward) > 3f && !hook.hookedCable)
			{
				if (pActor.flightInfo.isLanded)
				{
					FlightLogger.Log("LSO: Bolter!");
					voiceProfile.PlayLSOBolter();
					lsoRoutine = StartCoroutine(LSOWaveOffRoutine(callBolter: false));
				}
				else
				{
					FlightLogger.Log("LSO: Wave off - overshot");
					voiceProfile.PlayLSOWaveOff();
					lsoRoutine = StartCoroutine(LSOWaveOffRoutine());
				}
				yield break;
			}
			float num2 = Mathf.Min((carrierOlsTransform.position - hook.hookPointTf.position).magnitude / (playerActor.velocity - carrierSpawn.actor.velocity).magnitude, 1.5f);
			Vector3 vector = hook.hookPointTf.position + playerActor.velocity * num2 + 0.5f * playerActor.flightInfo.acceleration * num2 * num2;
			vector -= _cSpawn.actor.velocity * num2;
			float num3 = Vector3.Dot((vector - carrierOlsTransform.position).normalized, carrierOlsTransform.forward);
			float num4 = Vector3.Dot((hook.hookPointTf.position - carrierOlsTransform.position).normalized, carrierOlsTransform.forward);
			if (Time.time - avgAoaTime > avgAoaInterval)
			{
				avgAoa2 = 0f;
				for (int j = 0; j < AVG_AOA_COUNT; j++)
				{
					avgAoa2 += AoAs[j];
				}
				avgAoa2 /= (float)AVG_AOA_COUNT;
				AoAs[aoaIdx] = pActor.flightInfo.aoa;
				aoaIdx = (aoaIdx + 1) % AVG_AOA_COUNT;
			}
			float sqrMagnitude = (carrierOlsTransform.position - hook.hookPointTf.position).sqrMagnitude;
			if (sqrMagnitude < waveOffCheckSqr && sqrMagnitude > waveOffMinSqr && !pActor.flightInfo.isLanded)
			{
				bool flag = false;
				if (avgAoa2 > waveOffAoA.max || avgAoa2 < waveOffAoA.min)
				{
					voiceProfile.PlayLSOWaveOff();
					FlightLogger.Log("LSO: Wave off - AoA");
					flag = true;
				}
				else if (num4 < waveOffDotThresh)
				{
					voiceProfile.PlayLSOWaveOff();
					FlightLogger.Log("LSO: Wave off - bad alignment");
					flag = true;
				}
				else if (pActor.flightInfo.wheelsController.gearAnimator.state != 0)
				{
					voiceProfile.PlayLSOWaveOff();
					FlightLogger.Log("LSO: Wave off - landing gear");
				}
				else if ((bool)playerLandingRunway && !playerLandingRunway.IsRunwayClear(playerActor))
				{
					voiceProfile.PlayLSOFoulDeck();
					FlightLogger.Log("LSO: Wave off - foul deck");
					flag = true;
				}
				if (flag)
				{
					if ((bool)ols)
					{
						ols.BeginWaveOffSequence();
					}
					lsoRoutine = StartCoroutine(LSOWaveOffRoutine());
					yield break;
				}
			}
			if (pActor.flightInfo.isLanded && (bool)hook.hookedCable)
			{
				break;
			}
			if (sqrMagnitude > waveOffMinSqr)
			{
				if (num3 > linedUpDotThresh)
				{
					PlayIntervalMsg(0, voiceProfile.PlayLSOLinedUp);
				}
				else
				{
					Vector3 toDirection = carrierOlsTransform.InverseTransformPoint(vector);
					toDirection.z = 0f;
					float num5 = VectorUtils.SignedAngle(Vector3.up, toDirection, Vector3.right);
					if (Mathf.Abs(num5) < 22.5f)
					{
						PlayIntervalMsg(1, voiceProfile.PlayLSOYoureHigh);
					}
					else
					{
						if (num5 < 0f)
						{
							num5 += 360f;
						}
						if (num5 < 67.5f)
						{
							PlayIntervalMsg(2, voiceProfile.PlayLSOHighRight);
						}
						else if (num5 < 112.5f)
						{
							PlayIntervalMsg(3, voiceProfile.PlayLSORightForLineup);
						}
						else if (num5 < 157.5f)
						{
							PlayIntervalMsg(4, voiceProfile.PlayLSOLowRight);
						}
						else if (num5 < 202.5f)
						{
							PlayIntervalMsg(5, voiceProfile.PlayLSOPowerLow);
						}
						else if (num5 < 247.5f)
						{
							PlayIntervalMsg(6, voiceProfile.PlayLSOLowLeft);
						}
						else if (num5 < 292.5f)
						{
							PlayIntervalMsg(7, voiceProfile.PlayLSOComeLeft);
						}
						else
						{
							PlayIntervalMsg(8, voiceProfile.PlayLSOHighLeft);
						}
					}
				}
			}
			yield return null;
		}
		int num6 = carrierCables.IndexOf(hook.hookedCable);
		FlightLogger.Log("LSO: Player caught wire " + (num6 + 1));
		voiceProfile.PlayLSOXwire(num6);
		StopLSO();
	}

	private void SendPlayerTaxiToParkingRoute(ParkingSpace pSpace)
	{
		if (!navigation)
		{
			return;
		}
		playerRequestStatus = PlayerRequestStatus.TaxiToParking;
		if (isCarrier)
		{
			foreach (AirbaseNavNode navNode in navigation.navNodes)
			{
				if (navNode == pSpace.parkingNode)
				{
					WaypointManager.instance.currentWaypoint = navNode.transform;
					if ((bool)voiceProfile)
					{
						voiceProfile.PlayTaxiToParkingMsg();
					}
					StartCoroutine(ClearTaxiWpRoutine(navNode.transform));
					break;
				}
			}
			return;
		}
		List<AirbaseNavNode> parkingPath = navigation.GetParkingPath(playerActor.position, playerActor.transform.forward, pSpace.parkingNode);
		if (parkingPath != null)
		{
			WaypointManager.instance.taxiNodes = parkingPath;
			AirbaseNavNode airbaseNavNode = parkingPath[parkingPath.Count - 1];
			WaypointManager.instance.currentWaypoint = airbaseNavNode.transform;
			StartCoroutine(ClearTaxiWpRoutine(airbaseNavNode.transform));
			if ((bool)voiceProfile)
			{
				voiceProfile.PlayTaxiToParkingMsg();
			}
		}
	}

	private IEnumerator ClearTaxiWpRoutine(Transform tf, bool checkIsLanded = true)
	{
		while ((bool)playerActor && WaypointManager.instance.currentWaypoint == tf && (!checkIsLanded || playerActor.flightInfo.isLanded))
		{
			if ((playerActor.position - tf.position).sqrMagnitude < 225f)
			{
				WaypointManager.instance.ClearWaypoint();
				yield break;
			}
			yield return null;
		}
		if (WaypointManager.instance.currentWaypoint == tf)
		{
			WaypointManager.instance.ClearWaypoint();
		}
	}

	private bool PlayerReadyToCallBall(float dist, float dotThresh)
	{
		Vector3 velocity = FlightSceneManager.instance.playerActor.velocity;
		Vector3 position = FlightSceneManager.instance.playerActor.position;
		Runway runway = runways[0];
		float sqrMagnitude = (runway.transform.position - position).sqrMagnitude;
		if (sqrMagnitude < dist * dist && sqrMagnitude > 10000f && (FlightSceneManager.instance.playerActor.flightInfo.wheelsController.gearAnimator.state == GearAnimator.GearStates.Extended || FlightSceneManager.instance.playerActor.flightInfo.airspeed < 150f))
		{
			Vector3 vector = runway.transform.position - position;
			Vector3 rhs = -carrierOlsTransform.forward;
			if (Vector3.Dot(velocity.normalized, rhs) > dotThresh && Vector3.Dot(vector.normalized, rhs) > dotThresh)
			{
				return true;
			}
		}
		return false;
	}

	private Vector3 PlayerPosition()
	{
		return FlightSceneManager.instance.playerActor.position;
	}

	private float PlayerSqrDist()
	{
		return (base.transform.position - PlayerPosition()).sqrMagnitude;
	}

	private bool CarrierHasTakeoffRunway()
	{
		if (runways != null)
		{
			Runway[] array = runways;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].takeoff)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool HasPlayerRequestedVerticalLanding()
	{
		if (playerRequestStatus != PlayerRequestStatus.DirectingToAirbaseVertical)
		{
			return playerRequestStatus == PlayerRequestStatus.ClearedToLandVertical;
		}
		return true;
	}

	public bool HasPlayerRequestedVerticalTakeOff()
	{
		return playerRequestStatus == PlayerRequestStatus.ClearedToTakeoffVertical;
	}

	public bool HasPlayerRequestedTakeOff()
	{
		if (playerRequestStatus != PlayerRequestStatus.PreCatapult && playerRequestStatus != PlayerRequestStatus.RunUpEnginesCatapult && playerRequestStatus != PlayerRequestStatus.ClearedToTakeoff && playerRequestStatus != PlayerRequestStatus.TaxiToCatapult && playerRequestStatus != PlayerRequestStatus.TaxiToRunway && playerRequestStatus != PlayerRequestStatus.WaitingForCatapultClearance)
		{
			return playerRequestStatus == PlayerRequestStatus.WaitingForTakeoffClearance;
		}
		return true;
	}

	public bool PlayerRequestTakeoff()
	{
		Debug.Log("Player requested takeoff.");
		if (!CheckPlayerIsLandedAtAirport())
		{
			Debug.LogFormat(base.gameObject, "Player requested take off here but is not landed at this airbase. ({0})", airportName);
			if ((bool)voiceProfile)
			{
				voiceProfile.PlayRequestedWrongATCMsg();
			}
			return false;
		}
		if (playerRequestStatus != 0)
		{
			bool flag = false;
			if (isCarrier)
			{
				if (playerRequestStatus == PlayerRequestStatus.WaitingForCatapultClearance)
				{
					if ((bool)voiceProfile)
					{
						voiceProfile.PlayWaitForCatapultClearanceMsg();
					}
					flag = true;
				}
				else if (playerRequestStatus == PlayerRequestStatus.TaxiToCatapult)
				{
					if ((bool)voiceProfile)
					{
						voiceProfile.PlayTaxiToCatapultMsg(playerCat);
					}
					flag = true;
				}
				else if (playerRequestStatus == PlayerRequestStatus.RunUpEnginesCatapult)
				{
					if ((bool)voiceProfile)
					{
						voiceProfile.PlayRunUpEnginesCatapultMsg();
					}
					flag = true;
				}
			}
			else if (playerRequestStatus == PlayerRequestStatus.TaxiToRunway)
			{
				if ((bool)voiceProfile)
				{
					voiceProfile.PlayTaxiToRunwayMsg(takeoffRunwayHeading);
				}
				flag = true;
			}
			else if (playerRequestStatus == PlayerRequestStatus.WaitingForTakeoffClearance)
			{
				if ((bool)voiceProfile)
				{
					voiceProfile.PlayHoldShortAtRunwayMsg(takeoffRunwayHeading);
				}
				flag = true;
			}
			else if (playerRequestStatus == PlayerRequestStatus.ClearedToTakeoff)
			{
				if ((bool)voiceProfile)
				{
					voiceProfile.PlayClearForTakeoffRunwayMsg(takeoffRunwayHeading);
				}
				flag = true;
			}
			if (flag)
			{
				return false;
			}
			Debug.Log("Player requested take off while another request was in progress.  Cancelling older request. (" + playerRequestStatus.ToString() + ")");
		}
		CancelPlayerRequest();
		if (isCarrier)
		{
			CarrierCatapult carrierCatapult = _cSpawn.RegisterPlayerTakeoffRequest();
			if ((bool)carrierCatapult)
			{
				playerRequestRoutine = StartCoroutine(PlayerTakeoffCarrierRoutine(carrierCatapult));
				return true;
			}
			if (CarrierHasTakeoffRunway())
			{
				playerRequestRoutine = StartCoroutine(PlayerTakeOffAssaultShipRoutine());
				return true;
			}
			Debug.Log("No cat or runway found for player.");
			if ((bool)voiceProfile)
			{
				voiceProfile.PlayUnableMsg();
			}
			return false;
		}
		playerRequestRoutine = StartCoroutine(PlayerTakeOffRoutine());
		return true;
	}

	public bool PlayerRequestVerticalTakeoff()
	{
		Debug.Log("Player requested vertical take off.", base.gameObject);
		if (!CheckPlayerIsLandedAtAirport())
		{
			if ((bool)voiceProfile)
			{
				voiceProfile.PlayRequestedWrongATCMsg();
			}
			return false;
		}
		if (!FlightSceneManager.instance.playerActor.GetComponent<VehicleMaster>().isVTOLCapable)
		{
			if ((bool)voiceProfile)
			{
				voiceProfile.PlayUnableMsg();
			}
			return false;
		}
		if ((bool)voiceProfile)
		{
			voiceProfile.PlayClearedVerticalTakeoffMsg();
		}
		playerRequestRoutine = StartCoroutine(PlayerVerticalTakeoffRoutine());
		return true;
	}

	private IEnumerator PlayerVerticalTakeoffRoutine()
	{
		playerRequestStatus = PlayerRequestStatus.ClearedToTakeoffVertical;
		while (playerActor.flightInfo.isLanded)
		{
			yield return null;
		}
		playerRequestStatus = PlayerRequestStatus.None;
	}

	public PlayerLandingReponses PlayerRequestVerticalLanding(out Transform landingPadTf, out ParkingSpace pSpace)
	{
		Debug.Log("Player requested vertical landing", base.gameObject);
		pSpace = null;
		if (FlightSceneManager.instance.playerActor.GetComponent<VehicleMaster>().isVTOLCapable)
		{
			LandingRequestResponse landingRequestResponse = RequestLanding2(playerActor, playerActor.GetComponent<Rigidbody>().mass, 0f, VTOLLandingModes.ForceVTOL);
			if (landingRequestResponse.accepted)
			{
				landingPadTf = landingRequestResponse.landingPad;
				pSpace = landingRequestResponse.parkingSpace;
				float parkingRadius = -1f;
				if ((bool)landingRequestResponse.parkingSpace)
				{
					parkingRadius = landingRequestResponse.parkingSpace.parkingSize;
				}
				playerRequestRoutine = StartCoroutine(PlayerVerticalLandingRoutine(landingPadTf, parkingRadius, landingPads.IndexOf(landingPadTf)));
				WaypointManager.instance.currentWaypoint = landingRequestResponse.landingPad.transform;
				StartCoroutine(ClearTaxiWpRoutine(landingRequestResponse.landingPad.transform, checkIsLanded: false));
				return PlayerLandingReponses.ClearedToLand;
			}
			if (landingRequestResponse.incompatible)
			{
				if ((bool)voiceProfile)
				{
					voiceProfile.PlayUnableMsg();
				}
				landingPadTf = null;
				return PlayerLandingReponses.UnableToLand;
			}
			if ((bool)voiceProfile)
			{
				voiceProfile.PlayLandingPatternFullMsg();
			}
			landingPadTf = null;
			return PlayerLandingReponses.UnableToLand;
		}
		if ((bool)voiceProfile)
		{
			voiceProfile.PlayUnableMsg();
		}
		landingPadTf = null;
		return PlayerLandingReponses.UnableToLand;
	}

	private IEnumerator PlayerVerticalLandingRoutine(Transform padTf, float parkingRadius, int padIdx)
	{
		WaitForSeconds secWait = new WaitForSeconds(1f);
		float sqrThresh = 36000000f;
		if (Vector3.ProjectOnPlane(playerActor.position - padTf.position, Vector3.up).sqrMagnitude > sqrThresh)
		{
			playerRequestStatus = PlayerRequestStatus.DirectingToAirbaseVertical;
			if ((bool)voiceProfile)
			{
				float heading = VectorUtils.Bearing(padTf.position - playerActor.position);
				voiceProfile.PlayVerticalLandingFlyHeadingMsg(heading);
			}
			yield return secWait;
			while (Vector3.ProjectOnPlane(playerActor.position - padTf.position, Vector3.up).sqrMagnitude > sqrThresh)
			{
				yield return secWait;
			}
		}
		if (WaypointManager.instance.currentWaypoint == base.transform)
		{
			WaypointManager.instance.ClearWaypoint();
		}
		playerRequestStatus = PlayerRequestStatus.ClearedToLandVertical;
		if ((bool)voiceProfile)
		{
			voiceProfile.PlayClearedVerticalLandingMsg(padIdx + 1);
		}
		if (parkingRadius < 0f)
		{
			parkingRadius = 15f;
		}
		while (!playerActor.flightInfo.isLanded)
		{
			yield return null;
		}
		playerActor.SetAutoUnoccupyParking(b: true);
		if (playerActor.alive && playerActor.flightInfo.isLanded)
		{
			Vector3 vector = padTf.position - playerActor.position;
			vector.y = 0f;
			if (vector.magnitude > parkingRadius)
			{
				FlightLogger.Log($"{playerActor.actorName} did not land at the designated landing pad!");
			}
			else
			{
				FlightLogger.Log($"{playerActor.actorName} successfully landed on the designated landing pad.");
			}
		}
		playerRequestStatus = PlayerRequestStatus.None;
	}

	private IEnumerator PlayerTakeoffCarrierRoutine(CarrierCatapult playerCat)
	{
		this.playerCat = playerCat;
		WaitForSeconds secWait = new WaitForSeconds(1f);
		playerRequestStatus = PlayerRequestStatus.WaitingForCatapultClearance;
		yield return new WaitForSeconds(0.6f);
		if (!_cSpawn.IsAuthorizedForTakeoff(playerActor) && (bool)voiceProfile)
		{
			voiceProfile.PlayWaitForCatapultClearanceMsg();
		}
		while (!_cSpawn.IsAuthorizedForTakeoff(playerActor))
		{
			yield return secWait;
			if (!playerActor.alive || !playerActor.flightInfo.isLanded)
			{
				Debug.Log("Player died or took off while waiting for cat clearance.");
				playerRequestStatus = PlayerRequestStatus.None;
				yield break;
			}
		}
		playerRequestStatus = PlayerRequestStatus.TaxiToCatapult;
		if ((bool)voiceProfile)
		{
			voiceProfile.PlayTaxiToCatapultMsg(playerCat);
		}
		WaypointManager.instance.currentWaypoint = playerCat.transform;
		CatapultHook playerHook = playerActor.GetComponentInChildren<CatapultHook>();
		while (!playerHook.hooked)
		{
			yield return null;
			if (!playerActor.alive || !playerActor.flightInfo.isLanded)
			{
				Debug.Log("Player died or took off while carrier expected them to taxi to catapult.");
				playerRequestStatus = PlayerRequestStatus.None;
				yield break;
			}
		}
		playerRequestStatus = PlayerRequestStatus.PreCatapult;
		if ((bool)voiceProfile)
		{
			voiceProfile.PlayPreCatapultMsg();
		}
		if (WaypointManager.instance.currentWaypoint == playerCat.transform)
		{
			WaypointManager.instance.ClearWaypoint();
		}
		for (int i = 0; i < 10; i++)
		{
			yield return secWait;
		}
		playerRequestStatus = PlayerRequestStatus.RunUpEnginesCatapult;
		if ((bool)voiceProfile)
		{
			voiceProfile.PlayRunUpEnginesCatapultMsg();
		}
		playerRequestStatus = PlayerRequestStatus.None;
	}

	private IEnumerator PlayerTakeOffAssaultShipRoutine()
	{
		Runway[] array = runways;
		foreach (Runway runway in array)
		{
			if (runway.takeoff)
			{
				playerTakeoffRunway = runway;
				break;
			}
		}
		playerTakeoffRunway.RegisterUsageRequest(playerActor);
		bool msgWaiting = false;
		while (!playerTakeoffRunway.IsRunwayUsageAuthorized(playerActor))
		{
			if (!msgWaiting)
			{
				msgWaiting = true;
				if ((bool)voiceProfile)
				{
					voiceProfile.PlayWaitForCatapultClearanceMsg();
				}
			}
			playerRequestStatus = PlayerRequestStatus.WaitingForTakeoffClearance;
			yield return new WaitForSeconds(1f);
		}
		playerRequestStatus = PlayerRequestStatus.ClearedToTakeoff;
		if ((bool)voiceProfile)
		{
			voiceProfile.PlayClearForTakeoffRunwayMsg(VectorUtils.Bearing(playerTakeoffRunway.transform.forward));
		}
		while (playerActor.flightInfo.isLanded)
		{
			yield return new WaitForSeconds(5f);
		}
		playerTakeoffRunway.UnregisterUsageRequest(playerActor);
		playerRequestStatus = PlayerRequestStatus.None;
	}

	private IEnumerator PlayerTakeOffRoutine()
	{
		List<AirbaseNavNode> takeoffPath = navigation.GetTakeoffPath(PlayerPosition(), playerActor.transform.forward, out playerTakeoffRunway, 1f);
		WaypointManager.instance.taxiNodes = takeoffPath;
		if (playerTakeoffRunway == null)
		{
			playerTakeoffRunway = GetNextTakeoffRunway();
		}
		playerRequestStatus = PlayerRequestStatus.TaxiToRunway;
		takeoffRunwayHeading = VectorUtils.Bearing(playerTakeoffRunway.transform.position, playerTakeoffRunway.transform.position + playerTakeoffRunway.transform.forward);
		if ((bool)voiceProfile)
		{
			voiceProfile.PlayTaxiToRunwayMsg(takeoffRunwayHeading);
		}
		while (!playerTakeoffRunway.shortHoldTriggerBounds.Contains(playerTakeoffRunway.transform.InverseTransformPoint(playerActor.position + playerActor.velocity * 4f)))
		{
			yield return null;
		}
		bool flag = false;
		Runway[] array = runways;
		for (int i = 0; i < array.Length; i++)
		{
			Actor authorizedUser = array[i].GetAuthorizedUser();
			if (authorizedUser != null && authorizedUser != playerActor)
			{
				flag = true;
				break;
			}
		}
		bool toldToHold = false;
		if (flag)
		{
			toldToHold = true;
			if ((bool)voiceProfile)
			{
				voiceProfile.PlayHoldShortAtRunwayMsg(takeoffRunwayHeading);
			}
		}
		while (!playerTakeoffRunway.shortHoldTriggerBounds.Contains(playerTakeoffRunway.transform.InverseTransformPoint(playerActor.position)))
		{
			yield return null;
		}
		playerTakeoffRunway.RegisterUsageRequestHighPriority(playerActor);
		if (playerActor.parkingNode != null)
		{
			playerActor.parkingNode.UnOccupyParking(playerActor);
		}
		yield return null;
		if (!playerTakeoffRunway.IsRunwayUsageAuthorized(playerActor))
		{
			playerRequestStatus = PlayerRequestStatus.WaitingForTakeoffClearance;
			if (!toldToHold && (bool)voiceProfile)
			{
				voiceProfile.PlayHoldShortAtRunwayMsg(takeoffRunwayHeading);
			}
			while (!playerTakeoffRunway.IsRunwayUsageAuthorized(playerActor))
			{
				yield return new WaitForSeconds(0.5f);
			}
		}
		else
		{
			yield return new WaitForSeconds(0.5f);
		}
		playerRequestStatus = PlayerRequestStatus.ClearedToTakeoff;
		playerTakeoffRunway.ShowTakeoffLightObjects(playerActor);
		if ((bool)voiceProfile)
		{
			voiceProfile.PlayClearForTakeoffRunwayMsg(takeoffRunwayHeading);
		}
		while (playerActor.flightInfo.isLanded)
		{
			yield return new WaitForSeconds(1f);
		}
		playerTakeoffRunway.UnregisterUsageRequest(playerActor);
		playerRequestStatus = PlayerRequestStatus.None;
		playerTakeoffRunway.HideLightObjects(playerActor);
	}

	public static ConfigNode SaveParkingSpaceToConfigNode(string nodeName, ParkingSpace pSpace)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		AirportManager airport = pSpace.airport;
		int value = airport.parkingSpaces.IndexOf(pSpace);
		configNode.SetValue("pSpaceIdx", value);
		if (airport.isCarrier)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(airport._cSpawn.actor, "carrierActor"));
		}
		else
		{
			configNode.SetValue("apIdx", VTMapManager.fetch.airports.IndexOf(airport));
		}
		return configNode;
	}

	public static ParkingSpace RetrieveParkingSpaceFromConfigNode(ConfigNode node)
	{
		int value = node.GetValue<int>("pSpaceIdx");
		ConfigNode node2 = node.GetNode("carrierActor");
		if (node2 != null)
		{
			return ((AICarrierSpawn)QuicksaveManager.RetrieveActorFromNode(node2).unitSpawn).airportManager.parkingSpaces[value];
		}
		return VTMapManager.fetch.airports[node.GetValue<int>("apIdx")].parkingSpaces[value];
	}

	public static ConfigNode SaveAirportReferenceToNode(string nodeName, AirportManager ap)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		configNode.SetValue("isCarrier", ap.isCarrier);
		if (ap.isCarrier)
		{
			configNode.SetValue("unitID", ap.carrierSpawn.unitID);
		}
		else
		{
			configNode.SetValue("apIdx", VTMapManager.fetch.airports.IndexOf(ap));
		}
		return configNode;
	}

	public static AirportManager RetrieveAirportReferenceFromNode(ConfigNode node)
	{
		if (node.GetValue<bool>("isCarrier"))
		{
			int value = node.GetValue<int>("unitID");
			return ((AICarrierSpawn)VTScenario.current.units.GetUnit(value).spawnedUnit).airportManager;
		}
		int value2 = node.GetValue<int>("apIdx");
		return VTMapManager.fetch.airports[value2];
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode apNode = qsNode.AddNode("AirportManager");
		CommonQuicksave(apNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("AirportManager");
		if (node != null)
		{
			CommonQuickload(node);
		}
	}

	private void Instance_OnQuicksave(ConfigNode configNode)
	{
		ConfigNode configNode2 = configNode.AddNode(mapAirbaseNodeName);
		if (configNode2 != null)
		{
			CommonQuicksave(configNode2);
		}
	}

	private void Instance_OnQuickload(ConfigNode configNode)
	{
		ConfigNode node = configNode.GetNode(mapAirbaseNodeName);
		if (node != null)
		{
			CommonQuickload(node);
		}
	}

	private void CommonQuicksave(ConfigNode apNode)
	{
		for (int i = 0; i < runways.Length; i++)
		{
			ConfigNode node = runways[i].SaveToQsNode("runway_" + i);
			apNode.AddNode(node);
		}
		if ((bool)navigation)
		{
			for (int j = 0; j < navigation.navNodes.Count; j++)
			{
				ConfigNode configNode = navigation.navNodes[j].QuicksaveToNode("navNode");
				configNode.SetValue("idx", j);
				apNode.AddNode(configNode);
			}
		}
		apNode.SetValue("playerRequestStatus", playerRequestStatus);
		apNode.SetValue("pLandingState", pLandingState);
		apNode.SetValue("playerLandingRunway", runways.IndexOf(playerLandingRunway));
		if ((bool)playerActor.parkingNode && playerActor.parkingNode.airport == this)
		{
			Debug.Log("Saving player parking node: " + playerActor.parkingNode.idx);
			apNode.AddNode(SaveParkingSpaceToConfigNode("playerParkingNode", playerActor.parkingNode));
		}
	}

	private void CommonQuickload(ConfigNode apNode)
	{
		for (int i = 0; i < runways.Length; i++)
		{
			string text = "runway_" + i;
			if (apNode.HasNode(text))
			{
				ConfigNode node = apNode.GetNode(text);
				runways[i].LoadFromQsNode(node);
			}
		}
		if ((bool)navigation)
		{
			foreach (ConfigNode node2 in apNode.GetNodes("navNode"))
			{
				int value = node2.GetValue<int>("idx");
				navigation.navNodes[value].QuickloadFromNode(node2);
			}
		}
		apNode.GetValue<PlayerRequestStatus>("playerRequestStatus");
		PlayerLandingStates value2 = apNode.GetValue<PlayerLandingStates>("pLandingState");
		if (value2 == PlayerLandingStates.None)
		{
			return;
		}
		int value3 = apNode.GetValue<int>("playerLandingRunway");
		if (value3 >= 0)
		{
			ParkingSpace parkingSpace = playerActor.parkingNode;
			if (parkingSpace != null)
			{
				Debug.Log("Quickloaded player parking node for landing routine (from actor): " + parkingSpace.idx);
			}
			if (apNode.HasNode("playerParkingNode"))
			{
				parkingSpace = RetrieveParkingSpaceFromConfigNode(apNode.GetNode("playerParkingNode"));
				Debug.Log("Quickloaded player parking node for landing routine (from apNode): " + parkingSpace.idx);
			}
			playerRequestRoutine = StartCoroutine(PlayerLandingRoutine(runways[value3], parkingSpace, value2));
		}
	}
}
