using UnityEngine;
using UnityEngine.UI;

public class VTConditionalActionProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text buttonLabel;

	private ConditionalActionReference actionRef;

	public override void SetInitialValue(object value)
	{
		base.SetInitialValue(value);
		actionRef = (ConditionalActionReference)value;
		UpdateLabel();
	}

	public override object GetValue()
	{
		return actionRef;
	}

	public void EditActionButton()
	{
		if (actionRef.conditionalAction == null)
		{
			actionRef = new ConditionalActionReference(editor.currentScenario.conditionalActions.CreateNewAction());
			UpdateValue();
		}
		editor.conditionalActionEditor.Open(actionRef.conditionalAction, UpdateValue);
	}

	private void UpdateLabel()
	{
		VTConditionalEvents.ConditionalAction conditionalAction = actionRef.conditionalAction;
		if (conditionalAction != null && !string.IsNullOrEmpty(conditionalAction.name))
		{
			buttonLabel.text = conditionalAction.name.Substring(0, Mathf.Min(conditionalAction.name.Length, 19)).Replace("\n", " ") + "...";
		}
		else
		{
			buttonLabel.text = "Edit action...";
		}
	}

	private void UpdateValue()
	{
		UpdateLabel();
		ValueChanged();
	}
}
