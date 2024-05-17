using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioTriggerEvents
{
	public delegate void EventFiredDelegate(int eventID);

	public class TriggerEvent
	{
		public enum TriggerTypes
		{
			Proximity,
			Conditional
		}

		public const string NODE_NAME = "TriggerEvent";

		public int id;

		public string eventName = "New Trigger Event";

		public bool enabled = true;

		public TriggerTypes triggerType;

		public Waypoint waypoint;

		public float radius = 500f;

		public bool sphericalRadius;

		public TriggerEventModes triggerMode;

		public UnitReference unit;

		public TriggerProximityModes proxyMode;

		public ScenarioConditional conditional;

		public VTEventInfo eventInfo = new VTEventInfo();

		public VTSTriggerEventBehaviour behaviour;

		[VTEvent("Enable", "Enable the event so it can be triggered.")]
		public void Enable()
		{
			enabled = true;
			if ((bool)behaviour)
			{
				behaviour.Enable();
			}
		}

		[VTEvent("Disable", "Disable the event so it can not be triggered.")]
		public void Disable()
		{
			enabled = false;
			if ((bool)behaviour)
			{
				behaviour.Disable();
			}
		}

		[VTEvent("Trigger", "Trigger the event regardless of its state.")]
		public void Trigger()
		{
			if ((bool)behaviour)
			{
				behaviour.Trigger();
			}
		}

		public void RemoteTrigger()
		{
			Debug.Log("Remote Trigger event: " + eventName);
			if ((bool)behaviour)
			{
				behaviour.Trigger(remoteTriggered: true);
			}
			else
			{
				Debug.Log(" - no behaviour found.");
			}
		}
	}

	public const string NODE_NAME = "TRIGGER_EVENTS";

	public List<TriggerEvent> events = new List<TriggerEvent>();

	private Dictionary<int, TriggerEvent> eventDict = new Dictionary<int, TriggerEvent>();

	private int nextID;

	public event EventFiredDelegate OnEventFired;

	public TriggerEvent GetEvent(int id)
	{
		if (eventDict.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public TriggerEvent AddNewEvent()
	{
		TriggerEvent triggerEvent = new TriggerEvent();
		triggerEvent.id = nextID;
		nextID++;
		events.Add(triggerEvent);
		eventDict.Add(triggerEvent.id, triggerEvent);
		return triggerEvent;
	}

	public void DeleteEvent(int id)
	{
		if (eventDict.TryGetValue(id, out var value))
		{
			eventDict.Remove(id);
			events.Remove(value);
		}
	}

	public void DestroyAll()
	{
		foreach (TriggerEvent @event in events)
		{
			if (@event.triggerType == TriggerEvent.TriggerTypes.Conditional && @event.behaviour != null)
			{
				UnityEngine.Object.Destroy(@event.behaviour.gameObject);
			}
		}
	}

	public void ReportEventFired(int eventID)
	{
		this.OnEventFired?.Invoke(eventID);
	}

	public void BeginScenario()
	{
		foreach (TriggerEvent @event in events)
		{
			if (@event.triggerType == TriggerEvent.TriggerTypes.Conditional)
			{
				new GameObject(@event.eventName + "Conditional").AddComponent<VTSTriggerEventBehaviour>().Initialize(@event);
			}
			else if (@event.waypoint != null && @event.waypoint.GetTransform() != null)
			{
				@event.waypoint.GetTransform().gameObject.AddComponent<VTSTriggerEventBehaviour>().Initialize(@event);
			}
			else
			{
				Debug.Log("Trigger event '" + @event.eventName + "' doesn't have a waypoint set.");
			}
		}
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		if (!scenarioNode.HasNode("TRIGGER_EVENTS"))
		{
			return;
		}
		foreach (ConfigNode node in scenarioNode.GetNode("TRIGGER_EVENTS").GetNodes("TriggerEvent"))
		{
			TriggerEvent triggerEvent = new TriggerEvent();
			ConfigNodeUtils.TryParseValue(node, "id", ref triggerEvent.id);
			ConfigNodeUtils.TryParseValue(node, "enabled", ref triggerEvent.enabled);
			ConfigNodeUtils.TryParseValue(node, "triggerType", ref triggerEvent.triggerType);
			if (triggerEvent.triggerType == TriggerEvent.TriggerTypes.Proximity)
			{
				triggerEvent.waypoint = VTSConfigUtils.ParseObject<Waypoint>(node.GetValue("waypoint"));
				ConfigNodeUtils.TryParseValue(node, "radius", ref triggerEvent.radius);
				ConfigNodeUtils.TryParseValue(node, "sphericalRadius", ref triggerEvent.sphericalRadius);
				ConfigNodeUtils.TryParseValue(node, "triggerMode", ref triggerEvent.triggerMode);
				if (node.HasValue("unit"))
				{
					triggerEvent.unit = VTSConfigUtils.ParseObject<UnitReference>(node.GetValue("unit"));
				}
				ConfigNodeUtils.TryParseValue(node, "proxyMode", ref triggerEvent.proxyMode);
			}
			else if (triggerEvent.triggerType == TriggerEvent.TriggerTypes.Conditional && node.HasValue("conditional"))
			{
				int value = node.GetValue<int>("conditional");
				triggerEvent.conditional = VTScenario.current.conditionals.GetConditional(value);
			}
			ConfigNodeUtils.TryParseValue(node, "eventName", ref triggerEvent.eventName);
			if (node.HasNode("EventInfo"))
			{
				triggerEvent.eventInfo.LoadFromInfoNode(node.GetNode("EventInfo"));
			}
			events.Add(triggerEvent);
			eventDict.Add(triggerEvent.id, triggerEvent);
			nextID = Mathf.Max(triggerEvent.id + 1, nextID);
		}
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode configNode = new ConfigNode("TRIGGER_EVENTS");
		foreach (TriggerEvent @event in events)
		{
			try
			{
				ConfigNode configNode2 = new ConfigNode("TriggerEvent");
				configNode2.SetValue("id", @event.id);
				configNode2.SetValue("enabled", @event.enabled);
				configNode2.SetValue("triggerType", @event.triggerType);
				if (@event.triggerType == TriggerEvent.TriggerTypes.Proximity)
				{
					configNode2.SetValue("waypoint", VTSConfigUtils.WriteObject(typeof(Waypoint), @event.waypoint));
					configNode2.SetValue("radius", ConfigNodeUtils.WriteObject(@event.radius));
					configNode2.SetValue("sphericalRadius", @event.sphericalRadius);
					configNode2.SetValue("triggerMode", ConfigNodeUtils.WriteObject(@event.triggerMode));
					if (@event.triggerMode == TriggerEventModes.Unit)
					{
						configNode2.SetValue("unit", VTSConfigUtils.WriteObject(typeof(UnitReference), @event.unit));
					}
					configNode2.SetValue("proxyMode", ConfigNodeUtils.WriteObject(@event.proxyMode));
				}
				else if (@event.triggerType == TriggerEvent.TriggerTypes.Conditional && @event.conditional != null)
				{
					configNode2.SetValue("conditional", @event.conditional.id);
				}
				configNode2.SetValue("eventName", @event.eventName);
				@event.eventInfo.SaveToNode(configNode2);
				configNode.AddNode(configNode2);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		scenarioNode.AddNode(configNode);
	}

	public ConfigNode QuicksaveToNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		for (int i = 0; i < events.Count; i++)
		{
			TriggerEvent triggerEvent = events[i];
			if ((bool)triggerEvent.behaviour)
			{
				ConfigNode configNode2 = new ConfigNode("event_" + triggerEvent.id);
				configNode2.SetValue("enabled", triggerEvent.enabled);
				configNode2.SetValue("triggered", triggerEvent.behaviour.WasTriggered());
				configNode.AddNode(configNode2);
			}
		}
		return configNode;
	}

	public void QuickloadFromNode(ConfigNode stNode)
	{
		for (int i = 0; i < events.Count; i++)
		{
			TriggerEvent triggerEvent = events[i];
			string name = "event_" + triggerEvent.id;
			if (stNode.HasNode(name) && (bool)triggerEvent.behaviour)
			{
				ConfigNode node = stNode.GetNode(name);
				bool value = node.GetValue<bool>("enabled");
				if (node.GetValue<bool>("triggered"))
				{
					triggerEvent.Disable();
					triggerEvent.behaviour.PermaDisable();
				}
				else if (value)
				{
					triggerEvent.Enable();
				}
				else
				{
					triggerEvent.Disable();
				}
			}
		}
	}
}
