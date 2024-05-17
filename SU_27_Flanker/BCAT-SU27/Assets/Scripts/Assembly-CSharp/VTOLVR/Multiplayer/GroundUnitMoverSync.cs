using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class GroundUnitMoverSync : VTNetSyncRPCOnly
{
	public GroundUnitMover mover;

	private Transform rotationTf;

	private bool syncPaused;

	private float forceUpdateInterval = 0.5f;

	private Vector3 localAccel;

	private FixedPoint moveTarget;

	private float currCorrectionDist = 8f;

	private float targetCorrectionDist = 8f;

	private FixedPoint syncedPos;

	private Quaternion syncedRot;

	private Vector3 syncedVel;

	private Vector3 syncedAccel;

	public static float minInterpThresh = 0.1f;

	public static float maxInterpThresh = 2f;

	public static float interpSpeedDiv = 30f;

	private bool interpolatingPos;

	public void PauseSync()
	{
		syncPaused = true;
	}

	public void UnpauseSync()
	{
		syncedPos.point = mover.transform.position;
		if ((bool)rotationTf)
		{
			syncedRot = rotationTf.rotation;
		}
		syncedVel = Vector3.zero;
		syncedAccel = Vector3.zero;
		syncPaused = false;
	}

	private IEnumerator AccelRecord()
	{
		Vector3 lastVel = Vector3.zero;
		WaitForFixedUpdate fixedWait = new WaitForFixedUpdate();
		while (base.enabled)
		{
			if (Time.fixedDeltaTime > 0f)
			{
				localAccel = (mover.velocity - lastVel) / Time.fixedDeltaTime;
				lastVel = mover.velocity;
			}
			yield return fixedWait;
		}
	}

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
		if (mover.actor.alive)
		{
			SendDirectedRPC(obj, "RPC_Speed", mover.moveSpeed);
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
		if (mover is VehicleMover)
		{
			rotationTf = ((VehicleMover)mover).rotationTransform;
		}
		if (base.isMine)
		{
			yield return new WaitForSeconds(Random.Range(0f, forceUpdateInterval));
			if (mover is VehicleMover)
			{
				_ = (VehicleMover)mover;
			}
			float lastMoveSpeed = mover.moveSpeed;
			SendRPC("RPC_Speed", mover.moveSpeed);
			WaitForSeconds wait = new WaitForSeconds(forceUpdateInterval);
			StartCoroutine(AccelRecord());
			while (base.enabled)
			{
				if (mover.moveSpeed != lastMoveSpeed)
				{
					lastMoveSpeed = mover.moveSpeed;
					SendRPC("RPC_Speed", mover.moveSpeed);
				}
				FloatingOrigin.WorldToNetPoint(mover.transform.position, out var nsv, out var offset);
				if ((bool)rotationTf)
				{
					SendRPC("RPC_Send", offset, nsv, mover.velocity, rotationTf.rotation, localAccel, VTNetworkManager.GetNetworkTimestamp());
				}
				else
				{
					SendRPC("RPC_SendNoRot", offset, nsv, mover.velocity, localAccel, VTNetworkManager.GetNetworkTimestamp());
				}
				yield return wait;
			}
			yield break;
		}
		syncedPos.point = mover.transform.position;
		while (base.enabled)
		{
			if (!syncPaused)
			{
				mover.enabled = false;
				Quaternion currRot = (rotationTf ? rotationTf.rotation : mover.transform.rotation);
				SyncPhysics(Time.deltaTime, mover.transform.position, currRot, out var pos, out var rot);
				mover.transform.position = pos;
				if ((bool)rotationTf)
				{
					rotationTf.rotation = rot;
				}
				mover.SetVelocity(syncedVel);
			}
			yield return null;
		}
	}

	[VTRPC]
	private void RPC_Speed(float speed)
	{
		mover.moveSpeed = speed;
	}

	[VTRPC]
	private void RPC_Send(Vector3 posOffset, int nsv, Vector3 vel, Quaternion rot, Vector3 accel, float timestamp)
	{
		syncedPos.point = FloatingOrigin.NetToWorldPoint(posOffset, nsv);
		syncedVel = vel;
		syncedRot = rot;
		syncedAccel = accel;
		float num = Mathf.Max(0f, VTNetworkManager.networkTime - timestamp);
		syncedPos.point += syncedVel * num + 0.5f * accel * num * num;
		syncedVel += accel * num;
	}

	[VTRPC]
	private void RPC_SendNoRot(Vector3 posOffset, int nsv, Vector3 vel, Vector3 accel, float timestamp)
	{
		syncedPos.point = FloatingOrigin.NetToWorldPoint(posOffset, nsv);
		syncedVel = vel;
		syncedAccel = accel;
		float num = Mathf.Max(0f, VTNetworkManager.networkTime - timestamp);
		syncedPos.point += syncedVel * num + 0.5f * accel * num * num;
		syncedVel += accel * num;
	}

	private void SyncPhysics(float deltaTime, Vector3 currPos, Quaternion currRot, out Vector3 pos, out Quaternion rot)
	{
		float interpThreshold = GetInterpThreshold();
		Vector3 vector = currPos + syncedVel * deltaTime + 0.5f * deltaTime * deltaTime * syncedAccel;
		syncedVel += syncedAccel * deltaTime;
		syncedVel = Vector3.ClampMagnitude(syncedVel, mover.moveSpeed * 1.25f);
		syncedPos.point += 0.5f * deltaTime * deltaTime * syncedAccel + syncedVel * deltaTime;
		if (!float.IsNaN(syncedVel.x))
		{
			mover.SetVelocity(syncedVel);
		}
		else
		{
			Debug.LogError("syncedVel is NaN", base.gameObject);
			syncedVel = mover.velocity;
		}
		float magnitude = (syncedPos.point - vector).magnitude;
		float num = Mathf.Lerp(8f, 3f, syncedVel.sqrMagnitude / 6400f);
		Vector3 vector2 = Vector3.MoveTowards(vector, syncedPos.point, Mathf.Max(magnitude * num, magnitude * 3f) * deltaTime);
		if ((magnitude > 1f && syncedVel.sqrMagnitude < 16f) || magnitude > currCorrectionDist)
		{
			Vector3 vector3 = (pos = (mover.transform.position = syncedPos.point));
		}
		else if (interpolatingPos)
		{
			pos = vector2;
			if (magnitude < interpThreshold * 0.33f)
			{
				interpolatingPos = false;
			}
		}
		else
		{
			pos = vector;
			if (magnitude > interpThreshold)
			{
				interpolatingPos = true;
			}
		}
		rot = Quaternion.Lerp(currRot, syncedRot, 10f * deltaTime);
		mover.actor.SetCustomVelocity(syncedVel);
	}

	private float GetInterpThreshold()
	{
		return Mathf.Lerp(minInterpThresh, maxInterpThresh, mover.velocity.magnitude / interpSpeedDiv);
	}
}

}