using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class AIPilotSync : VTNetSyncRPCOnly
{
	public Rigidbody rb;

	public FlightInfo fInfo;

	public AIPilot aiPilot;

	public FlightControlComponent[] syncedControlOutputs;

	private Vector3 myPyr;

	private float myBrakes;

	private float myThrottle;

	private float myFlaps;

	private bool died;

	public AnimationCurve sendIntervalCurve;

	private MPPlayerDistanceChecker distChecker;

	private Vector3 syncedPyr;

	private float syncedBrakes;

	private float syncedThrottle;

	private float syncedFlaps;

	private float remoteInputLerpRate = 10f;

	private FixedPoint moveTarget;

	private float currCorrectionDist = 50f;

	private float targetCorrectionDist = 50f;

	private FixedPoint syncedPos;

	private Quaternion syncedRot;

	private Quaternion lastRot;

	private Vector3 syncedVel;

	private FixedPoint pPos;

	private Vector3 syncedAccel;

	private Quaternion smoothRot;

	private Vector3 syncedAngVel;

	private float lastTimestamp;

	public static float minInterpThresh = 0.1f;

	public static float maxInterpThresh = 9f;

	public static float interpSpeedDiv = 200f;

	private bool interpolatingPos;

	[Header("Vehicle Parts")]
	public VehiclePart[] vehicleParts;

	public void SetPitchYawRoll(Vector3 pyr)
	{
		if (base.isMine)
		{
			myPyr = pyr;
		}
	}

	public void SetBrakes(float brakes)
	{
		if (base.isMine)
		{
			myBrakes = brakes;
		}
	}

	public void SetThrottle(float t)
	{
		if (base.isMine)
		{
			myThrottle = t;
		}
	}

	public void SetFlaps(float f)
	{
		if (base.isMine)
		{
			myFlaps = f;
		}
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			aiPilot.OnCollisionDeath += OnCollisionDeath;
			aiPilot.OnExploded += AiPilot_OnExploded;
			aiPilot.GetComponent<Health>().OnDeath.AddListener(OnCollisionDeath);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			aiPilot.enabled = false;
			fInfo.PauseGCalculations();
			aiPilot.kPlane.enabled = false;
			rb.isKinematic = true;
		}
		SetupVehiclePartEvents();
	}

	private void AiPilot_OnExploded()
	{
		SendRPC("RPC_Explode");
	}

	[VTRPC]
	private void RPC_Explode()
	{
		died = true;
		ExplosionManager.instance.CreateExplosionEffect(ExplosionManager.ExplosionTypes.Aerial, rb.position, rb.velocity);
		base.gameObject.SetActive(value: false);
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
		if (died)
		{
			SendDirectedRPC(obj, "RPC_Death", VTMapManager.WorldToGlobalPoint(base.transform.position).toVector3, base.transform.rotation);
		}
	}

	private void OnCollisionDeath()
	{
		died = true;
		SendRPC("RPC_Death", VTMapManager.WorldToGlobalPoint(base.transform.position).toVector3, base.transform.rotation);
	}

	[VTRPC]
	private void RPC_Death(Vector3 globalPos, Quaternion rot)
	{
		died = true;
		Vector3 vector3 = (rb.position = (base.transform.position = VTMapManager.GlobalToWorldPoint(new Vector3D(globalPos))));
		Quaternion quaternion3 = (rb.rotation = (base.transform.rotation = rot));
		rb.isKinematic = false;
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
		if (died)
		{
			base.gameObject.SetActive(value: false);
		}
		distChecker = GetComponent<MPPlayerDistanceChecker>();
		if (!distChecker)
		{
			distChecker = base.gameObject.AddComponent<MPPlayerDistanceChecker>();
		}
		if (base.isMine)
		{
			yield return new WaitForSeconds(Random.Range(0f, 0.5f));
			while (base.enabled && aiPilot.actor.alive)
			{
				FloatingOrigin.WorldToNetPoint(rb.position, out var nsv, out var offset);
				Vector3 vector = (rb.isKinematic ? aiPilot.kPlane.velocity : rb.velocity);
				SendRPC("RPC_Send", offset, nsv, vector, rb.rotation, fInfo.acceleration, VTNetworkManager.GetNetworkTimestamp());
				SendRPCInputs();
				float wait = sendIntervalCurve.Evaluate(distChecker.closestDistance);
				float t = Time.time;
				while (Time.time - t < wait)
				{
					yield return null;
				}
			}
			yield break;
		}
		if (died)
		{
			rb.isKinematic = false;
			yield break;
		}
		aiPilot.enabled = false;
		aiPilot.StopAllCoroutines();
		aiPilot.autoPilot.enabled = false;
		fInfo.PauseGCalculations();
		aiPilot.kPlane.enabled = false;
		rb.isKinematic = true;
		WheelsController component = GetComponent<WheelsController>();
		component.remoteAutoSteer = true;
		RaySpringDamper[] suspensions = component.suspensions;
		for (int i = 0; i < suspensions.Length; i++)
		{
			suspensions[i].raycastWhileKinematic = true;
		}
		syncedPos.point = rb.position;
		smoothRot = rb.rotation;
		if ((bool)aiPilot.detectionRadar)
		{
			aiPilot.detectionRadar.SetToMPRemote();
		}
		if ((bool)aiPilot.lockingRadar)
		{
			aiPilot.lockingRadar.enabled = false;
		}
		WaitForFixedUpdate fixedWait = new WaitForFixedUpdate();
		StartCoroutine(RemoteInputRoutine());
		while (base.enabled)
		{
			if (died)
			{
				rb.isKinematic = false;
				break;
			}
			aiPilot.enabled = false;
			aiPilot.autoPilot.enabled = false;
			aiPilot.kPlane.enabled = false;
			rb.isKinematic = true;
			SyncPhysics(Time.fixedDeltaTime, rb.position, rb.rotation, out var pos, out var rot);
			rb.MovePosition(pos);
			rb.MoveRotation(rot);
			rb.velocity = syncedVel;
			aiPilot.actor.SetCustomVelocity(rb.velocity);
			yield return fixedWait;
		}
	}

	private IEnumerator RemoteInputRoutine()
	{
		while (base.enabled)
		{
			myPyr = Vector3.Lerp(myPyr, syncedPyr, remoteInputLerpRate * Time.deltaTime);
			myBrakes = Mathf.Lerp(myBrakes, syncedBrakes, remoteInputLerpRate * Time.deltaTime);
			myThrottle = Mathf.Lerp(myThrottle, syncedThrottle, remoteInputLerpRate * Time.deltaTime);
			myFlaps = syncedFlaps;
			for (int i = 0; i < syncedControlOutputs.Length; i++)
			{
				syncedControlOutputs[i].SetPitchYawRoll(myPyr);
				syncedControlOutputs[i].SetBrakes(myBrakes);
				syncedControlOutputs[i].SetThrottle(myThrottle);
				syncedControlOutputs[i].SetFlaps(myFlaps);
			}
			yield return null;
		}
	}

	[VTRPC]
	private void RPC_Send(Vector3 posOffset, int nsv, Vector3 vel, Quaternion rot, Vector3 accel, float timestamp)
	{
		float num = VTNetworkManager.GetNetworkTimestamp() - timestamp;
		syncedPos.point = FloatingOrigin.NetToWorldPoint(posOffset, nsv);
		syncedVel = vel + accel * num;
		syncedRot = rot;
		syncedAccel = accel;
		syncedPos.point += syncedVel * num + num * num * 0.5f * accel;
		float deltaTime = timestamp - lastTimestamp;
		lastTimestamp = timestamp;
		syncedAngVel = GetAngularVel(lastRot, syncedRot, deltaTime);
		lastRot = syncedRot;
	}

	private void SendRPCInputs()
	{
		SendRPC("RPC_Inputs", myPyr, myBrakes, myThrottle, myFlaps);
	}

	[VTRPC]
	private void RPC_Inputs(Vector3 pyr, float brakes, float throttle, float flaps)
	{
		syncedPyr = pyr;
		syncedBrakes = brakes;
		syncedThrottle = throttle;
		syncedFlaps = flaps;
	}

	private void SyncPhysics(float deltaTime, Vector3 currPos, Quaternion currRot, out Vector3 pos, out Quaternion rot)
	{
		float interpThreshold = GetInterpThreshold();
		Vector3 vector = currPos + syncedVel * deltaTime + 0.5f * deltaTime * deltaTime * syncedAccel;
		syncedVel += syncedAccel * deltaTime;
		syncedPos.point += 0.5f * deltaTime * deltaTime * syncedAccel + syncedVel * deltaTime;
		fInfo.OverrideRecordedAcceleration(syncedAccel);
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
		float num = Mathf.Lerp(8f, 3f, syncedVel.sqrMagnitude / 6400f);
		Vector3 vector2 = Vector3.MoveTowards(vector, syncedPos.point, Mathf.Max(magnitude * num, magnitude * 3f) * deltaTime);
		if ((magnitude > 1f && syncedVel.sqrMagnitude < 16f) || magnitude > currCorrectionDist)
		{
			Debug.Log($"{base.gameObject.name} Resetting sync tf! dist: {magnitude}", base.gameObject);
			Vector3 vector3 = (pos = (rb.position = syncedPos.point));
			pPos.point = pos;
		}
		else if (interpolatingPos)
		{
			pos = vector2;
			pPos.point = pos;
			if (magnitude < interpThreshold * 0.33f)
			{
				interpolatingPos = false;
			}
		}
		else
		{
			pos = vector;
			pPos.point = pos;
			if (magnitude > interpThreshold)
			{
				interpolatingPos = true;
			}
		}
		syncedRot = Quaternion.Euler(syncedAngVel * deltaTime) * syncedRot;
		smoothRot = Quaternion.RotateTowards(smoothRot, syncedRot, aiPilot.kPlane.rollRateCurve.Evaluate(fInfo.airspeed) * 2f * deltaTime);
		rot = Quaternion.Lerp(currRot, smoothRot, 6f * deltaTime);
	}

	private float GetInterpThreshold()
	{
		return Mathf.Lerp(minInterpThresh, maxInterpThresh, fInfo.airspeed / interpSpeedDiv);
	}

	private void SetupVehiclePartEvents()
	{
		for (int i = 0; i < vehicleParts.Length; i++)
		{
			int partIdx = i;
			if (base.isMine)
			{
				vehicleParts[i].OnPartDetach.AddListener(delegate
				{
					SendDetachRPC(partIdx);
				});
				vehicleParts[i].health.OnDeath.AddListener(delegate
				{
					SendKillPartRPC(partIdx);
				});
				vehicleParts[i].OnRepair.AddListener(delegate
				{
					SendPartRepairRPC(partIdx);
				});
			}
			else
			{
				vehicleParts[i].detachOnDeath = false;
			}
		}
	}

	private void SendPartRepairRPC(int partIdx)
	{
		SendRPC("RPC_PartRepair", partIdx);
	}

	[VTRPC]
	private void RPC_PartRepair(int idx)
	{
		vehicleParts[idx].Repair();
	}

	public void SendKillPartRPC(int partIdx)
	{
		SendRPC("RPC_PartKill", partIdx);
	}

	[VTRPC]
	private void RPC_PartKill(int idx)
	{
		vehicleParts[idx].RemoteKill(null);
	}

	public void SendDetachRPC(int partIdx)
	{
		SendRPC("RPC_PartDetach", partIdx);
	}

	[VTRPC]
	private void RPC_PartDetach(int idx)
	{
		vehicleParts[idx].RemoteDetachPart();
	}

	private void Refresh()
	{
		if (!base.isMine)
		{
			return;
		}
		for (int i = 0; i < vehicleParts.Length; i++)
		{
			if (vehicleParts[i].health.normalizedHealth == 0f)
			{
				SendKillPartRPC(i);
			}
			if (vehicleParts[i].hasDetached)
			{
				SendDetachRPC(i);
			}
		}
	}

	private Vector3 GetAngularVel(Quaternion a, Quaternion b, float deltaTime)
	{
		Quaternion quaternion = Quaternion.Inverse(a) * b;
		return new Vector3(Mathf.DeltaAngle(0f, quaternion.eulerAngles.x), Mathf.DeltaAngle(0f, quaternion.eulerAngles.y), Mathf.DeltaAngle(0f, quaternion.eulerAngles.z)) / deltaTime;
	}
}

}