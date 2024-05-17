using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class FixSeamNormals : MonoBehaviour
{
	private class MeshData
	{
		public Vector3[] verts;

		public Vector3[] normals;

		public List<int> edgeIndices;

		public MeshData(Mesh mesh, float vertBoundThresh)
		{
			verts = mesh.vertices;
			normals = mesh.normals;
			edgeIndices = new List<int>();
			for (int i = 0; i < verts.Length; i++)
			{
				if (!(Mathf.Abs(verts[i].x) < vertBoundThresh) || !(Mathf.Abs(verts[i].y) < vertBoundThresh))
				{
					edgeIndices.Add(i);
				}
			}
		}
	}

	public List<MeshFilter> meshes;

	public string meshNameFilter;

	public float vertexDistanceThreshold;

	public bool fixPositions = true;

	[ContextMenu("Get Meshes From Children")]
	public void GetMeshesFromChildren()
	{
		meshes = new List<MeshFilter>();
		MeshFilter[] componentsInChildren = GetComponentsInChildren<MeshFilter>();
		foreach (MeshFilter meshFilter in componentsInChildren)
		{
			if (string.IsNullOrEmpty(meshNameFilter) || meshFilter.gameObject.name.Contains(meshNameFilter))
			{
				meshes.Add(meshFilter);
			}
		}
	}

	[ContextMenu("Fix Normals")]
	public void FixNormals()
	{
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		Dictionary<string, MeshData> dictionary2 = new Dictionary<string, MeshData>();
		foreach (MeshFilter mesh in meshes)
		{
			float x = mesh.sharedMesh.bounds.extents.x;
			float num = x * 2.2f;
			float num2 = num * num;
			float vertBoundThresh = x * 0.99f;
			MeshData meshData;
			if (!dictionary2.ContainsKey(mesh.sharedMesh.name))
			{
				meshData = new MeshData(mesh.sharedMesh, vertBoundThresh);
				dictionary2.Add(mesh.sharedMesh.name, meshData);
			}
			else
			{
				meshData = dictionary2[mesh.sharedMesh.name];
			}
			dictionary.Add(mesh.sharedMesh.name, new List<string>());
			foreach (MeshFilter mesh2 in meshes)
			{
				if (mesh2 == mesh || !((mesh2.transform.position - mesh.transform.position).sqrMagnitude < num2) || (dictionary.ContainsKey(mesh2.sharedMesh.name) && dictionary[mesh2.sharedMesh.name].Contains(mesh.sharedMesh.name)))
				{
					continue;
				}
				dictionary[mesh.sharedMesh.name].Add(mesh2.sharedMesh.name);
				MeshData meshData2;
				if (!dictionary2.ContainsKey(mesh2.sharedMesh.name))
				{
					meshData2 = new MeshData(mesh2.sharedMesh, vertBoundThresh);
					dictionary2.Add(mesh2.sharedMesh.name, meshData2);
				}
				else
				{
					meshData2 = dictionary2[mesh2.sharedMesh.name];
				}
				foreach (int edgeIndex in meshData.edgeIndices)
				{
					Vector3 vector = mesh.transform.TransformPoint(meshData.verts[edgeIndex]);
					foreach (int edgeIndex2 in meshData2.edgeIndices)
					{
						Vector3 position = mesh2.transform.TransformPoint(meshData2.verts[edgeIndex2]);
						if (!(Mathf.Abs(position.x - vector.x) > vertexDistanceThreshold) && !(Mathf.Abs(position.z - vector.z) > vertexDistanceThreshold))
						{
							if (fixPositions)
							{
								meshData.verts[edgeIndex] = mesh.transform.InverseTransformPoint(position);
							}
							Vector3 normalized = (meshData.normals[edgeIndex] + meshData2.normals[edgeIndex2]).normalized;
							meshData.normals[edgeIndex] = normalized;
							meshData2.normals[edgeIndex2] = normalized;
						}
					}
				}
			}
		}
		foreach (MeshFilter mesh3 in meshes)
		{
			MeshData meshData3 = dictionary2[mesh3.sharedMesh.name];
			mesh3.sharedMesh.vertices = meshData3.verts;
			mesh3.sharedMesh.normals = meshData3.normals;
		}
	}

	public void FixNormalsAsync(out AsyncOpStatus status)
	{
		status = new AsyncOpStatus();
		StartCoroutine(FixAsyncRoutine(status));
	}

	private IEnumerator FixAsyncRoutine(AsyncOpStatus status)
	{
		Dictionary<string, List<string>> completedPairs = new Dictionary<string, List<string>>();
		Dictionary<string, MeshData> meshCache = new Dictionary<string, MeshData>();
		Stopwatch timer = new Stopwatch();
		timer.Start();
		status.progress = 0f;
		status.isDone = false;
		float total = meshes.Count;
		float meshesComplete = 0f;
		foreach (MeshFilter targetMesh in meshes)
		{
			float x = targetMesh.sharedMesh.bounds.extents.x;
			float num = x * 2.2f;
			float radiusSqr = num * num;
			float vertBoundThresh = x * 0.99f;
			MeshData targetMd;
			if (!meshCache.ContainsKey(targetMesh.sharedMesh.name))
			{
				targetMd = new MeshData(targetMesh.sharedMesh, vertBoundThresh);
				meshCache.Add(targetMesh.sharedMesh.name, targetMd);
			}
			else
			{
				targetMd = meshCache[targetMesh.sharedMesh.name];
			}
			completedPairs.Add(targetMesh.sharedMesh.name, new List<string>());
			foreach (MeshFilter mesh in meshes)
			{
				if (mesh == targetMesh || !((mesh.transform.position - targetMesh.transform.position).sqrMagnitude < radiusSqr) || (completedPairs.ContainsKey(mesh.sharedMesh.name) && completedPairs[mesh.sharedMesh.name].Contains(targetMesh.sharedMesh.name)))
				{
					continue;
				}
				completedPairs[targetMesh.sharedMesh.name].Add(mesh.sharedMesh.name);
				MeshData meshData;
				if (!meshCache.ContainsKey(mesh.sharedMesh.name))
				{
					meshData = new MeshData(mesh.sharedMesh, vertBoundThresh);
					meshCache.Add(mesh.sharedMesh.name, meshData);
				}
				else
				{
					meshData = meshCache[mesh.sharedMesh.name];
				}
				foreach (int edgeIndex in targetMd.edgeIndices)
				{
					Vector3 vector = targetMesh.transform.TransformPoint(targetMd.verts[edgeIndex]);
					foreach (int edgeIndex2 in meshData.edgeIndices)
					{
						Vector3 vector2 = mesh.transform.TransformPoint(meshData.verts[edgeIndex2]);
						if (!(Mathf.Abs(vector2.x - vector.x) > vertexDistanceThreshold) && !(Mathf.Abs(vector2.y - vector.y) > vertexDistanceThreshold) && !(Mathf.Abs(vector2.z - vector.z) > vertexDistanceThreshold))
						{
							Vector3 normalized = (targetMd.normals[edgeIndex] + meshData.normals[edgeIndex2]).normalized;
							targetMd.normals[edgeIndex] = normalized;
							meshData.normals[edgeIndex2] = normalized;
						}
					}
				}
				if (timer.ElapsedMilliseconds > 5)
				{
					yield return null;
					timer.Reset();
				}
			}
			meshesComplete += 1f;
			status.progress = meshesComplete / total;
			if (timer.ElapsedMilliseconds > 5)
			{
				yield return null;
				timer.Reset();
			}
		}
		foreach (MeshFilter mesh2 in meshes)
		{
			MeshData meshData2 = meshCache[mesh2.sharedMesh.name];
			mesh2.sharedMesh.vertices = meshData2.verts;
			mesh2.sharedMesh.normals = meshData2.normals;
			if (timer.ElapsedMilliseconds > 5)
			{
				yield return null;
				timer.Reset();
			}
		}
		status.progress = 1f;
		status.isDone = true;
	}
}
