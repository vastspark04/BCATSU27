using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTexTerrainLoader : MonoBehaviour
{
	public Texture2D terrainTexture;

	public Mesh chunkMeshTemplate;

	public float chunkSize;

	public float maxHeight;

	public int gridSize = 16;

	public float heightAdjust = -20f;

	private VTTerrainMesh tMeshTemplate;

	public Material mat;

	private List<Transform> terrainTfs;

	private void OnShift(Vector3 offset)
	{
		for (int i = 0; i < terrainTfs.Count; i++)
		{
			terrainTfs[i].position += offset;
		}
	}

	private IEnumerator Start()
	{
		terrainTfs = new List<Transform>(gridSize * gridSize);
		FloatingOrigin.instance.OnOriginShift += OnShift;
		tMeshTemplate = new VTTerrainMesh(chunkMeshTemplate);
		double num = Math.Round(Math.Sqrt(tMeshTemplate.vertCount) - 3.0);
		double metersPerPixel = (double)chunkSize / num;
		Vector3[] tempNormals = new Vector3[tMeshTemplate.vertCount];
		FastNoise noiseModule = new FastNoise(1345);
		VTTArchipelago.ColorProfile profile = new VTTArchipelago.ColorProfile();
		VTTerrainMesh[,] tMeshes = new VTTerrainMesh[gridSize, gridSize];
		for (int i = 0; i < gridSize; i++)
		{
			for (int j = 0; j < gridSize; j++)
			{
				IntVector2 chunkCoord = new IntVector2(i, j);
				VTTerrainMesh vTTerrainMesh = (tMeshes[i, j] = tMeshTemplate.Copy());
				VTTerrainTextureConverter.ApplyToMesh(terrainTexture, vTTerrainMesh, chunkCoord, chunkSize, metersPerPixel, maxHeight);
				for (int k = 0; k < vTTerrainMesh.vertCount; k++)
				{
					Vector3 value = vTTerrainMesh.verts[k];
					value.z += heightAdjust;
					Vector3 vector = new Vector3((float)i * chunkSize, 0f, (float)j * chunkSize) + new Vector3(0f - value.x, 0f, value.y);
					Vector3 vector2 = (float)(gridSize / 2) * chunkSize * new Vector3(1f, 0f, 1f);
					float t = Mathf.Pow((vector - vector2).magnitude / (chunkSize * (float)gridSize / 2f), 6f * Mathf.Clamp01(value.z / maxHeight));
					value.z = Mathf.Lerp(value.z, heightAdjust, t);
					vTTerrainMesh.verts[k] = value;
				}
				VTTerrainJob.RecalculateNormals(vTTerrainMesh, tempNormals);
				for (int l = 0; l < vTTerrainMesh.vertCount; l++)
				{
					Vector3 vert = vTTerrainMesh.verts[l];
					Vector2 uV = new Vector2((0f - vert.x) / chunkSize + (float)i, vert.y / chunkSize + (float)j);
					vTTerrainMesh.colors[l] = VTTArchipelago.CalculateColor(vert, vTTerrainMesh.normals[l], uV, noiseModule, out var _, profile);
				}
			}
		}
		base.transform.position += new Vector3(0f, 0f, 1f);
		for (int x = 0; x < gridSize; x++)
		{
			for (int m = 0; m < gridSize; m++)
			{
				GameObject gameObject = new GameObject(x + ", " + m);
				gameObject.transform.position = new Vector3((float)x * chunkSize, 0f, (float)m * chunkSize);
				MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
				MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
				Mesh unityMesh = (meshFilter.sharedMesh = new Mesh());
				meshRenderer.sharedMaterial = mat;
				terrainTfs.Add(gameObject.transform);
				tMeshes[x, m].ApplyToMesh(unityMesh);
				gameObject.transform.parent = base.transform;
				gameObject.transform.rotation = Quaternion.Euler(-90f, 0f, 180f);
			}
			yield return null;
		}
		base.transform.position += new Vector3(0f, 0f, -1f);
	}
}
