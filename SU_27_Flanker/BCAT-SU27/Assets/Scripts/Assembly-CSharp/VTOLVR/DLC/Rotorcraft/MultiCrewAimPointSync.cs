using System.Collections;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class MultiCrewAimPointSync : VTNetSyncRPCOnly
{
	public WeaponManager wm;

	public MultiUserVehicleSync muvs;

	public Transform referenceTransform;

	public float remoteLerpRate = 15f;

	private Vector3 lerpedDir = Vector3.forward;

	private Vector3 syncedDir;

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
		StartCoroutine(TwoWayRoutine());
	}

	private IEnumerator TwoWayRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(VTNetworkManager.CurrentSendInterval);
		while (base.enabled)
		{
			if (muvs.IsLocalPlayerSeated() && (bool)wm.currentEquip)
			{
				if (muvs.IsLocalWeaponController())
				{
					wm.ui.hudInfo.SetLocalAim();
					Vector3 aimPoint = wm.currentEquip.GetAimPoint();
					Vector3 normalized = referenceTransform.InverseTransformPoint(aimPoint).normalized;
					muvs.SendRPCToCopilots(this, "RPC_Dir", normalized);
					lerpedDir = normalized;
					yield return wait;
				}
				else
				{
					lerpedDir = Vector3.Slerp(lerpedDir, syncedDir, remoteLerpRate * Time.deltaTime);
					Vector3 remoteAimPoint = referenceTransform.TransformPoint(lerpedDir * 1000f);
					wm.ui.hudInfo.SetRemoteAimPoint(remoteAimPoint);
					yield return null;
				}
			}
			else
			{
				lerpedDir = Vector3.forward;
				yield return null;
			}
		}
	}

	private IEnumerator RemoteRoutine()
	{
		while (base.enabled)
		{
			if (muvs.IsLocalPlayerSeated() && (bool)wm.currentEquip)
			{
				lerpedDir = Vector3.Slerp(lerpedDir, syncedDir, remoteLerpRate * Time.deltaTime);
				Vector3 remoteAimPoint = referenceTransform.TransformPoint(lerpedDir * 1000f);
				wm.ui.hudInfo.SetRemoteAimPoint(remoteAimPoint);
			}
			else
			{
				lerpedDir = Vector3.forward;
			}
			yield return null;
		}
	}

	private IEnumerator OwnersRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(VTNetworkManager.CurrentSendInterval);
		while (base.enabled)
		{
			if ((bool)wm.currentEquip)
			{
				Vector3 aimPoint = wm.currentEquip.GetAimPoint();
				Vector3 normalized = referenceTransform.InverseTransformPoint(aimPoint).normalized;
				muvs.SendRPCToCopilots(this, "RPC_Dir", normalized);
				yield return wait;
			}
			else
			{
				yield return null;
			}
		}
	}

	[VTRPC]
	private void RPC_Dir(Vector3 localDir)
	{
		syncedDir = localDir;
	}
}

}