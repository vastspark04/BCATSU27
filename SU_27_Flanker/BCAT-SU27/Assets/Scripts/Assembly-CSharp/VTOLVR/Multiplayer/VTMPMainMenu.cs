using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using UnityEngine;
using UnityEngine.CrashReportHandler;
using UnityEngine.UI;

namespace VTOLVR.Multiplayer{

public class VTMPMainMenu : MonoBehaviour
{
	private static SteamId friendLobbyToJoinID;

	private static Lobby friendLobbyToJoin;

	public GameObject displayObj;

	public PilotSelectUI pilotSelect;

	public GameObject mainMenuDisplayObj;

	public Campaign swStandaloneCampaign;

	public VRPointInteractableCanvas vrCanvas;

	public ScrollRect lobbiesScrollRect;

	public GameObject lobbyItemTemplate;

	public GameObject refreshButton;

	public GameObject mainVehicleSelectObj;

	public VTUIErrorWindow errorUI;

	public VTConfirmationDialogue confirmUI;

	public GameObject joiningLobbyWaitingObj;

	public GameObject retrievingScenarioObj;

	public GameObject installingItemObj;

	public Transform installProgressTransform;

	public GameObject creatingLobbyWaitingObj;

	public GameObject enterPasswordJoinPrivateObj;

	public GameObject joiningPrivateObj;

	private static bool hasWarnedModded = false;

	private bool isRefreshing;

	private List<GameObject> lobbyObjs = new List<GameObject>();

	private static Queue<string> queuedErrors = new Queue<string>();

	[Header("Lobby Creation")]
	public GameObject newLobbyDisplayObj;

	public VRKeyboard vrKeyboard;

	public Text newLobbyNameText;

	public GameObject enteringNameObj;

	private string newLobbyName = string.Empty;

	private VTScenarioInfo selectedScenario;

	public VTMPScenarioSettings scenarioOptionsMenu;

	public Text selectedScenarioText;

	public MPMissionBrowser missionBrowser;

	public RawImage selectedScenarioImage;

	public RawImage selectedMapImage;

	public Text selectedScenarioDescription;

	public Text privacyText;

	public Text passwordText;

	public GameObject passwordButton;

	private VTOLMPLobbyManager.Privacy privacy;

	private string privatePassword;

	public GameObject publicIndicator;

	public GameObject privateIndicator;

	private bool privateMode;

	public static VTMPMainMenu instance { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
		SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataChanged;
	}

	public static void FriendLobbyJoinRequested(Lobby lobby, SteamId steamId)
	{
		friendLobbyToJoin = lobby;
		friendLobbyToJoinID = lobby.Id;
		if ((bool)instance && instance.mainMenuDisplayObj.activeInHierarchy)
		{
			instance.ProcessJoinFriend();
		}
	}

	private void ProcessJoinFriend()
	{
		if (friendLobbyToJoinID.Value != 0L)
		{
			Debug.Log($"Processing Join Friend Request. lobbyID: {friendLobbyToJoinID.Value}");
			SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChanged;
			friendLobbyToJoin.Refresh();
		}
	}

	private void OnLobbyDataChanged(Lobby lobby)
	{
		if ((ulong)lobby.Id != (ulong)friendLobbyToJoin.Id)
		{
			return;
		}
		if (int.TryParse(friendLobbyToJoin.GetData("pwh"), out var result))
		{
			if (result == 0)
			{
				JoinLobby(friendLobbyToJoin);
			}
			else
			{
				TryJoinPrivateLobby(friendLobbyToJoin);
			}
		}
		friendLobbyToJoinID = 0uL;
		friendLobbyToJoin = default(Lobby);
		SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataChanged;
	}

	public void Open()
	{
		displayObj.SetActive(value: true);
		mainMenuDisplayObj.SetActive(value: true);
		CrashReportHandler.SetUserMetadata("MP Status", "none");
		VTOLMPLobbyManager.localPlayerInfo.pilotName = PilotSaveManager.current.pilotName;
		VTOLMPLobbyManager.localPlayerInfo.steamUser = new Friend(BDSteamClient.mySteamID);
		VTOLMPLobbyManager.localPlayerInfo.team = Teams.Allied;
		VTOLMPLobbyManager.localPlayerInfo.selectedSlot = -1;
		if (queuedErrors.Count > 0)
		{
			StartCoroutine(QueuedErrorRoutine());
		}
		isRefreshing = false;
		JoinPublicButton();
		if (!hasWarnedModded)
		{
			if (GameStartup.version.releaseType == GameVersion.ReleaseTypes.Modded || Input.GetKey(KeyCode.RightShift))
			{
				ShowError("Mod Loader detected. You will only be able to join other modded games.");
			}
			else
			{
				if (GameStartup.version.releaseType == GameVersion.ReleaseTypes.Testing)
				{
					ShowError("public_testing build -- You will only be able to join other public_testing lobbies.");
				}
				ProcessJoinFriend();
			}
			hasWarnedModded = true;
		}
		else
		{
			ProcessJoinFriend();
		}
	}

	private IEnumerator QueuedErrorRoutine()
	{
		while (queuedErrors.Count > 0)
		{
			ShowError(queuedErrors.Dequeue());
			yield return null;
			while (errorUI.gameObject.activeSelf)
			{
				yield return null;
			}
		}
	}

	public void RefreshLobbiesButton()
	{
		StartCoroutine(RefreshRoutine(privateMode));
	}

	private IEnumerator RefreshRoutine(bool privateLobbies = false)
	{
		if (isRefreshing)
		{
			yield break;
		}
		isRefreshing = true;
		refreshButton.SetActive(value: false);
		lobbyItemTemplate.SetActive(value: false);
		foreach (GameObject lobbyObj in lobbyObjs)
		{
			UnityEngine.Object.Destroy(lobbyObj);
		}
		lobbyObjs.Clear();
		LobbyQuery lobbyQuery = SteamMatchmaking.LobbyList.FilterDistanceWorldwide().WithMaxResults(100).WithEqual("feature", (int)GameStartup.version.releaseType)
			.WithHigher("maxP", 1)
			.WithEqual("lReady", 1);
		Task<Lobby[]> request = ((!privateLobbies) ? lobbyQuery.WithEqual("pwh", 0) : lobbyQuery.WithNotEqual("pwh", 0)).RequestAsync();
		while (!request.IsCompleted)
		{
			yield return null;
		}
		if (request.Result != null)
		{
			List<Lobby> list = new List<Lobby>();
			Lobby[] result = request.Result;
			for (int i = 0; i < result.Length; i++)
			{
				Lobby lobby = result[i];
				if (!string.IsNullOrEmpty(lobby.GetData("lName")) && !string.IsNullOrEmpty(lobby.GetData("scID")) && !string.IsNullOrEmpty(lobby.GetData("pwh")) && !string.IsNullOrEmpty(lobby.GetData("maxP")) && !string.IsNullOrEmpty(lobby.GetData("oName")) && VTOLMPLobbyManager.CheckHasRequiredDLCs(lobby))
				{
					list.Add(lobby);
				}
			}
			list.Sort(LobbySorter);
			float num = ((RectTransform)lobbyItemTemplate.transform).rect.height * lobbyItemTemplate.transform.localScale.y;
			for (int j = 0; j < list.Count; j++)
			{
				Lobby l = list[j];
				GameObject gameObject = UnityEngine.Object.Instantiate(lobbyItemTemplate, lobbiesScrollRect.content);
				gameObject.SetActive(value: true);
				gameObject.transform.localPosition = new Vector3(0f, (float)(-j) * num, 0f);
				try
				{
					gameObject.GetComponent<VTMPLobbyListItem>().UpdateForLobby(l);
				}
				catch (Exception message)
				{
					Debug.LogError(message);
					UnityEngine.Object.Destroy(gameObject);
					list.RemoveAt(j);
					j--;
				}
				if ((bool)gameObject)
				{
					lobbyObjs.Add(gameObject);
				}
			}
			lobbiesScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num * (float)list.Count);
			lobbiesScrollRect.verticalNormalizedPosition = 1f;
		}
		isRefreshing = false;
		refreshButton.SetActive(value: true);
		vrCanvas.RefreshInteractables();
	}

	private int LobbySorter(Lobby a, Lobby b)
	{
		if (a.MemberCount < GetMaxPlayers(a) && b.MemberCount < GetMaxPlayers(b))
		{
			return b.MemberCount.CompareTo(a.MemberCount);
		}
		return a.MemberCount.CompareTo(b.MemberCount);
	}

	private int GetMaxPlayers(Lobby l)
	{
		return int.Parse(l.GetData("maxP"));
	}

	public void BackToVTOLButton()
	{
		displayObj.SetActive(value: false);
		mainVehicleSelectObj.SetActive(value: true);
	}

	public void TryJoinPrivateLobby(Lobby l)
	{
		StartCoroutine(JoinPrivateLobbyRoutine(l));
	}

	public void JoinLobby(Lobby l, string pw = null)
	{
		StartCoroutine(JoinLobbyRoutine(l, pw));
	}

	private IEnumerator JoinLobbyRoutine(Lobby l, string pw)
	{
		ControllerEventHandler.PauseEvents();
		Debug.Log(string.Format("VTMPMainMenu: Attempting to join lobby {0} ({1})", l.Id, l.GetData("lName")));
		joiningLobbyWaitingObj.SetActive(value: true);
		VTOLMPLobbyManager.LobbyTask joinTask = VTOLMPLobbyManager.JoinLobby(l, pw);
		while (!joinTask.isDone)
		{
			yield return null;
		}
		joiningLobbyWaitingObj.SetActive(value: false);
		joiningPrivateObj.SetActive(value: false);
		mainMenuDisplayObj.SetActive(value: true);
		if (!joinTask.isError)
		{
			StartCoroutine(ClientLaunchLobbyMissionRoutine(l));
			yield break;
		}
		ControllerEventHandler.UnpauseEvents();
		ShowError(joinTask.errorMessage);
	}

	private IEnumerator JoinPrivateLobbyRoutine(Lobby l)
	{
		mainMenuDisplayObj.SetActive(value: false);
		enterPasswordJoinPrivateObj.SetActive(value: true);
		string enteredPw = string.Empty;
		bool entered = false;
		Action<string> onEntered = delegate(string s)
		{
			enteredPw = s;
			entered = true;
		};
		vrKeyboard.Display(string.Empty, 10, onEntered, delegate
		{
			mainMenuDisplayObj.SetActive(value: true);
			enterPasswordJoinPrivateObj.SetActive(value: false);
		});
		while (!entered)
		{
			yield return null;
		}
		enterPasswordJoinPrivateObj.SetActive(value: false);
		if (VTOLMPLobbyManager.instance.TryPassword(enteredPw, l))
		{
			JoinLobby(l, enteredPw);
			yield break;
		}
		mainMenuDisplayObj.SetActive(value: true);
		ShowError("Incorrect password.");
	}

	private IEnumerator ClientLaunchLobbyMissionRoutine(Lobby l)
	{
		Debug.Log("VTMPMainMenu: Successfully joined.  Retreiving scenario...");
		joiningLobbyWaitingObj.SetActive(value: false);
		bool retrievingScenario = true;
		while (retrievingScenario)
		{
			Debug.Log("VTMPMainMenu: requesting scenario....");
			VTOLMPLobbyManager.MPScenarioRequest scenarioReq = VTOLMPLobbyManager.GetScenario(l);
			retrievingScenarioObj.SetActive(value: true);
			installingItemObj.SetActive(value: false);
			while (scenarioReq.status == VTOLMPLobbyManager.MPScenarioRequestStatus.Waiting || scenarioReq.status == VTOLMPLobbyManager.MPScenarioRequestStatus.Installing)
			{
				if (scenarioReq.status == VTOLMPLobbyManager.MPScenarioRequestStatus.Installing)
				{
					installingItemObj.SetActive(value: true);
					installProgressTransform.localScale = new Vector3(scenarioReq.downloadProgress, 1f, 1f);
				}
				yield return null;
			}
			retrievingScenarioObj.SetActive(value: false);
			if (scenarioReq.status == VTOLMPLobbyManager.MPScenarioRequestStatus.NeedSubscription)
			{
				Debug.Log("VTMPMainMenu: scenario is a workshop scenario that we haven't subscribed yet...");
				ControllerEventHandler.UnpauseEvents();
				int selection = 0;
				confirmUI.DisplayConfirmation("Workshop Content", "This game is running Steam Workshop content.  Would you like to download the necessary content?", delegate
				{
					selection = 1;
				}, delegate
				{
					selection = 2;
				});
				while (selection == 0)
				{
					yield return null;
				}
				if (selection != 1)
				{
					Debug.Log("VTMPMainMenu: User has declined to install the new workshop content.");
					VTOLMPLobbyManager.LeaveLobby();
					ControllerEventHandler.UnpauseEvents();
					break;
				}
				Debug.Log("VTMPMainMenu: user has chosen to install the new content");
				ControllerEventHandler.PauseEvents();
				installingItemObj.SetActive(value: true);
				installProgressTransform.localScale = new Vector3(0.01f, 1f, 1f);
				Task<Item?> query = SteamUGC.QueryFileAsync(scenarioReq.workshopId);
				while (!query.IsCompleted)
				{
					retrievingScenarioObj.SetActive(value: true);
					yield return null;
				}
				if (!query.Result.HasValue)
				{
					ShowError("Workshop content not found...");
					retrievingScenarioObj.SetActive(value: false);
					VTOLMPLobbyManager.LeaveLobby();
					ControllerEventHandler.UnpauseEvents();
					break;
				}
				VTSteamWorkshopUtils.AsyncSubscribeItem(query.Result.Value, null);
			}
			else
			{
				if (scenarioReq.status != VTOLMPLobbyManager.MPScenarioRequestStatus.Success)
				{
					if (scenarioReq.status == VTOLMPLobbyManager.MPScenarioRequestStatus.HostHasOldVersion)
					{
						ShowError("Error: Host has an old version of the workshop content.  Host needs to update!");
						VTOLMPLobbyManager.LeaveLobby();
						ControllerEventHandler.UnpauseEvents();
					}
					else
					{
						ShowError($"Error: {scenarioReq.status}");
						VTOLMPLobbyManager.LeaveLobby();
						ControllerEventHandler.UnpauseEvents();
					}
					break;
				}
				Debug.Log("VTMPMainMenu: Successfully retrieved the MP scenario!");
				retrievingScenario = false;
				joiningLobbyWaitingObj.SetActive(value: true);
				ScreenFader.FadeOut(UnityEngine.Color.black, 2f);
				yield return new WaitForSeconds(2.2f);
				CrashReportHandler.SetUserMetadata("MP Status", "client");
				ControllerEventHandler.UnpauseEvents();
				LaunchMPGameForScenario(scenarioReq.scenario);
			}
			yield return null;
		}
	}

	public void TransitionToNewMission()
	{
		Open();
		StartCoroutine(ClientLaunchLobbyMissionRoutine(VTOLMPLobbyManager.currentLobby));
	}

	private void ShowError(string msg)
	{
		Debug.Log("VTMPMainMenu: Error: " + msg);
		errorUI.DisplayError(msg, null);
	}

	public static void AddQueuedError(string s)
	{
		queuedErrors.Enqueue(s);
	}

	public void NextPrivacy()
	{
		int num = (int)privacy;
		num = (int)(privacy = (VTOLMPLobbyManager.Privacy)((num + 1) % 3));
		UpdatePrivacyText();
	}

	public void PreviousPrivacy()
	{
		int num = (int)privacy;
		num = (int)(privacy = (VTOLMPLobbyManager.Privacy)((num + 2) % 3));
		UpdatePrivacyText();
	}

	private void UpdatePrivacyText()
	{
		passwordButton.SetActive(privacy == VTOLMPLobbyManager.Privacy.Private);
		privacyText.text = privacy.ToString().Replace('_', ' ');
	}

	public void SetLobbyName()
	{
		newLobbyDisplayObj.SetActive(value: false);
		enteringNameObj.SetActive(value: true);
		vrKeyboard.Display(newLobbyName, 36, OnLobbyNameEntered, delegate
		{
			newLobbyDisplayObj.SetActive(value: true);
			enteringNameObj.SetActive(value: false);
		});
	}

	private void OnLobbyNameEntered(string s)
	{
		newLobbyDisplayObj.SetActive(value: true);
		enteringNameObj.SetActive(value: false);
		if (IsLobbyNameValid(s))
		{
			newLobbyName = s;
			newLobbyNameText.text = s;
		}
		else
		{
			ShowError("Please enter a non-empty lobby name.");
		}
	}

	private bool IsLobbyNameValid(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return false;
		}
		if (UIUtils.ContainsBadWord(s))
		{
			return false;
		}
		return true;
	}

	public void SetLobbyPassword()
	{
		newLobbyDisplayObj.SetActive(value: false);
		vrKeyboard.Display(string.Empty, 10, OnPasswordEntered, delegate
		{
			newLobbyDisplayObj.SetActive(value: true);
		});
	}

	private void OnPasswordEntered(string s)
	{
		if (s.Length < 4)
		{
			s = string.Empty;
			ShowError("Password must be at least 4 characters!");
		}
		newLobbyDisplayObj.SetActive(value: true);
		privatePassword = s;
		passwordText.text = s;
	}

	public void FinallyHostGameButton()
	{
		if (privacy == VTOLMPLobbyManager.Privacy.Private && string.IsNullOrEmpty(privatePassword))
		{
			confirmUI.DisplayConfirmation("Password", "Private lobbies require a password.", SetLobbyPassword, null);
		}
		else
		{
			FinallyCreateLobby(newLobbyNameText.text, selectedScenario);
		}
	}

	public void BackFromHostButton()
	{
		newLobbyName = string.Empty;
		newLobbyDisplayObj.SetActive(value: false);
		mainMenuDisplayObj.SetActive(value: true);
		Open();
	}

	public void HostGameButton()
	{
		mainMenuDisplayObj.SetActive(value: false);
		newLobbyDisplayObj.SetActive(value: true);
		string text = PilotSaveManager.current.pilotName;
		int num = 28;
		if (text.Length > num)
		{
			text = text.Substring(0, num);
		}
		newLobbyNameText.text = text + "'s lobby";
		if (GameSettings.TryGetGameSettingValue<string>("lastMPCampaign", out var val) && GameSettings.TryGetGameSettingValue<string>("lastMPScenario", out var val2))
		{
			selectedScenario = VTResources.GetBuiltInScenario(val, val2);
		}
		if (selectedScenario == null)
		{
			selectedScenario = VTResources.GetBuiltInScenario("airshowFreeflight", "quickMPFlights");
		}
		OnSelectedScenario(selectedScenario);
		scenarioOptionsMenu.SetupScenarioSettings(selectedScenario);
		passwordText.text = privatePassword;
		UpdatePrivacyText();
	}

	private void FinallyCreateLobby(string lobbyName, VTScenarioInfo scenario)
	{
		StartCoroutine(CreateLobbyRoutine(lobbyName, scenario));
	}

	private IEnumerator CreateLobbyRoutine(string lobbyName, VTScenarioInfo scenario)
	{
		int maxPlayerCount = VTOLMPUtils.GetMaxPlayerCount(scenario);
		VTOLMPLobbyManager.LobbyTask task = VTOLMPLobbyManager.CreateLobby(lobbyName, maxPlayerCount, privacy, privatePassword);
		creatingLobbyWaitingObj.SetActive(value: true);
		while (!task.isDone)
		{
			yield return null;
		}
		if (task.isError)
		{
			creatingLobbyWaitingObj.SetActive(value: false);
			ShowError(task.errorMessage);
			yield break;
		}
		VTOLMPLobbyManager.currentLobby.SetData("scID", VTOLMPLobbyManager.GenerateScenarioID(scenario));
		VTOLMPLobbyManager.currentLobby.SetData("lReady", "1");
		VTOLMPLobbyManager.currentLobby.SetData("gState", VTOLMPLobbyManager.GameStates.Briefing.ToString());
		if (scenario.campaign != null && scenario.isWorkshop)
		{
			if (scenario.campaign.wsItem.Download(highPriority: true))
			{
				Debug.Log("CreateLobbyRoutine: Downloading the workshop item again to ensure latest version.");
				float t = Time.realtimeSinceStartup;
				creatingLobbyWaitingObj.SetActive(value: false);
				installingItemObj.SetActive(value: true);
				retrievingScenarioObj.SetActive(value: true);
				while (scenario.campaign.wsItem.IsDownloadPending || scenario.campaign.wsItem.IsDownloading)
				{
					installProgressTransform.localScale = new Vector3(scenario.campaign.wsItem.DownloadAmount, 1f, 1f);
					yield return null;
				}
				creatingLobbyWaitingObj.SetActive(value: true);
				installingItemObj.SetActive(value: false);
				retrievingScenarioObj.SetActive(value: false);
				Debug.Log($"CreateLobbyRoutine: Workshop item download complete in {Time.realtimeSinceStartup - t} seconds");
				string id = scenario.id;
				VTCampaignInfo vTCampaignInfo = VTResources.LoadWorkshopCampaign(scenario.campaign.wsItem);
				scenario = vTCampaignInfo.GetScenario(id);
			}
			int num = 0;
			if (scenario.campaign.config.HasValue("wsUploadVersion"))
			{
				num = scenario.campaign.config.GetValue<int>("wsUploadVersion");
			}
			VTOLMPLobbyManager.currentLobby.SetData("wsUploadVersion", num.ToString());
			Debug.Log($"CreateLobbyRoutine: running workshop item version {num}");
		}
		string value = VTOLMPLobbyManager.GenerateRequiredDLCsList(scenario);
		if (!string.IsNullOrEmpty(value))
		{
			VTOLMPLobbyManager.currentLobby.SetData("dlcReq", value);
		}
		ScreenFader.FadeOut(UnityEngine.Color.black, 2f);
		yield return new WaitForSeconds(2.2f);
		LaunchMPGameForScenario(scenario);
	}

	private void LaunchMPGameForScenario(VTScenarioInfo s)
	{
		Debug.Log("Launching Multiplayer game for " + s.campaignID + ":" + s.id + " (map:" + s.mapID + ")");
		VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
		VTCampaignInfo customCampaign = null;
		if (s.isBuiltIn)
		{
			PilotSaveManager.currentCampaign = (customCampaign = VTResources.GetBuiltInCampaign(s.campaignID)).ToIngameCampaign();
		}
		else if (string.IsNullOrEmpty(s.campaignID))
		{
			PilotSaveManager.currentCampaign = swStandaloneCampaign;
		}
		else
		{
			PilotSaveManager.currentCampaign = (customCampaign = VTResources.GetSteamWorkshopCampaign(s.campaignID)).ToIngameCampaign();
		}
		CampaignScenario campaignScenario2 = (PilotSaveManager.currentScenario = s.ToIngameScenario(customCampaign));
		PlayerVehicle[] playerVehicles = VTResources.GetPlayerVehicles();
		foreach (PlayerVehicle overrideVehicle in playerVehicles)
		{
			CampaignSelectorUI.SetUpCampaignSave(PilotSaveManager.currentCampaign, null, null, null, overrideVehicle);
		}
		if (VTOLMPLobbyManager.isLobbyHost)
		{
			VTOLMPLobbyManager.currentLobby.SetData("scn", s.name);
			scenarioOptionsMenu.GetFinalSettings(out var envIdx, out var unitIcons, out var briefingRoomIdx);
			if (s.selectableEnv)
			{
				campaignScenario2.envIdx = envIdx;
			}
			VTOLMPLobbyManager.currentLobby.SetData("envIdx", envIdx.ToString());
			VTOLMPLobbyManager.currentLobby.SetData("brtype", briefingRoomIdx.ToString());
			VTOLMPSceneManager.unitIcons = unitIcons;
		}
		else
		{
			int num = int.Parse(VTOLMPLobbyManager.currentLobby.GetData("envIdx"));
			if (num >= 0)
			{
				campaignScenario2.envIdx = num;
			}
		}
		VTScenario.currentScenarioInfo = s;
		VTResources.LaunchMapForScenario(s, skipLoading: false);
	}

	public void OpenMissionSelector()
	{
		newLobbyDisplayObj.SetActive(value: false);
		missionBrowser.Open(selectedScenario, OnSelectedScenario);
	}

	private void OnSelectedScenario(VTScenarioInfo s)
	{
		selectedScenario = s;
		newLobbyDisplayObj.SetActive(value: true);
		selectedScenarioText.text = s.name;
		selectedScenarioDescription.text = s.description;
		selectedScenarioImage.texture = s.image;
		selectedMapImage.texture = VTResources.GetMapForScenario(s, out var _).previewImage;
		scenarioOptionsMenu.SetupScenarioSettings(s);
	}

	public void JoinPublicButton()
	{
		privateMode = false;
		publicIndicator.SetActive(value: true);
		privateIndicator.SetActive(value: false);
		RefreshLobbiesButton();
	}

	public void JoinPrivateButton()
	{
		privateMode = true;
		publicIndicator.SetActive(value: false);
		privateIndicator.SetActive(value: true);
		RefreshLobbiesButton();
	}
}

}