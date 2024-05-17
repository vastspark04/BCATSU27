public class VTOMDefendUnit : VTObjectiveModule
{
	public enum DefendCompletionModes
	{
		Waypoint,
		Trigger
	}

	[UnitSpawnAttributeURef("Target", TeamOptions.SameTeam, true)]
	public UnitReference target;

	[UnitSpawnAttributeRange("Radius", 5f, 10000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float radius;

	[UnitSpawn("Completion Mode")]
	public DefendCompletionModes completionMode;

	protected override void SetupMonobehaviour()
	{
		ProtectObjective protectObjective = (ProtectObjective)(base.objectiveBehaviour = objectiveObject.AddComponent<ProtectObjective>());
		protectObjective.protectActor = target.GetActor();
		protectObjective.radius = radius;
		protectObjective.completionMode = completionMode;
	}

	public override string GetDescription()
	{
		return "Player must prevent [Target] unit from being destroyed until the unit moves within [Radius] of the objective waypoint OR until the objective completion has been triggered. (Depending on [Completion Mode])\nBoth [Target] and objective waypoint are required.";
	}

	public override bool IsConfigurationComplete()
	{
		if (completionMode == DefendCompletionModes.Trigger || (objective.waypoint != null && objective.waypoint.GetTransform() != null))
		{
			return target.GetSpawner() != null;
		}
		return false;
	}
}
