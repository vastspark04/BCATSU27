using UnityEngine;

public class SCCMPTeamStatsUI : SCCNodeUI
{
	public VTEnumProperty teamProp;

	public VTEnumProperty statProp;

	public VTEnumProperty comparisonProp;

	public VTFloatRangeProperty valueProp;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		SCCMPTeamStats sCCMPTeamStats = (SCCMPTeamStats)base.component;
		teamProp.SetInitialValue(sCCMPTeamStats.team);
		statProp.SetInitialValue(sCCMPTeamStats.statType);
		comparisonProp.SetInitialValue(sCCMPTeamStats.comparison);
		valueProp.min = -9999f;
		valueProp.max = 9999f;
		float num = sCCMPTeamStats.count;
		valueProp.SetInitialValue(num);
		teamProp.OnPropertyValueChanged += ValueChanged;
		statProp.OnPropertyValueChanged += ValueChanged;
		comparisonProp.OnPropertyValueChanged += ValueChanged;
		valueProp.OnPropertyValueChanged += ValueChanged;
	}

	private void ValueChanged(object o)
	{
		UpdateComponent();
	}

	public override void UpdateComponent()
	{
		base.UpdateComponent();
		SCCMPTeamStats obj = (SCCMPTeamStats)base.component;
		obj.team = (Teams)teamProp.GetValue();
		obj.statType = (SCCMPTeamStats.StatTypes)statProp.GetValue();
		obj.comparison = (IntComparisons)comparisonProp.GetValue();
		obj.count = Mathf.RoundToInt((float)valueProp.GetValue());
	}
}
