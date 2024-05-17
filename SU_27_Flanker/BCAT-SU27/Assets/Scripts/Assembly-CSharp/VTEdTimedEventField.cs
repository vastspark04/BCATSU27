using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEdTimedEventField : MonoBehaviour
{
	public int idx;

	public InputField nameInputField;

	public InputField timeInputField;

	public VTEdTimedEventGroupEditor groupEditor;

	public Transform actionStartPos;

	public float baseHeight;

	public GameObject actionFieldTemplate;

	public float actionSpacing = 2f;

	private RectTransform rectTf;

	private List<VTEdEventActionField> actionFields = new List<VTEdEventActionField>();

	private VTTimedEventInfo timedEventInfo;

	[HideInInspector]
	public float time;

	private float totalFieldHeight;

	private int removingActionIdx;

	public float GetFieldHeight()
	{
		return baseHeight + totalFieldHeight;
	}

	private void Awake()
	{
		actionFieldTemplate.SetActive(value: false);
		rectTf = (RectTransform)base.transform;
	}

	public void SetupForEvent(VTTimedEventInfo eventInfo)
	{
		timedEventInfo = eventInfo;
		foreach (VTEdEventActionField actionField in actionFields)
		{
			Object.Destroy(actionField.gameObject);
		}
		actionFields = new List<VTEdEventActionField>();
		nameInputField.text = eventInfo.eventName;
		timeInputField.text = eventInfo.time.ToString();
		int num = 0;
		totalFieldHeight = 0f;
		foreach (VTEventTarget action in eventInfo.actions)
		{
			GameObject obj = Object.Instantiate(actionFieldTemplate, actionStartPos.parent);
			obj.SetActive(value: true);
			obj.transform.localPosition = actionStartPos.localPosition + new Vector3(0f, 0f - totalFieldHeight, 0f);
			VTEdEventActionField component = obj.GetComponent<VTEdEventActionField>();
			component.SetupForEventTarget(action);
			component.idx = num;
			component.OnRemoveAction = RemoveAction;
			component.OnSelectAction = SelectedAction;
			actionFields.Add(component);
			totalFieldHeight += component.GetFieldHeight() + actionSpacing;
			num++;
		}
		UpdateHeight();
	}

	public void DeleteAllActions()
	{
		foreach (VTEventTarget action in timedEventInfo.actions)
		{
			action?.DeleteEventTarget();
		}
	}

	private void SelectedAction(int idx, VTEventTarget tgt)
	{
		timedEventInfo.actions[idx] = tgt;
		SetupForEvent(timedEventInfo);
		groupEditor.RefreshEventListPositions();
	}

	private void RemoveAction(int idx)
	{
		removingActionIdx = idx;
		groupEditor.editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this action?", FinallyRemoveAction, null);
	}

	private void FinallyRemoveAction()
	{
		actionFields[removingActionIdx].FinallyDeleteAction();
		timedEventInfo.actions.RemoveAt(removingActionIdx);
		SetupForEvent(timedEventInfo);
		groupEditor.RefreshEventListPositions();
	}

	public void NewActionButton()
	{
		timedEventInfo.actions.Add(null);
		SetupForEvent(timedEventInfo);
		groupEditor.SortAndRefreshList();
	}

	public void DeleteEventButton()
	{
		groupEditor.DeleteEvent(idx);
	}

	private void UpdateHeight()
	{
		rectTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, GetFieldHeight());
	}

	public void OnEnteredTime(string t)
	{
		timedEventInfo.time = float.Parse(t);
		time = timedEventInfo.time;
		groupEditor.SortAndRefreshList();
	}

	public void OnEnteredName(string n)
	{
		timedEventInfo.eventName = n;
	}
}
