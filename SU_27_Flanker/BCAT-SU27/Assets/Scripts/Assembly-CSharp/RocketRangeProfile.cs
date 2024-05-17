using UnityEngine;

[CreateAssetMenu]
public class RocketRangeProfile : ScriptableObject
{
	public Texture2D tex;

	public float minElevation;

	public float maxElevation;

	public float maxRange;

	public float maxTime;

	public float GetAngle(float targetElevation, float range)
	{
		float v = Mathf.InverseLerp(minElevation, maxElevation, targetElevation);
		float u = range / maxRange;
		return tex.GetPixelBilinear(u, v).r * 45f;
	}

	public float GetTime(float targetElevation, float range)
	{
		float v = Mathf.InverseLerp(minElevation, maxElevation, targetElevation);
		float u = range / maxRange;
		return tex.GetPixelBilinear(u, v).g * maxTime;
	}
}
