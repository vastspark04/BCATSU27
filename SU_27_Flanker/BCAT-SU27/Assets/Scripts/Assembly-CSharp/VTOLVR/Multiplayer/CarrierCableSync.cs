using VTNetworking;

namespace VTOLVR.Multiplayer{

public class CarrierCableSync : VTNetSyncRPCOnly
{
	public CarrierCable cable;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		cable.OnSetHook += Cable_OnSetHook;
	}

	private void Cable_OnSetHook(Tailhook hook)
	{
		if (((bool)hook && VTOLMPUtils.IsMine(hook.actor.gameObject)) || (!hook && (bool)cable.hook && VTOLMPUtils.IsMine(cable.hook.actor.gameObject)))
		{
			SendRPC("RPC_SetHook", VTNetUtils.GetActorIdentifier(hook ? hook.actor : null));
		}
	}

	[VTRPC]
	private void RPC_SetHook(int actorId)
	{
		Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorId);
		if ((bool)actorFromIdentifier)
		{
			Tailhook componentInChildren = actorFromIdentifier.GetComponentInChildren<Tailhook>();
			if ((bool)componentInChildren)
			{
				componentInChildren.RemoteSetCable(cable);
				cable.RemoteSetHook(componentInChildren);
			}
		}
		else
		{
			cable.RemoteSetHook(null);
		}
	}
}

}