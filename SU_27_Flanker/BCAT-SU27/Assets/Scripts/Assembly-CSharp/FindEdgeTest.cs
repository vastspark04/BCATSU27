using UnityEngine;

public class FindEdgeTest : MonoBehaviour
{
	public Transform tf1;

	public Transform tf2;

	public MeshFilter meshFilter;

	private Vector3[] verts;

	private int[] tris;

	private void OnDrawGizmos()
	{
		if ((bool)tf1 && (bool)tf2 && (bool)meshFilter && (bool)meshFilter.sharedMesh)
		{
			if (FindEdge(out var vertA, out var vertB))
			{
				vertA = meshFilter.transform.TransformPoint(vertA);
				vertB = meshFilter.transform.TransformPoint(vertB);
				Gizmos.color = Color.green;
				Gizmos.DrawLine(vertA, vertB);
			}
			Gizmos.color = Color.gray;
			Gizmos.DrawLine(tf1.position, tf2.position);
		}
	}

	[ContextMenu("Refresh Mesh")]
	public void RefreshMesh()
	{
		verts = meshFilter.sharedMesh.vertices;
		tris = meshFilter.sharedMesh.triangles;
	}

	private bool FindEdge(out Vector3 vertA, out Vector3 vertB)
	{
		vertA = Vector3.zero;
		vertB = Vector3.zero;
		if (verts == null)
		{
			verts = meshFilter.sharedMesh.vertices;
		}
		if (tris == null)
		{
			tris = meshFilter.sharedMesh.triangles;
		}
		for (int i = 0; i < tris.Length; i += 3)
		{
			Vector3 vector = verts[tris[i]];
			Vector3 vector2 = verts[tris[i + 1]];
			Vector3 vector3 = verts[tris[i + 2]];
			if (CheckEdge(vector, vector2))
			{
				vertA = vector;
				vertB = vector2;
				return true;
			}
			if (CheckEdge(vector2, vector3))
			{
				vertA = vector2;
				vertB = vector3;
				return true;
			}
			if (CheckEdge(vector, vector3))
			{
				vertA = vector;
				vertB = vector3;
				return true;
			}
		}
		return false;
	}

	private bool CheckEdge(Vector3 a, Vector3 b)
	{
		Vector3 position = tf1.position;
		Vector3 position2 = tf2.position;
		a = meshFilter.transform.TransformPoint(a);
		b = meshFilter.transform.TransformPoint(b);
		return VTRoadSystem.SegmentsIntersect(position, position2, a, b);
	}
}
