using System.Collections.Generic;
using UnityEngine;

public class MeshCombiner : MonoBehaviour
{
	public bool combineOnStart;

	private MeshFilter[] meshes;

	private void Start()
	{
		if (combineOnStart)
		{
			CombineMeshes();
		}
	}

	public void CombineMeshes()
	{
		meshes = GetComponentsInChildren<MeshFilter>();
		Dictionary<Material, List<MeshFilter>> dictionary = new Dictionary<Material, List<MeshFilter>>();
		for (int i = 0; i < meshes.Length; i++)
		{
			Material sharedMaterial = meshes[i].GetComponent<MeshRenderer>().sharedMaterial;
			if (!dictionary.ContainsKey(sharedMaterial))
			{
				dictionary.Add(sharedMaterial, new List<MeshFilter>());
			}
			dictionary[sharedMaterial].Add(meshes[i]);
		}
		foreach (Material key in dictionary.Keys)
		{
			CombineMeshesForMaterial(key, dictionary[key].ToArray());
		}
	}

	private void CombineMeshesForMaterial(Material mat, MeshFilter[] meshes)
	{
		GameObject obj = new GameObject("Submesh");
		obj.transform.parent = base.transform;
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localRotation = Quaternion.identity;
		obj.transform.localScale = Vector3.one;
		MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
		MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		meshRenderer.material = mat;
		CombineInstance[] array = new CombineInstance[meshes.Length];
		Vector3 vector = meshes[0].transform.position + (base.transform.position - meshes[0].transform.position);
		for (int i = 0; i < meshes.Length; i++)
		{
			if (!(meshes[i].GetComponent<MeshRenderer>().sharedMaterial != mat))
			{
				meshes[i].transform.parent = base.transform;
				meshes[i].transform.position -= vector;
				array[i] = default(CombineInstance);
				array[i].mesh = meshes[i].mesh;
				array[i].subMeshIndex = 0;
				array[i].transform = meshes[i].transform.localToWorldMatrix;
				meshes[i].transform.position += vector;
				meshes[i].GetComponent<MeshRenderer>().enabled = false;
			}
		}
		mesh.CombineMeshes(array, mergeSubMeshes: true, useMatrices: true);
		mesh.name = mat.name + " Mesh";
		meshFilter.mesh = mesh;
	}
}
