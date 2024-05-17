using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class RefuelPortSync : VTNetSyncRPCOnly
{
	public RefuelPort refuelPort;

	public FuelDump fuelDump;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			refuelPort.OnSetState += RefuelPort_OnSetState;
			if ((bool)fuelDump)
			{
				fuelDump.OnDumpState += FuelDump_OnDumpState;
			}
			Refresh(0uL);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			refuelPort.isPlayer = false;
			refuelPort.SetToRemote();
			if ((bool)fuelDump)
			{
				fuelDump.SetToRemote();
			}
		}
	}

	private void OnEnable()
	{
		StartCoroutine(EnabledRoutine());
	}

	private IEnumerator EnabledRoutine()
	{
		while (!wasRegistered)
		{
			yield return null;
		}
		if (!base.isMine)
		{
			yield break;
		}
		bool needsFuel = false;
		while (base.enabled)
		{
			bool flag = refuelPort.fuelTank.fuelFraction < 0.95f;
			if (flag != needsFuel)
			{
				needsFuel = flag;
				SendRPC("RPC_SetNeedsFuel", needsFuel ? 1 : 0);
			}
			yield return null;
		}
	}

	[VTRPC]
	private void RPC_SetNeedsFuel(int n)
	{
		refuelPort.remoteNeedsFuel = n > 0;
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		Refresh(obj);
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void FuelDump_OnDumpState(int state)
	{
		SendRPC("RPC_DumpState", state);
	}

	[VTRPC]
	private void RPC_DumpState(int state)
	{
		if ((bool)fuelDump)
		{
			fuelDump.SetDump(state);
			return;
		}
		Debug.LogError("Received RPC_DumpState but fuelDump was null.");
		if (this != null && base.gameObject != null)
		{
			Debug.LogError(" - Hierarchy: " + UIUtils.GetHierarchyString(base.gameObject));
		}
		else
		{
			Debug.LogError(" - this is null.");
		}
	}

	private void Refresh(ulong target = 0uL)
	{
		SendDirectedRPC(target, "RPC_PortState", refuelPort.open ? 1 : 0);
		if ((bool)fuelDump)
		{
			SendDirectedRPC(target, "RPC_DumpState", fuelDump.isDumping ? 1 : 0);
		}
	}

	private void RefuelPort_OnSetState(int st)
	{
		SendRPC("RPC_PortState", st);
	}

	[VTRPC]
	private void RPC_PortState(int st)
	{
		refuelPort.SetOpenState(st);
	}
}

}