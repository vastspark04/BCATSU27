using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEdUnitSelector : MonoBehaviour
{
	public delegate void SelectedUnitDelegate(UnitReference uRef);

	public delegate void SelectedMultiUnitDelegate(UnitReferenceList unitList);

	public delegate void SelectedUnitOrWpt(Waypoint wpt);

	public class WaypointSelectButton : MonoBehaviour
	{
		public VTEdUnitSelector selector;

		public Waypoint wpt;

		private void Awake()
		{
			GetComponent<Button>().onClick.AddListener(OnClick);
		}

		private void OnClick()
		{
			selector.SelectWaypoint(wpt);
		}
	}

	public VTScenarioEditor editor;

	public Text titleText;

	public GameObject alliedViewObj;

	public GameObject enemyViewObj;

	public RectTransform alliedContentTf;

	private ScrollRect alliedScrollRect;

	public GameObject alliedButtonTemplate;

	public GameObject alliedMultiTemplate;

	public RectTransform enemyContentTf;

	private ScrollRect enemyScrollRect;

	public GameObject enemyButtonTemplate;

	public GameObject enemyMultiTemplate;

	public GameObject multiOkayButton;

	public GameObject selectAllButton;

	public float doubleWidth;

	public float singleWidth;

	public float tripleWidth;

	public GameObject waypointViewObj;

	public ScrollRect waypointScrollRect;

	public GameObject waypointButtonTemplate;

	private SelectedUnitDelegate OnSelected;

	private SelectedMultiUnitDelegate OnSelectedMulti;

	private SelectedUnitOrWpt OnSelectedUnitOrWpt;

	private List<GameObject> alliedButtons = new List<GameObject>();

	private List<GameObject> enemyButtons = new List<GameObject>();

	private List<GameObject> waypointButtons = new List<GameObject>();

	private UnitReferenceList selectedUnits;

	private bool multi;

	private IUnitFilter[] unitFilters;

	private bool allowSubunits;

	private bool unitOrWpt;

	[Header("Limit multi selection")]
	public Text multiLimitText;

	private int selectionLimit = -1;

	private void Awake()
	{
		alliedButtonTemplate.SetActive(value: false);
		alliedMultiTemplate.SetActive(value: false);
		alliedScrollRect = alliedContentTf.GetComponentInParent<ScrollRect>();
		enemyButtonTemplate.SetActive(value: false);
		enemyMultiTemplate.SetActive(value: false);
		enemyScrollRect = enemyContentTf.GetComponentInParent<ScrollRect>();
	}

	public void DisplayUnitOrWptSelector(string title, SelectedUnitOrWpt onSelected, IUnitFilter[] unitFilters = null)
	{
		base.gameObject.SetActive(value: true);
		selectAllButton.SetActive(value: false);
		allowSubunits = false;
		multi = false;
		multiOkayButton.SetActive(value: false);
		this.unitFilters = unitFilters;
		unitOrWpt = true;
		OnSelectedUnitOrWpt = onSelected;
		titleText.text = title;
		waypointViewObj.SetActive(value: true);
		((RectTransform)base.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tripleWidth);
		alliedViewObj.SetActive(value: true);
		enemyViewObj.SetActive(value: true);
		ClearAllUnitButtons();
		SetupUnitList(Teams.Allied);
		SetupUnitList(Teams.Enemy);
		SetupWaypointList();
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("unitSelector");
	}

	public void DisplayUnitSelector(string title, TeamOptions teamOption, Teams senderTeam, SelectedUnitDelegate onSelected, bool allowSubunits, IUnitFilter[] unitFilters = null)
	{
		base.gameObject.SetActive(value: true);
		this.allowSubunits = allowSubunits;
		multi = false;
		multiOkayButton.SetActive(value: false);
		selectAllButton.SetActive(value: false);
		this.unitFilters = unitFilters;
		OnSelected = onSelected;
		titleText.text = title;
		waypointViewObj.SetActive(value: false);
		unitOrWpt = false;
		RectTransform rectTransform = (RectTransform)base.transform;
		ClearAllUnitButtons();
		if (teamOption == TeamOptions.BothTeams)
		{
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, doubleWidth);
			alliedViewObj.SetActive(value: true);
			enemyViewObj.SetActive(value: true);
			ClearAllUnitButtons();
			SetupUnitList(Teams.Allied);
			SetupUnitList(Teams.Enemy);
		}
		else
		{
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, singleWidth);
			Teams teams = Teams.Allied;
			if ((senderTeam == Teams.Allied && teamOption == TeamOptions.OtherTeam) || (senderTeam == Teams.Enemy && teamOption == TeamOptions.SameTeam))
			{
				teams = Teams.Enemy;
			}
			if (teams == Teams.Allied)
			{
				alliedViewObj.SetActive(value: true);
				enemyViewObj.SetActive(value: false);
				SetupUnitList(Teams.Allied);
			}
			else
			{
				alliedViewObj.SetActive(value: false);
				enemyViewObj.SetActive(value: true);
				SetupUnitList(Teams.Enemy);
			}
		}
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("unitSelector");
	}

	public void DisplayMultiUnitSelector(string title, TeamOptions teamOption, Teams senderTeam, UnitReferenceList currentSelected, SelectedMultiUnitDelegate onSelected, bool allowSubunits, IUnitFilter[] unitFilters = null, int selectionLimit = -1)
	{
		base.gameObject.SetActive(value: true);
		this.allowSubunits = allowSubunits;
		multi = true;
		selectedUnits = new UnitReferenceList();
		foreach (UnitReference unit in currentSelected.units)
		{
			selectedUnits.units.Add(unit);
		}
		this.unitFilters = unitFilters;
		multiOkayButton.SetActive(value: true);
		selectAllButton.SetActive(value: true);
		OnSelectedMulti = onSelected;
		titleText.text = title;
		waypointViewObj.SetActive(value: false);
		unitOrWpt = false;
		RectTransform rectTransform = (RectTransform)base.transform;
		this.selectionLimit = selectionLimit;
		UpdateLimitText();
		ClearAllUnitButtons();
		if (teamOption == TeamOptions.BothTeams)
		{
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, doubleWidth);
			alliedViewObj.SetActive(value: true);
			enemyViewObj.SetActive(value: true);
			SetupUnitList(Teams.Allied);
			SetupUnitList(Teams.Enemy);
		}
		else
		{
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, singleWidth);
			Teams teams = Teams.Allied;
			if ((senderTeam == Teams.Allied && teamOption == TeamOptions.OtherTeam) || (senderTeam == Teams.Enemy && teamOption == TeamOptions.SameTeam))
			{
				teams = Teams.Enemy;
			}
			if (teams == Teams.Allied)
			{
				alliedViewObj.SetActive(value: true);
				enemyViewObj.SetActive(value: false);
				SetupUnitList(Teams.Allied);
			}
			else
			{
				alliedViewObj.SetActive(value: false);
				enemyViewObj.SetActive(value: true);
				SetupUnitList(Teams.Enemy);
			}
		}
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("unitSelector");
	}

	private void SetupWaypointList()
	{
		foreach (GameObject waypointButton in waypointButtons)
		{
			Object.Destroy(waypointButton);
		}
		waypointButtons.Clear();
		float height = ((RectTransform)waypointButtonTemplate.transform).rect.height;
		Waypoint[] waypoints = editor.currentScenario.waypoints.GetWaypoints();
		for (int i = 0; i < waypoints.Length; i++)
		{
			GameObject gameObject = Object.Instantiate(waypointButtonTemplate, waypointScrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * height, 0f);
			WaypointSelectButton waypointSelectButton = gameObject.AddComponent<WaypointSelectButton>();
			waypointSelectButton.wpt = waypoints[i];
			waypointSelectButton.selector = this;
			gameObject.GetComponentInChildren<Text>().text = waypoints[i].name;
			waypointButtons.Add(gameObject);
		}
		waypointScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)waypoints.Length * height);
		waypointScrollRect.ClampVertical();
		waypointButtonTemplate.SetActive(value: false);
	}

	private void ClearAllUnitButtons()
	{
		foreach (GameObject alliedButton in alliedButtons)
		{
			Object.Destroy(alliedButton);
		}
		alliedButtons.Clear();
		foreach (GameObject enemyButton in enemyButtons)
		{
			Object.Destroy(enemyButton);
		}
		enemyButtons.Clear();
	}

	private void SetupUnitList(Teams team)
	{
		StartCoroutine(SetupAfterDelay(team));
	}

	private IEnumerator SetupAfterDelay(Teams team)
	{
		yield return null;
		ICollection<UnitSpawner> values;
		GameObject gameObject;
		RectTransform rectTransform;
		List<GameObject> list;
		ScrollRect scrollRect;
		if (team == Teams.Allied)
		{
			values = editor.currentScenario.units.alliedUnits.Values;
			gameObject = ((!multi) ? alliedButtonTemplate : alliedMultiTemplate);
			rectTransform = alliedContentTf;
			list = alliedButtons;
			scrollRect = alliedScrollRect;
		}
		else
		{
			values = editor.currentScenario.units.enemyUnits.Values;
			gameObject = ((!multi) ? enemyButtonTemplate : enemyMultiTemplate);
			rectTransform = enemyContentTf;
			list = enemyButtons;
			scrollRect = enemyScrollRect;
		}
		List<UnitSpawner> list2 = new List<UnitSpawner>();
		foreach (UnitSpawner item in values)
		{
			bool flag = false;
			for (int i = 0; i < list2.Count; i++)
			{
				if (flag)
				{
					break;
				}
				if (item.unitInstanceID < list2[i].unitInstanceID)
				{
					list2.Insert(i, item);
					flag = true;
				}
			}
			if (!flag)
			{
				list2.Add(item);
			}
		}
		_ = ((RectTransform)gameObject.transform).rect.height;
		float num = 0f;
		foreach (UnitSpawner item2 in list2)
		{
			bool flag2 = true;
			if (unitFilters != null)
			{
				IUnitFilter[] array = unitFilters;
				for (int j = 0; j < array.Length; j++)
				{
					if (!array[j].PassesFilter(item2))
					{
						flag2 = false;
						break;
					}
				}
			}
			if (flag2)
			{
				GameObject gameObject2 = Object.Instantiate(gameObject, rectTransform);
				gameObject2.transform.localPosition = new Vector3(0f, 0f - num, 0f);
				gameObject2.SetActive(value: true);
				if (multi)
				{
					VTEdMultiUnitSelectorButton component = gameObject2.GetComponent<VTEdMultiUnitSelectorButton>();
					component.Setup(this, item2, selectedUnits, allowSubunits);
					num += component.GetHeight();
				}
				else
				{
					VTEdUnitSelectorButton component2 = gameObject2.GetComponent<VTEdUnitSelectorButton>();
					component2.Setup(this, item2, allowSubunits);
					num += component2.GetHeight();
				}
				list.Add(gameObject2);
			}
		}
		rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		scrollRect.ClampVertical();
		UpdateLimitText();
	}

	private void UpdateLimitText()
	{
		if (selectionLimit > 0)
		{
			multiLimitText.gameObject.SetActive(value: true);
			multiLimitText.text = $"{selectedUnits.units.Count}/{selectionLimit}";
			selectAllButton.SetActive(value: false);
			if (selectedUnits.units.Count >= selectionLimit)
			{
				foreach (GameObject alliedButton in alliedButtons)
				{
					VTBoolProperty componentInChildren = alliedButton.GetComponentInChildren<VTBoolProperty>();
					if (!(bool)componentInChildren.GetValue())
					{
						componentInChildren.gameObject.SetActive(value: false);
					}
				}
				{
					foreach (GameObject enemyButton in enemyButtons)
					{
						VTBoolProperty componentInChildren2 = enemyButton.GetComponentInChildren<VTBoolProperty>();
						if (!(bool)componentInChildren2.GetValue())
						{
							componentInChildren2.gameObject.SetActive(value: false);
						}
					}
					return;
				}
			}
			foreach (GameObject alliedButton2 in alliedButtons)
			{
				alliedButton2.GetComponentInChildren<VTBoolProperty>().gameObject.SetActive(value: true);
			}
			{
				foreach (GameObject enemyButton2 in enemyButtons)
				{
					enemyButton2.GetComponentInChildren<VTBoolProperty>().gameObject.SetActive(value: true);
				}
				return;
			}
		}
		multiLimitText.gameObject.SetActive(value: false);
	}

	public void SelectUnit(UnitSpawner unit)
	{
		if (unitOrWpt)
		{
			if (OnSelectedUnitOrWpt != null)
			{
				if ((bool)unit)
				{
					OnSelectedUnitOrWpt(unit.waypoint);
				}
				else
				{
					OnSelectedUnitOrWpt(null);
				}
			}
			Close();
		}
		else if (multi)
		{
			if (selectionLimit < 1 || selectedUnits.units.Count < selectionLimit)
			{
				if (!selectedUnits.ContainsUnit(unit.unitInstanceID))
				{
					selectedUnits.units.Add(new UnitReference(unit.unitInstanceID));
				}
				UpdateLimitText();
			}
		}
		else
		{
			if (OnSelected != null)
			{
				UnitReference uRef = (unit ? new UnitReference(unit.unitInstanceID) : default(UnitReference));
				OnSelected(uRef);
			}
			Close();
		}
	}

	public void DeselectUnit(UnitSpawner unit)
	{
		if (selectedUnits.ContainsUnit(unit.unitInstanceID))
		{
			selectedUnits.units.RemoveAll((UnitReference x) => x.unitID == unit.unitInstanceID);
		}
		UpdateLimitText();
	}

	public void SelectSubUnit(UnitSpawner unit, int subIdx)
	{
		if (multi)
		{
			if (!selectedUnits.ContainsUnit(unit.unitInstanceID, subIdx))
			{
				selectedUnits.units.Add(new UnitReference(unit.unitInstanceID, subIdx));
			}
			return;
		}
		if (OnSelected != null)
		{
			OnSelected(new UnitReference(unit.unitInstanceID, subIdx));
		}
		Close();
	}

	public void DeselectSubUnit(UnitSpawner unit, int subIdx)
	{
		if (selectedUnits.ContainsUnit(unit.unitInstanceID, subIdx))
		{
			selectedUnits.units.RemoveAll((UnitReference x) => x.unitID == unit.unitInstanceID && x.GetSubUnitIdx() == subIdx);
		}
	}

	public void MultiOkayButton()
	{
		if (OnSelectedMulti != null)
		{
			OnSelectedMulti(selectedUnits);
		}
		Close();
	}

	public void SelectAll()
	{
		foreach (GameObject alliedButton in alliedButtons)
		{
			VTEdMultiUnitSelectorButton component = alliedButton.GetComponent<VTEdMultiUnitSelectorButton>();
			if ((bool)component)
			{
				component.SetValue(selected: true);
			}
		}
		foreach (GameObject enemyButton in enemyButtons)
		{
			VTEdMultiUnitSelectorButton component2 = enemyButton.GetComponent<VTEdMultiUnitSelectorButton>();
			if ((bool)component2)
			{
				component2.SetValue(selected: true);
			}
		}
	}

	public void SelectNone()
	{
		if (unitOrWpt)
		{
			SelectWaypoint(null);
		}
		else if (multi)
		{
			foreach (GameObject alliedButton in alliedButtons)
			{
				VTBoolProperty component = alliedButton.GetComponent<VTBoolProperty>();
				if ((bool)component)
				{
					component.SetInitialValue(false);
				}
			}
			foreach (GameObject enemyButton in enemyButtons)
			{
				VTBoolProperty component2 = enemyButton.GetComponent<VTBoolProperty>();
				if ((bool)component2)
				{
					component2.SetInitialValue(false);
				}
			}
			selectedUnits.units.Clear();
			UpdateLimitText();
		}
		else
		{
			SelectUnit(null);
		}
	}

	public void Close()
	{
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("unitSelector");
		base.gameObject.SetActive(value: false);
	}

	public void SelectWaypoint(Waypoint wpt)
	{
		if (OnSelectedUnitOrWpt != null)
		{
			OnSelectedUnitOrWpt(wpt);
		}
		Close();
	}
}
