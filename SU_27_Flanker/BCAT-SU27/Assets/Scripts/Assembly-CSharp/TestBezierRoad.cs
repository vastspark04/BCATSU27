using UnityEngine;

public class TestBezierRoad : MonoBehaviour
{
	public BezierTest bezier;

	public Mesh roadmesh_shared;

	public Material roadMat;

	public float radius;

	private MeshFilter mf;

	private MeshRenderer mr;

	[ContextMenu("Test")]
	public void Test()
	{
		if (!mr)
		{
			mr = base.gameObject.GetComponent<MeshRenderer>();
			if (!mr)
			{
				mr = base.gameObject.AddComponent<MeshRenderer>();
			}
		}
		mr.material = roadMat;
		if (!mf)
		{
			mf = GetComponent<MeshFilter>();
			if (!mf)
			{
				mf = base.gameObject.AddComponent<MeshFilter>();
			}
		}
		VTTerrainMesh vTTerrainMesh = new VTTerrainMesh(roadmesh_shared);
		BezierCurve bezierCurve = new BezierCurve(bezier.startPt.position, bezier.midPt.position, bezier.endPt.position);
		float num = bezierCurve.EstimateLength(10);
		for (int i = 0; i < vTTerrainMesh.vertCount; i++)
		{
			Vector3 vector = vTTerrainMesh.verts[i];
			float z = vector.z;
			Vector3 point = bezierCurve.GetPoint(z);
			Vector3 vector2 = base.transform.InverseTransformPoint(point);
			Vector3 vector3 = base.transform.InverseTransformDirection(bezierCurve.GetTangent(z));
			Vector3 vector4 = Vector3.Cross(Vector3.up, vector3);
			Vector3 position = vector2 + vector.x * vector4 + vector.y * Vector3.up;
			Vector3 vector5 = base.transform.TransformPoint(position);
			bool flag = false;
			RaycastHit hitInfo;
			if (vector.y > 0f)
			{
				float y = point.y;
				for (int j = -1; j <= 1; j++)
				{
					if (Physics.Raycast(point + j * vector4 * radius + 2000f * Vector3.up, Vector3.down, out hitInfo, 4000f, 1) && hitInfo.point.y > y)
					{
						y = hitInfo.point.y;
						flag = true;
					}
				}
				if (flag)
				{
					vector5.y = y + vector.y;
				}
			}
			if ((!flag || vector.y < -40f) && Physics.Raycast(vector5 + 2000f * Vector3.up, Vector3.down, out hitInfo, 4000f, 1) && (hitInfo.point.y > point.y || vector.y < -40f))
			{
				vector5 = hitInfo.point + (vector.y + (float)((vector.y < -40f) ? 40 : 0)) * Vector3.up;
			}
			position = base.transform.InverseTransformPoint(vector5);
			Vector3 value = Quaternion.LookRotation(vector3, Vector3.Cross(vector3, vector4)) * vTTerrainMesh.normals[i];
			vTTerrainMesh.verts[i] = position;
			vTTerrainMesh.normals[i] = value;
			Vector2 value2 = vTTerrainMesh.uvs[i];
			value2.x *= num;
			vTTerrainMesh.uvs[i] = value2;
		}
		Vector3[] tempNormals = new Vector3[vTTerrainMesh.vertCount];
		VTTerrainJob.RecalculateNormals(vTTerrainMesh, tempNormals);
		Mesh mesh = new Mesh();
		vTTerrainMesh.ApplyToMesh(mesh);
		mf.mesh = mesh;
	}
}
