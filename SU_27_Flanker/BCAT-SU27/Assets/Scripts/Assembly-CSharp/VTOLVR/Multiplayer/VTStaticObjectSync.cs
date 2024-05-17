using Steamworks;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTStaticObjectSync : VTNetSyncRPCOnly
{
	public VTStaticObject vtso;

	public string resourcePath;

	private int soID;

	protected override void Awake()
	{
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
	}

	private void Instance_OnNewClientConnected(SteamId id)
	{
		SendDirectedRPC(id, "RPC_SO_Id", soID);
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	public void SendID(int soID)
	{
		this.soID = soID;
		vtso.SetNewID(soID);
		if (base.isMine)
		{
			SendRPC("RPC_SO_Id", soID);
		}
	}

	[VTRPC]
	private void RPC_SO_Id(int soID)
	{
		this.soID = soID;
		vtso.SetNewID(soID);
		VTOLMPUnitManager.instance.Client_ReplaceStaticObject(vtso);
	}
}

}