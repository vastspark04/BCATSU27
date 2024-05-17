using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioPaths
{
	public Dictionary<int, FollowPath> paths;

	private int nextPathID;

	public ScenarioPaths()
	{
		paths = new Dictionary<int, FollowPath>();
	}

	public FollowPath GetPath(int pathID)
	{
		if (paths.TryGetValue(pathID, out var value))
		{
			return value;
		}
		return null;
	}

	public int GetPathID(FollowPath path)
	{
		if (path == null)
		{
			return -1;
		}
		foreach (int key in paths.Keys)
		{
			if (paths[key] == path)
			{
				return key;
			}
		}
		return -1;
	}

	public int AddPath(FollowPath path)
	{
		int num = nextPathID;
		paths.Add(num, path);
		nextPathID++;
		return num;
	}

	public void RemovePath(int pathID)
	{
		FollowPath followPath = paths[pathID];
		paths.Remove(pathID);
		if (followPath.pointTransforms != null)
		{
			Transform[] pointTransforms = followPath.pointTransforms;
			for (int i = 0; i < pointTransforms.Length; i++)
			{
				UnityEngine.Object.Destroy(pointTransforms[i].gameObject);
			}
		}
		UnityEngine.Object.Destroy(followPath.gameObject);
	}

	public void DestroyAll()
	{
		if (paths == null)
		{
			return;
		}
		foreach (FollowPath value in paths.Values)
		{
			if ((bool)value)
			{
				UnityEngine.Object.Destroy(value.gameObject);
			}
		}
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode configNode = new ConfigNode("PATHS");
		if (paths != null)
		{
			foreach (int key in paths.Keys)
			{
				if (!(paths[key] == null) && paths[key].pointTransforms != null && paths[key].pointTransforms.Length >= 2)
				{
					try
					{
						ConfigNode configNode2 = new ConfigNode("PATH");
						configNode2.SetValue("id", key);
						configNode2.SetValue("name", paths[key].gameObject.name);
						configNode2.SetValue("loop", paths[key].loop);
						configNode2.SetValue("points", WritePathPoints(paths[key]));
						configNode2.SetValue("pathMode", paths[key].pathMode);
						configNode.AddNode(configNode2);
					}
					catch (Exception message)
					{
						Debug.LogError(message);
					}
				}
			}
		}
		scenarioNode.AddNode(configNode);
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		if (!scenarioNode.HasNode("PATHS"))
		{
			return;
		}
		foreach (ConfigNode node in scenarioNode.GetNode("PATHS").GetNodes("PATH"))
		{
			GameObject gameObject = new GameObject(node.GetValue("name"));
			gameObject.AddComponent<FloatingOriginTransform>();
			FollowPath followPath = gameObject.AddComponent<FollowPath>();
			followPath.uniformlyPartition = true;
			List<Transform> list = new List<Transform>();
			bool flag = true;
			foreach (string item in ConfigNodeUtils.ParseList(node.GetValue("points")))
			{
				Vector3D globalPoint = ConfigNodeUtils.ParseVector3D(item);
				GameObject gameObject2 = new GameObject("PathPoint");
				gameObject2.transform.position = VTMapManager.GlobalToWorldPoint(globalPoint);
				if (flag)
				{
					gameObject.transform.position = gameObject2.transform.position;
					flag = false;
				}
				gameObject2.transform.parent = gameObject.transform;
				list.Add(gameObject2.transform);
			}
			followPath.pointTransforms = list.ToArray();
			followPath.loop = ConfigNodeUtils.ParseBool(node.GetValue("loop"));
			Curve3D.PathModes target = Curve3D.PathModes.Smooth;
			ConfigNodeUtils.TryParseValue(node, "pathMode", ref target);
			followPath.SetPathMode(target);
			followPath.SetupCurve();
			int num = (followPath.scenarioPathID = ConfigNodeUtils.ParseInt(node.GetValue("id")));
			paths.Add(num, followPath);
			nextPathID = Mathf.Max(nextPathID, num + 1);
		}
	}

	private string WritePathPoints(FollowPath path)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < path.pointTransforms.Length; i++)
		{
			list.Add(ConfigNodeUtils.WriteVector3D(VTMapManager.WorldToGlobalPoint(path.pointTransforms[i].position)));
		}
		return ConfigNodeUtils.WriteList(list);
	}
}
