using UnityEngine;

public class HPEquipARML : HPEquipMissileLauncher
{
	public Actor targetActor;

	private bool launchAuthorized;

	private Vector3 aimPointVec;

	public override void OnEnableWeapon()
	{
		base.OnEnableWeapon();
		if (ml.missileCount > 0)
		{
			ml.GetNextMissile().antiRadRWR.myActor = base.weaponManager.actor;
			ml.GetNextMissile().antiRadRWR.enabled = true;
		}
		targetActor = null;
	}

	public override void OnDisableWeapon()
	{
		base.OnDisableWeapon();
		if ((bool)base.dlz)
		{
			base.dlz.SetNoTarget();
		}
		if (ml.missileCount > 0)
		{
			ml.GetNextMissile().antiRadRWR.enabled = false;
		}
	}

	public override void OnStartFire()
	{
		if (LaunchAuthorized())
		{
			ml.GetNextMissile().antiRadTargetActor = targetActor;
			ml.FireMissile();
			if (ml.missileCount == 0 && (bool)base.dlz)
			{
				base.dlz.SetNoTarget();
			}
			base.weaponManager.ToggleCombinedWeapon();
		}
	}

	private void Update()
	{
		if (!base.itemActivated || ml.missileCount <= 0)
		{
			return;
		}
		launchAuthorized = false;
		aimPointVec = base.transform.forward * 1000f;
		if ((bool)base.dlz)
		{
			base.dlz.SetNoTarget();
		}
		if (!targetActor)
		{
			return;
		}
		ModuleRWR antiRadRWR = ml.GetNextMissile().antiRadRWR;
		for (int i = 0; i < antiRadRWR.contacts.Length; i++)
		{
			ModuleRWR.RWRContact rWRContact = antiRadRWR.contacts[i];
			if (rWRContact != null && rWRContact.active && CheckRadarBelongsToTarget(rWRContact, targetActor))
			{
				aimPointVec = rWRContact.detectedPosition - base.transform.position;
				launchAuthorized = true;
				if ((bool)base.dlz)
				{
					base.dlz.UpdateLaunchParams(antiRadRWR.transform.position, base.weaponManager.actor.velocity, rWRContact.detectedPosition, rWRContact.radarActor.velocity);
				}
				break;
			}
		}
	}

	private bool CheckRadarBelongsToTarget(ModuleRWR.RWRContact c, Actor targetActor)
	{
		if (c.radarActor == targetActor)
		{
			return true;
		}
		Actor actor = c.radarActor;
		int num = 10;
		int num2 = 0;
		while ((bool)actor.parentActor && num2 < num)
		{
			if (actor.parentActor == targetActor)
			{
				return true;
			}
			actor = actor.parentActor;
			num2++;
		}
		return false;
	}

	protected override void OnJettison()
	{
		base.OnJettison();
		if (base.itemActivated && ml.missileCount > 0)
		{
			ml.GetNextMissile().antiRadRWR.enabled = false;
		}
	}

	public override Vector3 GetAimPoint()
	{
		Vector3 result = base.transform.position + base.transform.forward * 1000f;
		if (ml.missileCount > 0)
		{
			result = base.transform.position + aimPointVec * 1000f;
		}
		return result;
	}

	public override bool LaunchAuthorized()
	{
		if ((bool)base.dlz)
		{
			if (base.dlz.targetAcquired)
			{
				return base.dlz.inRangeMax;
			}
			return false;
		}
		return launchAuthorized;
	}
}
