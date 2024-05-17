using System;
using System.Collections;
using UnityEngine;

public class HPEquipOpticalML : HPEquipMissileLauncher, IRequiresOpticalTargeter, ILocalizationUser
{
	[HideInInspector]
	public OpticalMissileLauncher oml;

	private bool manualUncaged;

	public float uncagedFOVFraction = 1f;

	private string s_MANUAL;

	private string s_AUTO;

	private string s_uncageMode;

	private string s_noTgp;

	public float autoUncageFraction { get; private set; }

	public bool autoUncage { get; private set; } = true;


	public override void ApplyLocalization()
	{
		base.ApplyLocalization();
		s_AUTO = VTLocalizationManager.GetString("AUTO");
		s_MANUAL = VTLocalizationManager.GetString("MANUAL");
		s_uncageMode = VTLocalizationManager.GetString("oml_uncageMode", "UNCAGE MODE", "Option title for optical missiles' seeker uncage mode (unlocking from the forward aiming position).");
		s_noTgp = VTLocalizationManager.GetString("oml_noTgp", "NO TGP", "Optical AGM is selected but there is no TGP attached");
	}

	protected override void Awake()
	{
		base.Awake();
		oml = (OpticalMissileLauncher)ml;
	}

	public override Vector3 GetAimPoint()
	{
		return oml.GetAimPoint();
	}

	public override int GetReticleIndex()
	{
		return oml.GetReticleIndex();
	}

	public override int GetCount()
	{
		return oml.GetAmmoCount();
	}

	public override int GetMaxCount()
	{
		return ml.hardpoints.Length;
	}

	public override float GetWeaponDamage()
	{
		if (ml.missileCount > 0 && ml.GetNextMissile() is ClusterMissile)
		{
			ClusterMissile clusterMissile = (ClusterMissile)ml.GetNextMissile();
			if ((bool)clusterMissile.subMl.missiles[0])
			{
				return clusterMissile.subMl.missiles[0].explodeDamage;
			}
			Debug.LogError("Tried to get damage of a cluster missile submunition but it was null.");
			return 0f;
		}
		return base.GetWeaponDamage();
	}

	public override void OnEnableWeapon()
	{
		base.OnEnableWeapon();
		if ((bool)base.weaponManager.opticalTargeter)
		{
			oml.SetTargeter(base.weaponManager.opticalTargeter);
			oml.OnEnableWeapon();
		}
		else if ((bool)base.weaponManager.vm && (bool)base.weaponManager.vm.hudMessages)
		{
			base.weaponManager.vm.hudMessages.SetMessage(shortName, s_noTgp);
		}
		StartCoroutine(ItemActivatedRoutine());
	}

	public override void OnDisableWeapon()
	{
		base.OnDisableWeapon();
		oml.OnDisableWeapon();
		manualUncaged = false;
		if ((bool)base.dlz)
		{
			base.dlz.SetNoTarget();
		}
		if ((bool)base.weaponManager.vm && (bool)base.weaponManager.vm.hudMessages)
		{
			base.weaponManager.vm.hudMessages.RemoveMessage(shortName);
		}
	}

	protected override void OnJettison()
	{
		base.OnJettison();
		if ((bool)base.weaponManager && (bool)base.weaponManager.vm && (bool)base.weaponManager.vm.hudMessages)
		{
			base.weaponManager.vm.hudMessages.RemoveMessage(shortName);
		}
	}

	protected override void OnEquip()
	{
		base.OnEquip();
		oml = (OpticalMissileLauncher)ml;
		oml.SetTargeter(base.weaponManager.opticalTargeter);
		ml.parentActor = base.weaponManager.actor;
		if (!ml.parentActor)
		{
			Debug.LogError(fullName + " was equipped without a parent actor!");
		}
		autoUncageFraction = oml.boresightFOVFraction;
		if ((bool)oml.lockAudioSource && (bool)base.weaponManager.sensorAudioTransform)
		{
			oml.lockAudioSource.transform.position = base.weaponManager.sensorAudioTransform.position;
		}
		EquipFunction equipFunction = new EquipFunction();
		equipFunction.optionName = s_uncageMode;
		equipFunction.optionReturnLabel = (autoUncage ? s_AUTO : s_MANUAL);
		equipFunction.optionEvent = (EquipFunction.OptionEvent)Delegate.Combine(equipFunction.optionEvent, new EquipFunction.OptionEvent(ToggleAutoUncage));
		equipFunctions = new EquipFunction[1] { equipFunction };
		if (!autoUncage)
		{
			oml.boresightFOVFraction = 0f;
		}
	}

	private string ToggleAutoUncage()
	{
		autoUncage = !autoUncage;
		if (autoUncage)
		{
			oml.boresightFOVFraction = autoUncageFraction;
		}
		else
		{
			oml.boresightFOVFraction = 0f;
		}
		if (!autoUncage)
		{
			return s_MANUAL;
		}
		return s_AUTO;
	}

	public override void OnStartFire()
	{
		if (autoUncage)
		{
			Fire();
			return;
		}
		manualUncaged = true;
		oml.boresightFOVFraction = uncagedFOVFraction;
	}

	private void Fire()
	{
		if (oml.TryFireMissile())
		{
			base.weaponManager.ToggleCombinedWeapon();
			if (!autoUncage && (bool)base.weaponManager.currentEquip && base.weaponManager.currentEquip is HPEquipOpticalML)
			{
				manualUncaged = false;
				oml.boresightFOVFraction = 0f;
				HPEquipOpticalML obj = (HPEquipOpticalML)base.weaponManager.currentEquip;
				obj.manualUncaged = true;
				obj.oml.boresightFOVFraction = uncagedFOVFraction;
			}
		}
	}

	public override void OnStopFire()
	{
		manualUncaged = false;
		if (!autoUncage)
		{
			oml.boresightFOVFraction = 0f;
		}
	}

	public override void OnCycleWeaponButton()
	{
		if (autoUncage || !manualUncaged)
		{
			base.OnCycleWeaponButton();
		}
		else
		{
			Fire();
		}
	}

	public void SetOpticalTargeter(OpticalTargeter t)
	{
		oml.SetTargeter(t);
	}

	private IEnumerator ItemActivatedRoutine()
	{
		while (base.itemActivated && (bool)base.dlz)
		{
			if ((bool)oml.targeter && oml.targeter.locked)
			{
				base.dlz.UpdateLaunchParams(base.weaponManager.vesselRB.position, base.weaponManager.vesselRB.velocity, oml.targeter.lockTransform.position, oml.targeter.targetVelocity);
			}
			else
			{
				base.dlz.SetNoTarget();
			}
			yield return null;
		}
	}

	public override bool LaunchAuthorized()
	{
		if (oml.missileCount > 0)
		{
			Missile nextMissile = oml.GetNextMissile();
			if ((bool)nextMissile)
			{
				if (!base.dlz)
				{
					if (!oml.targetLocked)
					{
						return nextMissile.opticalLOAL;
					}
					return true;
				}
				if (!nextMissile.opticalLOAL)
				{
					if (base.dlz.inRangeMax)
					{
						return oml.targetLocked;
					}
					return false;
				}
				if (base.dlz.inRangeMax && (oml.targetLocked || Vector3.Angle(oml.aimPoint - base.transform.position, oml.targeter.lockTransform.position - base.transform.position) < 1f))
				{
					return true;
				}
			}
		}
		return false;
	}

	protected override void LoadEquipData(ConfigNode weaponNode)
	{
		base.LoadEquipData(weaponNode);
		bool target = false;
		ConfigNodeUtils.TryParseValue(weaponNode, "autoUncage", ref target);
		autoUncage = target;
	}

	protected override void SaveEquipData(ConfigNode weaponNode)
	{
		base.SaveEquipData(weaponNode);
		weaponNode.SetValue("autoUncage", autoUncage);
	}
}
