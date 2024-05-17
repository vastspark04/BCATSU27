using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRGameSettingsWindow : MonoBehaviour
{
	public GameObject mainSettingsObj;

	public GameObject boolOptionTemplate;

	public GameObject floatOptionTemplate;

	public PilotSelectUI psUI;

	public Text pageText;

	public GameObject pageControlsObj;

	public float itemHeight;

	public float itemWidth;

	public int yCount;

	public int xCount;

	private int currX;

	private int currY;

	public GameObject page1;

	private GameObject currentPage;

	private GameSettings.SettingCategories currCategory;

	private List<GameObject>[] pages;

	private int currPageIdx;

	private List<VRGameSettingsUI>[] settingUIs;

	public GameObject bindingsWindowObject;

	public WingmanVoicesSettingsUI wingmanVoicesSettings;

	private GameSettings.Setting[] settingsArray;

	private GameSettings.SettingCategories currentCat;

	private void Awake()
	{
		int length = Enum.GetValues(typeof(GameSettings.SettingCategories)).Length;
		settingUIs = new List<VRGameSettingsUI>[length];
		pages = new List<GameObject>[length];
		for (int i = 0; i < length; i++)
		{
			settingUIs[i] = new List<VRGameSettingsUI>();
			pages[i] = new List<GameObject>();
		}
	}

	private void Start()
	{
		GameSettings.EnsureSettings();
		boolOptionTemplate.SetActive(value: false);
		floatOptionTemplate.SetActive(value: false);
		settingsArray = GameSettings.CurrentSettings.GetSettingsArray();
		SetupCategory(GameSettings.SettingCategories.Game);
	}

	public void SetupCategory(int catIdx)
	{
		SetupCategory((GameSettings.SettingCategories)catIdx);
	}

	private void SetupCategory(GameSettings.SettingCategories cat)
	{
		currX = 0;
		currY = -1;
		foreach (GameObject item in pages[(int)currentCat])
		{
			item.SetActive(value: false);
		}
		currentCat = cat;
		page1.SetActive(value: false);
		if (settingUIs[(int)currentCat].Count == 0)
		{
			CreateNewPage();
			GameSettings.Setting[] array = settingsArray;
			foreach (GameSettings.Setting setting in array)
			{
				if (setting.category == cat && setting.showInUI)
				{
					SetupOption(setting);
				}
			}
		}
		foreach (GameObject item2 in pages[(int)currentCat])
		{
			item2.SetActive(value: false);
		}
		currentPage = pages[(int)currentCat][0];
		currentPage.SetActive(value: true);
		currPageIdx = 0;
		UpdatePageText();
		GetComponentInParent<VRPointInteractableCanvas>().RefreshInteractables();
	}

	private void SetupOption(GameSettings.Setting setting)
	{
		currY++;
		if (currY >= yCount)
		{
			currY = 0;
			currX++;
			if (currX >= xCount)
			{
				currX = 0;
				CreateNewPage();
			}
		}
		GameObject gameObject = null;
		VRGameSettingsUI vRGameSettingsUI = null;
		if (setting.settingType == GameSettings.SettingTypes.Bool)
		{
			gameObject = UnityEngine.Object.Instantiate(boolOptionTemplate, boolOptionTemplate.transform.parent);
			vRGameSettingsUI = gameObject.GetComponent<VRGameSettingsBool>();
		}
		else if (setting.settingType == GameSettings.SettingTypes.Float)
		{
			gameObject = UnityEngine.Object.Instantiate(floatOptionTemplate, boolOptionTemplate.transform.parent);
			vRGameSettingsUI = gameObject.GetComponent<VRGameSettingsFloat>();
		}
		vRGameSettingsUI.Setup(setting);
		settingUIs[(int)currentCat].Add(vRGameSettingsUI);
		Vector3 localPosition = boolOptionTemplate.transform.localPosition;
		localPosition.x += (float)currX * itemWidth;
		localPosition.y -= (float)currY * itemHeight;
		gameObject.transform.SetParent(currentPage.transform);
		gameObject.transform.localPosition = localPosition;
		gameObject.SetActive(value: true);
	}

	private void CreateNewPage()
	{
		GameObject gameObject = new GameObject("SettingsPage");
		pages[(int)currentCat].Add(gameObject);
		currentPage = gameObject;
		gameObject.transform.parent = page1.transform.parent;
		gameObject.transform.localPosition = page1.transform.localPosition;
		gameObject.transform.localRotation = page1.transform.localRotation;
		gameObject.transform.localScale = page1.transform.localScale;
	}

	public void OpenWingmanVoicesSettings()
	{
		mainSettingsObj.SetActive(value: false);
		wingmanVoicesSettings.OpenWindow();
	}

	public void CloseWingmanVoicesSettings()
	{
		wingmanVoicesSettings.SaveAndClose();
		mainSettingsObj.SetActive(value: true);
	}

	public void ApplyAndClose()
	{
		ApplySettings();
		psUI.HideSettingsButton();
	}

	public void ApplySettings()
	{
		List<VRGameSettingsUI>[] array = settingUIs;
		for (int i = 0; i < array.Length; i++)
		{
			foreach (VRGameSettingsUI item in array[i])
			{
				item.SaveSetting();
			}
		}
		GameSettings.ApplyGameSettings(GameSettings.CurrentSettings);
		GameSettings.SaveGameSettings();
	}

	public void RevertSettings()
	{
		List<VRGameSettingsUI>[] array = settingUIs;
		for (int i = 0; i < array.Length; i++)
		{
			foreach (VRGameSettingsUI item in array[i])
			{
				item.RevertSetting();
			}
		}
	}

	public void NextPage()
	{
		currentPage.SetActive(value: false);
		currPageIdx = Mathf.Min(currPageIdx + 1, pages[(int)currentCat].Count - 1);
		currentPage = pages[(int)currentCat][currPageIdx];
		currentPage.SetActive(value: true);
		UpdatePageText();
	}

	public void PrevPage()
	{
		currentPage.SetActive(value: false);
		currPageIdx = Mathf.Max(currPageIdx - 1, 0);
		currentPage = pages[(int)currentCat][currPageIdx];
		currentPage.SetActive(value: true);
		UpdatePageText();
	}

	private void UpdatePageText()
	{
		if (pages[(int)currentCat].Count < 2)
		{
			pageControlsObj.SetActive(value: false);
			return;
		}
		pageControlsObj.SetActive(value: true);
		pageText.text = $"Page {currPageIdx + 1}/{pages[(int)currentCat].Count}";
	}

	public void GoToBindings()
	{
		bindingsWindowObject.SetActive(value: true);
		bindingsWindowObject.GetComponent<VRRemapperWindow>().OpenWindow();
		base.gameObject.SetActive(value: false);
	}
}
