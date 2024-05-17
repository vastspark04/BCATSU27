public class TutObjEngineSpool : CustomTutorialObjective
{
	public ModuleEngine[] engines;

	public override bool GetIsCompleted()
	{
		bool result = true;
		for (int i = 0; i < engines.Length; i++)
		{
			if (!engines[i].startedUp)
			{
				result = false;
				break;
			}
		}
		return result;
	}
}
