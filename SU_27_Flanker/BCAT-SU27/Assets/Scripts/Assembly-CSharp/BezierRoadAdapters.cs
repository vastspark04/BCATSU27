using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BezierRoadAdapters : ScriptableObject
{
	[Serializable]
	public class Adapter
	{
		public int from;

		public int to;

		public Mesh mesh;

		public Mesh bridgeMesh;
	}

	public List<Adapter> adapters;

	private Dictionary<IntVector2, VTTerrainMesh> adaptDict;

	private Dictionary<IntVector2, VTTerrainMesh> bridgeAdaptDict;

	private void BuildDictionary()
	{
		adaptDict = new Dictionary<IntVector2, VTTerrainMesh>();
		bridgeAdaptDict = new Dictionary<IntVector2, VTTerrainMesh>();
		foreach (Adapter adapter in adapters)
		{
			IntVector2 key = new IntVector2(adapter.from, adapter.to);
			VTTerrainMesh value = new VTTerrainMesh(adapter.mesh);
			VTTerrainMesh value2 = new VTTerrainMesh(adapter.bridgeMesh);
			adaptDict.Add(key, value);
			bridgeAdaptDict.Add(key, value2);
		}
	}

	public VTTerrainMesh GetAdapter(int from, int to, bool bridge, out bool reverse)
	{
		if (adaptDict == null)
		{
			BuildDictionary();
		}
		Dictionary<IntVector2, VTTerrainMesh> dictionary = (bridge ? bridgeAdaptDict : adaptDict);
		VTTerrainMesh value = null;
		reverse = false;
		if (!dictionary.TryGetValue(new IntVector2(from, to), out value) && dictionary.TryGetValue(new IntVector2(to, from), out value))
		{
			reverse = true;
		}
		return value;
	}
}
