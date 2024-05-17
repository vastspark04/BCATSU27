using System;
using UnityEngine;

namespace TerrainComposer2{

[Serializable]
public struct Int3
{
	public int x;

	public int y;

	public int z;

	public Int3(int x, int y, int z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public Int3(float x, float y, float z)
	{
		this.x = (int)x;
		this.y = (int)y;
		this.z = (int)z;
	}

	public Int3(Vector3 v)
	{
		x = (int)v.x;
		y = (int)v.y;
		z = (int)v.z;
	}
}
}