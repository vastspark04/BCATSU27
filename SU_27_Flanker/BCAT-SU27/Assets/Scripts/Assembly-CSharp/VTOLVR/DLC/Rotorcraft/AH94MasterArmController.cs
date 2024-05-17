using UnityEngine;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class AH94MasterArmController : MonoBehaviour
{
	public WeaponManager wm;

	public WeaponManagerUI wmUI;

	public MultiUserVehicleSync muvs;

	public UIImageStatusLight safeLight;

	public UIImageStatusLight frontArmLight;

	public UIImageStatusLight rearArmLight;

	public Color wpnControllerColor = Color.red;

	public Color wpnNonControllerColor = Color.yellow;

	private void Start()
	{
		wm.OnWeaponChanged.AddListener(OnWpnChanged);
		muvs.OnSetWeaponControllerId += Muvs_OnSetWeaponControllerId;
		OnWpnChanged();
	}

	private void Muvs_OnSetWeaponControllerId(ulong obj)
	{
		OnWpnChanged();
	}

	private void OnWpnChanged()
	{
		if (!wm.isMasterArmed)
		{
			safeLight.SetStatus(1);
			frontArmLight.SetColor(Color.black);
			rearArmLight.SetColor(Color.black);
			return;
		}
		safeLight.SetStatus(0);
		if (VTOLMPUtils.IsMultiplayer())
		{
			bool flag = muvs.IsLocalWeaponController();
			if (muvs.LocalPlayerSeatIdx() == 1)
			{
				frontArmLight.SetColor(flag ? wpnControllerColor : wpnNonControllerColor);
				rearArmLight.SetColor((!flag) ? wpnControllerColor : wpnNonControllerColor);
			}
			else
			{
				frontArmLight.SetColor((!flag) ? wpnControllerColor : wpnNonControllerColor);
				rearArmLight.SetColor(flag ? wpnControllerColor : wpnNonControllerColor);
			}
		}
		else
		{
			frontArmLight.SetColor(wpnControllerColor);
			rearArmLight.SetColor(wpnControllerColor);
		}
	}

	public void MasterArm()
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			muvs.TakeWeaponControl();
		}
		wmUI.UISetMasterArm(1);
	}

	public void MasterSafe()
	{
		wmUI.UISetMasterArm(0);
	}
}

}