using System.Collections;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class ConnectedThrottlesSync : VTNetSyncRPCOnly
{
	public ConnectedThrottles cThrottle;

	public MultiUserVehicleSync muvs;

	public bool requireControlOwner = true;

	private bool[] localGrabs;

	private bool skippedDefaults;

	protected override void Awake()
	{
		base.Awake();
		localGrabs = new bool[cThrottle.throttles.Length];
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		cThrottle.OnLocalGrabbedThrottle += OnGrabbedThrottle;
		cThrottle.OnLocalReleasedThrottle += OnReleasedThrottle;
		if (base.isMine)
		{
			cThrottle.OnSetMasterIdx += CThrottle_OnSetMasterIdx;
		}
		muvs.OnOccupantEntered += Muvs_OnOccupantEntered;
	}

	private void Muvs_OnOccupantEntered(int seatIdx, ulong userID)
	{
		if (userID != BDSteamClient.mySteamID && ((!requireControlOwner && base.isMine) || (requireControlOwner && muvs.IsControlOwner())))
		{
			SendDirectedRPC(userID, "RPC_OverrideMaster", cThrottle.CurrentMasterIdx);
			if (cThrottle.CurrentMasterIdx >= 0)
			{
				SendDirectedRPC(userID, "RPC_OvrrdThrottles", cThrottle.throttles[cThrottle.CurrentMasterIdx].currentThrottle);
			}
			else
			{
				SendDirectedRPC(userID, "RPC_OvrrdThrottles", cThrottle.throttles[0].currentThrottle);
			}
		}
	}

	private void CThrottle_OnSetMasterIdx(int idx)
	{
		muvs.SendRPCToCopilots(this, "RPC_OverrideMaster", idx);
	}

	[VTRPC]
	private void RPC_OverrideMaster(int idx)
	{
		cThrottle.OverrideSetMaster(idx);
	}

	private void OnEnable()
	{
		StartCoroutine(SendRoutine());
	}

	private IEnumerator SendRoutine()
	{
		while (!wasRegistered)
		{
			yield return null;
		}
		WaitForSeconds wait = new WaitForSeconds(VTNetworkManager.CurrentSendInterval);
		while (base.enabled)
		{
			if (cThrottle.CurrentMasterIdx < 0)
			{
				for (int i = 0; i < localGrabs.Length; i++)
				{
					if (localGrabs[i])
					{
						cThrottle.OverrideSetMaster(i);
						break;
					}
				}
			}
			if (LocalIsMaster())
			{
				for (int j = 0; j < cThrottle.throttles.Length; j++)
				{
					if (localGrabs[j] && cThrottle.CurrentMasterIdx != j)
					{
						cThrottle.OverrideSetMaster(j);
					}
					cThrottle.throttles[j].SetRemoteOnly(r: false);
					cThrottle.throttles[j].sendEvents = localGrabs[j];
				}
				if (cThrottle.CurrentMasterIdx >= 0)
				{
					muvs.SendRPCToCopilots(this, "RPC_RemoteThrottle", cThrottle.CurrentMasterIdx, cThrottle.throttles[cThrottle.CurrentMasterIdx].currentThrottle);
				}
			}
			else
			{
				for (int k = 0; k < cThrottle.throttles.Length; k++)
				{
					cThrottle.throttles[k].SetRemoteOnly(r: true);
				}
			}
			yield return wait;
		}
	}

	private bool LocalIsMaster()
	{
		if (cThrottle.CurrentMasterIdx >= 0 && localGrabs[cThrottle.CurrentMasterIdx])
		{
			if (requireControlOwner)
			{
				return muvs.IsControlOwner();
			}
			return true;
		}
		return false;
	}

	public void ResetUngrabbedControls()
	{
		for (int i = 0; i < cThrottle.throttles.Length; i++)
		{
			if (!localGrabs[i])
			{
				RPC_RemoteInteract(i, 0);
			}
		}
	}

	private void OnGrabbedThrottle(int tIdx)
	{
		localGrabs[tIdx] = true;
		muvs.SendRPCToCopilots(this, "RPC_RemoteInteract", tIdx, 1);
	}

	private void OnReleasedThrottle(int tIdx)
	{
		localGrabs[tIdx] = false;
		muvs.SendRPCToCopilots(this, "RPC_RemoteInteract", tIdx, 0);
	}

	[VTRPC]
	private void RPC_RemoteInteract(int idx, int grabbed)
	{
		if (grabbed > 0)
		{
			cThrottle.RemoteGrab(idx);
		}
		else
		{
			cThrottle.RemoteRelease(idx);
		}
	}

	[VTRPC]
	private void RPC_RemoteThrottle(int idx, float throttle)
	{
		if (!skippedDefaults)
		{
			skippedDefaults = true;
			for (int i = 0; i < cThrottle.throttles.Length; i++)
			{
				cThrottle.throttles[i].skipApplyDefaults = true;
			}
		}
		cThrottle.throttles[idx].RemoteSetThrottle(throttle);
	}

	[VTRPC]
	private void RPC_OvrrdThrottles(float throttle)
	{
		Debug.Log($"RPC_OvrrdThrottles({throttle})");
		if (!skippedDefaults)
		{
			skippedDefaults = true;
			for (int i = 0; i < cThrottle.throttles.Length; i++)
			{
				cThrottle.throttles[i].skipApplyDefaults = true;
			}
		}
		cThrottle.OverrideSetThrottles(throttle);
	}
}

}