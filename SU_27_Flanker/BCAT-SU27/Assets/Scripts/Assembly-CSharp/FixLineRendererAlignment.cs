using System.Collections.Generic;
using UnityEngine;

public class FixLineRendererAlignment : MonoBehaviour
{
	public List<LineRenderer> lrs;

	[ContextMenu("Apply")]
	public void ApplyFix()
	{
		Vector3[] array = new Vector3[2];
		List<Vector3> list = new List<Vector3>();
		foreach (LineRenderer lr in lrs)
		{
			if (array.Length < lr.positionCount)
			{
				array = new Vector3[lr.positionCount];
			}
			list.Clear();
			int positions = lr.GetPositions(array);
			Quaternion quaternion = Quaternion.Euler(180f, 0f, 0f);
			for (int i = 0; i < positions; i++)
			{
				list.Add(quaternion * array[i]);
			}
			lr.SetPositions(list.ToArray());
			Vector3 axis = lr.transform.parent.InverseTransformDirection(lr.transform.right);
			lr.transform.localRotation = Quaternion.AngleAxis(180f, axis) * lr.transform.localRotation;
		}
	}
}
