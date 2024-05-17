using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VTNetworking;
using VTOLVR.Multiplayer;

public class LoadoutConfigurator : MonoBehaviour, ILocalizationUser
{
	public bool uiOnly;

	public Transform equipRigTf;

	public WeaponManager wm;

	public Transform seatTransform;

	public GameObject midFlightRearmObjects;

	public GameObject denyMessageObject;

	public Text denyMessageText;

	public AudioSource denyAudioSource;

	public AudioClip denySound;

	[HideInInspector]
	public Rigidbody vehicleRb;

	public float equipImpulse = 2f;

	public List<string> availableEquipStrings;

	public List<int> lockedHardpoints = new List<int>();

	private Dictionary<string, EqInfo> unlockedWeaponPrefabs = new Dictionary<string, EqInfo>();

	private Dictionary<string, EqInfo> allWeaponPrefabs = new Dictionary<string, EqInfo>();

	[HideInInspector]
	public HPEquippable[] equips;

	private Transform[] hpTransforms;

	private Coroutine[] attachRoutines;

	private Coroutine[] detachRoutines;

	private GameObject[] detachingObjs;

	public AudioClip attachAudioClip;

	public AudioClip detachAudioClip;

	public ParticleSystem attachPs;

	private FuelTank fuelTank;

	public float ui_maxFuel;

	public HPConfiguratorNode[] hpNodes;

	public HPConfiguratorFullInfo fullInfo;

	private int fullInfoEQIdx;

	public VRTwistKnob fuelKnob;

	public VRTwistKnob[] cmKnobs;

	[HideInInspector]
	public List<Countermeasure> cms;

	private CampaignSave campaignSave;

	private AudioSource[] hpAudioSources;

	private float startingFuel;

	[HideInInspector]
	public bool canArm = true;

	[HideInInspector]
	public bool canRefuel = true;

	private string vehicleConfig_denyBusy = "BUSY: RELOADING";

	private string vehicleConfig_invalidReload = "Full reload will be over budget!";

	[Header("Symmetry")]
	public GameObject symmetryCheckObj;

	public bool symmetryMode;

	private float returnedEquipmentValue;

	private object iwbAttach = new object();

	private object iwbDetach = new object();

	private float uiOnlyFuel = 1f;

	private object iwbFullInfo = new object();

	private Coroutine denyRoutine;

	private bool reloadingAll;

	[Header("Repairing")]
	public GameObject repairObject;

	public Text repairCostText;

	[Header("Night")]
	public GameObject nightObject;

	public float fuel { get; private set; }

	public int activeHardpoint { get; private set; }

	public float totalThrust { get; private set; }

	public event UnityAction<int> OnAttachHPIdx;

	public event UnityAction<int> OnDetachHPIdx;

	public bool TryGetEqInfo(string eqId, out EqInfo info)
	{
		return allWeaponPrefabs.TryGetValue(eqId, out info);
	}

	public bool TryGetSymmetryEquip(string eqFullName, int hpIdx, out EqInfo info)
	{
		foreach (EqInfo value in allWeaponPrefabs.Values)
		{
			if (value.eq.fullName == eqFullName && value.IsCompatibleWithHardpoint(hpIdx))
			{
				info = value;
				return true;
			}
		}
		info = default(EqInfo);
		return false;
	}

	public void ApplyLocalization()
	{
		vehicleConfig_denyBusy = VTLocalizationManager.GetString("vehicleConfig_denyBusy", vehicleConfig_denyBusy, "Denial message for vehicle configurator");
		vehicleConfig_invalidReload = VTLocalizationManager.GetString("vehicleConfig_invalidReload", vehicleConfig_invalidReload, "Denial message for vehicle configurator");
	}

	private void OnUISetCM(int idx, float n)
	{
	}

	public void Initialize(CampaignSave campaignSave, bool useMidflightEquips = false)
	{
		ApplyLocalization();
		this.campaignSave = campaignSave;
		activeHardpoint = -1;
		equips = new HPEquippable[wm.hardpointTransforms.Length];
		if (!uiOnly)
		{
			attachRoutines = new Coroutine[equips.Length];
			detachRoutines = new Coroutine[equips.Length];
			detachingObjs = new GameObject[equips.Length];
			hpTransforms = wm.hardpointTransforms;
		}
		vehicleRb = wm.GetComponentInParent<Rigidbody>();
		fuelTank = wm.GetComponent<FuelTank>();
		if (uiOnly)
		{
			ui_maxFuel = fuelTank.maxFuel;
		}
		if (cmKnobs.Length != 0)
		{
			CountermeasureManager componentInChildren = wm.GetComponentInChildren<CountermeasureManager>();
			int num = 0;
			cms = new List<Countermeasure>();
			foreach (Countermeasure countermeasure in componentInChildren.countermeasures)
			{
				cmKnobs[num].GetComponent<CMConfigurator>().cmIdx = num;
				float num2 = (float)countermeasure.count / (float)countermeasure.maxCount;
				if (!useMidflightEquips)
				{
					num2 = 1f;
				}
				cmKnobs[num].startValue = num2;
				if (!uiOnly)
				{
					cmKnobs[num].OnSetState.AddListener(countermeasure.SetNormalizedCount);
				}
				cmKnobs[num].SetKnobValue(num2);
				cms.Add(countermeasure);
				num++;
			}
		}
		if (useMidflightEquips)
		{
			for (int i = 0; i < equips.Length; i++)
			{
				equips[i] = hpTransforms[i].gameObject.GetComponentInChildrenImplementing<HPEquippable>();
			}
			fuel = fuelTank.fuel;
			startingFuel = fuelTank.fuel;
			fuelKnob.startValue = fuelTank.fuelFraction;
			fuelKnob.SetKnobValue(fuelTank.fuelFraction);
			if ((bool)midFlightRearmObjects)
			{
				midFlightRearmObjects.SetActive(value: true);
			}
		}
		else
		{
			if (uiOnly)
			{
				fuel = campaignSave.currentFuel * ui_maxFuel;
			}
			else
			{
				fuel = campaignSave.currentFuel * fuelTank.maxFuel;
			}
			fuelKnob.startValue = campaignSave.currentFuel;
			fuelKnob.SetKnobValue(campaignSave.currentFuel);
			startingFuel = 0f;
			if ((bool)midFlightRearmObjects)
			{
				midFlightRearmObjects.SetActive(value: false);
			}
		}
		foreach (string availableEquipString in availableEquipStrings)
		{
			string path = PilotSaveManager.currentVehicle.equipsResourcePath + "/" + availableEquipString;
			GameObject gameObject = Resources.Load<GameObject>(path);
			if ((bool)gameObject)
			{
				GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject);
				gameObject2.name = availableEquipString;
				if (IsMultiplayer())
				{
					MissileLauncher[] componentsInChildrenImplementing = gameObject2.GetComponentsInChildrenImplementing<MissileLauncher>(includeInactive: true);
					foreach (MissileLauncher missileLauncher in componentsInChildrenImplementing)
					{
						if (missileLauncher.loadOnStart)
						{
							missileLauncher.LoadAllMissiles();
						}
					}
				}
				gameObject2.SetActive(value: false);
				EqInfo value = new EqInfo(gameObject2, path);
				unlockedWeaponPrefabs.Add(availableEquipString, value);
				if (!uiOnly)
				{
					continue;
				}
				Renderer[] componentsInChildrenImplementing2 = gameObject2.GetComponentsInChildrenImplementing<Renderer>(includeInactive: true);
				foreach (Renderer renderer in componentsInChildrenImplementing2)
				{
					if (renderer.gameObject.layer == 8)
					{
						renderer.gameObject.layer = 27;
					}
				}
			}
			else
			{
				Debug.Log("Available equipment " + availableEquipString + " not found.");
			}
		}
		foreach (GameObject allEquipPrefab in PilotSaveManager.currentVehicle.allEquipPrefabs)
		{
			GameObject gameObject3 = UnityEngine.Object.Instantiate(allEquipPrefab);
			gameObject3.name = allEquipPrefab.name;
			if (IsMultiplayer())
			{
				MissileLauncher[] componentsInChildrenImplementing = gameObject3.GetComponentsInChildrenImplementing<MissileLauncher>(includeInactive: true);
				foreach (MissileLauncher missileLauncher2 in componentsInChildrenImplementing)
				{
					if (missileLauncher2.loadOnStart)
					{
						missileLauncher2.LoadAllMissiles();
					}
				}
			}
			gameObject3.SetActive(value: false);
			EqInfo value2 = new EqInfo(gameObject3, PilotSaveManager.currentVehicle.equipsResourcePath + "/" + gameObject3.name);
			allWeaponPrefabs.Add(gameObject3.name, value2);
		}
		if (!uiOnly)
		{
			hpAudioSources = new AudioSource[wm.hardpointTransforms.Length];
			for (int k = 0; k < hpAudioSources.Length; k++)
			{
				AudioSource audioSource = new GameObject("HPAudio").AddComponent<AudioSource>();
				audioSource.transform.parent = base.transform;
				audioSource.transform.position = wm.hardpointTransforms[k].position;
				audioSource.spatialBlend = 1f;
				audioSource.minDistance = 4f;
				audioSource.maxDistance = 1000f;
				audioSource.dopplerLevel = 0f;
				hpAudioSources[k] = audioSource;
			}
		}
		CalculateTotalThrust();
		UpdateRepairDisplay();
		UpdateNodes();
		if (useMidflightEquips)
		{
			FlightSceneManager.instance.OnExitScene += FlightManager_OnExitScene;
		}
		if ((bool)nightObject)
		{
			nightObject.SetActive(value: false);
			if ((bool)EnvironmentManager.instance && EnvironmentManager.instance.currentEnvironment.ToLower().Contains("night"))
			{
				nightObject.SetActive(value: true);
			}
		}
		UpdateSymmetryUI();
	}

	private void CalculateTotalThrust()
	{
		totalThrust = 0f;
		ModuleEngine[] componentsInChildren = wm.GetComponentsInChildren<ModuleEngine>();
		foreach (ModuleEngine moduleEngine in componentsInChildren)
		{
			if (moduleEngine.useCommonSpecs && (bool)moduleEngine.specs)
			{
				moduleEngine.LoadFromEngineSpecs();
			}
			if (moduleEngine.includeInTWR)
			{
				float num = moduleEngine.maxThrust * moduleEngine.abThrustMult;
				totalThrust += num;
			}
		}
	}

	private void FlightManager_OnExitScene()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void OnDestroy()
	{
		if (unlockedWeaponPrefabs != null)
		{
			foreach (EqInfo value in unlockedWeaponPrefabs.Values)
			{
				if ((bool)value.eqObject)
				{
					UnityEngine.Object.Destroy(value.eqObject);
				}
			}
		}
		if (allWeaponPrefabs != null)
		{
			foreach (EqInfo value2 in allWeaponPrefabs.Values)
			{
				if ((bool)value2.eqObject)
				{
					UnityEngine.Object.Destroy(value2.eqObject);
				}
			}
		}
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= FlightManager_OnExitScene;
		}
	}

	public float GetTotalFlightCost()
	{
		float num = 0f;
		num -= returnedEquipmentValue;
		HPEquippable[] array = equips;
		foreach (HPEquippable hPEquippable in array)
		{
			if (hPEquippable != null && !hPEquippable.wasPurchased)
			{
				num += hPEquippable.GetTotalCost();
			}
		}
		float num2 = Mathf.Max(0f, fuel - startingFuel);
		return num + VTOLVRConstants.FUEL_UNIT_COST * num2;
	}

	public void UpdateNodes()
	{
		if (!uiOnly)
		{
			wm.gameObject.GetComponent<MassUpdater>().UpdateMassObjects();
		}
		for (int i = 0; i < hpNodes.Length; i++)
		{
			hpNodes[i].configurator = this;
			hpNodes[i].UpdateInfo(equips[i], i);
		}
		fullInfo.UpdateUI();
	}

	public void ToggleSymmetry()
	{
		symmetryMode = !symmetryMode;
		UpdateSymmetryUI();
	}

	private void UpdateSymmetryUI()
	{
		symmetryCheckObj.SetActive(symmetryMode);
	}

	public void Attach(string weaponName, int hpIdx)
	{
		if (uiOnly)
		{
			AttachImmediate(weaponName, hpIdx);
			return;
		}
		VehiclePart componentInParent = wm.hardpointTransforms[hpIdx].GetComponentInParent<VehiclePart>();
		if ((!componentInParent || !componentInParent.hasDetached) && !componentInParent.partDied)
		{
			Detach(hpIdx);
			attachRoutines[hpIdx] = StartCoroutine(AttachRoutine(hpIdx, weaponName));
		}
	}

	public void AttachImmediate(string weaponName, int hpIdx)
	{
		DetachImmediate(hpIdx);
		if (allWeaponPrefabs.ContainsKey(weaponName))
		{
			if (uiOnly)
			{
				equips[hpIdx] = allWeaponPrefabs[weaponName].eq;
				equips[hpIdx].OnConfigAttach(this);
			}
			else
			{
				Transform transform = allWeaponPrefabs[weaponName].GetInstantiated().transform;
				equips[hpIdx] = transform.GetComponent<HPEquippable>();
				transform.parent = hpTransforms[hpIdx];
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
				transform.localScale = Vector3.one;
				equips[hpIdx].OnConfigAttach(this);
				if (this.OnAttachHPIdx != null)
				{
					this.OnAttachHPIdx(hpIdx);
				}
			}
		}
		UpdateNodes();
	}

	private InternalWeaponBay GetWeaponBay(int idx)
	{
		if (uiOnly)
		{
			return null;
		}
		for (int i = 0; i < wm.internalWeaponBays.Length; i++)
		{
			InternalWeaponBay internalWeaponBay = wm.internalWeaponBays[i];
			if (internalWeaponBay.hardpointIdx == idx)
			{
				return internalWeaponBay;
			}
		}
		return null;
	}

	public void DetachImmediate(int hpIdx)
	{
		if (equips[hpIdx] != null)
		{
			equips[hpIdx].OnConfigDetach(this);
			if (!uiOnly)
			{
				if (this.OnDetachHPIdx != null)
				{
					this.OnDetachHPIdx(hpIdx);
				}
				if (attachRoutines[hpIdx] != null)
				{
					StopCoroutine(attachRoutines[hpIdx]);
				}
				if (detachRoutines[hpIdx] != null)
				{
					StopCoroutine(detachRoutines[hpIdx]);
					DestroyEquip(detachingObjs[hpIdx]);
				}
				DestroyEquip(equips[hpIdx].gameObject);
			}
			else
			{
				equips[hpIdx] = null;
			}
		}
		UpdateNodes();
	}

	public void Detach(int hpIdx)
	{
		if (uiOnly)
		{
			DetachImmediate(hpIdx);
			return;
		}
		HPEquippable hPEquippable = equips[hpIdx];
		if (hPEquippable != null)
		{
			hPEquippable.OnConfigDetach(this);
			if (hPEquippable.wasPurchased)
			{
				returnedEquipmentValue += hPEquippable.GetTotalCost();
			}
			if (this.OnDetachHPIdx != null)
			{
				this.OnDetachHPIdx(hpIdx);
			}
			if (attachRoutines[hpIdx] != null)
			{
				StopCoroutine(attachRoutines[hpIdx]);
			}
			if (detachRoutines[hpIdx] != null)
			{
				StopCoroutine(detachRoutines[hpIdx]);
				DestroyEquip(detachingObjs[hpIdx]);
				UpdateNodes();
			}
			detachRoutines[hpIdx] = StartCoroutine(DetachRoutine(hpIdx));
		}
	}

	private IEnumerator AttachRoutine(int hpIdx, string weaponName)
	{
		if (detachRoutines[hpIdx] != null)
		{
			yield return detachRoutines[hpIdx];
		}
		if (!allWeaponPrefabs.ContainsKey(weaponName))
		{
			UpdateNodes();
			yield break;
		}
		GameObject instantiated = allWeaponPrefabs[weaponName].GetInstantiated();
		if (IsMultiplayer())
		{
			MissileLauncher[] componentsInChildrenImplementing = instantiated.GetComponentsInChildrenImplementing<MissileLauncher>(includeInactive: true);
			foreach (MissileLauncher missileLauncher in componentsInChildrenImplementing)
			{
				if (missileLauncher.loadOnStart)
				{
					missileLauncher.LoadAllMissiles();
				}
			}
		}
		Transform weaponTf = instantiated.transform;
		Transform hpTf = hpTransforms[hpIdx];
		InternalWeaponBay iwb = GetWeaponBay(hpIdx);
		if ((bool)iwb)
		{
			iwb.RegisterOpenReq(iwbAttach);
		}
		equips[hpIdx] = weaponTf.GetComponent<HPEquippable>();
		equips[hpIdx].OnConfigAttach(this);
		if (this.OnAttachHPIdx != null)
		{
			this.OnAttachHPIdx(hpIdx);
		}
		weaponTf.rotation = hpTf.rotation;
		Vector3 localPos = new Vector3(0f, -4f, 0f);
		weaponTf.position = hpTf.TransformPoint(localPos);
		UpdateNodes();
		Vector3 tgt = new Vector3(0f, 0f, 0.5f);
		if (hpIdx == 0 || (bool)iwb)
		{
			tgt = Vector3.zero;
		}
		while ((localPos - tgt).sqrMagnitude > 0.01f)
		{
			localPos = Vector3.Lerp(localPos, tgt, 5f * Time.deltaTime);
			weaponTf.position = hpTf.TransformPoint(localPos);
			yield return null;
		}
		weaponTf.parent = hpTf;
		weaponTf.localPosition = tgt;
		weaponTf.localRotation = Quaternion.identity;
		vehicleRb.AddForceAtPosition(Vector3.up * equipImpulse, wm.hardpointTransforms[hpIdx].position, ForceMode.Impulse);
		hpAudioSources[hpIdx].PlayOneShot(attachAudioClip);
		attachPs.transform.position = hpTf.position;
		attachPs.FireBurst();
		yield return new WaitForSeconds(0.2f);
		while (weaponTf.localPosition.sqrMagnitude > 0.001f)
		{
			weaponTf.localPosition = Vector3.MoveTowards(weaponTf.localPosition, Vector3.zero, 4f * Time.deltaTime);
			yield return null;
		}
		if ((bool)iwb)
		{
			iwb.UnregisterOpenReq(iwbAttach);
		}
		weaponTf.localPosition = Vector3.zero;
		UpdateNodes();
		attachRoutines[hpIdx] = null;
	}

	private IEnumerator DetachRoutine(int hpIdx)
	{
		Transform weaponTf = equips[hpIdx].transform;
		detachingObjs[hpIdx] = equips[hpIdx].gameObject;
		weaponTf.parent = null;
		InternalWeaponBay iwb = GetWeaponBay(hpIdx);
		if ((bool)iwb)
		{
			iwb.RegisterOpenReq(iwbDetach);
		}
		UpdateNodes();
		hpAudioSources[hpIdx].PlayOneShot(detachAudioClip);
		attachPs.transform.position = hpTransforms[hpIdx].position;
		attachPs.FireBurst();
		vehicleRb.AddForceAtPosition(Vector3.down * equipImpulse, wm.hardpointTransforms[hpIdx].position, ForceMode.Impulse);
		Vector3 tgt = hpTransforms[hpIdx].position + new Vector3(0f, -5f, 0f);
		while ((bool)weaponTf && (weaponTf.position - tgt).sqrMagnitude > 0.01f)
		{
			weaponTf.position = Vector3.Lerp(weaponTf.position, tgt, 5f * Time.deltaTime);
			yield return null;
		}
		equips[hpIdx] = null;
		if ((bool)weaponTf)
		{
			DestroyEquip(weaponTf.gameObject);
		}
		UpdateNodes();
		if ((bool)iwb)
		{
			iwb.UnregisterOpenReq(iwbDetach);
		}
	}

	private bool IsMultiplayer()
	{
		return VTOLMPUtils.IsMultiplayer();
	}

	private void DestroyEquip(GameObject equipObj)
	{
		if (!equipObj || uiOnly)
		{
			return;
		}
		if (IsMultiplayer())
		{
			VTNetEntity component = equipObj.GetComponent<VTNetEntity>();
			if ((bool)component && component.hasRegistered)
			{
				equipObj.transform.parent = null;
				equipObj.SetActive(value: false);
				VTNetworkManager.NetDestroyObject(equipObj);
			}
			else
			{
				UnityEngine.Object.Destroy(equipObj);
			}
		}
		else
		{
			UnityEngine.Object.Destroy(equipObj);
		}
	}

	public Loadout SaveConfig()
	{
		Loadout loadout = new Loadout();
		loadout.normalizedFuel = fuel / (uiOnly ? ui_maxFuel : fuelTank.maxFuel);
		loadout.hpLoadout = new string[equips.Length];
		if (campaignSave.currentWeapons.Length != equips.Length)
		{
			campaignSave.currentWeapons = new string[equips.Length];
		}
		for (int i = 0; i < equips.Length; i++)
		{
			if (equips[i] != null)
			{
				string text = equips[i].gameObject.name;
				loadout.hpLoadout[i] = text;
				if (campaignSave != null)
				{
					campaignSave.currentWeapons[i] = text;
				}
			}
			else if (campaignSave != null)
			{
				campaignSave.currentWeapons[i] = string.Empty;
			}
		}
		loadout.cmLoadout = new int[cms.Count];
		for (int j = 0; j < cms.Count; j++)
		{
			loadout.cmLoadout[j] = Mathf.RoundToInt(cmKnobs[j].currentValue * (float)cms[j].maxCount);
		}
		if (campaignSave != null)
		{
			campaignSave.currentFuel = fuel / (uiOnly ? ui_maxFuel : fuelTank.maxFuel);
		}
		VehicleEquipper.loadout = loadout;
		return loadout;
	}

	public void SetNormFuel(float t)
	{
		if (uiOnly)
		{
			uiOnlyFuel = t;
			fuel = uiOnlyFuel * ui_maxFuel;
		}
		else
		{
			fuelTank.SetNormFuel(t);
			fuel = fuelTank.fuel;
		}
		UpdateNodes();
	}

	public void SetActiveHardpoint(int idx)
	{
		if (!canArm)
		{
			return;
		}
		activeHardpoint = idx;
		if (activeHardpoint >= 0)
		{
			activeHardpoint = idx;
			List<HPEquippable> list = new List<HPEquippable>();
			bool flag = false;
			if (!uiOnly)
			{
				VehiclePart componentInParent = wm.hardpointTransforms[idx].GetComponentInParent<VehiclePart>();
				if ((bool)componentInParent && (componentInParent.partDied || componentInParent.hasDetached || componentInParent.health.normalizedHealth <= 0f))
				{
					flag = true;
				}
			}
			if (!flag)
			{
				if (lockedHardpoints.Contains(idx) && equips[idx] != null)
				{
					HPEquippable eq = allWeaponPrefabs[equips[idx].name].eq;
					if (!list.Contains(eq))
					{
						list.Add(eq);
					}
				}
				else
				{
					foreach (EqInfo value in unlockedWeaponPrefabs.Values)
					{
						if (value.IsCompatibleWithHardpoint(activeHardpoint))
						{
							list.Add(value.eq);
						}
					}
				}
			}
			fullInfo.OpenInfo(idx, list.ToArray());
		}
		UpdateNodes();
	}

	public void FullInfoOpenBay(int idx)
	{
		InternalWeaponBay weaponBay = GetWeaponBay(idx);
		if ((bool)weaponBay)
		{
			weaponBay.RegisterOpenReq(iwbFullInfo);
		}
	}

	public void FullInfoCloseBay(int idx)
	{
		InternalWeaponBay weaponBay = GetWeaponBay(idx);
		if ((bool)weaponBay)
		{
			weaponBay.UnregisterOpenReq(iwbFullInfo);
		}
	}

	public void EndActiveRearmingPoint()
	{
		if ((bool)ReArmingPoint.active)
		{
			if (reloadingAll)
			{
				DenyLaunch(vehicleConfig_denyBusy);
			}
			else if (ReArmingPoint.active.EndReArm())
			{
				SaveConfig();
				for (int i = 0; i < wm.equipCount; i++)
				{
					InternalWeaponBay weaponBay = GetWeaponBay(i);
					if ((bool)weaponBay)
					{
						weaponBay.UnregisterOpenReq(iwbAttach);
						weaponBay.UnregisterOpenReq(iwbDetach);
					}
				}
				wm.ReattachWeapons();
			}
			else
			{
				Debug.Log("Failed to end rearming.");
			}
		}
		else
		{
			Debug.Log("No active rearming point");
		}
	}

	public void EndBriefingConfiguration()
	{
		VTOLMPBriefingRoom.instance.CloseEquipConfig();
	}

	public void DenyLaunch(string message)
	{
		if (denyRoutine != null)
		{
			StopCoroutine(denyRoutine);
		}
		denyAudioSource.PlayOneShot(denySound);
		denyMessageText.text = message;
		denyRoutine = StartCoroutine(DenyRoutine());
	}

	private IEnumerator DenyRoutine()
	{
		denyMessageObject.SetActive(value: true);
		yield return new WaitForSeconds(4f);
		denyMessageObject.SetActive(value: false);
	}

	private bool CanEquipIdx(int hpIdx)
	{
		VehiclePart componentInParent = wm.hardpointTransforms[hpIdx].GetComponentInParent<VehiclePart>();
		if ((bool)componentInParent && componentInParent.hasDetached)
		{
			return false;
		}
		return true;
	}

	public void ReloadAll()
	{
		if (!PilotSaveManager.currentScenario.isTraining)
		{
			float num = 0f;
			float num2 = PilotSaveManager.currentScenario.totalBudget - PilotSaveManager.currentScenario.initialSpending - PilotSaveManager.currentScenario.inFlightSpending;
			for (int i = 0; i < equips.Length; i++)
			{
				if ((bool)equips[i] && equips[i].GetCount() < equips[i].GetMaxCount() && CanEquipIdx(equips[i].hardpointIdx))
				{
					string key = equips[i].gameObject.name;
					num += allWeaponPrefabs[key].eq.GetTotalCost() - equips[i].GetTotalCost();
					if (num > num2)
					{
						DenyLaunch(vehicleConfig_invalidReload);
						return;
					}
				}
			}
		}
		StartCoroutine(ReloadAllRoutine());
	}

	[ContextMenu("Test Repair")]
	public void TestRepair()
	{
		float num = PilotSaveManager.currentScenario.totalBudget - PilotSaveManager.currentScenario.initialSpending - PilotSaveManager.currentScenario.inFlightSpending;
		float repairCost = GetRepairCost();
		if (!PilotSaveManager.currentScenario.isTraining && repairCost <= num)
		{
			List<Health> list = new List<Health>();
			Health[] componentsInChildren = wm.GetComponentsInChildren<Health>();
			foreach (Health health in componentsInChildren)
			{
				if (health.normalizedHealth < 1f)
				{
					list.Add(health);
				}
			}
			VehiclePart component = wm.GetComponent<VehiclePart>();
			if ((bool)component)
			{
				component.Repair();
			}
			CalculateTotalThrust();
			UpdateRepairDisplay();
			PilotSaveManager.currentScenario.inFlightSpending += repairCost;
			fullInfo.UpdateUI();
			hpAudioSources[0].PlayOneShot(attachAudioClip);
			{
				foreach (Health item in list)
				{
					if ((bool)item)
					{
						attachPs.transform.position = item.transform.position;
						attachPs.FireBurst();
					}
				}
				return;
			}
		}
		DenyLaunch(VTLStaticStrings.vehicleConfig_repairOverBudget);
	}

	private void UpdateRepairDisplay()
	{
		float repairCost = GetRepairCost();
		if (repairCost > 0f)
		{
			repairObject.SetActive(value: true);
			if ((bool)repairCostText)
			{
				repairCostText.text = $"${Mathf.CeilToInt(repairCost)}";
			}
		}
		else
		{
			repairObject.SetActive(value: false);
		}
	}

	public float GetRepairCost()
	{
		if (uiOnly)
		{
			return 0f;
		}
		return RecurrGetCost(wm.GetComponentInChildren<VehiclePart>()) * VTOLVRConstants.REPAIR_UNIT_COST;
	}

	private float RecurrGetCost(VehiclePart p)
	{
		float num = p.health.maxHealth - p.health.currentHealth;
		foreach (VehiclePart child in p.children)
		{
			if ((bool)child)
			{
				num += RecurrGetCost(child);
			}
		}
		return num;
	}

	private IEnumerator ReloadAllRoutine()
	{
		reloadingAll = true;
		for (int i = 0; i < equips.Length; i++)
		{
			if ((bool)equips[i] && equips[i].GetCount() < equips[i].GetMaxCount() && CanEquipIdx(equips[i].hardpointIdx))
			{
				string weaponName = equips[i].gameObject.name;
				Attach(weaponName, i);
				yield return new WaitForSeconds(0.5f);
			}
		}
		while (reloadingAll)
		{
			reloadingAll = false;
			for (int j = 0; j < attachRoutines.Length; j++)
			{
				if (reloadingAll)
				{
					break;
				}
				if (attachRoutines[j] != null)
				{
					reloadingAll = true;
				}
			}
			yield return null;
		}
	}

	public static int EquipCompatibilityMask(HPEquippable equip)
	{
		int num = 0;
		string[] array = equip.allowedHardpoints.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			int num2 = int.Parse(array[i]);
			num |= 1 << num2;
		}
		return num;
	}

	public void ExitMission()
	{
		FlightSceneManager.instance.ReturnToBriefingOrExitScene();
	}

	public void SaveLoadout(string name)
	{
		VehicleSave lastVehicleSave = PilotSaveManager.current.lastVehicleSave;
		if (lastVehicleSave != null)
		{
			ConfigNode configNode = new ConfigNode("SavedLoadout");
			configNode.SetValue("name", name);
			configNode.SetValue("normFuel", uiOnly ? uiOnlyFuel : fuelTank.fuelFraction);
			for (int i = 0; i < equips.Length; i++)
			{
				HPEquippable hPEquippable = equips[i];
				if (hPEquippable != null)
				{
					configNode.SetValue("eq" + i, hPEquippable.gameObject.name);
				}
			}
			if (lastVehicleSave.savedLoadouts.ContainsKey(name))
			{
				lastVehicleSave.savedLoadouts[name] = configNode;
			}
			else
			{
				lastVehicleSave.savedLoadouts.Add(name, configNode);
			}
			PilotSaveManager.SavePilotsToFile();
		}
		else
		{
			Debug.LogError("Tried to save loadout but lastVehicleSave is null!");
		}
	}

	public void LoadLoadout(string name)
	{
		VehicleSave lastVehicleSave = PilotSaveManager.current.lastVehicleSave;
		if (lastVehicleSave != null)
		{
			if (lastVehicleSave.savedLoadouts.TryGetValue(name, out var value))
			{
				fuelKnob.SetKnobValue(value.GetValue<float>("normFuel"));
				for (int i = 0; i < equips.Length; i++)
				{
					if (lockedHardpoints.Contains(i))
					{
						continue;
					}
					if (value.HasValue("eq" + i))
					{
						string value2 = value.GetValue("eq" + i);
						if (availableEquipStrings.Contains(value2))
						{
							if (equips[i] == null || value2 != equips[i].gameObject.name)
							{
								Attach(value2, i);
							}
						}
						else
						{
							Detach(i);
						}
					}
					else
					{
						Detach(i);
					}
				}
			}
			else
			{
				Debug.Log("Tried to load loadout'" + name + "' but it doesn't exist!");
			}
		}
		else
		{
			Debug.LogError("Tried to load loadout but lastVehicleSave is null!");
		}
	}

	public void DeleteLoadout(string saveName)
	{
		PilotSaveManager.current.lastVehicleSave?.savedLoadouts.Remove(saveName);
	}
}
