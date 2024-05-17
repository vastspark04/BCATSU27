using System;
using UnityEngine;

public struct BezierCurveD : ICurve
{
	public Vector3D startPt;

	public Vector3D midPt;

	public Vector3D endPt;

	public BezierCurveD(Vector3D startPt, Vector3D midPt, Vector3D endPt)
	{
		this.startPt = startPt;
		this.midPt = midPt;
		this.endPt = endPt;
	}

	public BezierCurveD(Vector3D startPoint, Vector3D endPoint, Vector3 tangent1, Vector3 tangent2)
	{
		Vector3 toVector = (endPoint - startPoint).toVector3;
		float magnitude = toVector.magnitude;
		float num = Vector3.Angle(-tangent2, -toVector);
		float f = num * ((float)Math.PI / 180f);
		float num2 = magnitude * Mathf.Cos(f);
		float num3 = magnitude * Mathf.Sin(f);
		float num4 = Vector3.Angle(tangent1, toVector) - (90f - num);
		float num5 = num3 * Mathf.Tan(num4 * ((float)Math.PI / 180f));
		Vector3D vector3D = endPoint - tangent2.normalized * (num2 + num5);
		startPt = startPoint;
		midPt = vector3D;
		endPt = endPoint;
		if (num3 == 0f)
		{
			midPt = Vector3D.Lerp(startPt, endPt, 0.5f);
		}
	}

	public Vector3D GetPoint(float t)
	{
		t = Mathf.Clamp01(t);
		Vector3D a = Vector3D.Lerp(startPt, midPt, t);
		Vector3D b = Vector3D.Lerp(midPt, endPt, t);
		return Vector3D.Lerp(a, b, t);
	}

	public Vector3 GetTangent(float t)
	{
		return Vector3.Lerp((midPt - startPt).toVector3, (endPt - midPt).toVector3, t).normalized;
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

	public BezierCurveD[] SplitCurve(float t)
	{
		if (t == 0f || t == 1f)
		{
			Debug.LogError("Can not split a bezier curve at t = 0 or t = 1.");
			return null;
		}
		BezierCurveD[] array = new BezierCurveD[2];
		Vector3D point = GetPoint(t);
		Vector3 tangent = GetTangent(0f);
		Vector3 tangent2 = GetTangent(t);
		array[0] = new BezierCurveD(startPt, point, tangent, tangent2);
		array[1] = new BezierCurveD(tangent2: GetTangent(1f), startPoint: point, endPoint: endPt, tangent1: tangent2);
		return array;
	}

	public BezierCurveD[] SplitCurve(float t, float gapDist, int estimateSubdivs = 10)
	{
		float num = 0.5f * gapDist / (float)EstimateLength(estimateSubdivs);
		return new BezierCurveD[2]
		{
			SubCurve(0f, t - num),
			SubCurve(t + num, 1f)
		};
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

	public BezierCurveD SubCurve(float startT, float endT)
	{
		Vector3D point = GetPoint(startT);
		Vector3D point2 = GetPoint(endT);
		Vector3 tangent = GetTangent(startT);
		Vector3 tangent2 = GetTangent(endT);
		return new BezierCurveD(point, point2, tangent, tangent2);
	}

	public BezierCurveD[] Subdivide(int subdivisions)
	{
		if (subdivisions < 1)
		{
			Debug.LogError("Can not subdivide bezier curve to less than 1 subdivision.");
			return null;
		}
		if (subdivisions == 1)
		{
			return new BezierCurveD[1]
			{
				new BezierCurveD(startPt, midPt, endPt)
			};
		}
		BezierCurveD[] array = new BezierCurveD[subdivisions];
		double num = EstimateLength(subdivisions);
		double num2 = num / (double)subdivisions;
		float num3 = (float)(num2 / 10.0) / (float)num;
		int num4 = 0;
		float t = 0f;
		double num5 = 0.0;
		Vector3D vector3D = GetPoint(0f);
		for (float num6 = num3; num6 < 1f; num6 += num3)
		{
			if (num4 >= subdivisions)
			{
				break;
			}
			Vector3D point = GetPoint(num6);
			num5 += (point - vector3D).magnitude;
			vector3D = point;
			if (num4 == subdivisions - 1)
			{
				num6 = 1f;
			}
			if (num5 >= num2 || num4 == subdivisions - 1)
			{
				Vector3D point2 = GetPoint(t);
				Vector3D point3 = GetPoint(num6);
				Vector3 tangent = GetTangent(t);
				Vector3 tangent2 = GetTangent(num6);
				array[num4] = new BezierCurveD(point2, point3, tangent, tangent2);
				num4++;
				t = num6;
				num5 = 0.0;
			}
		}
		if (num4 == subdivisions - 1)
		{
			Vector3D point4 = GetPoint(t);
			Vector3D point5 = GetPoint(1f);
			Vector3 tangent3 = GetTangent(t);
			Vector3 tangent4 = GetTangent(1f);
			array[num4] = new BezierCurveD(point4, point5, tangent3, tangent4);
		}
		return array;
	}
}
