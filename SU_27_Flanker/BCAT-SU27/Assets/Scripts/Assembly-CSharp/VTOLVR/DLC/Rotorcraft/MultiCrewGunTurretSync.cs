using System.Collections;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class MultiCrewGunTurretSync : VTNetSyncRPCOnly
{
	public HPEquipGunTurret eq;

	private MultiUserVehicleSync muvs;

	private bool listenedEvt;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if ((bool)eq.weaponManager && !muvs)
		{
			muvs = eq.weaponManager.gameObject.GetComponent<MultiUserVehicleSync>();
		}
	}

	private void OnEnable()
	{
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		if (!muvs)
		{
			while (!muvs)
			{
				Debug.Log("MCGTS waiting for eq.weaponManager");
				while (!eq.weaponManager)
				{
					yield return null;
				}
				if (!muvs)
				{
					muvs = eq.weaponManager.gameObject.GetComponent<MultiUserVehicleSync>();
				}
				yield return null;
			}
			Debug.Log("MCGTS got MUVS");
		}
		if (!listenedEvt)
		{
			eq.weaponManager.OnWeaponFunctionCalled += WeaponManager_OnWeaponFunctionCalled;
			muvs.OnOccupantEntered += Muvs_OnOccupantEntered;
			listenedEvt = true;
		}
		while (!wasRegistered)
		{
			yield return null;
		}
		if (base.isMine)
		{
			muvs.SendRPCToCopilots(this, "RPC_WpnLocked", eq.isTurretLocked ? 1 : 0);
		}
	}

	private void OnDestroy()
	{
		if ((bool)muvs)
		{
			muvs.OnOccupantEntered -= Muvs_OnOccupantEntered;
		}
		if ((bool)eq && (bool)eq.weaponManager)
		{
			eq.weaponManager.OnWeaponFunctionCalled -= WeaponManager_OnWeaponFunctionCalled;
		}
	}

	private void Muvs_OnOccupantEntered(int seatIdx, ulong userID)
	{
		if (userID != BDSteamClient.mySteamID && base.isMine && (bool)muvs)
		{
			muvs.SendRPCToCopilots(this, "RPC_WpnLocked", eq.isTurretLocked ? 1 : 0);
		}
	}

	private void WeaponManager_OnWeaponFunctionCalled(int buttonIdx, int weaponIdx)
	{
		if (weaponIdx == eq.hardpointIdx)
		{
			muvs.SendRPCToCopilots(this, "RPC_WpnLocked", eq.isTurretLocked ? 1 : 0);
		}
	}

	[VTRPC]
	private void RPC_WpnLocked(int _l)
	{
		Debug.Log($"MCGTS.RPC_WpnLocked({_l})");
		if (_l > 0 != eq.isTurretLocked)
		{
			eq.weaponManager.WeaponFunctionButton(0, eq.hardpointIdx, sendEvent: false);
			eq.weaponManager.ui.hudInfo.RefreshWeaponInfo();
		}
	}
}

}