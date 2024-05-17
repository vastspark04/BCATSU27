using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct IntVector2 : IEquatable<IntVector2>, IComparer<IntVector2>
{
	public int x;

	public int y;

	public static IntVector2 zero => new IntVector2(0, 0);

	public static IntVector2 up => new IntVector2(0, 1);

	public static IntVector2 down => new IntVector2(0, -1);

	public static IntVector2 left => new IntVector2(-1, 0);

	public static IntVector2 right => new IntVector2(1, 0);

	public IntVector2(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public bool Equals(IntVector2 other)
	{
		if (x == other.x)
		{
			return y == other.y;
		}
		return false;
	}

	public int Compare(IntVector2 x, IntVector2 y)
	{
		if (x.x == y.x && x.y == y.y)
		{
			return 0;
		}
		return x.x.CompareTo(y.x);
	}

	public static int MaxOffset(IntVector2 a, IntVector2 b)
	{
		return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
	}

	public override string ToString()
	{
		return "(" + x + ", " + y + ")";
	}

	public static IntVector2 operator +(IntVector2 a, IntVector2 b)
	{
		return new IntVector2(a.x + b.x, a.y + b.y);
	}

	public static IntVector2 operator -(IntVector2 a, IntVector2 b)
	{
		return new IntVector2(a.x - b.x, a.y - b.y);
	}

	public static Vector2 operator +(Vector2 a, IntVector2 b)
	{
		return new Vector2(a.x + (float)b.x, a.y + (float)b.y);
	}

	public static IntVector2 operator *(IntVector2 v, int m)
	{
		return new IntVector2(v.x * m, v.y * m);
	}

	public static IntVector2 operator *(int m, IntVector2 v)
	{
		return new IntVector2(v.x * m, v.y * m);
	}

	public static bool operator ==(IntVector2 a, IntVector2 b)
	{
		if (a.x == b.x)
		{
			return a.y == b.y;
		}
		return false;
	}

	public static bool operator !=(IntVector2 a, IntVector2 b)
	{
		if (a.x == b.x)
		{
			return a.y != b.y;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is IntVector2))
		{
			return false;
		}
		return this == (IntVector2)obj;
	}

	public override int GetHashCode()
	{
		return (x % 7 + y % 13) * ((x + y) % 3) + (x - y);
	}

	public Vector2 ToVector2()
	{
		return new Vector2(x, y);
	}

	public IntVector2 Repeat(int size)
	{
		return new IntVector2(x % size, y % size);
	}
}
