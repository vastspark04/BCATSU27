using System;

namespace TerrainComposer2{

[Serializable]
public class TerrainLayerGroupItem
{
	public TC_TerrainLayerGroup terrainLayerGroup;

	public TC_TerrainLayer terrainLayer;

	public TerrainLayerGroupItem(TC_TerrainLayerGroup terrainLayerGroup, TC_TerrainLayer terrainLayer)
	{
		this.terrainLayer = terrainLayer;
		this.terrainLayerGroup = terrainLayerGroup;
	}
}
}