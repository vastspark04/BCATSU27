using System;
using UnityEngine;

[Serializable]
public class SerializableTerrainMesh
{
	[Serializable]
	public class SerializableVector3
	{
		public float x;

		public float y;

		public float z;

		public SerializableVector3(Vector3 v)
		{
			x = v.x;
			y = v.y;
			z = v.z;
		}

		public Vector3 ToVector3()
		{
			return new Vector3(x, y, z);
		}
	}

	[Serializable]
	public class SerializableVector2
	{
		public float x;

		public float y;

		public SerializableVector2(Vector2 v)
		{
			x = v.x;
			y = v.y;
		}

		public Vector3 ToVector2()
		{
			return new Vector2(x, y);
		}
	}

	[Serializable]
	public class SerializableColor
	{
		public float r;

		public float g;

		public float b;

		public float a;

		public SerializableColor(Color c)
		{
			r = c.r;
			g = c.g;
			b = c.b;
			a = c.a;
		}

		public Color ToColor()
		{
			return new Color(r, g, b, a);
		}
	}

	public int gridX;

	public int gridY;

	public SerializableVector3[] verts;

	public SerializableVector3[] normals;

	public SerializableVector2[] uvs;

	public int[] triangles;

	public SerializableColor[] colors;

	public SerializableTerrainMesh(VTTerrainMesh mesh, int gridX, int gridY)
	{
		this.gridX = gridX;
		this.gridY = gridY;
		verts = new SerializableVector3[mesh.verts.Count];
		normals = new SerializableVector3[mesh.normals.Count];
		uvs = new SerializableVector2[mesh.uvs.Count];
		triangles = new int[mesh.triangles.Count];
		colors = new SerializableColor[mesh.colors.Count];
		int num = verts.Length;
		for (int i = 0; i < num; i++)
		{
			verts[i] = new SerializableVector3(mesh.verts[i]);
			normals[i] = new SerializableVector3(mesh.normals[i]);
			if (i < mesh.uvs.Count)
			{
				uvs[i] = new SerializableVector2(mesh.uvs[i]);
			}
			colors[i] = new SerializableColor(mesh.colors[i]);
		}
		int count = mesh.triangles.Count;
		for (int j = 0; j < count; j++)
		{
			triangles[j] = mesh.triangles[j];
		}
	}

	public VTTerrainMesh ToTerrainMesh()
	{
		VTTerrainMesh vTTerrainMesh = new VTTerrainMesh();
		for (int i = 0; i < vTTerrainMesh.vertCount; i++)
		{
			vTTerrainMesh.verts.Add(verts[i].ToVector3());
			vTTerrainMesh.normals.Add(normals[i].ToVector3());
			vTTerrainMesh.uvs.Add(uvs[i].ToVector2());
			vTTerrainMesh.colors.Add(colors[i].ToColor());
		}
		int count = vTTerrainMesh.triangles.Count;
		for (int j = 0; j < count; j++)
		{
			vTTerrainMesh.triangles.Add(triangles[j]);
		}
		vTTerrainMesh.gridPos = new IntVector2(gridX, gridY);
		return vTTerrainMesh;
	}
}
