using System.Collections.Generic;
using UnityEngine;

public class VTTHeightMap : VTTerrainJob
{
	public enum EdgeModes
	{
		Water,
		Heightmap,
		Coast,
		None
	}

	public BDTexture heightMap;

	public new float maxHeight = 6000f;

	public float minHeight = -80f;

	public BDTexture oobHeightmap;

	public int oobGridScale;

	public EdgeModes edgeMode;

	public MapGenBiome.Biomes biome;

	public int vertsPerSide = 20;

	public bool loadCities = true;

	public bool coastalOOB;

	public CardinalDirections coastSide;

	public IntVector2 oobGridOffset;

	protected override float CalculateHeight(Vector2 uv)
	{
		Vector2 vector = (uv + gridPosition.ToVector2()) * chunkSize;
		Vector3 vert = new Vector3(0f - uv.x, uv.y, 0f) * chunkSize;
		float num = Mathf.Lerp(t: heightMap.GetPixel(VTTerrainTextureConverter.VertToPixel(vert, gridPosition.x, gridPosition.y, chunkSize, mapSize, vertsPerSide)).r, a: minHeight, b: maxHeight);
		if (edgeMode != EdgeModes.None)
		{
			float num2 = -1f;
			switch (edgeMode)
			{
			case EdgeModes.Heightmap:
				if (oobHeightmap != null)
				{
					Vector2 vector4 = (float)oobHeightmap.width / (chunkSize * (float)oobGridScale) * vector;
					num2 = oobHeightmap.GetColor(vector4.x, vector4.y).r * maxHeight;
				}
				break;
			case EdgeModes.Coast:
				if (oobHeightmap != null)
				{
					float num3 = (float)oobHeightmap.width / (chunkSize * (float)oobGridScale);
					Vector2 vector2 = (uv + (oobGridOffset + gridPosition).Repeat(oobGridScale).ToVector2()) * chunkSize;
					Vector2 vector3 = num3 * vector2;
					num2 = oobHeightmap.GetColor(vector3.x, vector3.y).r * maxHeight;
					if ((coastSide == CardinalDirections.South && gridPosition.y < oobGridScale) || (coastSide == CardinalDirections.West && gridPosition.x < oobGridScale) || (coastSide == CardinalDirections.North && gridPosition.y >= mapSize - oobGridScale) || (coastSide == CardinalDirections.East && gridPosition.x >= mapSize - oobGridScale))
					{
						float coastT = GetCoastT(vector2, chunkSize * (float)oobGridScale, coastSide);
						num2 = Mathf.Lerp(-80f, num2, coastT);
					}
				}
				break;
			case EdgeModes.Water:
				num2 = -80f;
				break;
			}
			num = GetBlendedHeight(num, num2, mapSize, chunkSize, maxHeight, vector, edgeMode == EdgeModes.Coast, coastSide);
		}
		if (coastalOOB)
		{
			float coastT2 = GetCoastT(vector, chunkSize * (float)mapSize, coastSide);
			num = Mathf.Lerp(-80f, num, coastT2);
		}
		return num;
	}

	private static float GetCoastT(Vector2 worldUV, float oobTotalSize, CardinalDirections side)
	{
		worldUV /= oobTotalSize;
		float num = Mathf.Clamp01(side switch
		{
			CardinalDirections.North => 1f - worldUV.y, 
			CardinalDirections.East => 1f - worldUV.x, 
			CardinalDirections.South => worldUV.y, 
			_ => worldUV.x, 
		});
		return Mathf.Clamp01(Pow(num * 2f, 2));
	}

	public static float GetBlendedHeight(float hmHeight, float oobHeight, float mapSize, float chunkSize, float maxHeight, Vector2 worldUV, bool coastal = false, CardinalDirections coastDir = CardinalDirections.West)
	{
		Vector2 vector = mapSize / 2f * new Vector2(chunkSize, chunkSize);
		Vector2 vector2 = worldUV - vector;
		float num = Mathf.Max(Mathf.Abs(vector2.x), Mathf.Abs(vector2.y)) / (chunkSize * mapSize / 2f);
		if (coastal)
		{
			float b = Mathf.Pow(num, 4f);
			if ((coastDir == CardinalDirections.West && vector2.x < 0f) || (coastDir == CardinalDirections.East && vector2.x > 0f))
			{
				float num2 = Mathf.Abs(vector2.y) / (chunkSize * mapSize / 2f);
				float num3 = Mathf.Abs(vector2.x) / (chunkSize * mapSize / 2f);
				num = Mathf.Lerp(num, b, num3 * (1f - num2 * num2));
			}
			else if ((coastDir == CardinalDirections.North && vector2.y > 0f) || (coastDir == CardinalDirections.South && vector2.y < 0f))
			{
				float num4 = Mathf.Abs(vector2.x) / (chunkSize * mapSize / 2f);
				float num5 = Mathf.Abs(vector2.y) / (chunkSize * mapSize / 2f);
				num = Mathf.Lerp(num, b, num5 * (1f - num4 * num4));
			}
		}
		float t = Mathf.Pow(num, Mathf.Clamp(6f * Mathf.Clamp01(hmHeight / maxHeight), 1f, 6f));
		t = Mathf.Lerp(0f, 1.05f, t);
		t = t * 4f - 3f;
		hmHeight = Mathf.Lerp(hmHeight, oobHeight, t);
		return hmHeight;
	}

	protected override void CalculateColors(VTTerrainMesh mesh)
	{
		new VTTArchipelago.ColorProfile
		{
			cliffPower = 5,
			cliffBias = 0f,
			snowHeightAdjust = -1000f
		};
		Vector2 vector = gridPosition.ToVector2();
		_ = Vector3.forward;
		for (int i = 0; i < mesh.vertCount; i++)
		{
			Vector3 vert = mesh.verts[i];
			Vector3 normal = mesh.normals[i];
			Vector2 uV = mesh.uvs[i] + vector;
			float treeValue;
			Color value = CalculateColor(biome, vert, normal, uV, noiseModule, out treeValue);
			mesh.treeValues[i] = treeValue;
			value.g = heightMap.GetPixel(VTTerrainTextureConverter.VertToPixel(vert, gridPosition.x, gridPosition.y, chunkSize, mapSize, vertsPerSide)).g;
			mesh.colors[i] = value;
		}
	}

	private static Color CalculateColor(MapGenBiome.Biomes biome, Vector3 vert, Vector3 normal, Vector2 m_UV, FastNoise noiseModule, out float treeValue)
	{
		return biome switch
		{
			MapGenBiome.Biomes.Desert => CalculateColorDesert(vert, normal, m_UV, noiseModule, out treeValue), 
			MapGenBiome.Biomes.Arctic => CalculateColorArctic(vert, normal, m_UV, noiseModule, out treeValue), 
			MapGenBiome.Biomes.Tropical => CalculateColorTropical(vert, normal, m_UV, noiseModule, out treeValue), 
			_ => CalculateColorBoreal(vert, normal, m_UV, noiseModule, out treeValue), 
		};
	}

	private static Color CalculateColorArctic(Vector3 vert, Vector3 normal, Vector2 m_UV, FastNoise noiseModule, out float treeValue)
	{
		float num = -1000f;
		int powr = 5;
		float num2 = 0f;
		float num3 = 100f;
		float num4 = 25f;
		float num5 = Vector3.Dot(normal, Vector3.forward);
		Color b = Color.Lerp(new Color(0f, 0f, 0f, 1f), t: (float)noiseModule.GetPerlinFractal(m_UV.x * num4, m_UV.y * num4), b: new Color(1f, 0f, 0f, 0f));
		float num6 = Pow(num5 + num2, powr);
		b = Color.Lerp(new Color(0f, 0f, 0f, 0f), b, num6);
		float val = Mathf.Clamp01((vert.z + num) * (1f + 1f * (float)noiseModule.GetPerlinFractal(m_UV.x * num3, m_UV.y * num3)) / Mathf.Lerp(2300f, 1200f, 1f - num6));
		val = Pow(val, 3);
		b = Color.Lerp(b, new Color(0f, 0f, 1f, 0f), val);
		if (vert.z > 20f)
		{
			treeValue = b.r;
		}
		else
		{
			treeValue = 0f;
		}
		float t2 = b.a * (2f * (float)noiseModule.GetPerlinFractal(m_UV.x * 85f, m_UV.y * 85f) + 1f) / 2.5f;
		return Color.Lerp(b, new Color(1f, 0f, 0f, 0f), t2);
	}

	private static Color CalculateColorDesert(Vector3 vert, Vector3 normal, Vector2 m_UV, FastNoise noiseModule, out float treeValue)
	{
		float num = -1000f;
		int powr = 5;
		float num2 = 0f;
		float num3 = 100f;
		float num4 = 25f;
		float num5 = Vector3.Dot(normal, Vector3.forward);
		Color b = Color.Lerp(new Color(0f, 0f, 0f, 1f), t: (float)noiseModule.GetPerlinFractal(m_UV.x * num4, m_UV.y * num4), b: new Color(1f, 0f, 0f, 0f));
		float num6 = Pow(num5 + num2, powr);
		b = Color.Lerp(new Color(0f, 0f, 0f, 0f), b, num6);
		float val = Mathf.Clamp01((vert.z + num) * (1f + 1f * (float)noiseModule.GetPerlinFractal(m_UV.x * num3, m_UV.y * num3)) / Mathf.Lerp(2300f, 1200f, 1f - num6));
		val = Pow(val, 3);
		b = Color.Lerp(b, new Color(0f, 0f, 1f, 0f), val);
		if (vert.z > 20f)
		{
			treeValue = b.r * 3f;
		}
		else
		{
			treeValue = 0f;
		}
		float t2 = b.a * (2f * (float)noiseModule.GetPerlinFractal(m_UV.x * 85f, m_UV.y * 85f) + 1f) / 2.5f;
		return Color.Lerp(b, new Color(1f, 0f, 0f, 0f), t2);
	}

	private static Color CalculateColorBoreal(Vector3 vert, Vector3 normal, Vector2 m_UV, FastNoise noiseModule, out float treeValue)
	{
		float num = -1000f;
		int powr = 5;
		float num2 = 0f;
		float amp = 35f;
		float num3 = 1f;
		float num4 = 0f;
		float num5 = 100f;
		float num6 = 10f;
		float num7 = Vector3.Dot(normal, Vector3.forward);
		Color a = new Color(0f, 0f, 0f, 1f);
		noiseModule.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
		noiseModule.SetCellularReturnType(FastNoise.CellularReturnType.CellValue);
		noiseModule.SetFrequency(0.009999999776482582);
		double x = 1f / num3 * num6 * m_UV.x;
		double y = 1f / num3 * num6 * m_UV.y;
		PerturbCoords(noiseModule, ref x, ref y, amp, 4f);
		a = Color.Lerp(t: (noiseModule.GetCellular(x, y) > (double)num4) ? 1 : 0, a: a, b: new Color(1f, 0f, 0f, 0f));
		float num8 = Pow(num7 + num2, powr);
		a = Color.Lerp(new Color(0f, 0f, 0f, 0f), a, num8);
		float val = Mathf.Clamp01((vert.z + num) * (1f + 1f * (float)noiseModule.GetPerlinFractal(m_UV.x * num5, m_UV.y * num5)) / Mathf.Lerp(2300f, 1200f, 1f - num8));
		val = Pow(val, 3);
		a = Color.Lerp(a, new Color(0f, 0f, 1f, 0f), val);
		if (vert.z > 20f)
		{
			treeValue = a.r;
		}
		else
		{
			treeValue = 0f;
		}
		float t2 = a.a * (2f * (float)noiseModule.GetPerlinFractal(m_UV.x * 85f, m_UV.y * 85f) + 1f) / 2.5f;
		return Color.Lerp(a, new Color(1f, 0f, 0f, 0f), t2);
	}

	private static Color CalculateColorTropical(Vector3 vert, Vector3 normal, Vector2 m_UV, FastNoise noiseModule, out float treeValue)
	{
		float num = -1000f;
		int powr = 5;
		float num2 = 0f;
		float amp = 35f;
		float num3 = 1f;
		float num4 = 0f;
		float num5 = 100f;
		float num6 = 10f;
		float num7 = Vector3.Dot(normal, Vector3.forward);
		Color a = new Color(0f, 0f, 0f, 1f);
		noiseModule.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
		noiseModule.SetCellularReturnType(FastNoise.CellularReturnType.CellValue);
		noiseModule.SetFrequency(0.009999999776482582);
		double x = 1f / num3 * num6 * m_UV.x;
		double y = 1f / num3 * num6 * m_UV.y;
		PerturbCoords(noiseModule, ref x, ref y, amp, 4f);
		a = Color.Lerp(t: (noiseModule.GetCellular(x, y) > (double)num4) ? 1 : 0, a: a, b: new Color(1f, 0f, 0f, 0f));
		float num8 = Pow(num7 + num2, powr);
		a = Color.Lerp(new Color(0f, 0f, 0f, 0f), a, num8);
		float val = Mathf.Clamp01((vert.z + num) * (1f + 1f * (float)noiseModule.GetPerlinFractal(m_UV.x * num5, m_UV.y * num5)) / Mathf.Lerp(2300f, 1200f, 1f - num8));
		val = Pow(val, 3);
		a = Color.Lerp(a, new Color(0f, 0f, 1f, 0f), val);
		if (vert.z > 20f)
		{
			treeValue = a.r;
		}
		else
		{
			treeValue = 0f;
		}
		float t2 = a.a * (2f * (float)noiseModule.GetPerlinFractal(m_UV.x * 85f, m_UV.y * 85f) + 1f) / 2.5f;
		return Color.Lerp(a, new Color(1f, 0f, 0f, 0f), t2);
	}

	public static void PerturbCoords(FastNoise noiseModule, ref double x, ref double y, float amp, float freq, float octaveRate = 2f, int octaves = 1)
	{
		double perlin = noiseModule.GetPerlin(x * (double)freq, y * (double)freq);
		double perlin2 = noiseModule.GetPerlin((x + 1263.234) * (double)freq, (y - 642.234) * (double)freq);
		x += (float)perlin * amp;
		y += (float)perlin2 * amp;
		if (octaves >= 1)
		{
			PerturbCoords(noiseModule, ref x, ref y, amp / octaveRate, freq * octaveRate, octaveRate, octaves - 1);
		}
	}

	private static float Pow(float val, int powr)
	{
		for (int i = 1; i < powr; i++)
		{
			val *= val;
		}
		return val;
	}

	protected override void OnFinalProcessing(VTTerrainMesh mesh)
	{
		base.OnFinalProcessing(mesh);
		if (!loadCities)
		{
			return;
		}
		bool flag = true;
		while (flag)
		{
			flag = false;
			List<Vector3> verts = mesh.verts;
			List<int> triangles = mesh.triangles;
			List<Color> colors = mesh.colors;
			for (int i = 0; i < triangles.Count - 2; i += 3)
			{
				if (flag)
				{
					break;
				}
				int index = triangles[i];
				int index2 = triangles[i + 1];
				int index3 = triangles[i + 2];
				Vector3 vector = verts[index];
				Vector3 vector2 = verts[index2];
				Vector3 vector3 = verts[index3];
				Vector3 vector4 = (vector + vector2 + vector3) / 3f;
				vector4.z = 0f;
				if (!(colors[index].g > 0.1f) || !(colors[index2].g > 0.1f) || !(colors[index3].g > 0.1f))
				{
					continue;
				}
				float num = float.MaxValue;
				int num2 = -1;
				_ = Vector3.zero;
				for (int j = 0; j < triangles.Count - 2; j += 3)
				{
					if (j != i)
					{
						int index4 = triangles[j];
						int index5 = triangles[j + 1];
						int index6 = triangles[j + 2];
						Vector3 vector5 = verts[index4];
						Vector3 vector6 = verts[index5];
						Vector3 vector7 = verts[index6];
						Vector3 vector8 = (vector5 + vector6 + vector7) / 3f;
						vector8.z = 0f;
						float sqrMagnitude = (vector4 - vector8).sqrMagnitude;
						if (sqrMagnitude < num)
						{
							num = sqrMagnitude;
							num2 = j;
						}
					}
				}
				if (num2 >= 0 && colors[triangles[num2]].g > 0.1f && colors[triangles[num2 + 1]].g > 0.1f && colors[triangles[num2 + 2]].g > 0.1f)
				{
					if (num2 < i)
					{
						mesh.SetTriangleMaterial(i, 0, 1);
						mesh.SetTriangleMaterial(num2, 0, 1);
					}
					else
					{
						mesh.SetTriangleMaterial(num2, 0, 1);
						mesh.SetTriangleMaterial(i, 0, 1);
					}
					flag = true;
				}
			}
		}
	}

	private void SetGreen(List<Color> colors, int idx, float g)
	{
		Color value = colors[idx];
		value.g = g;
		colors[idx] = value;
	}

	private BDColor VertToPixel(Vector3 vert)
	{
		return heightMap.GetPixel(VTTerrainTextureConverter.VertToPixel(vert, gridPosition.x, gridPosition.y, chunkSize, mapSize, vertsPerSide));
	}
}
