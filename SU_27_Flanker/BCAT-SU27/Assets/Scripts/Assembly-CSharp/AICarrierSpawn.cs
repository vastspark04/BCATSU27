using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICarrierSpawn : AISeaUnitSpawn, IHasAirport, IHasRTBWaypoint
{
	public const string CSPAWN_DATA_NAME = "carrierSpawns";

	public Runway runway;

	public AirportManager airportManager;

	public Transform rtbWaypoint;

	public bool usesCatapults = true;

	public List<CarrierSpawnPoint> spawnPoints;

	public List<CarrierSpawnableUnit> spawnableUnits;

	public GameObject customSpawnEditorPrefab;

	private UnitSpawner[] carrierSpawners;

	private List<Actor> vtolTakeOffUnits = new List<Actor>();

	private List<Actor> takeoffAuthorizedActors = new List<Actor>();

	private Dictionary<CarrierCatapult, Actor> catOccupied = new Dictionary<CarrierCatapult, Actor>();

	private List<UnitSpawn> takeoffRequesters = new List<UnitSpawn>();

	private CarrierCatapult playerCat;

	private Coroutine deauthPlayerTakeoffRoutine;

	private bool landingMode;

	private List<AIPilot> landingPilots = new List<AIPilot>();

	public void RegisterVTOLTakeoffUnit(Actor a)
	{
		if (!vtolTakeOffUnits.Contains(a))
		{
			vtolTakeOffUnits.Add(a);
		}
	}

	public void UnregisterVTOLTakeoffUnit(Actor a)
	{
		vtolTakeOffUnits.Remove(a);
	}

	public bool IsVTOLTakeoffAuthorized(Actor a)
	{
		if (vtolTakeOffUnits.Count > 0)
		{
			return vtolTakeOffUnits[0] == a;
		}
		return false;
	}

	public override void OnPreSpawnUnit()
	{
		base.OnPreSpawnUnit();
		airportManager.team = actor.team;
		if (unitSpawner.unitName.ToLower() == "tower")
		{
			airportManager.airportName = unitSpawner.prefabUnitSpawn.unitName;
		}
		else
		{
			airportManager.airportName = unitSpawner.unitName;
		}
		if ((bool)airportManager.location)
		{
			airportManager.location.locationName = airportManager.airportName;
		}
		ReArmingPoint[] componentsInChildren = GetComponentsInChildren<ReArmingPoint>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].team = actor.team;
		}
		carrierSpawners = new UnitSpawner[spawnPoints.Count];
		if (!unitSpawner.unitFields.ContainsKey("carrierSpawns"))
		{
			return;
		}
		foreach (string item in ConfigNodeUtils.ParseList(unitSpawner.unitFields["carrierSpawns"]))
		{
			string[] array = item.Split(':');
			int num = ConfigNodeUtils.ParseInt(array[0]);
			int num2 = ConfigNodeUtils.ParseInt(array[1]);
			if (num2 < 0)
			{
				continue;
			}
			UnitSpawner unit = VTScenario.current.units.GetUnit(num2);
			if ((bool)unit)
			{
				unit.transform.parent = spawnPoints[num].spawnTf;
				unit.transform.localPosition = Vector3.zero;
				unit.transform.localRotation = Quaternion.identity;
				unit.linkedToCarrier = true;
				carrierSpawners[num] = unit;
				unit.OnSpawnedUnit += OnSpawnedCarrierUnit;
				if (unit.spawned)
				{
					Debug.LogError("Carrier unit was spawned before carrier was prespawned!");
				}
			}
			else
			{
				Debug.LogError("No unit found for " + unitSpawner.GetUIDisplayName() + " carrier unit " + num);
			}
		}
	}

	public static void RemoveUnitFromCarrier(UnitSpawner unitToRemove)
	{
		Debug.LogFormat("Removing {0} from carrier", unitToRemove.GetUIDisplayName());
		Dictionary<int, UnitSpawner> obj = ((unitToRemove.team == Teams.Allied) ? VTScenario.current.units.alliedUnits : VTScenario.current.units.enemyUnits);
		bool flag = false;
		foreach (UnitSpawner value in obj.Values)
		{
			if (!(value.prefabUnitSpawn is AICarrierSpawn) || !value.unitFields.ContainsKey("carrierSpawns") || string.IsNullOrEmpty(value.unitFields["carrierSpawns"]))
			{
				continue;
			}
			bool flag2 = false;
			List<string> list = ConfigNodeUtils.ParseList(value.unitFields["carrierSpawns"]);
			for (int i = 0; i < list.Count; i++)
			{
				string[] array = list[i].Split(':');
				int num = ConfigNodeUtils.ParseInt(array[0]);
				if (ConfigNodeUtils.ParseInt(array[1]) == unitToRemove.unitInstanceID)
				{
					list[i] = $"{num}:{-1}";
					flag = true;
					flag2 = true;
				}
			}
			if (flag2)
			{
				value.unitFields["carrierSpawns"] = ConfigNodeUtils.WriteList(list);
			}
		}
		if (flag)
		{
			unitToRemove.spawnFlags.Remove("carrier");
			return;
		}
		Debug.LogErrorFormat(unitToRemove.gameObject, "Failed to remove {0} from carrier!", unitToRemove.GetUIDisplayName());
	}

	public static UnitSpawner GetParentCarrier(UnitSpawner unit)
	{
		if (!unit.spawnFlags.Contains("carrier"))
		{
			return null;
		}
		foreach (UnitSpawner value in ((unit.team == Teams.Allied) ? VTScenario.current.units.alliedUnits : VTScenario.current.units.enemyUnits).Values)
		{
			if (!(value.prefabUnitSpawn is AICarrierSpawn) || !value.unitFields.ContainsKey("carrierSpawns"))
			{
				continue;
			}
			List<string> list = ConfigNodeUtils.ParseList(value.unitFields["carrierSpawns"]);
			for (int i = 0; i < list.Count; i++)
			{
				string[] array = list[i].Split(':');
				ConfigNodeUtils.ParseInt(array[0]);
				if (ConfigNodeUtils.ParseInt(array[1]) == unit.unitInstanceID)
				{
					return value;
				}
			}
		}
		return null;
	}

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		CarrierCatapult[] componentsInChildren = GetComponentsInChildren<CarrierCatapult>(includeInactive: true);
		foreach (CarrierCatapult key in componentsInChildren)
		{
			catOccupied.Add(key, null);
		}
		StartCoroutine(TakeoffAuthRoutine());
	}

	private void OnSpawnedCarrierUnit(UnitSpawner spawner)
	{
		if (spawner.spawnedUnit is AIAircraftSpawn)
		{
			AIAircraftSpawn obj = (AIAircraftSpawn)spawner.spawnedUnit;
			int num2 = (obj.carrierSpawnIdx = IndexOfSpawner(spawner));
			obj.aiPilot.currentCarrier = this;
			obj.aiPilot.currentCarrierSpawnIdx = num2;
			obj.takeOffPath = spawnPoints[num2].catapultPath;
			obj.catapult = spawnPoints[num2].catapult;
		}
		else if (spawner.spawnedUnit is PlayerSpawn)
		{
			((PlayerSpawn)spawner.spawnedUnit).spawnedOnCarrier = this;
		}
		else if (spawner.spawnedUnit is MultiplayerSpawn)
		{
			((MultiplayerSpawn)spawner.spawnedUnit).spawnedOnCarrier = this;
		}
	}

	private int IndexOfSpawner(UnitSpawner spawner)
	{
		for (int i = 0; i < carrierSpawners.Length; i++)
		{
			if (carrierSpawners[i] != null && carrierSpawners[i] == spawner)
			{
				return i;
			}
		}
		return -1;
	}

	public AirportManager GetAirport()
	{
		return airportManager;
	}

	public Transform GetRTBWaypoint()
	{
		return rtbWaypoint;
	}

	[VTEvent("Launch All", "Command all onboard aircraft to take off.")]
	public void LaunchAllAircraft()
	{
		UnitSpawner[] array = carrierSpawners;
		foreach (UnitSpawner unitSpawner in array)
		{
			if (unitSpawner != null && unitSpawner.spawnedUnit is AIAircraftSpawn)
			{
				((AIAircraftSpawn)unitSpawner.spawnedUnit).TakeOff();
			}
		}
	}

	public bool IsAuthorizedForTakeoff(Actor a)
	{
		return takeoffAuthorizedActors.Contains(a);
	}

	public void RegisterAITakeoffRequest(AIAircraftSpawn s)
	{
		if (!takeoffRequesters.Contains(s))
		{
			takeoffRequesters.Add(s);
		}
	}

	public void CancelPlayerTakeOff()
	{
		if ((bool)playerCat)
		{
			UnitSpawn unitSpawn = FlightSceneManager.instance.playerActor.unitSpawn;
			takeoffRequesters.Remove(unitSpawn);
			takeoffAuthorizedActors.Remove(FlightSceneManager.instance.playerActor);
			catOccupied[playerCat] = null;
			playerCat = null;
			if (deauthPlayerTakeoffRoutine != null)
			{
				StopCoroutine(deauthPlayerTakeoffRoutine);
			}
		}
	}

	public CarrierCatapult RegisterPlayerTakeoffRequest()
	{
		UnitSpawn unitSpawn = FlightSceneManager.instance.playerActor.unitSpawn;
		if (!takeoffRequesters.Contains(unitSpawn))
		{
			playerCat = null;
			float num = float.MaxValue;
			Vector3 position = FlightSceneManager.instance.playerActor.position;
			foreach (CarrierCatapult key in catOccupied.Keys)
			{
				if (Vector3.Dot(key.transform.position - position, base.transform.forward) > 0f)
				{
					float sqrMagnitude = (key.transform.position - position).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						playerCat = key;
					}
				}
			}
			if ((bool)playerCat)
			{
				takeoffRequesters.Add(unitSpawn);
				return playerCat;
			}
		}
		return null;
	}

	private IEnumerator TakeoffAuthRoutine()
	{
		WaitForSeconds emptyWait = new WaitForSeconds(0.5f);
		WaitForSeconds longWait = new WaitForSeconds(4f);
		while (base.enabled && actor.alive)
		{
			while (takeoffRequesters.Count < 1 || landingMode)
			{
				yield return emptyWait;
				if (landingMode)
				{
					CheckLandingMode();
				}
			}
			UnitSpawn unitSpawn = null;
			float num = float.MinValue;
			foreach (UnitSpawn takeoffRequester in takeoffRequesters)
			{
				if (takeoffRequester is AIAircraftSpawn)
				{
					AIAircraftSpawn aIAircraftSpawn = (AIAircraftSpawn)takeoffRequester;
					if ((bool)aIAircraftSpawn.catapult)
					{
						float num2 = Vector3.Dot(base.transform.forward, aIAircraftSpawn.transform.position - base.transform.position);
						if (num2 > num && !catOccupied[aIAircraftSpawn.catapult])
						{
							num = num2;
							unitSpawn = takeoffRequester;
						}
					}
					else
					{
						Debug.LogErrorFormat("Carrier TakeoffAuthRoutine: An AI in the request list did not have a catapult assigned! {0}", aIAircraftSpawn.actor.DebugName());
					}
				}
				else if (takeoffRequester.actor == FlightSceneManager.instance.playerActor && (bool)playerCat)
				{
					Vector3 position = FlightSceneManager.instance.playerActor.position;
					float num3 = Vector3.Dot(base.transform.forward, position - base.transform.position);
					if (num3 > num && !catOccupied[playerCat])
					{
						num = num3;
						unitSpawn = takeoffRequester;
					}
				}
			}
			if (unitSpawn != null)
			{
				if (unitSpawn is AIAircraftSpawn)
				{
					AIAircraftSpawn aIAircraftSpawn2 = (AIAircraftSpawn)unitSpawn;
					if ((bool)aIAircraftSpawn2.catapult)
					{
						catOccupied[aIAircraftSpawn2.catapult] = aIAircraftSpawn2.actor;
						takeoffAuthorizedActors.Add(unitSpawn.actor);
						takeoffRequesters.Remove(unitSpawn);
						StartCoroutine(DeauthTakeoffRoutine(aIAircraftSpawn2));
					}
					else if (QuicksaveManager.isQuickload)
					{
						Debug.LogErrorFormat("Tried to authorize an AI pilot to take off from a carrier but it did not have a catapult assigned. {0}", aIAircraftSpawn2.actor.DebugName());
						QuicksaveManager.instance.IndicateError();
						catOccupied[aIAircraftSpawn2.catapult] = aIAircraftSpawn2.actor;
					}
				}
				else if (unitSpawn.actor == FlightSceneManager.instance.playerActor && (bool)playerCat)
				{
					catOccupied[playerCat] = unitSpawn.actor;
					takeoffAuthorizedActors.Add(unitSpawn.actor);
					takeoffRequesters.Remove(unitSpawn);
					deauthPlayerTakeoffRoutine = StartCoroutine(DeauthPlayerTakeoffRoutine());
				}
			}
			yield return longWait;
			yield return null;
		}
	}

	private IEnumerator DeauthPlayerTakeoffRoutine()
	{
		Actor playerActor = FlightSceneManager.instance.playerActor;
		CatapultHook catHook = playerActor.GetComponentInChildren<CatapultHook>();
		while ((bool)catHook && (bool)playerActor && playerActor.alive && !catHook.hooked && playerActor.flightInfo.isLanded)
		{
			yield return new WaitForSeconds(1f);
		}
		takeoffAuthorizedActors.Remove(playerActor);
		if ((bool)playerCat)
		{
			catOccupied[playerCat] = null;
			playerCat = null;
		}
	}

	private IEnumerator DeauthTakeoffRoutine(AIAircraftSpawn s)
	{
		CarrierCatapult cat = s.catapult;
		while ((bool)s && s.actor.alive && !s.aiPilot.catHook.hooked && s.aiPilot.autoPilot.flightInfo.isLanded)
		{
			yield return new WaitForSeconds(1f);
		}
		if ((bool)s)
		{
			takeoffAuthorizedActors.Remove(s.actor);
		}
		catOccupied[cat] = null;
	}

	[ContextMenu("Get Cats from paths")]
	public void EditorGetCatsFromPathsEditor()
	{
		CarrierCatapult[] componentsInChildren = GetComponentsInChildren<CarrierCatapult>(includeInactive: true);
		foreach (CarrierSpawnPoint spawnPoint in spawnPoints)
		{
			CarrierCatapult catapult = null;
			float num = float.MaxValue;
			CarrierCatapult[] array = componentsInChildren;
			foreach (CarrierCatapult carrierCatapult in array)
			{
				float sqrMagnitude = (spawnPoint.catapultPath.pointTransforms[spawnPoint.catapultPath.pointTransforms.Length - 1].position - carrierCatapult.transform.position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					catapult = carrierCatapult;
					num = sqrMagnitude;
				}
			}
			spawnPoint.catapult = catapult;
		}
	}

	public void BeginLandingMode(AIPilot pilot)
	{
		landingMode = true;
		if (!landingPilots.Contains(pilot))
		{
			landingPilots.Add(pilot);
		}
	}

	public void FinishLanding(AIPilot pilot)
	{
		landingPilots.Remove(pilot);
		CheckLandingMode();
	}

	private void CheckLandingMode()
	{
		landingPilots.RemoveAll((AIPilot x) => x == null);
		if (landingPilots.Count == 0)
		{
			landingMode = false;
		}
		else
		{
			landingMode = true;
		}
	}

	public override void Quicksave(ConfigNode qsNode)
	{
		base.Quicksave(qsNode);
		foreach (AIPilot landingPilot in landingPilots)
		{
			if ((bool)landingPilot && (bool)landingPilot.actor && landingPilot.actor.alive)
			{
				qsNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(landingPilot.actor, "landingPilot"));
			}
		}
	}

	public override void Quickload(ConfigNode qsNode)
	{
		base.Quickload(qsNode);
		foreach (ConfigNode node in qsNode.GetNodes("landingPilot"))
		{
			Actor actor = QuicksaveManager.RetrieveActorFromNode(node);
			if ((bool)actor)
			{
				AIPilot component = actor.GetComponent<AIPilot>();
				if ((bool)component)
				{
					landingPilots.Add(component);
				}
			}
		}
		CheckLandingMode();
	}
}
