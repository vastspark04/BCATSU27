using System.Collections.Generic;
using UnityEngine;

public class VTOMPickUp : VTObjectiveModule
{
	[UnitSpawn("Pickup Targets")]
	public UnitReferenceListSame targets;

	[UnitSpawnAttributeRange("Min Required", 1f, 50f, UnitSpawnAttributeRange.RangeTypes.Int)]
	public float minRequired;

	[UnitSpawnAttributeRange("Per Unit Reward", 0f, 100000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float perUnitReward;

	[UnitSpawnAttributeRange("Full Completion Bonus", 0f, 100000f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float fullCompleteBonus;

	public VTOMPickUp()
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
		PickupMission pickupMission = (PickupMission)(base.objectiveBehaviour = objectiveObject.AddComponent<PickupMission>());
		List<PickupObjective> list = new List<PickupObjective>();
		foreach (UnitReference unit in targets.units)
		{
			PickupObjective pickupObjective = unit.GetUnit().gameObject.AddComponent<PickupObjective>();
			pickupObjective.objectiveID = string.Format("{0}{1}{2}", unit.GetDisplayName(), "_pickup_", objective.objectiveID);
			list.Add(pickupObjective);
			Soldier component = pickupObjective.GetComponent<Soldier>();
			if ((bool)component)
			{
				component.StartWaitingForPickup();
			}
			else
			{
				Debug.LogError("VTOMPickup was setting up behavior but Soldier was not found!");
			}
		}
		pickupMission.objectives = list.ToArray();
		pickupMission.minRequired = Mathf.RoundToInt(Mathf.Min(minRequired, list.Count));
		pickupMission.perItemBudgetBonus = perUnitReward;
		pickupMission.fullCompletionBudgetBonus = fullCompleteBonus;
	}

	public override string GetDescription()
	{
		return "Player must pick up at least [Min Required] units to complete this mission.\n\nAt least one [Pickup Targets] must be selected. Only certain units can be picked up. The value of the minimum requirement will be clamped to the total amount of selected units.\n\nThe objective's base budget reward is given when the minimum is completed. The [Full Completion Bonus] is given in addition to that when all units are picked up.";
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
