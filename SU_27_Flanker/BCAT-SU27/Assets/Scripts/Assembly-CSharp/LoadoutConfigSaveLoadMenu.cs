using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadoutConfigSaveLoadMenu : MonoBehaviour
{
	public LoadoutConfigurator configurator;

	public GameObject menuDisplayObject;

	public GameObject mainDisplay;

	public VRKeyboard keyboard;

	[Header("Load List")]
	public ScrollRect loadScrollRect;

	public GameObject loadTemplate;

	private List<GameObject> loadObjs = new List<GameObject>();

	[Header("Save Over List")]
	public GameObject saveOverDisplayObj;

	public ScrollRect saveScrollRect;

	public GameObject saveTemplate;

	private List<GameObject> saveObjs = new List<GameObject>();

	[Header("Delete Menu")]
	public GameObject deleteDisplayObj;

	public ScrollRect deleteScrollRect;

	public GameObject deleteItemTemplate;

	private List<GameObject> deleteObjs = new List<GameObject>();

	private void SetupLoadList()
	{
		foreach (GameObject loadObj in loadObjs)
		{
			Object.Destroy(loadObj);
		}
		loadObjs.Clear();
		loadTemplate.SetActive(value: false);
		float num = ((RectTransform)loadTemplate.transform).rect.height * loadTemplate.transform.localScale.y;
		int num2 = 0;
		foreach (string key in PilotSaveManager.current.lastVehicleSave.savedLoadouts.Keys)
		{
			GameObject gameObject = Object.Instantiate(loadTemplate, loadScrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-num2) * num, 0f);
			gameObject.transform.localRotation = Quaternion.identity;
			gameObject.GetComponent<LoadoutConfigSavedItem>().Setup(key, configurator);
			loadObjs.Add(gameObject);
			num2++;
		}
		loadScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num2 * num);
		loadScrollRect.ClampVertical();
	}

	private void SetupSaveList()
	{
		foreach (GameObject saveObj in saveObjs)
		{
			Object.Destroy(saveObj);
		}
		saveObjs.Clear();
		saveTemplate.SetActive(value: false);
		float num = ((RectTransform)saveTemplate.transform).rect.height * saveTemplate.transform.localScale.y;
		int num2 = 0;
		foreach (string key in PilotSaveManager.current.lastVehicleSave.savedLoadouts.Keys)
		{
			GameObject gameObject = Object.Instantiate(saveTemplate, saveScrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-num2) * num, 0f);
			gameObject.transform.localRotation = Quaternion.identity;
			LoadoutConfigSavedItem component = gameObject.GetComponent<LoadoutConfigSavedItem>();
			component.Setup(key, configurator);
			component.GetComponentInChildren<VRInteractable>().OnInteract.AddListener(CancelSaveOverButton);
			saveObjs.Add(gameObject);
			num2++;
		}
		saveScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num2 * num);
		saveScrollRect.ClampVertical();
	}

	public void SaveNewButton()
	{
		mainDisplay.SetActive(value: false);
		keyboard.Display(string.Empty, 24, OnNewSaveName, OnCancelSave);
	}

	private void OnNewSaveName(string newName)
	{
		mainDisplay.SetActive(value: true);
		configurator.SaveLoadout(newName);
		SetupLoadList();
	}

	private void OnCancelSave()
	{
		mainDisplay.SetActive(value: true);
	}

	public void SaveOverButton()
	{
		saveOverDisplayObj.SetActive(value: true);
		mainDisplay.SetActive(value: false);
		SetupSaveList();
	}

	public void CancelSaveOverButton()
	{
		saveOverDisplayObj.SetActive(value: false);
		mainDisplay.SetActive(value: true);
	}

	public void BackButton()
	{
		menuDisplayObject.SetActive(value: false);
		configurator.fullInfo.hpDisplayObject.SetActive(value: true);
	}

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		menuDisplayObject.SetActive(value: true);
		configurator.fullInfo.hpDisplayObject.SetActive(value: false);
		SetupLoadList();
	}

	public void DeleteMenuButton()
	{
		mainDisplay.SetActive(value: false);
		deleteDisplayObj.SetActive(value: true);
		SetupDeleteList();
	}

	public void BackFromSubMenu()
	{
		deleteDisplayObj.SetActive(value: false);
		saveOverDisplayObj.SetActive(value: false);
		mainDisplay.SetActive(value: true);
		SetupLoadList();
	}

	private void SetupDeleteList()
	{
		foreach (GameObject deleteObj in deleteObjs)
		{
			Object.Destroy(deleteObj);
		}
		deleteObjs.Clear();
		float num = ((RectTransform)deleteItemTemplate.transform).rect.height * deleteItemTemplate.transform.localScale.y;
		deleteItemTemplate.SetActive(value: false);
		int num2 = 0;
		foreach (string key in PilotSaveManager.current.lastVehicleSave.savedLoadouts.Keys)
		{
			GameObject gameObject = Object.Instantiate(deleteItemTemplate, deleteScrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-num2) * num, 0f);
			gameObject.transform.localRotation = Quaternion.identity;
			LoadoutConfigSavedItem component = gameObject.GetComponent<LoadoutConfigSavedItem>();
			component.Setup(key, configurator);
			component.GetComponentInChildren<VRInteractable>().OnInteract.AddListener(OnDeleted);
			deleteObjs.Add(gameObject);
			num2++;
		}
		deleteScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num2 * num);
		deleteScrollRect.ClampVertical();
	}

	private void OnDeleted()
	{
		StartCoroutine(DelayedSetupDeleteMenu());
	}

	private IEnumerator DelayedSetupDeleteMenu()
	{
		yield return null;
		SetupDeleteList();
	}
}
