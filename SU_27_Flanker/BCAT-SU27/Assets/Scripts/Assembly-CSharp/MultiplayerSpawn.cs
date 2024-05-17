using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

public class MultiplayerSpawn : UnitSpawn, IHasTeam
{
	public enum Vehicles
	{
		AV42C,
		FA26B,
		F45A,
		AH94
	}

	public Teams team;

	public GameObject editorModelObject;

	[RefreshUnitOptionsOnChange]
	[UnitSpawn("Vehicle")]
	public Vehicles vehicle;

	[UnitSpawn("Start Mode")]
	public PlayerSpawn.FlightStartModes startMode;

	[UnitSpawn("Equipment")]
	public VehicleEquipmentList equipment = new VehicleEquipmentList();

	[UnitSpawnOptionConditional("IsMulticrew")]
	[UnitSpawnAttributeRange(min = 1f, max = 2f, name = "Allowed Slots", rangeType = UnitSpawnAttributeRange.RangeTypes.Int)]
	public float slots = 1f;

	[UnitSpawnAttributeRange("Initial Airspeed", 0f, 600f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float initialSpeed;

	public AICarrierSpawn spawnedOnCarrier;

	public string VehicleName()
	{
		return GetVehicleName(vehicle);
	}

	public bool IsMulticrew(Dictionary<string, string> unitFields)
	{
		return VTResources.GetPlayerVehicle(GetVehicleName(ConfigNodeUtils.ParseEnum<Vehicles>(unitFields["vehicle"]))).maxSlots > 1;
	}

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		if ((bool)editorModelObject)
		{
			editorModelObject.SetActive(value: false);
		}
	}

	public override void OnPreSpawnUnit()
	{
		base.OnPreSpawnUnit();
		base.transform.parent = unitSpawner.transform;
		base.transform.localPosition = Vector3.zero;
	}

	public Teams GetTeam()
	{
		return team;
	}

	public static string GetVehicleName(Vehicles v)
	{
		return v switch
		{
			Vehicles.AV42C => "AV-42C", 
			Vehicles.FA26B => "F/A-26B", 
			Vehicles.F45A => "F-45A", 
			Vehicles.AH94 => "AH-94", 
			_ => "ERROR", 
		};
	}

	public static Vehicles GetVehicleEnum(string vehicleName)
	{
		return vehicleName switch
		{
			"AV-42C" => Vehicles.AV42C, 
			"F/A-26B" => Vehicles.FA26B, 
			"F-45A" => Vehicles.F45A, 
			"AH-94" => Vehicles.AH94, 
			_ => (Vehicles)(-1), 
		};
	}

	public void SetupSpawnedVehicle(GameObject vehicleObj)
	{
		StartCoroutine(PlayerSpawnRoutine(vehicleObj));
	}

	private IEnumerator PlayerSpawnRoutine(GameObject vehicleObj)
	{
		PlayerVehicle playerVehicle = VTScenario.current.vehicle;
		Actor playerActor = vehicleObj.GetComponent<Actor>();
		playerActor.unitSpawn = this;
		Rigidbody vehicleRb = vehicleObj.GetComponent<Rigidbody>();
		VRHead vehicleVRHead = vehicleObj.GetComponentInChildren<VRHead>(includeInactive: true);
		playerActor.SetTeam(team);
		Debug.LogError("TODO: callsign designation");
		vehicleObj.SetActive(value: true);
		FlightInfo flightInfo = vehicleObj.GetComponentInChildren<FlightInfo>();
		flightInfo.PauseGCalculations();
		if ((bool)LevelBuilder.fetch)
		{
			LevelBuilder.fetch.playerTransform = vehicleObj.transform;
		}
		Vector3 vector3 = (vehicleObj.transform.position = (vehicleRb.position = base.transform.TransformPoint(playerVehicle.playerSpawnOffset)));
		vehicleObj.GetComponent<PlayerVehicleSetup>().SetupForFlightMP();
		RaySpringDamper[] suspensions = flightInfo.wheelsController.suspensions;
		for (int i = 0; i < suspensions.Length; i++)
		{
			suspensions[i].sampleCityStreets = true;
		}
		yield return new WaitForFixedUpdate();
		vector3 = (vehicleObj.transform.position = (vehicleRb.position = base.transform.TransformPoint(playerVehicle.playerSpawnOffset)));
		if (!VTMapManager.fetch.scenarioReady)
		{
			Debug.Log("MultiplayerSpawn waiting for VTMapManager scenarioReady");
			while (!VTMapManager.fetch.scenarioReady)
			{
				vector3 = (vehicleObj.transform.position = (vehicleRb.position = base.transform.TransformPoint(playerVehicle.playerSpawnOffset)));
				yield return null;
			}
		}
		vector3 = (vehicleObj.transform.position = (vehicleRb.position = base.transform.TransformPoint(playerVehicle.playerSpawnOffset)));
		Debug.LogError("TODO: airport parking spaces");
		AirportManager airportManager = null;
		if ((bool)airportManager)
		{
			foreach (AirportManager.ParkingSpace parkingSpace in airportManager.parkingSpaces)
			{
				if ((parkingSpace.transform.position - vehicleObj.transform.position).sqrMagnitude < parkingSpace.parkingNode.parkingSize * parkingSpace.parkingNode.parkingSize)
				{
					parkingSpace.OccupyParking(playerActor);
					break;
				}
			}
		}
		yield return null;
		vector3 = (vehicleObj.transform.position = (vehicleRb.position = base.transform.TransformPoint(playerVehicle.playerSpawnOffset)));
		Quaternion quaternion3 = (vehicleObj.transform.rotation = (vehicleRb.rotation = Quaternion.AngleAxis(playerVehicle.spawnPitch, base.transform.right) * base.transform.rotation));
		yield return null;
		vector3 = (vehicleObj.transform.position = (vehicleRb.position = base.transform.TransformPoint(playerVehicle.playerSpawnOffset)));
		VTOLQuickStart qs = vehicleObj.GetComponentInChildren<VTOLQuickStart>();
		if (startMode != 0)
		{
			qs.QuickStart();
		}
		bool onCarrier = unitSpawner.spawnFlags.Contains("carrier");
		if ((bool)VTMapGenerator.fetch)
		{
			VTMapGenerator.fetch.SetChunkLOD(VTMapGenerator.fetch.ChunkGridAtPos(vehicleObj.transform.position), 0);
			VTMapGenerator.fetch.BakeColliderAtPosition(vehicleObj.transform.position);
			yield return null;
			vector3 = (vehicleObj.transform.position = (vehicleRb.position = base.transform.TransformPoint(playerVehicle.playerSpawnOffset)));
		}
		flightInfo.ForceUpdateNow();
		flightInfo.ForceUpdateRadarAltitude();
		if ((unitSpawner.editorPlacementMode != 0) ? (!onCarrier && unitSpawner.editorPlacementMode == UnitSpawner.EditorPlacementModes.Air) : (!onCarrier && flightInfo.radarAltitude > 10f))
		{
			Debug.Log("Player started flying");
			vehicleRb.interpolation = RigidbodyInterpolation.Interpolate;
			flightInfo.rb.velocity = initialSpeed * base.transform.forward;
			if ((bool)qs.throttle)
			{
				qs.throttle.RemoteSetThrottle(0.6f);
			}
			if ((bool)qs.gearLever)
			{
				qs.gearLever.RemoteSetState(1);
			}
			TiltController componentInChildren = vehicleObj.GetComponentInChildren<TiltController>();
			if ((bool)componentInChildren)
			{
				componentInChildren.SetTiltImmediate(90f);
			}
			GearAnimator componentInChildren2 = vehicleObj.GetComponentInChildren<GearAnimator>();
			if ((bool)componentInChildren2)
			{
				componentInChildren2.RetractImmediate();
			}
			if (startMode == PlayerSpawn.FlightStartModes.FlightAP)
			{
				VTOLAutoPilot componentInChildren3 = vehicleObj.GetComponentInChildren<VTOLAutoPilot>();
				componentInChildren3.ToggleAltitudeHold();
				componentInChildren3.ToggleHeadingHold();
			}
			qs.FireStartFlyingEvents();
		}
		else
		{
			Debug.Log("Player started landed");
			flightInfo.rb.velocity = Vector3.zero;
			flightInfo.rb.angularVelocity = Vector3.zero;
			quaternion3 = (vehicleObj.transform.rotation = (vehicleRb.rotation = Quaternion.AngleAxis(playerVehicle.spawnPitch, base.transform.right) * base.transform.rotation));
			if ((bool)qs.brakeLockLever)
			{
				qs.brakeLockLever.RemoteSetState(1);
			}
			PlayerVehicleSetup pvs = vehicleObj.GetComponent<PlayerVehicleSetup>();
			pvs.LandVehicle(unitSpawner.transform);
			VehicleMaster component = vehicleObj.GetComponent<VehicleMaster>();
			if ((bool)component)
			{
				if (onCarrier)
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
			if (startMode == PlayerSpawn.FlightStartModes.Cold)
			{
				CanopyAnimator componentInChildren4 = vehicleObj.GetComponentInChildren<CanopyAnimator>();
				if ((bool)componentInChildren4)
				{
					if ((bool)pvs.canopyLever)
					{
						pvs.canopyLever.RemoteSetState(1);
					}
					componentInChildren4.SetCanopyImmediate(open: true);
				}
				if (!base.gameObject.activeSelf)
				{
					Debug.LogError("Vehicle object was not active");
				}
				VRDoor[] componentsInChildren = vehicleObj.GetComponentsInChildren<VRDoor>();
				foreach (VRDoor vRDoor in componentsInChildren)
				{
					if (vRDoor.muvsSeatIdx == 0 && vRDoor.openOnSpawn_mp)
					{
						vRDoor.RemoteSetState(1f);
					}
				}
			}
		}
		Debug.LogError("TODO: preloaded passengers");
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
		vehicleRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
		ScreenFader.FadeIn();
		yield return new WaitForSeconds(1f);
		flightInfo.UnpauseGCalculations();
	}

	[SCCUnitProperty("Is Alive", true)]
	public bool SCC_IsAlive()
	{
		if ((bool)actor)
		{
			return actor.alive;
		}
		return false;
	}

	[SCCUnitProperty("Landed", true)]
	public bool SCC_Landed()
	{
		if ((bool)actor)
		{
			return actor.flightInfo.isLanded;
		}
		return false;
	}

	[SCCUnitProperty("Near Waypoint", new string[] { "Waypoint", "Radius" }, true)]
	public bool SCC_NearWaypoint(Waypoint wpt, [VTRangeParam(10f, 200000f)] float radius)
	{
		if ((bool)actor)
		{
			return (actor.position - wpt.worldPosition).sqrMagnitude < radius * radius;
		}
		return false;
	}

	[SCCUnitProperty("Altitude ASL", new string[] { "Comparison", "Altitude(m)" }, false)]
	public bool SCC_Altitude(FloatComparisons comparison, [VTRangeParam(0f, 20000f)] float altitude)
	{
		if (!actor)
		{
			return false;
		}
		if (comparison == FloatComparisons.Greater_Than)
		{
			return actor.flightInfo.altitudeASL > altitude;
		}
		return actor.flightInfo.altitudeASL < altitude;
	}

	[SCCUnitProperty("Altitude Radar", new string[] { "Comparison", "Altitude(m)" }, false)]
	public bool SCC_AltitudeRadar(FloatComparisons comparison, [VTRangeParam(0f, 20000f)] float altitude)
	{
		if (!actor)
		{
			return false;
		}
		if (comparison == FloatComparisons.Greater_Than)
		{
			return actor.flightInfo.radarAltitude > altitude;
		}
		return actor.flightInfo.radarAltitude < altitude;
	}

	[SCCUnitProperty("Airspeed", new string[] { "Comparison", "Speed(m/s)" }, false)]
	public bool SCC_Airspeed(FloatComparisons comparison, [VTRangeParam(0f, 2000f)] float airspeed)
	{
		if (!actor)
		{
			return false;
		}
		if (comparison == FloatComparisons.Greater_Than)
		{
			return actor.flightInfo.airspeed > airspeed;
		}
		return actor.flightInfo.airspeed < airspeed;
	}

	[SCCUnitProperty("Surface Speed", new string[] { "Comparison", "Speed(m/s)" }, false)]
	public bool SCC_SurfaceSpeed(FloatComparisons comparison, [VTRangeParam(0f, 2000f)] float surfaceSpeed)
	{
		if (!actor)
		{
			return false;
		}
		if (comparison == FloatComparisons.Greater_Than)
		{
			return actor.flightInfo.surfaceSpeed > surfaceSpeed;
		}
		return actor.flightInfo.surfaceSpeed < surfaceSpeed;
	}

	[SCCUnitProperty("Locking Target", new string[] { "Target", "Method" }, true)]
	public bool SCC_IsLockingTarget([VTActionParam(typeof(TeamOptions), TeamOptions.BothTeams)] UnitReference target, PlayerSpawn.TargetingMethods method)
	{
		if (!base.actor)
		{
			return false;
		}
		Actor actor = target.GetActor();
		if ((bool)actor && actor.alive)
		{
			VTNetEntity netEntity = base.actor.GetNetEntity();
			if ((bool)netEntity)
			{
				return VTOLMPUnitManager.instance.IsPlayerLockingUnit(netEntity.ownerID, target.unitID, method);
			}
			return false;
		}
		return false;
	}

	[SCCUnitProperty("Locking Target In List", new string[] { "Units", "Method" }, false)]
	public bool SCC_IsLockingTgtList(UnitReferenceList list, PlayerSpawn.TargetingMethods method)
	{
		if (!actor)
		{
			return false;
		}
		for (int i = 0; i < list.units.Count; i++)
		{
			if (SCC_IsLockingTarget(list.units[i], method))
			{
				return true;
			}
		}
		return false;
	}
}
