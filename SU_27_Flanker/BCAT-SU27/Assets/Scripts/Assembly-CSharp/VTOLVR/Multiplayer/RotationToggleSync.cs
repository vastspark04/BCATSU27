using Steamworks;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class RotationToggleSync : VTNetSyncRPCOnly
{
	public RotationToggle rt;

	private bool awaitingRefresh;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			rt.OnStartDeploy += Rt_OnStartDeploy;
			rt.OnStartRetract += Rt_OnStartRetract;
			rt.OnStateSetImmediate += Rt_OnStateSetImmediate;
			Refresh(immediate: true);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			rt.remoteOnly = true;
			awaitingRefresh = true;
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
		if (base.isMine)
		{
			if (rt.deployed)
			{
				SendDirectedRPC(obj, "RPC_DepImm");
			}
			else
			{
				SendDirectedRPC(obj, "RPC_RetImm");
			}
		}
	}

	private void Refresh(bool immediate)
	{
		if (!base.isMine)
		{
			return;
		}
		if (rt.deployed)
		{
			if (immediate)
			{
				SendRPC("RPC_DepImm");
			}
			else
			{
				SendRPC("RPC_Deploy");
			}
		}
		else if (immediate)
		{
			SendRPC("RPC_RetImm");
		}
		else
		{
			SendRPC("RPC_Retract");
		}
	}

	private void Rt_OnStartRetract()
	{
		SendRPC("RPC_Retract");
	}

	[VTRPC]
	private void RPC_Retract()
	{
		rt.remoteOnly = false;
		if (awaitingRefresh)
		{
			awaitingRefresh = false;
			rt.SetNormalizedRotationImmediate(0f);
		}
		else
		{
			rt.SetDefault();
		}
		rt.remoteOnly = true;
	}

	[VTRPC]
	private void RPC_RetImm()
	{
		rt.remoteOnly = false;
		rt.SetNormalizedRotationImmediate(0f);
		rt.remoteOnly = true;
	}

	private void Rt_OnStartDeploy()
	{
		SendRPC("RPC_Deploy");
	}

	[VTRPC]
	private void RPC_Deploy()
	{
		rt.remoteOnly = false;
		if (awaitingRefresh)
		{
			awaitingRefresh = false;
			rt.SetNormalizedRotationImmediate(1f);
		}
		else
		{
			rt.SetDeployed();
		}
		rt.remoteOnly = true;
	}

	[VTRPC]
	private void RPC_DepImm()
	{
		rt.remoteOnly = false;
		rt.SetNormalizedRotationImmediate(1f);
		rt.remoteOnly = true;
	}

	private void Rt_OnStateSetImmediate(float obj)
	{
		SendRPC("RPC_NormImm", obj);
	}

	[VTRPC]
	private void RPC_NormImm(float t)
	{
		rt.remoteOnly = false;
		rt.SetNormalizedRotationImmediate(t);
		rt.remoteOnly = true;
	}
}

}