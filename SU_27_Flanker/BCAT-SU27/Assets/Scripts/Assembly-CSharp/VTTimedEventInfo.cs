using System.Collections.Generic;

public class VTTimedEventInfo
{
	public string eventName;

	public float time;

	public List<VTEventTarget> actions = new List<VTEventTarget>();

	public const string NODE_NAME = "TimedEventInfo";

	public void SaveToNode(ConfigNode node)
	{
		ConfigNode configNode = new ConfigNode("TimedEventInfo");
		configNode.SetValue("eventName", eventName);
		configNode.SetValue("time", time);
		foreach (VTEventTarget action in actions)
		{
			if (action != null && action.targetID >= 0 && action.TargetExists())
			{
				action.SaveToNode(configNode);
			}
		}
		node.AddNode(configNode);
	}

	public void LoadFromInfoNode(ConfigNode infoNode)
	{
		eventName = infoNode.GetValue("eventName");
		time = ConfigNodeUtils.ParseFloat(infoNode.GetValue("time"));
		actions = new List<VTEventTarget>();
		foreach (ConfigNode node in infoNode.GetNodes("EventTarget"))
		{
			VTEventTarget vTEventTarget = new VTEventTarget();
			vTEventTarget.LoadFromNode(node);
			actions.Add(vTEventTarget);
		}
	}
}
