using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class RoadMeshProfile : ScriptableObject
{
	private class RoadVertSorter : IComparer<int>
	{
		private Vector3[] verts;

		private Vector3 refPos;

		public RoadVertSorter(Vector3[] verts, bool fromLeft)
		{
			this.verts = verts;
			if (fromLeft)
			{
				refPos = new Vector3(-10000f, -10000f, 0f);
			}
			else
			{
				refPos = new Vector3(10000f, -10000f, 0f);
			}
		}

		public int Compare(int x, int y)
		{
			return (verts[x] - refPos).sqrMagnitude.CompareTo((verts[y] - refPos).sqrMagnitude);
		}
	}

	public Mesh roadMesh;

	public float segmentLength = 20f;

	public List<int> frontVerts;

	public List<int> rearVerts;

	public List<int> bottomVerts;

	private VTTerrainMesh _segMesh;

	public VTTerrainMesh segMesh
	{
		get
		{
			if (_segMesh == null)
			{
				_segMesh = new VTTerrainMesh(roadMesh);
			}
			return _segMesh;
		}
	}

	[ContextMenu("Auto Assign FR Verts")]
	public void AutoAssignFRVerts()
	{
	}
}
