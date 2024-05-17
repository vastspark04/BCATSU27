using System.Collections.Generic;
using UnityEngine;

public class LinearPathD
{
	private Vector3D[] points;

	private float[] linearLengths;

	private bool curveReady;

	private float _approxLength = 1f;

	public bool isCurveReady => curveReady;

	public float length => _approxLength;

	public int pointCount
	{
		get
		{
			if (points != null)
			{
				return points.Length;
			}
			return 0;
		}
	}

	public LinearPathD(Vector3D[] newPoints)
	{
		SetPoints(newPoints);
	}

	public void SetPoints(Vector3D[] newPoints)
	{
		points = new Vector3D[newPoints.Length];
		for (int i = 0; i < points.Length; i++)
		{
			points[i] = newPoints[i];
		}
		UpdateCurve();
	}

	public void SetPoints(Vector3D[] newPoints, float[] newTimes)
	{
		points = new Vector3D[newPoints.Length];
		for (int i = 0; i < points.Length; i++)
		{
			points[i] = newPoints[i];
		}
		UpdateCurve();
	}

	public void SetPoint(int index, Vector3D newPoint)
	{
		if (index < points.Length)
		{
			points[index] = newPoint;
			UpdateCurve();
		}
		else
		{
			Debug.LogError("Tried to set new point in a Curve3D beyond the existing array.  Not yet implemented.");
		}
	}

	private void UpdateCurve()
	{
		curveReady = false;
		if (points.Length >= 2)
		{
			if (linearLengths == null || linearLengths.Length != points.Length)
			{
				linearLengths = new float[points.Length];
			}
			for (int i = 1; i < points.Length; i++)
			{
				linearLengths[i] = (float)(points[i] - points[i - 1]).magnitude;
			}
			curveReady = true;
			_approxLength = ApproximateLength();
		}
	}

	public Vector3D GetPoint(float time)
	{
		if (!curveReady)
		{
			Debug.LogWarning("Curve was accessed but it was not properly initialized.");
			return Vector3D.zero;
		}
		float num = 0f;
		for (int i = 1; i < points.Length; i++)
		{
			float num2 = linearLengths[i] / _approxLength + num;
			if (time < num2)
			{
				float t = Mathf.InverseLerp(num, num2, time);
				return Vector3D.Lerp(points[i - 1], points[i], t);
			}
			num = num2;
		}
		return points[points.Length - 1];
	}

	public Vector3D GetIndexPoint(int idx)
	{
		return points[idx];
	}

	public Vector3 GetTangent(float time)
	{
		if (!curveReady)
		{
			Debug.LogError("Curve was accessed but it was not properly initialized.");
			return Vector3.one;
		}
		return (GetPoint(Mathf.Min(time + 0.01f, 1f)).toVector3 - GetPoint(Mathf.Min(time - 0.01f, 1f)).toVector3).normalized;
	}

	public float GetClosestTime2(Vector3D position, int iterations)
	{
		float num = 0f;
		float num2 = 0.5f;
		Vector3D vector3D = GetPoint(num);
		for (int i = 0; i < iterations; i++)
		{
			double num3 = (position - vector3D).sqrMagnitude;
			Vector3D point = GetPoint(num - num2);
			double sqrMagnitude = (position - point).sqrMagnitude;
			if (sqrMagnitude < num3)
			{
				vector3D = point;
				num3 = sqrMagnitude;
				num -= num2;
			}
			Vector3D point2 = GetPoint(num + num2);
			if ((position - point2).sqrMagnitude < num3)
			{
				vector3D = point2;
				num3 = sqrMagnitude;
				num += num2;
			}
			num2 /= 2f;
		}
		return num;
	}

	public float GetClosestTime(Vector3D position, int iterations)
	{
		iterations = Mathf.Clamp(iterations, 1, 15);
		int i = 0;
		float num = 0f;
		float num2 = 0.02f;
		float num3 = 0f;
		float num4 = 1f;
		double num5 = double.MaxValue;
		for (; i < iterations; i++)
		{
			for (float num6 = num3; num6 < num4 + num2; num6 += num2)
			{
				double sqrMagnitude = (position - GetPoint(num6)).sqrMagnitude;
				if (sqrMagnitude < num5)
				{
					num5 = sqrMagnitude;
					num = num6;
				}
			}
			num3 = num - num2;
			num4 = num + num2;
			num2 *= 0.25f;
		}
		return Mathf.Clamp01(num);
	}

	private float ApproximateLength()
	{
		float num = 0f;
		for (int i = 1; i < linearLengths.Length; i++)
		{
			num += linearLengths[i];
		}
		return num;
	}

	public ConfigNode SaveToConfigNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		List<Vector3D> list = new List<Vector3D>();
		for (int i = 0; i < points.Length; i++)
		{
			list.Add(points[i]);
		}
		configNode.SetValue("points", list);
		return configNode;
	}

	public static LinearPathD LoadFromConfigNode(ConfigNode node)
	{
		return new LinearPathD(node.GetValue<List<Vector3D>>("points").ToArray());
	}
}
