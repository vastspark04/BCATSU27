using System.Collections.Generic;
using UnityEngine;

public class VTMapPrefabs
{
	public const string NODE_NAME = "StaticPrefabs";

	private Dictionary<int, VTMapEdPrefab> prefabs = new Dictionary<int, VTMapEdPrefab>();

	public List<VTMapEdPrefab> prefabList = new List<VTMapEdPrefab>();

	private int nextID;

	public void AddNewPrefab(VTMapEdPrefab spawnedPrefab)
	{
		spawnedPrefab.id = nextID;
		nextID++;
		prefabs.Add(spawnedPrefab.id, spawnedPrefab);
		prefabList.Add(spawnedPrefab);
	}

	public VTMapEdPrefab GetPrefab(int id)
	{
		if (prefabs.ContainsKey(id))
		{
			return prefabs[id];
		}
		return null;
	}

	public void LoadFromMapConfig(ConfigNode mapConfig)
	{
		nextID = -1;
		VTMapEdResources.LoadPrefabs();
		if (mapConfig.HasNode("StaticPrefabs"))
		{
			foreach (ConfigNode node in mapConfig.GetNode("StaticPrefabs").GetNodes("StaticPrefab"))
			{
				VTMapEdPrefab prefab = VTMapEdResources.GetPrefab(node.GetValue("prefab"));
				if (prefab != null)
				{
					GameObject gameObject = Object.Instantiate(prefab.gameObject);
					gameObject.name = prefab.gameObject.name;
					VTMapEdPrefab component = gameObject.GetComponent<VTMapEdPrefab>();
					component.LoadFromConfigNode(node);
					prefabs.Add(component.id, component);
					prefabList.Add(component);
					nextID = Mathf.Max(nextID, component.id);
				}
			}
		}
		nextID++;
	}

	public void SaveToMapConfig(ConfigNode mapConfig)
	{
		Debug.Log("Saving map prefabs. Prefab count: " + prefabs.Count);
		ConfigNode configNode = new ConfigNode("StaticPrefabs");
		foreach (VTMapEdPrefab value in prefabs.Values)
		{
			value.SaveToConfigNode(configNode);
		}
		mapConfig.AddNode(configNode);
	}

	public List<VTMapEdPrefab> GetAllPrefabs()
	{
		return prefabList;
	}

	public void RemovePrefab(int prefabId)
	{
		if (prefabs.ContainsKey(prefabId))
		{
			VTMapEdPrefab vTMapEdPrefab = prefabs[prefabId];
			prefabs.Remove(prefabId);
			prefabList.Remove(vTMapEdPrefab);
			Object.Destroy(vTMapEdPrefab.gameObject);
		}
	}
}
