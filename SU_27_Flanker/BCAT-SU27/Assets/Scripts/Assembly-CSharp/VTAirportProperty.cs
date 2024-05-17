using System.Collections.Generic;
using UnityEngine.UI;

public class VTAirportProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text valueText;

	public bool allowNoneOption;

	private AirportReference currentValue;

	public bool useTeamFilter;

	public Teams teamFilter;

	public override void SetInitialValue(object value)
	{
		currentValue = (AirportReference)value;
		valueText.text = currentValue.GetLabel();
	}

	public override object GetValue()
	{
		return currentValue;
	}

	public void SelectButton()
	{
		List<string> list = (useTeamFilter ? editor.currentScenario.GetAllAirportIDs(teamFilter) : editor.currentScenario.GetAllAirportIDs());
		if (allowNoneOption)
		{
			list.Insert(0, string.Empty);
		}
		string[] array = new string[list.Count];
		int selected = -1;
		for (int i = 0; i < list.Count; i++)
		{
			array[i] = new AirportReference(list[i]).GetLabel();
			if (list[i] == currentValue.id)
			{
				selected = i;
			}
		}
		VTEdOptionSelector optionSelector = editor.optionSelector;
		object[] returnValues = list.ToArray();
		optionSelector.Display("Select Airfield", array, returnValues, selected, OnSelectedAirfield);
	}

	private void OnSelectedAirfield(object apId)
	{
		if (apId != null)
		{
			currentValue = new AirportReference((string)apId);
		}
		else
		{
			currentValue = default(AirportReference);
		}
		valueText.text = currentValue.GetLabel();
		ValueChanged();
	}
}
