using System;
using System.Collections.Generic;
using UnityEngine;

public static class TreePopulator
{
	public enum ColorChannels
	{
		Red,
		Green,
		Blue,
		Alpha
	}

	[Serializable]
	public struct NoiseFunction
	{
		public enum BlendModes
		{
			Add,
			Multiply
		}

		public FastNoise.NoiseType noiseType;

		public BlendModes blendMode;

		public float scale;

		public Vector2 offset;

		public float multiplier;

		public float power;
	}

	public class NoiseProfile
	{
		public FastNoise fastNoise;

		public NoiseFunction[] noiseFunctions;

		public float GetNoise(float x, float y)
		{
			float num = 0f;
			for (int i = 0; i < noiseFunctions.Length; i++)
			{
				NoiseFunction noiseFunction = noiseFunctions[i];
				fastNoise.SetNoiseType(noiseFunction.noiseType);
				float f = noiseFunction.multiplier * (float)fastNoise.GetNoise(noiseFunction.scale * (noiseFunction.offset.x + x), noiseFunction.scale * (noiseFunction.offset.y + y));
				f = Mathf.Pow(f, noiseFunction.power);
				switch (noiseFunction.blendMode)
				{
				case NoiseFunction.BlendModes.Add:
					num += f;
					break;
				case NoiseFunction.BlendModes.Multiply:
					num *= f;
					break;
				}
			}
			return num;
		}
	}

	public class TreePoints
	{
	}

	private static FastNoise _tNoise;

	private static FastNoise tNoise
	{
		get
		{
			if (_tNoise == null)
			{
				_tNoise = new FastNoise(667970);
				_tNoise.SetNoiseType(FastNoise.NoiseType.WhiteNoise);
			}
			return _tNoise;
		}
	}

	public static List<Vector3> GenerateTreePoints(MeshFilter terrainMeshFilter, float maxTreesPerTri, ColorChannels treeColorChannel, NoiseProfile noiseProfile)
	{
		Mesh sharedMesh = terrainMeshFilter.sharedMesh;
		List<Vector3> list = new List<Vector3>();
		int[] triangles = sharedMesh.triangles;
		Vector3[] vertices = sharedMesh.vertices;
		Color[] colors = sharedMesh.colors;
		for (int i = 0; i < triangles.Length; i += 3)
		{
			Vector3 vector = vertices[triangles[i]];
			Vector3 vector2 = vertices[triangles[i + 1]];
			Vector3 vector3 = vertices[triangles[i + 2]];
			Color color = AvgColor(colors[triangles[i]], colors[triangles[i + 1]], colors[triangles[i + 2]]);
			float num = treeColorChannel switch
			{
				ColorChannels.Red => color.r, 
				ColorChannels.Green => color.g, 
				ColorChannels.Blue => color.b, 
				_ => color.a, 
			};
			Vector3 position = (vector + vector2 + vector3) / 3f;
			Vector3 vector4 = terrainMeshFilter.transform.TransformPoint(position);
			vector4 += FloatingOrigin.accumOffset.toVector3;
			num *= noiseProfile.GetNoise(vector4.x, vector4.z);
			float num2 = num * maxTreesPerTri;
			for (float num3 = UnityEngine.Random.Range(0f, num2 + 1f); num3 < num2; num3 += 1f)
			{
				Vector3 item = DeterministicPointInTriangle(vector, vector2, vector3, Mathf.FloorToInt(num3), i / 3);
				list.Add(item);
			}
		}
		return list;
	}

	public static List<Vector3> GenerateTreePoints(AkuTreeData treeData, float maxTreesPerTri, Transform terrainTf, Transform treeTf, Bounds bounds)
	{
		List<Vector3> list = new List<Vector3>();
		Vector3[] verts = treeData.verts;
		int[] triangles = treeData.GetComponent<MeshFilter>().sharedMesh.triangles;
		float[] treeVals = treeData.treeVals;
		Vector3 position = terrainTf.position;
		Vector3 position2 = treeTf.position;
		terrainTf.position = Vector3.zero;
		treeTf.position = Vector3.zero;
		int num = 0;
		for (int i = 0; i < triangles.Length; i += 3)
		{
			float num2 = treeVals[triangles[i]];
			float num3 = treeVals[triangles[i + 1]];
			float num4 = treeVals[triangles[i + 2]];
			float num5 = 0.75f;
			if (num2 > num5 && num3 > num5 && num4 > num5)
			{
				Vector3 position3 = verts[triangles[i]];
				Vector3 position4 = verts[triangles[i + 1]];
				Vector3 position5 = verts[triangles[i + 2]];
				position3 = treeTf.InverseTransformPoint(terrainTf.TransformPoint(position3));
				position4 = treeTf.InverseTransformPoint(terrainTf.TransformPoint(position4));
				position5 = treeTf.InverseTransformPoint(terrainTf.TransformPoint(position5));
				float f = AvgValue(num2, num3, num4) * maxTreesPerTri;
				SetRandomPositionsDeterministic(position3, position4, position5, list, Mathf.RoundToInt(f), bounds, num);
			}
			num++;
		}
		terrainTf.position = position;
		treeTf.position = position2;
		return list;
	}

	public static List<Vector3> GenerateTreePoints(VTTerrainMesh terrainMesh, float maxTreesPerTri, Transform terrainTf, Transform treeTf, Bounds bounds, float treeThresh = 0.75f)
	{
		List<Vector3> list = new List<Vector3>();
		List<Vector3> verts = terrainMesh.verts;
		List<int> triangles = terrainMesh.triangles;
		List<float> treeValues = terrainMesh.treeValues;
		Vector3 position = terrainTf.position;
		Vector3 position2 = treeTf.position;
		terrainTf.position = Vector3.zero;
		treeTf.position = Vector3.zero;
		int num = 0;
		for (int i = 0; i < triangles.Count; i += 3)
		{
			float num2 = treeValues[triangles[i]];
			float num3 = treeValues[triangles[i + 1]];
			float num4 = treeValues[triangles[i + 2]];
			if (num2 > treeThresh && num3 > treeThresh && num4 > treeThresh)
			{
				Vector3 position3 = verts[triangles[i]];
				Vector3 position4 = verts[triangles[i + 1]];
				Vector3 position5 = verts[triangles[i + 2]];
				position3 = treeTf.InverseTransformPoint(terrainTf.TransformPoint(position3));
				position4 = treeTf.InverseTransformPoint(terrainTf.TransformPoint(position4));
				position5 = treeTf.InverseTransformPoint(terrainTf.TransformPoint(position5));
				float f = AvgValue(num2, num3, num4) * maxTreesPerTri;
				SetRandomPositionsDeterministic(position3, position4, position5, list, Mathf.RoundToInt(f), bounds, num);
			}
			num++;
		}
		terrainTf.position = position;
		treeTf.position = position2;
		return list;
	}

	private static bool PointInBoundsXZ(Vector3 point, Bounds bounds)
	{
		Vector3 size = bounds.size;
		size.y = float.MaxValue - Mathf.Abs(bounds.center.y);
		bounds.size = size;
		return bounds.Contains(point);
	}

	private static float AvgValue(float a, float b, float c)
	{
		return (a + b + c) / 3f;
	}

	private static Vector3 DeterministicPointInTriangle(Vector3 a, Vector3 b, Vector3 c, int treeIdx, int triIdx)
	{
		treeIdx++;
		float t = Mathf.Repeat((float)tNoise.GetNoise(a.x * (float)treeIdx * (float)triIdx, a.z * (float)treeIdx * (float)triIdx), 1f);
		float t2 = Mathf.Repeat((float)tNoise.GetNoise(a.z * (float)treeIdx * (float)triIdx, b.x * (float)treeIdx * (float)triIdx), 1f);
		return Vector3.Lerp(Vector3.Lerp(a, b, t), c, t2);
	}

	private static void SetRandomPositionsDeterministic(Vector3 a, Vector3 b, Vector3 c, List<Vector3> pointList, int treeCount, Bounds bounds, int triIdx)
	{
		for (int i = 0; i < treeCount; i++)
		{
			Vector3 vector = DeterministicPointInTriangle(a, b, c, i, triIdx);
			if (PointInBoundsXZ(vector, bounds))
			{
				pointList.Add(vector);
			}
		}
	}

	private static Color AvgColor(Color a, Color b, Color c)
	{
		return (a + b + c) / 3f;
	}
}
