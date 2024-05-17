using Steamworks;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VRDoorSync : VTNetSyncRPCOnly
{
	public VRDoor door;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		door.OnSetNormalizedState += Door_OnSetNormalizedState;
		foreach (VRDoor attachedHandle in door.attachedHandles)
		{
			attachedHandle.OnSetNormalizedState += Door_OnSetNormalizedState;
		}
		if (base.isMine)
		{
			Door_OnSetNormalizedState(door.currentAngle / door.maxDoorAngle);
		}
		VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
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
		float num = door.currentAngle / door.maxDoorAngle;
		SendDirectedRPC(obj, "RPC_D", num);
	}

	private void Door_OnSetNormalizedState(float t)
	{
		SendRPC("RPC_D", t);
	}

	[VTRPC]
	private void RPC_D(float t)
	{
		door.RemoteSetState(t, sendEvent: false);
	}
}

}