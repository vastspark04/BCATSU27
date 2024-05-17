using System;
using System.Collections.Generic;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

public class ScenarioStaticObjects
{
	public const string NODE_NAME = "StaticObjects";

	private Dictionary<int, VTStaticObject> staticObjects = new Dictionary<int, VTStaticObject>();

	public void DestroyAll()
	{
		foreach (VTStaticObject value in staticObjects.Values)
		{
			if ((bool)value)
			{
				UnityEngine.Object.Destroy(value.gameObject);
			}
		}
	}

	public VTStaticObject CreateObject(GameObject objectPrefab)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(objectPrefab);
		VTStaticObject component = gameObject.GetComponent<VTStaticObject>();
		gameObject.name = objectPrefab.name;
		gameObject.SetActive(value: true);
		if (!gameObject.GetComponent<FloatingOriginTransform>())
		{
			gameObject.AddComponent<FloatingOriginTransform>();
		}
		component.SetNewID(NextID());
		staticObjects.Add(component.id, component);
		return component;
	}

	public VTStaticObject GetObject(int id)
	{
		if (staticObjects.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public void MP_ReplaceObject(VTStaticObject newObj)
	{
		VTStaticObject @object = GetObject(newObj.id);
		newObj.transform.position = @object.transform.position;
		newObj.transform.rotation = @object.transform.rotation;
		if (!newObj.GetComponent<FloatingOriginTransform>())
		{
			newObj.gameObject.AddComponent<FloatingOriginTransform>();
		}
		staticObjects[newObj.id] = newObj;
		UnityEngine.Object.Destroy(@object.gameObject);
		newObj.gameObject.SetActive(value: true);
	}

	public List<VTStaticObject> GetAllObjects()
	{
		List<VTStaticObject> list = new List<VTStaticObject>();
		foreach (VTStaticObject value in staticObjects.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public void RemoveObject(int id)
	{
		if (staticObjects.ContainsKey(id))
		{
			GameObject gameObject = staticObjects[id].gameObject;
			staticObjects.Remove(id);
			UnityEngine.Object.Destroy(gameObject);
		}
	}

	private int NextID()
	{
		if (staticObjects.Count < 1)
		{
			return 0;
		}
		int num = 0;
		foreach (VTStaticObject value in staticObjects.Values)
		{
			num = Mathf.Max(num, value.id);
		}
		return num + 1;
	}

	public void BeginScenario()
	{
		List<int> list = new List<int>();
		foreach (int key in staticObjects.Keys)
		{
			list.Add(key);
		}
		foreach (int item in list)
		{
			VTStaticObject vTStaticObject = staticObjects[item];
			if (VTOLMPUtils.IsMultiplayer() && VTNetworkManager.isHost && (bool)vTStaticObject.GetComponent<VTStaticObjectSync>())
			{
				VTOLMPUnitManager.instance.RespawnStaticObjectForNet(vTStaticObject);
			}
			else
			{
				vTStaticObject.Spawn();
			}
		}
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		staticObjects = new Dictionary<int, VTStaticObject>();
		if (!scenarioNode.HasNode("StaticObjects"))
		{
			return;
		}
		foreach (ConfigNode node in scenarioNode.GetNode("StaticObjects").GetNodes("StaticObject"))
		{
			VTStaticObject vTStaticObject = VTStaticObject.LoadFromConfigNode(node);
			if (!vTStaticObject.GetComponent<FloatingOriginTransform>())
			{
				vTStaticObject.gameObject.AddComponent<FloatingOriginTransform>();
			}
			staticObjects.Add(vTStaticObject.id, vTStaticObject);
			vTStaticObject.OnLoadedFromConfig();
		}
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode configNode = new ConfigNode("StaticObjects");
		foreach (VTStaticObject value in staticObjects.Values)
		{
			try
			{
				configNode.AddNode(value.SaveToConfigNode());
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		scenarioNode.AddNode(configNode);
	}

	public ConfigNode QuickSaveToNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		foreach (VTStaticObject value in staticObjects.Values)
		{
			ConfigNode configNode2 = configNode.AddNode("StaticObject");
			configNode2.SetValue("id", value.id);
			IQSVehicleComponent[] componentsInChildrenImplementing = value.gameObject.GetComponentsInChildrenImplementing<IQSVehicleComponent>();
			foreach (IQSVehicleComponent iQSVehicleComponent in componentsInChildrenImplementing)
			{
				try
				{
					iQSVehicleComponent.OnQuicksave(configNode2);
				}
				catch (Exception message)
				{
					Debug.LogError(message);
					QuicksaveManager.instance.IndicateError();
				}
			}
		}
		return configNode;
	}

	public void QuickLoadFromNode(ConfigNode node)
	{
		foreach (ConfigNode node2 in node.GetNodes("StaticObject"))
		{
			int value = node2.GetValue<int>("id");
			VTStaticObject @object = GetObject(value);
			if (!(@object != null))
			{
				continue;
			}
			IQSVehicleComponent[] componentsInChildrenImplementing = @object.gameObject.GetComponentsInChildrenImplementing<IQSVehicleComponent>();
			foreach (IQSVehicleComponent iQSVehicleComponent in componentsInChildrenImplementing)
			{
				try
				{
					iQSVehicleComponent.OnQuickload(node2);
				}
				catch (Exception message)
				{
					Debug.LogError(message);
					QuicksaveManager.instance.IndicateError();
				}
			}
		}
	}
}
