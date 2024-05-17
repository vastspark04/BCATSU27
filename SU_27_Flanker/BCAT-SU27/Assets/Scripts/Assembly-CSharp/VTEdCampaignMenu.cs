using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdCampaignMenu : MonoBehaviour
{
	public class CampaignFileItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public int idx;

		public VTEdCampaignMenu menu;

		private float lastClickTime;

		public void OnPointerClick(PointerEventData e)
		{
			if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				menu.EditButton();
				return;
			}
			menu.SelectCampaign(idx);
			lastClickTime = Time.unscaledTime;
		}
	}

	public VTEditMainMenu mainMenu;

	public VTEdCampaignEditWindow editMenu;

	public VTEdNewCampaignSaveMenu saveMenu;

	public ScrollRect filesScrollRect;

	public Button[] itemDependentButtons;

	public GameObject fileTemplate;

	public Transform selectionTf;

	public RawImage campaignImage;

	public Texture2D defaultImage;

	public ScrollRect descriptionScrollRect;

	public Text campaignNameText;

	public Text vehicleText;

	public Text descriptionText;

	private ContentSizeFitter descriptionFitter;

	private List<VTCampaignInfo> campaigns;

	private List<GameObject> listObjects = new List<GameObject>();

	private float lineHeight;

	private int currIdx;

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		RefreshList();
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}

	private void RefreshList()
	{
		lineHeight = ((RectTransform)fileTemplate.transform).rect.height;
		selectionTf.gameObject.SetActive(value: false);
		descriptionFitter = descriptionText.GetComponent<ContentSizeFitter>();
		VTResources.LoadCustomScenarios();
		fileTemplate.SetActive(value: false);
		campaigns = VTResources.GetCustomCampaigns();
		foreach (GameObject listObject in listObjects)
		{
			Object.Destroy(listObject);
		}
		listObjects = new List<GameObject>();
		for (int i = 0; i < campaigns.Count; i++)
		{
			GameObject gameObject = Object.Instantiate(fileTemplate, filesScrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (0f - lineHeight) * (float)i, 0f);
			listObjects.Add(gameObject);
			gameObject.GetComponentInChildren<Text>().text = campaigns[i].campaignID;
			CampaignFileItem campaignFileItem = gameObject.AddComponent<CampaignFileItem>();
			campaignFileItem.idx = i;
			campaignFileItem.menu = this;
		}
		filesScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)campaigns.Count * lineHeight);
		filesScrollRect.ClampVertical();
		itemDependentButtons.SetInteractable(interactable: false);
		campaignNameText.text = string.Empty;
		vehicleText.text = string.Empty;
		descriptionText.text = string.Empty;
		descriptionFitter.SetLayoutVertical();
		descriptionScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1f);
		campaignImage.texture = defaultImage;
	}

	public void NewCampaignButton()
	{
		saveMenu.Open();
		Close();
	}

	public void EditButton()
	{
		editMenu.Open(campaigns[currIdx]);
		Close();
	}

	public void DeleteButton()
	{
		mainMenu.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this campaign and all of its missions?", FinallyDelete, null);
	}

	private void FinallyDelete()
	{
		VTCampaignInfo vTCampaignInfo = campaigns[currIdx];
		Debug.Log("Deleting campaign: " + vTCampaignInfo.campaignID);
		VTResources.DeleteCustomCampaign(vTCampaignInfo.campaignID);
		VTResources.LoadCustomScenarios();
		RefreshList();
	}

	public void BackButton()
	{
		Close();
		mainMenu.Open();
	}

	public void SelectCampaign(int idx)
	{
		currIdx = idx;
		itemDependentButtons.SetInteractable(interactable: true);
		selectionTf.gameObject.SetActive(value: true);
		selectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
		VTCampaignInfo vTCampaignInfo = campaigns[idx];
		campaignNameText.text = vTCampaignInfo.campaignName;
		descriptionText.text = vTCampaignInfo.description;
		descriptionFitter.SetLayoutVertical();
		descriptionScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, descriptionText.rectTransform.rect.height);
		vehicleText.text = vTCampaignInfo.vehicle;
		if (vTCampaignInfo.image != null)
		{
			campaignImage.texture = vTCampaignInfo.image;
		}
		else
		{
			campaignImage.texture = defaultImage;
		}
	}
}
