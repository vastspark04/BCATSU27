using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEdSequencedEventEditor : MonoBehaviour
{
	public VTScenarioEditor editor;

	public VTEdSequencedEventsWindow sequencesWindow;

	public Text nameText;

	public VTBoolProperty startImmediatelyProp;

	public ScrollRect eventsScrollRect;

	public GameObject nodeTemplate;

	private List<VTEdSequencedEventUI> nodeUIs = new List<VTEdSequencedEventUI>();

	public VTSequencedEvent currentSequence { get; private set; }

	private void Awake()
	{
		nodeTemplate.SetActive(value: false);
	}

	private void Start()
	{
		startImmediatelyProp.OnValueChanged += StartImmediatelyProp_OnValueChanged;
	}

	private void StartImmediatelyProp_OnValueChanged(bool b_startImmediate)
	{
		if (currentSequence != null)
		{
			currentSequence.startImmediately = b_startImmediate;
		}
	}

	public void OpenForSequence(VTSequencedEvent seq)
	{
		base.gameObject.SetActive(value: true);
		currentSequence = seq;
		nameText.text = seq.sequenceName;
		startImmediatelyProp.SetInitialValue(seq.startImmediately);
		SetupNodesList();
	}

	private void SetupNodesList()
	{
		foreach (VTEdSequencedEventUI nodeUI in nodeUIs)
		{
			Object.Destroy(nodeUI.gameObject);
		}
		nodeUIs.Clear();
		for (int i = 0; i < currentSequence.eventNodes.Count; i++)
		{
			GameObject obj = Object.Instantiate(nodeTemplate, eventsScrollRect.content);
			obj.SetActive(value: true);
			VTEdSequencedEventUI component = obj.GetComponent<VTEdSequencedEventUI>();
			component.Setup(currentSequence.eventNodes[i], i);
			component.eventField.onChangedEvent += EventField_onChangedEvent;
			nodeUIs.Add(component);
		}
		eventsScrollRect.verticalNormalizedPosition = 1f;
		RespaceNodeUIs();
	}

	private void EventField_onChangedEvent()
	{
		RespaceNodeUIs();
	}

	private void RespaceNodeUIs()
	{
		float num = 0f;
		for (int i = 0; i < nodeUIs.Count; i++)
		{
			VTEdSequencedEventUI vTEdSequencedEventUI = nodeUIs[i];
			vTEdSequencedEventUI.transform.localPosition = new Vector3(0f, 0f - num, 0f);
			num += vTEdSequencedEventUI.eventField.GetFieldHeight();
		}
		eventsScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		eventsScrollRect.ClampVertical();
	}

	public void NewNodeButton()
	{
		VTSequencedEvent.EventNode eventNode = new VTSequencedEvent.EventNode();
		eventNode.nodeName = "New Node";
		eventNode.eventInfo = new VTEventInfo();
		currentSequence.eventNodes.Add(eventNode);
		SetupNodesList();
		eventsScrollRect.verticalNormalizedPosition = 0f;
		eventsScrollRect.ClampVertical();
	}

	public void DeleteNode(VTEdSequencedEventUI nUI)
	{
		editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this event node?", delegate
		{
			FinalDeleteNode(nUI);
		}, null);
	}

	private void FinalDeleteNode(VTEdSequencedEventUI nUI)
	{
		if (nUI.node != null && nUI.node.eventInfo != null && nUI.node.eventInfo.actions != null)
		{
			foreach (VTEventTarget action in nUI.node.eventInfo.actions)
			{
				action?.DeleteEventTarget();
			}
		}
		currentSequence.eventNodes.Remove(nUI.node);
		nodeUIs.Remove(nUI);
		Object.Destroy(nUI.gameObject);
		RefreshSequenceIndices();
		RespaceNodeUIs();
	}

	private void RefreshSequenceIndices()
	{
		for (int i = 0; i < nodeUIs.Count; i++)
		{
			nodeUIs[i].sequenceIndex = (nodeUIs[i].dragger.sequenceIndex = i);
		}
	}

	public void RenameButton()
	{
		editor.textInputWindow.Display("Change Name", "Set a name for the event sequence.", currentSequence.sequenceName, 40, OnEnteredNewName);
	}

	private void OnEnteredNewName(string newName)
	{
		currentSequence.sequenceName = newName;
		nameText.text = newName;
		sequencesWindow.ChangedName(currentSequence.id);
	}

	public void CloseButton()
	{
		base.gameObject.SetActive(value: false);
	}

	public void BeginReorder(int idx)
	{
		StartCoroutine(ReorderRoutine(idx));
	}

	private IEnumerator ReorderRoutine(int startIdx)
	{
		VTEdSequencedEventUI movingUI = nodeUIs[startIdx];
		movingUI.transform.SetAsLastSibling();
		Vector3 uiOffset = movingUI.transform.localPosition - eventsScrollRect.content.InverseTransformPoint(Input.mousePosition);
		while (Input.GetMouseButton(0))
		{
			float y = (eventsScrollRect.content.InverseTransformPoint(Input.mousePosition) + uiOffset).y;
			movingUI.transform.localPosition = new Vector3(0f, y, 0f);
			float y2 = movingUI.transform.localPosition.y;
			float num = movingUI.transform.localPosition.y - ((RectTransform)movingUI.transform).rect.height;
			if (startIdx < nodeUIs.Count - 1)
			{
				RectTransform rectTransform = (RectTransform)nodeUIs[startIdx + 1].transform;
				float num2 = rectTransform.localPosition.y - 3f * rectTransform.rect.height / 4f;
				if (num < num2)
				{
					VTEdSequencedEventUI vTEdSequencedEventUI = nodeUIs[startIdx + 1];
					VTSequencedEvent.EventNode item = currentSequence.eventNodes[startIdx];
					currentSequence.eventNodes.Remove(item);
					currentSequence.eventNodes.Insert(startIdx + 1, item);
					movingUI.dragger.sequenceIndex = (movingUI.sequenceIndex = startIdx + 1);
					nodeUIs.Remove(movingUI);
					nodeUIs.Insert(startIdx + 1, movingUI);
					vTEdSequencedEventUI.dragger.sequenceIndex = (vTEdSequencedEventUI.dragger.sequenceIndex = startIdx);
					startIdx = movingUI.sequenceIndex;
					RespaceNodeUIs();
				}
			}
			if (startIdx > 0)
			{
				RectTransform rectTransform2 = (RectTransform)nodeUIs[startIdx - 1].transform;
				float num3 = rectTransform2.localPosition.y - rectTransform2.rect.height / 4f;
				if (y2 > num3)
				{
					VTEdSequencedEventUI vTEdSequencedEventUI2 = nodeUIs[startIdx - 1];
					VTSequencedEvent.EventNode item2 = currentSequence.eventNodes[startIdx];
					currentSequence.eventNodes.Remove(item2);
					currentSequence.eventNodes.Insert(startIdx - 1, item2);
					movingUI.dragger.sequenceIndex = (movingUI.sequenceIndex = startIdx - 1);
					nodeUIs.Remove(movingUI);
					nodeUIs.Insert(startIdx - 1, movingUI);
					vTEdSequencedEventUI2.dragger.sequenceIndex = (vTEdSequencedEventUI2.dragger.sequenceIndex = startIdx);
					startIdx = movingUI.sequenceIndex;
					RespaceNodeUIs();
				}
			}
			yield return null;
		}
		RespaceNodeUIs();
	}
}
