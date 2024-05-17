public class VTRangeTypeParam : VTActionParamAttribute
{
	public VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes rangeType)
	{
		type = typeof(UnitSpawnAttributeRange.RangeTypes);
		data = rangeType;
	}
}
