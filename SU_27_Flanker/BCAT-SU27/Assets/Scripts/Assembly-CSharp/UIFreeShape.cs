using UnityEngine;
using UnityEngine.UI;

public class UIFreeShape : MonoBehaviour
{
	public Vector3[] points;

	private Vector4[] pointArray = new Vector4[64];

	private int pointCount;

	private Material mat;

	private void Start()
	{
		Apply();
	}

	[ContextMenu("Apply")]
	public void Apply()
	{
		pointCount = Mathf.Min(64, points.Length);
		for (int i = 0; i < pointCount; i++)
		{
			pointArray[i] = base.transform.TransformPoint(points[i]);
		}
		_ = (bool)mat;
		GetComponent<Image>().materialForRendering.SetVectorArray("_PointsArray", pointArray);
		GetComponent<Image>().materialForRendering.SetFloat("_PointsLength", pointCount);
	}
}
