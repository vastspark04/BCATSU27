public class AILockingRadarSpawn : GroundUnitSpawn
{
	public LockingRadar lockingRadar;

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		if (base.isLocal)
		{
			lockingRadar.radar.radarEnabled = engageEnemies;
		}
	}

	protected override void OnSetEngageEnemies(bool engage)
	{
		if (base.isLocal)
		{
			lockingRadar.radar.radarEnabled = engage;
		}
	}
}
