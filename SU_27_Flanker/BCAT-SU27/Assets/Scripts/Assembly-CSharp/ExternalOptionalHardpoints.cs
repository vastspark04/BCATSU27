using System;
using System.Collections.Generic;
using UnityEngine;

public class ExternalOptionalHardpoints : MonoBehaviour
{
	[Serializable]
	public class Hardpoint
	{
		public int hpIdx;

		public GameObject pylonModel;

		public bool invert;
	}

	public List<Hardpoint> hardpoints;

	public WeaponManager wm;

	public PlayerVehicleSetup vehicleSetup;

	private Dictionary<int, Hardpoint> hpDict = new Dictionary<int, Hardpoint>();

	private void Awake()
	{
		foreach (Hardpoint hp in hardpoints)
		{
			hpDict.Add(hp.hpIdx, hp);
			bool flag = wm.GetEquip(hp.hpIdx) != null;
			if (hp.invert)
			{
				flag = !flag;
			}
			hp.pylonModel.SetActive(flag);
			VehiclePart componentInParent = wm.hardpointTransforms[hp.hpIdx].GetComponentInParent<VehiclePart>();
			if (!componentInParent)
			{
				continue;
			}
			componentInParent.OnRepair.AddListener(delegate
			{
				if (!wm.hardpointTransforms[hp.hpIdx].gameObject.GetComponentInChildrenImplementing<HPEquippable>(includeInactive: true))
				{
					Wm_OnWeaponUnequippedHPIdx(hp.hpIdx);
				}
			});
		}
		wm.OnWeaponEquippedHPIdx += Wm_OnWeaponEquippedHPIdx;
		wm.OnWeaponUnequippedHPIdx += Wm_OnWeaponUnequippedHPIdx;
		if ((bool)vehicleSetup)
		{
			vehicleSetup.OnBeginUsingConfigurator += VehicleSetup_OnBeginUsingConfigurator;
			vehicleSetup.OnEndUsingConfigurator += VehicleSetup_OnEndUsingConfigurator;
		}
	}

	private void VehicleSetup_OnEndUsingConfigurator(LoadoutConfigurator configurator)
	{
		configurator.OnAttachHPIdx -= Wm_OnWeaponEquippedHPIdx;
		configurator.OnDetachHPIdx -= Wm_OnWeaponUnequippedHPIdx;
	}

	private void VehicleSetup_OnBeginUsingConfigurator(LoadoutConfigurator configurator)
	{
		configurator.OnAttachHPIdx += Wm_OnWeaponEquippedHPIdx;
		configurator.OnDetachHPIdx += Wm_OnWeaponUnequippedHPIdx;
	}

	private void Wm_OnWeaponUnequippedHPIdx(int hpIdx)
	{
		if (hpDict.TryGetValue(hpIdx, out var value))
		{
			Debug.Log("Disabling external hardpoint " + hpIdx);
			value.pylonModel.SetActive(value.invert);
		}
	}

	private void Wm_OnWeaponEquippedHPIdx(int hpIdx)
	{
		if (hpDict.TryGetValue(hpIdx, out var value))
		{
			value.pylonModel.SetActive(!value.invert);
		}
	}

	public void Refresh()
	{
		foreach (Hardpoint hardpoint in hardpoints)
		{
			bool flag = wm.GetEquip(hardpoint.hpIdx);
			if (hardpoint.invert)
			{
				flag = !flag;
			}
			hardpoint.pylonModel.SetActive(flag);
		}
	}
}
