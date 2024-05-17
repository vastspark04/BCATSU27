using TerrainComposer2;
using UnityEngine;

public class LoadTerrainLayer : MonoBehaviour
{
	public GameObject terrainLayerPrefab;

	public bool generateOnStart;

	public bool instantGenerate;

	private void Start()
	{
		InstantiateTerrainLayer();
		if (generateOnStart)
		{
			
		}
	}

	public void InstantiateTerrainLayer()
	{
		if (terrainLayerPrefab == null || terrainLayerPrefab.GetComponent<TC_TerrainLayer>() == null)
		{
			return;
		}
		
	}
}
