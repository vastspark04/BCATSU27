using System;

[AttributeUsage(AttributeTargets.Parameter)]
public class UnitReferenceParamAttribute : Attribute
{
	public TeamOptions teamOption;

	public bool allowSubUnits;

	public UnitReferenceParamAttribute()
	{
		teamOption = TeamOptions.BothTeams;
	}

	public UnitReferenceParamAttribute(TeamOptions teamOption)
	{
		this.teamOption = teamOption;
	}

	public UnitReferenceParamAttribute(TeamOptions teamOption, bool allowSubUnits)
	{
		this.teamOption = teamOption;
		this.allowSubUnits = allowSubUnits;
	}
}
