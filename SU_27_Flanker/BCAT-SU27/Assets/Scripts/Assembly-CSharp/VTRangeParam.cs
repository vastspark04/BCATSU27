public class VTRangeParam : VTActionParamAttribute
{
	public float min => ((MinMax)data).min;

	public float max => ((MinMax)data).max;

	public VTRangeParam(float min, float max)
	{
		type = typeof(MinMax);
		data = new MinMax(min, max);
	}
}
