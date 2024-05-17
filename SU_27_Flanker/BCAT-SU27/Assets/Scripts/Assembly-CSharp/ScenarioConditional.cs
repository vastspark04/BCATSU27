using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ScenarioConditional
{
	public int id;

	public Dictionary<int, ScenarioConditionalComponent> components = new Dictionary<int, ScenarioConditionalComponent>();

	public ScenarioConditionalComponent rootComponent;

	public Vector3 outputNodePos;

	private int nextComponentID;

	public const string NODE_NAME = "CONDITIONAL";

	public int GetNewComponentID()
	{
		int result = nextComponentID;
		nextComponentID++;
		return result;
	}

	public bool GetCondition()
	{
		if (rootComponent != null)
		{
			return rootComponent.GetCondition();
		}
		return false;
	}

	public void DebugCondition()
	{
		string text = "conditional: " + id;
		foreach (ScenarioConditionalComponent value in components.Values)
		{
			text = text + "\n" + value.id + " (" + value.GetType()?.ToString() + "): ";
			text = text + "\n" + value.GetDebugString() + "\n";
			text = text + "    CONDITION: " + value.GetCondition();
		}
		Debug.Log(text);
	}

	public ScenarioConditionalComponent GetComponent(int id)
	{
		if (components.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public void LoadFromNode(ConfigNode conditionalNode)
	{
		components.Clear();
		rootComponent = null;
		id = conditionalNode.GetValue<int>("id");
		outputNodePos = conditionalNode.GetValue<Vector3>("outputNodePos");
		nextComponentID = -1;
		foreach (ConfigNode node in conditionalNode.GetNodes("COMP"))
		{
			Type type = Type.GetType(node.GetValue("type"));
			if (!type.IsSubclassOf(typeof(ScenarioConditionalComponent)))
			{
				continue;
			}
			ScenarioConditionalComponent scenarioConditionalComponent = (ScenarioConditionalComponent)Activator.CreateInstance(type);
			scenarioConditionalComponent.conditionalSys = this;
			scenarioConditionalComponent.id = node.GetValue<int>("id");
			scenarioConditionalComponent.uiPos = node.GetValue<Vector3>("uiPos");
			FieldInfo[] fields = scenarioConditionalComponent.GetType().GetFields();
			foreach (FieldInfo fieldInfo in fields)
			{
				if (fieldInfo.IsDefined(typeof(SCCField), inherit: true) && node.HasValue(fieldInfo.Name))
				{
					object value = VTSConfigUtils.ParseObject(fieldInfo.FieldType, node.GetValue(fieldInfo.Name));
					fieldInfo.SetValue(scenarioConditionalComponent, value);
				}
			}
			components.Add(scenarioConditionalComponent.id, scenarioConditionalComponent);
			nextComponentID = Mathf.Max(nextComponentID, scenarioConditionalComponent.id + 1);
		}
		if (nextComponentID == -1)
		{
			nextComponentID = 0;
		}
		if (conditionalNode.HasValue("root"))
		{
			int value2 = conditionalNode.GetValue<int>("root");
			components.TryGetValue(value2, out rootComponent);
		}
	}

	public void GatherReferences()
	{
		foreach (ScenarioConditionalComponent value in components.Values)
		{
			value.GatherReferences();
		}
	}

	public ConfigNode SaveToNode()
	{
		ConfigNode configNode = new ConfigNode("CONDITIONAL");
		configNode.SetValue("id", id);
		configNode.SetValue("outputNodePos", outputNodePos);
		if (rootComponent != null)
		{
			configNode.SetValue("root", rootComponent.id);
		}
		foreach (ScenarioConditionalComponent value2 in components.Values)
		{
			ConfigNode configNode2 = new ConfigNode("COMP");
			configNode2.SetValue("id", value2.id);
			configNode2.SetValue("type", value2.GetType().ToString());
			configNode2.SetValue("uiPos", value2.uiPos);
			FieldInfo[] fields = value2.GetType().GetFields();
			foreach (FieldInfo fieldInfo in fields)
			{
				if (fieldInfo.IsDefined(typeof(SCCField), inherit: true))
				{
					string value = VTSConfigUtils.WriteObject(fieldInfo.FieldType, fieldInfo.GetValue(value2));
					configNode2.SetValue(fieldInfo.Name, value);
				}
			}
			configNode.AddNode(configNode2);
		}
		return configNode;
	}

	public ConfigNode QuicksaveToNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		configNode.SetValue("id", id);
		foreach (ScenarioConditionalComponent value in components.Values)
		{
			if (value is IQSVehicleComponent)
			{
				IQSVehicleComponent obj = (IQSVehicleComponent)value;
				ConfigNode configNode2 = configNode.AddNode("component");
				configNode2.SetValue("id", value.id);
				obj.OnQuicksave(configNode2);
			}
		}
		return configNode;
	}

	public void QuickloadFromNode(ConfigNode node)
	{
		foreach (ConfigNode node2 in node.GetNodes("component"))
		{
			int value = node2.GetValue<int>("id");
			((IQSVehicleComponent)components[value]).OnQuickload(node2);
		}
	}
}
