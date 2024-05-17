using UnityEngine;

[ExecuteInEditMode]
public class VertexIndexGizmo : MonoBehaviour
{
	private Mesh mesh;

	public float labelLength = 0.25f;

	[HideInInspector]
	public Vector3[] verts;

	private void OnEnable()
	{
		MeshFilter component = GetComponent<MeshFilter>();
		if ((bool)component)
		{
			mesh = component.sharedMesh;
			verts = mesh.vertices;
		}
	}
}
