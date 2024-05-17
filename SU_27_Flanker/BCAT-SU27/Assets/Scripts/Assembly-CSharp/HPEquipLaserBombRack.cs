using System;
using UnityEngine;

public class HPEquipLaserBombRack : HPEquipMissileLauncher, ICCIPCompatible, IGuidedBombWeapon, ILocalizationUser
{
	public enum TargetModes
	{
		LASER,
		DUMB
	}

	private FlightInfo flightInfo;

	public float crossRangeFunctionHeight;

	public float crossRangeFunctionSpeed;

	public float crossRangeMultiplier = 1f;

	private FixedPoint _impactPoint;

	private float dragArea;

	private float bombMass = 1f;

	private bool awaitingManualTarget;

	private InternalWeaponBay weaponBay;

	private bool bayFire;

	private bool bayOpen;

	private bool holdingFire;

	private float lastTimeFired;

	private bool willLaunchLOAL;

	private string[] targetModeLabels = new string[2] { "LASER", "DUMB" };

	[HideInInspector]
	public TargetModes targetMode;

	private string s_targetMode;

	private string s_LOAL = "LOAL";

	private bool launchAuthorized;

	private const string LOAL_MSG_ID = "laserBombLOAL";

	private bool hasSetLoalMsg;

	public override void ApplyLocalization()
	{
		base.ApplyLocalization();
		s_targetMode = VTLocalizationManager.GetString("lgb_targetMode", "TARGET MODE", "Option title for laser guided bombs' targeting mode.");
		s_LOAL = VTLocalizationManager.GetString("s_LOAL", "LOAL", "Lock on after launch HUD label for laser bomb");
		for (int i = 0; i < targetModeLabels.Length; i++)
		{
			string key = $"s_lgbMode_{i}";
			targetModeLabels[i] = VTLocalizationManager.GetString(key, targetModeLabels[i], "Laser guided bomb targeting mode");
		}
	}

	protected override void Awake()
	{
		base.Awake();
	}

	protected override void OnEquip()
	{
		base.OnEquip();
		flightInfo = GetComponentInParent<FlightInfo>();
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
		ml.parentActor = base.weaponManager.actor;
		EquipFunction equipFunction = new EquipFunction();
		equipFunction.optionEvent = (EquipFunction.OptionEvent)Delegate.Combine(equipFunction.optionEvent, new EquipFunction.OptionEvent(ToggleTargetMode));
		equipFunction.optionName = s_targetMode;
		equipFunction.optionReturnLabel = targetModeLabels[(int)targetMode];
		equipFunctions = new EquipFunction[1] { equipFunction };
	}

	private string ToggleTargetMode()
	{
		if (targetMode == TargetModes.DUMB)
		{
			targetMode = TargetModes.LASER;
		}
		else if (targetMode == TargetModes.LASER)
		{
			targetMode = TargetModes.DUMB;
		}
		if (base.itemActivated)
		{
			base.weaponManager.RefreshWeapon();
		}
		return targetModeLabels[(int)targetMode];
	}

	public override void OnEnableWeapon()
	{
		base.OnEnableWeapon();
		launchAuthorized = false;
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
		awaitingManualTarget = false;
		launchAuthorized = false;
		HideLOALMsg();
	}

	protected override void OnJettison()
	{
		base.OnJettison();
		HideLOALMsg();
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
					((HPEquipLaserBombRack)equip).holdingFire = true;
				}
			}
		}
		else if (targetMode == TargetModes.DUMB)
		{
			DropBomb();
		}
		else if (targetMode == TargetModes.LASER && (bool)base.weaponManager.opticalTargeter && base.weaponManager.opticalTargeter.locked)
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
				((HPEquipLaserBombRack)equip).holdingFire = false;
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
			if (bayFire)
			{
				if (weaponBay.doorState > 0.99f && targetMode == TargetModes.DUMB)
				{
					DropBomb();
				}
				if (targetMode == TargetModes.LASER && (bool)base.weaponManager.opticalTargeter && base.weaponManager.opticalTargeter.locked)
				{
					awaitingManualTarget = true;
				}
			}
		}
		else
		{
			base.OnCycleWeaponButton();
		}
	}

	public override void OnReleasedCycleWeaponButton()
	{
		base.OnReleasedCycleWeaponButton();
		if ((bool)weaponBay)
		{
			awaitingManualTarget = false;
		}
	}

	private void ShowLOALMsg()
	{
		if ((bool)base.weaponManager.vm && (bool)base.weaponManager.vm.hudMessages && !hasSetLoalMsg)
		{
			hasSetLoalMsg = true;
			base.weaponManager.vm.hudMessages.SetMessage("laserBombLOAL", s_LOAL);
		}
	}

	private void HideLOALMsg()
	{
		if ((bool)base.weaponManager.vm && (bool)base.weaponManager.vm.hudMessages && hasSetLoalMsg)
		{
			base.weaponManager.vm.hudMessages.RemoveMessage("laserBombLOAL");
			hasSetLoalMsg = false;
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
		if (base.itemActivated)
		{
			if (targetMode == TargetModes.LASER)
			{
				willLaunchLOAL = false;
				launchAuthorized = CheckLaunchAuthorized(requireLaserLOS: true);
				if (willLaunchLOAL)
				{
					ShowLOALMsg();
				}
				else
				{
					HideLOALMsg();
				}
			}
			else
			{
				HideLOALMsg();
				launchAuthorized = CheckLaunchAuthorized(requireLaserLOS: false);
			}
			if (awaitingManualTarget && launchAuthorized && (!weaponBay || weaponBay.doorState > 0.99f))
			{
				DropBomb();
				awaitingManualTarget = false;
			}
		}
		else if ((bool)base.weaponManager)
		{
			HideLOALMsg();
		}
	}

	public override bool LaunchAuthorized()
	{
		return launchAuthorized;
	}

	private bool CheckLaunchAuthorized(bool requireLaserLOS)
	{
		if (ml.missileCount == 0)
		{
			return false;
		}
		if (!requireLaserLOS)
		{
			return true;
		}
		if (!base.weaponManager.opticalTargeter || !base.weaponManager.opticalTargeter.locked)
		{
			return false;
		}
		Missile nextMissile = ml.GetNextMissile();
		Vector3 vector = (nextMissile.opticalLOAL ? base.weaponManager.opticalTargeter.lockTransform.position : base.weaponManager.opticalTargeter.laserPoint.point);
		if (!(Vector3.Angle(nextMissile.transform.forward, vector - nextMissile.transform.position) < nextMissile.opticalFOV / 2f))
		{
			if (!nextMissile.opticalLOAL)
			{
				return false;
			}
			willLaunchLOAL = true;
		}
		float deployRadius = GetDeployRadius(vector);
		float num = deployRadius * deployRadius;
		if ((_impactPoint.point - vector).sqrMagnitude < num)
		{
			return true;
		}
		willLaunchLOAL = false;
		return false;
	}

	public float GetDeployRadius(Vector3 targetPosition)
	{
		float airspeed = flightInfo.airspeed;
		float num = base.transform.position.y - targetPosition.y;
		return (crossRangeFunctionHeight * num + crossRangeFunctionSpeed * airspeed) * crossRangeMultiplier;
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
				if (!HasGuidedBombTarget())
				{
					return;
				}
				ml.GetNextMissile().SetOpticalTarget(base.weaponManager.opticalTargeter.lockTransform, base.weaponManager.opticalTargeter.lockedActor, base.weaponManager.opticalTargeter);
				if (ml.GetNextMissile().opticalLOAL)
				{
					ml.GetNextMissile().SetLOALInitialTarget(base.weaponManager.opticalTargeter.lockTransform.position);
				}
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
		bool flag = HasGuidedBombTarget();
		Vector3 targeterPosition = (flag ? base.weaponManager.opticalTargeter.lockTransform.position : Vector3.zero);
		Vector3 position = base.weaponManager.transform.position;
		Vector3 velocity = base.weaponManager.vesselRB.velocity;
		Missile nextMissile = ml.GetNextMissile();
		if ((bool)nextMissile)
		{
			velocity += nextMissile.decoupleSpeed * nextMissile.decoupleDirection;
		}
		_impactPoint.point = HPEquipBombRack.GetBombImpactPoint(out var _, ml, 0.2f, 60f, dragArea, bombMass, flag, targeterPosition, position, velocity);
		return _impactPoint.point;
	}

	protected override void SaveEquipData(ConfigNode weaponNode)
	{
		base.SaveEquipData(weaponNode);
		weaponNode.SetValue("targetMode", targetMode);
	}

	protected override void LoadEquipData(ConfigNode weaponNode)
	{
		base.LoadEquipData(weaponNode);
		ConfigNodeUtils.TryParseValue(weaponNode, "targetMode", ref targetMode);
	}

	public bool HasGuidedBombTarget()
	{
		OpticalTargeter opticalTargeter = base.weaponManager.opticalTargeter;
		if (ml.missileCount > 0 && (bool)opticalTargeter && opticalTargeter.locked)
		{
			return !opticalTargeter.lockedSky;
		}
		return false;
	}

	public bool IsDumbMode()
	{
		return targetMode == TargetModes.DUMB;
	}
}
