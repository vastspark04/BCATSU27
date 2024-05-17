using UnityEngine;

public class SwapSubmeshes : MonoBehaviour
{
	[ContextMenu("Swap")]
	public void Swap()
	{
		Mesh sharedMesh = GetComponent<MeshFilter>().sharedMesh;
		int[] triangles = sharedMesh.GetTriangles(0);
		int[] triangles2 = sharedMesh.GetTriangles(1);
		sharedMesh.SetTriangles(triangles2, 0);
		sharedMesh.SetTriangles(triangles, 1);
	}

	[ContextMenu("SaveAs")]
	public void SaveAs()
	{
	}
}
