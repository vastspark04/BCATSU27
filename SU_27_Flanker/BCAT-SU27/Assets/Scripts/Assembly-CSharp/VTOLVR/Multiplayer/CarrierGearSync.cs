using Steamworks;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class CarrierGearSync : VTNetSyncRPCOnly
{
	public Tailhook tailHook;

	public CatapultHook catHook;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			tailHook.OnExtendState += TailHook_OnExtendState;
			catHook.OnExtendState += CatHook_OnExtendState;
			catHook.OnHooked.AddListener(CatHook_OnHooked);
			Refresh(0uL);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			catHook.SetToRemote();
		}
	}

	private void CatHook_OnHooked()
	{
		Actor componentInParent = catHook.catapult.GetComponentInParent<Actor>();
		int num = catHook.catapult.GetComponentInParent<CarrierCatapultManager>().catapults.IndexOf(catHook.catapult);
		SendRPC("RPC_CatHooked", VTNetUtils.GetActorIdentifier(componentInParent), num);
	}

	[VTRPC]
	private void RPC_CatHooked(int actorId, int catIdx)
	{
		Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorId);
		if ((bool)actorFromIdentifier)
		{
			CarrierCatapult c = actorFromIdentifier.GetComponent<CarrierCatapultManager>().catapults[catIdx];
			catHook.RemoteHook(c);
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
		if (target == 0L)
		{
			SendRPC("RPC_CatState", catHook.deployed ? 1 : 0);
			SendRPC("RPC_TailState", tailHook.isDeployed ? 1 : 0);
		}
		else
		{
			SendDirectedRPC(target, "RPC_CatState", catHook.deployed ? 1 : 0);
			SendDirectedRPC(target, "RPC_TailState", tailHook.isDeployed ? 1 : 0);
		}
	}

	private void CatHook_OnExtendState(int state)
	{
		SendRPC("RPC_CatState", state);
	}

	[VTRPC]
	private void RPC_CatState(int state)
	{
		catHook.SetState(state);
	}

	private void TailHook_OnExtendState(int state)
	{
		SendRPC("RPC_TailState", state);
	}

	[VTRPC]
	private void RPC_TailState(int state)
	{
		tailHook.SetHook(state);
	}
}

}