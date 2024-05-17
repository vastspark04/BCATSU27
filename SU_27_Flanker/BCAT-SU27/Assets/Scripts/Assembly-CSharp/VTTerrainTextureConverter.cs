using System;
using System.IO;
using UnityEngine;

public static class VTTerrainTextureConverter
{
	public static void SaveTexture(VTMapGenerator generator)
	{
		double metersPerPixel = GetMetersPerPixel(generator);
		int num = Mathf.FloorToInt(generator.chunkSize / (float)metersPerPixel) * generator.gridSize;
		float chunkSize = generator.chunkSize;
		float num2 = 8000f;
		Texture2D texture2D = new Texture2D(num, num);
		for (int i = 0; i < generator.gridSize; i++)
		{
			for (int j = 0; j < generator.gridSize; j++)
			{
				VTTerrainMesh vTTerrainMesh = generator.GetTerrainChunk(i, j).terrainMeshes[0];
				for (int k = 0; k < vTTerrainMesh.verts.Count; k++)
				{
					Vector3 vert = vTTerrainMesh.verts[k];
					IntVector2 intVector = VertToPixel(vert, i, j, chunkSize, generator.gridSize, 20);
					if (intVector.x < num && intVector.y < num)
					{
						float num3 = vert.z / num2;
						_ = vTTerrainMesh.normals[k];
						texture2D.SetPixel(intVector.x, intVector.y, new Color(num3, num3, vTTerrainMesh.treeValues[k], 1f));
					}
				}
			}
		}
		texture2D.Apply();
		byte[] bytes = texture2D.EncodeToPNG();
		UnityEngine.Object.Destroy(texture2D);
		File.WriteAllBytes(Path.Combine(Path.GetFullPath("."), "test.png"), bytes);
	}

	public static double GetMetersPerPixel(VTMapGenerator generator)
	{
		double num = Math.Round(Math.Sqrt(generator.terrainProfiles[0].mesh.vertexCount) - 3.0);
		return (double)generator.chunkSize / num;
	}

	public static IntVector2 VertToPixel(Vector3 vert, int chunkX, int chunkY, float chunkSize, int gridSize, int vertsPerSide)
	{
		IntVector2 intVector = new IntVector2(vertsPerSide * chunkX, vertsPerSide * chunkY);
		float num = chunkSize / (float)vertsPerSide;
		return intVector + new IntVector2(Mathf.RoundToInt((0f - vert.x) / num), Mathf.RoundToInt(vert.y / num));
	}

	private static Vector2 VertToPixelInterp(Vector3 vert, int chunkX, int chunkY, float chunkSize, double metersPerPixel)
	{
		double num = (double)((float)chunkX * chunkSize) - (double)vert.x;
		double num2 = (double)((float)chunkY * chunkSize) + (double)vert.y;
		float x = (float)(num / metersPerPixel);
		float y = (float)(num2 / metersPerPixel);
		return new Vector2(x, y);
	}

	public static void ApplyToMesh(Texture2D tex, VTTerrainMesh mesh, IntVector2 chunkCoord, float chunkSize, double metersPerPixel, float maxHeight)
	{
		for (int i = 0; i < mesh.verts.Count; i++)
		{
			Vector3 vector = mesh.verts[i];
			Vector2 vector2 = VertToPixelInterp(vector, chunkCoord.x, chunkCoord.y, chunkSize, metersPerPixel) / tex.width;
			Color pixelBilinear = tex.GetPixelBilinear(vector2.x, vector2.y);
			vector.z = pixelBilinear.r * maxHeight;
			mesh.verts[i] = vector;
			mesh.treeValues[i] = pixelBilinear.b;
		}
	}
}
