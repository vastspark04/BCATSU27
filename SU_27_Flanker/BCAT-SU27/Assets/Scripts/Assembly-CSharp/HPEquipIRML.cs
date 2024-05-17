using System;
using System.Collections;
using UnityEngine;
using VTOLVR.Multiplayer;

public class HPEquipIRML : HPEquipMissileLauncher, IWeaponLeadIndicator, ILocalizationUser
{
	public bool useVrHead;

	public bool externallyControlInternalBay;

	private InternalWeaponBay iwb;

	private object iwbControlObj = new object();

	private string[] seekerModeLabels = new string[5] { "Caged", "Uncaged", "Vertical Scan", "Head Track", "Hard Lock" };

	public bool triggerUncage;

	public bool trigUncageBoresightOnly;

	private string s_ON;

	private string s_OFF;

	private string s_seekMode;

	private string s_trigUncage;

	private bool holdingTrigger;

	private Coroutine activeRoutine;

	public IRMissileLauncher irml { get; set; }

	public HeatSeeker.SeekerModes seekerMode { get; private set; }

	public override void ApplyLocalization()
	{
		base.ApplyLocalization();
		s_ON = VTLocalizationManager.GetString("ON");
		s_OFF = VTLocalizationManager.GetString("OFF");
		s_seekMode = VTLocalizationManager.GetString("irml_seekMode", "SEEK MODE", "Option title for IR missiles' heat-seeking mode");
		s_trigUncage = VTLocalizationManager.GetString("irml_trigUncage", "TRIG UNCAGE", "Option title for IR missiles' seeker to be uncaged by the trigger (trigger uncage, shortened for UI space).");
		for (int i = 0; i < seekerModeLabels.Length; i++)
		{
			string key = $"s_seekerMode_{i}";
			seekerModeLabels[i] = VTLocalizationManager.GetString(key, seekerModeLabels[i], "Label for heatseeker mode");
		}
	}

	protected override void Awake()
	{
		base.Awake();
		irml = (IRMissileLauncher)ml;
		if (useVrHead && !VTOLMPUtils.IsMultiplayer())
		{
			VRHead.OnVRHeadChanged += VRHead_OnVRHeadChanged;
		}
	}

	private void OnDestroy()
	{
		VRHead.OnVRHeadChanged -= VRHead_OnVRHeadChanged;
	}

	private void VRHead_OnVRHeadChanged()
	{
		if (!useVrHead || !base.weaponManager || !base.weaponManager.actor.isPlayer || !VRHead.instance)
		{
			return;
		}
		irml.headTransform = VRHead.instance.transform;
		Missile[] missiles = irml.missiles;
		foreach (Missile missile in missiles)
		{
			if ((bool)missile)
			{
				missile.heatSeeker.headTransform = VRHead.instance.transform;
			}
		}
	}

	public override Vector3 GetAimPoint()
	{
		return irml.GetAimPoint();
	}

	public override int GetCount()
	{
		return irml.GetAmmoCount();
	}

	public override int GetMaxCount()
	{
		return ml.hardpoints.Length;
	}

	public override void OnEnableWeapon()
	{
		base.OnEnableWeapon();
		irml.EnableWeapon();
		holdingTrigger = false;
		if (externallyControlInternalBay && (bool)iwb && !triggerUncage)
		{
			iwb.RegisterOpenReq(iwbControlObj);
		}
		if (base.weaponManager.isUserTriggerHeld && triggerUncage)
		{
			base.weaponManager.StartFire();
		}
		if (activeRoutine != null)
		{
			StopCoroutine(activeRoutine);
		}
		activeRoutine = StartCoroutine(ItemActivatedRoutine());
	}

	public override void OnDisableWeapon()
	{
		base.OnDisableWeapon();
		irml.DisableWeapon();
		if (externallyControlInternalBay && (bool)iwb)
		{
			iwb.UnregisterOpenReq(iwbControlObj);
		}
		if ((bool)base.dlz)
		{
			base.dlz.SetNoTarget();
		}
	}

	protected override void OnJettison()
	{
		base.OnJettison();
		if (externallyControlInternalBay && (bool)iwb)
		{
			iwb.UnregisterOpenReq(iwbControlObj);
		}
	}

	public override void OnConfigDetach(LoadoutConfigurator configurator)
	{
		base.OnConfigDetach(configurator);
		if (externallyControlInternalBay && (bool)iwb)
		{
			iwb.UnregisterOpenReq(iwbControlObj);
		}
	}

	public override void OnStartFire()
	{
		holdingTrigger = true;
		if (triggerUncage)
		{
			Missile nextMissile = irml.GetNextMissile();
			if ((bool)nextMissile)
			{
				nextMissile.heatSeeker.manualUncage = false;
			}
			if (externallyControlInternalBay && (bool)iwb)
			{
				iwb.RegisterOpenReq(iwbControlObj);
			}
		}
		else if (irml.TryFireMissile())
		{
			base.weaponManager.ToggleCombinedWeapon();
		}
	}

	private IEnumerator ItemActivatedRoutine()
	{
		while (base.itemActivated)
		{
			Missile nextMissile = irml.GetNextMissile();
			if ((bool)nextMissile)
			{
				if ((bool)base.dlz)
				{
					if (nextMissile.hasTarget)
					{
						Vector3 targetPosition = nextMissile.heatSeeker.targetPosition;
						Vector3 targetVelocity = nextMissile.heatSeeker.targetVelocity;
						base.dlz.UpdateLaunchParams(nextMissile.transform.position, base.weaponManager.actor.velocity, targetPosition, targetVelocity);
					}
					else
					{
						base.dlz.SetNoTarget();
					}
				}
				if (triggerUncage && holdingTrigger && (bool)nextMissile)
				{
					if (nextMissile.hasTarget)
					{
						nextMissile.heatSeeker.SetSeekerMode(HeatSeeker.SeekerModes.Uncaged);
					}
					else if (nextMissile.heatSeeker.seekerMode != seekerMode)
					{
						nextMissile.heatSeeker.SetSeekerMode(seekerMode);
					}
				}
			}
			else if ((bool)base.dlz)
			{
				base.dlz.SetNoTarget();
			}
			yield return null;
		}
	}

	public override void OnStopFire()
	{
		holdingTrigger = false;
		if (triggerUncage)
		{
			Missile nextMissile = irml.GetNextMissile();
			if ((bool)nextMissile)
			{
				nextMissile.heatSeeker.SetSeekerMode(seekerMode);
				nextMissile.heatSeeker.manualUncage = true;
			}
			if (externallyControlInternalBay && (bool)iwb)
			{
				iwb.UnregisterOpenReq(iwbControlObj);
			}
		}
	}

	public override void OnCycleWeaponButton()
	{
		if (triggerUncage && holdingTrigger)
		{
			if (irml.TryFireMissile())
			{
				base.weaponManager.ToggleCombinedWeapon();
			}
		}
		else
		{
			base.OnCycleWeaponButton();
		}
	}

	private string ToggleTriggerUncage()
	{
		triggerUncage = !triggerUncage;
		if (triggerUncage)
		{
			if (seekerMode == HeatSeeker.SeekerModes.Uncaged)
			{
				base.weaponManager.WeaponFunctionButton(0, hardpointIdx);
			}
		}
		else if (externallyControlInternalBay && (bool)iwb)
		{
			iwb.UnregisterOpenReq(iwbControlObj);
		}
		Missile[] missiles = ml.missiles;
		foreach (Missile missile in missiles)
		{
			if ((bool)missile)
			{
				missile.heatSeeker.manualUncage = triggerUncage;
				if (triggerUncage)
				{
					missile.heatSeeker.lockingRadar = null;
				}
				else
				{
					missile.heatSeeker.lockingRadar = base.weaponManager.lockingRadar;
				}
			}
		}
		if (!triggerUncage)
		{
			return s_OFF;
		}
		return s_ON;
	}

	private string ToggleScanMode()
	{
		switch (seekerMode)
		{
		case HeatSeeker.SeekerModes.Caged:
			if (triggerUncage)
			{
				seekerMode = HeatSeeker.SeekerModes.VerticalScan;
			}
			else
			{
				seekerMode = HeatSeeker.SeekerModes.Uncaged;
			}
			break;
		case HeatSeeker.SeekerModes.Uncaged:
			seekerMode = HeatSeeker.SeekerModes.VerticalScan;
			break;
		case HeatSeeker.SeekerModes.VerticalScan:
			if ((bool)irml.headTransform)
			{
				seekerMode = HeatSeeker.SeekerModes.HeadTrack;
			}
			else
			{
				seekerMode = HeatSeeker.SeekerModes.Caged;
			}
			break;
		case HeatSeeker.SeekerModes.HeadTrack:
			seekerMode = HeatSeeker.SeekerModes.Caged;
			break;
		}
		Missile[] missiles = ml.missiles;
		foreach (Missile missile in missiles)
		{
			if ((bool)missile)
			{
				missile.heatSeeker.SetSeekerMode(seekerMode);
			}
		}
		return seekerModeLabels[(int)seekerMode];
	}

	private void ForceTrigUncageBoresight()
	{
		seekerMode = HeatSeeker.SeekerModes.Uncaged;
		triggerUncage = true;
		Missile[] missiles = ml.missiles;
		foreach (Missile missile in missiles)
		{
			if ((bool)missile)
			{
				missile.heatSeeker.SetSeekerMode(seekerMode);
				missile.heatSeeker.manualUncage = true;
				missile.heatSeeker.lockingRadar = null;
			}
		}
	}

	protected override void OnEquip()
	{
		base.OnEquip();
		if (!trigUncageBoresightOnly)
		{
			EquipFunction equipFunction = new EquipFunction();
			equipFunction.optionEvent = (EquipFunction.OptionEvent)Delegate.Combine(equipFunction.optionEvent, new EquipFunction.OptionEvent(ToggleScanMode));
			equipFunction.optionName = s_seekMode;
			equipFunction.optionReturnLabel = seekerModeLabels[(int)seekerMode];
			EquipFunction equipFunction2 = new EquipFunction();
			equipFunction2.optionEvent = (EquipFunction.OptionEvent)Delegate.Combine(equipFunction2.optionEvent, new EquipFunction.OptionEvent(ToggleTriggerUncage));
			equipFunction2.optionName = s_trigUncage;
			equipFunction2.optionReturnLabel = (triggerUncage ? s_ON : s_OFF);
			equipFunctions = new EquipFunction[2] { equipFunction, equipFunction2 };
		}
		else
		{
			ForceTrigUncageBoresight();
		}
		ml.parentActor = base.weaponManager.actor;
		if (base.weaponManager.actor.isPlayer)
		{
			irml.headTransform = VRHead.instance.transform;
		}
		irml.vssReferenceTransform = base.weaponManager.transform;
		ml.OnLoadMissile -= SetupMissile;
		ml.OnLoadMissile += SetupMissile;
		Missile[] missiles = ml.missiles;
		foreach (Missile m in missiles)
		{
			SetupMissile(m);
		}
		if (!externallyControlInternalBay)
		{
			return;
		}
		InternalWeaponBay[] internalWeaponBays = base.weaponManager.internalWeaponBays;
		foreach (InternalWeaponBay internalWeaponBay in internalWeaponBays)
		{
			if (internalWeaponBay.hardpointIdx == hardpointIdx)
			{
				iwb = internalWeaponBay;
				break;
			}
		}
	}

	private void SetupMissile(Missile m)
	{
		if (!m)
		{
			return;
		}
		if ((bool)base.weaponManager)
		{
			if ((bool)base.weaponManager.sensorAudioTransform)
			{
				if ((bool)m.heatSeeker.seekerAudio)
				{
					m.heatSeeker.seekerAudio.transform.position = base.weaponManager.sensorAudioTransform.position;
				}
				if ((bool)m.heatSeeker.lockToneAudio)
				{
					m.heatSeeker.lockToneAudio.transform.position = base.weaponManager.sensorAudioTransform.position;
				}
			}
			m.heatSeeker.headTransform = irml.headTransform;
			m.heatSeeker.vssReferenceTransform = irml.vssReferenceTransform;
			m.heatSeeker.SetSeekerMode(seekerMode);
			if ((bool)base.weaponManager.irForwardOverrideTransform)
			{
				m.heatSeeker.SetForwardOverrideTransform(base.weaponManager.irForwardOverrideTransform);
			}
			if (!triggerUncage)
			{
				m.heatSeeker.lockingRadar = base.weaponManager.lockingRadar;
			}
			m.heatSeeker.manualUncage = triggerUncage;
		}
		else
		{
			Debug.LogError("Tried to set up missile but weaponManager is null");
		}
	}

	public override bool LaunchAuthorized()
	{
		if ((bool)base.dlz)
		{
			if (ml.missileCount > 0 && irml.activeMissile.hasTarget)
			{
				return base.dlz.inRangeMax;
			}
			return false;
		}
		if (ml.missileCount > 0)
		{
			return irml.activeMissile.hasTarget;
		}
		return false;
	}

	public bool GetShowLeadIndicator()
	{
		if (ml.missileCount > 0)
		{
			return ml.GetNextMissile().heatSeeker.seekerLock > 0.75f;
		}
		return false;
	}

	public Vector3 GetLeadIndicatorPosition()
	{
		Missile nextMissile = ml.GetNextMissile();
		Vector3 missileVel = base.weaponManager.vesselRB.velocity + 0.75f * nextMissile.boostThrust * nextMissile.boostTime * nextMissile.transform.forward;
		return Missile.GetLeadPoint(nextMissile.heatSeeker.targetPosition, nextMissile.heatSeeker.targetVelocity, nextMissile.transform.position, missileVel, nextMissile.maxLeadTime);
	}

	protected override void SaveEquipData(ConfigNode weaponNode)
	{
		base.SaveEquipData(weaponNode);
		weaponNode.SetValue("seekerMode", seekerMode);
		weaponNode.SetValue("triggerUncage", triggerUncage);
	}

	protected override void LoadEquipData(ConfigNode weaponNode)
	{
		base.LoadEquipData(weaponNode);
		HeatSeeker.SeekerModes target = HeatSeeker.SeekerModes.Caged;
		ConfigNodeUtils.TryParseValue(weaponNode, "seekerMode", ref target);
		seekerMode = target;
		ConfigNodeUtils.TryParseValue(weaponNode, "triggerUncage", ref triggerUncage);
	}
}
