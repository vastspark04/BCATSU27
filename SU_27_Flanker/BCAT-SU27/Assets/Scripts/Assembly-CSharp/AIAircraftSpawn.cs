using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class AIAircraftSpawn : AIUnitSpawnEquippable, ICanHoldPassengers
{
	public enum PlayerCommandsModes
	{
		Unit_Group_Only,
		Force_Allow,
		Force_Disallow
	}

	public enum DefaultBehaviors
	{
		Orbit,
		Path,
		Parked,
		TakeOff
	}

	public enum ParkedStartModes
	{
		FlightReady,
		Cold
	}

	public class LoadableUnitFilter : IUnitFilter
	{
		public bool PassesFilter(UnitSpawner uSpawner)
		{
			return uSpawner.prefabUnitSpawn.gameObject.GetComponent<Soldier>() != null;
		}
	}

	private float taxiSpeed;

	private float carrierTaxiSpeed;

	public string vehicleName;

	public bool tsdDataLinkAlways;

	[UnitSpawn("Aircraft Group")]
	public VTUnitGroup.UnitGroup unitGroup;

	public bool wingmanVoice;

	[UnitSpawnAttributeConditional("IsWingmanVoice")]
	[UnitSpawn("Voice")]
	public WingmanVoiceProfile voiceProfile;

	[UnitSpawnAttributeConditional("IsWingmanVoice")]
	[UnitSpawn("Player Commands")]
	public PlayerCommandsModes playerCommandsMode;

	[UnitSpawn("Default Behavior")]
	public DefaultBehaviors defaultBehavior;

	[UnitSpawnAttributeRange("Initial Airspeed", 70f, 700f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float initialSpeed;

	[UnitSpawnAttributeRange("Default Nav Speed", 90f, 700f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float defaultNavSpeed;

	[UnitSpawn("Default Orbit Point")]
	public Waypoint defaultOrbitPoint;

	[UnitSpawn("Default Path")]
	public FollowPath defaultPath;

	[UnitSpawnAttributeRange("Default Altitude", 300f, 10000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float orbitAltitude = 3000f;

	[UnitSpawnAttributeRange("Fuel %", 0f, 100f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float fuel = 100f;

	[UnitSpawn("Auto Refuel")]
	public bool autoRefuel = true;

	[UnitSpawn("Auto RTB")]
	public bool autoRTB = true;

	[UnitSpawnAttributeConditional("US_HasRadar")]
	[UnitSpawn("Radar Enabled")]
	public bool defaultRadarEnabled = true;

	[UnitSpawnAirportReference("RTB destination", TeamOptions.SameTeam, new string[] { "allowNoneOption=true" })]
	public AirportReference rtbDestination;

	[UnitSpawn("Parked Start Mode")]
	public ParkedStartModes parkedStartMode;

	public FollowPath takeOffPath;

	public Runway takeOffRunway;

	public AirportManager closestAirport;

	public AIPilot aiPilot;

	public KinematicPlane kPlane;

	public AIPassengerBay passengerBay;

	private bool spawnedOnCarrier;

	private AirbaseNavNode parkingNode;

	public CarrierCatapult catapult { get; set; }

	public AICarrierSpawn carrier => aiPilot.currentCarrier;

	[HideInInspector]
	public int carrierSpawnIdx
	{
		get
		{
			return aiPilot.currentCarrierSpawnIdx;
		}
		set
		{
			aiPilot.currentCarrierSpawnIdx = value;
		}
	}

	public bool IsWingmanVoice()
	{
		if (wingmanVoice && (bool)actor)
		{
			return actor.team == Teams.Allied;
		}
		return false;
	}

	[UnitSpawnAttributeConditional("IsWingmanVoice")]
	[VTEvent("Set Player Commands", "Set whether the unit can be commanded by the player.", new string[] { "Mode" })]
	public void SetPlayerCommands(PlayerCommandsModes mode)
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			Debug.LogError("TODO: player commands mode for wingmen");
			return;
		}
		PlayerSpawn playerSpawn = (PlayerSpawn)VTScenario.current.units.GetPlayerSpawner().spawnedUnit;
		switch (mode)
		{
		case PlayerCommandsModes.Unit_Group_Only:
			if (playerSpawn.unitGroup != null && playerSpawn.unitGroup == unitGroup)
			{
				aiPilot.allowPlayerCommands = true;
			}
			else
			{
				aiPilot.allowPlayerCommands = false;
			}
			break;
		case PlayerCommandsModes.Force_Allow:
			aiPilot.allowPlayerCommands = true;
			break;
		case PlayerCommandsModes.Force_Disallow:
			aiPilot.allowPlayerCommands = false;
			break;
		}
	}

	public bool US_HasRadar()
	{
		return GetComponentInChildren<Radar>() != null;
	}

	[VTEvent("Set Path", "Set the aircraft to fly along a path.", new string[] { "Path" })]
	public void SetPath(FollowPath path)
	{
		if (actor.alive && unitSpawner.spawned)
		{
			aiPilot.CommandCancelOverride();
			aiPilot.FlyNavPath(path);
		}
	}

	[VTEvent("Taxi Path", "Command the aircraft to taxi on a certain path.", new string[] { "Path" })]
	public void TaxiPath(FollowPath path)
	{
		if (actor.alive)
		{
			aiPilot.taxiSpeed = taxiSpeed;
			aiPilot.Taxi(path);
		}
	}

	[VTEvent("Taxi Path Speed", "Command the aircraft to taxi on a certain path at a certain speed.", new string[] { "Path", "Speed" })]
	public void TaxiPathSpeed(FollowPath path, [VTRangeParam(1f, 20f)] float speed)
	{
		if (actor.alive)
		{
			aiPilot.taxiSpeed = speed;
			aiPilot.Taxi(path);
		}
	}

	[VTEvent("Orbit Waypoint", "Command the aircraft to orbit a waypoint.", new string[] { "Waypoint", "Radius", "Altitude" })]
	public void SetOrbitNow(Waypoint wpt, [VTRangeParam(1000f, 80000f)] float radius, [VTRangeParam(1000f, 10000f)] float alt)
	{
		if (actor.alive && wpt != null)
		{
			aiPilot.orbitRadius = radius;
			aiPilot.defaultAltitude = alt;
			aiPilot.CommandCancelOverride();
			aiPilot.OrbitTransform(wpt.GetTransform());
		}
	}

	[VTEvent("Set Nav Speed", "Set the aircraft's default navigation airspeed (when not in combat).", new string[] { "Airspeed" })]
	public void SetNavSpeed([VTRangeParam(60f, 700f)] float speed)
	{
		if (actor.alive)
		{
			aiPilot.SetNavSpeed(speed);
		}
	}

	[VTEvent("Set Altitude", "Set the aircraft's default altitude.", new string[] { "Altitude" })]
	public void SetAltitude([VTRangeParam(1500f, 10000f)] float alt)
	{
		if (actor.alive)
		{
			orbitAltitude = alt;
			aiPilot.defaultAltitude = alt;
		}
	}

	[VTEvent("Form On Pilot", "Command the aircraft to form up on a particular air unit.", new string[] { "Target Leader" })]
	public void FormOnPilot([VTTeamOptionParam(TeamOptions.SameTeam)][VTActionParam(typeof(PilotUnitFilter), null)] UnitReference target)
	{
		if (actor.alive && !(target.GetSpawner() == null) && target.GetSpawner().spawned && target.GetActor().alive)
		{
			UnitSpawn unit = target.GetUnit();
			if (unit is PlayerSpawn)
			{
				aiPilot.CommandCancelOverride();
				aiPilot.FormOnPlayer();
			}
			else if (unit is AIAircraftSpawn)
			{
				aiPilot.CommandCancelOverride();
				AIPilot pilot = ((AIAircraftSpawn)unit).aiPilot;
				aiPilot.FormOnPilot(pilot);
			}
		}
	}

	[VTEvent("Refuel From Tanker", "Command the aircraft to refuel from a particular tanker.", new string[] { "Target Tanker" })]
	public void RefuelWithUnit([VTTeamOptionParam(TeamOptions.SameTeam)][VTActionParam(typeof(RefuelUnitFilter), null)] UnitReference fuelerUnit)
	{
		if (actor.alive && !(fuelerUnit.GetSpawner() == null) && fuelerUnit.GetSpawner().spawned && fuelerUnit.GetActor().alive)
		{
			UnitSpawner spawner = fuelerUnit.GetSpawner();
			if (spawner.spawnedUnit == this)
			{
				Debug.LogErrorFormat("A unit ({0}) was commanded to refuel from itself! Impossible!", actor.DebugName());
			}
			else
			{
				RefuelPlane refuelPlane = ((AIAirTankerSpawn)spawner.spawnedUnit).refuelPlane;
				aiPilot.CommandCancelOverride();
				aiPilot.GoRefuel(refuelPlane);
			}
		}
	}

	[VTEvent("Fire ASM", "Command the aircraft to fire an anti-ship missile on the given path if available.", new string[] { "Path", "Terminal Mode" })]
	public void FireASMOnPath(FollowPath asmPath, AntiShipGuidance.ASMTerminalBehaviors tMode)
	{
		Debug.Log(actor.DebugName() + " was commanded to fire an ASM on path: " + asmPath.gameObject.name);
		aiPilot.CommandAntiShipOnPath(asmPath, tMode);
	}

	[VTEvent("Take Off", "Command the pilot to take off.")]
	public void TakeOff()
	{
		if (!unitSpawner.spawned)
		{
			Debug.Log(unitSpawner.GetUIDisplayName() + " tried to take off but unit is not spawned");
		}
		else if (actor.alive)
		{
			if (!aiPilot.autoPilot.flightInfo.isLanded || aiPilot.rearming)
			{
				aiPilot.SetTakeOffAfterLanding(b: true);
			}
			else
			{
				TryTakeoff();
			}
		}
	}

	[ContextMenu("Take Off")]
	private void TryTakeoff()
	{
		foreach (ModuleEngine engine in aiPilot.autoPilot.engines)
		{
			if (!engine.startedUp)
			{
				engine.SetPower(0);
				engine.startupDrain = 0f;
				engine.SetPower(1);
			}
		}
		Debug.Log(unitSpawner.GetUIDisplayName() + " requesting takeoff.");
		if (unitSpawner.spawned && aiPilot.autoPilot.flightInfo.isLanded)
		{
			if (aiPilot.isVtol && aiPilot.thrustToWeightRatio > 1.05f && !Physics.Raycast(base.transform.position, Vector3.up, 500f, 1))
			{
				float num = 0f;
				if ((bool)carrier)
				{
					num = VectorUtils.Bearing(carrier.transform.position, carrier.transform.position + 100f * carrier.transform.forward);
				}
				else if ((bool)aiPilot.navPath)
				{
					num = VectorUtils.Bearing(base.transform.position, aiPilot.navPath.pointTransforms[0].position);
				}
				else if ((bool)aiPilot.orbitTransform)
				{
					num = VectorUtils.Bearing(base.transform.position, aiPilot.orbitTransform.position);
				}
				float num2 = 10f;
				float num3 = (carrier ? 40 : 20);
				Vector3 origin = base.transform.position + num3 * Vector3.up;
				bool flag = false;
				float num4 = 0f;
				float heading = num;
				for (float num5 = 0f; num5 < 360f; num5 += num2)
				{
					if (flag)
					{
						break;
					}
					if (Physics.Raycast(origin, Quaternion.AngleAxis(num, Vector3.up) * Vector3.forward, out var hitInfo, 3000f, 1))
					{
						if (hitInfo.distance > num4)
						{
							num4 = hitInfo.distance;
							heading = num;
						}
					}
					else
					{
						flag = true;
					}
					num += num2;
				}
				if (flag)
				{
					aiPilot.TakeOffVTOL(num, num3 + 10f);
				}
				else
				{
					aiPilot.TakeOffVTOL(heading, 100f);
				}
				return;
			}
			if (aiPilot.isVtol)
			{
				Debug.Log("VTOL AI can't VTO because TWR is too low or vertically obstructed.");
			}
			if (unitSpawner.spawnFlags.Contains("carrier") && (bool)carrier)
			{
				if (carrier.usesCatapults || ((bool)carrier.spawnPoints[carrierSpawnIdx].stoPath && aiPilot.sto_capable))
				{
					aiPilot.TakeOffCarrier(carrier, carrierSpawnIdx);
				}
				else
				{
					Debug.Log(unitSpawner.GetUIDisplayName() + " can't take off because it's on a vtol-only carrier and is overweight!", base.gameObject);
				}
				return;
			}
			GetTakeoffReqs();
			if (!(takeOffRunway != null))
			{
				return;
			}
			if (aiPilot.TakeOff(takeOffRunway))
			{
				if (closestAirport != null && takeOffRunway.clearanceBounds.Contains(takeOffRunway.transform.InverseTransformPoint(actor.position)))
				{
					Debug.Log(unitSpawner.GetUIDisplayName() + " is already on a runway.  Taking off immediately.");
					takeOffRunway.RegisterUsageRequestHighPriority(actor);
				}
				else if (closestAirport != null && closestAirport.navigation != null)
				{
					aiPilot.taxiSpeed = taxiSpeed;
					StartCoroutine(TakeOffPathAsync());
				}
				else if (takeOffPath != null)
				{
					aiPilot.taxiSpeed = taxiSpeed;
					aiPilot.Taxi(takeOffPath);
				}
			}
			else
			{
				Debug.LogFormat(base.gameObject, "{0} was commanded to take off, but is not in a valid state to receive the command.", actor.DebugName());
			}
		}
		else
		{
			Debug.Log(unitSpawner.GetUIDisplayName() + " can't take off because it is not landed or not spawned.");
		}
	}

	private IEnumerator TakeOffPathAsync()
	{
		Debug.Log("Getting take off path Async!");
		AirbaseNavigation.AsyncPathRequest req = closestAirport.navigation.GetTakeoffPathAsync(base.transform.position, base.transform.forward, 1f);
		while (!req.done)
		{
			yield return null;
		}
		List<AirbaseNavNode> path = req.path;
		if (path != null)
		{
			Runway tgtRunway = (takeOffRunway = path[path.Count - 1].GetComponent<AirbaseNavNode>().takeoffRunway);
			aiPilot.TaxiAirbaseNav(path, tgtRunway);
			if ((bool)aiPilot.actor.parkingNode)
			{
				aiPilot.actor.parkingNode.UnOccupyParking(aiPilot.actor);
			}
			if (takeOffRunway != null)
			{
				aiPilot.targetRunway = takeOffRunway;
			}
			else
			{
				Debug.Log("takeoff runway is null");
			}
		}
		else
		{
			Debug.Log("navTfs is null");
		}
	}

	private IEnumerator TakeOffCarrierRoutine()
	{
		carrier.RegisterAITakeoffRequest(this);
		while (!carrier.IsAuthorizedForTakeoff(actor))
		{
			yield return new WaitForSeconds(1f);
		}
		aiPilot.taxiSpeed = carrierTaxiSpeed;
		if (takeOffPath != null)
		{
			if (carrier.airportManager.reserveRunwayForCarrierTakeOff)
			{
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
					yield return null;
				}
			}
			Debug.Log("Carrier unit taking off");
			aiPilot.Taxi(takeOffPath);
			aiPilot.TakeOffCatapult(catapult);
			while (actor.flightInfo.isLanded)
			{
				yield return null;
				if (carrier.airportManager.reserveRunwayForCarrierTakeOff)
				{
					carrier.runway.UnregisterUsageRequest(actor);
				}
			}
		}
		else
		{
			Debug.Log("Carrier unit has no take off path");
		}
	}

	[VTEvent("Land", "Land at a specified airfield.", new string[] { "Airfield" })]
	public void Land([VTTeamOptionParam(TeamOptions.SameTeam)] AirportReference airport)
	{
		if (!actor.alive || !unitSpawner.spawned)
		{
			return;
		}
		if (aiPilot.autoPilot.flightInfo.isLanded)
		{
			Debug.Log(unitSpawner.GetUIDisplayName() + " requested landing but is already landed");
			return;
		}
		AirportManager airport2 = airport.GetAirport();
		if (airport2 != null)
		{
			aiPilot.CommandCancelOverride();
			aiPilot.LandAtAirport(airport2);
		}
	}

	[VTEvent("Land At Wpt", "Vertically land on a specified waypoint if capable.", new string[] { "Waypoint" })]
	public void LandAtWpt(Waypoint wpt)
	{
		if (actor.alive && !aiPilot.autoPilot.flightInfo.isLanded)
		{
			aiPilot.CommandCancelOverride();
			aiPilot.TemporaryLandAt(wpt.GetTransform());
		}
	}

	[VTEvent("Land At Wpt w/ Direction", "Vertically land on a specified waypoint with the specified heading, if capable.", new string[] { "Waypoint", "In Heading" })]
	public void LandAtWptHdg(Waypoint wpt, [VTRangeParam(0f, 360f)] float inHeading)
	{
		if (actor.alive && !aiPilot.autoPilot.flightInfo.isLanded)
		{
			aiPilot.CommandCancelOverride();
			aiPilot.TemporaryLandAt(wpt.GetTransform(), inHeading);
		}
	}

	[VTEvent("Land At Wpt w/ Dir Facing", "Vertically land on a specified waypoint with the specified heading, if capable.", new string[] { "Waypoint", "In Heading", "Land Facing" })]
	public void LandAtWptHdgFcg(Waypoint wpt, [VTRangeParam(0f, 360f)] float inHeading, [VTRangeParam(0f, 360f)] float landFacing)
	{
		if (actor.alive && !aiPilot.autoPilot.flightInfo.isLanded)
		{
			aiPilot.CommandCancelOverride();
			aiPilot.TemporaryLandAt(wpt.GetTransform(), inHeading, landFacing);
		}
	}

	[VTEvent("Rearm", "Land, rearm/refuel, and take off again from specified airfield.", new string[] { "Airfield" })]
	public void RearmAt([VTTeamOptionParam(TeamOptions.SameTeam)] AirportReference airport)
	{
		if (!actor.alive)
		{
			return;
		}
		if (aiPilot.autoPilot.flightInfo.isLanded)
		{
			Debug.Log(unitSpawner.GetUIDisplayName() + " requested landing/rearm but is already landed");
			return;
		}
		AirportManager airport2 = airport.GetAirport();
		if (airport2 != null)
		{
			aiPilot.CommandCancelOverride();
			aiPilot.SetRearmAfterLanding(rearm: true);
			aiPilot.LandAtAirport(airport2);
		}
	}

	[VTEvent("Attack Target", "Attack a specific target, regardless of detection or other threats.", new string[] { "Target" })]
	public void AttackTarget([VTTeamOptionParam(TeamOptions.OtherTeam)][VTUnitReferenceSubsParam(true)] UnitReference tgt)
	{
		if (actor.alive)
		{
			UnitSpawn unit = tgt.GetUnit();
			if ((bool)unit)
			{
				aiPilot.CommandCancelOverride();
				aiPilot.OrderAttackTarget(unit.actor);
			}
			else
			{
				Debug.Log("Attack target unit was null: " + tgt.unitID);
			}
		}
	}

	[VTEvent("Cancel Attack Tgt", "Cancel the override attack target and return to normal behavior.")]
	public void CancelAttackTarget()
	{
		if (actor.alive)
		{
			aiPilot.CommandCancelOverride();
			aiPilot.CancelAttackOrder();
		}
	}

	[VTEvent("Set Radio Comms", "Set whether the AI pilot will communicate with the player via radio.", new string[] { "Enable" })]
	public void SetRadioComms(bool radioEnabled)
	{
		if (actor.alive)
		{
			if (this is AIAWACSSpawn)
			{
				((AIAWACSSpawn)this).commsEnabled = radioEnabled;
			}
			else
			{
				aiPilot.doRadioComms = radioEnabled;
			}
		}
	}

	[VTEvent("Add Priority Targets", "Adds unit(s) to this AI pilot's priority targets.  They will attack these targets before others when they are detected.", new string[] { "Targets" })]
	public new void AddPriorityTargets(UnitReferenceListOtherSubs targets)
	{
		if (!actor.alive || targets == null)
		{
			return;
		}
		foreach (UnitReference unit in targets.units)
		{
			aiPilot.AddPriorityTarget(unit.GetActor());
		}
	}

	[VTEvent("Set Priority Targets", "Sets or replaces the AI pilot's priority target list to a new list. They will prioritize attacking these targets once they are detected.", new string[] { "Targets" })]
	public new void SetPriorityTargets(UnitReferenceListOtherSubs targets)
	{
		if (actor.alive)
		{
			aiPilot.ClearPriorityTargets();
			AddPriorityTargets(targets);
		}
	}

	[VTEvent("Clear Priority Targets", "Clears the AI pilot's priority target list.")]
	public new void ClearPriorityTargets()
	{
		if (actor.alive)
		{
			aiPilot.ClearPriorityTargets();
		}
	}

	[VTEvent("Add Non-Targets", "Adds unit(s) to this AI pilot's list of non-targets.  They will never attack these target.", new string[] { "Targets" })]
	public new void AddNonTargets(UnitReferenceListOtherSubs targets)
	{
		if (!actor.alive || targets == null)
		{
			return;
		}
		foreach (UnitReference unit in targets.units)
		{
			aiPilot.AddNonTarget(unit.GetActor());
		}
	}

	[VTEvent("Set Non-Targets", "Sets or replaces the AI pilot's non-target list to a new list. They will never attack these targets.", new string[] { "Targets" })]
	public new void SetNonTargets(UnitReferenceListOtherSubs targets)
	{
		if (actor.alive)
		{
			aiPilot.ClearNonTargets();
			AddNonTargets(targets);
		}
	}

	[VTEvent("Clear Non-Targets", "Clears the AI pilot's non-target list.")]
	public new void ClearNonTargets()
	{
		if (actor.alive)
		{
			aiPilot.ClearNonTargets();
		}
	}

	[VTEvent("Add Designated Targets", "Adds units to the AI pilot's designated targets, which it will attack at highest priority, immediately, whether or not these targets have been detected.", new string[] { "Targets" })]
	public void AddDesignatedTargets(UnitReferenceListOtherSubs targets)
	{
		if (!actor.alive || targets == null)
		{
			return;
		}
		foreach (UnitReference unit in targets.units)
		{
			aiPilot.AddDesignatedTarget(unit.GetActor());
		}
	}

	[VTEvent("Set Designated Targets", "Sets or replaces the AI pilot's designated targets, which it will attack at highest priority, immediately, whether or not these targets have been detected.", new string[] { "Targets" })]
	public void SetDesignatedTargets(UnitReferenceListOtherSubs targets)
	{
		if (actor.alive && targets != null)
		{
			aiPilot.ClearDesignatedTargets();
			AddDesignatedTargets(targets);
		}
	}

	[VTEvent("Clear Designated Targets", "Clears the AI pilot's designated targets.")]
	public void ClearDesignatedTargets()
	{
		if (actor.alive)
		{
			aiPilot.ClearDesignatedTargets();
		}
	}

	[VTEvent("Fire Countermeasures", "Fires a set amount of chaff and/or flares.", new string[] { "Flares", "Chaff", "Count", "Interval" })]
	public void CountermeasureProgram(bool flares, bool chaff, [VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)][VTRangeParam(1f, 50f)] float count, [VTRangeParam(0.1f, 10f)] float interval)
	{
		aiPilot.CountermeasureProgram(flares, chaff, Mathf.RoundToInt(count), interval);
	}

	private void GetClosestAirport()
	{
		closestAirport = null;
		float num = float.MaxValue;
		foreach (AirportManager allAirport in VTScenario.current.GetAllAirports())
		{
			float sqrMagnitude = (allAirport.transform.position - base.transform.position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				closestAirport = allAirport;
			}
		}
	}

	private void GetTakeoffReqs()
	{
		GetClosestAirport();
		if ((bool)closestAirport)
		{
			Runway runway = null;
			float num = -1f;
			Runway[] runways = closestAirport.runways;
			foreach (Runway runway2 in runways)
			{
				if (runway2.clearanceBounds.Contains(runway2.transform.InverseTransformPoint(actor.position)))
				{
					float num2 = Vector3.Dot(runway2.transform.forward, actor.transform.forward);
					if (num2 > num)
					{
						num = num2;
						runway = runway2;
					}
				}
			}
			if (!runway)
			{
				runway = closestAirport.GetNextTakeoffRunway();
			}
			if ((bool)runway)
			{
				takeOffRunway = runway;
				takeOffPath = runway.GetTakeoffTaxiPath(base.transform);
				if (!takeOffPath)
				{
					Debug.Log(unitSpawner.GetUIDisplayName() + " No taxi path for takeoff.");
				}
			}
			else
			{
				Debug.Log(unitSpawner.GetUIDisplayName() + " no runway for takeoff.");
			}
		}
		else
		{
			Debug.Log(unitSpawner.GetUIDisplayName() + " no airfield for takeoff.");
		}
	}

	public bool HasPassengerBay()
	{
		return passengerBay != null;
	}

	public override void OnPreSpawnUnit()
	{
		base.OnPreSpawnUnit();
		if (!aiPilot.kPlane.rb)
		{
			aiPilot.kPlane.rb = aiPilot.GetComponent<Rigidbody>();
		}
		spawnedOnCarrier = unitSpawner.spawnFlags.Contains("carrier");
		Debug.Log("Prespawning " + base.gameObject.name);
		if ((bool)VTMapGenerator.fetch)
		{
			VTMapGenerator.fetch.BakeColliderAtPosition(unitSpawner.transform.position);
		}
		bool flag = false;
		if (spawnedOnCarrier || Physics.Raycast(unitSpawner.transform.position + 3f * Vector3.up, Vector3.down, 13f, 1))
		{
			kPlane.SetToDynamic();
			kPlane.rb.velocity = Vector3.zero;
			aiPilot.startLanded = true;
			flag = true;
		}
		else
		{
			initialSpeed = Mathf.Clamp(initialSpeed, 70f, 600f);
			kPlane.SetVelocity(initialSpeed * base.transform.forward);
			aiPilot.initialSpeed = initialSpeed;
			aiPilot.startLanded = false;
		}
		if ((bool)aiPilot.wingRotator)
		{
			if (spawnedOnCarrier)
			{
				aiPilot.wingRotator.SetDeployed();
				aiPilot.wingRotator.SetNormalizedRotationImmediate(1f);
			}
			else
			{
				aiPilot.wingRotator.SetDefault();
				aiPilot.wingRotator.SetNormalizedRotationImmediate(0f);
			}
		}
		aiPilot.navSpeed = defaultNavSpeed;
		if (flag)
		{
			foreach (ModuleEngine engine in aiPilot.autoPilot.engines)
			{
				engine.engineEnabled = parkedStartMode == ParkedStartModes.FlightReady;
				Debug.Log($"{engine.GetInstanceID()} e.engineEnabled = {engine.engineEnabled}");
			}
		}
		taxiSpeed = aiPilot.taxiSpeed;
		carrierTaxiSpeed = aiPilot.carrierTaxiSpeed;
		SetRadar(defaultRadarEnabled);
	}

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		if (unitGroup != null)
		{
			AIWing wing = ((VTUnitGroup.UnitGroup.AirGroupActions)unitGroup.groupActions).wing;
			if ((bool)wing && !wing.pilots.Contains(aiPilot))
			{
				aiPilot.aiWing = wing;
				wing.pilots.Add(aiPilot);
				wing.UpdateLeader();
			}
		}
		SetPlayerCommands(playerCommandsMode);
		if (defaultOrbitPoint != null)
		{
			aiPilot.orbitTransform = defaultOrbitPoint.GetTransform();
		}
		GetClosestAirport();
		if ((bool)closestAirport && (bool)closestAirport.navigation)
		{
			foreach (AirportManager.ParkingSpace parkingSpace in closestAirport.parkingSpaces)
			{
				if ((parkingSpace.transform.position - base.transform.position).sqrMagnitude < parkingSpace.parkingNode.parkingSize * parkingSpace.parkingNode.parkingSize)
				{
					parkingSpace.OccupyParking(actor);
					break;
				}
			}
		}
		aiPilot.defaultAltitude = orbitAltitude;
		aiPilot.autoRefuel = autoRefuel;
		if (defaultPath != null)
		{
			aiPilot.navPath = defaultPath;
		}
		if (!QuicksaveManager.isQuickload)
		{
			SetInitialBehavior();
		}
		else
		{
			StartCoroutine(SetInitialBehaviorAfterQuickload());
		}
		FuelTank component = GetComponent<FuelTank>();
		if ((bool)component)
		{
			component.SetNormFuel(fuel / 100f);
		}
	}

	private IEnumerator SetInitialBehaviorAfterQuickload()
	{
		while (!base.quickloaded)
		{
			yield return null;
		}
		if (!base.qsSpawned)
		{
			SetInitialBehavior();
		}
	}

	private void SetInitialBehavior()
	{
		if (aiPilot.startLanded)
		{
			Debug.LogFormat("{0} started landed.", actor.DebugName());
			if (Physics.Raycast(base.transform.position + 3f * Vector3.up, Vector3.down, out var hitInfo, 13f, 1))
			{
				base.transform.position = hitInfo.point + heightFromSurface * Vector3.up;
				MovingPlatform component = hitInfo.collider.GetComponent<MovingPlatform>();
				if ((bool)component)
				{
					aiPilot.autoPilot.flightInfo.rb.velocity = component.GetVelocity(hitInfo.point);
				}
				else
				{
					aiPilot.autoPilot.flightInfo.rb.velocity = Vector3.zero;
				}
			}
			switch (defaultBehavior)
			{
			case DefaultBehaviors.Path:
				if (spawnedOnCarrier)
				{
					aiPilot.commandState = AIPilot.CommandStates.Park;
				}
				else
				{
					aiPilot.commandState = AIPilot.CommandStates.Taxi;
				}
				break;
			case DefaultBehaviors.TakeOff:
				TakeOff();
				break;
			default:
				aiPilot.commandState = AIPilot.CommandStates.Park;
				break;
			}
		}
		else
		{
			if (!aiPilot.formationLeader)
			{
				if (defaultBehavior == DefaultBehaviors.Path)
				{
					aiPilot.commandState = AIPilot.CommandStates.Navigation;
				}
				else
				{
					aiPilot.commandState = AIPilot.CommandStates.Orbit;
				}
			}
			aiPilot.initialSpeed = initialSpeed;
			aiPilot.kPlane.SetSpeed(initialSpeed);
		}
		if (wingmanVoice)
		{
			if (voiceProfile != null)
			{
				aiPilot.voiceProfile = voiceProfile;
			}
			else
			{
				aiPilot.voiceProfile = CommRadioManager.GetNextRandomWingmanVoice();
			}
		}
		else
		{
			aiPilot.doRadioComms = false;
		}
		if ((bool)passengerBay && unitSpawner.childSpawners.Count > 0)
		{
			StartCoroutine(PreloadPassengersRoutine());
		}
	}

	private IEnumerator PreloadPassengersRoutine()
	{
		bool ready = false;
		while (!ready)
		{
			ready = true;
			foreach (UnitSpawner childSpawner in unitSpawner.childSpawners)
			{
				if (!childSpawner.spawned)
				{
					ready = false;
				}
			}
			yield return null;
		}
		Debug.Log("Preloading soldiers into " + unitSpawner.GetUIDisplayName());
		foreach (UnitSpawner childSpawner2 in unitSpawner.childSpawners)
		{
			childSpawner2.spawnedUnit.GetComponent<Soldier>().BoardAIBayImmediate(passengerBay);
		}
	}

	[VTEvent("Bomb Waypoint", "Bomb a waypoint. Aircraft must have unguided bombs equipped.", new string[] { "Waypoint", "Heading", "Count", "Altitude" })]
	public void BombWaypoint(Waypoint wpt, [VTRangeParam(0f, 360f)] float hdg, [VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)][VTRangeParam(1f, 100f)] float count, [VTRangeParam(100f, 8000f)] float altitude)
	{
		if (actor.alive)
		{
			if (wpt == null || wpt.GetTransform() == null)
			{
				Debug.Log(unitSpawner.GetUIDisplayName() + " is missing a bombing target.");
			}
			else
			{
				aiPilot.OrderCarpetBomb(wpt, hdg, Mathf.RoundToInt(count), altitude);
			}
		}
	}

	[VTEvent("Set Radar", "Set's the unit's radar on or off, if it has one.", new string[] { "Radar On" })]
	public void SetRadar(bool radarOn)
	{
		if (actor.alive && (bool)aiPilot)
		{
			aiPilot.vt_radarEnabled = radarOn;
		}
	}

	public bool CommandRTB()
	{
		if (aiPilot.autoPilot.flightInfo.isLanded)
		{
			Debug.LogFormat("{0} was commanded to RTB, but is already landed.", actor.DebugName());
			return false;
		}
		AirportManager airportManager = rtbDestination.GetAirport();
		if ((bool)airportManager && airportManager.team != actor.team)
		{
			airportManager = null;
		}
		if (!airportManager)
		{
			float num = float.MaxValue;
			if ((bool)VTMapManager.fetch && VTMapManager.fetch.airports != null)
			{
				foreach (AirportManager airport in VTMapManager.fetch.airports)
				{
					if ((bool)airport && airport.team == actor.team && (!airport.vtolOnlyLanding || aiPilot.isVtol))
					{
						float sqrMagnitude = (actor.position - airport.transform.position).sqrMagnitude;
						if (sqrMagnitude < num)
						{
							num = sqrMagnitude;
							airportManager = airport;
						}
					}
				}
			}
			if ((bool)aiPilot.tailHook)
			{
				foreach (UnitSpawner value in ((actor.team == Teams.Allied) ? VTScenario.current.units.alliedUnits : VTScenario.current.units.enemyUnits).Values)
				{
					if (!value || !value.spawned || !value.spawnedUnit || !(value.spawnedUnit is AICarrierSpawn) || !value.spawnedUnit.actor || !value.spawnedUnit.actor.alive)
					{
						continue;
					}
					AirportManager airportManager2 = ((AICarrierSpawn)value.spawnedUnit).airportManager;
					if (airportManager2.hasArrestor)
					{
						float sqrMagnitude2 = (actor.position - value.spawnedUnit.transform.position).sqrMagnitude;
						if (sqrMagnitude2 < num)
						{
							num = sqrMagnitude2;
							airportManager = airportManager2;
						}
					}
				}
			}
		}
		if ((bool)airportManager)
		{
			aiPilot.LandAtAirport(airportManager);
			return true;
		}
		Debug.LogFormat("{0} couldn't find an RTB airport...", actor.DebugName());
		return false;
	}

	[UnitSpawnAttributeConditional("HasPassengerBay")]
	[VTEvent("Unload Passengers", "Unload all passengers when available.", new string[] { "Rally Waypoint" })]
	public void UnloadAllPassengers(Waypoint rallyWp)
	{
		Debug.Log("Calling UnloadAllPassengers on " + unitSpawner.GetUIDisplayName());
		passengerBay.UnloadAllSoldiersWhenAvailable(rallyWp.GetTransform());
	}

	[UnitSpawnAttributeConditional("HasPassengerBay")]
	[VTEvent("Load Passengers", "Command the selected units to board this aircraft.", new string[] { "Targets" })]
	public void LoadPassengers([VTActionParam(typeof(LoadableUnitFilter), null)] UnitReferenceListSame targets)
	{
		foreach (UnitReference unit in targets.units)
		{
			if (unit.GetActor().alive && unit.GetSpawner().spawned)
			{
				unit.GetUnit().GetComponent<Soldier>().BoardAIBay(passengerBay);
			}
		}
	}

	[UnitSpawnAttributeConditional("HasPassengerBay")]
	[VTEvent("Load Unit Group", "Command the selected unit group to board this aircraft.", new string[] { "Group" })]
	public void LoadPassengerGroup([VTActionParam(typeof(VTUnitGroup.GroupTypes), VTUnitGroup.GroupTypes.Ground)] VTUnitGroup.UnitGroup group)
	{
		foreach (int unitID in group.unitIDs)
		{
			UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
			Soldier component;
			if (unit.spawned && unit.spawnedUnit.actor.alive && (bool)(component = unit.spawnedUnit.GetComponent<Soldier>()))
			{
				component.BoardAIBay(passengerBay);
			}
		}
	}

	[SCCUnitProperty("Fuel Percent", new string[] { "Comparison", "Percent" }, false)]
	public bool SC_FuelPercent(HealthComparisons comparison, [VTRangeParam(0f, 100f)] float percent)
	{
		float num = percent / 100f;
		if ((bool)aiPilot.fuelTank)
		{
			if (comparison == HealthComparisons.Greater_Than)
			{
				return aiPilot.fuelTank.fuelFraction > num;
			}
			return aiPilot.fuelTank.fuelFraction < num;
		}
		return false;
	}

	[SCCUnitProperty("Landed", true)]
	public bool SC_IsLanded()
	{
		return aiPilot.autoPilot.flightInfo.isLanded;
	}

	public int GetMaximumPassengers()
	{
		return passengerBay.capacity;
	}

	public Transform GetSeatTransform(int seatIdx)
	{
		if (passengerBay.seatTransforms != null && passengerBay.seatTransforms.Length == passengerBay.capacity)
		{
			return passengerBay.seatTransforms[seatIdx];
		}
		return passengerBay.transform;
	}
}
