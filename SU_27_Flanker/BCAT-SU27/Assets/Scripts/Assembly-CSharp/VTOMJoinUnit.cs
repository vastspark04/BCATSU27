public class VTOMJoinUnit : VTObjectiveModule
{
	[UnitSpawnAttributeURef("Target Unit", TeamOptions.SameTeam, false)]
	public UnitReference targetUnit;

	[UnitSpawnAttributeRange("Radius", 10f, 100000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float radius;

	protected override void SetupMonobehaviour()
	{
		FlyToPositionMission flyToPositionMission = (FlyToPositionMission)(base.objectiveBehaviour = objectiveObject.AddComponent<FlyToPositionMission>());
		flyToPositionMission.radius = radius;
		flyToPositionMission.targetTransform = targetUnit.GetSpawner().spawnedUnit.transform;
		flyToPositionMission.targetUnit = targetUnit.GetSpawner();
		if (objective.waypoint == null)
		{
			flyToPositionMission.waypointTransform = flyToPositionMission.targetTransform;
		}
		else
		{
			flyToPositionMission.waypoint = objective.waypoint;
		}
	}

	public override string GetDescription()
	{
		return "The objective is completed when the player flies within [Radius] of the [Target Unit]. A [Target Unit] is required. If the objective does not have a waypoint set, it will automatically be set to the [Target Unit].";
	}

	public override bool IsConfigurationComplete()
	{
		return targetUnit.unitID >= 0;
	}
}
