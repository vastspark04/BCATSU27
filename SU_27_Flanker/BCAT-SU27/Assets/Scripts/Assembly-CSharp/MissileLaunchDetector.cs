using UnityEngine;

public class MissileLaunchDetector : MonoBehaviour
{
	public float fov;

	public float maxRange;

	private float sqrMaxRange;

	private float halfFov;

	private void Awake()
	{
		sqrMaxRange = maxRange * maxRange;
		halfFov = fov / 2f;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		if ((bool)Camera.current && Vector3.Angle(base.transform.forward, Camera.current.transform.position - base.transform.position) < fov / 2f)
		{
			Gizmos.color = Color.green;
		}
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, Vector3.one);
		Gizmos.DrawFrustum(Vector3.zero, fov, maxRange, 0f, 1f);
		Gizmos.matrix = Matrix4x4.identity;
	}

	public bool TryDetectLaunch(Vector3 launchPosition, Vector3 direction)
	{
		Vector3 vector = launchPosition - base.transform.position;
		float sqrMagnitude = vector.sqrMagnitude;
		if (sqrMagnitude > sqrMaxRange)
		{
			return false;
		}
		if (sqrMagnitude < 10000f)
		{
			return false;
		}
		if (Vector3.Angle(vector, base.transform.forward) > halfFov)
		{
			return false;
		}
		if (Vector3.Dot(direction, -vector) < 0f)
		{
			return false;
		}
		if (Physics.Linecast(base.transform.position, launchPosition, 1))
		{
			return false;
		}
		return true;
	}
}
