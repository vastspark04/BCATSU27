using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdTimedEventsWindow : MonoBehaviour
{
	public class EventGroupItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public int idx;

		public VTEdTimedEventsWindow window;

		private float clickTime;

		public void OnPointerClick(PointerEventData eventData)
		{
			window.SelectItem(idx);
			if (Time.unscaledTime - clickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				window.EditButton();
			}
			clickTime = Time.unscaledTime;
		}
	}

	public VTScenarioEditor editor;

	public GameObject groupTemplate;

	public Transform selectionTransform;

	public RectTransform listContentTf;

	private int selectedIdx = -1;

	private float lineHeight;

	public bool isOpen = true;

	public Button[] itemDependentButtons;

	public VTEdTimedEventGroupEditor groupEditor;

	private List<GameObject> listObjects = new List<GameObject>();

	private List<VTTimedEventGroup> groups;

	private void Awake()
	{
		lineHeight = ((RectTransform)groupTemplate.transform).rect.height;
		groupTemplate.SetActive(value: false);
	}

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		groups = editor.currentScenario.timedEventGroups.GetAllGroups();
		foreach (GameObject listObject in listObjects)
		{
			Object.Destroy(listObject);
		}
		listObjects.Clear();
		for (int i = 0; i < groups.Count; i++)
		{
			GameObject gameObject = Object.Instantiate(groupTemplate, listContentTf);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			gameObject.GetComponentInChildren<Text>().text = groups[i].groupName;
			EventGroupItem eventGroupItem = gameObject.AddComponent<EventGroupItem>();
			eventGroupItem.idx = i;
			eventGroupItem.window = this;
			listObjects.Add(gameObject);
		}
		listContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)groups.Count * lineHeight);
		SelectItem(-1);
		isOpen = true;
	}

	public void Close()
	{
		isOpen = false;
		base.gameObject.SetActive(value: false);
	}

	public void NewGroupButton()
	{
		VTTimedEventGroup vTTimedEventGroup = new VTTimedEventGroup();
		vTTimedEventGroup.groupName = "New Event Group";
		editor.currentScenario.timedEventGroups.AddGroup(vTTimedEventGroup);
		Open();
		SelectItem(groups.Count - 1);
	}

	public void DeleteGroupButton()
	{
		editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this timed event group?", FinallyDelete, null);
	}

	private void FinallyDelete()
	{
		VTTimedEventGroup evtGroup = groups[selectedIdx];
		editor.currentScenario.timedEventGroups.RemoveGroup(evtGroup);
		Open();
	}

	public void EditButton()
	{
		groupEditor.OpenForGroup(groups[selectedIdx]);
	}

	public void SelectItem(int idx)
	{
		if (idx != selectedIdx || idx < 0)
		{
			groupEditor.Close();
		}
		selectedIdx = idx;
		itemDependentButtons.SetInteractable(idx >= 0);
		selectionTransform.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
	}

	public void RenameGroup(string newName)
	{
		listObjects[selectedIdx].GetComponentInChildren<Text>().text = newName;
		UIMaskedTextScroller componentInChildren = listObjects[selectedIdx].GetComponentInChildren<UIMaskedTextScroller>();
		if ((bool)componentInChildren)
		{
			componentInChildren.Refresh();
		}
	}
}
