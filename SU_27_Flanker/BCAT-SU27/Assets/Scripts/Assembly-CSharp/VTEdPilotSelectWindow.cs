using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdPilotSelectWindow : MonoBehaviour
{
	public class PilotButton : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public int idx;

		public VTEdPilotSelectWindow window;

		private float lastClickTime;

		public void OnPointerClick(PointerEventData data)
		{
			window.SelectPilot(idx);
			if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				window.Launch();
			}
			else
			{
				lastClickTime = Time.unscaledTime;
			}
		}
	}

	public VTScenarioEditor editor;

	public GameObject pilotLineTemplate;

	public Transform selectionTf;

	public Text pilotNameText;

	public Text pilotDescriptionText;

	public RectTransform contentTf;

	private ScrollRect scrollRect;

	public Button launchButton;

	public VTBoolProperty godmodeProperty;

	private List<GameObject> pilotObjects = new List<GameObject>();

	private int selectedIdx = -1;

	private float lineHeight;

	private List<PilotSave> pilots;

	private VTScenarioInfo currentScenario;

	private void Awake()
	{
		scrollRect = contentTf.GetComponentInParent<ScrollRect>();
	}

	public void Open(VTScenarioInfo scenario)
	{
		currentScenario = scenario;
		base.gameObject.SetActive(value: true);
		if ((bool)editor)
		{
			editor.BlockEditor(base.transform);
			editor.editorCamera.inputLock.AddLock("pilotSelect");
		}
		launchButton.interactable = false;
		pilotNameText.text = string.Empty;
		pilotDescriptionText.text = string.Empty;
		selectionTf.gameObject.SetActive(value: false);
		lineHeight = ((RectTransform)pilotLineTemplate.transform).rect.height;
		pilotLineTemplate.SetActive(value: false);
		foreach (GameObject pilotObject in pilotObjects)
		{
			Object.Destroy(pilotObject);
		}
		pilotObjects.Clear();
		PilotSaveManager.LoadPilotsFromFile();
		pilots = new List<PilotSave>();
		int num = 0;
		foreach (PilotSave value in PilotSaveManager.pilots.Values)
		{
			pilots.Add(value);
			GameObject gameObject = Object.Instantiate(pilotLineTemplate, contentTf);
			gameObject.SetActive(value: true);
			gameObject.GetComponent<Text>().text = value.pilotName;
			gameObject.transform.localPosition = new Vector3(0f, (float)(-num) * lineHeight, 0f);
			PilotButton pilotButton = gameObject.AddComponent<PilotButton>();
			pilotButton.idx = num;
			pilotButton.window = this;
			pilotObjects.Add(gameObject);
			num++;
		}
		contentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num * lineHeight);
		scrollRect.ClampVertical();
		godmodeProperty.SetInitialValue(PlayerVehicleSetup.godMode);
	}

	public void Open()
	{
		if ((bool)editor)
		{
			Open(VTResources.GetCustomScenario(editor.currentScenario.scenarioID, editor.currentScenario.campaignID));
		}
	}

	public void Launch()
	{
		VTResources.LoadCustomScenarios();
		VTResources.LoadMaps();
		string pilotName = pilots[selectedIdx].pilotName;
		PilotSaveManager.LoadPilotsFromFile();
		PilotSaveManager.current = PilotSaveManager.pilots[pilotName];
		PlayerVehicleSetup.godMode = (bool)godmodeProperty.GetValue();
		VTScenarioInfo customScenario = VTResources.GetCustomScenario(currentScenario.id, currentScenario.campaignID);
		Campaign campaign;
		if (!string.IsNullOrEmpty(currentScenario.campaignID))
		{
			campaign = VTResources.GetCustomCampaign(currentScenario.campaignID).ToIngameCampaign();
		}
		else
		{
			campaign = new Campaign();
			campaign.campaignName = "EditorTest";
			campaign.campaignID = "EditorTest";
			campaign.isCustomScenarios = true;
			campaign.isStandaloneScenarios = true;
		}
		PilotSaveManager.currentCampaign = campaign;
		PilotSaveManager.currentVehicle = customScenario.vehicle;
		CampaignScenario campaignScenario = new CampaignScenario();
		campaignScenario.baseBudget = customScenario.baseBudget;
		campaignScenario.scenarioID = customScenario.id;
		campaignScenario.scenarioName = customScenario.name;
		campaignScenario.description = customScenario.description;
		campaignScenario.equipConfigurable = customScenario.equipsConfigurable;
		campaignScenario.environmentName = customScenario.envName;
		if (customScenario.selectableEnv)
		{
			campaignScenario.envOptions = EnvironmentManager.GetGlobalEnvOptions();
			for (int i = 0; i < campaignScenario.envOptions.Length; i++)
			{
				if (campaignScenario.envOptions[i].envName == customScenario.envName)
				{
					campaignScenario.envIdx = i;
				}
			}
		}
		campaignScenario.isTraining = customScenario.isTraining;
		if (customScenario.forceEquips)
		{
			List<CampaignScenario.ForcedEquip> list = new List<CampaignScenario.ForcedEquip>();
			for (int j = 0; j < customScenario.forcedEquips.Count; j++)
			{
				if (!string.IsNullOrEmpty(customScenario.forcedEquips[j]))
				{
					CampaignScenario.ForcedEquip forcedEquip = new CampaignScenario.ForcedEquip();
					forcedEquip.hardpointIdx = j;
					forcedEquip.weaponName = customScenario.forcedEquips[j];
					list.Add(forcedEquip);
				}
			}
			campaignScenario.forcedEquips = list.ToArray();
		}
		if (!customScenario.equipsConfigurable)
		{
			campaignScenario.forcedFuel = customScenario.normForcedFuel;
		}
		campaignScenario.briefingNotes = new CampaignScenario.BriefingNote[0];
		campaign.missions = new List<CampaignScenario>();
		campaign.missions.Add(campaignScenario);
		PilotSaveManager.currentScenario = campaignScenario;
		VTScenario.currentScenarioInfo = customScenario;
		VehicleSave vehicleSave = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName);
		CampaignSave campaignSave = vehicleSave.GetCampaignSave(campaign.campaignID);
		if (campaignSave == null)
		{
			campaignSave = new CampaignSave();
			campaignSave.campaignName = campaign.campaignName;
			campaignSave.campaignID = campaign.campaignID;
			campaignSave.availableScenarios = new List<string>();
			campaignSave.completedScenarios = new List<CampaignSave.CompletedScenarioInfo>();
			campaignSave.currentFuel = 1f;
			vehicleSave.campaignSaves.Add(campaignSave);
		}
		campaignSave.availableWeapons = customScenario.allowedEquips;
		campaignSave.vehicleName = PilotSaveManager.currentVehicle.vehicleName;
		if (campaignSave.currentWeapons == null || campaignSave.currentWeapons.Length != PilotSaveManager.currentVehicle.hardpointCount)
		{
			campaignSave.currentWeapons = new string[PilotSaveManager.currentVehicle.hardpointCount];
		}
		PilotSaveManager.current.lastVehicleUsed = campaignSave.vehicleName;
		PilotSaveManager.currentScenario.totalBudget = 900000f;
		PilotSaveManager.SavePilotsToFile();
		VTScenarioEditor.returnToEditor = true;
		VTScenarioEditor.launchWithScenario = currentScenario.id;
		if ((bool)editor)
		{
			editor.BlockEditor(new GameObject().transform);
		}
		if (campaignScenario.equipConfigurable)
		{
			LoadingSceneController.SwitchToVRScene("VehicleConfiguration");
			return;
		}
		Loadout loadout = new Loadout();
		loadout.normalizedFuel = PilotSaveManager.currentScenario.forcedFuel;
		loadout.hpLoadout = new string[PilotSaveManager.currentVehicle.hardpointCount];
		loadout.cmLoadout = new int[2] { 99999, 99999 };
		if (PilotSaveManager.currentScenario.forcedEquips != null)
		{
			CampaignScenario.ForcedEquip[] forcedEquips = PilotSaveManager.currentScenario.forcedEquips;
			foreach (CampaignScenario.ForcedEquip forcedEquip2 in forcedEquips)
			{
				loadout.hpLoadout[forcedEquip2.hardpointIdx] = forcedEquip2.weaponName;
			}
		}
		VehicleEquipper.loadout = loadout;
		VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
		VTResources.LaunchMapForScenario(currentScenario, skipLoading: false);
	}

	public void Cancel()
	{
		base.gameObject.SetActive(value: false);
		if ((bool)editor)
		{
			editor.UnblockEditor(base.transform);
			editor.editorCamera.inputLock.RemoveLock("pilotSelect");
		}
	}

	public void SelectPilot(int idx)
	{
		selectedIdx = idx;
		selectionTf.gameObject.SetActive(value: true);
		selectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
		PilotSave pilotSave = pilots[idx];
		pilotNameText.text = pilotSave.pilotName;
		pilotDescriptionText.text = pilotSave.GetInfoString();
		launchButton.interactable = true;
	}
}
