using System;
using UnityEngine;

public class HPEquipASML : HPEquipMissileLauncher, ILocalizationUser
{
	public AntiShipGuidance.ASMTerminalBehaviors defaultTerminalMode;

	private AntiShipGuidance.ASMTerminalBehaviors currentTMode;

	private string[] tModeLabels = new string[4] { "Direct", "SeaSkim", "SSEvasive", "Popup" };

	public float offBoresightLaunchAngle = 15f;

	private string s_terminalMode;

	private string s_noTarget;

	private string s_overRange;

	private string s_inRange;

	private string s_path;

	private string s_point;

	private bool pathInRange;

	private string hudMsgID;

	public override void ApplyLocalization()
	{
		base.ApplyLocalization();
		s_terminalMode = VTLocalizationManager.GetString("asml_terminalMode", "TERMINAL MODE", "An option for anti-ship cruise missiles' flight behavior in the last (terminal) phase of guidance.");
		s_noTarget = VTLocalizationManager.GetString("asml_noTarget", "NO TARGET", "HUD label shown when the selected anti-ship missile has no target.");
		s_overRange = VTLocalizationManager.GetString("asml_overRange", "OVER RANGE", "HUD label shown when the selected anti-ship missile has a target that is too far away.");
		s_inRange = VTLocalizationManager.GetString("asml_inRange", "IN RANGE", "HUD label shown when the selected anti-ship missile has a target that is within firing range.");
		s_path = VTLocalizationManager.GetString("asml_path", "PATH", "HUD label shown when the selected anti-ship missile will follow a flight path.");
		s_point = VTLocalizationManager.GetString("asml_point", "POINT", "HUD label shown when the selected anti-ship missile will fly to a specific target point.");
		for (int i = 0; i < tModeLabels.Length; i++)
		{
			string key = $"s_asmTmode_{i}";
			tModeLabels[i] = VTLocalizationManager.GetString(key, tModeLabels[i], "Antiship terminal guidance mode");
		}
	}

	private void SetupMissile(Missile m)
	{
		if ((bool)m)
		{
			m.GetComponent<AntiShipGuidance>().terminalBehavior = currentTMode;
		}
	}

	protected override void OnEquip()
	{
		base.OnEquip();
		ml.parentActor = base.weaponManager.actor;
		ml.OnLoadMissile -= SetupMissile;
		ml.OnLoadMissile += SetupMissile;
		Missile[] missiles = ml.missiles;
		foreach (Missile m in missiles)
		{
			SetupMissile(m);
		}
		EquipFunction equipFunction = new EquipFunction();
		equipFunction.optionName = s_terminalMode;
		equipFunction.optionReturnLabel = tModeLabels[(int)currentTMode];
		equipFunction.optionEvent = (EquipFunction.OptionEvent)Delegate.Combine(equipFunction.optionEvent, new EquipFunction.OptionEvent(CycleTerminalMode));
		equipFunctions = new EquipFunction[1] { equipFunction };
		hudMsgID = shortName + "asmInfo" + hardpointIdx;
	}

	public override void OnStartFire()
	{
		base.OnStartFire();
		if (!IsLaunchAuthorized())
		{
			return;
		}
		AntiShipGuidance component = ml.GetNextMissile().GetComponent<AntiShipGuidance>();
		if (base.weaponManager.gpsSystem.currentGroup.isPath)
		{
			GPSTargetGroup gPSTargetGroup = new GPSTargetGroup("ASM", 0);
			GPSTargetGroup currentGroup = base.weaponManager.gpsSystem.currentGroup;
			for (int i = currentGroup.currentTargetIdx; i < currentGroup.targets.Count; i++)
			{
				GPSTarget target = currentGroup.targets[i];
				gPSTargetGroup.AddTarget(target);
			}
			gPSTargetGroup.currentTargetIdx = 0;
			gPSTargetGroup.isPath = true;
			component.SetTarget(gPSTargetGroup);
		}
		else
		{
			component.SetTarget(base.weaponManager.gpsSystem.currentGroup.currentTarget);
		}
		ml.FireMissile();
		base.weaponManager.ToggleCombinedWeapon();
		UpdateHUDMessage();
	}

	public override int GetCount()
	{
		return ml.missileCount;
	}

	public override int GetMaxCount()
	{
		return ml.hardpoints.Length;
	}

	public override Vector3 GetAimPoint()
	{
		if (IsLaunchAuthorized())
		{
			return base.weaponManager.gpsSystem.currentGroup.currentTarget.worldPosition;
		}
		return base.GetAimPoint();
	}

	private bool IsLaunchAuthorized()
	{
		UpdateHUDMessage();
		if (pathInRange && ml.missileCount > 0 && base.weaponManager.gpsSystem.hasTarget)
		{
			return Vector3.Angle(base.weaponManager.transform.forward, base.weaponManager.gpsSystem.currentGroup.currentTarget.worldPosition - base.weaponManager.transform.position) < offBoresightLaunchAngle;
		}
		return false;
	}

	public override bool LaunchAuthorized()
	{
		UpdateHUDMessage();
		return IsLaunchAuthorized();
	}

	private string CycleTerminalMode()
	{
		switch (currentTMode)
		{
		case AntiShipGuidance.ASMTerminalBehaviors.Direct:
			currentTMode = AntiShipGuidance.ASMTerminalBehaviors.SeaSkim;
			break;
		case AntiShipGuidance.ASMTerminalBehaviors.SeaSkim:
			currentTMode = AntiShipGuidance.ASMTerminalBehaviors.SSEvasive;
			break;
		case AntiShipGuidance.ASMTerminalBehaviors.SSEvasive:
			currentTMode = AntiShipGuidance.ASMTerminalBehaviors.Popup;
			break;
		case AntiShipGuidance.ASMTerminalBehaviors.Popup:
			currentTMode = AntiShipGuidance.ASMTerminalBehaviors.Direct;
			break;
		}
		Missile[] missiles = ml.missiles;
		foreach (Missile missile in missiles)
		{
			if ((bool)missile)
			{
				missile.GetComponent<AntiShipGuidance>().terminalBehavior = currentTMode;
			}
		}
		return tModeLabels[(int)currentTMode];
	}

	public void SetTerminalMode(AntiShipGuidance.ASMTerminalBehaviors mode)
	{
		currentTMode = mode;
		Missile[] missiles = ml.missiles;
		foreach (Missile missile in missiles)
		{
			if ((bool)missile)
			{
				missile.GetComponent<AntiShipGuidance>().terminalBehavior = currentTMode;
			}
		}
	}

	protected override void SaveEquipData(ConfigNode weaponNode)
	{
		base.SaveEquipData(weaponNode);
		weaponNode.SetValue("currentTMode", currentTMode);
	}

	protected override void LoadEquipData(ConfigNode weaponNode)
	{
		base.LoadEquipData(weaponNode);
		ConfigNodeUtils.TryParseValue(weaponNode, "currentTMode", ref currentTMode);
	}

	public override void OnDisableWeapon()
	{
		base.OnDisableWeapon();
		UpdateHUDMessage();
	}

	protected override void OnJettison()
	{
		base.OnJettison();
		UpdateHUDMessage();
	}

	private void UpdatePathInRangeAI()
	{
		pathInRange = false;
		if (!base.itemActivated || !base.weaponManager.currentEquip || !(base.weaponManager.currentEquip == this) || ml.missileCount <= 0)
		{
			return;
		}
		AntiShipGuidance antiShipGuidance = (AntiShipGuidance)ml.GetNextMissile().guidanceUnit;
		if (base.weaponManager.gpsSystem.hasTarget)
		{
			GPSTargetGroup currentGroup = base.weaponManager.gpsSystem.currentGroup;
			if (currentGroup.GetPathLength(currentGroup.currentTargetIdx) + (currentGroup.currentTarget.worldPosition - base.transform.position).magnitude < antiShipGuidance.estimatedRange)
			{
				pathInRange = true;
			}
		}
	}

	private void UpdateHUDMessage()
	{
		if (!base.weaponManager.isPlayer)
		{
			UpdatePathInRangeAI();
			return;
		}
		pathInRange = false;
		if ((bool)base.weaponManager.currentEquip && base.weaponManager.currentEquip == this)
		{
			if (ml.missileCount > 0)
			{
				string text = tModeLabels[(int)currentTMode];
				AntiShipGuidance antiShipGuidance = (AntiShipGuidance)ml.GetNextMissile().guidanceUnit;
				string text2 = s_noTarget;
				if (base.weaponManager.gpsSystem.hasTarget)
				{
					GPSTargetGroup currentGroup = base.weaponManager.gpsSystem.currentGroup;
					if (currentGroup.GetPathLength(currentGroup.currentTargetIdx) + (currentGroup.currentTarget.worldPosition - base.transform.position).magnitude > antiShipGuidance.estimatedRange)
					{
						text2 = s_overRange;
					}
					else
					{
						pathInRange = true;
						text2 = s_inRange;
					}
					string text3 = s_path;
					string text4 = s_point;
					text2 = text2 + "\n" + ((currentGroup.isPath && currentGroup.currentTargetIdx < currentGroup.targets.Count - 1) ? text3 : text4);
				}
				text = text + "\n" + text2;
				if ((bool)base.weaponManager.vm)
				{
					base.weaponManager.vm.hudMessages.SetMessage(hudMsgID, text);
				}
			}
			else
			{
				RemoveHUDMessages();
			}
		}
		else
		{
			RemoveHUDMessages();
		}
	}

	private void RemoveHUDMessages()
	{
		if ((bool)base.weaponManager.vm)
		{
			base.weaponManager.vm.hudMessages.RemoveMessage(hudMsgID);
		}
	}
}
