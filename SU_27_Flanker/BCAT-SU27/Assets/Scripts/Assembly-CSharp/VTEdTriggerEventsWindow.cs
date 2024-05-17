using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdTriggerEventsWindow : MonoBehaviour
{
	public class TriggerEventItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public VTEdTriggerEventsWindow window;

		public int idx;

		private float clickTime;

		public void OnPointerClick(PointerEventData e)
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

	public VTEdTriggerEventEditor eventEditor;

	public Button[] itemDependentButtons;

	public GameObject itemTemplate;

	public Transform selectionTf;

	private float itemHeight;

	private List<GameObject> itemObjs = new List<GameObject>();

	public ScrollRect scrollRect;

	private int selectedIdx;

	public bool isOpen;

	private void Awake()
	{
		eventEditor.gameObject.SetActive(value: false);
		itemHeight = ((RectTransform)itemTemplate.transform).rect.height;
		itemTemplate.SetActive(value: false);
	}

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		SetupList();
		SelectItem(-1);
		isOpen = true;
	}

	public void CloseWindow()
	{
		if (eventEditor.gameObject.activeSelf)
		{
			eventEditor.Close();
		}
		isOpen = false;
		base.gameObject.SetActive(value: false);
	}

	public void NewButton()
	{
		editor.currentScenario.triggerEvents.AddNewEvent();
		SetupList();
		SelectItem(editor.currentScenario.triggerEvents.events.Count - 1);
		EditButton();
	}

	public void DeleteButton()
	{
		editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this trigger event?", FinallyDelete, null);
	}

	private void FinallyDelete()
	{
		if (eventEditor.gameObject.activeSelf)
		{
			eventEditor.Close();
		}
		ScenarioTriggerEvents.TriggerEvent triggerEvent = editor.currentScenario.triggerEvents.events[selectedIdx];
		if (triggerEvent.triggerType == ScenarioTriggerEvents.TriggerEvent.TriggerTypes.Conditional && triggerEvent.conditional != null)
		{
			editor.currentScenario.conditionals.DeleteConditional(triggerEvent.conditional.id);
		}
		if (triggerEvent.eventInfo != null)
		{
			foreach (VTEventTarget action in triggerEvent.eventInfo.actions)
			{
				action?.DeleteEventTarget();
			}
		}
		editor.currentScenario.triggerEvents.DeleteEvent(triggerEvent.id);
		SetupList();
		SelectItem(-1);
	}

	public void EditButton()
	{
		eventEditor.Open(editor.currentScenario.triggerEvents.events[selectedIdx]);
	}

	public void SetupList()
	{
		foreach (GameObject itemObj in itemObjs)
		{
			Object.Destroy(itemObj);
		}
		itemObjs = new List<GameObject>();
		int count = editor.currentScenario.triggerEvents.events.Count;
		for (int i = 0; i < count; i++)
		{
			ScenarioTriggerEvents.TriggerEvent triggerEvent = editor.currentScenario.triggerEvents.events[i];
			GameObject gameObject = Object.Instantiate(itemTemplate, scrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * itemHeight, 0f);
			gameObject.GetComponentInChildren<Text>().text = triggerEvent.eventName;
			gameObject.GetComponentInChildren<UIMaskedTextScroller>().Refresh();
			TriggerEventItem triggerEventItem = gameObject.AddComponent<TriggerEventItem>();
			triggerEventItem.window = this;
			triggerEventItem.idx = i;
			itemObjs.Add(gameObject);
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)count * itemHeight);
		scrollRect.ClampVertical();
	}

	public void SelectItem(int idx)
	{
		if (idx != selectedIdx && eventEditor.gameObject.activeSelf)
		{
			eventEditor.Close();
		}
		selectionTf.localPosition = new Vector3(0f, (float)(-idx) * itemHeight, 0f);
		selectedIdx = idx;
		itemDependentButtons.SetInteractable(idx >= 0);
		if (idx < 0 && eventEditor.gameObject.activeSelf)
		{
			eventEditor.Close();
		}
	}
}
