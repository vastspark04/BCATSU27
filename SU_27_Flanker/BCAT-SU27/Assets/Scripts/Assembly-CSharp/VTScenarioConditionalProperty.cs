using UnityEngine;
using UnityEngine.UI;

public class VTScenarioConditionalProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text buttonText;

	public GameObject warningObject;

	private int conditionalID = -1;

	public bool isMission;

	public bool useLocalConditional;

	private ScenarioConditional localConditional;

	private Button button;

	private Color buttonColor;

	private Color emptyColor = new Color(0.35f, 0.35f, 0.35f, 1f);

	public GameObject deleteButton;

	private void Awake()
	{
		button = GetComponentInChildren<Button>();
		buttonColor = button.image.color;
	}

	public override void SetInitialValue(object value)
	{
		if (useLocalConditional)
		{
			localConditional = (ScenarioConditional)value;
		}
		else if (value != null)
		{
			ScenarioConditional scenarioConditional = (ScenarioConditional)value;
			conditionalID = scenarioConditional.id;
		}
		else
		{
			conditionalID = -1;
		}
		UpdateLabel();
	}

	public override object GetValue()
	{
		if (useLocalConditional)
		{
			return localConditional;
		}
		if (conditionalID >= 0)
		{
			return editor.currentScenario.conditionals.GetConditional(conditionalID);
		}
		return null;
	}

	private void UpdateLabel()
	{
		if (useLocalConditional)
		{
			if ((bool)deleteButton)
			{
				deleteButton.SetActive(value: false);
			}
			if (localConditional != null)
			{
				button.image.color = buttonColor;
				buttonText.text = "Edit Condition";
			}
			else
			{
				button.image.color = emptyColor;
				buttonText.text = "None";
			}
		}
		else if (conditionalID >= 0)
		{
			button.image.color = buttonColor;
			buttonText.text = "Edit Condition";
			if ((bool)deleteButton)
			{
				deleteButton.SetActive(value: true);
			}
		}
		else
		{
			button.image.color = emptyColor;
			buttonText.text = "None";
			if ((bool)deleteButton)
			{
				deleteButton.SetActive(value: false);
			}
		}
	}

	public void EditButton()
	{
		ScenarioConditional scenarioConditional;
		if (useLocalConditional)
		{
			scenarioConditional = localConditional;
		}
		else if (conditionalID >= 0)
		{
			scenarioConditional = editor.currentScenario.conditionals.GetConditional(conditionalID);
		}
		else
		{
			scenarioConditional = editor.currentScenario.conditionals.CreateNewConditional();
			conditionalID = scenarioConditional.id;
		}
		editor.conditionalEditor.Open(scenarioConditional, isMission);
		editor.conditionalEditor.OnFinishedEdit += ConditionalEditor_OnFinishedEdit;
	}

	private void ConditionalEditor_OnFinishedEdit(ScenarioConditionalEditor.FinishStates finishState)
	{
		UpdateLabel();
		editor.conditionalEditor.OnFinishedEdit -= ConditionalEditor_OnFinishedEdit;
		if (finishState != ScenarioConditionalEditor.FinishStates.Cancelled)
		{
			warningObject.SetActive(finishState == ScenarioConditionalEditor.FinishStates.Incomplete);
		}
		ValueChanged();
	}

	public void DeleteConditionalButton()
	{
		editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this conditional?", FinalDelete, null);
	}

	private void FinalDelete()
	{
		if (conditionalID >= 0)
		{
			editor.currentScenario.conditionals.DeleteConditional(conditionalID);
		}
		SetInitialValue(null);
		warningObject.SetActive(value: false);
		ValueChanged();
	}
}
