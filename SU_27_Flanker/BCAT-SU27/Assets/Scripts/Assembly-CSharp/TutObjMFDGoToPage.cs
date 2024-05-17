public class TutObjMFDGoToPage : CustomTutorialObjective
{
	public MFD mfd;

	public string page;

	public override bool GetIsCompleted()
	{
		return mfd.activePage.pageName == page;
	}
}
