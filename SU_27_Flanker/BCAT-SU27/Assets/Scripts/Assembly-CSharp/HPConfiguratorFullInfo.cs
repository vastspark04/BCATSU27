using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class HPConfiguratorFullInfo : MonoBehaviour, ILocalizationUser
{
	public class EquipGroup
	{
		public string shortName;

		public List<HPEquippable> variants;

		public int variantIdx;
	}

	public GameObject displayObject;

	public Text titleText;

	public Text descriptionText;

	public Text countWeightText;

	public GameObject countButtons;

	public Text costText;

	public GameObject hpDisplayObject;

	public GameObject systemsDisplayObject;

	public LoadoutConfigurator configurator;

	public Text fuelText;

	public Transform fuelBarTf;

	public Text massText;

	public Text totalCostText;

	public Text budgetText;

	private int hpIdx;

	private HPEquippable[] availableEquips;

	private List<EquipGroup> equipGroups;

	private int equipGroupIdx = -1;

	public LineRenderer equipLine;

	public Text twrText;

	public Transform holoCamRotator;

	private Camera holoCam;

	private GameObject currentlyViewedEquip;

	public GameObject equipListTemplate;

	public ScrollRect equipListScroll;

	private List<GameObject> equipListItems = new List<GameObject>();

	private string s_noEquips;

	private string s_noEquipsDescription;

	private string s_equipCost;

	private string s_count;

	private string s_equipMass;

	private string s_fuel;

	private Vector3 lineEndPoint;

	private Vector3 lineOffsetPoint;

	public int currIdx { get; private set; }

	public int equippedIdx { get; private set; }

	public HPEquippable GetEquip(int idx)
	{
		return availableEquips[idx];
	}

	public void ApplyLocalization()
	{
		s_noEquips = VTLocalizationManager.GetString("equipConfig_noEquips", "No equipment.", "A label shown in the vehicle configurator when there are no available equipment.");
		s_noEquipsDescription = VTLocalizationManager.GetString("equipConfig_noEquipsDescription", "Equipment for this hardpoint is not yet available.", "A more descriptive label shown when there is no equipment available.");
		s_equipCost = VTLocalizationManager.GetString("vehicleConfig_equipCost", "Cost:");
		s_count = VTLocalizationManager.GetString("vehicleConfig_count", "Count:");
		s_equipMass = VTLocalizationManager.GetString("vehicleConfig_equipMass", "Mass:");
		s_fuel = VTLocalizationManager.GetString("vehicleConfig_fuel", "FUEL");
	}

	private void Awake()
	{
		ApplyLocalization();
	}

	private void Start()
	{
		displayObject.SetActive(value: false);
		hpDisplayObject.SetActive(value: true);
		systemsDisplayObject.SetActive(value: true);
		if ((bool)equipLine)
		{
			equipLine.positionCount = 4;
			equipLine.SetPositions(new Vector3[4]
			{
				Vector3.zero,
				Vector3.zero,
				Vector3.zero,
				Vector3.zero
			});
			lineEndPoint = Vector3.zero;
			lineOffsetPoint = Vector3.zero;
		}
		holoCam = holoCamRotator.GetComponentInChildren<Camera>(includeInactive: true);
	}

	private void Update()
	{
		UpdateEquipLine();
		UpdateHoloView();
	}

	private void UpdateHoloView()
	{
		if (displayObject.activeInHierarchy)
		{
			holoCamRotator.rotation = Quaternion.AngleAxis(45f * Time.deltaTime, Vector3.up) * holoCamRotator.rotation;
		}
	}

	public void OpenInfo(int hpIdx, HPEquippable[] equips)
	{
		displayObject.SetActive(value: true);
		hpDisplayObject.SetActive(value: false);
		systemsDisplayObject.SetActive(value: false);
		this.hpIdx = hpIdx;
		currIdx = 0;
		equippedIdx = -1;
		availableEquips = equips;
		configurator.FullInfoOpenBay(hpIdx);
		if (configurator.equips[hpIdx] != null)
		{
			for (int i = 0; i < equips.Length; i++)
			{
				if (equips[i].gameObject.name == configurator.equips[hpIdx].gameObject.name)
				{
					currIdx = i;
					equippedIdx = i;
				}
			}
		}
		equipGroups = new List<EquipGroup>();
		equipGroupIdx = 0;
		foreach (HPEquippable hPEquippable in equips)
		{
			bool flag = false;
			EquipGroup equipGroup = null;
			int num = 0;
			foreach (EquipGroup equipGroup3 in equipGroups)
			{
				if (equipGroup3.shortName == hPEquippable.shortName)
				{
					equipGroup3.variants.Add(hPEquippable);
					flag = true;
					equipGroup = equipGroup3;
					break;
				}
				num++;
			}
			if (!flag)
			{
				EquipGroup equipGroup2 = new EquipGroup();
				equipGroup2.shortName = hPEquippable.shortName;
				equipGroup2.variants = new List<HPEquippable>();
				equipGroup2.variants.Add(hPEquippable);
				equipGroup = equipGroup2;
				equipGroups.Add(equipGroup2);
			}
			if (configurator.equips[hpIdx] != null && hPEquippable.gameObject.name == configurator.equips[hpIdx].gameObject.name)
			{
				equipGroup.variantIdx = equipGroup.variants.Count - 1;
				equipGroupIdx = num;
			}
		}
		SetupEquipList();
		UpdateUI();
	}

	private void SetupEquipList()
	{
		equipListTemplate.SetActive(value: false);
		if (configurator.lockedHardpoints.Contains(hpIdx))
		{
			return;
		}
		VehiclePart componentInParent = configurator.wm.hardpointTransforms[hpIdx].GetComponentInParent<VehiclePart>();
		if ((!componentInParent || (!componentInParent.hasDetached && !(componentInParent.health.normalizedHealth <= 0f))) && equipGroups.Count != 0)
		{
			float num = 0f;
			for (int i = 0; i < equipGroups.Count; i++)
			{
				GameObject gameObject = Object.Instantiate(equipListTemplate);
				gameObject.transform.SetParent(equipListTemplate.transform.parent);
				gameObject.transform.localPosition = equipListTemplate.transform.localPosition;
				gameObject.transform.localRotation = equipListTemplate.transform.localRotation;
				gameObject.transform.localScale = equipListTemplate.transform.localScale;
				EquipListItemUI component = gameObject.GetComponent<EquipListItemUI>();
				component.SetupItem(i, equipGroups[i].shortName, this);
				float num2 = ((RectTransform)gameObject.transform).rect.height + component.margin;
				num += num2;
				equipListItems.Add(gameObject);
			}
			if ((bool)equipListScroll)
			{
				equipListScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
				equipListScroll.verticalNormalizedPosition = 1f;
			}
		}
	}

	private void DestroyEquipList()
	{
		foreach (GameObject equipListItem in equipListItems)
		{
			Object.Destroy(equipListItem);
		}
		equipListItems.Clear();
	}

	public void CloseInfo()
	{
		displayObject.SetActive(value: false);
		hpDisplayObject.SetActive(value: true);
		systemsDisplayObject.SetActive(value: true);
		if ((bool)equipLine)
		{
			equipLine.SetPositions(new Vector3[4]
			{
				Vector3.zero,
				Vector3.zero,
				Vector3.zero,
				Vector3.zero
			});
			lineEndPoint = Vector3.zero;
			lineOffsetPoint = Vector3.zero;
		}
		configurator.SetActiveHardpoint(-1);
		configurator.FullInfoCloseBay(hpIdx);
		DestroyEquipList();
	}

	private float GetVehicleMass()
	{
		if (configurator.uiOnly)
		{
			float baseMass = configurator.wm.GetComponent<MassUpdater>().baseMass;
			FuelTank component = configurator.wm.GetComponent<FuelTank>();
			float num = component.baseMass + configurator.fuelKnob.currentValue * configurator.ui_maxFuel * component.fuelDensity;
			baseMass += num;
			HPEquippable[] equips = configurator.equips;
			foreach (HPEquippable hPEquippable in equips)
			{
				if ((bool)hPEquippable && configurator.TryGetEqInfo(hPEquippable.name, out var info))
				{
					float estimatedMass = info.eq.GetEstimatedMass();
					baseMass += estimatedMass;
				}
			}
			return baseMass;
		}
		return configurator.vehicleRb.mass;
	}

	public void UpdateUI()
	{
		float num = configurator.totalThrust / (GetVehicleMass() * 9.81f);
		twrText.text = "TWR: " + num.ToString("0.00");
		if (num > 1.15f)
		{
			twrText.color = Color.green;
		}
		else if (num > 1f)
		{
			twrText.color = Color.yellow;
		}
		else
		{
			twrText.color = Color.red;
		}
		float num2 = GetVehicleMass() * 1000f;
		string text = $"{num2:n0}" + " kg";
		massText.text = text;
		if (configurator.activeHardpoint >= 0)
		{
			if (currIdx == equippedIdx)
			{
				titleText.color = Color.green;
			}
			else
			{
				titleText.color = Color.white;
			}
			if (availableEquips.Length != 0)
			{
				titleText.text = $"E{hpIdx}: {availableEquips[currIdx].GetLocalizedFullName()}";
				descriptionText.text = availableEquips[currIdx].GetLocalizedDescription();
				ViewEquip(currIdx);
				UpdateCountWeight(availableEquips[currIdx]);
				UpdateCostText(availableEquips[currIdx]);
			}
			else
			{
				titleText.text = s_noEquips;
				descriptionText.text = s_noEquipsDescription;
				countWeightText.enabled = false;
				costText.enabled = false;
				countButtons.SetActive(value: false);
				if ((bool)currentlyViewedEquip)
				{
					currentlyViewedEquip.SetActive(value: false);
				}
			}
		}
		if (PilotSaveManager.currentScenario != null)
		{
			if (PilotSaveManager.currentScenario.isTraining)
			{
				totalCostText.enabled = false;
				budgetText.enabled = false;
				return;
			}
			totalCostText.enabled = true;
			budgetText.enabled = true;
			float num3 = PilotSaveManager.currentScenario.totalBudget - PilotSaveManager.currentScenario.initialSpending - PilotSaveManager.currentScenario.inFlightSpending;
			if (configurator.uiOnly)
			{
				num3 = VTOLMPSceneManager.instance.GetTotalBudget();
			}
			float totalFlightCost = configurator.GetTotalFlightCost();
			totalCostText.text = "$" + Mathf.RoundToInt(totalFlightCost);
			if (totalFlightCost > num3)
			{
				totalCostText.color = Color.red;
			}
			else if (totalFlightCost < 0f)
			{
				totalCostText.color = Color.green;
			}
			else
			{
				totalCostText.color = Color.white;
			}
			budgetText.text = "$" + Mathf.RoundToInt(num3);
		}
		else
		{
			totalCostText.enabled = false;
			budgetText.enabled = false;
		}
	}

	private void UpdateEquipLine()
	{
		if ((bool)equipLine)
		{
			if (displayObject.activeInHierarchy)
			{
				equipLine.enabled = true;
				equipLine.positionCount = 4;
				equipLine.SetPosition(0, Vector3.zero);
				equipLine.SetPosition(1, new Vector3(0f, 0f, 0.5f));
				Vector3 b = equipLine.transform.InverseTransformPoint(configurator.wm.hardpointTransforms[hpIdx].position);
				lineEndPoint = Vector3.Lerp(lineEndPoint, b, 15f * Time.deltaTime);
				equipLine.SetPosition(3, lineEndPoint);
				Vector3 b2 = lineEndPoint + equipLine.transform.InverseTransformDirection(Vector3.down);
				lineOffsetPoint = Vector3.Lerp(lineOffsetPoint, b2, 10f * Time.deltaTime);
				equipLine.SetPosition(2, lineOffsetPoint);
			}
			else
			{
				equipLine.enabled = false;
			}
		}
	}

	public void EquipButton()
	{
		if (!configurator.lockedHardpoints.Contains(hpIdx) && availableEquips.Length >= 1)
		{
			string weaponName = availableEquips[currIdx].gameObject.name;
			configurator.Attach(weaponName, hpIdx);
			equippedIdx = currIdx;
			if (configurator.symmetryMode)
			{
				AttachSymmetry(weaponName, hpIdx, availableEquips[currIdx]);
			}
			UpdateUI();
		}
	}

	private void AttachSymmetry(string weaponName, int hpIdx, HPEquippable eqPrefab)
	{
		if (configurator.wm.symmetryIndices != null && hpIdx < configurator.wm.symmetryIndices.Length)
		{
			int num = configurator.wm.symmetryIndices[hpIdx];
			if (num >= 0 && !configurator.lockedHardpoints.Contains(num) && configurator.TryGetSymmetryEquip(eqPrefab.fullName, num, out var info))
			{
				configurator.Attach(info.eqObject.name, num);
			}
		}
	}

	public void ItemListButton(int idx)
	{
		equipGroupIdx = idx;
		currIdx = AvailableIdxFromEquip(equipGroups[equipGroupIdx].variants[0]);
		SetIdxToCurrentCount();
		UpdateUI();
	}

	public void DetachButton()
	{
		if (!configurator.lockedHardpoints.Contains(hpIdx))
		{
			configurator.Detach(hpIdx);
			if (configurator.symmetryMode)
			{
				DetachSymmetry(hpIdx);
			}
			equippedIdx = -1;
			UpdateUI();
		}
	}

	private void DetachSymmetry(int hpIdx)
	{
		if (configurator.wm.symmetryIndices != null && hpIdx < configurator.wm.symmetryIndices.Length)
		{
			int num = configurator.wm.symmetryIndices[hpIdx];
			if (num >= 0 && !configurator.lockedHardpoints.Contains(num))
			{
				configurator.Detach(num);
			}
		}
	}

	public void NextButton()
	{
		if (!configurator.lockedHardpoints.Contains(hpIdx) && availableEquips.Length >= 1)
		{
			equipGroupIdx = (equipGroupIdx + 1) % equipGroups.Count;
			currIdx = AvailableIdxFromEquip(equipGroups[equipGroupIdx].variants[0]);
			SetIdxToCurrentCount();
			UpdateUI();
		}
	}

	public void PrevButton()
	{
		if (!configurator.lockedHardpoints.Contains(hpIdx) && availableEquips.Length >= 1)
		{
			equipGroupIdx--;
			if (equipGroupIdx < 0)
			{
				equipGroupIdx = equipGroups.Count - 1;
			}
			currIdx = AvailableIdxFromEquip(equipGroups[equipGroupIdx].variants[0]);
			SetIdxToCurrentCount();
			UpdateUI();
		}
	}

	public void SetIdxToCurrentCount()
	{
		if (equippedIdx >= 0 && availableEquips[currIdx].shortName == availableEquips[equippedIdx].shortName)
		{
			currIdx = equippedIdx;
			for (int i = 0; i < equipGroups[equipGroupIdx].variants.Count; i++)
			{
				if (equipGroups[equipGroupIdx].variants[i].gameObject.name == availableEquips[equippedIdx].gameObject.name)
				{
					equipGroups[equipGroupIdx].variantIdx = i;
					break;
				}
			}
		}
		else
		{
			currIdx = AvailableIdxFromEquip(equipGroups[equipGroupIdx].variants[0]);
		}
	}

	private void ViewEquip(int idx)
	{
		if ((bool)currentlyViewedEquip)
		{
			currentlyViewedEquip.SetActive(value: false);
		}
		currentlyViewedEquip = availableEquips[currIdx].gameObject;
		currentlyViewedEquip.SetActive(value: true);
		currentlyViewedEquip.transform.position = Vector3.zero;
		Bounds bounds = default(Bounds);
		MeshRenderer[] componentsInChildren = currentlyViewedEquip.GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			bounds.Encapsulate(meshRenderer.bounds);
		}
		Vector3 vector = currentlyViewedEquip.transform.position - bounds.center;
		currentlyViewedEquip.transform.position = holoCamRotator.position + vector;
		currentlyViewedEquip.transform.parent = holoCamRotator.parent;
		float num = Mathf.Atan(bounds.extents.magnitude / 7f) * 57.29578f;
		holoCam.enabled = true;
		holoCam.fieldOfView = num * 2f;
	}

	public void SetNormFuel(float f)
	{
		if (configurator.canRefuel)
		{
			f = Mathf.Clamp(f, 0.1f, 1f);
			fuelBarTf.localScale = new Vector3(1f, f, 1f);
			configurator.SetNormFuel(f);
			float fuel = configurator.fuel;
			fuelText.text = $"{s_fuel}\n{Mathf.Round(fuel)} L";
			UpdateUI();
		}
	}

	private void UpdateCountWeight(HPEquippable eq)
	{
		countWeightText.enabled = true;
		int count = eq.GetCount();
		float mass = eq.gameObject.GetComponentImplementing<IMassObject>().GetMass();
		mass = Mathf.Round(mass * 1000f);
		string text = $"{s_count} {count}\n{s_equipMass} {mass} kg";
		countWeightText.text = text;
		if (equipGroups[equipGroupIdx].variants.Count > 1)
		{
			countButtons.SetActive(value: true);
		}
		else
		{
			countButtons.SetActive(value: false);
		}
	}

	private void UpdateCostText(HPEquippable eq)
	{
		costText.enabled = true;
		costText.text = string.Format("{0} ${1}", s_equipCost, eq.GetTotalCost().ToString("0"));
	}

	public void NextCount()
	{
		equipGroups[equipGroupIdx].variantIdx = (equipGroups[equipGroupIdx].variantIdx + 1) % equipGroups[equipGroupIdx].variants.Count;
		HPEquippable eq = equipGroups[equipGroupIdx].variants[equipGroups[equipGroupIdx].variantIdx];
		currIdx = AvailableIdxFromEquip(eq);
		UpdateUI();
	}

	public void PrevCount()
	{
		equipGroups[equipGroupIdx].variantIdx--;
		if (equipGroups[equipGroupIdx].variantIdx < 0)
		{
			equipGroups[equipGroupIdx].variantIdx = equipGroups[equipGroupIdx].variants.Count - 1;
		}
		HPEquippable eq = equipGroups[equipGroupIdx].variants[equipGroups[equipGroupIdx].variantIdx];
		currIdx = AvailableIdxFromEquip(eq);
		UpdateUI();
	}

	private int AvailableIdxFromEquip(HPEquippable eq)
	{
		for (int i = 0; i < availableEquips.Length; i++)
		{
			if (availableEquips[i] == eq)
			{
				return i;
			}
		}
		return -1;
	}
}
