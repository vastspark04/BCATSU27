public class UnitSpawnAttributeRange : UnitSpawnAttribute
{
	public enum RangeTypes
	{
		Float,
		Int
	}

	public RangeTypes rangeType;

	public float min;

	public float max;

	public UnitSpawnAttributeRange(string name, float min, float max, RangeTypes rangeType = RangeTypes.Float)
	{
		base.name = name;
		this.min = min;
		this.max = max;
		this.rangeType = rangeType;
	}

	public UnitSpawnAttributeRange()
	{
		min = 0f;
		max = 1f;
	}
}
