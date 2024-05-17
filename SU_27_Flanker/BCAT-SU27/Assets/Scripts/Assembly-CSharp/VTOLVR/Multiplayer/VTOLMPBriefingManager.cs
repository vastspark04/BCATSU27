using System;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTOLMPBriefingManager : VTNetSyncRPCOnly
{
	private ulong briefingController;

	private ulong briefingControllerB;

	public bool briefingBothTeams;

	private int currentNoteIdx;

	private int currentNoteIdxB;

	public static VTOLMPBriefingManager instance { get; private set; }

	public event Action<ulong, Teams> OnSetBriefingController;

	public event Action<int, Teams> OnSetBriefingNote;

	protected override void Awake()
	{
		instance = this;
	}

	public bool IsBriefingController(ulong steamId)
	{
		if (briefingController != steamId)
		{
			return briefingControllerB == steamId;
		}
		return true;
	}

	public bool LocalPlayerIsBriefingController()
	{
		return IsBriefingController(BDSteamClient.mySteamID);
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		SendDirectedRPC(obj, "RPC_SetControl", briefingController, 0);
		SendDirectedRPC(obj, "RPC_NoteIdx", currentNoteIdx, 0);
		if (!briefingBothTeams)
		{
			SendDirectedRPC(obj, "RPC_SetControl", briefingControllerB, 1);
			SendDirectedRPC(obj, "RPC_NoteIdx", currentNoteIdxB, 1);
		}
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	public void RequestBriefingControl()
	{
		Debug.Log("Requesting briefing control.");
		if (base.isMine)
		{
			RPC_ReqControl(BDSteamClient.mySteamID);
			return;
		}
		SendRPC("RPC_ReqControl", BDSteamClient.mySteamID);
	}

	[VTRPC]
	private void RPC_ReqControl(ulong userID)
	{
		if (!base.isMine)
		{
			return;
		}
		Debug.Log($"Received request for briefing control from {userID}");
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(userID);
		if (player == null)
		{
			return;
		}
		if (player.team == Teams.Allied || briefingBothTeams)
		{
			if (briefingController == 0L)
			{
				Debug.Log(" - giving control");
				RPC_SetControl(userID, 0);
				SendRPC("RPC_SetControl", userID, 0);
			}
			else
			{
				Debug.Log($" - another user is already in control: {briefingController}");
			}
		}
		else if (briefingControllerB == 0L)
		{
			Debug.Log(" - giving control (B)");
			RPC_SetControl(userID, 1);
			SendRPC("RPC_SetControl", userID, 1);
		}
		else
		{
			Debug.Log($" - another user is already in control: {briefingControllerB}");
		}
	}

	[VTRPC]
	private void RPC_SetControl(ulong userID, int team)
	{
		Debug.Log($"BriefingManager RPC_SetControl({userID})");
		if (VTMapManager.fetch.mpScenarioStart && userID != 0L)
		{
			Debug.LogError("BriefingManager: briefingController tried to be set but the scenario has already started!");
		}
		else if (team == 0 || briefingBothTeams)
		{
			if (briefingController != userID)
			{
				briefingController = userID;
				this.OnSetBriefingController?.Invoke(userID, Teams.Allied);
			}
		}
		else if (briefingControllerB != userID)
		{
			briefingControllerB = userID;
			this.OnSetBriefingController?.Invoke(userID, (Teams)team);
		}
	}

	public void ReleaseControl()
	{
		if (base.isMine)
		{
			RPC_ReleaseControl(BDSteamClient.mySteamID, (int)VTOLMPLobbyManager.localPlayerInfo.team);
			return;
		}
		SendRPC("RPC_ReleaseControl", BDSteamClient.mySteamID, (int)VTOLMPLobbyManager.localPlayerInfo.team);
	}

	[VTRPC]
	private void RPC_ReleaseControl(ulong userID, int team)
	{
		if (base.isMine && IsBriefingController(userID))
		{
			RPC_SetControl(0uL, team);
			SendRPC("RPC_SetControl", 0uL, team);
		}
	}

	public void SetBriefingNote(int noteIdx)
	{
		if (briefingBothTeams || VTOLMPLobbyManager.localPlayerInfo.team == Teams.Allied)
		{
			if (BDSteamClient.mySteamID == briefingController)
			{
				currentNoteIdx = noteIdx;
				this.OnSetBriefingNote?.Invoke(currentNoteIdx, VTOLMPLobbyManager.localPlayerInfo.team);
				SendRPC("RPC_NoteIdx", currentNoteIdx, (int)VTOLMPLobbyManager.localPlayerInfo.team);
			}
		}
		else if (BDSteamClient.mySteamID == briefingControllerB)
		{
			currentNoteIdxB = noteIdx;
			this.OnSetBriefingNote?.Invoke(currentNoteIdxB, VTOLMPLobbyManager.localPlayerInfo.team);
			SendRPC("RPC_NoteIdx", currentNoteIdxB, (int)VTOLMPLobbyManager.localPlayerInfo.team);
		}
	}

	[VTRPC]
	private void RPC_NoteIdx(int idx, int team)
	{
		Debug.Log($"RPC_NoteIdx{idx}");
		if (currentNoteIdx != idx)
		{
			currentNoteIdx = idx;
			this.OnSetBriefingNote?.Invoke(idx, (Teams)team);
		}
	}
}

}