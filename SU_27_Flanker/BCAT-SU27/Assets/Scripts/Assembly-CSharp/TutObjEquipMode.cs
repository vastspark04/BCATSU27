public class TutObjEquipMode : CustomTutorialObjective
{
	public WeaponManagerUI wmUI;

	public int mode;

	public override bool GetIsCompleted()
	{
		return mode == wmUI.currentMode;
	}
}
