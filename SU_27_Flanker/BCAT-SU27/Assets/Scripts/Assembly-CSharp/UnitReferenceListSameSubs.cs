public class UnitReferenceListSameSubs : UnitReferenceListSame, IConfigValue
{
	public UnitReferenceListSameSubs()
	{
		allowSubunits = true;
	}

	public UnitReferenceListSameSubs(IUnitFilter[] unitFilters)
	{
		allowSubunits = true;
		base.unitFilters = unitFilters;
	}
}
