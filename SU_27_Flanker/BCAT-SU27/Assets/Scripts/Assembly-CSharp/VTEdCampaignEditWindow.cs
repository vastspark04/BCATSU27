using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VTOLVR.Multiplayer;
using VTOLVR.SteamWorkshop;

public class VTEdCampaignEditWindow : MonoBehaviour
{
	public class CampaignScenarioItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		public int idx;

		public VTEdCampaignEditWindow window;

		public bool allowDrag;

		private float lastClickTime;

		private float startDragY;

		public void OnPointerClick(PointerEventData e)
		{
			if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				window.EditButton();
				return;
			}
			lastClickTime = Time.unscaledTime;
			window.SelectScenario(idx);
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (allowDrag)
			{
				startDragY = base.transform.localPosition.y;
			}
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (allowDrag)
			{
				base.transform.localPosition += eventData.delta.y * Vector3.up;
				window.DragScenario(base.transform.localPosition.y);
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (allowDrag)
			{
				window.EndDrag(idx, base.transform.localPosition.y);
			}
		}
	}

	public VTEditMainMenu mainMenu;

	public VTEdCampaignMenu campaignMenu;

	public bool standaloneWindow;

	private bool standaloneImporter;

	public VTEdNewMissionMenu newMissionMenu;

	[Header("Campaign Info")]
	public Text fileNameText;

	public InputField campaignNameInputField;

	public RawImage campaignImage;

	public InputField campaignDescriptionInputField;

	public Button saveButton;

	public Button repairButton;

	public VTEdCampaignEditWindow standaloneImporterWindow;

	[Header("Missions")]
	public Text missionNameText;

	public Button[] itemDependentButtons;

	public ScrollRect missionsScrollRect;

	public GameObject missionItemTemplate;

	public Transform selectionTf;

	private float lineHeight;

	public RawImage missionImage;

	public Text mapIDText;

	public Text missionDescriptionText;

	private ContentSizeFitter descriptionFitter;

	public ScrollRect missionDescriptionScrollRect;

	private List<GameObject> listObjects = new List<GameObject>();

	public VTEnumProperty availabilityModeProp;

	public GameObject[] hideOnStandaloneImporter;

	public GameObject[] showOnStandaloneImporter;

	public Button missionsButton;

	public Button trainingsButton;

	[Header("Vehicle")]
	public Text vehicleText;

	[Header("Dragging UI")]
	public GameObject dragPositionIndicator;

	[Header("Steam Workshop")]
	public GameObject uploadButtonObj;

	public VTEdProgressWindow progressWindow;

	public VTEdTextInputWindow textInput;

	[Header("Launch Mission")]
	public VTEdPilotSelectWindow pilotSelectWindow;

	[Header("Special")]
	public GameObject convertToMPButton;

	public GameObject[] showForMultiplayer;

	public GameObject[] hideForMultiplayer;

	private VTCampaignInfo campaign;

	private List<VTScenarioInfo> scenarios;

	private int currIdx;

	private bool viewingMissions = true;

	private string newImagePath = string.Empty;

	public event Action<VTScenarioInfo> OnSelectedImport;

	private void Awake()
	{
		if (!standaloneWindow)
		{
			campaignNameInputField.onValueChanged.AddListener(SetCampaignInfoDirty);
			campaignDescriptionInputField.onValueChanged.AddListener(SetCampaignInfoDirty);
			availabilityModeProp.OnPropertyValueChanged += SetCampaignInfoDirty;
		}
		if ((bool)uploadButtonObj)
		{
			if (SteamClient.IsValid)
			{
				uploadButtonObj.SetActive(value: true);
			}
			else
			{
				uploadButtonObj.SetActive(value: false);
			}
		}
	}

	public void Open(VTCampaignInfo campaign, string selectScenario)
	{
		Open(campaign);
		MissionsButton();
		int num = scenarios.FindIndex((VTScenarioInfo x) => x.id == selectScenario);
		if (!standaloneWindow && num < 0)
		{
			TrainingsButton();
			num = scenarios.FindIndex((VTScenarioInfo x) => x.id == selectScenario);
		}
		SelectScenario(num);
	}

	public void MissionsButton()
	{
		viewingMissions = true;
		SetupList(campaign.missionScenarios);
		missionsButton.interactable = false;
		trainingsButton.interactable = true;
	}

	public void TrainingsButton()
	{
		viewingMissions = false;
		SetupList(campaign.trainingScenarios);
		missionsButton.interactable = true;
		trainingsButton.interactable = false;
	}

	public void Open(VTCampaignInfo campaign, bool standaloneImporter = false)
	{
		this.standaloneImporter = standaloneImporter;
		base.gameObject.SetActive(value: true);
		if (!standaloneImporter)
		{
			this.campaign = campaign;
		}
		if (!standaloneWindow)
		{
			fileNameText.text = campaign.campaignID + ".vtc";
			campaignNameInputField.text = campaign.campaignName;
			if (campaign.image != null)
			{
				campaignImage.texture = campaign.image;
			}
			else
			{
				campaignImage.texture = campaignMenu.defaultImage;
			}
			campaignDescriptionInputField.text = campaign.description;
			vehicleText.text = campaign.vehicle;
			viewingMissions = true;
			trainingsButton.interactable = true;
			missionsButton.interactable = false;
			if ((bool)convertToMPButton)
			{
				convertToMPButton.SetActive(!campaign.multiplayer);
			}
			showForMultiplayer.SetActive(campaign.multiplayer);
			hideForMultiplayer.SetActive(!campaign.multiplayer);
		}
		else
		{
			if (hideOnStandaloneImporter != null)
			{
				hideOnStandaloneImporter.SetActive(!standaloneImporter);
			}
			if (showOnStandaloneImporter != null)
			{
				showOnStandaloneImporter.SetActive(standaloneImporter);
			}
		}
		currIdx = -1;
		descriptionFitter = missionDescriptionText.GetComponent<ContentSizeFitter>();
		lineHeight = ((RectTransform)missionItemTemplate.transform).rect.height;
		missionItemTemplate.SetActive(value: false);
		if (standaloneWindow)
		{
			if (standaloneImporter)
			{
				List<VTScenarioInfo> customScenarios = VTResources.GetCustomScenarios();
				customScenarios.RemoveAll((VTScenarioInfo x) => x.vehicle.vehicleName != campaign.vehicle);
				SetupList(customScenarios);
			}
			else
			{
				SetupList(VTResources.GetCustomScenarios());
			}
		}
		else
		{
			SetupList(campaign.missionScenarios);
		}
		if (!standaloneWindow)
		{
			saveButton.interactable = false;
			saveButton.GetComponentInChildren<Text>().text = "Saved";
			availabilityModeProp.SetInitialValue(campaign.availability);
		}
		else
		{
			vehicleText.text = string.Empty;
		}
		if (!standaloneWindow && campaign != null)
		{
			if (VTCampaignInfo.RepairCampaignFileStructure(campaign, campaign.directoryPath, doRepair: false))
			{
				repairButton.gameObject.SetActive(value: true);
				RepairCampaignButton();
			}
			else
			{
				repairButton.gameObject.SetActive(value: false);
			}
		}
	}

	private void SetupList(List<VTScenarioInfo> scenariosList)
	{
		foreach (GameObject listObject in listObjects)
		{
			UnityEngine.Object.Destroy(listObject);
		}
		listObjects = new List<GameObject>();
		if (!standaloneWindow)
		{
			scenariosList.Sort((VTScenarioInfo a, VTScenarioInfo b) => a.campaignOrderIdx.CompareTo(b.campaignOrderIdx));
		}
		ScrollRect scrollRect = missionsScrollRect;
		scenarios = scenariosList;
		for (int i = 0; i < scenariosList.Count; i++)
		{
			VTScenarioInfo vTScenarioInfo = scenariosList[i];
			GameObject gameObject = UnityEngine.Object.Instantiate(missionItemTemplate, scrollRect.content);
			gameObject.gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			listObjects.Add(gameObject);
			string text = vTScenarioInfo.id;
			if (!standaloneWindow)
			{
				text = vTScenarioInfo.campaignOrderIdx + 1 + " : " + text;
			}
			gameObject.GetComponentInChildren<Text>().text = text;
			CampaignScenarioItem campaignScenarioItem = gameObject.AddComponent<CampaignScenarioItem>();
			campaignScenarioItem.idx = i;
			campaignScenarioItem.window = this;
			if (!standaloneWindow)
			{
				campaignScenarioItem.allowDrag = true;
			}
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0.5f * lineHeight + (float)scenariosList.Count * lineHeight);
		scrollRect.ClampVertical();
		missionNameText.text = string.Empty;
		missionDescriptionText.text = string.Empty;
		missionDescriptionScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1f);
		missionImage.texture = campaignMenu.defaultImage;
		mapIDText.text = string.Empty;
		selectionTf.gameObject.SetActive(value: false);
		itemDependentButtons.SetInteractable(interactable: false);
	}

	public void SelectScenario(int idx)
	{
		currIdx = idx;
		selectionTf.gameObject.SetActive(value: true);
		selectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
		VTScenarioInfo vTScenarioInfo = scenarios[idx];
		missionNameText.text = vTScenarioInfo.name;
		missionDescriptionText.text = vTScenarioInfo.description;
		descriptionFitter.SetLayoutVertical();
		missionDescriptionScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, missionDescriptionText.rectTransform.rect.height);
		if (vTScenarioInfo.image != null)
		{
			missionImage.texture = vTScenarioInfo.image;
		}
		else
		{
			missionImage.texture = campaignMenu.defaultImage;
		}
		mapIDText.text = vTScenarioInfo.mapID;
		itemDependentButtons.SetInteractable(interactable: true);
		if (standaloneWindow)
		{
			vehicleText.text = vTScenarioInfo.vehicle.vehicleName;
		}
	}

	public void NewMissionButton()
	{
		if ((bool)saveButton && saveButton.interactable)
		{
			mainMenu.confirmDialogue.DisplayConfirmation("Unsaved Changes", "Are you sure you want to continue? There are unsaved changes in the campaign info panel.", FinallyNewMission, null);
		}
		else
		{
			FinallyNewMission();
		}
	}

	private void FinallyNewMission()
	{
		newMissionMenu.Open(campaign);
		base.gameObject.SetActive(value: false);
	}

	public void EditButton()
	{
		if ((bool)saveButton && saveButton.interactable)
		{
			mainMenu.confirmDialogue.DisplayConfirmation("Unsaved Changes", "Are you sure you want to continue? There are unsaved changes in the campaign info panel.", FinallyEdit, null);
		}
		else
		{
			FinallyEdit();
		}
	}

	private void FinallyEdit()
	{
		if (campaign != null)
		{
			VTScenarioEditor.currentCampaign = campaign.campaignID;
		}
		else
		{
			VTScenarioEditor.currentCampaign = string.Empty;
		}
		VTScenarioInfo vTScenarioInfo = scenarios[currIdx];
		VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Editor;
		VTScenarioEditor.launchWithScenario = vTScenarioInfo.id;
		VTScenario.currentScenarioInfo = vTScenarioInfo;
		VTResources.LaunchMapForScenario(vTScenarioInfo, skipLoading: false);
	}

	public void LaunchMissionButton()
	{
		VTScenarioEditor.launchWithScenario = scenarios[currIdx].id;
		VTScenarioEditor.currentCampaign = scenarios[currIdx].campaignID;
		pilotSelectWindow.Open(scenarios[currIdx]);
	}

	public void SelectStandaloneImportButton()
	{
		this.OnSelectedImport?.Invoke(scenarios[currIdx]);
		base.gameObject.SetActive(value: false);
	}

	public void DeleteButton()
	{
		mainMenu.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this mission?", FinallyDelete, null);
	}

	private void FinallyDelete()
	{
		VTResources.DeleteCustomScenario(scenarios[currIdx].id, (campaign == null) ? null : campaign.campaignID);
		if (!standaloneWindow)
		{
			for (int i = 0; i < scenarios.Count; i++)
			{
				if (i > currIdx)
				{
					scenarios[i].campaignOrderIdx--;
					scenarios[i].SaveNewOrderIdx();
				}
			}
		}
		VTResources.LoadCustomScenarios();
		if (campaign != null)
		{
			campaign = VTResources.GetCustomCampaign(campaign.campaignID);
		}
		if (standaloneWindow)
		{
			Open(null);
		}
		else
		{
			Open(campaign);
		}
	}

	public void BackButton()
	{
		if (standaloneImporter)
		{
			this.OnSelectedImport?.Invoke(null);
			base.gameObject.SetActive(value: false);
		}
		else if ((bool)saveButton && saveButton.interactable)
		{
			mainMenu.confirmDialogue.DisplayConfirmation("Unsaved changes", "Are you sure you want to leave? There are unsaved changes in the campaign info panel.", FinallyBack, null);
		}
		else
		{
			FinallyBack();
		}
	}

	private void FinallyBack()
	{
		if (standaloneWindow)
		{
			mainMenu.Open();
		}
		else
		{
			campaignMenu.Open();
		}
		base.gameObject.SetActive(value: false);
	}

	public void SaveButton()
	{
		Debug.Log("Saving campaign " + campaign.campaignID);
		campaign.campaignName = campaignNameInputField.text;
		campaign.description = campaignDescriptionInputField.text;
		if (campaign.multiplayer)
		{
			campaign.availability = VTCampaignInfo.AvailabilityModes.All_Available;
		}
		else
		{
			campaign.availability = (VTCampaignInfo.AvailabilityModes)availabilityModeProp.GetValue();
		}
		if (!string.IsNullOrEmpty(newImagePath))
		{
			Path.GetFileName(newImagePath);
			string directoryPath = campaign.directoryPath;
			string[] files = Directory.GetFiles(directoryPath);
			foreach (string path in files)
			{
				string fileName = Path.GetFileName(path);
				if (File.Exists(path) && (fileName == "image.png" || fileName == "image.jpg"))
				{
					File.Delete(path);
				}
			}
			string extension = Path.GetExtension(newImagePath);
			File.Copy(newImagePath, Path.Combine(directoryPath, "image" + extension));
			VTResources.LoadCustomScenarios();
		}
		newImagePath = string.Empty;
		campaign.SaveToConfigNode().SaveToFile(campaign.filePath);
		saveButton.interactable = false;
		saveButton.GetComponentInChildren<Text>().text = "Saved";
	}

	private void SetCampaignInfoDirty()
	{
		saveButton.interactable = true;
		saveButton.GetComponentInChildren<Text>().text = "Save";
	}

	private void SetCampaignInfoDirty(object o)
	{
		SetCampaignInfoDirty();
	}

	private void SetCampaignInfoDirty(string text)
	{
		SetCampaignInfoDirty();
	}

	public void ChangeImageButton()
	{
		mainMenu.resourceBrowser.OpenBrowser("Select Image", OnSelectedImage, VTResources.supportedImageExtensions);
	}

	public void SelectStartingEquipButton()
	{
		PlayerVehicle playerVehicle = VTResources.GetPlayerVehicle(campaign.vehicle);
		List<string> list = new List<string>();
		List<int> list2 = new List<int>();
		for (int i = 0; i < playerVehicle.allEquipPrefabs.Count; i++)
		{
			HPEquippable component = playerVehicle.allEquipPrefabs[i].GetComponent<HPEquippable>();
			list.Add(component.fullName);
			if (campaign.startingEquips.Contains(component.gameObject.name))
			{
				list2.Add(i);
			}
		}
		mainMenu.multiSelector.Display("Starting Equips", list.ToArray(), list2, OnSelectedEquips);
	}

	private void OnSelectedEquips(int[] selected)
	{
		PlayerVehicle playerVehicle = VTResources.GetPlayerVehicle(campaign.vehicle);
		campaign.startingEquips = new List<string>();
		foreach (int index in selected)
		{
			campaign.startingEquips.Add(playerVehicle.allEquipPrefabs[index].name);
		}
		SetCampaignInfoDirty(string.Empty);
	}

	private void OnSelectedImage(string path)
	{
		newImagePath = path;
		SetCampaignInfoDirty(string.Empty);
		campaignImage.texture = VTResources.GetTexture(path);
	}

	public void DragScenario(float yPos)
	{
		dragPositionIndicator.SetActive(value: true);
		dragPositionIndicator.transform.localPosition = new Vector3(0f, (float)(-DragIndex(yPos)) * lineHeight, 0f);
	}

	public void EndDrag(int idx, float yPos)
	{
		dragPositionIndicator.SetActive(value: false);
		int num = DragIndex(yPos);
		if (num > idx)
		{
			num--;
		}
		if (idx != num)
		{
			for (int i = 0; i < scenarios.Count; i++)
			{
				if (i == idx)
				{
					scenarios[i].campaignOrderIdx = num;
				}
				else if (i <= num && i > idx)
				{
					scenarios[i].campaignOrderIdx--;
				}
				else if (i >= num && i < idx)
				{
					scenarios[i].campaignOrderIdx++;
				}
				scenarios[i].SaveNewOrderIdx();
			}
		}
		VTResources.LoadCustomScenarios();
		VTCampaignInfo customCampaign = VTResources.GetCustomCampaign(campaign.campaignID);
		Open(customCampaign, scenarios[idx].id);
	}

	private int DragIndex(float yPos)
	{
		return Mathf.Clamp(Mathf.CeilToInt((0f - yPos) / lineHeight), 0, scenarios.Count);
	}

	public void UploadCampaignToWorkshop()
	{
		Queue<VTSteamWorkshopUtils.ScenarioValidation> vQueue = new Queue<VTSteamWorkshopUtils.ScenarioValidation>();
		VTSteamWorkshopUtils.ScenarioValidation item = VTSteamWorkshopUtils.ValidateCampaign(campaign.campaignID);
		if (!item.valid)
		{
			vQueue.Enqueue(item);
		}
		foreach (VTScenarioInfo allScenario in campaign.allScenarios)
		{
			VTSteamWorkshopUtils.ScenarioValidation item2 = VTSteamWorkshopUtils.ValidateScenarioForUpload(allScenario.id, campaign.campaignID);
			if (!item2.valid)
			{
				vQueue.Enqueue(item2);
			}
		}
		if (vQueue.Count > 0)
		{
			UnityAction ShowNextError = null;
			ShowNextError = delegate
			{
				if (vQueue.Count > 0)
				{
					VTSteamWorkshopUtils.ScenarioValidation scenarioValidation = vQueue.Dequeue();
					StringBuilder stringBuilder = new StringBuilder();
					if (scenarioValidation.scenarioInfo != null)
					{
						stringBuilder.AppendLine("Invalid scenario: " + scenarioValidation.scenarioInfo.id);
					}
					else if (scenarioValidation.campaignInfo != null)
					{
						stringBuilder.AppendLine("Invalid campaign:");
					}
					else
					{
						stringBuilder.AppendLine("Invalid:");
					}
					foreach (string message in scenarioValidation.messages)
					{
						stringBuilder.AppendLine(message);
					}
					mainMenu.confirmDialogue.DisplayConfirmation("Invalid Scenario", stringBuilder.ToString(), ShowNextError, ShowNextError);
				}
			};
			ShowNextError();
		}
		else
		{
			mainMenu.confirmDialogue.DisplayConfirmation("Upload?", "Upload this campaign to the Steam Workshop?", FinallyUploadCampaignToSteam, null);
		}
	}

	private void OnRequestChangeNote(Action<string> ChangeNote, Action Cancel)
	{
		textInput.Display("Change Note", "Updating existing workshop content.  Enter change notes.", string.Empty, 140, ChangeNote, Cancel);
		progressWindow.SetDone();
	}

	private void OnBeginUpdate(WorkshopItemUpdate u)
	{
		progressWindow.SetDone();
		progressWindow.Display("Uploading", "Uploading campaign to Steam Workshop", () => u.GetUploadProgress(), null);
	}

	private void OnUploadComplete(WorkshopItemUpdateEventArgs args)
	{
		progressWindow.SetDone();
		if (args.IsError)
		{
			mainMenu.confirmDialogue.DisplayConfirmation("Error", "Upload error! " + args.ErrorMessage, null, null);
		}
		else
		{
			mainMenu.confirmDialogue.DisplayConfirmation("Success", "Upload successful!", null, null);
		}
	}

	private void FinallyUploadCampaignToSteam()
	{
		float t = Time.time;
		progressWindow.Display("Preparing", "Preparing for upload...", () => (Time.time - t) / 2f, null);
		VTResources.UploadCampaignToSteamWorkshop(campaign.campaignID, OnRequestChangeNote, OnBeginUpdate, OnUploadComplete);
	}

	public void RepairCampaignButton()
	{
		mainMenu.confirmDialogue.DisplayConfirmation("Repair campaign?", "This campaign's file structure has known problems which can be corrected.  Do you want to try to repair it?", delegate
		{
			if (VTCampaignInfo.RepairCampaignFileStructure(campaign, campaign.directoryPath))
			{
				UnityAction unityAction = delegate
				{
					string campaignID = campaign.campaignID;
					_ = campaign.filePath;
					VTResources.LoadCustomCampaignAtPath(campaign.filePath, skipUnmodified: false);
					VTCampaignInfo customCampaign = VTResources.GetCustomCampaign(campaignID);
					Open(customCampaign);
				};
				mainMenu.confirmDialogue.DisplayConfirmation("Repaired", "The campaign file structure had issues and was repaired successfully!", unityAction, unityAction);
			}
			else
			{
				mainMenu.confirmDialogue.DisplayConfirmation("No repairs", "The campaign did not need any repairs.", null, null);
			}
		}, null);
	}

	public void ImportStandaloneButton()
	{
		standaloneImporterWindow.OnSelectedImport += OnImportedMission;
		standaloneImporterWindow.Open(campaign, standaloneImporter: true);
		base.gameObject.SetActive(value: false);
	}

	private void OnImportedMission(VTScenarioInfo s)
	{
		base.gameObject.SetActive(value: true);
		standaloneImporterWindow.OnSelectedImport -= OnImportedMission;
		if (s == null)
		{
			return;
		}
		Debug.LogFormat("Importing scenario {0} to campaign {1}", s.id, campaign.campaignID);
		string id = s.id;
		string id2 = s.id;
		string empty = string.Empty;
		int num = 0;
		bool flag = true;
		string text = id2 + empty;
		while (flag)
		{
			flag = false;
			foreach (VTScenarioInfo allScenario in campaign.allScenarios)
			{
				if (allScenario.id == text)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				num++;
				empty = num.ToString();
				text = id2 + empty;
			}
		}
		Debug.Log("old dir: " + s.directoryPath);
		string text2 = Path.Combine(campaign.directoryPath, Path.GetFileName(s.directoryPath));
		text2 = text2.Substring(0, text2.Length - id.Length) + text;
		Debug.Log("new dir: " + text2);
		VTResources.CopyDirectory(s.directoryPath, text2, new string[1] { ".meta" });
		string sourceFileName = Path.Combine(text2, id + ".vts");
		string text3 = Path.Combine(text2, text + ".vts");
		File.Move(sourceFileName, text3);
		ConfigNode configNode = ConfigNode.LoadFromFile(text3);
		string[] files = Directory.GetFiles(text2, "*.vtm", SearchOption.AllDirectories);
		foreach (string text4 in files)
		{
			string text5 = text4.Substring(0, text4.Length - (Path.GetFileName(text4).Length + 1));
			Debug.Log("Found map dir: " + text5);
			string fileName = Path.GetFileName(text5);
			string text6 = Path.Combine(campaign.directoryPath, fileName);
			Debug.Log(" - new map dir: " + text6);
			if (Directory.Exists(text6))
			{
				bool flag2 = false;
				string[] files2 = Directory.GetFiles(text6, "*.vtm", SearchOption.TopDirectoryOnly);
				for (int j = 0; j < files2.Length; j++)
				{
					_ = files2[j];
					flag2 = true;
					Debug.LogError("A map with the same ID already exists here.");
					Directory.Delete(text5, recursive: true);
				}
				if (flag2)
				{
					continue;
				}
				Debug.Log(" - a directory with the same name as the map exists, and it is not a map.  Renaming map.");
				bool flag3 = true;
				string text7 = fileName;
				int num2 = 0;
				string text8 = text7 + num2;
				while (flag3)
				{
					if (Directory.Exists(Path.Combine(campaign.directoryPath, text8)))
					{
						num2++;
						text8 = text7 + num2;
					}
					else
					{
						flag3 = false;
					}
				}
				text6 = Path.Combine(campaign.directoryPath, text8);
				Directory.Move(text5, text6);
				string sourceFileName2 = Path.Combine(text6, fileName + ".vtm");
				string text9 = Path.Combine(text6, text8 + ".vtm");
				Debug.Log(" - new map file: " + text9);
				File.Move(sourceFileName2, text9);
				ConfigNode.LoadFromFile(text9).SetValue("mapID", text8);
				configNode.SetValue("mapID", text8);
			}
			else
			{
				Directory.Move(text5, text6);
			}
		}
		configNode.SetValue("scenarioID", text);
		configNode.SetValue("campaignID", campaign.campaignID);
		configNode.SetValue("campaignOrderIdx", viewingMissions ? campaign.missionScenarios.Count : campaign.trainingScenarios.Count);
		configNode.SetValue("isTraining", !viewingMissions);
		configNode.SaveToFile(text3);
		string campaignID = campaign.campaignID;
		VTResources.LoadCustomScenarios();
		Open(VTResources.GetCustomCampaign(campaignID), text);
	}

	public void ConvertToMP()
	{
		VTCampaignInfo vTCampaignInfo = VTOLMPUtils.ConvertSingleToMultiplayerCampaign(campaign);
		Open(vTCampaignInfo);
	}
}
