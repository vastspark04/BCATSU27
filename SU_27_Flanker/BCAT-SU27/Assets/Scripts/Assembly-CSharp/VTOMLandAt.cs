using UnityEngine;

public class VTOMLandAt : VTObjectiveModule
{
	[UnitSpawnAttributeRange("Radius", 5f, 10000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float radius;

	protected override void SetupMonobehaviour()
	{
		Debug.Log("Setting up behaviour for LandingZoneMission");
		LandingZoneWaypointMission landingZoneWaypointMission = (LandingZoneWaypointMission)(base.objectiveBehaviour = objectiveObject.AddComponent<LandingZoneWaypointMission>());
		if (objective.waypoint == null || objective.waypoint.GetTransform() == null)
		{
			Debug.LogError("VTOMLandAt objective did not have a waypoint. (" + objective.objectiveName + ")");
			landingZoneWaypointMission.incompleteSetup = true;
		}
		else
		{
			landingZoneWaypointMission.wpt = objective.waypoint;
			landingZoneWaypointMission.radius = radius;
		}
	}

	public override string GetDescription()
	{
		return "Player needs to land within [Radius] of the objective waypoint to complete this objective.\n\nAn Objective waypoint is required.";
	}

	public override bool IsConfigurationComplete()
	{
		if (objective.waypoint != null)
		{
			return objective.waypoint.GetTransform() != null;
		}
		return false;
	}
}
