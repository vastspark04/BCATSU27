using Steamworks;
using UnityEngine;
using VTNetworking;

public class HandControlledTurret : VTNetSyncRPCOnly
{
	public Gun gun;

	public Transform leftHandTransform;

	public Transform rightHandTransform;

	public Transform referenceFulcrum;

	public ModuleTurret turret;

	public Vector2 handOffset;

	[Header("Interaction")]
	public VRInteractable leftInteractable;

	public VRInteractable rightInteractable;

	private ulong occupiedID;

	private Vector3 remoteDirection;

	private ulong localID;

	private bool sentOccupyRequest;

	private float timeSentDir;

	public static HandControlledTurret localNonVRTurret { get; private set; }

	private bool remoteControlled
	{
		get
		{
			if (occupiedID != 0L)
			{
				return occupiedID != localID;
			}
			return false;
		}
	}

	private bool isControlling => occupiedID == localID;

	[VTRPC]
	private void SetRemoteFire()
	{
		if (base.isMine)
		{
			gun.SetFire(fire: true);
		}
	}

	[VTRPC]
	private void StopRemoteFire()
	{
		if (base.isMine)
		{
			gun.SetFire(fire: false);
		}
	}

	[VTRPC]
	private void SetIsOccupied(ulong id)
	{
		occupiedID = id;
		if (id == 0L)
		{
			gun.SetFire(fire: false);
		}
		Debug.Log("SetIsOccupied(" + new Friend(id).Name + ")");
		if (base.isMine)
		{
			SendRPCBuffered("SetIsOccupied", id);
		}
	}

	[VTRPC]
	private void A(Vector3 dir)
	{
		remoteDirection = dir;
	}

	protected override void Awake()
	{
		base.Awake();
		localID = SteamClient.SteamId.Value;
	}

	private void Update()
	{
		bool flag = false;
		if (occupiedID == 0L || occupiedID == localID)
		{
			if ((bool)leftInteractable.activeController)
			{
				leftHandTransform = leftInteractable.activeController.transform;
				if (isControlling && leftInteractable.activeController.triggerAxis > 0.8f)
				{
					flag = true;
				}
			}
			else
			{
				leftHandTransform = null;
			}
			if ((bool)rightInteractable.activeController)
			{
				rightHandTransform = rightInteractable.activeController.transform;
				if (isControlling && rightInteractable.activeController.triggerAxis > 0.8f)
				{
					flag = true;
				}
			}
			else
			{
				rightHandTransform = null;
			}
			if (((bool)leftHandTransform || (bool)rightHandTransform) && occupiedID == 0L)
			{
				if (base.isMine)
				{
					SetIsOccupied(SteamClient.SteamId.Value);
				}
				else if (!sentOccupyRequest)
				{
					SendRPCBuffered("SetIsOccupied", SteamClient.SteamId.Value);
					Debug.Log("Sending SetIsOccupied rpc from client");
					sentOccupyRequest = true;
				}
			}
			if ((occupiedID == localID || sentOccupyRequest) && !leftHandTransform && !rightHandTransform)
			{
				if (base.isMine)
				{
					SetIsOccupied(0uL);
				}
				else if (sentOccupyRequest)
				{
					SendRPCBuffered("SetIsOccupied", 0);
					sentOccupyRequest = false;
				}
			}
			if (gun.isFiring != flag)
			{
				if (base.isMine)
				{
					gun.SetFire(flag);
				}
				else if (isControlling)
				{
					if (flag)
					{
						SendRPC("SetRemoteFire");
					}
					else
					{
						SendRPC("StopRemoteFire");
					}
				}
			}
		}
		if (remoteControlled)
		{
			turret.AimToTarget(referenceFulcrum.position + remoteDirection * 1000f);
		}
		else if (isControlling && occupiedID == SteamClient.SteamId.Value && ((bool)leftHandTransform || (bool)rightHandTransform))
		{
			Vector3 vector = (((bool)leftHandTransform && !rightHandTransform) ? (leftHandTransform.position + handOffset.x * referenceFulcrum.right + handOffset.y * referenceFulcrum.up) : ((!rightHandTransform || (bool)leftHandTransform) ? (Vector3.Lerp(leftHandTransform.position, rightHandTransform.position, 0.5f) + handOffset.y * referenceFulcrum.up) : (rightHandTransform.position + (0f - handOffset.x) * referenceFulcrum.right + handOffset.y * referenceFulcrum.up)));
			Vector3 normalized = (referenceFulcrum.position - vector).normalized;
			turret.AimToTarget(referenceFulcrum.position + normalized * 1000f);
			if (Time.time - timeSentDir > VTNetworkManager.CurrentSendInterval * 2f)
			{
				timeSentDir = Time.time;
				SendRPC("A", normalized);
			}
		}
	}
}
