using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class VTTerrainJob
{
	public static bool deleteUnderwaterTris = true;

	public int lod;

	private VTTerrainMesh[] readOnlyTMeshes;

	public VTTerrainMesh[] outputMeshes;

	private int numLods;

	public IntVector2 gridPosition;

	public Vector3D originGlobalPosition;

	public float chunkSize;

	public int mapSize;

	public FastNoise noiseModule;

	public bool isRecalculate;

	private Vector3[] tempNormals;

	private int maxVerts;

	public VTTerrainMod[] mods;

	public float maxHeight;

	public const string SETTINGS_NODE_NAME = "TerrainSettings";

	public void Initialize(VTTerrainMesh[] readOnlyMeshes)
	{
		readOnlyTMeshes = readOnlyMeshes;
		numLods = readOnlyMeshes.Length;
		outputMeshes = new VTTerrainMesh[numLods];
		for (int i = 0; i < numLods; i++)
		{
			maxVerts = Mathf.Max(maxVerts, readOnlyTMeshes[i].vertCount);
		}
		tempNormals = new Vector3[maxVerts];
	}

	public void DoJob()
	{
		try
		{
			VTTerrainMesh vTTerrainMesh = readOnlyTMeshes[lod].Copy();
			CalculateHeights(vTTerrainMesh);
			if (mods != null)
			{
				for (int i = 0; i < mods.Length; i++)
				{
					mods[i].ApplyHeightMod(this, vTTerrainMesh);
				}
			}
			RecalculateNormals(vTTerrainMesh, tempNormals);
			CalculateColors(vTTerrainMesh);
			if (mods != null)
			{
				for (int j = 0; j < mods.Length; j++)
				{
					mods[j].ApplyColorMod(this, vTTerrainMesh);
				}
			}
			if (deleteUnderwaterTris)
			{
				RemoveUnderwaterTris(vTTerrainMesh);
			}
			if (lod == 0)
			{
				TrimEdges(vTTerrainMesh);
			}
			OnFinalProcessing(vTTerrainMesh);
			mods = null;
			FillEmptySubmeshes(vTTerrainMesh);
			outputMeshes[lod] = vTTerrainMesh;
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	protected virtual void OnFinalProcessing(VTTerrainMesh mesh)
	{
	}

	private void FillEmptySubmeshes(VTTerrainMesh mesh)
	{
		bool flag = false;
		for (int i = 0; i < mesh.subMeshCount; i++)
		{
			if (mesh.subMeshTriangles[i].Count == 0)
			{
				flag = true;
				for (int j = 0; j < 3; j++)
				{
					mesh.subMeshTriangles[i].Add(mesh.verts.Count);
					mesh.verts.Add(new Vector3(-10000f, -10000f, -10000f));
					mesh.normals.Add(Vector3.forward);
					mesh.uvs.Add(new Vector2(-10000f, -10000f));
					mesh.colors.Add(Color.clear);
					mesh.treeValues.Add(0f);
					mesh.tangents.Add(default(Vector4));
				}
			}
		}
		if (flag)
		{
			maxVerts = Mathf.Max(maxVerts, mesh.vertCount);
			tempNormals = new Vector3[maxVerts];
		}
	}

	private void TrimEdges(VTTerrainMesh mesh)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < mesh.verts.Count; i++)
		{
			Vector2 vector = mesh.uvs[i];
			if (vector.x < -0.002f || vector.x > 1.002f || vector.y < -0.002f || vector.y > 1.002f)
			{
				list.Add(i);
			}
		}
		int num = 0;
		while (num < mesh.triangles.Count)
		{
			int num2 = mesh.triangles[num];
			int num3 = mesh.triangles[num + 1];
			int num4 = mesh.triangles[num + 2];
			bool flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j] == num2 || list[j] == num3 || list[j] == num4)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				mesh.triangles.RemoveRange(num, 3);
			}
			else
			{
				num += 3;
			}
		}
	}

	private void CalculateHeights(VTTerrainMesh mesh)
	{
		maxHeight = 0f;
		for (int i = 0; i < mesh.vertCount; i++)
		{
			Vector2 vector = mesh.uvs[i];
			if (vector.x < 0.001f && vector.x > -0.001f)
			{
				vector.x = 0f;
			}
			if (vector.x > 0.999f && vector.x < 1.001f)
			{
				vector.x = 1f;
			}
			if (vector.y < 0.001f && vector.y > -0.001f)
			{
				vector.y = 0f;
			}
			if (vector.y > 0.999f && vector.y < 1.001f)
			{
				vector.y = 1f;
			}
			mesh.uvs[i] = vector;
			float num = CalculateHeight(vector);
			Vector3 value = mesh.verts[i];
			value.z = num;
			mesh.verts[i] = value;
			maxHeight = Mathf.Max(num, maxHeight);
		}
	}

	protected virtual float CalculateHeight(Vector2 uv)
	{
		return 0f;
	}

	public float GetHeight(Vector2 uv)
	{
		return CalculateHeight(uv);
	}

	protected virtual void CalculateColors(VTTerrainMesh mesh)
	{
	}

	public static void RecalculateNormals(VTTerrainMesh mesh, Vector3[] tempNormals)
	{
		int count = mesh.triangles.Count;
		for (int i = 0; i < mesh.vertCount; i++)
		{
			tempNormals[i] = Vector3.zero;
		}
		for (int j = 0; j < count; j += 3)
		{
			Vector3 vector = mesh.verts[mesh.triangles[j]];
			Vector3 vector2 = mesh.verts[mesh.triangles[j + 1]];
			Vector3 vector3 = mesh.verts[mesh.triangles[j + 2]];
			Vector3 normalized = Vector3.Cross(vector2 - vector, vector3 - vector).normalized;
			tempNormals[mesh.triangles[j]] += normalized;
			Vector3 normalized2 = Vector3.Cross(vector3 - vector2, vector - vector2).normalized;
			tempNormals[mesh.triangles[j + 1]] += normalized2;
			Vector3 normalized3 = Vector3.Cross(vector - vector3, vector2 - vector3).normalized;
			tempNormals[mesh.triangles[j + 2]] += normalized3;
		}
		for (int k = 0; k < mesh.vertCount; k++)
		{
			mesh.normals[k] = tempNormals[k].normalized;
		}
	}

	public void ApplySettings(ConfigNode settingsConfig)
	{
		List<ConfigNode.ConfigValue> values = settingsConfig.GetValues();
		FieldInfo[] fields = GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(VTTerrainAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				_ = customAttributes[j];
				foreach (ConfigNode.ConfigValue item in values)
				{
					if (item.name == fieldInfo.Name)
					{
						fieldInfo.SetValue(this, ConfigNodeUtils.ParseObject(fieldInfo.FieldType, item.value));
					}
				}
			}
		}
	}

	private void RemoveUnderwaterTris(VTTerrainMesh mesh)
	{
		float num = -55f;
		int count = mesh.triangles.Count;
		for (int i = 0; i < count; i += 3)
		{
			if (mesh.verts[mesh.triangles[i]].z < num && mesh.verts[mesh.triangles[i + 1]].z < num && mesh.verts[mesh.triangles[i + 2]].z < num)
			{
				mesh.triangles[i] = -1;
				mesh.triangles[i + 1] = -1;
				mesh.triangles[i + 2] = -1;
			}
		}
		mesh.triangles.RemoveAll((int x) => x == -1);
	}

	public ConfigNode GetSettingsConfig()
	{
		ConfigNode configNode = new ConfigNode("TerrainSettings");
		FieldInfo[] fields = GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(VTTerrainAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				_ = customAttributes[j];
				configNode.SetValue(fieldInfo.Name, ConfigNodeUtils.WriteObject(fieldInfo.GetValue(this)));
			}
		}
		return configNode;
	}

	protected double Lerp(double a, double b, double t)
	{
		return a + t * (b - a);
	}

	protected double Clamp(double val, double min, double max)
	{
		if (val < min)
		{
			return min;
		}
		if (val > max)
		{
			return max;
		}
		return val;
	}

	protected double Clamp01(double a)
	{
		return Clamp(a, 0.0, 1.0);
	}
}
