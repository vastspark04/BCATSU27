using UnityEngine;

public class CurveNearestPointTest : MonoBehaviour
{
	public FollowPath path;

	public int iterations = 5;

	public float leadDistance;

	private void OnDrawGizmos()
	{
		if ((bool)path)
		{
			Vector3 followPoint = path.GetFollowPoint(base.transform.position, leadDistance, iterations);
			Gizmos.DrawLine(base.transform.position, followPoint);
		}
	}
}
