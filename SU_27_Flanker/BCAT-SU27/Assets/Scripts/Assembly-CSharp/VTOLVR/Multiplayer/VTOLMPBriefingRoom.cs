using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTOLMPBriefingRoom : MonoBehaviour
{
	public VTOLMPBriefingRoomUI ui;

	public BriefingSpawnPoint[] alliedSpawnTransforms;

	public BriefingSpawnPoint[] enemySpawnTransforms;

	public VTOLMPMissionBriefing alliedBriefing;

	public BriefingSpawnPoint alliedBriefingControllerTf;

	public VTOLMPMissionBriefing enemyBriefing;

	public BriefingSpawnPoint enemyBriefingControllerTf;

	private bool clientReady;

	private List<VehicleSlotListItem> slotUIs = new List<VehicleSlotListItem>();

	private Teams myTeam;

	private Coroutine waitForMCVehicleRoutine;

	private GameObject configObj;

	private VTScenarioInfo selectedNewMission;

	private string newLobbyName;

	public static VTOLMPBriefingRoom instance { get; private set; }

	public Loadout playerLoadout { get; set; }

	private void Awake()
	{
		instance = this;
	}

	public void SetupForTeam()
	{
		myTeam = VTOLMPLobbyManager.localPlayerInfo.team;
		Debug.Log($"Setting up briefing room for Team {myTeam}");
		VTOLMPLobbyManager.instance.OnConnectedPlayerListUpdated += UpdateAllSlots;
		SetupSlots(ui.slotsScrollRect, (myTeam == Teams.Allied) ? VTOLMPSceneManager.instance.alliedSlots : VTOLMPSceneManager.instance.enemySlots, slotUIs);
		UpdateAllSlots();
		VTOLMPSceneManager.instance.OnSlotUpdated += Instance_OnSlotUpdated;
		UpdateButtonVisibility();
		VTOLMPSceneManager.instance.OnMPScenarioStart += Instance_OnMPScenarioStart;
		ui.clientReadyIndicator.SetActive(value: false);
		ui.otherTeamInfoObj.SetActive(!VTMapManager.fetch.mpScenarioStart);
		VTOLMPSceneManager.instance.OnDeclaredReady += Instance_OnDeclaredReady;
		EndMission.OnFinalWinner += EndMission_OnFinalWinner;
		if (MissionManager.instance.finalWinner != 0)
		{
			EndMission_OnFinalWinner((MissionManager.instance.finalWinner != MissionManager.FinalWinner.Allied) ? Teams.Enemy : Teams.Allied);
		}
		bool flag = !enemyBriefing;
		VTOLMPBriefingManager.instance.briefingBothTeams = flag;
		if (myTeam == Teams.Allied)
		{
			if ((bool)alliedBriefing)
			{
				alliedBriefing.Setup(flag);
			}
		}
		else if ((bool)enemyBriefing)
		{
			enemyBriefing.Setup(flag);
		}
		else if ((bool)alliedBriefing)
		{
			alliedBriefing.Setup(flag);
		}
	}

	private void OnDestroy()
	{
		EndMission.OnFinalWinner -= EndMission_OnFinalWinner;
		if (VTOLMPLobbyManager.hasInstance)
		{
			VTOLMPLobbyManager.instance.OnConnectedPlayerListUpdated -= UpdateAllSlots;
		}
	}

	private void EndMission_OnFinalWinner(Teams obj)
	{
		if ((bool)configObj)
		{
			CloseEquipConfig();
		}
		ui.slotsMenuDisplayObj.SetActive(value: false);
		ui.teamAWinObj.SetActive(obj == Teams.Allied);
		ui.teamBWinObj.SetActive(obj == Teams.Enemy);
		if (VTOLMPLobbyManager.isLobbyHost)
		{
			VTOLMPLobbyManager.currentLobby.SetData("gState", VTOLMPLobbyManager.GameStates.Debrief.ToString());
		}
		UpdateButtonVisibility();
	}

	private void Instance_OnDeclaredReady(ulong arg1, bool arg2)
	{
		Teams teams = ((myTeam == Teams.Allied) ? Teams.Enemy : Teams.Allied);
		int num = 0;
		int num2 = 0;
		foreach (PlayerInfo connectedPlayer in VTOLMPLobbyManager.instance.connectedPlayers)
		{
			if (connectedPlayer.team == teams)
			{
				num++;
				if (connectedPlayer.isReady)
				{
					num2++;
				}
			}
		}
		if (num > 0 && num2 >= num)
		{
			ui.otherTeamNotReady.SetActive(value: false);
			ui.otherTeamReady.SetActive(value: true);
		}
		else
		{
			ui.otherTeamNotReady.SetActive(value: true);
			ui.otherTeamReady.SetActive(value: false);
		}
	}

	private void Instance_OnMPScenarioStart()
	{
		if (!VTOLMPLobbyManager.isLobbyHost)
		{
			if (clientReady)
			{
				if (IsInCopilotSlot())
				{
					TryStartWaitingForMC();
				}
				else
				{
					EnterVehicle();
				}
			}
		}
		else if (IsInCopilotSlot())
		{
			if (clientReady)
			{
				TryStartWaitingForMC();
			}
		}
		else
		{
			EnterVehicle();
		}
		UpdateButtonVisibility();
		ui.otherTeamInfoObj.SetActive(value: false);
		VTOLMPSceneManager.instance.OnMPScenarioStart -= Instance_OnMPScenarioStart;
	}

	public void EnterVehicle()
	{
		if (VTOLMPLobbyManager.localPlayerInfo.selectedSlot < 0)
		{
			return;
		}
		ui.voiceToggleObj.SetActive(value: false);
		foreach (VRHandController controller in VRHandController.controllers)
		{
			if ((bool)controller)
			{
				controller.ReleaseFromInteractable();
			}
		}
		if ((bool)configObj)
		{
			CloseEquipConfig();
		}
		VTOLMPSceneManager.instance.SpawnVehicleForMe();
	}

	public void ToggleClientReady()
	{
		clientReady = !clientReady;
		ui.clientReadyIndicator.SetActive(clientReady);
		VTOLMPSceneManager.instance.DeclareReady(clientReady);
		TryStartWaitingForMC();
	}

	private bool IsInCopilotSlot()
	{
		VTOLMPSceneManager.VehicleSlot slot = VTOLMPSceneManager.instance.GetSlot(VTOLMPSceneManager.instance.localPlayer);
		if (slot != null && slot.seatIdx > 0)
		{
			return true;
		}
		return false;
	}

	private void TryStartWaitingForMC()
	{
		if (clientReady && VTMapManager.fetch.mpScenarioStart && IsInCopilotSlot())
		{
			if (waitForMCVehicleRoutine != null)
			{
				StopCoroutine(waitForMCVehicleRoutine);
			}
			waitForMCVehicleRoutine = StartCoroutine(WaitForMCVehicleRoutine());
		}
	}

	private IEnumerator WaitForMCVehicleRoutine()
	{
		while (clientReady)
		{
			VTOLMPSceneManager.VehicleSlot slot = VTOLMPSceneManager.instance.GetSlot(VTOLMPSceneManager.instance.localPlayer);
			if (slot != null && slot.seatIdx > 0)
			{
				MultiplayerSpawn spawn = VTOLMPSceneManager.instance.GetSpawn(slot.team, slot.idx);
				if ((bool)spawn && (bool)spawn.actor && spawn.actor.alive)
				{
					EnterVehicle();
					ToggleClientReady();
					break;
				}
				yield return null;
				continue;
			}
			break;
		}
	}

	public void UpdateButtonVisibility()
	{
		bool flag = VTOLMPLobbyManager.localPlayerInfo.selectedSlot >= 0;
		VTOLMPSceneManager.VehicleSlot slot = VTOLMPSceneManager.instance.GetSlot(VTOLMPSceneManager.instance.localPlayer);
		bool flag2 = MissionManager.instance.finalWinner != MissionManager.FinalWinner.None;
		ui.scenarioStartedObj.SetActive(VTMapManager.fetch.mpScenarioStart && !flag2);
		ui.equipConfigButton.SetActive(flag && !flag2 && slot.seatIdx == 0);
		ui.missionFinishedObj.SetActive(flag2);
		ui.hostNewGameButton.SetActive(VTOLMPLobbyManager.isLobbyHost);
		ui.skipMissionButton.SetActive(VTOLMPLobbyManager.isLobbyHost);
		if ((bool)ui.briefingControlButton)
		{
			ui.briefingControlButton.SetActive(!VTMapManager.fetch.mpScenarioStart && (bool)alliedBriefing);
		}
		if (flag2)
		{
			ui.slotsMenuDisplayObj.SetActive(value: false);
		}
		if (VTOLMPLobbyManager.isLobbyHost)
		{
			ui.clientReadyObject.SetActive(value: false);
			if (!flag2)
			{
				ui.hostStartButton.SetActive(!VTMapManager.fetch.mpScenarioStart);
				if (VTMapManager.fetch.mpScenarioStart)
				{
					ui.enterVehicleButton.SetActive(flag);
					ui.selectASlotObj.SetActive(!flag);
				}
				else
				{
					ui.enterVehicleButton.SetActive(value: false);
					ui.selectASlotObj.SetActive(value: false);
				}
			}
			else
			{
				ui.enterVehicleButton.SetActive(value: false);
				ui.selectASlotObj.SetActive(value: false);
				ui.hostStartButton.SetActive(value: false);
			}
		}
		else
		{
			ui.hostStartButton.SetActive(value: false);
			if (!flag2)
			{
				ui.selectASlotObj.SetActive(!flag);
				if (VTMapManager.fetch.mpScenarioStart)
				{
					ui.enterVehicleButton.SetActive(flag);
					ui.clientReadyObject.SetActive(value: false);
				}
				else
				{
					ui.enterVehicleButton.SetActive(value: false);
					if (flag && !VTMapManager.fetch.mpScenarioStart)
					{
						ui.clientReadyObject.SetActive(value: true);
					}
					else
					{
						ui.clientReadyObject.SetActive(value: false);
						if (clientReady)
						{
							ToggleClientReady();
						}
					}
				}
			}
			else
			{
				ui.selectASlotObj.SetActive(value: false);
				ui.enterVehicleButton.SetActive(value: false);
				ui.clientReadyObject.SetActive(value: false);
			}
		}
		if (slot != null && slot.seatIdx > 0 && (bool)VTMapManager.fetch && VTMapManager.fetch.mpScenarioStart)
		{
			ui.enterVehicleButton.SetActive(value: false);
			ui.clientReadyObject.SetActive(value: true);
			TryStartWaitingForMC();
		}
	}

	private void Instance_OnSlotUpdated(VTOLMPSceneManager.VehicleSlot slot)
	{
		Debug.Log($"A slot has been updated: {slot.team}:{slot.idx}");
		if (slot.team == VTOLMPLobbyManager.localPlayerInfo.team)
		{
			foreach (VehicleSlotListItem slotUI in slotUIs)
			{
				if (slotUI.slot == slot)
				{
					slotUI.Setup(slot);
				}
			}
			UpdateButtonVisibility();
		}
		if (slot.player == null || slot.player != VTOLMPLobbyManager.localPlayerInfo)
		{
			return;
		}
		Debug.Log("Local player has entered a slot.  Loading equipment config!");
		PlayerVehicle playerVehicle2 = (PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(slot.vehicleName));
		PilotSaveManager.current.lastVehicleUsed = playerVehicle2.vehicleName;
		CampaignSave campaignSave = PilotSaveManager.current.GetVehicleSave(playerVehicle2.vehicleName).GetCampaignSave(PilotSaveManager.currentCampaign.campaignID);
		playerLoadout = new Loadout();
		playerLoadout.cmLoadout = new int[2] { 9999, 9999 };
		playerLoadout.normalizedFuel = campaignSave.currentFuel;
		playerLoadout.hpLoadout = new string[playerVehicle2.hardpointCount];
		PlayerInfo localPlayer = VTOLMPSceneManager.instance.localPlayer;
		MultiplayerSpawn mPSpawn = VTOLMPSceneManager.instance.GetMPSpawn(localPlayer.team, localPlayer.selectedSlot);
		List<string> list = ((mPSpawn.equipment == null) ? new List<string>(0) : mPSpawn.equipment.equipment);
		List<string> list2 = new List<string>();
		foreach (GameObject allEquipPrefab in PilotSaveManager.currentVehicle.allEquipPrefabs)
		{
			if (!list.Contains(allEquipPrefab.gameObject.name))
			{
				list2.Add(allEquipPrefab.gameObject.name);
			}
		}
		for (int i = 0; i < playerLoadout.hpLoadout.Length; i++)
		{
			if (list2.Contains(campaignSave.currentWeapons[i]))
			{
				playerLoadout.hpLoadout[i] = campaignSave.currentWeapons[i];
			}
		}
	}

	private void UpdateAllSlots()
	{
		for (int i = 0; i < slotUIs.Count; i++)
		{
			VehicleSlotListItem vehicleSlotListItem = slotUIs[i];
			bool flag = false;
			foreach (PlayerInfo connectedPlayer in VTOLMPLobbyManager.instance.connectedPlayers)
			{
				if (connectedPlayer.team == VTOLMPLobbyManager.localPlayerInfo.team && connectedPlayer.selectedSlot == i)
				{
					vehicleSlotListItem.slot.player = connectedPlayer;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				vehicleSlotListItem.slot.player = null;
			}
			vehicleSlotListItem.Setup(vehicleSlotListItem.slot);
		}
	}

	private void SetupSlots(ScrollRect scrollRect, List<VTOLMPSceneManager.VehicleSlot> slots, List<VehicleSlotListItem> uiList)
	{
		ui.slotTemplate.SetActive(value: false);
		float num = ((RectTransform)ui.slotTemplate.transform).rect.height * ui.slotTemplate.transform.localScale.y;
		for (int i = 0; i < slots.Count; i++)
		{
			GameObject obj = Object.Instantiate(ui.slotTemplate, scrollRect.content);
			obj.SetActive(value: true);
			obj.transform.localPosition = new Vector3(0f, (float)(-i) * num, 0f);
			VehicleSlotListItem component = obj.GetComponent<VehicleSlotListItem>();
			component.Setup(slots[i]);
			uiList.Add(component);
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num * (float)slots.Count);
		scrollRect.verticalNormalizedPosition = 1f;
	}

	public void Host_BeginScenario()
	{
		ui.slotsMenuDisplayObj.SetActive(value: false);
		ui.hostStartConfirmDialogue.DisplayConfirmation("Begin Scenario?", "This will begin the scenario whether players are ready or not.  Players will still be able to join late.", FinallyHostBegin, delegate
		{
			ui.slotsMenuDisplayObj.SetActive(value: true);
		});
	}

	private void FinallyHostBegin()
	{
		ui.slotsMenuDisplayObj.SetActive(value: true);
		if (VTOLMPLobbyManager.localPlayerInfo.selectedSlot >= 0 && !IsInCopilotSlot())
		{
			ControllerEventHandler.PauseEvents();
		}
		VTOLMPSceneManager.instance.Host_BeginScenario();
		UpdateButtonVisibility();
	}

	public void QuitButton()
	{
		ui.slotsMenuDisplayObj.SetActive(value: false);
		ui.missionFinishedObj.SetActive(value: false);
		ui.hostStartConfirmDialogue.DisplayConfirmation("Leave Game?", "Are you sure you want to leave this game?", delegate
		{
			StartCoroutine(QuitRoutine());
		}, delegate
		{
			ui.slotsMenuDisplayObj.SetActive(MissionManager.instance.finalWinner == MissionManager.FinalWinner.None);
			ui.missionFinishedObj.SetActive(MissionManager.instance.finalWinner != MissionManager.FinalWinner.None);
		});
	}

	private IEnumerator QuitRoutine()
	{
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black, 2f);
		yield return new WaitForSeconds(2.2f);
		VTOLMPSceneManager.instance.DisconnectToMainMenu();
		ControllerEventHandler.UnpauseEvents();
	}

	public void OpenEquipConfig()
	{
		int selectedSlot = VTOLMPLobbyManager.localPlayerInfo.selectedSlot;
		if (selectedSlot < 0)
		{
			return;
		}
		ui.slotsMenuDisplayObj.SetActive(value: false);
		PlayerVehicle playerVehicle = VTResources.GetPlayerVehicle(((VTOLMPLobbyManager.localPlayerInfo.team == Teams.Allied) ? VTOLMPSceneManager.instance.alliedSlots : VTOLMPSceneManager.instance.enemySlots)[selectedSlot].vehicleName);
		GameObject uiOnlyConfiguratorPrefab = playerVehicle.uiOnlyConfiguratorPrefab;
		PilotSaveManager.currentVehicle = playerVehicle;
		PilotSaveManager.current.lastVehicleUsed = playerVehicle.vehicleName;
		CampaignSave campaignSave = PilotSaveManager.current.GetVehicleSave(playerVehicle.vehicleName).GetCampaignSave(PilotSaveManager.currentCampaign.campaignID);
		configObj = Object.Instantiate(uiOnlyConfiguratorPrefab, ui.configUIParent);
		configObj.transform.localPosition = Vector3.zero;
		configObj.transform.localRotation = Quaternion.identity;
		configObj.transform.localScale = Vector3.one;
		LoadoutConfigurator component = configObj.GetComponent<LoadoutConfigurator>();
		component.wm = playerVehicle.vehiclePrefab.GetComponent<WeaponManager>();
		Debug.Log("Opening configurator with allowed equips:");
		foreach (string item in VTOLMPSceneManager.instance.GetMPSpawn(VTOLMPLobbyManager.localPlayerInfo.team, selectedSlot).equipment.equipment)
		{
			Debug.Log(" - " + item);
		}
		Debug.Log($"Scenario game version: {VTScenario.currentScenarioInfo.gameVersion}");
		component.availableEquipStrings = new List<string>();
		PlayerInfo localPlayer = VTOLMPSceneManager.instance.localPlayer;
		List<string> equipment = VTOLMPSceneManager.instance.GetMPSpawn(localPlayer.team, localPlayer.selectedSlot).equipment.equipment;
		foreach (GameObject allEquipPrefab in PilotSaveManager.currentVehicle.allEquipPrefabs)
		{
			if (!equipment.Contains(allEquipPrefab.gameObject.name))
			{
				component.availableEquipStrings.Add(allEquipPrefab.gameObject.name);
			}
		}
		component.Initialize(PilotSaveManager.current.GetVehicleSave(playerVehicle.vehicleName).GetCampaignSave(PilotSaveManager.currentCampaign.campaignID));
		if (campaignSave.currentWeapons == null)
		{
			return;
		}
		for (int i = 0; i < campaignSave.currentWeapons.Length; i++)
		{
			if (!component.lockedHardpoints.Contains(i))
			{
				string text = campaignSave.currentWeapons[i];
				if (!string.IsNullOrEmpty(text) && component.availableEquipStrings.Contains(text))
				{
					component.AttachImmediate(text, i);
				}
			}
		}
	}

	public void CloseEquipConfig()
	{
		LoadoutConfigurator component = configObj.GetComponent<LoadoutConfigurator>();
		playerLoadout = component.SaveConfig();
		PilotSaveManager.SavePilotsToFile();
		ui.slotsMenuDisplayObj.SetActive(value: true);
		Object.Destroy(configObj);
	}

	public void RequestBriefingControlButton()
	{
		if (!VTMapManager.fetch.mpScenarioStart)
		{
			VTOLMPBriefingManager.instance.RequestBriefingControl();
		}
	}

	public void ReleaseBriefingControlButton()
	{
		VTOLMPBriefingManager.instance.ReleaseControl();
	}

	public void SkipMissionButton()
	{
		ui.slotsMenuDisplayObj.SetActive(value: false);
		ui.hostStartConfirmDialogue.DisplayConfirmation("Skip Mission?", "This will end the mission and allow you to choose a new one.", FinallySkipMission, delegate
		{
			ui.slotsMenuDisplayObj.SetActive(value: true);
		});
	}

	private void FinallySkipMission()
	{
		ui.slotsMenuDisplayObj.SetActive(value: true);
		MissionManager.instance.SkipMission();
		UpdateButtonVisibility();
	}

	public void HostNewGameButton()
	{
		ui.hostNewGameWindow.SetActive(value: true);
		ui.missionFinishedObj.SetActive(value: false);
		newLobbyName = VTOLMPLobbyManager.currentLobby.GetData("lName");
		ui.lobbyNameText.text = newLobbyName;
		selectedNewMission = VTScenario.currentScenarioInfo;
		NG_UpdateSelectedMission();
	}

	private void NG_UpdateSelectedMission()
	{
		ui.missionNameText.text = selectedNewMission.name;
		ui.missionDescriptionText.text = selectedNewMission.description;
		ui.missionImage.texture = selectedNewMission.image;
		ui.mapImage.texture = VTResources.GetMapForScenario(selectedNewMission, out var _).previewImage;
		ui.scenarioSettings.SetupScenarioSettings(selectedNewMission);
		ui.newMissionPlayerCountWarning.SetActive(selectedNewMission.mpPlayerCount < VTOLMPLobbyManager.currentLobby.MemberCount);
	}

	public void NG_BackButton()
	{
		ui.hostNewGameWindow.SetActive(value: false);
		ui.missionFinishedObj.SetActive(value: true);
	}

	public void NG_StartButton()
	{
		StartCoroutine(HostNewMissionRoutine());
	}

	private IEnumerator HostNewMissionRoutine()
	{
		VTScenarioInfo s = selectedNewMission;
		VTOLMPLobbyManager.currentLobby.SetData("scID", VTOLMPLobbyManager.GenerateScenarioID(s));
		VTOLMPLobbyManager.instance.Host_SendTransitionToNewGameMsg(selectedNewMission);
		ui.hostNewGameWindow.SetActive(value: false);
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut();
		yield return new WaitForSeconds(3f);
		VTNetworkManager.StopHost();
		Debug.Log("Launching Multiplayer game for " + s.campaignID + ":" + s.id + " (map:" + s.mapID + ")");
		VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
		VTCampaignInfo vTCampaignInfo = null;
		if (s.isBuiltIn)
		{
			PilotSaveManager.currentCampaign = (vTCampaignInfo = VTResources.GetBuiltInCampaign(s.campaignID)).ToIngameCampaign();
			VTOLMPLobbyManager.currentLobby.DeleteData("wsUploadVersion");
		}
		else
		{
			if (string.IsNullOrEmpty(s.campaignID))
			{
				PilotSaveManager.currentCampaign = ui.swStandaloneCampaign;
			}
			else
			{
				PilotSaveManager.currentCampaign = (vTCampaignInfo = VTResources.GetSteamWorkshopCampaign(s.campaignID)).ToIngameCampaign();
			}
			int num = 0;
			if (vTCampaignInfo.config.HasValue("wsUploadVersion"))
			{
				num = vTCampaignInfo.config.GetValue<int>("wsUploadVersion");
			}
			VTOLMPLobbyManager.currentLobby.SetData("wsUploadVersion", num.ToString());
		}
		CampaignScenario campaignScenario2 = (PilotSaveManager.currentScenario = s.ToIngameScenario(vTCampaignInfo));
		PlayerVehicle[] playerVehicles = VTResources.GetPlayerVehicles();
		foreach (PlayerVehicle overrideVehicle in playerVehicles)
		{
			CampaignSelectorUI.SetUpCampaignSave(PilotSaveManager.currentCampaign, null, null, null, overrideVehicle);
		}
		int maxPlayerCount = VTOLMPUtils.GetMaxPlayerCount(s);
		VTOLMPLobbyManager.currentLobby.SetData("scn", s.name);
		VTOLMPLobbyManager.currentLobby.SetData("lName", newLobbyName);
		VTOLMPLobbyManager.currentLobby.SetData("maxP", maxPlayerCount.ToString());
		VTOLMPLobbyManager.currentLobby.SetData("gState", VTOLMPLobbyManager.GameStates.Briefing.ToString());
		VTOLMPLobbyManager.currentLobby.MaxMembers = maxPlayerCount + 1;
		VTOLMPLobbyManager.instance.SetCurrentMaxPlayers(maxPlayerCount);
		PlayerInfo[] array = VTOLMPLobbyManager.instance.connectedPlayers.ToArray();
		for (int j = 0; j < array.Length; j++)
		{
			array[j].chosenTeam = false;
			array[j].isReady = false;
			array[j].selectedSlot = -1;
			if (j + 1 > maxPlayerCount)
			{
				VTOLMPSceneManager.instance.KickMember(array[j].steamUser.Id);
			}
		}
		VTOLMPLobbyManager.localPlayerInfo.chosenTeam = false;
		VTOLMPLobbyManager.localPlayerInfo.selectedSlot = -1;
		VTOLMPLobbyManager.localPlayerInfo.isReady = false;
		VTOLMPLobbyManager.instance.SendPlayerInfosToClients(clearTeams: true);
		ui.scenarioSettings.GetFinalSettings(out var envIdx, out var unitIcons, out var briefingRoomIdx);
		if (s.selectableEnv)
		{
			campaignScenario2.envIdx = envIdx;
		}
		VTOLMPLobbyManager.currentLobby.SetData("envIdx", envIdx.ToString());
		VTOLMPLobbyManager.currentLobby.SetData("brtype", briefingRoomIdx.ToString());
		VTOLMPSceneManager.unitIcons = unitIcons;
		EndMission.Stop();
		VTScenario.current = null;
		VTScenario.currentScenarioInfo = s;
		VTResources.LaunchMapForScenario(s, skipLoading: false);
		ControllerEventHandler.UnpauseEvents();
	}

	public void NG_SetLobbyNameButton()
	{
		ui.hostNewGameWindow.SetActive(value: false);
		ui.keyboard.Display(newLobbyName, 36, OnLobbyNameEntered, delegate
		{
			ui.hostNewGameWindow.SetActive(value: true);
		});
	}

	private void OnLobbyNameEntered(string s)
	{
		newLobbyName = s;
		ui.hostNewGameWindow.SetActive(value: true);
		ui.lobbyNameText.text = newLobbyName;
	}

	public void NG_SelectMissionButton()
	{
		ui.hostNewGameWindow.SetActive(value: false);
		ui.missionBrowser.Open(selectedNewMission, OnSelectNewMission);
	}

	private void OnSelectNewMission(VTScenarioInfo s)
	{
		ui.hostNewGameWindow.SetActive(value: true);
		selectedNewMission = s;
		NG_UpdateSelectedMission();
	}
}

}