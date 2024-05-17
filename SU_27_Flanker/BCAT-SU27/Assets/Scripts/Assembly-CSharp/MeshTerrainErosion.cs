using System.Collections.Generic;
using UnityEngine;

public class MeshTerrainErosion
{
	public int dropletMaxLifetime = 30;

	public float initialSpeed = 1f;

	public float initialWaterAmt = 1f;

	public float inertia = 0.02f;

	public float dropletCapacityRate = 4f;

	public float minSedimentCap = 0.01f;

	public float depositRate = 0.3f;

	public float erosionRate = 0.3f;

	public float gravity = 4f;

	public float evaporationRate = 0.01f;

	public int erosionRadius = 3;

	public int resolution = 3072;

	public float maxHeight = 6000f;

	public float pixelSize = 153.6f;

	private List<Vector3> vertices = new List<Vector3>();

	private float lastErosionRadius = -1f;

	private float weightSum = 1f;

	private float depositWeightSum;

	private bool debug;

	private VTLineDrawer drawer;

	private bool hasDrawer;

	private VTMapGenerator.VTTerrainChunk lastChunk;

	private List<IntVector2> chunksToRecalc;

	private int[] depositBuffer = new int[4];

	private List<VTMapGenerator.VTTerrainChunk> otherChunks = new List<VTMapGenerator.VTTerrainChunk>();

	private List<Vector3> otherVerts = new List<Vector3>();

	public VTLineDrawer lineDrawer
	{
		get
		{
			return drawer;
		}
		set
		{
			drawer = value;
			hasDrawer = value != null;
		}
	}

	public void RunDroplet(Vector3 brushWorldPos, float radius, List<IntVector2> chunksToRecalc, bool debug = false)
	{
		this.chunksToRecalc = chunksToRecalc;
		this.debug = debug;
		if (lastErosionRadius != (float)erosionRadius)
		{
			weightSum = CalculateBrushWeightSum(erosionRadius);
			lastErosionRadius = erosionRadius;
			depositWeightSum = CalculateBrushWeightSum(1f);
		}
		Vector3 vector = brushWorldPos + Random.insideUnitSphere * radius;
		if (hasDrawer)
		{
			vector.y = WaterPhysics.instance.height + VTMapGenerator.fetch.GetHeightmapAltitude(vector);
		}
		Vector3 vector2 = Vector2.zero;
		float num = initialSpeed;
		float num2 = initialWaterAmt;
		float sediment = 0f;
		lastChunk = null;
		for (int i = 0; i < dropletMaxLifetime; i++)
		{
			Vector3 worldPos = vector;
			VTMapGenerator.VTTerrainChunk terrainChunk = VTMapGenerator.fetch.GetTerrainChunk(vector);
			Mesh mesh = terrainChunk.sharedMeshes[0];
			if (terrainChunk != lastChunk)
			{
				if (lastChunk != null)
				{
					lastChunk.sharedMeshes[0].SetVertices(vertices);
				}
				mesh.GetVertices(vertices);
				lastChunk = terrainChunk;
				if (!chunksToRecalc.Contains(terrainChunk.grid))
				{
					chunksToRecalc.Add(terrainChunk.grid);
				}
			}
			Vector3 vector3 = terrainChunk.lodObjects[0].transform.InverseTransformPoint(vector);
			float height;
			Vector2 gradient = GetGradient(vector, vector3, out height);
			vector2.Normalize();
			vector2.x = vector2.x * inertia - gradient.x * (1f - inertia);
			vector2.y = vector2.y * inertia - gradient.y * (1f - inertia);
			vector2.Normalize();
			vector2 *= pixelSize;
			vector.x += vector2.x;
			vector.z += vector2.y;
			if (vector2.x == 0f && vector2.y == 0f)
			{
				break;
			}
			if (hasDrawer)
			{
				vector.y = WaterPhysics.instance.height - 80f + height * maxHeight;
				Color color = Color.Lerp(Color.green, Color.red, (float)i / (float)(dropletMaxLifetime - 1));
				drawer.DrawLine(vector, vector + new Vector3(0f, 50f, 0f), color);
				drawer.DrawLine(terrainChunk.lodObjects[0].transform.TransformPoint(vector3) + new Vector3(0f, 50f, 0f), vector + new Vector3(0f, 50f, 0f), color);
			}
			float num3 = GetHeight(terrainChunk.lodObjects[0].transform.InverseTransformPoint(vector)) - height;
			float num4 = Mathf.Max((0f - num3) * num * num2 * dropletCapacityRate, minSedimentCap);
			if (sediment > num4 || num3 > 0f)
			{
				float num5 = ((num3 > 0f) ? Mathf.Min(num3, sediment) : ((sediment - num4) * depositRate));
				if (debug)
				{
					Debug.Log("Depositing " + num5);
				}
				Erode(0f - num5, worldPos, vector3, 1f, ref sediment);
			}
			else
			{
				float amount = Mathf.Min((num4 - sediment) * erosionRate, 0f - num3);
				Erode(amount, worldPos, vector3, erosionRadius, ref sediment);
			}
			num = Mathf.Sqrt(num * num + num3 * gravity);
			num2 *= 1f - evaporationRate;
		}
		if (lastChunk != null)
		{
			lastChunk.sharedMeshes[0].SetVertices(vertices);
		}
	}

	private void FillDeposit(float depositAmt, Vector3 localPos)
	{
		for (int i = 0; i < 4; i++)
		{
			depositBuffer[i] = -1;
		}
		int num = 0;
		for (int j = 0; j < vertices.Count; j++)
		{
			if (num >= 4)
			{
				break;
			}
			Vector3 vector = vertices[j];
			float num2 = Mathf.Abs(0f - vector.x - (0f - localPos.x)) / pixelSize;
			float num3 = Mathf.Abs(vector.y - localPos.y) / pixelSize;
			if (!(num2 < 1f) || !(num3 < 1f))
			{
				continue;
			}
			if (vector.x < localPos.x)
			{
				if (vector.y < localPos.y)
				{
					depositBuffer[1] = j;
					num++;
				}
				else
				{
					depositBuffer[3] = j;
					num++;
				}
			}
			else if (vector.y < localPos.y)
			{
				depositBuffer[0] = j;
				num++;
			}
			else
			{
				depositBuffer[2] = j;
				num++;
			}
		}
		float num4;
		float num5;
		if (depositBuffer[0] >= 0)
		{
			num4 = 0f - localPos.x - (0f - vertices[depositBuffer[0]].x);
			num5 = localPos.y - vertices[depositBuffer[0]].y;
		}
		else if (depositBuffer[1] >= 0)
		{
			num4 = 0f - localPos.x - (0f - vertices[depositBuffer[1]].x - pixelSize);
			num5 = localPos.y - vertices[depositBuffer[1]].y;
		}
		else if (depositBuffer[2] >= 0)
		{
			num4 = 0f - localPos.x - (0f - vertices[depositBuffer[2]].x);
			num5 = localPos.y - (vertices[depositBuffer[0]].y - pixelSize);
		}
		else
		{
			Debug.Log("vert not found for fill deposit");
			num4 = 0f;
			num5 = 0f;
		}
		num4 /= pixelSize;
		num5 /= pixelSize;
		float z = maxHeight * depositAmt * (1f - num4) * (1f - num5);
		float z2 = maxHeight * depositAmt * num4 * (1f - num5);
		float z3 = maxHeight * depositAmt * (1f - num4) * num5;
		float z4 = maxHeight * depositAmt * num4 * num5;
		if (depositBuffer[0] >= 0)
		{
			vertices[depositBuffer[0]] += new Vector3(0f, 0f, z);
		}
		if (depositBuffer[1] >= 0)
		{
			vertices[depositBuffer[1]] += new Vector3(0f, 0f, z2);
		}
		if (depositBuffer[2] >= 0)
		{
			vertices[depositBuffer[2]] += new Vector3(0f, 0f, z3);
		}
		if (depositBuffer[3] >= 0)
		{
			vertices[depositBuffer[3]] += new Vector3(0f, 0f, z4);
		}
	}

	private void Erode(float amount, Vector3 worldPos, Vector3 localPos, float radius, ref float sediment)
	{
		float num = radius * pixelSize;
		for (int i = 0; i < vertices.Count; i++)
		{
			Vector3 vector = vertices[i];
			float sqrMagnitude = (vector - localPos).sqrMagnitude;
			if (sqrMagnitude < num * num)
			{
				float num2 = Mathf.Sqrt(sqrMagnitude);
				float num3 = 1f - num2 / num;
				num3 /= weightSum;
				float num4 = amount * num3;
				vector.z -= num4 * maxHeight;
				sediment += num4;
				vertices[i] = vector;
			}
		}
		GetAffectedTerrainChunks(worldPos, num, otherChunks);
		foreach (VTMapGenerator.VTTerrainChunk otherChunk in otherChunks)
		{
			if (otherChunk == lastChunk)
			{
				continue;
			}
			otherChunk.sharedMeshes[0].GetVertices(otherVerts);
			bool flag = false;
			for (int j = 0; j < otherVerts.Count; j++)
			{
				Vector3 vector2 = otherVerts[j];
				float sqrMagnitude2 = (vector2 - localPos).sqrMagnitude;
				if (sqrMagnitude2 < num * num)
				{
					float num5 = Mathf.Sqrt(sqrMagnitude2);
					float num6 = 1f - num5 / num;
					num6 /= weightSum;
					float num7 = amount * num6;
					vector2.z -= num7 * maxHeight;
					sediment += num7;
					otherVerts[j] = vector2;
					flag = true;
				}
			}
			if (flag && !chunksToRecalc.Contains(otherChunk.grid))
			{
				chunksToRecalc.Add(otherChunk.grid);
			}
		}
	}

	private Vector2 GetGradient(Vector3 worldPos, Vector3 localPos, out float height)
	{
		height = GetHeight(localPos);
		float x = GetHeight(localPos + new Vector3(0f - pixelSize, 0f, 0f)) - height;
		float y = GetHeight(localPos + new Vector3(0f, pixelSize, 0f)) - height;
		return new Vector2(x, y);
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

	private float GetHeight(Vector3 localPos)
	{
		for (int i = 0; i < 4; i++)
		{
			depositBuffer[i] = -1;
		}
		float num = 0f;
		int num2 = 0;
		for (int j = 0; j < vertices.Count; j++)
		{
			if (num2 >= 4)
			{
				break;
			}
			Vector3 vector = vertices[j];
			float num3 = Mathf.Abs(0f - vector.x - (0f - localPos.x)) / pixelSize;
			float num4 = Mathf.Abs(vector.y - localPos.y) / pixelSize;
			if (!(num3 < 1f) || !(num4 < 1f))
			{
				continue;
			}
			num = vector.z / maxHeight;
			if (vector.x < localPos.x)
			{
				if (vector.y < localPos.y)
				{
					depositBuffer[1] = j;
					num2++;
				}
				else
				{
					depositBuffer[3] = j;
					num2++;
				}
			}
			else if (vector.y < localPos.y)
			{
				depositBuffer[0] = j;
				num2++;
			}
			else
			{
				depositBuffer[2] = j;
				num2++;
			}
		}
		float num5;
		float num6;
		if (depositBuffer[0] >= 0)
		{
			num5 = 0f - localPos.x - (0f - vertices[depositBuffer[0]].x);
			num6 = localPos.y - vertices[depositBuffer[0]].y;
		}
		else if (depositBuffer[1] >= 0)
		{
			num5 = 0f - localPos.x - (0f - vertices[depositBuffer[1]].x - pixelSize);
			num6 = localPos.y - vertices[depositBuffer[1]].y;
		}
		else if (depositBuffer[2] >= 0)
		{
			num5 = 0f - localPos.x - (0f - vertices[depositBuffer[2]].x);
			num6 = localPos.y - (vertices[depositBuffer[0]].y - pixelSize);
		}
		else
		{
			Debug.Log("vert not found for fill deposit");
			num5 = 0f;
			num6 = 0f;
		}
		num5 /= pixelSize;
		num6 /= pixelSize;
		Vector4 vector2 = Vector4.one * num;
		if (depositBuffer[0] >= 0)
		{
			vector2.x = vertices[depositBuffer[0]].z / maxHeight;
		}
		if (depositBuffer[1] >= 0)
		{
			vector2.y = vertices[depositBuffer[1]].z / maxHeight;
		}
		if (depositBuffer[2] >= 0)
		{
			vector2.z = vertices[depositBuffer[2]].z / maxHeight;
		}
		if (depositBuffer[3] >= 0)
		{
			vector2.w = vertices[depositBuffer[3]].z / maxHeight;
		}
		float num7 = num5;
		float num8 = num6;
		return vector2.x * (1f - num7) * (1f - num8) + vector2.y * num7 * (1f - num8) + vector2.z * (1f - num7) * num8 + vector2.w * num7 * num8;
	}

	private void GetAffectedTerrainChunks(Vector3 worldPos, float radius, List<VTMapGenerator.VTTerrainChunk> list)
	{
		list.Clear();
		VTMapGenerator mapGenerator = VTCustomMapManager.instance.mapGenerator;
		IntVector2 intVector = mapGenerator.ChunkGridAtPos(worldPos);
		int num = Mathf.CeilToInt(radius / mapGenerator.chunkSize);
		for (int i = intVector.x - num; i <= intVector.x + num; i++)
		{
			for (int j = intVector.y - num; j <= intVector.y + num; j++)
			{
				if (Mathf.Min(i, j) >= 0 && Mathf.Max(i, j) < mapGenerator.gridSize)
				{
					list.Add(mapGenerator.GetTerrainChunk(i, j));
				}
			}
		}
	}
}
