using System.Collections.Generic;
using UnityEngine;

public static class UnitCatalogue
{
	public class UnitTeam
	{
		public Dictionary<string, UnitCategory> categories = new Dictionary<string, UnitCategory>();

		public List<string> keys = new List<string>();

		public List<Unit> allUnits = new List<Unit>();
	}

	public class UnitCategory
	{
		public string name;

		public Dictionary<string, Unit> units = new Dictionary<string, Unit>();

		public List<string> keys = new List<string>();
	}

	public class Unit
	{
		public string name;

		public string prefabName;

		public string description;

		public string resourcePath;

		public string editorSprite;

		public bool isPlayerSpawn;

		public bool hideFromEditor;

		public int teamIdx;

		public int categoryIdx;

		public int unitIdx;
	}

	public static Dictionary<Teams, UnitTeam> catalogue;

	public static Dictionary<Teams, string[]> categoryOptions;

	private static Dictionary<string, GameObject> unitPrefabs;

	private static Dictionary<string, Unit> units;

	private static bool isMp;

	public static void UpdateCatalogue()
	{
		bool flag = false;
		if (VTScenario.current != null && VTScenario.current.multiplayer != isMp)
		{
			isMp = VTScenario.current.multiplayer;
			flag = true;
		}
		if (!flag && catalogue != null && catalogue.Count > 0)
		{
			return;
		}
		catalogue = new Dictionary<Teams, UnitTeam>();
		catalogue.Add(Teams.Allied, new UnitTeam());
		catalogue.Add(Teams.Enemy, new UnitTeam());
		unitPrefabs = new Dictionary<string, GameObject>();
		units = new Dictionary<string, Unit>();
		foreach (Teams key in catalogue.Keys)
		{
			string text = "Units/" + key.ToString() + "/";
			GameObject[] array = Resources.LoadAll<GameObject>(text);
			foreach (GameObject gameObject in array)
			{
				UnitSpawn component = gameObject.GetComponent<UnitSpawn>();
				if (units.ContainsKey(gameObject.name))
				{
					Debug.LogErrorFormat("There's a duplicate unit ID (prefab name)! '{0}'", gameObject.name);
					continue;
				}
				Unit unit = new Unit();
				unit.name = component.unitName;
				unit.prefabName = gameObject.name;
				unit.description = component.unitDescription;
				unit.resourcePath = text + gameObject.name;
				unit.teamIdx = (int)key;
				unit.isPlayerSpawn = component is PlayerSpawn;
				unit.hideFromEditor = component.hideFromEditor;
				if (VTScenario.current != null)
				{
					if (VTScenario.current.multiplayer && component.singleplayerOnly)
					{
						unit.hideFromEditor = true;
					}
					else if (!VTScenario.current.multiplayer && component.multiplayerOnly)
					{
						unit.hideFromEditor = true;
					}
					if (component is AIUnitSpawn && !((AIUnitSpawn)component).mpReady && VTScenario.current.multiplayer)
					{
						unit.hideFromEditor = true;
					}
				}
				UnitCategory unitCategory;
				if (!catalogue[key].categories.ContainsKey(component.category))
				{
					unitCategory = new UnitCategory();
					unitCategory.name = component.category;
					catalogue[key].categories.Add(component.category, unitCategory);
					catalogue[key].keys.Add(component.category);
					unit.categoryIdx = catalogue[key].categories.Count - 1;
				}
				else
				{
					unitCategory = catalogue[key].categories[component.category];
					unit.categoryIdx = catalogue[key].keys.IndexOf(component.category);
				}
				unitCategory.units.Add(gameObject.name, unit);
				unitCategory.keys.Add(gameObject.name);
				unit.unitIdx = unitCategory.keys.Count - 1;
				unit.editorSprite = component.editorSprite;
				unitPrefabs.Add(unit.prefabName, gameObject);
				units.Add(unit.prefabName, unit);
				catalogue[key].allUnits.Add(unit);
			}
		}
		categoryOptions = new Dictionary<Teams, string[]>();
		string[] array2 = new string[catalogue[Teams.Allied].categories.Count];
		int num = 0;
		foreach (string key2 in catalogue[Teams.Allied].categories.Keys)
		{
			string text2 = (array2[num] = key2);
			num++;
		}
		categoryOptions.Add(Teams.Allied, array2);
		string[] array3 = new string[catalogue[Teams.Enemy].categories.Count];
		num = 0;
		foreach (string key3 in catalogue[Teams.Enemy].categories.Keys)
		{
			string text3 = (array3[num] = key3);
			num++;
		}
		categoryOptions.Add(Teams.Enemy, array3);
	}

	public static string[] GetUnitOptions(Teams team, string category)
	{
		string[] array = new string[catalogue[team].categories[category].units.Count];
		int num = 0;
		foreach (Unit value in catalogue[team].categories[category].units.Values)
		{
			array[num] = value.name;
			num++;
		}
		return array;
	}

	public static Unit GetUnit(Teams team, int catIdx, int unitIdx)
	{
		UnitTeam unitTeam = catalogue[team];
		UnitCategory unitCategory = unitTeam.categories[unitTeam.keys[catIdx]];
		return unitCategory.units[unitCategory.keys[unitIdx]];
	}

	public static GameObject GetUnitPrefab(string unitID)
	{
		try
		{
			if (unitPrefabs != null)
			{
				return unitPrefabs[unitID];
			}
			Debug.LogError("Tried to access the unit prefab catalogue but it was null! (Not updated!)");
			return null;
		}
		catch (KeyNotFoundException)
		{
			Debug.LogErrorFormat("The unitID {0} was not found in the unit catalogue!", unitID);
			return null;
		}
	}

	public static Unit GetUnit(string unitID)
	{
		try
		{
			if (units != null)
			{
				return units[unitID];
			}
			Debug.LogError("Tried to access the unit catalogue but it was null! (Not updated!)");
			return null;
		}
		catch (KeyNotFoundException)
		{
			Debug.LogErrorFormat("The unitID {0} was not found in the unit catalogue!", unitID);
			return null;
		}
	}
}
