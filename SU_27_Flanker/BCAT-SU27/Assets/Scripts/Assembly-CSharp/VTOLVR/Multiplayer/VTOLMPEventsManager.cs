using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTOLMPEventsManager : VTNetSyncRPCOnly
{
	private struct FiredFlare
	{
		public float duration;

		public int color;

		public Vector3 globalPosition;

		public float timestamp;
	}

	private List<FiredFlare> firedFlares = new List<FiredFlare>();

	public static VTOLMPEventsManager instance { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		instance = this;
	}

	private void OnEnable()
	{
		StartCoroutine(EnableRoutine());
	}

	private IEnumerator EnableRoutine()
	{
		while (!wasRegistered)
		{
			yield return null;
		}
		while (VTScenario.current == null)
		{
			yield return null;
		}
		if (base.isMine)
		{
			VTScenario.current.timedEventGroups.OnFiredEventActions += TimedEventGroups_OnFiredEventActions;
			VTScenario.current.triggerEvents.OnEventFired += TriggerEvents_OnEventFired;
			VTScenario.current.sequencedEvents.OnFiredEvent += SequencedEvents_OnFiredEvent;
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
			SmokeFlare.OnFiredFlare += SmokeFlare_OnFiredFlare;
		}
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		foreach (FiredFlare firedFlare in firedFlares)
		{
			SendDirectedRPC(obj, "RPC_SmokeFlare", firedFlare.duration, firedFlare.color, firedFlare.globalPosition, firedFlare.timestamp, obj.Value);
		}
	}

	private void SmokeFlare_OnFiredFlare(float duration, SmokeFlare.FlareColors color, Vector3D globalPosition, float timestamp)
	{
		Vector3 toVector = globalPosition.toVector3;
		SendRPC("RPC_SmokeFlare", duration, (int)color, toVector, timestamp, 0uL);
		FiredFlare firedFlare = default(FiredFlare);
		firedFlare.duration = duration;
		firedFlare.color = (int)color;
		firedFlare.globalPosition = toVector;
		firedFlare.timestamp = timestamp;
		FiredFlare item = firedFlare;
		firedFlares.Add(item);
	}

	[VTRPC]
	private void RPC_SmokeFlare(float duration, int color, Vector3 globalPosition, float timestamp, ulong forUserId)
	{
		if (forUserId == 0L || forUserId == BDSteamClient.mySteamID)
		{
			float num = duration - (VTNetworkManager.GetNetworkTimestamp() - timestamp);
			if (num > 0f)
			{
				SmokeFlare.IgniteFlare(num, (SmokeFlare.FlareColors)color, VTMapManager.GlobalToWorldPoint(new Vector3D(globalPosition)), remote: true);
			}
		}
	}

	private void OnDisable()
	{
		SmokeFlare.OnFiredFlare -= SmokeFlare_OnFiredFlare;
		if ((bool)VTNetworkManager.instance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void SequencedEvents_OnFiredEvent(int sequenceID, int eventIdx)
	{
		SendRPC("RPC_SeqEvt", sequenceID, eventIdx);
	}

	[VTRPC]
	private void RPC_SeqEvt(int sequenceID, int eventIdx)
	{
		VTScenario.current.sequencedEvents.GetSequence(sequenceID).eventNodes[eventIdx].eventInfo.Invoke();
	}

	private void TriggerEvents_OnEventFired(int eventID)
	{
		SendRPC("RPC_TrigEvt", eventID);
	}

	[VTRPC]
	private void RPC_TrigEvt(int eventID)
	{
		VTScenario.current.triggerEvents.GetEvent(eventID).RemoteTrigger();
	}

	private void TimedEventGroups_OnFiredEventActions(int groupId, int eventIdx)
	{
		SendRPC("RPC_TimedEvt", groupId, eventIdx);
	}

	[VTRPC]
	private void RPC_TimedEvt(int groupId, int eventIdx)
	{
		VTScenario.current.timedEventGroups.GetGroup(groupId).RemoteFireEvent(eventIdx);
	}

	public void ReportObjectiveStart(int objectiveID)
	{
		if (VTScenario.isScenarioHost)
		{
			SendRPC("RPC_ObjectiveEvent", objectiveID, 0);
		}
	}

	public void ReportObjectiveComplete(int objectiveID)
	{
		if (VTScenario.isScenarioHost)
		{
			SendRPC("RPC_ObjectiveEvent", objectiveID, 1);
		}
	}

	public void ReportObjectiveFail(int objectiveID)
	{
		if (VTScenario.isScenarioHost)
		{
			SendRPC("RPC_ObjectiveEvent", objectiveID, 2);
		}
	}

	[VTRPC]
	private void RPC_ObjectiveEvent(int objectiveID, int eventT)
	{
		Debug.Log($"RPC_ObjectiveEvent({objectiveID}, {eventT})");
		VTObjective objective = VTScenario.current.objectives.GetObjective(objectiveID);
		VTEventInfo vTEventInfo = null;
		switch (eventT)
		{
		case 0:
			vTEventInfo = objective.startEvent;
			break;
		case 1:
			vTEventInfo = objective.completeEvent;
			break;
		case 2:
			vTEventInfo = objective.failedEvent;
			break;
		}
		vTEventInfo?.Invoke();
	}
}

}