using System.Collections;
using UnityEngine;

public class GroundUnitSpawn : AIUnitSpawnEquippable
{
	public enum MoveSpeeds
	{
		Slow_10,
		Medium_20,
		Fast_30
	}

	public class BoardableAircraftFilter : IUnitFilter
	{
		public bool PassesFilter(UnitSpawner uSpawner)
		{
			if (uSpawner.prefabUnitSpawn is AIAircraftSpawn)
			{
				return ((AIAircraftSpawn)uSpawner.prefabUnitSpawn).HasPassengerBay();
			}
			return false;
		}
	}

	public GroundUnitMover mover;

	[UnitSpawnAttributeConditional("UnitCanMove")]
	[UnitSpawn("Unit Group")]
	public VTUnitGroup.UnitGroup unitGroup;

	[UnitSpawnAttributeConditional("UnitCanMove")]
	[UnitSpawnAttributeConditional("GroundUnitIsVehicle")]
	[UnitSpawn("Move Speed")]
	public MoveSpeeds moveSpeed;

	[UnitSpawnAttributeConditional("UnitCanMove")]
	[UnitSpawn("Behavior")]
	public GroundUnitMover.Behaviors behavior = GroundUnitMover.Behaviors.Parked;

	[UnitSpawnAttributeConditional("UnitCanMove")]
	[UnitSpawn("Default Path")]
	public FollowPath defaultPath;

	[UnitSpawnAttributeConditional("UnitCanMove")]
	[UnitSpawn("Default Waypoint")]
	public Waypoint waypoint;

	public bool optionalStopToEngage;

	[UnitSpawnAttributeConditional("IsOptionalStopToEngage")]
	[UnitSpawn("Stop to Engage")]
	public bool stopToEngage = true;

	public bool UnitCanMove()
	{
		return mover != null;
	}

	public bool GroundUnitIsVehicle()
	{
		return GetComponent<VehicleMover>() != null;
	}

	public bool IsOptionalStopToEngage()
	{
		return optionalStopToEngage;
	}

	[UnitSpawnAttributeConditional("UnitCanMove")]
	[UnitSpawnAttributeConditional("GroundUnitIsVehicle")]
	[VTEvent("Set Movement Speed", "Set the movement speed of this ground vehicle.", new string[] { "Speed" })]
	public void SetMovementSpeed(MoveSpeeds s)
	{
		moveSpeed = s;
		switch (s)
		{
		case MoveSpeeds.Slow_10:
			mover.moveSpeed = 10f;
			break;
		case MoveSpeeds.Medium_20:
			mover.moveSpeed = 20f;
			break;
		case MoveSpeeds.Fast_30:
			mover.moveSpeed = 30f;
			break;
		}
		if ((bool)mover.squad)
		{
			mover.squad.UpdateSizeAndSpeed();
		}
	}

	[UnitSpawnAttributeConditional("UnitCanMove")]
	[VTEvent("Set Path", "Command the unit to move along a path (if able).", new string[] { "Path" })]
	public void SetPath(FollowPath path)
	{
		if ((bool)mover)
		{
			mover.RefreshBehaviorRoutines();
			mover.path = path;
			mover.behavior = GroundUnitMover.Behaviors.Path;
		}
	}

	[UnitSpawnAttributeConditional("UnitCanMove")]
	[VTEvent("Park Now", "Command the unit to park where it stands.")]
	public void ParkNow()
	{
		if ((bool)mover)
		{
			mover.RefreshBehaviorRoutines();
			mover.behavior = GroundUnitMover.Behaviors.Parked;
		}
	}

	[UnitSpawnAttributeConditional("UnitCanMove")]
	[VTEvent("Move To", "Command the unit to move directly to a waypoint.", new string[] { "Waypoint" })]
	public void MoveTo(Waypoint wpt)
	{
		if ((bool)mover)
		{
			mover.MoveToWaypoint(wpt.GetTransform());
			mover.parkWhenInRallyRadius = true;
			mover.RefreshBehaviorRoutines();
		}
	}

	[UnitSpawnAttributeConditional("CanLoadPassengerBay")]
	[VTEvent("Board Aircraft", "Command the unit to board an AI aircraft's passenger bay.", new string[] { "Target" })]
	public void BoardAIBay([VTActionParam(typeof(BoardableAircraftFilter), null)][VTTeamOptionParam(TeamOptions.SameTeam)] UnitReference target)
	{
		UnitSpawn unit = target.GetUnit();
		if ((bool)unit && unit.unitSpawner.spawned)
		{
			AIAircraftSpawn aIAircraftSpawn = (AIAircraftSpawn)unit;
			GetComponent<Soldier>().BoardAIBay(aIAircraftSpawn.passengerBay);
			mover.RefreshBehaviorRoutines();
		}
	}

	[UnitSpawnAttributeConditional("CanLoadPassengerBay")]
	[VTEvent("Dismount Aircraft", "Command the unit to dismount the aircraft it's riding when available. It will move towards Rally Point once dismounted.", new string[] { "Rally Point" })]
	public void DismountAIBay(Waypoint wp)
	{
		GetComponent<Soldier>().DismountAIBay(wp.GetTransform());
	}

	public bool CanLoadPassengerBay()
	{
		return GetComponent<Soldier>() != null;
	}

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		if ((bool)mover)
		{
			mover.behavior = behavior;
			mover.path = defaultPath;
			if (waypoint != null)
			{
				mover.rallyTransform = waypoint.GetTransform();
			}
			if (mover is VehicleMover)
			{
				SetMovementSpeed(moveSpeed);
			}
			if (optionalStopToEngage)
			{
				IOptionalStopToEngage[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IOptionalStopToEngage>(includeInactive: true);
				for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
				{
					componentsInChildrenImplementing[i].SetStopToEngage(stopToEngage);
				}
			}
			mover.transform.position = base.transform.position + mover.height * Vector3.up;
		}
		else
		{
			actor.SetCustomVelocity(Vector3.zero);
		}
	}

	private IEnumerator SpawnRoutine()
	{
		mover.behavior = behavior;
		mover.path = defaultPath;
		if (waypoint != null)
		{
			mover.rallyTransform = waypoint.GetTransform();
		}
		yield return null;
		base.transform.position = unitSpawner.transform.position + mover.height * unitSpawner.transform.up;
		if (behavior == GroundUnitMover.Behaviors.RailPath)
		{
			float closestTimeWorld = defaultPath.GetClosestTimeWorld(base.transform.position, 6);
			Vector3 worldPoint = defaultPath.GetWorldPoint(closestTimeWorld);
			Vector3 worldTangent = defaultPath.GetWorldTangent(closestTimeWorld);
			Vector3 vector = Vector3.Cross(Vector3.up, worldTangent);
			float magnitude = Vector3.Project(base.transform.position - worldPoint, vector).magnitude;
			magnitude *= Mathf.Sign(Vector3.Dot(base.transform.position - worldPoint, vector));
			mover.railPathOffset = new Vector3(magnitude, 0f, 0f);
		}
	}
}
