public class UnitReferenceListOtherSubs : UnitReferenceListOther, IConfigValue
{
	public UnitReferenceListOtherSubs()
	{
		allowSubunits = true;
	}

	public UnitReferenceListOtherSubs(IUnitFilter[] unitFilters)
	{
		allowSubunits = true;
		base.unitFilters = unitFilters;
	}
}
