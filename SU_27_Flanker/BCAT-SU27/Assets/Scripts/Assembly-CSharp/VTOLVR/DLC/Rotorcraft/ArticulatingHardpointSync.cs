using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class ArticulatingHardpointSync : VTNetSyncRPCOnly
{
	public ArticulatingHardpoint ahp;

	public MultiUserVehicleSync muvs;

	private float[] syncedTilts;

	private float[] lerpedTilts;

	private bool hasSetup;

	private float lastRemoteInput;

	private float timeRemoteInputted;

	private float syncedRemoteInput;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			ahp.OnSetTilt += Ahp_OnSetTilt;
			if ((bool)muvs)
			{
				ahp.OnSetAuto += Ahp_OnSetAuto;
			}
		}
		else
		{
			ahp.remoteOnly = true;
		}
		Setup();
	}

	protected override void Awake()
	{
		base.Awake();
		Setup();
	}

	private void Ahp_OnSetTilt(int arrIdx, float tiltAngle)
	{
		SendRPC("RPC_SetTilt", arrIdx, tiltAngle);
	}

	[VTRPC]
	private void RPC_SetTilt(int arrIdx, float tiltAngle)
	{
		syncedTilts[arrIdx] = tiltAngle;
	}

	private void Setup()
	{
		if (!hasSetup)
		{
			syncedTilts = new float[ahp.hardpoints.Length];
			lerpedTilts = new float[ahp.hardpoints.Length];
			hasSetup = true;
		}
	}

	private void Ahp_OnSetAuto(bool auto)
	{
		muvs.SendRPCToCopilots(this, "RPC_Auto", auto ? 1 : 0);
	}

	[VTRPC]
	private void RPC_Auto(int a)
	{
		ahp.autoMode = a > 0;
	}

	public void RemoteInput(float i)
	{
		if (!base.isMine && (!(Mathf.Abs(i) < 0.01f) || !(Mathf.Abs(lastRemoteInput) < 0.01f)))
		{
			lastRemoteInput = i;
			SendDirectedRPC(base.netEntity.ownerID, "RPC_RemoteInput", i);
		}
	}

	[VTRPC]
	private void RPC_RemoteInput(float i)
	{
		if (base.isMine)
		{
			syncedRemoteInput = i;
			timeRemoteInputted = Time.time;
		}
	}

	public void RemoteSetAuto()
	{
		if (!base.isMine)
		{
			SendDirectedRPC(base.netEntity.ownerID, "RPC_ToggleAuto");
		}
	}

	[VTRPC]
	private void RPC_ToggleAuto()
	{
		ahp.autoMode = true;
	}

	private void Update()
	{
		if (base.isMine)
		{
			if (Time.time - timeRemoteInputted < 1f)
			{
				ahp.Tilt(syncedRemoteInput, Time.deltaTime);
			}
			return;
		}
		for (int i = 0; i < syncedTilts.Length; i++)
		{
			lerpedTilts[i] = Mathf.Lerp(lerpedTilts[i], syncedTilts[i], 10f * Time.deltaTime);
			ahp.RemoteSetTilt(i, lerpedTilts[i]);
		}
	}
}

}