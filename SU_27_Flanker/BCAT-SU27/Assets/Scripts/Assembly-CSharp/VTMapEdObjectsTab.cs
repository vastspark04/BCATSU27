using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTMapEdObjectsTab : VTEdUITab
{
	public VTMapEditor editor;

	public VTMapEdObjectCategoryList catList;

	[Header("Objects List")]
	public GameObject objectsPanelObj;

	public Button[] itemDependentButtons;

	public ScrollRect scrollRect;

	public GameObject itemTemplate;

	public Transform selectionTf;

	private float lineHeight;

	[Header("Bases List")]
	public ScrollRect basesScrollRect;

	private Vector2 openPos;

	private Vector2 closedPos;

	private RectTransform rTf;

	private List<GameObject> listObjs = new List<GameObject>();

	private List<VTMapEdPrefab> prefabs;

	private int currIdx;

	private VTMapEdScenarioBasePrefab editingBaseName;

	public override void OnOpenedTab()
	{
		Open();
	}

	public override void OnClosedTab()
	{
		Close();
	}

	private void Awake()
	{
		rTf = (RectTransform)base.transform;
		openPos = rTf.anchoredPosition;
		closedPos = openPos;
		closedPos.x = 0f;
		Close();
	}

	public void Toggle()
	{
		if (base.isOpen)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	private void Close()
	{
		foreach (GameObject listObj in listObjs)
		{
			Object.Destroy(listObj);
		}
		listObjs.Clear();
		if (catList.prefabSelector.gameObject.activeSelf)
		{
			catList.prefabSelector.Close();
		}
		if (catList.gameObject.activeSelf)
		{
			catList.Close();
		}
	}

	public void Open()
	{
		objectsPanelObj.SetActive(value: true);
		SetupList();
	}

	private void SetupList()
	{
		foreach (GameObject listObj in listObjs)
		{
			Object.Destroy(listObj);
		}
		listObjs.Clear();
		VTMapCustom vTMapCustom = (VTMapCustom)VTMapManager.fetch.map;
		prefabs = vTMapCustom.prefabs.GetAllPrefabs();
		lineHeight = ((RectTransform)itemTemplate.transform).rect.height;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < prefabs.Count; i++)
		{
			bool flag = prefabs[i] is VTMapEdScenarioBasePrefab;
			RectTransform parent = (flag ? basesScrollRect.content : scrollRect.content);
			GameObject gameObject = Object.Instantiate(itemTemplate, parent);
			gameObject.SetActive(value: true);
			int num3 = (flag ? num2 : num);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-num3) * lineHeight, 0f);
			gameObject.GetComponentInChildren<Text>().text = prefabs[i].GetDisplayName();
			VTMapEdObjectSelectItem component = gameObject.GetComponent<VTMapEdObjectSelectItem>();
			component.idx = i;
			component.lineIdx = num3;
			component.objTab = this;
			component.baseEditButton.SetActive(flag);
			listObjs.Add(gameObject);
			if (flag)
			{
				num2++;
			}
			else
			{
				num++;
			}
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num * lineHeight);
		scrollRect.ClampVertical();
		basesScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num2 * lineHeight);
		basesScrollRect.ClampVertical();
		itemTemplate.SetActive(value: false);
		SelectItem(-1, -1);
	}

	public void EditBaseName(int idx)
	{
		editingBaseName = (VTMapEdScenarioBasePrefab)prefabs[idx];
		editor.textInputWindow.Display("Change name", "Change the base's name so it can be identified in the mission editor.", editingBaseName.baseName, 24, OnEditedBaseName);
	}

	private void OnEditedBaseName(string n)
	{
		editingBaseName.baseName = n;
		SetupList();
	}

	public void SelectItem(int idx, int lineIdx)
	{
		currIdx = idx;
		itemDependentButtons.SetInteractable(idx >= 0);
		selectionTf.gameObject.SetActive(idx >= 0);
		if (idx >= 0)
		{
			bool flag = prefabs[idx] is VTMapEdScenarioBasePrefab;
			selectionTf.SetParent(flag ? basesScrollRect.content : scrollRect.content);
			selectionTf.SetAsFirstSibling();
		}
		selectionTf.transform.localPosition = new Vector3(0f, (float)(-lineIdx) * lineHeight, 0f);
	}

	public void NewButton()
	{
		objectsPanelObj.SetActive(value: false);
		catList.Open();
	}

	public void DeleteButton()
	{
		VTMapEdPrefab vTMapEdPrefab = prefabs[currIdx];
		VTMapGenerator.fetch.RemoveMod("prefab" + vTMapEdPrefab.id);
		((VTMapCustom)VTMapManager.fetch.map).prefabs.RemovePrefab(vTMapEdPrefab.id);
		SetupList();
	}

	public void GoToObject(int idx)
	{
		editor.editorCamera.FocusOnPoint(prefabs[idx].transform.position);
	}
}
