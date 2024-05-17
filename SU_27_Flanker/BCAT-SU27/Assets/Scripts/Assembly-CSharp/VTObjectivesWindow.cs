using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTObjectivesWindow : VTEdUITab
{
	public class ObjectiveListItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerDownHandler, IDragHandler
	{
		public int objectiveID;

		public int orderID;

		public VTObjectivesWindow window;

		private float lastClickTime;

		public void OnPointerClick(PointerEventData data)
		{
			window.SelectListItem(orderID);
			if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				window.EditButton();
			}
			lastClickTime = Time.unscaledTime;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			window.BeginMovingObjective(orderID);
		}

		public void OnDrag(PointerEventData eventData)
		{
			eventData.Use();
		}
	}

	public VTScenarioEditor editor;

	public VTObjectiveEditorUI objEditor;

	public RectTransform listContentTf;

	private ScrollRect listScrollRect;

	public Transform selectorTf;

	public Button[] itemDependentButtons;

	public GameObject objectiveTemplate;

	private List<ObjectiveListItem> listObjects = new List<ObjectiveListItem>();

	private float lineHeight;

	private List<VTObjective> objectives;

	private int selectedIdx = -1;

	[Header("Team Selection")]
	public GameObject mpOnlyObj;

	public Image teamAImg;

	public Image teamBImg;

	public Color selectedColor;

	public Color unselectedColor;

	private Teams currentTeam;

	private void Awake()
	{
		lineHeight = ((RectTransform)objectiveTemplate.transform).rect.height;
		objectiveTemplate.SetActive(value: false);
		listScrollRect = listContentTf.GetComponentInParent<ScrollRect>();
		SelectListItem(-1);
	}

	private void Start()
	{
		editor.OnScenarioLoaded += Editor_OnScenarioLoaded;
	}

	private void Editor_OnScenarioLoaded()
	{
		if (base.isOpen)
		{
			UpdateList();
			SelectListItem(-1);
		}
	}

	public override void OnOpenedTab()
	{
		base.OnOpenedTab();
		UpdateList();
		SelectListItem(-1);
		mpOnlyObj.SetActive(VTScenario.current.multiplayer);
	}

	public override void OnClosedTab()
	{
		base.OnClosedTab();
		objEditor.Close();
	}

	private void UpdateList()
	{
		objectives = editor.currentScenario.objectives.GetObjectives(currentTeam);
		objectives.Sort((VTObjective a, VTObjective b) => a.orderID.CompareTo(b.orderID));
		foreach (ObjectiveListItem listObject in listObjects)
		{
			Object.Destroy(listObject.gameObject);
		}
		listObjects.Clear();
		for (int i = 0; i < objectives.Count; i++)
		{
			GameObject obj = Object.Instantiate(objectiveTemplate, listContentTf);
			obj.SetActive(value: true);
			obj.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			obj.GetComponentInChildren<Text>().text = objectives[i].objectiveName;
			ObjectiveListItem objectiveListItem = obj.AddComponent<ObjectiveListItem>();
			objectiveListItem.objectiveID = objectives[i].objectiveID;
			objectiveListItem.orderID = (objectives[i].orderID = i);
			objectiveListItem.window = this;
			listObjects.Add(objectiveListItem);
		}
		listContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)objectives.Count * lineHeight);
		listScrollRect.ClampVertical();
	}

	public void SelectListItem(int orderID)
	{
		selectedIdx = orderID;
		selectorTf.localPosition = new Vector3(0f, (float)(-orderID) * lineHeight, 0f);
		itemDependentButtons.SetInteractable(orderID >= 0);
		if (orderID >= 0)
		{
			if (objEditor.isOpen && objEditor.activeObjective.objectiveID != objectives[orderID].objectiveID)
			{
				objEditor.Close();
			}
		}
		else
		{
			objEditor.Close();
		}
	}

	public void TeamAButton()
	{
		currentTeam = Teams.Allied;
		teamAImg.color = selectedColor;
		teamBImg.color = unselectedColor;
		UpdateList();
		SelectListItem(-1);
	}

	public void TeamBButton()
	{
		currentTeam = Teams.Enemy;
		teamBImg.color = selectedColor;
		teamAImg.color = unselectedColor;
		UpdateList();
		SelectListItem(-1);
	}

	public void NewButton()
	{
		VTObjective vTObjective = new VTObjective();
		vTObjective.SetObjectiveType(VTObjective.ObjectiveTypes.Destroy);
		vTObjective.objectiveID = editor.currentScenario.objectives.RequestNewID();
		vTObjective.orderID = editor.currentScenario.objectives.GetObjectiveCount(currentTeam);
		editor.currentScenario.objectives.AddObjective(vTObjective, currentTeam);
		UpdateList();
		SelectListItem(objectives.Count - 1);
	}

	public void DeleteButton()
	{
		editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this objective?", FinallyDelete, null);
	}

	private void FinallyDelete()
	{
		objEditor.Close();
		if (objectives[selectedIdx].objectiveType == VTObjective.ObjectiveTypes.Conditional)
		{
			VTOMConditional vTOMConditional = (VTOMConditional)objectives[selectedIdx].module;
			if (vTOMConditional.successConditional != null)
			{
				editor.currentScenario.conditionals.DeleteConditional(vTOMConditional.successConditional.id);
			}
			if (vTOMConditional.failConditional != null)
			{
				editor.currentScenario.conditionals.DeleteConditional(vTOMConditional.failConditional.id);
			}
		}
		editor.currentScenario.objectives.RemoveObjective(objectives[selectedIdx]);
		UpdateList();
		SelectListItem(-1);
	}

	public void EditButton()
	{
		objEditor.OpenForObjective(objectives[selectedIdx]);
	}

	public void UpdateObjectiveName(int objectiveID)
	{
		for (int i = 0; i < objectives.Count; i++)
		{
			if (objectives[i].objectiveID == objectiveID)
			{
				listObjects[i].GetComponentInChildren<Text>().text = objectives[i].objectiveName;
			}
		}
	}

	private IEnumerator MovingListItemRoutine(ObjectiveListItem draggedItem)
	{
		int origOrderID = draggedItem.orderID;
		int newOrderID = -1;
		bool moving = true;
		while (moving)
		{
			float y = listContentTf.InverseTransformPoint(Input.mousePosition).y;
			int mouseOrderPos = Mathf.Clamp(Mathf.FloorToInt((0f - y) / lineHeight), 0, listObjects.Count - 1);
			for (int i = 0; i < listObjects.Count; i++)
			{
				ObjectiveListItem objectiveListItem = listObjects[i];
				float y2 = objectiveListItem.transform.localPosition.y;
				float b2 = (float)(-i) * lineHeight;
				if (i == origOrderID)
				{
					b2 = (float)(-mouseOrderPos) * lineHeight;
				}
				else if (mouseOrderPos < origOrderID && mouseOrderPos <= i && i < origOrderID)
				{
					b2 = (float)(-i - 1) * lineHeight;
				}
				else if (mouseOrderPos > origOrderID && mouseOrderPos >= i && i > origOrderID)
				{
					b2 = (float)(-i + 1) * lineHeight;
				}
				y2 = Mathf.Lerp(y2, b2, 10f * Time.deltaTime);
				Vector3 localPosition = objectiveListItem.transform.localPosition;
				localPosition.y = y2;
				if (i != origOrderID)
				{
					localPosition.x = Mathf.Lerp(localPosition.x, 10f, 2f * Time.deltaTime);
				}
				objectiveListItem.transform.localPosition = localPosition;
			}
			selectorTf.localPosition = draggedItem.transform.localPosition;
			yield return null;
			if (!Input.GetMouseButton(0))
			{
				newOrderID = mouseOrderPos;
				moving = false;
			}
		}
		if (newOrderID >= 0)
		{
			listObjects.Remove(draggedItem);
			listObjects.Insert(newOrderID, draggedItem);
			for (int j = 0; j < listObjects.Count; j++)
			{
				listObjects[j].orderID = (VTScenario.current.objectives.GetObjective(listObjects[j].objectiveID).orderID = j);
			}
		}
		for (int k = 0; k < listObjects.Count; k++)
		{
			listObjects[k].transform.localPosition = new Vector3(0f, (float)(-k) * lineHeight, 0f);
		}
		objectives.Sort((VTObjective a, VTObjective b) => a.orderID.CompareTo(b.orderID));
		SelectListItem(draggedItem.orderID);
	}

	public void BeginMovingObjective(int orderID)
	{
		StartCoroutine(MovingListItemRoutine(listObjects[orderID]));
	}
}
