using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class AIFixedSAMSpawn : GroundUnitSpawn
{
	public class LockingRadarUnitFilter : IUnitFilter
	{
		public bool PassesFilter(UnitSpawner spawner)
		{
			return spawner.prefabUnitSpawn is AILockingRadarSpawn;
		}
	}

	[UnitSpawn("Radars")]
	public UnitReferenceListSame radarUnits = new UnitReferenceListSame(new IUnitFilter[1]
	{
		new LockingRadarUnitFilter()
	});

	[UnitSpawn("Allow Reload")]
	public bool allowReload;

	[UnitSpawnAttributeRange("Reload Time (sec)", 0f, 600f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float reloadTime = 120f;

	public SAMLauncher samLauncher;

	private List<LockingRadar> lrs = new List<LockingRadar>();

	[VTEvent("Reload", "Force the launcher to reload now.")]
	public void ReloadNow()
	{
		samLauncher.LoadAllMissiles();
	}

	[VTEvent("Set Allow Reloads", "Set whether the launcher is allowed to reload.", new string[] { "Allow Reload" })]
	public void SetAllowReload(bool allowed)
	{
		allowReload = allowed;
		samLauncher.allowReload = allowed;
		if (allowed && samLauncher.missileCount == 0)
		{
			samLauncher.LoadAllMissiles();
		}
	}

	[VTEvent("Set Reload Time", "Set the reload time for the launcher.", new string[] { "Reload Time" })]
	public void SetReloadTime([VTRangeParam(0f, 600f)] float time)
	{
		samLauncher.reloadTime = time;
	}

	public override void OnPreSpawnUnit()
	{
		base.OnPreSpawnUnit();
		SetEquipPrefab();
	}

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		LoadMissiles();
		samLauncher.allowReload = allowReload;
		samLauncher.reloadTime = reloadTime;
		foreach (UnitReference unit2 in radarUnits.units)
		{
			UnitSpawner spawner = unit2.GetSpawner();
			if (!spawner)
			{
				continue;
			}
			if (spawner.spawned)
			{
				UnitSpawn unit = unit2.GetUnit();
				if (unit is AILockingRadarSpawn)
				{
					lrs.Add(((AILockingRadarSpawn)unit).lockingRadar);
				}
			}
			else if (spawner.prefabUnitSpawn is AILockingRadarSpawn)
			{
				StartCoroutine(AddLockingRadarWhenReady(spawner));
			}
		}
		samLauncher.lockingRadars = lrs.ToArray();
	}

	private IEnumerator AddLockingRadarWhenReady(UnitSpawner lrSpawner)
	{
		while (!lrSpawner.spawned)
		{
			yield return null;
		}
		lrs.Add(((AILockingRadarSpawn)lrSpawner.spawnedUnit).lockingRadar);
		samLauncher.lockingRadars = lrs.ToArray();
	}

	private void SetEquipPrefab()
	{
		for (int i = 0; i < loadout.hpLoadout.Length; i++)
		{
			GameObject[] array = equipPrefabs;
			foreach (GameObject gameObject in array)
			{
				if (loadout.hpLoadout[i] == gameObject.gameObject.name)
				{
					samLauncher.missilePrefab = gameObject;
					ResourcePath component = gameObject.GetComponent<ResourcePath>();
					if ((bool)component)
					{
						samLauncher.missileResourcePath = component.path;
					}
					return;
				}
			}
		}
	}

	private void LoadMissiles()
	{
		if (!VTOLMPUtils.IsMultiplayer())
		{
			samLauncher.RemoveAllMissiles();
			samLauncher.LoadAllMissiles();
		}
	}

	public override void OnEditorUpdate(VTScenarioEditor editor)
	{
		base.OnEditorUpdate(editor);
		Vector3 position = base.transform.position;
		foreach (UnitReference unit in radarUnits.units)
		{
			Vector3 position2 = unit.GetSpawner().transform.position;
			editor.editorCamera.DrawLine(position, position2, Color.yellow);
		}
	}

	public override void Quicksave(ConfigNode qsNode)
	{
		base.Quicksave(qsNode);
		qsNode.SetValue("allowReload", allowReload);
	}

	public override void Quickload(ConfigNode qsNode)
	{
		base.Quickload(qsNode);
		bool value = qsNode.GetValue<bool>("allowReload");
		SetAllowReload(value);
	}
}
