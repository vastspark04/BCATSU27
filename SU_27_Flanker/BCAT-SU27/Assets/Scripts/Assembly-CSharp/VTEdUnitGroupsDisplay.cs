using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEdUnitGroupsDisplay : MonoBehaviour
{
	public class GroupListItem : MonoBehaviour
	{
		public int idx;

		public Teams team;

		public VTEdUnitGroupsDisplay display;

		public void OnPointerClick()
		{
			display.SelectGroup(team, idx);
		}
	}

	public VTScenarioEditor editor;

	public VTEdUnitsWindow unitsWindow;

	public Transform groupSelectionTf;

	public ScrollRect alliedScrollRect;

	public ScrollRect enemyScrollRect;

	public GameObject groupTemplate;

	private float groupLineHeight;

	private VTUnitGroup.UnitGroup selectedGroup;

	private List<GameObject> groupObjs = new List<GameObject>();

	private Dictionary<Teams, List<VTUnitGroup.UnitGroup>> groups = new Dictionary<Teams, List<VTUnitGroup.UnitGroup>>();

	public ScrollRect unitsScrollRect;

	public GameObject unitItemTemplate;

	private List<GameObject> unitItemObjs = new List<GameObject>();

	private float unitLineHeight;

	private VTUnitGroup.GroupTypes currentGroupType;

	public Image groundTabImage;

	public Image airTabImage;

	public Image seaTabImage;

	private Color activeTabColor;

	private Color inactiveTabColor;

	private void Awake()
	{
		groupLineHeight = ((RectTransform)groupTemplate.transform).rect.height;
		unitLineHeight = ((RectTransform)unitItemTemplate.transform).rect.height;
		groupTemplate.SetActive(value: false);
		unitItemTemplate.SetActive(value: false);
		activeTabColor = groundTabImage.color;
		inactiveTabColor = airTabImage.color;
		editor.OnScenarioObjectsChanged += Editor_OnScenarioObjectsChanged;
	}

	private void Editor_OnScenarioObjectsChanged(VTScenarioEditor.ScenarioChangeEventInfo e)
	{
		if (e.type != VTScenarioEditor.ChangeEventTypes.UnitGroups)
		{
			return;
		}
		VTUnitGroup.UnitGroup unitGroup = selectedGroup;
		if (!base.gameObject.activeInHierarchy)
		{
			return;
		}
		Display();
		if (unitGroup != null)
		{
			int num = groups[unitGroup.team].IndexOf(unitGroup);
			if (num >= 0)
			{
				SelectGroup(unitGroup.team, num);
			}
		}
	}

	public void Display()
	{
		base.gameObject.SetActive(value: true);
		selectedGroup = null;
		SetupGroupLists();
		SetupUnitsList();
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}

	public void SetGround()
	{
		SetGroupType(VTUnitGroup.GroupTypes.Ground);
	}

	public void SetAir()
	{
		SetGroupType(VTUnitGroup.GroupTypes.Air);
	}

	public void SetSea()
	{
		SetGroupType(VTUnitGroup.GroupTypes.Sea);
	}

	public void SetGroupType(VTUnitGroup.GroupTypes groupType)
	{
		currentGroupType = groupType;
		SetupGroupLists();
		groundTabImage.color = ((groupType == VTUnitGroup.GroupTypes.Ground) ? activeTabColor : inactiveTabColor);
		airTabImage.color = ((groupType == VTUnitGroup.GroupTypes.Air) ? activeTabColor : inactiveTabColor);
		seaTabImage.color = ((groupType == VTUnitGroup.GroupTypes.Sea) ? activeTabColor : inactiveTabColor);
	}

	private void SetupGroupLists()
	{
		foreach (GameObject groupObj in groupObjs)
		{
			Object.Destroy(groupObj);
		}
		groupObjs.Clear();
		groups.Clear();
		groupSelectionTf.gameObject.SetActive(value: false);
		SetupGroupList(Teams.Allied, currentGroupType);
		SetupGroupList(Teams.Enemy, currentGroupType);
	}

	private void SetupGroupList(Teams team, VTUnitGroup.GroupTypes groupType)
	{
		ScrollRect scrollRect = ((team == Teams.Allied) ? alliedScrollRect : enemyScrollRect);
		List<VTUnitGroup.UnitGroup> existingGroups = editor.currentScenario.groups.GetExistingGroups(team);
		existingGroups.RemoveAll((VTUnitGroup.UnitGroup g) => g.groupType != groupType);
		groups.Add(team, existingGroups);
		for (int i = 0; i < existingGroups.Count; i++)
		{
			GameObject gameObject = Object.Instantiate(groupTemplate, scrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * groupLineHeight, 0f);
			gameObject.GetComponentInChildren<Text>().text = existingGroups[i].groupID.ToString();
			groupObjs.Add(gameObject);
			GroupListItem groupListItem = gameObject.AddComponent<GroupListItem>();
			groupListItem.idx = i;
			groupListItem.team = team;
			groupListItem.display = this;
			gameObject.GetComponentInChildren<Button>().onClick.AddListener(groupListItem.OnPointerClick);
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)existingGroups.Count * groupLineHeight);
		scrollRect.verticalNormalizedPosition = 1f;
	}

	public void SelectGroup(Teams team, int idx)
	{
		groupSelectionTf.gameObject.SetActive(value: true);
		if (team == Teams.Allied)
		{
			groupSelectionTf.SetParent(alliedScrollRect.content);
		}
		else
		{
			groupSelectionTf.SetParent(enemyScrollRect.content);
		}
		groupSelectionTf.SetAsFirstSibling();
		groupSelectionTf.transform.localPosition = new Vector3(0f, (float)(-idx) * groupLineHeight, 0f);
		selectedGroup = groups[team][idx];
		SetupUnitsList();
	}

	private void SetupUnitsList()
	{
		foreach (GameObject unitItemObj in unitItemObjs)
		{
			Object.Destroy(unitItemObj);
		}
		unitItemObjs = new List<GameObject>();
		if (selectedGroup != null)
		{
			for (int i = 0; i < selectedGroup.unitIDs.Count; i++)
			{
				int unitID = selectedGroup.unitIDs[i];
				GameObject gameObject = Object.Instantiate(unitItemTemplate, unitsScrollRect.content);
				gameObject.SetActive(value: true);
				gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * unitLineHeight, 0f);
				unitItemObjs.Add(gameObject);
				gameObject.GetComponent<VTEdUnitWindowItem>().SetUnit(editor.currentScenario.units.GetUnit(unitID));
			}
			unitsScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)selectedGroup.unitIDs.Count * unitLineHeight);
		}
		else
		{
			unitsScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, unitLineHeight);
		}
		unitsScrollRect.verticalNormalizedPosition = 1f;
	}
}
