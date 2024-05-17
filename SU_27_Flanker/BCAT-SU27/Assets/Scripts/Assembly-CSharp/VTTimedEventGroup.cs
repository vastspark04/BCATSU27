using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VTTimedEventGroup
{
	public string groupName;

	public int groupID;

	public bool beginImmediately;

	public float initialDelay;

	public List<VTTimedEventInfo> timedEvents = new List<VTTimedEventInfo>();

	private TimedEvents runtimeTimedEvents;

	public const string NODE_NAME = "TimedEventGroup";

	public void DestroyObjects()
	{
		if ((bool)runtimeTimedEvents)
		{
			Object.Destroy(runtimeTimedEvents.gameObject);
		}
	}

	public void SaveToNode(ConfigNode node)
	{
		ConfigNode configNode = new ConfigNode("TimedEventGroup");
		configNode.SetValue("groupName", groupName);
		configNode.SetValue("groupID", groupID);
		configNode.SetValue("beginImmediately", beginImmediately);
		configNode.SetValue("initialDelay", initialDelay);
		foreach (VTTimedEventInfo timedEvent in timedEvents)
		{
			timedEvent.SaveToNode(configNode);
		}
		node.AddNode(configNode);
	}

	public void LoadFromNode(ConfigNode groupNode)
	{
		groupName = groupNode.GetValue("groupName");
		groupID = ConfigNodeUtils.ParseInt(groupNode.GetValue("groupID"));
		beginImmediately = ConfigNodeUtils.ParseBool(groupNode.GetValue("beginImmediately"));
		initialDelay = ConfigNodeUtils.ParseFloat(groupNode.GetValue("initialDelay"));
		timedEvents = new List<VTTimedEventInfo>();
		foreach (ConfigNode node in groupNode.GetNodes("TimedEventInfo"))
		{
			VTTimedEventInfo vTTimedEventInfo = new VTTimedEventInfo();
			vTTimedEventInfo.LoadFromInfoNode(node);
			timedEvents.Add(vTTimedEventInfo);
		}
	}

	[VTEvent("Begin", "Begin the sequence of timed events if it hasn't already begun.")]
	public void Begin()
	{
		if (VTScenario.isScenarioHost)
		{
			if ((bool)runtimeTimedEvents)
			{
				runtimeTimedEvents.StartTimedEvents();
			}
			else
			{
				Debug.LogError("Attempted to begin a timed event group but the TimedEvents object doesn't exist!");
			}
		}
	}

	[VTEvent("Stop", "Stop the timed event sequence if it has started.")]
	public void Stop()
	{
		if (VTScenario.isScenarioHost && (bool)runtimeTimedEvents)
		{
			runtimeTimedEvents.StopTimedEvents();
		}
	}

	public void BeginScenario()
	{
		if (!VTScenario.isScenarioHost)
		{
			return;
		}
		runtimeTimedEvents = new GameObject(groupName + groupID).AddComponent<TimedEvents>();
		runtimeTimedEvents.scenarioTimedEventId = groupID;
		runtimeTimedEvents.initialDelay = initialDelay;
		runtimeTimedEvents.events = new List<TimedEvent>();
		runtimeTimedEvents.startImmediately = beginImmediately;
		foreach (VTTimedEventInfo timedEvent2 in timedEvents)
		{
			TimedEvent timedEvent = new TimedEvent();
			timedEvent.time = timedEvent2.time;
			timedEvent.name = timedEvent2.eventName;
			timedEvent.OnFireEvent = new UnityEvent();
			foreach (VTEventTarget action in timedEvent2.actions)
			{
				if (action == null)
				{
					Debug.Log("action is null");
				}
				if (timedEvent.OnFireEvent == null)
				{
					Debug.Log("event is null");
				}
				timedEvent.OnFireEvent.AddListener(action.Invoke);
			}
			runtimeTimedEvents.events.Add(timedEvent);
		}
	}

	public void RemoteFireEvent(int eventIdx)
	{
		List<VTEventTarget> actions = timedEvents[eventIdx].actions;
		for (int i = 0; i < actions.Count; i++)
		{
			actions[i].Invoke();
		}
	}
}
