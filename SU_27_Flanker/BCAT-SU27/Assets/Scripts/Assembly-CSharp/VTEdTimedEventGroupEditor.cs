using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEdTimedEventGroupEditor : MonoBehaviour
{
	public VTScenarioEditor editor;

	public VTEdTimedEventsWindow timedEventsWindow;

	public Text groupNameText;

	public VTBoolProperty beginImmediatelyBool;

	public InputField initialDelayField;

	public GameObject timedEventTemplate;

	public RectTransform eventsContentTf;

	private ScrollRect eventsScrollRect;

	public float eventSpacing = 5f;

	private VTTimedEventGroup eventGroup;

	private List<VTEdTimedEventField> timedEventFields = new List<VTEdTimedEventField>();

	private int deletingIdx;

	private void Awake()
	{
		timedEventTemplate.SetActive(value: false);
		beginImmediatelyBool.OnValueChanged += BeginImmediatelyBool_OnValueChanged;
		eventsScrollRect = eventsContentTf.GetComponentInParent<ScrollRect>();
	}

	private void BeginImmediatelyBool_OnValueChanged(bool arg0)
	{
		if (eventGroup != null)
		{
			eventGroup.beginImmediately = (bool)beginImmediatelyBool.GetValue();
		}
	}

	public void OpenForGroup(VTTimedEventGroup eventGroup)
	{
		base.gameObject.SetActive(value: true);
		this.eventGroup = eventGroup;
		groupNameText.text = eventGroup.groupName;
		beginImmediatelyBool.SetInitialValue(eventGroup.beginImmediately);
		initialDelayField.text = eventGroup.initialDelay.ToString();
		foreach (VTEdTimedEventField timedEventField in timedEventFields)
		{
			Object.Destroy(timedEventField.gameObject);
		}
		timedEventFields.Clear();
		SortEvents();
		int num = 0;
		foreach (VTTimedEventInfo timedEvent in eventGroup.timedEvents)
		{
			GameObject obj = Object.Instantiate(timedEventTemplate, eventsContentTf);
			obj.SetActive(value: true);
			VTEdTimedEventField component = obj.GetComponent<VTEdTimedEventField>();
			component.groupEditor = this;
			component.SetupForEvent(timedEvent);
			component.idx = num;
			timedEventFields.Add(component);
			num++;
		}
		RefreshEventListPositions();
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}

	public void RenameButton()
	{
		editor.textInputWindow.Display("Rename Group", "Rename the timed event group.", groupNameText.text, 40, OnRenamed);
	}

	private void OnRenamed(string s)
	{
		groupNameText.text = s;
		eventGroup.groupName = s;
		timedEventsWindow.RenameGroup(s);
	}

	private void SortEvents()
	{
		if (eventGroup.timedEvents == null || eventGroup.timedEvents.Count <= 1)
		{
			return;
		}
		List<VTTimedEventInfo> list = new List<VTTimedEventInfo>();
		list.Add(eventGroup.timedEvents[0]);
		for (int i = 1; i < eventGroup.timedEvents.Count; i++)
		{
			bool flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				if (eventGroup.timedEvents[i].time < list[j].time)
				{
					list.Insert(j, eventGroup.timedEvents[i]);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(eventGroup.timedEvents[i]);
			}
		}
		eventGroup.timedEvents = list;
	}

	public void SortAndRefreshList()
	{
		OpenForGroup(eventGroup);
	}

	public void RefreshEventListPositions()
	{
		if (timedEventFields.Count == 0)
		{
			return;
		}
		float num = 0f;
		int num2 = 0;
		foreach (VTEdTimedEventField timedEventField in timedEventFields)
		{
			timedEventField.transform.localPosition = new Vector3(0f, 0f - num, 0f);
			num += timedEventField.GetFieldHeight() + eventSpacing;
			timedEventField.idx = num2;
			num2++;
		}
		eventsContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		eventsScrollRect.ClampVertical();
	}

	public void OnEnteredInitialDelay(string t)
	{
		eventGroup.initialDelay = float.Parse(t);
	}

	public void NewEventButton()
	{
		VTTimedEventInfo vTTimedEventInfo = new VTTimedEventInfo();
		vTTimedEventInfo.eventName = "New Timed Event";
		vTTimedEventInfo.time = 0f;
		eventGroup.timedEvents.Add(vTTimedEventInfo);
		OpenForGroup(eventGroup);
	}

	public void DeleteEvent(int idx)
	{
		deletingIdx = idx;
		editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this event?", FinallyDelete, null);
	}

	private void FinallyDelete()
	{
		eventGroup.timedEvents.RemoveAt(deletingIdx);
		VTEdTimedEventField vTEdTimedEventField = timedEventFields[deletingIdx];
		timedEventFields.Remove(vTEdTimedEventField);
		vTEdTimedEventField.DeleteAllActions();
		Object.Destroy(vTEdTimedEventField.gameObject);
		RefreshEventListPositions();
	}
}
