using System.Collections.Generic;

public class VTEventInfo
{
	public string eventName;

	public List<VTEventTarget> actions = new List<VTEventTarget>();

	public const string NODE_NAME = "EventInfo";

	public void SaveToNode(ConfigNode node)
	{
		ConfigNode configNode = new ConfigNode("EventInfo");
		configNode.SetValue("eventName", eventName);
		foreach (VTEventTarget action in actions)
		{
			if (action != null && action.targetID >= 0 && action.TargetExists())
			{
				action.SaveToNode(configNode);
			}
		}
		node.AddNode(configNode);
	}

	public ConfigNode SaveToNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		configNode.SetValue("eventName", eventName);
		foreach (VTEventTarget action in actions)
		{
			if (action != null && action.targetID >= 0 && action.TargetExists())
			{
				action.SaveToNode(configNode);
			}
		}
		return configNode;
	}

	public void LoadFromInfoNode(ConfigNode infoNode)
	{
		eventName = infoNode.GetValue("eventName");
		actions = new List<VTEventTarget>();
		foreach (ConfigNode node in infoNode.GetNodes("EventTarget"))
		{
			VTEventTarget vTEventTarget = new VTEventTarget();
			vTEventTarget.LoadFromNode(node);
			actions.Add(vTEventTarget);
		}
	}

	public void Invoke()
	{
		foreach (VTEventTarget action in actions)
		{
			action?.Invoke();
		}
	}
}
