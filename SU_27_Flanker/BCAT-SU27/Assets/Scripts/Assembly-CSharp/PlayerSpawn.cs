using System;
using System.Collections;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

public class PlayerSpawn : UnitSpawn, IHasTeam, ICanHoldPassengers
{
	public enum FlightStartModes
	{
		Cold,
		FlightReady,
		FlightAP
	}

	public enum TargetingMethods
	{
		Radar,
		TGP,
		TSD,
		ARAD
	}

	public class IsTankerUnitFilter : IUnitFilter
	{
		public bool PassesFilter(UnitSpawner uSpawner)
		{
			return uSpawner.prefabUnitSpawn is AIAirTankerSpawn;
		}
	}

	private static PlayerSpawn instance;

	private bool pvReady;

	public GameObject editorModelObject;

	public AICarrierSpawn spawnedOnCarrier;

	private VehicleMaster playerVm;

	private Rigidbody vehicleRb;

	[UnitSpawn("Start Mode")]
	public FlightStartModes startMode;

	[UnitSpawnAttributeRange("Initial Airspeed", 0f, 600f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float initialSpeed;

	[UnitSpawn("Aircraft Group")]
	public VTUnitGroup.UnitGroup unitGroup;

	private ConfigNode qsNode;

	public static bool qLoadPlayerComplete;

	private RefuelPort playerRefuelPort;

	public static bool playerVehicleReady
	{
		get
		{
			if (VTNetworkManager.instance.netState != 0 || VTOLMPLobbyManager.isInLobby)
			{
				Debug.LogError("We checked if the single player spawn was ready in a multiplayer game!");
			}
			if ((bool)instance)
			{
				return instance.pvReady;
			}
			return false;
		}
	}

	public Teams GetTeam()
	{
		return Teams.Allied;
	}

	[VTEvent("Set Waypoint", "Set the player's current waypoint.", new string[] { "Waypoint" })]
	public void SetWaypoint(Waypoint wpt)
	{
		if ((bool)WaypointManager.instance)
		{
			if (wpt != null)
			{
				WaypointManager.instance.SetWaypoint(wpt);
			}
			else
			{
				WaypointManager.instance.SetWaypoint(null);
			}
		}
		else
		{
			Debug.LogError("Event tried to set player's waypoint but waypoint manager doesn't exist!");
		}
	}

	[VTEvent("Kill Pilot", "Kill the pilot instantaneously (as if killed by g-forces or impact)")]
	public void KillPilot()
	{
		BlackoutEffect componentInChildren = playerVm.GetComponentInChildren<BlackoutEffect>(includeInactive: true);
		EjectionSeat componentInChildren2 = playerVm.GetComponentInChildren<EjectionSeat>(includeInactive: true);
		if ((bool)componentInChildren && (bool)componentInChildren2 && !componentInChildren.accelDied && !componentInChildren2.ejected)
		{
			Debug.Log("Killing pilot via event action.");
			componentInChildren.AccelDie();
		}
	}

	[VTEvent("Destroy Vehicle", "Destroy the player's aircraft without killing the pilot.")]
	public void DestroyVehicle()
	{
		Health component = playerVm.GetComponent<Health>();
		if ((bool)component && component.normalizedHealth > 0f)
		{
			Debug.Log("Destroying player vehicle by event action.");
			component.Damage(float.MaxValue, playerVm.transform.position, Health.DamageTypes.Impact, null, "Vehicle destroyed by event action.");
		}
	}

	[VTEvent("Repair Vehicle", "Fully repair the vehicle.")]
	public void RepairVehicle()
	{
		playerVm.GetComponent<VehiclePart>().Repair();
	}

	[VTEvent("Reset Vehicle", "Recover the vehicle and return it to this state.", new string[] { "Position", "Heading", "Speed", "Mode", "Fade Time" })]
	public void ResetVehicle(Waypoint wp, float heading, float speed, FlightStartModes mode, float fadeTime)
	{
		StartCoroutine(ResetRoutine(wp, heading, speed, mode, fadeTime));
	}

	private IEnumerator ResetRoutine(Waypoint wp, float heading, float speed, FlightStartModes mode, float fadeTime)
	{
		if (fadeTime > 0f)
		{
			ScreenFader.FadeOut(fadeTime);
		}
		RepairVehicle();
		Rigidbody rb = playerVm.flightInfo.rb;
		FlightInfo flightInfo = playerVm.flightInfo;
		flightInfo.PauseGCalculations();
		yield return new WaitForFixedUpdate();
		Vector3 vector2 = (rb.position = (rb.transform.position = wp.worldPosition));
		Vector3 vector3 = VectorUtils.BearingVector(heading);
		Quaternion quaternion3 = (rb.rotation = (rb.transform.rotation = Quaternion.LookRotation(vector3)));
		rb.velocity = speed * vector3;
		rb.angularVelocity = Vector3.zero;
		VTOLQuickStart component = playerVm.gameObject.GetComponent<VTOLQuickStart>();
		switch (mode)
		{
		case FlightStartModes.Cold:
			component.quickStopComponents.ApplySettings();
			break;
		case FlightStartModes.FlightReady:
			component.QuickStart();
			break;
		case FlightStartModes.FlightAP:
		{
			component.QuickStart();
			VTOLAutoPilot componentInChildren = playerVm.gameObject.GetComponentInChildren<VTOLAutoPilot>();
			if (playerVm.isVTOLCapable && speed < 1f)
			{
				componentInChildren.ToggleHoverMode();
				componentInChildren.ToggleAltitudeHold();
			}
			else
			{
				componentInChildren.ToggleAltitudeHold();
				componentInChildren.ToggleHeadingHold();
			}
			break;
		}
		}
		yield return new WaitForFixedUpdate();
		flightInfo.UnpauseGCalculations();
		if (fadeTime > 0f)
		{
			ScreenFader.FadeIn(fadeTime);
		}
	}

	[UnitSpawnAttributeConditional("IsEditor")]
	[VTEvent("Close Doors")]
	public void CloseAllDoors()
	{
		VRDoor[] doors = playerVm.GetComponent<VehicleControlManifest>().doors;
		for (int i = 0; i < doors.Length; i++)
		{
			doors[i].RemoteSetState(0f);
		}
	}

	private void Awake()
	{
		instance = this;
	}

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		if ((bool)editorModelObject)
		{
			editorModelObject.SetActive(value: false);
		}
		StartCoroutine(PlayerSpawnRoutine());
	}

	public override void OnPreSpawnUnit()
	{
		base.OnPreSpawnUnit();
		GameObject gameObject = null;
		PlayerVehicle vehicle = VTScenario.current.vehicle;
		gameObject = vehicle.vehiclePrefab;
		if ((bool)gameObject)
		{
			if ((bool)VTMapGenerator.fetch)
			{
				VTMapGenerator.fetch.BakeColliderAtPosition(base.transform.position);
			}
			Vector3 position = base.transform.TransformPoint(vehicle.playerSpawnOffset);
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, position, base.transform.rotation);
			vehicleRb = gameObject2.GetComponent<Rigidbody>();
			playerVm = gameObject2.GetComponent<VehicleMaster>();
			vehicleRb.interpolation = RigidbodyInterpolation.None;
			Actor actor = (FlightSceneManager.instance.playerActor = gameObject2.GetComponent<Actor>());
			Actor obj = (base.actor = actor);
			PlayerVehicleNetSync component2 = gameObject2.GetComponent<PlayerVehicleNetSync>();
			if ((bool)component2)
			{
				component2.Initialize();
			}
			obj.actorName = PilotSaveManager.current.pilotName;
			obj.unitSpawn = this;
		}
	}

	private void OnDestroy()
	{
		Debug.Log("PlayerSpawn destroyed.");
	}

	private IEnumerator PlayerSpawnRoutine()
	{
		qLoadPlayerComplete = false;
		PlayerVehicle playerVehicle = VTScenario.current.vehicle;
		Actor playerActor = actor;
		GameObject vehicleObj = playerActor.gameObject;
		VRHead vehicleVRHead = vehicleObj.GetComponentInChildren<VRHead>(includeInactive: true);
		if (unitGroup != null)
		{
			int num = 1;
			int num2 = unitGroup.unitIDs.IndexOf(unitSpawner.unitInstanceID) + 1;
			playerActor.designation = new Actor.Designation(unitGroup.groupID, num, num2);
		}
		else
		{
			for (int i = 0; i < 26; i++)
			{
				if (VTScenario.current.groups.GetUnitGroup(Teams.Allied, (PhoneticLetters)i) == null)
				{
					playerActor.designation = new Actor.Designation((PhoneticLetters)i, 1, 1);
					i = 27;
				}
			}
		}
		vehicleObj.SetActive(value: true);
		FlightInfo flightInfo = vehicleObj.GetComponentInChildren<FlightInfo>();
		flightInfo.PauseGCalculations();
		if ((bool)LevelBuilder.fetch)
		{
			LevelBuilder.fetch.playerTransform = vehicleObj.transform;
		}
		vehicleObj.GetComponent<PlayerVehicleSetup>().SetupForFlight();
		RaySpringDamper[] suspensions = flightInfo.wheelsController.suspensions;
		for (int j = 0; j < suspensions.Length; j++)
		{
			suspensions[j].sampleCityStreets = true;
		}
		yield return new WaitForFixedUpdate();
		if (!VTMapManager.fetch.scenarioReady)
		{
			Debug.Log("PlayerSpawn waiting for VTMapManager scenarioReady");
			while (!VTMapManager.fetch.scenarioReady)
			{
				yield return null;
			}
		}
		bool hookLoaded = false;
		if (QuicksaveManager.isQuickload)
		{
			while (qsNode == null)
			{
				yield return null;
			}
			yield return null;
			Debug.Log("PlayerSpawn quickloading...");
			yield return new WaitForFixedUpdate();
			try
			{
				PilotSaveManager.currentScenario.inFlightSpending = qsNode.GetValue<float>("inFlightSpending");
			}
			catch (Exception ex)
			{
				Debug.LogError("Error when quickloading inFlightSpending!\n" + ex);
				QuicksaveManager.instance.IndicateError();
			}
			Vector3D globalPoint = ConfigNodeUtils.ParseVector3D(qsNode.GetValue("playerGlobalPos"));
			Quaternion rotation = Quaternion.Euler(ConfigNodeUtils.ParseVector3(qsNode.GetValue("playerRot")));
			Vector3 velocity = ConfigNodeUtils.ParseVector3(qsNode.GetValue("playerVel"));
			Vector3 angularVelocity = ConfigNodeUtils.ParseVector3(qsNode.GetValue("playerAngVel"));
			vehicleObj.transform.position = VTMapManager.GlobalToWorldPoint(globalPoint);
			vehicleObj.transform.rotation = rotation;
			vehicleRb.velocity = velocity;
			vehicleRb.position = vehicleObj.transform.position;
			vehicleRb.rotation = rotation;
			vehicleRb.angularVelocity = angularVelocity;
			int catIdx = -1;
			CatapultHook catHook = vehicleObj.GetComponentInChildren<CatapultHook>();
			if ((bool)catHook && qsNode.GetValue<bool>("catHooked"))
			{
				Vector3 vector = VTMapManager.GlobalToWorldPoint(qsNode.GetValue<Vector3D>("catapultPos"));
				catIdx = qsNode.GetValue<int>("catIdx");
				Vector3 vector2 = vehicleObj.transform.position - catHook.hookForcePointTransform.position;
				vehicleObj.transform.position = vector + vector2;
				vehicleRb.velocity = qsNode.GetValue<Vector3>("carrierVelocity");
				vehicleRb.position = vehicleObj.transform.position;
				vehicleRb.angularVelocity = Vector3.zero;
				hookLoaded = true;
			}
			yield return null;
			flightInfo.ForceUpdateNow();
			vehicleRb.interpolation = RigidbodyInterpolation.Interpolate;
			vehicleRb.GetComponentInChildren<Battery>().Connect();
			IQSVehicleComponent[] componentsInChildrenImplementing = vehicleObj.GetComponentsInChildrenImplementing<IQSVehicleComponent>(includeInactive: true);
			foreach (IQSVehicleComponent iQSVehicleComponent in componentsInChildrenImplementing)
			{
				try
				{
					iQSVehicleComponent.OnQuickload(qsNode);
				}
				catch (Exception ex2)
				{
					Debug.LogError("Player spawn had an error when quickloading component: " + UIUtils.GetHierarchyString(((Component)iQSVehicleComponent).gameObject) + "\n" + ex2);
					QuicksaveManager.instance.IndicateError();
				}
			}
			if (hookLoaded)
			{
				Debug.Log("PlayerSpawn hookLoaded");
				VTOLQuickStart componentInChildren = vehicleObj.GetComponentInChildren<VTOLQuickStart>();
				componentInChildren.throttle.RemoteSetThrottle(0f);
				if ((bool)playerVm.launchBarSwitch)
				{
					playerVm.launchBarSwitch.RemoteSetState(1);
				}
				VTOLQuickStart.QuickStartComponents.QSEngine[] engines = componentInChildren.quickStartComponents.engines;
				foreach (VTOLQuickStart.QuickStartComponents.QSEngine qSEngine in engines)
				{
					qSEngine.engine.SetThrottle(qSEngine.engine.idleThrottle);
				}
				yield return StartCoroutine(QL_ReHookRoutine(catIdx, catHook, vehicleRb));
			}
			yield return null;
			if (!hookLoaded && qsNode.HasValue("landedLocalPos"))
			{
				RaySpringDamper susp = playerActor.flightInfo.wheelsController.suspensions[0];
				while (!susp.touchingCollider)
				{
					yield return null;
				}
				if (susp.touchingCollider.gameObject.name == qsNode.GetValue("landedColName"))
				{
					Vector3 vector5 = (playerActor.transform.position = (playerActor.flightInfo.rb.position = susp.touchingCollider.transform.TransformPoint(qsNode.GetValue<Vector3>("landedLocalPos"))));
				}
			}
			Debug.Log("PlayerSpawn quickload complete");
			qLoadPlayerComplete = true;
			Debug.Log("qLoadPlayerComplete = true");
			MissionManager.instance.UpdateMissions();
			AirportManager closestAirportAtSpawn = GetClosestAirportAtSpawn();
			if ((bool)closestAirportAtSpawn)
			{
				foreach (AirportManager.ParkingSpace parkingSpace in closestAirportAtSpawn.parkingSpaces)
				{
					if ((parkingSpace.transform.position - vehicleObj.transform.position).sqrMagnitude < parkingSpace.parkingNode.parkingSize * parkingSpace.parkingNode.parkingSize)
					{
						parkingSpace.OccupyParking(playerActor);
						break;
					}
				}
			}
		}
		else
		{
			AirportManager closestAirportAtSpawn2 = GetClosestAirportAtSpawn();
			if ((bool)closestAirportAtSpawn2)
			{
				foreach (AirportManager.ParkingSpace parkingSpace2 in closestAirportAtSpawn2.parkingSpaces)
				{
					if ((parkingSpace2.transform.position - vehicleObj.transform.position).sqrMagnitude < parkingSpace2.parkingNode.parkingSize * parkingSpace2.parkingNode.parkingSize)
					{
						parkingSpace2.OccupyParking(playerActor);
						break;
					}
				}
			}
			yield return null;
			Vector3 vector5 = (vehicleObj.transform.position = (vehicleRb.position = base.transform.TransformPoint(playerVehicle.playerSpawnOffset)));
			Quaternion quaternion3 = (vehicleObj.transform.rotation = (vehicleRb.rotation = Quaternion.AngleAxis(playerVehicle.spawnPitch, base.transform.right) * base.transform.rotation));
			yield return null;
			VTOLQuickStart componentInChildren2 = vehicleObj.GetComponentInChildren<VTOLQuickStart>();
			if ((bool)componentInChildren2 && startMode != 0)
			{
				componentInChildren2.QuickStart();
			}
			bool flag = unitSpawner.spawnFlags.Contains("carrier");
			if ((bool)VTMapGenerator.fetch)
			{
				VTMapGenerator.fetch.SetChunkLOD(VTMapGenerator.fetch.ChunkGridAtPos(vehicleObj.transform.position), 0);
				VTMapGenerator.fetch.BakeColliderAtPosition(vehicleObj.transform.position);
			}
			flightInfo.ForceUpdateNow();
			flightInfo.ForceUpdateRadarAltitude();
			if ((unitSpawner.editorPlacementMode != 0) ? (!flag && unitSpawner.editorPlacementMode == UnitSpawner.EditorPlacementModes.Air) : (!flag && flightInfo.radarAltitude > 10f))
			{
				Debug.Log("Player started flying");
				vehicleRb.interpolation = RigidbodyInterpolation.Interpolate;
				flightInfo.rb.velocity = initialSpeed * base.transform.forward;
				if ((bool)componentInChildren2.throttle)
				{
					componentInChildren2.throttle.RemoteSetThrottle(0.6f);
				}
				if ((bool)componentInChildren2.gearLever)
				{
					componentInChildren2.gearLever.RemoteSetState(1);
				}
				TiltController componentInChildren3 = vehicleObj.GetComponentInChildren<TiltController>();
				if ((bool)componentInChildren3)
				{
					componentInChildren3.SetTiltImmediate(90f);
				}
				GearAnimator componentInChildren4 = vehicleObj.GetComponentInChildren<GearAnimator>();
				if ((bool)componentInChildren4)
				{
					componentInChildren4.RetractImmediate();
				}
				if (startMode == FlightStartModes.FlightAP)
				{
					VTOLAutoPilot componentInChildren5 = vehicleObj.GetComponentInChildren<VTOLAutoPilot>();
					if (playerVm.isVTOLCapable && initialSpeed < 1f)
					{
						componentInChildren5.ToggleHoverMode();
						componentInChildren5.ToggleAltitudeHold();
					}
					else
					{
						componentInChildren5.ToggleAltitudeHold();
						componentInChildren5.ToggleHeadingHold();
					}
				}
				componentInChildren2.FireStartFlyingEvents();
			}
			else
			{
				Debug.Log("Player started landed");
				flightInfo.rb.velocity = Vector3.zero;
				flightInfo.rb.angularVelocity = Vector3.zero;
				flightInfo.rb.rotation = base.transform.rotation;
				if ((bool)componentInChildren2 && (bool)componentInChildren2.brakeLockLever)
				{
					componentInChildren2.brakeLockLever.RemoteSetState(1);
				}
				PlayerVehicleSetup pvs = vehicleObj.GetComponent<PlayerVehicleSetup>();
				pvs.LandVehicle(base.transform);
				VehicleMaster component = vehicleObj.GetComponent<VehicleMaster>();
				if ((bool)component)
				{
					if (flag)
					{
						component.SetWingFoldImmediate(folded: true);
					}
					else
					{
						component.SetWingFoldImmediate(folded: false);
					}
				}
				while (!flightInfo.isLanded)
				{
					yield return null;
				}
				if (startMode == FlightStartModes.Cold)
				{
					CanopyAnimator componentInChildren6 = vehicleObj.GetComponentInChildren<CanopyAnimator>();
					if ((bool)componentInChildren6)
					{
						if ((bool)pvs.canopyLever)
						{
							pvs.canopyLever.RemoteSetState(1);
						}
						componentInChildren6.SetCanopyImmediate(open: true);
					}
					VRDoor[] componentsInChildren = vehicleObj.GetComponentsInChildren<VRDoor>();
					foreach (VRDoor vRDoor in componentsInChildren)
					{
						if (vRDoor.openOnSpawn_sp)
						{
							vRDoor.RemoteSetState(1f);
						}
					}
				}
			}
			StartCoroutine(PreloadedPassengersRoutine());
		}
		if ((bool)VTMapGenerator.fetch)
		{
			if ((bool)vehicleVRHead)
			{
				VTMapGenerator.fetch.StartLODRoutine(vehicleVRHead.transform);
			}
			else if ((bool)VRHead.instance)
			{
				VTMapGenerator.fetch.StartLODRoutine(VRHead.instance.transform);
			}
		}
		yield return null;
		if (!QuicksaveManager.quickloading)
		{
			ScreenFader.FadeIn();
		}
		yield return new WaitForSeconds(1f);
		if (!hookLoaded)
		{
			flightInfo.UnpauseGCalculations();
		}
		pvReady = true;
	}

	private AirportManager GetClosestAirportAtSpawn()
	{
		if ((bool)spawnedOnCarrier)
		{
			return spawnedOnCarrier.airportManager;
		}
		AirportManager result = null;
		float num = float.MaxValue;
		foreach (AirportManager allAirport in VTScenario.current.GetAllAirports())
		{
			float sqrMagnitude = (allAirport.transform.position - FlightSceneManager.instance.playerActor.position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = allAirport;
			}
		}
		return result;
	}

	private IEnumerator PreloadedPassengersRoutine()
	{
		Debug.Log("PreloadedPassengersRoutine()");
		while (!VTMapManager.fetch.scenarioReady)
		{
			yield return null;
		}
		PassengerBay passengerBay = actor.GetComponentInChildren<PassengerBay>(includeInactive: true);
		if ((bool)passengerBay && unitSpawner.childSpawners.Count > 0)
		{
			Debug.Log("Player PassengerBay and preloaded passengers found. Waiting for spawns...");
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
			{
				foreach (UnitSpawner childSpawner2 in unitSpawner.childSpawners)
				{
					passengerBay.LoadSoldier(childSpawner2.spawnedUnit.GetComponent<Soldier>(), instantMove: true);
				}
				yield break;
			}
		}
		if (!passengerBay)
		{
			Debug.Log("No passenger bay found.");
		}
		if (unitSpawner.childSpawners.Count == 0)
		{
			Debug.Log("No child spawners.");
		}
	}

	public override void Quicksave(ConfigNode qsNode)
	{
		Actor playerActor = FlightSceneManager.instance.playerActor;
		Vector3D v = VTMapManager.WorldToGlobalPoint(playerActor.transform.position);
		Quaternion rotation = playerActor.transform.rotation;
		Vector3 velocity = playerActor.velocity;
		Vector3 angularVelocity = playerActor.GetComponent<Rigidbody>().angularVelocity;
		qsNode.SetValue("playerGlobalPos", ConfigNodeUtils.WriteVector3D(v));
		qsNode.SetValue("playerRot", ConfigNodeUtils.WriteVector3(rotation.eulerAngles));
		qsNode.SetValue("playerVel", ConfigNodeUtils.WriteVector3(velocity));
		qsNode.SetValue("playerAngVel", ConfigNodeUtils.WriteVector3(angularVelocity));
		if (playerActor.flightInfo.isLanded)
		{
			RaySpringDamper raySpringDamper = playerActor.flightInfo.wheelsController.suspensions[0];
			if ((bool)raySpringDamper.touchingCollider)
			{
				Vector3 value = raySpringDamper.touchingCollider.transform.InverseTransformPoint(playerActor.transform.position);
				qsNode.SetValue("landedColName", raySpringDamper.touchingCollider.gameObject.name);
				qsNode.SetValue("landedLocalPos", value);
			}
		}
		CatapultHook componentInChildren = playerActor.GetComponentInChildren<CatapultHook>();
		if ((bool)componentInChildren)
		{
			qsNode.SetValue("catHooked", componentInChildren.hooked);
			if (componentInChildren.hooked)
			{
				qsNode.SetValue("catapultPos", VTMapManager.WorldToGlobalPoint(componentInChildren.catapult.transform.position));
				qsNode.SetValue("catIdx", componentInChildren.catapult.GetComponentInParent<CarrierCatapultManager>().catapults.IndexOf(componentInChildren.catapult));
				Vector3 value2 = Vector3.zero;
				ShipMover componentInParent = componentInChildren.catapult.GetComponentInParent<ShipMover>();
				if ((bool)componentInParent)
				{
					value2 = componentInParent.velocity;
				}
				qsNode.SetValue("carrierVelocity", value2);
			}
		}
		IQSVehicleComponent[] componentsInChildrenImplementing = playerActor.gameObject.GetComponentsInChildrenImplementing<IQSVehicleComponent>(includeInactive: true);
		foreach (IQSVehicleComponent iQSVehicleComponent in componentsInChildrenImplementing)
		{
			try
			{
				iQSVehicleComponent.OnQuicksave(qsNode);
			}
			catch (Exception ex)
			{
				Debug.LogError("Player spawn had an error when quicksaving component: " + UIUtils.GetHierarchyString(((Component)iQSVehicleComponent).gameObject) + "\n" + ex);
				QuicksaveManager.instance.IndicateError();
			}
		}
		try
		{
			qsNode.SetValue("inFlightSpending", PilotSaveManager.currentScenario.inFlightSpending);
		}
		catch (Exception ex2)
		{
			Debug.LogError("Error when quicksaving inFlightSpending!\n" + ex2);
			QuicksaveManager.instance.IndicateError();
		}
	}

	public override void Quickload(ConfigNode qsNode)
	{
		this.qsNode = qsNode;
	}

	private IEnumerator QL_ReHookRoutine(int catIdx, CatapultHook hook, Rigidbody vehicleRb)
	{
		Transform vehicleTf = vehicleRb.transform;
		AICarrierSpawn cSpawn = null;
		float num = float.MaxValue;
		foreach (UnitSpawner value in VTScenario.current.units.alliedUnits.Values)
		{
			if (value.prefabUnitSpawn is AICarrierSpawn)
			{
				float sqrMagnitude = (vehicleTf.position - value.spawnedUnit.transform.position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					cSpawn = (AICarrierSpawn)value.spawnedUnit;
				}
			}
		}
		if (!cSpawn)
		{
			yield break;
		}
		CarrierCatapult cat = cSpawn.GetComponentInChildren<CarrierCatapultManager>().catapults[catIdx];
		if ((bool)cat)
		{
			while (!hook.hooked)
			{
				Vector3 vector = hook.hookForcePointTransform.position - vehicleTf.position;
				Vector3 vector4 = (vehicleTf.position = (vehicleRb.position = cat.catapultTransform.position - vector));
				vehicleTf.transform.rotation = cat.catapultTransform.rotation;
				vehicleRb.velocity = cSpawn.actor.GetComponent<Rigidbody>().GetPointVelocity(vehicleRb.position);
				vehicleRb.angularVelocity = cSpawn.actor.GetComponent<Rigidbody>().angularVelocity;
				yield return new WaitForFixedUpdate();
			}
		}
	}

	[SCCUnitProperty("Landed", true)]
	public bool SCC_Landed()
	{
		return FlightSceneManager.instance.playerActor.flightInfo.isLanded;
	}

	[SCCUnitProperty("Near Waypoint", new string[] { "Waypoint", "Radius" }, true)]
	public bool SCC_NearWaypoint(Waypoint wpt, [VTRangeParam(10f, 200000f)] float radius)
	{
		return (FlightSceneManager.instance.playerActor.position - wpt.worldPosition).sqrMagnitude < radius * radius;
	}

	[SCCUnitProperty("Fuel Level", new string[] { "Comparison", "Percent" }, false)]
	public bool SCC_FuelLevel(FloatComparisons comparison, [VTRangeParam(0f, 100f)] float percent)
	{
		float num = percent / 100f;
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 0; i < playerVm.fuelTanks.Length; i++)
		{
			if ((bool)playerVm.fuelTanks[i])
			{
				num2 += playerVm.fuelTanks[i].fuel;
				num3 += playerVm.fuelTanks[i].maxFuel;
			}
		}
		float num4 = num2 / num3;
		return comparison switch
		{
			FloatComparisons.Greater_Than => num4 > num, 
			FloatComparisons.Less_Than => num4 < num, 
			_ => false, 
		};
	}

	[SCCUnitProperty("Engines On", true)]
	public bool SCC_EnginesOn()
	{
		for (int i = 0; i < playerVm.engines.Length; i++)
		{
			ModuleEngine moduleEngine = playerVm.engines[i];
			if (!moduleEngine)
			{
				return false;
			}
			if (moduleEngine.useTorquePhysics)
			{
				if (moduleEngine.outputRPM < playerVm.powerGovernor.idleRPM)
				{
					return false;
				}
				if ((bool)playerVm.powerGovernor && playerVm.powerGovernor.currentThrottleLimit < playerVm.powerGovernor.throttleIdleNotch)
				{
					return false;
				}
			}
			else if (!moduleEngine.startedUp)
			{
				return false;
			}
		}
		return true;
	}

	[SCCUnitProperty("Altitude ASL", new string[] { "Comparison", "Altitude(m)" }, false)]
	public bool SCC_Altitude(FloatComparisons comparison, [VTRangeParam(0f, 20000f)] float altitude)
	{
		if (comparison == FloatComparisons.Greater_Than)
		{
			return FlightSceneManager.instance.playerActor.flightInfo.altitudeASL > altitude;
		}
		return FlightSceneManager.instance.playerActor.flightInfo.altitudeASL < altitude;
	}

	[SCCUnitProperty("Altitude Radar", new string[] { "Comparison", "Altitude(m)" }, false)]
	public bool SCC_AltitudeRadar(FloatComparisons comparison, [VTRangeParam(0f, 20000f)] float altitude)
	{
		if (comparison == FloatComparisons.Greater_Than)
		{
			return FlightSceneManager.instance.playerActor.flightInfo.radarAltitude > altitude;
		}
		return FlightSceneManager.instance.playerActor.flightInfo.radarAltitude < altitude;
	}

	[SCCUnitProperty("Airspeed", new string[] { "Comparison", "Speed(m/s)" }, false)]
	public bool SCC_Airspeed(FloatComparisons comparison, [VTRangeParam(0f, 2000f)] float airspeed)
	{
		if (comparison == FloatComparisons.Greater_Than)
		{
			return FlightSceneManager.instance.playerActor.flightInfo.airspeed > airspeed;
		}
		return FlightSceneManager.instance.playerActor.flightInfo.airspeed < airspeed;
	}

	[SCCUnitProperty("Surface Speed", new string[] { "Comparison", "Speed(m/s)" }, false)]
	public bool SCC_SurfaceSpeed(FloatComparisons comparison, [VTRangeParam(0f, 2000f)] float surfaceSpeed)
	{
		if (comparison == FloatComparisons.Greater_Than)
		{
			return FlightSceneManager.instance.playerActor.flightInfo.surfaceSpeed > surfaceSpeed;
		}
		return FlightSceneManager.instance.playerActor.flightInfo.surfaceSpeed < surfaceSpeed;
	}

	[SCCUnitProperty("Detected by", new string[] { "Team" }, true)]
	public bool SCC_DetectedBy(Teams d_team)
	{
		if (d_team == Teams.Allied)
		{
			return actor.detectedByAllied;
		}
		return actor.detectedByEnemy;
	}

	[SCCUnitProperty("Locking Target In List", new string[] { "Units", "Method" }, false)]
	public bool SCC_IsLockingTgtList(UnitReferenceList list, TargetingMethods method)
	{
		for (int i = 0; i < list.units.Count; i++)
		{
			if (SCC_IsLockingTarget(list.units[i], method))
			{
				return true;
			}
		}
		return false;
	}

	[SCCUnitProperty("Locking Target", new string[] { "Target", "Method" }, true)]
	public bool SCC_IsLockingTarget([VTActionParam(typeof(TeamOptions), TeamOptions.BothTeams)] UnitReference target, TargetingMethods method)
	{
		Actor actor = target.GetActor();
		if ((bool)actor && actor.alive)
		{
			switch (method)
			{
			case TargetingMethods.Radar:
				if ((bool)base.actor.weaponManager.lockingRadar)
				{
					if (base.actor.weaponManager.lockingRadar.IsLocked())
					{
						return base.actor.weaponManager.lockingRadar.currentLock.actor == actor;
					}
					return false;
				}
				return false;
			case TargetingMethods.TGP:
				if ((bool)base.actor.weaponManager.opticalTargeter)
				{
					return base.actor.weaponManager.opticalTargeter.lockedActor == actor;
				}
				return false;
			case TargetingMethods.TSD:
				if ((bool)base.actor.weaponManager.tsc)
				{
					return base.actor.weaponManager.tsc.GetCurrentSelectionActor() == actor;
				}
				return false;
			case TargetingMethods.ARAD:
				if ((bool)base.actor.weaponManager.arad)
				{
					return base.actor.weaponManager.arad.selectedActor == actor;
				}
				return false;
			default:
				return false;
			}
		}
		return false;
	}

	[SCCUnitProperty("Pitch Attitude", new string[] { "Comparison", "Pitch" }, false)]
	public bool SCC_PitchComparison(FloatComparisons c, [VTRangeParam(-90f, 90f)] float pitch)
	{
		if (c == FloatComparisons.Greater_Than)
		{
			return actor.flightInfo.pitch > pitch;
		}
		return actor.flightInfo.pitch < pitch;
	}

	[SCCUnitProperty("Roll Attitude", new string[] { "Comparison", "Roll" }, false)]
	public bool SCC_RollComparison(FloatComparisons c, [VTRangeParam(-180f, 180f)] float roll)
	{
		if (c == FloatComparisons.Greater_Than)
		{
			return actor.flightInfo.roll > roll;
		}
		return actor.flightInfo.roll < roll;
	}

	[SCCUnitProperty("Connected to Tanker", new string[] { "Tanker Unit" }, false)]
	public bool SCC_ConnectedToTanker([VTActionParam(typeof(AllowSubUnits), AllowSubUnits.Disallow)][VTActionParam(typeof(TeamOptions), TeamOptions.SameTeam)][VTActionParam(typeof(IsTankerUnitFilter), null)] UnitReference tankerUnit)
	{
		if (playerRefuelPort == null)
		{
			playerRefuelPort = actor.GetComponentInChildren<RefuelPort>();
		}
		if (playerRefuelPort.currentRefuelPlane != null)
		{
			return playerRefuelPort.currentRefuelPlane.actor == tankerUnit.GetActor();
		}
		return false;
	}

	[SCCUnitProperty("Using Weapon", new string[] { "Wpn ShortName" }, false)]
	public bool SCC_UsingWeaponStr([VTActionParam(typeof(TextInputModes), TextInputModes.SingleLine)][VTActionParam(typeof(int), 16)] string shortName)
	{
		if (actor.weaponManager.currentEquip != null)
		{
			return actor.weaponManager.currentEquip.shortName == shortName;
		}
		return false;
	}

	[UnitSpawnAttributeConditional("IsEditor")]
	[SCCUnitProperty("Wpn Count", new string[] { "Wpn ShortName", "Comparison", "Count" }, false)]
	public bool SCC_WeaponCount([VTActionParam(typeof(TextInputModes), TextInputModes.SingleLine)][VTActionParam(typeof(int), 16)] string shortName, IntComparisons comparison, [VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)][VTRangeParam(0f, 999f)] float compareCount)
	{
		int num = 0;
		for (int i = 0; i < actor.weaponManager.equipCount; i++)
		{
			HPEquippable equip = actor.weaponManager.GetEquip(i);
			if ((bool)equip && equip.shortName == shortName)
			{
				num += equip.GetCount();
			}
		}
		return comparison switch
		{
			IntComparisons.Equals => (float)num == compareCount, 
			IntComparisons.Greater_Than => (float)num > compareCount, 
			IntComparisons.Less_Than => (float)num < compareCount, 
			_ => false, 
		};
	}

	[SCCUnitProperty("Is Damaged", true)]
	public bool SCC_IsDamaged()
	{
		VehiclePart component = actor.GetComponent<VehiclePart>();
		return RecurrCheckDamage(component);
	}

	private bool RecurrCheckDamage(VehiclePart part)
	{
		if (part.health.normalizedHealth < 1f)
		{
			return true;
		}
		bool flag = false;
		if (part.children != null)
		{
			foreach (VehiclePart child in part.children)
			{
				flag = flag || RecurrCheckDamage(child);
				if (flag)
				{
					return flag;
				}
			}
			return flag;
		}
		return flag;
	}

	public bool IsEditor()
	{
		return VTResources.isEditorOrDevTools;
	}

	public bool HasPassengerBay()
	{
		GameObject vehiclePrefab = VTScenario.current.vehicle.vehiclePrefab;
		if ((bool)vehiclePrefab)
		{
			return vehiclePrefab.GetComponentInChildren<PassengerBay>();
		}
		return false;
	}

	public int GetMaximumPassengers()
	{
		GameObject vehiclePrefab = VTScenario.current.vehicle.vehiclePrefab;
		if ((bool)vehiclePrefab)
		{
			PassengerBay componentInChildren = vehiclePrefab.GetComponentInChildren<PassengerBay>();
			if ((bool)componentInChildren)
			{
				return componentInChildren.seats.Length;
			}
		}
		return 0;
	}

	public Transform GetSeatTransform(int seatIdx)
	{
		return VTScenario.current.vehicle.vehiclePrefab.GetComponentInChildren<PassengerBay>().seats[seatIdx];
	}
}
