using System.Collections.Generic;
using UnityEngine;

public struct BezierCurveD5 : ICurve, IConfigValue
{
	public Vector3D startPt;

	public Vector3D startTanPt;

	public Vector3D midPt;

	public Vector3D endTanPt;

	public Vector3D endPt;

	private bool _isDefined;

	public bool isDefined => _isDefined;

	public BezierCurveD5(Vector3D startPt, Vector3D startTanPt, Vector3D midPt, Vector3D endTanPt, Vector3D endPt)
	{
		this.startPt = startPt;
		this.startTanPt = startTanPt;
		this.midPt = midPt;
		this.endTanPt = endTanPt;
		this.endPt = endPt;
		_isDefined = true;
	}

	public Vector3D GetPoint(float t)
	{
		if (!_isDefined)
		{
			Debug.LogError("A bezier curve point was accessed before it was defined!");
		}
		t = Mathf.Clamp01(t);
		Vector3D a = Vector3D.Lerp(startPt, startTanPt, t);
		Vector3D vector3D = Vector3D.Lerp(startTanPt, midPt, t);
		Vector3D vector3D2 = Vector3D.Lerp(midPt, endTanPt, t);
		Vector3D b = Vector3D.Lerp(endTanPt, endPt, t);
		Vector3D a2 = Vector3D.Lerp(a, vector3D, t);
		Vector3D vector3D3 = Vector3D.Lerp(vector3D, vector3D2, t);
		Vector3D b2 = Vector3D.Lerp(vector3D2, b, t);
		Vector3D a3 = Vector3D.Lerp(a2, vector3D3, t);
		Vector3D b3 = Vector3D.Lerp(vector3D3, b2, t);
		return Vector3D.Lerp(a3, b3, t);
	}

	public Vector3 GetTangent(float t)
	{
		if (!_isDefined)
		{
			Debug.LogError("A bezier curve tangent was accessed before it was defined!");
		}
		t = Mathf.Clamp(t, 0f, 0.995f);
		float t2 = t + 0.005f;
		return (GetPoint(t2) - GetPoint(t)).normalized.toVector3;
	}

	public double EstimateLength(int subdivisions)
	{
		double num = 0.0;
		for (int i = 0; i < subdivisions; i++)
		{
			float t = (float)i / (float)subdivisions;
			float t2 = (float)(i + 1) / (float)subdivisions;
			Vector3D point = GetPoint(t);
			Vector3D point2 = GetPoint(t2);
			num += (point2 - point).magnitude;
		}
		return num;
	}

	public float GetClosestTime(Vector3D position, int iterations)
	{
		if (!_isDefined)
		{
			Debug.LogError("A bezier curve time was accessed before it was defined!");
		}
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

	private double Clamp01(double d)
	{
		if (d < 0.0)
		{
			return 0.0;
		}
		if (d > 1.0)
		{
			return 1.0;
		}
		return d;
	}

	public string WriteValue()
	{
		return ConfigNodeUtils.WriteList(new List<Vector3D>(5) { startPt, startTanPt, midPt, endTanPt, endPt });
	}

	public void ConstructFromValue(string s)
	{
		List<Vector3D> list = ConfigNodeUtils.ParseList<Vector3D>(s);
		startPt = list[0];
		startTanPt = list[1];
		midPt = list[2];
		endTanPt = list[3];
		endPt = list[4];
	}
}
