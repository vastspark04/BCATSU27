using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshCombiner2 : MonoBehaviour
{
	public struct MeshInfo
	{
		public Mesh mesh;

		public int subMeshIndex;

		public Transform transform;
	}

	public bool combineOnStart;

	public int frameDelays;

	public bool waitForLowOffset;

	public bool destroyNonColliders;

	public int combinedObjectLayer;

	public bool autoWeld;

	public bool autoSmoothing;

	public float autoWeldThreshold = 0.01f;

	public float autoWeldBucketStep = 1f;

	public float autoWeldEdgeSplitAngle = 30f;

	public bool createMeshColliders;

	public bool gatherMeshesFromChildren = true;

	public MeshFilter[] meshFilters;

	private bool combined;

	private static List<Mesh> createdMeshes = new List<Mesh>();

	private List<Mesh> myCreatedMeshes = new List<Mesh>();

	public bool destroyMeshesOnDestroy;

	private const int MAX_VERTS = 65530;

	private void OnDestroy()
	{
		if (!destroyMeshesOnDestroy)
		{
			return;
		}
		foreach (Mesh myCreatedMesh in myCreatedMeshes)
		{
			if ((bool)myCreatedMesh)
			{
				createdMeshes.Remove(myCreatedMesh);
				UnityEngine.Object.Destroy(myCreatedMesh);
			}
		}
	}

	private void Start()
	{
		if (combineOnStart)
		{
			if (frameDelays <= 0 && !waitForLowOffset)
			{
				CombineMeshes();
			}
			else
			{
				StartCoroutine(FrameDelayedCombine());
			}
		}
	}

	private IEnumerator FrameDelayedCombine()
	{
		for (int i = 0; i < frameDelays; i++)
		{
			yield return null;
		}
		if (waitForLowOffset)
		{
			while (base.transform.position.sqrMagnitude > 360000f)
			{
				yield return null;
			}
		}
		CombineMeshes();
	}

	private void CombineMeshes(List<MeshFilter> meshFilters, GameObject colliderObject, GameObject meshesObj, List<GameObject> outputList = null)
	{
		int count = meshFilters.Count;
		Dictionary<Material, List<MeshInfo>> dictionary = new Dictionary<Material, List<MeshInfo>>();
		for (int i = 0; i < count; i++)
		{
			MeshRenderer component = meshFilters[i].GetComponent<MeshRenderer>();
			if (!component)
			{
				continue;
			}
			for (int j = 0; j < meshFilters[i].sharedMesh.subMeshCount; j++)
			{
				MeshInfo item = default(MeshInfo);
				item.mesh = meshFilters[i].sharedMesh;
				item.subMeshIndex = j;
				item.transform = meshFilters[i].transform;
				Material key = component.sharedMaterials[j];
				component.enabled = false;
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, new List<MeshInfo>());
				}
				dictionary[key].Add(item);
			}
			Collider[] componentsInChildren = component.gameObject.GetComponentsInChildren<Collider>();
			foreach (Collider collider in componentsInChildren)
			{
				if (!collider.gameObject.GetComponent<MeshRenderer>())
				{
					collider.transform.parent = colliderObject.transform;
				}
			}
			UnityEngine.Object.DestroyImmediate(component);
			UnityEngine.Object.DestroyImmediate(meshFilters[i]);
		}
		foreach (Material key2 in dictionary.Keys)
		{
			GameObject gameObject = new GameObject(key2.name);
			gameObject.layer = combinedObjectLayer;
			gameObject.transform.parent = meshesObj.transform;
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.localRotation = Quaternion.identity;
			gameObject.transform.localScale = Vector3.one;
			MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			Mesh mesh = new Mesh();
			createdMeshes.Add(mesh);
			myCreatedMeshes.Add(mesh);
			meshRenderer.material = key2;
			meshRenderer.sharedMaterial = key2;
			meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
			meshRenderer.lightProbeUsage = LightProbeUsage.Off;
			CombineInstance[] array = new CombineInstance[dictionary[key2].Count];
			Vector3 position = base.transform.position;
			for (int l = 0; l < array.Length; l++)
			{
				MeshInfo meshInfo = dictionary[key2][l];
				meshInfo.transform.position -= position;
				array[l] = default(CombineInstance);
				array[l].mesh = meshInfo.mesh;
				array[l].subMeshIndex = meshInfo.subMeshIndex;
				array[l].transform = meshInfo.transform.localToWorldMatrix;
				meshInfo.transform.position += position;
			}
			mesh.CombineMeshes(array, mergeSubMeshes: true, useMatrices: true);
			mesh.name = gameObject.name;
			if (autoWeld)
			{
				AutoWeldEdgeSplit(mesh, autoWeldThreshold, autoWeldBucketStep, autoWeldEdgeSplitAngle);
			}
			else if (autoSmoothing)
			{
				AutoSmoothing(mesh, autoWeldThreshold, autoWeldBucketStep, autoWeldEdgeSplitAngle);
			}
			meshFilter.mesh = mesh;
			meshFilter.sharedMesh = mesh;
			if (createMeshColliders)
			{
				meshFilter.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
			}
			outputList?.Add(gameObject);
		}
		GameObject gameObject2 = new GameObject();
		if (!destroyNonColliders)
		{
			return;
		}
		Transform[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren2)
		{
			if (transform != base.transform && transform != colliderObject.transform && transform != meshesObj.transform)
			{
				if ((bool)transform.gameObject.GetComponent<Collider>())
				{
					transform.parent = colliderObject.transform;
				}
				else if (!transform.gameObject.GetComponent<MeshRenderer>())
				{
					transform.parent = gameObject2.transform;
				}
			}
		}
		UnityEngine.Object.Destroy(gameObject2);
	}

	public static int GetCombinedMeshesCount()
	{
		int num = 0;
		if (createdMeshes != null)
		{
			foreach (Mesh createdMesh in createdMeshes)
			{
				if ((bool)createdMesh)
				{
					num++;
				}
			}
			return num;
		}
		return num;
	}

	public static void DestroyAllCombinedMeshes()
	{
		int num = 0;
		if (createdMeshes != null)
		{
			foreach (Mesh createdMesh in createdMeshes)
			{
				if ((bool)createdMesh)
				{
					UnityEngine.Object.Destroy(createdMesh);
					num++;
				}
			}
			createdMeshes.Clear();
		}
		Debug.LogFormat("MeshCombiner2 destroyed {0} meshes!", num);
	}

	[ContextMenu("Combine Meshes")]
	public void CombineMeshes()
	{
		if (combined)
		{
			return;
		}
		combined = true;
		GameObject gameObject = new GameObject("Colliders");
		gameObject.transform.parent = base.transform;
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.localScale = Vector3.one;
		GameObject gameObject2 = new GameObject("Meshes");
		gameObject2.layer = combinedObjectLayer;
		gameObject2.transform.parent = base.transform;
		gameObject2.transform.localPosition = Vector3.zero;
		gameObject2.transform.localRotation = Quaternion.identity;
		gameObject2.transform.localScale = Vector3.one;
		if (gatherMeshesFromChildren)
		{
			meshFilters = GetComponentsInChildren<MeshFilter>();
		}
		int num = meshFilters.Length;
		Quaternion rotation = base.transform.rotation;
		List<List<MeshFilter>> list = new List<List<MeshFilter>>();
		List<MeshFilter> list2 = new List<MeshFilter>();
		int num2 = 0;
		list.Add(list2);
		for (int i = 0; i < num; i++)
		{
			meshFilters[i].transform.parent = base.transform;
			num2 += meshFilters[i].sharedMesh.vertexCount;
			if (num2 > 65530)
			{
				list2 = new List<MeshFilter>();
				list.Add(list2);
				num2 = meshFilters[i].sharedMesh.vertexCount;
			}
			list2.Add(meshFilters[i]);
		}
		base.transform.rotation = Quaternion.identity;
		foreach (List<MeshFilter> item in list)
		{
			CombineMeshes(item, gameObject, gameObject2);
		}
		base.transform.rotation = rotation;
	}

	public void CombineMeshesLOD(LOD[] lods, LODGroup[] lodGroups = null)
	{
		GameObject gameObject = new GameObject("Colliders");
		gameObject.transform.parent = base.transform;
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.localScale = Vector3.one;
		GameObject gameObject2 = new GameObject("Meshes");
		gameObject2.layer = combinedObjectLayer;
		gameObject2.transform.parent = base.transform;
		gameObject2.transform.localPosition = Vector3.zero;
		gameObject2.transform.localRotation = Quaternion.identity;
		gameObject2.transform.localScale = Vector3.one;
		if (lodGroups == null)
		{
			lodGroups = GetComponentsInChildren<LODGroup>();
		}
		CombineMeshesLOD(lods, gameObject, gameObject2, lodGroups);
	}

	public void CombineMeshesLOD(LOD[] lods, GameObject colliderObject, GameObject meshesObj, LODGroup[] lodGroups)
	{
		List<MeshFilter>[] array = new List<MeshFilter>[lods.Length];
		List<Renderer>[] array2 = new List<Renderer>[lods.Length];
		for (int i = 0; i < lods.Length; i++)
		{
			array[i] = new List<MeshFilter>();
			array2[i] = new List<Renderer>();
		}
		Quaternion rotation = base.transform.rotation;
		foreach (LODGroup lODGroup in lodGroups)
		{
			if (!(lODGroup != null))
			{
				continue;
			}
			if (lODGroup.lodCount != lods.Length)
			{
				Debug.LogErrorFormat("Tried to combine meshes with LODs but {0} has {1} LODs. ({2} specified).", lODGroup.gameObject.name, lODGroup.lodCount, lods.Length);
				continue;
			}
			LOD[] lODs = lODGroup.GetLODs();
			for (int k = 0; k < lODs.Length; k++)
			{
				Renderer[] renderers = lODs[k].renderers;
				foreach (Renderer renderer in renderers)
				{
					if ((bool)renderer)
					{
						array[k].Add(renderer.GetComponent<MeshFilter>());
						continue;
					}
					Debug.LogFormat("GameObject {0} is missing an mr in lods.", lODGroup.gameObject.name, lODGroup.gameObject);
				}
			}
			UnityEngine.Object.Destroy(lODGroup);
		}
		for (int m = 0; m < lods.Length; m++)
		{
			List<MeshFilter> list = array[m];
			for (int n = 0; n < list.Count; n++)
			{
				list[n].transform.parent = base.transform;
			}
		}
		base.transform.rotation = Quaternion.identity;
		List<GameObject> list2 = new List<GameObject>();
		for (int num = 0; num < lods.Length; num++)
		{
			int num2 = 0;
			List<MeshFilter> list3 = array[num];
			List<List<MeshFilter>> list4 = new List<List<MeshFilter>>();
			List<MeshFilter> list5 = new List<MeshFilter>();
			list4.Add(list5);
			for (int num3 = 0; num3 < list3.Count; num3++)
			{
				num2 += list3[num3].sharedMesh.vertexCount;
				if (num2 > 65530)
				{
					list5 = new List<MeshFilter>();
					list4.Add(list5);
					num2 = list3[num3].sharedMesh.vertexCount;
				}
				list5.Add(list3[num3]);
			}
			foreach (List<MeshFilter> item in list4)
			{
				CombineMeshes(item, colliderObject, meshesObj, list2);
				foreach (GameObject item2 in list2)
				{
					MeshRenderer component = item2.GetComponent<MeshRenderer>();
					array2[num].Add(component);
				}
				list2.Clear();
			}
			lods[num].renderers = array2[num].ToArray();
		}
		base.transform.rotation = rotation;
	}

	public static void AutoWeld(Mesh mesh, float threshold, float bucketStep)
	{
		Vector3[] vertices = mesh.vertices;
		Vector3[] array = new Vector3[vertices.Length];
		int[] array2 = new int[vertices.Length];
		int num = 0;
		Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		for (int i = 0; i < vertices.Length; i++)
		{
			if (vertices[i].x < vector.x)
			{
				vector.x = vertices[i].x;
			}
			if (vertices[i].y < vector.y)
			{
				vector.y = vertices[i].y;
			}
			if (vertices[i].z < vector.z)
			{
				vector.z = vertices[i].z;
			}
			if (vertices[i].x > vector2.x)
			{
				vector2.x = vertices[i].x;
			}
			if (vertices[i].y > vector2.y)
			{
				vector2.y = vertices[i].y;
			}
			if (vertices[i].z > vector2.z)
			{
				vector2.z = vertices[i].z;
			}
		}
		int num2 = Mathf.FloorToInt((vector2.x - vector.x) / bucketStep) + 1;
		int num3 = Mathf.FloorToInt((vector2.y - vector.y) / bucketStep) + 1;
		int num4 = Mathf.FloorToInt((vector2.z - vector.z) / bucketStep) + 1;
		List<int>[,,] array3 = new List<int>[num2, num3, num4];
		for (int j = 0; j < vertices.Length; j++)
		{
			int num5 = Mathf.FloorToInt((vertices[j].x - vector.x) / bucketStep);
			int num6 = Mathf.FloorToInt((vertices[j].y - vector.y) / bucketStep);
			int num7 = Mathf.FloorToInt((vertices[j].z - vector.z) / bucketStep);
			if (array3[num5, num6, num7] == null)
			{
				array3[num5, num6, num7] = new List<int>();
			}
			int num8 = 0;
			while (true)
			{
				if (num8 < array3[num5, num6, num7].Count)
				{
					if (Vector3.SqrMagnitude(array[array3[num5, num6, num7][num8]] - vertices[j]) < threshold)
					{
						array2[j] = array3[num5, num6, num7][num8];
						break;
					}
					num8++;
					continue;
				}
				array[num] = vertices[j];
				array3[num5, num6, num7].Add(num);
				array2[j] = num;
				num++;
				break;
			}
		}
		int[] triangles = mesh.triangles;
		int[] array4 = new int[triangles.Length];
		for (int k = 0; k < triangles.Length; k++)
		{
			array4[k] = array2[triangles[k]];
		}
		Vector3[] array5 = new Vector3[num];
		for (int l = 0; l < num; l++)
		{
			array5[l] = array[l];
		}
		mesh.Clear();
		mesh.vertices = array5;
		mesh.triangles = array4;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

	public static void AutoWeldEdgeSplit(Mesh mesh, float threshold, float bucketStep, float angleLimit)
	{
		Vector3[] vertices = mesh.vertices;
		Vector3[] normals = mesh.normals;
		Vector3[] array = new Vector3[vertices.Length];
		Vector3[] array2 = new Vector3[normals.Length];
		Vector2[] uv = mesh.uv;
		Vector2[] array3 = new Vector2[uv.Length];
		int[] array4 = new int[vertices.Length];
		int num = 0;
		Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		for (int i = 0; i < vertices.Length; i++)
		{
			if (vertices[i].x < vector.x)
			{
				vector.x = vertices[i].x;
			}
			if (vertices[i].y < vector.y)
			{
				vector.y = vertices[i].y;
			}
			if (vertices[i].z < vector.z)
			{
				vector.z = vertices[i].z;
			}
			if (vertices[i].x > vector2.x)
			{
				vector2.x = vertices[i].x;
			}
			if (vertices[i].y > vector2.y)
			{
				vector2.y = vertices[i].y;
			}
			if (vertices[i].z > vector2.z)
			{
				vector2.z = vertices[i].z;
			}
		}
		int num2 = Mathf.FloorToInt((vector2.x - vector.x) / bucketStep) + 1;
		int num3 = Mathf.FloorToInt((vector2.y - vector.y) / bucketStep) + 1;
		int num4 = Mathf.FloorToInt((vector2.z - vector.z) / bucketStep) + 1;
		List<int>[,,] array5 = new List<int>[num2, num3, num4];
		for (int j = 0; j < vertices.Length; j++)
		{
			int num5 = Mathf.FloorToInt((vertices[j].x - vector.x) / bucketStep);
			int num6 = Mathf.FloorToInt((vertices[j].y - vector.y) / bucketStep);
			int num7 = Mathf.FloorToInt((vertices[j].z - vector.z) / bucketStep);
			if (array5[num5, num6, num7] == null)
			{
				array5[num5, num6, num7] = new List<int>();
			}
			bool flag = true;
			for (int k = 0; k < array5[num5, num6, num7].Count && flag; k++)
			{
				Vector3 vector3 = array[array5[num5, num6, num7][k]] - vertices[j];
				float num8 = Vector3.Angle(normals[j], array2[array5[num5, num6, num7][k]]);
				if (Vector3.SqrMagnitude(vector3) < threshold && num8 < angleLimit)
				{
					array4[j] = array5[num5, num6, num7][k];
					flag = false;
				}
			}
			if (flag)
			{
				array[num] = vertices[j];
				array2[num] = normals[j];
				array3[num] = uv[j];
				array5[num5, num6, num7].Add(num);
				array4[j] = num;
				num++;
			}
		}
		int[] triangles = mesh.triangles;
		int[] array6 = new int[triangles.Length];
		for (int l = 0; l < triangles.Length; l++)
		{
			array6[l] = array4[triangles[l]];
		}
		Vector3[] array7 = new Vector3[num];
		Vector2[] array8 = new Vector2[num];
		for (int m = 0; m < num; m++)
		{
			array7[m] = array[m];
			array8[m] = array3[m];
		}
		mesh.Clear();
		mesh.vertices = array7;
		mesh.uv = array8;
		mesh.triangles = array6;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

	public static void AutoSmoothing(Mesh mesh, float threshold, float bucketStep, float angleLimit)
	{
		Vector3[] vertices = mesh.vertices;
		Vector3[] array = new Vector3[vertices.Length];
		Vector3[] normals = mesh.normals;
		Vector3[] array2 = new Vector3[normals.Length];
		int num = 0;
		Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		for (int i = 0; i < vertices.Length; i++)
		{
			if (vertices[i].x < vector.x)
			{
				vector.x = vertices[i].x;
			}
			if (vertices[i].y < vector.y)
			{
				vector.y = vertices[i].y;
			}
			if (vertices[i].z < vector.z)
			{
				vector.z = vertices[i].z;
			}
			if (vertices[i].x > vector2.x)
			{
				vector2.x = vertices[i].x;
			}
			if (vertices[i].y > vector2.y)
			{
				vector2.y = vertices[i].y;
			}
			if (vertices[i].z > vector2.z)
			{
				vector2.z = vertices[i].z;
			}
		}
		int num2 = Mathf.FloorToInt((vector2.x - vector.x) / bucketStep) + 1;
		int num3 = Mathf.FloorToInt((vector2.y - vector.y) / bucketStep) + 1;
		int num4 = Mathf.FloorToInt((vector2.z - vector.z) / bucketStep) + 1;
		List<int>[,,] array3 = new List<int>[num2, num3, num4];
		for (int j = 0; j < vertices.Length; j++)
		{
			int num5 = Mathf.FloorToInt((vertices[j].x - vector.x) / bucketStep);
			int num6 = Mathf.FloorToInt((vertices[j].y - vector.y) / bucketStep);
			int num7 = Mathf.FloorToInt((vertices[j].z - vector.z) / bucketStep);
			if (array3[num5, num6, num7] == null)
			{
				array3[num5, num6, num7] = new List<int>();
			}
			bool flag = true;
			for (int k = 0; k < array3[num5, num6, num7].Count && flag; k++)
			{
				Vector3 vector3 = array[array3[num5, num6, num7][k]] - vertices[j];
				Vector3 vector4 = normals[j];
				Vector3 vector5 = array2[array3[num5, num6, num7][k]];
				float num8 = Vector3.Angle(vector4, vector5);
				if (Vector3.SqrMagnitude(vector3) < threshold && num8 < angleLimit)
				{
					normals[j] = (vector4 + vector5).normalized;
					array2[array3[num5, num6, num7][k]] = normals[j];
				}
			}
			if (flag)
			{
				array[num] = vertices[j];
				array2[num] = normals[j];
				array3[num5, num6, num7].Add(num);
				num++;
			}
		}
		array3 = null;
		GC.Collect();
		int[] triangles = mesh.triangles;
		Vector2[] uv = mesh.uv;
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh.normals = array2;
		mesh.RecalculateBounds();
	}
}
