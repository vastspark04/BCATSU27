using UnityEngine;

public class VTOMFlyTo : VTObjectiveModule
{
	[UnitSpawnAttributeRange("Radius", 10f, 100000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float triggerRadius = 2000f;

	[UnitSpawn("Spherical Radius")]
	public bool sphericalRadius;

	protected override void SetupMonobehaviour()
	{
		FlyToPositionMission flyToPositionMission = (FlyToPositionMission)(base.objectiveBehaviour = objectiveObject.AddComponent<FlyToPositionMission>());
		flyToPositionMission.radius = triggerRadius;
		flyToPositionMission.planarRadius = !sphericalRadius;
		if (objective.waypoint != null && objective.waypoint.GetTransform() != null)
		{
			flyToPositionMission.targetTransform = objective.waypoint.GetTransform();
		}
		else
		{
			Debug.Log("Objective " + objective.objectiveName + " is missing a waypoint!");
		}
	}

	public override string GetDescription()
	{
		return "Player must fly at least [Radius] meters from the waypoint to complete this objective.\n\nAn objective waypoint is required. \n\nBy default, the altitude component of distance is ignored. Set [Spherical Radius] to take altitude into account.";
	}

	public override bool IsConfigurationComplete()
	{
		return objective.waypoint != null;
	}
}
