using UnityEngine;

public class SCCUnitAlive : ScenarioConditionalComponent
{
	[SCCField]
	public UnitReference unitRef;

	public override bool GetCondition()
	{
		int unitID = unitRef.unitID;
		UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
		if (unit != null)
		{
			if (unit.prefabUnitSpawn is PlayerSpawn)
			{
				if (FlightSceneManager.instance.playerActor != null)
				{
					return FlightSceneManager.instance.playerActor.alive;
				}
				return false;
			}
			if (unit.prefabUnitSpawn is AIUnitSpawn)
			{
				if (unit.spawned)
				{
					if ((bool)unit.spawnedUnit && (bool)unit.spawnedUnit.actor)
					{
						return unit.spawnedUnit.actor.alive;
					}
					return false;
				}
				return true;
			}
			Debug.LogError("Unknown unit type in SCCUnitAlive conditional:" + unit.GetType().ToString());
			return false;
		}
		return false;
	}
}
