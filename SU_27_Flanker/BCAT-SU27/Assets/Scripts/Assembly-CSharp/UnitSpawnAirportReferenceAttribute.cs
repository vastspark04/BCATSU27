public class UnitSpawnAirportReferenceAttribute : UnitSpawnAttribute
{
	public TeamOptions teamOption;

	public UnitSpawnAirportReferenceAttribute(string name, TeamOptions teamOption, params string[] uiOptionsParams)
	{
		base.name = name;
		this.teamOption = teamOption;
		SetupUIOptions(uiOptionsParams);
	}

	public Teams GetTeamFilter(IHasTeam target)
	{
		Teams team = target.GetTeam();
		if (teamOption == TeamOptions.OtherTeam)
		{
			if (team != 0)
			{
				return Teams.Allied;
			}
			return Teams.Enemy;
		}
		return team;
	}
}
