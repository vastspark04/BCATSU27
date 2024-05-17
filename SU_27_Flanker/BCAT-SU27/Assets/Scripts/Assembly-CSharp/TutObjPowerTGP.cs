public class TutObjPowerTGP : CustomTutorialObjective
{
	public TargetingMFDPage tgpPage;

	public override bool GetIsCompleted()
	{
		return tgpPage.powered;
	}
}
