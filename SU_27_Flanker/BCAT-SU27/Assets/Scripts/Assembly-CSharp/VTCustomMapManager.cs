using System;
using System.Collections;
using UnityEngine;

public class VTCustomMapManager : MonoBehaviour
{
	public static VTMapCustom customMapToLoad;

	public VTMapGenerator mapGenerator;

	public static VTCustomMapManager instance { get; private set; }

	public static event Action<VTMapCustom> OnLoadedMap;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		if (customMapToLoad != null)
		{
			FlightSceneManager.FlightReadyContingent flightReadyContingent = new FlightSceneManager.FlightReadyContingent();
			flightReadyContingent.ready = false;
			FlightSceneManager.instance.AddReadyContingent(flightReadyContingent);
			customMapToLoad.LoadFlightSceneObjects();
			VTTerrainJob.deleteUnderwaterTris = false;
			mapGenerator.GenerateVTMap(customMapToLoad);
			StartCoroutine(ReadyRoutine(flightReadyContingent));
			SetupMapManager(customMapToLoad);
			if (VTCustomMapManager.OnLoadedMap != null)
			{
				VTCustomMapManager.OnLoadedMap(customMapToLoad);
			}
		}
	}

	private IEnumerator ReadyRoutine(FlightSceneManager.FlightReadyContingent rc)
	{
		while (mapGenerator.IsGenerating())
		{
			yield return null;
		}
		rc.ready = true;
	}

	private void SetupMapManager(VTMapCustom map)
	{
		VTMapManager.fetch.map = map;
	}
}
