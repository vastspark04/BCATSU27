using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class MissileLauncherSync : EquippableSync
{
	public HPEquipMissileLauncher mlEq;

	private bool _gotMl;

	private MissileLauncher _ml;

	private MultiUserVehicleSync muvs;

	public SAMLauncher samLauncher;

	public IRSamLauncher irSamLauncher;

	public Soldier manpadSoldier;

	public string manpadMissileResourcePath;

	private bool listenedNewClient;

	private bool hasSetupSam;

	private bool hasLoadedMl;

	private bool listenedToMulticrewML;

	private Actor turretEngagingActor;

	private MissileLauncher ml
	{
		get
		{
			if (!_gotMl)
			{
				if ((bool)mlEq)
				{
					_ml = mlEq.ml;
				}
				else if ((bool)irSamLauncher)
				{
					_ml = irSamLauncher.ml;
				}
				else if ((bool)manpadSoldier)
				{
					_ml = manpadSoldier.irMissileLauncher;
				}
				_gotMl = true;
			}
			return _ml;
		}
	}

	[ContextMenu("Get Launcher Equip")]
	private void GetLauncherEq()
	{
		mlEq = base.gameObject.GetComponentImplementing<HPEquipMissileLauncher>();
		samLauncher = GetComponent<SAMLauncher>();
		irSamLauncher = GetComponent<IRSamLauncher>();
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			if (!ml)
			{
				return;
			}
			if (!hasLoadedMl)
			{
				hasLoadedMl = true;
				if ((bool)manpadSoldier)
				{
					manpadSoldier.OnWillReloadManpad += ManpadSoldier_OnWillReloadManpad;
				}
				ml.OnFiredMissileIdx += Ml_OnFiredMissileIdx;
				VTOLMPSceneManager.instance.StartCoroutine(LoadMissilesRoutine());
			}
			Refresh(0uL);
			if (!listenedNewClient)
			{
				VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
				listenedNewClient = true;
			}
		}
		else if ((bool)irSamLauncher)
		{
			irSamLauncher.StopAllCoroutines();
			irSamLauncher.enabled = false;
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
		if (base.isMine)
		{
			if (!hasSetupSam)
			{
				hasSetupSam = true;
				if ((bool)samLauncher)
				{
					samLauncher.OnFiringMissileIdx += Sam_OnFiringMissileIdx;
					samLauncher.OnFiredMissile += SamLauncher_OnFiredMissile;
					samLauncher.OnWillReloadMissiles += SamLauncher_OnWillReloadMissiles;
					if ((bool)samLauncher.turret)
					{
						samLauncher.OnTurretEngageActor += SamLauncher_OnTurretEngageActor;
					}
					StartCoroutine(LoadMissilesRoutine());
					if (!listenedNewClient)
					{
						VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
						listenedNewClient = true;
					}
					Refresh(0uL);
				}
			}
		}
		else if ((bool)samLauncher)
		{
			samLauncher.SetToMPRemote();
			if ((bool)samLauncher.turret)
			{
				StartCoroutine(RemoteSamTurretRoutine());
			}
		}
		if (!base.isMine && (bool)mlEq)
		{
			while (!mlEq.weaponManager)
			{
				yield return null;
			}
			muvs = mlEq.weaponManager.GetComponent<MultiUserVehicleSync>();
			if ((bool)muvs && !listenedToMulticrewML)
			{
				mlEq.ml.OnRemoteFiredMissile += Ml_OnRemoteFiredMissile;
				mlEq.ml.remoteOnly = true;
				listenedToMulticrewML = true;
			}
		}
	}

	private void ManpadSoldier_OnWillReloadManpad()
	{
		StartCoroutine(LoadMissilesRoutine());
	}

	private void Ml_OnRemoteFiredMissile(Actor target)
	{
		Debug.Log("MissileLauncherSync.Ml_OnRemoteFiredMissile()");
		int actorIdentifier = VTNetUtils.GetActorIdentifier(target);
		SendDirectedRPC(base.netEntity.ownerID, "RPC_MCRemoteFired", actorIdentifier);
	}

	[VTRPC]
	private void RPC_MCRemoteFired(int targetId)
	{
		Debug.Log("RPC_MCRemoteFired()");
		mlEq.ml.RemoteFireOn(VTNetUtils.GetActorFromIdentifier(targetId));
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
		Refresh(obj.Value);
	}

	private void SamLauncher_OnWillReloadMissiles()
	{
		StartCoroutine(LoadMissilesRoutine());
	}

	private void SamLauncher_OnFiredMissile(Missile obj)
	{
		obj.GetComponent<MissileSync>().BeginFlightSend();
	}

	private void Ml_OnFiredMissileIdx(int i)
	{
		MissileSync component = ml.missiles[i].GetComponent<MissileSync>();
		component.rb = component.GetComponent<Rigidbody>();
		component.BeginFlightSend();
		SendRPC("RPC_MlFire", i);
		if ((bool)mlEq && (bool)mlEq.weaponManager && mlEq.weaponManager.actor.isPlayer)
		{
			ml.missiles[i].sourcePlayer = VTOLMPLobbyManager.localPlayerInfo;
		}
	}

	[VTRPC]
	private void RPC_MlFire(int i)
	{
		if (ml.missiles != null && (bool)ml.missiles[i])
		{
			ml.missiles[i].GetComponent<MissileSync>().Client_FireMissile();
			ml.RefreshCount();
		}
	}

	private void Sam_OnFiringMissileIdx(int i)
	{
		SendRPC("RPC_SamLaunch", i);
	}

	[VTRPC]
	private void RPC_SamLaunch(int idx)
	{
		if ((bool)samLauncher)
		{
			samLauncher.MP_RemoteFireMissile(idx);
		}
		else if (!irSamLauncher)
		{
			Debug.LogError("Received RPC to launch SAM but samLauncher was null");
		}
	}

	private IEnumerator LoadMissilesRoutine()
	{
		VTNetworkManager.NetInstantiateRequest[] requests = ((!ml) ? new VTNetworkManager.NetInstantiateRequest[samLauncher.fireTransforms.Length] : new VTNetworkManager.NetInstantiateRequest[ml.hardpoints.Length]);
		if ((bool)ml)
		{
			ml.RemoveAllMissiles();
			string resourcePath = (mlEq ? mlEq.missileResourcePath : ((!manpadSoldier) ? irSamLauncher.missileResourcePath : manpadMissileResourcePath));
			for (int j = 0; j < ml.hardpoints.Length; j++)
			{
				requests[j] = VTNetworkManager.NetInstantiate(resourcePath, ml.hardpoints[j].position, ml.hardpoints[j].rotation);
			}
		}
		else
		{
			for (int k = 0; k < samLauncher.fireTransforms.Length; k++)
			{
				requests[k] = VTNetworkManager.NetInstantiate(samLauncher.missileResourcePath, samLauncher.fireTransforms[k].position, samLauncher.fireTransforms[k].rotation);
			}
		}
		string ownerName;
		if ((bool)samLauncher)
		{
			ownerName = samLauncher.actor.actorName;
		}
		else
		{
			PlayerInfo player = VTOLMPLobbyManager.GetPlayer(base.netEntity.owner.Id);
			Actor componentInParent = GetComponentInParent<Actor>();
			ownerName = ((player != null && (!componentInParent || (bool)componentInParent.GetComponent<PlayerVehicleNetSync>() || (bool)componentInParent.GetComponent<MultiUserVehicleSync>())) ? player.pilotName : ((!componentInParent) ? base.netEntity.owner.Name : componentInParent.actorName));
		}
		for (int i = 0; i < requests.Length; i++)
		{
			while (!requests[i].isReady)
			{
				yield return null;
			}
			if ((bool)ml)
			{
				ml.LoadMissile(MissileLauncher.LoadMissile(requests[i].obj, ml.hardpoints[i], ml.useEdgeTf, ml.hideUntilLaunch, instantiate: false), i);
				requests[i].obj.name = ml.missilePrefab.name + " (" + ownerName + ")";
			}
			else
			{
				samLauncher.LoadMissile(MissileLauncher.LoadMissile(requests[i].obj, samLauncher.fireTransforms[i], samLauncher.useEdgeTf, samLauncher.onlySpawnOnLaunch, instantiate: false), i);
				requests[i].obj.name = samLauncher.missilePrefab.name + " (" + ownerName + ")";
			}
			VTNetEntity component = requests[i].obj.GetComponent<VTNetEntity>();
			SendRPC("RPC_MissileRail", component.entityID, i);
		}
	}

	[VTRPC]
	private void RPC_MissileRail(int entId, int rail)
	{
		VTNetEntity entity = VTNetworkManager.instance.GetEntity(entId);
		if ((bool)entity)
		{
			if ((bool)ml)
			{
				PlayerInfo player = VTOLMPLobbyManager.GetPlayer(base.netEntity.owner.Id);
				if (player != null)
				{
					entity.gameObject.name = ml.missilePrefab.name + " (" + player.pilotName + ")";
				}
				else
				{
					entity.gameObject.name = ml.missilePrefab.name + " (" + (irSamLauncher ? irSamLauncher.GetComponent<Actor>().actorName : base.netEntity.owner.Name) + ")";
				}
				VTNetEntity componentInChildren = ml.hardpoints[rail].GetComponentInChildren<VTNetEntity>(includeInactive: true);
				if (!componentInChildren || componentInChildren.entityID != entId)
				{
					ml.LoadMissile(MissileLauncher.LoadMissile(entity.gameObject, ml.hardpoints[rail], ml.useEdgeTf, ml.hideUntilLaunch, instantiate: false), rail);
				}
			}
			else
			{
				entity.gameObject.name = samLauncher.missilePrefab.name + " (" + samLauncher.actor.actorName + ")";
				VTNetEntity componentInChildren2 = samLauncher.fireTransforms[rail].GetComponentInChildren<VTNetEntity>(includeInactive: true);
				if (!componentInChildren2 || componentInChildren2.entityID != entId)
				{
					samLauncher.LoadMissile(MissileLauncher.LoadMissile(entity.gameObject, samLauncher.fireTransforms[rail], samLauncher.useEdgeTf, samLauncher.onlySpawnOnLaunch, instantiate: false), rail);
				}
			}
		}
		else
		{
			Debug.LogError(" - missile entity not found!");
		}
	}

	private void Refresh(ulong target = 0uL)
	{
		if (!base.isMine)
		{
			return;
		}
		if ((bool)ml)
		{
			for (int i = 0; i < ml.missiles.Length; i++)
			{
				if ((bool)ml.missiles[i])
				{
					VTNetEntity component = ml.missiles[i].GetComponent<VTNetEntity>();
					if (component.hasRegistered)
					{
						SendDirectedRPC(target, "RPC_MissileRail", component.entityID, i);
					}
				}
			}
			return;
		}
		for (int j = 0; j < samLauncher.missiles.Length; j++)
		{
			if ((bool)samLauncher.missiles[j])
			{
				VTNetEntity component2 = samLauncher.missiles[j].GetComponent<VTNetEntity>();
				if (component2.hasRegistered)
				{
					SendDirectedRPC(target, "RPC_MissileRail", component2.entityID, j);
				}
			}
		}
	}

	private void SamLauncher_OnTurretEngageActor(Actor obj)
	{
		SendRPC("RPC_SAMTurret", VTNetUtils.GetActorIdentifier(obj));
	}

	[VTRPC]
	private void RPC_SAMTurret(int actorId)
	{
		turretEngagingActor = VTNetUtils.GetActorFromIdentifier(actorId);
		if (!turretEngagingActor)
		{
			samLauncher.turret.ReturnTurretOneshot();
		}
	}

	private IEnumerator RemoteSamTurretRoutine()
	{
		while (base.enabled)
		{
			if ((bool)turretEngagingActor)
			{
				if (SAMLauncher.GetAirToAirFireSolution(samLauncher.fireTransforms[0].position, samLauncher.actor.velocity, samLauncher.targetingSimSpeed, turretEngagingActor, samLauncher.turret, samLauncher.fireSafetyReferenceTf, out var fireSolution))
				{
					samLauncher.turret.AimToTarget(fireSolution);
				}
				else
				{
					samLauncher.turret.AimToTarget(turretEngagingActor.position);
				}
			}
			yield return null;
		}
	}
}

}