using System.Collections.Generic;

public class VTOMRefuel : VTObjectiveModule
{
	[UnitSpawn("Refuel Target")]
	public UnitReferenceListSame targets;

	[UnitSpawnAttributeRange("Fuel Level", 0.01f, 0.95f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float fuelLevel;

	public VTOMRefuel()
	{
		FueltankUnitFilter fueltankUnitFilter = new FueltankUnitFilter();
		targets = new UnitReferenceListSame(new IUnitFilter[1] { fueltankUnitFilter });
	}

	protected override void SetupMonobehaviour()
	{
		RefuelMission refuelMission = (RefuelMission)(base.objectiveBehaviour = objectiveObject.AddComponent<RefuelMission>());
		refuelMission.completionThreshold = fuelLevel;
		List<FuelTank> list = new List<FuelTank>();
		List<MultiplayerSpawn> list2 = new List<MultiplayerSpawn>();
		foreach (UnitReference unit in targets.units)
		{
			FuelTank fuelTank = null;
			if (unit.GetSpawner().spawnedUnit is PlayerSpawn)
			{
				fuelTank = FlightSceneManager.instance.playerActor.GetComponentInChildren<FuelTank>();
			}
			else if (unit.GetSpawner().spawnedUnit is MultiplayerSpawn)
			{
				MultiplayerSpawn item = (MultiplayerSpawn)unit.GetSpawner().spawnedUnit;
				list2.Add(item);
			}
			else
			{
				fuelTank = unit.GetSpawner().spawnedUnit.GetComponentInChildren<FuelTank>();
			}
			if ((bool)fuelTank)
			{
				list.Add(fuelTank);
			}
		}
		refuelMission.fuelTanks = list.ToArray();
		refuelMission.mpSpawnTargets = list2.ToArray();
	}

	public override string GetDescription()
	{
		return "The [Refuel Target] units selected must be refueled to at least [Fuel Level] to complete the objective.\n\nAt least one [Refuel Target] must be selected.";
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
