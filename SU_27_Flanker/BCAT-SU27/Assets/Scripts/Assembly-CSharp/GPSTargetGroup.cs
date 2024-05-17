using System.Collections.Generic;
using UnityEngine;

public class GPSTargetGroup
{
	public string denom;

	public int numeral;

	public List<GPSTarget> targets;

	public int currentTargetIdx = -1;

	public bool isPath;

	public int datalinkID;

	public string groupName => $"{denom} {numeral}";

	public GPSTarget currentTarget
	{
		get
		{
			currentTargetIdx = Mathf.Clamp(currentTargetIdx, -1, targets.Count - 1);
			if (currentTargetIdx >= 0)
			{
				return targets[currentTargetIdx];
			}
			return null;
		}
	}

	public GPSTargetGroup(string denom, int numeral)
	{
		if (denom.Length != 3)
		{
			Debug.LogError("A GPSTargetGroup was created with a non-3-length string: " + denom);
		}
		this.denom = denom.ToUpper();
		this.numeral = numeral;
		targets = new List<GPSTarget>();
		currentTargetIdx = -1;
	}

	public void AddTarget(GPSTarget target)
	{
		targets.Add(target);
		if (targets.Count == 1)
		{
			currentTargetIdx = 0;
		}
	}

	public void RemoveTarget(string targetName)
	{
		targets.RemoveAll((GPSTarget t) => t.targetName == targetName);
		if (targets.Count == 0)
		{
			currentTargetIdx = -1;
		}
	}

	public void RemoveAllTargets()
	{
		targets.Clear();
		currentTargetIdx = -1;
	}

	public void RemoveTarget(GPSTarget target)
	{
		targets.Remove(target);
		if (targets.Count == 0)
		{
			currentTargetIdx = -1;
		}
	}

	public void RemoveCurrentTarget()
	{
		if (targets.Count > 0)
		{
			RemoveTarget(currentTarget);
		}
		if (targets.Count == 0)
		{
			currentTargetIdx = -1;
		}
		else if (currentTargetIdx == targets.Count)
		{
			currentTargetIdx--;
		}
	}

	public GPSTarget NextTarget()
	{
		if (targets == null || targets.Count < 1)
		{
			currentTargetIdx = -1;
			return null;
		}
		currentTargetIdx = (currentTargetIdx + 1) % targets.Count;
		return currentTarget;
	}

	public GPSTarget PreviousTarget()
	{
		if (targets == null || targets.Count < 1)
		{
			currentTargetIdx = -1;
			return null;
		}
		currentTargetIdx--;
		if (currentTargetIdx < 0)
		{
			currentTargetIdx = targets.Count - 1;
		}
		return currentTarget;
	}

	public float GetPathLength(int startIdx)
	{
		float num = 0f;
		if (targets.Count > 1 && startIdx < targets.Count - 1)
		{
			for (int i = startIdx + 1; i < targets.Count; i++)
			{
				num += (targets[i].worldPosition - targets[i - 1].worldPosition).magnitude;
			}
		}
		return num;
	}

	public ConfigNode SaveToConfigNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		configNode.SetValue("denom", denom);
		configNode.SetValue("numeral", numeral);
		configNode.SetValue("currentTargetIdx", currentTargetIdx);
		configNode.SetValue("isPath", isPath);
		for (int i = 0; i < targets.Count; i++)
		{
			ConfigNode configNode2 = new ConfigNode("TARGET");
			configNode2.SetValue("denom", targets[i].denom);
			configNode2.SetValue("numeral", targets[i].numeral);
			configNode2.SetValue("globalPos", VTMapManager.WorldToGlobalPoint(targets[i].worldPosition));
			configNode.AddNode(configNode2);
		}
		return configNode;
	}

	public static GPSTargetGroup LoadFromConfigNode(ConfigNode node)
	{
		GPSTargetGroup gPSTargetGroup = new GPSTargetGroup(node.GetValue("denom"), node.GetValue<int>("numeral"));
		gPSTargetGroup.currentTargetIdx = node.GetValue<int>("currentTargetIdx");
		gPSTargetGroup.isPath = node.GetValue<bool>("isPath");
		foreach (ConfigNode node2 in node.GetNodes("TARGET"))
		{
			gPSTargetGroup.AddTarget(new GPSTarget(VTMapManager.GlobalToWorldPoint(node2.GetValue<Vector3D>("globalPos")), node2.GetValue("denom"), node2.GetValue<int>("numeral")));
		}
		return gPSTargetGroup;
	}
}
