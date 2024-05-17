using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTOLMPDataLinkManager : VTNetSyncRPCOnly
{
	private struct ViewedActor
	{
		public Actor actor;

		public Teams viewingTeam;
	}

	public delegate void ReceiveGPSTargetDelegate(PlayerInfo owner, int groupId, int index, GPSTarget target);

	public delegate void ClearGPSGroupDelegate(PlayerInfo owner, int groupId);

	private List<ViewedActor> actorsToReport = new List<ViewedActor>();

	private int nextGPSGroupId = 1;

	public static VTOLMPDataLinkManager instance { get; private set; }

	public event Action<Actor> OnDataLinkReceived;

	public event ReceiveGPSTargetDelegate OnReceivedGPSTarget;

	public event ClearGPSGroupDelegate OnClearedGPSGroup;

	protected override void Awake()
	{
		base.Awake();
		instance = this;
	}

	private void Start()
	{
		StartCoroutine(StartupRoutine());
	}

	private IEnumerator StartupRoutine()
	{
		while (!VTMapManager.fetch.mpScenarioStart)
		{
			yield return null;
		}
		StartCoroutine(DataLinkRoutine());
	}

	public void ReportKnownPosition(Actor a, Teams viewingTeam)
	{
		for (int i = 0; i < actorsToReport.Count; i++)
		{
			if (actorsToReport[i].actor == a)
			{
				return;
			}
		}
		actorsToReport.Add(new ViewedActor
		{
			actor = a,
			viewingTeam = viewingTeam
		});
	}

	private IEnumerator DataLinkRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(0.2f);
		while (VTNetworkManager.instance.netState != 0)
		{
			if (actorsToReport.Count > 0)
			{
				ViewedActor viewedActor = actorsToReport[0];
				if ((bool)viewedActor.actor)
				{
					int viewingTeam = (int)viewedActor.viewingTeam;
					SendRPC("RPC_ActorPos", VTNetUtils.GetActorIdentifier(viewedActor.actor), viewingTeam, viewedActor.actor.LastSeenTime(viewedActor.viewingTeam));
				}
				actorsToReport.RemoveAt(0);
			}
			yield return wait;
		}
	}

	[VTRPC]
	private void RPC_ActorPos(int actorID, int team, float timestamp)
	{
		PlayerInfo localPlayerInfo = VTOLMPLobbyManager.localPlayerInfo;
		if (team == (int)localPlayerInfo.team && (bool)localPlayerInfo.vehicleActor)
		{
			Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorID);
			if ((bool)actorFromIdentifier)
			{
				actorFromIdentifier.UpdateKnownPosition(localPlayerInfo.vehicleActor, mpBroadcast: false, timestamp);
				this.OnDataLinkReceived?.Invoke(actorFromIdentifier);
			}
		}
	}

	public AsyncOpStatus ShareGPSGroup(GPSTargetGroup group, Teams team)
	{
		AsyncOpStatus asyncOpStatus = new AsyncOpStatus();
		StartCoroutine(ShareGPSGroupRoutine(group, team, asyncOpStatus));
		return asyncOpStatus;
	}

	private IEnumerator ShareGPSGroupRoutine(GPSTargetGroup group, Teams team, AsyncOpStatus status)
	{
		Debug.Log("Sharing GPS group '" + group.groupName + "'");
		if (group.datalinkID <= 0)
		{
			Debug.Log(" - GPS group needs a new datalink ID.");
			if (base.isMine)
			{
				group.datalinkID = RPC_RequestGPSGroupID();
			}
			else
			{
				RPCRequest req = SendRPCRequest(typeof(int), VTOLMPLobbyManager.currentLobby.Owner.Id.Value, "RPC_RequestGPSGroupID");
				while (!req.isComplete)
				{
					yield return null;
				}
				group.datalinkID = (int)req.Value;
			}
			Debug.Log($" - GPS group '{group.groupName}' was assigned datalink ID {group.datalinkID}");
		}
		SendRPC("RPC_ClearGroup", BDSteamClient.mySteamID, group.datalinkID);
		for (int i = 0; i < group.targets.Count; i++)
		{
			SendGPSTarget(i, group.datalinkID, group.targets[i]);
		}
		Debug.Log($" - Done sharing GPS group, ID {group.datalinkID}");
		status.isDone = true;
	}

	private void SendGPSTarget(int index, int groupId, GPSTarget target)
	{
		ulong mySteamID = BDSteamClient.mySteamID;
		FloatingOrigin.WorldToNetPoint(target.worldPosition, out var nsv, out var offset);
		SendRPC("RPC_SendGPSTarget", mySteamID, groupId, index, offset, nsv);
	}

	[VTRPC]
	private void RPC_SendGPSTarget(ulong ownerId, int groupId, int index, Vector3 offset, int nsv)
	{
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(ownerId);
		if (player != null && player.team == VTOLMPLobbyManager.localPlayerInfo.team)
		{
			GPSTarget target = new GPSTarget(FloatingOrigin.NetToWorldPoint(offset, nsv), "DL ", index);
			Debug.Log($"Received a shared GPS target from {player.pilotName}, datalink ID {groupId}");
			this.OnReceivedGPSTarget?.Invoke(player, groupId, index, target);
		}
	}

	[VTRPC]
	private void RPC_ClearGroup(ulong ownerId, int groupId)
	{
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(ownerId);
		if (player != null && player.team == VTOLMPLobbyManager.localPlayerInfo.team)
		{
			this.OnClearedGPSGroup?.Invoke(player, groupId);
		}
	}

	[VTRPC]
	private int RPC_RequestGPSGroupID()
	{
		return nextGPSGroupId++;
	}
}

}