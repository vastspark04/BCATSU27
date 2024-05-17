using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTStaticObjectsWindow : VTEdUITab
{
	public class StaticObjListItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public int id;

		public int listIdx;

		public VTStaticObjectsWindow window;

		private float lastClickTime;

		public void OnPointerClick(PointerEventData e)
		{
			if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				window.GoToStaticObj(id);
				return;
			}
			lastClickTime = Time.unscaledTime;
			window.SelectObject(listIdx);
		}
	}

	public VTScenarioEditor editor;

	public ScrollRect scrollRect;

	public GameObject itemTemplate;

	public Transform selectionTf;

	public Button[] itemDependentButtons;

	public VTEdStaticObjectBrowser objectBrowser;

	private float lineHeight;

	private List<VTStaticObject> staticObjects = new List<VTStaticObject>();

	private List<GameObject> listObjects = new List<GameObject>();

	private int currIdx = -1;

	private List<VTStaticObject> allPrefabs;

	private Coroutine moveRoutine;

	private bool moving;

	private VTStaticObject movingObj;

	private FixedPoint prevPoint;

	private Quaternion prevRot;

	private void Start()
	{
		editor.OnScenarioLoaded += Editor_OnScenarioLoaded;
		editor.OnBeforeSave += Editor_OnBeforeSave;
	}

	private void Editor_OnBeforeSave()
	{
		if (base.isOpen && moving)
		{
			CancelMove();
		}
	}

	private void Editor_OnScenarioLoaded()
	{
		if (base.isOpen)
		{
			if (moving)
			{
				CancelMove();
			}
			SetupList();
		}
	}

	public override void OnOpenedTab()
	{
		base.OnOpenedTab();
		SetupList();
		SelectObject(-1);
	}

	public override void OnClosedTab()
	{
		base.OnClosedTab();
		if (moving)
		{
			CancelMove();
		}
	}

	public void SetupList()
	{
		lineHeight = ((RectTransform)itemTemplate.transform).rect.height;
		foreach (GameObject listObject in listObjects)
		{
			Object.Destroy(listObject);
		}
		listObjects.Clear();
		staticObjects.Clear();
		staticObjects = editor.currentScenario.staticObjects.GetAllObjects();
		for (int i = 0; i < staticObjects.Count; i++)
		{
			VTStaticObject vTStaticObject = staticObjects[i];
			GameObject gameObject = Object.Instantiate(itemTemplate, scrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			gameObject.GetComponentInChildren<Text>().text = vTStaticObject.GetUIDisplayName();
			StaticObjListItem staticObjListItem = gameObject.AddComponent<StaticObjListItem>();
			staticObjListItem.id = vTStaticObject.id;
			staticObjListItem.listIdx = i;
			staticObjListItem.window = this;
			listObjects.Add(gameObject);
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)staticObjects.Count * lineHeight);
		scrollRect.ClampVertical();
		itemTemplate.SetActive(value: false);
		SelectObject(-1);
	}

	public void GoToStaticObj(int id)
	{
		VTStaticObject @object = editor.currentScenario.staticObjects.GetObject(id);
		if ((bool)@object)
		{
			editor.editorCamera.FocusOnPoint(@object.transform.position);
		}
	}

	public void SelectObject(int idx)
	{
		selectionTf.gameObject.SetActive(idx >= 0);
		itemDependentButtons.SetInteractable(idx >= 0);
		selectionTf.localPosition = new Vector3(0f, (0f - lineHeight) * (float)idx, 0f);
		currIdx = idx;
	}

	public void NewButton()
	{
		if (moving)
		{
			CancelMove();
		}
		objectBrowser.Open();
	}

	private void OnSelectedNewObject(object o)
	{
		if (o != null)
		{
			VTStaticObject vTStaticObject = (VTStaticObject)o;
			VTStaticObject vTStaticObject2 = editor.currentScenario.staticObjects.CreateObject(vTStaticObject.gameObject);
			vTStaticObject2.transform.position = editor.editorCamera.focusTransform.position;
			vTStaticObject2.transform.rotation = editor.editorCamera.focusTransform.rotation;
			UnitSpawner.PlacementValidityInfo placementValidityInfo = vTStaticObject2.TryPlaceInEditor(editor.editorCamera.cursorLocation);
			if (placementValidityInfo.isValid)
			{
				SetupList();
				return;
			}
			editor.currentScenario.staticObjects.RemoveObject(vTStaticObject2.id);
			editor.confirmDialogue.DisplayConfirmation("Invalid Placement", placementValidityInfo.reason, null, null);
		}
	}

	public void DeleteButton()
	{
		if (moving)
		{
			CancelMove();
		}
		if (currIdx >= 0)
		{
			editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this static object?", FinallyDelete, null);
		}
	}

	private void FinallyDelete()
	{
		int a = currIdx;
		int id = staticObjects[currIdx].id;
		editor.currentScenario.staticObjects.RemoveObject(id);
		SetupList();
		SelectObject(Mathf.Min(a, staticObjects.Count - 1));
	}

	public void SelectLast()
	{
		SelectObject(staticObjects.Count - 1);
		scrollRect.verticalNormalizedPosition = 0f;
	}

	public void MoveButton()
	{
		if (moving)
		{
			if (moveRoutine != null)
			{
				StopCoroutine(moveRoutine);
			}
			moving = false;
			InputStopMove();
			return;
		}
		if (moveRoutine != null)
		{
			StopCoroutine(moveRoutine);
		}
		movingObj = staticObjects[currIdx];
		Collider[] componentsInChildren = movingObj.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if (collider.gameObject.layer == 0)
			{
				collider.gameObject.layer = 1;
			}
		}
		prevPoint = new FixedPoint(movingObj.transform.position);
		prevRot = movingObj.transform.rotation;
		editor.popupMessages.DisplayPersistentMessage("Moving object...", Color.yellow, "staticMove");
		moveRoutine = StartCoroutine(MoveRoutine());
	}

	private IEnumerator MoveRoutine()
	{
		moving = true;
		while (moving)
		{
			movingObj.transform.position = editor.editorCamera.focusTransform.position;
			movingObj.transform.rotation = editor.editorCamera.focusTransform.rotation;
			movingObj.MoveInEditor();
			if (Input.GetKeyDown(KeyCode.Space))
			{
				yield return null;
				moving = false;
				InputStopMove();
				break;
			}
			yield return null;
		}
	}

	private void InputStopMove()
	{
		UnitSpawner.PlacementValidityInfo placementValidityInfo = movingObj.TryPlaceInEditor(editor.editorCamera.cursorLocation);
		if (!placementValidityInfo.isValid)
		{
			editor.popupMessages.DisplayMessage(placementValidityInfo.reason, 3f, Color.red);
			MoveButton();
			return;
		}
		Collider[] componentsInChildren = movingObj.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if (collider.gameObject.layer == 1)
			{
				collider.gameObject.layer = 0;
			}
		}
		movingObj = null;
		editor.popupMessages.RemovePersistentMessage("staticMove");
	}

	private void CancelMove()
	{
		if (moving)
		{
			if (moveRoutine != null)
			{
				StopCoroutine(moveRoutine);
			}
			moving = false;
			if ((bool)movingObj)
			{
				Collider[] componentsInChildren = movingObj.GetComponentsInChildren<Collider>();
				foreach (Collider collider in componentsInChildren)
				{
					if (collider.gameObject.layer == 1)
					{
						collider.gameObject.layer = 0;
					}
				}
				movingObj.transform.position = prevPoint.point;
				movingObj.transform.rotation = prevRot;
				movingObj.MoveInEditor();
			}
		}
		moving = false;
		movingObj = null;
		editor.popupMessages.RemovePersistentMessage("staticMove");
	}
}
