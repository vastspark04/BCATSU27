public class TutMissionObjective : CustomTutorialObjective
{
	public MissionObjective objective;

	public override void OnStartObjective()
	{
		objective.BeginMission();
	}

	public override bool GetIsCompleted()
	{
		return objective.completed;
	}
}
