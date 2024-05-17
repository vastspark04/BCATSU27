using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuiltInTutorialsUI : MonoBehaviour
{
	private struct ListItemInfo
	{
		public VTScenarioInfo scenario;

		public float uiPosY;
	}

	public CampaignSelectorUI campaignUI;

	[Header("List")]
	public ScrollRect listScroll;

	public GameObject listItemTemplate;

	public GameObject listLabelTemplate;

	public Transform selectionTf;

	[Header("Display")]
	public RawImage displayImage;

	public Text displayTitle;

	public Text displayDescription;

	private bool setupComplete;

	public static bool usingTutorialsUI;

	private List<ListItemInfo> items = new List<ListItemInfo>();

	private float listLineHeight;

	private int selectedIdx = -1;

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		if (!setupComplete)
		{
			Setup();
		}
		usingTutorialsUI = true;
	}

	private void Setup()
	{
		listLineHeight = ((RectTransform)listItemTemplate.transform).rect.height * listItemTemplate.transform.localScale.y;
		listItemTemplate.SetActive(value: false);
		listLabelTemplate.SetActive(value: false);
		int idx = 0;
		int num = 0;
		Vector3 zero = Vector3.zero;
		foreach (VTCampaignInfo builtInTutorial in VTResources.GetBuiltInTutorials())
		{
			GameObject obj = Object.Instantiate(listLabelTemplate, listScroll.content);
			obj.SetActive(value: true);
			obj.GetComponentInChildren<Text>().text = builtInTutorial.vehicle;
			obj.transform.localPosition = zero;
			zero.y -= listLineHeight;
			foreach (VTScenarioInfo missionScenario in builtInTutorial.missionScenarios)
			{
				GameObject obj2 = Object.Instantiate(listItemTemplate, listScroll.content);
				obj2.SetActive(value: true);
				obj2.transform.localPosition = zero;
				obj2.GetComponent<VRUIListItemTemplate>().Setup(missionScenario.GetLocalizedName(), items.Count, ClickItem);
				ListItemInfo listItemInfo = default(ListItemInfo);
				listItemInfo.scenario = missionScenario;
				listItemInfo.uiPosY = zero.y;
				ListItemInfo item = listItemInfo;
				items.Add(item);
				zero.y -= listLineHeight;
				if (PilotSaveManager.currentScenario != null && PilotSaveManager.currentScenario.scenarioID == missionScenario.id)
				{
					idx = num;
				}
				num++;
			}
		}
		listScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f - zero.y);
		ClickItem(idx);
		GetComponentInParent<VRPointInteractableCanvas>().RefreshInteractables();
		setupComplete = true;
	}

	private void ClickItem(int idx)
	{
		ListItemInfo listItemInfo = items[idx];
		selectionTf.transform.localPosition = new Vector3(0f, listItemInfo.uiPosY, 0f);
		selectedIdx = idx;
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		VTScenarioInfo scenario = items[selectedIdx].scenario;
		displayImage.texture = scenario.image;
		displayTitle.text = scenario.GetLocalizedName();
		displayDescription.text = scenario.GetLocalizedDescription();
	}

	public void NextItem()
	{
		if (selectedIdx < items.Count - 1)
		{
			ClickItem(selectedIdx + 1);
		}
	}

	public void PrevItem()
	{
		if (selectedIdx > 0)
		{
			ClickItem(selectedIdx - 1);
		}
	}

	public void LaunchScenarioButton()
	{
		campaignUI.StartTutorial(items[selectedIdx].scenario);
		base.gameObject.SetActive(value: false);
	}
}
