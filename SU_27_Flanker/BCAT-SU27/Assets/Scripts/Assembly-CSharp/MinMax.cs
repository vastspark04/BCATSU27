using System;
using UnityEngine;

[Serializable]
public struct MinMax
{
	public float min;

	public float max;

	public MinMax(float min, float max)
	{
		this.min = min;
		this.max = max;
	}

	public float Random()
	{
		return UnityEngine.Random.Range(min, max);
	}

	public float MidPoint()
	{
		return Mathf.Lerp(min, max, 0.5f);
	}

	public float Lerp(float t)
	{
		return Mathf.Lerp(min, max, t);
	}
}
