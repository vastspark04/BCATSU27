using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class EngageEnemiesAnimationSync : VTNetSyncRPCOnly, IEngageEnemies
{
	public EngageEnemiesAnimation[] anims;

	public RadarDeployAnimator[] radarAnims;

	private bool localEngage;

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
		if (base.isMine && wasRegistered)
		{
			SendDirectedRPC(obj, "RPC_E", localEngage ? 1 : 0);
		}
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
			SetEngageEnemies(localEngage);
		}
	}

	public void SetEngageEnemies(bool engage)
	{
		Debug.Log($"{base.gameObject.GetInstanceID()} {base.gameObject.name} SetEngageEnemies({engage})");
		if (base.isMine)
		{
			localEngage = engage;
			if (wasRegistered)
			{
				SendRPC("RPC_E", engage ? 1 : 0);
			}
		}
	}

	[VTRPC]
	private void RPC_E(int e)
	{
		Debug.Log($"{base.gameObject.GetInstanceID()} {base.gameObject.name} RPC_E({e > 0})");
		for (int i = 0; i < anims.Length; i++)
		{
			anims[i].SetEngageEnemies(e > 0);
		}
		for (int j = 0; j < radarAnims.Length; j++)
		{
			radarAnims[j].SetEngageEnemies(e > 0);
		}
	}
}

}