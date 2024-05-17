using System;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralIntersection : MonoBehaviour
{
	[Serializable]
	public class IntersectionNode
	{
		public Transform nodeTransform;

		public int[] vertIndices;

		[HideInInspector]
		public Vector3[] verts;

		public void UpdateVerts(MeshFilter mf)
		{
			verts = new Vector3[vertIndices.Length];
			Vector3[] vertices = mf.sharedMesh.vertices;
			for (int i = 0; i < vertIndices.Length; i++)
			{
				verts[i] = mf.transform.TransformPoint(vertices[vertIndices[i]]);
			}
		}
	}

	public List<IntersectionNode> nodes;

	public MeshFilter mesh;

	private void OnDrawGizmosSelected()
	{
		if (!mesh || nodes == null)
		{
			return;
		}
		Vector3[] vertices = mesh.sharedMesh.vertices;
		foreach (IntersectionNode node in nodes)
		{
			if (node == null || node.nodeTransform == null || node.vertIndices == null)
			{
				continue;
			}
			Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
			Gizmos.DrawSphere(node.nodeTransform.position, 0.5f);
			Gizmos.color = Color.white;
			int[] vertIndices = node.vertIndices;
			foreach (int num in vertIndices)
			{
				if (num >= 0 && num < vertices.Length)
				{
					Gizmos.DrawLine(node.nodeTransform.position, mesh.transform.TransformPoint(vertices[num]));
				}
			}
		}
	}

	public void UpdateNodeVerts()
	{
		foreach (IntersectionNode node in nodes)
		{
			node.UpdateVerts(mesh);
		}
	}
}
