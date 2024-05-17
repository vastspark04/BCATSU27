using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTSBasesWindow : VTEdUITab
{
	public VTScenarioEditor editor;

	public ScrollRect baseScrollRect;

	public VTEdBaseEditor baseEditor;

	public GameObject listItemTemplate;

	public GameObject infoWindow;

	public Color alliedColor;

	public Color enemyColor;

	private List<VTEdBaseListItem> listItems = new List<VTEdBaseListItem>();

	public override void OnOpenedTab()
	{
		base.OnOpenedTab();
		SetupList();
		baseEditor.gameObject.SetActive(value: false);
		infoWindow.SetActive(value: false);
		editor.OnScenarioLoaded += Editor_OnScenarioLoaded;
	}

	private void Editor_OnScenarioLoaded()
	{
		baseEditor.gameObject.SetActive(value: false);
		SetupList();
	}

	public override void OnClosedTab()
	{
		base.OnClosedTab();
		if (baseEditor.gameObject.activeSelf)
		{
			baseEditor.OkayButton();
		}
		infoWindow.SetActive(value: false);
		editor.OnScenarioLoaded -= Editor_OnScenarioLoaded;
	}

	private void SetupList()
	{
		foreach (VTEdBaseListItem listItem in listItems)
		{
			Object.Destroy(listItem.gameObject);
		}
		listItems.Clear();
		listItemTemplate.SetActive(value: false);
		int num = 0;
		float height = ((RectTransform)listItemTemplate.transform).rect.height;
		foreach (KeyValuePair<int, ScenarioBases.ScenarioBaseInfo> baseInfo in editor.currentScenario.bases.baseInfos)
		{
			GameObject obj = Object.Instantiate(listItemTemplate, baseScrollRect.content);
			obj.SetActive(value: true);
			obj.transform.localPosition = new Vector3(0f, (float)(-num) * height, 0f);
			VTEdBaseListItem component = obj.GetComponent<VTEdBaseListItem>();
			component.baseID = baseInfo.Key;
			listItems.Add(component);
			UpdateBaseLabel(component);
			num++;
		}
		baseScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height * (float)num);
		baseScrollRect.ClampVertical();
	}

	private void UpdateBaseLabel(VTEdBaseListItem li)
	{
		ScenarioBases.ScenarioBaseInfo scenarioBaseInfo = editor.currentScenario.bases.baseInfos[li.baseID];
		li.baseNameText.text = scenarioBaseInfo.GetFinalName();
		li.selectorButtonImage.color = ((scenarioBaseInfo.baseTeam == Teams.Allied) ? alliedColor : enemyColor);
	}

	public void UpdateBaseLabels()
	{
		foreach (VTEdBaseListItem listItem in listItems)
		{
			UpdateBaseLabel(listItem);
		}
	}

	public void OpenBaseEditor(int baseID)
	{
		baseEditor.OpenForBase(editor.currentScenario.bases.baseInfos[baseID]);
	}

	public void ToggleBaseInfoWindow()
	{
		infoWindow.SetActive(!infoWindow.activeSelf);
	}
}
