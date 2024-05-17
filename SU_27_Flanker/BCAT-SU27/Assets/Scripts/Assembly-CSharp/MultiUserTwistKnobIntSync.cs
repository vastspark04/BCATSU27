using VTNetworking;

public class MultiUserTwistKnobIntSync : VTNetSyncRPCOnly
{
	public VRTwistKnobInt knob;

	public VRInteractableSync intSync;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		knob.OnSetState.AddListener(OnSetState);
		intSync.OnSetExclusiveUser += IntSync_OnSetExclusiveUser;
	}

	private void IntSync_OnSetExclusiveUser(ulong id)
	{
		if (id == BDSteamClient.mySteamID)
		{
			OnSetState(knob.currentState);
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
		knob.RemoteSetState(i);
	}
}
