using System.Collections.Generic;
using UnityEngine;

public class Curve3D
{
	public enum PathModes
	{
		Smooth,
		Linear,
		Bezier
	}

	private PathModes _pathMode;

	private Vector3[] points;

	private float[] times;

	private AnimationCurve[] curves;

	private bool useTimes;

	private float[] linearLengths;

	private bool curveReady;

	private float _approxLength = 1f;

	private BezierCurve[] beziers;

	private List<Vector3> uPoints;

	private int uPointCount;

	private bool uPartitioned;

	public PathModes pathMode
	{
		get
		{
			return _pathMode;
		}
		set
		{
			if (value != _pathMode)
			{
				_pathMode = value;
				if (_pathMode == PathModes.Smooth && curves == null)
				{
					curves = new AnimationCurve[3]
					{
						new AnimationCurve(),
						new AnimationCurve(),
						new AnimationCurve()
					};
				}
				UpdateCurve();
			}
		}
	}

	public bool isCurveReady => curveReady;

	public float approximateLength => _approxLength;

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

	public Curve3D()
	{
		_pathMode = PathModes.Smooth;
		curves = new AnimationCurve[3]
		{
			new AnimationCurve(),
			new AnimationCurve(),
			new AnimationCurve()
		};
	}

	public Curve3D(Vector3[] newPoints)
	{
		_pathMode = PathModes.Smooth;
		curves = new AnimationCurve[3]
		{
			new AnimationCurve(),
			new AnimationCurve(),
			new AnimationCurve()
		};
		SetPoints(newPoints);
	}

	public Curve3D(Vector3[] newPoints, PathModes mode)
	{
		_pathMode = mode;
		if (mode == PathModes.Smooth)
		{
			curves = new AnimationCurve[3]
			{
				new AnimationCurve(),
				new AnimationCurve(),
				new AnimationCurve()
			};
		}
		SetPoints(newPoints);
	}

	public void SetPoints(Vector3[] newPoints)
	{
		points = new Vector3[newPoints.Length];
		for (int i = 0; i < points.Length; i++)
		{
			points[i] = newPoints[i];
		}
		UpdateCurve();
	}

	public void SetPoints(Vector3[] newPoints, float[] newTimes)
	{
		points = new Vector3[newPoints.Length];
		times = new float[newTimes.Length];
		for (int i = 0; i < points.Length; i++)
		{
			points[i] = newPoints[i];
			times[i] = newTimes[i];
		}
		UpdateCurve();
	}

	public void SetPoint(int index, Vector3 newPoint)
	{
		if (index < points.Length)
		{
			if (!(points[index] == newPoint))
			{
				points[index] = newPoint;
				UpdateCurve();
			}
		}
		else
		{
			Debug.LogError("Tried to set new point in a Curve3D beyond the existing array.  Not yet implemented.");
		}
	}

	private void UpdateCurve()
	{
		curveReady = false;
		if (_pathMode == PathModes.Smooth)
		{
			for (int i = 0; i < 3; i++)
			{
				curves[i] = new AnimationCurve();
			}
		}
		if (points.Length < 2)
		{
			return;
		}
		useTimes = times != null;
		if (_pathMode == PathModes.Smooth)
		{
			for (int j = 0; j < points.Length; j++)
			{
				SetAnimKey(time: (!useTimes) ? ((float)j * (1f / ((float)points.Length - 1f))) : times[j], index: j, point: points[j]);
			}
		}
		else if (_pathMode == PathModes.Linear)
		{
			linearLengths = new float[points.Length];
			for (int k = 1; k < points.Length; k++)
			{
				linearLengths[k] = Vector3.Distance(points[k], points[k - 1]);
			}
		}
		else if (_pathMode == PathModes.Bezier)
		{
			SetupBeziers();
		}
		curveReady = true;
		_approxLength = ApproximateLength(0.005f);
		uPartitioned = false;
	}

	private void SetupBeziers()
	{
		bool flag = false;
		beziers = new BezierCurve[points.Length - 1];
		Vector3 vector = Vector3.zero;
		for (int i = 0; i < points.Length - 1; i++)
		{
			Vector3 vector2 = points[i];
			Vector3 vector3 = points[i + 1];
			if (i == points.Length - 2 && flag)
			{
				continue;
			}
			Vector3 vector4;
			if (i == 0)
			{
				if (points.Length > 2)
				{
					Vector3 normalized = (points[2] - vector2).normalized;
					vector4 = vector3 - normalized * (vector2 - vector3).magnitude / 2f;
				}
				else
				{
					vector4 = Vector3.Lerp(vector2, vector3, 0.5f);
				}
			}
			else
			{
				Vector3 normalized2 = (vector2 - vector).normalized;
				float num = (vector2 - vector3).magnitude / 2f;
				vector4 = vector2 + normalized2 * num;
				float magnitude = (vector3 - vector4).magnitude;
				float num2 = (num + magnitude) / 2f;
				vector4 = vector2 + normalized2 * num2;
			}
			beziers[i] = new BezierCurve(vector2, vector4, vector3);
			vector = vector4;
		}
	}

	private Vector3 GetBezierPoint(float t)
	{
		t = Mathf.Clamp01(t);
		int num = beziers.Length;
		int num2 = Mathf.Min(Mathf.FloorToInt(t * (float)num), num - 1);
		float num3 = t - (float)num2 / (float)num;
		num3 *= (float)num;
		return beziers[num2].GetPoint(num3);
	}

	public void DrawBezierGizmos(Transform transform)
	{
		Matrix4x4 matrix = Gizmos.matrix;
		Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
		if (_pathMode == PathModes.Bezier && curveReady)
		{
			for (int i = 0; i < beziers.Length; i++)
			{
				BezierCurve bezierCurve = beziers[i];
				Gizmos.DrawLine(bezierCurve.startPt, bezierCurve.midPt);
				Gizmos.DrawLine(bezierCurve.midPt, bezierCurve.endPt);
			}
		}
		Gizmos.matrix = matrix;
	}

	private void SetAnimKey(int index, Vector3 point, float time)
	{
		if (index >= curves[0].keys.Length)
		{
			curves[0].AddKey(time, point.x);
			curves[1].AddKey(time, point.y);
			curves[2].AddKey(time, point.z);
		}
		else
		{
			curves[0].MoveKey(index, new Keyframe(time, point.x));
			curves[1].MoveKey(index, new Keyframe(time, point.y));
			curves[2].MoveKey(index, new Keyframe(time, point.z));
		}
	}

	public Vector3 GetPoint(float time)
	{
		if (!curveReady)
		{
			Debug.LogWarning("Curve was accessed but it was not properly initialized.");
			return Vector3.zero;
		}
		if (_pathMode == PathModes.Linear)
		{
			float num = 0f;
			for (int i = 1; i < points.Length; i++)
			{
				float num2 = linearLengths[i] / _approxLength + num;
				if (time < num2)
				{
					float t = Mathf.InverseLerp(num, num2, time);
					return Vector3.Lerp(points[i - 1], points[i], t);
				}
				num = num2;
			}
			return points[points.Length - 1];
		}
		if (_pathMode == PathModes.Bezier)
		{
			return GetBezierPoint(time);
		}
		if (uPartitioned)
		{
			return GetSmoothUPoint(time);
		}
		float x = curves[0].Evaluate(time);
		float y = curves[1].Evaluate(time);
		float z = curves[2].Evaluate(time);
		return new Vector3(x, y, z);
	}

	public Vector3 GetTangent(float time)
	{
		if (!curveReady)
		{
			Debug.LogError("Curve was accessed but it was not properly initialized.");
			return Vector3.one;
		}
		return (GetPoint(Mathf.Min(time + 0.01f, 1f)) - GetPoint(Mathf.Min(time - 0.01f, 1f))).normalized;
	}

	public float GetClosestTime2(Vector3 position, int iterations)
	{
		float num = 0f;
		float num2 = 0.5f;
		Vector3 vector = GetPoint(num);
		for (int i = 0; i < iterations; i++)
		{
			float num3 = (position - vector).sqrMagnitude;
			Vector3 point = GetPoint(num - num2);
			float sqrMagnitude = (position - point).sqrMagnitude;
			if (sqrMagnitude < num3)
			{
				vector = point;
				num3 = sqrMagnitude;
				num -= num2;
			}
			Vector3 point2 = GetPoint(num + num2);
			if ((position - point2).sqrMagnitude < num3)
			{
				vector = point2;
				num3 = sqrMagnitude;
				num += num2;
			}
			num2 /= 2f;
		}
		return num;
	}

	public float GetClosestTime(Vector3 position, int iterations, bool loop = false)
	{
		iterations = Mathf.Clamp(iterations, 1, 15);
		int i = 0;
		float num = 0f;
		int num2 = 50;
		float num3 = 0.02f;
		float num4 = 0f;
		float num5 = float.MaxValue;
		for (; i < iterations; i++)
		{
			float num6 = num4;
			for (int j = 0; j <= num2; j++)
			{
				Vector3 point = GetPoint(num6);
				float sqrMagnitude = (position - point).sqrMagnitude;
				if (sqrMagnitude < num5)
				{
					num5 = sqrMagnitude;
					num = num6;
				}
				num6 = ((!loop) ? Mathf.Min(num6 + num3, 1f) : Mathf.Repeat(num6 + num3, 1f));
			}
			num4 = num - num3 / 2f;
			if (loop)
			{
				num4 = Mathf.Repeat(num4, 1f);
			}
			_ = num3 / 2f;
			num3 *= 0.25f;
			num2 = 4;
		}
		return Mathf.Clamp01(num);
	}

	private float ApproximateLength(float tInterval)
	{
		float num = 0f;
		if (_pathMode == PathModes.Smooth || _pathMode == PathModes.Bezier)
		{
			float num2 = 0f;
			Vector3 a = GetPoint(num2);
			while (num2 < 1f)
			{
				num2 = Mathf.Min(num2 + tInterval, 1f);
				Vector3 point = GetPoint(num2);
				num += Vector3.Distance(a, point);
				a = point;
			}
		}
		else
		{
			for (int i = 1; i < linearLengths.Length; i++)
			{
				num += linearLengths[i];
			}
		}
		return num;
	}

	public void UniformlyParition(float intervalDistance)
	{
		if (uPartitioned || _pathMode == PathModes.Linear)
		{
			return;
		}
		if (pathMode == PathModes.Bezier)
		{
			List<float> list = new List<float>();
			float num = float.MaxValue;
			for (int i = 0; i < beziers.Length; i++)
			{
				float num2 = beziers[i].EstimateLength(5);
				num = Mathf.Min(num, num2);
				list.Add(num2);
			}
			List<BezierCurve> list2 = new List<BezierCurve>();
			for (int j = 0; j < beziers.Length; j++)
			{
				int subdivisions = Mathf.Max(1, Mathf.RoundToInt(list[j] / num));
				beziers[j].SubdivideNonAlloc(subdivisions, list2, clearList: false);
			}
			beziers = list2.ToArray();
		}
		else
		{
			float num3 = 0.1f * (intervalDistance / approximateLength);
			float num4 = intervalDistance * intervalDistance;
			uPoints = new List<Vector3>();
			Vector3 vector = GetPoint(0f);
			uPoints.Add(vector);
			float num5 = 0f;
			Vector3 vector2 = vector;
			while (num5 < 1f)
			{
				num5 += num3;
				vector2 = GetPoint(num5);
				if ((vector - vector2).sqrMagnitude >= num4)
				{
					uPoints.Add(vector2);
					vector = vector2;
				}
			}
			uPoints.Add(GetPoint(1f));
			uPointCount = uPoints.Count;
		}
		uPartitioned = true;
	}

	private Vector3 GetSmoothUPoint(float t)
	{
		if (uPoints == null || uPoints.Count == 0)
		{
			return Vector3.zero;
		}
		if (t >= 1f)
		{
			return uPoints[uPointCount - 1];
		}
		if (t <= 0f)
		{
			return uPoints[0];
		}
		float num = t * (float)(uPointCount - 1);
		int num2 = Mathf.FloorToInt(num);
		num -= (float)num2;
		return Vector3.Lerp(uPoints[num2], uPoints[num2 + 1], num);
	}
}
