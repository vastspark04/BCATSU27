using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class RWRSync : VTNetSyncRPCOnly
{
	public ModuleRWR rwr;

	public MultiUserVehicleSync muvs;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			rwr.OnEnableRWR += Rwr_OnEnableRWR;
			rwr.OnDisableRWR += Rwr_OnDisableRWR;
			if ((bool)muvs)
			{
				rwr.OnDetectPing += RwrOnDetPingMuvsOwner;
				rwr.OnLockDetectPing += RwrOnLockPingMuvsOwner;
			}
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
			SendRPC("RPC_RWREnabled", rwr.enabled ? 1 : 0);
		}
		else
		{
			rwr.OnDetectPing += Rwr_OnDetectPing;
			rwr.OnLockDetectPing += Rwr_OnLockDetectPing;
		}
	}

	private void OnDestroy()
	{
		if ((bool)VTNetworkManager.instance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		if (base.isMine)
		{
			SendDirectedRPC(obj, "RPC_RWREnabled", rwr.enabled ? 1 : 0);
		}
	}

	private void Rwr_OnDisableRWR()
	{
		SendRPC("RPC_RWREnabled", 0);
	}

	private void Rwr_OnEnableRWR()
	{
		SendRPC("RPC_RWREnabled", 1);
	}

	[VTRPC]
	private void RPC_RWREnabled(int e)
	{
		rwr.enabled = e > 0;
	}

	private void Rwr_OnLockDetectPing(ModuleRWR.RWRContact contact)
	{
		if ((bool)muvs && !muvs.isMine && muvs.IsLocalPlayerSeated())
		{
			SendDirectedRPC(base.netEntity.ownerID, "RPC_LockPingHalt", VTNetUtils.GetActorIdentifier(contact.radarActor), contact.signalStrength);
		}
		else
		{
			SendDirectedRPC(base.netEntity.ownerID, "RPC_LockPing", VTNetUtils.GetActorIdentifier(contact.radarActor), contact.signalStrength);
		}
	}

	private void Rwr_OnDetectPing(ModuleRWR.RWRContact contact)
	{
		if ((bool)muvs && !muvs.isMine && muvs.IsLocalPlayerSeated())
		{
			SendDirectedRPC(base.netEntity.ownerID, "RPC_DetPingHalt", VTNetUtils.GetActorIdentifier(contact.radarActor), contact.signalStrength);
		}
		else
		{
			SendDirectedRPC(base.netEntity.ownerID, "RPC_DetPing", VTNetUtils.GetActorIdentifier(contact.radarActor), contact.signalStrength);
		}
	}

	private PlayerInfo GetPlayer(Actor a)
	{
		return VTOLMPSceneManager.instance.GetPlayer(a);
	}

	private void RwrOnDetPingMuvsOwner(ModuleRWR.RWRContact contact)
	{
		muvs.SendRPCToCopilots(this, "RPC_DetPing", VTNetUtils.GetActorIdentifier(contact.radarActor), contact.signalStrength);
	}

	private void RwrOnLockPingMuvsOwner(ModuleRWR.RWRContact contact)
	{
		muvs.SendRPCToCopilots(this, "RPC_LockPing", VTNetUtils.GetActorIdentifier(contact.radarActor), contact.signalStrength);
	}

	[VTRPC]
	private void RPC_DetPing(int actorId, float signalStrength)
	{
		if (base.isMine || ((bool)muvs && muvs.IsLocalPlayerSeated()))
		{
			try
			{
				Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorId);
				if ((bool)actorFromIdentifier)
				{
					List<Radar> radars = actorFromIdentifier.GetRadars();
					if (radars != null && radars.Count > 0)
					{
						Radar radar = radars[0];
						if ((bool)radar)
						{
							rwr.Radar_OnDetect(rwr.myActor, actorFromIdentifier, radar.radarSymbol, radar.detectionPersistanceTime, radar.radarTransform ? radar.radarTransform.position : radar.transform.position, signalStrength);
						}
					}
				}
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		if (base.isMine && (bool)muvs)
		{
			muvs.SendRPCToCopilots(this, "RPC_DetPing", actorId, signalStrength);
		}
	}

	[VTRPC]
	private void RPC_DetPingHalt(int actorId, float signalStrength)
	{
		if (!base.isMine && (!muvs || !muvs.IsLocalPlayerSeated()))
		{
			return;
		}
		try
		{
			Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorId);
			if (!actorFromIdentifier)
			{
				return;
			}
			List<Radar> radars = actorFromIdentifier.GetRadars();
			if (radars == null || radars.Count <= 0)
			{
				return;
			}
			Radar radar = radars[0];
			if ((bool)radar)
			{
				if (base.isMine)
				{
					rwr.OnDetectPing -= RwrOnDetPingMuvsOwner;
				}
				rwr.Radar_OnDetect(rwr.myActor, actorFromIdentifier, radar.radarSymbol, radar.detectionPersistanceTime, radar.radarTransform ? radar.radarTransform.position : radar.transform.position, signalStrength);
				if (base.isMine)
				{
					rwr.OnDetectPing += RwrOnDetPingMuvsOwner;
				}
			}
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	[VTRPC]
	private void RPC_LockPing(int actorId, float signalStrength)
	{
		if (base.isMine || ((bool)muvs && muvs.IsLocalPlayerSeated()))
		{
			try
			{
				Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorId);
				if ((bool)actorFromIdentifier)
				{
					LockingRadar[] lockingRadars = actorFromIdentifier.GetLockingRadars();
					if (lockingRadars != null && lockingRadars.Length != 0)
					{
						LockingRadar lockingRadar = lockingRadars[0];
						if ((bool)lockingRadar)
						{
							if (CheckIfValidLock(lockingRadar))
							{
								string radarSymbol = (lockingRadar.isMissile ? "M" : ((!lockingRadar.radar) ? "?" : lockingRadar.radar.radarSymbol));
								rwr.Radar_OnLockPing(rwr.myActor, actorFromIdentifier, radarSymbol, 0.5f, lockingRadar.referenceTransform ? lockingRadar.referenceTransform.position : lockingRadar.transform.position, signalStrength);
							}
							else if (base.isMine)
							{
								VTNetEntity vTNetEntity = actorFromIdentifier.GetNetEntity();
								if ((bool)vTNetEntity)
								{
									if (!vTNetEntity.isMine)
									{
										Debug.Log("RWRSync: Local raycast from rwr(" + rwr.myActor.name + ") detected that a lock from " + actorFromIdentifier.actorName + " should have been occluded. Forcing occlusion remotely.");
										SendDirectedRPC(vTNetEntity.ownerID, "RPC_ForceOcclude", VTNetUtils.GetActorIdentifier(actorFromIdentifier));
									}
									else
									{
										lockingRadar.ForceOcclude(rwr.myActor);
									}
								}
							}
						}
					}
				}
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		if (base.isMine && (bool)muvs)
		{
			muvs.SendRPCToCopilots(this, "RPC_LockPing", actorId, signalStrength);
		}
	}

	[VTRPC]
	private void RPC_LockPingHalt(int actorId, float signalStrength)
	{
		if (!base.isMine && (!muvs || !muvs.IsLocalPlayerSeated()))
		{
			return;
		}
		try
		{
			Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorId);
			if (!actorFromIdentifier)
			{
				return;
			}
			LockingRadar[] lockingRadars = actorFromIdentifier.GetLockingRadars();
			if (lockingRadars == null || lockingRadars.Length == 0)
			{
				return;
			}
			LockingRadar lockingRadar = lockingRadars[0];
			if (!lockingRadar)
			{
				return;
			}
			if (CheckIfValidLock(lockingRadar))
			{
				string radarSymbol = (lockingRadar.isMissile ? "M" : ((!lockingRadar.radar) ? "?" : lockingRadar.radar.radarSymbol));
				rwr.OnLockDetectPing -= RwrOnLockPingMuvsOwner;
				rwr.Radar_OnLockPing(rwr.myActor, actorFromIdentifier, radarSymbol, 0.5f, lockingRadar.referenceTransform ? lockingRadar.referenceTransform.position : lockingRadar.transform.position, signalStrength);
				rwr.OnLockDetectPing += RwrOnLockPingMuvsOwner;
			}
			else
			{
				if (!base.isMine)
				{
					return;
				}
				VTNetEntity vTNetEntity = actorFromIdentifier.GetNetEntity();
				if ((bool)vTNetEntity)
				{
					if (!vTNetEntity.isMine)
					{
						Debug.Log("RWRSync: Local raycast from rwr(" + rwr.myActor.name + ") detected that a lock from " + actorFromIdentifier.actorName + " should have been occluded. Forcing occlusion remotely.");
						SendDirectedRPC(vTNetEntity.ownerID, "RPC_ForceOcclude", VTNetUtils.GetActorIdentifier(actorFromIdentifier));
					}
					else
					{
						lockingRadar.ForceOcclude(rwr.myActor);
					}
				}
			}
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	private bool CheckIfValidLock(LockingRadar r)
	{
		Vector3 position = rwr.myActor.position;
		if (!r.referenceTransform)
		{
			r.referenceTransform = r.transform;
		}
		if (Physics.Linecast(position, r.referenceTransform.position, out var hitInfo, 1) && (hitInfo.point - r.referenceTransform.position).sqrMagnitude > 25f)
		{
			return false;
		}
		return true;
	}

	[VTRPC]
	private void RPC_ForceOcclude(int radarActorId)
	{
		Debug.Log(rwr.myActor.actorName + " : RPC_ForceOcclude()");
		Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(radarActorId);
		if (!actorFromIdentifier)
		{
			return;
		}
		Debug.Log(" - radarActor: " + actorFromIdentifier.actorName);
		VTNetEntity vTNetEntity = actorFromIdentifier.GetNetEntity();
		if (!vTNetEntity || !vTNetEntity.isMine || !rwr.myActor)
		{
			return;
		}
		LockingRadar[] lockingRadars = actorFromIdentifier.GetLockingRadars();
		if (lockingRadars != null)
		{
			if (lockingRadars.Length != 0 && (bool)lockingRadars[0])
			{
				lockingRadars[0].ForceOcclude(rwr.myActor);
			}
		}
		else
		{
			Debug.Log(" - radarActor's lockingRadars array was null!");
		}
	}
}

}