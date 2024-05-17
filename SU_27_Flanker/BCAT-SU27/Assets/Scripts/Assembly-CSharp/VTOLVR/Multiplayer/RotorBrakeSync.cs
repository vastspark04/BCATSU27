using System.Collections;
using Steamworks;
using VTNetworking;
using VTOLVR.DLC.Rotorcraft;

namespace VTOLVR.Multiplayer{

public class RotorBrakeSync : VTNetSyncRPCOnly
{
	public RotorBrake rotorBrake;

	private void OnEnable()
	{
		StartCoroutine(EnableRoutine());
	}

	private IEnumerator EnableRoutine()
	{
		while (!wasRegistered)
		{
			yield return null;
		}
		if (base.isMine)
		{
			RotorBrake_OnSetBrake(rotorBrake.IsBraking());
		}
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			rotorBrake.OnSetBrake += RotorBrake_OnSetBrake;
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		if (base.isMine)
		{
			SendDirectedRPC(obj, "RPC_B", rotorBrake.IsBraking() ? 1 : 0);
		}
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void RotorBrake_OnSetBrake(bool b)
	{
		if (base.isMine)
		{
			SendRPC("RPC_B", b ? 1 : 0);
		}
	}

	[VTRPC]
	private void RPC_B(int b)
	{
		rotorBrake.RemoteSetBrake(b > 0);
	}
}

}