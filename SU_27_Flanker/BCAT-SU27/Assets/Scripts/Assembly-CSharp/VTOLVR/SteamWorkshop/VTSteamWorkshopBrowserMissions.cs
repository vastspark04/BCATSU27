using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.SteamWorkshop{

public class VTSteamWorkshopBrowserMissions : VTSteamWorkshopBrowser
{
	public CampaignSelectorUI campaignSelector;

	public GameObject mainMenuObj;

	public GameObject vehicleMenuObj;

	public Text missionTypeText;

	private bool campaigns;

	public ScrollRect selectVehicleScrollRect;

	public GameObject vehicleListTemplate;

	public void ScenariosButton()
	{
		campaigns = false;
		mainMenuObj.SetActive(value: false);
		vehicleMenuObj.SetActive(value: true);
		missionTypeText.text = "Scenarios";
	}

	public void CampaignsButton()
	{
		campaigns = true;
		mainMenuObj.SetActive(value: false);
		vehicleMenuObj.SetActive(value: true);
		missionTypeText.text = "Campaigns";
	}

	public void MPCampaignsButton()
	{
		mainMenuObj.SetActive(value: false);
		ClearTags();
		tags.Add("Multiplayer Campaigns");
		mainDisplayObj.SetActive(value: true);
		DisplayPage(1);
	}

	protected override void Awake()
	{
		base.Awake();
		float num = ((RectTransform)vehicleListTemplate.transform).rect.height * vehicleListTemplate.transform.localScale.y;
		int num2 = 0;
		PlayerVehicle[] playerVehicles = VTResources.GetPlayerVehicles();
		foreach (PlayerVehicle playerVehicle in playerVehicles)
		{
			if (!playerVehicle.dlc || playerVehicle.IsDLCOwned())
			{
				GameObject obj = Object.Instantiate(vehicleListTemplate, selectVehicleScrollRect.content);
				obj.SetActive(value: true);
				obj.transform.localPosition = new Vector3(0f, (float)(-num2) * num, 0f);
				VTSTeamWorkshopVehicleItem component = obj.GetComponent<VTSTeamWorkshopVehicleItem>();
				string vName = playerVehicle.vehicleName;
				component.vehicleNameText.text = playerVehicle.vehicleName;
				component.vehicleImage.texture = playerVehicle.vehicleImage;
				component.interactable.OnInteract.AddListener(delegate
				{
					VehicleButton(vName);
				});
				num2++;
			}
		}
		selectVehicleScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num * (float)num2);
		vehicleListTemplate.SetActive(value: false);
	}

	public void VehicleButton(string vName)
	{
		ClearTags();
		if (campaigns)
		{
			tags.Add("Campaigns");
		}
		else
		{
			tags.Add("Single Scenarios");
		}
		tags.Add(vName);
		vehicleMenuObj.SetActive(value: false);
		mainDisplayObj.SetActive(value: true);
		DisplayPage(1);
	}

	public void BackFromBrowser()
	{
		mainDisplayObj.SetActive(value: false);
		vehicleMenuObj.SetActive(value: true);
	}

	public void BackFromVehicles()
	{
		vehicleMenuObj.SetActive(value: false);
		mainMenuObj.SetActive(value: true);
	}

	public void BackFromMain()
	{
		campaignSelector.pilotSelectUI.CloseWorkshopBrowser();
	}
}

}