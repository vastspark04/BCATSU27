using UnityEngine;

public class HPEquipAGMCruiseMissile : HPEquipMissileLauncher
{
	public float tgtAcquireFOV = 10f;

	private FixedPoint tgtPoint;

	private bool tgtAcquired;

	public override Vector3 GetAimPoint()
	{
		if (tgtAcquired)
		{
			return tgtPoint.point;
		}
		return base.transform.position + base.transform.forward * 1000f;
	}

	public override bool LaunchAuthorized()
	{
		if (tgtAcquired)
		{
			return GetCount() > 0;
		}
		return false;
	}

	public override void OnDisableWeapon()
	{
		base.OnDisableWeapon();
		tgtAcquired = false;
	}

	public override void OnStartFire()
	{
		base.OnStartFire();
		if (ml.waitingForWpnBay || !tgtAcquired)
		{
			return;
		}
		if (GetCount() > 0)
		{
			GPSTargetGroup currentGroup = base.weaponManager.gpsSystem.currentGroup;
			Missile nextMissile = ml.GetNextMissile();
			AntiSurfaceCruiseGuidance component = nextMissile.GetComponent<AntiSurfaceCruiseGuidance>();
			if (currentGroup.isPath && currentGroup.targets.Count > 1 && currentGroup.currentTargetIdx < currentGroup.targets.Count - 1)
			{
				nextMissile.SetGPSTarget(currentGroup.targets[currentGroup.targets.Count - 1]);
				component.SetTarget(currentGroup);
			}
			else
			{
				nextMissile.SetGPSTarget(currentGroup.currentTarget);
				component.SetTarget(currentGroup.currentTarget);
			}
			ml.FireMissile();
		}
		base.weaponManager.ToggleCombinedWeapon();
	}

	private void Update()
	{
		tgtAcquired = false;
		if (base.itemActivated && base.weaponManager.gpsSystem.hasTarget)
		{
			Vector3 worldPosition = base.weaponManager.gpsSystem.currentGroup.currentTarget.worldPosition;
			tgtPoint = new FixedPoint(worldPosition);
			if (Vector3.Angle(worldPosition - base.transform.position, base.transform.forward) < tgtAcquireFOV / 2f)
			{
				tgtAcquired = true;
			}
		}
	}
}
