using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("FastNoise/FastNoise Mesh Warp", 3)]
public class FastNoiseMeshWarp : MonoBehaviour
{
	public FastNoiseUnity fastNoiseUnity;

	public bool fractal;

	private Dictionary<GameObject, Mesh> originalMeshes = new Dictionary<GameObject, Mesh>();

	private void Start()
	{
		WarpAllMeshes();
	}

	public void WarpAllMeshes()
	{
		MeshFilter[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshFilter>();
		foreach (MeshFilter meshFilter in componentsInChildren)
		{
			WarpMesh(meshFilter);
		}
	}

	public void WarpMesh(MeshFilter meshFilter)
	{
		if (meshFilter.sharedMesh == null)
		{
			return;
		}
		Vector3 vector = meshFilter.gameObject.transform.position - base.gameObject.transform.position;
		Vector3[] vertices;
		if (originalMeshes.ContainsKey(meshFilter.gameObject))
		{
			vertices = originalMeshes[meshFilter.gameObject].vertices;
		}
		else
		{
			originalMeshes[meshFilter.gameObject] = meshFilter.sharedMesh;
			vertices = meshFilter.sharedMesh.vertices;
		}
		double decimalType = FastNoise.GetDecimalType();
		double num = decimalType;
		double num2 = decimalType;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] += vector;
			decimalType = vertices[i].x;
			num = vertices[i].y;
			num2 = vertices[i].z;
			if (fractal)
			{
				fastNoiseUnity.fastNoise.GradientPerturbFractal(ref decimalType, ref num, ref num2);
			}
			else
			{
				fastNoiseUnity.fastNoise.GradientPerturb(ref decimalType, ref num, ref num2);
			}
			vertices[i].Set((float)decimalType, (float)num, (float)num2);
			vertices[i] -= vector;
		}
		meshFilter.mesh.vertices = vertices;
		meshFilter.mesh.RecalculateNormals();
		meshFilter.mesh.RecalculateBounds();
	}
}
