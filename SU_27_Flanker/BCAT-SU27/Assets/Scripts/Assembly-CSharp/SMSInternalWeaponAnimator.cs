using System;
using UnityEngine;

public class SMSInternalWeaponAnimator : MonoBehaviour
{
	[Serializable]
	public class StoreCountProfile
	{
		public GameObject[] countObjects;
	}

	public StoreCountProfile[] countProfiles;

	public InternalWeaponBay weaponBay;

	public RotationToggle uiDoors;

	private StoreCountProfile currProfile;

	private HPEquippable eq;

	private int lastCount;

	private void Awake()
	{
		weaponBay.weaponManager.OnWeaponChanged.AddListener(OnWpnChanged);
	}

	private void OnEnable()
	{
		OnWpnChanged();
	}

	private void OnWpnChanged()
	{
		HPEquippable equip = weaponBay.weaponManager.GetEquip(weaponBay.hardpointIdx);
		if ((bool)equip)
		{
			SetupForWeapon(equip);
			return;
		}
		for (int i = 0; i < countProfiles.Length; i++)
		{
			countProfiles[i].countObjects.SetActive(active: false);
		}
	}

	public void SetupForWeapon(HPEquippable eq)
	{
		this.eq = eq;
		int maxCount = eq.GetMaxCount();
		lastCount = eq.GetCount();
		int num = maxCount - 1;
		for (int i = 0; i < countProfiles.Length; i++)
		{
			if (i == num)
			{
				countProfiles[i].countObjects.SetActive(active: true);
				currProfile = countProfiles[i];
			}
			else
			{
				countProfiles[i].countObjects.SetActive(active: false);
			}
		}
		UpdateCurrentProfile();
	}

	private void UpdateCurrentProfile()
	{
		eq.GetMaxCount();
		int count = eq.GetCount();
		for (int i = 0; i < currProfile.countObjects.Length; i++)
		{
			currProfile.countObjects[i].SetActive(i < count);
		}
	}

	private void Update()
	{
		uiDoors.SetNormalizedRotationImmediate(weaponBay.doorState);
		if (eq != null && currProfile != null && lastCount != eq.GetCount())
		{
			UpdateCurrentProfile();
		}
	}
}
