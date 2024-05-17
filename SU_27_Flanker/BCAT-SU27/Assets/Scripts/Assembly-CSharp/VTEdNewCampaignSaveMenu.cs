using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class VTEdNewCampaignSaveMenu : MonoBehaviour
{
	public class VehicleSelectButton : MonoBehaviour
	{
		public string vehicle;

		public VTEdNewCampaignSaveMenu menu;

		public void OnClick()
		{
			menu.SelectVehicle(vehicle);
		}
	}

	public VTEdCampaignMenu campaignMenu;

	public VTEdCampaignEditWindow campaignEditMenu;

	public InputField textField;

	public Button saveButton;

	public RectTransform scrollContentTf;

	private ScrollRect scrollRect;

	public GameObject filenameTemplate;

	private float lineHeight;

	private string filename;

	private string vehicle;

	private List<GameObject> displayedFiles = new List<GameObject>();

	private List<string> existingFileNames = new List<string>();

	[Header("Vehicle Select")]
	public Text selectedVehicleText;

	public GameObject vehicleTemplate;

	public RectTransform vehicleMenuBg;

	private List<GameObject> vehicleListObjects = new List<GameObject>();

	[Header("Multiplayer")]
	public VTBoolProperty multiplayerProperty;

	public GameObject mpVehicleBlocker;

	private void Awake()
	{
		lineHeight = ((RectTransform)filenameTemplate.transform).rect.height;
		filenameTemplate.SetActive(value: false);
		scrollRect = scrollContentTf.GetComponentInParent<ScrollRect>();
		if ((bool)multiplayerProperty)
		{
			multiplayerProperty.OnValueChanged += MultiplayerProperty_OnValueChanged;
		}
	}

	private void MultiplayerProperty_OnValueChanged(bool arg0)
	{
		mpVehicleBlocker.SetActive(arg0);
	}

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		saveButton.interactable = false;
		if ((bool)multiplayerProperty)
		{
			multiplayerProperty.SetInitialValue(false);
		}
		mpVehicleBlocker.SetActive(value: false);
		foreach (GameObject displayedFile in displayedFiles)
		{
			Object.Destroy(displayedFile);
		}
		displayedFiles = new List<GameObject>();
		existingFileNames = new List<string>();
		VTResources.LoadCustomScenarios();
		List<VTCampaignInfo> customCampaigns = VTResources.GetCustomCampaigns();
		int num = 0;
		foreach (VTCampaignInfo item in customCampaigns)
		{
			GameObject gameObject = Object.Instantiate(filenameTemplate, scrollContentTf);
			gameObject.SetActive(value: true);
			Vector3 localPosition = filenameTemplate.transform.localPosition;
			localPosition.y = (float)(-num) * lineHeight;
			gameObject.transform.localPosition = localPosition;
			gameObject.GetComponent<Text>().text = item.campaignID + ".vtc";
			displayedFiles.Add(gameObject);
			existingFileNames.Add(item.campaignID);
			num++;
		}
		scrollContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 10f + (float)num * lineHeight);
		scrollRect.verticalNormalizedPosition = 1f;
		foreach (GameObject vehicleListObject in vehicleListObjects)
		{
			Object.Destroy(vehicleListObject);
		}
		vehicleListObjects = new List<GameObject>();
		vehicleTemplate.SetActive(value: false);
		VTResources.LoadPlayerVehicles();
		PlayerVehicle[] playerVehicles = VTResources.GetPlayerVehicles();
		float height = ((RectTransform)vehicleTemplate.transform).rect.height;
		for (int i = 0; i < playerVehicles.Length; i++)
		{
			GameObject gameObject2 = Object.Instantiate(vehicleTemplate, vehicleMenuBg);
			gameObject2.SetActive(value: true);
			gameObject2.transform.localPosition = new Vector3(0f, (float)(-i) * height, 0f);
			vehicleListObjects.Add(gameObject2);
			VehicleSelectButton vehicleSelectButton = gameObject2.AddComponent<VehicleSelectButton>();
			vehicleSelectButton.vehicle = playerVehicles[i].vehicleName;
			vehicleSelectButton.menu = this;
			gameObject2.GetComponentInChildren<Text>().text = playerVehicles[i].vehicleName;
			gameObject2.GetComponentInChildren<Button>().onClick.AddListener(vehicleSelectButton.OnClick);
		}
		vehicleMenuBg.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)playerVehicles.Length * height);
		vehicleMenuBg.gameObject.SetActive(value: false);
		vehicle = playerVehicles[0].vehicleName;
	}

	public void VehicleButton()
	{
		vehicleMenuBg.gameObject.SetActive(!vehicleMenuBg.gameObject.activeSelf);
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		textField.text = string.Empty;
	}

	public void OnEnteredText(string text)
	{
		saveButton.interactable = VTResources.IsValidFilename(text, existingFileNames);
		filename = text;
	}

	public void Save()
	{
		string text = Path.Combine(VTResources.customCampaignsDir, filename);
		Directory.CreateDirectory(text);
		ConfigNode configNode = VTCampaignInfo.CreateEmptyCampaignConfig(filename, vehicle);
		string filePath = Path.Combine(text, filename + ".vtc");
		if ((bool)multiplayerProperty)
		{
			configNode.SetValue("multiplayer", (bool)multiplayerProperty.GetValue());
		}
		configNode.SaveToFile(filePath);
		VTResources.LoadCustomScenarios();
		VTCampaignInfo customCampaign = VTResources.GetCustomCampaign(filename);
		campaignEditMenu.Open(customCampaign);
		Close();
	}

	public void CancelButton()
	{
		campaignMenu.Open();
		Close();
	}

	public void SelectVehicle(string vehicleName)
	{
		vehicle = vehicleName;
		vehicleMenuBg.gameObject.SetActive(value: false);
		selectedVehicleText.text = vehicleName;
	}
}
