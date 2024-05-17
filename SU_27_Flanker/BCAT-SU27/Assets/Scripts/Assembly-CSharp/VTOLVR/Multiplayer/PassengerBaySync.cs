using Steamworks;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class PassengerBaySync : VTNetSyncRPCOnly
{
	public PassengerBay bay;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			bay.OnRampState += Bay_OnRampState;
			Refresh(0uL);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			bay.SetToRemote();
		}
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		Refresh(obj);
	}

	private void Refresh(ulong target = 0uL)
	{
		if (base.isMine)
		{
			PassengerBay.RampStates rampStates = PassengerBay.RampStates.Opening;
			if (bay.rampState == PassengerBay.RampStates.Closed || bay.rampState == PassengerBay.RampStates.Closing)
			{
				rampStates = PassengerBay.RampStates.Closing;
			}
			SendDirectedRPC(target, "RPC_RampState", (int)rampStates);
		}
	}

	[VTRPC]
	private void RPC_RampState(int state)
	{
		switch (state)
		{
		case 3:
			bay.SetRamp(0);
			break;
		case 2:
			bay.SetRamp(1);
			break;
		}
	}

	private void Bay_OnRampState(PassengerBay.RampStates obj)
	{
		SendRPC("RPC_RampState", (int)obj);
	}
}

}