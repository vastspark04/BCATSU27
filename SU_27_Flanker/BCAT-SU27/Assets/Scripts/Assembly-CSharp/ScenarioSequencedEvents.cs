using System.Collections.Generic;
using UnityEngine;

public class ScenarioSequencedEvents
{
	public delegate void EventFiredDelegate(int sequenceID, int eventIdx);

	private Dictionary<int, VTSequencedEvent> sequences = new Dictionary<int, VTSequencedEvent>();

	private const string NODE_NAME = "EventSequences";

	private const string S_NODE_NAME = "SEQUENCE";

	private int nextID;

	public event EventFiredDelegate OnFiredEvent;

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode configNode = scenarioNode.AddNode("EventSequences");
		foreach (VTSequencedEvent value in sequences.Values)
		{
			configNode.AddNode(value.SaveToConfigNode());
		}
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode node = scenarioNode.GetNode("EventSequences");
		if (node == null)
		{
			return;
		}
		foreach (ConfigNode node2 in node.GetNodes("SEQUENCE"))
		{
			VTSequencedEvent vTSequencedEvent = new VTSequencedEvent();
			vTSequencedEvent.LoadFromConfigNode(node2);
			sequences.Add(vTSequencedEvent.id, vTSequencedEvent);
			nextID = Mathf.Max(nextID, vTSequencedEvent.id + 1);
		}
	}

	public void BeginScenario()
	{
		if (QuicksaveManager.isQuickload)
		{
			return;
		}
		foreach (VTSequencedEvent value in sequences.Values)
		{
			if (value.startImmediately)
			{
				value.BeginEvent();
			}
		}
	}

	public void DestroyAll()
	{
		foreach (VTSequencedEvent value in sequences.Values)
		{
			value.Dispose();
		}
		sequences.Clear();
	}

	public VTSequencedEvent GetSequence(int id)
	{
		if (sequences.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public VTSequencedEvent CreateNewSequence()
	{
		VTSequencedEvent vTSequencedEvent = new VTSequencedEvent();
		vTSequencedEvent.id = nextID++;
		vTSequencedEvent.sequenceName = "New Event Sequence";
		sequences.Add(vTSequencedEvent.id, vTSequencedEvent);
		return vTSequencedEvent;
	}

	public void DeleteSequence(int id)
	{
		VTSequencedEvent sequence = GetSequence(id);
		if (sequence == null)
		{
			return;
		}
		foreach (VTSequencedEvent.EventNode eventNode in sequence.eventNodes)
		{
			if (eventNode.conditional != null)
			{
				VTScenario.current.conditionals.DeleteConditional(eventNode.conditional.id);
			}
			if (eventNode.eventInfo != null)
			{
				foreach (VTEventTarget action in eventNode.eventInfo.actions)
				{
					action?.DeleteEventTarget();
				}
			}
			if (eventNode.exitConditional != null)
			{
				VTScenario.current.conditionals.DeleteConditional(eventNode.exitConditional.id);
			}
		}
		sequences.Remove(id);
	}

	public List<VTSequencedEvent> GetAllSequences()
	{
		List<VTSequencedEvent> list = new List<VTSequencedEvent>();
		foreach (VTSequencedEvent value in sequences.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public ConfigNode QuickSaveToNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		foreach (VTSequencedEvent value in sequences.Values)
		{
			if (value.began)
			{
				value.QuicksaveToNode(configNode);
			}
		}
		return configNode;
	}

	public void QuickLoadFromNode(ConfigNode sseNode)
	{
		foreach (ConfigNode node in sseNode.GetNodes("SEQUENCE"))
		{
			int value = node.GetValue<int>("id");
			sequences[value].QuickloadFromNode(node);
		}
	}

	public void ResumeQuicksave()
	{
		foreach (VTSequencedEvent value in sequences.Values)
		{
			if (value.began)
			{
				value.QS_ResumeEvent();
			}
		}
	}

	public void ReportEventFired(int sequenceID, int eventIdx)
	{
		this.OnFiredEvent?.Invoke(sequenceID, eventIdx);
	}
}
