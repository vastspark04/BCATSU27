using UnityEngine;

public static class ColorUtils
{
	public static Color From255(float r, float g, float b, float a)
	{
		return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
	}

	public static Color FromVector3(Vector3 colorVec, bool clamp = false)
	{
		if (clamp)
		{
			return new Color(Mathf.Clamp01(colorVec.x), Mathf.Clamp01(colorVec.y), Mathf.Clamp01(colorVec.z), 1f);
		}
		return new Color(colorVec.x, colorVec.y, colorVec.z, 1f);
	}

	public static Vector3 ToVector3(Color color)
	{
		return new Vector3(color.r, color.g, color.b);
	}

	public static Color AvgColor(Color[] colors)
	{
		Color color = new Color(0f, 0f, 0f, 0f);
		if (colors == null || colors.Length == 0)
		{
			return color;
		}
		for (int i = 0; i < colors.Length; i++)
		{
			color += colors[i];
		}
		return color / colors.Length;
	}
}
