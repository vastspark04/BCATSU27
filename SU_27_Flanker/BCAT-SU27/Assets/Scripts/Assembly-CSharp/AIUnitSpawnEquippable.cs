using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class AIUnitSpawnEquippable : AIUnitSpawn
{
	public GameObject[] equipPrefabs;

	public string[] hardpoints;

	public WeaponManager weaponManager;

	protected Loadout loadout;

	public override void OnPreSpawnUnit()
	{
		base.OnPreSpawnUnit();
		loadout = new Loadout();
		Dictionary<string, string> activeAltUnitFields = unitSpawner.activeAltUnitFields;
		if (activeAltUnitFields.ContainsKey("equips"))
		{
			loadout.hpLoadout = ConfigNodeUtils.ParseList(activeAltUnitFields["equips"]).ToArray();
		}
		else
		{
			loadout.hpLoadout = hardpoints;
		}
	}

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		EquipLoadout();
	}

	public void EquipLoadout()
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			if (VTOLMPLobbyManager.isLobbyHost)
			{
				WeaponManagerSync componentInChildren = GetComponentInChildren<WeaponManagerSync>(includeInactive: true);
				if ((bool)componentInChildren)
				{
					componentInChildren.NetClearWeapons();
					componentInChildren.NetEquipWeapons(loadout);
				}
			}
		}
		else if ((bool)weaponManager)
		{
			weaponManager.EquipWeapons(loadout);
		}
	}
}
