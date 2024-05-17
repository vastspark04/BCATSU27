using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScenarioSelectorUI : MonoBehaviour
{
	[Serializable]
	public class MapInfo
	{
		public string mapTitle;

		public string sceneName;

		[TextArea(3, 10)]
		public string mapDescription;

		public Texture thumbnailTexture;

		public MapScenarios scenarios;
	}

	public GameObject mapSelectorObject;

	public Text mapTitleText;

	public Text mapDescriptionText;

	public RawImage mapThumbnailImage;

	public MapInfo[] maps;

	private int mapIdx;

	private int sceneIdx;

	[Space]
	public int scenarioIdx;

	public static string scenarioName;

	public static bool scenarioChosen;

	public GameObject scenarioSelectorObject;

	public Text scenarioNameText;

	public Text scenarioDescriptionText;

	private void Start()
	{
		mapSelectorObject.SetActive(value: true);
		scenarioSelectorObject.SetActive(value: false);
		scenarioIdx = 0;
		UpdateMapDisplay();
	}

	public void NextMap()
	{
		mapIdx = (mapIdx + 1) % maps.Length;
		UpdateMapDisplay();
	}

	public void PrevMap()
	{
		mapIdx--;
		if (mapIdx < 0)
		{
			mapIdx = maps.Length - 1;
		}
		UpdateMapDisplay();
	}

	private void UpdateMapDisplay()
	{
		MapInfo mapInfo = maps[mapIdx];
		mapTitleText.text = mapInfo.mapTitle;
		mapDescriptionText.text = mapInfo.mapDescription;
		mapThumbnailImage.texture = mapInfo.thumbnailTexture;
	}

	public void SelectMap()
	{
		if (!string.IsNullOrEmpty(maps[mapIdx].sceneName))
		{
			sceneIdx = SceneUtility.GetBuildIndexByScenePath(maps[mapIdx].sceneName);
			mapSelectorObject.SetActive(value: false);
			scenarioSelectorObject.SetActive(value: true);
			UpdateScenarioDisplay();
		}
	}

	public void NextScenario()
	{
		scenarioIdx = (scenarioIdx + 1) % maps[mapIdx].scenarios.scenarios.Length;
		UpdateScenarioDisplay();
	}

	public void PrevScenario()
	{
		scenarioIdx--;
		if (scenarioIdx < 0)
		{
			scenarioIdx = maps[mapIdx].scenarios.scenarios.Length - 1;
		}
		UpdateScenarioDisplay();
	}

	private void UpdateScenarioDisplay()
	{
		ScenarioInfo scenarioInfo = maps[mapIdx].scenarios.scenarios[scenarioIdx];
		scenarioNameText.text = scenarioInfo.scenarioName;
		scenarioDescriptionText.text = scenarioInfo.scenarioDescription;
	}

	public void StartScenario()
	{
		scenarioChosen = true;
		scenarioName = maps[mapIdx].scenarios.scenarios[scenarioIdx].scenarioName;
		LoadingSceneController.LoadScene(sceneIdx);
	}

	public void QuitGame()
	{
		Application.Quit();
	}

	public void BackToMapSelect()
	{
		mapSelectorObject.SetActive(value: true);
		scenarioSelectorObject.SetActive(value: false);
		UpdateMapDisplay();
	}
}
