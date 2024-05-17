public struct UnitReference
{
	private int idPlusOne;

	private int subUnitIdx;

	public int unitID
	{
		get
		{
			return idPlusOne - 1;
		}
		set
		{
			idPlusOne = value + 1;
		}
	}

	public UnitReference(int id)
	{
		idPlusOne = id + 1;
		subUnitIdx = -1;
	}

	public UnitReference(int id, int subIdx)
	{
		idPlusOne = id + 1;
		subUnitIdx = subIdx;
	}

	public UnitSpawner GetSpawner()
	{
		return VTScenario.current.units.GetUnit(unitID);
	}

	public UnitSpawn GetUnit()
	{
		if (idPlusOne == 0)
		{
			return null;
		}
		UnitSpawner spawner = GetSpawner();
		if ((bool)spawner)
		{
			return spawner.spawnedUnit;
		}
		return null;
	}

	public int GetSubUnitIdx()
	{
		return subUnitIdx;
	}

	public Actor GetActor()
	{
		if (unitID >= 0)
		{
			UnitSpawn unit = GetUnit();
			if ((bool)unit)
			{
				if (subUnitIdx >= 0)
				{
					return ((AIUnitSpawn)unit).subUnits[subUnitIdx];
				}
				return unit.actor;
			}
			return null;
		}
		return null;
	}

	public string GetDisplayName()
	{
		if (unitID >= 0)
		{
			UnitSpawner spawner = GetSpawner();
			if ((bool)spawner)
			{
				if (subUnitIdx >= 0)
				{
					return spawner.GetUIDisplayName() + ":" + (subUnitIdx + 1) + " (" + ((AIUnitSpawn)spawner.prefabUnitSpawn).subUnits[subUnitIdx].actorName + ")";
				}
				return spawner.GetUIDisplayName();
			}
			return "Missing!";
		}
		return "None";
	}
}
