using System;
using System.Collections.Generic;
using UnityEngine;

public class HPEquipBombRack : HPEquipMissileLauncher, ICCIPCompatible, IRippleWeapon, IRequiresOpticalTargeter, ILocalizationUser
{
	public bool enableSalvoOption = true;

	protected Vector3 _impactPoint;

	protected float _leadTime;

	protected OpticalTargeter targeter;

	protected float dragArea;

	protected float bombMass = 1f;

	private bool holdingFire;

	private bool autoReleaseCCRP;

	private bool autoReleased;

	private float[] rippleRates = new float[4] { 0f, 120f, 240f, 480f };

	private int rippleIdx;

	private int rAreaIdx;

	private float[] releaseAreas = new float[4] { 25f, 50f, 100f, 200f };

	public float ai_maxBombAltitude = -1f;

	private InternalWeaponBay weaponBay;

	private string s_ON;

	private string s_OFF;

	private string s_RELEASE_AREA;

	private string s_SALVO;

	private string s_lowG;

	private string s_ccrpAuto;

	private float lastTimeFired;

	private bool bayOpen;

	private bool bayFire;

	private bool isNegG;

	private Vector3 lastLocalAimPos = Vector3.forward;

	private int salvoCount = 1;

	private int maxSalvo = 4;

	private List<HPEquipBombRack> salvoLaunchers = new List<HPEquipBombRack>();

	public float TimeToImpact()
	{
		return _leadTime;
	}

	public override void ApplyLocalization()
	{
		base.ApplyLocalization();
		s_ON = VTLocalizationManager.GetString("ON");
		s_OFF = VTLocalizationManager.GetString("OFF");
		s_RELEASE_AREA = VTLocalizationManager.GetString("bombRack_releaseArea", "RELEASE AREA", "An option for bomb weapons - how wide of a target area to automatically release bombs in.");
		s_SALVO = VTLocalizationManager.GetString("SALVO");
		s_lowG = VTLocalizationManager.GetString("bomb_lowG", "LOW G\nNO RELEASE", "HUD label shown when aircraft is under low or negative g-forces, preventing bomb release.");
		s_ccrpAuto = VTLocalizationManager.GetString("s_ccrpAuto", "CCRP AUTO");
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
		ml.parentActor = base.weaponManager.actor;
		List<EquipFunction> list = new List<EquipFunction>();
		EquipFunction equipFunction = new EquipFunction();
		equipFunction.optionEvent = (EquipFunction.OptionEvent)Delegate.Combine(equipFunction.optionEvent, new EquipFunction.OptionEvent(ToggleAutoRelease));
		equipFunction.optionName = s_ccrpAuto;
		equipFunction.optionReturnLabel = (autoReleaseCCRP ? s_ON : s_OFF);
		list.Add(equipFunction);
		EquipFunction equipFunction2 = new EquipFunction();
		equipFunction2.optionEvent = (EquipFunction.OptionEvent)Delegate.Combine(equipFunction2.optionEvent, new EquipFunction.OptionEvent(ToggleReleaseArea));
		equipFunction2.optionName = s_RELEASE_AREA;
		equipFunction2.optionReturnLabel = releaseAreas[rAreaIdx].ToString();
		list.Add(equipFunction2);
		if (enableSalvoOption)
		{
			EquipFunction equipFunction3 = new EquipFunction();
			equipFunction3.optionEvent = (EquipFunction.OptionEvent)Delegate.Combine(equipFunction3.optionEvent, new EquipFunction.OptionEvent(ToggleSalvo));
			equipFunction3.optionName = s_SALVO;
			equipFunction3.optionReturnLabel = salvoCount.ToString();
			list.Add(equipFunction3);
		}
		equipFunctions = list.ToArray();
	}

	public void SetOpticalTargeter(OpticalTargeter t)
	{
		targeter = t;
	}

	private string ToggleReleaseArea()
	{
		rAreaIdx = (rAreaIdx + 1) % releaseAreas.Length;
		return releaseAreas[rAreaIdx].ToString();
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

	public override bool IsPickleToFire()
	{
		if ((bool)weaponBay)
		{
			return base.weaponManager.isPlayer;
		}
		return false;
	}

	public override void OnStartFire()
	{
		base.OnStartFire();
		holdingFire = true;
		if ((bool)weaponBay && base.weaponManager.isPlayer)
		{
			for (int i = 0; i < base.weaponManager.equipCount; i++)
			{
				HPEquippable equip = base.weaponManager.GetEquip(i);
				if ((bool)equip && equip.shortName == shortName)
				{
					((HPEquipBombRack)equip).holdingFire = true;
				}
			}
		}
		autoReleased = false;
		if ((!weaponBay || !base.weaponManager.isPlayer) && (!autoReleaseCCRP || !base.weaponManager.opticalTargeter || !base.weaponManager.opticalTargeter.locked || base.weaponManager.opticalTargeter.lockedSky))
		{
			if (salvoCount == 1)
			{
				DropBomb();
			}
			else
			{
				FireSalvo();
			}
		}
	}

	public override void OnCycleWeaponButton()
	{
		if (bayFire && (bool)weaponBay)
		{
			if (weaponBay.doorState > 0.99f)
			{
				if (salvoCount == 1)
				{
					DropBomb();
				}
				else
				{
					FireSalvo();
				}
			}
		}
		else
		{
			base.OnCycleWeaponButton();
		}
	}

	public override void OnDisableWeapon()
	{
		base.OnDisableWeapon();
		if ((bool)base.weaponManager.vm && (bool)base.weaponManager.vm.hudMessages)
		{
			base.weaponManager.vm.hudMessages.RemoveMessage(shortName);
		}
	}

	protected override void OnJettison()
	{
		base.OnJettison();
		if ((bool)base.weaponManager.vm && (bool)base.weaponManager.vm.hudMessages)
		{
			base.weaponManager.vm.hudMessages.RemoveMessage(shortName);
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
		if (holdingFire)
		{
			Missile nextMissile = ml.GetNextMissile();
			if (autoReleaseCCRP && !autoReleased && (bool)nextMissile && (bool)base.weaponManager.opticalTargeter && base.weaponManager.opticalTargeter.locked && !base.weaponManager.opticalTargeter.lockedSky)
			{
				Vector3 vector = base.weaponManager.opticalTargeter.lockTransform.position + base.weaponManager.opticalTargeter.targetVelocity * _leadTime - (_impactPoint + base.weaponManager.actor.velocity * Time.deltaTime);
				if (vector.sqrMagnitude < Mathf.Pow((rippleIdx == 0) ? (nextMissile.explodeRadius * 0.5f) : releaseAreas[rAreaIdx], 2f) && (rippleIdx > 0 || Vector3.Dot(vector, base.transform.forward) < 0f || Vector3.Dot(vector + base.weaponManager.actor.velocity * Time.deltaTime, base.transform.forward) < 0f) && (!weaponBay || weaponBay.doorState > 0.99f))
				{
					if (salvoCount == 1)
					{
						DropBomb();
					}
					else
					{
						FireSalvo();
					}
					autoReleased = true;
				}
			}
		}
		UpdateNegGLabel();
	}

	private void UpdateNegGLabel()
	{
		if (!base.weaponManager || !base.weaponManager.isPlayer || !base.weaponManager.vm || !base.weaponManager.vm.hudMessages)
		{
			return;
		}
		HUDMessages hudMessages = base.weaponManager.vm.hudMessages;
		if ((bool)base.weaponManager.currentEquip && base.weaponManager.currentEquip.shortName == shortName && base.weaponManager.actor.flightInfo.playerGs < 0.15f)
		{
			if (!isNegG)
			{
				hudMessages.SetMessage(shortName, s_lowG);
				isNegG = true;
			}
		}
		else if (isNegG)
		{
			hudMessages.RemoveMessage(shortName);
			isNegG = false;
		}
	}

	private string ToggleAutoRelease()
	{
		autoReleaseCCRP = !autoReleaseCCRP;
		if (!autoReleaseCCRP)
		{
			return s_OFF;
		}
		return s_ON;
	}

	private bool DropBomb()
	{
		if (isNegG)
		{
			return false;
		}
		bool result = false;
		if (ml.missileCount > 0)
		{
			ml.FireMissile();
			lastTimeFired = Time.time;
			result = true;
		}
		base.weaponManager.ToggleCombinedWeapon();
		return result;
	}

	public override void OnStopFire()
	{
		base.OnStopFire();
		holdingFire = false;
		if (!weaponBay || !base.weaponManager.isPlayer)
		{
			return;
		}
		for (int i = 0; i < base.weaponManager.equipCount; i++)
		{
			HPEquippable equip = base.weaponManager.GetEquip(i);
			if ((bool)equip && equip.shortName == shortName)
			{
				((HPEquipBombRack)equip).holdingFire = false;
			}
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

	public override Vector3 GetAimPoint()
	{
		if (GetCount() > 0)
		{
			lastLocalAimPos = base.transform.InverseTransformPoint(_impactPoint);
			return _impactPoint;
		}
		return base.transform.TransformPoint(lastLocalAimPos);
	}

	public static Vector3 GetBombImpactPoint(out float time, MissileLauncher ml, float simDeltaTime, float timeLimit, float dragArea, float bombMass, bool hasTargeter, Vector3 targeterPosition, Vector3 initialPos, Vector3 initialVel, bool checkIwb = true)
	{
		time = -1f;
		if (!ml.parentRb || (hasTargeter && initialVel.y < 0f && initialPos.y < targeterPosition.y))
		{
			return Vector3.zero;
		}
		float num = 0f;
		Vector3 vector = initialPos;
		Vector3 vector2 = initialVel;
		if ((bool)ml.iwb)
		{
			vector += vector2 * ml.iwb.estTimeToOpen;
		}
		float num2 = -0.5f * dragArea * simDeltaTime / bombMass;
		Ray ray = default(Ray);
		for (; num < timeLimit; num += simDeltaTime)
		{
			Vector3 vector3 = vector;
			vector2 += Physics.gravity * simDeltaTime;
			vector2 += num2 * AerodynamicsController.fetch.AtmosDensityAtPosition(vector3) * vector2.magnitude * vector2;
			vector3 += vector2 * simDeltaTime;
			float enter;
			if (hasTargeter)
			{
				if (vector3.y < targeterPosition.y && vector2.y < 0f)
				{
					ray.origin = vector;
					ray.direction = vector2;
					if (new Plane(Vector3.up, targeterPosition).Raycast(ray, out enter))
					{
						vector = ray.GetPoint(enter);
						break;
					}
				}
			}
			else
			{
				if (vector3.y < WaterPhysics.instance.height)
				{
					ray.origin = vector;
					ray.direction = vector2;
					vector = ((!WaterPhysics.instance.waterPlane.Raycast(ray, out enter)) ? vector3 : ray.GetPoint(enter));
					break;
				}
				if (Physics.Linecast(vector, vector3, out var hitInfo, 1, QueryTriggerInteraction.Ignore))
				{
					vector = hitInfo.point;
					break;
				}
			}
			vector = vector3;
		}
		time = num;
		return vector;
	}

	public virtual Vector3 GetImpactPointWithLead(out float t)
	{
		bool flag = (bool)targeter && targeter.locked && !targeter.lockedSky;
		float timeLimit = (flag ? 120 : 30);
		float simDeltaTime = (flag ? 0.1f : 0.2f);
		Vector3 targeterPosition = (flag ? targeter.lockTransform.position : Vector3.zero);
		Vector3 position = base.weaponManager.transform.position;
		Vector3 velocity = base.weaponManager.vesselRB.velocity;
		Missile nextMissile = ml.GetNextMissile();
		if ((bool)nextMissile)
		{
			velocity += nextMissile.decoupleSpeed * nextMissile.decoupleDirection;
		}
		_impactPoint = GetBombImpactPoint(out t, ml, simDeltaTime, timeLimit, dragArea, bombMass, flag, targeterPosition, position, velocity);
		_leadTime = t;
		return _impactPoint;
	}

	public Vector3 GetImpactPointWithLead(out float t, bool checkIwb)
	{
		bool flag = (bool)targeter && targeter.locked && !targeter.lockedSky;
		float timeLimit = (flag ? 120 : 30);
		float simDeltaTime = (flag ? 0.1f : 0.2f);
		Vector3 targeterPosition = (flag ? targeter.lockTransform.position : Vector3.zero);
		Vector3 position = base.weaponManager.transform.position;
		Vector3 velocity = base.weaponManager.vesselRB.velocity;
		Missile nextMissile = ml.GetNextMissile();
		if ((bool)nextMissile)
		{
			velocity += nextMissile.decoupleSpeed * nextMissile.decoupleDirection;
		}
		_impactPoint = GetBombImpactPoint(out t, ml, simDeltaTime, timeLimit, dragArea, bombMass, flag, targeterPosition, position, velocity, checkIwb);
		_leadTime = t;
		return _impactPoint;
	}

	public Vector3 GetImpactPoint()
	{
		_impactPoint = GetImpactPointWithLead(out _leadTime);
		return _impactPoint;
	}

	public float[] GetRippleRates()
	{
		return rippleRates;
	}

	public void SetRippleRateIdx(int idx)
	{
		rippleIdx = idx;
	}

	public int GetRippleRateIdx()
	{
		return rippleIdx;
	}

	protected override void LoadEquipData(ConfigNode weaponNode)
	{
		base.LoadEquipData(weaponNode);
		ConfigNodeUtils.TryParseValue(weaponNode, "rAreaIdx", ref rAreaIdx);
		ConfigNodeUtils.TryParseValue(weaponNode, "rippleIdx", ref rippleIdx);
		ConfigNodeUtils.TryParseValue(weaponNode, "autoReleaseCCRP", ref autoReleaseCCRP);
	}

	protected override void SaveEquipData(ConfigNode weaponNode)
	{
		base.SaveEquipData(weaponNode);
		weaponNode.SetValue("rAreaIdx", rAreaIdx);
		weaponNode.SetValue("rippleIdx", rippleIdx);
		weaponNode.SetValue("autoReleaseCCRP", autoReleaseCCRP);
	}

	private string ToggleSalvo()
	{
		maxSalvo = 0;
		for (int i = 0; i < base.weaponManager.equipCount; i++)
		{
			HPEquippable equip = base.weaponManager.GetEquip(i);
			if (equip is HPEquipBombRack && equip.shortName == shortName)
			{
				maxSalvo++;
			}
		}
		if (maxSalvo == 0)
		{
			maxSalvo = 1;
		}
		salvoCount++;
		if (salvoCount > maxSalvo)
		{
			salvoCount = 1;
		}
		return salvoCount.ToString();
	}

	private void FireSalvo()
	{
		salvoLaunchers.Clear();
		for (int i = 0; i < base.weaponManager.equipCount; i++)
		{
			HPEquippable equip = base.weaponManager.GetEquip(i);
			if (equip is HPEquipBombRack && equip.GetCount() > 0 && equip.shortName == shortName)
			{
				salvoLaunchers.Add((HPEquipBombRack)equip);
			}
		}
		int num = 0;
		bool flag = false;
		while (num < salvoCount && salvoLaunchers.Count > 0)
		{
			int index = (flag ? (salvoLaunchers.Count - 1) : 0);
			bool flag2 = false;
			while (!flag2 && salvoLaunchers.Count > 0)
			{
				if (salvoLaunchers[index].DropBomb())
				{
					flag2 = true;
					num++;
				}
				salvoLaunchers.RemoveAt(index);
				if (flag)
				{
					index = salvoLaunchers.Count - 1;
				}
			}
			flag = !flag;
		}
	}
}
