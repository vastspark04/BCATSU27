using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarrierSpawnEdButton : MonoBehaviour
{
	public class CarrierSpawnableFilter : IUnitFilter
	{
		public List<string> spawnableUnits;

		public bool PassesFilter(UnitSpawner uSpawner)
		{
			if (spawnableUnits.Contains(uSpawner.unitID))
			{
				return !uSpawner.spawnFlags.Contains("carrier");
			}
			return false;
		}
	}

	public VTEdCustomCarrierSpawnEditor spawnEditor;

	public Button button;

	public Text currentUnitText;

	[HideInInspector]
	public UnitSpawner selectedUnit;

	private CarrierSpawnableFilter unitFilter;

	[HideInInspector]
	public int spawnPointIndex;

	private UnitSpawner tempAltSpawnUnit;

	public void Initialize(int idx, UnitSpawner currentUnit)
	{
		spawnPointIndex = idx;
		selectedUnit = currentUnit;
		unitFilter = new CarrierSpawnableFilter();
		unitFilter.spawnableUnits = new List<string>();
		foreach (CarrierSpawnableUnit spawnableUnit in spawnEditor.carrierUnit.prefabUnitSpawn.GetComponent<AICarrierSpawn>().spawnableUnits)
		{
			if (spawnableUnit.spawnPoints.Contains(idx))
			{
				unitFilter.spawnableUnits.Add(spawnableUnit.unitID);
			}
		}
		button.onClick.AddListener(OnClick);
		UpdateText();
	}

	private void OnClick()
	{
		spawnEditor.spawnWindow.editor.unitSelector.DisplayUnitSelector("Carrier Spawn", TeamOptions.SameTeam, spawnEditor.carrierUnit.team, OnSelectedUnit, allowSubunits: false, new IUnitFilter[1] { unitFilter });
	}

	private void OnSelectedUnit(UnitReference unitRef)
	{
		UnitSpawner spawner = unitRef.GetSpawner();
		if (spawner != null && spawner.alternateSpawns.Count > 0)
		{
			tempAltSpawnUnit = spawner;
			spawnEditor.spawnWindow.editor.confirmDialogue.DisplayConfirmation("Attach unit?", "This unit has alternate spawns. Attaching to the carrier will delete the unit's alt spawns.", ConfirmRemoveAltSpawns, UpdateText);
			return;
		}
		if (selectedUnit != null)
		{
			Vector3D globalPosition = selectedUnit.GetGlobalPosition();
			globalPosition.y += 40.0;
			selectedUnit.SetGlobalPosition(globalPosition);
			selectedUnit.RemoveFlag("carrier");
		}
		selectedUnit = spawner;
		UpdateText();
		if (spawner != null)
		{
			selectedUnit.SetFlag("carrier");
		}
		spawnEditor.UpdateSpawnerPositions();
	}

	private void ConfirmRemoveAltSpawns()
	{
		selectedUnit = tempAltSpawnUnit;
		UpdateText();
		if (selectedUnit != null)
		{
			selectedUnit.SetFlag("carrier");
		}
		spawnEditor.UpdateSpawnerPositions();
	}

	public void ClearSelection()
	{
		currentUnitText.text = "None";
		selectedUnit = null;
	}

	private void UpdateText()
	{
		if (selectedUnit == null)
		{
			currentUnitText.text = "None";
		}
		else
		{
			currentUnitText.text = selectedUnit.GetUIDisplayName();
		}
	}
}
