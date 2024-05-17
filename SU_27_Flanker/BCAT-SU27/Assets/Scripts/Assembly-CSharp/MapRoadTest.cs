using System.Collections.Generic;
using UnityEngine;

public class MapRoadTest : MonoBehaviour
{
	public struct RoadVertex
	{
		public Vector3 worldPos;

		public Vector3 tangent;

		public float height;

		public RoadVertex(Vector3 worldPos, Vector3 tangent, float height)
		{
			this.worldPos = worldPos;
			this.tangent = tangent;
			this.height = height;
		}
	}

	public class RoadSegment
	{
		public List<RoadVertex> roadVerts;

		public RoadSegment()
		{
			roadVerts = new List<RoadVertex>();
		}
	}

	public Material[] materials;

	public Transform ptA;

	public Transform ptB;

	public Transform turnPt;

	public float segmentLength = 10f;

	public RoadMeshProfile mainRoadProfile;

	public RoadMeshProfile bridgeTransitionProfile;

	public RoadMeshProfile bridgeRoadProfile;

	private void Start()
	{
		CreateRoad(ptA.position, ptB.position, turnPt.position);
	}

	private void CreateRoad(Vector3 start, Vector3 end, Vector3 turnPoint)
	{
		VTTerrainMesh vTTerrainMesh = new VTTerrainMesh();
		vTTerrainMesh.subMeshCount = mainRoadProfile.roadMesh.subMeshCount;
		float num = (start - turnPoint).magnitude + (end - turnPoint).magnitude;
		int num2 = Mathf.RoundToInt(num / segmentLength);
		float intervalDistance = num / (float)num2;
		RoadSegment roadSegment = new RoadSegment();
		Curve3D curve3D = new Curve3D(new Vector3[3] { start, turnPoint, end });
		curve3D.UniformlyParition(intervalDistance);
		Vector3 vector = start;
		for (int i = 0; i <= num2; i++)
		{
			float time = (float)i / (float)num2;
			Vector3 point = curve3D.GetPoint(time);
			Vector3 tangent = curve3D.GetTangent(time);
			float num3 = GetVertHeight(point, tangent, 9.66f) + 0.25f;
			Vector3 vector2 = SurfacePoint(point, num3);
			if (num3 > 5f)
			{
				vector2.y = Mathf.MoveTowards(vector.y, point.y, 5f);
			}
			else if (vector2.y < vector.y)
			{
				vector2.y = Mathf.MoveTowards(vector.y, vector2.y, 5f);
			}
			roadSegment.roadVerts.Add(new RoadVertex(vector2, tangent, 4f * num3));
			vector = vector2;
		}
		CreateRoad(roadSegment, mainRoadProfile, vTTerrainMesh);
		Mesh mesh = new Mesh();
		vTTerrainMesh.ApplyToMesh(mesh);
		base.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
		base.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
	}

	private void CreateRoad(RoadSegment segment, RoadMeshProfile roadProfile, VTTerrainMesh masterMesh)
	{
		for (int i = 0; i < segment.roadVerts.Count - 1; i++)
		{
			AppendSegment(segment.roadVerts[i], segment.roadVerts[i + 1], roadProfile, masterMesh);
		}
	}

	private Vector3 SurfacePoint(Vector3 pos, float height)
	{
		if (Physics.Raycast(pos + 2500f * Vector3.up, Vector3.down, out var hitInfo, 5000f, 1))
		{
			return hitInfo.point + new Vector3(0f, height, 0f);
		}
		return pos;
	}

	private void AppendSegment(RoadVertex a, RoadVertex b, RoadMeshProfile roadProfile, VTTerrainMesh masterMesh)
	{
		Quaternion quaternion = Quaternion.LookRotation(a.tangent);
		Quaternion quaternion2 = Quaternion.LookRotation(b.tangent);
		Vector3 vector = quaternion * Vector3.right;
		VTTerrainMesh segMesh = roadProfile.segMesh;
		List<int> rearVerts = roadProfile.rearVerts;
		List<int> frontVerts = roadProfile.frontVerts;
		List<int> bottomVerts = roadProfile.bottomVerts;
		int count = masterMesh.verts.Count;
		for (int i = 0; i < segMesh.verts.Count; i++)
		{
			masterMesh.verts.Add(segMesh.verts[i]);
			masterMesh.uvs.Add(segMesh.uvs[i]);
			masterMesh.normals.Add(base.transform.InverseTransformDirection(quaternion * segMesh.normals[i]));
		}
		for (int j = 0; j < segMesh.subMeshCount; j++)
		{
			for (int k = 0; k < segMesh.subMeshTriangles[j].Count; k++)
			{
				masterMesh.subMeshTriangles[j].Add(segMesh.subMeshTriangles[j][k] + count);
			}
		}
		for (int l = 0; l < rearVerts.Count; l++)
		{
			Vector3 vector2 = segMesh.verts[rearVerts[l]];
			vector2.z = 0f;
			vector2 = quaternion * vector2;
			vector2 += a.worldPos;
			vector2 = base.transform.InverseTransformPoint(vector2);
			masterMesh.verts[rearVerts[l] + count] = vector2;
		}
		for (int m = 0; m < frontVerts.Count; m++)
		{
			Vector3 vector3 = segMesh.verts[frontVerts[m]];
			vector3.z = 0f;
			vector3 = quaternion2 * vector3;
			vector3 += b.worldPos;
			vector3 = base.transform.InverseTransformPoint(vector3);
			masterMesh.verts[frontVerts[m] + count] = vector3;
		}
		for (int n = 0; n < bottomVerts.Count; n++)
		{
			int index = bottomVerts[n] + count;
			Vector3 position = base.transform.TransformPoint(masterMesh.verts[index]);
			float height = b.height;
			position += new Vector3(0f, 0f - height + segMesh.verts[bottomVerts[n]].y, 0f);
			position += 2f * height * vector * Mathf.Sign(Vector3.Dot(segMesh.verts[bottomVerts[n]], Vector3.right));
			masterMesh.verts[index] = base.transform.InverseTransformPoint(position);
		}
		if (count > 0)
		{
			for (int num = 0; num < rearVerts.Count; num++)
			{
				int index2 = frontVerts[num] + count - segMesh.verts.Count;
				int index3 = rearVerts[num] + count;
				Vector3 vector4 = masterMesh.verts[index2];
				Vector3 vector7 = (masterMesh.verts[index2] = (masterMesh.verts[index3] = vector4));
				Vector3 normalized = (masterMesh.normals[index2] + masterMesh.normals[index3]).normalized;
				vector7 = (masterMesh.normals[index2] = (masterMesh.normals[index3] = normalized));
			}
		}
	}

	private float GetVertHeight(Vector3 a, Vector3 tangent, float radius)
	{
		Vector3 vector = a;
		Vector3 vector2 = Quaternion.LookRotation(tangent) * Vector3.right;
		float y;
		float a2 = (y = vector.y);
		if (Physics.Raycast(vector + radius * vector2 + 2500f * Vector3.up, Vector3.down, out var hitInfo, 5000f, 1))
		{
			y = hitInfo.point.y;
		}
		if (Physics.Raycast(vector - radius * vector2 + 2500f * Vector3.up, Vector3.down, out hitInfo, 5000f, 1))
		{
			a2 = hitInfo.point.y;
		}
		return Mathf.Max(a2, y) - SurfacePoint(vector, 0f).y;
	}
}
