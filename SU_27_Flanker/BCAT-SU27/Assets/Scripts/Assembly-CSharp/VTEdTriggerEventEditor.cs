using UnityEngine;
using UnityEngine.UI;

public class VTEdTriggerEventEditor : MonoBehaviour
{
	public VTScenarioEditor editor;

	public VTEdTriggerEventsWindow triggerWindow;

	public Text eventNameText;

	public VTEnumProperty triggerTypeProp;

	public GameObject proximityObjects;

	public GameObject conditionalObjects;

	public VTScenarioConditionalProperty conditionalProp;

	public VTBoolProperty enabledProp;

	public VTWaypointProperty waypointProp;

	public VTEnumProperty triggerByProp;

	public VTUnitReferenceProperty unitRefProp;

	public VTEnumProperty eventModeProp;

	public VTFloatRangeProperty radiusProp;

	public VTBoolProperty sphericalRadiusProp;

	public VTEdEventField eventField;

	public ScrollRect scrollRect;

	private ScenarioTriggerEvents.TriggerEvent activeEvent;

	private ScenarioTriggerEvents.TriggerEvent.TriggerTypes changeFromConditionalTypeTo;

	public void Open(ScenarioTriggerEvents.TriggerEvent triggerEvent)
	{
		if (base.gameObject.activeSelf)
		{
			Close();
		}
		base.gameObject.SetActive(value: true);
		activeEvent = triggerEvent;
		eventNameText.text = activeEvent.eventName;
		triggerTypeProp.SetInitialValue(activeEvent.triggerType);
		enabledProp.SetInitialValue(activeEvent.enabled);
		triggerTypeProp.OnPropertyValueChanged += TriggerTypeProp_OnPropertyValueChanged;
		SetupTriggerTypeFields();
		triggerByProp.OnPropertyValueChanged += TriggerByProp_OnPropertyValueChanged;
		eventField.SetupForEvent(activeEvent.eventInfo);
		eventField.onChangedEvent += EventField_onChangedEvent;
		UpdateScrollRect();
		editor.OnBeforeSave += Editor_OnBeforeSave;
	}

	private void SetupTriggerTypeFields()
	{
		if (activeEvent.triggerType == ScenarioTriggerEvents.TriggerEvent.TriggerTypes.Conditional)
		{
			conditionalObjects.SetActive(value: true);
			proximityObjects.SetActive(value: false);
			conditionalProp.SetInitialValue(activeEvent.conditional);
			conditionalProp.OnPropertyValueChanged -= ConditionalProp_OnPropertyValueChanged;
			conditionalProp.OnPropertyValueChanged += ConditionalProp_OnPropertyValueChanged;
		}
		else if (activeEvent.triggerType == ScenarioTriggerEvents.TriggerEvent.TriggerTypes.Proximity)
		{
			conditionalObjects.SetActive(value: false);
			proximityObjects.SetActive(value: true);
			waypointProp.SetInitialValue(activeEvent.waypoint);
			triggerByProp.SetInitialValue(activeEvent.triggerMode);
			unitRefProp.teamOption = TeamOptions.BothTeams;
			if (activeEvent.triggerMode == TriggerEventModes.Unit)
			{
				unitRefProp.gameObject.SetActive(value: true);
				unitRefProp.SetInitialValue(activeEvent.unit);
			}
			else
			{
				unitRefProp.gameObject.SetActive(value: false);
			}
			eventModeProp.SetInitialValue(activeEvent.proxyMode);
			radiusProp.min = 5f;
			radiusProp.max = 64000f;
			radiusProp.SetInitialValue(activeEvent.radius);
			sphericalRadiusProp.SetInitialValue(activeEvent.sphericalRadius);
		}
	}

	private void ConditionalProp_OnPropertyValueChanged(object arg0)
	{
		activeEvent.conditional = (ScenarioConditional)arg0;
	}

	private void TriggerTypeProp_OnPropertyValueChanged(object arg0)
	{
		ScenarioTriggerEvents.TriggerEvent.TriggerTypes triggerTypes = (ScenarioTriggerEvents.TriggerEvent.TriggerTypes)arg0;
		if (triggerTypes != ScenarioTriggerEvents.TriggerEvent.TriggerTypes.Conditional && activeEvent.triggerType == ScenarioTriggerEvents.TriggerEvent.TriggerTypes.Conditional)
		{
			changeFromConditionalTypeTo = triggerTypes;
			editor.confirmDialogue.DisplayConfirmation("Change trigger type?", "Are you sure you want to change the trigger type? Existing conditional logic will be lost for this event.", ChangeFromConditionalType, CancelChangeFromConditionalType);
		}
		else
		{
			activeEvent.triggerType = triggerTypes;
			SetupTriggerTypeFields();
		}
	}

	private void ChangeFromConditionalType()
	{
		if (activeEvent.conditional != null)
		{
			editor.currentScenario.conditionals.DeleteConditional(activeEvent.conditional.id);
			activeEvent.conditional = null;
		}
		activeEvent.triggerType = changeFromConditionalTypeTo;
		SetupTriggerTypeFields();
	}

	private void CancelChangeFromConditionalType()
	{
		triggerTypeProp.SetInitialValue(activeEvent.triggerType);
	}

	private void Update()
	{
		if (activeEvent.triggerType == ScenarioTriggerEvents.TriggerEvent.TriggerTypes.Proximity)
		{
			object value = waypointProp.GetValue();
			if (value != null && ((Waypoint)value).GetTransform() != null)
			{
				Transform transform = ((Waypoint)value).GetTransform();
				editor.editorCamera.DrawCircle(transform.position, (float)radiusProp.GetValue(), Color.cyan);
			}
		}
	}

	private void Editor_OnBeforeSave()
	{
		if (activeEvent == null)
		{
			return;
		}
		activeEvent.enabled = (bool)enabledProp.GetValue();
		if (activeEvent.triggerType == ScenarioTriggerEvents.TriggerEvent.TriggerTypes.Proximity)
		{
			activeEvent.waypoint = (Waypoint)waypointProp.GetValue();
			activeEvent.triggerMode = (TriggerEventModes)triggerByProp.GetValue();
			if (activeEvent.triggerMode == TriggerEventModes.Unit)
			{
				activeEvent.unit = (UnitReference)unitRefProp.GetValue();
			}
			activeEvent.proxyMode = (TriggerProximityModes)eventModeProp.GetValue();
			activeEvent.radius = (float)radiusProp.GetValue();
			activeEvent.sphericalRadius = (bool)sphericalRadiusProp.GetValue();
		}
		else
		{
			activeEvent.conditional = (ScenarioConditional)conditionalProp.GetValue();
		}
	}

	private void EventField_onChangedEvent()
	{
		UpdateScrollRect();
	}

	private void TriggerByProp_OnPropertyValueChanged(object arg0)
	{
		if ((TriggerEventModes)arg0 == TriggerEventModes.Unit)
		{
			unitRefProp.gameObject.SetActive(value: true);
			unitRefProp.SetInitialValue(activeEvent.unit);
		}
		else
		{
			unitRefProp.gameObject.SetActive(value: false);
		}
	}

	public void Close()
	{
		triggerByProp.OnPropertyValueChanged -= TriggerByProp_OnPropertyValueChanged;
		triggerTypeProp.OnPropertyValueChanged -= TriggerTypeProp_OnPropertyValueChanged;
		eventField.onChangedEvent -= EventField_onChangedEvent;
		editor.OnBeforeSave -= Editor_OnBeforeSave;
		Editor_OnBeforeSave();
		base.gameObject.SetActive(value: false);
	}

	public void Rename()
	{
		editor.textInputWindow.Display("Rename", "Set a new name for the trigger event.", activeEvent.eventName, 40, OnRenamed);
	}

	private void OnRenamed(string n)
	{
		activeEvent.eventName = ConfigNodeUtils.SanitizeInputString(n);
		eventNameText.text = activeEvent.eventName;
		triggerWindow.SetupList();
	}

	private void UpdateScrollRect()
	{
		float fieldHeight = eventField.GetFieldHeight();
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fieldHeight);
		scrollRect.ClampVertical();
	}
}
