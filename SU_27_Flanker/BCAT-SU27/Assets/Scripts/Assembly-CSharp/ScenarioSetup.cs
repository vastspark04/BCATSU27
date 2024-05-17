using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioSetup : MonoBehaviour
{
	[Serializable]
	public class ScenarioSetupConfig
	{
		public string name;

		public Transform spawnTransform;

		public List<GameObject> scenarioObjects;
	}

	public MapScenarios mapScenarioObject;

	public List<ScenarioSetupConfig> scenarios;

	public static ScenarioSetup instance { get; private set; }

	public ScenarioSetupConfig currentScenarioConfig { get; private set; }

	private void Awake()
	{
		instance = this;
		if (ScenarioSelectorUI.scenarioChosen)
		{
			int count = scenarios.Count;
			for (int i = 0; i < count; i++)
			{
				if (scenarios[i] == null)
				{
					continue;
				}
				bool flag = scenarios[i].name == ScenarioSelectorUI.scenarioName;
				if (flag)
				{
					currentScenarioConfig = scenarios[i];
				}
				foreach (GameObject scenarioObject in scenarios[i].scenarioObjects)
				{
					scenarioObject.SetActive(flag);
				}
			}
			return;
		}
		foreach (ScenarioSetupConfig scenario in scenarios)
		{
			if (scenario == null)
			{
				continue;
			}
			foreach (GameObject scenarioObject2 in scenario.scenarioObjects)
			{
				scenarioObject2.SetActive(value: false);
			}
		}
	}

	private void OnValidate()
	{
		if (!(mapScenarioObject != null))
		{
			return;
		}
		bool flag = false;
		if (scenarios != null && scenarios.Count == mapScenarioObject.scenarios.Length)
		{
			for (int i = 0; i < scenarios.Count; i++)
			{
				if (scenarios[i].name != mapScenarioObject.scenarios[i].scenarioName)
				{
					flag = true;
					break;
				}
			}
		}
		if (scenarios == null || scenarios.Count != mapScenarioObject.scenarios.Length || flag)
		{
			if (scenarios == null)
			{
				scenarios = new List<ScenarioSetupConfig>();
			}
			List<ScenarioSetupConfig> list = new List<ScenarioSetupConfig>();
			ScenarioInfo[] array = mapScenarioObject.scenarios;
			foreach (ScenarioInfo scenarioInfo in array)
			{
				ScenarioSetupConfig scenarioSetupConfig = new ScenarioSetupConfig();
				scenarioSetupConfig.name = scenarioInfo.scenarioName;
				list.Add(scenarioSetupConfig);
				foreach (ScenarioSetupConfig scenario in scenarios)
				{
					if (scenario.name == scenarioSetupConfig.name)
					{
						scenarioSetupConfig.spawnTransform = scenario.spawnTransform;
						scenarioSetupConfig.scenarioObjects = scenario.scenarioObjects;
					}
				}
			}
			scenarios = list;
		}
		else
		{
			for (int k = 0; k < mapScenarioObject.scenarios.Length; k++)
			{
				scenarios[k].name = mapScenarioObject.scenarios[k].scenarioName;
			}
		}
	}
}
