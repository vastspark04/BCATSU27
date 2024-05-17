using UnityEngine;

public class TestMapTree : MonoBehaviour
{
	public Transform pos0Tf;

	public Transform pos1tf;

	public Mesh modelMesh;

	public Mesh billboardMesh;

	public Material mat;

	public Transform rendererTf;

	private int vtxOffset;

	private void Start()
	{
		Mesh mesh = new Mesh();
		VTTerrainMesh vTTerrainMesh = new VTTerrainMesh();
		CreateTree(new VTTerrainMesh(modelMesh), vTTerrainMesh, rendererTf.InverseTransformPoint(pos0Tf.position), billboard: false, ref vtxOffset);
		CreateTree(new VTTerrainMesh(billboardMesh), vTTerrainMesh, rendererTf.InverseTransformPoint(pos1tf.position), billboard: true, ref vtxOffset);
		vTTerrainMesh.ApplyToMesh(mesh);
		MeshRenderer meshRenderer = rendererTf.gameObject.AddComponent<MeshRenderer>();
		rendererTf.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		meshRenderer.material = mat;
	}

	private void CreateTree(VTTerrainMesh treeMesh, VTTerrainMesh masterMesh, Vector3 position, bool billboard, ref int vtxOffset)
	{
		for (int i = 0; i < treeMesh.vertCount; i++)
		{
			Vector3 vector = treeMesh.verts[i];
			masterMesh.verts.Add(2f * vector + position);
			masterMesh.uvs.Add(treeMesh.uvs[i]);
			Vector3 vector2 = treeMesh.normals[i];
			if (billboard)
			{
				Vector3 vector3 = new Vector3(0.15f * vector.x, 0.2f * vector.y + 4f, 0.15f * vector.z);
				vector2 = (vector2 + vector3).normalized;
			}
			masterMesh.normals.Add(vector2);
			Vector4 item = new Vector4(position.x, position.y, position.z, billboard ? 1 : 0);
			masterMesh.tangents.Add(item);
		}
		for (int j = 0; j < treeMesh.triangles.Count; j++)
		{
			masterMesh.triangles.Add(treeMesh.triangles[j] + vtxOffset);
		}
		vtxOffset += treeMesh.vertCount;
	}
}
