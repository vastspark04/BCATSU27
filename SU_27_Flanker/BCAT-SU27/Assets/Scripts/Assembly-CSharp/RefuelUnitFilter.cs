public class RefuelUnitFilter : IUnitFilter
{
	public bool PassesFilter(UnitSpawner uSpawner)
	{
		return uSpawner.prefabUnitSpawn is AIAirTankerSpawn;
	}
}
