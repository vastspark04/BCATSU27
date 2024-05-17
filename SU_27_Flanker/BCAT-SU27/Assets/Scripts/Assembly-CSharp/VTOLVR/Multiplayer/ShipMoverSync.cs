using System;
using System.Collections;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class ShipMoverSync : VTNetSync
{
	public ShipMover shipMover;

	public static float minInterpThresh = 0.1f;

	public static float maxInterpThresh = 2f;

	public static float interpSpeedDiv = 15f;

	private bool interpolatingPos;

	public float rotationInterpRate = 4f;

	private bool hasInitialized;

	private FixedPoint syncedPos;

	private Vector3 syncedVel;

	private Vector3 syncedAccel;

	private Quaternion syncedRot;

	private Rigidbody rb => shipMover.rb;

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
		Initialize();
		syncedPos.point = base.transform.position;
		syncedRot = base.transform.rotation;
		if (!base.netEntity.isMine)
		{
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			shipMover.enabled = false;
		}
	}

	private void FixedUpdate()
	{
		if (!base.isMine)
		{
			SyncPhysics(Time.fixedDeltaTime, rb.position, rb.rotation, out var pos, out var rot);
			rb.MovePosition(pos);
			rb.MoveRotation(rot);
			rb.velocity = syncedVel;
		}
	}

	private float GetInterpThreshold()
	{
		return Mathf.Lerp(minInterpThresh, maxInterpThresh, rb.velocity.magnitude / interpSpeedDiv);
	}

	private void SyncPhysics(float deltaTime, Vector3 currPos, Quaternion currRot, out Vector3 pos, out Quaternion rot)
	{
		float interpThreshold = GetInterpThreshold();
		Vector3 vector = currPos + syncedVel * deltaTime + 0.5f * deltaTime * deltaTime * syncedAccel;
		syncedVel += syncedAccel * deltaTime;
		syncedVel = Vector3.ClampMagnitude(syncedVel, shipMover.maxSpeed);
		syncedPos.point += 0.5f * deltaTime * deltaTime * syncedAccel + syncedVel * deltaTime;
		if (!float.IsNaN(syncedVel.x))
		{
			rb.velocity = syncedVel;
		}
		else
		{
			Debug.LogError("syncedVel is NaN", base.gameObject);
			syncedVel = rb.velocity;
		}
		float magnitude = (syncedPos.point - vector).magnitude;
		bool flag = true;
		if ((magnitude > 3f && shipMover.speed < 4f) || magnitude > 15f)
		{
			Debug.LogFormat("Resetting sync tf! Ship Speed: {0}, dist: {1}", shipMover.speed, magnitude);
			Vector3 vector2 = (pos = (rb.position = syncedPos.point));
			_ = Color.yellow;
			flag = false;
		}
		else if (interpolatingPos)
		{
			float num = Mathf.Lerp(5f, 2f, syncedVel.sqrMagnitude / 225f);
			Vector3 vector3 = (pos = Vector3.MoveTowards(vector, syncedPos.point, Mathf.Max(magnitude * num, magnitude) * deltaTime));
			if (magnitude < interpThreshold * 0.33f)
			{
				interpolatingPos = false;
			}
			_ = Color.red;
		}
		else
		{
			pos = vector;
			if (magnitude > interpThreshold)
			{
				interpolatingPos = true;
			}
			_ = Color.green;
		}
		if (flag)
		{
			Vector3 vector4 = pos - currPos;
			vector4 = Vector3.ClampMagnitude(vector4, shipMover.maxSpeed * 1.1f * deltaTime);
			pos = currPos + vector4;
		}
		rot = Quaternion.Lerp(currRot, syncedRot, rotationInterpRate * deltaTime);
		shipMover.actor.SetCustomVelocity(syncedVel);
	}

	public void Initialize()
	{
		if (!hasInitialized)
		{
			hasInitialized = true;
			if (VTOLMPUtils.IsMultiplayer() && !base.isMine)
			{
				rb.isKinematic = true;
			}
		}
	}

	public override void UploadData(SyncDataUp d)
	{
		base.UploadData(d);
		Vector3D globalPoint = shipMover.fixedPos.globalPoint;
		DoubleToInts(globalPoint.x, out var a, out var b);
		DoubleToInts(globalPoint.y, out var a2, out var b2);
		DoubleToInts(globalPoint.z, out var a3, out var b3);
		d.AddInt(a);
		d.AddInt(b);
		d.AddInt(a2);
		d.AddInt(b2);
		d.AddInt(a3);
		d.AddInt(b3);
		d.AddVector3(shipMover.velocity);
		d.AddVector3(shipMover.currentAccel);
		d.AddQuaternion(rb.rotation);
	}

	public override void DownloadData(ISyncDataDown d)
	{
		base.DownloadData(d);
		int nextInt = d.GetNextInt();
		int nextInt2 = d.GetNextInt();
		int nextInt3 = d.GetNextInt();
		int nextInt4 = d.GetNextInt();
		int nextInt5 = d.GetNextInt();
		int nextInt6 = d.GetNextInt();
		Vector3D globalPoint = new Vector3D(IntsToDouble(nextInt, nextInt2), IntsToDouble(nextInt3, nextInt4), IntsToDouble(nextInt5, nextInt6));
		syncedPos.globalPoint = globalPoint;
		Vector3 nextVector = d.GetNextVector3();
		Vector3 nextVector2 = d.GetNextVector3();
		Quaternion nextQuaternion = d.GetNextQuaternion();
		if (float.IsNaN(nextVector.x))
		{
			Debug.LogError("Received a sync message with NaN values (syncedVel)");
		}
		else
		{
			syncedVel = nextVector;
		}
		if (float.IsNaN(nextVector2.x))
		{
			Debug.LogError("Received a sync message with NaN values (syncedAccel)");
		}
		else
		{
			syncedAccel = nextVector2;
		}
		if (float.IsNaN(nextQuaternion.x))
		{
			Debug.LogError("Received a sync message with NaN values (syncedRot)");
		}
		else
		{
			syncedRot = nextQuaternion;
		}
		if (float.IsNaN(VTNetworkManager.networkTime))
		{
			Debug.LogError("networkTime is NaN");
		}
		if (float.IsNaN(d.Timestamp))
		{
			Debug.LogError("d.Timestamp is NaN");
		}
		float num = Mathf.Max(0f, VTNetworkManager.networkTime - d.Timestamp);
		syncedPos.point += 0.5f * syncedAccel * num * num + syncedVel * num;
		syncedVel += syncedAccel * num;
	}

	public static void DoubleToInts(double d, out int a, out int b)
	{
		a = (int)Math.Floor(d);
		b = (int)Math.Floor((d - (double)a) * 100000000.0);
	}

	public static double IntsToDouble(int a, int b)
	{
		return (double)a + (double)b / 100000000.0;
	}
}

}