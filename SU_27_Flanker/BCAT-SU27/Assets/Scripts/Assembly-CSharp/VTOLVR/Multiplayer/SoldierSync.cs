using System.Collections;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class SoldierSync : VTNetSyncRPCOnly
{
	public Soldier soldier;

	public GroundUnitMoverSync moverSync;

	private Actor target;

	private bool isManpadAiming;

	private FixedPoint manpadAimPos;

	private PassengerBay loadedInBay;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			if (soldier.soldierType == Soldier.SoldierTypes.Standard)
			{
				soldier.OnAimingAtTarget += Soldier_OnAimingAtTarget;
			}
			else
			{
				soldier.OnManpadAiming += Soldier_OnManpadAiming;
				soldier.OnManpadStopAiming += Soldier_OnManpadStopAiming;
			}
			soldier.enabled = true;
			soldier.OnPassengerBayDiedWhileInvincible += Soldier_OnPassengerBayDiedWhileInvincible;
		}
		else
		{
			soldier.SetToRemote();
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
		if (!base.isMine)
		{
			if (soldier.soldierType == Soldier.SoldierTypes.Standard)
			{
				StartCoroutine(StdRemoteUpdateRoutine());
			}
			else if (soldier.soldierType == Soldier.SoldierTypes.IRMANPAD)
			{
				StartCoroutine(ManpadRemoteUpdateRoutine());
			}
		}
	}

	private void Soldier_OnAimingAtTarget(Actor tgt)
	{
		SendRPC("RPC_StdTarget", VTNetUtils.GetActorIdentifier(tgt));
	}

	[VTRPC]
	private void RPC_StdTarget(int actorId)
	{
		target = VTNetUtils.GetActorFromIdentifier(actorId);
	}

	private IEnumerator StdRemoteUpdateRoutine()
	{
		while (base.enabled && soldier.GetIsAlive())
		{
			if ((bool)target)
			{
				soldier.AimAtTarget(target.position);
				soldier.isAiming = true;
			}
			else
			{
				soldier.LookToVelocity();
				soldier.isAiming = false;
			}
			yield return null;
		}
	}

	private void Soldier_OnManpadStopAiming()
	{
		SendRPC("RPC_ManpadStop");
	}

	[VTRPC]
	private void RPC_ManpadStop()
	{
		isManpadAiming = false;
	}

	private void Soldier_OnManpadAiming(Vector3 worldPos)
	{
		SendRPC("RPC_ManpadAim", VTMapManager.WorldToGlobalPoint(worldPos).toVector3);
	}

	[VTRPC]
	private void RPC_ManpadAim(Vector3 globalPos)
	{
		isManpadAiming = true;
		manpadAimPos.globalPoint = new Vector3D(globalPos);
	}

	private IEnumerator ManpadRemoteUpdateRoutine()
	{
		while (base.enabled && soldier.GetIsAlive())
		{
			if (isManpadAiming)
			{
				soldier.AimAtTarget(manpadAimPos.point);
				soldier.isAiming = true;
			}
			else
			{
				soldier.LookToVelocity();
				soldier.isAiming = false;
			}
			yield return null;
		}
	}

	public void HostLoadIntoBay(VTNetEntity vehicleEntity)
	{
		if (base.isMine)
		{
			if (!loadedInBay)
			{
				RPC_LoadIntoBay(vehicleEntity.entityID);
				SendRPC("RPC_LoadIntoBay", vehicleEntity.entityID);
			}
			else
			{
				Debug.Log("SoldierSync: HostLoadIntoBay called but already loadedInBay");
			}
		}
	}

	[VTRPC]
	private void RPC_LoadIntoBay(int vehicleEntId)
	{
		VTNetEntity entity = VTNetworkManager.instance.GetEntity(vehicleEntId);
		if ((bool)entity)
		{
			Debug.Log("SoldierSync.RPC_LoadIntoBay(" + entity.gameObject.name + ")");
			loadedInBay = entity.GetComponentInChildren<PassengerBay>();
			loadedInBay.MP_LoadSoldier(soldier);
			moverSync.PauseSync();
		}
		else
		{
			Debug.Log($"SoldierSync.RPC_LoadIntoBay({vehicleEntId}) (entity not found)");
		}
	}

	public void ClientRequestLoadIntoBay(VTNetEntity clientVehicleEntity)
	{
		if (!base.isMine)
		{
			Debug.Log("SoldierSync requested to load into client bay.");
			SendRPC("RPC_ReqLoadIntoBay", clientVehicleEntity.entityID);
		}
	}

	[VTRPC]
	private void RPC_ReqLoadIntoBay(int vehicleEntId)
	{
		if (!base.isMine)
		{
			return;
		}
		Debug.Log($"SoldierSync.RPC_ReqLoadIntoBay({vehicleEntId})");
		if (!loadedInBay)
		{
			VTNetEntity entity = VTNetworkManager.instance.GetEntity(vehicleEntId);
			if ((bool)entity)
			{
				HostLoadIntoBay(entity);
			}
			else
			{
				Debug.Log(" - vehicle entity not found");
			}
		}
		else
		{
			Debug.Log(" - already loaded into a bay");
		}
	}

	public void UnloadFromBay(UnloadingZone unloadZone)
	{
		Debug.Log("SoldierSync.UnloadFromBay");
		RPC_Unload(unloadZone.dropoffObjectiveID);
		SendRPC("RPC_Unload", unloadZone.dropoffObjectiveID);
	}

	[VTRPC]
	private void RPC_Unload(int dropoffId)
	{
		Debug.Log($"SoldierSync.RPC_Unload({dropoffId})");
		if (!loadedInBay)
		{
			Debug.LogError(" - loadedInBay is null!");
			return;
		}
		if (base.isMine)
		{
			StartCoroutine(UnloadFromBay(loadedInBay, dropoffId));
		}
		else
		{
			soldier.OnUnloadFromBay();
		}
		soldier.isLoadedInBay = false;
		loadedInBay = null;
		moverSync.UnpauseSync();
	}

	private void Soldier_OnPassengerBayDiedWhileInvincible()
	{
		if (base.isMine)
		{
			RPC_ResetPBDied();
			SendRPC("RPC_ResetPBDied");
		}
	}

	[VTRPC]
	private void RPC_ResetPBDied()
	{
		Debug.Log("SoldierSync.RPC_ResetPBDied()");
		soldier.isLoadedInBay = false;
		soldier.StartWaitingForPickup();
		if ((bool)soldier.actor.unitSpawn)
		{
			soldier.transform.rotation = soldier.actor.unitSpawn.unitSpawner.transform.rotation;
		}
		loadedInBay = null;
		moverSync.UnpauseSync();
	}

	private IEnumerator UnloadFromBay(PassengerBay bay, int dropoffID)
	{
		UnloadingZone unloadZone = VTScenario.current.objectives.GetObjective(dropoffID).waypoint.GetTransform().GetComponent<UnloadingZone>();
		if (!unloadZone)
		{
			Debug.LogError("SoldierSync.UnloadFromBay: no unloadZone found!");
			yield break;
		}
		Soldier s = soldier;
		s.isLoadedInBay = false;
		Vector3 tgtFwd2 = bay.exitTransform.forward;
		tgtFwd2.y = 0f;
		tgtFwd2 = tgtFwd2.normalized;
		Quaternion targetRot = Quaternion.LookRotation(tgtFwd2, Vector3.up);
		Vector3 lp = bay.exitTransform.InverseTransformPoint(s.transform.position);
		while (Vector3.SqrMagnitude(lp) > 0.1f)
		{
			lp = Vector3.MoveTowards(lp, Vector3.zero, s.mover.moveSpeed * Time.deltaTime);
			s.transform.rotation = Quaternion.Lerp(s.transform.rotation, targetRot, 15f * Time.deltaTime);
			s.transform.position = bay.exitTransform.TransformPoint(lp);
			yield return null;
		}
		FollowPath followPath = null;
		Waypoint waypoint = null;
		s.SetVelocity(tgtFwd2 * s.mover.moveSpeed);
		if (unloadZone.unloadPaths != null && unloadZone.unloadPaths.Length != 0)
		{
			float num = float.MaxValue;
			FollowPath followPath2 = null;
			FollowPath[] unloadPaths = unloadZone.unloadPaths;
			foreach (FollowPath followPath3 in unloadPaths)
			{
				float sqrMagnitude = (s.transform.position - followPath3.pointTransforms[0].position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					followPath2 = followPath3;
				}
			}
			s.mover.path = followPath2;
			s.mover.behavior = GroundUnitMover.Behaviors.Path;
			followPath = followPath2;
		}
		else
		{
			s.mover.rallyTransform = unloadZone.unloadRallyPoint;
			s.mover.behavior = GroundUnitMover.Behaviors.StayInRadius;
			waypoint = unloadZone.unloadRallyWpt;
		}
		s.mover.rallyRadius = 1f;
		s.OnUnloadFromBay();
		s.mover.RefreshBehaviorRoutines();
		if ((bool)s.mover.squad && s.mover.squad.leaderMover == s.mover)
		{
			if (waypoint != null)
			{
				s.mover.squad.MoveTo(waypoint.GetTransform());
			}
			else if (followPath != null)
			{
				s.mover.squad.MovePath(followPath);
			}
		}
	}
}

}