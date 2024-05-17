using System.Collections;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class MultiCrewFuelTankSync : VTNetSyncRPCOnly
{
	public MultiUserVehicleSync muvs;

	public FuelTank fuelTank;

	private void OnEnable()
	{
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		while (!wasRegistered)
		{
			yield return null;
		}
		if (!base.isMine)
		{
			fuelTank.remoteOnly = true;
			yield break;
		}
		WaitForSeconds wait = new WaitForSeconds(VTNetworkManager.CurrentSendInterval);
		string rpcName = "RPC_F";
		while (base.enabled)
		{
			muvs.SendRPCToCopilots(this, rpcName, fuelTank.fuelFraction);
			if (fuelTank.subFuelTanks != null)
			{
				for (int i = 0; i < fuelTank.subFuelTanks.Count; i++)
				{
					muvs.SendRPCToCopilots(this, rpcName, i, fuelTank.subFuelTanks[i].fuelFraction);
				}
			}
			yield return wait;
		}
	}

	[VTRPC]
	private void RPC_F(float norm)
	{
		fuelTank.SetNormFuel(norm);
	}

	[VTRPC]
	private void RPC_SF(int subIdx, float norm)
	{
		if (subIdx < fuelTank.subFuelTanks.Count)
		{
			fuelTank.subFuelTanks[subIdx].SetNormFuel(norm);
		}
	}
}

}