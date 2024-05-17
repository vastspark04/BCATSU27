using VTNetworking;

public class MultiUserLeverSync : VTNetSyncRPCOnly
{
	public VRLever lever;

	public VRInteractableSync intSync;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		lever.OnSetState.AddListener(OnSetState);
		intSync.OnSetExclusiveUser += IntSync_OnSetExclusiveUser;
	}

	private void IntSync_OnSetExclusiveUser(ulong id)
	{
		if (id == BDSteamClient.mySteamID)
		{
			OnSetState(lever.currentState);
		}
	}

	private void OnSetState(int i)
	{
		if (intSync.GetCurrentExclusiveUser() == BDSteamClient.mySteamID)
		{
			SendRPC("RPC_State", i);
		}
	}

	[VTRPC]
	private void RPC_State(int i)
	{
		lever.RemoteSetState(i);
	}
}
