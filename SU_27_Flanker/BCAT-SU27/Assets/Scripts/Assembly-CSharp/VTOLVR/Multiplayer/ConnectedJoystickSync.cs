using System.Collections;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class ConnectedJoystickSync : VTNetSyncRPCOnly
{
	public ConnectedJoysticks cStick;

	public MultiUserVehicleSync muvs;

	public bool[] localGrabs;

	protected override void Awake()
	{
		base.Awake();
		localGrabs = new bool[cStick.joysticks.Length];
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		cStick.OnLocalGrabbedStick += OnGrabbedStick;
		cStick.OnLocalReleasedStick += OnReleasedStick;
		if (base.isMine)
		{
			cStick.OnSetMasterIdx += CStick_OnSetMasterIdx;
		}
	}

	private void CStick_OnSetMasterIdx(int idx)
	{
		muvs.SendRPCToCopilots(this, "RPC_OverrideMaster", idx);
	}

	[VTRPC]
	private void RPC_OverrideMaster(int idx)
	{
		cStick.OverrideSetMaster(idx);
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
			if (cStick.CurrentMasterIdx < 0)
			{
				for (int i = 0; i < localGrabs.Length; i++)
				{
					if (localGrabs[i])
					{
						cStick.OverrideSetMaster(i);
						break;
					}
				}
			}
			if (LocalIsMaster())
			{
				for (int j = 0; j < cStick.joysticks.Length; j++)
				{
					if (localGrabs[j] && cStick.CurrentMasterIdx != j)
					{
						cStick.OverrideSetMaster(j);
					}
					cStick.joysticks[j].SetRemoteOnly(r: false);
					cStick.joysticks[j].sendEvents = localGrabs[j];
				}
				if (cStick.CurrentMasterIdx >= 0)
				{
					muvs.SendRPCToCopilots(this, "RPC_RemoteJoystick", cStick.CurrentMasterIdx, cStick.joysticks[cStick.CurrentMasterIdx].CurrentStick);
				}
			}
			else if (cStick.CurrentMasterIdx == -1)
			{
				for (int k = 0; k < cStick.joysticks.Length; k++)
				{
					cStick.joysticks[k].SetRemoteOnly(r: false);
				}
			}
			else
			{
				for (int l = 0; l < cStick.joysticks.Length; l++)
				{
					cStick.joysticks[l].SetRemoteOnly(r: true);
				}
			}
			yield return wait;
		}
	}

	private bool LocalIsMaster()
	{
		if (cStick.CurrentMasterIdx >= 0 && localGrabs[cStick.CurrentMasterIdx])
		{
			return muvs.IsControlOwner();
		}
		return false;
	}

	private void OnGrabbedStick(int tIdx)
	{
		localGrabs[tIdx] = true;
		muvs.SendRPCToCopilots(this, "RPC_RemoteInteract", tIdx, 1);
	}

	private void OnReleasedStick(int tIdx)
	{
		localGrabs[tIdx] = false;
		muvs.SendRPCToCopilots(this, "RPC_RemoteInteract", tIdx, 0);
	}

	public void ResetUngrabbedControls()
	{
		for (int i = 0; i < cStick.joysticks.Length; i++)
		{
			if (!localGrabs[i])
			{
				RPC_RemoteInteract(i, 0);
			}
		}
	}

	[VTRPC]
	private void RPC_RemoteInteract(int idx, int grabbed)
	{
		if (grabbed > 0)
		{
			cStick.RemoteGrab(idx);
		}
		else
		{
			cStick.RemoteRelease(idx);
		}
	}

	[VTRPC]
	private void RPC_RemoteJoystick(int idx, Vector3 stick)
	{
		cStick.joysticks[idx].RemoteSetStick(stick);
	}
}

}