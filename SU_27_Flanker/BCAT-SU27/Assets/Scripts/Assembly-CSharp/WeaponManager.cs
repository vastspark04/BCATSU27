using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class WeaponManager : MonoBehaviour, IQSVehicleComponent
{
	public delegate void RippleChangedDelegate(int equipIdx, int rippleRateIdx);

	public delegate void EquipStateChangedDelegate(HPEquippable eq, bool state);

	public delegate void EqFunctionCalledDelegate(int buttonIdx, int weaponIdx);

	public class AvailableWeaponTypes
	{
		public bool gun;

		public bool antiShip;

		public bool aam;

		public bool agm;

		public bool rocket;

		public bool bomb;

		public bool antirad;

		public bool agmCruise;

		public void Reset()
		{
			gun = false;
			antiShip = false;
			aam = false;
			agm = false;
			rocket = false;
			bomb = false;
			antirad = false;
			agmCruise = false;
		}

		public bool HasAny()
		{
			if (!gun && !antiShip && !aam && !agm && !rocket && !bomb && !antirad)
			{
				return agmCruise;
			}
			return true;
		}

		public override string ToString()
		{
			return $"gun = {gun}, antiShip = {antiShip}, aam = {aam}, agm = {agm}, rocket = {rocket}, bomb = {bomb}, antirad = {antirad}, agmCruise = {agmCruise}";
		}
	}

	private Actor _a;

	private bool gotA;

	public VehicleMaster vm;

	[HideInInspector]
	public WeaponManagerUI ui;

	public Battery battery;

	public float powerDrain = 1f;

	public MeshRenderer liverySample;

	private bool powered;

	public Transform sensorAudioTransform;

	public Transform irForwardOverrideTransform;

	public bool markJettisonDisarms = true;

	public string resourcePath = "HPEquips/VTOL";

	public Transform[] hardpointTransforms;

	public int[] symmetryIndices;

	[HideInInspector]
	public UnityEvent OnWeaponChanged;

	public UnityEvent OnUserCycledWeapon;

	public AudioClip jettisonAudioClip;

	private bool gotRb;

	private Rigidbody _rb;

	public Dictionary<string, object> commonData = new Dictionary<string, object>();

	public LockingRadar lockingRadar;

	public OpticalTargeter opticalTargeter;

	public MFDAntiRadarAttackDisplay arad;

	public TacticalSituationController tsc;

	private Missile lfm;

	public AvailableWeaponTypes availableWeaponTypes = new AvailableWeaponTypes();

	private bool _masterArmed;

	private int weaponIdx;

	private HPEquippable[] equips;

	private string activeWeaponName = "NONE";

	private List<string> uniqueWeapons = new List<string>();

	private List<HPEquippable> combinedEquips = new List<HPEquippable>();

	private int combinedWeaponIdx;

	private bool _singleFire;

	private bool _singleFired;

	private bool rippleWeapon;

	private float[] rippleRates;

	private int rippleRateIdx;

	private float lastTimeFired;

	[HideInInspector]
	public DynamicLaunchZone dlz;

	[HideInInspector]
	public InternalWeaponBay[] internalWeaponBays;

	private HPEquippable partDestroyedEquip;

	private bool rcsAddDirty = true;

	private float rcsAdd;

	public Actor actor
	{
		get
		{
			if (!_a && !gotA)
			{
				gotA = true;
				if ((bool)this)
				{
					_a = GetComponent<Actor>();
				}
				else
				{
					_a = null;
				}
			}
			return _a;
		}
	}

	public WeaponManagerSync remoteSync { get; private set; }

	public Rigidbody vesselRB
	{
		get
		{
			if (!gotRb)
			{
				_rb = base.gameObject.GetComponent<Rigidbody>();
				gotRb = true;
			}
			return _rb;
		}
	}

	public GPSTargetSystem gpsSystem { get; } = new GPSTargetSystem();


	public bool isPlayer => actor == FlightSceneManager.instance.playerActor;

	public Missile lastFiredMissile
	{
		get
		{
			return lfm;
		}
		set
		{
			if (lfm != value)
			{
				lfm = value;
				this.OnFiredMissile?.Invoke(lfm);
			}
		}
	}

	public float maxAntiAirRange { get; private set; }

	public float maxAntiRadRange { get; private set; }

	public float maxAGMRange { get; private set; }

	public bool isMasterArmed => _masterArmed;

	public bool noArms { get; private set; }

	public int equipCount
	{
		get
		{
			if (equips == null)
			{
				return 0;
			}
			return equips.Length;
		}
	}

	public HPEquippable currentEquip
	{
		get
		{
			if (noArms)
			{
				return null;
			}
			if (combinedEquips.Count > 0 && combinedWeaponIdx >= 0 && combinedWeaponIdx < combinedEquips.Count)
			{
				return combinedEquips[combinedWeaponIdx];
			}
			return null;
		}
	}

	public int combinedCount
	{
		get
		{
			int num = 0;
			foreach (HPEquippable combinedEquip in combinedEquips)
			{
				num += combinedEquip.GetCount();
			}
			return num;
		}
	}

	public bool isFiring { get; private set; }

	public bool equippedGun { get; private set; }

	public bool isUserTriggerHeld { get; private set; }

	public event UnityAction<HPEquippable> OnWeaponEquipped;

	public event UnityAction<int> OnWeaponEquippedHPIdx;

	public event UnityAction<int> OnWeaponUnequippedHPIdx;

	public event Action<Missile> OnFiredMissile;

	public event Action OnStartFire;

	public event Action OnEndFire;

	public event RippleChangedDelegate OnRippleChanged;

	public event EquipStateChangedDelegate OnEquipArmingChanged;

	public event EquipStateChangedDelegate OnEquipJettisonChanged;

	public event EqFunctionCalledDelegate OnWeaponFunctionCalled;

	public void InvokeUnequipEvent(int idx)
	{
		this.OnWeaponUnequippedHPIdx?.Invoke(idx);
	}

	public void SetRemoteWmSync(WeaponManagerSync s)
	{
		remoteSync = s;
		remoteSync.muvs.OnSetWeaponControllerId += Muvs_OnSetWeaponControllerId;
	}

	private void Muvs_OnSetWeaponControllerId(ulong id)
	{
		if (id != BDSteamClient.mySteamID && isUserTriggerHeld)
		{
			EndFire();
		}
	}

	public void SetLockingRadar(LockingRadar lr)
	{
		lockingRadar = lr;
		HPEquippable[] array = equips;
		foreach (HPEquippable hPEquippable in array)
		{
			if (!hPEquippable)
			{
				continue;
			}
			Component[] componentsInChildren = hPEquippable.gameObject.GetComponentsInChildren<Component>();
			foreach (Component component in componentsInChildren)
			{
				if (component is IRequiresLockingRadar)
				{
					((IRequiresLockingRadar)component).SetLockingRadar(lockingRadar);
				}
			}
		}
	}

	public void SetOpticalTargeter(OpticalTargeter t)
	{
		opticalTargeter = t;
		IRequiresOpticalTargeter[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IRequiresOpticalTargeter>(includeInactive: true);
		for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
		{
			componentsInChildrenImplementing[i].SetOpticalTargeter(opticalTargeter);
		}
	}

	public void ToggleMasterArmed()
	{
		_masterArmed = !_masterArmed;
		if (isMasterArmed)
		{
			RefreshWeapon();
		}
		else
		{
			EndAllTriggerAxis();
			if (isFiring)
			{
				EndAllFire();
			}
			HPEquippable hPEquippable = currentEquip;
			noArms = true;
			if ((bool)hPEquippable)
			{
				hPEquippable.OnDisableWeapon();
			}
		}
		if (OnWeaponChanged != null)
		{
			OnWeaponChanged.Invoke();
		}
	}

	public void SetMasterArmed(int idx)
	{
		SetMasterArmed(idx > 0);
	}

	public void SetMasterArmed(bool armed)
	{
		if ((!armed || !battery || battery.Drain(0.1f * Time.deltaTime)) && _masterArmed != armed)
		{
			ToggleMasterArmed();
		}
	}

	public HPEquippable GetEquip(int idx)
	{
		if (equips != null && idx >= 0 && idx < equips.Length)
		{
			return equips[idx];
		}
		return null;
	}

	public List<HPEquippable> GetCombinedEquips()
	{
		return combinedEquips;
	}

	public void ToggleCombinedWeapon()
	{
		if (noArms)
		{
			return;
		}
		int index = combinedWeaponIdx;
		float num = -1f;
		int num2 = -1;
		float num3 = -1f;
		int num4 = -1;
		float num5 = Mathf.Sign(Vector3.Dot(combinedEquips[combinedWeaponIdx].transform.position - base.transform.position, base.transform.right));
		for (int i = 0; i < combinedEquips.Count; i++)
		{
			HPEquippable hPEquippable = combinedEquips[i];
			if (hPEquippable.GetCount() > 0)
			{
				float f = Vector3.Dot(hPEquippable.transform.position - base.transform.position, base.transform.right);
				float num6 = Mathf.Abs(f);
				if (num6 > num)
				{
					num = num6;
					num2 = i;
				}
				if (Mathf.Sign(f) != num5 && num6 > num3)
				{
					num3 = num6;
					num4 = i;
				}
			}
		}
		if (num4 >= 0)
		{
			combinedWeaponIdx = num4;
		}
		else if (num2 >= 0)
		{
			combinedWeaponIdx = num2;
		}
		else
		{
			combinedWeaponIdx = index;
		}
		combinedEquips[index].OnDisableWeapon();
		try
		{
			if (combinedWeaponIdx < 0)
			{
				combinedWeaponIdx = index;
			}
			combinedEquips[combinedWeaponIdx].OnEnableWeapon();
		}
		catch (ArgumentOutOfRangeException)
		{
			Debug.LogError("Combined weapon idx out of range.  combinedWeaponIdx: " + combinedWeaponIdx + " combinedEquips.Count: " + combinedEquips.Count);
		}
		if (OnWeaponChanged != null)
		{
			OnWeaponChanged.Invoke();
		}
	}

	public void SingleFire()
	{
		_singleFire = true;
		_singleFired = false;
	}

	public bool IsLaunchAuthorized()
	{
		if (currentEquip != null)
		{
			return currentEquip.LaunchAuthorized();
		}
		return false;
	}

	public InternalWeaponBay GetIWBForEquip(int hpIdx)
	{
		if (internalWeaponBays != null)
		{
			InternalWeaponBay[] array = internalWeaponBays;
			foreach (InternalWeaponBay internalWeaponBay in array)
			{
				if (internalWeaponBay.hardpointIdx == hpIdx)
				{
					return internalWeaponBay;
				}
			}
		}
		return null;
	}

	private void Awake()
	{
		equips = new HPEquippable[hardpointTransforms.Length];
		internalWeaponBays = GetComponentsInChildren<InternalWeaponBay>(includeInactive: true);
		noArms = true;
		OnWeaponChanged.AddListener(OnWpnChanged);
		if (!sensorAudioTransform)
		{
			sensorAudioTransform = base.transform;
		}
		if (!vm)
		{
			vm = GetComponentInParent<VehicleMaster>();
		}
	}

	private void Start()
	{
		if ((bool)opticalTargeter)
		{
			SetOpticalTargeter(opticalTargeter);
		}
		if ((bool)lockingRadar)
		{
			SetLockingRadar(lockingRadar);
		}
		OnWeaponChanged.AddListener(Evt_WeaponChanged);
		if (isPlayer)
		{
			VRJoystick[] componentsInChildren = vm.GetComponentsInChildren<VRJoystick>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].OnTriggerAxis.AddListener(SetTriggerAxis);
			}
			Debug.Log("Set up WM event for joystick trigger axis.");
		}
	}

	private void Evt_WeaponChanged()
	{
		rcsAddDirty = true;
	}

	private void Update()
	{
		bool flag = !battery || battery.Drain(powerDrain * Time.deltaTime);
		if (flag != powered)
		{
			powered = flag;
			if (!powered)
			{
				EndAllTriggerAxis();
				if (isMasterArmed)
				{
					ToggleMasterArmed();
				}
			}
		}
		if (_singleFired)
		{
			if (isFiring)
			{
				EndAllFire();
			}
			_singleFire = false;
			_singleFired = false;
		}
		else if (_singleFire)
		{
			StartFire();
			_singleFired = true;
		}
		if (!isFiring)
		{
			return;
		}
		if (rippleWeapon)
		{
			float num = rippleRates[rippleRateIdx];
			if (num > 0f && Time.time - lastTimeFired > 60f / num && (bool)currentEquip)
			{
				if (currentEquip.IsPickleToFire())
				{
					UserCycleActiveWeapon();
				}
				else
				{
					StartFire();
				}
			}
		}
		if (!powered)
		{
			EndAllFire();
		}
	}

	private void OnWpnChanged()
	{
		if ((bool)currentEquip)
		{
			dlz = currentEquip.dlz;
		}
		else
		{
			dlz = null;
		}
		UpdateAvailableWeaponTypes();
	}

	private void UpdateAvailableWeaponTypes()
	{
		availableWeaponTypes.Reset();
		for (int i = 0; i < equips.Length; i++)
		{
			HPEquippable hPEquippable = equips[i];
			if ((bool)hPEquippable && hPEquippable.GetCount() >= 1)
			{
				hPEquippable.UpdateWeaponType();
				switch (hPEquippable.weaponType)
				{
				case HPEquippable.WeaponTypes.AAM:
					availableWeaponTypes.aam = true;
					break;
				case HPEquippable.WeaponTypes.AGM:
					availableWeaponTypes.agm = true;
					break;
				case HPEquippable.WeaponTypes.AntiShip:
					availableWeaponTypes.antiShip = true;
					break;
				case HPEquippable.WeaponTypes.Gun:
					availableWeaponTypes.gun = true;
					break;
				case HPEquippable.WeaponTypes.Rocket:
					availableWeaponTypes.rocket = true;
					break;
				case HPEquippable.WeaponTypes.Bomb:
					availableWeaponTypes.bomb = true;
					break;
				case HPEquippable.WeaponTypes.AntiRadMissile:
					availableWeaponTypes.antirad = true;
					break;
				case HPEquippable.WeaponTypes.AGMCruise:
					availableWeaponTypes.agmCruise = true;
					break;
				}
			}
		}
	}

	public void StartFire()
	{
		if (!AllowControl())
		{
			return;
		}
		isUserTriggerHeld = true;
		if (!noArms && isMasterArmed && powered && (bool)currentEquip)
		{
			currentEquip.OnStartFire();
			if (!currentEquip.IsPickleToFire())
			{
				isFiring = true;
				lastTimeFired = Time.time;
			}
			this.OnStartFire?.Invoke();
		}
	}

	private bool AllowControl()
	{
		if (!VTOLMPUtils.IsMultiplayer())
		{
			return true;
		}
		if (!remoteSync)
		{
			return true;
		}
		return remoteSync.muvs.IsLocalWeaponController();
	}

	public void EndFire()
	{
		if (!isUserTriggerHeld)
		{
			return;
		}
		isUserTriggerHeld = false;
		if (!noArms && isMasterArmed && (bool)currentEquip)
		{
			if (!currentEquip.IsPickleToFire())
			{
				isFiring = false;
			}
			currentEquip.OnStopFire();
			this.OnEndFire?.Invoke();
		}
	}

	public void EndAllFire()
	{
		foreach (HPEquippable combinedEquip in combinedEquips)
		{
			combinedEquip.OnStopFire();
		}
		isFiring = false;
	}

	public void EndAllTriggerAxis()
	{
		foreach (HPEquippable combinedEquip in combinedEquips)
		{
			combinedEquip.OnTriggerAxis(0f);
		}
	}

	public void SetTriggerAxis(float axis)
	{
		if ((bool)currentEquip)
		{
			currentEquip.OnTriggerAxis(Mathf.Clamp01(axis));
		}
	}

	public bool IsAnyWeaponMarkedJettison()
	{
		for (int i = 0; i < equips.Length; i++)
		{
			if ((bool)equips[i] && equips[i].markedForJettison)
			{
				return true;
			}
		}
		return false;
	}

	public void JettisonMarkedItems()
	{
		bool flag = false;
		for (int i = 0; i < equips.Length; i++)
		{
			HPEquippable hPEquippable = equips[i];
			if (!hPEquippable || !hPEquippable.markedForJettison)
			{
				continue;
			}
			bool flag2 = false;
			for (int j = 0; j < internalWeaponBays.Length; j++)
			{
				if (flag2)
				{
					break;
				}
				if ((bool)internalWeaponBays[j] && internalWeaponBays[j].hardpointIdx == i)
				{
					flag2 = true;
					StartCoroutine(InternalBayJettison(internalWeaponBays[j]));
				}
			}
			if (!flag2)
			{
				hPEquippable.Jettison();
				flag = true;
				equips[i] = null;
				AudioSource component = hardpointTransforms[i].GetComponent<AudioSource>();
				if ((bool)component && (bool)jettisonAudioClip)
				{
					component.PlayOneShot(jettisonAudioClip);
				}
				MassUpdater component2 = vesselRB.GetComponent<MassUpdater>();
				IMassObject[] componentsInChildren = hPEquippable.GetComponentsInChildren<IMassObject>();
				foreach (IMassObject o in componentsInChildren)
				{
					component2.RemoveMassObject(o);
				}
			}
		}
		if (flag)
		{
			RefreshWeapon();
		}
	}

	private void JettisonEq(int i)
	{
		HPEquippable hPEquippable = equips[i];
		if ((bool)hPEquippable)
		{
			hPEquippable.Jettison();
			equips[i] = null;
			AudioSource component = hardpointTransforms[i].GetComponent<AudioSource>();
			if ((bool)component && (bool)jettisonAudioClip)
			{
				component.PlayOneShot(jettisonAudioClip);
			}
			MassUpdater component2 = vesselRB.GetComponent<MassUpdater>();
			IMassObject[] componentsInChildren = hPEquippable.GetComponentsInChildren<IMassObject>();
			foreach (IMassObject o in componentsInChildren)
			{
				component2.RemoveMassObject(o);
			}
			RefreshWeapon();
		}
	}

	public void JettisonByPartDestruction(int i)
	{
		HPEquippable hPEquippable = equips[i];
		if ((bool)hPEquippable)
		{
			hPEquippable.OnDisabledByPartDestroy();
			hPEquippable.Jettison();
			equips[i] = null;
			MassUpdater component = vesselRB.GetComponent<MassUpdater>();
			IMassObject[] componentsInChildren = hPEquippable.GetComponentsInChildren<IMassObject>();
			foreach (IMassObject o in componentsInChildren)
			{
				component.RemoveMassObject(o);
			}
			RefreshWeapon();
		}
	}

	public void DisableWeaponByPartDestruction(int i)
	{
		HPEquippable hPEquippable = equips[i];
		if ((bool)hPEquippable)
		{
			partDestroyedEquip = hPEquippable;
			hPEquippable.OnDisabledByPartDestroy();
			hPEquippable.enabled = false;
			equips[i] = null;
			RefreshWeapon();
		}
	}

	public void RepairDestroyedEquip(int i)
	{
		if (!equips[i] && (bool)partDestroyedEquip)
		{
			equips[i] = partDestroyedEquip;
			partDestroyedEquip = null;
			equips[i].enabled = true;
			equips[i].OnRepairedDestroyedPart();
			RefreshWeapon();
		}
	}

	private IEnumerator InternalBayJettison(InternalWeaponBay bay)
	{
		object jettObj = new object();
		bay.RegisterOpenReq(jettObj);
		while (bay.doorState < 0.99f)
		{
			yield return null;
		}
		JettisonEq(bay.hardpointIdx);
		yield return new WaitForSeconds(1f);
		bay.UnregisterOpenReq(jettObj);
	}

	public void MarkEmptyToJettison()
	{
		HPEquippable[] array = equips;
		foreach (HPEquippable hPEquippable in array)
		{
			if ((bool)hPEquippable)
			{
				if (hPEquippable.GetCount() == 0)
				{
					hPEquippable.markedForJettison = true;
				}
				else
				{
					hPEquippable.markedForJettison = false;
				}
				ReportEquipJettisonMark(hPEquippable);
			}
		}
	}

	public void MarkDroptanksToJettison()
	{
		HPEquippable[] array = equips;
		foreach (HPEquippable hPEquippable in array)
		{
			if ((bool)hPEquippable)
			{
				if (hPEquippable is HPEquipDropTank)
				{
					hPEquippable.markedForJettison = true;
				}
				else
				{
					hPEquippable.markedForJettison = false;
				}
				ReportEquipJettisonMark(hPEquippable);
			}
		}
	}

	public void MarkAllJettison()
	{
		HPEquippable[] array = equips;
		foreach (HPEquippable hPEquippable in array)
		{
			if ((bool)hPEquippable)
			{
				hPEquippable.markedForJettison = true;
				ReportEquipJettisonMark(hPEquippable);
			}
		}
	}

	public void MarkNoneJettison()
	{
		HPEquippable[] array = equips;
		foreach (HPEquippable hPEquippable in array)
		{
			if ((bool)hPEquippable)
			{
				hPEquippable.markedForJettison = false;
				ReportEquipJettisonMark(hPEquippable);
			}
		}
	}

	public void ClearEquips()
	{
		for (int i = 0; i < equips.Length; i++)
		{
			HPEquippable hPEquippable = equips[i];
			if ((bool)hPEquippable)
			{
				UnityEngine.Object.Destroy(hPEquippable.gameObject);
			}
			equips[i] = null;
		}
		vesselRB.GetComponent<MassUpdater>().UpdateMassObjects();
		RefreshWeapon();
	}

	public void EquipWeapons(Loadout loadout)
	{
		maxAntiAirRange = 0f;
		maxAntiRadRange = 0f;
		maxAGMRange = 0f;
		MassUpdater component = vesselRB.GetComponent<MassUpdater>();
		for (int i = 0; i < equips.Length; i++)
		{
			if (equips[i] != null)
			{
				IMassObject[] componentsInChildren = equips[i].GetComponentsInChildren<IMassObject>();
				foreach (IMassObject o in componentsInChildren)
				{
					component.RemoveMassObject(o);
				}
				equips[i].OnUnequip();
				InvokeUnequipEvent(i);
				UnityEngine.Object.Destroy(equips[i].gameObject);
				equips[i] = null;
			}
		}
		string[] hpLoadout = loadout.hpLoadout;
		for (int k = 0; k < hardpointTransforms.Length && k < hpLoadout.Length; k++)
		{
			if (string.IsNullOrEmpty(hpLoadout[k]))
			{
				continue;
			}
			UnityEngine.Object @object = Resources.Load(resourcePath + "/" + hpLoadout[k]);
			if (!@object)
			{
				continue;
			}
			GameObject obj = (GameObject)UnityEngine.Object.Instantiate(@object, hardpointTransforms[k]);
			obj.name = hpLoadout[k];
			obj.transform.localRotation = Quaternion.identity;
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localScale = Vector3.one;
			HPEquippable component2 = obj.GetComponent<HPEquippable>();
			component2.SetWeaponManager(this);
			equips[k] = component2;
			component2.wasPurchased = true;
			component2.hardpointIdx = k;
			component2.Equip();
			if (this.OnWeaponEquipped != null)
			{
				this.OnWeaponEquipped(component2);
			}
			if (this.OnWeaponEquippedHPIdx != null)
			{
				this.OnWeaponEquippedHPIdx(k);
			}
			if (component2.jettisonable)
			{
				Rigidbody component3 = component2.GetComponent<Rigidbody>();
				if ((bool)component3)
				{
					component3.interpolation = RigidbodyInterpolation.None;
				}
			}
			if (component2.armable)
			{
				component2.armed = true;
				if (!uniqueWeapons.Contains(component2.shortName))
				{
					uniqueWeapons.Add(component2.shortName);
				}
			}
			obj.SetActive(value: true);
			Component[] componentsInChildren2 = component2.gameObject.GetComponentsInChildren<Component>();
			foreach (Component component4 in componentsInChildren2)
			{
				if (component4 is IParentRBDependent)
				{
					((IParentRBDependent)component4).SetParentRigidbody(vesselRB);
				}
				if (component4 is IRequiresLockingRadar)
				{
					((IRequiresLockingRadar)component4).SetLockingRadar(lockingRadar);
				}
				if (component4 is IRequiresOpticalTargeter)
				{
					((IRequiresOpticalTargeter)component4).SetOpticalTargeter(opticalTargeter);
				}
			}
			if (component2 is HPEquipIRML || component2 is HPEquipRadarML)
			{
				if ((bool)component2.dlz)
				{
					maxAntiAirRange = Mathf.Max(component2.dlz.GetDynamicLaunchParams(base.transform.forward * 343f, base.transform.position + base.transform.forward * 10000f, Vector3.zero).maxLaunchRange, maxAntiAirRange);
				}
			}
			else if (component2 is HPEquipARML)
			{
				if ((bool)component2.dlz)
				{
					maxAntiRadRange = Mathf.Max(component2.dlz.GetDynamicLaunchParams(base.transform.forward * 343f, base.transform.position + base.transform.forward * 10000f, Vector3.zero).maxLaunchRange, maxAntiRadRange);
				}
			}
			else if (component2 is HPEquipOpticalML && (bool)component2.dlz)
			{
				maxAGMRange = Mathf.Max(component2.dlz.GetDynamicLaunchParams(base.transform.forward * 280f, base.transform.position + base.transform.forward * 10000f, Vector3.zero).maxLaunchRange, maxAGMRange);
			}
			ReportWeaponArming(component2);
			ReportEquipJettisonMark(component2);
		}
		if ((bool)vesselRB)
		{
			vesselRB.ResetInertiaTensor();
		}
		if (loadout.cmLoadout != null)
		{
			CountermeasureManager componentInChildren = GetComponentInChildren<CountermeasureManager>();
			if ((bool)componentInChildren)
			{
				for (int l = 0; l < componentInChildren.countermeasures.Count && l < loadout.cmLoadout.Length; l++)
				{
					componentInChildren.countermeasures[l].count = Mathf.Clamp(loadout.cmLoadout[l], 0, componentInChildren.countermeasures[l].maxCount);
					componentInChildren.countermeasures[l].UpdateCountText();
				}
			}
		}
		weaponIdx = 0;
		ToggleMasterArmed();
		ToggleMasterArmed();
		if (OnWeaponChanged != null)
		{
			OnWeaponChanged.Invoke();
		}
		component.UpdateMassObjects();
		rcsAddDirty = true;
	}

	public void CycleActiveWeapons(bool userFired = false)
	{
		if (!isMasterArmed)
		{
			return;
		}
		EndAllTriggerAxis();
		if (isFiring)
		{
			EndAllFire();
		}
		rippleWeapon = false;
		HPEquippable hPEquippable = currentEquip;
		if (uniqueWeapons.Count == 0)
		{
			noArms = true;
			combinedEquips = new List<HPEquippable>();
			weaponIdx = 0;
			activeWeaponName = "NONE";
			return;
		}
		int num = weaponIdx;
		int num2 = (num + 1) % uniqueWeapons.Count;
		while (num2 != num)
		{
			int index = num2;
			num2 = (num2 + 1) % uniqueWeapons.Count;
			bool flag = false;
			for (int i = 0; i < equips.Length; i++)
			{
				if ((bool)equips[i] && equips[i].shortName == uniqueWeapons[index] && equips[i].armed)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				weaponIdx = index;
				if (userFired && OnUserCycledWeapon != null)
				{
					OnUserCycledWeapon.Invoke();
				}
				break;
			}
		}
		activeWeaponName = uniqueWeapons[weaponIdx];
		SetCombinedEquips();
		if ((bool)hPEquippable)
		{
			hPEquippable.OnDisableWeapon();
		}
		if (noArms)
		{
			activeWeaponName = "NONE";
		}
		else
		{
			currentEquip.OnEnableWeapon();
		}
		ToggleCombinedWeapon();
		if (OnWeaponChanged != null)
		{
			OnWeaponChanged.Invoke();
		}
	}

	public void UserCycleActiveWeapon()
	{
		if (!AllowControl())
		{
			return;
		}
		if (currentEquip != null)
		{
			if (currentEquip.IsPickleToFire())
			{
				isFiring = true;
				lastTimeFired = Time.time;
			}
			currentEquip.OnCycleWeaponButton();
		}
		else
		{
			CycleActiveWeapons(userFired: true);
		}
	}

	public void UserReleaseCycleActiveWeaponButton()
	{
		if (AllowControl() && currentEquip != null)
		{
			currentEquip.OnReleasedCycleWeaponButton();
			if (currentEquip.IsPickleToFire())
			{
				isFiring = false;
			}
		}
	}

	private void SetCombinedEquips()
	{
		equippedGun = false;
		noArms = true;
		combinedEquips = new List<HPEquippable>();
		int num = -1;
		combinedWeaponIdx = 0;
		for (int i = 0; i < equips.Length; i++)
		{
			HPEquippable hPEquippable = equips[i];
			if ((bool)hPEquippable && hPEquippable.armed && hPEquippable.shortName == activeWeaponName)
			{
				noArms = false;
				combinedEquips.Add(hPEquippable);
				if (hPEquippable.GetCount() >= num)
				{
					num = hPEquippable.GetCount();
					combinedWeaponIdx = combinedEquips.Count - 1;
				}
				if (hPEquippable is IRippleWeapon)
				{
					IRippleWeapon rippleWeapon = (IRippleWeapon)hPEquippable;
					this.rippleWeapon = true;
					rippleRates = rippleWeapon.GetRippleRates();
					rippleRateIdx = rippleWeapon.GetRippleRateIdx();
				}
				if (hPEquippable is HPEquipGun || hPEquippable is VTOLCannon)
				{
					equippedGun = true;
				}
			}
		}
	}

	public void SetWeapon(string shortName)
	{
		if (!isMasterArmed)
		{
			return;
		}
		for (int i = 0; i < equips.Length; i++)
		{
			if (equips[i] != null && equips[i].shortName == shortName)
			{
				SetWeapon(i);
				break;
			}
		}
	}

	public void SetWeapon(int eqIdx)
	{
		if (equips[eqIdx] == null || !isMasterArmed)
		{
			return;
		}
		EndAllTriggerAxis();
		if (isFiring)
		{
			EndAllFire();
		}
		rippleWeapon = false;
		HPEquippable hPEquippable = currentEquip;
		for (int i = 0; i < uniqueWeapons.Count; i++)
		{
			if (uniqueWeapons[i] == equips[eqIdx].shortName)
			{
				weaponIdx = i;
				break;
			}
		}
		activeWeaponName = uniqueWeapons[weaponIdx];
		SetCombinedEquips();
		if ((bool)hPEquippable)
		{
			hPEquippable.OnDisableWeapon();
		}
		if (noArms)
		{
			activeWeaponName = "NONE";
		}
		else
		{
			currentEquip.OnEnableWeapon();
		}
		ToggleCombinedWeapon();
		if (OnWeaponChanged != null)
		{
			OnWeaponChanged.Invoke();
		}
	}

	public void RefreshWeapon()
	{
		uniqueWeapons = new List<string>();
		for (int i = 0; i < equips.Length; i++)
		{
			HPEquippable hPEquippable = equips[i];
			if ((bool)hPEquippable)
			{
				hPEquippable.transform.localPosition = Vector3.zero;
				hPEquippable.transform.localRotation = Quaternion.identity;
				hPEquippable.hardpointIdx = i;
				if (hPEquippable.armable && !uniqueWeapons.Contains(hPEquippable.shortName))
				{
					uniqueWeapons.Add(hPEquippable.shortName);
				}
			}
		}
		if (uniqueWeapons.Count > 0)
		{
			weaponIdx = (weaponIdx + uniqueWeapons.Count - 1) % uniqueWeapons.Count;
		}
		else
		{
			weaponIdx = 0;
		}
		CycleActiveWeapons();
		if (OnWeaponChanged != null)
		{
			OnWeaponChanged.Invoke();
		}
	}

	public void ReattachWeapons()
	{
		uniqueWeapons.Clear();
		if (equips == null)
		{
			equips = new HPEquippable[hardpointTransforms.Length];
		}
		for (int i = 0; i < equips.Length; i++)
		{
			equips[i] = hardpointTransforms[i].gameObject.GetComponentInChildrenImplementing<HPEquippable>();
			if (!equips[i])
			{
				continue;
			}
			HPEquippable hPEquippable = equips[i];
			hPEquippable.hardpointIdx = i;
			hPEquippable.SetWeaponManager(this);
			hPEquippable.Equip();
			if (!hPEquippable.wasPurchased)
			{
				hPEquippable.wasPurchased = true;
			}
			if (this.OnWeaponEquipped != null)
			{
				this.OnWeaponEquipped(hPEquippable);
			}
			if (this.OnWeaponEquippedHPIdx != null)
			{
				this.OnWeaponEquippedHPIdx(i);
			}
			if (hPEquippable.jettisonable)
			{
				Rigidbody component = hPEquippable.GetComponent<Rigidbody>();
				if ((bool)component)
				{
					component.interpolation = RigidbodyInterpolation.None;
				}
				ReportEquipJettisonMark(hPEquippable);
			}
			if (hPEquippable.armable)
			{
				hPEquippable.armed = true;
				if (!uniqueWeapons.Contains(hPEquippable.shortName))
				{
					uniqueWeapons.Add(hPEquippable.shortName);
				}
				ReportWeaponArming(hPEquippable);
			}
			Component[] componentsInChildren = hPEquippable.gameObject.GetComponentsInChildren<Component>();
			foreach (Component component2 in componentsInChildren)
			{
				if (component2 is IParentRBDependent)
				{
					((IParentRBDependent)component2).SetParentRigidbody(vesselRB);
				}
				if (component2 is IRequiresLockingRadar)
				{
					((IRequiresLockingRadar)component2).SetLockingRadar(lockingRadar);
				}
				if (component2 is IRequiresOpticalTargeter)
				{
					((IRequiresOpticalTargeter)component2).SetOpticalTargeter(opticalTargeter);
				}
			}
		}
		MassUpdater component3 = vesselRB.GetComponent<MassUpdater>();
		if ((bool)component3)
		{
			component3.UpdateMassObjects();
		}
		vesselRB.ResetInertiaTensor();
		RefreshWeapon();
		UpdateAvailableWeaponTypes();
	}

	public void CycleRippleRates(int equipIdx, bool sendEvent = true)
	{
		HPEquippable hPEquippable = equips[equipIdx];
		IRippleWeapon rippleWeapon = (IRippleWeapon)hPEquippable;
		int num = (rippleWeapon.GetRippleRateIdx() + 1) % rippleWeapon.GetRippleRates().Length;
		HPEquippable[] array = equips;
		foreach (HPEquippable hPEquippable2 in array)
		{
			if ((bool)hPEquippable2 && hPEquippable2.shortName == hPEquippable.shortName)
			{
				((IRippleWeapon)hPEquippable2).SetRippleRateIdx(num);
			}
		}
		if (activeWeaponName == hPEquippable.shortName)
		{
			rippleRateIdx = num;
		}
		hPEquippable.EquipDataChanged();
		if (sendEvent)
		{
			this.OnRippleChanged?.Invoke(equipIdx, num);
		}
	}

	public void ReportWeaponArming(HPEquippable eq)
	{
		if ((bool)eq)
		{
			this.OnEquipArmingChanged?.Invoke(eq, eq.armed);
		}
	}

	public void ReportEquipJettisonMark(HPEquippable eq)
	{
		if ((bool)eq)
		{
			this.OnEquipJettisonChanged?.Invoke(eq, eq.markedForJettison);
		}
	}

	public void WeaponFunctionButton(int buttonIdx, int weaponIdx, bool sendEvent = true)
	{
		HPEquippable hPEquippable = equips[weaponIdx];
		HPEquippable[] array = equips;
		foreach (HPEquippable hPEquippable2 in array)
		{
			if ((bool)hPEquippable2 && hPEquippable2.shortName == hPEquippable.shortName)
			{
				hPEquippable2.equipFunctions[buttonIdx].PressFunction();
				hPEquippable2.EquipDataChanged();
			}
		}
		if (sendEvent)
		{
			this.OnWeaponFunctionCalled?.Invoke(buttonIdx, weaponIdx);
		}
		else if ((bool)ui && (bool)ui.hudInfo)
		{
			ui.hudInfo.RefreshWeaponInfo();
			ui.UpdateDisplay();
		}
	}

	public void SetRCSDirty()
	{
		rcsAddDirty = true;
	}

	public float GetAdditionalRadarCrossSection()
	{
		if (rcsAddDirty)
		{
			if (equips == null)
			{
				rcsAdd = 0f;
				rcsAddDirty = false;
				return 0f;
			}
			rcsAdd = 0f;
			for (int i = 0; i < equips.Length; i++)
			{
				if ((bool)equips[i])
				{
					rcsAdd += equips[i].GetRadarCrossSection();
				}
			}
			rcsAddDirty = false;
		}
		return rcsAdd;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_WeaponManager");
		if (currentEquip != null)
		{
			configNode.SetValue("currentEquip", currentEquip.shortName);
		}
		configNode.SetValue("masterArmed", _masterArmed);
		for (int i = 0; i < equips.Length; i++)
		{
			if (equips[i] != null)
			{
				string value = equips[i].gameObject.name;
				ConfigNode configNode2 = new ConfigNode("EQUIP");
				configNode2.SetValue("idx", i);
				configNode2.SetValue("equipID", value);
				equips[i].OnQuicksaveEquip(configNode2);
				configNode.AddNode(configNode2);
			}
		}
		if (commonData != null && commonData.Count > 0)
		{
			ConfigNode configNode3 = new ConfigNode("commonData");
			foreach (string key in commonData.Keys)
			{
				try
				{
					string value2 = ConfigNodeUtils.WriteObject(commonData[key]);
					string value3 = commonData[key].GetType().ToString();
					ConfigNode configNode4 = new ConfigNode("DATA");
					configNode4.SetValue("key", key);
					configNode4.SetValue("type", value3);
					configNode4.SetValue("value", value2);
					configNode3.AddNode(configNode4);
				}
				catch (Exception ex)
				{
					Debug.LogFormat("Failed to write weaponManager common data to quicksave node: {0}", ex);
				}
			}
			configNode.AddNode(configNode3);
		}
		if (gpsSystem != null && !gpsSystem.noGroups)
		{
			ConfigNode configNode5 = new ConfigNode("GPS");
			configNode5.SetValue("currGroupIdx", gpsSystem.currGroupIdx);
			foreach (string groupName in gpsSystem.groupNames)
			{
				GPSTargetGroup gPSTargetGroup = gpsSystem.targetGroups[groupName];
				ConfigNode configNode6 = new ConfigNode("GROUP");
				configNode6.SetValue("denom", gPSTargetGroup.denom);
				configNode6.SetValue("numeral", gPSTargetGroup.numeral);
				configNode6.SetValue("currentTargetIdx", gPSTargetGroup.currentTargetIdx);
				configNode6.SetValue("isPath", gPSTargetGroup.isPath);
				foreach (GPSTarget target in gPSTargetGroup.targets)
				{
					ConfigNode configNode7 = new ConfigNode("TARGET");
					configNode7.SetValue("denom", target.denom);
					configNode7.SetValue("numeral", target.numeral);
					configNode7.SetValue("globalPoint", ConfigNodeUtils.WriteVector3D(VTMapManager.WorldToGlobalPoint(target.worldPosition)));
					configNode6.AddNode(configNode7);
				}
				configNode5.AddNode(configNode6);
			}
			configNode.AddNode(configNode5);
		}
		qsNode.AddNode(configNode);
		if ((bool)tsc)
		{
			tsc.Quicksave(qsNode);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode[] array = new ConfigNode[hardpointTransforms.Length];
		Loadout loadout = new Loadout();
		loadout.hpLoadout = new string[hardpointTransforms.Length];
		string text = base.gameObject.name + "_WeaponManager";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			foreach (ConfigNode node3 in node.GetNodes("EQUIP"))
			{
				string value = node3.GetValue("equipID");
				int num = ConfigNodeUtils.ParseInt(node3.GetValue("idx"));
				loadout.hpLoadout[num] = value;
				array[num] = node3;
			}
			if (node.HasNode("commonData"))
			{
				commonData = new Dictionary<string, object>();
				foreach (ConfigNode node4 in node.GetNode("commonData").GetNodes("DATA"))
				{
					string value2 = node4.GetValue("key");
					object value3 = ConfigNodeUtils.ParseObject(Type.GetType(node4.GetValue("type")), node4.GetValue("value"));
					commonData.Add(value2, value3);
				}
			}
			if (node.HasNode("GPS"))
			{
				gpsSystem.targetGroups = new Dictionary<string, GPSTargetGroup>();
				ConfigNode node2 = node.GetNode("GPS");
				foreach (ConfigNode node5 in node2.GetNodes("GROUP"))
				{
					string value4 = node5.GetValue("denom");
					int value5 = node5.GetValue<int>("numeral");
					int currentTargetIdx = ConfigNodeUtils.ParseInt(node5.GetValue("currentTargetIdx"));
					bool isPath = ConfigNodeUtils.ParseBool(node5.GetValue("isPath"));
					GPSTargetGroup gPSTargetGroup = new GPSTargetGroup(value4, value5);
					foreach (ConfigNode node6 in node5.GetNodes("TARGET"))
					{
						Vector3D globalPoint = ConfigNodeUtils.ParseVector3D(node6.GetValue("globalPoint"));
						gPSTargetGroup.AddTarget(new GPSTarget(VTMapManager.GlobalToWorldPoint(globalPoint), node6.GetValue("denom"), node6.GetValue<int>("numeral")));
					}
					gPSTargetGroup.isPath = isPath;
					gPSTargetGroup.currentTargetIdx = currentTargetIdx;
					gpsSystem.groupNames.Add(gPSTargetGroup.groupName);
					gpsSystem.targetGroups.Add(gPSTargetGroup.groupName, gPSTargetGroup);
				}
				int currentGroup = ConfigNodeUtils.ParseInt(node2.GetValue("currGroupIdx"));
				gpsSystem.UpdateRemotelyModifiedGroups();
				gpsSystem.SetCurrentGroup(currentGroup);
			}
			EquipWeapons(loadout);
			bool value6 = node.GetValue<bool>("masterArmed");
			SetMasterArmed(value6);
			for (int i = 0; i < equips.Length; i++)
			{
				if (equips[i] != null && array[i] != null)
				{
					equips[i].OnQuickloadEquip(array[i]);
				}
			}
			RefreshWeapon();
			if (!noArms && node.HasValue("currentEquip"))
			{
				string value7 = node.GetValue("currentEquip");
				SetWeapon(value7);
			}
			if ((bool)ui)
			{
				ui.UpdateDisplay();
			}
		}
		if ((bool)tsc)
		{
			tsc.Quickload(qsNode);
		}
		if (isPlayer)
		{
			Debug.Log("Player WeaponManager quickloaded");
		}
	}
}
