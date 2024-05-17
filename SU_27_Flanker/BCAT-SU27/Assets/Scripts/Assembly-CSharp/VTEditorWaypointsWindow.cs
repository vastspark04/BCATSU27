using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEditorWaypointsWindow : VTEdUITab
{
	public class WaypointListItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public int idx;

		public VTEditorWaypointsWindow window;

		private float lastClickTime = -1f;

		public void OnPointerClick(PointerEventData eventData)
		{
			if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				window.FocusOnPoint(idx);
			}
			else
			{
				lastClickTime = Time.unscaledTime;
			}
			window.SelectPoint(idx);
		}
	}

	public VTScenarioEditor editor;

	public Button[] pointDependentButtons;

	public GameObject listItemTemplate;

	public RectTransform listContentTf;

	private ScrollRect listScrollRect;

	public Transform selectionBox;

	public GameObject movingUIObj;

	private List<GameObject> listItems = new List<GameObject>();

	private Waypoint[] waypoints;

	private float lineHeight;

	private int selectIdx = -1;

	private bool firstOpen = true;

	private bool moving;

	private Waypoint movingWaypoint;

	private void Awake()
	{
		listItemTemplate.SetActive(value: false);
		selectionBox.transform.localPosition = new Vector3(0f, 100f, 0f);
		lineHeight = ((RectTransform)listItemTemplate.transform).rect.height;
		listScrollRect = listContentTf.GetComponentInParent<ScrollRect>();
	}

	private void ConstructList()
	{
		foreach (GameObject listItem in listItems)
		{
			Object.Destroy(listItem);
		}
		listItems = new List<GameObject>();
		waypoints = editor.currentScenario.waypoints.GetWaypoints();
		for (int i = 0; i < waypoints.Length; i++)
		{
			GameObject gameObject = Object.Instantiate(listItemTemplate, listContentTf);
			gameObject.SetActive(value: true);
			WaypointListItem waypointListItem = gameObject.AddComponent<WaypointListItem>();
			waypointListItem.idx = i;
			waypointListItem.window = this;
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			gameObject.GetComponentInChildren<Text>().text = waypoints[i].name;
			listItems.Add(gameObject);
		}
		listContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeight * (float)waypoints.Length);
		listScrollRect.ClampVertical();
		SelectPoint(-1);
	}

	private IEnumerator DelayedConstruct()
	{
		yield return null;
		ConstructList();
	}

	public override void OnOpenedTab()
	{
		if (firstOpen)
		{
			firstOpen = false;
			StartCoroutine(DelayedConstruct());
		}
		else
		{
			ConstructList();
		}
		editor.OnScenarioLoaded += Editor_OnScenarioLoaded;
	}

	private void Editor_OnScenarioLoaded()
	{
		ConstructList();
	}

	public override void OnClosedTab()
	{
		CancelMove();
		SelectPoint(-1);
		editor.OnScenarioLoaded -= Editor_OnScenarioLoaded;
	}

	public void SelectPoint(int idx)
	{
		selectIdx = idx;
		selectionBox.transform.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
		pointDependentButtons.SetInteractable(idx >= 0);
	}

	public void FocusOnPoint(int idx)
	{
		Vector3 point = VTMapManager.GlobalToWorldPoint(waypoints[idx].globalPoint);
		editor.editorCamera.FocusOnPoint(point);
	}

	public void DeleteButton()
	{
		editor.confirmDialogue.DisplayConfirmation("Delete Waypoint?", "Are you sure you want to delete " + waypoints[selectIdx].name + "?", FinallyDelete, null);
	}

	private void FinallyDelete()
	{
		editor.currentScenario.waypoints.RemoveWaypoint(waypoints[selectIdx].id);
		ConstructList();
	}

	public void MoveButton()
	{
		StartCoroutine(MoveRoutine());
	}

	public void RenameButton()
	{
		editor.textInputWindow.Display("Rename Waypoint", "Enter a name for the waypoint.", waypoints[selectIdx].name, 40, OnRenamed);
	}

	private void OnRenamed(string text)
	{
		waypoints[selectIdx].name = text;
		listItems[selectIdx].GetComponentInChildren<Text>().text = text;
	}

	private IEnumerator MoveRoutine()
	{
		editor.canClickUnits = false;
		editor.popupMessages.DisplayPersistentMessage("Moving waypoint...", Color.yellow, "movingWaypoint");
		movingUIObj.SetActive(value: true);
		moving = true;
		movingWaypoint = waypoints[selectIdx];
		Transform movingTransform = movingWaypoint.GetTransform();
		while (moving)
		{
			movingTransform.position = editor.editorCamera.focusTransform.position;
			if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
			{
				ApplyMove();
			}
			else if (Input.GetKeyDown(KeyCode.Escape))
			{
				CancelMove();
			}
			yield return null;
		}
	}

	public void CancelMove()
	{
		if (moving)
		{
			editor.popupMessages.RemovePersistentMessage("movingWaypoint");
			moving = false;
			if (movingWaypoint != null && movingWaypoint.GetTransform() != null)
			{
				movingWaypoint.GetTransform().position = VTMapManager.GlobalToWorldPoint(movingWaypoint.globalPoint);
			}
			movingUIObj.SetActive(value: false);
			editor.canClickUnits = true;
		}
	}

	public void ApplyMove()
	{
		if (moving)
		{
			editor.popupMessages.RemovePersistentMessage("movingWaypoint");
			moving = false;
			if (movingWaypoint != null && movingWaypoint.GetTransform() != null)
			{
				movingWaypoint.globalPoint = VTMapManager.WorldToGlobalPoint(movingWaypoint.GetTransform().position);
			}
			movingUIObj.SetActive(value: false);
			editor.canClickUnits = true;
		}
	}

	public void NewWaypointButton()
	{
		Transform transform = new GameObject("New Waypoint").transform;
		transform.position = editor.editorCamera.focusTransform.position;
		Waypoint waypoint = editor.currentScenario.waypoints.AddWaypoint(transform, "New Waypoint");
		ConstructList();
		SelectPoint(waypoints.Length - 1);
		transform.gameObject.AddComponent<VTEdWaypointRenderer>().Setup(editor, waypoint);
	}
}
