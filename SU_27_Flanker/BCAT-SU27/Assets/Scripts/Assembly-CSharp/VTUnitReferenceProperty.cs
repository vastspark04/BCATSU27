using UnityEngine;
using UnityEngine.UI;

public class VTUnitReferenceProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text currentValueText;

	[HideInInspector]
	public TeamOptions teamOption;

	[HideInInspector]
	public Teams unitTeam;

	[HideInInspector]
	public bool allowSubunits;

	public IUnitFilter[] filters;

	private UnitReference unitRef;

	public override void SetInitialValue(object value)
	{
		unitRef = (UnitReference)value;
		UpdateValueText();
	}

	public override object GetValue()
	{
		return unitRef;
	}

	public void SelectButton()
	{
		editor.unitSelector.DisplayUnitSelector("Select " + labelText.text, teamOption, unitTeam, OnSelected, allowSubunits, filters);
	}

	private void OnSelected(UnitReference unitRef)
	{
		this.unitRef = unitRef;
		UpdateValueText();
		ValueChanged();
	}

	private void UpdateValueText()
	{
		string text = "None";
		if (unitRef.unitID >= 0)
		{
			if (!editor.currentScenario.units.GetUnit(unitRef.unitID))
			{
				Debug.Log("No unit found of id: " + unitRef.unitID);
				text = "Missing!";
			}
			else
			{
				text = unitRef.GetDisplayName();
			}
		}
		currentValueText.text = text;
	}
}
