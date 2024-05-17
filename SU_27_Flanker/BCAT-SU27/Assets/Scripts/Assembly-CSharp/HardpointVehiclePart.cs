using UnityEngine;

public class HardpointVehiclePart : MonoBehaviour
{
	public WeaponManager wm;

	public int hpIdx;

	private void Awake()
	{
		VehiclePart component = GetComponent<VehiclePart>();
		GetComponent<Health>().OnDeath.AddListener(HPDied);
		component.OnRepair.AddListener(HPRepaired);
	}

	public void HPDied()
	{
		HPEquippable equip = wm.GetEquip(hpIdx);
		if ((bool)equip)
		{
			if (equip.jettisonOnHPDied && equip.jettisonable)
			{
				wm.JettisonByPartDestruction(hpIdx);
			}
			else
			{
				wm.DisableWeaponByPartDestruction(hpIdx);
			}
		}
	}

	public void HPRepaired()
	{
		wm.RepairDestroyedEquip(hpIdx);
	}
}
