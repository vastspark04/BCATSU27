using Steamworks;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class LandingGearSync : VTNetSyncRPCOnly
{
	public GearAnimator gear;

	private bool awaitingRefresh;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			gear.OnSetTargetState += Gear_OnSetTargetState;
			gear.OnSetStateImmediate += Gear_OnSetStateImmediate;
			Refresh(0uL);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			gear.remoteOnly = true;
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
		Refresh(obj);
	}

	private void Gear_OnSetTargetState(GearAnimator.GearStates obj)
	{
		switch (obj)
		{
		case GearAnimator.GearStates.Extended:
			SendRPC("RPC_SetGear", 1);
			break;
		case GearAnimator.GearStates.Retracted:
			SendRPC("RPC_SetGear", 0);
			break;
		}
	}

	private void Gear_OnSetStateImmediate(int state)
	{
		SendRPC("RPC_RImmediate", state);
	}

	[VTRPC]
	private void RPC_SetGear(int state)
	{
		gear.remoteOnly = false;
		if (awaitingRefresh)
		{
			if (state > 0)
			{
				gear.ExtendImmediate();
			}
			else
			{
				gear.RetractImmediate();
			}
			awaitingRefresh = false;
		}
		else
		{
			gear.SetExtend(state);
		}
		gear.remoteOnly = true;
	}

	private void Refresh(ulong target = 0uL)
	{
		if (base.isMine)
		{
			SendDirectedRPC(target, "RPC_SetGear", (gear.targetState == GearAnimator.GearStates.Extended) ? 1 : 0);
		}
	}

	[VTRPC]
	private void RPC_RImmediate(int state)
	{
		gear.remoteOnly = false;
		if (state > 0)
		{
			gear.ExtendImmediate();
		}
		else
		{
			gear.RetractImmediate();
		}
		gear.remoteOnly = true;
	}
}

}