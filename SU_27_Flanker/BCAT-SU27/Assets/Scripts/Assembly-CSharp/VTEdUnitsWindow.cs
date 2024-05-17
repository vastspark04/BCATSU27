using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdUnitsWindow : VTEdUITab, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public enum UnitsTabDisplayStates
	{
		All,
		Groups
	}

	public VTScenarioEditor editor;

	public VTEdUnitOptionsWindow unitOptionsWindow;

	public VTEdUnitGroupsDisplay groupsDisplay;

	public GameObject allUnitsDisplay;

	public GameObject unitItemTemplate;

	public RectTransform alliedContentTf;

	private ScrollRect alliedScroll;

	public RectTransform enemyContentTf;

	private ScrollRect enemyScroll;

	public Image allUnitsTab;

	public Image groupsTab;

	private Color activeTabColor;

	private Color inactiveTabColor;

	private float itemHeight;

	private List<VTEdUnitWindowItem> unitItems = new List<VTEdUnitWindowItem>();

	private List<UnitSpawner> selectedUnits = new List<UnitSpawner>();

	public UnitsTabDisplayStates displayState { get; private set; }

	public bool anyUnitSelected => selectedUnits.Count > 0;

	public event Action<UnitSpawner> OnSelectedUnit;

	public bool IsUnitSelected(UnitSpawner us)
	{
		return selectedUnits.Contains(us);
	}

	private void Awake()
	{
		unitItemTemplate.SetActive(value: false);
		itemHeight = ((RectTransform)unitItemTemplate.transform).rect.height;
		unitOptionsWindow.Close();
		activeTabColor = allUnitsTab.color;
		inactiveTabColor = groupsTab.color;
		alliedScroll = alliedContentTf.GetComponentInParent<ScrollRect>();
		enemyScroll = enemyContentTf.GetComponentInParent<ScrollRect>();
	}

	private void Start()
	{
		editor.OnScenarioObjectsChanged += OnScenarioObjectsChanged;
		editor.OnScenarioLoaded += Editor_OnScenarioLoaded;
	}

	private void Editor_OnScenarioLoaded()
	{
		selectedUnits = new List<UnitSpawner>();
	}

	private void OnScenarioObjectsChanged(VTScenarioEditor.ScenarioChangeEventInfo e)
	{
		if (base.isOpen && displayState == UnitsTabDisplayStates.All && (e.type == VTScenarioEditor.ChangeEventTypes.Units || e.type == VTScenarioEditor.ChangeEventTypes.All))
		{
			UpdateUnitLists();
		}
	}

	public void UpdateUnitLists()
	{
		foreach (VTEdUnitWindowItem unitItem in unitItems)
		{
			UnityEngine.Object.Destroy(unitItem.gameObject);
		}
		unitItems = new List<VTEdUnitWindowItem>();
		PopulateList(alliedContentTf, editor.currentScenario.units.alliedUnits.Values);
		PopulateList(enemyContentTf, editor.currentScenario.units.enemyUnits.Values);
		alliedScroll.ClampVertical();
		enemyScroll.ClampVertical();
	}

	private void PopulateList(RectTransform contentTf, ICollection<UnitSpawner> units)
	{
		List<UnitSpawner> list = new List<UnitSpawner>();
		foreach (UnitSpawner unit2 in units)
		{
			bool flag = false;
			for (int i = 0; i < list.Count; i++)
			{
				if (flag)
				{
					break;
				}
				if (unit2.unitInstanceID < list[i].unitInstanceID)
				{
					list.Insert(i, unit2);
					flag = true;
				}
			}
			if (!flag)
			{
				list.Add(unit2);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			UnitSpawner unit = list[j];
			GameObject obj = UnityEngine.Object.Instantiate(unitItemTemplate, contentTf);
			obj.transform.localPosition = new Vector3(0f, (float)(-j) * itemHeight, 0f);
			VTEdUnitWindowItem component = obj.GetComponent<VTEdUnitWindowItem>();
			component.SetUnit(unit);
			obj.SetActive(value: true);
			unitItems.Add(component);
		}
		contentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)list.Count * itemHeight);
	}

	public override void OnOpenedTab()
	{
		UpdateUnitLists();
	}

	public override void OnClosedTab()
	{
		unitOptionsWindow.Close();
	}

	public void OpenToolsForUnit(UnitSpawner spawner)
	{
		unitOptionsWindow.SetupForUnit(spawner);
	}

	public void NewUnit()
	{
		editor.newUnitsWindow.OpenWindow();
	}

	public void DeleteUnit()
	{
		if (anyUnitSelected)
		{
			editor.confirmDialogue.DisplayConfirmation("Delete unit?", "Are you sure you want to delete the selected units?", DeleteSelectedUnits, null);
		}
	}

	public void DeleteSelectedUnits()
	{
		int num = -1;
		for (int i = 0; i < selectedUnits.Count; i++)
		{
			for (int j = 0; j < unitItems.Count; j++)
			{
				if (num >= 0)
				{
					break;
				}
				if (unitItems[j].GetUnitSpawner() == selectedUnits[i])
				{
					num = j;
				}
			}
			DetachUnitsFromDeletedCarrier(selectedUnits[i]);
			editor.currentScenario.groups.RemoveUnitFromGroups(selectedUnits[i]);
			editor.currentScenario.units.RemoveSpawner(selectedUnits[i]);
			editor.FireDestroyedUnitIDEvent(selectedUnits[i].unitInstanceID);
			UnityEngine.Object.Destroy(selectedUnits[i].gameObject);
		}
		selectedUnits = new List<UnitSpawner>();
		editor.ScenarioObjectsChanged(new VTScenarioEditor.ScenarioChangeEventInfo(VTScenarioEditor.ChangeEventTypes.Units, -1, null));
		if (num >= 0 && unitItems.Count > 0)
		{
			num = Mathf.Clamp(num, 0, unitItems.Count - 1);
			unitItems[num].Select();
		}
		unitOptionsWindow.Close();
	}

	private void DetachUnitsFromDeletedCarrier(UnitSpawner carrierSpawner)
	{
		if (!(carrierSpawner.prefabUnitSpawn is AICarrierSpawn) || !carrierSpawner.unitFields.ContainsKey("carrierSpawns"))
		{
			return;
		}
		List<string> list = ConfigNodeUtils.ParseList(carrierSpawner.unitFields["carrierSpawns"]);
		for (int i = 0; i < list.Count; i++)
		{
			int num = ConfigNodeUtils.ParseInt(list[i].Split(':')[1]);
			if (num != -1)
			{
				UnitSpawner unit = editor.currentScenario.units.GetUnit(num);
				if ((bool)unit)
				{
					AICarrierSpawn.RemoveUnitFromCarrier(unit);
				}
			}
		}
	}

	public void DeselectUnit(UnitSpawner us)
	{
		selectedUnits.Clear();
		if (this.OnSelectedUnit != null)
		{
			this.OnSelectedUnit(null);
		}
		us.GetComponent<VTEditorSpawnRenderer>().SetDeselectedColor();
	}

	public void DuplicateUnit()
	{
		if (selectedUnits[0].prefabUnitSpawn is PlayerSpawn)
		{
			editor.popupMessages.DisplayMessage("Can not copy player spawn", 2f, Color.red);
		}
		else
		{
			StartCoroutine(DupeRoutine());
		}
	}

	private IEnumerator DupeRoutine()
	{
		unitOptionsWindow.Close();
		UnitSpawner copyFrom = selectedUnits[0];
		UnitSpawner newUnit = editor.CreateNewUnit(copyFrom.unitID, checkPlacementValid: false);
		newUnit.SetGlobalPosition(copyFrom.GetGlobalPosition());
		yield return null;
		foreach (string key in copyFrom.unitFields.Keys)
		{
			newUnit.unitFields.Add(key, copyFrom.unitFields[key]);
		}
		if (newUnit.prefabUnitSpawn is AIAircraftSpawn)
		{
			if (newUnit.unitFields.ContainsKey("unitGroup"))
			{
				VTUnitGroup.UnitGroup unitGroup = VTSConfigUtils.ParseUnitGroup(newUnit.unitFields["unitGroup"]);
				if (unitGroup != null)
				{
					editor.currentScenario.groups.AddUnitToGroup(newUnit, unitGroup.groupID);
					editor.ScenarioObjectsChanged(new VTScenarioEditor.ScenarioChangeEventInfo(VTScenarioEditor.ChangeEventTypes.UnitGroups, -1, null));
				}
			}
		}
		else if (newUnit.prefabUnitSpawn is AISeaUnitSpawn)
		{
			if (newUnit.unitFields.ContainsKey("unitGroup"))
			{
				VTUnitGroup.UnitGroup unitGroup2 = VTSConfigUtils.ParseUnitGroup(newUnit.unitFields["unitGroup"]);
				if (unitGroup2 != null)
				{
					editor.currentScenario.groups.AddUnitToGroup(newUnit, unitGroup2.groupID);
					editor.ScenarioObjectsChanged(new VTScenarioEditor.ScenarioChangeEventInfo(VTScenarioEditor.ChangeEventTypes.UnitGroups, -1, null));
				}
			}
		}
		else if (newUnit.prefabUnitSpawn is GroundUnitSpawn && newUnit.unitFields.ContainsKey("unitGroup"))
		{
			VTUnitGroup.UnitGroup unitGroup3 = VTSConfigUtils.ParseUnitGroup(newUnit.unitFields["unitGroup"]);
			if (unitGroup3 != null)
			{
				editor.currentScenario.groups.AddUnitToGroup(newUnit, unitGroup3.groupID);
				editor.ScenarioObjectsChanged(new VTScenarioEditor.ScenarioChangeEventInfo(VTScenarioEditor.ChangeEventTypes.UnitGroups, -1, null));
			}
		}
		foreach (VTEdUnitWindowItem unitItem in unitItems)
		{
			if (unitItem.GetUnitSpawner() == newUnit)
			{
				unitItem.Select();
			}
		}
		yield return null;
		OpenToolsForUnit(newUnit);
		unitOptionsWindow.MoveButton();
	}

	public void SelectUnitAndOpenOptions(UnitSpawner us)
	{
		StartCoroutine(SelectAndOpenUnitRoutine(us));
	}

	private IEnumerator SelectAndOpenUnitRoutine(UnitSpawner us)
	{
		yield return null;
		foreach (VTEdUnitWindowItem unitItem in unitItems)
		{
			if (unitItem.GetUnitSpawner() == us)
			{
				unitItem.Select();
			}
		}
		yield return null;
		OpenToolsForUnit(us);
	}

	public void SelectUnit(UnitSpawner us)
	{
		UnitSpawner[] array = selectedUnits.ToArray();
		foreach (UnitSpawner us2 in array)
		{
			DeselectUnit(us2);
		}
		if (!selectedUnits.Contains(us))
		{
			selectedUnits.Add(us);
			us.GetComponent<VTEditorSpawnRenderer>().SetSelectedColor();
		}
		if (this.OnSelectedUnit != null)
		{
			this.OnSelectedUnit(us);
		}
	}

	public void GroupsButton()
	{
		displayState = UnitsTabDisplayStates.Groups;
		groupsDisplay.Display();
		allUnitsDisplay.SetActive(value: false);
		groupsTab.color = activeTabColor;
		allUnitsTab.color = inactiveTabColor;
	}

	public void AllUnitsButton()
	{
		displayState = UnitsTabDisplayStates.All;
		groupsDisplay.Hide();
		allUnitsDisplay.SetActive(value: true);
		allUnitsTab.color = activeTabColor;
		groupsTab.color = inactiveTabColor;
		UpdateUnitLists();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		editor.editorCamera.scrollLock.AddLock("unitsWindow");
		editor.editorCamera.doubleClickLock.AddLock("unitsWindow");
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		editor.editorCamera.scrollLock.RemoveLock("unitsWindow");
		editor.editorCamera.doubleClickLock.RemoveLock("unitsWindow");
	}
}
