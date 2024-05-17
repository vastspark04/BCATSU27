using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioConditionals
{
	private Dictionary<int, ScenarioConditional> conditionals = new Dictionary<int, ScenarioConditional>();

	private int nextID;

	public const string NODE_NAME = "Conditionals";

	public ScenarioConditional GetConditional(int id)
	{
		if (conditionals.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public ScenarioConditional CreateNewConditional()
	{
		ScenarioConditional scenarioConditional = new ScenarioConditional();
		scenarioConditional.id = nextID;
		nextID++;
		conditionals.Add(scenarioConditional.id, scenarioConditional);
		return scenarioConditional;
	}

	public void DeleteConditional(int id)
	{
		conditionals.Remove(id);
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		nextID = -1;
		if (scenarioNode.HasNode("Conditionals"))
		{
			foreach (ConfigNode node in scenarioNode.GetNode("Conditionals").GetNodes("CONDITIONAL"))
			{
				ScenarioConditional scenarioConditional = new ScenarioConditional();
				scenarioConditional.LoadFromNode(node);
				conditionals.Add(scenarioConditional.id, scenarioConditional);
				nextID = Mathf.Max(nextID, scenarioConditional.id + 1);
			}
		}
		if (nextID == -1)
		{
			nextID = 0;
		}
	}

	public void GatherReferences()
	{
		foreach (ScenarioConditional value in conditionals.Values)
		{
			value?.GatherReferences();
		}
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode configNode = new ConfigNode("Conditionals");
		scenarioNode.AddNode(configNode);
		foreach (ScenarioConditional value in conditionals.Values)
		{
			try
			{
				ConfigNode node = value.SaveToNode();
				configNode.AddNode(node);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
	}

	public ConfigNode QuicksaveToNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		foreach (KeyValuePair<int, ScenarioConditional> conditional in conditionals)
		{
			configNode.AddNode(conditional.Value.QuicksaveToNode("Conditional"));
		}
		return configNode;
	}

	public void QuickloadFromNode(ConfigNode node)
	{
		foreach (ConfigNode node2 in node.GetNodes("Conditional"))
		{
			int value = node2.GetValue<int>("id");
			conditionals[value].QuickloadFromNode(node2);
		}
	}
}
