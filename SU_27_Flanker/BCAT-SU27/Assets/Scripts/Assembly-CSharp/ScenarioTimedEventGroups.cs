using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioTimedEventGroups
{
	public delegate void EventFiredDelegate(int groupId, int eventIdx);

	private int nextID;

	private Dictionary<int, VTTimedEventGroup> groups;

	public const string NODE_NAME = "TimedEventGroups";

	public event EventFiredDelegate OnFiredEventActions;

	public ScenarioTimedEventGroups()
	{
		groups = new Dictionary<int, VTTimedEventGroup>();
	}

	public void AddGroup(VTTimedEventGroup evtGroup)
	{
		evtGroup.groupID = nextID;
		nextID++;
		groups.Add(evtGroup.groupID, evtGroup);
	}

	public List<VTTimedEventGroup> GetAllGroups()
	{
		List<VTTimedEventGroup> list = new List<VTTimedEventGroup>();
		foreach (VTTimedEventGroup value in groups.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public VTTimedEventGroup GetGroup(int id)
	{
		if (groups.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public void RemoveGroup(VTTimedEventGroup evtGroup)
	{
		foreach (VTTimedEventInfo timedEvent in evtGroup.timedEvents)
		{
			foreach (VTEventTarget action in timedEvent.actions)
			{
				action?.DeleteEventTarget();
			}
		}
		groups.Remove(evtGroup.groupID);
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		if (!scenarioNode.HasNode("TimedEventGroups"))
		{
			return;
		}
		foreach (ConfigNode node in scenarioNode.GetNode("TimedEventGroups").GetNodes("TimedEventGroup"))
		{
			VTTimedEventGroup vTTimedEventGroup = new VTTimedEventGroup();
			vTTimedEventGroup.LoadFromNode(node);
			groups.Add(vTTimedEventGroup.groupID, vTTimedEventGroup);
			nextID = Mathf.Max(nextID, vTTimedEventGroup.groupID + 1);
		}
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode node = new ConfigNode("TimedEventGroups");
		foreach (VTTimedEventGroup value in groups.Values)
		{
			try
			{
				value.SaveToNode(node);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		scenarioNode.AddNode(node);
	}

	public void DestroyAll()
	{
		foreach (VTTimedEventGroup value in groups.Values)
		{
			value.DestroyObjects();
		}
	}

	public void ReportEventFired(int groupId, int eventIdx)
	{
		this.OnFiredEventActions?.Invoke(groupId, eventIdx);
	}
}
