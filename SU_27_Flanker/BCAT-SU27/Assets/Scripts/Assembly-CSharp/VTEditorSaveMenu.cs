using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEditorSaveMenu : MonoBehaviour
{
	public VTScenarioEditor editor;

	public InputField textField;

	public Button saveButton;

	public RectTransform scrollContentTf;

	private ScrollRect scrollRect;

	public GameObject filenameTemplate;

	private float lineHeight;

	private string filename;

	private List<GameObject> displayedFiles = new List<GameObject>();

	private List<string> existingFilesnames = new List<string>();

	private void Awake()
	{
		lineHeight = ((RectTransform)filenameTemplate.transform).rect.height;
		filenameTemplate.SetActive(value: false);
		scrollRect = scrollContentTf.GetComponentInParent<ScrollRect>();
	}

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		editor.editorCamera.inputLock.AddLock("saveMenu");
		saveButton.interactable = false;
		editor.BlockEditor(base.transform);
		foreach (GameObject displayedFile in displayedFiles)
		{
			Object.Destroy(displayedFile);
		}
		displayedFiles = new List<GameObject>();
		existingFilesnames = new List<string>();
		VTResources.LoadCustomScenarios();
		List<VTScenarioInfo> list = ((!string.IsNullOrEmpty(VTScenarioEditor.currentCampaign)) ? VTResources.GetCustomCampaign(VTScenarioEditor.currentCampaign).allScenarios : VTResources.GetCustomScenarios());
		int num = 0;
		foreach (VTScenarioInfo item in list)
		{
			GameObject gameObject = Object.Instantiate(filenameTemplate, scrollContentTf);
			gameObject.SetActive(value: true);
			Vector3 localPosition = filenameTemplate.transform.localPosition;
			localPosition.y = (float)(-num) * lineHeight;
			gameObject.transform.localPosition = localPosition;
			gameObject.GetComponent<Text>().text = item.id + ".vts";
			displayedFiles.Add(gameObject);
			existingFilesnames.Add(item.id);
			num++;
		}
		scrollContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 10f + (float)num * lineHeight);
		scrollRect.verticalNormalizedPosition = 1f;
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		editor.editorCamera.inputLock.RemoveLock("saveMenu");
		editor.UnblockEditor(base.transform);
		textField.text = string.Empty;
	}

	public void OnEnteredText(string text)
	{
		saveButton.interactable = VTResources.IsValidFilename(text, existingFilesnames);
		filename = text;
	}

	public void Save()
	{
		if (!string.IsNullOrEmpty(VTScenarioEditor.currentCampaign))
		{
			foreach (VTScenarioInfo allScenario in VTResources.GetCustomCampaign(VTScenarioEditor.currentCampaign).allScenarios)
			{
				if (allScenario.mapID == filename)
				{
					editor.confirmDialogue.DisplayConfirmation("Invalid Name", "The filename conflicts with an existing map in the campaign!", null, null);
					return;
				}
			}
		}
		if (!string.IsNullOrEmpty(editor.currentScenario.scenarioID))
		{
			editor.SaveToNewName(filename);
		}
		else
		{
			editor.currentScenario.scenarioID = filename;
			editor.Save();
		}
		Close();
	}
}
