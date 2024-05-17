using System.Collections.Generic;
using UnityEngine;

public class AkuTreeData : MonoBehaviour
{
	public Vector3[] verts;

	public float[] treeVals;

	private Mesh mesh;

	private List<Color> colors;

	public void PaintColor(Vector3 localPos, float radius, Color c, float power)
	{
		if (colors == null || mesh == null)
		{
			mesh = GetComponent<MeshFilter>().sharedMesh;
			colors = new List<Color>();
			mesh.GetColors(colors);
		}
		if (verts == null)
		{
			verts = mesh.vertices;
		}
		float num = radius * radius;
		for (int i = 0; i < colors.Count; i++)
		{
			float sqrMagnitude = (localPos - verts[i]).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				float num2 = Mathf.Sqrt(sqrMagnitude);
				float t = (1f - num2 / radius) * power * 0.2f;
				colors[i] = Color.Lerp(colors[i], c, t);
			}
		}
		mesh.SetColors(colors);
	}
}
