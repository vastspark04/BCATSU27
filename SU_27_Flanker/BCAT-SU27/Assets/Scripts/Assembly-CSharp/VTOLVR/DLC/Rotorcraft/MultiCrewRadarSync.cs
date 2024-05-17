using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class MultiCrewRadarSync : VTNetSyncRPCOnly
{
	public MultiUserVehicleSync muvs;

	public MFDRadarUI radarUI;

	public TargetingMFDPage tgpPage;

	private bool requestingSlave;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			radarUI.OnHardLockActor += RadarUI_OnHardLockActor;
			radarUI.OnUIDetectedActor += RadarUI_OnUIDetectedActor;
			radarUI.OnSetRadarPower += RadarUI_OnSetRadarPower;
			radarUI.OnUnlocked += RadarUI_OnUnlocked;
			muvs.OnOccupantEntered += Muvs_OnOccupantEntered;
			muvs.OnOccupantLeft += Muvs_OnOccupantLeft;
		}
		else if ((bool)radarUI)
		{
			radarUI.isRemoteUI = true;
			OnSetRemotePlayerRadar(radarUI.playerRadar, radarUI.lockingRadar);
			radarUI.OnSetPlayerRadar += OnSetRemotePlayerRadar;
			radarUI.OnRemoteAttemptHardlock += RadarUI_OnRemoteAttemptHardlock;
			radarUI.OnSetRadarPower += RadarUI_OnSetRadarPower_Remote;
			radarUI.OnRemoteUnlock += RadarUI_OnRemoteUnlock;
		}
		radarUI.OnRangeIdx += RadarUI_OnRangeIdx;
		radarUI.radarCtrlr.OnElevationAdjusted += RadarCtrlr_OnElevationAdjusted;
		radarUI.OnToggledAGMode += RadarUI_OnToggledAGMode;
	}

	private void RadarUI_OnToggledAGMode(bool obj)
	{
		muvs.SendRPCToCopilots(this, "RPC_AGMode", obj ? 1 : 0);
	}

	[VTRPC]
	private void RPC_AGMode(int m)
	{
		radarUI.RemoteSetAGMode(m > 0);
	}

	private void RadarCtrlr_OnElevationAdjusted(float obj)
	{
		muvs.SendRPCToCopilots(this, "RPC_Elev", obj);
	}

	[VTRPC]
	private void RPC_Elev(float e)
	{
		radarUI.radarCtrlr.currentElevationAdjust = e;
		radarUI.UpdateElevationText(e);
	}

	private void Muvs_OnOccupantEntered(int seatIdx, ulong userID)
	{
		if (!base.isMine || userID == BDSteamClient.mySteamID)
		{
			return;
		}
		if ((bool)radarUI.playerRadar)
		{
			SendDirectedRPC(userID, "RPC_RadarPwr", radarUI.playerRadar.radarEnabled ? 1 : 0);
			if ((bool)radarUI.currentLockedActor)
			{
				SendDirectedRPC(userID, "RPC_HardLock", VTNetUtils.GetActorIdentifier(radarUI.currentLockedActor));
			}
		}
		SendDirectedRPC(userID, "RPC_RangeIdx", radarUI.viewRangeIdx);
	}

	private void Muvs_OnOccupantLeft(int seatIdx, ulong userID)
	{
		if (!muvs.IsLocalPlayerSeated() && (bool)radarUI.grDispatcher)
		{
			radarUI.grDispatcher.radarCamera.enabled = false;
			radarUI.grDispatcher.enabled = false;
		}
	}

	private void RadarUI_OnRangeIdx(int obj)
	{
		muvs.SendRPCToCopilots(this, "RPC_RangeIdx", obj);
	}

	[VTRPC]
	private void RPC_RangeIdx(int idx)
	{
		radarUI.RemoteSetRange(idx);
	}

	private void RadarUI_OnUnlocked()
	{
		muvs.SendRPCToCopilots(this, "RPC_Unlock");
	}

	[VTRPC]
	private void RPC_Unlock()
	{
		radarUI.Unlock();
	}

	private void RadarUI_OnSetRadarPower_Remote(int st)
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_RemotePwr", st);
	}

	[VTRPC]
	private void RPC_RemotePwr(int p)
	{
		radarUI.SetRadarPower(p);
	}

	private void RadarUI_OnSetRadarPower(int st)
	{
		muvs.SendRPCToCopilots(this, "RPC_RadarPwr", st);
	}

	[VTRPC]
	private void RPC_RadarPwr(int p)
	{
		radarUI.RemoteRadarPower(p);
	}

	private void RadarUI_OnRemoteAttemptHardlock(Actor a)
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_RemoteHardlock", VTNetUtils.GetActorIdentifier(a));
	}

	[VTRPC]
	private void RPC_RemoteHardlock(int actorID)
	{
		if (base.isMine)
		{
			radarUI.RemoteHardLock(VTNetUtils.GetActorFromIdentifier(actorID));
		}
	}

	private void RadarUI_OnRemoteUnlock()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_RemoteUnlock");
	}

	[VTRPC]
	private void RPC_RemoteUnlock()
	{
		if (base.isMine)
		{
			radarUI.Unlock();
		}
	}

	private void OnSetRemotePlayerRadar(Radar r, LockingRadar lr)
	{
		if ((bool)r)
		{
			r.transmissionStrength = 1E-07f;
		}
		if ((bool)lr)
		{
			lr.transmissionStrength = 1E-06f;
		}
	}

	private void RadarUI_OnUIDetectedActor(Actor obj)
	{
		muvs.SendRPCToCopilots(this, "RPC_Detect", VTNetUtils.GetActorIdentifier(obj));
	}

	[VTRPC]
	private void RPC_Detect(int actorId)
	{
		radarUI.RemoteDetectActor(VTNetUtils.GetActorFromIdentifier(actorId));
	}

	private void RadarUI_OnHardLockActor(Actor a)
	{
		muvs.SendRPCToCopilots(this, "RPC_HardLock", VTNetUtils.GetActorIdentifier(a));
	}

	[VTRPC]
	private void RPC_HardLock(int actorId)
	{
		radarUI.RemoteHardLock(VTNetUtils.GetActorFromIdentifier(actorId));
	}

	public void RemoteRequestTGPSlave()
	{
		if ((bool)radarUI.currentLockedActor)
		{
			if (tgpPage.isSOI)
			{
				tgpPage.SlewAndLockActor(radarUI.currentLockedActor, 360f);
			}
			else if (!requestingSlave)
			{
				StartCoroutine(RequestRadarSlaveRoutine());
			}
		}
	}

	private IEnumerator RequestRadarSlaveRoutine()
	{
		requestingSlave = true;
		Debug.Log("Requesting permission to slave TGP to radar.");
		bool success = true;
		for (int i = 0; i < muvs.seatCount && success; i++)
		{
			ulong id = muvs.GetOccupantID(i);
			if (id == 0L || id == BDSteamClient.mySteamID)
			{
				continue;
			}
			RPCRequest req = SendRPCRequest(typeof(int), id, "RPC_ReqSlave");
			while (!req.isComplete)
			{
				yield return null;
			}
			if ((int)req.Value == 0)
			{
				Debug.Log(" -- TGP Slave request failed.  TGP in use by " + new Friend(id).Name);
				if ((bool)radarUI.errorFlasher)
				{
					radarUI.errorFlasher.DisplayError("SENSOR IN USE", 2f);
				}
				success = false;
			}
		}
		if (success && (bool)radarUI.currentLockedActor)
		{
			Debug.Log(" -- TGP Slave request granted.");
			muvs.TakeTGPControl();
			tgpPage.SlewAndLockActor(radarUI.currentLockedActor, 360f);
		}
		requestingSlave = false;
	}

	[VTRPC]
	private int RPC_ReqSlave()
	{
		if (tgpPage.isSOI)
		{
			return 0;
		}
		return 1;
	}
}

}