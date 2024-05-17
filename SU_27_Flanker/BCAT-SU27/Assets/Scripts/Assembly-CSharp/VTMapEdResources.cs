using System.Collections.Generic;
using UnityEngine;

public static class VTMapEdResources
{
	private class PrefabCategory
	{
		public Dictionary<string, VTMapEdPrefab> prefabs;
	}

	private const string resourcesPath = "VTMapEditor/Prefabs";

	private static Dictionary<string, VTMapEdPrefab> allPrefabs;

	private static Dictionary<string, PrefabCategory> categories;

	public static void LoadAll()
	{
		LoadPrefabs();
	}

	public static void LoadPrefabs()
	{
		categories = new Dictionary<string, PrefabCategory>();
		allPrefabs = new Dictionary<string, VTMapEdPrefab>();
		GameObject[] array = Resources.LoadAll<GameObject>("VTMapEditor/Prefabs");
		foreach (GameObject gameObject in array)
		{
			VTMapEdPrefab component = gameObject.GetComponent<VTMapEdPrefab>();
			if (!component)
			{
				continue;
			}
			if (allPrefabs.ContainsKey(gameObject.name))
			{
				Debug.LogError("Duplicate map editor prefab id: " + gameObject.name);
				continue;
			}
			PrefabCategory prefabCategory = null;
			if (categories.ContainsKey(component.category))
			{
				prefabCategory = categories[component.category];
			}
			else
			{
				prefabCategory = new PrefabCategory();
				categories.Add(component.category, prefabCategory);
				prefabCategory.prefabs = new Dictionary<string, VTMapEdPrefab>();
			}
			prefabCategory.prefabs.Add(gameObject.name, component);
			allPrefabs.Add(gameObject.name, component);
		}
	}

	public static VTMapEdPrefab[] GetPrefabs(string category)
	{
		if (categories.ContainsKey(category))
		{
			PrefabCategory prefabCategory = categories[category];
			VTMapEdPrefab[] array = new VTMapEdPrefab[prefabCategory.prefabs.Count];
			int num = 0;
			{
				foreach (VTMapEdPrefab value in prefabCategory.prefabs.Values)
				{
					VTMapEdPrefab vTMapEdPrefab = (array[num] = value);
					num++;
				}
				return array;
			}
		}
		return null;
	}

	public static List<string> GetAllCategories()
	{
		List<string> list = new List<string>();
		foreach (string key in categories.Keys)
		{
			list.Add(key);
		}
		return list;
	}

	public static VTMapEdPrefab GetPrefab(string id)
	{
		if (allPrefabs.ContainsKey(id))
		{
			return allPrefabs[id];
		}
		return null;
	}
}
