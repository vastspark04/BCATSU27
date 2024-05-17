using System.Collections.Generic;
using UnityEngine;

public class AkuTreePainter : MonoBehaviour
{
	public enum PaintModes
	{
		Disabled,
		Paint,
		Erase,
		Color
	}

	public enum PaintColors
	{
		Red,
		Green,
		Blue,
		Alpha
	}

	public PaintModes paintMode;

	public float radius;

	[Header("Color Painting")]
	public PaintColors paintColor;

	public float paintPower = 1f;

	public List<MeshFilter> meshes;

	[HideInInspector]
	public Vector3 mouseWorldPos;

	private Color CurrentColor()
	{
		return paintColor switch
		{
			PaintColors.Red => new Color(1f, 0f, 0f, 0f), 
			PaintColors.Green => new Color(0f, 1f, 0f, 0f), 
			PaintColors.Blue => new Color(0f, 0f, 1f, 0f), 
			PaintColors.Alpha => new Color(0f, 0f, 0f, 1f), 
			_ => Color.black, 
		};
	}

	public void Paint(Vector3 worldMousePos, float tValue)
	{
	}

	[ContextMenu("Apply Colors")]
	public void ApplyColors()
	{
	}

	private void OnDrawGizmos()
	{
		if (paintMode == PaintModes.Paint || paintMode == PaintModes.Erase)
		{
			Gizmos.color = Color.green;
			foreach (MeshFilter mesh in meshes)
			{
				AkuTreeData component = mesh.GetComponent<AkuTreeData>();
				if (!component)
				{
					continue;
				}
				for (int i = 0; i < component.verts.Length; i++)
				{
					if (component.treeVals[i] > 0.1f)
					{
						Vector3 vector = mesh.transform.TransformPoint(component.verts[i]);
						Gizmos.DrawLine(vector, vector + component.treeVals[i] * 30f * Vector3.up);
					}
				}
			}
		}
		if (paintMode != 0)
		{
			Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
			Gizmos.DrawWireSphere(mouseWorldPos, radius);
		}
	}
}
