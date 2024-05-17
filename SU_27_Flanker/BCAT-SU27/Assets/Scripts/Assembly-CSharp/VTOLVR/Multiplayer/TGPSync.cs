using System.Collections;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class TGPSync : VTNetSyncRPCOnly
{
	public OpticalTargeter targeter;

	public MultiUserVehicleSync muvs;

	private bool hasMuvs;

	private bool listened;

	private float lastMuvsDirTime;

	private float lastODirTime;

	private bool remoteSlewing;

	private Vector3 currentRemoteDir = Vector3.forward;

	private Vector3 syncedRemoteDir = Vector3.forward;

	private bool ignoreNextSlew;

	public bool debug;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if ((bool)muvs)
		{
			hasMuvs = true;
		}
	}

	private void Targeter_OnSetVisibleLaser(bool lase)
	{
		if (!hasMuvs || !muvs.tgpPage.remoteOnly)
		{
			SendRPC("RPC_Laser", lase ? 1 : 0);
		}
	}

	private void OnEnable()
	{
		StartCoroutine(UpdateRoutine());
	}

	private void Targeter_OnSlewedDirection(Vector3 dir)
	{
		if (!hasMuvs || !muvs.tgpPage.remoteOnly)
		{
			if (Time.time - lastODirTime > 0.3f)
			{
				SendRPC("RPC_Slewed", dir);
				lastODirTime = Time.time;
			}
			else if (hasMuvs && muvs.IsLocalPlayerSeated() && Time.time - lastMuvsDirTime > VTNetworkManager.CurrentSendInterval)
			{
				muvs.SendRPCToCopilots(this, "RPC_Slewed", dir);
				lastMuvsDirTime = Time.time;
			}
		}
	}

	private void Targeter_OnUnlocked()
	{
		if (!hasMuvs || !muvs.tgpPage.remoteOnly)
		{
			SendRPC("RPC_Unlock");
		}
	}

	private void Targeter_OnOverridenDirection(Vector3 dir)
	{
		if (!hasMuvs || !muvs.tgpPage.remoteOnly)
		{
			if (Time.time - lastODirTime > 0.3f)
			{
				SendRPC("RPC_PointUnlocked", dir);
				lastODirTime = Time.time;
			}
			else if (hasMuvs && muvs.IsLocalPlayerSeated() && Time.time - lastMuvsDirTime > VTNetworkManager.CurrentSendInterval)
			{
				muvs.SendRPCToCopilots(this, "RPC_PointUnlocked", dir);
				lastMuvsDirTime = Time.time;
			}
		}
	}

	private void Targeter_OnLockedGround(Vector3 pos)
	{
		if (!hasMuvs || !muvs.tgpPage.remoteOnly)
		{
			FloatingOrigin.WorldToNetPoint(pos, out var nsv, out var offset);
			SendRPC("RPC_LockWorldPoint", nsv, offset);
		}
	}

	private void Targeter_OnLockedSky(Vector3 direction)
	{
		if (!hasMuvs || !muvs.tgpPage.remoteOnly)
		{
			SendRPC("RPC_LockedSky", direction);
		}
	}

	private void Targeter_OnLockedActor(Actor obj)
	{
		if (!hasMuvs || !muvs.tgpPage.remoteOnly)
		{
			UnitSpawn parentSpawn = VTOLMPUnitManager.GetParentSpawn(obj);
			if ((bool)parentSpawn)
			{
				VTOLMPUnitManager.instance.ReportLockingUnit(parentSpawn.unitID, PlayerSpawn.TargetingMethods.TGP);
			}
			SendRPC("RPC_LockActor", VTNetUtils.GetActorIdentifier(obj));
		}
	}

	private void Targeter_OnUnlockedActor(Actor obj)
	{
		UnitSpawn parentSpawn = VTOLMPUnitManager.GetParentSpawn(obj);
		if ((bool)parentSpawn)
		{
			VTOLMPUnitManager.instance.ReportUnlockUnit(parentSpawn.unitID, PlayerSpawn.TargetingMethods.TGP);
		}
	}

	private IEnumerator UpdateRoutine()
	{
		WaitForSeconds unlockedWait = new WaitForSeconds(1f);
		if ((bool)muvs)
		{
			hasMuvs = true;
		}
		while (!wasRegistered)
		{
			yield return null;
		}
		if (base.isMine || hasMuvs)
		{
			if (!listened)
			{
				listened = true;
				targeter.OnLockedActor += Targeter_OnLockedActor;
				targeter.OnUnlockedActor += Targeter_OnUnlockedActor;
				targeter.OnLockedGround += Targeter_OnLockedGround;
				targeter.OnOverridenDirection += Targeter_OnOverridenDirection;
				targeter.OnUnlocked += Targeter_OnUnlocked;
				targeter.OnSlewedDirection += Targeter_OnSlewedDirection;
				targeter.OnSetVisibleLaser += Targeter_OnSetVisibleLaser;
				targeter.OnLockedSky += Targeter_OnLockedSky;
			}
		}
		else
		{
			StartCoroutine(RemoteUpdateRoutine());
		}
		if (hasMuvs)
		{
			if (!base.isMine)
			{
				muvs.tgpPage.remoteOnly = true;
			}
			StartCoroutine(RemoteUpdateRoutine());
		}
		if (!base.isMine && !hasMuvs)
		{
			yield break;
		}
		while (base.enabled)
		{
			while (hasMuvs && muvs.tgpPage.remoteOnly)
			{
				yield return null;
			}
			while (!targeter.powered)
			{
				yield return null;
			}
			while (!targeter.locked && targeter.powered && (!hasMuvs || !muvs.tgpPage.remoteOnly))
			{
				SendRPC("RPC_PointUnlocked", targeter.cameraTransform.forward);
				currentRemoteDir = (syncedRemoteDir = targeter.cameraTransform.forward);
				yield return unlockedWait;
			}
			while (targeter.locked && targeter.powered)
			{
				currentRemoteDir = (syncedRemoteDir = targeter.cameraTransform.forward);
				if (hasMuvs && muvs.tgpPage.remoteOnly)
				{
					break;
				}
				yield return null;
			}
			yield return null;
		}
	}

	private IEnumerator RemoteUpdateRoutine()
	{
		while (base.enabled)
		{
			while (hasMuvs && !muvs.tgpPage.remoteOnly)
			{
				remoteSlewing = false;
				yield return null;
			}
			bool isCopilot = hasMuvs && muvs.IsLocalPlayerSeated();
			while (hasMuvs && !targeter.powered)
			{
				yield return null;
			}
			while (!targeter.locked && (!hasMuvs || targeter.powered) && (!hasMuvs || muvs.tgpPage.remoteOnly))
			{
				currentRemoteDir = Vector3.Slerp(currentRemoteDir, syncedRemoteDir, (float)(isCopilot ? 15 : 5) * Time.deltaTime);
				targeter.OverrideAimToDirection(currentRemoteDir * 1000f, Vector3.up);
				yield return null;
			}
			while (targeter.locked && (!hasMuvs || targeter.powered) && (!hasMuvs || muvs.tgpPage.remoteOnly))
			{
				if (remoteSlewing)
				{
					currentRemoteDir = Vector3.Slerp(currentRemoteDir, syncedRemoteDir, (float)(isCopilot ? 15 : 5) * Time.deltaTime);
					targeter.RemoteSlewToDirection(currentRemoteDir);
				}
				else
				{
					currentRemoteDir = targeter.cameraTransform.forward;
				}
				yield return null;
			}
			yield return null;
		}
	}

	[VTRPC]
	private void RPC_Slewed(Vector3 direction)
	{
		if (hasMuvs && !muvs.tgpPage.remoteOnly)
		{
			return;
		}
		if (ignoreNextSlew)
		{
			ignoreNextSlew = false;
			return;
		}
		if (debug)
		{
			Debug.Log("TGPSync.RPC_Slewed");
		}
		remoteSlewing = true;
		syncedRemoteDir = direction;
		if ((bool)targeter.lockedActor)
		{
			targeter.AreaLockPosition(targeter.lockedActor.position);
		}
	}

	[VTRPC]
	private void RPC_PointUnlocked(Vector3 dir)
	{
		if (hasMuvs && !muvs.tgpPage.remoteOnly)
		{
			return;
		}
		if (ignoreNextSlew)
		{
			ignoreNextSlew = false;
			return;
		}
		if (debug)
		{
			Debug.Log("TGPSync.RPC_PointUnlocked");
		}
		if (targeter.locked)
		{
			targeter.Unlock();
		}
		remoteSlewing = true;
		syncedRemoteDir = dir;
	}

	[VTRPC]
	private void RPC_LockWorldPoint(int nsv, Vector3 offset)
	{
		if (!hasMuvs || muvs.tgpPage.remoteOnly)
		{
			if (debug)
			{
				Debug.Log("TGPSync.RPC_LockWorldPoint ================");
			}
			ignoreNextSlew = true;
			if ((bool)muvs && muvs.IsLocalPlayerSeated())
			{
				muvs.tgpPage.uiAudioSource.PlayOneShot(muvs.tgpPage.audio_areaLockClip);
			}
			Vector3 position = FloatingOrigin.NetToWorldPoint(offset, nsv);
			targeter.AreaLockPosition(position);
			remoteSlewing = false;
		}
	}

	[VTRPC]
	private void RPC_LockActor(int actorID)
	{
		if (hasMuvs && !muvs.tgpPage.remoteOnly)
		{
			return;
		}
		if (debug)
		{
			Debug.Log("TGPSync.RPC_LockActor");
		}
		ignoreNextSlew = true;
		remoteSlewing = false;
		Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorID);
		if ((bool)actorFromIdentifier)
		{
			targeter.ForceLockActor(actorFromIdentifier);
			if (hasMuvs && muvs.IsLocalPlayerSeated())
			{
				muvs.tgpPage.uiAudioSource.PlayOneShot(muvs.tgpPage.audio_targetLockClip);
			}
		}
		else
		{
			targeter.Unlock();
		}
	}

	[VTRPC]
	private void RPC_LockedSky(Vector3 direction)
	{
		if (!hasMuvs || muvs.tgpPage.remoteOnly)
		{
			if (debug)
			{
				Debug.Log("TGPSync.RPC_LockedSky");
			}
			ignoreNextSlew = true;
			remoteSlewing = false;
			targeter.Lock(targeter.cameraTransform.position, direction, lockActor: false);
		}
	}

	[VTRPC]
	private void RPC_Unlock()
	{
		if (!hasMuvs || muvs.tgpPage.remoteOnly)
		{
			if (debug)
			{
				Debug.Log("TGPSync.RPC_Unlock");
			}
			targeter.Unlock();
		}
	}

	[VTRPC]
	private void RPC_Laser(int status)
	{
		bool visibleLaser = status > 0;
		targeter.visibleLaser = visibleLaser;
	}
}

}