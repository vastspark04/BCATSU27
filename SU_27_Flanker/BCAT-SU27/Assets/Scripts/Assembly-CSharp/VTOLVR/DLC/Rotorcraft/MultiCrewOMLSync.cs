using System.Collections;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class MultiCrewOMLSync : VTNetSyncRPCOnly
{
	public HPEquipOpticalML eq;

	private MultiUserVehicleSync muvs;

	private bool listenedEvt;

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
			muvs.SendRPCToCopilots(this, "RPC_SetAutoUncage", eq.autoUncage ? 1 : 0);
		}
	}

	private void WeaponManager_OnWeaponFunctionCalled(int buttonIdx, int weaponIdx)
	{
		if ((bool)muvs && weaponIdx == eq.hardpointIdx)
		{
			muvs.SendRPCToCopilots(this, "RPC_SetAutoUncage", eq.autoUncage ? 1 : 0);
		}
	}

	private void Muvs_OnOccupantEntered(int seatIdx, ulong userID)
	{
		if (userID != BDSteamClient.mySteamID && base.isMine && (bool)muvs)
		{
			SendDirectedRPC(userID, "RPC_SetAutoUncage", eq.autoUncage ? 1 : 0);
		}
	}

	[VTRPC]
	private void RPC_SetAutoUncage(int i_au)
	{
		if (i_au > 0 != eq.autoUncage)
		{
			eq.weaponManager.WeaponFunctionButton(0, eq.hardpointIdx, sendEvent: false);
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
}

}