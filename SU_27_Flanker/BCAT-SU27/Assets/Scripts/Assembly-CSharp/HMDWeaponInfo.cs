using UnityEngine;
using UnityEngine.UI;
using VTOLVR.DLC.Rotorcraft;

public class HMDWeaponInfo : MonoBehaviour, IPilotReceiverHandler
{
	public WeaponManager wm;

	public Text weaponNameText;

	public Text weaponCountText;

	private HPEquippable lastEq;

	private void Start()
	{
		if (!wm)
		{
			wm = GetComponentInParent<WeaponManager>();
			lastEq = wm.currentEquip;
		}
		UpdateWeaponName();
	}

	private void UpdateWeaponName()
	{
		if ((bool)wm && (bool)wm.currentEquip)
		{
			weaponNameText.text = wm.currentEquip.shortName.ToUpper();
		}
		else
		{
			weaponNameText.text = "NO ARM";
		}
	}

	private void Update()
	{
		if ((bool)wm)
		{
			if ((bool)wm.currentEquip)
			{
				weaponCountText.gameObject.SetActive(value: true);
				weaponCountText.text = wm.combinedCount.ToString();
			}
			else
			{
				weaponCountText.gameObject.SetActive(value: false);
			}
			if (lastEq != wm.currentEquip)
			{
				lastEq = wm.currentEquip;
				UpdateWeaponName();
			}
		}
		else
		{
			weaponCountText.gameObject.SetActive(value: false);
		}
	}

	public void OnPilotReceiver(AH94PilotReceiver receiver)
	{
		wm = receiver.flightInfo.GetComponent<WeaponManager>();
	}
}
