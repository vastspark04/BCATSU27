public class HPEquipOpticalTargeter : HPEquippable, IMassObject
{
	public float mass;

	public OpticalTargeter targeter;

	protected override void OnEquip()
	{
		base.OnEquip();
		targeter.actor = base.weaponManager.actor;
		targeter.wm = base.weaponManager;
		base.weaponManager.SetOpticalTargeter(targeter);
	}

	public float GetMass()
	{
		return mass;
	}

	public override int GetCount()
	{
		return 1;
	}

	public override float GetEstimatedMass()
	{
		return mass;
	}

	public override void OnDisabledByPartDestroy()
	{
		base.OnDisabledByPartDestroy();
		base.weaponManager.SetOpticalTargeter(null);
	}

	public override void OnRepairedDestroyedPart()
	{
		base.OnRepairedDestroyedPart();
		base.weaponManager.SetOpticalTargeter(targeter);
	}
}
