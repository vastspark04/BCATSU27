using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.Multiplayer{

public class VTMPScenarioSettings : MonoBehaviour
{
	public GameObject displayObj;

	[Header("Env Selection")]
	public GameObject envOptionsUIobj;

	public Text envText;

	private int envIdx;

	[Header("Briefing Room Options")]
	public ScriptableGameObjectList briefingRoomPrefabs;

	public Text brText;

	private int brIdx;

	[Header("Unit Icons")]
	public GameObject iconsIndicator;

	private bool unitIcons = true;

	private VTScenarioInfo selectedScenario;

	private CampaignScenario.EnvironmentOption[] envOptions;

	private List<int> availableBriefingRooms = new List<int>();

	public void SetupScenarioSettings(VTScenarioInfo selectedScenario)
	{
		if (envOptions == null)
		{
			envOptions = EnvironmentManager.GetGlobalEnvOptions();
		}
		if (selectedScenario == null)
		{
			displayObj.SetActive(value: false);
		}
		else
		{
			displayObj.SetActive(value: true);
			envOptionsUIobj.SetActive(selectedScenario.selectableEnv);
			if (selectedScenario.selectableEnv)
			{
				for (int i = 0; i < envOptions.Length; i++)
				{
					if (envOptions[i].envName == selectedScenario.envName)
					{
						envIdx = i;
						break;
					}
				}
				envText.text = envOptions[envIdx].envLabel;
			}
			else
			{
				envIdx = -1;
			}
			unitIcons = GameSettings.CurrentSettings.GetBoolSetting("UNIT_ICONS");
			iconsIndicator.SetActive(unitIcons);
		}
		CountPlayers(selectedScenario, out var alliedCount, out var enemyCount);
		availableBriefingRooms.Clear();
		for (int j = 0; j < briefingRoomPrefabs.list.Count; j++)
		{
			VTOLMPBriefingRoom component = briefingRoomPrefabs.list[j].GetComponent<VTOLMPBriefingRoom>();
			if ((!selectedScenario.separateBriefings || (bool)component.enemyBriefing) && component.alliedSpawnTransforms.Length >= alliedCount && component.enemySpawnTransforms.Length >= enemyCount)
			{
				availableBriefingRooms.Add(j);
			}
		}
		brIdx = Mathf.Clamp(brIdx, 0, availableBriefingRooms.Count - 1);
		brText.text = briefingRoomPrefabs.list[availableBriefingRooms[brIdx]].name;
	}

	private void CountPlayers(VTScenarioInfo scenario, out int alliedCount, out int enemyCount)
	{
		alliedCount = (enemyCount = 0);
		foreach (ConfigNode node2 in scenario.config.GetNode("UNITS").GetNodes("UnitSpawner"))
		{
			string value = node2.GetValue("unitID");
			int num = 1;
			ConfigNode node = node2.GetNode("UnitFields");
			if (node != null && node.HasValue("slots"))
			{
				num = node.GetValue<int>("slots");
			}
			if (value.Equals("MultiplayerSpawn"))
			{
				alliedCount += num;
			}
			else if (value.Equals("MultiplayerSpawnEnemy"))
			{
				enemyCount += num;
			}
		}
		int a = 8;
		alliedCount = Mathf.Min(a, alliedCount);
		enemyCount = Mathf.Min(a, enemyCount);
	}

	public void ToggleUnitIcons()
	{
		unitIcons = !unitIcons;
		iconsIndicator.SetActive(unitIcons);
	}

	public void NextEnv()
	{
		envIdx = (envIdx + 1) % envOptions.Length;
		envText.text = envOptions[envIdx].envLabel;
	}

	public void PrevEnv()
	{
		envIdx = (envIdx + (envOptions.Length - 1)) % envOptions.Length;
		envText.text = envOptions[envIdx].envLabel;
	}

	public void GetFinalSettings(out int envIdx, out bool unitIcons, out int briefingRoomIdx)
	{
		envIdx = this.envIdx;
		unitIcons = this.unitIcons;
		briefingRoomIdx = availableBriefingRooms[brIdx];
	}

	public void NextBr()
	{
		brIdx = (brIdx + 1) % availableBriefingRooms.Count;
		brText.text = briefingRoomPrefabs.list[availableBriefingRooms[brIdx]].name;
	}

	public void PrevBr()
	{
		int count = availableBriefingRooms.Count;
		brIdx = (brIdx + (count - 1)) % count;
		brText.text = briefingRoomPrefabs.list[availableBriefingRooms[brIdx]].name;
	}
}

}