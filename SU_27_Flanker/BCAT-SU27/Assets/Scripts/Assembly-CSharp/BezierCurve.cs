using System;
using System.Collections.Generic;
using UnityEngine;

public struct BezierCurve
{
	public Vector3 startPt;

	public Vector3 midPt;

	public Vector3 endPt;

	public BezierCurve(Vector3 startPt, Vector3 midPt, Vector3 endPt)
	{
		this.startPt = startPt;
		this.midPt = midPt;
		this.endPt = endPt;
	}

	public BezierCurve(Vector3 startPoint, Vector3 endPoint, Vector3 tangent1, Vector3 tangent2)
	{
		Vector3 vector = endPoint - startPoint;
		float magnitude = vector.magnitude;
		float num = Vector3.Angle(-tangent2, -vector);
		float f = num * ((float)Math.PI / 180f);
		float num2 = magnitude * Mathf.Cos(f);
		float num3 = magnitude * Mathf.Sin(f);
		float num4 = Vector3.Angle(tangent1, vector) - (90f - num);
		float num5 = num3 * Mathf.Tan(num4 * ((float)Math.PI / 180f));
		Vector3 vector2 = endPoint - tangent2.normalized * (num2 + num5);
		startPt = startPoint;
		midPt = vector2;
		endPt = endPoint;
		if (num3 == 0f)
		{
			midPt = Vector3.Lerp(startPt, endPt, 0.5f);
		}
	}

	public Vector3 GetPoint(float t)
	{
		t = Mathf.Clamp01(t);
		Vector3 a = Vector3.Lerp(startPt, midPt, t);
		Vector3 b = Vector3.Lerp(midPt, endPt, t);
		return Vector3.Lerp(a, b, t);
	}

	public Vector3 GetTangent(float t)
	{
		return Vector3.Lerp(midPt - startPt, endPt - midPt, t).normalized;
	}

	public float EstimateLength(int subdivisions)
	{
		float num = 0f;
		for (int i = 0; i < subdivisions; i++)
		{
			float t = (float)i / (float)subdivisions;
			float t2 = (float)(i + 1) / (float)subdivisions;
			Vector3 point = GetPoint(t);
			Vector3 point2 = GetPoint(t2);
			num += (point2 - point).magnitude;
		}
		return num;
	}

	public BezierCurve[] SplitCurve(float t)
	{
		if (t == 0f || t == 1f)
		{
			Debug.LogError("Can not split a bezier curve at t = 0 or t = 1.");
			return null;
		}
		BezierCurve[] array = new BezierCurve[2];
		Vector3 point = GetPoint(t);
		Vector3 tangent = GetTangent(0f);
		Vector3 tangent2 = GetTangent(t);
		array[0] = new BezierCurve(startPt, point, tangent, tangent2);
		array[1] = new BezierCurve(tangent2: GetTangent(1f), startPoint: point, endPoint: endPt, tangent1: tangent2);
		return array;
	}

	public BezierCurve[] SplitCurve(float t, float gapDist, int estimateSubdivs = 10)
	{
		float num = 0.5f * gapDist / EstimateLength(estimateSubdivs);
		return new BezierCurve[2]
		{
			SubCurve(0f, t - num),
			SubCurve(t + num, 1f)
		};
	}

	public float GetClosestTime(Vector3 position, int iterations)
	{
		iterations = Mathf.Clamp(iterations, 1, 15);
		int i = 0;
		float num = 0f;
		float num2 = 0.02f;
		float num3 = 0f;
		float num4 = 1f;
		float num5 = float.MaxValue;
		for (; i < iterations; i++)
		{
			for (float num6 = num3; num6 < num4 + num2; num6 += num2)
			{
				float sqrMagnitude = (position - GetPoint(num6)).sqrMagnitude;
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

	public BezierCurve SubCurve(float startT, float endT)
	{
		Vector3 point = GetPoint(startT);
		Vector3 point2 = GetPoint(endT);
		Vector3 tangent = GetTangent(startT);
		Vector3 tangent2 = GetTangent(endT);
		return new BezierCurve(point, point2, tangent, tangent2);
	}

	public BezierCurve[] Subdivide(int subdivisions)
	{
		if (subdivisions < 1)
		{
			Debug.LogError("Can not subdivide bezier curve to less than 1 subdivision.");
			return null;
		}
		if (subdivisions == 1)
		{
			return new BezierCurve[1]
			{
				new BezierCurve(startPt, midPt, endPt)
			};
		}
		BezierCurve[] array = new BezierCurve[subdivisions];
		for (int i = 0; i < subdivisions; i++)
		{
			float t = (float)i / (float)subdivisions;
			float t2 = (float)(i + 1) / (float)subdivisions;
			Vector3 point = GetPoint(t);
			Vector3 point2 = GetPoint(t2);
			Vector3 tangent = GetTangent(t);
			Vector3 tangent2 = GetTangent(t2);
			array[i] = new BezierCurve(point, point2, tangent, tangent2);
		}
		return array;
	}

	public void SubdivideNonAlloc(int subdivisions, List<BezierCurve> list, bool clearList = true)
	{
		if (clearList)
		{
			list.Clear();
		}
		if (subdivisions < 1)
		{
			Debug.LogError("Can not subdivide bezier curve to less than 1 subdivision.");
			return;
		}
		if (subdivisions == 1)
		{
			list.Add(new BezierCurve(startPt, midPt, endPt));
			return;
		}
		for (int i = 0; i < subdivisions; i++)
		{
			float t = (float)i / (float)subdivisions;
			float t2 = (float)(i + 1) / (float)subdivisions;
			Vector3 point = GetPoint(t);
			Vector3 point2 = GetPoint(t2);
			Vector3 tangent = GetTangent(t);
			Vector3 tangent2 = GetTangent(t2);
			list.Add(new BezierCurve(point, point2, tangent, tangent2));
		}
	}
}
