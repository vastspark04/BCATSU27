using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdSequencedEventsWindow : MonoBehaviour
{
	public class SequenceItemUI : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public VTEdSequencedEventsWindow window;

		public int idx;

		public int evtID;

		private float lastClickTime;

		public void OnPointerClick(PointerEventData eventData)
		{
			window.SelectItem(idx);
			if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				window.EditButton();
			}
			lastClickTime = Time.unscaledTime;
		}
	}

	public VTScenarioEditor editor;

	public VTEdSequencedEventEditor sequenceEditor;

	public Button[] itemDependentButtons;

	public GameObject itemTemplate;

	public RectTransform selectionTf;

	public ScrollRect scrollRect;

	public bool isOpen;

	private int currIdx = -1;

	private float lineHeight;

	private List<SequenceItemUI> listUIs = new List<SequenceItemUI>();

	public void Open()
	{
		itemTemplate.SetActive(value: false);
		base.gameObject.SetActive(value: true);
		SetupList();
		SelectItem(-1);
		isOpen = true;
	}

	public void CloseWindow()
	{
		if (sequenceEditor.gameObject.activeSelf)
		{
			sequenceEditor.CloseButton();
		}
		isOpen = false;
		base.gameObject.SetActive(value: false);
	}

	private void SetupList()
	{
		foreach (SequenceItemUI listUI in listUIs)
		{
			Object.Destroy(listUI.gameObject);
		}
		listUIs.Clear();
		lineHeight = ((RectTransform)itemTemplate.transform).rect.height;
		List<VTSequencedEvent> allSequences = editor.currentScenario.sequencedEvents.GetAllSequences();
		for (int i = 0; i < allSequences.Count; i++)
		{
			GameObject obj = Object.Instantiate(itemTemplate, scrollRect.content);
			obj.SetActive(value: true);
			obj.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			SequenceItemUI sequenceItemUI = obj.AddComponent<SequenceItemUI>();
			sequenceItemUI.window = this;
			sequenceItemUI.idx = i;
			sequenceItemUI.evtID = allSequences[i].id;
			sequenceItemUI.GetComponentInChildren<Text>().text = allSequences[i].sequenceName;
			obj.GetComponentInChildren<UIMaskedTextScroller>().Refresh();
			listUIs.Add(sequenceItemUI);
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)allSequences.Count * lineHeight);
		scrollRect.ClampVertical();
	}

	public void SelectItem(int idx)
	{
		selectionTf.transform.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
		currIdx = idx;
		itemDependentButtons.SetInteractable(idx >= 0);
	}

	public void EditButton()
	{
		if (currIdx >= 0)
		{
			sequenceEditor.OpenForSequence(editor.currentScenario.sequencedEvents.GetSequence(listUIs[currIdx].evtID));
		}
	}

	public void NewSequenceButton()
	{
		editor.currentScenario.sequencedEvents.CreateNewSequence();
		SetupList();
		scrollRect.verticalNormalizedPosition = 0f;
		scrollRect.ClampVertical();
		SelectItem(listUIs.Count - 1);
		EditButton();
	}

	public void DeleteButton()
	{
		editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this event sequence?", FinalDelete, null);
	}

	private void FinalDelete()
	{
		if (sequenceEditor.gameObject.activeSelf)
		{
			sequenceEditor.CloseButton();
		}
		int evtID = listUIs[currIdx].evtID;
		int value = currIdx;
		editor.currentScenario.sequencedEvents.DeleteSequence(evtID);
		SetupList();
		SelectItem(Mathf.Clamp(value, 0, listUIs.Count - 1));
	}

	public void ChangedName(int evtID)
	{
		foreach (SequenceItemUI listUI in listUIs)
		{
			if (listUI.evtID == evtID)
			{
				listUI.GetComponentInChildren<Text>().text = editor.currentScenario.sequencedEvents.GetSequence(evtID).sequenceName;
			}
		}
	}
}
