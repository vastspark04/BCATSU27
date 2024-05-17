using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioUnits
{
	public const string NODE_NAME = "UNITS";

	private int nextID;

	public Dictionary<int, UnitSpawner> units;

	public Dictionary<int, UnitSpawner> alliedUnits;

	public Dictionary<int, UnitSpawner> enemyUnits;

	public ScenarioUnits()
	{
		nextID = 0;
		units = new Dictionary<int, UnitSpawner>();
		alliedUnits = new Dictionary<int, UnitSpawner>();
		enemyUnits = new Dictionary<int, UnitSpawner>();
	}

	public void AddSpawner(UnitSpawner sp)
	{
		RemoveSpawner(sp);
		units.Add(sp.unitInstanceID, sp);
		if (sp.team == Teams.Allied)
		{
			alliedUnits.Add(sp.unitInstanceID, sp);
		}
		else
		{
			enemyUnits.Add(sp.unitInstanceID, sp);
		}
	}

	public void RemoveSpawner(UnitSpawner sp)
	{
		units.Remove(sp.unitInstanceID);
		alliedUnits.Remove(sp.unitInstanceID);
		enemyUnits.Remove(sp.unitInstanceID);
	}

	public UnitSpawner GetUnit(int unitID)
	{
		if (units.TryGetValue(unitID, out var value))
		{
			return value;
		}
		return null;
	}

	public void DestroyAll()
	{
		foreach (UnitSpawner value in units.Values)
		{
			if ((bool)value)
			{
				if ((bool)value.spawnedUnit)
				{
					UnityEngine.Object.Destroy(value.spawnedUnit.gameObject);
				}
				UnityEngine.Object.Destroy(value.gameObject);
			}
		}
	}

	public UnitSpawner GetPlayerSpawner()
	{
		foreach (UnitSpawner value in alliedUnits.Values)
		{
			if (value.prefabUnitSpawn is PlayerSpawn)
			{
				return value;
			}
		}
		return null;
	}

	public int RequestUnitID()
	{
		int result = nextID;
		nextID++;
		return result;
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		if (!scenarioNode.HasNode("UNITS"))
		{
			return;
		}
		foreach (ConfigNode node in scenarioNode.GetNode("UNITS").GetNodes("UnitSpawner"))
		{
			GameObject gameObject = new GameObject();
			UnitSpawner unitSpawner = gameObject.AddComponent<UnitSpawner>();
			if (unitSpawner.LoadFromSpawnerNode(node))
			{
				AddSpawner(unitSpawner);
				nextID = Mathf.Max(nextID, unitSpawner.unitInstanceID + 1);
			}
			else
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
		foreach (UnitSpawner value in units.Values)
		{
			if (value.unlinkedParentID >= 0)
			{
				value.AttachToParent(GetUnit(value.unlinkedParentID));
			}
		}
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode node = new ConfigNode("UNITS");
		if (units == null)
		{
			return;
		}
		foreach (UnitSpawner value in units.Values)
		{
			try
			{
				value.SaveToParentConfigNode(node);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		scenarioNode.AddNode(node);
	}
}
