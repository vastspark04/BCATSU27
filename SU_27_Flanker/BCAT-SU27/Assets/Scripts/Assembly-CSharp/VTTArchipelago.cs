using System;
using UnityEngine;

public class VTTArchipelago : VTTerrainJob
{
	[Serializable]
	public class ColorProfile
	{
		public float snowHeightAdjust = 700f;

		public int cliffPower = 25;

		public float cliffBias = 0.045f;

		public int snowCliffPower = 105;

		public float snowCliffBias = 0.4f;

		public float forestPerturbAmp = 35f;

		public float forestSize = 1f;

		public float forestThreshold;
	}

	private float maxHeightContinent = 1255f;

	private float heightAdjust = 255f;

	[VTTerrain("Islands Size", 1f, 10f)]
	public float mainNoiseScale = 6f;

	private float secNoiseScale = 15f;

	[VTTerrain("Mountain Height", 0f, 4000f)]
	public float maxMtnHeight = 4000f;

	private float cellMtnScale = 12f;

	private int detailOctaves = 6;

	private float detailScale = 16f;

	private float detailIntensity = 190f;

	private const float SNOW_SCALE = 100f;

	private float riverDepth = 10000f;

	private float riverScale = 9f;

	private float riverPower = 9f;

	private float riverPerturbAmp = 1f;

	private float riverPerturbFreq = 49f;

	private float riverOctaveRate = 1.5f;

	private const float FOREST_SCALE = 120f;

	private static ColorProfile defaultColorProfile;

	protected override float CalculateHeight(Vector2 uvF)
	{
		Vector3D vector3D = new Vector3D(uvF.x, uvF.y, 0.0);
		double num = (double)chunkSize / 3000.0;
		if (vector3D.x < 0.0010000000474974513 || vector3D.x > 0.9990000128746033 || vector3D.y < 0.0010000000474974513)
		{
			_ = 1;
		}
		else
			_ = vector3D.y > 0.9990000128746033;
		IntVector2 intVector = gridPosition - new IntVector2(mapSize / 2, mapSize / 2);
		Vector3D vector3D2 = (vector3D + intVector.ToVector2()) * chunkSize;
		vector3D *= num;
		vector3D += new Vector3D(intVector.x, intVector.y, 0.0) * num;
		double num2 = 11.0 - Clamp(mainNoiseScale, 1.0, 10.0);
		double simplexFractal = noiseModule.GetSimplexFractal(num2 * vector3D.x, num2 * vector3D.y);
		simplexFractal /= 0.936;
		double num3 = noiseModule.GetSimplexFractal((double)secNoiseScale * (vector3D.x + 669.345), (double)secNoiseScale * (vector3D.y + 699.159)) / 0.536;
		num3 = (num3 + 1.0) / 2.0;
		simplexFractal *= num3;
		double x = Clamp01(simplexFractal);
		if (simplexFractal < 0.0)
		{
			simplexFractal *= 8.0;
		}
		simplexFractal *= (double)maxHeightContinent;
		if (simplexFractal > 0.0)
		{
			noiseModule.SetCellularJitter(0.5f);
			noiseModule.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Euclidean);
			noiseModule.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Div);
			double num4 = 3.0 * (noiseModule.GetCellular((double)cellMtnScale * (vector3D.x + 123.0), (double)cellMtnScale * (vector3D.y + 321.0)) + 0.5 * noiseModule.GetCellular((double)(cellMtnScale * 2f) * (vector3D.x + 321.0), (double)(cellMtnScale * 2f) * (vector3D.y + 123.0)));
			double num5 = Math.Pow(x, 1.5) * (double)maxMtnHeight * num4;
			simplexFractal += num5;
			noiseModule.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Add);
			double num6 = 185.0;
			double num7 = 1.0 + Lerp(0.2, 0.04, simplexFractal / (double)maxMtnHeight) * noiseModule.GetCellular(num6 * vector3D.x, num6 * vector3D.y);
			simplexFractal *= num7;
		}
		double x2 = vector3D.x;
		double y = vector3D.y;
		PerturbCoords(noiseModule, ref x2, ref y, riverPerturbAmp, riverPerturbFreq, riverOctaveRate, 6);
		x2 *= (double)riverScale;
		y *= (double)riverScale;
		noiseModule.SetNoiseType(FastNoise.NoiseType.Cellular);
		noiseModule.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
		noiseModule.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Div);
		noiseModule.SetCellularJitter(0.685f);
		double x3 = Clamp01(noiseModule.GetNoise(x2, y));
		double num8 = Math.Max(0.0, Lerp(riverDepth, 0.0, 4.0 * simplexFractal / (double)maxMtnHeight));
		x3 = Math.Pow(x3, riverPower) * num8;
		simplexFractal -= x3;
		for (int i = 1; i <= detailOctaves; i++)
		{
			double simplexFractal2 = noiseModule.GetSimplexFractal((double)(detailScale * (float)i) * (vector3D.x + 4816.0), (double)(detailScale * (float)i) * (vector3D.y + 1345.0));
			simplexFractal += (double)detailIntensity * simplexFractal2 / (1.0 + 0.25 * (double)i);
		}
		simplexFractal += (double)heightAdjust;
		double magnitude = vector3D2.magnitude;
		double num9 = (float)mapSize * chunkSize / 2f;
		double t = Clamp01((num9 - magnitude) / (num9 / 2.0));
		simplexFractal = Lerp(-50.0, simplexFractal, t);
		return (float)simplexFractal;
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

	protected override void CalculateColors(VTTerrainMesh mesh)
	{
		if (defaultColorProfile == null)
		{
			defaultColorProfile = new ColorProfile();
		}
		Vector2 vector = gridPosition.ToVector2();
		_ = Vector3.forward;
		for (int i = 0; i < mesh.vertCount; i++)
		{
			Vector3 vert = mesh.verts[i];
			Vector3 normal = mesh.normals[i];
			Vector2 uV = mesh.uvs[i] + vector;
			float treeValue;
			Color value = CalculateColor(vert, normal, uV, noiseModule, out treeValue, defaultColorProfile);
			mesh.treeValues[i] = treeValue;
			mesh.colors[i] = value;
		}
	}

	public static Color CalculateColor(Vector3 vert, Vector3 normal, Vector2 m_UV, FastNoise noiseModule, out float treeValue, ColorProfile profile)
	{
		float num = Vector3.Dot(normal, Vector3.forward);
		Color a = new Color(0f, 0f, 0f, 1f);
		noiseModule.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
		noiseModule.SetCellularReturnType(FastNoise.CellularReturnType.CellValue);
		noiseModule.SetFrequency(0.009999999776482582);
		double x = 1f / profile.forestSize * 120f * m_UV.x;
		double y = 1f / profile.forestSize * 120f * m_UV.y;
		PerturbCoords(noiseModule, ref x, ref y, profile.forestPerturbAmp, 4f);
		a = Color.Lerp(b: Color.Lerp(t: (noiseModule.GetCellular(x, y) > (double)profile.forestThreshold) ? 1 : 0, a: a, b: new Color(1f, 0f, 0f, 0f)), a: new Color(0f, 1f, 0f, 0f), t: Mathf.Clamp01((vert.z + 20f) / 55f));
		float num2 = Pow(num + profile.cliffBias, profile.cliffPower);
		a = Color.Lerp(new Color(0f, 1f, 0f, 0f), a, num2);
		float num3 = vert.z + profile.snowHeightAdjust;
		num3 *= 1f + 1f * (float)noiseModule.GetPerlinFractal(m_UV.x * 100f, m_UV.y * 100f);
		a = Color.Lerp(a, new Color(0f, 1f, 0f, 0f), Pow(num3 / 2000f, 6));
		float val = Mathf.Clamp01(num3 / Mathf.Lerp(2300f, 1200f, 1f - num2));
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

	private static float Pow(float val, int powr)
	{
		for (int i = 1; i < powr; i++)
		{
			val *= val;
		}
		return val;
	}
}
