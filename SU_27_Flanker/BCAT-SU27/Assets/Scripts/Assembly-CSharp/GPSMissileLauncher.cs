using UnityEngine;

public class GPSMissileLauncher : MissileLauncher
{
	public override void RemoteFireOn(Actor actor)
	{
		Missile nextMissile = GetNextMissile();
		HPEquipGPSBombRack component = GetComponent<HPEquipGPSBombRack>();
		if ((bool)nextMissile && (bool)component && (bool)component.weaponManager && component.weaponManager.gpsSystem.hasTarget)
		{
			nextMissile.SetGPSTarget(component.weaponManager.gpsSystem.currentGroup.currentTarget);
			FireMissile();
			return;
		}
		Debug.LogError(string.Format("gps remote fire fail. nextM={0}, gpsEq={1}, gpsEq.weaponManager={2}, hasTarget={3}", nextMissile ? "exist" : "null", component ? "exist" : "null", component.weaponManager ? "exist" : "null", component.weaponManager.gpsSystem.hasTarget));
	}
}
