using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class RefuelPlaneSync : VTNetSyncRPCOnly
{
	public RefuelPlane rp;

	private bool aiRdy;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			rp.OnSetRefuelPort += Rp_OnSetRefuelPort;
			rp.OnAIPilotReady += Rp_OnAIPilotReady;
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			rp.SetToRemote();
		}
	}

	private void Rp_OnAIPilotReady(bool rdy)
	{
		aiRdy = rdy;
		SendRPC("RPC_SetAIReady", rdy ? 1 : 0);
	}

	[VTRPC]
	private void RPC_SetAIReady(int r)
	{
		rp.RemoteSetAIReady(r > 0);
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
		int num = -1;
		if ((bool)rp.targetRefuelPort && (bool)rp.targetRefuelPort.actor)
		{
			num = VTNetUtils.GetActorIdentifier(rp.targetRefuelPort.actor);
		}
		SendDirectedRPC(obj, "RPC_SetTarget", num);
		SendDirectedRPC(obj, "RPC_SetAIReady", aiRdy ? 1 : 0);
	}

	private void Rp_OnSetRefuelPort(RefuelPort obj)
	{
		int num = -1;
		if ((bool)obj && (bool)obj.actor)
		{
			num = VTNetUtils.GetActorIdentifier(obj.actor);
		}
		SendRPC("RPC_SetTarget", num);
	}

	[VTRPC]
	private void RPC_SetTarget(int actorId)
	{
		Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorId);
		Debug.Log("RefuelPlaneSync RPC_SetTarget(" + (actorFromIdentifier ? actorFromIdentifier.actorName : "null") + ")");
		if ((bool)actorFromIdentifier)
		{
			rp.RemoteSetTargetPort(actorFromIdentifier.GetComponentInChildren<RefuelPort>(includeInactive: true));
		}
		else
		{
			rp.RemoteSetTargetPort(null);
		}
	}
}

}