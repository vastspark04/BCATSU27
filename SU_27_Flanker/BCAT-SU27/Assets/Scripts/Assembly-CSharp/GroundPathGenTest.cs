using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundPathGenTest : MonoBehaviour
{
	public Transform start;

	public Transform end;

	public float maxSlope = 5f;

	public float distInterval = 15f;

	public Color lineColor = Color.red;

	public float pointSphereRadius = 5f;

	private GroundPathGenerator.GroundPathRequest currRequest;

	[ContextMenu("Test")]
	public void Test()
	{
		StartCoroutine(TestRoutine());
	}

	private IEnumerator TestRoutine()
	{
		GroundPathGenerator.GroundPathRequest request = (currRequest = GroundPathGenerator.GetGroundPath(start.position, end.position, maxSlope, distInterval));
		while (!request.ready)
		{
			yield return null;
		}
	}

	private void OnDrawGizmos()
	{
		if (currRequest == null)
		{
			return;
		}
		Gizmos.color = new Color(1f, 0f, 1f, 0.25f);
		float radius = 1f * distInterval;
		for (int i = 0; i < currRequest.problemAreas.Count; i++)
		{
			Gizmos.DrawWireSphere(currRequest.problemAreas[i].point, radius);
		}
		if (currRequest.ready)
		{
			List<FixedPoint> points = currRequest.points;
			Gizmos.color = lineColor;
			for (int j = 1; j < points.Count; j++)
			{
				Gizmos.DrawSphere(points[j].point, pointSphereRadius);
				Gizmos.DrawLine(points[j].point, points[j - 1].point);
			}
		}
	}
}
