using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTOLMPUnitManager : VTNetSyncRPCOnly
{
	private class SensorLockInfo
	{
		public bool tgp;

		public bool radar;

		public bool arad;

		public bool tsd;
	}

	private List<int> entIdsSpawned = new List<int>();

	private Dictionary<ulong, Dictionary<int, SensorLockInfo>> playersLockingUnits = new Dictionary<ulong, Dictionary<int, SensorLockInfo>>();

	public static VTOLMPUnitManager instance { get; private set; }

	protected override void Awake()
	{
		instance = this;
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void Instance_OnNewClientConnected(SteamId newId)
	{
		foreach (int item in entIdsSpawned)
		{
			SendDirectedRPC(newId, "RPC_Spawn", item);
		}
	}

	public void NetSpawnUnit(int entId)
	{
		if (base.isMine)
		{
			SendRPC("RPC_Spawn", entId);
			entIdsSpawned.Add(entId);
		}
	}

	[VTRPC]
	private void RPC_Spawn(int entId)
	{
		Debug.Log($"RPC_Spawn({entId})");
		VTNetEntity entity = VTNetworkManager.instance.GetEntity(entId);
		if ((bool)entity)
		{
			UnitSpawn component = entity.GetComponent<UnitSpawn>();
			_ = component.unitID;
			component.unitSpawner.SpawnUnit();
		}
		else
		{
			Debug.LogError($"VTOLMPUnitManager tried to spawn a unit but it was not found! (entId={entId})");
		}
	}

	public void SetUnitID(int entId, int unitId)
	{
		if (base.isMine)
		{
			SendRPCBuffered("RPC_SetUnitID", entId, unitId);
		}
	}

	[VTRPC]
	private void RPC_SetUnitID(int entId, int unitId)
	{
		Debug.Log($"RPC_SetUnitID({entId}, {unitId})");
		VTNetEntity entity = VTNetworkManager.instance.GetEntity(entId);
		if ((bool)entity)
		{
			UnitSpawn component = entity.GetComponent<UnitSpawn>();
			VTScenario.current.units.GetUnit(unitId).MPClient_PrespawnUnit(component);
		}
		else
		{
			Debug.LogError($"VTOLMPUnitManager tried to prespawn a unit but it was not found! (entId={entId})");
		}
	}

	public void RespawnStaticObjectForNet(VTStaticObject vtso)
	{
		StartCoroutine(RespawnStaticObject(vtso));
	}

	private IEnumerator RespawnStaticObject(VTStaticObject vtso)
	{
		string resourcePath = vtso.GetComponent<VTStaticObjectSync>().resourcePath;
		VTNetworkManager.NetInstantiateRequest req = VTNetworkManager.NetInstantiate(resourcePath, vtso.transform.position, vtso.transform.rotation, active: false);
		while (!req.isReady)
		{
			yield return null;
		}
		VTStaticObjectSync component = req.obj.GetComponent<VTStaticObjectSync>();
		component.SendID(vtso.id);
		Client_ReplaceStaticObject(component.vtso);
	}

	public void Client_ReplaceStaticObject(VTStaticObject netInstantiatedObject)
	{
		VTScenario.current.staticObjects.MP_ReplaceObject(netInstantiatedObject);
	}

	public void ReportLockingUnit(int unitID, PlayerSpawn.TargetingMethods targetingMethod)
	{
		RPC_LockingUnit(BDSteamClient.mySteamID, unitID, (int)targetingMethod);
		SendRPC("RPC_LockingUnit", BDSteamClient.mySteamID, unitID, (int)targetingMethod);
	}

	[VTRPC]
	private void RPC_LockingUnit(ulong player, int unitID, int targetingMethod)
	{
		ReportLockStatus(player, unitID, targetingMethod, locked: true);
	}

	public void ReportUnlockUnit(int unitID, PlayerSpawn.TargetingMethods targetingMethod)
	{
		RPC_UnlockingUnit(BDSteamClient.mySteamID, unitID, (int)targetingMethod);
		SendRPC("RPC_UnlockingUnit", BDSteamClient.mySteamID, unitID, (int)targetingMethod);
	}

	[VTRPC]
	private void RPC_UnlockingUnit(ulong player, int unitID, int targetingMethod)
	{
		ReportLockStatus(player, unitID, targetingMethod, locked: false);
	}

	private void ReportLockStatus(ulong player, int unitID, int targetingMethod, bool locked)
	{
		if (!playersLockingUnits.TryGetValue(player, out var value))
		{
			value = new Dictionary<int, SensorLockInfo>();
			playersLockingUnits.Add(player, value);
		}
		if (!value.TryGetValue(unitID, out var value2))
		{
			value2 = new SensorLockInfo();
			value.Add(unitID, value2);
		}
		switch (targetingMethod)
		{
		case 0:
			value2.radar = locked;
			break;
		case 1:
			value2.tgp = locked;
			break;
		case 2:
			value2.tsd = locked;
			break;
		case 3:
			value2.arad = locked;
			break;
		}
	}

	public static UnitSpawn GetParentSpawn(Actor a)
	{
		if ((bool)a.unitSpawn)
		{
			return a.unitSpawn;
		}
		if ((bool)a.parentActor)
		{
			return GetParentSpawn(a.parentActor);
		}
		return null;
	}

	public bool IsPlayerLockingUnit(ulong player, int unitID, PlayerSpawn.TargetingMethods tMethod)
	{
		if (!playersLockingUnits.TryGetValue(player, out var value))
		{
			return false;
		}
		if (!value.TryGetValue(unitID, out var value2))
		{
			return false;
		}
		return tMethod switch
		{
			PlayerSpawn.TargetingMethods.Radar => value2.radar, 
			PlayerSpawn.TargetingMethods.TGP => value2.tgp, 
			PlayerSpawn.TargetingMethods.TSD => value2.tsd, 
			PlayerSpawn.TargetingMethods.ARAD => value2.arad, 
			_ => false, 
		};
	}
}

}