using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ScenarioInfoWindow : MonoBehaviour
{
	public VTScenarioEditor editor;

	private string title = string.Empty;

	private string description = string.Empty;

	public InputField titleField;

	public InputField descriptionField;

	public RawImage scenarioImage;

	public Texture2D noImageTexture;

	public VTBoolProperty equipsConfigBool;

	public VTBoolProperty forceEquipsBool;

	public Button forcedEquipsButton;

	public VTFloatRangeProperty forcedFuelField;

	public VTBoolProperty trainingBool;

	public InputField budgetField;

	public Text selectedVehicleText;

	private string selectedVehicleID;

	public Text rtbWptText;

	public Text refuelWptText;

	public Text bullseyeText;

	public Text envSelectText;

	public VTBoolProperty optionalEnvBool;

	private List<int> modifiedSelectedEquips;

	private List<string> allEquipIDs;

	private List<string> allEquipFullNames;

	private List<HPEquippable> allEquippables;

	private string[] modifiedForcedEquips;

	private string newImagePath = string.Empty;

	private bool imageDirty;

	private List<string> modifiedEquipsOnComplete;

	public GameObject equipsOnCompleteButton;

	[Header("Quicksave")]
	public VTEnumProperty qsModeProp;

	public VTFloatRangeProperty qsLimitProp;

	private string[] envOptions;

	private string selectedEnv;

	[Header("Multiplayer")]
	public GameObject spObject;

	public GameObject mpObject;

	public VTWaypointProperty rtbProp_A;

	public VTWaypointProperty rtbProp_B;

	public VTWaypointProperty refuelProp_A;

	public VTWaypointProperty refuelProp_B;

	public VTWaypointProperty bullseyeProp_A;

	public VTWaypointProperty bullseyeProp_B;

	public VTFloatRangeProperty budgetProp_A;

	public VTFloatRangeProperty budgetProp_B;

	public GameObject oneBriefingObj;

	public GameObject separateBriefingsObj;

	public VTBoolProperty separateBriefingsProp;

	private bool hasSetupMPEvents;

	private void Awake()
	{
		forceEquipsBool.OnValueChanged += ForceEquipsBool_OnValueChanged;
		equipsConfigBool.OnValueChanged += EquipsConfigBool_OnValueChanged;
		forcedFuelField.min = 15f;
		forcedFuelField.max = 100f;
		qsModeProp.OnPropertyValueChanged += QsModeProp_OnPropertyValueChanged;
		qsLimitProp.OnPropertyValueChanged += QsLimitProp_OnPropertyValueChanged;
		qsLimitProp.rangeType = UnitSpawnAttributeRange.RangeTypes.Int;
		qsLimitProp.min = -1f;
		qsLimitProp.max = 100f;
	}

	private void QsLimitProp_OnPropertyValueChanged(object arg0)
	{
		if (Mathf.RoundToInt((float)arg0) <= 0)
		{
			qsLimitProp.SetLabel("Unlimited");
		}
		else
		{
			qsLimitProp.SetLabel("Limit");
		}
	}

	private void QsModeProp_OnPropertyValueChanged(object arg0)
	{
		UpdateQSObjects((QuicksaveManager.QSModes)arg0);
	}

	private void UpdateQSObjects(QuicksaveManager.QSModes mode)
	{
		if (mode == QuicksaveManager.QSModes.None)
		{
			qsLimitProp.gameObject.SetActive(value: false);
		}
		else
		{
			qsLimitProp.gameObject.SetActive(value: true);
		}
	}

	private void EquipsConfigBool_OnValueChanged(bool arg0)
	{
		UpdateForcedFuelVisibility();
	}

	private void ForceEquipsBool_OnValueChanged(bool forceEqs)
	{
		forcedEquipsButton.interactable = forceEqs;
	}

	private void UpdateForcedFuelVisibility()
	{
		bool flag = !(bool)equipsConfigBool.GetValue();
		bool activeSelf = forcedFuelField.gameObject.activeSelf;
		forcedFuelField.gameObject.SetActive(flag);
		if (flag && !activeSelf)
		{
			forcedFuelField.SetInitialValue(editor.currentScenario.normForcedFuel * 100f);
		}
	}

	public void OnChangedTitle(string text)
	{
		title = text;
	}

	public void OnChangedDescription(string text)
	{
		description = text;
	}

	public void Cancel()
	{
		Close();
	}

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		editor.editorCamera.inputLock.AddLock("infoWindow");
		editor.BlockEditor(base.transform);
		title = editor.currentScenario.scenarioName;
		description = editor.currentScenario.scenarioDescription;
		titleField.text = title;
		descriptionField.text = description;
		if (VTScenario.current.multiplayer)
		{
			SetupMultiplayer();
		}
		else
		{
			if (!string.IsNullOrEmpty(VTScenarioEditor.currentCampaign))
			{
				trainingBool.gameObject.SetActive(value: false);
			}
			else
			{
				trainingBool.SetInitialValue(editor.currentScenario.isTraining);
			}
			UpdateEquipLists(editor.currentScenario.vehicle, editor.currentScenario.allowedEquips);
			equipsConfigBool.SetInitialValue(editor.currentScenario.equipsConfigurable);
			forceEquipsBool.SetInitialValue(editor.currentScenario.forceEquips);
			forcedEquipsButton.interactable = editor.currentScenario.forceEquips;
			budgetField.text = editor.currentScenario.baseBudget.ToString();
			selectedVehicleText.text = editor.currentScenario.vehicle.vehicleName;
			selectedVehicleID = editor.currentScenario.vehicle.vehicleName;
			UpdateRTBWptText();
			UpdateRefuelWptText();
			UpdateBullsText();
			if (!string.IsNullOrEmpty(VTScenarioEditor.currentCampaign))
			{
				equipsOnCompleteButton.SetActive(value: true);
				modifiedEquipsOnComplete = new List<string>();
				foreach (string item in editor.currentScenario.equipsOnComplete)
				{
					modifiedEquipsOnComplete.Add(item);
				}
			}
			else
			{
				equipsOnCompleteButton.SetActive(value: false);
			}
			forcedFuelField.gameObject.SetActive(value: true);
			UpdateForcedFuelVisibility();
			qsModeProp.SetInitialValue(editor.currentScenario.qsMode);
			qsLimitProp.SetInitialValue((float)editor.currentScenario.qsLimit);
			QsLimitProp_OnPropertyValueChanged((float)editor.currentScenario.qsLimit);
			UpdateQSObjects(editor.currentScenario.qsMode);
		}
		Texture2D texture = noImageTexture;
		if (!string.IsNullOrEmpty(editor.currentScenario.scenarioID))
		{
			Texture2D image = VTResources.GetCustomScenario(editor.currentScenario.scenarioID, VTScenarioEditor.currentCampaign).image;
			if (image != null)
			{
				texture = image;
			}
		}
		scenarioImage.texture = texture;
		if (string.IsNullOrEmpty(editor.currentScenario.envName))
		{
			selectedEnv = "day";
		}
		else
		{
			selectedEnv = editor.currentScenario.envName;
		}
		UpdateEnvText();
		optionalEnvBool.SetInitialValue(editor.currentScenario.selectableEnv);
		StartCoroutine(DelayedOpen());
	}

	private IEnumerator DelayedOpen()
	{
		yield return null;
		forcedFuelField.SetInitialValue(editor.currentScenario.normForcedFuel * 100f);
	}

	private void UpdateEnvText()
	{
		envSelectText.text = selectedEnv;
	}

	public void SelectEnvButton()
	{
		int selected = -1;
		envOptions = new string[EnvironmentManager.instance.options.Count];
		for (int i = 0; i < EnvironmentManager.instance.options.Count; i++)
		{
			envOptions[i] = EnvironmentManager.instance.options[i].name;
			if (envOptions[i] == selectedEnv)
			{
				selected = i;
			}
		}
		editor.optionSelector.Display("Select Environment", envOptions, selected, OnSelectedEnv);
	}

	private void OnSelectedEnv(int idx)
	{
		selectedEnv = envOptions[idx];
		UpdateEnvText();
	}

	public void Okay()
	{
		editor.UpdateScenarioInfo(title, description);
		editor.currentScenario.envName = selectedEnv;
		editor.currentScenario.selectableEnv = (bool)optionalEnvBool.GetValue();
		if (imageDirty)
		{
			string filePath = VTResources.GetCustomScenario(editor.currentScenario.scenarioID, VTScenarioEditor.currentCampaign).filePath;
			filePath = Path.GetDirectoryName(filePath);
			string[] files = Directory.GetFiles(filePath);
			foreach (string path in files)
			{
				string text = Path.GetFileName(path).ToLower();
				if (File.Exists(path) && (text == "image.png" || text == "image.jpg"))
				{
					File.Delete(path);
				}
			}
			string extension = Path.GetExtension(newImagePath);
			File.Copy(newImagePath, Path.Combine(filePath, "image" + extension));
			VTResources.LoadCustomScenarios();
		}
		if (VTScenario.current.multiplayer)
		{
			VTScenario.current.waypoints.bullseyeB = (Waypoint)bullseyeProp_B.GetValue();
			VTScenario.current.waypoints.bullseye = (Waypoint)bullseyeProp_A.GetValue();
			VTScenario.current.refuelWptID_B = VTScenario.current.GetUnitOrWaypointID(refuelProp_B.GetValue());
			VTScenario.current.refuelWptID = VTScenario.current.GetUnitOrWaypointID(refuelProp_A.GetValue());
			VTScenario.current.rtbWptID_B = VTScenario.current.GetUnitOrWaypointID(rtbProp_B.GetValue());
			VTScenario.current.rtbWptID = VTScenario.current.GetUnitOrWaypointID(rtbProp_A.GetValue());
			VTScenario.current.baseBudget = (float)budgetProp_A.GetValue();
			VTScenario.current.baseBudgetB = (float)budgetProp_B.GetValue();
			VTScenario.current.separateBriefings = (bool)separateBriefingsProp.GetValue();
		}
		else
		{
			List<string> list = new List<string>();
			foreach (int modifiedSelectedEquip in modifiedSelectedEquips)
			{
				list.Add(allEquipIDs[modifiedSelectedEquip]);
			}
			editor.currentScenario.allowedEquips = list;
			editor.currentScenario.equipsConfigurable = (bool)equipsConfigBool.GetValue();
			editor.currentScenario.forceEquips = (bool)forceEquipsBool.GetValue();
			editor.currentScenario.normForcedFuel = (float)forcedFuelField.GetValue() / 100f;
			editor.currentScenario.vehicle = VTResources.GetPlayerVehicle(selectedVehicleID);
			int num = Mathf.RoundToInt((float)qsLimitProp.GetValue());
			if (num <= 0)
			{
				num = -1;
			}
			editor.currentScenario.qsLimit = num;
			editor.currentScenario.qsMode = (QuicksaveManager.QSModes)qsModeProp.GetValue();
			if (!string.IsNullOrEmpty(VTScenarioEditor.currentCampaign))
			{
				editor.currentScenario.equipsOnComplete = new List<string>();
				foreach (string item in modifiedEquipsOnComplete)
				{
					editor.currentScenario.equipsOnComplete.Add(item);
				}
			}
			else
			{
				editor.currentScenario.isTraining = (bool)trainingBool.GetValue();
			}
			editor.currentScenario.forcedEquips = new List<string>();
			string[] files = modifiedForcedEquips;
			foreach (string text2 in files)
			{
				if (!string.IsNullOrEmpty(text2) && modifiedSelectedEquips.Contains(allEquipIDs.IndexOf(text2)))
				{
					editor.currentScenario.forcedEquips.Add(text2);
				}
				else
				{
					editor.currentScenario.forcedEquips.Add(string.Empty);
				}
			}
			if (!string.IsNullOrEmpty(budgetField.text))
			{
				editor.currentScenario.baseBudget = float.Parse(budgetField.text);
			}
		}
		Close();
	}

	private void Close()
	{
		if (string.IsNullOrEmpty(editor.currentScenario.scenarioName))
		{
			if (title == string.Empty)
			{
				title = "untitled";
			}
			editor.UpdateScenarioInfo(title, description);
		}
		base.gameObject.SetActive(value: false);
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("infoWindow");
		imageDirty = false;
	}

	public void VehicleSelectButton()
	{
		if (!string.IsNullOrEmpty(editor.currentScenario.campaignID))
		{
			return;
		}
		PlayerVehicle[] playerVehicles = VTResources.GetPlayerVehicles();
		string[] array = new string[playerVehicles.Length];
		int selected = 0;
		for (int i = 0; i < playerVehicles.Length; i++)
		{
			if (playerVehicles[i].vehicleName == editor.currentScenario.vehicle.vehicleName)
			{
				selected = i;
			}
			array[i] = playerVehicles[i].vehicleName;
		}
		VTEdOptionSelector optionSelector = editor.optionSelector;
		object[] returnValues = playerVehicles;
		optionSelector.Display("Select Vehicle", array, returnValues, selected, OnSelectedVehicle);
	}

	private void OnSelectedVehicle(object vehicle)
	{
		string vehicleName = ((PlayerVehicle)vehicle).vehicleName;
		if (vehicleName != selectedVehicleID)
		{
			editor.currentScenario.vehicle = (PlayerVehicle)vehicle;
			editor.currentScenario.allowedEquips = editor.currentScenario.vehicle.GetEquipNamesList();
			if (vehicleName == editor.currentScenario.vehicle.vehicleName)
			{
				UpdateEquipLists(editor.currentScenario.vehicle, editor.currentScenario.allowedEquips);
			}
			else
			{
				UpdateEquipLists((PlayerVehicle)vehicle, null);
			}
			selectedVehicleID = vehicleName;
			selectedVehicleText.text = vehicleName;
			if ((bool)VTScenario.current.units.GetPlayerSpawner())
			{
				VTScenario.current.units.GetPlayerSpawner().DetachAllChildren();
			}
			if (editor.unitsTab.isOpen)
			{
				editor.unitsTab.unitOptionsWindow.Close();
			}
		}
	}

	private void UpdateEquipLists(PlayerVehicle vehicle, List<string> currentAllowedEquips)
	{
		allEquipIDs = vehicle.GetEquipNamesList();
		allEquippables = vehicle.GetPrefabEquipList();
		allEquipFullNames = new List<string>();
		modifiedSelectedEquips = new List<int>();
		for (int i = 0; i < allEquipIDs.Count; i++)
		{
			if (currentAllowedEquips != null && currentAllowedEquips.Contains(allEquipIDs[i]))
			{
				modifiedSelectedEquips.Add(i);
			}
			allEquipFullNames.Add(vehicle.GetEquipPrefab(allEquipIDs[i]).GetComponent<HPEquippable>().fullName);
		}
		modifiedForcedEquips = new string[editor.currentScenario.vehicle.hardpointCount];
		if (editor.currentScenario.forcedEquips != null)
		{
			for (int j = 0; j < editor.currentScenario.forcedEquips.Count && j < modifiedForcedEquips.Length; j++)
			{
				modifiedForcedEquips[j] = editor.currentScenario.forcedEquips[j];
			}
		}
	}

	public void EquipSelectButton()
	{
		editor.multiSelector.Display("Select Equips", allEquipFullNames.ToArray(), modifiedSelectedEquips, OnSelectedEquips);
	}

	private void OnSelectedEquips(int[] selectedIndices)
	{
		modifiedSelectedEquips = new List<int>();
		foreach (int item in selectedIndices)
		{
			modifiedSelectedEquips.Add(item);
		}
	}

	public void ForcedEquipsButton()
	{
		List<HPEquippable> list = new List<HPEquippable>();
		foreach (int modifiedSelectedEquip in modifiedSelectedEquips)
		{
			list.Add(allEquippables[modifiedSelectedEquip]);
		}
		editor.airEquipEditor.Display(editor.currentScenario.vehicle.vehiclePrefab, selectedVehicleID, modifiedForcedEquips, list, OnSelectedForcedEquips);
	}

	private void OnSelectedForcedEquips(string[] eqs)
	{
		modifiedForcedEquips = eqs;
	}

	public void BriefingEditButton()
	{
		editor.briefingEditor.OpenEditor();
	}

	public void SelectImageButton()
	{
		if (string.IsNullOrEmpty(editor.currentScenario.scenarioID))
		{
			editor.confirmDialogue.DisplayConfirmation("Save Required", "The scenario must be saved to a file before an image can be imported.", SaveBeforeSelectImage, null);
		}
		else
		{
			editor.resourceBrowser.OpenBrowser("Select Image", OnSelectedScenarioImage, VTResources.supportedImageExtensions);
		}
	}

	private void SaveBeforeSelectImage()
	{
		Okay();
		editor.Save();
		editor.OpenInfoWindow();
		editor.BlockEditor(editor.saveMenu.transform);
	}

	private void OnSelectedScenarioImage(string path)
	{
		Texture2D texture = VTResources.GetTexture(path);
		if (texture != null)
		{
			scenarioImage.texture = texture;
			newImagePath = path;
			imageDirty = true;
		}
	}

	private void UpdateRTBWptText()
	{
		rtbWptText.text = GetUnitOrWptLabel(editor.currentScenario.GetRTBWaypointObject());
	}

	public void SelectRTBButton()
	{
		List<string> list = new List<string>();
		List<object> list2 = new List<object>();
		int num = -1;
		list.Add("None");
		list2.Add(null);
		int num2 = 1;
		object rTBWaypointObject = editor.currentScenario.GetRTBWaypointObject();
		if (rTBWaypointObject == null)
		{
			num = 0;
		}
		Waypoint[] waypoints = editor.currentScenario.waypoints.GetWaypoints();
		foreach (Waypoint waypoint in waypoints)
		{
			list.Add(waypoint.name);
			list2.Add(waypoint);
			if (num < 0 && rTBWaypointObject.Equals(waypoint))
			{
				num = num2;
			}
			num2++;
		}
		foreach (UnitSpawner value in editor.currentScenario.units.alliedUnits.Values)
		{
			if (value.prefabUnitSpawn is IHasRTBWaypoint)
			{
				list.Add(value.GetUIDisplayName());
				list2.Add(value);
				if (num < 0 && rTBWaypointObject.Equals(value))
				{
					num = num2;
				}
				num2++;
			}
		}
		editor.optionSelector.Display("Select RTB Waypoint", list.ToArray(), list2.ToArray(), num, OnSelectedRTBObj);
	}

	private void OnSelectedRTBObj(object wptObj)
	{
		editor.currentScenario.SetRTBWaypoint(wptObj);
		UpdateRTBWptText();
	}

	private void UpdateRefuelWptText()
	{
		refuelWptText.text = GetUnitOrWptLabel(editor.currentScenario.GetRefuelWaypointObject());
	}

	public void SelectRefuelButton()
	{
		List<string> list = new List<string>();
		List<object> list2 = new List<object>();
		int num = -1;
		int num2 = 1;
		list.Add("None");
		list2.Add(null);
		object refuelWaypointObject = editor.currentScenario.GetRefuelWaypointObject();
		if (refuelWaypointObject == null)
		{
			num = 0;
		}
		Waypoint[] waypoints = editor.currentScenario.waypoints.GetWaypoints();
		foreach (Waypoint waypoint in waypoints)
		{
			list.Add(waypoint.name);
			list2.Add(waypoint);
			if (num < 0 && refuelWaypointObject.Equals(waypoint))
			{
				num = num2;
			}
			num2++;
		}
		foreach (UnitSpawner value in editor.currentScenario.units.alliedUnits.Values)
		{
			if (value.prefabUnitSpawn is IHasRefuelWaypoint)
			{
				list.Add(value.GetUIDisplayName());
				list2.Add(value);
				if (num < 0 && refuelWaypointObject.Equals(value))
				{
					num = num2;
				}
				num2++;
			}
		}
		editor.optionSelector.Display("Select Refuel Waypoint", list.ToArray(), list2.ToArray(), num, OnSelectedRefuelObj);
	}

	private void OnSelectedRefuelObj(object wptObj)
	{
		editor.currentScenario.SetRefuelWaypoint(wptObj);
		UpdateRefuelWptText();
	}

	public void SelectBullseyeButton()
	{
		List<string> list = new List<string>();
		List<object> list2 = new List<object>();
		int num = -1;
		list.Add("None");
		list2.Add(null);
		int num2 = 1;
		object bullseye = editor.currentScenario.waypoints.bullseye;
		if (bullseye == null)
		{
			num = 0;
		}
		Waypoint[] waypoints = editor.currentScenario.waypoints.GetWaypoints();
		foreach (Waypoint waypoint in waypoints)
		{
			list.Add(waypoint.name);
			list2.Add(waypoint);
			if (num < 0 && bullseye.Equals(waypoint))
			{
				num = num2;
			}
			num2++;
		}
		editor.optionSelector.Display("Select Bullseye Waypoint", list.ToArray(), list2.ToArray(), num, OnSelectedBullsObj);
	}

	private void OnSelectedBullsObj(object bullsObj)
	{
		editor.currentScenario.waypoints.bullseye = (Waypoint)bullsObj;
		UpdateBullsText();
	}

	private void UpdateBullsText()
	{
		if (editor.currentScenario.waypoints.bullseye != null)
		{
			bullseyeText.text = editor.currentScenario.waypoints.bullseye.name;
		}
		else
		{
			bullseyeText.text = "Select Wpt";
		}
	}

	private string GetUnitOrWptLabel(object wptObj)
	{
		string result = "Select Wpt";
		if (wptObj is Waypoint)
		{
			result = ((Waypoint)wptObj).name;
		}
		else if (wptObj is UnitSpawner)
		{
			result = ((UnitSpawner)wptObj).GetUIDisplayName();
		}
		return result;
	}

	public void SelectCompletionEquipsButton()
	{
		PlayerVehicle playerVehicle = VTResources.GetPlayerVehicle(VTResources.GetCustomCampaign(VTScenarioEditor.currentCampaign).vehicle);
		List<string> list = new List<string>();
		List<int> list2 = new List<int>();
		for (int i = 0; i < playerVehicle.allEquipPrefabs.Count; i++)
		{
			HPEquippable component = playerVehicle.allEquipPrefabs[i].GetComponent<HPEquippable>();
			list.Add(component.fullName);
			if (modifiedEquipsOnComplete.Contains(component.gameObject.name))
			{
				list2.Add(i);
			}
		}
		editor.multiSelector.Display("Completion Equips", list.ToArray(), list2, OnSelectedCompletionEquips);
	}

	private void OnSelectedCompletionEquips(int[] selected)
	{
		PlayerVehicle playerVehicle = VTResources.GetPlayerVehicle(VTResources.GetCustomCampaign(VTScenarioEditor.currentCampaign).vehicle);
		modifiedEquipsOnComplete = new List<string>();
		foreach (int index in selected)
		{
			modifiedEquipsOnComplete.Add(playerVehicle.allEquipPrefabs[index].name);
		}
	}

	private void SetupMultiplayer()
	{
		spObject.SetActive(value: false);
		mpObject.SetActive(value: true);
		oneBriefingObj.SetActive(!VTScenario.current.separateBriefings);
		separateBriefingsObj.SetActive(VTScenario.current.separateBriefings);
		separateBriefingsProp.SetInitialValue(VTScenario.current.separateBriefings);
		SetInitial(rtbProp_A, VTScenario.current.rtbWptID);
		SetInitial(rtbProp_B, VTScenario.current.rtbWptID_B);
		SetInitial(refuelProp_A, VTScenario.current.refuelWptID);
		SetInitial(refuelProp_B, VTScenario.current.refuelWptID_B);
		bullseyeProp_A.SetInitialValue(VTScenario.current.waypoints.bullseye);
		bullseyeProp_B.SetInitialValue(VTScenario.current.waypoints.bullseyeB);
		budgetProp_A.min = (budgetProp_B.min = 0f);
		budgetProp_A.max = (budgetProp_B.max = 9999999f);
		budgetProp_A.SetInitialValue(VTScenario.current.baseBudget);
		budgetProp_B.SetInitialValue(VTScenario.current.baseBudgetB);
		if (!hasSetupMPEvents)
		{
			hasSetupMPEvents = true;
			separateBriefingsProp.OnValueChanged += SeparateBriefingsProp_OnValueChanged;
		}
	}

	private void SeparateBriefingsProp_OnValueChanged(bool arg0)
	{
		oneBriefingObj.SetActive(!arg0);
		separateBriefingsObj.SetActive(arg0);
	}

	private void SetInitial(VTWaypointProperty prop, string unitOrWptId)
	{
		object unitOrWaypoint = VTScenario.current.GetUnitOrWaypoint(unitOrWptId);
		if (unitOrWaypoint != null)
		{
			if (unitOrWaypoint is Waypoint)
			{
				prop.SetInitialValue(unitOrWaypoint);
			}
			else if (unitOrWaypoint is UnitSpawner)
			{
				prop.SetInitialValue(((UnitSpawner)unitOrWaypoint).waypoint);
			}
			else
			{
				Debug.LogError("ScenarioInfoWindow SetInitial: the return object is neither a unit nor a waypoint.");
			}
		}
		else
		{
			prop.SetInitialValue(null);
		}
	}

	public void BriefingEditButtonB()
	{
		editor.briefingEditor.OpenEditor(teamB: true);
	}
}
