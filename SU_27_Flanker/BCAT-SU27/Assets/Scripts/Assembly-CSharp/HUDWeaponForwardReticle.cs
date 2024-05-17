using UnityEngine;

public class HUDWeaponForwardReticle : MonoBehaviour
{
	public WeaponManager wm;

	private float depth = 1000f;

	private void Start()
	{
		CollimatedHUDUI componentInParent = GetComponentInParent<CollimatedHUDUI>();
		if ((bool)componentInParent)
		{
			depth = componentInParent.depth;
		}
	}

	private void LateUpdate()
	{
		if (!wm)
		{
			return;
		}
		HPEquippable currentEquip = wm.currentEquip;
		if ((bool)currentEquip && currentEquip is HPEquipMissileLauncher)
		{
			HPEquipMissileLauncher obj = (HPEquipMissileLauncher)currentEquip;
			Vector3 forward = obj.transform.forward;
			Missile nextMissile = obj.ml.GetNextMissile();
			if ((bool)nextMissile)
			{
				forward = nextMissile.transform.forward;
			}
			base.transform.position = VRHead.position + forward * depth;
			base.transform.rotation = Quaternion.LookRotation(forward, base.transform.parent.up);
		}
	}
}
