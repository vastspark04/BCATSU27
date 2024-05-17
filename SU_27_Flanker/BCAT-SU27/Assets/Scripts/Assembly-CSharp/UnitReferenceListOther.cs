public class UnitReferenceListOther : UnitReferenceList, IConfigValue
{
	public UnitReferenceListOther()
	{
		base.teamOption = TeamOptions.OtherTeam;
	}

	public UnitReferenceListOther(IUnitFilter[] unitFilters)
	{
		base.teamOption = TeamOptions.OtherTeam;
		base.unitFilters = unitFilters;
	}
}
