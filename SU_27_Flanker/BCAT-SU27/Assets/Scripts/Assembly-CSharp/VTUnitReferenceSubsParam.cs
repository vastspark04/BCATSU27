public class VTUnitReferenceSubsParam : VTActionParamAttribute
{
	public VTUnitReferenceSubsParam(bool allowSubs)
	{
		type = typeof(AllowSubUnits);
		data = ((!allowSubs) ? AllowSubUnits.Disallow : AllowSubUnits.Allow);
	}
}
