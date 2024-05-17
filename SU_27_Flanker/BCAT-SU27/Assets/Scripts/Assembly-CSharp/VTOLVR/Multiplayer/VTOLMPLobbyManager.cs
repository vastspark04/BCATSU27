using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using UnityEngine;
using UnityEngine.CrashReportHandler;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTOLMPLobbyManager : MonoBehaviour
{
	public class LobbyTask
	{
		public Lobby lobby;

		public bool isDone;

		public bool isError;

		public string errorMessage;

		public void SetError(string msg)
		{
			isDone = true;
			isError = true;
			errorMessage = msg;
		}
	}

	public enum Privacy
	{
		Public,
		Private,
		Friends_Only
	}

	private enum JoinRequestStatus
	{
		None,
		Accepted,
		Full,
		Banned,
		WrongPassword
	}

	public enum MPScenarioRequestStatus
	{
		Waiting,
		Installing,
		Success,
		NeedSubscription,
		WorkshopRequestFailed,
		HostHasOldVersion,
		FileNotFound
	}

	public class MPScenarioRequest
	{
		public MPScenarioRequestStatus status;

		public VTScenarioInfo scenario;

		public PublishedFileId workshopId;

		public float downloadProgress;
	}

	public enum GameStates
	{
		Briefing,
		Mission,
		Debrief
	}

	public delegate void VoteDelegate(PlayerInfo target, PlayerInfo voter);

	public static bool UseSocketPW = true;

	private static VTOLMPLobbyManager _instance;

	private static PlayerInfo _localPlayerInfo;

	public static Lobby currentLobby;

	private static int currentMaxPlayers;

	private string hostedLobbyPassword;

	private bool hasSetupChatPrefixDict;

	private Dictionary<string, Action<SteamId, string>> chatDict;

	private const string chatPrefix_system = "$sys_";

	private const string sys_hostCreated = "hostCreated";

	private const string sys_hostLeft = "hostLeft";

	private const string chatPrefix_log = "$log_";

	private const string chatPrefix_joinRequest = "join_";

	private JoinRequestStatus joinStatus;

	private const string chatPrefix_joinAccept = "acpt_";

	private const string chatPrefix_joinDeny = "deny_";

	private const string chatPrefix_joinDenyBanned = "dnyB_";

	private const string chatPrefix_joinDenyPassword = "dnyP_";

	public List<PlayerInfo> connectedPlayers = new List<PlayerInfo>();

	private Dictionary<SteamId, PlayerInfo> connectedPlayerDict = new Dictionary<SteamId, PlayerInfo>();

	private const string chatPrefix_infoList = "ilst_";

	private int playerListID = 1;

	private int receivedPlayerListID;

	private static Dictionary<SteamId, Texture2D> userImages = new Dictionary<SteamId, Texture2D>();

	private const string chatPrefix_transitionNewGame = "newG_";

	private List<SteamId> bannedMembers = new List<SteamId>();

	private List<SteamId> bannedFromLobbies = new List<SteamId>();

	public static bool hasInstance => _instance != null;

	public static VTOLMPLobbyManager instance
	{
		get
		{
			if (!_instance)
			{
				_instance = new GameObject("VTOLMPLobbyManager").AddComponent<VTOLMPLobbyManager>();
				UnityEngine.Object.DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}

	public static PlayerInfo localPlayerInfo
	{
		get
		{
			if (_localPlayerInfo == null)
			{
				_localPlayerInfo = new PlayerInfo();
			}
			return _localPlayerInfo;
		}
	}

	public SteamId currentLobbyHost => currentLobby.Owner.Id;

	public static bool isInLobby { get; private set; }

	public static bool isLobbyHost { get; private set; }

	public static bool remoteHostCreated { get; private set; }

	public static event Action OnLobbyDataChanged;

	public static event Action<PlayerInfo> OnPlayerLeft;

	public static event Action<string> OnLogMessage;

	public static event Action<PlayerInfo> OnNewPlayerJoined;

	public event Action OnConnectedPlayerListUpdated;

	public event VoteDelegate OnVoteBan;

	public event VoteDelegate OnVoteKick;

	public GameStates GetLobbyGameState()
	{
		return ConfigNodeUtils.ParseEnum<GameStates>(currentLobby.GetData("gState"));
	}

	public void SetCurrentMaxPlayers(int m)
	{
		currentMaxPlayers = m;
	}

	public static LobbyTask CreateLobby(string lobbyName, int maxPlayers, Privacy p, string pw)
	{
		LobbyTask lobbyTask = new LobbyTask();
		if (isInLobby)
		{
			lobbyTask.isDone = true;
			lobbyTask.isError = true;
			lobbyTask.errorMessage = "We are already in a lobby!";
		}
		else
		{
			instance.StartCoroutine(instance.CreateLobbyRoutine(lobbyTask, lobbyName, maxPlayers, p, pw));
		}
		return lobbyTask;
	}

	private IEnumerator CreateLobbyRoutine(LobbyTask task, string lobbyName, int maxPlayers, Privacy p, string pw)
	{
		Debug.Log($"VTOLMPLobbyManager: Creating a lobby for {maxPlayers} players");
		playerListID = 1;
		Task<Lobby?> s_task = SteamMatchmaking.CreateLobbyAsync(maxPlayers + ((p != Privacy.Private) ? 1 : 0));
		while (!s_task.IsCompleted)
		{
			yield return null;
		}
		if (s_task.IsFaulted || s_task.IsCanceled || !s_task.Result.HasValue)
		{
			task.isError = true;
			task.errorMessage = "Lobby creation failed!";
		}
		else
		{
			Debug.Log("VTOLMPLobbyManager: Successfully created lobby.");
			task.lobby = (currentLobby = s_task.Result.Value);
			currentLobby.SetData("lName", lobbyName);
			currentLobby.SetData("oName", PilotSaveManager.current.pilotName);
			currentLobby.SetData("oId", BDSteamClient.mySteamID.ToString());
			currentLobby.SetData("maxP", maxPlayers.ToString());
			currentLobby.SetData("feature", ((int)GameStartup.version.releaseType).ToString());
			currentLobby.SetData("ver", GameStartup.version.ToString());
			PlayerVehicle[] playerVehicles = VTResources.GetPlayerVehicles();
			foreach (PlayerVehicle playerVehicle in playerVehicles)
			{
				if (playerVehicle.dlc && playerVehicle.dlcLoaded)
				{
					currentLobby.SetData(string.Format("{0}{1}", "dlcVer_", playerVehicle.dlcID), playerVehicle.loadedDLCVersion.ToString());
				}
			}
			switch (p)
			{
			case Privacy.Public:
			case Privacy.Private:
				currentLobby.SetPublic();
				break;
			case Privacy.Friends_Only:
				currentLobby.SetFriendsOnly();
				break;
			}
			if (p == Privacy.Private)
			{
				if (UseSocketPW)
				{
					currentLobby.SetData("pwh", "1");
					VTNetworkManager.CreatePasswordHost(pw);
				}
				else
				{
					currentLobby.SetData("pwh", HashPassword(pw + BDSteamClient.mySteamID, 12345uL).ToString());
				}
				hostedLobbyPassword = pw;
			}
			else
			{
				currentLobby.SetData("pwh", "0");
				hostedLobbyPassword = null;
			}
			instance.SetupLobbyListeners();
			isInLobby = true;
			isLobbyHost = true;
			currentMaxPlayers = maxPlayers;
			CrashReportHandler.SetUserMetadata("MP Status", "host");
			connectedPlayers.Add(localPlayerInfo);
			connectedPlayerDict.Add(BDSteamClient.mySteamID, localPlayerInfo);
			receivedPlayerListID = 0;
		}
		task.isDone = true;
	}

	internal static ulong HashPassword(string pw, ulong seed = 12345uL)
	{
		ulong num = seed;
		for (int i = 0; i < pw.Length; i++)
		{
			ulong num2 = (ulong)(pw[i] + 1);
			num = (num + num2) * num2 % 9999999uL;
		}
		return num;
	}

	public bool TryPassword(string pw, Lobby l)
	{
		if (UseSocketPW)
		{
			return true;
		}
		if (ulong.TryParse(l.GetData("pwh"), out var result))
		{
			return HashPassword(pw + l.GetData("oId"), 12345uL) == result;
		}
		return false;
	}

	public static void LeaveLobby(bool checkMPScene = true)
	{
		if (!isInLobby)
		{
			return;
		}
		Debug.Log("LeaveLobby()");
		if (checkMPScene && (bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.DisconnectToMainMenu();
			return;
		}
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.ClosePasswordHost();
		}
		if (isLobbyHost)
		{
			currentLobby.SendChatString("$sys_hostLeft");
		}
		currentLobby.Leave();
		localPlayerInfo.chosenTeam = false;
		localPlayerInfo.selectedSlot = -1;
		isInLobby = false;
		isLobbyHost = false;
		if ((bool)instance)
		{
			instance.ClearLobbyCollections();
			instance.RemoveLobbyListeners();
			instance.joinStatus = JoinRequestStatus.None;
		}
		ClearUserImages();
		VTNetworkVoice.instance.EndVoiceChat();
	}

	private void OnDestroy()
	{
		LeaveLobby();
	}

	private void SetupLobbyListeners()
	{
		SteamMatchmaking.OnChatMessage += Lobby_OnChatMessage;
		SteamMatchmaking.OnLobbyMemberJoined += Lobby_OnLobbyMemberJoined;
		SteamMatchmaking.OnLobbyMemberLeave += Lobby_OnLobbyMemberLeave;
		SteamMatchmaking.OnLobbyDataChanged += Lobby_OnLobbyDataChanged;
		SteamMatchmaking.OnLobbyMemberDisconnected += SteamMatchmaking_OnLobbyMemberDisconnected;
		VTNetworkManager.OnDisconnected += VTNetworkManager_OnDisconnected;
		VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		if (isLobbyHost && bannedMembers.Contains(obj) && VTNetworkManager.instance.netState == VTNetworkManager.NetStates.IsHost)
		{
			VTNetworkManager.instance.socketHost.CloseConnectionTo(obj);
		}
	}

	private void SteamMatchmaking_OnLobbyMemberDisconnected(Lobby arg1, Friend arg2)
	{
		Debug.Log($"OnLobbyMemberDisconnected({arg2.Id.Value} {arg2.Name}");
	}

	private void VTNetworkManager_OnDisconnected(string reason)
	{
		remoteHostCreated = false;
		if ((bool)VTNetworkVoice.instance)
		{
			VTNetworkVoice.instance.EndVoiceChat();
		}
	}

	private void RemoveLobbyListeners()
	{
		SteamMatchmaking.OnChatMessage -= Lobby_OnChatMessage;
		SteamMatchmaking.OnLobbyMemberJoined -= Lobby_OnLobbyMemberJoined;
		SteamMatchmaking.OnLobbyMemberLeave -= Lobby_OnLobbyMemberLeave;
		SteamMatchmaking.OnLobbyDataChanged -= Lobby_OnLobbyDataChanged;
		SteamMatchmaking.OnLobbyMemberDisconnected -= SteamMatchmaking_OnLobbyMemberDisconnected;
		VTNetworkManager.OnDisconnected -= VTNetworkManager_OnDisconnected;
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void Lobby_OnLobbyDataChanged(Lobby obj)
	{
		if (isInLobby && (ulong)obj.Id == (ulong)currentLobby.Id)
		{
			VTOLMPLobbyManager.OnLobbyDataChanged?.Invoke();
		}
	}

	private void Lobby_OnLobbyMemberLeave(Lobby lobby, Friend friend)
	{
		Debug.Log("LobbyMemberLeave(" + friend.Name + ")");
		if (isInLobby && (ulong)lobby.Id == (ulong)currentLobby.Id)
		{
			if (connectedPlayerDict.TryGetValue(friend.Id, out var value))
			{
				if (value.selectedSlot >= 0)
				{
					if ((bool)VTOLMPSceneManager.instance)
					{
						VTOLMPSceneManager.instance.VacateSlot(value);
					}
					else
					{
						value.selectedSlot = -1;
					}
				}
				connectedPlayerDict.Remove(friend.Id);
				connectedPlayers.Remove(value);
				VTOLMPLobbyManager.OnPlayerLeft?.Invoke(value);
				if (isLobbyHost)
				{
					SendPlayerInfosToClients();
				}
				this.OnConnectedPlayerListUpdated?.Invoke();
			}
			if (ulong.TryParse(lobby.GetData("oId"), out var result) && result == friend.Id.Value)
			{
				VTMPMainMenu.AddQueuedError("The host has quit!");
				LeaveLobby();
			}
		}
		if (friend.IsMe && VTNetworkManager.instance.socketClient != null)
		{
			VTNetworkManager.StopClient();
		}
	}

	private void Lobby_OnLobbyMemberJoined(Lobby arg1, Friend friend)
	{
		Debug.Log($"{friend.Name} ({friend.Id.Value}) is joining...");
	}

	private void Lobby_OnChatMessage(Lobby lobby, Friend friend, string msg)
	{
		Debug.Log("Lobby message from " + friend.Name + ": " + msg);
		if (!hasSetupChatPrefixDict)
		{
			SetupChatPrefixDict();
		}
		if (msg.Length < 5)
		{
			Debug.LogError("A chat message was less than the minimum length! '" + msg + "'");
			return;
		}
		string text = msg.Substring(0, 5);
		string arg = msg.Substring(5, msg.Length - 5);
		if (chatDict.TryGetValue(text, out var value))
		{
			value(friend.Id, arg);
		}
		else
		{
			Debug.LogError("A chat message had an unhandled prefix! '" + text + "'");
		}
	}

	private void SetupChatPrefixDict()
	{
		Debug.Log("VTOLMPLobbyManager: Setting up chat prefix dict");
		chatDict = new Dictionary<string, Action<SteamId, string>>();
		chatDict.Add("$log_", ReceiveLogMessage);
		chatDict.Add("join_", ReceiveJoinMessage);
		chatDict.Add("acpt_", ReceiveJoinAcceptMessage);
		chatDict.Add("deny_", ReceiveJoinDenyMessage);
		chatDict.Add("dnyB_", ReceiveJoinDenyBannedMessage);
		chatDict.Add("dnyP_", ReceiveJoinDenyPWMessage);
		chatDict.Add("ilst_", ReceiveInfoListForClients);
		chatDict.Add("$sys_", ReceiveSystemMessage);
		chatDict.Add("newG_", ReceiveTransitionNewGameMsg);
		hasSetupChatPrefixDict = true;
	}

	public static void SendSocketHostCreatedMessage()
	{
		if (isLobbyHost)
		{
			Debug.Log("Sending 'host created' message to clients");
			currentLobby.SendChatString("$sys_hostCreated");
		}
	}

	public void ReceiveSystemMessage(SteamId sender, string msg)
	{
		if (msg == "hostCreated")
		{
			remoteHostCreated = true;
		}
		else
		{
			_ = msg == "hostLeft";
		}
	}

	private void ReceiveLogMessage(SteamId sender, string msg)
	{
		VTOLMPLobbyManager.OnLogMessage?.Invoke(msg);
	}

	public static void SendLogMessage(string msg)
	{
		if (isInLobby)
		{
			currentLobby.SendChatString("$log_" + msg);
		}
	}

	private void ReceiveJoinMessage(SteamId sender, string msg)
	{
		if (!isLobbyHost)
		{
			return;
		}
		Debug.Log("VTOLMPLobbyManager: Received a join message...");
		string[] array = msg.Split(',');
		ulong num = ulong.Parse(array[0]);
		string pilotName = array[1];
		if (!string.IsNullOrEmpty(hostedLobbyPassword))
		{
			if (UseSocketPW)
			{
				if (!VTNetworkManager.IsConnectingUserValidated(sender))
				{
					currentLobby.SendChatString(string.Format("{0}{1}", "dnyP_", num));
					Debug.Log("- Password mismatch!");
					return;
				}
				Debug.Log("- Password matched!");
			}
			else
			{
				if (array.Length <= 2)
				{
					currentLobby.SendChatString(string.Format("{0}{1}", "dnyP_", num));
					Debug.Log("- No password attached!");
					return;
				}
				if (!ulong.TryParse(array[2], out var result) || result != HashPassword(hostedLobbyPassword, currentLobby.Id))
				{
					currentLobby.SendChatString(string.Format("{0}{1}", "dnyP_", num));
					Debug.Log("- Password mismatch! (" + array[2] + ")");
					return;
				}
				Debug.Log("- Password matched! (" + array[2] + ")");
			}
		}
		if (bannedMembers.Contains(num))
		{
			currentLobby.SendChatString(string.Format("{0}{1}", "dnyB_", num));
			Debug.Log("- User was banned!");
		}
		else if (connectedPlayers.Count < currentMaxPlayers)
		{
			Debug.Log(" - We're accepting the join and reserving a seat!");
			currentLobby.SendChatString(string.Format("{0}{1}", "acpt_", num));
			PlayerInfo playerInfo = new PlayerInfo();
			playerInfo.pilotName = pilotName;
			playerInfo.steamUser = new Friend(num);
			connectedPlayers.Add(playerInfo);
			connectedPlayerDict.Add(num, playerInfo);
			VTOLMPLobbyManager.OnNewPlayerJoined?.Invoke(playerInfo);
			SendPlayerInfosToClients();
		}
		else
		{
			Debug.Log("We're denying the request.  The seats are full!");
			currentLobby.SendChatString(string.Format("{0}{1}", "deny_", num));
		}
	}

	private void ReceiveJoinAcceptMessage(SteamId sender, string msg)
	{
		ulong num = ulong.Parse(msg);
		if ((ulong)sender == (ulong)currentLobby.Owner.Id && num == BDSteamClient.mySteamID)
		{
			joinStatus = JoinRequestStatus.Accepted;
		}
	}

	private void ReceiveJoinDenyMessage(SteamId sender, string msg)
	{
		if (ulong.Parse(msg) == BDSteamClient.mySteamID)
		{
			joinStatus = JoinRequestStatus.Full;
		}
	}

	private void ReceiveJoinDenyBannedMessage(SteamId sender, string msg)
	{
		if (ulong.Parse(msg) == BDSteamClient.mySteamID)
		{
			joinStatus = JoinRequestStatus.Banned;
		}
	}

	private void ReceiveJoinDenyPWMessage(SteamId sender, string msg)
	{
		if (ulong.Parse(msg) == BDSteamClient.mySteamID)
		{
			joinStatus = JoinRequestStatus.WrongPassword;
		}
	}

	private void SendJoinRequestMessage(Lobby l, string pw)
	{
		Debug.Log("VTOLMPLobbyManager: sending join request message.");
		string message = ((!UseSocketPW && !string.IsNullOrEmpty(pw)) ? string.Format("{0}{1},{2},{3}", "join_", BDSteamClient.mySteamID, localPlayerInfo.pilotName, HashPassword(pw, l.Id)) : string.Format("{0}{1},{2}", "join_", BDSteamClient.mySteamID, localPlayerInfo.pilotName));
		l.SendChatString(message);
	}

	public static bool CheckHasRequiredDLCs(Lobby l)
	{
		string data = l.GetData("dlcReq");
		bool result = false;
		if (!string.IsNullOrEmpty(data))
		{
			foreach (string item in ConfigNodeUtils.ParseList(data))
			{
				if (uint.TryParse(item, out var result2) && VTResources.HasDLCInstalled(result2))
				{
					result = true;
				}
			}
			return result;
		}
		return true;
	}

	public static LobbyTask JoinLobby(Lobby l, string pw)
	{
		LobbyTask lobbyTask = new LobbyTask();
		if (isInLobby)
		{
			lobbyTask.isDone = true;
			lobbyTask.isError = true;
			lobbyTask.errorMessage = "You are already in a lobby!";
		}
		else
		{
			string data = l.GetData("ver");
			if (string.IsNullOrEmpty(data) || data != GameStartup.version.ToString())
			{
				lobbyTask.isDone = true;
				lobbyTask.isError = true;
				lobbyTask.errorMessage = "Invalid game version! (" + data + ")";
				return lobbyTask;
			}
			PlayerVehicle[] playerVehicles = VTResources.GetPlayerVehicles();
			foreach (PlayerVehicle playerVehicle in playerVehicles)
			{
				if (playerVehicle.dlc && playerVehicle.dlcLoaded)
				{
					string data2 = l.GetData(string.Format("{0}{1}", "dlcVer_", playerVehicle.dlcID));
					if (!string.IsNullOrEmpty(data2) && GameVersion.Parse(data2) != playerVehicle.loadedDLCVersion)
					{
						lobbyTask.isDone = true;
						lobbyTask.isError = true;
						lobbyTask.errorMessage = "Invalid DLC version!\n(" + playerVehicle.vehicleName + " v" + data2 + " required)";
						return lobbyTask;
					}
				}
			}
			if (!CheckHasRequiredDLCs(l))
			{
				lobbyTask.isDone = true;
				lobbyTask.isError = true;
				lobbyTask.errorMessage = "Mission requires DLCs which are not installed.";
				return lobbyTask;
			}
			instance.StartCoroutine(instance.JoinLobbyRoutine(l, lobbyTask, pw));
		}
		return lobbyTask;
	}

	private IEnumerator JoinLobbyRoutine(Lobby l, LobbyTask task, string pw)
	{
		Debug.Log("VTOLMPLobbyManager: Joining a lobby");
		receivedPlayerListID = 0;
		Task<RoomEnter> jTask = l.Join();
		while (!jTask.IsCompleted)
		{
			yield return null;
		}
		if (jTask.IsFaulted || jTask.IsCanceled)
		{
			task.SetError("Join failed!");
			yield break;
		}
		RoomEnter result = jTask.Result;
		if (result == RoomEnter.Success)
		{
			isInLobby = true;
			currentLobby = l;
			isLobbyHost = false;
			SetupLobbyListeners();
			if (UseSocketPW && !string.IsNullOrEmpty(pw))
			{
				ulong num = ulong.Parse(l.GetData("oId"));
				VTPasswordAttempt pwAttempt = VTNetworkManager.TrySocketPassword(num, pw);
				float pwT = Time.realtimeSinceStartup;
				while (pwAttempt.status == VTPasswordAttempt.Statuses.Pending)
				{
					if (Time.realtimeSinceStartup - pwT > 10f)
					{
						task.SetError("Timeout when attempting to join! (PW)");
						LeaveLobby();
						yield break;
					}
					yield return null;
				}
				if (pwAttempt.status == VTPasswordAttempt.Statuses.WrongPassword)
				{
					task.SetError("Join failed! Incorrect password!");
					LeaveLobby();
					yield break;
				}
				if (pwAttempt.status == VTPasswordAttempt.Statuses.NoResponse)
				{
					task.SetError("Join failed! Too many attempts or host not responding.");
					LeaveLobby();
					yield break;
				}
			}
			joinStatus = JoinRequestStatus.None;
			SendJoinRequestMessage(l, pw);
			float t = Time.time;
			while (joinStatus == JoinRequestStatus.None)
			{
				if (Time.time - t > 10f)
				{
					task.SetError("Join failed!  Timed out.");
					LeaveLobby();
					yield break;
				}
				yield return null;
			}
			if (joinStatus == JoinRequestStatus.Accepted)
			{
				task.isDone = true;
				task.lobby = l;
				currentLobby = l;
				CrashReportHandler.SetUserMetadata("MP Status", "client");
				Debug.Log("VTOLMPLobbyManager: Join request accepted!");
				if (connectedPlayerDict.ContainsKey(BDSteamClient.mySteamID))
				{
					Debug.Log("JoinLobbyRoutine: local player is already in connectedPlayersDict");
					yield break;
				}
				Debug.Log("Added local player info to dict in JoinLobbyRoutine");
				connectedPlayers.Add(localPlayerInfo);
				connectedPlayerDict.Add(BDSteamClient.mySteamID, localPlayerInfo);
			}
			else
			{
				if (joinStatus == JoinRequestStatus.Full)
				{
					task.SetError("Join failed! The game is full!");
				}
				else if (joinStatus == JoinRequestStatus.Banned)
				{
					task.SetError("Join failed! You are banned from this lobby!");
				}
				else if (joinStatus == JoinRequestStatus.WrongPassword)
				{
					task.SetError("Join failed! Incorrect password!");
				}
				LeaveLobby();
			}
		}
		else
		{
			task.SetError($"Join failed! {result}");
		}
	}

	public void SendPlayerInfosToClients(bool clearTeams = false)
	{
		if (!isLobbyHost)
		{
			return;
		}
		playerListID++;
		Debug.Log("VTOLMPLobbyManager: Sending player infos to clients.");
		List<string> list = new List<string>();
		list.Add(playerListID.ToString());
		if (clearTeams)
		{
			list.Add("refresh");
		}
		foreach (PlayerInfo connectedPlayer in connectedPlayers)
		{
			list.Add(connectedPlayer.ToString());
		}
		string text = ConfigNodeUtils.WriteList(list);
		currentLobby.SendChatString("ilst_" + text);
	}

	private void ReceiveInfoListForClients(SteamId sender, string msg)
	{
		if (isLobbyHost)
		{
			return;
		}
		bool flag = false;
		Debug.Log("VTOLMPLobbyManager: Received player info list from host.");
		List<string> list = ConfigNodeUtils.ParseList(msg);
		if (int.TryParse(list[0], out var result))
		{
			if (receivedPlayerListID > result)
			{
				Debug.Log($"Received an old player info list (id ={result}, existing id = {receivedPlayerListID}");
				return;
			}
			receivedPlayerListID = result;
			for (int i = 1; i < list.Count; i++)
			{
				string text = list[i];
				if (text == "refresh")
				{
					foreach (KeyValuePair<SteamId, PlayerInfo> item in connectedPlayerDict)
					{
						item.Value.chosenTeam = false;
					}
					continue;
				}
				ulong num = ulong.Parse(text.Substring(0, text.IndexOf(',')));
				if (!connectedPlayerDict.TryGetValue(num, out var value))
				{
					value = ((num != BDSteamClient.mySteamID) ? new PlayerInfo() : localPlayerInfo);
					connectedPlayers.Add(value);
					connectedPlayerDict.Add(num, value);
					flag = true;
				}
				bool chosenTeam = value.chosenTeam;
				Teams team = value.team;
				value.UpdateFromString(text);
				if (chosenTeam)
				{
					value.chosenTeam = true;
					value.team = team;
				}
				else if (!chosenTeam && value.chosenTeam && (bool)VTOLMPSceneManager.instance)
				{
					VTOLMPSceneManager.instance.ReportPlayerSelectedTeam(value);
				}
				Debug.Log(" - Info: " + value.ToString());
			}
			if (flag)
			{
				this.OnConnectedPlayerListUpdated?.Invoke();
			}
		}
		else
		{
			Debug.LogError("Received a player info list with an invalid ID");
		}
	}

	public static PlayerInfo GetPlayer(ulong steamId)
	{
		if (instance.connectedPlayerDict.TryGetValue(steamId, out var value))
		{
			return value;
		}
		Debug.Log($"PlayerInfo not found for steamId {steamId} ({new Friend(steamId).Name})");
		return null;
	}

	private void ClearLobbyCollections()
	{
		connectedPlayers.Clear();
		connectedPlayerDict.Clear();
		remoteHostCreated = false;
		bannedMembers.Clear();
	}

	public static string GenerateScenarioID(VTScenarioInfo info)
	{
		string text = (info.isWorkshop ? "w" : "b");
		return text + "," + info.id + "," + info.campaignID;
	}

	public static string GenerateRequiredDLCsList(VTScenarioInfo info)
	{
		bool flag = false;
		List<string> list = new List<string>();
		foreach (ConfigNode node2 in info.config.GetNode("UNITS").GetNodes("UnitSpawner"))
		{
			node2.GetValue("unitID");
			ConfigNode node = node2.GetNode("UnitFields");
			if (node != null && node.HasValue("vehicle"))
			{
				PlayerVehicle playerVehicle = VTResources.GetPlayerVehicle(MultiplayerSpawn.GetVehicleName(node.GetValue<MultiplayerSpawn.Vehicles>("vehicle")));
				if (playerVehicle.dlc)
				{
					list.Add(playerVehicle.dlcID.ToString());
				}
				else
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			return string.Empty;
		}
		return ConfigNodeUtils.WriteList(list);
	}

	public static MPScenarioRequest GetScenario(Lobby l)
	{
		MPScenarioRequest mPScenarioRequest = new MPScenarioRequest();
		mPScenarioRequest.status = MPScenarioRequestStatus.Waiting;
		string data = l.GetData("scID");
		instance.StartCoroutine(instance.GetScenarioRoutine(mPScenarioRequest, data, l));
		return mPScenarioRequest;
	}

	private IEnumerator GetScenarioRoutine(MPScenarioRequest req, string scenarioID, Lobby l)
	{
		Debug.Log("VTOLMPLobbyManager: getting scenario (" + scenarioID + ")");
		string[] split = scenarioID.Split(',');
		if (split[0] == "b")
		{
			Debug.Log("VTOLMPLobbyManager: It's a built-in scenario");
			string campaignID = split[2];
			VTScenarioInfo builtInScenario = VTResources.GetBuiltInScenario(split[1], campaignID);
			if (builtInScenario != null)
			{
				req.scenario = builtInScenario;
				req.status = MPScenarioRequestStatus.Success;
			}
			else
			{
				req.status = MPScenarioRequestStatus.FileNotFound;
			}
			yield break;
		}
		Debug.Log("VTOLMPLobbyManager: The scenario is workshop content.");
		ulong swId;
		Task<Item?> task;
		if (split.Length < 3 || string.IsNullOrEmpty(split[2]))
		{
			Debug.Log(" - It's a single scenario");
			swId = ulong.Parse(split[1]);
			task = SteamUGC.QueryFileAsync(swId);
			while (!task.IsCompleted)
			{
				yield return null;
			}
			if (task.Result.HasValue)
			{
				Item item = task.Result.Value;
				req.workshopId = swId;
				if (item.IsSubscribed)
				{
					if (!item.IsInstalled)
					{
						Debug.Log("VTOLMPLobbyManager: The item is still being installed.");
						while (!item.IsInstalled)
						{
							req.status = MPScenarioRequestStatus.Installing;
							req.downloadProgress = item.DownloadAmount;
							yield return null;
						}
					}
					if (item.IsInstalled)
					{
						Debug.Log("VTOLMPLobbyManager: Workshop scenario installed!");
						VTResources.LoadWorkshopSingleScenario(item);
						VTScenarioInfo steamWorkshopStandaloneScenario = VTResources.GetSteamWorkshopStandaloneScenario(swId.ToString());
						if (steamWorkshopStandaloneScenario != null)
						{
							req.scenario = steamWorkshopStandaloneScenario;
							req.status = MPScenarioRequestStatus.Success;
						}
						else
						{
							Debug.Log("VTOLMPLobbyManager: The workshop content could not be loaded properly...");
							req.status = MPScenarioRequestStatus.FileNotFound;
						}
					}
				}
				else
				{
					Debug.Log("VTOLMPLobbyManager: The workshop content has not been subscribed to yet.");
					req.status = MPScenarioRequestStatus.NeedSubscription;
				}
			}
			else
			{
				Debug.Log("VTOLMPLobbyManager: The workshop item query task did not return a result.");
				req.status = MPScenarioRequestStatus.WorkshopRequestFailed;
			}
			yield break;
		}
		Debug.Log(" - it's part of a campaign");
		swId = ulong.Parse(split[2]);
		task = SteamUGC.QueryFileAsync(swId);
		while (!task.IsCompleted)
		{
			yield return null;
		}
		if (task.Result.HasValue)
		{
			Item item = task.Result.Value;
			req.workshopId = swId;
			if (item.IsSubscribed)
			{
				if (!item.IsInstalled)
				{
					Debug.Log("VTOLMPLobbyManager: The item is still being installed.");
					while (!item.IsInstalled)
					{
						req.status = MPScenarioRequestStatus.Installing;
						req.downloadProgress = item.DownloadAmount;
						yield return null;
					}
				}
				if (!item.IsInstalled)
				{
					yield break;
				}
				Debug.Log("VTOLMPLobbyManager: Workshop campaign installed!");
				VTResources.LoadWorkshopCampaign(item);
				VTCampaignInfo campaign = VTResources.GetSteamWorkshopCampaign(swId.ToString());
				if (campaign != null)
				{
					int ourV = 0;
					if (campaign.config.HasValue("wsUploadVersion"))
					{
						ourV = campaign.config.GetValue<int>("wsUploadVersion");
					}
					int hostV = 0;
					string data = l.GetData("wsUploadVersion");
					if (!string.IsNullOrEmpty(data))
					{
						int.TryParse(data, out hostV);
					}
					while (ourV != hostV)
					{
						if (ourV < hostV)
						{
							Debug.Log("VTOLMPLobbyManager: Host has a newer version.  Downloading update.");
							if (!item.Download(highPriority: true))
							{
								Debug.LogError("VTOLMPLobbyManager: Host has a newer version, but download failed.");
								req.status = MPScenarioRequestStatus.WorkshopRequestFailed;
								yield break;
							}
							while (item.IsDownloading || item.IsDownloadPending)
							{
								req.status = MPScenarioRequestStatus.Installing;
								req.downloadProgress = item.DownloadAmount;
								yield return null;
							}
							campaign = VTResources.LoadWorkshopCampaign(item);
							if (campaign.config.HasValue("wsUploadVersion"))
							{
								ourV = campaign.config.GetValue<int>("wsUploadVersion");
							}
						}
						else if (hostV < ourV)
						{
							Debug.Log("VTOLMPLobbyManager: Host has an old version.  We can't join.");
							req.status = MPScenarioRequestStatus.HostHasOldVersion;
							yield break;
						}
						yield return null;
					}
					req.scenario = campaign.GetScenario(split[1]);
					req.status = MPScenarioRequestStatus.Success;
				}
				else
				{
					Debug.Log("VTOLMPLobbyManager: The workshop content could not be loaded properly...");
					req.status = MPScenarioRequestStatus.FileNotFound;
				}
			}
			else
			{
				Debug.Log("VTOLMPLobbyManager: The workshop content has not been subscribed to yet.");
				req.status = MPScenarioRequestStatus.NeedSubscription;
			}
		}
		else
		{
			Debug.Log("VTOLMPLobbyManager: The workshop item query task did not return a result.");
			req.status = MPScenarioRequestStatus.WorkshopRequestFailed;
		}
	}

	public static async Task<Texture2D> GetUserImage(SteamId id)
	{
		Image? image = await new Friend(id).GetLargeAvatarAsync();
		if (userImages.TryGetValue(id, out var value))
		{
			return value;
		}
		if (image.HasValue)
		{
			Image value2 = image.Value;
			Debug.Log($"Got user avatar. width={value2.Width}, height={value2.Height}");
			Texture2D texture2D = new Texture2D((int)value2.Width, (int)value2.Height, TextureFormat.RGBA32, mipChain: false);
			for (int i = 0; i < value2.Width; i++)
			{
				for (int j = 0; j < value2.Height; j++)
				{
					Steamworks.Data.Color pixel = value2.GetPixel(i, j);
					texture2D.SetPixel(color: new Color32(pixel.r, pixel.g, pixel.b, byte.MaxValue), x: i, y: (int)(value2.Height - j - 1));
				}
			}
			texture2D.Apply();
			userImages.Add(id, texture2D);
			return texture2D;
		}
		return null;
	}

	public static void GetUserImageForCallback(SteamId id, Action<SteamId, Texture2D> onLoaded = null)
	{
		instance.StartCoroutine(instance.GetUserImageCallbackRoutine(id, onLoaded));
	}

	private IEnumerator GetUserImageCallbackRoutine(SteamId id, Action<SteamId, Texture2D> onLoaded = null)
	{
		Task<Texture2D> task = GetUserImage(id);
		while (!task.IsCompleted)
		{
			yield return null;
		}
		if ((bool)task.Result)
		{
			onLoaded?.Invoke(id, task.Result);
		}
	}

	private static void ClearUserImages()
	{
		SteamId steamId = 0uL;
		try
		{
			steamId = SteamClient.SteamId;
		}
		catch (Exception)
		{
			return;
		}
		List<SteamId> list = new List<SteamId>();
		foreach (KeyValuePair<SteamId, Texture2D> userImage in userImages)
		{
			if ((ulong)userImage.Key != (ulong)steamId)
			{
				UnityEngine.Object.DestroyImmediate(userImage.Value);
				list.Add(userImage.Key);
			}
		}
		foreach (SteamId item in list)
		{
			userImages.Remove(item);
		}
	}

	public void Host_SendTransitionToNewGameMsg(VTScenarioInfo newScenario)
	{
		if (isLobbyHost)
		{
			string text = GenerateScenarioID(newScenario);
			currentLobby.SendChatString("newG_" + text);
		}
	}

	private void ReceiveTransitionNewGameMsg(SteamId sender, string scenarioId)
	{
		if ((bool)VTOLMPSceneManager.instance && !isLobbyHost)
		{
			VTOLMPSceneManager.instance.DontExitOnDisconnect();
			StartCoroutine(Client_TransitionNewGameRoutine(scenarioId));
		}
	}

	private IEnumerator Client_TransitionNewGameRoutine(string scenarioId)
	{
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut();
		yield return new WaitForSeconds(1.2f);
		PilotSaveManager.currentCampaign = null;
		PilotSaveManager.currentScenario = null;
		PilotSaveManager.currentVehicle = null;
		VTScenario.current = null;
		LoadingSceneController.LoadSceneImmediate("ReadyRoom");
		if (VTNetworkManager.instance.connectionState != 0)
		{
			VTNetworkManager.StopClient();
		}
		remoteHostCreated = false;
		ControllerEventHandler.UnpauseEvents();
		localPlayerInfo.chosenTeam = false;
		localPlayerInfo.selectedSlot = -1;
		localPlayerInfo.isReady = false;
		connectedPlayers.Clear();
		connectedPlayerDict.Clear();
		EndMission.Stop();
		Debug.Log(" - Transitioning to new mission: " + scenarioId);
		yield return null;
		Debug.Log(" - Waiting for VTMPMainMenu instance");
		while (!VTMPMainMenu.instance)
		{
			yield return null;
		}
		Debug.Log(" - VTMPMainMenu found.  Transitioning to new mission.");
		yield return null;
		VTMPMainMenu.instance.TransitionToNewMission();
	}

	public void ReceiveKick()
	{
		LeaveLobby();
		VTMPMainMenu.AddQueuedError("You were kicked!");
	}

	public void SetMemberBanned(SteamId id)
	{
		if (isLobbyHost && !bannedMembers.Contains(id))
		{
			bannedMembers.Add(id);
		}
	}

	public void ReceiveBan()
	{
		if (isInLobby)
		{
			bannedFromLobbies.Add(currentLobby.Id);
		}
		LeaveLobby();
		VTMPMainMenu.AddQueuedError("You were banned!");
	}

	public void ReceiveVoteKick(ulong voterId, ulong targetId)
	{
		if (!isLobbyHost)
		{
			return;
		}
		PlayerInfo player = GetPlayer(targetId);
		if (player != null)
		{
			if (isLobbyHost)
			{
				player.voteKicks++;
				SendPlayerInfosToClients();
				player.ReportKickBanUpdated();
			}
			PlayerInfo player2 = GetPlayer(voterId);
			this.OnVoteKick?.Invoke(player, player2);
		}
	}

	public void ReceiveVoteBan(ulong voterId, ulong targetId)
	{
		if (!isLobbyHost)
		{
			return;
		}
		PlayerInfo player = GetPlayer(targetId);
		if (player != null)
		{
			if (isLobbyHost)
			{
				player.voteBans++;
				SendPlayerInfosToClients();
				player.ReportKickBanUpdated();
			}
			PlayerInfo player2 = GetPlayer(voterId);
			this.OnVoteBan?.Invoke(player, player2);
		}
	}

	public void ClientNotifyVote(bool ban, ulong target, ulong voter)
	{
		PlayerInfo player = GetPlayer(target);
		PlayerInfo player2 = GetPlayer(voter);
		if (player != null && player2 != null)
		{
			if (ban)
			{
				this.OnVoteBan?.Invoke(player, player2);
			}
			else
			{
				this.OnVoteKick?.Invoke(player, player2);
			}
		}
	}

	public void TestHash()
	{
	}
}

}