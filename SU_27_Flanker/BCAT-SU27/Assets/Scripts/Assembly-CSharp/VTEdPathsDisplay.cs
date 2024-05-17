using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdPathsDisplay : MonoBehaviour
{
	public class PathItemButton : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public VTEdPathsDisplay display;

		public int idx;

		private float clickTime;

		public void OnPointerClick(PointerEventData eventData)
		{
			display.SelectPath(idx);
			if (Time.unscaledTime - clickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				display.EditPathButton();
			}
			clickTime = Time.unscaledTime;
		}
	}

	public VTScenarioEditor editor;

	public VTEditorPathsWindow pathsWindow;

	public Button deleteButton;

	public Button editButton;

	public RectTransform scrollContentTf;

	private ScrollRect scrollRect;

	public GameObject pathButtonTemplate;

	public Transform selectionTf;

	private int selectedIdx = -1;

	private List<PathItemButton> buttons = new List<PathItemButton>();

	private int[] pathIDs;

	private float lineHeight;

	private void Awake()
	{
		pathButtonTemplate.SetActive(value: false);
		lineHeight = ((RectTransform)pathButtonTemplate.transform).rect.height;
		scrollRect = scrollContentTf.GetComponentInParent<ScrollRect>();
	}

	private void Start()
	{
		SelectPath(-1);
	}

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		SetupPathList();
		SelectPath(-1);
	}

	public void Close()
	{
		SelectPath(-1);
		base.gameObject.SetActive(value: false);
	}

	public void NewPathButton()
	{
		GameObject obj = new GameObject("New Path");
		FollowPath followPath = obj.AddComponent<FollowPath>();
		followPath.uniformlyPartition = true;
		VTEditorPathRenderer vTEditorPathRenderer = obj.AddComponent<VTEditorPathRenderer>();
		vTEditorPathRenderer.cameraTransform = editor.editorCamera.transform;
		vTEditorPathRenderer.arrowMaterial = editor.pathMaterial;
		vTEditorPathRenderer.lineWidth = editor.pathLineWidth;
		vTEditorPathRenderer.arrowMesh = editor.pathArrowMesh;
		obj.transform.position = editor.editorCamera.focusTransform.position;
		int pathID = editor.currentScenario.paths.AddPath(followPath);
		obj.AddComponent<FloatingOriginTransform>();
		pathsWindow.pathsEditorDisplay.OpenForPath(pathID);
		Close();
	}

	public void DeletePathButton()
	{
		editor.confirmDialogue.DisplayConfirmation("Delete Path", "Are you sure you want to delete the whole path?", FinallyDelete, null);
	}

	private void FinallyDelete()
	{
		editor.currentScenario.paths.RemovePath(pathIDs[selectedIdx]);
		SetupPathList();
		SelectPath(-1);
	}

	private void SetupPathList()
	{
		foreach (PathItemButton button in buttons)
		{
			Object.Destroy(button.gameObject);
		}
		buttons = new List<PathItemButton>();
		pathIDs = new int[editor.currentScenario.paths.paths.Count];
		int num = 0;
		foreach (int key in editor.currentScenario.paths.paths.Keys)
		{
			GameObject obj = Object.Instantiate(pathButtonTemplate, scrollContentTf);
			obj.transform.localPosition = new Vector3(0f, (float)(-num) * lineHeight, 0f);
			obj.SetActive(value: true);
			PathItemButton pathItemButton = obj.AddComponent<PathItemButton>();
			pathItemButton.idx = num;
			pathItemButton.display = this;
			buttons.Add(pathItemButton);
			pathIDs[num] = key;
			obj.GetComponentInChildren<Text>().text = editor.currentScenario.paths.paths[key].gameObject.name;
			num++;
		}
		scrollContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num * lineHeight);
		scrollRect.ClampVertical();
	}

	public void EditPathButton()
	{
		pathsWindow.pathsEditorDisplay.OpenForPath(pathIDs[selectedIdx]);
		Close();
	}

	public void SelectPath(int idx)
	{
		selectedIdx = idx;
		if (selectedIdx >= 0)
		{
			selectionTf.gameObject.SetActive(value: true);
			Vector3 localPosition = selectionTf.localPosition;
			localPosition.y = (float)(-selectedIdx) * lineHeight;
			selectionTf.localPosition = localPosition;
			editButton.interactable = true;
			deleteButton.interactable = true;
		}
		else
		{
			selectionTf.gameObject.SetActive(value: false);
			editButton.interactable = false;
			deleteButton.interactable = false;
		}
	}
}
