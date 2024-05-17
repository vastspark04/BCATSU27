using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CampaignScenarioSetup : MonoBehaviour
{
	[Serializable]
	public class CampaignScenarioConfig
	{
		public string name;

		public string scenarioID;

		public List<GameObject> scenarioObjects;

		public List<GameObject> globalObjectsToDisable;

		public Transform spawnTransform;

		public Transform fuelTransform;

		public Transform rtbTransform;
	}

	public Campaign campaign;

	[Header("Missions")]
	public List<CampaignScenarioConfig> missionConfigs;

	[Header("Training")]
	public List<CampaignScenarioConfig> trainingConfigs;

	public bool testThisScenario;

	public string testThisScenarioID;

	[ContextMenu("Refresh Configs")]
	public void RefreshConfigs()
	{
		if (!campaign)
		{
			return;
		}
		if (missionConfigs == null)
		{
			missionConfigs = new List<CampaignScenarioConfig>();
		}
		List<CampaignScenarioConfig> list = new List<CampaignScenarioConfig>();
		foreach (CampaignScenarioConfig missionConfig in missionConfigs)
		{
			list.Add(missionConfig);
		}
		missionConfigs.Clear();
		string text = SceneManager.GetActiveScene().name;
		foreach (CampaignScenario mission in campaign.missions)
		{
			if (!(mission.mapSceneName == text))
			{
				continue;
			}
			bool flag = false;
			CampaignScenarioConfig campaignScenarioConfig = null;
			foreach (CampaignScenarioConfig item in list)
			{
				if (item.scenarioID == mission.scenarioID)
				{
					missionConfigs.Add(item);
					campaignScenarioConfig = item;
					flag = true;
					break;
				}
			}
			if (flag && campaignScenarioConfig != null)
			{
				list.Remove(campaignScenarioConfig);
			}
			if (!flag)
			{
				CampaignScenarioConfig campaignScenarioConfig2 = new CampaignScenarioConfig();
				campaignScenarioConfig2.scenarioID = mission.scenarioID;
				campaignScenarioConfig2.name = mission.scenarioName;
				missionConfigs.Add(campaignScenarioConfig2);
			}
		}
		foreach (CampaignScenarioConfig item2 in list)
		{
			if (item2.scenarioObjects == null)
			{
				continue;
			}
			foreach (GameObject scenarioObject in item2.scenarioObjects)
			{
				TrashObject(scenarioObject);
			}
		}
		list = new List<CampaignScenarioConfig>();
		foreach (CampaignScenarioConfig trainingConfig in trainingConfigs)
		{
			list.Add(trainingConfig);
		}
		trainingConfigs.Clear();
		foreach (CampaignScenario trainingMission in campaign.trainingMissions)
		{
			if (!(trainingMission.mapSceneName == text))
			{
				continue;
			}
			bool flag2 = false;
			CampaignScenarioConfig campaignScenarioConfig3 = null;
			foreach (CampaignScenarioConfig item3 in list)
			{
				if (item3.scenarioID == trainingMission.scenarioID)
				{
					trainingConfigs.Add(item3);
					campaignScenarioConfig3 = item3;
					flag2 = true;
					break;
				}
			}
			if (flag2 && campaignScenarioConfig3 != null)
			{
				list.Remove(campaignScenarioConfig3);
			}
			if (!flag2)
			{
				CampaignScenarioConfig campaignScenarioConfig4 = new CampaignScenarioConfig();
				campaignScenarioConfig4.scenarioID = trainingMission.scenarioID;
				campaignScenarioConfig4.name = trainingMission.scenarioName;
				trainingConfigs.Add(campaignScenarioConfig4);
			}
		}
		foreach (CampaignScenarioConfig item4 in list)
		{
			if (item4.scenarioObjects == null)
			{
				continue;
			}
			foreach (GameObject scenarioObject2 in item4.scenarioObjects)
			{
				TrashObject(scenarioObject2);
			}
		}
	}

	private void TrashObject(GameObject obj)
	{
		if ((bool)obj)
		{
			Transform transform = base.transform.Find("Trash");
			if (!transform)
			{
				transform = new GameObject("Trash").transform;
				transform.parent = base.transform;
			}
			obj.transform.parent = transform;
		}
	}

	private void Awake()
	{
		string text = string.Empty;
		if (PilotSaveManager.currentCampaign != null && PilotSaveManager.currentCampaign == campaign && PilotSaveManager.currentScenario != null)
		{
			text = PilotSaveManager.currentScenario.scenarioID;
			CampaignScenario currentScenario = PilotSaveManager.currentScenario;
			string text2 = currentScenario.environmentName;
			if ((bool)EnvironmentManager.instance)
			{
				if (currentScenario.envIdx >= 0)
				{
					text2 = currentScenario.envOptions[currentScenario.envIdx].envName;
				}
				if (!string.IsNullOrEmpty(text2))
				{
					EnvironmentManager.instance.currentEnvironment = text2;
					EnvironmentManager.instance.SetCurrent();
				}
			}
		}
		foreach (CampaignScenarioConfig missionConfig in missionConfigs)
		{
			if (missionConfig.scenarioID == text)
			{
				if ((bool)missionConfig.spawnTransform && (bool)LevelBuilder.fetch)
				{
					LevelBuilder.fetch.playerSpawnTransform = missionConfig.spawnTransform;
				}
				if ((bool)missionConfig.rtbTransform)
				{
					WaypointManager.instance.rtbWaypoint = missionConfig.rtbTransform;
				}
				if ((bool)missionConfig.fuelTransform)
				{
					WaypointManager.instance.fuelWaypoint = missionConfig.fuelTransform;
				}
				foreach (GameObject scenarioObject in missionConfig.scenarioObjects)
				{
					scenarioObject.SetActive(value: true);
				}
				foreach (GameObject item in missionConfig.globalObjectsToDisable)
				{
					if ((bool)item)
					{
						UnityEngine.Object.Destroy(item);
					}
				}
				continue;
			}
			foreach (GameObject scenarioObject2 in missionConfig.scenarioObjects)
			{
				UnityEngine.Object.Destroy(scenarioObject2);
			}
		}
		foreach (CampaignScenarioConfig trainingConfig in trainingConfigs)
		{
			if (trainingConfig.scenarioID == text)
			{
				if ((bool)trainingConfig.spawnTransform && (bool)LevelBuilder.fetch)
				{
					LevelBuilder.fetch.playerSpawnTransform = trainingConfig.spawnTransform;
				}
				if ((bool)trainingConfig.rtbTransform)
				{
					WaypointManager.instance.rtbWaypoint = trainingConfig.rtbTransform;
				}
				if ((bool)trainingConfig.fuelTransform)
				{
					WaypointManager.instance.fuelWaypoint = trainingConfig.fuelTransform;
				}
				foreach (GameObject scenarioObject3 in trainingConfig.scenarioObjects)
				{
					scenarioObject3.SetActive(value: true);
				}
				foreach (GameObject item2 in trainingConfig.globalObjectsToDisable)
				{
					if ((bool)item2)
					{
						UnityEngine.Object.Destroy(item2);
					}
				}
				continue;
			}
			foreach (GameObject scenarioObject4 in trainingConfig.scenarioObjects)
			{
				UnityEngine.Object.Destroy(scenarioObject4);
			}
		}
	}
}
