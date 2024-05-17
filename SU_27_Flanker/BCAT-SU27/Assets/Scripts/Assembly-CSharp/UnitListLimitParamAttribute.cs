public class UnitListLimitParamAttribute : VTActionParamAttribute
{
	public struct SelectionLimit
	{
		public int limit;

		public SelectionLimit(int limit)
		{
			this.limit = limit;
		}
	}

	public UnitListLimitParamAttribute(int limit)
	{
		type = typeof(SelectionLimit);
		data = new SelectionLimit(limit);
	}
}
