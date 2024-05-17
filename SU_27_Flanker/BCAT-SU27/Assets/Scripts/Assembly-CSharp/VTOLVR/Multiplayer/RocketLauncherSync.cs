using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class RocketLauncherSync : EquippableSync
{
	public RocketLauncher rl;

	private MultiUserVehicleSync muvs;

	private bool listenedRockets;

	[ContextMenu("Get RL")]
	private void GetRL()
	{
		rl = GetComponent<RocketLauncher>();
	}

	private void OnEnable()
	{
		StartCoroutine(EnableRoutine());
	}

	private IEnumerator EnableRoutine()
	{
		while (!rl.weaponManager)
		{
			yield return null;
		}
		while (!wasRegistered)
		{
			yield return null;
		}
		muvs = rl.weaponManager.GetComponent<MultiUserVehicleSync>();
		if ((bool)muvs)
		{
			rl.weaponManager.OnWeaponFunctionCalled += WeaponManager_OnWeaponFunctionCalled;
			if (!listenedRockets)
			{
				rl.OnFiredRocket += Rl_OnFiredRocket;
				listenedRockets = true;
			}
		}
	}

	private void Rl_OnReloaded()
	{
		RefreshCt(0uL);
	}

	private void OnDisable()
	{
		if ((bool)rl && (bool)rl.weaponManager)
		{
			rl.weaponManager.OnWeaponFunctionCalled -= WeaponManager_OnWeaponFunctionCalled;
		}
	}

	private void WeaponManager_OnWeaponFunctionCalled(int buttonIdx, int weaponIdx)
	{
		if ((bool)muvs && muvs.IsLocalPlayerSeated())
		{
			muvs.SendRPCToCopilots(this, "RPC_SalvoOption", rl.salvoCount);
		}
	}

	[VTRPC]
	private void RPC_SalvoOption(int s)
	{
		for (int i = 0; i < 100; i++)
		{
			if (rl.salvoCount == s)
			{
				break;
			}
			rl.weaponManager.WeaponFunctionButton(0, rl.hardpointIdx, sendEvent: false);
		}
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			rl.OnFiredRocket += Rl_OnFiredRocket;
			rl.OnReloaded += Rl_OnReloaded;
			listenedRockets = true;
			RefreshCt(0uL);
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
		RefreshCt(obj);
	}

	private void Rl_OnFiredRocket(Rocket r)
	{
		bool flag = base.isMine;
		if ((bool)muvs)
		{
			flag = muvs.IsLocalWeaponController();
		}
		if (flag)
		{
			FloatingOrigin.WorldToNetPoint(r.transform.position, out var nsv, out var offset);
			SendRPC("RPC_Rkt", nsv, offset, r.transform.forward);
			if ((bool)rl.weaponManager && rl.weaponManager.actor.isPlayer)
			{
				r.sourcePlayer = VTOLMPLobbyManager.localPlayerInfo;
			}
		}
	}

	[VTRPC]
	private void RPC_Rkt(int nsv, Vector3 offset, Vector3 dir)
	{
		Vector3 position = FloatingOrigin.NetToWorldPoint(offset, nsv);
		rl.MP_FireRocket(position, dir);
	}

	private void RefreshCt(ulong target = 0uL)
	{
		if (base.isMine)
		{
			SendDirectedRPC(target, "RPC_Ct", rl.GetCount());
		}
	}

	[VTRPC]
	private void RPC_Ct(int ct)
	{
		rl.LoadCount(ct);
	}
}

}