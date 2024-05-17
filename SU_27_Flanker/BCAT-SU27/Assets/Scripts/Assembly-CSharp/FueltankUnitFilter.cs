public class FueltankUnitFilter : IUnitFilter
{
	public bool PassesFilter(UnitSpawner uSpawner)
	{
		if (!(uSpawner.prefabUnitSpawn is PlayerSpawn) && !(uSpawner.prefabUnitSpawn is MultiplayerSpawn))
		{
			return uSpawner.prefabUnitSpawn.GetComponentInChildren<RefuelPort>();
		}
		return true;
	}
}
