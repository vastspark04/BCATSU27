using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VTMapGenSaveUI : MonoBehaviour
{
	public InputField textField;

	public Button saveButton;

	public ScrollRect scrollRect;

	public GameObject filenameTemplate;

	private float lineHeight;

	private string mapID;

	private List<GameObject> displayedFiles = new List<GameObject>();

	private List<string> existingFilesnames = new List<string>();

	private VTMapCustom map;

	private UnityAction<string> OnSaved;

	private UnityAction OnCancelled;

	private void Awake()
	{
		lineHeight = ((RectTransform)filenameTemplate.transform).rect.height;
		filenameTemplate.SetActive(value: false);
	}

	public void Open(VTMapCustom newMap, UnityAction<string> onSaved, UnityAction onCancelled)
	{
		base.gameObject.SetActive(value: true);
		saveButton.interactable = false;
		map = newMap;
		OnSaved = onSaved;
		OnCancelled = onCancelled;
		foreach (GameObject displayedFile in displayedFiles)
		{
			Object.Destroy(displayedFile);
		}
		displayedFiles = new List<GameObject>();
		existingFilesnames = new List<string>();
		int num = 0;
		foreach (string existingMapFilename in VTResources.GetExistingMapFilenames())
		{
			GameObject gameObject = Object.Instantiate(filenameTemplate, scrollRect.content);
			gameObject.SetActive(value: true);
			Vector3 localPosition = filenameTemplate.transform.localPosition;
			localPosition.y = (float)(-num) * lineHeight;
			gameObject.transform.localPosition = localPosition;
			gameObject.GetComponent<Text>().text = existingMapFilename;
			displayedFiles.Add(gameObject);
			existingFilesnames.Add(existingMapFilename);
			num++;
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 10f + (float)num * lineHeight);
		scrollRect.verticalNormalizedPosition = 1f;
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		textField.text = string.Empty;
		if (OnCancelled != null)
		{
			OnCancelled();
		}
	}

	public void OnEnteredText(string text)
	{
		saveButton.interactable = VTResources.IsValidFilename(text + ".vtm", existingFilesnames);
		mapID = text;
	}

	public void Save()
	{
		map.mapID = mapID;
		string mapDirectoryPath = VTResources.GetMapDirectoryPath(mapID);
		string mapFilePath = VTResources.GetMapFilePath(mapID);
		if (!Directory.Exists(mapDirectoryPath))
		{
			Directory.CreateDirectory(mapDirectoryPath);
		}
		map.SaveToConfigNode().SaveToFile(mapFilePath);
		if (OnSaved != null)
		{
			Debug.Log("Invoking onSaved with: " + mapID);
			OnSaved(mapID);
		}
		base.gameObject.SetActive(value: false);
		textField.text = string.Empty;
	}
}
