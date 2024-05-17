using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DashMissionDisplay : MonoBehaviour, IQSVehicleComponent, ILocalizationUser
{
	public Text missionText;

	public Text infoText;

	private int currIdx;

	private MissionObjective currObjective;

	private MissionObjective currentObjective;

	private Actor _a;

	private string s_noMissions;

	private string NODE_NAME = "DashMissionDisplay";

	private Actor actor
	{
		get
		{
			if (!_a)
			{
				_a = GetComponentInParent<Actor>();
				_a.OnSetTeam += Actor_OnSetTeam;
			}
			return _a;
		}
	}

	private void Awake()
	{
		ApplyLocalization();
	}

	public void ApplyLocalization()
	{
		s_noMissions = VTLocalizationManager.GetString("dashMissionDisplay_noMissions", "No objectives.", "Label in the mission objectives display when the current mission has no objectives.");
	}

	private void Start()
	{
		SetMission(MissionManager.instance.GetObjective(0, actor.team));
		MissionManager instance = MissionManager.instance;
		instance.OnMissionUpdated = (UnityAction)Delegate.Combine(instance.OnMissionUpdated, new UnityAction(OnMissionUpdated));
		MissionManager.instance.OnObjectiveRegistered += Instance_OnObjectiveRegistered;
	}

	private void Actor_OnSetTeam(Teams obj)
	{
		OnMissionUpdated();
	}

	private void Instance_OnObjectiveRegistered(MissionObjective objective)
	{
		if (objective.team == actor.team && (!currentObjective || currentObjective.objectiveFinished))
		{
			SetMission(objective);
		}
	}

	private void OnDestroy()
	{
		if ((bool)MissionManager.instance)
		{
			MissionManager instance = MissionManager.instance;
			instance.OnMissionUpdated = (UnityAction)Delegate.Remove(instance.OnMissionUpdated, new UnityAction(OnMissionUpdated));
			MissionManager.instance.OnObjectiveRegistered -= Instance_OnObjectiveRegistered;
		}
	}

	public void NextMission()
	{
		if (MissionManager.instance.ObjectiveCount(actor.team) > 0)
		{
			currIdx = (currIdx + 1) % MissionManager.instance.ObjectiveCount(actor.team);
			SetMission(MissionManager.instance.GetObjective(currIdx, actor.team));
		}
	}

	public void PrevMission()
	{
		if (MissionManager.instance.ObjectiveCount(actor.team) > 0)
		{
			currIdx--;
			if (currIdx < 0)
			{
				currIdx = MissionManager.instance.ObjectiveCount(actor.team) - 1;
			}
			SetMission(MissionManager.instance.GetObjective(currIdx, actor.team));
		}
	}

	private void SetMission(MissionObjective objective)
	{
		currObjective = objective;
		if (!objective)
		{
			missionText.text = string.Empty;
			infoText.text = s_noMissions;
			return;
		}
		if (!objective.objectiveFinished)
		{
			missionText.color = Color.white;
		}
		else if (objective.failed)
		{
			missionText.color = Color.red;
		}
		else
		{
			missionText.color = Color.green;
		}
		missionText.text = $"{MissionManager.instance.ObjectiveCount(actor.team) - currIdx}: {objective.GetObjectiveTitle()}";
		infoText.text = objective.info;
		currentObjective = objective;
	}

	private void OnMissionUpdated()
	{
		if (!currentObjective || currentObjective.objectiveFinished || currentObjective.team != actor.team)
		{
			for (int i = 0; i < MissionManager.instance.ObjectiveCount(actor.team); i++)
			{
				MissionObjective objective = MissionManager.instance.GetObjective(i, actor.team);
				if ((bool)objective && !objective.objectiveFinished)
				{
					currIdx = i;
					SetMission(objective);
					return;
				}
			}
		}
		if (MissionManager.instance.ObjectiveCount(actor.team) == 0)
		{
			currIdx = 0;
			SetMission(null);
		}
		else
		{
			currIdx = MissionManager.instance.IndexOfObjective(currentObjective, actor.team);
			SetMission(currentObjective);
		}
	}

	public void SetWaypoint()
	{
		if (!currentObjective)
		{
			SetMission(MissionManager.instance.GetObjective(currIdx, actor.team));
		}
		if ((bool)WaypointManager.instance && (bool)currentObjective)
		{
			if (currentObjective.waypoint != null)
			{
				WaypointManager.instance.SetWaypoint(currentObjective.waypoint);
			}
			else
			{
				WaypointManager.instance.currentWaypoint = currentObjective.waypointTransform;
			}
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode(NODE_NAME).SetValue("currIdx", currIdx);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(NODE_NAME);
		if (node != null)
		{
			currIdx = node.GetValue<int>("currIdx");
		}
	}
}
