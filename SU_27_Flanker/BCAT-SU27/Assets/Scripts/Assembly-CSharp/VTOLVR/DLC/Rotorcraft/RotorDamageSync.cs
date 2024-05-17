using Steamworks;
using VTNetworking;

namespace VTOLVR.DLC.Rotorcraft{

public class RotorDamageSync : VTNetSyncRPCOnly
{
	public HelicopterRotor rotor;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			rotor.OnDamageLevel += Rotor_OnDamageLevel;
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			rotor.doCollision = false;
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
		SendDirectedRPC(obj, "RPC_Damage", rotor.damageLevel);
	}

	private void Rotor_OnDamageLevel(int damageLevel)
	{
		SendRPC("RPC_Damage", damageLevel);
	}

	[VTRPC]
	private void RPC_Damage(int idx)
	{
		if (rotor.damageLevel != idx)
		{
			rotor.DamageRotor(idx);
		}
	}
}

}