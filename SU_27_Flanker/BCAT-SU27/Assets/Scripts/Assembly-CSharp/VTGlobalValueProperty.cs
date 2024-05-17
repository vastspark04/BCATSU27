using UnityEngine;
using UnityEngine.UI;

public class VTGlobalValueProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text nameText;

	private GlobalValue currVal;

	private bool setListener;

	public override void SetInitialValue(object value)
	{
		currVal = (GlobalValue)value;
		UpdateText();
		if (!setListener && (bool)editor && (bool)editor.globalValueEditor)
		{
			editor.globalValueEditor.OnGlobalValueEdited += GlobalValueEditor_OnGlobalValueEdited;
			setListener = true;
		}
	}

	private void GlobalValueEditor_OnGlobalValueEdited(GlobalValue gv)
	{
		if (gv.id == currVal.id)
		{
			UpdateText();
		}
	}

	private void OnDestroy()
	{
		if (setListener)
		{
			if ((bool)editor && (bool)editor.globalValueEditor)
			{
				editor.globalValueEditor.OnGlobalValueEdited -= GlobalValueEditor_OnGlobalValueEdited;
			}
			setListener = false;
		}
	}

	public override object GetValue()
	{
		return currVal;
	}

	public void SelectButton()
	{
		editor.globalValueEditor.OpenSelector(Select, currVal);
	}

	private void UpdateText()
	{
		if (currVal.data != null)
		{
			nameText.text = currVal.name;
			nameText.fontStyle = FontStyle.Normal;
		}
		else
		{
			nameText.text = "None";
			nameText.fontStyle = FontStyle.Italic;
		}
	}

	private void Select(GlobalValue gv)
	{
		currVal = gv;
		UpdateText();
		ValueChanged();
	}
}
