using System.Collections.Generic;
using UnityEngine;

public class ScenarioGlobalValues
{
	public class GlobalValueData
	{
		public int id;

		public string name;

		public string description;

		public int initialValue;

		public int currentValue;

		public GlobalValue GetReference()
		{
			GlobalValue result = default(GlobalValue);
			result.id = id;
			return result;
		}

		public void ParseFromData(string s)
		{
			List<string> list = ConfigNodeUtils.ParseList(s);
			id = ConfigNodeUtils.ParseInt(list[0]);
			name = list[1];
			description = list[2];
			initialValue = ConfigNodeUtils.ParseInt(list[3]);
		}

		public string WriteData()
		{
			return ConfigNodeUtils.WriteList(new List<string>
			{
				id.ToString(),
				name,
				description,
				initialValue.ToString()
			});
		}
	}

	private Dictionary<int, GlobalValueData> globalValues = new Dictionary<int, GlobalValueData>();

	private int nextID;

	public void SaveToScenarioNode(ConfigNode node)
	{
		ConfigNode configNode = node.AddNode("GlobalValues");
		foreach (GlobalValueData value in globalValues.Values)
		{
			configNode.AddNode("gv").SetValue("data", value.WriteData());
		}
	}

	public void LoadFromScenarioNode(ConfigNode node)
	{
		ConfigNode node2 = node.GetNode("GlobalValues");
		if (node2 == null)
		{
			return;
		}
		foreach (ConfigNode node3 in node2.GetNodes("gv"))
		{
			GlobalValueData globalValueData = new GlobalValueData();
			globalValueData.ParseFromData(node3.GetValue("data"));
			globalValues.Add(globalValueData.id, globalValueData);
			nextID = Mathf.Max(nextID, globalValueData.id + 1);
		}
	}

	public GlobalValue CreateNewValue()
	{
		GlobalValueData globalValueData = new GlobalValueData();
		globalValueData.id = nextID++;
		globalValueData.name = "New Value";
		globalValues.Add(globalValueData.id, globalValueData);
		return globalValueData.GetReference();
	}

	public void DeleteValue(GlobalValue gv)
	{
		globalValues.Remove(gv.id);
	}

	public GlobalValueData GetValueData(int id)
	{
		if (globalValues.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public GlobalValue GetValue(int id)
	{
		if (globalValues.TryGetValue(id, out var value))
		{
			return value.GetReference();
		}
		GlobalValue result = default(GlobalValue);
		result.id = -1;
		return result;
	}

	public List<GlobalValue> GetAllValues()
	{
		List<GlobalValue> list = new List<GlobalValue>();
		foreach (GlobalValueData value in globalValues.Values)
		{
			list.Add(value.GetReference());
		}
		return list;
	}

	public List<GlobalValueData> GetAllDatas()
	{
		List<GlobalValueData> list = new List<GlobalValueData>();
		foreach (GlobalValueData value in globalValues.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public ConfigNode QuickSaveToNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		foreach (GlobalValueData value in globalValues.Values)
		{
			ConfigNode configNode2 = configNode.AddNode("gv");
			configNode2.SetValue("id", value.id);
			configNode2.SetValue("currVal", value.currentValue);
		}
		return configNode;
	}

	public void QuickLoadFromNode(ConfigNode gvNode)
	{
		foreach (ConfigNode node in gvNode.GetNodes("gv"))
		{
			int value = node.GetValue<int>("id");
			int value2 = node.GetValue<int>("currVal");
			globalValues[value].currentValue = value2;
		}
	}

	public void BeginScenario()
	{
		foreach (GlobalValueData value in globalValues.Values)
		{
			value.currentValue = value.initialValue;
		}
	}
}
