public class UnitReferenceListSubs : UnitReferenceList, IConfigValue
{
	public UnitReferenceListSubs()
	{
		allowSubunits = true;
	}

	public UnitReferenceListSubs(IUnitFilter[] unitFilters)
	{
		allowSubunits = true;
		base.unitFilters = unitFilters;
	}
}
