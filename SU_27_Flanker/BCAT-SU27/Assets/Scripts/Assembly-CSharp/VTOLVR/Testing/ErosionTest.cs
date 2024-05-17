using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace VTOLVR.Testing{

public class ErosionTest : MonoBehaviour
{
	public MeshRenderer mr;

	private int resolution;

	public bool animate = true;

	[Header("Erosion")]
	public int dropletsPerPass = 100;

	public int passes = 1;

	public float erosionRate = 0.01f;

	public float erosionRadius = 2f;

	public float initialSpeed = 1f;

	public float minSedimentCap = 0.01f;

	public float dropletCapacityRate = 1f;

	public float evaporationRate = 0.1f;

	public int dropletMaxLifetime = 30;

	public float depositRate = 0.2f;

	public float gravity = 1f;

	public float initialWaterAmt = 2f;

	public float inertia;

	private int currPass;

	private int currDroplet;

	private float weightSum;

	private BDTexture hmbdt;

	private int passesPerWorker;

	private List<Thread> workerThreads = new List<Thread>();

	private float maxSpeed = -1f;

	private int noMovCount;

	private float maxSediment;

	private object heightmapLock = new object();

	private System.Random rand = new System.Random();

	private object randLock = new object();

	private IEnumerator Start()
	{
		Texture2D texture2D = (Texture2D)mr.sharedMaterial.GetTexture("_Heightmap");
		resolution = texture2D.width;
		weightSum = CalculateBrushWeightSum(erosionRadius);
		Texture2D modTexture = new Texture2D(resolution, resolution, texture2D.format, mipChain: false, linear: true)
		{
			filterMode = FilterMode.Bilinear
		};
		for (int i = 0; i < resolution; i++)
		{
			for (int j = 0; j < resolution; j++)
			{
				modTexture.SetPixel(i, j, texture2D.GetPixel(i, j));
			}
		}
		hmbdt = new BDTexture(modTexture);
		Material mat = mr.material;
		mr.material = mat;
		yield return new WaitForSeconds(1f);
		int threadCount = Environment.ProcessorCount - 1;
		if (passes < threadCount)
		{
			passesPerWorker = passes;
			threadCount = 1;
		}
		else
		{
			passesPerWorker = passes / threadCount;
			passes = passesPerWorker * threadCount;
		}
		for (int t = 0; t < threadCount; t++)
		{
			Thread thread = new Thread(ErosionWorker);
			workerThreads.Add(thread);
			thread.Start();
			yield return null;
		}
		while (true)
		{
			if (animate)
			{
				bool flag = false;
				lock (heightmapLock)
				{
					hmbdt.ApplyToTexture(modTexture);
					_ = currPass;
					_ = passes;
				}
				mat.SetTexture("_Heightmap", modTexture);
				if (flag)
				{
					break;
				}
			}
			else
			{
				lock (heightmapLock)
				{
					if (currPass >= passes)
					{
						break;
					}
				}
			}
			yield return new WaitForSeconds(0.02f);
		}
		lock (heightmapLock)
		{
			hmbdt.ApplyToTexture(modTexture);
		}
		mat.SetTexture("_Heightmap", modTexture);
	}

	private void ErosionWorker()
	{
		for (int i = 0; i < passesPerWorker; i++)
		{
			try
			{
				DoErosionPass(hmbdt);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
			lock (heightmapLock)
			{
				currPass++;
				if (currPass >= passes)
				{
					break;
				}
			}
			Thread.Sleep(5);
		}
	}

	private void OnDestroy()
	{
		if (workerThreads != null)
		{
			foreach (Thread workerThread in workerThreads)
			{
				if (workerThread != null && workerThread.IsAlive)
				{
					workerThread.Abort();
				}
			}
		}
		hmbdt = null;
	}

	private void DoErosionPass(BDTexture heightmap)
	{
		for (int i = 0; i < dropletsPerPass; i++)
		{
			RunDroplet(heightmap);
		}
	}

	private void RunDroplet(BDTexture heightmap)
	{
		float x;
		float y;
		lock (randLock)
		{
			x = (float)(rand.NextDouble() * (double)resolution - 1.0);
			y = (float)(rand.NextDouble() * (double)resolution - 1.0);
		}
		Vector2 pos = new Vector2(x, y);
		Vector2 zero = Vector2.zero;
		float num = initialSpeed;
		float num2 = initialWaterAmt;
		float sediment = 0f;
		for (int i = 0; i < dropletMaxLifetime; i++)
		{
			int num3 = (int)pos.x;
			int num4 = (int)pos.y;
			float cellOffsetX = pos.x - (float)num3;
			float cellOffsetY = pos.y - (float)num4;
			float height;
			Vector2 gradient = GetGradient(heightmap, pos, out height);
			zero.x = zero.x * inertia - gradient.x * (1f - inertia);
			zero.y = zero.y * inertia - gradient.y * (1f - inertia);
			zero.Normalize();
			pos += zero;
			if (pos.x < 0f || pos.y < 0f || pos.x >= (float)(resolution - 1) || pos.y >= (float)(resolution - 1) || (zero.x == 0f && zero.y == 0f))
			{
				if (zero.x == 0f && zero.y == 0f)
				{
					noMovCount++;
				}
				break;
			}
			GetGradient(heightmap, pos, out var height2);
			float num5 = height2 - height;
			float num6 = Mathf.Max((0f - num5) * num * num2 * dropletCapacityRate, minSedimentCap);
			if (sediment > num6 || num5 > 0f)
			{
				float num7 = ((num5 > 0f) ? Mathf.Min(num5, sediment) : ((sediment - num6) * depositRate));
				sediment -= num7;
				FillDeposit(heightmap, num7, cellOffsetX, cellOffsetY, num3, num4);
			}
			else
			{
				float erodeAmt = Mathf.Min((num6 - sediment) * erosionRate, 0f - num5);
				Erode(heightmap, erodeAmt, num3, num4, erosionRadius, ref sediment);
			}
			num = Mathf.Sqrt(num * num + num5 * gravity);
			num2 *= 1f - evaporationRate;
		}
		currDroplet++;
	}

	private float CalculateBrushWeightSum(float f_radius)
	{
		int num = Mathf.CeilToInt(f_radius);
		Debug.Log("Calcing brush weight sum, radius " + num);
		float num2 = 0f;
		Vector2 vector = new Vector2(0.5f, 0.5f) * resolution;
		IntVector2 intVector = new IntVector2(Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y));
		Vector2 vector2 = intVector.ToVector2();
		IntVector2 intVector2 = intVector;
		Debug.Log(" - pixCenter: " + intVector2.ToString());
		for (int i = Mathf.Max(intVector.x - num, 0); i <= Mathf.Min(intVector.x + num, resolution - 1); i++)
		{
			for (int j = Mathf.Max(intVector.y - num, 0); j <= Mathf.Min(intVector.y + num, resolution - 1); j++)
			{
				float magnitude = (vector2 - new Vector2(i, j)).magnitude;
				if (magnitude <= f_radius)
				{
					float num3 = 1f - magnitude / f_radius;
					num2 += num3;
				}
			}
		}
		Debug.Log("Calculated weight sum: " + num2);
		return num2;
	}

	private void Erode(BDTexture heightmap, float erodeAmt, int nodeX, int nodeY, float f_radius, ref float sediment)
	{
		int num = Mathf.CeilToInt(f_radius);
		IntVector2 intVector = new IntVector2(nodeX, nodeY);
		Vector2 vector = intVector.ToVector2();
		for (int i = Mathf.Max(intVector.x - num, 0); i <= Mathf.Min(intVector.x + num, resolution - 1); i++)
		{
			for (int j = Mathf.Max(intVector.y - num, 0); j <= Mathf.Min(intVector.y + num, resolution - 1); j++)
			{
				float magnitude = (vector - new Vector2(i, j)).magnitude;
				float num2 = 1f - magnitude / (float)num;
				num2 /= weightSum;
				if (num2 > float.Epsilon)
				{
					float r = heightmap.GetPixel(i, j).r;
					float num3 = erodeAmt * num2;
					float num4 = ((r < num3) ? r : num3);
					r -= num4;
					sediment += num4;
					heightmap.SetPixel(i, j, new BDColor(r, r, r, 1f));
				}
			}
		}
	}

	private void FillDeposit(BDTexture heightmap, float deposit, float cellOffsetX, float cellOffsetY, int nodeX, int nodeY)
	{
		float r = heightmap.GetPixel(nodeX, nodeY).r;
		float r2 = heightmap.GetPixel(nodeX + 1, nodeY).r;
		float r3 = heightmap.GetPixel(nodeX, nodeY + 1).r;
		float r4 = heightmap.GetPixel(nodeX + 1, nodeY + 1).r;
		r += deposit * (1f - cellOffsetX) * (1f - cellOffsetY);
		r2 += deposit * cellOffsetX * (1f - cellOffsetY);
		r3 += deposit * (1f - cellOffsetX) * cellOffsetY;
		r4 += deposit * cellOffsetX * cellOffsetY;
		heightmap.SetPixel(nodeX, nodeY, new BDColor(r, r, r, 1f));
		heightmap.SetPixel(nodeX + 1, nodeY, new BDColor(r2, r2, r2, 1f));
		heightmap.SetPixel(nodeX, nodeY + 1, new BDColor(r3, r3, r3, 1f));
		heightmap.SetPixel(nodeX + 1, nodeY + 1, new BDColor(r4, r4, r4, 1f));
	}

	private float GetHeight(Texture2D heightmap, Vector2 uv)
	{
		return heightmap.GetPixelBilinear(uv.x, uv.y).r;
	}

	private Vector3 GetNormal(Texture2D heightmap, Vector2 uv, float height)
	{
		float num = 1f / (float)resolution;
		Vector2 uv2 = uv + new Vector2(0f, num);
		Vector2 uv3 = uv + new Vector2(num, 0f);
		float num2 = 5f;
		Vector3 vector = new Vector3(uv.x, uv.y, height * num2);
		Vector3 vector2 = new Vector3(uv2.x, uv2.y, GetHeight(heightmap, uv2) * num2);
		Vector3 vector3 = Vector3.Normalize(Vector3.Cross(rhs: new Vector3(uv3.x, uv3.y, GetHeight(heightmap, uv3) * num2) - vector, lhs: vector2 - vector));
		Vector2 uv4 = uv - new Vector2(0f, num);
		Vector2 uv5 = uv - new Vector2(num, 0f);
		Vector3 vector4 = new Vector3(uv4.x, uv4.y, GetHeight(heightmap, uv4) * num2);
		Vector3 vector5 = Vector3.Normalize(Vector3.Cross(rhs: new Vector3(uv5.x, uv5.y, GetHeight(heightmap, uv5) * num2) - vector, lhs: vector4 - vector));
		return -(vector3 + vector5).normalized;
	}

	private Vector2 GetGradient(BDTexture heightmap, Vector2 pos, out float height)
	{
		int num = Mathf.FloorToInt(pos.x);
		int num2 = Mathf.FloorToInt(pos.y);
		float num3 = pos.x - (float)num;
		float num4 = pos.y - (float)num2;
		float r = heightmap.GetPixel(num, num2).r;
		float r2 = heightmap.GetPixel(num + 1, num2).r;
		float r3 = heightmap.GetPixel(num, num2 + 1).r;
		float r4 = heightmap.GetPixel(num + 1, num2 + 1).r;
		float x = (r2 - r) * (1f - num4) + (r4 - r3) * num4;
		float y = (r3 - r) * (1f - num3) + (r4 - r2) * num3;
		height = r * (1f - num3) * (1f - num4) + r2 * num3 * (1f - num4) + r3 * (1f - num3) * num4 + r4 * num3 * num4;
		return new Vector2(x, y);
	}

	private void OnGUI()
	{
		GUI.Label(new Rect(10f, 10f, 1000f, 1000f), string.Format("Pass: {0}/{3}\nDroplet: {1}\nNoMove: {2}", currPass, currDroplet, noMovCount, passes));
	}
}

}