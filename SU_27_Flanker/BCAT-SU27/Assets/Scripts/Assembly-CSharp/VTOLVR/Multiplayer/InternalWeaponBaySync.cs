using Steamworks;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class InternalWeaponBaySync : VTNetSyncRPCOnly
{
	public InternalWeaponBay[] iwbs;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			for (int i = 0; i < iwbs.Length; i++)
			{
				int idx = i;
				iwbs[i].OnStateChanged += delegate(bool o)
				{
					StateChanged(idx, o);
				};
			}
			Refresh(0uL);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			for (int j = 0; j < iwbs.Length; j++)
			{
				iwbs[j].externallyControlled = true;
			}
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
		for (int i = 0; i < iwbs.Length; i++)
		{
			SendDirectedRPC(target, "RPC_IWB", i, iwbs[i].opening ? 1 : 0);
		}
	}

	private void StateChanged(int idx, bool open)
	{
		SendRPC("RPC_IWB", idx, open ? 1 : 0);
	}

	[VTRPC]
	private void RPC_IWB(int idx, int open)
	{
		InternalWeaponBay internalWeaponBay = iwbs[idx];
		if (open > 0)
		{
			internalWeaponBay.RegisterOpenReq(this);
		}
		else
		{
			internalWeaponBay.UnregisterOpenReq(this);
		}
	}
}

}