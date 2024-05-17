using System.Collections;
using UnityEngine;

public class AIPilotObstacleChecker : MonoBehaviour
{
	private class AvoidPoint
	{
		public bool detected;

		public FixedPoint point;
	}

	private FlightInfo flightInfo;

	private const float CELL_SIZE = 100f;

	private const int BOX_DIM = 4;

	private const int POINTS_ARR_LENGTH = 16;

	private const float AVOID_RADIUS = 400f;

	private const float AVOID_RADIUS_SQRD = 160000f;

	private AvoidPoint[] points = new AvoidPoint[16];

	private Rigidbody rb => flightInfo.rb;

	private void Awake()
	{
		flightInfo = GetComponent<FlightInfo>();
	}

	private void OnEnable()
	{
		StartCoroutine(TestRoutine());
		StartCoroutine(CheckRoutine());
	}

	private IEnumerator TestRoutine()
	{
		yield return null;
		while (base.enabled)
		{
			CheckAvoidObstacles(base.transform.position + rb.velocity.normalized * 1000f, out var _);
			yield return null;
		}
	}

	private IEnumerator CheckRoutine()
	{
		yield return null;
		WaitForFixedUpdate fixedUpdate = new WaitForFixedUpdate();
		int arrIdx = 0;
		while (base.enabled)
		{
			Ray ray = new Ray(rb.position, RayDir(arrIdx));
			AvoidPoint avoidPoint = ((points[arrIdx] == null) ? (points[arrIdx] = new AvoidPoint()) : points[arrIdx]);
			if (Physics.SphereCast(ray, 50f, out var hitInfo, RayDist(), 1))
			{
				avoidPoint.detected = true;
				avoidPoint.point = new FixedPoint(hitInfo.point);
				arrIdx = (arrIdx + 1) % 16;
				Debug.DrawLine(rb.position, hitInfo.point, Color.magenta);
			}
			else
			{
				avoidPoint.detected = false;
			}
			yield return fixedUpdate;
		}
	}

	private float RayDist()
	{
		return flightInfo.airspeed * 8f;
	}

	private Vector3 RayDir(int idx)
	{
		int num = idx % 4;
		int num2 = idx / 4;
		Vector3 velocity = rb.velocity;
		Vector3 normalized = Vector3.Cross(Vector3.up, rb.velocity).normalized;
		velocity -= 200f * Vector3.up;
		velocity -= 200f * normalized;
		return (velocity + (100f * (float)num * normalized + 100f * (float)num2 * Vector3.up)).normalized;
	}

	public bool CheckAvoidObstacles(Vector3 targetPosition, out Vector3 correctedPosition)
	{
		correctedPosition = targetPosition;
		_ = targetPosition - base.transform.position;
		Vector3 vector = new Vector3(0f, float.MinValue, 0f);
		Vector3 zero = Vector3.zero;
		int num = 0;
		for (int i = 0; i < 16; i++)
		{
			AvoidPoint avoidPoint = points[i];
			if (avoidPoint == null || !avoidPoint.detected)
			{
				continue;
			}
			if (Vector3.Dot(base.transform.forward, avoidPoint.point.point - base.transform.position) > 0f)
			{
				num++;
				zero += avoidPoint.point.point;
				Debug.DrawLine(base.transform.position, avoidPoint.point.point, Color.red);
				if (avoidPoint.point.point.y > vector.y)
				{
					vector = avoidPoint.point.point;
				}
			}
			else
			{
				avoidPoint.detected = false;
			}
		}
		if (num > 0)
		{
			zero /= (float)num;
			Debug.DrawLine(base.transform.position, zero, Color.yellow);
			correctedPosition.y = Mathf.Max(correctedPosition.y, vector.y + 300f);
			return true;
		}
		return false;
	}
}
