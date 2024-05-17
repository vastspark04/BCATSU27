using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class CanopySync : VTNetSyncRPCOnly
{
	public CanopyAnimator canopy;

	private bool awaitingRefresh;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		Debug.Log("CanopySync initialized");
		if (base.isMine)
		{
			canopy.OnSetState += Canopy_OnSetState;
			canopy.OnBroken += Canopy_OnBroken;
			Refresh(0uL);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			canopy.SetToRemote();
			awaitingRefresh = true;
		}
	}

	private void OnEnable()
	{
		awaitingRefresh = true;
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
			Refresh(obj.Value);
		}
	}

	private void Refresh(ulong target = 0uL)
	{
		if (!base.isMine)
		{
			return;
		}
		if (canopy.isBroken)
		{
			if (target == 0L)
			{
				SendRPC("RPC_Break");
			}
			else
			{
				SendDirectedRPC(target, "RPC_Break");
			}
		}
		else if (target == 0L)
		{
			SendRPC("RPC_State", canopy.targetState);
		}
		else
		{
			SendDirectedRPC(target, "RPC_State", canopy.targetState);
		}
	}

	private void Canopy_OnBroken()
	{
		SendRPC("RPC_Break");
	}

	[VTRPC]
	private void RPC_Break()
	{
		canopy.BreakCanopy();
	}

	private void Canopy_OnSetState(int state)
	{
		SendRPC("RPC_State", state);
	}

	[VTRPC]
	private void RPC_State(int st)
	{
		Debug.Log($"Canopy RPC_State({st})");
		if (awaitingRefresh)
		{
			awaitingRefresh = false;
			canopy.SetCanopyImmediate(st > 0);
		}
		else
		{
			canopy.SetCanopyState(st);
		}
	}
}

}