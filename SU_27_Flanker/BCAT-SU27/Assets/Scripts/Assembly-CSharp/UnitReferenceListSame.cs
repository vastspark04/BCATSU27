public class UnitReferenceListSame : UnitReferenceList, IConfigValue
{
	public UnitReferenceListSame()
	{
		base.teamOption = TeamOptions.SameTeam;
	}

	public UnitReferenceListSame(IUnitFilter[] unitFilters)
	{
		base.teamOption = TeamOptions.SameTeam;
		base.unitFilters = unitFilters;
	}
}
