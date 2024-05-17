using Steamworks;
using VTNetworking;

namespace VTOLVR.DLC.Rotorcraft{

public class TurbineStarterSync : VTNetSyncRPCOnly
{
	public TurbineStarterMotor m;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		m.OnMotorState += M_OnMotorState;
		if (base.isMine)
		{
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
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
		SendDirectedRPC(obj, "RPC_MotorState", m.motorEnabled ? 1 : 0);
	}

	private void M_OnMotorState(bool obj)
	{
		SendRPC("RPC_MotorState", obj ? 1 : 0);
	}

	[VTRPC]
	private void RPC_MotorState(int s)
	{
		m.RemoteSetMotor(s > 0);
	}
}

}