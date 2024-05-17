using UnityEngine;

public struct BDColor
{
	public float r;

	public float g;

	public float b;

	public float a;

	public BDColor(Color uColor)
	{
		r = uColor.r;
		g = uColor.g;
		b = uColor.b;
		a = uColor.a;
	}

	public BDColor(float r, float g, float b, float a)
	{
		this.r = r;
		this.g = g;
		this.b = b;
		this.a = a;
	}

	public static BDColor Lerp(BDColor a, BDColor b, float t)
	{
		return new BDColor(Lerp(a.r, b.r, t), Lerp(a.g, b.g, t), Lerp(a.b, b.b, t), Lerp(a.a, b.a, t));
	}

	private static float Lerp(float a, float b, float t)
	{
		return a + (b - a) * t;
	}

	public Color ToColor()
	{
		return new Color(r, g, b, a);
	}

	public Vector4 ToVector()
	{
		return new Vector4(r, g, b, a);
	}
}
