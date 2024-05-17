using System.Collections.Generic;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTOLMPObjectivesManager : VTNetSyncRPCOnly
{
	private class ObjectiveOutcome
	{
		public int objectiveID;

		public bool success;
	}

	private Dictionary<int, ObjectiveOutcome> bufferedOutcomes = new Dictionary<int, ObjectiveOutcome>();

	public static VTOLMPObjectivesManager instance { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		instance = this;
	}

	private void Start()
	{
		MissionManager.instance.OnObjectiveRegistered += Instance_OnObjectiveRegistered;
	}

	private void Instance_OnObjectiveRegistered(MissionObjective mObj)
	{
		if (bufferedOutcomes.TryGetValue(mObj.objectiveID, out var value))
		{
			if (value.success)
			{
				RPC_CompleteObjective(mObj.objectiveID);
			}
			else
			{
				RPC_FailObjective(mObj.objectiveID);
			}
		}
	}

	public void NetCompleteObjective(int objectiveID)
	{
		if (base.isMine)
		{
			SendRPCBuffered("RPC_CompleteObjective", objectiveID);
		}
	}

	[VTRPC]
	private void RPC_CompleteObjective(int objectiveID)
	{
		Debug.Log($"RPC_CompleteObjective({objectiveID})");
		VTObjective objective = VTScenario.current.objectives.GetObjective(objectiveID);
		if (objective != null && (bool)objective.module.objectiveBehaviour)
		{
			objective.module.objectiveBehaviour.CompleteObjective(hostOnly: false);
			return;
		}
		Debug.LogError(" - objective not found. Storing the outcome.");
		bufferedOutcomes.Add(objectiveID, new ObjectiveOutcome
		{
			objectiveID = objectiveID,
			success = true
		});
	}

	public void NetFailObjective(int objectiveID)
	{
		if (base.isMine)
		{
			SendRPCBuffered("RPC_FailObjective", objectiveID);
		}
	}

	[VTRPC]
	private void RPC_FailObjective(int objectiveID)
	{
		Debug.Log($"RPC_FailObjective({objectiveID})");
		VTObjective objective = VTScenario.current.objectives.GetObjective(objectiveID);
		if (objective != null && (bool)objective.module.objectiveBehaviour)
		{
			objective.module.objectiveBehaviour.FailObjective(hostOnly: false);
			return;
		}
		Debug.LogError(" - objective not found. Storing the outcome.");
		bufferedOutcomes.Add(objectiveID, new ObjectiveOutcome
		{
			objectiveID = objectiveID,
			success = false
		});
	}

	public void NotifyHostSkippedMission()
	{
		if (base.isMine)
		{
			SendRPCBuffered("RPC_MissionSkipped");
		}
	}

	[VTRPC]
	private void RPC_MissionSkipped()
	{
		MissionManager.instance.SkipMission();
		if ((bool)VTOLMPLobbyManager.localPlayerInfo.vehicleObject)
		{
			FlightSceneManager.instance.ReturnToBriefingOrExitScene();
		}
		VTOLMPBriefingRoom.instance.UpdateButtonVisibility();
	}
}

}