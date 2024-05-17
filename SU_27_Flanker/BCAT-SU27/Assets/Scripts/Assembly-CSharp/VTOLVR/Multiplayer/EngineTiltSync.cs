using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class EngineTiltSync : VTNetSyncRPCOnly
{
	public TiltController tiltController;

	private float tilt;

	private bool localTiltDirty;

	private float r_tilt;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			tiltController.OnTiltChanged += TiltController_OnTiltChanged;
			Refresh(0uL);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
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
			StartCoroutine(LocalRoutine());
			Refresh(0uL);
		}
		else
		{
			StartCoroutine(RemoteRoutine());
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
		Refresh(obj);
	}

	private void Refresh(ulong target = 0uL)
	{
		SendDirectedRPC(target, "RPC_Tilt", tiltController.currentTilt);
	}

	private void TiltController_OnTiltChanged(float obj)
	{
		if (obj != tilt)
		{
			localTiltDirty = true;
			tilt = obj;
		}
	}

	private IEnumerator LocalRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(VTNetworkManager.CurrentSendInterval);
		while (base.enabled)
		{
			if (localTiltDirty)
			{
				SendRPC("RPC_Tilt", tilt);
				localTiltDirty = false;
			}
			yield return wait;
		}
	}

	[VTRPC]
	private void RPC_Tilt(float t)
	{
		tilt = t;
	}

	private IEnumerator RemoteRoutine()
	{
		while (base.enabled)
		{
			r_tilt = Mathf.Lerp(r_tilt, tilt, 10f * Time.deltaTime);
			tiltController.SetTiltImmediate(r_tilt);
			yield return null;
		}
	}
}

}