using UnityEngine;

public class UnitWaypoint : Waypoint
{
	public UnitSpawner unitSpawner;

	public override void SetTransform(Transform tf)
	{
	}

	public override Transform GetTransform()
	{
		if ((bool)unitSpawner.spawnedUnit)
		{
			if (unitSpawner.spawnedUnit is PlayerSpawn)
			{
				return FlightSceneManager.instance.playerActor.transform;
			}
			return unitSpawner.spawnedUnit.transform;
		}
		return unitSpawner.transform;
	}

	public override string GetName()
	{
		return unitSpawner.GetUIDisplayName();
	}
}
