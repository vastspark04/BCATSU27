using System.Collections;
using UnityEngine;

public class GripperHandDigit : MonoBehaviour
{
	public float rayStartRadius;

	public GripperHandDigit childDigit;

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawLine(base.transform.position, base.transform.TransformPoint(new Vector3(0f, 0f, rayStartRadius)));
	}

	public void Grab()
	{
		float num = ArcCast(base.transform, rayStartRadius, 111f, 10, 4);
		base.transform.localEulerAngles = new Vector3(0f, num, 0f);
		if ((bool)childDigit)
		{
			childDigit.Grab();
		}
		StartCoroutine(GripRoutine(num));
	}

	private IEnumerator GripRoutine(float angle)
	{
		float a = 0f;
		while (a < angle)
		{
			a = Mathf.MoveTowards(a, angle, 180f * Time.deltaTime);
			base.transform.localEulerAngles = new Vector3(0f, a, 0f);
			yield return null;
		}
	}

	private static float ArcCast(Transform transform, float radius, float maxAngle, int resolution, int lengthIntervals)
	{
		bool flag = false;
		bool flag2 = true;
		float num = maxAngle;
		for (float num2 = 1f; num2 <= (float)lengthIntervals; num2 += 1f)
		{
			float z = num2 * radius / (float)lengthIntervals;
			float angle = maxAngle / (float)resolution;
			Vector3 vector = Quaternion.AngleAxis((0f - maxAngle) / 2f, Vector3.up) * new Vector3(0f, 0f, z);
			for (int i = 0; i <= resolution * 2; i++)
			{
				Vector3 vector2 = Quaternion.AngleAxis(angle, Vector3.up) * vector;
				Vector3 start = transform.TransformPoint(vector);
				Vector3 end = transform.TransformPoint(vector2);
				if (Physics.Linecast(start, end, out var hitInfo, 1))
				{
					flag = true;
					if (i < resolution - 2)
					{
						Debug.DrawLine(start, end, Color.blue, 5f);
						break;
					}
					Debug.DrawLine(start, end, Color.red, 5f);
					num = Mathf.Min(num, Vector3.Angle(Vector3.forward, transform.InverseTransformPoint(hitInfo.point)));
					flag2 = false;
				}
				else
				{
					Debug.DrawLine(start, end, Color.green, 5f);
					vector = vector2;
				}
			}
		}
		if (flag)
		{
			if (flag2)
			{
				return 0f;
			}
			return num;
		}
		return maxAngle;
	}
}
