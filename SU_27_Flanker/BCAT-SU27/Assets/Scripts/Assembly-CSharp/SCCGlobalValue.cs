public class SCCGlobalValue : ScenarioConditionalComponent
{
	[SCCField]
	public GlobalValue gv;

	[SCCField]
	public IntComparisons comparison;

	[SCCField]
	public int c_value;

	public override bool GetCondition()
	{
		if (gv.data != null)
		{
			return comparison switch
			{
				IntComparisons.Equals => gv.currentValue == c_value, 
				IntComparisons.Greater_Than => gv.currentValue > c_value, 
				IntComparisons.Less_Than => gv.currentValue < c_value, 
				_ => false, 
			};
		}
		return false;
	}
}
