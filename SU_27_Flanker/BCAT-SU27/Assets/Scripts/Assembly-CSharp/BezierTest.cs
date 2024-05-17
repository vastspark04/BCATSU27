using UnityEngine;

public class BezierTest : MonoBehaviour
{
	public Transform startPt;

	public Transform midPt;

	public Transform endPt;

	public int testSubdivs;

	public bool useTangents;

	private void OnDrawGizmos()
	{
		if (!startPt || (!useTangents && !midPt) || !endPt)
		{
			return;
		}
		BezierCurve bezierCurve = ((!useTangents) ? new BezierCurve(startPt.position, midPt.position, endPt.position) : new BezierCurve(startPt.position, endPt.position, startPt.forward, endPt.forward));
		float num = 20f;
		for (int i = 0; (float)i < num; i++)
		{
			float num2 = (float)i * (1f / num);
			Vector3 point = bezierCurve.GetPoint(num2);
			Vector3 point2 = bezierCurve.GetPoint(num2 + 1f / num);
			if (i % 4 == 0)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawLine(point, point + bezierCurve.GetTangent(num2));
			}
			Gizmos.color = Color.white;
			Gizmos.DrawLine(point, point2);
		}
		Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
		Gizmos.DrawLine(bezierCurve.startPt, bezierCurve.midPt);
		Gizmos.DrawLine(bezierCurve.midPt, bezierCurve.endPt);
		if (testSubdivs <= 1)
		{
			return;
		}
		BezierCurve[] array = bezierCurve.Subdivide(testSubdivs);
		for (int j = 0; j < testSubdivs; j++)
		{
			BezierCurve bezierCurve2 = array[j];
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(bezierCurve2.startPt, 1f);
			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(bezierCurve2.midPt, 1f);
			for (int k = 0; (float)k < num; k++)
			{
				float num3 = (float)k * (1f / num);
				Vector3 point3 = bezierCurve2.GetPoint(num3);
				Vector3 point4 = bezierCurve2.GetPoint(num3 + 1f / num);
				Gizmos.color = Color.green;
				Gizmos.DrawLine(point3, point4);
			}
		}
	}
}
