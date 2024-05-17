using System;
using UnityEngine;

public class HPEquipGPSBombRack : HPEquipMissileLauncher, ICCIPCompatible, IGuidedBombWeapon, ILocalizationUser
{
	public enum TargetModes
	{
		MANUAL,
		AUTO,
		DUMB
	}

	private const string RELEASE_TIME_NAME = "gpsAutoReleaseTime";

	private const string TARGET_SWITCH_TIME_NAME = "gpsTargetSwitchTime";

	private FlightInfo flightInfo;

	public float crossRangeFunctionHeight;

	public float crossRangeFunctionSpeed;

	public float crossRangeMultiplier = 1f;

	public float initialSpeedAdd;

	private FixedPoint _impactPoint;

	private float dragArea;

	private float bombMass = 1f;

	private bool awaitingManualTarget;

	private bool holdingFire;

	private string[] targetModesLabels = new string[3] { "MANUAL", "AUTO", "DUMB" };

	[HideInInspector]
	public TargetModes targetMode;

	private float autoAcquireInterval = 0.2f;

	private int autoReleaseIdx;

	private float[] autoReleaseRates = new float[3] { 120f, 240f, 480f };

	private InternalWeaponBay weaponBay;

	private bool bayFire;

	private bool bayOpen;

	private float lastTimeFired;

	private string s_targetMode;

	private string s_autoRelRate;

	private bool inRange;

	public override void ApplyLocalization()
	{
		base.ApplyLocalization();
		s_targetMode = VTLocalizationManager.GetString("gpsBomb_targetMode", "TARGET MODE", "An option for GPS bombs' targeting mode (auto, manual, dumb)");
		s_autoRelRate = VTLocalizationManager.GetString("gpsBomb_autoRelRate", "AUTO REL RATE", "An option for GPS bomb's automatic release rate (shortened to fit UI space).");
		for (int i = 0; i < targetModesLabels.Length; i++)
		{
			string key = $"s_gbu_mode_{i}";
			targetModesLabels[i] = VTLocalizationManager.GetString(key, targetModesLabels[i], "Targeting mode label for GPS bomb");
		}
	}

	protected override void OnEquip()
	{
		base.OnEquip();
		InternalWeaponBay[] componentsInChildren = base.weaponManager.GetComponentsInChildren<InternalWeaponBay>(includeInactive: true);
		foreach (InternalWeaponBay internalWeaponBay in componentsInChildren)
		{
			if (internalWeaponBay.hardpointIdx == hardpointIdx)
			{
				weaponBay = internalWeaponBay;
				ml.openAndCloseBayOnLaunch = false;
				break;
			}
		}
		if (!base.weaponManager.commonData.ContainsKey("gpsAutoReleaseTime"))
		{
			base.weaponManager.commonData.Add("gpsAutoReleaseTime", -1f);
		}
		if (!base.weaponManager.commonData.ContainsKey("gpsTargetSwitchTime"))
		{
			base.weaponManager.commonData.Add("gpsTargetSwitchTime", -1f);
		}
		flightInfo = GetComponentInParent<FlightInfo>();
		ml.parentActor = base.weaponManager.actor;
		EquipFunction equipFunction = new EquipFunction();
		equipFunction.optionEvent = (EquipFunction.OptionEvent)Delegate.Combine(equipFunction.optionEvent, new EquipFunction.OptionEvent(ToggleTargetMode));
		equipFunction.optionName = s_targetMode;
		equipFunction.optionReturnLabel = targetModesLabels[(int)targetMode];
		EquipFunction equipFunction2 = new EquipFunction();
		equipFunction2.optionEvent = (EquipFunction.OptionEvent)Delegate.Combine(equipFunction2.optionEvent, new EquipFunction.OptionEvent(ToggleAutoRate));
		equipFunction2.optionName = s_autoRelRate;
		equipFunction2.optionReturnLabel = autoReleaseRates[autoReleaseIdx].ToString();
		equipFunctions = new EquipFunction[2] { equipFunction, equipFunction2 };
	}

	private string ToggleAutoRate()
	{
		autoReleaseIdx = (autoReleaseIdx + 1) % autoReleaseRates.Length;
		return autoReleaseRates[autoReleaseIdx].ToString();
	}

	private string ToggleTargetMode()
	{
		if (targetMode == TargetModes.MANUAL)
		{
			targetMode = TargetModes.AUTO;
		}
		else if (targetMode == TargetModes.AUTO)
		{
			targetMode = TargetModes.DUMB;
		}
		else
		{
			targetMode = TargetModes.MANUAL;
		}
		if ((bool)base.weaponManager.currentEquip && base.weaponManager.currentEquip.shortName == shortName && (bool)base.weaponManager.vm && (bool)base.weaponManager.vm.hudWeaponInfo)
		{
			base.weaponManager.vm.hudWeaponInfo.SetWeapon(base.weaponManager.currentEquip);
		}
		return targetModesLabels[(int)targetMode];
	}

	public override void OnEnableWeapon()
	{
		base.OnEnableWeapon();
		Missile nextMissile = ml.GetNextMissile();
		if ((bool)nextMissile)
		{
			SimpleDrag component = nextMissile.GetComponent<SimpleDrag>();
			dragArea = component.area;
			bombMass = nextMissile.mass;
		}
	}

	public override void OnDisableWeapon()
	{
		base.OnDisableWeapon();
		inRange = false;
		awaitingManualTarget = false;
	}

	public override void OnStartFire()
	{
		base.OnStartFire();
		if ((bool)weaponBay && base.weaponManager.isPlayer)
		{
			for (int i = 0; i < base.weaponManager.equipCount; i++)
			{
				HPEquippable equip = base.weaponManager.GetEquip(i);
				if ((bool)equip && equip.shortName == shortName)
				{
					((HPEquipGPSBombRack)equip).holdingFire = true;
				}
			}
		}
		else if (targetMode == TargetModes.DUMB || !base.weaponManager.isPlayer)
		{
			DropBomb();
		}
		else if (targetMode == TargetModes.MANUAL && base.weaponManager.gpsSystem.hasTarget)
		{
			awaitingManualTarget = true;
		}
	}

	public override void OnStopFire()
	{
		base.OnStopFire();
		awaitingManualTarget = false;
		if (!weaponBay || !base.weaponManager.isPlayer)
		{
			return;
		}
		for (int i = 0; i < base.weaponManager.equipCount; i++)
		{
			HPEquippable equip = base.weaponManager.GetEquip(i);
			if ((bool)equip && equip.shortName == shortName)
			{
				((HPEquipGPSBombRack)equip).holdingFire = false;
			}
		}
	}

	public override bool IsPickleToFire()
	{
		if ((bool)weaponBay)
		{
			return base.weaponManager.isPlayer;
		}
		return false;
	}

	public override void OnCycleWeaponButton()
	{
		if ((bool)weaponBay && holdingFire)
		{
			if (bayFire && weaponBay.doorState > 0.99f && (targetMode == TargetModes.DUMB || (targetMode == TargetModes.MANUAL && CheckGPSTargetInRange())))
			{
				DropBomb();
			}
		}
		else
		{
			base.OnCycleWeaponButton();
		}
	}

	private void LateUpdate()
	{
		if ((bool)weaponBay)
		{
			bool flag = (bayFire = ml.missileCount > 0 && holdingFire);
			if (Time.time - lastTimeFired < 1f)
			{
				flag = true;
			}
			if (flag != bayOpen)
			{
				bayOpen = flag;
				if (flag)
				{
					weaponBay.RegisterOpenReq(this);
				}
				else
				{
					weaponBay.UnregisterOpenReq(this);
				}
			}
		}
		if (!base.itemActivated)
		{
			return;
		}
		if (targetMode == TargetModes.AUTO || targetMode == TargetModes.MANUAL)
		{
			inRange = CheckGPSTargetInRange();
		}
		if (targetMode == TargetModes.AUTO)
		{
			if (base.weaponManager.isFiring && (!weaponBay || weaponBay.doorState > 0.99f) && Time.time - (float)base.weaponManager.commonData["gpsAutoReleaseTime"] > 60f / autoReleaseRates[autoReleaseIdx] && Time.time - (float)base.weaponManager.commonData["gpsTargetSwitchTime"] > autoAcquireInterval)
			{
				if (inRange)
				{
					DropBomb();
					base.weaponManager.commonData["gpsAutoReleaseTime"] = Time.time;
				}
				base.weaponManager.gpsSystem.NextTarget();
				base.weaponManager.commonData["gpsTargetSwitchTime"] = Time.time;
			}
		}
		else if (base.weaponManager.isPlayer)
		{
			inRange = false;
			if (awaitingManualTarget && CheckGPSTargetInRange())
			{
				DropBomb();
				awaitingManualTarget = false;
			}
		}
	}

	public override bool LaunchAuthorized()
	{
		return inRange;
	}

	private bool CheckGPSTargetInRange()
	{
		if (!base.weaponManager.gpsSystem.hasTarget)
		{
			return false;
		}
		Vector3 worldPosition = base.weaponManager.gpsSystem.currentGroup.currentTarget.worldPosition;
		float deployRadius = GetDeployRadius(worldPosition);
		float num = deployRadius * deployRadius;
		if ((_impactPoint.point - worldPosition).sqrMagnitude < num)
		{
			return true;
		}
		return false;
	}

	public bool GPSTargetInOptimalRange()
	{
		if (!inRange || !base.weaponManager.gpsSystem.hasTarget)
		{
			return false;
		}
		Vector3 worldPosition = base.weaponManager.gpsSystem.currentGroup.currentTarget.worldPosition;
		float num = GetDeployRadius(worldPosition) * 0.75f;
		float num2 = num * num;
		if ((_impactPoint.point - worldPosition).sqrMagnitude < num2)
		{
			return true;
		}
		return false;
	}

	public float GetDeployRadius(Vector3 targetPosition)
	{
		float num = flightInfo.airspeed + initialSpeedAdd;
		float num2 = base.transform.position.y - targetPosition.y;
		return (crossRangeFunctionHeight * num2 + crossRangeFunctionSpeed * num) * crossRangeMultiplier;
	}

	private void DropBomb()
	{
		if (ml.missileCount > 0)
		{
			if (targetMode == TargetModes.DUMB)
			{
				ml.GetNextMissile().guidanceMode = Missile.GuidanceModes.Bomb;
			}
			else
			{
				if (!base.weaponManager.gpsSystem.hasTarget)
				{
					Debug.Log("GPSBombRack no GPSTarget");
					return;
				}
				ml.GetNextMissile().SetGPSTarget(base.weaponManager.gpsSystem.currentGroup.currentTarget);
			}
			ml.FireMissile();
			lastTimeFired = Time.time;
		}
		base.weaponManager.ToggleCombinedWeapon();
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
		return _impactPoint.point;
	}

	public Vector3 GetImpactPoint()
	{
		bool hasTarget = base.weaponManager.gpsSystem.hasTarget;
		Vector3 targeterPosition = (hasTarget ? base.weaponManager.gpsSystem.currentGroup.currentTarget.worldPosition : Vector3.zero);
		Vector3 position = base.weaponManager.transform.position;
		Vector3 velocity = base.weaponManager.vesselRB.velocity;
		Missile nextMissile = ml.GetNextMissile();
		if ((bool)nextMissile)
		{
			velocity += nextMissile.decoupleSpeed * nextMissile.decoupleDirection;
			velocity += initialSpeedAdd * nextMissile.transform.forward;
		}
		_impactPoint.point = HPEquipBombRack.GetBombImpactPoint(out var _, ml, 0.2f, 60f, dragArea, bombMass, hasTarget, targeterPosition, position, velocity);
		return _impactPoint.point;
	}

	protected override void SaveEquipData(ConfigNode weaponNode)
	{
		base.SaveEquipData(weaponNode);
		weaponNode.SetValue("autoReleaseIdx", autoReleaseIdx);
		weaponNode.SetValue("targetMode", targetMode);
	}

	protected override void LoadEquipData(ConfigNode weaponNode)
	{
		base.LoadEquipData(weaponNode);
		ConfigNodeUtils.TryParseValue(weaponNode, "autoReleaseIdx", ref autoReleaseIdx);
		ConfigNodeUtils.TryParseValue(weaponNode, "targetMode", ref targetMode);
	}

	public bool HasGuidedBombTarget()
	{
		return base.weaponManager.gpsSystem.hasTarget;
	}

	public bool IsDumbMode()
	{
		return targetMode == TargetModes.DUMB;
	}
}
