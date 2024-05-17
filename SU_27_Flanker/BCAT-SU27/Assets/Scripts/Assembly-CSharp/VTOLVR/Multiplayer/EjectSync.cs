using VTNetworking;

namespace VTOLVR.Multiplayer{

public class EjectSync : VTNetSyncRPCOnly
{
	public EjectionSeat localEjector;

	public AIEjectPilot remoteEjector;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			localEjector.OnJettisonCanopy.AddListener(OnBeginEject);
		}
	}

	private void OnBeginEject()
	{
		SendRPC("RPC_Eject");
	}

	[VTRPC]
	private void RPC_Eject()
	{
		remoteEjector.BeginEjectSequence();
	}
}

}