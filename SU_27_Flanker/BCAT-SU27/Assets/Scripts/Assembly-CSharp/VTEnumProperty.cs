using System;
using UnityEngine.UI;

public class VTEnumProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public VTEdOptionSelector optionSelector;

	public Text valueText;

	private object currentValue;

	public string[] overrideLabels;

	private string[] options;

	private object[] values;

	private bool hasSetupOptions;

	public override void SetInitialValue(object value)
	{
		currentValue = value;
		UpdateValueText();
		if (hasSetupOptions)
		{
			return;
		}
		options = Enum.GetNames(currentValue.GetType());
		for (int i = 0; i < options.Length; i++)
		{
			if (overrideLabels != null && i < overrideLabels.Length)
			{
				options[i] = overrideLabels[i];
			}
			else
			{
				options[i] = options[i].Replace("_", " ");
			}
		}
		Array array = Enum.GetValues(currentValue.GetType());
		values = new object[array.Length];
		for (int j = 0; j < array.Length; j++)
		{
			values[j] = array.GetValue(j);
		}
		hasSetupOptions = true;
	}

	public void SetInitialValueLimited(object value, object[] limitedOptions)
	{
		currentValue = value;
		UpdateValueText();
		if (hasSetupOptions)
		{
			return;
		}
		string[] names = Enum.GetNames(currentValue.GetType());
		Array array = Enum.GetValues(currentValue.GetType());
		object[] array2 = new object[array.Length];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = array.GetValue(i);
		}
		values = new object[limitedOptions.Length];
		options = new string[values.Length];
		for (int j = 0; j < limitedOptions.Length; j++)
		{
			values[j] = limitedOptions[j];
			int num = array2.IndexOf(values[j]);
			_ = names[num];
			if (overrideLabels != null && j < overrideLabels.Length)
			{
				options[j] = overrideLabels[num];
			}
			else
			{
				options[j] = limitedOptions[j].ToString().Replace("_", " ");
			}
		}
		hasSetupOptions = true;
	}

	public override object GetValue()
	{
		return currentValue;
	}

	public override void SetLabel(string label)
	{
		base.SetLabel(label);
	}

	public void SelectButton()
	{
		int selected = 0;
		if (currentValue != null)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].Equals(currentValue))
				{
					selected = i;
				}
			}
		}
		if ((bool)editor)
		{
			editor.optionSelector.Display(fieldName, options, values, selected, OnSelected);
		}
		else if ((bool)optionSelector)
		{
			optionSelector.Display(fieldName, options, values, selected, OnSelected);
		}
	}

	private void OnSelected(object returnValue)
	{
		currentValue = returnValue;
		UpdateValueText();
		ValueChanged();
	}

	private void UpdateValueText()
	{
		int num = (int)currentValue;
		if (overrideLabels != null && num < overrideLabels.Length)
		{
			valueText.text = overrideLabels[num];
		}
		else
		{
			valueText.text = currentValue.ToString().Replace("_", " ");
		}
	}
}
