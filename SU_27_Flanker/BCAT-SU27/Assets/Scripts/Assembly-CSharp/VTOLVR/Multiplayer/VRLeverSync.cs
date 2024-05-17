using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VRLeverSync : VTNetSyncRPCOnly
{
	public VRLever lever;

	[Header("Set this to send state only to copilots")]
	public MultiUserVehicleSync muvs;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			lever.OnSetState.AddListener(OnSetState);
			if ((bool)muvs)
			{
				muvs.OnOccupantEntered += Muvs_OnOccupantEntered;
			}
		}
		else
		{
			lever.GetComponent<VRInteractable>().enabled = false;
		}
	}

	private void Muvs_OnOccupantEntered(int seatIdx, ulong userID)
	{
		OnSetState(lever.currentState);
	}

	private void OnSetState(int state)
	{
		if ((bool)muvs)
		{
			muvs.SendRPCToCopilots(this, "RPC_State", state);
		}
		else
		{
			SendRPC("RPC_State", state);
		}
	}

	[VTRPC]
	private void RPC_State(int st)
	{
		lever.RemoteSetState(st);
	}
}

}