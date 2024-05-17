using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEditorOpenMenu : MonoBehaviour
{
	public class ScenarioFileButton : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
	{
		public VTEditorOpenMenu menu;

		public int idx;

		private float lastClickTime;

		public void OnPointerDown(PointerEventData eventData)
		{
			menu.PressFileButton(idx);
			if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				menu.OpenButton();
			}
			else
			{
				lastClickTime = Time.unscaledTime;
			}
		}
	}

	public VTScenarioEditor editor;

	public GameObject fileNameTemplate;

	public RectTransform scrollContentTf;

	private ScrollRect scrollRect;

	public Transform selectionBoxTransform;

	public Text nameText;

	public Text descriptionText;

	public Button openButton;

	public Button deleteButton;

	public RawImage scenarioImage;

	private List<VTScenarioInfo> scenarioInfos;

	private List<GameObject> fileButtons;

	private int currIdx = -1;

	private float lineHeight;

	private bool open;

	private void Awake()
	{
		lineHeight = ((RectTransform)fileNameTemplate.transform).rect.height;
		fileNameTemplate.SetActive(value: false);
		selectionBoxTransform.gameObject.SetActive(value: false);
		scrollRect = scrollContentTf.GetComponentInParent<ScrollRect>();
	}

	public void OpenMenu()
	{
		if (!open)
		{
			open = true;
			editor.BlockEditor(base.transform);
			editor.editorCamera.inputLock.AddLock("openMenu");
			base.gameObject.SetActive(value: true);
			StartCoroutine(SetupFileList());
		}
	}

	public void CloseMenu()
	{
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("openMenu");
		base.gameObject.SetActive(value: false);
		open = false;
		if (editor.currentScenario == null)
		{
			editor.OpenIntroWindow();
		}
	}

	public void OpenButton()
	{
		if (currIdx >= 0)
		{
			if (editor.currentScenario != null)
			{
				editor.confirmDialogue.DisplayConfirmation("Open?", "Opening the scenario will cause all unsaved changes to be lost.", FinallyOpen, null);
			}
			else
			{
				FinallyOpen();
			}
		}
	}

	private void FinallyOpen()
	{
		VTScenarioInfo info = scenarioInfos[currIdx];
		editor.LoadScenario(info);
		editor.Recenter();
		CloseMenu();
	}

	public void PressFileButton(int idx)
	{
		Vector3 localPosition = fileNameTemplate.transform.localPosition;
		localPosition.y -= (float)idx * lineHeight;
		selectionBoxTransform.localPosition = localPosition;
		selectionBoxTransform.gameObject.SetActive(value: true);
		currIdx = idx;
		VTScenarioInfo vTScenarioInfo = scenarioInfos[idx];
		nameText.text = vTScenarioInfo.name;
		descriptionText.text = vTScenarioInfo.description;
		if ((bool)vTScenarioInfo.image)
		{
			scenarioImage.texture = vTScenarioInfo.image;
			scenarioImage.enabled = true;
		}
		else
		{
			scenarioImage.enabled = false;
		}
		openButton.interactable = true;
		deleteButton.interactable = true;
	}

	public void DeleteButton()
	{
		if (currIdx >= 0)
		{
			editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete the scenario?", FinallyDelete, null);
		}
	}

	private void FinallyDelete()
	{
		string id = scenarioInfos[currIdx].id;
		VTResources.DeleteCustomScenario(id, null);
		StartCoroutine(SetupFileList());
		if (editor.currentScenario != null && id == editor.currentScenario.scenarioID)
		{
			CloseMenu();
			editor.ForceReturnToIntroWindow();
		}
	}

	private IEnumerator SetupFileList()
	{
		VTResources.LoadCustomScenarios();
		if (string.IsNullOrEmpty(VTScenarioEditor.currentCampaign))
		{
			scenarioInfos = VTResources.GetCustomScenarios();
			scenarioInfos.Sort((VTScenarioInfo a, VTScenarioInfo b) => a.id.CompareTo(b.id));
		}
		else
		{
			scenarioInfos = VTResources.GetCustomCampaign(VTScenarioEditor.currentCampaign).allScenarios;
			scenarioInfos.Sort((VTScenarioInfo a, VTScenarioInfo b) => a.campaignOrderIdx.CompareTo(b.campaignOrderIdx));
		}
		scenarioImage.enabled = false;
		nameText.text = string.Empty;
		descriptionText.text = string.Empty;
		selectionBoxTransform.gameObject.SetActive(value: false);
		openButton.interactable = false;
		deleteButton.interactable = false;
		currIdx = -1;
		if (fileButtons != null)
		{
			foreach (GameObject fileButton in fileButtons)
			{
				Object.Destroy(fileButton);
			}
		}
		fileButtons = new List<GameObject>();
		yield return null;
		int num = 0;
		foreach (VTScenarioInfo scenarioInfo in scenarioInfos)
		{
			GameObject obj = Object.Instantiate(fileNameTemplate, scrollContentTf);
			obj.SetActive(value: true);
			ScenarioFileButton scenarioFileButton = obj.AddComponent<ScenarioFileButton>();
			scenarioFileButton.idx = num;
			scenarioFileButton.menu = this;
			fileButtons.Add(scenarioFileButton.gameObject);
			obj.GetComponent<Text>().text = scenarioInfo.id;
			RectTransform obj2 = (RectTransform)obj.transform;
			Vector3 localPosition = fileNameTemplate.transform.localPosition;
			localPosition.y = 0f - (5f + (float)num * lineHeight);
			obj2.localPosition = localPosition;
			num++;
		}
		scrollContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 10f + (float)num * lineHeight);
		scrollRect.ClampVertical();
	}
}
