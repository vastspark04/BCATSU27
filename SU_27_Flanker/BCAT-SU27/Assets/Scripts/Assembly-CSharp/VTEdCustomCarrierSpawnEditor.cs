using System.Collections.Generic;
using UnityEngine;

public class VTEdCustomCarrierSpawnEditor : MonoBehaviour
{
	[HideInInspector]
	public VTEdCarrierSpawnWindow spawnWindow;

	[HideInInspector]
	public UnitSpawner carrierUnit;

	public List<CarrierSpawnEdButton> spawnButtons;

	private string originalData;

	public void Initialize(UnitSpawner cUnit)
	{
		carrierUnit = cUnit;
		UpdateButtonsFromFieldData();
		UpdateSpawnerPositions();
	}

	private void ParseCarrierSpawn(string csList, int[] spawnIds)
	{
		for (int i = 0; i < spawnIds.Length; i++)
		{
			spawnIds[i] = -1;
		}
		if (!string.IsNullOrEmpty(csList))
		{
			List<string> list = ConfigNodeUtils.ParseList(csList);
			for (int j = 0; j < list.Count; j++)
			{
				string[] array = list[j].Split(':');
				int num = int.Parse(array[0]);
				int num2 = (spawnIds[num] = int.Parse(array[1]));
			}
		}
	}

	private string WriteCarrierSpawnList()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < spawnButtons.Count; i++)
		{
			list.Add(WriteCarrierSpawn(i, spawnButtons[i].selectedUnit));
		}
		return ConfigNodeUtils.WriteList(list);
	}

	private string WriteCarrierSpawn(int idx, UnitSpawner unit)
	{
		return $"{idx}:{(unit ? unit.unitInstanceID : (-1))}";
	}

	public void SaveCarrierSpawnData()
	{
		string value = (originalData = WriteCarrierSpawnList());
		if (carrierUnit.unitFields.ContainsKey("carrierSpawns"))
		{
			carrierUnit.unitFields["carrierSpawns"] = value;
		}
		else
		{
			carrierUnit.unitFields.Add("carrierSpawns", value);
		}
	}

	public void Revert()
	{
		AICarrierSpawn component = carrierUnit.prefabUnitSpawn.GetComponent<AICarrierSpawn>();
		int[] array = new int[component.spawnPoints.Count];
		int[] array2 = new int[component.spawnPoints.Count];
		ParseCarrierSpawn(WriteCarrierSpawnList(), array);
		ParseCarrierSpawn(originalData, array2);
		foreach (int num in array)
		{
			if (num >= 0 && !array2.Contains(num))
			{
				UnitSpawner unit = VTScenario.current.units.GetUnit(num);
				unit.SetGlobalPosition(unit.lastValidPlacement);
				unit.RemoveFlag("carrier");
			}
		}
		foreach (int num2 in array2)
		{
			if (num2 >= 0 && !array.Contains(num2))
			{
				VTScenario.current.units.GetUnit(num2).SetFlag("carrier");
			}
		}
		carrierUnit.unitFields["carrierSpawns"] = originalData;
		UpdateButtonsFromFieldData();
		UpdateSpawnerPositions();
	}

	private void UpdateButtonsFromFieldData()
	{
		AICarrierSpawn component = carrierUnit.prefabUnitSpawn.GetComponent<AICarrierSpawn>();
		if (carrierUnit.unitFields != null && carrierUnit.unitFields.ContainsKey("carrierSpawns"))
		{
			int[] array = new int[component.spawnPoints.Count];
			originalData = carrierUnit.unitFields["carrierSpawns"];
			ParseCarrierSpawn(originalData, array);
			for (int i = 0; i < spawnButtons.Count; i++)
			{
				UnitSpawner currentUnit = null;
				if (array[i] >= 0)
				{
					currentUnit = spawnWindow.editor.currentScenario.units.GetUnit(array[i]);
				}
				spawnButtons[i].Initialize(i, currentUnit);
			}
		}
		else
		{
			for (int j = 0; j < spawnButtons.Count; j++)
			{
				spawnButtons[j].Initialize(j, null);
			}
		}
	}

	public void UpdateSpawnerPositions()
	{
		AICarrierSpawn component = carrierUnit.prefabUnitSpawn.GetComponent<AICarrierSpawn>();
		for (int i = 0; i < spawnButtons.Count; i++)
		{
			if ((bool)spawnButtons[i].selectedUnit)
			{
				Vector3 position = component.spawnPoints[i].spawnTf.position;
				Vector3D globalPoint = VTMapManager.WorldToGlobalPoint(carrierUnit.transform.TransformPoint(position));
				Vector3 forward = carrierUnit.transform.TransformDirection(component.spawnPoints[i].spawnTf.forward);
				Vector3 worldPoint = VTMapManager.GlobalToWorldPoint(carrierUnit.GetGlobalPosition()) + carrierUnit.spawnerRotation * position;
				Quaternion spawnerRotation = carrierUnit.spawnerRotation * component.spawnPoints[i].spawnTf.rotation;
				spawnButtons[i].selectedUnit.SetGlobalPosition(VTMapManager.WorldToGlobalPoint(worldPoint));
				spawnButtons[i].selectedUnit.spawnerRotation = spawnerRotation;
				spawnButtons[i].selectedUnit.transform.position = VTMapManager.GlobalToWorldPoint(globalPoint);
				spawnButtons[i].selectedUnit.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
				spawnButtons[i].selectedUnit.alternateSpawns.Clear();
			}
		}
	}
}
