using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTGroupSelectorBrowser : MonoBehaviour
{
	public delegate void OnSelectedGroup(VTUnitGroup.UnitGroup group);

	public class GroupListItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public int idx;

		public bool existing;

		public VTGroupSelectorBrowser browser;

		public bool allGroups;

		public Teams team;

		private float clickTime;

		public void OnPointerClick(PointerEventData eventData)
		{
			bool num = Time.unscaledTime - clickTime < VTOLVRConstants.DOUBLE_CLICK_TIME;
			if (allGroups)
			{
				browser.SelectFromAllGroups(team, idx);
			}
			else if (existing)
			{
				browser.SelectExistingGroup(idx);
			}
			else
			{
				browser.SelectNewGroup(idx);
			}
			if (num)
			{
				browser.Okay();
			}
			clickTime = Time.unscaledTime;
		}
	}

	public VTScenarioEditor editor;

	public GameObject itemTemplate;

	public Button okayButton;

	public GameObject unitGroupsObj;

	[Header("New Group")]
	public RectTransform newContentTf;

	public ScrollRect newGroupScrollRect;

	public Transform newGroupSelectionTf;

	private List<PhoneticLetters> newGroups = new List<PhoneticLetters>();

	private List<GameObject> newGroupObjs = new List<GameObject>();

	[Header("Existing Group")]
	public RectTransform existingContentTf;

	public ScrollRect existGroupScrollRect;

	public Transform existingSelectionTf;

	private List<PhoneticLetters> existingGroups = new List<PhoneticLetters>();

	private List<GameObject> existingGroupObjs = new List<GameObject>();

	[Header("Units")]
	public RectTransform unitsContentTf;

	public ScrollRect unitsScrollRect;

	private List<GameObject> unitItems = new List<GameObject>();

	private UnitSpawner unit;

	private VTUnitGroup.GroupTypes groupType;

	private Teams groupTeam;

	[Header("All Groups")]
	public GameObject allGroupsObjs;

	public ScrollRect alliedGroupsScrollRect;

	public ScrollRect enemyGroupsScrollRect;

	public Transform alliedSelectionTf;

	public Transform enemySelectionTf;

	private List<PhoneticLetters> enemyGroups = new List<PhoneticLetters>();

	private List<PhoneticLetters> alliedGroups = new List<PhoneticLetters>();

	private List<GameObject> alliedGroupsObjs = new List<GameObject>();

	private List<GameObject> enemyGroupsObjs = new List<GameObject>();

	private float lineHeight;

	private PhoneticLetters selectedGroup;

	private OnSelectedGroup OnSelected;

	private void Awake()
	{
		itemTemplate.SetActive(value: false);
		lineHeight = ((RectTransform)itemTemplate.transform).rect.height;
	}

	public void OpenForUnit(UnitSpawner unit, OnSelectedGroup onSelected)
	{
		base.gameObject.SetActive(value: true);
		unitGroupsObj.SetActive(value: true);
		allGroupsObjs.SetActive(value: false);
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("groupBrowser");
		this.unit = unit;
		groupType = unit.prefabUnitSpawn.groupType;
		groupTeam = unit.team;
		OnSelected = onSelected;
		okayButton.interactable = false;
		existingSelectionTf.gameObject.SetActive(value: false);
		newGroupSelectionTf.gameObject.SetActive(value: false);
		newGroups.Clear();
		existingGroups.Clear();
		ClearUnits();
		PopulateGroupLists();
	}

	public void OpenForExistingGroups(OnSelectedGroup onSelected, int groupTypeFilter, Teams team = (Teams)(-1))
	{
		base.gameObject.SetActive(value: true);
		unitGroupsObj.SetActive(value: false);
		allGroupsObjs.SetActive(value: true);
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("groupBrowser");
		groupType = (VTUnitGroup.GroupTypes)groupTypeFilter;
		groupTeam = team;
		OnSelected = onSelected;
		okayButton.interactable = false;
		alliedSelectionTf.gameObject.SetActive(value: false);
		enemySelectionTf.gameObject.SetActive(value: false);
		alliedGroups.Clear();
		enemyGroups.Clear();
		ClearUnits();
		PopulateAllGroupsLists();
	}

	public void Okay()
	{
		Close();
		if ((bool)unit)
		{
			VTScenario.current.groups.RemoveUnitFromGroups(unit);
			VTScenario.current.groups.AddUnitToGroup(unit, selectedGroup);
			if (OnSelected != null)
			{
				OnSelected(VTScenario.current.groups.GetUnitGroup(groupTeam, selectedGroup));
			}
		}
		else if (OnSelected != null)
		{
			OnSelected(VTScenario.current.groups.GetUnitGroup(groupTeam, selectedGroup));
		}
	}

	public void SelectNone()
	{
		Close();
		VTScenario.current.groups.RemoveUnitFromGroups(unit);
		if (OnSelected != null)
		{
			OnSelected(null);
		}
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("groupBrowser");
	}

	public void SelectNewGroup(int idx)
	{
		ClearUnits();
		selectedGroup = newGroups[idx];
		existingSelectionTf.gameObject.SetActive(value: false);
		newGroupSelectionTf.gameObject.SetActive(value: true);
		newGroupSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
		okayButton.interactable = true;
	}

	public void SelectExistingGroup(int idx)
	{
		selectedGroup = existingGroups[idx];
		PopulateUnitList();
		newGroupSelectionTf.gameObject.SetActive(value: false);
		existingSelectionTf.gameObject.SetActive(value: true);
		existingSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
		okayButton.interactable = true;
	}

	private void PopulateGroupLists()
	{
		foreach (GameObject existingGroupObj in existingGroupObjs)
		{
			UnityEngine.Object.Destroy(existingGroupObj);
		}
		existingGroupObjs.Clear();
		foreach (GameObject newGroupObj in newGroupObjs)
		{
			UnityEngine.Object.Destroy(newGroupObj);
		}
		newGroupObjs.Clear();
		List<VTUnitGroup.UnitGroup> list = VTScenario.current.groups.GetExistingGroups(groupTeam);
		newGroups.Clear();
		foreach (object value in Enum.GetValues(typeof(PhoneticLetters)))
		{
			newGroups.Add((PhoneticLetters)value);
		}
		foreach (VTUnitGroup.UnitGroup item in list)
		{
			newGroups.Remove(item.groupID);
		}
		if (groupType >= VTUnitGroup.GroupTypes.Ground)
		{
			list.RemoveAll((VTUnitGroup.UnitGroup g) => g.groupType != groupType);
		}
		existingGroups.Clear();
		for (int i = 0; i < list.Count; i++)
		{
			existingGroups.Add(list[i].groupID);
			GameObject gameObject = UnityEngine.Object.Instantiate(itemTemplate, existingContentTf);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			gameObject.GetComponent<Text>().text = list[i].groupID.ToString();
			GroupListItem groupListItem = gameObject.AddComponent<GroupListItem>();
			groupListItem.browser = this;
			groupListItem.idx = i;
			groupListItem.existing = true;
			existingGroupObjs.Add(gameObject);
		}
		existingContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)list.Count * lineHeight);
		for (int j = 0; j < newGroups.Count; j++)
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate(itemTemplate, newContentTf);
			gameObject2.SetActive(value: true);
			gameObject2.transform.localPosition = new Vector3(0f, (float)(-j) * lineHeight, 0f);
			gameObject2.GetComponent<Text>().text = newGroups[j].ToString();
			GroupListItem groupListItem2 = gameObject2.AddComponent<GroupListItem>();
			groupListItem2.browser = this;
			groupListItem2.idx = j;
			groupListItem2.existing = false;
			newGroupObjs.Add(gameObject2);
		}
		newContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)newGroups.Count * lineHeight);
		newGroupScrollRect.verticalNormalizedPosition = 1f;
		existGroupScrollRect.verticalNormalizedPosition = 1f;
	}

	private void PopulateAllGroupsLists()
	{
		foreach (GameObject alliedGroupsObj in alliedGroupsObjs)
		{
			UnityEngine.Object.Destroy(alliedGroupsObj);
		}
		alliedGroupsObjs.Clear();
		foreach (GameObject enemyGroupsObj in enemyGroupsObjs)
		{
			UnityEngine.Object.Destroy(enemyGroupsObj);
		}
		enemyGroupsObjs.Clear();
		if (groupTeam != Teams.Enemy)
		{
			List<VTUnitGroup.UnitGroup> list = VTScenario.current.groups.GetExistingGroups(Teams.Allied);
			if (groupType >= VTUnitGroup.GroupTypes.Ground)
			{
				list.RemoveAll((VTUnitGroup.UnitGroup g) => g.groupType != groupType);
			}
			for (int i = 0; i < list.Count; i++)
			{
				alliedGroups.Add(list[i].groupID);
				GameObject gameObject = UnityEngine.Object.Instantiate(itemTemplate, alliedGroupsScrollRect.content);
				gameObject.SetActive(value: true);
				gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
				gameObject.GetComponent<Text>().text = list[i].groupID.ToString();
				GroupListItem groupListItem = gameObject.AddComponent<GroupListItem>();
				groupListItem.browser = this;
				groupListItem.idx = i;
				groupListItem.allGroups = true;
				groupListItem.team = Teams.Allied;
				alliedGroupsObjs.Add(gameObject);
			}
			alliedGroupsScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)alliedGroups.Count * lineHeight);
		}
		else
		{
			alliedGroupsScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1f);
		}
		if (groupTeam != 0)
		{
			List<VTUnitGroup.UnitGroup> list2 = VTScenario.current.groups.GetExistingGroups(Teams.Enemy);
			for (int j = 0; j < list2.Count; j++)
			{
				enemyGroups.Add(list2[j].groupID);
				GameObject gameObject2 = UnityEngine.Object.Instantiate(itemTemplate, enemyGroupsScrollRect.content);
				gameObject2.SetActive(value: true);
				gameObject2.transform.localPosition = new Vector3(0f, (float)(-j) * lineHeight, 0f);
				gameObject2.GetComponent<Text>().text = list2[j].groupID.ToString();
				GroupListItem groupListItem2 = gameObject2.AddComponent<GroupListItem>();
				groupListItem2.browser = this;
				groupListItem2.idx = j;
				groupListItem2.allGroups = true;
				groupListItem2.team = Teams.Enemy;
				enemyGroupsObjs.Add(gameObject2);
			}
			enemyGroupsScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)enemyGroups.Count * lineHeight);
		}
		else
		{
			enemyGroupsScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1f);
		}
		alliedGroupsScrollRect.verticalNormalizedPosition = 1f;
		enemyGroupsScrollRect.verticalNormalizedPosition = 1f;
	}

	private void PopulateUnitList()
	{
		ClearUnits();
		List<int> list = ((groupTeam != 0) ? VTScenario.current.groups.enemyGroups[selectedGroup].unitIDs : VTScenario.current.groups.alliedGroups[selectedGroup].unitIDs);
		for (int i = 0; i < list.Count; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(itemTemplate, unitsContentTf);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			gameObject.SetActive(value: true);
			gameObject.GetComponent<Text>().text = VTScenario.current.units.GetUnit(list[i]).GetUIDisplayName();
			unitItems.Add(gameObject);
		}
		unitsContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)list.Count * lineHeight);
		unitsScrollRect.verticalNormalizedPosition = 1f;
	}

	private void ClearUnits()
	{
		foreach (GameObject unitItem in unitItems)
		{
			UnityEngine.Object.Destroy(unitItem);
		}
		unitItems.Clear();
		unitsContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeight);
	}

	public void SelectFromAllGroups(Teams team, int idx)
	{
		List<PhoneticLetters> list = ((team == Teams.Allied) ? alliedGroups : enemyGroups);
		selectedGroup = list[idx];
		groupTeam = team;
		okayButton.interactable = true;
		if (team == Teams.Allied)
		{
			alliedSelectionTf.gameObject.SetActive(value: true);
			alliedSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			enemySelectionTf.gameObject.SetActive(value: false);
		}
		else
		{
			enemySelectionTf.gameObject.SetActive(value: true);
			enemySelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			alliedSelectionTf.gameObject.SetActive(value: false);
		}
		PopulateUnitList();
	}
}
