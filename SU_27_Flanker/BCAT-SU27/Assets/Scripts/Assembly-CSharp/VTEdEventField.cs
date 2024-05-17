using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VTEdEventField : MonoBehaviour
{
	public VTScenarioEditor editor;

	public float baseHeight;

	public GameObject actionFieldTemplate;

	public Transform actionStartPos;

	public float actionSpacing = 5f;

	public Text eventNameText;

	private VTEventInfo eventInfo;

	private List<VTEdEventActionField> actionFields = new List<VTEdEventActionField>();

	private float actionsHeight;

	private int removingActionIdx;

	private RectTransform rectTf => (RectTransform)base.transform;

	public event UnityAction onChangedEvent;

	private void Awake()
	{
		actionFieldTemplate.SetActive(value: false);
	}

	public float GetFieldHeight()
	{
		return baseHeight + actionsHeight;
	}

	public void SetupForEvent(VTEventInfo eInfo)
	{
		eventInfo = eInfo;
		foreach (VTEdEventActionField actionField in actionFields)
		{
			Object.Destroy(actionField.gameObject);
		}
		actionFields = new List<VTEdEventActionField>();
		if ((bool)eventNameText)
		{
			eventNameText.text = eInfo.eventName;
		}
		int num = 0;
		actionsHeight = 0f;
		foreach (VTEventTarget action in eInfo.actions)
		{
			GameObject obj = Object.Instantiate(actionFieldTemplate, actionStartPos.parent);
			obj.SetActive(value: true);
			obj.transform.localPosition = actionStartPos.localPosition + new Vector3(0f, 0f - actionsHeight, 0f);
			VTEdEventActionField component = obj.GetComponent<VTEdEventActionField>();
			component.SetupForEventTarget(action);
			component.idx = num;
			component.OnRemoveAction = RemoveAction;
			component.OnSelectAction = SelectedAction;
			actionFields.Add(component);
			actionsHeight += component.GetFieldHeight() + actionSpacing;
			num++;
		}
		UpdateHeight();
	}

	private void SelectedAction(int idx, VTEventTarget tgt)
	{
		eventInfo.actions[idx] = tgt;
		SetupForEvent(eventInfo);
		if (this.onChangedEvent != null)
		{
			this.onChangedEvent();
		}
	}

	private void RemoveAction(int idx)
	{
		removingActionIdx = idx;
		editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this action?", FinallyRemoveAction, null);
	}

	private void FinallyRemoveAction()
	{
		actionFields[removingActionIdx].FinallyDeleteAction();
		eventInfo.actions.RemoveAt(removingActionIdx);
		SetupForEvent(eventInfo);
		if (this.onChangedEvent != null)
		{
			this.onChangedEvent();
		}
	}

	public void NewActionButton()
	{
		eventInfo.actions.Add(null);
		SetupForEvent(eventInfo);
		if (this.onChangedEvent != null)
		{
			this.onChangedEvent();
		}
	}

	private void UpdateHeight()
	{
		rectTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, GetFieldHeight());
	}
}
