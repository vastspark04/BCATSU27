using System.Collections.Generic;
using UnityEngine;

public class TestRemoveVert : MonoBehaviour
{
	public List<int> removeIndices;

	[ContextMenu("Test")]
	public void Test()
	{
		MeshFilter component = GetComponent<MeshFilter>();
		VTTerrainMesh vTTerrainMesh = new VTTerrainMesh(component.sharedMesh);
		vTTerrainMesh.RemoveVertices(removeIndices);
		Mesh mesh = new Mesh();
		vTTerrainMesh.ApplyToMesh(mesh);
		component.sharedMesh = mesh;
	}
}
