using System.Collections;
using Steamworks;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class AIRadarSync : VTNetSyncRPCOnly
{
	public Radar radar;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
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
		if (radar.destroyed)
		{
			SendDirectedRPC(obj, "RPC_DestroyRadar");
			return;
		}
		SendDirectedRPC(obj, "RPC_RadarEnabled", radar.radarEnabled ? 1 : 0);
	}

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
			radar.OnRadarEnabled -= Radar_OnRadarEnabled;
			radar.OnRadarEnabled += Radar_OnRadarEnabled;
			radar.OnRadarDestroyed -= Radar_OnRadarDestroyed;
			radar.OnRadarDestroyed += Radar_OnRadarDestroyed;
		}
		else
		{
			radar.SetToMPRemote();
		}
	}

	private void Radar_OnRadarDestroyed()
	{
		SendRPC("RPC_DestroyRadar");
	}

	private void Radar_OnRadarEnabled(bool obj)
	{
		SendRPC("RPC_RadarEnabled", obj ? 1 : 0);
	}

	[VTRPC]
	private void RPC_RadarEnabled(int i_enabled)
	{
		radar.radarEnabled = i_enabled > 0;
	}

	[VTRPC]
	private void RPC_DestroyRadar()
	{
		radar.KillRadar();
	}
}

}