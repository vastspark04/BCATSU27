public class VTTeamOptionParam : VTActionParamAttribute
{
	public VTTeamOptionParam(TeamOptions teamOptions)
	{
		type = typeof(TeamOptions);
		data = teamOptions;
	}
}
