using TerrainComposer2;
using UnityEngine;

public class EditTerrain
{
	public static float GetHeight(Vector3 worldPos)
	{
		Terrain terrain = GetTerrain(worldPos);
		if (terrain == null)
		{
			return -1f;
		}
		Vector2 vector = new Vector2(worldPos.x - terrain.transform.position.x, worldPos.z - terrain.transform.position.z);
		Vector3 size = terrain.terrainData.size;
		int heightmapResolution = terrain.terrainData.heightmapResolution;
		return terrain.terrainData.GetHeight(Mathf.RoundToInt(vector.x / size.x * (float)heightmapResolution), Mathf.RoundToInt(vector.y / size.x) * heightmapResolution);
	}

	public static void SetHeight(Vector3 worldPos, float height)
	{
		Terrain terrain = GetTerrain(worldPos);
		if (!(terrain == null))
		{
			Vector2 vector = new Vector2(worldPos.x - terrain.transform.position.x, worldPos.z - terrain.transform.position.z);
			Vector3 size = terrain.terrainData.size;
			int heightmapResolution = terrain.terrainData.heightmapResolution;
			float[,] heights = new float[1, 1] { { height - terrain.transform.position.y } };
			terrain.terrainData.SetHeights(Mathf.RoundToInt(vector.x / size.x * (float)heightmapResolution), Mathf.RoundToInt(vector.y / size.x) * heightmapResolution, heights);
		}
	}

	public static Terrain GetTerrain(Vector3 worldPos)
	{
		
		return null;
	}
}
