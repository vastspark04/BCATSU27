public class PilotUnitFilter : IUnitFilter
{
	public bool PassesFilter(UnitSpawner uSpawner)
	{
		if (!(uSpawner.prefabUnitSpawn is AIAircraftSpawn))
		{
			return uSpawner.prefabUnitSpawn is PlayerSpawn;
		}
		return true;
	}
}
