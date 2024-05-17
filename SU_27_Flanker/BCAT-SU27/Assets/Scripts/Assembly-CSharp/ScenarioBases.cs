using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioBases
{
	public class ScenarioBaseInfo
	{
		public VTMapEdScenarioBasePrefab basePrefab;

		public string overrideBaseName;

		public Teams baseTeam;

		public string GetFinalName()
		{
			if (!string.IsNullOrEmpty(overrideBaseName))
			{
				return overrideBaseName;
			}
			if (!string.IsNullOrEmpty(basePrefab.baseName))
			{
				return basePrefab.baseName;
			}
			return "unnamed (" + basePrefab.id + ")";
		}
	}

	public Dictionary<int, ScenarioBaseInfo> baseInfos;

	public ScenarioBases()
	{
		baseInfos = new Dictionary<int, ScenarioBaseInfo>();
		if (!VTMapManager.fetch)
		{
			return;
		}
		foreach (AirportManager airport in VTMapManager.fetch.airports)
		{
			VTMapEdScenarioBasePrefab componentInParent = airport.GetComponentInParent<VTMapEdScenarioBasePrefab>();
			if ((bool)componentInParent && !baseInfos.ContainsKey(componentInParent.id))
			{
				ScenarioBaseInfo value = new ScenarioBaseInfo
				{
					basePrefab = componentInParent
				};
				baseInfos.Add(componentInParent.id, value);
			}
		}
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode configNode = new ConfigNode("BASES");
		foreach (KeyValuePair<int, ScenarioBaseInfo> baseInfo in baseInfos)
		{
			try
			{
				ConfigNode configNode2 = new ConfigNode("BaseInfo");
				configNode.AddNode(configNode2);
				configNode2.SetValue("id", baseInfo.Key);
				configNode2.SetValue("overrideBaseName", baseInfo.Value.overrideBaseName);
				configNode2.SetValue("baseTeam", baseInfo.Value.baseTeam);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		scenarioNode.AddNode(configNode);
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		baseInfos = new Dictionary<int, ScenarioBaseInfo>();
		if (scenarioNode.HasNode("BASES"))
		{
			foreach (ConfigNode node in scenarioNode.GetNode("BASES").GetNodes("BaseInfo"))
			{
				int value = node.GetValue<int>("id");
				foreach (AirportManager airport in VTMapManager.fetch.airports)
				{
					VTMapEdScenarioBasePrefab componentInParent = airport.GetComponentInParent<VTMapEdScenarioBasePrefab>();
					if ((bool)componentInParent && componentInParent.id == value)
					{
						ScenarioBaseInfo scenarioBaseInfo = new ScenarioBaseInfo();
						scenarioBaseInfo.overrideBaseName = node.GetValue("overrideBaseName");
						scenarioBaseInfo.baseTeam = node.GetValue<Teams>("baseTeam");
						scenarioBaseInfo.basePrefab = componentInParent;
						baseInfos.Add(value, scenarioBaseInfo);
						break;
					}
				}
			}
		}
		foreach (AirportManager airport2 in VTMapManager.fetch.airports)
		{
			VTMapEdScenarioBasePrefab componentInParent2 = airport2.GetComponentInParent<VTMapEdScenarioBasePrefab>();
			if ((bool)componentInParent2 && !baseInfos.ContainsKey(componentInParent2.id))
			{
				ScenarioBaseInfo scenarioBaseInfo2 = new ScenarioBaseInfo();
				scenarioBaseInfo2.basePrefab = componentInParent2;
				baseInfos.Add(componentInParent2.id, scenarioBaseInfo2);
			}
		}
	}

	public void BeginScenario()
	{
		foreach (AirportManager airport in VTMapManager.fetch.airports)
		{
			VTMapEdScenarioBasePrefab componentInParent = airport.GetComponentInParent<VTMapEdScenarioBasePrefab>();
			if (!componentInParent)
			{
				continue;
			}
			if (baseInfos.TryGetValue(componentInParent.id, out var value))
			{
				if (!string.IsNullOrEmpty(value.overrideBaseName))
				{
					componentInParent.baseName = value.overrideBaseName;
				}
				componentInParent.team = value.baseTeam;
			}
			componentInParent.BeginScenario();
		}
	}
}
