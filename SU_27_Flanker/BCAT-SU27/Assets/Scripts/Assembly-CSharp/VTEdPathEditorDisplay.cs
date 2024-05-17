using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdPathEditorDisplay : MonoBehaviour
{
	public class PathPointButton : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
	{
		public VTEdPathEditorDisplay display;

		public int idx;

		private float lastClickTime = -1f;

		public void OnPointerClick(PointerEventData eventData)
		{
			display.SelectIdx(idx);
			if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				display.FocusOnPoint(idx);
			}
			else
			{
				lastClickTime = Time.unscaledTime;
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			display.HoverPoint(idx);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			display.UnHoverPoint(idx);
		}
	}

	public VTScenarioEditor editor;

	public VTEditorPathsWindow pathsWindow;

	public GameObject buttonTemplate;

	public Transform selectionTf;

	private Transform highlightTf;

	public RectTransform scrollContentTf;

	private ScrollRect scrollRect;

	public Text nameText;

	public Button[] itemDependentButtons;

	public VTBoolProperty loopField;

	public VTEnumProperty pathModeField;

	private int selectedIdx = -1;

	private int highlightedIdx = -1;

	private bool movingPoint;

	private FollowPath path;

	private VTEditorPathRenderer pathRender;

	private float lineHeight;

	private List<GameObject> pointButtons = new List<GameObject>();

	public bool isOpen;

	private bool firstTime = true;

	private void Awake()
	{
		scrollRect = scrollContentTf.GetComponentInParent<ScrollRect>();
		lineHeight = ((RectTransform)buttonTemplate.transform).rect.height;
		buttonTemplate.SetActive(value: false);
		SelectIdx(-1);
		highlightTf = Object.Instantiate(selectionTf.gameObject, selectionTf.parent).transform;
		Color color = highlightTf.gameObject.GetComponent<Image>().color;
		color.a /= 4f;
		highlightTf.gameObject.GetComponent<Image>().color = color;
		highlightTf.transform.localPosition = new Vector3(0f, 100f);
		highlightTf.gameObject.SetActive(value: true);
		loopField.OnValueChanged += LoopField_OnValueChanged;
		pathModeField.OnPropertyValueChanged += PathModeField_OnPropertyValueChanged;
	}

	private void PathModeField_OnPropertyValueChanged(object arg0)
	{
		if ((bool)path)
		{
			Curve3D.PathModes pathMode = (Curve3D.PathModes)arg0;
			path.SetPathMode(pathMode);
		}
	}

	public void EditNameButton()
	{
		editor.textInputWindow.Display("Path Name", "Set a name for the path.", path.gameObject.name, 25, NameEdited);
	}

	private void NameEdited(string n)
	{
		if ((bool)path)
		{
			string text = ConfigNodeUtils.SanitizeInputString(n);
			path.gameObject.name = text;
			nameText.text = text;
		}
	}

	private void LoopField_OnValueChanged(bool arg0)
	{
		if ((bool)path)
		{
			path.loop = !path.loop;
			path.SetupCurve();
		}
	}

	public void OpenForPath(int pathID)
	{
		path = editor.currentScenario.paths.GetPath(pathID);
		pathRender = path.GetComponent<VTEditorPathRenderer>();
		pathRender.StartEditing();
		nameText.text = path.gameObject.name;
		loopField.SetInitialValue(path.loop);
		pathModeField.SetInitialValue(path.pathMode);
		base.gameObject.SetActive(value: true);
		if (firstTime)
		{
			firstTime = false;
			StartCoroutine(DelayedSetupPointsList());
		}
		else
		{
			SetupPointsList();
			SelectIdx(-1);
		}
		isOpen = true;
		highlightTf.transform.localPosition = new Vector3(0f, 100f);
		StopMovingPoint();
	}

	private IEnumerator DelayedSetupPointsList()
	{
		yield return null;
		yield return null;
		SetupPointsList();
		SelectIdx(-1);
	}

	private void Update()
	{
		if (movingPoint)
		{
			path.pointTransforms[selectedIdx].position = editor.editorCamera.focusTransform.position;
			if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
			{
				StopMovingPoint();
			}
			path.SetupCurve();
		}
	}

	private void SetupPointsList()
	{
		foreach (GameObject pointButton in pointButtons)
		{
			Object.Destroy(pointButton);
		}
		pointButtons = new List<GameObject>();
		if (path.pointTransforms != null && path.pointTransforms.Length != 0)
		{
			for (int i = 0; i < path.pointTransforms.Length; i++)
			{
				Transform transform = path.pointTransforms[i];
				GameObject gameObject = Object.Instantiate(buttonTemplate, scrollContentTf);
				gameObject.SetActive(value: true);
				gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
				Vector3D vector3D = VTMapManager.fetch.WorldPositionToGPSCoords(transform.position);
				gameObject.GetComponentInChildren<Text>().text = "N:" + VTMapManager.FormattedCoordsMinSec(vector3D.x) + " E:" + VTMapManager.FormattedCoordsMinSec(vector3D.y) + "\nAlt: " + vector3D.z.ToString("0") + " m";
				PathPointButton pathPointButton = gameObject.AddComponent<PathPointButton>();
				pathPointButton.display = this;
				pathPointButton.idx = i;
				pointButtons.Add(gameObject);
			}
			scrollContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)path.pointTransforms.Length * lineHeight);
			scrollRect.ClampVertical();
		}
		else
		{
			scrollContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeight);
		}
	}

	public void SelectIdx(int idx)
	{
		selectedIdx = idx;
		if (selectedIdx >= 0)
		{
			itemDependentButtons.SetInteractable(interactable: true);
			selectionTf.gameObject.SetActive(value: true);
			selectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight);
			pathRender.SelectPoint(idx);
		}
		else
		{
			selectionTf.gameObject.SetActive(value: false);
			itemDependentButtons.SetInteractable(interactable: false);
		}
		StopMovingPoint();
	}

	public void NewPointButton()
	{
		if (path.pointTransforms == null || path.pointTransforms.Length == 0)
		{
			GameObject gameObject = new GameObject("PathPoint");
			gameObject.transform.position = editor.editorCamera.focusTransform.position;
			path.transform.position = gameObject.transform.position;
			gameObject.transform.parent = path.transform;
			path.pointTransforms = new Transform[1] { gameObject.transform };
		}
		else
		{
			GameObject gameObject2 = new GameObject("PathPoint");
			gameObject2.transform.position = editor.editorCamera.focusTransform.position;
			gameObject2.transform.parent = path.transform;
			Transform[] array = new Transform[path.pointTransforms.Length + 1];
			for (int i = 0; i < path.pointTransforms.Length; i++)
			{
				array[i] = path.pointTransforms[i];
			}
			array[array.Length - 1] = gameObject2.transform;
			path.pointTransforms = array;
			path.SetupCurve();
		}
		SetupPointsList();
		SelectIdx(path.pointTransforms.Length - 1);
	}

	public void RemovePointButton()
	{
		Transform transform = path.pointTransforms[selectedIdx];
		path.pointTransforms[selectedIdx] = null;
		Transform[] array = new Transform[path.pointTransforms.Length - 1];
		int i = 0;
		int num = 0;
		for (; i < path.pointTransforms.Length; i++)
		{
			if (path.pointTransforms[i] != null)
			{
				array[num] = path.pointTransforms[i];
				num++;
			}
		}
		path.pointTransforms = array;
		path.SetupCurve();
		Object.Destroy(transform.gameObject);
		SelectIdx(-1);
		SetupPointsList();
	}

	public void MovePointButton()
	{
		if (movingPoint)
		{
			StopMovingPoint();
		}
		else
		{
			StartMovingPoint();
		}
	}

	private void StartMovingPoint()
	{
		if (!movingPoint)
		{
			movingPoint = true;
			editor.popupMessages.DisplayPersistentMessage("Moving point...", Color.yellow, "movingPathPoint");
			path.uniformlyPartition = false;
			editor.canClickUnits = false;
		}
	}

	private void StopMovingPoint()
	{
		if (movingPoint)
		{
			movingPoint = false;
			path.uniformlyPartition = true;
			path.SetupCurve();
			editor.popupMessages.RemovePersistentMessage("movingPathPoint");
			editor.canClickUnits = true;
		}
	}

	public void ShiftUpButton()
	{
		if (selectedIdx > 0)
		{
			Transform transform = path.pointTransforms[selectedIdx - 1];
			path.pointTransforms[selectedIdx - 1] = path.pointTransforms[selectedIdx];
			path.pointTransforms[selectedIdx] = transform;
			path.SetupCurve();
			SetupPointsList();
			SelectIdx(selectedIdx - 1);
		}
	}

	public void ShiftDownButton()
	{
		if (selectedIdx < path.pointTransforms.Length - 1)
		{
			Transform transform = path.pointTransforms[selectedIdx + 1];
			path.pointTransforms[selectedIdx + 1] = path.pointTransforms[selectedIdx];
			path.pointTransforms[selectedIdx] = transform;
			path.SetupCurve();
			SetupPointsList();
			SelectIdx(selectedIdx + 1);
		}
	}

	public void ReverseButton()
	{
		if (path.pointTransforms != null && path.pointTransforms.Length > 1)
		{
			int num = path.pointTransforms.Length;
			Transform[] array = new Transform[num];
			int num2 = 0;
			int num3 = num - 1;
			while (num2 < num)
			{
				array[num2] = path.pointTransforms[num3];
				num2++;
				num3--;
			}
			path.pointTransforms = array;
			path.SetupCurve();
			SetupPointsList();
			SelectIdx(selectedIdx);
		}
	}

	public void BackButton()
	{
		if ((bool)path)
		{
			path.GetComponent<VTEditorPathRenderer>().StopEditing();
		}
		StopMovingPoint();
		base.gameObject.SetActive(value: false);
		pathsWindow.pathsDisplay.Open();
		isOpen = false;
	}

	public void FocusOnPoint(int idx)
	{
		editor.editorCamera.FocusOnPoint(path.pointTransforms[idx].position);
	}

	public void HoverPoint(int idx)
	{
		pathRender.HighlightPoint(idx);
		highlightTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
		highlightedIdx = idx;
	}

	public void UnHoverPoint(int idx)
	{
		pathRender.UnHighlightPoint(idx);
		if (highlightedIdx == idx)
		{
			highlightTf.transform.localPosition = new Vector3(0f, 100f);
			highlightedIdx = -1;
		}
	}
}
