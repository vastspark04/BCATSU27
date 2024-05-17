using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class MissileSync : VTNetSyncRPCOnly, IParentRBDependent
{
	public Missile m;

	public static float minInterpThresh = 0.1f;

	public static float maxInterpThresh = 9f;

	public static float interpSpeedDiv = 200f;

	private bool interpolatingPos;

	private Coroutine fixedFiredRoutine;

	private Coroutine remoteUpdateVelRoutine;

	private FixedPoint pPos;

	private Vector3 acceleration;

	private bool startedAccelRecord;

	private Vector3 a_lastVel;

	private Vector3 syncedPos;

	private Vector3 syncedVel;

	private Vector3 syncedAccel;

	private Quaternion syncedRot;

	public Rigidbody rb { get; set; }

	[ContextMenu("Get Missile")]
	private void GetMissile()
	{
		m = GetComponent<Missile>();
	}

	protected override void Awake()
	{
		base.Awake();
		if (!VTOLMPUtils.IsMultiplayer())
		{
			base.enabled = false;
		}
		else
		{
			m.OnFired += M_OnFired;
		}
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			m.OnDetonate.AddListener(OnDetonate);
			m.isLocal = true;
			Refresh(0uL);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			m.isLocal = false;
			FloatingOrigin.instance.OnOriginShift += Instance_OnOriginShift;
		}
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		Refresh(obj);
	}

	private void Refresh(ulong target = 0uL)
	{
		if (base.isMine && m.fired)
		{
			SendDirectedRPC(target, "RPC_FireMissileParent", VTNetUtils.GetActorIdentifier(m.launchedByActor));
		}
	}

	private void OnDestroy()
	{
		if ((bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.OnOriginShift -= Instance_OnOriginShift;
		}
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void OnDetonate()
	{
		if (base.isMine)
		{
			SendRPC("RPC_Detonate", VTMapManager.WorldToGlobalPoint(base.transform.position).toVector3);
			VTNetworkManager.NetDestroyDelayed(base.gameObject, 5f);
		}
	}

	[VTRPC]
	private void RPC_Detonate(Vector3 globalPoint)
	{
		base.transform.position = VTMapManager.GlobalToWorldPoint(new Vector3D(globalPoint));
		m.Detonate();
	}

	public void BeginFlightSend()
	{
		StartCoroutine(FlightSendRoutine());
	}

	private IEnumerator FlightSendRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(VTNetworkManager.CurrentSendInterval);
		while (base.enabled)
		{
			SendFlightData();
			yield return wait;
		}
	}

	[VTRPC]
	private void RPC_FireMissile()
	{
		Client_FireMissile();
	}

	public void Client_FireMissile()
	{
		if (!m.fired)
		{
			Actor componentInParent = GetComponentInParent<Actor>();
			if (!componentInParent)
			{
				Debug.LogError("RPC_FireMissile aborted due to null parentActor.");
				return;
			}
			Actor actor = m.gameObject.AddComponent<Actor>();
			actor.role = Actor.Roles.Missile;
			actor.SetMissile(m);
			actor.fixedVelocityUpdate = false;
			actor.team = componentInParent.team;
			actor.iconType = UnitIconManager.MapIconTypes.Missile;
			actor.drawIcon = false;
			actor.actorName = m.gameObject.name;
			m.launchedByActor = componentInParent;
			m.Fire();
			rb = m.GetComponent<Rigidbody>();
		}
	}

	[VTRPC]
	private void RPC_FireMissileParent(int parentActorId)
	{
		if (!m.fired)
		{
			Actor actor = m.gameObject.AddComponent<Actor>();
			actor.role = Actor.Roles.Missile;
			actor.SetMissile(m);
			actor.fixedVelocityUpdate = false;
			Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(parentActorId);
			actor.actorName = m.gameObject.name;
			if ((bool)actorFromIdentifier)
			{
				actor.team = actorFromIdentifier.team;
			}
			actor.iconType = UnitIconManager.MapIconTypes.Missile;
			actor.drawIcon = false;
			m.launchedByActor = actorFromIdentifier;
			m.Fire();
			rb = m.GetComponent<Rigidbody>();
		}
	}

	private void Instance_OnOriginShift(Vector3 offset)
	{
		if (!base.isMine && m.fired)
		{
			syncedPos += offset;
			rb.velocity = syncedVel;
		}
	}

	private float GetInterpThreshold()
	{
		return Mathf.Lerp(minInterpThresh, maxInterpThresh, rb.velocity.magnitude / interpSpeedDiv);
	}

	private void OnEnable()
	{
		if ((bool)m && m.fired)
		{
			M_OnFired();
		}
	}

	private void M_OnFired()
	{
		if (fixedFiredRoutine != null)
		{
			StopCoroutine(fixedFiredRoutine);
		}
		if (remoteUpdateVelRoutine != null)
		{
			StopCoroutine(remoteUpdateVelRoutine);
		}
		fixedFiredRoutine = StartCoroutine(FiredRoutine());
		if (!base.isMine)
		{
			remoteUpdateVelRoutine = StartCoroutine(RemoteUpdateVelRoutine());
		}
	}

	private IEnumerator FiredRoutine()
	{
		WaitForFixedUpdate fixedWait = new WaitForFixedUpdate();
		while ((bool)m && m.fired)
		{
			if (!base.isMine)
			{
				SyncPhysics(Time.fixedDeltaTime, rb.position, rb.rotation, out var pos, out var rot);
				rb.MovePosition(pos);
				rb.MoveRotation(rot);
				rb.velocity = syncedVel;
			}
			else if (!startedAccelRecord)
			{
				a_lastVel = rb.velocity;
			}
			else
			{
				acceleration = (rb.velocity - a_lastVel) / Time.fixedDeltaTime;
				a_lastVel = rb.velocity;
			}
			yield return fixedWait;
		}
	}

	private IEnumerator RemoteUpdateVelRoutine()
	{
		while (base.enabled)
		{
			rb.velocity = syncedVel;
			yield return null;
		}
	}

	private void SyncPhysics(float deltaTime, Vector3 currPos, Quaternion currRot, out Vector3 pos, out Quaternion rot)
	{
		float interpThreshold = GetInterpThreshold();
		Vector3 vector = currPos + syncedVel * deltaTime + 0.5f * deltaTime * deltaTime * syncedAccel;
		syncedVel += syncedAccel * deltaTime;
		syncedPos += 0.5f * deltaTime * deltaTime * syncedAccel + syncedVel * deltaTime;
		rb.velocity = syncedVel;
		float magnitude = (syncedPos - vector).magnitude;
		float num = Mathf.Lerp(8f, 3f, syncedVel.sqrMagnitude / 6400f);
		Vector3 vector2 = Vector3.MoveTowards(vector, syncedPos, Mathf.Max(magnitude * num, magnitude * 3f) * deltaTime);
		if (magnitude > 50f)
		{
			Vector3 vector4 = (pos = (rb.position = syncedPos));
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
		rot = Quaternion.Lerp(currRot, syncedRot, 10f * deltaTime);
	}

	private void SendFlightData()
	{
		FloatingOrigin.WorldToNetPoint(base.transform.position, out var nsv, out var offset);
		SendRPC("F", nsv, offset, rb.velocity, acceleration, rb.rotation, VTNetworkManager.GetNetworkTimestamp());
	}

	[VTRPC]
	private void F(int nsv, Vector3 offset, Vector3 velocity, Vector3 accel, Quaternion rotation, float timestamp)
	{
		syncedPos = FloatingOrigin.NetToWorldPoint(offset, nsv);
		syncedVel = velocity;
		syncedAccel = accel;
		syncedRot = rotation;
		float num = Mathf.Max(0f, VTNetworkManager.networkTime - timestamp);
		syncedVel += syncedAccel * num;
		syncedPos += 0.5f * syncedAccel * num * num + syncedVel * num;
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		if ((bool)m && m.fired)
		{
			this.rb = rb;
		}
	}
}

}