using UnityEngine;
using UnityEngine.UI;

public class VTUnitListProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text currentValueText;

	[HideInInspector]
	public Teams unitTeam;

	public int selectionLimit = -1;

	private UnitReferenceList currentList;

	public override void SetInitialValue(object value)
	{
		if (value == null)
		{
			currentList = new UnitReferenceList();
		}
		else
		{
			currentList = (UnitReferenceList)value;
		}
		currentList.units.RemoveAll((UnitReference x) => !editor.currentScenario.units.units.ContainsKey(x.unitID));
		UpdateValueText();
	}

	public override object GetValue()
	{
		return currentList;
	}

	public void SelectButton()
	{
		editor.unitSelector.DisplayMultiUnitSelector("Select " + labelText.text, currentList.teamOption, unitTeam, currentList, OnSelected, currentList.allowSubunits, currentList.unitFilters, selectionLimit);
	}

	private void OnSelected(UnitReferenceList newList)
	{
		currentList.units.Clear();
		foreach (UnitReference unit in newList.units)
		{
			currentList.units.Add(unit);
		}
		UpdateValueText();
		ValueChanged();
	}

	private void UpdateValueText()
	{
		string text = $"{currentList.units.Count} Selected";
		currentValueText.text = text;
	}
}
