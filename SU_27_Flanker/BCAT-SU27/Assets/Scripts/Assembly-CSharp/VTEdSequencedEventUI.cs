using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdSequencedEventUI : MonoBehaviour
{
	public class DraggerScript : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler
	{
		public int sequenceIndex;

		public VTEdSequencedEventEditor sequenceEditor;

		public void OnDrag(PointerEventData eventData)
		{
			eventData.Use();
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			sequenceEditor.BeginReorder(sequenceIndex);
		}
	}

	public VTEdSequencedEventEditor sequenceEditor;

	public VTEdEventField eventField;

	public InputField nameInputField;

	public VTFloatRangeProperty delayProp;

	public VTScenarioConditionalProperty conditionalProp;

	public VTScenarioConditionalProperty exitConditionalProp;

	public GameObject draggerObj;

	public int sequenceIndex;

	public VTSequencedEvent.EventNode node { get; private set; }

	public DraggerScript dragger { get; private set; }

	public void Setup(VTSequencedEvent.EventNode node, int sequenceIndex)
	{
		this.node = node;
		eventField.SetupForEvent(node.eventInfo);
		nameInputField.text = node.nodeName;
		nameInputField.onEndEdit.AddListener(EndEditNodeName);
		delayProp.min = 0f;
		delayProp.max = 999999f;
		delayProp.SetInitialValue(node.delay);
		delayProp.OnPropertyValueChanged += DelayProp_OnPropertyValueChanged;
		conditionalProp.SetInitialValue(node.conditional);
		conditionalProp.OnPropertyValueChanged += ConditionalProp_OnPropertyValueChanged;
		exitConditionalProp.SetInitialValue(node.exitConditional);
		exitConditionalProp.OnPropertyValueChanged += ExitConditionalProp_OnPropertyValueChanged;
		dragger = draggerObj.AddComponent<DraggerScript>();
		dragger.sequenceIndex = (this.sequenceIndex = sequenceIndex);
		dragger.sequenceEditor = sequenceEditor;
	}

	private void ExitConditionalProp_OnPropertyValueChanged(object newConditional)
	{
		node.exitConditional = (ScenarioConditional)newConditional;
	}

	private void DelayProp_OnPropertyValueChanged(object f_delay)
	{
		node.delay = (float)f_delay;
	}

	private void EndEditNodeName(string n)
	{
		node.nodeName = n;
	}

	private void ConditionalProp_OnPropertyValueChanged(object newConditional)
	{
		node.conditional = (ScenarioConditional)newConditional;
	}

	public void DeleteButton()
	{
		sequenceEditor.DeleteNode(this);
	}
}
