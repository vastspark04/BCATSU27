public class TutObjSwitchToWeapon : CustomTutorialObjective
{
	public string shortName;

	private WeaponManager wm;

	public override void OnStartObjective()
	{
		base.OnStartObjective();
		wm = GetComponentInParent<WeaponManager>();
	}

	public override bool GetIsCompleted()
	{
		if (wm.currentEquip != null)
		{
			return wm.currentEquip.shortName == shortName;
		}
		return false;
	}
}
