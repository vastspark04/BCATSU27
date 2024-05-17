using System.Collections.Generic;
using UnityEngine;

public class LineRenderer_GenerateCircle : MonoBehaviour
{
	public int vertices;

	public float degrees;

	public bool loop;

	public float radius;

	[ContextMenu("Generate")]
	private void Generate()
	{
		LineRenderer component = GetComponent<LineRenderer>();
		component.loop = loop;
		Vector3 vector = new Vector3(0f, radius, 0f);
		float angle = degrees / (float)vertices;
		List<Vector3> list = new List<Vector3>();
		for (int i = 0; i < vertices; i++)
		{
			list.Add(vector);
			vector = Quaternion.AngleAxis(angle, Vector3.forward) * vector;
		}
		if (loop && degrees < 360f)
		{
			vertices++;
			list.Add(Vector3.zero);
		}
		component.positionCount = vertices;
		component.SetPositions(list.ToArray());
	}
}
