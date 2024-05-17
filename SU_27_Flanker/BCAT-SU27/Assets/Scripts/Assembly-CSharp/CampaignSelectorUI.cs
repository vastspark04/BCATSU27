using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class CampaignSelectorUI : MonoBehaviour, ILocalizationUser
{
	public struct WorkshopLaunchStatus
	{
		public bool success;

		public string message;
	}

	public Texture2D noImage;

	public PilotSelectUI pilotSelectUI;

	[Header("CampaignSelector")]
	public GameObject campaignDisplayObject;

	public GameObject campaignTemplate;

	private int campaignIdx;

	private float campaignWidth;

	private Transform campaignsParent;

	public GameObject[] disableOnResetDialogue;

	public GameObject resetDialogue;

	public RectTransform customCampaignLabelTf;

	[Header("Campaign List")]
	public GameObject cListItemTemplate;

	public ScrollRect cListScrollRect;

	public Transform cListSelectionTf;

	private float cListHeight;

	[Header("ScenarioSelector")]
	public GameObject scenarioDisplayObject;

	public GameObject scenarioInfoTemplate;

	public GameObject missionDisplayObj;

	public GameObject trainingDisplayObj;

	public GameObject missionButton;

	public GameObject trainingButton;

	public CampaignNotificationSystem notificationSystem;

	public Text scenarioCountText;

	private Transform missionsParent;

	private Transform trainingParent;

	private float missionWidth;

	private int availableMissionCount;

	private int missionIdx;

	private int availableTrainingCount;

	private int trainingIdx;

	[Header("Scenario List")]
	public GameObject sListItemTemplate;

	public ScrollRect sListScrollRect;

	public Transform sListSelectionTf;

	[Header("No Missions")]
	public GameObject noMissionsObj;

	public GameObject[] hideOnNoMissions;

	private List<Campaign> campaigns;

	[Header("MissionBriefing")]
	public GameObject missionBriefingTemplate;

	public GameObject missionBriefingDisplayObject;

	private GameObject missionBriefingObject;

	[Header("Steam Workshop")]
	public GameObject loadingWorkshopObj;

	public Transform loadingWorkshopBar;

	public GameObject workshopListLabel;

	private string s_cs_granted = "Granted";

	private string s_cs_budget = "Budget";

	private string s_cs_newMission = "New Mission:";

	private string s_cs_newTraining = "New Training:";

	private string s_cs_newEquip = "New Equipment:";

	private string s_cs_incompatibleVersion = "Incompatible game version";

	private string campaign_customScenarios;

	private string campaign_customScenarios_description;

	private string campaign_ws;

	private string campaign_ws_description;

	public GameObject loadingCampaignScreenObj;

	private Campaign swStandaloneCampaign;

	private List<GameObject> cListObjects = new List<GameObject>();

	private Campaign viewingCampaign;

	private static string workshopCampaignID;

	private static List<Campaign> loadedWorkshopCampaigns = new List<Campaign>();

	private static bool openedFromWorkshop = false;

	private static bool openedWorkshopCampaign = false;

	private static List<CampaignScenario> loadedWorkshopSingleScenarios = new List<CampaignScenario>();

	private bool openedTutorial;

	private List<GameObject> sListObjects = new List<GameObject>();

	public void ApplyLocalization()
	{
		s_cs_granted = VTLocalizationManager.GetString("s_cs_granted", s_cs_granted, "Text in campaign selector");
		s_cs_budget = VTLocalizationManager.GetString("s_cs_budget", s_cs_budget, "Text in campaign selector");
		s_cs_newMission = VTLocalizationManager.GetString("s_cs_newMission", s_cs_newMission, "Text in campaign selector");
		s_cs_newTraining = VTLocalizationManager.GetString("s_cs_newTraining", s_cs_newTraining, "Text in campaign selector");
		s_cs_newEquip = VTLocalizationManager.GetString("s_cs_newEquip", s_cs_newEquip, "Text in campaign selector");
		s_cs_incompatibleVersion = VTLocalizationManager.GetString("s_cs_incompatibleVersion", s_cs_incompatibleVersion, "Text in campaign selector");
		campaign_customScenarios = VTLocalizationManager.GetString("campaign_customScenarios", "Custom Scenarios");
		campaign_customScenarios_description = VTLocalizationManager.GetString("campaign_customScenarios_description", "Play custom scenarios created in the VTEditor.");
		campaign_ws = VTLocalizationManager.GetString("campaign_ws", "Steam Workshop Scenarios");
		campaign_ws_description = VTLocalizationManager.GetString("campaign_ws_description", "Play custom scenarios installed through Steam Workshop.");
	}

	private void Awake()
	{
		ApplyLocalization();
	}

	private IEnumerator SetupCampaignScreenRoutine()
	{
		loadingCampaignScreenObj.SetActive(value: true);
		bool wasInputEnabled = !ControllerEventHandler.eventsPaused;
		ControllerEventHandler.PauseEvents();
		VTScenarioEditor.returnToEditor = false;
		VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
		PlayerVehicleSetup.godMode = false;
		campaignDisplayObject.SetActive(value: true);
		scenarioDisplayObject.SetActive(value: false);
		if ((bool)campaignsParent)
		{
			UnityEngine.Object.Destroy(campaignsParent.gameObject);
		}
		campaignsParent = new GameObject("campaigns").transform;
		campaignsParent.parent = campaignTemplate.transform.parent;
		campaignsParent.localPosition = campaignTemplate.transform.localPosition;
		campaignsParent.localRotation = Quaternion.identity;
		campaignsParent.localScale = Vector3.one;
		campaignWidth = ((RectTransform)campaignTemplate.transform).rect.width;
		Stopwatch sw = new Stopwatch();
		sw.Start();
		campaigns = new List<Campaign>();
		foreach (VTCampaignInfo builtInCampaign in VTResources.GetBuiltInCampaigns())
		{
			if (builtInCampaign.vehicle == PilotSaveManager.currentVehicle.vehicleName && !builtInCampaign.hideFromMenu)
			{
				BDCoroutine coroutine;
				Campaign c2 = builtInCampaign.ToIngameCampaignAsync(this, out coroutine);
				yield return coroutine;
				campaigns.Add(c2);
			}
		}
		sw.Stop();
		UnityEngine.Debug.Log("Time loading BuiltInCampaigns: " + sw.ElapsedMilliseconds);
		sw.Reset();
		sw.Start();
		foreach (Campaign campaign in PilotSaveManager.currentVehicle.campaigns)
		{
			if (campaign.isSteamworksStandalone)
			{
				if (SteamClient.IsValid)
				{
					swStandaloneCampaign = campaign;
					campaign.campaignName = campaign_ws;
					campaign.description = campaign_ws_description;
				}
			}
			else if (campaign.readyToPlay)
			{
				campaigns.Add(campaign);
				if (campaign.isCustomScenarios && campaign.isStandaloneScenarios && !campaign.isSteamworksStandalone)
				{
					campaign.campaignName = campaign_customScenarios;
					campaign.description = campaign_customScenarios_description;
				}
			}
		}
		sw.Stop();
		UnityEngine.Debug.Log("Time loading vehicle campaigns: " + sw.ElapsedMilliseconds);
		sw.Reset();
		sw.Start();
		VTResources.GetCustomCampaigns();
		sw.Stop();
		UnityEngine.Debug.Log("Time loading custom campaigns list: " + sw.ElapsedMilliseconds);
		sw.Reset();
		sw.Start();
		foreach (VTCampaignInfo customCampaign in VTResources.GetCustomCampaigns())
		{
			if (customCampaign.vehicle == PilotSaveManager.currentVehicle.vehicleName && !customCampaign.multiplayer)
			{
				BDCoroutine coroutine2;
				Campaign c2 = customCampaign.ToIngameCampaignAsync(this, out coroutine2);
				yield return coroutine2;
				campaigns.Add(c2);
			}
		}
		sw.Stop();
		UnityEngine.Debug.Log("Time converting custom campaigns ToIngameCampaigns: " + sw.ElapsedMilliseconds);
		sw.Reset();
		for (int i = 0; i < campaigns.Count; i++)
		{
			GameObject obj = UnityEngine.Object.Instantiate(campaignTemplate, campaignsParent);
			obj.transform.localPosition += campaignWidth * (float)i * Vector3.right;
			CampaignInfoUI component = obj.GetComponent<CampaignInfoUI>();
			component.campaignImage.texture = noImage;
			component.UpdateDisplay(campaigns[i], PilotSaveManager.currentVehicle.vehicleName);
			obj.SetActive(value: true);
			yield return null;
		}
		campaignIdx = Mathf.Clamp(campaignIdx, 0, campaigns.Count - 1);
		campaignTemplate.SetActive(value: false);
		SetupCampaignList();
		loadingCampaignScreenObj.SetActive(value: false);
		if (wasInputEnabled)
		{
			ControllerEventHandler.UnpauseEvents();
		}
	}

	public void BackToVehicleButton()
	{
		campaignDisplayObject.SetActive(value: false);
		PilotSaveManager.currentVehicle = null;
		pilotSelectUI.BackToVehicle();
	}

	public void OpenCampaignSelector(Action onOpenedSelector = null)
	{
		ScreenFader.FadeIn();
		campaignDisplayObject.SetActive(value: true);
		FinallyOpenCampaignSelector(onOpenedSelector);
	}

	private void FinallyOpenCampaignSelector(Action onOpenedSelector)
	{
		StartCoroutine(FinallyOpenCampaignSelectorRoutine(onOpenedSelector));
	}

	private IEnumerator FinallyOpenCampaignSelectorRoutine(Action onOpenedSelector)
	{
		campaignDisplayObject.SetActive(value: true);
		missionBriefingDisplayObject.SetActive(value: false);
		yield return StartCoroutine(SetupCampaignScreenRoutine());
		if (onOpenedSelector != null)
		{
			onOpenedSelector();
		}
		else
		{
			if (!PilotSaveManager.currentCampaign)
			{
				yield break;
			}
			CampaignSave lastCSave = PilotSaveManager.current.lastVehicleSave.GetCampaignSave(PilotSaveManager.currentCampaign.campaignID);
			int num = campaigns.FindIndex((Campaign x) => x.campaignID == PilotSaveManager.currentCampaign.campaignID);
			if (num >= 0)
			{
				PilotSaveManager.currentCampaign = campaigns[num];
				campaignIdx = num;
				ViewCampaign(num);
				SelectCampaign();
				if (PilotSaveManager.currentScenario == null)
				{
					yield break;
				}
				while (viewingCampaign == null)
				{
					yield return null;
				}
				int num2 = campaigns[campaignIdx].missions.FindIndex((CampaignScenario x) => PilotSaveManager.currentScenario.scenarioID == x.scenarioID);
				if (num2 >= 0)
				{
					missionIdx = num2;
					MissionsButton();
					yield break;
				}
				num2 = campaigns[campaignIdx].trainingMissions.FindIndex((CampaignScenario x) => PilotSaveManager.currentScenario.scenarioID == x.scenarioID);
				if (num2 >= 0)
				{
					trainingIdx = num2;
					TrainingButton();
				}
			}
			else
			{
				if (!openedWorkshopCampaign)
				{
					yield break;
				}
				foreach (Campaign loadedWorkshopCampaign in loadedWorkshopCampaigns)
				{
					if (!(loadedWorkshopCampaign.campaignID == workshopCampaignID))
					{
						continue;
					}
					StartWorkshopCampaign(workshopCampaignID);
					while (viewingCampaign == null)
					{
						yield return null;
					}
					if (lastCSave != null)
					{
						if (lastCSave.lastScenarioWasTraining)
						{
							trainingIdx = lastCSave.lastScenarioIdx;
							TrainingButton();
						}
						else
						{
							missionIdx = lastCSave.lastScenarioIdx;
							MissionsButton();
						}
					}
					break;
				}
			}
		}
	}

	private void Update()
	{
		if (campaignDisplayObject.activeSelf && (bool)campaignsParent)
		{
			campaignsParent.localPosition = Vector3.Lerp(campaignsParent.localPosition, new Vector3((float)(-campaignIdx) * campaignWidth, 0f, 0f), 12f * Time.deltaTime);
		}
		if ((bool)missionsParent && missionsParent.gameObject.activeSelf)
		{
			missionsParent.localPosition = Vector3.Lerp(missionsParent.localPosition, new Vector3((float)(-missionIdx) * missionWidth, 0f, 0f), 12f * Time.deltaTime);
			float num = ((RectTransform)sListItemTemplate.transform).rect.height * sListItemTemplate.transform.localScale.y;
			sListSelectionTf.localPosition = new Vector3(0f, (0f - num) * (float)missionIdx, 0f);
		}
		if ((bool)trainingParent && trainingParent.gameObject.activeSelf)
		{
			trainingParent.localPosition = Vector3.Lerp(trainingParent.localPosition, new Vector3((float)(-trainingIdx) * missionWidth, 0f, 0f), 12f * Time.deltaTime);
			float num2 = ((RectTransform)sListItemTemplate.transform).rect.height * sListItemTemplate.transform.localScale.y;
			sListSelectionTf.localPosition = new Vector3(0f, (0f - num2) * (float)trainingIdx, 0f);
		}
	}

	public void NextCampaign()
	{
		campaignIdx = (campaignIdx + 1) % campaigns.Count;
		ViewCampaign(campaignIdx);
	}

	public void PrevCampaign()
	{
		campaignIdx--;
		if (campaignIdx < 0)
		{
			campaignIdx = campaigns.Count - 1;
		}
		ViewCampaign(campaignIdx);
	}

	public void SelectCampaign()
	{
		if (campaigns == null)
		{
			UnityEngine.Debug.Log("campaigns is null in CampaignSelectorUI.SelectCampaign");
		}
		if (campaigns[campaignIdx] == null)
		{
			UnityEngine.Debug.LogError("c is null in CampaignSelectorUI.SelectCampaign");
		}
		FinallySelectCampaign();
	}

	private void FinallySelectCampaign()
	{
		campaignDisplayObject.SetActive(value: false);
		PilotSaveManager.currentCampaign = campaigns[campaignIdx];
		openedFromWorkshop = false;
		openedWorkshopCampaign = false;
		SetupCampaignScenarios(campaigns[campaignIdx]);
	}

	public void OpenSecretCampaign(string campaignID)
	{
		VTCampaignInfo builtInCampaign = VTResources.GetBuiltInCampaign(campaignID);
		if (builtInCampaign == null)
		{
			VTResources.LoadCustomScenarios();
			builtInCampaign = VTResources.GetBuiltInCampaign(campaignID);
		}
		if (builtInCampaign != null)
		{
			campaignDisplayObject.SetActive(value: false);
			Campaign c = (PilotSaveManager.currentCampaign = builtInCampaign.ToIngameCampaign());
			SetupCampaignScenarios(c);
		}
	}

	public void ResetCampaign()
	{
		disableOnResetDialogue.SetActive(active: false);
		resetDialogue.SetActive(value: true);
	}

	public void FinallyResetCampaign()
	{
		Campaign campaign = campaigns[campaignIdx];
		PilotSaveManager.ResetCampaignSave(PilotSaveManager.current, PilotSaveManager.currentVehicle.vehicleName, campaign.campaignID);
		CancelReset();
		OpenCampaignSelector();
	}

	public void CancelReset()
	{
		disableOnResetDialogue.SetActive(active: true);
		resetDialogue.SetActive(value: false);
	}

	private void SetupCampaignList()
	{
		foreach (GameObject cListObject in cListObjects)
		{
			UnityEngine.Object.Destroy(cListObject);
		}
		cListObjects = new List<GameObject>();
		cListHeight = ((RectTransform)cListItemTemplate.transform).rect.height;
		bool flag = false;
		customCampaignLabelTf.gameObject.SetActive(value: false);
		bool flag2 = false;
		workshopListLabel.SetActive(value: false);
		int num = 0;
		for (int i = 0; i < campaigns.Count; i++)
		{
			if (!campaigns[i].isBuiltIn && !flag)
			{
				customCampaignLabelTf.gameObject.SetActive(value: true);
				customCampaignLabelTf.localPosition = new Vector3(0f, (float)(-i) * cListHeight, 0f);
				flag = true;
				num++;
			}
			if (!campaigns[i].isSteamworksStandalone)
			{
				if (campaigns[i].isSteamworksStandalone && !campaigns[i].isStandaloneScenarios && !flag2)
				{
					workshopListLabel.SetActive(value: true);
					workshopListLabel.transform.localPosition = new Vector3(0f, (float)(-(i + 1)) * cListHeight, 0f);
					flag2 = true;
					num++;
				}
				GameObject gameObject = UnityEngine.Object.Instantiate(cListItemTemplate, cListScrollRect.content);
				gameObject.SetActive(value: true);
				gameObject.transform.localPosition = new Vector3(0f, (float)(-num) * cListHeight, 0f);
				gameObject.GetComponent<VRUIListItemTemplate>().Setup(campaigns[i].campaignName, i, ViewCampaign);
				cListObjects.Add(gameObject);
				num++;
			}
		}
		cListScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)(2 + campaigns.Count) * cListHeight);
		cListScrollRect.ClampVertical();
		cListItemTemplate.SetActive(value: false);
		ViewCampaign(campaignIdx);
		GetComponentInParent<VRPointInteractableCanvas>().RefreshInteractables();
	}

	private void UpdateCampaignSelectTf()
	{
		int num = campaignIdx;
		if (!campaigns[campaignIdx].isBuiltIn)
		{
			num++;
		}
		if (campaigns[campaignIdx].isSteamworksStandalone && !campaigns[campaignIdx].isStandaloneScenarios)
		{
			num++;
		}
		cListSelectionTf.localPosition = new Vector3(0f, (float)(-num) * cListHeight, 0f);
	}

	private void ViewCampaign(int idx)
	{
		campaignIdx = idx;
		UpdateCampaignSelectTf();
		cListScrollRect.ViewContent((RectTransform)cListObjects[idx].transform);
	}

	public static CampaignSave SetUpCampaignSave(Campaign c, Action<string> onNewMission, Action<string> onNewTraining, Action<string> onNewEquipment, PlayerVehicle overrideVehicle = null)
	{
		if (overrideVehicle == null)
		{
			overrideVehicle = PilotSaveManager.currentVehicle;
		}
		CampaignSave campaignSave = null;
		VehicleSave vehicleSave = PilotSaveManager.current.GetVehicleSave(overrideVehicle.vehicleName);
		foreach (CampaignSave campaignSafe in vehicleSave.campaignSaves)
		{
			if (campaignSafe.campaignID == c.campaignID)
			{
				campaignSave = campaignSafe;
				break;
			}
		}
		if (campaignSave == null)
		{
			campaignSave = new CampaignSave();
			campaignSave.campaignName = c.campaignName;
			campaignSave.campaignID = c.campaignID;
			campaignSave.vehicleName = overrideVehicle.vehicleName;
			campaignSave.completedScenarios = new List<CampaignSave.CompletedScenarioInfo>();
			campaignSave.availableScenarios = new List<string>();
			campaignSave.currentFuel = 1f;
			campaignSave.currentWeapons = new string[overrideVehicle.hardpointCount];
			campaignSave.availableWeapons = new List<string>();
			foreach (string item in c.weaponsOnStart)
			{
				campaignSave.availableWeapons.Add(item);
			}
			foreach (string item2 in c.scenariosOnStart)
			{
				campaignSave.availableScenarios.Add(item2);
			}
			vehicleSave.campaignSaves.Add(campaignSave);
			return campaignSave;
		}
		foreach (string startMission in c.scenariosOnStart)
		{
			if (campaignSave.availableScenarios.Contains(startMission))
			{
				continue;
			}
			bool flag = false;
			CampaignScenario campaignScenario = c.missions.Find((CampaignScenario x) => x.scenarioID == startMission);
			if (campaignScenario == null)
			{
				flag = true;
				campaignScenario = c.trainingMissions.Find((CampaignScenario x) => x.scenarioID == startMission);
			}
			if (flag)
			{
				onNewTraining?.Invoke(campaignScenario.scenarioName);
			}
			else
			{
				onNewMission?.Invoke(campaignScenario.scenarioName);
			}
			campaignSave.availableScenarios.Add(startMission);
		}
		foreach (string item3 in c.weaponsOnStart)
		{
			if (campaignSave.availableWeapons.Contains(item3))
			{
				continue;
			}
			campaignSave.availableWeapons.Add(item3);
			UnityEngine.Object @object = Resources.Load(overrideVehicle.equipsResourcePath + "/" + item3);
			if ((bool)@object)
			{
				if (!c.isStandaloneScenarios)
				{
					string localizedFullName = ((GameObject)@object).GetComponentImplementing<HPEquippable>().GetLocalizedFullName();
					onNewEquipment?.Invoke(localizedFullName);
				}
			}
			else
			{
				UnityEngine.Debug.Log("Start weapon " + item3 + " not found.");
			}
		}
		return campaignSave;
	}

	public void SetupCampaignScenarios(Campaign c, bool notifications = true)
	{
		UnityEngine.Debug.Log("Setting up campaign scenarios.");
		viewingCampaign = c;
		CampaignSave campaignSave = SetUpCampaignSave(c, delegate(string s)
		{
			notificationSystem.AddNotification("New Mission:", s, notificationSystem.newMissionColor);
		}, delegate(string s)
		{
			notificationSystem.AddNotification("New Training:", s, notificationSystem.newTrainingColor);
		}, delegate(string s)
		{
			notificationSystem.AddNotification("New Equipment:", s, notificationSystem.newWeaponColor);
		});
		scenarioDisplayObject.SetActive(value: true);
		missionWidth = ((RectTransform)scenarioInfoTemplate.transform).rect.width;
		Dictionary<string, CampaignSave.CompletedScenarioInfo> dictionary = new Dictionary<string, CampaignSave.CompletedScenarioInfo>();
		float num = 0f;
		foreach (CampaignSave.CompletedScenarioInfo completedScenario in campaignSave.completedScenarios)
		{
			dictionary.Add(completedScenario.scenarioID, completedScenario);
			num += completedScenario.earnedBudget;
		}
		if (c.isCustomScenarios && c.isStandaloneScenarios)
		{
			List<VTScenarioInfo> list;
			if (c.isSteamworksStandalone)
			{
				list = VTResources.GetSteamWorkshopSingleScenarios();
			}
			else
			{
				VTResources.LoadCustomScenarios();
				list = VTResources.GetCustomScenarios();
			}
			c.missions = new List<CampaignScenario>();
			c.trainingMissions = new List<CampaignScenario>();
			campaignSave.availableScenarios = new List<string>();
			foreach (VTScenarioInfo item2 in list)
			{
				if (item2.vehicle == PilotSaveManager.currentVehicle)
				{
					CampaignScenario item = item2.ToIngameScenario(null);
					if (item2.isTraining)
					{
						c.trainingMissions.Add(item);
					}
					else
					{
						c.missions.Add(item);
					}
					campaignSave.availableScenarios.Add(item2.id);
				}
			}
		}
		missionIdx = 0;
		if ((bool)missionsParent)
		{
			UnityEngine.Object.Destroy(missionsParent.gameObject);
		}
		missionsParent = new GameObject("missions").transform;
		missionsParent.parent = scenarioInfoTemplate.transform.parent;
		missionsParent.localPosition = Vector3.zero;
		missionsParent.localRotation = Quaternion.identity;
		missionsParent.localScale = Vector3.one;
		missionDisplayObj.SetActive(value: true);
		foreach (CampaignSave.CompletedScenarioInfo cm in campaignSave.completedScenarios)
		{
			CampaignScenario campaignScenario = c.missions.Find((CampaignScenario x) => x.scenarioID == cm.scenarioID);
			if (campaignScenario == null)
			{
				campaignScenario = c.trainingMissions.Find((CampaignScenario x) => x.scenarioID == cm.scenarioID);
			}
			if (campaignScenario == null)
			{
				continue;
			}
			if (campaignScenario.scenariosOnComplete != null)
			{
				foreach (string nm in campaignScenario.scenariosOnComplete)
				{
					if (campaignSave.availableScenarios.Contains(nm))
					{
						continue;
					}
					bool flag = false;
					CampaignScenario campaignScenario2 = c.missions.Find((CampaignScenario x) => x.scenarioID == nm);
					if (campaignScenario2 == null)
					{
						campaignScenario2 = c.trainingMissions.Find((CampaignScenario x) => x.scenarioID == nm);
						flag = true;
					}
					if (flag)
					{
						notificationSystem.AddNotification(s_cs_newTraining, campaignScenario2.scenarioName, notificationSystem.newTrainingColor);
					}
					else
					{
						notificationSystem.AddNotification(s_cs_newMission, campaignScenario2.scenarioName, notificationSystem.newMissionColor);
					}
					campaignSave.availableScenarios.Add(nm);
				}
			}
			if (campaignScenario.equipmentOnComplete == null)
			{
				continue;
			}
			foreach (string item3 in campaignScenario.equipmentOnComplete)
			{
				if (!campaignSave.availableWeapons.Contains(item3))
				{
					if (!c.isStandaloneScenarios)
					{
						string localizedFullName = ((GameObject)Resources.Load(PilotSaveManager.currentVehicle.equipsResourcePath + "/" + item3)).GetComponentImplementing<HPEquippable>().GetLocalizedFullName();
						notificationSystem.AddNotification(s_cs_newEquip, localizedFullName, notificationSystem.newWeaponColor);
					}
					campaignSave.availableWeapons.Add(item3);
				}
			}
		}
		if (notifications)
		{
			notificationSystem.PlayNotifications();
		}
		int num2 = 0;
		foreach (CampaignScenario mission in c.missions)
		{
			if (campaignSave.availableScenarios.Contains(mission.scenarioID))
			{
				GameObject obj = UnityEngine.Object.Instantiate(scenarioInfoTemplate, missionsParent);
				obj.SetActive(value: true);
				obj.transform.localPosition = new Vector3((float)num2 * missionWidth, 0f, 0f);
				CampaignScenarioUI component = obj.GetComponent<CampaignScenarioUI>();
				component.scenarioName.text = mission.scenarioName;
				component.scenarioDescription.text = mission.description;
				mission.totalBudget = mission.baseBudget + num;
				component.scenarioBudget.text = $"{s_cs_budget}: ${mission.totalBudget.ToString()}";
				if (dictionary.ContainsKey(mission.scenarioID))
				{
					component.completedObject.SetActive(value: true);
					component.grantedBudget.text = $"{s_cs_granted}: ${dictionary[mission.scenarioID].earnedBudget.ToString()}";
				}
				else
				{
					component.completedObject.SetActive(value: false);
				}
				if ((bool)mission.scenarioImage)
				{
					component.scenarioImage.texture = mission.scenarioImage;
				}
				else
				{
					component.scenarioImage.texture = noImage;
				}
				component.fullVersionOnlyObject.SetActive(value: false);
				component.underConstructionObject.SetActive(mission.underConstruction);
				component.customMapMissingObject.SetActive(string.IsNullOrEmpty(mission.mapSceneName));
				component.oldVersionObject.SetActive(mission.customScenarioInfo != null && GameStartup.version < mission.customScenarioInfo.gameVersion);
				if (component.oldVersionObject.activeSelf)
				{
					component.oldVersionObject.GetComponentInChildren<Text>().text = $"{s_cs_incompatibleVersion}\n{mission.customScenarioInfo.gameVersion}";
				}
				if ((bool)component.rTrainingObject)
				{
					component.rTrainingObject.SetActive(value: true);
					component.rTraining.text = mission.recommendedTraining;
				}
				num2++;
			}
		}
		availableMissionCount = num2;
		scenarioCountText.text = missionIdx + 1 + "/" + availableMissionCount;
		trainingIdx = 0;
		if ((bool)trainingParent)
		{
			UnityEngine.Object.Destroy(trainingParent.gameObject);
		}
		trainingParent = new GameObject("trainings").transform;
		trainingParent.parent = scenarioInfoTemplate.transform.parent;
		trainingParent.localPosition = Vector3.zero;
		trainingParent.localRotation = Quaternion.identity;
		trainingParent.localScale = Vector3.one;
		trainingDisplayObj.SetActive(value: false);
		int num3 = 0;
		foreach (CampaignScenario trainingMission in c.trainingMissions)
		{
			if (campaignSave.availableScenarios.Contains(trainingMission.scenarioID))
			{
				trainingMission.isTraining = true;
				GameObject obj2 = UnityEngine.Object.Instantiate(scenarioInfoTemplate, trainingParent);
				obj2.SetActive(value: true);
				obj2.transform.localPosition = new Vector3((float)num3 * missionWidth, 0f, 0f);
				CampaignScenarioUI component2 = obj2.GetComponent<CampaignScenarioUI>();
				component2.scenarioName.text = trainingMission.scenarioName;
				component2.scenarioDescription.text = trainingMission.description;
				component2.scenarioBudget.enabled = false;
				if (dictionary.ContainsKey(trainingMission.scenarioID))
				{
					component2.completedObject.SetActive(value: true);
					component2.grantedBudget.text = "Granted: $" + dictionary[trainingMission.scenarioID].earnedBudget;
				}
				else
				{
					component2.completedObject.SetActive(value: false);
				}
				if ((bool)trainingMission.scenarioImage)
				{
					component2.scenarioImage.texture = trainingMission.scenarioImage;
				}
				else
				{
					component2.scenarioImage.texture = noImage;
				}
				component2.fullVersionOnlyObject.SetActive(value: false);
				component2.underConstructionObject.SetActive(trainingMission.underConstruction);
				component2.customMapMissingObject.SetActive(string.IsNullOrEmpty(trainingMission.mapSceneName));
				component2.oldVersionObject.SetActive(trainingMission.customScenarioInfo != null && GameStartup.version < trainingMission.customScenarioInfo.gameVersion);
				if ((bool)component2.rTrainingObject)
				{
					component2.rTrainingObject.SetActive(value: false);
				}
				num3++;
			}
		}
		availableTrainingCount = num3;
		trainingParent.gameObject.SetActive(value: false);
		trainingButton.SetActive(c.trainingMissions != null && c.trainingMissions.Count > 0);
		if (c.missions == null || c.missions.Count == 0)
		{
			missionButton.SetActive(value: false);
			TrainingButton();
		}
		else
		{
			missionButton.SetActive(value: true);
			MissionsButton();
		}
		scenarioInfoTemplate.SetActive(value: false);
		if (c.missions.Count == 0 && c.trainingMissions.Count == 0)
		{
			noMissionsObj.SetActive(value: true);
			hideOnNoMissions.SetActive(active: false);
			trainingDisplayObj.SetActive(value: false);
			missionDisplayObj.SetActive(value: false);
		}
		else
		{
			noMissionsObj.SetActive(value: false);
			hideOnNoMissions.SetActive(active: true);
		}
		PilotSaveManager.SavePilotsToFile();
	}

	public void NextMission()
	{
		if (missionsParent.gameObject.activeSelf)
		{
			missionIdx = Mathf.Min(missionIdx + 1, availableMissionCount - 1);
			scenarioCountText.text = missionIdx + 1 + "/" + availableMissionCount;
		}
		else if (trainingParent.gameObject.activeSelf)
		{
			trainingIdx = Mathf.Min(trainingIdx + 1, availableTrainingCount - 1);
			scenarioCountText.text = trainingIdx + 1 + "/" + availableTrainingCount;
		}
	}

	public void PrevMission()
	{
		if (missionsParent.gameObject.activeSelf)
		{
			missionIdx = Mathf.Max(missionIdx - 1, 0);
			scenarioCountText.text = missionIdx + 1 + "/" + availableMissionCount;
		}
		else if (trainingParent.gameObject.activeSelf)
		{
			trainingIdx = Mathf.Max(trainingIdx - 1, 0);
			scenarioCountText.text = trainingIdx + 1 + "/" + availableTrainingCount;
		}
	}

	public void StartMission()
	{
		if (missionsParent.gameObject.activeSelf)
		{
			if (PilotSaveManager.currentCampaign.missions.Count != 0)
			{
				if (missionIdx < 0 || missionIdx >= PilotSaveManager.currentCampaign.missions.Count)
				{
					UnityEngine.Debug.LogError("Mission idx is out of range: " + missionIdx + ". missions.Count: " + PilotSaveManager.currentCampaign.missions.Count);
				}
				StartMission(PilotSaveManager.currentCampaign.missions[missionIdx]);
			}
		}
		else if (trainingParent.gameObject.activeSelf && PilotSaveManager.currentCampaign.trainingMissions.Count != 0)
		{
			if (missionIdx < 0 || trainingIdx >= PilotSaveManager.currentCampaign.trainingMissions.Count)
			{
				UnityEngine.Debug.LogError("Training idx is out of range: " + trainingIdx + ". trainingMissions.Count: " + PilotSaveManager.currentCampaign.trainingMissions.Count);
			}
			StartMission(PilotSaveManager.currentCampaign.trainingMissions[trainingIdx]);
		}
	}

	public void StartWorkshopCampaign(string campaignID)
	{
		StartCoroutine(StartWorkshopCampaignRoutine(campaignID));
	}

	private IEnumerator StartWorkshopCampaignRoutine(string campaignID)
	{
		openedFromWorkshop = true;
		openedWorkshopCampaign = true;
		workshopCampaignID = campaignID;
		VTCampaignInfo campaign = VTResources.GetSteamWorkshopCampaign(campaignID);
		if (campaign == null)
		{
			UnityEngine.Debug.Log("Missing campaign in StartWorkshopCampaignRoutine");
		}
		string vehicle = campaign.vehicle;
		PilotSaveManager.current.lastVehicleUsed = vehicle;
		PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(vehicle);
		yield return StartCoroutine(SetupCampaignScreenRoutine());
		campaignIdx = 0;
		Campaign campaign2 = null;
		foreach (Campaign loadedWorkshopCampaign in loadedWorkshopCampaigns)
		{
			if (loadedWorkshopCampaign.campaignID == campaignID)
			{
				campaign2 = loadedWorkshopCampaign;
				break;
			}
		}
		if (campaign2 == null)
		{
			campaign2 = campaign.ToIngameCampaign();
			loadedWorkshopCampaigns.Add(campaign2);
		}
		if (campaign2 != null)
		{
			campaignDisplayObject.SetActive(value: false);
			PilotSaveManager.currentCampaign = campaign2;
			SetupCampaignScenarios(campaign2);
		}
	}

	public WorkshopLaunchStatus StartWorkshopMission(string scenarioID)
	{
		VTScenarioInfo steamWorkshopStandaloneScenario = VTResources.GetSteamWorkshopStandaloneScenario(scenarioID);
		WorkshopLaunchStatus result;
		if (steamWorkshopStandaloneScenario == null)
		{
			UnityEngine.Debug.LogError("Tried to run workshop scenario but scenario was null!");
			result = default(WorkshopLaunchStatus);
			result.success = false;
			result.message = VTLStaticStrings.err_scenarioNotFound;
			return result;
		}
		if (GameStartup.version < steamWorkshopStandaloneScenario.gameVersion)
		{
			string text = GameStartup.version.ToString();
			GameVersion gameVersion = steamWorkshopStandaloneScenario.gameVersion;
			UnityEngine.Debug.LogError("Tried to run workshop scenario but version incompatible.  Game version: " + text + ", scenario game version: " + gameVersion.ToString());
			result = default(WorkshopLaunchStatus);
			result.success = false;
			result.message = VTLStaticStrings.err_version;
			return result;
		}
		StartCoroutine(StartWorkshopMissionRoutine(scenarioID));
		result = default(WorkshopLaunchStatus);
		result.success = true;
		return result;
	}

	private IEnumerator StartWorkshopMissionRoutine(string scenarioID)
	{
		openedFromWorkshop = true;
		openedWorkshopCampaign = false;
		UnityEngine.Debug.Log("========== A steam workshop standalone mission should be loaded at this time ===========");
		VTScenarioInfo scenario = VTResources.GetSteamWorkshopStandaloneScenario(scenarioID);
		PlayerVehicle playerVehicle = (PilotSaveManager.currentVehicle = scenario.vehicle);
		PilotSaveManager.current.lastVehicleUsed = playerVehicle.vehicleName;
		yield return StartCoroutine(SetupCampaignScreenRoutine());
		Campaign campaign = swStandaloneCampaign;
		if (campaign.missions != null)
		{
			campaign.missions.Clear();
		}
		else
		{
			campaign.missions = new List<CampaignScenario>(1);
		}
		CampaignScenario campaignScenario = null;
		foreach (CampaignScenario loadedWorkshopSingleScenario in loadedWorkshopSingleScenarios)
		{
			if (loadedWorkshopSingleScenario.scenarioID == scenarioID)
			{
				campaignScenario = loadedWorkshopSingleScenario;
				break;
			}
		}
		if (campaignScenario == null)
		{
			campaignScenario = scenario.ToIngameScenario(null);
		}
		campaign.missions.Add(campaignScenario);
		if (campaign != null)
		{
			PilotSaveManager.currentCampaign = campaign;
			SetupCampaignScenarios(campaign, notifications: false);
			for (int i = 0; i < campaign.missions.Count; i++)
			{
				if (campaign.missions[i].scenarioID == scenarioID)
				{
					StartMission(campaign.missions[i]);
					break;
				}
			}
		}
		else
		{
			UnityEngine.Debug.LogError("Tried to start workshop mission but steamworksStandalone campaign is null.");
		}
	}

	private void StartMission(CampaignScenario cs)
	{
		if (cs.underConstruction || string.IsNullOrEmpty(cs.mapSceneName) || (cs.customScenarioInfo != null && GameStartup.version < cs.customScenarioInfo.gameVersion))
		{
			return;
		}
		campaignDisplayObject.SetActive(value: false);
		PilotSaveManager.currentScenario = cs;
		scenarioDisplayObject.SetActive(value: false);
		missionBriefingDisplayObject.SetActive(value: true);
		missionBriefingObject = UnityEngine.Object.Instantiate(missionBriefingTemplate, missionBriefingTemplate.transform.parent);
		missionBriefingObject.SetActive(value: true);
		CampaignSave campaignSave = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).GetCampaignSave(PilotSaveManager.currentCampaign.campaignID);
		campaignSave.lastScenarioWasTraining = PilotSaveManager.currentCampaign.trainingMissions.Contains(cs);
		if (campaignSave.lastScenarioWasTraining)
		{
			campaignSave.lastScenarioIdx = trainingIdx;
		}
		else
		{
			campaignSave.lastScenarioIdx = missionIdx;
		}
		if (PilotSaveManager.currentCampaign.isCustomScenarios)
		{
			VTScenario.currentScenarioInfo = VTResources.GetScenario(cs.scenarioID, PilotSaveManager.currentCampaign);
			VTScenarioInfo currentScenarioInfo = VTScenario.currentScenarioInfo;
			_ = currentScenarioInfo.config;
			if (PilotSaveManager.currentCampaign.isStandaloneScenarios)
			{
				campaignSave.availableWeapons = currentScenarioInfo.allowedEquips;
			}
		}
		PilotSaveManager.SavePilotsToFile();
		missionBriefingObject.GetComponent<MissionBriefingUI>().InitializeMission(cs);
	}

	public void StartTutorial(VTScenarioInfo tutorialInfo)
	{
		VTCampaignInfo builtInCampaign = VTResources.GetBuiltInCampaign(tutorialInfo.campaignID);
		Campaign currentCampaign = builtInCampaign.ToIngameCampaign();
		CampaignScenario campaignScenario = tutorialInfo.ToIngameScenario(builtInCampaign);
		PilotSaveManager.currentVehicle = tutorialInfo.vehicle;
		PilotSaveManager.current.lastVehicleUsed = PilotSaveManager.currentVehicle.vehicleName;
		PilotSaveManager.currentCampaign = currentCampaign;
		PilotSaveManager.currentScenario = campaignScenario;
		PilotSaveManager.SavePilotsToFile();
		missionBriefingDisplayObject.SetActive(value: true);
		missionBriefingObject = UnityEngine.Object.Instantiate(missionBriefingTemplate, missionBriefingTemplate.transform.parent);
		missionBriefingObject.SetActive(value: true);
		missionBriefingObject.GetComponent<MissionBriefingUI>().InitializeMission(campaignScenario);
		openedTutorial = true;
	}

	public void MissionsButton()
	{
		missionsParent.gameObject.SetActive(value: true);
		trainingParent.gameObject.SetActive(value: false);
		missionDisplayObj.SetActive(value: true);
		trainingDisplayObj.SetActive(value: false);
		scenarioCountText.text = missionIdx + 1 + "/" + availableMissionCount;
		SetupScenarioList(viewingCampaign.missions);
		SelectScenario(missionIdx);
	}

	public void TrainingButton()
	{
		missionsParent.gameObject.SetActive(value: false);
		trainingParent.gameObject.SetActive(value: true);
		missionDisplayObj.SetActive(value: false);
		trainingDisplayObj.SetActive(value: true);
		scenarioCountText.text = trainingIdx + 1 + "/" + availableTrainingCount;
		SetupScenarioList(viewingCampaign.trainingMissions);
		SelectScenario(trainingIdx);
	}

	private void SetupScenarioList(List<CampaignScenario> mList)
	{
		foreach (GameObject sListObject in sListObjects)
		{
			UnityEngine.Object.Destroy(sListObject);
		}
		sListObjects.Clear();
		CampaignSave campaignSave = null;
		foreach (CampaignSave campaignSafe in PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).campaignSaves)
		{
			if (campaignSafe.campaignID == viewingCampaign.campaignID)
			{
				campaignSave = campaignSafe;
				break;
			}
		}
		float height = ((RectTransform)sListItemTemplate.transform).rect.height;
		int num = 0;
		for (int i = 0; i < mList.Count; i++)
		{
			CampaignScenario campaignScenario = mList[i];
			if (campaignSave.availableScenarios.Contains(campaignScenario.scenarioID))
			{
				num++;
				GameObject gameObject = UnityEngine.Object.Instantiate(sListItemTemplate, sListItemTemplate.transform.parent);
				gameObject.SetActive(value: true);
				gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * height, 0f);
				gameObject.GetComponent<VRUIListItemTemplate>().Setup(campaignScenario.scenarioName, i, SelectScenario);
				sListObjects.Add(gameObject);
			}
		}
		sListItemTemplate.SetActive(value: false);
		sListScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num * cListHeight);
		VRPointInteractableCanvas componentInParent = sListScrollRect.GetComponentInParent<VRPointInteractableCanvas>();
		if ((bool)componentInParent)
		{
			componentInParent.RefreshInteractables();
		}
	}

	private void SelectScenario(int idx)
	{
		if (idx >= 0 && idx < sListObjects.Count)
		{
			int num = 0;
			if (trainingDisplayObj.activeSelf)
			{
				trainingIdx = idx;
				num = availableTrainingCount;
			}
			else if (missionDisplayObj.activeSelf)
			{
				missionIdx = idx;
				num = availableMissionCount;
			}
			sListScrollRect.ViewContent((RectTransform)sListObjects[idx].transform);
			sListSelectionTf.gameObject.SetActive(value: true);
			if (num == 0)
			{
				scenarioCountText.gameObject.SetActive(value: false);
				return;
			}
			scenarioCountText.gameObject.SetActive(value: true);
			scenarioCountText.text = idx + 1 + "/" + num;
		}
		else
		{
			sListSelectionTf.gameObject.SetActive(value: false);
		}
	}

	public void BackToCampaigns()
	{
		if ((bool)missionsParent)
		{
			UnityEngine.Object.Destroy(missionsParent.gameObject);
		}
		if ((bool)trainingParent)
		{
			UnityEngine.Object.Destroy(trainingParent.gameObject);
		}
		PilotSaveManager.currentCampaign = null;
		scenarioDisplayObject.SetActive(value: false);
		if (PilotSelectUI.returnToArcadeTutorial)
		{
			BackToVehicleButton();
			pilotSelectUI.VehicleToPilotSelectScreen();
			pilotSelectUI.SelectToMainButton();
			return;
		}
		campaignDisplayObject.SetActive(value: true);
		if (openedFromWorkshop)
		{
			BackToVehicleButton();
			pilotSelectUI.OpenWorkshopBrowser();
		}
	}

	public void BriefingBackButton()
	{
		if ((bool)missionBriefingObject)
		{
			UnityEngine.Object.Destroy(missionBriefingObject);
		}
		PilotSaveManager.currentScenario = null;
		scenarioDisplayObject.SetActive(value: true);
		missionBriefingDisplayObject.SetActive(value: false);
		BGMManager.FadeIn();
		if (openedFromWorkshop && !openedWorkshopCampaign)
		{
			BackToCampaigns();
			BackToVehicleButton();
			pilotSelectUI.OpenWorkshopBrowser();
		}
		else if (openedTutorial)
		{
			openedTutorial = false;
			BackToCampaigns();
			BackToVehicleButton();
			pilotSelectUI.OpenTutorialsPage();
		}
	}
}
