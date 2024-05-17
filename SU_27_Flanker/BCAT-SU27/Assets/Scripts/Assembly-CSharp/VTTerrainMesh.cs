using System.Collections.Generic;
using UnityEngine;

public class VTTerrainMesh
{
	private static int _Total_vt_meshes;

	public List<Vector3> verts;

	public List<Vector3> normals;

	public List<Vector2> uvs;

	public List<Color> colors;

	public List<Vector4> tangents;

	public List<float> treeValues;

	public List<int>[] subMeshTriangles;

	public IntVector2 gridPos;

	private List<int>[][,] triSectors;

	private const int TRISECTOR_DIM = 10;

	public static int TOTAL_VT_MESHES => _Total_vt_meshes;

	public int vertCount => verts.Count;

	public List<int> triangles => subMeshTriangles[0];

	public int subMeshCount
	{
		get
		{
			return subMeshTriangles.Length;
		}
		set
		{
			if (value != subMeshTriangles.Length)
			{
				List<int>[] array = subMeshTriangles;
				subMeshTriangles = new List<int>[value];
				for (int i = 0; i < array.Length && i < value; i++)
				{
					subMeshTriangles[i] = array[i];
				}
				for (int j = array.Length; j < value; j++)
				{
					subMeshTriangles[j] = new List<int>();
				}
			}
		}
	}

	public VTTerrainMesh()
	{
		verts = new List<Vector3>();
		normals = new List<Vector3>();
		uvs = new List<Vector2>();
		subMeshTriangles = new List<int>[1];
		subMeshTriangles[0] = new List<int>();
		tangents = new List<Vector4>();
		colors = new List<Color>();
		treeValues = new List<float>();
		gridPos = IntVector2.zero;
		_Total_vt_meshes++;
	}

	public VTTerrainMesh(Mesh unityMesh)
	{
		verts = new List<Vector3>();
		unityMesh.GetVertices(verts);
		subMeshTriangles = new List<int>[unityMesh.subMeshCount];
		for (int i = 0; i < subMeshTriangles.Length; i++)
		{
			subMeshTriangles[i] = new List<int>();
			unityMesh.GetTriangles(subMeshTriangles[i], i);
		}
		uvs = new List<Vector2>(vertCount);
		unityMesh.GetUVs(0, uvs);
		colors = new List<Color>(vertCount);
		normals = new List<Vector3>(vertCount);
		unityMesh.GetNormals(normals);
		treeValues = new List<float>();
		tangents = new List<Vector4>();
		unityMesh.GetTangents(tangents);
		for (int j = 0; j < vertCount; j++)
		{
			colors.Add(Color.white);
			treeValues.Add(0f);
		}
		_Total_vt_meshes++;
	}

	~VTTerrainMesh()
	{
		_Total_vt_meshes--;
	}

	public void Clear()
	{
		if (verts != null)
		{
			verts.Clear();
		}
		else
		{
			verts = new List<Vector3>();
		}
		if (normals != null)
		{
			normals.Clear();
		}
		else
		{
			normals = new List<Vector3>();
		}
		if (uvs != null)
		{
			uvs.Clear();
		}
		else
		{
			uvs = new List<Vector2>();
		}
		subMeshTriangles = new List<int>[1];
		subMeshTriangles[0] = new List<int>();
		tangents = new List<Vector4>();
		colors = new List<Color>();
		treeValues = new List<float>();
		gridPos = IntVector2.zero;
	}

	public VTTerrainMesh Copy()
	{
		VTTerrainMesh vTTerrainMesh = new VTTerrainMesh();
		vTTerrainMesh.gridPos = gridPos;
		bool flag = colors != null && colors.Count > 0;
		bool flag2 = tangents != null && tangents.Count > 0;
		bool flag3 = treeValues != null && treeValues.Count > 0;
		bool flag4 = uvs != null && uvs.Count > 0;
		for (int i = 0; i < vertCount; i++)
		{
			vTTerrainMesh.verts.Add(verts[i]);
			vTTerrainMesh.normals.Add(normals[i]);
			if (flag4)
			{
				vTTerrainMesh.uvs.Add(uvs[i]);
			}
			if (flag)
			{
				vTTerrainMesh.colors.Add(colors[i]);
			}
			if (flag3)
			{
				vTTerrainMesh.treeValues.Add(treeValues[i]);
			}
			if (flag2)
			{
				vTTerrainMesh.tangents.Add(tangents[i]);
			}
		}
		vTTerrainMesh.subMeshTriangles = new List<int>[subMeshTriangles.Length];
		for (int j = 0; j < subMeshTriangles.Length; j++)
		{
			int count = subMeshTriangles[j].Count;
			vTTerrainMesh.subMeshTriangles[j] = new List<int>();
			for (int k = 0; k < count; k++)
			{
				vTTerrainMesh.subMeshTriangles[j].Add(subMeshTriangles[j][k]);
			}
		}
		return vTTerrainMesh;
	}

	public void AppendMesh(VTTerrainMesh m, float gridSize, bool forceColors = false, bool forceTangents = false, bool forceTrees = false, bool forceUVs = false, bool skipDummyTriangles = true)
	{
		bool flag = forceColors || (colors != null && colors.Count > 0);
		bool flag2 = forceTangents || (tangents != null && tangents.Count > 0);
		bool flag3 = forceTrees || (treeValues != null && treeValues.Count > 0);
		bool flag4 = forceUVs || (uvs != null && uvs.Count > 0);
		IntVector2 intVector = m.gridPos - gridPos;
		Vector3 vector = new Vector3(-intVector.x, intVector.y, 0f) * gridSize;
		int count = verts.Count;
		for (int i = 0; i < m.vertCount; i++)
		{
			verts.Add(m.verts[i] + vector);
			normals.Add(m.normals[i]);
			if (flag4)
			{
				uvs.Add(m.uvs[i]);
			}
			if (flag)
			{
				colors.Add(m.colors[i]);
			}
			if (flag3)
			{
				treeValues.Add(m.treeValues[i]);
			}
			if (flag2)
			{
				tangents.Add(m.tangents[i]);
			}
		}
		if (subMeshTriangles.Length < m.subMeshTriangles.Length)
		{
			List<int>[] array = new List<int>[m.subMeshTriangles.Length];
			int j;
			for (j = 0; j < subMeshTriangles.Length; j++)
			{
				array[j] = subMeshTriangles[j];
			}
			for (; j < array.Length; j++)
			{
				array[j] = new List<int>();
			}
			subMeshTriangles = array;
		}
		for (int k = 0; k < m.subMeshTriangles.Length; k++)
		{
			int count2 = m.subMeshTriangles[k].Count;
			if (!(count2 == 3 && skipDummyTriangles))
			{
				for (int l = 0; l < count2; l++)
				{
					subMeshTriangles[k].Add(m.subMeshTriangles[k][l] + count);
				}
			}
		}
	}

	public void ApplyToMesh(Mesh unityMesh, bool recalculateBounds = true)
	{
		if (unityMesh.vertexCount != vertCount)
		{
			unityMesh.Clear();
		}
		unityMesh.SetVertices(verts);
		unityMesh.subMeshCount = subMeshTriangles.Length;
		_ = subMeshTriangles.Length;
		for (int i = 0; i < subMeshTriangles.Length; i++)
		{
			unityMesh.SetTriangles(subMeshTriangles[i], i);
		}
		if (colors != null && colors.Count > 0)
		{
			unityMesh.SetColors(colors);
		}
		unityMesh.SetNormals(normals);
		unityMesh.SetUVs(0, uvs);
		if (tangents != null && tangents.Count > 0)
		{
			unityMesh.SetTangents(tangents);
		}
		if (recalculateBounds)
		{
			unityMesh.RecalculateBounds();
		}
	}

	public void SetTriangleMaterial(int triangleIndex, int fromMaterialIndex, int toMaterialIndex)
	{
		if (fromMaterialIndex >= subMeshCount)
		{
			Debug.LogError("FROM Material index is greater or equal to submesh count.");
			return;
		}
		if (toMaterialIndex >= subMeshCount)
		{
			Debug.LogError("TO Material index is greater or equal to submesh count.");
			return;
		}
		if (triangleIndex >= subMeshTriangles[fromMaterialIndex].Count - 2)
		{
			Debug.LogError("Triangle index is not within triangle list.");
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			int item = subMeshTriangles[fromMaterialIndex][triangleIndex + i];
			subMeshTriangles[toMaterialIndex].Add(item);
		}
		subMeshTriangles[fromMaterialIndex].RemoveRange(triangleIndex, 3);
	}

	public Vector3 WorldProjectPointOnTerrain(Vector3 worldPos, Transform meshTransform)
	{
		Vector3 localPosition = meshTransform.InverseTransformPoint(worldPos);
		localPosition = ProjectPointOnTerrain(localPosition);
		return meshTransform.TransformPoint(localPosition);
	}

	private void BuildTriSectors()
	{
		triSectors = new List<int>[subMeshCount][,];
		for (int i = 0; i < subMeshCount; i++)
		{
			triSectors[i] = new List<int>[10, 10];
			for (int j = 0; j < 10; j++)
			{
				for (int k = 0; k < 10; k++)
				{
					triSectors[i][j, k] = new List<int>();
				}
			}
			for (int l = 0; l < subMeshTriangles[i].Count; l += 3)
			{
				Vector3 vector = verts[subMeshTriangles[i][l]];
				Vector3 vector2 = verts[subMeshTriangles[i][l + 1]];
				Vector3 vector3 = verts[subMeshTriangles[i][l + 2]];
				AddToSector(i, vector, l);
				AddToSector(i, vector2, l);
				AddToSector(i, vector3, l);
				AddToSector(i, (vector + vector2 + vector3) / 3f, l);
			}
		}
	}

	private void AddToSector(int mIndex, Vector3 vert, int tIndex)
	{
		IntVector2 intVector = PosToTriSector(vert);
		List<int> list = triSectors[mIndex][intVector.x, intVector.y];
		if (!list.Contains(tIndex))
		{
			list.Add(tIndex);
		}
	}

	private IntVector2 PosToTriSector(Vector3 localPos)
	{
		int x = Mathf.Clamp(Mathf.RoundToInt(-10f * localPos.x / 3072f), 0, 9);
		int y = Mathf.Clamp(Mathf.RoundToInt(10f * localPos.y / 3072f), 0, 9);
		return new IntVector2(x, y);
	}

	public Vector3 ProjectPointOnTerrain(Vector3 localPosition)
	{
		if (triSectors == null)
		{
			BuildTriSectors();
		}
		Vector3 vector = localPosition;
		vector.z = 0f;
		IntVector2 intVector = PosToTriSector(vector);
		int num = -1;
		int num2 = -1;
		for (int i = 0; i < subMeshCount; i++)
		{
			if (num >= 0)
			{
				break;
			}
			List<int> list = subMeshTriangles[i];
			List<int> list2 = triSectors[i][intVector.x, intVector.y];
			for (int j = 0; j < list2.Count; j++)
			{
				int num3 = list2[j];
				Vector3 a = verts[list[num3]];
				Vector3 b = verts[list[num3 + 1]];
				Vector3 c = verts[list[num3 + 2]];
				a.z = (b.z = (c.z = 0f));
				if (IsInTri(vector, a, b, c))
				{
					num = num3;
					num2 = i;
				}
			}
		}
		if (num >= 0)
		{
			Vector3 vector2 = verts[subMeshTriangles[num2][num]];
			Vector3 vector3 = verts[subMeshTriangles[num2][num + 1]];
			Vector3 vector4 = verts[subMeshTriangles[num2][num + 2]];
			_ = (vector2 + vector3 + vector4) / 3f;
			Plane plane = new Plane(vector2, vector3, vector4);
			plane.normal *= Mathf.Sign(plane.normal.z);
			Vector3 origin = vector + (Mathf.Max(Mathf.Max(vector2.z, vector3.z), vector4.z) + 1f) * Vector3.forward;
			Vector3 back = Vector3.back;
			Ray ray = new Ray(origin, back);
			if (plane.Raycast(ray, out var enter))
			{
				return ray.GetPoint(enter);
			}
			return localPosition;
		}
		return localPosition;
	}

	private float MaxAxialDist(Vector3 a, Vector3 b)
	{
		return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
	}

	private float AreaOfTri(float x1, float y1, float x2, float y2, float x3, float y3)
	{
		return Mathf.Abs((x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2)) / 2f);
	}

	private bool IsInTri(Vector3 pt, Vector3 a, Vector3 b, Vector3 c)
	{
		float x = pt.x;
		float y = pt.y;
		float x2 = a.x;
		float x3 = b.x;
		float x4 = c.x;
		float y2 = a.y;
		float y3 = b.y;
		float y4 = c.y;
		float num = AreaOfTri(x2, y2, x3, y3, x4, y4);
		float num2 = AreaOfTri(x, y, x3, y3, x4, y4);
		float num3 = AreaOfTri(x2, y2, x, y, x4, y4);
		float num4 = AreaOfTri(x2, y2, x3, y3, x, y);
		return Mathf.Abs(num - (num2 + num3 + num4)) < 0.001f;
	}

	public void RemoveVertices(List<int> removeVertIndices)
	{
		removeVertIndices.Sort();
		bool flag = colors != null && colors.Count > 0;
		bool flag2 = tangents != null && tangents.Count > 0;
		bool flag3 = uvs != null && uvs.Count > 0;
		for (int num = removeVertIndices.Count - 1; num >= 0; num--)
		{
			int index = removeVertIndices[num];
			verts.RemoveAt(index);
			normals.RemoveAt(index);
			if (flag3)
			{
				uvs.RemoveAt(index);
			}
			if (flag)
			{
				colors.RemoveAt(index);
			}
			if (flag2)
			{
				tangents.RemoveAt(index);
			}
		}
		int num2 = 0;
		while (num2 < triangles.Count)
		{
			int num3 = triangles[num2];
			int num4 = triangles[num2 + 1];
			int num5 = triangles[num2 + 2];
			bool flag4 = false;
			for (int i = 0; i < removeVertIndices.Count; i++)
			{
				int num6 = removeVertIndices[i];
				if (num6 == num3 || num6 == num4 || num6 == num5)
				{
					flag4 = true;
				}
			}
			if (flag4)
			{
				triangles.RemoveRange(num2, 3);
				continue;
			}
			int num7 = num3;
			int num8 = num4;
			int num9 = num5;
			for (int j = 0; j < removeVertIndices.Count; j++)
			{
				int num10 = removeVertIndices[j];
				if (num3 > num10)
				{
					num7--;
				}
				if (num4 > num10)
				{
					num8--;
				}
				if (num5 > num10)
				{
					num9--;
				}
			}
			triangles[num2] = num7;
			triangles[num2 + 1] = num8;
			triangles[num2 + 2] = num9;
			num2 += 3;
		}
	}
}
