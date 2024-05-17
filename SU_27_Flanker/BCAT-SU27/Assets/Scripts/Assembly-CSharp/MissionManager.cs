using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class MissionManager : MonoBehaviour
{
	public enum FinalWinner
	{
		None,
		Allied,
		Enemy
	}

	private List<MissionObjective> objectives = new List<MissionObjective>();

	private List<MissionObjective> opforObjectives = new List<MissionObjective>();

	public UnityAction OnMissionUpdated;

	private MissionObjective finalObjective;

	private List<MissionObjective> requiredObjectives = new List<MissionObjective>();

	private List<MissionObjective> requiredOpforObjectives = new List<MissionObjective>();

	private string QS_IDLIST_NAME = "MissionManager_IDList";

	private float metOfLastRequiredMissionCleared;

	private Coroutine finalCheckRoutine;

	private FinalWinner _finalWinner;

	public static MissionManager instance { get; private set; }

	public FinalWinner finalWinner => _finalWinner;

	public event UnityAction OnBeginMissions;

	public event UnityAction<MissionObjective> OnObjectiveRegistered;

	public event UnityAction<MissionObjective> OnObjectiveCompleted;

	public event UnityAction<MissionObjective> OnObjectiveFailed;

	public int ObjectiveCount(Teams team)
	{
		if (team != 0)
		{
			return opforObjectives.Count;
		}
		return objectives.Count;
	}

	private void Awake()
	{
		instance = this;
	}

	private void Instance_OnQuicksave(ConfigNode configNode)
	{
		ConfigNode configNode2 = configNode.AddNode("MissionManager");
		List<int> list = new List<int>(objectives.Count);
		for (int i = 0; i < objectives.Count; i++)
		{
			list.Add(objectives[i].objectiveID);
		}
		configNode2.SetValue(QS_IDLIST_NAME, ConfigNodeUtils.WriteList(list));
		configNode2.SetValue("metOfLastRequiredMissionCleared", metOfLastRequiredMissionCleared);
		configNode2.SetValue("finalWinner", _finalWinner);
	}

	private void Instance_OnQuickloadLate(ConfigNode configNode)
	{
		ConfigNode node = configNode.GetNode("MissionManager");
		if (node == null)
		{
			return;
		}
		if (node.HasValue(QS_IDLIST_NAME))
		{
			List<int> list = ConfigNodeUtils.ParseList<int>(node.GetValue(QS_IDLIST_NAME));
			List<MissionObjective> list2 = objectives.Copy();
			objectives.Clear();
			for (int i = 0; i < list.Count; i++)
			{
				foreach (MissionObjective item in list2)
				{
					if (item.objectiveID == list[i])
					{
						objectives.Add(item);
						break;
					}
				}
			}
			OnMissionUpdated?.Invoke();
		}
		metOfLastRequiredMissionCleared = node.GetValue<float>("metOfLastRequiredMissionCleared");
		_finalWinner = node.GetValue<FinalWinner>("finalWinner");
	}

	private void Start()
	{
		QuicksaveManager.instance.OnQuicksave += Instance_OnQuicksave;
		QuicksaveManager.instance.OnQuickloadLate += Instance_OnQuickloadLate;
		StartCoroutine(StartupRoutine());
	}

	public float GetMissionCompletionElapsedTime()
	{
		if (_finalWinner != 0)
		{
			return metOfLastRequiredMissionCleared;
		}
		return -1f;
	}

	public void CancelObjective(MissionObjective o)
	{
		o.CancelObjective();
		if (o.required)
		{
			requiredObjectives.Remove(o);
			requiredOpforObjectives.Remove(o);
			metOfLastRequiredMissionCleared = FlightSceneManager.instance.missionElapsedTime;
		}
		objectives.Remove(o);
		opforObjectives.Remove(o);
		CheckFinalMissionComplete();
		UpdateMissions();
	}

	public void ClearMissionsForRestart()
	{
		objectives.Clear();
		opforObjectives.Clear();
		requiredObjectives.Clear();
		requiredOpforObjectives.Clear();
		finalObjective = null;
		_finalWinner = FinalWinner.None;
		if (finalCheckRoutine != null)
		{
			StopCoroutine(finalCheckRoutine);
		}
	}

	public void RestartMissions()
	{
		StartCoroutine(StartupRoutine());
	}

	private IEnumerator StartupRoutine()
	{
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		yield return null;
		yield return null;
		if (this.OnBeginMissions != null)
		{
			this.OnBeginMissions();
		}
	}

	public void FireMissionCompleted(MissionObjective m)
	{
		if (m.required)
		{
			metOfLastRequiredMissionCleared = FlightSceneManager.instance.missionElapsedTime;
		}
		if (this.OnObjectiveCompleted != null)
		{
			this.OnObjectiveCompleted(m);
		}
	}

	public void FireMissionFailed(MissionObjective m)
	{
		if (this.OnObjectiveFailed != null)
		{
			this.OnObjectiveFailed(m);
		}
	}

	public void RegisterObjective(MissionObjective objective)
	{
		List<MissionObjective> list = ((objective.team == Teams.Allied) ? objectives : opforObjectives);
		if (list.Contains(objective))
		{
			return;
		}
		if (list.Count == 0)
		{
			list.Add(objective);
		}
		else
		{
			bool flag = false;
			for (int i = 0; i < list.Count; i++)
			{
				if (flag)
				{
					break;
				}
				if (list[i].priority <= objective.priority)
				{
					list.Insert(i, objective);
					flag = true;
				}
			}
			if (!flag)
			{
				list.Add(objective);
			}
		}
		UpdateMissions();
		if (this.OnObjectiveRegistered != null)
		{
			this.OnObjectiveRegistered(objective);
		}
	}

	public void RegisterFinalObjective(MissionObjective finalMission)
	{
		if (!finalObjective)
		{
			Debug.Log("Registering final mission: " + finalMission.name);
			finalObjective = finalMission;
		}
		else
		{
			Debug.LogWarning("Trying to register final mission (" + finalMission.gameObject.name + ") when one already exists (" + finalObjective.gameObject.name);
		}
	}

	public void RegisterRequiredObjective(MissionObjective obj)
	{
		if (obj.team == Teams.Allied)
		{
			if (!requiredObjectives.Contains(obj))
			{
				requiredObjectives.Add(obj);
			}
		}
		else if (!requiredOpforObjectives.Contains(obj))
		{
			requiredOpforObjectives.Add(obj);
		}
	}

	public float GetMissionBudgetAwards()
	{
		float num = 0f;
		foreach (MissionObjective objective in objectives)
		{
			num += objective.GetCompletionAward();
		}
		return num;
	}

	public MissionObjective GetObjective(int idx, Teams team)
	{
		List<MissionObjective> list = ((team == Teams.Allied) ? objectives : opforObjectives);
		if (list.Count == 0)
		{
			return null;
		}
		return list[idx];
	}

	public int IndexOfObjective(MissionObjective o, Teams team)
	{
		return ((team == Teams.Allied) ? objectives : opforObjectives).IndexOf(o);
	}

	public void UpdateMissions()
	{
		if (OnMissionUpdated != null)
		{
			OnMissionUpdated();
		}
	}

	public void CheckFinalMissionComplete()
	{
		if (finalCheckRoutine != null)
		{
			StopCoroutine(finalCheckRoutine);
		}
		finalCheckRoutine = StartCoroutine(FinalCheckRoutine());
	}

	private IEnumerator FinalCheckRoutine()
	{
		yield return new WaitForSeconds(1f);
		Debug.Log("Final Mission Completion Check Routine");
		if (_finalWinner == FinalWinner.None)
		{
			if (requiredObjectives.Count > 0)
			{
				bool flag = true;
				requiredObjectives.RemoveAll((MissionObjective o) => o == null);
				foreach (MissionObjective requiredObjective in requiredObjectives)
				{
					Debug.Log(" - Checking objective: " + requiredObjective.objectiveName);
					if (!requiredObjective.objectiveFinished && !requiredObjective.isFinalMission && requiredObjective.registerMission)
					{
						Debug.Log(" - - not finished.");
						flag = false;
					}
					else if (requiredObjective.failed)
					{
						Debug.Log(" - - objective failed!");
						flag = false;
						_finalWinner = FinalWinner.Enemy;
						break;
					}
				}
				if (flag)
				{
					_finalWinner = FinalWinner.Allied;
				}
			}
			if (_finalWinner == FinalWinner.None && requiredOpforObjectives.Count > 0)
			{
				bool flag2 = true;
				requiredOpforObjectives.RemoveAll((MissionObjective o) => o == null);
				foreach (MissionObjective requiredOpforObjective in requiredOpforObjectives)
				{
					if (!requiredOpforObjective.objectiveFinished && !requiredOpforObjective.isFinalMission && requiredOpforObjective.registerMission)
					{
						flag2 = false;
					}
					else if (requiredOpforObjective.failed)
					{
						flag2 = false;
						_finalWinner = FinalWinner.Allied;
						break;
					}
				}
				if (flag2)
				{
					_finalWinner = FinalWinner.Enemy;
				}
			}
		}
		if (_finalWinner != 0 && (bool)finalObjective && !finalObjective.objectiveFinished)
		{
			Debug.Log("All primary missions complete.  Starting final mission.");
			finalObjective.BeginMission();
			_finalWinner = FinalWinner.None;
		}
		else if (_finalWinner != 0)
		{
			Debug.Log($"Endmission - Final Winner: {_finalWinner}");
			EndMission.SetFinalWinner((_finalWinner != FinalWinner.Allied) ? Teams.Enemy : Teams.Allied);
		}
	}

	public void SkipMission()
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			Debug.Log("Host is skipping the mission.");
			_finalWinner = FinalWinner.Allied;
			EndMission.SetFinalWinner(Teams.Allied);
		}
	}
}
