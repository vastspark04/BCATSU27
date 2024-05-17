public class TutObjSOIMFDPage : CustomTutorialObjective
{
	public MFDPage page;

	public override bool GetIsCompleted()
	{
		return page.isSOI;
	}
}
