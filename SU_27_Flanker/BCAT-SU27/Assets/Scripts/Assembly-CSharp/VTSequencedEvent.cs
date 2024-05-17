using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class VTSequencedEvent
{
	public class EventNode
	{
		public string nodeName;

		public float delay;

		public ScenarioConditional conditional;

		public VTEventInfo eventInfo;

		public ScenarioConditional exitConditional;
	}

	public class VTSequencedEventBehaviour : MonoBehaviour
	{
		private VTSequencedEvent evt;

		private int startIdx;

		private float addElapsed;

		public void BeginEvent(VTSequencedEvent evt, int startIdx = 0, float addElapsed = 0f)
		{
			this.evt = evt;
			this.startIdx = startIdx;
			this.addElapsed = addElapsed;
			StartCoroutine(EventCoroutine());
		}

		private IEnumerator EventCoroutine()
		{
			if (!VTOLMPUtils.IsMultiplayer())
			{
				while (!PlayerSpawn.playerVehicleReady)
				{
					yield return null;
				}
			}
			else if (!VTScenario.isScenarioHost)
			{
				yield break;
			}
			if (QuicksaveManager.isQuickload)
			{
				if (startIdx > 0)
				{
					Debug.LogFormat("Quickloaded sequenced event '{0}' at idx {1}", evt.sequenceName, startIdx);
				}
				while (!QuicksaveManager.hasQuickloadedMissiles)
				{
					yield return null;
				}
			}
			Debug.LogFormat("Beginning event sequence {0}", evt.sequenceName);
			for (int i = startIdx; i < evt.eventNodes.Count; i++)
			{
				EventNode node = evt.eventNodes[i];
				evt.currNodeIdx = i;
				evt.timeNodeStarted = Time.time;
				float num = node.delay;
				if (i == startIdx)
				{
					evt.timeNodeStarted -= addElapsed;
					num -= addElapsed;
				}
				Debug.LogFormat("Beginning sequence node {0} : {1}, delay({2})", evt.sequenceName, node.nodeName, node.delay);
				yield return new WaitForSeconds(Mathf.Max(0f, num));
				if (node.conditional != null)
				{
					Debug.LogFormat("Sequence node {0} : {1} awaiting conditional.", evt.sequenceName, node.nodeName);
					while (!node.conditional.GetCondition())
					{
						yield return null;
					}
				}
				Debug.LogFormat("Firing actions for sequence node {0} : {1}", evt.sequenceName, node.nodeName);
				if (!evt.hasInvokedCurrentNodeEvents)
				{
					node.eventInfo.Invoke();
					evt.hasInvokedCurrentNodeEvents = true;
					if (VTScenario.isScenarioHost)
					{
						VTScenario.current.sequencedEvents.ReportEventFired(evt.id, i);
					}
				}
				yield return null;
				if (node.exitConditional != null)
				{
					Debug.LogFormat("Sequence node {0} : {1} awaiting EXIT conditional.", evt.sequenceName, node.nodeName);
					while (!node.exitConditional.GetCondition())
					{
						yield return null;
					}
				}
				evt.hasInvokedCurrentNodeEvents = false;
			}
			evt.stopped = true;
			Object.Destroy(base.gameObject);
		}
	}

	public int id;

	public string sequenceName;

	public bool startImmediately;

	public List<EventNode> eventNodes = new List<EventNode>();

	private int currNodeIdx;

	private float timeNodeStarted;

	private bool hasInvokedCurrentNodeEvents;

	private GameObject behaviourObject;

	private float qsResumeElapsedTime;

	public const string NODE_NAME = "SEQUENCE";

	public bool began { get; private set; }

	public bool stopped { get; private set; }

	public void QuicksaveToNode(ConfigNode parentNode)
	{
		ConfigNode configNode = parentNode.AddNode("SEQUENCE");
		configNode.SetValue("id", id);
		configNode.SetValue("stopped", stopped);
		configNode.SetValue("currNodeIdx", currNodeIdx);
		configNode.SetValue("nodeElapsedTime", Time.time - timeNodeStarted);
		configNode.SetValue("hasInvokedCurrentNodeEvents", hasInvokedCurrentNodeEvents);
	}

	public void QuickloadFromNode(ConfigNode sequenceNode)
	{
		began = true;
		stopped = sequenceNode.GetValue<bool>("stopped");
		if (!stopped)
		{
			currNodeIdx = sequenceNode.GetValue<int>("currNodeIdx");
			qsResumeElapsedTime = sequenceNode.GetValue<float>("nodeElapsedTime");
			hasInvokedCurrentNodeEvents = sequenceNode.GetValue<bool>("hasInvokedCurrentNodeEvents");
		}
	}

	public void QS_ResumeEvent()
	{
		if (began && !stopped)
		{
			began = true;
			behaviourObject = new GameObject(sequenceName);
			behaviourObject.AddComponent<VTSequencedEventBehaviour>().BeginEvent(this, currNodeIdx, qsResumeElapsedTime);
		}
	}

	[VTEvent("Begin", "Begin the event sequence if it has not already started.")]
	public void BeginEvent()
	{
		if (!began)
		{
			began = true;
			behaviourObject = new GameObject(sequenceName);
			behaviourObject.AddComponent<VTSequencedEventBehaviour>().BeginEvent(this);
		}
	}

	[VTEvent("Stop", "Stop the event sequence if it has begun, or prevent it from starting if it hasn't already.")]
	public void Stop()
	{
		began = true;
		stopped = true;
		Dispose();
	}

	public void Dispose()
	{
		if ((bool)behaviourObject)
		{
			Object.Destroy(behaviourObject);
		}
	}

	public ConfigNode SaveToConfigNode()
	{
		ConfigNode configNode = new ConfigNode("SEQUENCE");
		configNode.SetValue("id", id);
		configNode.SetValue("sequenceName", sequenceName);
		configNode.SetValue("startImmediately", startImmediately);
		for (int i = 0; i < eventNodes.Count; i++)
		{
			EventNode eventNode = eventNodes[i];
			ConfigNode configNode2 = new ConfigNode("EVENT");
			configNode2.AddNode(eventNode.eventInfo.SaveToNode("EventInfo"));
			if (eventNode.conditional != null)
			{
				configNode2.SetValue("conditional", eventNode.conditional.id);
			}
			configNode2.SetValue("delay", eventNode.delay);
			configNode2.SetValue("nodeName", eventNode.nodeName);
			if (eventNode.exitConditional != null)
			{
				configNode2.SetValue("exitConditional", eventNode.exitConditional.id);
			}
			configNode.AddNode(configNode2);
		}
		return configNode;
	}

	public void LoadFromConfigNode(ConfigNode sNode)
	{
		id = sNode.GetValue<int>("id");
		sequenceName = sNode.GetValue("sequenceName");
		startImmediately = sNode.GetValue<bool>("startImmediately");
		foreach (ConfigNode node in sNode.GetNodes("EVENT"))
		{
			EventNode eventNode = new EventNode();
			if (node.HasValue("conditional"))
			{
				eventNode.conditional = VTScenario.current.conditionals.GetConditional(node.GetValue<int>("conditional"));
			}
			eventNode.delay = node.GetValue<float>("delay");
			eventNode.nodeName = node.GetValue("nodeName");
			eventNode.eventInfo = new VTEventInfo();
			eventNode.eventInfo.LoadFromInfoNode(node.GetNode("EventInfo"));
			if (node.HasValue("exitConditional"))
			{
				eventNode.exitConditional = VTScenario.current.conditionals.GetConditional(node.GetValue<int>("exitConditional"));
			}
			eventNodes.Add(eventNode);
		}
	}
}
