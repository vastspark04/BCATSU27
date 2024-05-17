using VTOLVR.Multiplayer;

public class SCCMPTeamStats : ScenarioConditionalComponent
{
	public enum StatTypes
	{
		Kills,
		Deaths
	}

	[SCCField]
	public Teams team;

	[SCCField]
	public StatTypes statType;

	[SCCField]
	public IntComparisons comparison;

	[SCCField]
	public int count;

	private int currCount;

	private bool listened;

	public override bool GetCondition()
	{
		if (!listened && (bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnStatsUpdated += Instance_OnStatsUpdated;
			listened = true;
		}
		return comparison switch
		{
			IntComparisons.Equals => currCount == count, 
			IntComparisons.Greater_Than => currCount > count, 
			IntComparisons.Less_Than => currCount < count, 
			_ => false, 
		};
	}

	private void Instance_OnStatsUpdated(VTOLMPSceneManager.PlayerStats obj)
	{
		switch (statType)
		{
		case StatTypes.Kills:
			currCount = VTOLMPSceneManager.instance.GetTotalKills(team);
			break;
		case StatTypes.Deaths:
			currCount = VTOLMPSceneManager.instance.GetTotalDeaths(team);
			break;
		}
	}

	~SCCMPTeamStats()
	{
		if (listened && (bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnStatsUpdated -= Instance_OnStatsUpdated;
			listened = false;
		}
	}
}
