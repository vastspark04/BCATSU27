using VTNetworking;

namespace VTOLVR.Multiplayer{

public class EquippableSync : VTNetSyncRPCOnly
{
	private HPEquippable eq;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		eq = base.gameObject.GetComponentImplementing<HPEquippable>();
		if (base.isMine && (bool)eq)
		{
			eq.OnJettisoned += Eq_OnJettisoned;
		}
	}

	private void Eq_OnJettisoned()
	{
		SendRPC("RPC_Jettison");
		VTNetworkManager.NetDestroyDelayed(base.gameObject, 15f);
	}

	[VTRPC]
	protected void RPC_Jettison()
	{
		eq.Jettison();
	}
}

}