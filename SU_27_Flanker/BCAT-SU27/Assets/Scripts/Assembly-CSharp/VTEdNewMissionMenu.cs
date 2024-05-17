using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class VTEdNewMissionMenu : MonoBehaviour
{
	public class MapItemButton : MonoBehaviour
	{
		public VTEdNewMissionMenu menu;

		public string mapID;

		public void OnClick()
		{
			menu.SelectMap(mapID);
		}
	}

	[Header("Window references")]
	public VTEdCampaignEditWindow campaignEditWindow;

	public VTEdCampaignEditWindow standaloneWindow;

	public VTECMenuConfirmDialogue confirmDialogue;

	[Header("File saving")]
	public InputField fileInputField;

	public Button saveButton;

	public VTBoolProperty isTrainingBool;

	[Header("Map select")]
	public Text selectedMapText;

	public GameObject mapItemTemplate;

	public RectTransform mapMenuTf;

	private List<GameObject> mapListObjects = new List<GameObject>();

	[Header("File list")]
	public ScrollRect filesScrollRect;

	public GameObject fileTemplate;

	private List<GameObject> fileListObjects = new List<GameObject>();

	public GameObject[] hideForMultiplayer;

	private List<string> existingFilenames = new List<string>();

	private string filename;

	private string vehicle;

	private string selectedMap;

	private VTCampaignInfo campaign;

	public VTMapMenuOpenUI openMapUI;

	public void Open(VTCampaignInfo campaign)
	{
		this.campaign = campaign;
		if (campaign != null)
		{
			vehicle = campaign.vehicle;
			hideForMultiplayer.SetActive(!campaign.multiplayer);
		}
		else
		{
			vehicle = VTResources.GetPlayerVehicles()[0].vehicleName;
		}
		isTrainingBool.SetInitialValue(false);
		selectedMap = VTResources.GetMaps()[0].mapID;
		selectedMapText.text = selectedMap;
		base.gameObject.SetActive(value: true);
		fileInputField.text = string.Empty;
		saveButton.interactable = false;
		foreach (GameObject fileListObject in fileListObjects)
		{
			Object.Destroy(fileListObject);
		}
		fileListObjects = new List<GameObject>();
		existingFilenames = new List<string>();
		List<VTScenarioInfo> list = ((campaign == null) ? VTResources.GetCustomScenarios() : campaign.allScenarios);
		float height = ((RectTransform)fileTemplate.transform).rect.height;
		for (int i = 0; i < list.Count; i++)
		{
			VTScenarioInfo vTScenarioInfo = list[i];
			GameObject gameObject = Object.Instantiate(fileTemplate, filesScrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * height, 0f);
			gameObject.GetComponentInChildren<Text>().text = vTScenarioInfo.id + ".vts";
			fileListObjects.Add(gameObject);
			existingFilenames.Add(vTScenarioInfo.id);
		}
		filesScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)list.Count * height);
		filesScrollRect.ClampVertical();
		fileTemplate.SetActive(value: false);
		foreach (GameObject mapListObject in mapListObjects)
		{
			Object.Destroy(mapListObject);
		}
		mapListObjects = new List<GameObject>();
		float height2 = ((RectTransform)mapItemTemplate.transform).rect.height;
		List<VTMap> maps = VTResources.GetMaps();
		for (int j = 0; j < maps.Count; j++)
		{
			VTMap vTMap = maps[j];
			GameObject gameObject2 = Object.Instantiate(mapItemTemplate, mapMenuTf);
			gameObject2.SetActive(value: true);
			gameObject2.transform.localPosition = new Vector3(0f, (float)(-j) * height2, 0f);
			gameObject2.GetComponentInChildren<Text>().text = vTMap.mapID;
			Button componentInChildren = gameObject2.GetComponentInChildren<Button>();
			MapItemButton mapItemButton = gameObject2.AddComponent<MapItemButton>();
			mapItemButton.menu = this;
			mapItemButton.mapID = vTMap.mapID;
			componentInChildren.onClick.AddListener(mapItemButton.OnClick);
			mapListObjects.Add(gameObject2);
		}
		mapMenuTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)maps.Count * height2);
		mapMenuTf.gameObject.SetActive(value: false);
		mapItemTemplate.SetActive(value: false);
	}

	public void MapButton()
	{
		openMapUI.Open();
		openMapUI.OnBackButtonPressed += OpenMapUI_OnBackButtonPressed;
		openMapUI.OnMapSelected += OpenMapUI_OnMapSelected;
	}

	private void OpenMapUI_OnMapSelected(VTMap obj)
	{
		openMapUI.OnMapSelected -= OpenMapUI_OnMapSelected;
		openMapUI.OnBackButtonPressed -= OpenMapUI_OnBackButtonPressed;
		openMapUI.gameObject.SetActive(value: false);
		SelectMap(obj.mapID);
	}

	private void OpenMapUI_OnBackButtonPressed()
	{
		openMapUI.OnMapSelected -= OpenMapUI_OnMapSelected;
		openMapUI.OnBackButtonPressed -= OpenMapUI_OnBackButtonPressed;
		openMapUI.gameObject.SetActive(value: false);
	}

	private void Close()
	{
		base.gameObject.SetActive(value: false);
	}

	private bool CheckIfPassesNameRequirements()
	{
		if (selectedMap == filename)
		{
			confirmDialogue.DisplayConfirmation("Invalid name", "The mission filename must be different than the map name", null, null);
			return false;
		}
		if (campaign != null)
		{
			foreach (VTScenarioInfo allScenario in campaign.allScenarios)
			{
				if (filename == allScenario.mapID)
				{
					confirmDialogue.DisplayConfirmation("Invalid name", "A map in the campaign already uses the filename: " + filename + "\nPlease use a different mission filename.", null, null);
					return false;
				}
			}
		}
		return true;
	}

	public void Save()
	{
		if (!CheckIfPassesNameRequirements())
		{
			return;
		}
		ConfigNode configNode = VTScenarioInfo.CreateEmptyScenarioConfig(filename, vehicle, selectedMap);
		bool flag = (bool)isTrainingBool.GetValue();
		configNode.SetValue("isTraining", flag);
		string path;
		if (campaign != null)
		{
			path = campaign.directoryPath;
			configNode.SetValue("campaignID", campaign.campaignID);
			configNode.SetValue("multiplayer", campaign.multiplayer);
			int value = (flag ? campaign.trainingScenarios.Count : campaign.missionScenarios.Count);
			configNode.SetValue("campaignOrderIdx", value);
		}
		else
		{
			path = VTResources.customScenariosDir;
		}
		string text = Path.Combine(path, filename);
		string filePath = Path.Combine(text, filename + ".vts");
		Directory.CreateDirectory(text);
		configNode.SaveToFile(filePath);
		if (campaign != null)
		{
			VTResources.LoadCustomCampaignAtPath(campaign.filePath, skipUnmodified: false);
		}
		VTResources.LoadCustomScenarios();
		if (campaign != null)
		{
			campaign = VTResources.GetCustomCampaign(campaign.campaignID);
			VTScenarioEditor.currentCampaign = campaign.campaignID;
		}
		else
		{
			VTScenarioEditor.currentCampaign = string.Empty;
		}
		string campaignID = ((campaign == null) ? string.Empty : campaign.campaignID);
		VTScenarioInfo customScenario = VTResources.GetCustomScenario(filename, campaignID);
		string sceneName;
		VTMap mapForScenario = VTResources.GetMapForScenario(customScenario, out sceneName);
		if (mapForScenario is VTMapCustom)
		{
			VTMapCustom vTMapCustom = (VTMapCustom)mapForScenario;
			string mapDir = vTMapCustom.mapDir;
			string path2 = customScenario.directoryPath;
			if (!string.IsNullOrEmpty(customScenario.campaignID))
			{
				path2 = Path.GetFullPath(Path.Combine(customScenario.directoryPath, ".."));
			}
			path2 = Path.Combine(path2, customScenario.mapID);
			if (!Directory.Exists(path2) || (!File.Exists(Path.Combine(path2, vTMapCustom.mapID + ".vtm")) && !File.Exists(Path.Combine(path2, vTMapCustom.mapID + ".vtmb"))))
			{
				VTResources.CopyDirectory(mapDir, path2, VTScenarioEditor.packMapExcludeExtensions);
			}
		}
		if (campaign != null)
		{
			campaignEditWindow.Open(campaign, filename);
			Close();
		}
		else
		{
			VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Editor;
			VTScenarioEditor.launchWithScenario = filename;
			VTResources.LaunchMapForScenario(customScenario, skipLoading: false);
		}
	}

	public void Cancel()
	{
		if (campaign != null)
		{
			campaignEditWindow.Open(campaign);
		}
		else
		{
			standaloneWindow.Open(null);
		}
		Close();
	}

	public void OnEnteredText(string text)
	{
		saveButton.interactable = VTResources.IsValidFilename(text, existingFilenames);
		filename = text;
	}

	public void SelectMap(string mapID)
	{
		selectedMap = mapID;
		selectedMapText.text = mapID;
		if ((bool)mapMenuTf)
		{
			mapMenuTf.gameObject.SetActive(value: false);
		}
	}
}
