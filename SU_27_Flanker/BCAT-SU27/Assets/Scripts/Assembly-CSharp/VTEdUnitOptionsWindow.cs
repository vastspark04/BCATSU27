using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdUnitOptionsWindow : MonoBehaviour
{
	public class AltSpawnsListItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public VTEdUnitOptionsWindow optionsWindow;

		public int altSpawnIdx;

		public void OnPointerClick(PointerEventData eventData)
		{
			optionsWindow.SelectAltSpawn(altSpawnIdx);
		}
	}

	public VTEdUnitsWindow unitsWindow;

	private UnitSpawner uSpawner;

	private UnitSpawn prefabUnitSpawn;

	public Text unitNameText;

	public VTFloatRangeProperty spawnChanceProp;

	[Header("Options")]
	public GameObject optionsDisplayObject;

	public Image optionsTabImage;

	public RectTransform optionsContentTf;

	public ScrollRect optionsScrollRect;

	private List<VTPropertyField> currentUnitFields = new List<VTPropertyField>();

	[Header("Equips")]
	public GameObject equipsDisplayObject;

	public Image equipsTabImage;

	public GameObject eqListTemplate;

	public RectTransform eqListContentTf;

	public ScrollRect equipsScrollRect;

	public Text twrText;

	private Color tabActiveColor;

	private Color tabInactiveColor;

	[Header("Carrier")]
	public GameObject carrierEditButton;

	[Header("Passengers")]
	public GameObject passengerEditButton;

	[Header("Alternate Spawns")]
	public GameObject altSpawnsWindowObj;

	public GameObject showAltSpawnsButtonObj;

	public ScrollRect altSpawnsScrollRect;

	public GameObject altSpawnTemplate;

	public Transform altSpawnSelectTf;

	public VTFloatRangeProperty weightProp;

	public GameObject defaultWeightText;

	public VTBoolProperty syncGroupProp;

	private int altSpawnIdx = -1;

	private bool moving;

	private Vector3D originalMoveGlobalPos;

	private Quaternion originalMoveRotation;

	private bool isOpen;

	public float unselectedAltSpawnAlpha = 0.6f;

	private List<GameObject> equipListObjs = new List<GameObject>();

	private List<HPEquippable> availableEquips = new List<HPEquippable>();

	private string[] currentEquips;

	private bool altSpawnsWindowOpen;

	private List<GameObject> altSpawnItems = new List<GameObject>();

	private float altSpawnItemHeight;

	private int lastSelectedAltSpawn = -2;

	private float lastSelectedAltSpawnTime;

	private Dictionary<string, string> currAltUnitFields
	{
		get
		{
			if (altSpawnIdx == -1)
			{
				return uSpawner.unitFields;
			}
			return uSpawner.alternateSpawns[altSpawnIdx].unitFields;
		}
	}

	private Vector3D currAltUnitGlobalPos
	{
		get
		{
			if (altSpawnIdx == -1)
			{
				return uSpawner.GetGlobalPosition();
			}
			return uSpawner.alternateSpawns[altSpawnIdx].globalPos;
		}
	}

	private Quaternion currAltUnitRot
	{
		get
		{
			if (altSpawnIdx == -1)
			{
				return uSpawner.spawnerRotation;
			}
			return uSpawner.alternateSpawns[altSpawnIdx].rotation;
		}
	}

	private void Awake()
	{
		tabActiveColor = optionsTabImage.color;
		tabInactiveColor = equipsTabImage.color;
		syncGroupProp.OnValueChanged += SyncGroupProp_OnValueChanged;
	}

	private void VTUnitGroup_OnUnitAddedToGroup(UnitSpawner unit, VTUnitGroup.UnitGroup group)
	{
		if (altSpawnsWindowObj.activeSelf)
		{
			StartCoroutine(DelayedRefreshAltSpawns());
		}
	}

	private IEnumerator DelayedRefreshAltSpawns()
	{
		yield return null;
		SaveDataToUnitSpawner();
		yield return null;
		if (altSpawnsWindowObj.activeSelf)
		{
			SetupAltSpawnsWindow();
		}
	}

	private void SyncGroupProp_OnValueChanged(bool s)
	{
		if (!uSpawner)
		{
			return;
		}
		VTUnitGroup.UnitGroup unitGroup = uSpawner.GetUnitGroup();
		if (unitGroup == null)
		{
			return;
		}
		unitGroup.syncAltSpawns = s;
		if (!s)
		{
			return;
		}
		int num = 0;
		foreach (int unitID in unitGroup.unitIDs)
		{
			UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
			num = Mathf.Max(num, unit.alternateSpawns.Count);
		}
		bool flag = false;
		foreach (int unitID2 in unitGroup.unitIDs)
		{
			UnitSpawner unit2 = VTScenario.current.units.GetUnit(unitID2);
			if (unit2.alternateSpawns.Count < num)
			{
				flag = true;
				for (int i = unit2.alternateSpawns.Count; i < num; i++)
				{
					UnitSpawner.AlternateSpawn alternateSpawn = new UnitSpawner.AlternateSpawn();
					alternateSpawn.position = unit2.transform.position;
					alternateSpawn.rotation = unit2.transform.rotation;
					unit2.alternateSpawns.Add(alternateSpawn);
				}
			}
		}
		if (flag)
		{
			unitsWindow.editor.confirmDialogue.DisplayConfirmation("Alt Spawn Sync", $"Units in the group have been modified to have {num} alt-spawns.", null, null);
			SetupAltSpawnsWindow();
		}
	}

	private void OnEnable()
	{
		if ((bool)unitsWindow && (bool)unitsWindow.editor)
		{
			unitsWindow.editor.OnBeforeSave += Editor_OnBeforeSave;
			unitsWindow.editor.OnScenarioLoaded += Close;
		}
		VTUnitGroup.OnUnitAddedToGroup += VTUnitGroup_OnUnitAddedToGroup;
	}

	private void OnDisable()
	{
		if ((bool)unitsWindow && (bool)unitsWindow.editor)
		{
			unitsWindow.editor.OnBeforeSave -= Editor_OnBeforeSave;
			unitsWindow.editor.OnScenarioLoaded -= Close;
		}
		VTUnitGroup.OnUnitAddedToGroup -= VTUnitGroup_OnUnitAddedToGroup;
	}

	private void Update()
	{
		if (!uSpawner || uSpawner.alternateSpawns.Count <= 0)
		{
			return;
		}
		Vector3 vector = VTMapManager.GlobalToWorldPoint(uSpawner.GetGlobalPosition());
		float num = Mathf.Clamp(Vector3.Distance(vector, unitsWindow.editor.editorCamera.transform.position) * 7f / 1000f, 5f, 5000f);
		unitsWindow.editor.editorCamera.DrawWireSphere(vector, num, Color.green);
		unitsWindow.editor.editorCamera.DrawLine(vector, vector + uSpawner.spawnerRotation * new Vector3(0f, 0f, 2.5f * num), Color.green);
		int num2 = 0;
		foreach (UnitSpawner.AlternateSpawn alternateSpawn in uSpawner.alternateSpawns)
		{
			Color color = ((num2 == altSpawnIdx) ? Color.green : new Color(0.15f, 1f, 0f, unselectedAltSpawnAlpha));
			float num3 = Mathf.Clamp(Vector3.Distance(alternateSpawn.position, unitsWindow.editor.editorCamera.transform.position) * 7f / 1000f, 5f, 5000f);
			unitsWindow.editor.editorCamera.DrawLine(vector, alternateSpawn.position, color * 0.75f);
			unitsWindow.editor.editorCamera.DrawWireSphere(alternateSpawn.position, num3, color);
			unitsWindow.editor.editorCamera.DrawLine(alternateSpawn.position, alternateSpawn.position + alternateSpawn.rotation * new Vector3(0f, 0f, 2.5f * num3), color);
			num2++;
		}
		VTUnitGroup.UnitGroup unitGroup = uSpawner.GetUnitGroup();
		if (unitGroup == null || !unitGroup.syncAltSpawns)
		{
			return;
		}
		foreach (int unitID in unitGroup.unitIDs)
		{
			UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
			if (!(unit != uSpawner))
			{
				continue;
			}
			Vector3 a = VTMapManager.GlobalToWorldPoint(unit.GetGlobalPosition());
			int num4 = 0;
			foreach (UnitSpawner.AlternateSpawn alternateSpawn2 in unit.alternateSpawns)
			{
				Color color2 = ((num4 == altSpawnIdx) ? Color.yellow : new Color(1f, 1f, 0f, unselectedAltSpawnAlpha));
				float num5 = Mathf.Clamp(Vector3.Distance(alternateSpawn2.position, unitsWindow.editor.editorCamera.transform.position) * 7f / 1000f, 5f, 5000f);
				unitsWindow.editor.editorCamera.DrawLine(a, alternateSpawn2.position, color2 * 0.75f);
				unitsWindow.editor.editorCamera.DrawWireSphere(alternateSpawn2.position, num5, color2);
				unitsWindow.editor.editorCamera.DrawLine(alternateSpawn2.position, alternateSpawn2.position + alternateSpawn2.rotation * new Vector3(0f, 0f, 2.5f * num5), color2);
				num4++;
			}
		}
	}

	private void Editor_OnBeforeSave()
	{
		if (isOpen && (bool)uSpawner)
		{
			SaveDataToUnitSpawner();
		}
	}

	private void SaveDataToUnitSpawner()
	{
		if (altSpawnIdx >= 0)
		{
			uSpawner.alternateSpawns[altSpawnIdx].weight = (float)weightProp.GetValue();
		}
		if ((bool)spawnChanceProp)
		{
			uSpawner.spawnChance = Mathf.Clamp((float)spawnChanceProp.GetValue(), 0f, 100f);
		}
		foreach (VTPropertyField currentUnitField in currentUnitFields)
		{
			if (currAltUnitFields.ContainsKey(currentUnitField.fieldName))
			{
				currAltUnitFields[currentUnitField.fieldName] = VTSConfigUtils.WriteObject(currentUnitField.type, currentUnitField.GetValue());
			}
			else
			{
				currAltUnitFields.Add(currentUnitField.fieldName, VTSConfigUtils.WriteObject(currentUnitField.type, currentUnitField.GetValue()));
			}
		}
	}

	public void SetupForUnit(UnitSpawner uSpawner)
	{
		if (isOpen)
		{
			Close();
		}
		base.gameObject.SetActive(value: true);
		this.uSpawner = uSpawner;
		prefabUnitSpawn = UnitCatalogue.GetUnitPrefab(uSpawner.unitID).GetComponentImplementing<UnitSpawn>();
		unitNameText.text = uSpawner.GetUIDisplayName();
		if ((bool)spawnChanceProp)
		{
			if (uSpawner.prefabUnitSpawn is PlayerSpawn || uSpawner.prefabUnitSpawn is AICarrierSpawn)
			{
				spawnChanceProp.gameObject.SetActive(value: false);
			}
			else
			{
				spawnChanceProp.gameObject.SetActive(value: true);
				spawnChanceProp.min = 0f;
				spawnChanceProp.max = 100f;
				spawnChanceProp.SetInitialValue(uSpawner.spawnChance);
			}
		}
		SetupOptionsList();
		twrText.gameObject.SetActive(value: false);
		if (uSpawner.prefabUnitSpawn is AIUnitSpawnEquippable && ((AIUnitSpawnEquippable)uSpawner.prefabUnitSpawn).hardpoints != null && ((AIUnitSpawnEquippable)uSpawner.prefabUnitSpawn).hardpoints.Length != 0)
		{
			equipsTabImage.gameObject.SetActive(value: true);
			SetupEquipsList();
		}
		else
		{
			equipsTabImage.gameObject.SetActive(value: false);
		}
		equipsTabImage.color = tabInactiveColor;
		optionsTabImage.color = tabActiveColor;
		equipsDisplayObject.SetActive(value: false);
		optionsDisplayObject.SetActive(value: true);
		optionsScrollRect.verticalNormalizedPosition = 1f;
		carrierEditButton.SetActive(uSpawner.prefabUnitSpawn is AICarrierSpawn);
		if (uSpawner.prefabUnitSpawn is ICanHoldPassengers)
		{
			passengerEditButton.SetActive(((ICanHoldPassengers)uSpawner.prefabUnitSpawn).HasPassengerBay());
		}
		else
		{
			passengerEditButton.SetActive(value: false);
		}
		FloatingOrigin.instance.OnPostOriginShift -= OnPostOriginShift;
		FloatingOrigin.instance.OnPostOriginShift += OnPostOriginShift;
		isOpen = true;
	}

	private void OnPostOriginShift(Vector3 offset)
	{
		uSpawner.transform.position = VTMapManager.GlobalToWorldPoint(currAltUnitGlobalPos);
		if (uSpawner.prefabUnitSpawn is AICarrierSpawn && !unitsWindow.editor.carrierEditorWindow.isOpen)
		{
			unitsWindow.editor.carrierEditorWindow.OpenForUnit(uSpawner);
			unitsWindow.editor.carrierEditorWindow.Close();
		}
	}

	private void SetupEquipsList()
	{
		foreach (GameObject equipListObj in equipListObjs)
		{
			UnityEngine.Object.Destroy(equipListObj);
		}
		equipListObjs.Clear();
		availableEquips.Clear();
		AIUnitSpawnEquippable aIUnitSpawnEquippable = (AIUnitSpawnEquippable)uSpawner.prefabUnitSpawn;
		currentEquips = new string[aIUnitSpawnEquippable.hardpoints.Length];
		if (currAltUnitFields.ContainsKey("equips"))
		{
			List<string> list = ConfigNodeUtils.ParseList(currAltUnitFields["equips"]);
			for (int i = 0; i < list.Count; i++)
			{
				currentEquips[i] = list[i];
			}
		}
		else
		{
			for (int j = 0; j < aIUnitSpawnEquippable.hardpoints.Length; j++)
			{
				currentEquips[j] = aIUnitSpawnEquippable.hardpoints[j];
			}
		}
		GameObject[] equipPrefabs = aIUnitSpawnEquippable.equipPrefabs;
		for (int k = 0; k < equipPrefabs.Length; k++)
		{
			HPEquippable component = equipPrefabs[k].GetComponent<HPEquippable>();
			availableEquips.Add(component);
		}
		float num = ((RectTransform)eqListTemplate.transform).rect.height + 2f;
		for (int l = 0; l < currentEquips.Length; l++)
		{
			string arg = "None";
			if (!string.IsNullOrEmpty(currentEquips[l]))
			{
				foreach (HPEquippable availableEquip in availableEquips)
				{
					if (availableEquip.gameObject.name == currentEquips[l])
					{
						arg = availableEquip.fullName;
						break;
					}
				}
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(eqListTemplate, eqListContentTf);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-l) * num, 0f);
			gameObject.GetComponent<Text>().text = $"HP{l}: {arg}";
			equipListObjs.Add(gameObject);
		}
		eqListContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)currentEquips.Length * num);
		eqListTemplate.SetActive(value: false);
		if (uSpawner.prefabUnitSpawn is AIAircraftSpawn)
		{
			twrText.gameObject.SetActive(value: true);
			float aicraftTWR = GetAicraftTWR();
			twrText.text = "TWR: " + aicraftTWR.ToString("0.00");
		}
		else
		{
			twrText.gameObject.SetActive(value: false);
		}
	}

	private float GetAicraftTWR()
	{
		float num = 0f;
		MassUpdater component = uSpawner.prefabUnitSpawn.GetComponent<MassUpdater>();
		if ((bool)component)
		{
			num += component.baseMass;
		}
		float num2 = ((AIAircraftSpawn)prefabUnitSpawn).fuel;
		foreach (VTPropertyField currentUnitField in currentUnitFields)
		{
			if (currentUnitField.fieldName == "fuel")
			{
				num2 = (float)currentUnitField.GetValue();
			}
		}
		num2 /= 100f;
		FuelTank component2 = prefabUnitSpawn.GetComponent<FuelTank>();
		if ((bool)component2)
		{
			num += num2 * component2.maxFuel * component2.fuelDensity;
		}
		for (int i = 0; i < currentEquips.Length; i++)
		{
			if (string.IsNullOrEmpty(currentEquips[i]))
			{
				continue;
			}
			foreach (HPEquippable availableEquip in availableEquips)
			{
				if (availableEquip.gameObject.name == currentEquips[i])
				{
					num += availableEquip.GetEstimatedMass();
					break;
				}
			}
		}
		float num3 = 0f;
		ModuleEngine[] componentsInChildren = prefabUnitSpawn.GetComponentsInChildren<ModuleEngine>();
		foreach (ModuleEngine moduleEngine in componentsInChildren)
		{
			if (moduleEngine.includeInTWR)
			{
				num3 += moduleEngine.maxThrust * moduleEngine.abThrustMult;
			}
		}
		return num3 / (num * 9.81f);
	}

	public void EditEquipsButton()
	{
		if (uSpawner.prefabUnitSpawn is AIAircraftSpawn)
		{
			unitsWindow.editor.airEquipEditor.Display(uSpawner.prefabUnitSpawn.gameObject, uSpawner.GetUIDisplayName(), currentEquips, availableEquips, OnSelectedEquips);
		}
		else
		{
			unitsWindow.editor.equipEditor.Display(uSpawner.GetUIDisplayName(), currentEquips, availableEquips, OnSelectedEquips);
		}
	}

	private void OnSelectedEquips(string[] newEquips)
	{
		currentEquips = newEquips;
		List<string> list = new List<string>();
		list.AddRange(currentEquips);
		string value = ConfigNodeUtils.WriteList(list);
		if (currAltUnitFields.ContainsKey("equips"))
		{
			currAltUnitFields["equips"] = value;
		}
		else
		{
			currAltUnitFields.Add("equips", value);
		}
		SetupEquipsList();
	}

	private void SetupOptionsList()
	{
		foreach (VTPropertyField currentUnitField in currentUnitFields)
		{
			UnityEngine.Object.Destroy(currentUnitField.gameObject);
		}
		currentUnitFields = new List<VTPropertyField>();
		float num = 0f;
		PlayerVehicle playerVehicle = null;
		VTEquipmentListProperty vTEquipmentListProperty = null;
		FieldInfo[] fields = prefabUnitSpawn.GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			bool flag = true;
			if (altSpawnIdx >= 0 && typeof(VTUnitGroup.UnitGroup).IsAssignableFrom(fieldInfo.FieldType))
			{
				continue;
			}
			object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(UnitSpawnAttributeConditional), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				UnitSpawnAttributeConditional unitSpawnAttributeConditional = (UnitSpawnAttributeConditional)customAttributes[j];
				bool flag2 = (bool)prefabUnitSpawn.GetType().GetMethod(unitSpawnAttributeConditional.conditionalMethodName).Invoke(prefabUnitSpawn, null);
				flag = flag && flag2;
				if (!flag)
				{
					break;
				}
			}
			customAttributes = fieldInfo.GetCustomAttributes(typeof(UnitSpawnOptionConditionalAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				UnitSpawnOptionConditionalAttribute unitSpawnOptionConditionalAttribute = (UnitSpawnOptionConditionalAttribute)customAttributes[j];
				bool flag3 = (bool)prefabUnitSpawn.GetType().GetMethod(unitSpawnOptionConditionalAttribute.conditionalMethodName).Invoke(prefabUnitSpawn, new object[1] { currAltUnitFields });
				flag = flag && flag3;
				if (!flag)
				{
					break;
				}
			}
			if (!flag)
			{
				currAltUnitFields.Remove(fieldInfo.Name);
				continue;
			}
			customAttributes = fieldInfo.GetCustomAttributes(typeof(UnitSpawnAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				UnitSpawnAttribute unitSpawnAttribute = (UnitSpawnAttribute)customAttributes[j];
				object obj;
				if (currAltUnitFields.ContainsKey(fieldInfo.Name))
				{
					obj = VTSConfigUtils.ParseObject(fieldInfo.FieldType, currAltUnitFields[fieldInfo.Name]);
				}
				else
				{
					obj = fieldInfo.GetValue(prefabUnitSpawn);
					currAltUnitFields.Add(fieldInfo.Name, VTSConfigUtils.WriteObject(fieldInfo.FieldType, obj));
				}
				if (obj is UnitReferenceList)
				{
					((UnitReferenceList)obj).unitFilters = ((UnitReferenceList)fieldInfo.GetValue(prefabUnitSpawn)).unitFilters;
				}
				GameObject newPropertyObject = GetNewPropertyObject(fieldInfo, unitSpawnAttribute);
				if (!newPropertyObject)
				{
					continue;
				}
				newPropertyObject.SetActive(value: true);
				RectTransform rectTransform = (RectTransform)newPropertyObject.transform;
				rectTransform.localPosition = new Vector3(0f, 0f - num, 0f);
				VTPropertyField componentImplementing = newPropertyObject.GetComponentImplementing<VTPropertyField>();
				componentImplementing.fieldName = fieldInfo.Name;
				componentImplementing.SetLabel(unitSpawnAttribute.name);
				componentImplementing.type = fieldInfo.FieldType;
				componentImplementing.SetInitialValue(obj);
				currentUnitFields.Add(componentImplementing);
				if (fieldInfo.GetCustomAttribute<RefreshUnitOptionsOnChangeAttribute>() != null)
				{
					componentImplementing.OnPropertyValueChanged += delegate
					{
						SaveDataToUnitSpawner();
						SetupOptionsList();
					};
				}
				num += rectTransform.rect.height;
				if (obj is AirportReference && unitSpawnAttribute is UnitSpawnAirportReferenceAttribute)
				{
					UnitSpawnAirportReferenceAttribute unitSpawnAirportReferenceAttribute = (UnitSpawnAirportReferenceAttribute)unitSpawnAttribute;
					if (unitSpawnAirportReferenceAttribute.teamOption != TeamOptions.BothTeams)
					{
						VTAirportProperty obj2 = (VTAirportProperty)componentImplementing;
						obj2.useTeamFilter = true;
						obj2.teamFilter = unitSpawnAirportReferenceAttribute.GetTeamFilter(uSpawner);
					}
				}
				if (prefabUnitSpawn is MultiplayerSpawn)
				{
					if (obj is MultiplayerSpawn.Vehicles)
					{
						playerVehicle = VTResources.GetPlayerVehicle(MultiplayerSpawn.GetVehicleName((MultiplayerSpawn.Vehicles)obj));
					}
					else if (obj is VehicleEquipmentList)
					{
						vTEquipmentListProperty = (VTEquipmentListProperty)componentImplementing;
					}
				}
				if (unitSpawnAttribute.uiOptions != null)
				{
					foreach (string uiOption in unitSpawnAttribute.uiOptions)
					{
						try
						{
							string[] array = uiOption.Split('=');
							FieldInfo field = componentImplementing.GetType().GetField(array[0]);
							field.SetValue(componentImplementing, ConfigNodeUtils.ParseObject(field.FieldType, array[1]));
						}
						catch (Exception ex)
						{
							Debug.Log("Error trying to apply " + componentImplementing.GetType()?.ToString() + " uiOption: " + uiOption + "\n" + ex);
						}
					}
				}
				using IEnumerator<UnitSpawnTooltipAttribute> enumerator3 = fieldInfo.GetCustomAttributes<UnitSpawnTooltipAttribute>(inherit: true).GetEnumerator();
				if (enumerator3.MoveNext())
				{
					UnitSpawnTooltipAttribute current2 = enumerator3.Current;
					newPropertyObject.AddComponent<UIToolTipRect>().text = current2.tooltip;
				}
			}
		}
		if ((bool)playerVehicle && (bool)vTEquipmentListProperty)
		{
			vTEquipmentListProperty.SetVehicle(playerVehicle);
		}
		optionsContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
	}

	private GameObject GetNewPropertyObject(FieldInfo field, UnitSpawnAttribute attribute)
	{
		GameObject gameObject = null;
		gameObject = unitsWindow.editor.propertyTemplates.GetPropertyFieldForType(field.FieldType, optionsContentTf);
		VTPropertyField component = gameObject.GetComponent<VTPropertyField>();
		foreach (VTOnChangeCallbackAttribute customAttribute in field.GetCustomAttributes<VTOnChangeCallbackAttribute>(inherit: true))
		{
			component.onChangeAttributeCallbacks.Add(new VTPropertyField.VTOnChangeAttributeCallback
			{
				methodName = customAttribute.methodName,
				spawner = uSpawner
			});
		}
		if (field.FieldType == typeof(float))
		{
			if (attribute is UnitSpawnAttributeRange)
			{
				UnitSpawnAttributeRange unitSpawnAttributeRange = (UnitSpawnAttributeRange)attribute;
				VTFloatRangeProperty component2 = gameObject.GetComponent<VTFloatRangeProperty>();
				component2.min = unitSpawnAttributeRange.min;
				component2.max = unitSpawnAttributeRange.max;
				component2.rangeType = unitSpawnAttributeRange.rangeType;
			}
			else
			{
				Debug.LogError("UnitSpawner float property '" + field.Name + "' had an improper attribute. It was " + attribute.GetType().ToString());
			}
		}
		else if (field.FieldType == typeof(UnitReference))
		{
			if (attribute is UnitSpawnAttributeURef)
			{
				UnitSpawnAttributeURef unitSpawnAttributeURef = (UnitSpawnAttributeURef)attribute;
				VTUnitReferenceProperty component3 = gameObject.GetComponent<VTUnitReferenceProperty>();
				component3.teamOption = unitSpawnAttributeURef.teamOption;
				component3.unitTeam = uSpawner.team;
				component3.allowSubunits = unitSpawnAttributeURef.allowSubunits;
			}
			else
			{
				Debug.LogError("UnitSpawner UnitReference property '" + field.Name + "' had an improper attribute. It was " + attribute.GetType().ToString());
			}
		}
		else if (field.FieldType == typeof(VTUnitGroup.UnitGroup))
		{
			gameObject.GetComponent<VTUnitGroupProperty>().unitOptionsUnit = uSpawner;
		}
		else if (component is VTUnitListProperty)
		{
			VTUnitListProperty vTUnitListProperty = (VTUnitListProperty)component;
			vTUnitListProperty.unitTeam = uSpawner.team;
			if (attribute is UnitSpawnUnitListAttribute)
			{
				UnitSpawnUnitListAttribute unitSpawnUnitListAttribute = (UnitSpawnUnitListAttribute)attribute;
				int num = (vTUnitListProperty.selectionLimit = (int)prefabUnitSpawn.GetType().GetMethod(unitSpawnUnitListAttribute.getLimitMethodName).Invoke(prefabUnitSpawn, null));
			}
		}
		return gameObject;
	}

	public void MoveButton()
	{
		if (moving)
		{
			TryFinishMove();
			return;
		}
		if (uSpawner.spawnFlags.Contains("carrier"))
		{
			unitsWindow.editor.confirmDialogue.DisplayConfirmation("Carrier unit!", "This unit is attached to a carrier.  Remove it from the carrier before moving?", RemoveUnitFromCarrierBeforeMoving, null);
			return;
		}
		if (uSpawner.parentSpawner != null)
		{
			unitsWindow.editor.confirmDialogue.DisplayConfirmation("Attached!", "This unit is attached to another unit.  Remove it from the other unit before moving?", RemoveUnitFromParentBeforeMoving, null);
			return;
		}
		originalMoveGlobalPos = currAltUnitGlobalPos;
		originalMoveRotation = currAltUnitRot;
		StartCoroutine(MoveRoutine());
	}

	private void RemoveUnitFromParentBeforeMoving()
	{
		uSpawner.DetachFromParentSpawner();
		MoveButton();
	}

	private void RemoveUnitFromCarrierBeforeMoving()
	{
		AICarrierSpawn.RemoveUnitFromCarrier(uSpawner);
		MoveButton();
	}

	public void GoToButton()
	{
		if (altSpawnIdx >= 0)
		{
			unitsWindow.editor.editorCamera.FocusOnPoint(VTMapManager.GlobalToWorldPoint(uSpawner.alternateSpawns[altSpawnIdx].globalPos));
		}
		else
		{
			unitsWindow.editor.editorCamera.FocusOnPoint(uSpawner.transform.position);
		}
	}

	private IEnumerator MoveRoutine()
	{
		unitsWindow.editor.canClickUnits = false;
		unitsWindow.editor.popupMessages.DisplayPersistentMessage("Moving unit...", Color.yellow, "moveUnit");
		moving = true;
		uSpawner.GetComponent<VTEditorSpawnRenderer>().SetMovingColor();
		yield return null;
		while (moving)
		{
			uSpawner.transform.position = unitsWindow.editor.editorCamera.focusTransform.position;
			uSpawner.transform.rotation = unitsWindow.editor.editorCamera.focusTransform.rotation;
			if (uSpawner.childSpawners.Count > 0)
			{
				uSpawner.MoveAttachedChildSpawners();
			}
			if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
			{
				TryFinishMove();
				break;
			}
			yield return null;
		}
	}

	private void TryFinishMove()
	{
		moving = false;
		UnitSpawner.PlacementValidityInfo placementValidity = uSpawner.GetPlacementValidity(unitsWindow.editor);
		if (placementValidity.isValid)
		{
			UnitSpawner unitSpawner = uSpawner;
			switch (unitsWindow.editor.editorCamera.cursorLocation)
			{
			case ScenarioEditorCamera.CursorLocations.Air:
				unitSpawner.editorPlacementMode = UnitSpawner.EditorPlacementModes.Air;
				break;
			case ScenarioEditorCamera.CursorLocations.Ground:
				unitSpawner.editorPlacementMode = UnitSpawner.EditorPlacementModes.Ground;
				break;
			case ScenarioEditorCamera.CursorLocations.Water:
				unitSpawner.editorPlacementMode = UnitSpawner.EditorPlacementModes.Sea;
				break;
			}
			moving = false;
			if (altSpawnIdx == -1)
			{
				uSpawner.SetGlobalPosition(VTMapManager.WorldToGlobalPoint(uSpawner.transform.position));
				uSpawner.spawnerRotation = uSpawner.transform.rotation;
				uSpawner.MoveAttachedChildSpawners();
			}
			else
			{
				uSpawner.alternateSpawns[altSpawnIdx].position = uSpawner.transform.position;
				uSpawner.alternateSpawns[altSpawnIdx].rotation = uSpawner.transform.rotation;
			}
			uSpawner.GetComponent<VTEditorSpawnRenderer>().SetSelectedColor();
			unitsWindow.editor.canClickUnits = true;
			if (uSpawner.prefabUnitSpawn is AICarrierSpawn)
			{
				unitsWindow.editor.carrierEditorWindow.OpenForUnit(uSpawner);
				unitsWindow.editor.carrierEditorWindow.Close();
			}
		}
		else
		{
			moving = true;
			unitsWindow.editor.confirmDialogue.DisplayConfirmation("Invalid Placement", placementValidity.reason, ResumeMoveUnit, CancelMoveReturnToUnit);
		}
		unitsWindow.editor.popupMessages.RemovePersistentMessage("moveUnit");
	}

	private void ResumeMoveUnit()
	{
		StartCoroutine(MoveRoutine());
	}

	private void CancelMoveReturnToUnit()
	{
		CancelMove();
		unitsWindow.editor.editorCamera.FocusOnPoint(uSpawner.transform.position);
	}

	private void CancelMove()
	{
		if (!moving)
		{
			return;
		}
		if ((bool)uSpawner)
		{
			if (altSpawnIdx == -1)
			{
				uSpawner.SetGlobalPosition(originalMoveGlobalPos);
				uSpawner.MoveAttachedChildSpawners();
			}
			else
			{
				uSpawner.transform.position = VTMapManager.GlobalToWorldPoint(originalMoveGlobalPos);
			}
			uSpawner.transform.rotation = originalMoveRotation;
			if (moving)
			{
				VTEditorSpawnRenderer component = uSpawner.GetComponent<VTEditorSpawnRenderer>();
				if (unitsWindow.IsUnitSelected(uSpawner))
				{
					component.SetSelectedColor();
				}
				else
				{
					component.SetDeselectedColor();
				}
			}
		}
		moving = false;
		unitsWindow.editor.popupMessages.RemovePersistentMessage("moveUnit");
		unitsWindow.editor.canClickUnits = true;
	}

	public void Close()
	{
		if (isOpen)
		{
			if (moving)
			{
				CancelMove();
			}
			if (altSpawnsWindowOpen)
			{
				CloseAltSpawnsWindow();
			}
			if ((bool)uSpawner)
			{
				SaveDataToUnitSpawner();
			}
			FloatingOrigin.instance.OnPostOriginShift -= OnPostOriginShift;
		}
		isOpen = false;
		base.gameObject.SetActive(value: false);
	}

	public void OptionsTabButton()
	{
		optionsDisplayObject.SetActive(value: true);
		equipsDisplayObject.SetActive(value: false);
		optionsTabImage.color = tabActiveColor;
		equipsTabImage.color = tabInactiveColor;
		optionsScrollRect.verticalNormalizedPosition = 1f;
	}

	public void EquipsTabButton()
	{
		optionsDisplayObject.SetActive(value: false);
		equipsDisplayObject.SetActive(value: true);
		optionsTabImage.color = tabInactiveColor;
		equipsTabImage.color = tabActiveColor;
		SetupEquipsList();
		equipsScrollRect.verticalNormalizedPosition = 1f;
	}

	public void EditCarrierButton()
	{
		unitsWindow.editor.carrierEditorWindow.OpenForUnit(uSpawner);
	}

	public void EditNameButton()
	{
		unitsWindow.editor.textInputWindow.Display("Set Custom Name", "Change the name of this unit.", uSpawner.unitName, 20, OnEditedName);
	}

	private void OnEditedName(string n)
	{
		uSpawner.unitName = n;
		unitsWindow.editor.ScenarioObjectsChanged(new VTScenarioEditor.ScenarioChangeEventInfo(VTScenarioEditor.ChangeEventTypes.Units, uSpawner.unitInstanceID, null));
		unitNameText.text = uSpawner.GetUIDisplayName();
	}

	public void OpenAltSpawnsWindow()
	{
		if (!altSpawnsWindowOpen)
		{
			SaveDataToUnitSpawner();
		}
		altSpawnsWindowOpen = true;
		altSpawnsWindowObj.SetActive(value: true);
		showAltSpawnsButtonObj.SetActive(value: false);
		SetupAltSpawnsWindow();
	}

	public void CloseAltSpawnsWindow()
	{
		altSpawnsWindowOpen = false;
		SelectAltSpawn(-1);
		altSpawnsWindowObj.SetActive(value: false);
		showAltSpawnsButtonObj.SetActive(value: true);
	}

	private void SetupAltSpawnsWindow()
	{
		foreach (GameObject altSpawnItem in altSpawnItems)
		{
			UnityEngine.Object.Destroy(altSpawnItem);
		}
		altSpawnItems.Clear();
		altSpawnItemHeight = ((RectTransform)altSpawnTemplate.transform).rect.height * altSpawnTemplate.transform.localScale.y;
		for (int i = -1; i < uSpawner.alternateSpawns.Count; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(altSpawnTemplate, altSpawnsScrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.GetComponent<Text>().text = "Spawn " + (i + 2);
			AltSpawnsListItem altSpawnsListItem = gameObject.AddComponent<AltSpawnsListItem>();
			altSpawnsListItem.optionsWindow = this;
			altSpawnsListItem.altSpawnIdx = i;
			gameObject.transform.localPosition = new Vector3(0f, altSpawnItemHeight * (float)(-(i + 1)), 0f);
			altSpawnItems.Add(gameObject);
		}
		altSpawnTemplate.SetActive(value: false);
		altSpawnsScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)(uSpawner.alternateSpawns.Count + 1) * altSpawnItemHeight);
		altSpawnsScrollRect.verticalNormalizedPosition = 1f;
		VTUnitGroup.UnitGroup unitGroup = uSpawner.GetUnitGroup();
		if (unitGroup != null)
		{
			syncGroupProp.gameObject.SetActive(value: true);
			syncGroupProp.SetInitialValue(unitGroup.syncAltSpawns);
		}
		else
		{
			syncGroupProp.gameObject.SetActive(value: false);
		}
		SelectAltSpawn(altSpawnIdx, saveFirst: false);
	}

	public void AddAltSpawn()
	{
		if (uSpawner.spawnFlags.Contains("carrier"))
		{
			unitsWindow.editor.confirmDialogue.DisplayConfirmation("Invalid", "Units attached to a carrier can not have alternate spawns!", null, null);
			return;
		}
		SaveDataToUnitSpawner();
		Vector3 position = uSpawner.transform.position;
		Quaternion rotation = uSpawner.transform.rotation;
		if (altSpawnIdx >= 0)
		{
			position = uSpawner.alternateSpawns[altSpawnIdx].position;
			rotation = uSpawner.alternateSpawns[altSpawnIdx].rotation;
		}
		UnitSpawner.AlternateSpawn alternateSpawn = new UnitSpawner.AlternateSpawn();
		alternateSpawn.position = position;
		alternateSpawn.rotation = rotation;
		foreach (KeyValuePair<string, string> currAltUnitField in currAltUnitFields)
		{
			alternateSpawn.unitFields.Add(currAltUnitField.Key, currAltUnitField.Value);
		}
		uSpawner.alternateSpawns.Add(alternateSpawn);
		VTUnitGroup.UnitGroup unitGroup = uSpawner.GetUnitGroup();
		if (unitGroup != null && unitGroup.syncAltSpawns)
		{
			foreach (int unitID in unitGroup.unitIDs)
			{
				UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
				if (unit != uSpawner)
				{
					UnitSpawner.AlternateSpawn alternateSpawn2 = new UnitSpawner.AlternateSpawn();
					alternateSpawn2.position = unit.transform.position;
					alternateSpawn2.rotation = unit.transform.rotation;
					unit.alternateSpawns.Add(alternateSpawn2);
				}
			}
		}
		SetupAltSpawnsWindow();
		SelectAltSpawn(uSpawner.alternateSpawns.Count - 1);
	}

	public void RemoveAltSpawn()
	{
		if (altSpawnIdx >= 0)
		{
			int num = altSpawnIdx;
			uSpawner.alternateSpawns.RemoveAt(altSpawnIdx);
			altSpawnIdx = Mathf.Clamp(num, -1, uSpawner.alternateSpawns.Count - 1);
			VTUnitGroup.UnitGroup unitGroup = uSpawner.GetUnitGroup();
			if (unitGroup != null && unitGroup.syncAltSpawns)
			{
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if (unit != uSpawner && unit.alternateSpawns.Count > num)
					{
						unit.alternateSpawns.RemoveAt(num);
					}
				}
			}
			SetupAltSpawnsWindow();
		}
		else
		{
			unitsWindow.editor.confirmDialogue.DisplayConfirmation("Invalid", "Can not remove the default spawn variant! Use the Units panel to delete the spawner entirely.", null, null);
		}
	}

	public void SelectAltSpawn(int idx, bool saveFirst = true)
	{
		if (saveFirst)
		{
			SaveDataToUnitSpawner();
		}
		altSpawnSelectTf.localPosition = new Vector3(0f, altSpawnItemHeight * (float)(-(idx + 1)), 0f);
		altSpawnIdx = idx;
		uSpawner.transform.position = VTMapManager.GlobalToWorldPoint(currAltUnitGlobalPos);
		uSpawner.transform.rotation = currAltUnitRot;
		SetupOptionsList();
		if (uSpawner.prefabUnitSpawn is AIUnitSpawnEquippable && ((AIUnitSpawnEquippable)uSpawner.prefabUnitSpawn).hardpoints != null && ((AIUnitSpawnEquippable)uSpawner.prefabUnitSpawn).hardpoints.Length != 0)
		{
			equipsTabImage.gameObject.SetActive(value: true);
			SetupEquipsList();
		}
		if (uSpawner.prefabUnitSpawn is AICarrierSpawn)
		{
			unitsWindow.editor.carrierEditorWindow.OpenForUnit(uSpawner);
			unitsWindow.editor.carrierEditorWindow.Close();
		}
		if (idx == -1)
		{
			defaultWeightText.SetActive(value: true);
			weightProp.gameObject.SetActive(value: false);
		}
		else
		{
			defaultWeightText.SetActive(value: false);
			weightProp.gameObject.SetActive(value: true);
			weightProp.rangeType = UnitSpawnAttributeRange.RangeTypes.Int;
			weightProp.min = 1f;
			weightProp.max = 9999f;
			weightProp.SetInitialValue(uSpawner.alternateSpawns[idx].weight);
		}
		bool flag = false;
		if (lastSelectedAltSpawn == idx && Time.unscaledTime - lastSelectedAltSpawnTime < 0.3f)
		{
			flag = true;
		}
		lastSelectedAltSpawn = idx;
		lastSelectedAltSpawnTime = Time.unscaledTime;
		if (flag)
		{
			GoToButton();
		}
	}

	public void EditPassengersButton()
	{
		unitsWindow.editor.passengerEditor.OpenForUnit(uSpawner);
	}
}
