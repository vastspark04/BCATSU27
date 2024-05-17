using VTNetworking;

namespace VTOLVR.Multiplayer{

public class CBUSync : VTNetSyncRPCOnly
{
	public SensorFuzedCB cbu;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			cbu.OnBeginDeploy += Cbu_OnBeginDeploy;
		}
		else
		{
			cbu.MP_SetRemote();
		}
	}

	private void Cbu_OnBeginDeploy()
	{
		SendRPC("RPC_Deploy");
	}

	[VTRPC]
	private void RPC_Deploy()
	{
		cbu.RemoteBeginDeploy();
	}
}

}