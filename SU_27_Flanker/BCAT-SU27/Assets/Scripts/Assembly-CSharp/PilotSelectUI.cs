using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class PilotSelectUI : MonoBehaviour
{
	public GameObject mainDisplayObject;

	public GameObject settingsObject;

	[Header("Select Pilot")]
	public GameObject selectPilotDisplayObject;

	public GameObject selectPilotButtons;

	public GameObject confirmDeleteObject;

	public GameObject startSelectedPilotButton;

	public GameObject deletePilotButton;

	public GameObject pilotInfoTemplate;

	private Transform pilotInfoParent;

	private float pilotInfoWidth;

	private int pilotIdx;

	private List<PilotSave> pilots;

	[Header("New Pilot")]
	public GameObject createPilotDisplayObject;

	public Text newPilotNameText;

	public GameObject[] newPilotButtons;

	public GameObject newPilotStartButton;

	public GameObject invalidNameObject;

	public VRKeyboard keyboard;

	private string newPilotName = string.Empty;

	[Header("Vehicle Select")]
	private List<PlayerVehicle> vehicles;

	public GameObject vehicleSelectDisplayObject;

	public GameObject vehicleInfoTemplate;

	public Text vehicleMenuPilotName;

	private int vehicleIdx;

	private float vehicleInfoWidth;

	private Transform vehicleParent;

	public CampaignSelectorUI campaginUI;

	public PilotColorEditorUI pilotColorEditor;

	[Header("Tutorials")]
	public BuiltInTutorialsUI tutorialsUI;

	[Header("Editor")]
	public GameObject edConfirmObject;

	public GameObject mapEdConfirmObject;

	[Header("Steam Workshop")]
	public GameObject workshopBrowserObj;

	[Header("MP")]
	public VTMPMainMenu mpMenu;

	public static bool wasMP;

	public static bool returnToArcadeTutorial;

	public AudioClip menuBGM;

	public AudioClip creditsBGM;

	public CreditsScroller credits;

	private void Start()
	{
		VTScenario.current = null;
		VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
		mainDisplayObject.SetActive(value: true);
		createPilotDisplayObject.SetActive(value: false);
		selectPilotDisplayObject.SetActive(value: false);
		vehicleSelectDisplayObject.SetActive(value: false);
		PilotSaveManager.LoadPilotsFromFile();
		vehicles = PilotSaveManager.GetVehicleList();
		if (PilotSaveManager.current != null)
		{
			if (returnToArcadeTutorial)
			{
				ArcadeTutorialButton();
			}
			else
			{
				SelectPilotButton();
				pilotIdx = pilots.IndexOf(PilotSaveManager.current);
				StartCurrentPilot();
				if (BuiltInTutorialsUI.usingTutorialsUI)
				{
					OpenTutorialsPage();
				}
				else if ((bool)PilotSaveManager.currentVehicle)
				{
					SelectVehicleButton();
				}
			}
		}
		else
		{
			PilotSaveManager.currentVehicle = null;
			PilotSaveManager.currentCampaign = null;
			PilotSaveManager.currentScenario = null;
		}
		ScreenFader.FadeIn();
		ControllerEventHandler.UnpauseEvents();
	}

	public void OpenWorkshopBrowser()
	{
		vehicleSelectDisplayObject.SetActive(value: false);
		workshopBrowserObj.SetActive(value: true);
		GetComponentInParent<VRPointInteractableCanvas>().RefreshInteractables();
	}

	public void CloseWorkshopBrowser()
	{
		workshopBrowserObj.SetActive(value: false);
		vehicleSelectDisplayObject.SetActive(value: true);
	}

	public void OpenTutorialsPage()
	{
		vehicleSelectDisplayObject.SetActive(value: false);
		tutorialsUI.Open();
	}

	public void CloseTutorialsPage()
	{
		tutorialsUI.gameObject.SetActive(value: false);
		vehicleSelectDisplayObject.SetActive(value: true);
		BuiltInTutorialsUI.usingTutorialsUI = false;
	}

	private void Update()
	{
		if (selectPilotDisplayObject.activeSelf && (bool)pilotInfoParent)
		{
			pilotInfoParent.localPosition = Vector3.Lerp(pilotInfoParent.localPosition, new Vector3((float)(-pilotIdx) * pilotInfoWidth, 0f, 0f), 12f * Time.deltaTime);
		}
		if (vehicleSelectDisplayObject.activeSelf && (bool)vehicleParent)
		{
			vehicleParent.localPosition = Vector3.Lerp(vehicleParent.localPosition, new Vector3((float)(-vehicleIdx) * vehicleInfoWidth, 0f, 0f), 12f * Time.deltaTime);
		}
	}

	public void ShowSettingsButton()
	{
		mainDisplayObject.SetActive(value: false);
		settingsObject.SetActive(value: true);
	}

	public void HideSettingsButton()
	{
		mainDisplayObject.SetActive(value: true);
		settingsObject.SetActive(value: false);
	}

	private void SetupVehicleScreen()
	{
		if ((bool)vehicleParent)
		{
			UnityEngine.Object.Destroy(vehicleParent.gameObject);
		}
		vehicleParent = new GameObject("vehicles").transform;
		vehicleParent.parent = vehicleInfoTemplate.transform.parent;
		vehicleParent.transform.localPosition = Vector3.zero;
		vehicleParent.transform.localRotation = Quaternion.identity;
		vehicleParent.transform.localScale = Vector3.one;
		vehicleIdx = 0;
		vehicleInfoWidth = ((RectTransform)vehicleInfoTemplate.transform).rect.width;
		int num = 0;
		foreach (PlayerVehicle vehicle in vehicles)
		{
			if (PilotSaveManager.current.lastVehicleUsed == vehicle.vehicleName)
			{
				vehicleIdx = num;
			}
			GameObject obj = UnityEngine.Object.Instantiate(vehicleInfoTemplate, vehicleParent);
			obj.transform.localPosition = new Vector3((float)num * vehicleInfoWidth, 0f, 0f);
			obj.GetComponent<VehicleSelectUI>().UpdateUI(vehicle);
			obj.SetActive(value: true);
			num++;
		}
		vehicleMenuPilotName.text = PilotSaveManager.current.pilotName;
		vehicleInfoTemplate.SetActive(value: false);
	}

	public void NextVehicleButton()
	{
		vehicleIdx = (vehicleIdx + 1) % vehicles.Count;
	}

	public void PrevVehicleButton()
	{
		vehicleIdx--;
		if (vehicleIdx < 0)
		{
			vehicleIdx = vehicles.Count - 1;
		}
	}

	public void SelectVehicleButton()
	{
		SelectVehicle(vehicles[vehicleIdx]);
	}

	private void SelectVehicle(PlayerVehicle vehicle, Action onOpenedCSelector = null)
	{
		if ((VTResources.isEditorOrDevTools || vehicle.readyToFly) && (!vehicle.dlc || SteamApps.IsDlcInstalled(vehicle.dlcID)) && (bool)vehicle.vehiclePrefab)
		{
			PilotSaveManager.currentVehicle = vehicle;
			string vehicleName = vehicle.vehicleName;
			PilotSaveManager.current.GetVehicleSave(vehicleName);
			PilotSaveManager.current.lastVehicleUsed = vehicleName;
			PilotSaveManager.SavePilotsToFile();
			vehicleSelectDisplayObject.SetActive(value: false);
			campaginUI.OpenCampaignSelector(onOpenedCSelector);
		}
	}

	public void VehicleToPilotSelectScreen()
	{
		vehicleSelectDisplayObject.SetActive(value: false);
		selectPilotDisplayObject.SetActive(value: true);
		SetupSelectPilotScreen(resetIdx: false);
	}

	public void SelectPilotButton()
	{
		mainDisplayObject.SetActive(value: false);
		selectPilotDisplayObject.SetActive(value: true);
		SetupSelectPilotScreen();
	}

	public void StartSelectedPilotButton()
	{
		PilotSaveManager.current = pilots[pilotIdx];
		selectPilotDisplayObject.SetActive(value: false);
		vehicleSelectDisplayObject.SetActive(value: true);
		SetupVehicleScreen();
	}

	private void StartCurrentPilot()
	{
		selectPilotDisplayObject.SetActive(value: false);
		vehicleSelectDisplayObject.SetActive(value: true);
		SetupVehicleScreen();
	}

	public void DeletePilotButton()
	{
		confirmDeleteObject.SetActive(value: true);
		selectPilotButtons.SetActive(value: false);
	}

	public void CancelDeleteButton()
	{
		confirmDeleteObject.SetActive(value: false);
		selectPilotButtons.SetActive(value: true);
	}

	public void ConfirmDeleteButton()
	{
		string pilotName = pilots[pilotIdx].pilotName;
		PilotSaveManager.pilots.Remove(pilotName);
		PilotSaveManager.SavePilotsToFile();
		PilotSaveManager.LoadPilotsFromFile();
		SetupSelectPilotScreen();
	}

	public void NextPilotButton()
	{
		if (pilots.Count != 0)
		{
			pilotIdx = (pilotIdx + 1) % pilots.Count;
		}
	}

	public void PrevPilotButton()
	{
		if (pilots.Count != 0)
		{
			pilotIdx--;
			if (pilotIdx < 0)
			{
				pilotIdx = pilots.Count - 1;
			}
		}
	}

	public void BackToVehicle()
	{
		vehicleSelectDisplayObject.SetActive(value: true);
		SetupVehicleScreen();
	}

	private void SetupSelectPilotScreen(bool resetIdx = true)
	{
		PilotSaveManager.SavePilotsToFile();
		PilotSaveManager.LoadPilotsFromFile();
		if (resetIdx)
		{
			pilotIdx = 0;
		}
		pilots = new List<PilotSave>();
		if ((bool)pilotInfoParent)
		{
			UnityEngine.Object.Destroy(pilotInfoParent.gameObject);
		}
		pilotInfoParent = new GameObject("pilots").transform;
		pilotInfoParent.parent = pilotInfoTemplate.transform.parent;
		pilotInfoParent.localPosition = Vector3.zero;
		pilotInfoParent.localRotation = Quaternion.identity;
		pilotInfoParent.localScale = Vector3.one;
		pilotInfoWidth = ((RectTransform)pilotInfoTemplate.transform).rect.width;
		int num = 0;
		foreach (PilotSave value in PilotSaveManager.pilots.Values)
		{
			pilots.Add(value);
			GameObject obj = UnityEngine.Object.Instantiate(pilotInfoTemplate, pilotInfoParent);
			obj.GetComponent<PilotSelectInfoUI>().UpdateUI(value);
			obj.transform.localPosition = new Vector3((float)num * pilotInfoWidth, 0f, 0f);
			if (!resetIdx && PilotSaveManager.current != null && PilotSaveManager.current.pilotName == value.pilotName)
			{
				pilotIdx = num;
			}
			obj.SetActive(value: true);
			num++;
		}
		if (pilots.Count > 0)
		{
			deletePilotButton.SetActive(value: true);
			startSelectedPilotButton.SetActive(value: true);
		}
		else
		{
			deletePilotButton.SetActive(value: false);
			startSelectedPilotButton.SetActive(value: false);
		}
		confirmDeleteObject.SetActive(value: false);
		selectPilotButtons.SetActive(value: true);
		pilotInfoTemplate.SetActive(value: false);
		if (wasMP)
		{
			mpMenu.Open();
			wasMP = false;
		}
	}

	public void SelectToMainButton()
	{
		mainDisplayObject.SetActive(value: true);
		selectPilotDisplayObject.SetActive(value: false);
	}

	public void NewPilotButton()
	{
		mainDisplayObject.SetActive(value: false);
		createPilotDisplayObject.SetActive(value: true);
		newPilotStartButton.SetActive(value: false);
		invalidNameObject.SetActive(value: false);
		newPilotName = string.Empty;
		newPilotNameText.text = "New Pilot";
	}

	public void NewPilotToMain()
	{
		createPilotDisplayObject.SetActive(value: false);
		mainDisplayObject.SetActive(value: true);
	}

	public void EditNameButton()
	{
		keyboard.Display(newPilotName, 30, OnPilotNameEntered);
		createPilotDisplayObject.SetActive(value: false);
	}

	public void StartNewPilotButton()
	{
		PilotSaveManager.current = PilotSaveManager.CreateNewPilot(newPilotName);
		PilotSaveManager.SavePilotsToFile();
		createPilotDisplayObject.SetActive(value: false);
		vehicleSelectDisplayObject.SetActive(value: true);
		SetupVehicleScreen();
	}

	private void OnPilotNameEntered(string s)
	{
		newPilotName = s;
		newPilotName = ConfigNodeUtils.SanitizeInputStringStrict(newPilotName);
		newPilotName = PilotSave.SanitizeName(newPilotName);
		createPilotDisplayObject.SetActive(value: true);
		if (string.IsNullOrEmpty(newPilotName))
		{
			newPilotName = string.Empty;
			newPilotNameText.text = "New Pilot";
			newPilotStartButton.SetActive(value: false);
			invalidNameObject.SetActive(value: true);
		}
		else if (PilotSaveManager.pilots.ContainsKey(newPilotName))
		{
			newPilotName = string.Empty;
			newPilotNameText.text = "New Pilot";
			newPilotStartButton.SetActive(value: false);
			invalidNameObject.SetActive(value: true);
		}
		else
		{
			newPilotNameText.text = newPilotName;
			newPilotStartButton.SetActive(value: true);
			invalidNameObject.SetActive(value: false);
		}
	}

	public void EditorButton()
	{
		mainDisplayObject.SetActive(value: false);
		edConfirmObject.SetActive(value: true);
	}

	public void MapEditorButton()
	{
		mainDisplayObject.SetActive(value: false);
		mapEdConfirmObject.SetActive(value: true);
	}

	public void EditorConfirmButton()
	{
		edConfirmObject.SetActive(value: false);
		StartCoroutine(LaunchScenarioEditorRoutine());
	}

	private IEnumerator LaunchScenarioEditorRoutine()
	{
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black, 2f);
		yield return new WaitForSeconds(2.5f);
		ControllerEventHandler.UnpauseEvents();
		VTScenarioEditor.LaunchEditor(string.Empty);
	}

	public void MapEditorConfirmButton()
	{
		mapEdConfirmObject.SetActive(value: false);
		StartCoroutine(LaunchMapEditorRoutine());
	}

	private IEnumerator LaunchMapEditorRoutine()
	{
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black, 2f);
		yield return new WaitForSeconds(2.5f);
		ControllerEventHandler.UnpauseEvents();
		LoadingSceneController.LoadVTEditScene("VTMapEditMenu");
	}

	public void EditorCancelButton()
	{
		mainDisplayObject.SetActive(value: true);
		edConfirmObject.SetActive(value: false);
		mapEdConfirmObject.SetActive(value: false);
	}

	public void EditPilotColorsButton()
	{
		pilotColorEditor.OpenForPilot(PilotSaveManager.current);
	}

	public void Quit()
	{
		Application.Quit();
	}

	public void ArcadeSelectMissionButton()
	{
		PilotSaveManager.current = PilotSaveManager.pilots["Player"];
		selectPilotDisplayObject.SetActive(value: false);
		vehicleSelectDisplayObject.SetActive(value: true);
		SetupVehicleScreen();
		mainDisplayObject.SetActive(value: false);
		returnToArcadeTutorial = false;
	}

	public void ArcadeTutorialButton()
	{
		ArcadeSelectMissionButton();
		PlayerVehicle playerVehicle = VTResources.GetPlayerVehicle("AV-42C");
		SelectVehicle(playerVehicle, delegate
		{
			campaginUI.OpenSecretCampaign("arcadeTutorial");
		});
		returnToArcadeTutorial = true;
	}

	public void CreditsButton()
	{
		mainDisplayObject.SetActive(value: false);
		BGMManager.FadeTo(creditsBGM, 1f);
		credits.StartCredits(OnFinishedCredits);
	}

	private void OnFinishedCredits()
	{
		mainDisplayObject.SetActive(value: true);
		BGMManager.FadeTo(menuBGM, 2f);
	}
}
