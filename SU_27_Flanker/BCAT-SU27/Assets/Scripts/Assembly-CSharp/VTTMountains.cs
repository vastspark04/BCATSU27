using System;
using UnityEngine;

public class VTTMountains : VTTerrainJob
{
	private float maxHeightContinent = 100f;

	[VTTerrain("Islands Size", 1f, 10f)]
	public float mainNoiseScale = 10f;

	private float secNoiseScale = 12f;

	private float tertNoiseScale = 3f;

	[VTTerrain("Mountain Height", 0f, 4000f)]
	public float maxMtnHeight = 4000f;

	private float cellMtnScale = 12f;

	private int detailOctaves = 6;

	private float detailScale = 16f;

	private float detailIntensity = 190f;

	private float snowNoiseScale = 300f;

	private float riverDepth = 5555f;

	[VTTerrain("Water Scale", 0.25f, 15f)]
	public float riverScale = 1f;

	private float riverPower = 12f;

	private float riverPerturbAmp = 1f;

	private float riverPerturbFreq = 49f;

	private float riverOctaveRate = 1.5f;

	protected override float CalculateHeight(Vector2 uv)
	{
		float num = chunkSize / 3000f;
		IntVector2 intVector = gridPosition - new IntVector2(mapSize / 2, mapSize / 2);
		Vector3 vector = (uv + intVector.ToVector2()) * chunkSize;
		uv *= num;
		uv += intVector.ToVector2() * num;
		float magnitude = vector.magnitude;
		float num2 = (float)mapSize * chunkSize / 2f;
		float num3 = Mathf.Clamp01((num2 - magnitude) / (num2 / 2f));
		if (num3 <= 0f)
		{
			return maxHeightContinent;
		}
		float num4 = 11f - Mathf.Clamp(mainNoiseScale, 1f, 10f);
		double num5 = (noiseModule.GetSimplexFractal(num4 * uv.x, num4 * uv.y) + 1.0) / 2.0;
		num5 /= 0.9359999895095825;
		double num6 = noiseModule.GetSimplexFractal(secNoiseScale * (uv.x + 669.345f), secNoiseScale * (uv.y + 699.159f)) / 0.5360000133514404;
		num6 = (num6 + 1.0) / 2.0;
		num5 *= num6;
		double x = Clamp01(num5);
		if (!(num5 > 0.0))
		{
			num5 *= 8.0;
		}
		num5 *= (double)maxHeightContinent;
		if (num5 > 0.0)
		{
			noiseModule.SetCellularJitter(0.5f);
			noiseModule.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Euclidean);
			noiseModule.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Div);
			double num7 = 3.0 * (noiseModule.GetCellular(cellMtnScale * (uv.x + 123f), cellMtnScale * (uv.y + 321f)) + 0.5 * noiseModule.GetCellular(cellMtnScale * 2f * (uv.x + 321f), cellMtnScale * 2f * (uv.y + 123f)));
			double num8 = Math.Pow(x, 1.5) * (double)maxMtnHeight * num7;
			num5 += num8;
			noiseModule.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Add);
			double num9 = 185.0;
			double num10 = 1.0 + Lerp(0.20000000298023224, 0.03999999910593033, num5 / (double)maxMtnHeight) * noiseModule.GetCellular(num9 * (double)uv.x, num9 * (double)uv.y);
			num5 *= num10;
		}
		float x2 = uv.x;
		float y = uv.y;
		PerturbCoords(ref x2, ref y, riverPerturbAmp, riverPerturbFreq, riverOctaveRate, 6);
		x2 *= riverScale;
		y *= riverScale;
		noiseModule.SetNoiseType(FastNoise.NoiseType.Cellular);
		noiseModule.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
		noiseModule.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Div);
		noiseModule.SetCellularJitter(0.685f);
		double x3 = Clamp01(noiseModule.GetNoise(x2, y));
		double num11 = Math.Max(0.0, Lerp(riverDepth, 0.0, 4.0 * num5 / (double)maxMtnHeight));
		x3 = Math.Pow(x3, riverPower) * num11;
		num5 -= x3;
		for (int i = 1; i <= detailOctaves; i++)
		{
			double simplexFractal = noiseModule.GetSimplexFractal(detailScale * (float)i * (uv.x + 4816f), detailScale * (float)i * (uv.y + 1345f));
			num5 += (double)detailIntensity * simplexFractal / (double)(1f + 0.25f * (float)i);
		}
		num5 = Lerp(maxHeightContinent, num5, num3);
		return (float)num5;
	}

	private void PerturbCoords(ref float x, ref float y, float amp, float freq, float octaveRate = 2f, int octaves = 1)
	{
		double perlin = noiseModule.GetPerlin(x * freq, y * freq);
		double perlin2 = noiseModule.GetPerlin((x + 1263.234f) * freq, (y - 642.234f) * freq);
		x += (float)perlin * amp;
		y += (float)perlin2 * amp;
		if (octaves >= 1)
		{
			PerturbCoords(ref x, ref y, amp / octaveRate, freq * octaveRate, octaveRate, octaves - 1);
		}
	}

	protected override void CalculateColors(VTTerrainMesh mesh)
	{
		Vector2 vector = gridPosition.ToVector2();
		Vector3 forward = Vector3.forward;
		for (int i = 0; i < mesh.vertCount; i++)
		{
			Vector3 vector2 = mesh.verts[i];
			Vector3 lhs = mesh.normals[i];
			float num = Vector3.Dot(lhs, forward);
			Color a = new Color(1f, 0f, 0f, 0f);
			Vector2 vector3 = mesh.uvs[i] + vector;
			float t = 1f - Mathf.Clamp01(Mathf.Abs(vector2.z) / 2500f);
			a = Color.Lerp(a, new Color(0f, 0f, 0f, 1f), t);
			a = Color.Lerp(new Color(0f, 1f, 0f, 0f), a, (vector2.z + 10f + (0f - num) * 5f) / (num * 80f + 25f));
			a = Color.Lerp(new Color(0f, 1f, 0f, 0f), a, Pow(num + 0.045f, 25));
			float f = Mathf.Clamp01(vector2.z * (1f + 1f * (float)noiseModule.GetPerlinFractal(vector3.x * snowNoiseScale, vector3.y * snowNoiseScale)) / 2300f);
			f = Mathf.Pow(f, 10f);
			a = Color.Lerp(a, new Color(0f, 0f, 1f, 0f), f);
			a = Color.Lerp(new Color(0f, 1f, 0f, 0f), a, Pow(Vector3.Dot(lhs, forward) + 0.4f, 105));
			mesh.colors[i] = a;
		}
	}

	private float Pow(float val, int powr)
	{
		for (int i = 1; i < powr; i++)
		{
			val *= val;
		}
		return val;
	}
}
