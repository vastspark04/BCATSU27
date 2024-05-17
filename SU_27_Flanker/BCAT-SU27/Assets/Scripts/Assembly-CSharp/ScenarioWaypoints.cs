using System.Collections.Generic;
using UnityEngine;

public class ScenarioWaypoints
{
	private Dictionary<int, Waypoint> waypoints;

	private int bullseyeID = -1;

	private int bullseyeID_B = -1;

	private int nextID;

	public Waypoint bullseye
	{
		get
		{
			return GetWaypoint(bullseyeID);
		}
		set
		{
			if (value != null)
			{
				bullseyeID = value.id;
			}
			else
			{
				bullseyeID = -1;
			}
		}
	}

	public Transform bullseyeTransform
	{
		get
		{
			if (bullseye != null)
			{
				return bullseye.GetTransform();
			}
			return null;
		}
	}

	public Waypoint bullseyeB
	{
		get
		{
			return GetWaypoint(bullseyeID_B);
		}
		set
		{
			if (value != null)
			{
				bullseyeID_B = value.id;
			}
			else
			{
				bullseyeID_B = -1;
			}
		}
	}

	public Transform bullseyeBTransform
	{
		get
		{
			if (bullseyeB != null)
			{
				return bullseyeB.GetTransform();
			}
			return null;
		}
	}

	public ScenarioWaypoints()
	{
		waypoints = new Dictionary<int, Waypoint>();
		nextID = 0;
	}

	public void DestroyAll()
	{
		if (waypoints == null)
		{
			return;
		}
		foreach (Waypoint value in waypoints.Values)
		{
			if (value != null && value.GetTransform() != null)
			{
				Object.Destroy(value.GetTransform().gameObject);
			}
		}
	}

	public Waypoint AddWaypoint(Transform tf, string name, int id = -1)
	{
		if (id < 0)
		{
			id = GetNewID();
		}
		Waypoint waypoint = new Waypoint();
		waypoint.id = id;
		waypoint.name = name;
		waypoint.SetTransform(tf);
		waypoint.globalPoint = VTMapManager.WorldToGlobalPoint(tf.position);
		if (!tf.GetComponent<FloatingOriginTransform>())
		{
			tf.gameObject.AddComponent<FloatingOriginTransform>();
		}
		waypoints.Add(id, waypoint);
		return waypoint;
	}

	public void RemoveWaypoint(int id)
	{
		if (waypoints.ContainsKey(id))
		{
			Waypoint waypoint = waypoints[id];
			if ((bool)waypoint.GetTransform())
			{
				Object.Destroy(waypoint.GetTransform().gameObject);
			}
			waypoints.Remove(id);
		}
		else
		{
			Debug.LogError("Tried to remove a waypoint but the ID doesn't exist!");
		}
	}

	public Waypoint GetWaypoint(int id)
	{
		if (waypoints.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public Waypoint GetWaypoint(Transform wpTf)
	{
		foreach (Waypoint value in waypoints.Values)
		{
			if (value.GetTransform() == wpTf)
			{
				return value;
			}
		}
		return null;
	}

	public Waypoint[] GetWaypoints()
	{
		Waypoint[] array = new Waypoint[waypoints.Count];
		waypoints.Values.CopyTo(array, 0);
		return array;
	}

	public int GetNewID()
	{
		int result = nextID;
		nextID++;
		return result;
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode configNode = new ConfigNode("WAYPOINTS");
		if (waypoints != null)
		{
			foreach (Waypoint value in waypoints.Values)
			{
				if (value != null)
				{
					ConfigNode configNode2 = new ConfigNode("WAYPOINT");
					configNode2.SetValue("id", value.id);
					configNode2.SetValue("name", value.name);
					configNode2.SetValue("globalPoint", ConfigNodeUtils.WriteVector3D(value.globalPoint));
					configNode.AddNode(configNode2);
				}
			}
			if (bullseye != null)
			{
				configNode.SetValue("bullseyeID", bullseye.id);
			}
			if (bullseyeB != null)
			{
				configNode.SetValue("bullseyeID_B", bullseyeID_B);
			}
		}
		scenarioNode.AddNode(configNode);
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		waypoints.Clear();
		if (!scenarioNode.HasNode("WAYPOINTS"))
		{
			return;
		}
		ConfigNode node = scenarioNode.GetNode("WAYPOINTS");
		foreach (ConfigNode node2 in node.GetNodes("WAYPOINT"))
		{
			Waypoint waypoint = new Waypoint();
			waypoint.id = ConfigNodeUtils.ParseInt(node2.GetValue("id"));
			waypoint.name = node2.GetValue("name");
			waypoint.globalPoint = ConfigNodeUtils.ParseVector3D(node2.GetValue("globalPoint"));
			waypoint.SetTransform(new GameObject(waypoint.name).transform);
			waypoint.GetTransform().position = VTMapManager.GlobalToWorldPoint(waypoint.globalPoint);
			waypoint.GetTransform().gameObject.AddComponent<FloatingOriginTransform>();
			nextID = Mathf.Max(nextID, waypoint.id + 1);
			waypoints.Add(waypoint.id, waypoint);
		}
		if (node.HasValue("bullseyeID"))
		{
			bullseyeID = node.GetValue<int>("bullseyeID");
		}
		if (node.HasValue("bullseyeID_B"))
		{
			bullseyeID_B = node.GetValue<int>("bullseyeID_B");
		}
	}
}
