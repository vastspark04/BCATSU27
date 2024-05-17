using UnityEngine;
using UnityEngine.Events;
using VTNetworking;
using VTOLVR.Multiplayer;

public class HPEquippable : MonoBehaviour, ILocalizationUser
{
	public class EquipFunction
	{
		public delegate string OptionEvent();

		public string optionName;

		public OptionEvent optionEvent;

		public string optionReturnLabel;

		public void PressFunction()
		{
			if (optionEvent != null)
			{
				optionReturnLabel = optionEvent();
			}
		}
	}

	public enum WeaponTypes
	{
		Unknown,
		Gun,
		AntiShip,
		AAM,
		AGM,
		Rocket,
		Bomb,
		AntiRadMissile,
		AGMCruise
	}

	public string fullName;

	public string shortName;

	public bool localize = true;

	public float unitCost;

	[TextArea(3, 10)]
	public string description;

	public string subLabel;

	public bool jettisonable = true;

	public bool armable = true;

	public bool jettisonOnHPDied = true;

	public int reticleIndex;

	public EquipFunction[] equipFunctions;

	public string allowedHardpoints = "1,2,3,4";

	[Tooltip("The default base cross section for the equip.  This automatically gets overriden by the average cross section if a RadarCrossSection component is attached.")]
	public float baseRadarCrossSection = 0.25f;

	public MeshRenderer[] matchLiveries;

	[HideInInspector]
	public bool wasPurchased;

	[HideInInspector]
	public bool rcsMasked;

	[HideInInspector]
	public int hardpointIdx;

	private bool _markedForJettison;

	private bool _armed;

	private bool jetted;

	private string subLabelKey;

	public bool itemActivated { get; private set; }

	public DynamicLaunchZone dlz { get; private set; }

	public WeaponManager weaponManager { get; private set; }

	public bool markedForJettison
	{
		get
		{
			if (!jettisonable)
			{
				return false;
			}
			return _markedForJettison;
		}
		set
		{
			if (jettisonable)
			{
				_markedForJettison = value;
				if (armed && _markedForJettison && weaponManager.markJettisonDisarms)
				{
					armed = false;
					weaponManager.ReportWeaponArming(this);
				}
			}
		}
	}

	public bool armed
	{
		get
		{
			if (armable)
			{
				return _armed;
			}
			return false;
		}
		set
		{
			if (armable)
			{
				_armed = value;
			}
		}
	}

	public WeaponTypes weaponType { get; private set; }

	public event UnityAction OnJettisoned;

	public event UnityAction OnEquipped;

	public virtual float GetTotalCost()
	{
		return unitCost;
	}

	protected virtual void Awake()
	{
		ApplyLocalization();
		UpdateWeaponType();
	}

	protected virtual void Start()
	{
	}

	public virtual int GetCount()
	{
		return 0;
	}

	public virtual int GetMaxCount()
	{
		return -1;
	}

	public virtual float GetWeaponDamage()
	{
		Debug.LogErrorFormat("GetWeaponDamage was not defined for this weapon! {0}", base.gameObject.name);
		return 0f;
	}

	public void SetWeaponManager(WeaponManager wm)
	{
		weaponManager = wm;
	}

	public virtual void OnCycleWeaponButton()
	{
		weaponManager.CycleActiveWeapons(userFired: true);
	}

	public virtual void OnReleasedCycleWeaponButton()
	{
	}

	public virtual bool IsPickleToFire()
	{
		return false;
	}

	public virtual bool LaunchAuthorized()
	{
		return false;
	}

	public void Jettison()
	{
		if (!jetted)
		{
			jetted = true;
			FloatingOrigin.instance.AddQueuedFixedUpdateAction(LateFixedUpdateJettison);
		}
	}

	private void LateFixedUpdateJettison()
	{
		if (this == null || !base.transform)
		{
			return;
		}
		if (itemActivated)
		{
			OnDisableWeapon();
		}
		base.transform.parent = null;
		Rigidbody rigidbody = base.gameObject.AddComponent<Rigidbody>();
		IMassObject componentImplementing = base.gameObject.GetComponentImplementing<IMassObject>();
		if (componentImplementing != null)
		{
			rigidbody.mass = componentImplementing.GetMass();
		}
		else
		{
			rigidbody.mass = 0.01f;
		}
		rigidbody.isKinematic = false;
		rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		if ((bool)weaponManager && (bool)weaponManager.vesselRB)
		{
			rigidbody.velocity = weaponManager.vesselRB.GetPointVelocity(base.transform.position);
		}
		IParentRBDependent[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IParentRBDependent>(includeInactive: true);
		for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
		{
			componentsInChildrenImplementing[i].SetParentRigidbody(rigidbody);
		}
		FloatingOriginTransform floatingOriginTransform = GetComponent<FloatingOriginTransform>();
		if (!floatingOriginTransform)
		{
			floatingOriginTransform = base.gameObject.AddComponent<FloatingOriginTransform>();
		}
		floatingOriginTransform.SetRigidbody(rigidbody);
		SimpleDrag component = GetComponent<SimpleDrag>();
		if ((bool)component)
		{
			component.enabled = true;
		}
		OnJettison();
		if (this.OnJettisoned != null)
		{
			this.OnJettisoned();
		}
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += OnExitScene;
		}
		if (VTOLMPUtils.IsMultiplayer())
		{
			VTNetEntity component2 = GetComponent<VTNetEntity>();
			if ((bool)component2 && component2.isMine)
			{
				VTNetworkManager.NetDestroyDelayed(base.gameObject, 15f);
			}
		}
		else
		{
			Object.Destroy(base.gameObject, 15f);
		}
	}

	private void OnDestroy()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= OnExitScene;
		}
	}

	private void OnExitScene()
	{
		if ((bool)this && (bool)base.gameObject)
		{
			Object.Destroy(base.gameObject);
		}
	}

	protected virtual void OnJettison()
	{
	}

	public virtual void OnConfigAttach(LoadoutConfigurator configurator)
	{
		if (matchLiveries != null && (bool)configurator.wm.liverySample)
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			configurator.wm.liverySample.GetPropertyBlock(materialPropertyBlock);
			MeshRenderer[] array = matchLiveries;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetPropertyBlock(materialPropertyBlock);
			}
		}
	}

	public virtual void OnConfigDetach(LoadoutConfigurator configurator)
	{
	}

	public virtual void OnDisabledByPartDestroy()
	{
	}

	public virtual void OnRepairedDestroyedPart()
	{
	}

	public void Equip()
	{
		if ((bool)FlightSceneManager.instance && FlightSceneManager.instance.playerActor == weaponManager.actor && PilotSaveManager.current != null && PilotSaveManager.current.lastVehicleSave != null && PilotSaveManager.current.lastVehicleSave.vehicleDataNode != null)
		{
			LoadEquipData(GetWeaponConfigNode(PilotSaveManager.current.lastVehicleSave.vehicleDataNode));
		}
		RadarCrossSection component = GetComponent<RadarCrossSection>();
		if ((bool)component)
		{
			baseRadarCrossSection = component.GetAverageCrossSection();
		}
		OnEquip();
		if (!weaponManager.isPlayer)
		{
			ISetLowPoly[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<ISetLowPoly>();
			for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
			{
				componentsInChildrenImplementing[i].SetLowPoly();
			}
			AudioSource[] componentsInChildren = GetComponentsInChildren<AudioSource>(includeInactive: true);
			foreach (AudioSource audioSource in componentsInChildren)
			{
				if (audioSource.outputAudioMixerGroup == AudioController.instance.exteriorAttachedChannel)
				{
					audioSource.outputAudioMixerGroup = AudioController.instance.exteriorChannel;
				}
				else if (audioSource.outputAudioMixerGroup == AudioController.instance.interiorChannel)
				{
					audioSource.mute = true;
				}
			}
		}
		if (matchLiveries != null && (bool)weaponManager.liverySample)
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			weaponManager.liverySample.GetPropertyBlock(materialPropertyBlock);
			MeshRenderer[] array = matchLiveries;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetPropertyBlock(materialPropertyBlock);
			}
		}
		this.OnEquipped?.Invoke();
	}

	public virtual void OnUnequip()
	{
	}

	public void EquipDataChanged()
	{
		if ((bool)FlightSceneManager.instance && FlightSceneManager.instance.playerActor == weaponManager.actor && PilotSaveManager.current != null && PilotSaveManager.current.lastVehicleSave != null && PilotSaveManager.current.lastVehicleSave.vehicleDataNode != null)
		{
			SaveEquipData(GetWeaponConfigNode(PilotSaveManager.current.lastVehicleSave.vehicleDataNode));
		}
	}

	protected virtual void OnEquip()
	{
		dlz = GetComponent<DynamicLaunchZone>();
	}

	public void UpdateWeaponType()
	{
		weaponType = WeaponTypes.Unknown;
		if (this is HPEquipGun || this is HPEquipGunTurret)
		{
			weaponType = WeaponTypes.Gun;
		}
		else if (this is HPEquipIRML || this is HPEquipRadarML)
		{
			weaponType = WeaponTypes.AAM;
		}
		else if (this is HPEquipOpticalML)
		{
			weaponType = WeaponTypes.AGM;
		}
		else if (this is RocketLauncher)
		{
			weaponType = WeaponTypes.Rocket;
		}
		else if (this is HPEquipBombRack || this is HPEquipGPSBombRack || this is HPEquipLaserBombRack)
		{
			weaponType = WeaponTypes.Bomb;
		}
		else if (this is HPEquipASML)
		{
			weaponType = WeaponTypes.AntiShip;
		}
		else if (this is HPEquipAGMCruiseMissile)
		{
			weaponType = WeaponTypes.AGMCruise;
		}
		else if (this is HPEquipARML)
		{
			weaponType = WeaponTypes.AntiRadMissile;
		}
	}

	public virtual void OnTriggerAxis(float axis)
	{
	}

	public virtual Vector3 GetAimPoint()
	{
		return base.transform.position + 2000f * base.transform.forward;
	}

	public virtual void OnStartFire()
	{
	}

	public virtual void OnStopFire()
	{
	}

	public virtual int GetReticleIndex()
	{
		return reticleIndex;
	}

	public virtual void OnEnableWeapon()
	{
		itemActivated = true;
	}

	public virtual void OnDisableWeapon()
	{
		itemActivated = false;
	}

	protected virtual void LoadEquipData(ConfigNode weaponNode)
	{
	}

	protected virtual void SaveEquipData(ConfigNode weaponNode)
	{
	}

	private ConfigNode GetWeaponConfigNode(ConfigNode vehicleNode)
	{
		ConfigNode configNode;
		if (vehicleNode.HasNode(shortName))
		{
			configNode = vehicleNode.GetNode(shortName);
		}
		else
		{
			configNode = new ConfigNode(shortName);
			vehicleNode.AddNode(configNode);
		}
		return configNode;
	}

	public virtual float GetEstimatedMass()
	{
		return base.gameObject.GetComponentImplementing<IMassObject>()?.GetMass() ?? 0f;
	}

	public float GetRadarCrossSection()
	{
		if (rcsMasked)
		{
			return 0f;
		}
		return _GetRadarCrossSection();
	}

	protected virtual float _GetRadarCrossSection()
	{
		return baseRadarCrossSection;
	}

	public virtual void OnQuicksaveEquip(ConfigNode eqNode)
	{
		eqNode.SetValue("markedForJettison", markedForJettison);
		eqNode.SetValue("armed", armed);
	}

	public virtual void OnQuickloadEquip(ConfigNode eqNode)
	{
		markedForJettison = eqNode.GetValue<bool>("markedForJettison");
		armed = eqNode.GetValue<bool>("armed");
	}

	public string GetLocalizedDescription()
	{
		if (string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(fullName))
		{
			return string.Empty;
		}
		if (localize)
		{
			return VTLocalizationManager.GetString(fullName + "_description", description, "Item description of: " + fullName);
		}
		return description;
	}

	public string GetLocalizedFullName()
	{
		if (localize)
		{
			return VTLocalizationManager.GetString(fullName + "_fullName", fullName, "Full equip name of: " + fullName);
		}
		return fullName;
	}

	public string GetLocalizedSublabel()
	{
		if (localize)
		{
			string text = SublabelKey();
			if (!string.IsNullOrEmpty(text))
			{
				return VTLocalizationManager.GetString(text, subLabel, "HUD sub-label of an equip");
			}
		}
		return subLabel;
	}

	public virtual void ApplyLocalization()
	{
		if (localize && !string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(fullName))
		{
			VTLocalizationManager.GetString(fullName + "_fullName", fullName, "Full equip name of: " + fullName);
			VTLocalizationManager.GetString(fullName + "_description", description, "Item description of: " + fullName);
			if (!string.IsNullOrEmpty(SublabelKey()))
			{
				VTLocalizationManager.GetString(SublabelKey(), subLabel, "HUD sub-label of an equip");
			}
		}
	}

	private string SublabelKey()
	{
		if (string.IsNullOrEmpty(subLabel))
		{
			return null;
		}
		if (string.IsNullOrEmpty(subLabelKey))
		{
			subLabelKey = $"eqSublabel_{subLabel.Replace('\n', '-').Replace(' ', '-')}";
		}
		return subLabelKey;
	}
}
