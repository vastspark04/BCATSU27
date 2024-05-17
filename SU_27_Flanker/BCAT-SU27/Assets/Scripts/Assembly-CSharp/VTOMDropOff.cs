using System.Collections.Generic;
using UnityEngine;

public class VTOMDropOff : VTObjectiveModule
{
	[UnitSpawn("Drop Off Targets")]
	public UnitReferenceListSame targets;

	[UnitSpawnAttributeRange("Min Required", 1f, 50f, UnitSpawnAttributeRange.RangeTypes.Int)]
	public float minRequired;

	[UnitSpawnAttributeRange("Per Unit Reward", 0f, 100000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float perUnitReward;

	[UnitSpawnAttributeRange("Full Completion Bonus", 0f, 100000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float fullCompleteBonus;

	[UnitSpawnAttributeRange("Unload Radius", 10f, 1000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float unloadRadius = 300f;

	[UnitSpawn("Dropoff Rally Point")]
	public Waypoint dropoffRallyPt;

	public VTOMDropOff()
	{
		PickupUnitFilter[] array = new PickupUnitFilter[1]
		{
			new PickupUnitFilter()
		};
		IUnitFilter[] unitFilters = array;
		targets = new UnitReferenceListSame(unitFilters);
	}

	protected override void SetupMonobehaviour()
	{
		RescueMission rescueMission = (RescueMission)(base.objectiveBehaviour = objectiveObject.AddComponent<RescueMission>());
		UnloadingZone unloadingZone = null;
		if (objective.waypoint != null)
		{
			unloadingZone = objective.waypoint.GetTransform().gameObject.AddComponent<UnloadingZone>();
			unloadingZone.dropoffObjectiveID = objective.objectiveID;
			unloadingZone.radius = unloadRadius;
			if (dropoffRallyPt != null)
			{
				unloadingZone.unloadRallyPoint = dropoffRallyPt.GetTransform();
				unloadingZone.unloadRallyWpt = dropoffRallyPt;
			}
		}
		List<RescueObjective> list = new List<RescueObjective>();
		foreach (UnitReference unit in targets.units)
		{
			RescueObjective rescueObjective = unit.GetUnit().gameObject.AddComponent<RescueObjective>();
			rescueObjective.objectiveID = string.Format("{0}{1}{2}", unit.GetDisplayName(), "_dropoff_", objective.objectiveID);
			unit.GetUnit().GetComponentInChildren<Soldier>().targetUnloadZones.Add(unloadingZone);
			list.Add(rescueObjective);
		}
		rescueMission.objectives = list.ToArray();
		rescueMission.minRequired = Mathf.RoundToInt(Mathf.Min(minRequired, list.Count));
		rescueMission.perItemBudgetBonus = perUnitReward;
		rescueMission.fullCompletionBudgetBonus = fullCompleteBonus;
	}

	public override string GetDescription()
	{
		return "This usually should be paired with a Pick-Up objective. \n\nPlayer must drop off at least [Min Required] units at the objective waypoint to complete this mission. \n\nAn objective waypoint is required. Units will unload when the player is within [Unload Radius] from the waypoint. Once the units are dropped off, they will be directed to the [Dropoff Rally Point]\n\nAt least one [Drop Off Targets] must be selected. Only certain units can be picked up. The value of the minimum requirement will be clamped to the total amount of selected units. \n\nThe objective's base budget reward is given when the minimum is completed. The [Full Completion Bonus] is given in addition to that when all units are picked up.";
	}

	public override bool IsConfigurationComplete()
	{
		if (targets != null && targets.units != null && targets.units.Count > 0)
		{
			return objective.waypoint != null;
		}
		return false;
	}
}
