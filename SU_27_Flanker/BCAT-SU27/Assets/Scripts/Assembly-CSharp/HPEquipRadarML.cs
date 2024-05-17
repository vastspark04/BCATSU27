using System.Collections;
using UnityEngine;

public class HPEquipRadarML : HPEquipMissileLauncher, IWeaponLeadIndicator
{
	public RadarMissileLauncher rml;

	private string hudID;

	protected override void Awake()
	{
		base.Awake();
		rml = (RadarMissileLauncher)ml;
		hudID = GetInstanceID().ToString();
	}

	public override void OnStartFire()
	{
		if ((bool)rml.iwb && rml.waitingForWpnBay)
		{
			return;
		}
		base.OnStartFire();
		if (ml.missileCount <= 0)
		{
			return;
		}
		Missile nextMissile = ml.GetNextMissile();
		Actor overrideRadarLock = null;
		Vector3 initialTargetPos = default(Vector3);
		if ((bool)base.weaponManager.tsc)
		{
			Actor currentSelectionActor = base.weaponManager.tsc.GetCurrentSelectionActor();
			if ((bool)currentSelectionActor && currentSelectionActor.role == Actor.Roles.Air && !currentSelectionActor.flightInfo.isLanded)
			{
				overrideRadarLock = currentSelectionActor;
				initialTargetPos = ((TacticalSituationController.TSActorTargetInfo)base.weaponManager.tsc.GetCurrentSelectionInfo()).estimatedPosition;
			}
		}
		if (rml.TryFireMissile(overrideRadarLock, initialTargetPos))
		{
			base.weaponManager.ToggleCombinedWeapon();
			StartCoroutine(HudInfoRoutine(nextMissile));
		}
	}

	private IEnumerator HudInfoRoutine(Missile m)
	{
		yield return null;
		if (!base.weaponManager.vm || !base.weaponManager.vm.hudMessages)
		{
			yield break;
		}
		WaitForSeconds wait = new WaitForSeconds(0.5f);
		bool wasPit = false;
		while ((bool)m && m.hasTarget && m.gameObject.activeSelf)
		{
			if (m.isPitbull && !wasPit)
			{
				wasPit = true;
				base.weaponManager.vm.flightWarnings.AddCommonWarning(FlightWarnings.CommonWarnings.Pitbull);
			}
			string message = string.Format("{0}\n{1}{2}", shortName, m.isPitbull ? "M " : "T-", Mathf.Round(m.timeToImpact));
			base.weaponManager.vm.hudMessages.SetMessage(hudID, message);
			yield return wait;
		}
		base.weaponManager.vm.hudMessages.RemoveMessage(hudID);
	}

	protected override void OnJettison()
	{
		base.OnJettison();
		if ((bool)base.weaponManager.vm && (bool)base.weaponManager.vm.hudMessages)
		{
			base.weaponManager.vm.hudMessages.RemoveMessage(hudID);
		}
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		if ((bool)base.weaponManager.vm && (bool)base.weaponManager.vm.hudMessages)
		{
			base.weaponManager.vm.hudMessages.RemoveMessage(hudID);
		}
	}

	public override int GetCount()
	{
		return ml.missileCount;
	}

	public override int GetMaxCount()
	{
		return ml.hardpoints.Length;
	}

	protected override void OnEquip()
	{
		base.OnEquip();
		ml.parentActor = base.weaponManager.actor;
		if (!base.weaponManager.isPlayer)
		{
			ml.openAndCloseBayOnLaunch = false;
		}
	}

	public override void OnEnableWeapon()
	{
		base.OnEnableWeapon();
		StartCoroutine(ItemActivatedRoutine());
	}

	private IEnumerator ItemActivatedRoutine()
	{
		while (base.itemActivated && (bool)base.dlz)
		{
			if (base.weaponManager.lockingRadar.IsLocked())
			{
				RadarLockData currentLock = base.weaponManager.lockingRadar.currentLock;
				base.dlz.UpdateLaunchParams(base.weaponManager.vesselRB.position, base.weaponManager.vesselRB.velocity, currentLock.actor.position, currentLock.actor.velocity);
			}
			else if ((bool)base.weaponManager.tsc)
			{
				Actor currentSelectionActor = base.weaponManager.tsc.GetCurrentSelectionActor();
				if ((bool)currentSelectionActor && currentSelectionActor.role == Actor.Roles.Air && !currentSelectionActor.flightInfo.isLanded)
				{
					TacticalSituationController.TSActorTargetInfo tSActorTargetInfo = (TacticalSituationController.TSActorTargetInfo)base.weaponManager.tsc.GetCurrentSelectionInfo();
					base.dlz.UpdateLaunchParams(base.weaponManager.vesselRB.position, base.weaponManager.vesselRB.velocity, tSActorTargetInfo.point, tSActorTargetInfo.velocity);
				}
				else
				{
					base.dlz.SetNoTarget();
				}
			}
			else
			{
				base.dlz.SetNoTarget();
			}
			yield return null;
		}
	}

	public override void OnDisableWeapon()
	{
		base.OnDisableWeapon();
		if ((bool)base.dlz)
		{
			base.dlz.SetNoTarget();
		}
	}

	public override bool LaunchAuthorized()
	{
		if (ml.missileCount > 0)
		{
			if ((bool)base.weaponManager.lockingRadar && base.weaponManager.lockingRadar.IsLocked())
			{
				return base.dlz.inRangeMax;
			}
			if ((bool)base.weaponManager.tsc)
			{
				Actor currentSelectionActor = base.weaponManager.tsc.GetCurrentSelectionActor();
				if ((bool)currentSelectionActor && ((currentSelectionActor.role == Actor.Roles.Air && (bool)currentSelectionActor.flightInfo && !currentSelectionActor.flightInfo.isLanded) || currentSelectionActor.role == Actor.Roles.Missile))
				{
					return base.dlz.inRangeMax;
				}
			}
		}
		return false;
	}

	public bool GetShowLeadIndicator()
	{
		if (ml.missileCount > 0 && (bool)rml.lockingRadar)
		{
			return rml.lockingRadar.IsLocked();
		}
		return false;
	}

	public Vector3 GetLeadIndicatorPosition()
	{
		Missile nextMissile = ml.GetNextMissile();
		Vector3 missileVel = base.weaponManager.vesselRB.velocity + 0.75f * nextMissile.boostThrust * nextMissile.boostTime * nextMissile.transform.forward;
		return Missile.GetLeadPoint(rml.lockingRadar.currentLock.actor.position, rml.lockingRadar.currentLock.actor.velocity, nextMissile.transform.position, missileVel, nextMissile.maxLeadTime);
	}
}
