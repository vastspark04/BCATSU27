using UnityEngine.UI;

public class VTWaypointProperty : VTPropertyField
{
	public enum SpecialOptions
	{
		None,
		RTB,
		Refuel
	}

	private class RTBUnitFilter : IUnitFilter
	{
		public bool PassesFilter(UnitSpawner uSpawner)
		{
			return uSpawner.prefabUnitSpawn is IHasRTBWaypoint;
		}
	}

	private class RefuelUnitFilter : IUnitFilter
	{
		public bool PassesFilter(UnitSpawner uSpawner)
		{
			return uSpawner.prefabUnitSpawn is IHasRefuelWaypoint;
		}
	}

	private class TeamFilter : IUnitFilter
	{
		private Teams team;

		public TeamFilter(Teams team)
		{
			this.team = team;
		}

		public bool PassesFilter(UnitSpawner uSpawner)
		{
			return uSpawner.team == team;
		}
	}

	public VTScenarioEditor editor;

	public Text currValueText;

	private Waypoint currentWaypoint;

	public bool allowUnits;

	public SpecialOptions specialOption;

	public Teams specialOptionTeam;

	private static IUnitFilter rtbFilter = new RTBUnitFilter();

	private static IUnitFilter refuelFilter = new RefuelUnitFilter();

	public override void SetInitialValue(object value)
	{
		currentWaypoint = (Waypoint)value;
		UpdateValueText();
	}

	public override object GetValue()
	{
		return currentWaypoint;
	}

	public void SelectButton()
	{
		if (allowUnits || specialOption != 0)
		{
			IUnitFilter[] unitFilters = null;
			switch (specialOption)
			{
			case SpecialOptions.Refuel:
				unitFilters = new IUnitFilter[2]
				{
					refuelFilter,
					new TeamFilter(specialOptionTeam)
				};
				break;
			case SpecialOptions.RTB:
				unitFilters = new IUnitFilter[2]
				{
					rtbFilter,
					new TeamFilter(specialOptionTeam)
				};
				break;
			}
			editor.unitSelector.DisplayUnitOrWptSelector(labelText ? labelText.text : "Select", OnSelected, unitFilters);
			return;
		}
		Waypoint[] waypoints = editor.currentScenario.waypoints.GetWaypoints();
		Waypoint[] array = new Waypoint[waypoints.Length + 1];
		string[] array2 = new string[waypoints.Length + 1];
		int num = -1;
		for (int i = 0; i < waypoints.Length; i++)
		{
			array[i] = waypoints[i];
			array2[i] = waypoints[i].name;
			if (currentWaypoint != null && waypoints[i] == currentWaypoint)
			{
				num = i;
			}
		}
		array[array.Length - 1] = null;
		array2[array2.Length - 1] = "None";
		if (num < 0)
		{
			num = array2.Length - 1;
		}
		VTEdOptionSelector optionSelector = editor.optionSelector;
		string title = (labelText ? labelText.text : "Select");
		object[] returnValues = array;
		optionSelector.Display(title, array2, returnValues, num, OnSelected);
	}

	private void OnSelected(object selected)
	{
		currentWaypoint = (Waypoint)selected;
		UpdateValueText();
		ValueChanged();
	}

	private void UpdateValueText()
	{
		if (currentWaypoint == null)
		{
			currValueText.text = "None";
		}
		else
		{
			currValueText.text = currentWaypoint.name;
		}
	}
}
