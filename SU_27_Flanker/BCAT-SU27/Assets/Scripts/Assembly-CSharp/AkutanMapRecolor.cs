using System.Collections.Generic;
using UnityEngine;

public class AkutanMapRecolor : MonoBehaviour
{
	public List<MeshFilter> meshFilters;

	public float uvScale;

	public int noiseSeed;

	public VTTArchipelago.ColorProfile colorProfile;

	[ContextMenu("Recolor")]
	public void Recolor()
	{
		FastNoise noiseModule = new FastNoise(noiseSeed);
		foreach (MeshFilter meshFilter in meshFilters)
		{
			Vector3[] vertices = meshFilter.sharedMesh.vertices;
			Vector3[] normals = meshFilter.sharedMesh.normals;
			Vector2[] uv = meshFilter.sharedMesh.uv;
			Color[] colors = meshFilter.sharedMesh.colors;
			for (int i = 0; i < vertices.Length; i++)
			{
				Vector2 uV = uv[i] * uvScale;
				float treeValue;
				Color color = (colors[i] = VTTArchipelago.CalculateColor(vertices[i], normals[i], uV, noiseModule, out treeValue, colorProfile));
			}
			meshFilter.sharedMesh.colors = colors;
		}
	}

	[ContextMenu("Reset Origin Offset")]
	public void ResetOriginOffset()
	{
		Shader.SetGlobalVector("_GlobalOriginOffset", Vector3.zero);
	}
}
