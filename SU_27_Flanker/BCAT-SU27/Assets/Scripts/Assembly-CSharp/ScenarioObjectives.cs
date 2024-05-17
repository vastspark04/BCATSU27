using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioObjectives
{
	public const string NODE_NAME = "OBJECTIVES";

	public const string NODE_NAME_OPFOR = "OBJECTIVES_OPFOR";

	private int nextID;

	private Dictionary<int, VTObjective> objectives;

	private Dictionary<int, VTObjective> opforObjectives;

	public ScenarioObjectives()
	{
		objectives = new Dictionary<int, VTObjective>();
		opforObjectives = new Dictionary<int, VTObjective>();
	}

	public void AddObjective(VTObjective o, Teams team)
	{
		o.team = team;
		if (team == Teams.Enemy)
		{
			opforObjectives.Add(o.objectiveID, o);
		}
		else
		{
			objectives.Add(o.objectiveID, o);
		}
	}

	public void RemoveObjective(VTObjective o)
	{
		if (!objectives.ContainsKey(o.objectiveID) && !opforObjectives.ContainsKey(o.objectiveID))
		{
			return;
		}
		if (o.startEvent != null)
		{
			foreach (VTEventTarget action in o.startEvent.actions)
			{
				action?.DeleteEventTarget();
			}
		}
		if (o.completeEvent != null)
		{
			foreach (VTEventTarget action2 in o.completeEvent.actions)
			{
				action2?.DeleteEventTarget();
			}
		}
		if (o.failedEvent != null)
		{
			foreach (VTEventTarget action3 in o.failedEvent.actions)
			{
				action3?.DeleteEventTarget();
			}
		}
		objectives.Remove(o.objectiveID);
		opforObjectives.Remove(o.objectiveID);
	}

	public VTObjective GetObjective(int id)
	{
		if (objectives.TryGetValue(id, out var value))
		{
			return value;
		}
		if (opforObjectives.TryGetValue(id, out value))
		{
			return value;
		}
		return null;
	}

	public List<VTObjective> GetObjectives(Teams team)
	{
		List<VTObjective> list = new List<VTObjective>();
		if (team == Teams.Allied)
		{
			foreach (VTObjective value in objectives.Values)
			{
				list.Add(value);
			}
			return list;
		}
		foreach (VTObjective value2 in opforObjectives.Values)
		{
			list.Add(value2);
		}
		return list;
	}

	public List<VTObjective> GetBothTeamObjectives()
	{
		List<VTObjective> list = new List<VTObjective>();
		foreach (VTObjective value in objectives.Values)
		{
			list.Add(value);
		}
		foreach (VTObjective value2 in opforObjectives.Values)
		{
			list.Add(value2);
		}
		return list;
	}

	public int GetObjectiveCount(Teams team)
	{
		return ((team == Teams.Allied) ? objectives : opforObjectives).Count;
	}

	public int RequestNewID()
	{
		int result = nextID;
		nextID++;
		return result;
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		nextID = 0;
		if (scenarioNode.HasNode("OBJECTIVES"))
		{
			foreach (ConfigNode node in scenarioNode.GetNode("OBJECTIVES").GetNodes("Objective"))
			{
				VTObjective vTObjective = new VTObjective();
				vTObjective.LoadFromNode(node);
				AddObjective(vTObjective, Teams.Allied);
				nextID = Mathf.Max(nextID, vTObjective.objectiveID + 1);
			}
			List<VTObjective> list = GetObjectives(Teams.Allied);
			list.Sort((VTObjective a, VTObjective b) => a.orderID.CompareTo(b.orderID));
			for (int i = 0; i < list.Count; i++)
			{
				list[i].orderID = i;
			}
		}
		if (!scenarioNode.HasNode("OBJECTIVES_OPFOR"))
		{
			return;
		}
		foreach (ConfigNode node2 in scenarioNode.GetNode("OBJECTIVES_OPFOR").GetNodes("Objective"))
		{
			VTObjective vTObjective2 = new VTObjective();
			vTObjective2.LoadFromNode(node2);
			AddObjective(vTObjective2, Teams.Enemy);
			nextID = Mathf.Max(nextID, vTObjective2.objectiveID + 1);
		}
		List<VTObjective> list2 = GetObjectives(Teams.Enemy);
		list2.Sort((VTObjective a, VTObjective b) => a.orderID.CompareTo(b.orderID));
		for (int j = 0; j < list2.Count; j++)
		{
			list2[j].orderID = j;
		}
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode node = new ConfigNode("OBJECTIVES");
		foreach (VTObjective value in objectives.Values)
		{
			try
			{
				value.SaveToParentNode(node);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		scenarioNode.AddNode(node);
		ConfigNode node2 = new ConfigNode("OBJECTIVES_OPFOR");
		foreach (VTObjective value2 in opforObjectives.Values)
		{
			try
			{
				value2.SaveToParentNode(node2);
			}
			catch (Exception message2)
			{
				Debug.LogError(message2);
			}
		}
		scenarioNode.AddNode(node2);
	}

	public void DestroyAll()
	{
		if (objectives != null)
		{
			foreach (VTObjective value in objectives.Values)
			{
				value?.Dispose();
			}
		}
		if (opforObjectives == null)
		{
			return;
		}
		foreach (VTObjective value2 in opforObjectives.Values)
		{
			value2?.Dispose();
		}
	}

	public void QuickloadObjectives(ConfigNode qsNode)
	{
		foreach (VTObjective value in objectives.Values)
		{
			string name = "objective_" + value.objectiveID;
			ConfigNode node = qsNode.GetNode(name);
			if (node != null)
			{
				Debug.Log("Quickloading objective " + value.objectiveName + "[" + value.objectiveID + "]");
				if (node.GetValue<bool>("started"))
				{
					Debug.Log(" - started! quickloading from node");
					value.module.SetupObjective();
					value.module.objectiveBehaviour.Quickload(qsNode);
				}
			}
		}
	}
}
