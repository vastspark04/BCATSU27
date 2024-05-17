using System.Collections.Generic;
using UnityEngine;

public class VTOMKillMission : VTObjectiveModule
{
	[UnitSpawn("Destroy Targets")]
	public UnitReferenceListOtherSubs targets = new UnitReferenceListOtherSubs();

	[UnitSpawnAttributeRange("Min Required", 1f, 50f, UnitSpawnAttributeRange.RangeTypes.Int)]
	public float minRequired;

	[UnitSpawnAttributeRange("Per Kill Reward", 0f, 100000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float perUnitReward;

	[UnitSpawnAttributeRange("Full Completion Bonus", 0f, 100000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float fullCompleteBonus;

	protected override void SetupMonobehaviour()
	{
		KillMission killMission = (KillMission)(base.objectiveBehaviour = objectiveObject.AddComponent<KillMission>());
		List<KillObjective> list = new List<KillObjective>();
		foreach (UnitReference unit in targets.units)
		{
			Actor actor = unit.GetActor();
			if ((bool)actor)
			{
				KillObjective killObjective = actor.gameObject.AddComponent<KillObjective>();
				killObjective.SetMission(killMission);
				killObjective.objectiveID = string.Format("{0}{1}{2}", unit.GetDisplayName(), "_kill_", objective.objectiveID);
				list.Add(killObjective);
			}
		}
		killMission.objectives = list.ToArray();
		killMission.minRequired = Mathf.RoundToInt(Mathf.Min(minRequired, list.Count));
		killMission.perItemBudgetBonus = perUnitReward;
		killMission.fullCompletionBudgetBonus = fullCompleteBonus;
		foreach (KillObjective item in list)
		{
			item.ForceCheckCompleted();
		}
	}

	public override string GetDescription()
	{
		return "Player must destroy at least [Min Required] units to complete this mission. The value of the minimum requirement will be clamped to the total amount of selected units. At least one [Destroy Targets] must be selected. The objective's base budget reward is given when the minimum is completed. The [Full Completion Bonus] is given in addition to that when all units are destroyed.";
	}

	public override bool IsConfigurationComplete()
	{
		if (targets != null && targets.units != null)
		{
			return targets.units.Count > 0;
		}
		return false;
	}
}
