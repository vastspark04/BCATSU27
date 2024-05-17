using System;
using System.Collections.Generic;

[Serializable]
public class SerializableTerrainMap
{
	public int gridSize;

	public SerializableTerrainMesh[] meshes;

	public SerializableTerrainMap(List<SerializableTerrainMesh> meshList, int gridSize)
	{
		this.gridSize = gridSize;
		int count = meshList.Count;
		meshes = new SerializableTerrainMesh[count];
		for (int i = 0; i < count; i++)
		{
			meshes[i] = meshList[i];
		}
	}
}
