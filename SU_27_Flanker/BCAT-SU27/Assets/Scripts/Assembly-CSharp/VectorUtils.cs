using System;
using UnityEngine;

public static class VectorUtils
{
	public static Vector3 ClampDistance(Vector3 clampThis, Vector3 toThis, float distance)
	{
		Vector3 vector = Vector3.ClampMagnitude(clampThis - toThis, distance);
		return toThis + vector;
	}

	public static Vector3 RandomDirectionDeviation(Vector3 direction, float maxAngle)
	{
		return Vector3.RotateTowards(direction, UnityEngine.Random.rotation * direction, UnityEngine.Random.Range(0f, maxAngle * ((float)Math.PI / 180f)), 0f).normalized;
	}

	public static Vector3 WeightedDirectionDeviation(Vector3 direction, float maxAngle)
	{
		float num = UnityEngine.Random.Range(0f, 1f);
		float maxRadiansDelta = maxAngle * (num * num) * ((float)Math.PI / 180f);
		return Vector3.RotateTowards(direction, Vector3.ProjectOnPlane(UnityEngine.Random.onUnitSphere, direction), maxRadiansDelta, 0f).normalized;
	}

	public static bool PointIsOnScreen(Vector3 worldPoint)
	{
		Vector3 vector = Camera.main.WorldToViewportPoint(worldPoint);
		return !(vector.x > 1f) && !(vector.x < 0f) && !(vector.y > 1f) && !(vector.y < 0f);
	}

	public static Vector3 AveragePosition(Vector3[] vertices)
	{
		Vector3 vector = vertices[0];
		int num = vertices.Length;
		for (int i = 1; i < num; i++)
		{
			vector += vertices[i];
		}
		return vector / num;
	}

	public static float SignedAngle(Vector3 fromDirection, Vector3 toDirection, Vector3 referenceRight)
	{
		float num = Vector3.Angle(fromDirection, toDirection);
		return Mathf.Sign(Vector3.Dot(toDirection, referenceRight)) * num;
	}

	public static float SignedProjectionSqrMag(Vector3 vector, Vector3 onNormal)
	{
		Vector3 vector2 = Vector3.Project(vector, onNormal);
		return (float)((!(Vector3.Angle(onNormal, vector) > 90f)) ? 1 : (-1)) * vector2.sqrMagnitude;
	}

	public static Vector3[] PositionsFromTransforms(Transform[] transforms, int start, int end)
	{
		Vector3[] array = new Vector3[end - start + 1];
		int num = 0;
		for (int i = start; i <= end; i++)
		{
			array[num] = transforms[i].position;
			num++;
		}
		return array;
	}

	public static float MaxAxialDistance(Vector3 from, Vector3 to)
	{
		float num = Mathf.Abs(from.x - to.x);
		float num2 = Mathf.Abs(from.y - to.y);
		float num3 = Mathf.Abs(from.z - to.z);
		return Mathf.Max(num, num2, num3);
	}

	public static float MinAxialDistance(Vector3 from, Vector3 to)
	{
		float num = Mathf.Abs(from.x - to.x);
		float num2 = Mathf.Abs(from.y - to.y);
		float num3 = Mathf.Abs(from.z - to.z);
		return Mathf.Min(num, num2, num3);
	}

	public static float CalculateLeadTime(Vector3 delta, Vector3 relV, float muzzleV)
	{
		float num = Vector3.Dot(relV, relV) - muzzleV * muzzleV;
		float num2 = 2f * Vector3.Dot(relV, delta);
		float num3 = Vector3.Dot(delta, delta);
		float num4 = num2 * num2 - 4f * num * num3;
		if (num4 > 0f)
		{
			return 2f * num3 / (Mathf.Sqrt(num4) - num2);
		}
		return -1f;
	}

	public static float FullRangePerlinNoise(float x, float y)
	{
		return (Mathf.PerlinNoise(x, y) - 0.5f) * 2f;
	}

	public static bool BallisticDirection(Vector3 targetPosition, Vector3 firingPosition, float speed, bool direct, out Vector3 direction)
	{
		Vector3 up = Vector3.up;
		Vector3 vector = Vector3.ProjectOnPlane(targetPosition - firingPosition, up);
		float num = speed * speed;
		float num2 = num * num;
		float magnitude = Physics.gravity.magnitude;
		float num3 = targetPosition.y - firingPosition.y;
		float sqrMagnitude = vector.sqrMagnitude;
		float num4 = Mathf.Sqrt(sqrMagnitude);
		float num5 = ((!direct) ? 1 : (-1));
		float num6 = num + num5 * Mathf.Sqrt(num2 - magnitude * (magnitude * sqrMagnitude + 2f * num3 * num));
		float num7 = magnitude * num4;
		float num8 = Mathf.Atan(num6 / num7);
		if (!float.IsNaN(num8))
		{
			direction = (Quaternion.AngleAxis(num8 * 57.29578f, Vector3.Cross(vector, up)) * vector).normalized;
			return true;
		}
		direction = Vector3.zero;
		return false;
	}

	public static float Bearing(Vector3 direction)
	{
		return Bearing(Vector3.zero, direction);
	}

	public static float Bearing(Vector3 fromPt, Vector3 toPt)
	{
		Vector3 toDirection = toPt - fromPt;
		toDirection.y = 0f;
		float num = SignedAngle(Vector3.forward, toDirection, Vector3.right);
		if (num < 0f)
		{
			num += 360f;
		}
		return num;
	}

	public static Vector3 BearingVector(float bearing)
	{
		return Quaternion.AngleAxis(bearing, Vector3.up) * Vector3.forward;
	}

	public static float Triangle(float t)
	{
		t += 0.5f;
		float num = Mathf.Repeat(t, 1f);
		if (Mathf.FloorToInt(t) % 2 != 0)
		{
			num = 1f - num;
		}
		return (num - 0.5f) * 2f;
	}

	public static bool SphereRayIntersect(Ray ray, Vector3 sphereCenter, float sphereRadius, out float distance)
	{
		Vector3 origin = ray.origin;
		float num = Vector3.Dot(ray.direction, origin - sphereCenter);
		float num2 = 0f - (num + Mathf.Sqrt(num * num - (origin - sphereCenter).sqrMagnitude + sphereRadius * sphereRadius));
		if (float.IsNaN(num2))
		{
			distance = 0f;
			return false;
		}
		distance = num2;
		return true;
	}
}
