public struct ConditionalActionReference : IConfigValue
{
	private int idPlusOne;

	public VTConditionalEvents.ConditionalAction conditionalAction => VTScenario.current.conditionalActions.GetAction(id);

	public int id => idPlusOne - 1;

	public ConditionalActionReference(VTConditionalEvents.ConditionalAction a)
	{
		if (a != null)
		{
			idPlusOne = a.id + 1;
		}
		else
		{
			idPlusOne = 0;
		}
	}

	public void ConstructFromValue(string s)
	{
		idPlusOne = ConfigNodeUtils.ParseInt(s) + 1;
	}

	public string WriteValue()
	{
		return id.ToString();
	}
}
