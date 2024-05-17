using System.Collections.Generic;
using UnityEngine;

public class VTConditionalTriggers
{
	public class ConditionalTriggersBehaviour : MonoBehaviour
	{
		public List<ConditionalTrigger> conditionalTriggers = new List<ConditionalTrigger>();

		private List<ConditionalTrigger> finishedTriggers = new List<ConditionalTrigger>();

		private void Update()
		{
			for (int i = 0; i < conditionalTriggers.Count; i++)
			{
				ConditionalTrigger conditionalTrigger = conditionalTriggers[i];
				if (conditionalTrigger.enabled && conditionalTrigger.Update())
				{
					finishedTriggers.Add(conditionalTrigger);
				}
			}
			if (finishedTriggers.Count <= 0)
			{
				return;
			}
			foreach (ConditionalTrigger finishedTrigger in finishedTriggers)
			{
				conditionalTriggers.Remove(finishedTrigger);
			}
			finishedTriggers.Clear();
		}
	}

	public class ConditionalTrigger
	{
		public const string NODE_NAME = "CONDITIONAL_TRIGGER";

		public int id;

		public string name;

		public bool enabled = true;

		public ScenarioConditional conditional;

		public VTEventInfo actions;

		public ConfigNode SaveToConfigNode()
		{
			ConfigNode configNode = new ConfigNode("CONDITIONAL_TRIGGER");
			configNode.SetValue("id", id);
			configNode.SetValue("name", name);
			configNode.SetValue("enabled", enabled);
			if (conditional != null)
			{
				configNode.AddNode(conditional.SaveToNode());
			}
			if (actions != null)
			{
				configNode.AddNode(actions.SaveToNode("actions"));
			}
			return configNode;
		}

		public void LoadFromConfigNode(ConfigNode node)
		{
			id = node.GetValue<int>("id");
			name = node.GetValue("name");
			enabled = node.GetValue<bool>("enabled");
			if (node.HasNode("CONDITIONAL"))
			{
				conditional = new ScenarioConditional();
				conditional.LoadFromNode(node.GetNode("CONDITIONAL"));
			}
			if (node.HasNode("actions"))
			{
				actions = new VTEventInfo();
				actions.LoadFromInfoNode(node.GetNode("actions"));
			}
		}

		public bool Update()
		{
			if (conditional != null)
			{
				if (conditional.GetCondition())
				{
					if (actions != null)
					{
						actions.Invoke();
					}
					return true;
				}
				return false;
			}
			return true;
		}
	}

	public const string NODE_NAME = "ConditionalTriggers";

	private int nextID;

	private Dictionary<int, ConditionalTrigger> triggers = new Dictionary<int, ConditionalTrigger>();

	public ConditionalTrigger CreateNewTrigger()
	{
		ConditionalTrigger result = new ConditionalTrigger
		{
			id = nextID
		};
		nextID++;
		return result;
	}

	public ConditionalTrigger GetTrigger(int id)
	{
		if (triggers.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public void SetTriggerEnabled(int id, bool enabled)
	{
		if (triggers.TryGetValue(id, out var value))
		{
			value.enabled = enabled;
		}
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode configNode = new ConfigNode("ConditionalTriggers");
		scenarioNode.AddNode(configNode);
		foreach (ConditionalTrigger value in triggers.Values)
		{
			ConfigNode node = value.SaveToConfigNode();
			configNode.AddNode(node);
		}
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		nextID = -1;
		if (scenarioNode.HasNode("ConditionalTriggers"))
		{
			foreach (ConfigNode node in scenarioNode.GetNode("ConditionalTriggers").GetNodes("CONDITIONAL_TRIGGER"))
			{
				ConditionalTrigger conditionalTrigger = new ConditionalTrigger();
				conditionalTrigger.LoadFromConfigNode(node);
				triggers.Add(conditionalTrigger.id, conditionalTrigger);
				nextID = Mathf.Max(nextID, conditionalTrigger.id + 1);
			}
		}
		if (nextID == -1)
		{
			nextID = 0;
		}
	}
}
