public class PickupUnitFilter : IUnitFilter
{
	public bool PassesFilter(UnitSpawner uSpawner)
	{
		return uSpawner.prefabUnitSpawn.GetComponent<PickupObjective>() != null;
	}
}
