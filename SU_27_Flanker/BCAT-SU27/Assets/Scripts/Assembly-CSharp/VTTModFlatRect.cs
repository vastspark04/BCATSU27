using UnityEngine;

public class VTTModFlatRect : VTTerrainMod
{
	public Transform transform;

	public Matrix4x4 terrainToLocalMatrix;

	public Bounds bounds;

	public float radius;

	public IntVector2 gridSpace;

	public Vector3 tSpacePos;

	public override bool AppliesToChunk(VTMapGenerator.VTTerrainChunk chunk)
	{
		Vector3 vector = new Vector3(VTMapGenerator.fetch.chunkSize, 0f, VTMapGenerator.fetch.chunkSize);
		Vector3 center = VTMapGenerator.fetch.GridToWorldPos(chunk.grid) + vector / 2f;
		vector.y = float.MaxValue;
		Bounds bounds = new Bounds(center, vector);
		Vector3 size = (1.5f * Mathf.Max(this.bounds.size.x, this.bounds.size.z) + radius) * Vector3.one;
		size.y = float.MaxValue;
		Vector3 center2 = VTMapGenerator.fetch.GridToWorldPos(gridSpace) + Quaternion.Inverse(Quaternion.Euler(-90f, 0f, 180f)) * tSpacePos;
		return new Bounds(center2, size).Intersects(bounds);
	}

	public override void ApplyHeightMod(VTTerrainJob job, VTTerrainMesh mesh)
	{
		float y = bounds.center.y;
		Vector3 size = bounds.size;
		size.y = float.MaxValue;
		bounds.size = size;
		int count = mesh.verts.Count;
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = mesh.verts[i];
			Vector3 vector2 = TerrainToLocalPoint(job, vector);
			if (bounds.Contains(vector2))
			{
				vector.z = y;
			}
			else
			{
				float t = Mathf.Clamp01(DistFromEdge(bounds, vector2) / radius);
				vector.z = Mathf.Lerp(y, vector.z, t);
			}
			mesh.verts[i] = vector;
		}
	}

	public override void ApplyColorMod(VTTerrainJob job, VTTerrainMesh mesh)
	{
		Vector3 size = bounds.size;
		size.y = float.MaxValue;
		bounds.size = size;
		int count = mesh.verts.Count;
		for (int i = 0; i < count; i++)
		{
			Vector3 terrainVert = mesh.verts[i];
			Vector3 vector = TerrainToLocalPoint(job, terrainVert);
			if (bounds.Contains(vector))
			{
				mesh.treeValues[i] = 0f;
			}
			float num = 1f;
			if (!bounds.Contains(vector))
			{
				float num2 = DistFromEdge(bounds, vector);
				num = 1f - Mathf.Clamp01(num2 / radius);
			}
			mesh.colors[i] = Color.Lerp(mesh.colors[i], new Color(0f, mesh.colors[i].g, 0f, 1f), Mathf.Pow(mesh.colors[i].r * num, 2f));
		}
	}

	private Vector3 TerrainToLocalPoint(VTTerrainJob job, Vector3 terrainVert)
	{
		Vector3 vector = job.chunkSize * (job.gridPosition - gridSpace).ToVector2();
		vector.x *= -1f;
		return terrainToLocalMatrix * (terrainVert - tSpacePos + vector);
	}

	private float DistFromEdge(Bounds bounds, Vector3 vert)
	{
		Vector3 a = vert;
		a.y = 0f;
		if (vert.z > bounds.center.z + bounds.extents.z)
		{
			if (vert.x < bounds.center.x - bounds.extents.x)
			{
				return Vector3.Distance(b: new Vector3(bounds.center.x - bounds.extents.x, 0f, bounds.center.z + bounds.extents.z), a: a);
			}
			if (vert.x > bounds.center.x + bounds.extents.x)
			{
				return Vector3.Distance(b: new Vector3(bounds.center.x + bounds.extents.x, 0f, bounds.center.z + bounds.extents.z), a: a);
			}
			return a.z - (bounds.center.z + bounds.extents.z);
		}
		if (vert.z < bounds.center.z - bounds.extents.z)
		{
			if (vert.x < bounds.center.x - bounds.extents.x)
			{
				return Vector3.Distance(b: new Vector3(bounds.center.x - bounds.extents.x, 0f, bounds.center.z - bounds.extents.z), a: a);
			}
			if (vert.x > bounds.center.x + bounds.extents.x)
			{
				return Vector3.Distance(b: new Vector3(bounds.center.x + bounds.extents.x, 0f, bounds.center.z - bounds.extents.z), a: a);
			}
			return bounds.center.z - bounds.extents.z - a.z;
		}
		if (vert.x < bounds.center.x - bounds.extents.x)
		{
			return bounds.center.x - bounds.extents.x - a.x;
		}
		return a.x - (bounds.center.x + bounds.extents.x);
	}
}
