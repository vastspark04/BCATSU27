using System.Diagnostics;
using UnityEngine;

[ExecuteInEditMode]
public class CGSkyFogGen : MonoBehaviour
{
	public Texture2D tex;

	public Color fogColor;

	public Color groundColor;

	public float fadeRate = 1f;

	[Range(0f, 1f)]
	public float startHeight = 0.5f;

	public void Apply()
	{
		int width = tex.width;
		int num = Mathf.FloorToInt((float)tex.height * startHeight);
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		for (int i = 0; i < width; i++)
		{
			Color color = tex.GetPixel(i, num);
			float num2 = 0f;
			for (int num3 = num; num3 >= 0; num3--)
			{
				tex.SetPixel(i, num3, color);
				num2 = Mathf.Lerp(num2, 1f, fadeRate * 0.01f);
				color = ((!(num2 < 0.35f)) ? Color.Lerp(color, groundColor, fadeRate * 0.01f * num2) : Color.Lerp(color, fogColor, fadeRate * 0.01f * num2));
				if (stopwatch.Elapsed.Seconds > 10)
				{
					num3 = -1;
					i = width;
				}
			}
		}
		tex.Apply();
	}
}
