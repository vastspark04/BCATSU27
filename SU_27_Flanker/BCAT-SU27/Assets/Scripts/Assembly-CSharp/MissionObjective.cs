using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class MissionObjective : MonoBehaviour
{
	public string objectiveName;

	public int objectiveID;

	public bool beginOnStart = true;

	public bool isFinalMission;

	public float completionBudgetAward;

	public UnityEvent OnBegin;

	public UnityEvent OnComplete;

	public UnityEvent OnFail;

	public Teams team;

	public string info;

	public bool registerMission = true;

	public int priority;

	public Transform waypointTransform;

	public Waypoint waypoint;

	public bool autoSetWaypointOnStart;

	public bool required = true;

	private bool startedWithEvents;

	public bool objectiveFinished { get; private set; }

	public bool failed { get; private set; }

	public bool completed { get; private set; }

	public bool started { get; private set; }

	public bool cancelled { get; private set; }

	public bool isPlayersMission
	{
		get
		{
			if (!VTOLMPUtils.IsMultiplayer())
			{
				return team == Teams.Allied;
			}
			return team == VTOLMPLobbyManager.localPlayerInfo.team;
		}
	}

	protected string quicksaveNodeName => "objective_" + objectiveID;

	protected virtual void Awake()
	{
		QuicksaveManager.instance.OnQuicksave += Instance_OnQuicksave;
		completed = false;
		failed = false;
		objectiveFinished = false;
	}

	private void OnDestroy()
	{
		if ((bool)QuicksaveManager.instance)
		{
			QuicksaveManager.instance.OnQuicksave -= Instance_OnQuicksave;
			if ((bool)MissionManager.instance)
			{
				MissionManager.instance.OnBeginMissions -= BeginMission;
			}
		}
	}

	public void Quickload(ConfigNode configNode)
	{
		try
		{
			if (configNode.HasNode(quicksaveNodeName))
			{
				ConfigNode node = configNode.GetNode(quicksaveNodeName);
				started = ConfigNodeUtils.ParseBool(node.GetValue("started"));
				completed = ConfigNodeUtils.ParseBool(node.GetValue("completed"));
				failed = ConfigNodeUtils.ParseBool(node.GetValue("failed"));
				if (started)
				{
					BeginMissionNoEvents();
				}
				if (completed)
				{
					CompleteObjectiveNoEvents();
				}
				if (failed)
				{
					FailObjectiveNoEvents();
				}
				OnQuickload(node);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("MissionObjective " + objectiveName + " had an error quickloading!\n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
	}

	private void Instance_OnQuicksave(ConfigNode configNode)
	{
		try
		{
			ConfigNode configNode2 = new ConfigNode(quicksaveNodeName);
			configNode.AddNode(configNode2);
			configNode2.SetValue("started", started);
			configNode2.SetValue("completed", completed);
			configNode2.SetValue("failed", failed);
			OnQuicksave(configNode2);
		}
		catch (Exception ex)
		{
			Debug.LogError("MissionObjective " + objectiveName + " had an error quicksaving!\n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
	}

	protected virtual void OnQuicksave(ConfigNode objectiveNode)
	{
	}

	protected virtual void OnQuickload(ConfigNode objectiveNode)
	{
	}

	public virtual void Start()
	{
		if (required)
		{
			MissionManager.instance.RegisterRequiredObjective(this);
		}
		if (isFinalMission)
		{
			MissionManager.instance.RegisterFinalObjective(this);
		}
		else if (beginOnStart)
		{
			MissionManager.instance.OnBeginMissions += BeginMission;
		}
	}

	public void BeginMission()
	{
		if (started)
		{
			return;
		}
		started = true;
		startedWithEvents = true;
		if (registerMission)
		{
			MissionManager.instance.RegisterObjective(this);
		}
		if (OnBegin != null)
		{
			OnBegin.Invoke();
		}
		if (autoSetWaypointOnStart)
		{
			Teams teams = Teams.Allied;
			bool flag = true;
			if (VTOLMPUtils.IsMultiplayer())
			{
				teams = VTOLMPLobbyManager.localPlayerInfo.team;
				flag = VTOLMPLobbyManager.localPlayerInfo.chosenTeam;
				if (!flag)
				{
					StartCoroutine(SetWPOnceChosenTeam());
				}
			}
			if (flag && team == teams)
			{
				if (waypoint != null)
				{
					WaypointManager.instance.SetWaypoint(waypoint);
				}
				else
				{
					WaypointManager.instance.currentWaypoint = waypointTransform;
				}
			}
		}
		OnBeginMission();
	}

	private IEnumerator SetWPOnceChosenTeam()
	{
		PlayerInfo pInfo = VTOLMPLobbyManager.localPlayerInfo;
		while (!pInfo.chosenTeam)
		{
			yield return null;
		}
		if (!pInfo.chosenTeam || objectiveFinished || !autoSetWaypointOnStart)
		{
			yield break;
		}
		Teams teams = pInfo.team;
		if (team == teams)
		{
			if (waypoint != null)
			{
				WaypointManager.instance.SetWaypoint(waypoint);
			}
			else
			{
				WaypointManager.instance.currentWaypoint = waypointTransform;
			}
		}
	}

	private void BeginMissionNoEvents()
	{
		if (startedWithEvents)
		{
			Debug.LogError("Tried to start objective " + objectiveName + " without events, but it had already been started WITH events!", base.gameObject);
			return;
		}
		started = true;
		if (registerMission)
		{
			MissionManager.instance.RegisterObjective(this);
		}
		OnBeginMission();
	}

	public virtual void OnBeginMission()
	{
	}

	public void CompleteObjective(bool hostOnly = true)
	{
		if (!objectiveFinished && (!hostOnly || !VTOLMPUtils.IsMultiplayer() || VTOLMPLobbyManager.isLobbyHost))
		{
			objectiveFinished = true;
			completed = true;
			failed = false;
			if (OnComplete != null)
			{
				OnComplete.Invoke();
			}
			MissionManager.instance.CheckFinalMissionComplete();
			MissionManager.instance.UpdateMissions();
			FlightLogger.Log($"Completed objective: {objectiveName}");
			MissionManager.instance.FireMissionCompleted(this);
			if (VTOLMPUtils.IsMultiplayer() && VTOLMPLobbyManager.isLobbyHost)
			{
				VTOLMPObjectivesManager.instance.NetCompleteObjective(objectiveID);
			}
		}
	}

	private void CompleteObjectiveNoEvents()
	{
		objectiveFinished = true;
		completed = true;
		failed = false;
		MissionManager.instance.CheckFinalMissionComplete();
		MissionManager.instance.UpdateMissions();
		FlightLogger.Log($"Completed objective: {objectiveName}");
	}

	public void FailObjective(bool hostOnly = true)
	{
		if (!objectiveFinished && (!hostOnly || !VTOLMPUtils.IsMultiplayer() || VTOLMPLobbyManager.isLobbyHost))
		{
			objectiveFinished = true;
			completed = false;
			failed = true;
			if (OnFail != null)
			{
				OnFail.Invoke();
			}
			MissionManager.instance.CheckFinalMissionComplete();
			MissionManager.instance.UpdateMissions();
			FlightLogger.Log($"Failed objective: {objectiveName}");
			MissionManager.instance.FireMissionFailed(this);
			if (VTOLMPUtils.IsMultiplayer() && VTOLMPLobbyManager.isLobbyHost)
			{
				VTOLMPObjectivesManager.instance.NetFailObjective(objectiveID);
			}
		}
	}

	private void FailObjectiveNoEvents()
	{
		objectiveFinished = true;
		completed = false;
		failed = true;
		MissionManager.instance.CheckFinalMissionComplete();
		MissionManager.instance.UpdateMissions();
		FlightLogger.Log($"Failed objective: {objectiveName}");
	}

	public virtual string GetObjectiveTitle()
	{
		return objectiveName;
	}

	public virtual float GetCompletionAward()
	{
		if (completed)
		{
			return completionBudgetAward;
		}
		return 0f;
	}

	public void CancelObjective()
	{
		cancelled = true;
		OnCancelObjective();
		base.gameObject.SetActive(value: false);
	}

	protected virtual void OnCancelObjective()
	{
	}
}
