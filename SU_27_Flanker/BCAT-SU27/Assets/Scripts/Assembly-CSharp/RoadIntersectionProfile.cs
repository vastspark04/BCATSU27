using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class RoadIntersectionProfile : ScriptableObject
{
	[Serializable]
	public class ConnectionVertIndices
	{
		public List<int> indices;
	}

	public Mesh unityMesh;

	public float intersectionRadius = 9.126f;

	public string[] connectionTypes;

	public ConnectionVertIndices[] connectionIndices;

	public List<int> bottomVerts;

	private VTTerrainMesh _intMesh;

	public VTTerrainMesh intersectionMesh
	{
		get
		{
			if (_intMesh == null)
			{
				_intMesh = new VTTerrainMesh(unityMesh);
			}
			return _intMesh;
		}
	}
}
