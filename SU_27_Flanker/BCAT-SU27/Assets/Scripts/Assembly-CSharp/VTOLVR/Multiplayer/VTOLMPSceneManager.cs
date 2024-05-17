using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Steamworks;
using UnityEngine;
using Valve.VR;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTOLMPSceneManager : VTNetSyncRPCOnly
{
	public class VehicleSlot
	{
		public string slotTitle;

		public string vehicleName;

		public PlayerInfo player;

		public Teams team;

		public int idx;

		public int spawnID;

		public int seatIdx;

		public Actor.Designation designation;
	}

	public class TeamRequest
	{
		public bool isReady;

		public bool accepted;
	}

	private class BriefingSeatRequest
	{
		public bool isReady;

		public int seatIdx;
	}

	public delegate void BriefingSeatDelegate(ulong id, Teams team, int seatIdx);

	public class PlayerStats
	{
		public PlayerInfo player;

		public int kills;

		public int assists;

		public int deaths;

		public int teamKills;
	}

	public static bool unitIcons = true;

	public ScriptableGameObjectList briefingRoomPrefabs;

	private GameObject localAvatarObj;

	public List<VehicleSlot> alliedSlots;

	public List<VehicleSlot> enemySlots;

	private PlayerInfo[] alliedPlayers;

	private PlayerInfo[] enemyPlayers;

	private PlayerInfo[] alliedBriefingSeats;

	private PlayerInfo[] enemyBriefingSeats;

	private Dictionary<ulong, int> briefingSeatAssignments = new Dictionary<ulong, int>();

	private Dictionary<PlayerInfo, PlayerStats> playerStats = new Dictionary<PlayerInfo, PlayerStats>();

	public const float kickBanVoteCooldown = 60f;

	private string chatPrefix_voteKick = "votK_";

	private string chatPrefix_voteBan = "votB_";

	public static VTOLMPSceneManager instance { get; private set; }

	public PlayerInfo localPlayer => VTOLMPLobbyManager.localPlayerInfo;

	public int altSpawnSeed { get; private set; }

	public float timeKickBanVoted { get; private set; }

	public float kickbanCooldownLeft => 60f - (Time.time - timeKickBanVoted);

	public event Action OnWillReturnToBriefingRoom;

	public event Action OnLocalBriefingAvatarSpawned;

	public event Action<PlayerInfo> OnPlayerSpawnedInVehicle;

	public event Action<PlayerInfo> OnPlayerUnspawnedVehicle;

	public event Action<VehicleSlot> OnSlotUpdated;

	public event Action<PlayerInfo> OnPlayerSelectedTeam;

	public event Action<ulong, bool> OnDeclaredReady;

	public event Action OnMPScenarioStart;

	public event Action OnEnterVehicle;

	public event BriefingSeatDelegate OnBriefingSeatUpdated;

	public event Action<PlayerStats> OnStatsUpdated;

	protected override void Awake()
	{
		base.Awake();
		instance = this;
		altSpawnSeed = -1;
		if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Scenario)
		{
			StartCoroutine(MPStartupRoutine());
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public void BeginVoiceChat()
	{
		VTNetworkVoice.instance.BeginVoiceChat(VTOLMPLobbyManager.currentLobby);
	}

	private IEnumerator MPStartupRoutine()
	{
		while (VTScenario.current == null)
		{
			yield return null;
		}
		if (VTScenario.current.multiplayer)
		{
			SetupVehicleSlots();
			SetupBriefingSeats();
			if (!GameSettings.VR_SDK_IS_OCULUS)
			{
				SteamVR.settings.pauseGameWhenDashboardVisible = false;
			}
			int result = 0;
			int.TryParse(VTOLMPLobbyManager.currentLobby.GetData("brtype"), out result);
			UnityEngine.Object.Instantiate(briefingRoomPrefabs.list[result]);
			if (VTOLMPLobbyManager.isLobbyHost)
			{
				Debug.Log("Setting up host");
				VTNetworkManager.CreateHost();
				while (VTNetworkManager.instance.connectionState != VTNetworkManager.ConnectionStates.Connected)
				{
					yield return null;
				}
				VTNetworkManager.OnDisconnected += OnSocketHostFailed;
				VTOLMPLobbyManager.SendSocketHostCreatedMessage();
				VTOLMPLobbyManager.OnNewPlayerJoined += Lobby_OnNewPlayerJoined;
				VTNetworkManager.instance.OnNewClientConnected += VTNetworkManager_OnClientConnected;
				VTOLMPLobbyManager.OnPlayerLeft += Lobby_OnPlayerLeft;
				while (!VTNetSceneManager.instance.sceneEntitiesReady)
				{
					yield return null;
				}
				SendRPCBuffered("RPC_SetUnitIcons", unitIcons ? 1 : 0);
				altSpawnSeed = UnityEngine.Random.Range(13, 9999999);
				Debug.Log($"VTOLMPSceneManager: altSpawnSeed set to {altSpawnSeed}");
				SendRPCBuffered("RPC_SetAltSpawnSeed", altSpawnSeed);
				VTOLMPLobbyManager.instance.OnVoteKick += Instance_OnVoteKick;
				VTOLMPLobbyManager.instance.OnVoteBan += Instance_OnVoteBan;
			}
			else
			{
				yield return ConnectToHostRoutine();
			}
			VTOLMPBriefingManager.instance.OnSetBriefingController += Instance_OnSetBriefingController;
			VTOLMPLobbyManager.SendLogMessage(VTOLMPLobbyManager.localPlayerInfo.pilotName + " has connected.");
		}
		else
		{
			if (!GameSettings.VR_SDK_IS_OCULUS && (bool)SteamVR.settings)
			{
				SteamVR.settings.pauseGameWhenDashboardVisible = true;
			}
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void Instance_OnVoteKick(PlayerInfo target, PlayerInfo voter)
	{
		SendRPC("RPC_NotifVoteKick", target.steamUser.Id.Value, voter.steamUser.Id.Value);
	}

	[VTRPC]
	private void RPC_NotifVoteKick(ulong target, ulong voter)
	{
		VTOLMPLobbyManager.instance.ClientNotifyVote(ban: false, target, voter);
	}

	private void Instance_OnVoteBan(PlayerInfo target, PlayerInfo voter)
	{
		SendRPC("RPC_NotifVoteBan", target.steamUser.Id.Value, voter.steamUser.Id.Value);
	}

	[VTRPC]
	private void RPC_NotifVoteBan(ulong target, ulong voter)
	{
		VTOLMPLobbyManager.instance.ClientNotifyVote(ban: true, target, voter);
	}

	private void Instance_OnSetBriefingController(ulong controllerSteamID, Teams team)
	{
		foreach (KeyValuePair<ulong, int> briefingSeatAssignment in briefingSeatAssignments)
		{
			PlayerInfo player = VTOLMPLobbyManager.GetPlayer(briefingSeatAssignment.Key);
			if (player != null && !VTOLMPBriefingManager.instance.IsBriefingController(briefingSeatAssignment.Key))
			{
				this.OnBriefingSeatUpdated?.Invoke(briefingSeatAssignment.Key, player.team, briefingSeatAssignment.Value);
			}
		}
		if (controllerSteamID != 0L && VTOLMPLobbyManager.GetPlayer(controllerSteamID) != null)
		{
			SetBriefingControllerSeat(controllerSteamID);
		}
	}

	[VTRPC]
	private void RPC_SetAltSpawnSeed(int s)
	{
		Debug.Log($"RPC_SetAltSpawnSeed({s})");
		altSpawnSeed = s;
	}

	private void Lobby_OnPlayerLeft(PlayerInfo info)
	{
		Debug.Log("Player left the lobby: " + info.pilotName);
		PlayerInfo[] array = ((info.team == Teams.Allied) ? alliedPlayers : enemyPlayers);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == info)
			{
				array[i] = null;
			}
		}
		PlayerInfo[] array2 = ((info.team == Teams.Allied) ? alliedBriefingSeats : enemyBriefingSeats);
		for (int j = 0; j < array2.Length; j++)
		{
			if (array2[j] == info)
			{
				array2[j] = null;
			}
		}
		briefingSeatAssignments.Remove(info.steamUser.Id);
		List<VehicleSlot> list = ((info.team == Teams.Allied) ? alliedSlots : enemySlots);
		for (int k = 0; k < list.Count; k++)
		{
			if (list[k].player == info)
			{
				list[k].player = null;
				ReportSlotChanged(list[k]);
			}
		}
		VTOLMPLobbyManager.SendLogMessage(info.pilotName + " has disconnected.");
		if (base.isMine)
		{
			VTNetworkManager.instance.socketHost.CloseConnectionTo(info.steamUser.Id);
		}
	}

	[VTRPC]
	private void RPC_SetUnitIcons(int _unitIcons)
	{
		Debug.Log($"RPC_SetUnitIcons({_unitIcons})");
		unitIcons = _unitIcons > 0;
	}

	private void VTNetworkManager_OnClientConnected(SteamId obj)
	{
		Friend friend = new Friend(obj);
		bool flag = false;
		foreach (Friend member in VTOLMPLobbyManager.currentLobby.Members)
		{
			if ((ulong)member.Id == (ulong)friend.Id)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Debug.LogError($"A user [{obj.AccountId}] connected via VTNetworking but they are not in our lobby!  Closing the connection.");
			VTNetworkManager.instance.socketHost.CloseConnectionTo(obj);
			return;
		}
		VTOLMPLobbyManager.instance.SendPlayerInfosToClients();
		ResendSlotsForNewPlayer(obj);
		SendAllStatsToPlayer(obj);
		foreach (PlayerInfo connectedPlayer in VTOLMPLobbyManager.instance.connectedPlayers)
		{
			if (connectedPlayer != null && connectedPlayer.vehicleEntityID >= 0)
			{
				SendDirectedRPC(obj.Value, "RPC_SetPlayerVehicleEntity", connectedPlayer.steamUser.Id, connectedPlayer.vehicleEntityID, connectedPlayer.selectedSlot, (int)connectedPlayer.team);
			}
		}
	}

	public void ReturnToBriefingRoom()
	{
		Debug.Log("Returning to briefing room.");
		GameObject vehicleObjectToDestroy = null;
		if (instance.GetSlot(localPlayer).seatIdx == 0)
		{
			vehicleObjectToDestroy = localPlayer.vehicleObject;
		}
		GameObject ava = localPlayer.multicrewAvatar;
		SpawnBriefingAvatar(delegate
		{
			if ((bool)vehicleObjectToDestroy)
			{
				VTNetworkManager.NetDestroyObject(vehicleObjectToDestroy);
			}
			if ((bool)ava)
			{
				VTNetworkManager.NetDestroyObject(ava);
			}
			SetMyVehicleEntityID(null);
		});
		this.OnWillReturnToBriefingRoom?.Invoke();
	}

	public void SpawnBriefingAvatar(Action onAvatarSpawned = null)
	{
		StartCoroutine(SpawnAvatarRoutine(onAvatarSpawned));
	}

	private IEnumerator SpawnAvatarRoutine(Action onAvatarSpawned)
	{
		if ((bool)localAvatarObj)
		{
			Debug.LogError("Tried to SpawnAvatarRoutine but localAvatarObj already exists!");
			yield break;
		}
		Teams team = localPlayer.team;
		int seatIdx = -1;
		while (seatIdx == -1)
		{
			if (VTOLMPLobbyManager.isLobbyHost)
			{
				seatIdx = Host_RequestBriefingSeat(BDSteamClient.mySteamID, (int)team);
			}
			else
			{
				RPCRequest seatReq = SendRPCRequest(typeof(int), VTOLMPLobbyManager.currentLobby.Owner.Id, "RPC_RequestBriefingSeat", BDSteamClient.mySteamID, (int)team);
				while (!seatReq.isComplete)
				{
					yield return null;
				}
				seatIdx = (int)seatReq.Value;
			}
			yield return new WaitForSeconds(0.5f);
		}
		BriefingSpawnPoint briefingSpawnPoint = ((team == Teams.Allied) ? VTOLMPBriefingRoom.instance.alliedSpawnTransforms : VTOLMPBriefingRoom.instance.enemySpawnTransforms)[seatIdx];
		VTNetworkManager.NetInstantiateRequest avatarReq = VTNetworkManager.NetInstantiate("Multiplayer/BriefingAvatar", briefingSpawnPoint.transform.position, briefingSpawnPoint.transform.rotation);
		Transform obj = VTOLMPBriefingRoom.instance.ui.transform;
		obj.SetParent(briefingSpawnPoint.uiTransform);
		obj.localPosition = Vector3.zero;
		obj.localScale = Vector3.one;
		obj.localRotation = Quaternion.identity;
		while (!avatarReq.isReady)
		{
			yield return null;
		}
		localAvatarObj = avatarReq.obj;
		onAvatarSpawned?.Invoke();
		this.OnLocalBriefingAvatarSpawned?.Invoke();
		VTOLMPBriefingRoom.instance.ui.slotsMenuDisplayObj.SetActive(MissionManager.instance.finalWinner == MissionManager.FinalWinner.None);
		VTOLMPBriefingRoom.instance.ui.voiceToggleObj.SetActive(value: true);
		AudioController.instance.ClearAllOpenings();
		AudioController.instance.MP_SetNearVoiceAtten(1f);
		ScreenFader.FadeIn();
	}

	public void ReportPlayerSpawnedInVehicle(PlayerInfo player)
	{
		this.OnPlayerSpawnedInVehicle?.Invoke(player);
	}

	public void ReportPlayerUnspawnedVehicle(PlayerInfo player)
	{
		this.OnPlayerUnspawnedVehicle?.Invoke(player);
	}

	public void RequestNewBriefingSeat()
	{
		if ((bool)localAvatarObj)
		{
			StartCoroutine(SwitchSeatRoutine());
		}
	}

	private IEnumerator SwitchSeatRoutine()
	{
		Teams team = localPlayer.team;
		int seatIdx = -1;
		while (seatIdx == -1)
		{
			if (VTOLMPLobbyManager.isLobbyHost)
			{
				seatIdx = Host_RequestBriefingSeat(BDSteamClient.mySteamID, (int)team);
			}
			else
			{
				RPCRequest seatReq = SendRPCRequest(typeof(int), VTOLMPLobbyManager.currentLobby.Owner.Id, "RPC_RequestBriefingSeat", BDSteamClient.mySteamID, (int)team);
				while (!seatReq.isComplete)
				{
					yield return null;
				}
				seatIdx = (int)seatReq.Value;
			}
			yield return new WaitForSeconds(0.5f);
		}
		BriefingSpawnPoint briefingSpawnPoint = ((team == Teams.Allied) ? VTOLMPBriefingRoom.instance.alliedSpawnTransforms : VTOLMPBriefingRoom.instance.enemySpawnTransforms)[seatIdx];
		if ((bool)localAvatarObj)
		{
			localAvatarObj.transform.position = briefingSpawnPoint.transform.position;
			localAvatarObj.transform.rotation = briefingSpawnPoint.transform.rotation;
		}
	}

	private void OnDestroy()
	{
		if ((bool)VTOLMPBriefingManager.instance)
		{
			VTOLMPBriefingManager.instance.OnSetBriefingController -= Instance_OnSetBriefingController;
		}
		if (VTOLMPLobbyManager.hasInstance)
		{
			VTOLMPLobbyManager.instance.OnVoteKick -= Instance_OnVoteKick;
			VTOLMPLobbyManager.instance.OnVoteBan -= Instance_OnVoteBan;
		}
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= VTNetworkManager_OnClientConnected;
		}
		VTOLMPLobbyManager.OnNewPlayerJoined -= Lobby_OnNewPlayerJoined;
		VTOLMPLobbyManager.OnPlayerLeft -= Lobby_OnPlayerLeft;
		VTNetworkManager.OnDisconnected -= OnSocketClientDisconnected;
		VTNetworkManager.OnDisconnected -= OnSocketHostFailed;
	}

	private void Lobby_OnNewPlayerJoined(PlayerInfo obj)
	{
		if (VTNetworkManager.instance.netState == VTNetworkManager.NetStates.IsHost)
		{
			VTOLMPLobbyManager.SendSocketHostCreatedMessage();
		}
	}

	private void SetLoadingText(string txt)
	{
		Debug.Log("VTOLMPSceneManager loading: " + txt);
	}

	public void DisconnectToMainMenu()
	{
		Debug.Log("VTOLMPSceneManager: Disconnecting and returning to main menu.");
		if (VTNetworkManager.instance.netState == VTNetworkManager.NetStates.IsClient)
		{
			VTNetworkManager.StopClient();
		}
		else if (VTNetworkManager.instance.netState == VTNetworkManager.NetStates.IsHost)
		{
			VTNetworkManager.StopHost();
		}
		VTOLMPLobbyManager.LeaveLobby(checkMPScene: false);
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.InvokeExitScene();
		}
		VTScenario.current = null;
		VTScenario.currentScenarioInfo = null;
		PilotSaveManager.currentScenario = null;
		PilotSaveManager.currentCampaign = null;
		PilotSaveManager.currentVehicle = null;
		PilotSelectUI.wasMP = true;
		LoadingSceneController.LoadSceneImmediate("ReadyRoom");
		if (!GameSettings.VR_SDK_IS_OCULUS)
		{
			SteamVR.settings.pauseGameWhenDashboardVisible = true;
		}
	}

	private IEnumerator ConnectToHostRoutine()
	{
		float attemptDuration = 10f;
		int maxAttempts = 3;
		int currAttempt = 0;
		float timeBetweenAttempts = 1f;
		float waitForHostT = Time.time;
		SetLoadingText("Waiting for host");
		while (!VTOLMPLobbyManager.remoteHostCreated)
		{
			if (Time.time - waitForHostT > 20f)
			{
				SetLoadingText("Host timed out.\nQuitting");
				yield return new WaitForSeconds(3f);
				VTMPMainMenu.AddQueuedError("Failed to connect to host.");
				DisconnectToMainMenu();
				yield break;
			}
			yield return null;
		}
		for (; currAttempt < maxAttempts; currAttempt++)
		{
			if (VTNetworkManager.instance.connectionState == VTNetworkManager.ConnectionStates.Connected)
			{
				break;
			}
			SetLoadingText($"Connecting to host ({currAttempt + 1})");
			if (!VTNetworkManager.CreateClient(VTOLMPLobbyManager.currentLobby.Owner.Id))
			{
				SetLoadingText("Failed to connect to host... quitting.");
				VTMPMainMenu.AddQueuedError("Failed to connect to host.");
				yield return new WaitForSeconds(2f);
				DisconnectToMainMenu();
				yield break;
			}
			Func<ConnectionState> GetClientState = () => VTNetworkManager.instance.clientConnectionState;
			if (GetClientState() != ConnectionState.Connecting && GetClientState() != ConnectionState.Connected)
			{
				Debug.Log("ConnectToHostRoutine: Waiting to start connecting.  Current state: " + GetClientState());
				while (GetClientState() != ConnectionState.Connecting)
				{
					yield return null;
				}
			}
			float t = Time.time;
			ConnectionState currState = GetClientState();
			while (Time.time - t < attemptDuration && currState != ConnectionState.Connected)
			{
				if (currState != GetClientState())
				{
					currState = GetClientState();
					SetLoadingText($"Connecting to host ({currAttempt + 1})\n{currState}");
					switch (currState)
					{
					case ConnectionState.Dead:
					case ConnectionState.FinWait:
					case ConnectionState.None:
					case ConnectionState.ClosedByPeer:
					case ConnectionState.ProblemDetectedLocally:
						t = 0f;
						break;
					}
				}
				yield return null;
			}
			if (VTNetworkManager.instance.connectionState != VTNetworkManager.ConnectionStates.Connected)
			{
				VTNetworkManager.StopClient();
				SetLoadingText("Connection timed out.\nRetrying");
				yield return new WaitForSeconds(timeBetweenAttempts);
			}
		}
		if (VTNetworkManager.instance.connectionState == VTNetworkManager.ConnectionStates.Connected)
		{
			Debug.LogFormat("Successfully connected to host after {0} attempts", currAttempt + 1);
			VTNetworkManager.OnDisconnected += OnSocketClientDisconnected;
			Debug.Log("VTOLMPSceneManager: Waiting for net scene entities to be initialized.");
			int i = 0;
			while (!VTNetSceneManager.instance.sceneEntitiesReady)
			{
				i++;
				yield return null;
			}
			Debug.Log($"VTOLMPSceneManager: Scene entities ready after {i} frames.");
		}
		else
		{
			SetLoadingText($"Connection failed after {maxAttempts} attempts. Quitting.");
			yield return new WaitForSeconds(3f);
			VTMPMainMenu.AddQueuedError("Failed to connect to host.");
			DisconnectToMainMenu();
			yield return null;
		}
	}

	private void OnSocketClientDisconnected(string reason)
	{
		Debug.Log("Socket client disconnected.  Leaving game.");
		VTNetworkManager.OnDisconnected -= OnSocketClientDisconnected;
		if (reason != "None")
		{
			VTMPMainMenu.AddQueuedError($"Disconnected: {reason}");
		}
		DisconnectToMainMenu();
	}

	private void OnSocketHostFailed(string reason)
	{
		Debug.Log("Socket host failed.  Leaving game.");
		VTNetworkManager.OnDisconnected -= OnSocketHostFailed;
		if (reason != "None")
		{
			VTMPMainMenu.AddQueuedError($"Disconnected: {reason}");
		}
		DisconnectToMainMenu();
	}

	public void DontExitOnDisconnect()
	{
		VTNetworkManager.OnDisconnected -= OnSocketClientDisconnected;
	}

	private void SetupVehicleSlots()
	{
		Debug.Log("SetupVehicleSlots()");
		alliedSlots = new List<VehicleSlot>();
		enemySlots = new List<VehicleSlot>();
		List<UnitSpawner> list = new List<UnitSpawner>();
		foreach (UnitSpawner value2 in VTScenario.current.units.units.Values)
		{
			if (value2.prefabUnitSpawn is MultiplayerSpawn)
			{
				list.Add(value2);
			}
		}
		list.Sort(SlotSpawnSorter);
		int num = 1;
		int num2 = 1;
		foreach (UnitSpawner item in list)
		{
			VehicleSlot vehicleSlot = new VehicleSlot();
			List<VehicleSlot> list2 = ((item.team == Teams.Allied) ? alliedSlots : enemySlots);
			vehicleSlot.idx = list2.Count;
			vehicleSlot.vehicleName = GetVehicleName(item);
			vehicleSlot.spawnID = item.unitInstanceID;
			vehicleSlot.team = item.team;
			if (vehicleSlot.team == Teams.Allied)
			{
				vehicleSlot.designation = new Actor.Designation(PhoneticLetters.Alpha, 1, num);
				num++;
			}
			else
			{
				vehicleSlot.designation = new Actor.Designation(PhoneticLetters.Bravo, 1, num2);
				num2++;
			}
			vehicleSlot.slotTitle = vehicleSlot.designation.ToString() ?? "";
			int num3 = 1;
			if (item.unitFields.TryGetValue("slots", out var value))
			{
				int num4 = int.Parse(value);
				if (num4 > 0)
				{
					num3 = num4;
				}
			}
			Debug.Log($"Setup Vehicle Slot: spawnID={vehicleSlot.spawnID}, idx={vehicleSlot.idx}");
			vehicleSlot.seatIdx = 0;
			if (num3 == 1)
			{
				list2.Add(vehicleSlot);
				continue;
			}
			vehicleSlot.slotTitle += " Pilot";
			list2.Add(vehicleSlot);
			for (int i = 1; i < num3; i++)
			{
				VehicleSlot vehicleSlot2 = new VehicleSlot();
				vehicleSlot2.slotTitle = "     Gunner";
				vehicleSlot2.vehicleName = vehicleSlot.vehicleName;
				vehicleSlot2.spawnID = item.unitInstanceID;
				vehicleSlot2.team = vehicleSlot.team;
				vehicleSlot2.idx = list2.Count;
				vehicleSlot2.seatIdx = i;
				vehicleSlot2.designation = vehicleSlot.designation;
				list2.Add(vehicleSlot2);
			}
		}
		alliedSlots.Sort(SlotComparer);
		enemySlots.Sort(SlotComparer);
	}

	private int SlotSpawnSorter(UnitSpawner a, UnitSpawner b)
	{
		if (a.team == b.team)
		{
			return a.unitInstanceID.CompareTo(b.unitInstanceID);
		}
		return a.team.CompareTo(b.team);
	}

	private string GetVehicleName(UnitSpawner mpSpawner)
	{
		return MultiplayerSpawn.GetVehicleName(ConfigNodeUtils.ParseEnum<MultiplayerSpawn.Vehicles>(mpSpawner.unitFields["vehicle"]));
	}

	private int SlotComparer(VehicleSlot a, VehicleSlot b)
	{
		return a.spawnID.CompareTo(b.spawnID);
	}

	public void RequestSlot(VehicleSlot slot)
	{
		if (base.isMine)
		{
			Debug.Log($"Local host has requested a slot: {slot.team}:{slot.idx}");
			if (slot.player == null)
			{
				VacateSlot();
				slot.player = localPlayer;
				localPlayer.selectedSlot = slot.idx;
				ReportSlotChanged(slot);
				DeclareReady(r: true);
			}
		}
		else
		{
			SendRPC("RPC_RequestSlot", (int)slot.team, slot.idx, BDSteamClient.mySteamID);
		}
	}

	[VTRPC]
	private void RPC_RequestSlot(int team, int idx, ulong playerID)
	{
		if (!base.isMine)
		{
			return;
		}
		VehicleSlot vehicleSlot = ((team == 0) ? alliedSlots[idx] : enemySlots[idx]);
		if (vehicleSlot.player != null)
		{
			return;
		}
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(playerID);
		if (player != null)
		{
			if (player.selectedSlot >= 0)
			{
				VehicleSlot vehicleSlot2 = ((player.team == Teams.Allied) ? alliedSlots[player.selectedSlot] : enemySlots[player.selectedSlot]);
				vehicleSlot2.player = null;
				ReportSlotChanged(vehicleSlot2);
			}
			player.selectedSlot = idx;
			vehicleSlot.player = player;
			ReportSlotChanged(vehicleSlot);
		}
		else
		{
			Debug.LogError($"A player tried to request a slot but they were not found in our player info list!  ({playerID} {new Friend(playerID).Name})");
		}
	}

	private void ReportTeamChanged(PlayerInfo player)
	{
		if (base.isMine)
		{
			RPC_TeamChanged(player.steamUser.Id.Value, (int)player.team);
			SendRPC("RPC_TeamChanged", player.steamUser.Id.Value, (int)player.team);
		}
	}

	[VTRPC]
	private void RPC_TeamChanged(ulong playerId, int team)
	{
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(playerId);
		if (player != null)
		{
			Debug.Log($"RPC_TeamChanged({player.pilotName}, {(Teams)team})");
			player.team = (Teams)team;
			player.chosenTeam = true;
			this.OnPlayerSelectedTeam?.Invoke(player);
			if (player == localPlayer)
			{
				RequestNewBriefingSeat();
			}
		}
		if (!base.isMine || VTMapManager.fetch.mpScenarioStart)
		{
			return;
		}
		foreach (PlayerInfo connectedPlayer in VTOLMPLobbyManager.instance.connectedPlayers)
		{
			SendRPC("RPC_DeclareReady", connectedPlayer.steamUser.Id.Value, connectedPlayer.isReady ? 1 : 0);
		}
	}

	public void ReportPlayerSelectedTeam(PlayerInfo player)
	{
		Debug.Log($"{player.pilotName} has picked team {player.team}.");
		this.OnPlayerSelectedTeam?.Invoke(player);
	}

	public void VacateSlot(PlayerInfo player)
	{
		if (player.selectedSlot < 0 || alliedSlots == null)
		{
			return;
		}
		VehicleSlot vehicleSlot = ((player.team == Teams.Allied) ? alliedSlots[player.selectedSlot] : enemySlots[player.selectedSlot]);
		if (base.isMine)
		{
			vehicleSlot.player = null;
			player.selectedSlot = -1;
			ReportSlotChanged(vehicleSlot);
			return;
		}
		SendRPC("RPC_VacateSlot", (int)vehicleSlot.team, vehicleSlot.idx);
		if (vehicleSlot.player == player)
		{
			vehicleSlot.player = null;
			player.selectedSlot = -1;
			ReportSlotChanged(vehicleSlot);
		}
	}

	public void DeclareReady(bool r)
	{
		localPlayer.isReady = r;
		SendRPC("RPC_DeclareReady", BDSteamClient.mySteamID, r ? 1 : 0);
		this.OnDeclaredReady?.Invoke(BDSteamClient.mySteamID, r);
	}

	[VTRPC]
	private void RPC_DeclareReady(ulong id, int r)
	{
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(id);
		if (player != null)
		{
			player.isReady = r > 0;
			this.OnDeclaredReady?.Invoke(id, r > 0);
		}
	}

	public void VacateSlot()
	{
		if (localPlayer.selectedSlot < 0)
		{
			return;
		}
		VehicleSlot vehicleSlot = ((localPlayer.team == Teams.Allied) ? alliedSlots[localPlayer.selectedSlot] : enemySlots[localPlayer.selectedSlot]);
		if (base.isMine)
		{
			vehicleSlot.player = null;
			localPlayer.selectedSlot = -1;
			ReportSlotChanged(vehicleSlot);
			return;
		}
		SendRPC("RPC_VacateSlot", (int)vehicleSlot.team, vehicleSlot.idx);
		if (vehicleSlot.player == localPlayer)
		{
			vehicleSlot.player = null;
			localPlayer.selectedSlot = -1;
			ReportSlotChanged(vehicleSlot);
		}
	}

	[VTRPC]
	private void RPC_VacateSlot(int team, int idx)
	{
		if (base.isMine)
		{
			VehicleSlot vehicleSlot = ((team == 0) ? alliedSlots[idx] : enemySlots[idx]);
			PlayerInfo player = vehicleSlot.player;
			if (player != null)
			{
				player.selectedSlot = -1;
				SendRPC("RPC_SetPlayerVacated", player.steamUser.Id.Value);
			}
			vehicleSlot.player = null;
			ReportSlotChanged(vehicleSlot);
		}
	}

	private void ReportSlotChanged(VehicleSlot slot)
	{
		Debug.Log($"Reporting a slot has changed: {slot.team}:{slot.idx}");
		this.OnSlotUpdated?.Invoke(slot);
		if (base.isMine)
		{
			SendRPC("RPC_SetSlotPlayer", (int)slot.team, slot.idx, (slot.player == null) ? 0 : slot.player.steamUser.Id.Value);
		}
	}

	private void ReportSlotChanged(VehicleSlot slot, ulong target)
	{
		Debug.Log($"Reporting a slot has changed (directed): {slot.team}:{slot.idx}");
		this.OnSlotUpdated?.Invoke(slot);
		if (base.isMine)
		{
			SendDirectedRPC(target, "RPC_SetSlotPlayer", (int)slot.team, slot.idx, (slot.player == null) ? 0 : slot.player.steamUser.Id.Value);
		}
	}

	[VTRPC]
	private void RPC_SetSlotPlayer(int team, int idx, ulong playerID)
	{
		if (!base.isMine)
		{
			VehicleSlot vehicleSlot = ((team != 0) ? enemySlots[idx] : alliedSlots[idx]);
			vehicleSlot.player = VTOLMPLobbyManager.GetPlayer(playerID);
			if (vehicleSlot.player != null)
			{
				vehicleSlot.player.selectedSlot = vehicleSlot.idx;
				vehicleSlot.player.team = (Teams)team;
			}
			Debug.Log(string.Format("RPC_SetSlotPlayer: {0} {1} {2}", (Teams)team, idx, (vehicleSlot.player == null) ? "null" : vehicleSlot.player.pilotName));
			ReportSlotChanged(vehicleSlot);
		}
	}

	private void ResendSlotsForNewPlayer(ulong target)
	{
		if (!base.isMine)
		{
			return;
		}
		for (int i = 0; i < alliedSlots.Count; i++)
		{
			VehicleSlot vehicleSlot = alliedSlots[i];
			if (vehicleSlot.player != null)
			{
				ReportSlotChanged(vehicleSlot, target);
			}
		}
		for (int j = 0; j < enemySlots.Count; j++)
		{
			VehicleSlot vehicleSlot2 = enemySlots[j];
			if (vehicleSlot2.player != null)
			{
				ReportSlotChanged(vehicleSlot2, target);
			}
		}
	}

	[VTRPC]
	private void RPC_SetPlayerVacated(ulong playerID)
	{
		if (!base.isMine)
		{
			PlayerInfo player = VTOLMPLobbyManager.GetPlayer(playerID);
			if (player != null)
			{
				player.selectedSlot = -1;
			}
		}
	}

	public PlayerInfo[] GetPlayers(Teams team)
	{
		if (team != 0)
		{
			return enemyPlayers;
		}
		return alliedPlayers;
	}

	public Actor GetActor(ulong steamId)
	{
		return VTOLMPLobbyManager.GetPlayer(steamId)?.vehicleActor;
	}

	public PlayerInfo GetPlayer(Actor actor)
	{
		PlayerEntityIdentifier component = actor.GetComponent<PlayerEntityIdentifier>();
		if ((bool)component)
		{
			return VTOLMPLobbyManager.GetPlayer(component.netEnt.ownerID);
		}
		return null;
	}

	public void Host_BeginScenario()
	{
		if (VTOLMPLobbyManager.isLobbyHost)
		{
			RPC_BeginScenario();
			SendRPCBuffered("RPC_BeginScenario");
			CultureInfo cultureInfo = new CultureInfo("en-US");
			string text = DateTime.UtcNow.ToString(cultureInfo);
			Debug.Log("Beginning the scenario at " + text);
			VTOLMPLobbyManager.currentLobby.SetData("mUtc", text);
			VTOLMPLobbyManager.currentLobby.SetData("gState", VTOLMPLobbyManager.GameStates.Mission.ToString());
		}
	}

	[VTRPC]
	private void RPC_BeginScenario()
	{
		Debug.Log("RPC_BeginScenario!");
		if (VTMapManager.fetch.mpScenarioStart)
		{
			Debug.LogError(" -- The scenario was already started previously!!!");
		}
		else
		{
			VTMapManager.fetch.mpScenarioStart = true;
		}
	}

	public void ReportScenarioStarted()
	{
		this.OnMPScenarioStart?.Invoke();
	}

	public void SpawnVehicleForMe()
	{
		if (localPlayer.selectedSlot >= 0)
		{
			Debug.Log("Spawning vehicle for me!");
			StartCoroutine(SpawnRoutine());
		}
		else
		{
			Debug.LogError("We requested to be spawned for MP but we don't have a slot selected!");
		}
	}

	private IEnumerator SpawnRoutine()
	{
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut();
		MultiplayerSpawn mpSpawn = GetSpawn(localPlayer.team, localPlayer.selectedSlot);
		yield return new WaitForSeconds(1.5f);
		AudioController.instance.MP_SetNearVoiceAtten(0f);
		VTOLMPBriefingRoom.instance.ui.slotsMenuDisplayObj.SetActive(value: false);
		while (!VTMapManager.fetch.scenarioReady)
		{
			yield return null;
		}
		yield return new WaitForFixedUpdate();
		yield return null;
		FloatingOrigin.instance.ShiftOrigin(mpSpawn.transform.position);
		yield return new WaitForFixedUpdate();
		yield return null;
		string vehicleName = mpSpawn.VehicleName();
		PlayerVehicle pv = (PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(vehicleName));
		if (GetSlot(localPlayer).seatIdx > 0)
		{
			Debug.LogError("TEMP: We are seat2.  waiting for seat1 owner to spawn");
			while (!mpSpawn.actor)
			{
				yield return null;
			}
		}
		if ((bool)mpSpawn.actor)
		{
			Debug.Log("Trying to spawn a vehicle for ourself but the actor already exists!");
			if (mpSpawn.slots > 1f)
			{
				Debug.Log(" - It's a multicrew vehicle!  Spawning avatar in existing vehicle.");
				StartCoroutine(SpawnInMulticrew(mpSpawn));
				Debug.Log("Invoking OnEnterVehicle (1)");
				this.OnEnterVehicle?.Invoke();
				int entityID = mpSpawn.actor.GetComponent<VTNetEntity>().entityID;
				SendRPC("RPC_SetPlayerVehicleEntity", BDSteamClient.mySteamID, entityID, localPlayer.selectedSlot, (int)localPlayer.team);
				yield break;
			}
			Debug.LogError(" - It was not multicrew!");
		}
		VTNetworkManager.NetInstantiateRequest req = VTNetworkManager.NetInstantiate(pv.resourcePath, mpSpawn.transform.position, mpSpawn.transform.rotation, active: false);
		while (!req.isReady)
		{
			yield return null;
		}
		Debug.Log("Invoking OnEnterVehicle (2)");
		this.OnEnterVehicle?.Invoke();
		req.obj.transform.position = mpSpawn.transform.TransformPoint(pv.playerSpawnOffset);
		req.obj.transform.rotation = Quaternion.AngleAxis(pv.spawnPitch, mpSpawn.transform.right) * mpSpawn.transform.rotation;
		req.obj.SetActive(value: true);
		SetMyVehicleEntityID(req.obj.GetComponent<VTNetEntity>());
		mpSpawn.SetupSpawnedVehicle(req.obj);
		if ((bool)req.obj.GetComponent<MultiUserVehicleSync>())
		{
			StartCoroutine(SpawnInMulticrew(mpSpawn));
		}
		VTOLMPLobbyManager.SendLogMessage(VTOLMPLobbyManager.localPlayerInfo.pilotName + " has spawned.");
		yield return null;
		if (VTOLMPBriefingRoom.instance.playerLoadout != null)
		{
			Loadout playerLoadout = VTOLMPBriefingRoom.instance.playerLoadout;
			req.obj.GetComponentInChildren<WeaponManagerSync>().NetEquipWeapons(playerLoadout);
		}
		PilotSaveManager.currentScenario.totalBudget = GetTotalBudget();
		PilotSaveManager.currentScenario.inFlightSpending = 0f;
		ScreenFader.FadeIn();
		ControllerEventHandler.UnpauseEvents();
	}

	private void SetMyVehicleEntityID(VTNetEntity vehicleEntity)
	{
		int num = (vehicleEntity ? vehicleEntity.entityID : (-1));
		localPlayer.vehicleEntityID = num;
		SendRPC("RPC_SetPlayerVehicleEntity", BDSteamClient.mySteamID, num, localPlayer.selectedSlot, (int)localPlayer.team);
	}

	private IEnumerator SpawnInMulticrew(MultiplayerSpawn mpSpawn)
	{
		while (!mpSpawn.actor)
		{
			yield return null;
		}
		MultiUserVehicleSync component = mpSpawn.actor.GetComponent<MultiUserVehicleSync>();
		component.SpawnLocalPlayerAvatar();
		SetMyVehicleEntityID(component.netEntity);
	}

	public float GetTotalBudget()
	{
		return 900000f;
	}

	[VTRPC]
	private void RPC_SetPlayerVehicleEntity(ulong steamId, int entityID, int slot, int team)
	{
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(steamId);
		if (player != null)
		{
			if ((bool)VTNetworkManager.instance.GetEntity(entityID))
			{
				player.team = (Teams)team;
				player.selectedSlot = slot;
				player.chosenTeam = true;
				player.vehicleEntityID = entityID;
			}
		}
		else
		{
			Debug.LogError($"Tried to set vehicleEntityID for player but player not found! {steamId} ({new Friend(steamId).Name})");
		}
	}

	public MultiplayerSpawn GetSpawn(Teams team, int slot)
	{
		int num = 0;
		foreach (UnitSpawner value2 in ((team == Teams.Allied) ? VTScenario.current.units.alliedUnits : VTScenario.current.units.enemyUnits).Values)
		{
			if (!(value2.prefabUnitSpawn is MultiplayerSpawn))
			{
				continue;
			}
			int num2 = 1;
			if (value2.unitFields.TryGetValue("slots", out var value))
			{
				int num3 = int.Parse(value);
				if (num3 > 0)
				{
					num2 = num3;
				}
			}
			for (int i = 0; i < num2; i++)
			{
				if (num == slot)
				{
					return (MultiplayerSpawn)value2.spawnedUnit;
				}
				num++;
			}
		}
		return null;
	}

	public MultiplayerSpawn GetMPSpawn(Teams team, int slot)
	{
		List<VehicleSlot> list = ((team == Teams.Allied) ? alliedSlots : enemySlots);
		return (MultiplayerSpawn)VTScenario.current.units.GetUnit(list[slot].spawnID).spawnedUnit;
	}

	public VehicleSlot GetSlot(PlayerInfo p)
	{
		if (!p.chosenTeam)
		{
			return null;
		}
		if (p.selectedSlot < 0)
		{
			return null;
		}
		if (p.team != 0)
		{
			return enemySlots[p.selectedSlot];
		}
		return alliedSlots[p.selectedSlot];
	}

	public TeamRequest RequestTeam(Teams team)
	{
		TeamRequest teamRequest = new TeamRequest();
		StartCoroutine(RequestTeamRoutine(team, teamRequest));
		return teamRequest;
	}

	private IEnumerator RequestTeamRoutine(Teams team, TeamRequest req)
	{
		int num;
		if (base.isMine)
		{
			Debug.Log("(HOST) Sending request for team " + team);
			num = RPC_RequestTeam(BDSteamClient.mySteamID, (int)team);
		}
		else
		{
			Debug.Log("Sending request for team " + team);
			RPCRequest rpcReq = SendRPCRequest(typeof(int), VTOLMPLobbyManager.currentLobby.Owner.Id.Value, "RPC_RequestTeam", BDSteamClient.mySteamID, (int)team);
			while (!rpcReq.isComplete)
			{
				yield return null;
			}
			num = (int)rpcReq.Value;
		}
		req.accepted = num == 1;
		req.isReady = true;
		Debug.Log($"Team request accepted = {req.accepted}");
		if (req.accepted)
		{
			localPlayer.team = team;
			if ((bool)UnitIconManager.instance)
			{
				UnitIconManager.instance.UpdateIconTeams();
			}
			if (VTScenario.current != null)
			{
				Debug.Log("VTOLMPSceneManager : Setting team waypoints after joining team.");
				SetTeamWaypoints();
			}
		}
	}

	private void SetTeamWaypoints()
	{
		VTScenario current = VTScenario.current;
		WaypointManager.instance.rtbWaypoint = current.GetRTBWaypoint();
		WaypointManager.instance.fuelWaypoint = current.GetRefuelWaypoint();
		WaypointManager.instance.bullseye = current.waypoints.bullseyeTransform;
		if (VTOLMPLobbyManager.localPlayerInfo.team == Teams.Enemy)
		{
			WaypointManager.instance.rtbWaypoint = current.GetRTBWaypoint(Teams.Enemy);
			WaypointManager.instance.fuelWaypoint = current.GetRefuelWaypoint(Teams.Enemy);
			WaypointManager.instance.bullseye = current.waypoints.bullseyeBTransform;
		}
	}

	[VTRPC]
	private int RPC_RequestTeam(ulong playerId, int team)
	{
		Teams teams = (Teams)team;
		Debug.Log("(Host) received team request for " + teams);
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(playerId);
		if (player == null)
		{
			Debug.LogError(" - Team request failed!  PlayerInfo for this player is not found!");
			return 0;
		}
		PlayerInfo[] array = ((team == 0) ? alliedPlayers : enemyPlayers);
		int num = -1;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == null && num < 0)
			{
				num = i;
			}
			if (array[i] != null && (ulong)array[i].steamUser.Id == playerId)
			{
				Debug.Log(" - player is already on this team!");
				return -1;
			}
		}
		if (num >= 0)
		{
			array[num] = player;
			player.chosenTeam = true;
			player.team = ((team != 0) ? Teams.Enemy : Teams.Allied);
			Debug.Log(" - accepting team request.");
			ReportTeamChanged(player);
			return 1;
		}
		Debug.Log(" - denying team request.");
		return 0;
	}

	private void SetupBriefingSeats()
	{
		VTScenario.current.GetMPSeatCounts(out var allies, out var enemies);
		alliedBriefingSeats = new PlayerInfo[allies];
		enemyBriefingSeats = new PlayerInfo[enemies];
		alliedPlayers = new PlayerInfo[allies];
		enemyPlayers = new PlayerInfo[enemies];
	}

	[VTRPC]
	private int RPC_RequestBriefingSeat(ulong id, int team)
	{
		if (VTOLMPLobbyManager.isLobbyHost)
		{
			return Host_RequestBriefingSeat(id, team);
		}
		throw new InvalidOperationException("Only the host should receive briefing seat requests!");
	}

	private int Host_RequestBriefingSeat(ulong id, int team)
	{
		PlayerInfo[] array = ((team == 0) ? alliedBriefingSeats : enemyBriefingSeats);
		int num = -1;
		for (int i = 0; i < array.Length; i++)
		{
			if ((array[i] == null && num < 0) || (array[i] != null && (ulong)array[i].steamUser.Id == id))
			{
				num = i;
			}
		}
		if (num >= 0)
		{
			for (int j = 0; j < enemyBriefingSeats.Length; j++)
			{
				if (enemyBriefingSeats[j] != null && (ulong)enemyBriefingSeats[j].steamUser.Id == id)
				{
					enemyBriefingSeats[j] = null;
				}
			}
			for (int k = 0; k < alliedBriefingSeats.Length; k++)
			{
				if (alliedBriefingSeats[k] != null && (ulong)alliedBriefingSeats[k].steamUser.Id == id)
				{
					alliedBriefingSeats[k] = null;
				}
			}
			PlayerInfo playerInfo = (array[num] = VTOLMPLobbyManager.GetPlayer(id));
			SendRPCBuffered("RPC_SetBriefingSeat", id, team, num);
			RPC_SetBriefingSeat(id, team, num);
		}
		return num;
	}

	public int GetSeatIdx(ulong userId)
	{
		if (briefingSeatAssignments.TryGetValue(userId, out var value))
		{
			return value;
		}
		return -1;
	}

	[VTRPC]
	private void RPC_SetBriefingSeat(ulong id, int team, int seatIdx)
	{
		this.OnBriefingSeatUpdated?.Invoke(id, (Teams)team, seatIdx);
		briefingSeatAssignments.Remove(id);
		briefingSeatAssignments.Add(id, seatIdx);
	}

	private void SetBriefingControllerSeat(ulong id)
	{
		Debug.Log($"SetBriefingControllerSeat({id})");
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(id);
		if (player != null)
		{
			Debug.Log(" - moving " + player.pilotName + " to the lectern");
			BriefingSpawnPoint[] array = ((player.team == Teams.Allied) ? VTOLMPBriefingRoom.instance.alliedSpawnTransforms : VTOLMPBriefingRoom.instance.enemySpawnTransforms);
			this.OnBriefingSeatUpdated?.Invoke(id, player.team, array.Length);
		}
	}

	public int GetTotalKills(Teams team)
	{
		int num = 0;
		foreach (PlayerStats value in playerStats.Values)
		{
			if (value.player.team == team)
			{
				num += value.kills;
			}
		}
		return num;
	}

	public int GetTotalDeaths(Teams team)
	{
		int num = 0;
		foreach (PlayerStats value in playerStats.Values)
		{
			if (value.player.team == team)
			{
				num += value.deaths;
			}
		}
		return num;
	}

	public PlayerStats GetPlayerStats(PlayerInfo p)
	{
		if (this.playerStats.TryGetValue(p, out var value))
		{
			return value;
		}
		PlayerStats playerStats = new PlayerStats();
		playerStats.player = p;
		this.playerStats.Add(p, playerStats);
		return playerStats;
	}

	public void GiveKillCredit(PlayerInfo killer, Actor victim)
	{
		if (killer != null && !(victim == null) && MissionManager.instance.finalWinner == MissionManager.FinalWinner.None)
		{
			int num = ((killer.team != victim.team) ? 1 : (-3));
			if (base.isMine)
			{
				Host_GiveKillCredit(killer, num, victim);
				return;
			}
			SendRPC("RPC_KillCredit", killer.steamUser.Id.Value, num, VTNetUtils.GetActorIdentifier(victim));
		}
	}

	[VTRPC]
	private void RPC_KillCredit(ulong playerId, int count, int victimId)
	{
		if (base.isMine)
		{
			PlayerInfo player = VTOLMPLobbyManager.GetPlayer(playerId);
			Host_GiveKillCredit(player, count, VTNetUtils.GetActorFromIdentifier(victimId));
		}
	}

	private void Host_GiveKillCredit(PlayerInfo p, int count, Actor victim)
	{
		if (p != null)
		{
			PlayerStats playerStats = GetPlayerStats(p);
			if (count < 0)
			{
				playerStats.teamKills++;
				playerStats.kills -= playerStats.teamKills;
			}
			else
			{
				playerStats.kills += count;
			}
			SendUpdateStats(playerStats);
			if (victim != null)
			{
				VTOLMPLobbyManager.SendLogMessage(p.pilotName + " killed " + victim.actorName + ".");
			}
		}
	}

	public void GiveAssistCredit(PlayerInfo p)
	{
		if (p != null && MissionManager.instance.finalWinner == MissionManager.FinalWinner.None)
		{
			if (base.isMine)
			{
				Host_GiveAssistCredit(p);
				return;
			}
			SendRPC("RPC_AssistCredit", p.steamUser.Id.Value);
		}
	}

	[VTRPC]
	private void RPC_AssistCredit(ulong playerId)
	{
		if (base.isMine)
		{
			Host_GiveAssistCredit(VTOLMPLobbyManager.GetPlayer(playerId));
		}
	}

	private void Host_GiveAssistCredit(PlayerInfo p)
	{
		if (p != null)
		{
			PlayerStats playerStats = GetPlayerStats(p);
			playerStats.assists++;
			SendUpdateStats(playerStats);
		}
	}

	public void GiveDeathCredit(PlayerInfo p)
	{
		if (p != null && MissionManager.instance.finalWinner == MissionManager.FinalWinner.None)
		{
			if (base.isMine)
			{
				Host_GiveDeathCredit(p);
				return;
			}
			SendRPC("RPC_DeathCredit", p.steamUser.Id.Value);
		}
	}

	[VTRPC]
	private void RPC_DeathCredit(ulong playerId)
	{
		if (base.isMine)
		{
			Host_GiveDeathCredit(VTOLMPLobbyManager.GetPlayer(playerId));
		}
	}

	private void Host_GiveDeathCredit(PlayerInfo p)
	{
		if (p != null)
		{
			PlayerStats playerStats = GetPlayerStats(p);
			playerStats.deaths++;
			SendUpdateStats(playerStats);
		}
	}

	private void SendUpdateStats(PlayerStats stat)
	{
		this.OnStatsUpdated?.Invoke(stat);
		SendRPC("RPC_UpdateStats", stat.player.steamUser.Id.Value, stat.kills, stat.assists, stat.deaths);
	}

	[VTRPC]
	private void RPC_UpdateStats(ulong id, int kills, int assists, int deaths)
	{
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(id);
		if (player != null)
		{
			PlayerStats playerStats = GetPlayerStats(player);
			playerStats.kills = kills;
			playerStats.assists = assists;
			playerStats.deaths = deaths;
			this.OnStatsUpdated?.Invoke(playerStats);
		}
	}

	private void SendAllStatsToPlayer(ulong target)
	{
		foreach (PlayerStats value in playerStats.Values)
		{
			SendDirectedRPC(target, "RPC_UpdateStats", value.player.steamUser.Id.Value, value.kills, value.assists, value.deaths);
		}
	}

	public void BanMember(SteamId id)
	{
		if (base.isMine)
		{
			VTOLMPLobbyManager.instance.SetMemberBanned(id);
			SendDirectedRPC(id, "RPC_Ban");
			StartCoroutine(DisconnectUserDelayed(id));
		}
	}

	[VTRPC]
	private void RPC_Ban()
	{
		if (VTNetworkManager.currentRPCInfo.exists && VTNetworkManager.currentRPCInfo.senderId == VTOLMPLobbyManager.instance.currentLobbyHost.Value)
		{
			VTOLMPLobbyManager.instance.ReceiveBan();
		}
	}

	public void KickMember(SteamId id)
	{
		if (base.isMine)
		{
			SendDirectedRPC(id, "RPC_Kick");
			StartCoroutine(DisconnectUserDelayed(id));
		}
	}

	private IEnumerator DisconnectUserDelayed(SteamId id)
	{
		yield return new WaitForSeconds(2f * VTNetworkManager.CurrentSendInterval);
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.socketHost.CloseConnectionTo(id);
		}
	}

	[VTRPC]
	private void RPC_Kick()
	{
		if (VTNetworkManager.currentRPCInfo.exists && VTNetworkManager.currentRPCInfo.senderId == VTOLMPLobbyManager.instance.currentLobbyHost.Value)
		{
			VTOLMPLobbyManager.instance.ReceiveKick();
		}
	}

	public void VoteKick(SteamId id)
	{
		if (Time.time - timeKickBanVoted > 60f)
		{
			SendDirectedRPC(base.netEntity.ownerID, "RPC_VoteKick", BDSteamClient.mySteamID, id.Value);
			timeKickBanVoted = Time.time;
		}
	}

	[VTRPC]
	private void RPC_VoteKick(ulong voter, ulong target)
	{
	}

	public void VoteBan(SteamId id)
	{
		if (Time.time - timeKickBanVoted > 60f)
		{
			SendDirectedRPC(base.netEntity.ownerID, "RPC_VoteBan", BDSteamClient.mySteamID, id.Value);
			timeKickBanVoted = Time.time;
		}
	}

	[VTRPC]
	private void RPC_VoteBan(ulong voter, ulong target)
	{
		if (base.isMine)
		{
			VTOLMPLobbyManager.instance.ReceiveVoteBan(voter, target);
		}
	}
}

}