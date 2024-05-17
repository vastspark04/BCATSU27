using UnityEngine;

public class RadarMissileLauncher : MissileLauncher, IRequiresLockingRadar
{
	public LockingRadar lockingRadar;

	public bool TryFireMissile(Actor overrideRadarLock = null, Vector3 initialTargetPos = default(Vector3))
	{
		if ((bool)lockingRadar && lockingRadar.currentLock != null && lockingRadar.currentLock.locked)
		{
			GetNextMissile().SetRadarLock(lockingRadar.currentLock);
			FireMissile();
			return true;
		}
		if ((bool)overrideRadarLock && (bool)lockingRadar)
		{
			GetNextMissile().SetDataLinkOnly(lockingRadar, overrideRadarLock, initialTargetPos);
			FireMissile();
			return true;
		}
		return false;
	}

	public void SetLockingRadar(LockingRadar lr)
	{
		lockingRadar = lr;
	}
}
