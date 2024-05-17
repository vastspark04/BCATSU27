using UnityEngine;
using UnityEngine.UI;

public class VTUnitGroupProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text valueText;

	[HideInInspector]
	public UnitSpawner unitOptionsUnit;

	public Teams team = (Teams)(-1);

	public int unitGroupType = -1;

	private VTUnitGroup.UnitGroup currentGroup;

	public override void SetInitialValue(object value)
	{
		currentGroup = (VTUnitGroup.UnitGroup)value;
		UpdateValueText();
	}

	private void UpdateValueText()
	{
		if (currentGroup != null)
		{
			if ((bool)unitOptionsUnit)
			{
				valueText.text = currentGroup.groupID.ToString();
			}
			else
			{
				valueText.text = currentGroup.team.ToString() + " : " + currentGroup.groupID;
			}
		}
		else
		{
			valueText.text = "None";
		}
	}

	public override object GetValue()
	{
		return currentGroup;
	}

	public void SelectGroupButton()
	{
		if (unitOptionsUnit != null)
		{
			editor.groupSelector.OpenForUnit(unitOptionsUnit, OnSelected);
		}
		else
		{
			editor.groupSelector.OpenForExistingGroups(OnSelectedExisting, unitGroupType, team);
		}
	}

	private void OnSelectedExisting(VTUnitGroup.UnitGroup selected)
	{
		currentGroup = selected;
		UpdateValueText();
		ValueChanged();
	}

	private void OnSelected(VTUnitGroup.UnitGroup selected)
	{
		currentGroup = selected;
		UpdateValueText();
		editor.ScenarioObjectsChanged(new VTScenarioEditor.ScenarioChangeEventInfo(VTScenarioEditor.ChangeEventTypes.UnitGroups, -1, null));
		ValueChanged();
	}
}
