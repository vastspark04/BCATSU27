public class TutObjTGPMode : CustomTutorialObjective
{
	public TargetingMFDPage tgp;

	public TargetingMFDPage.TGPModes mode;

	public override bool GetIsCompleted()
	{
		return tgp.tgpMode == mode;
	}
}
