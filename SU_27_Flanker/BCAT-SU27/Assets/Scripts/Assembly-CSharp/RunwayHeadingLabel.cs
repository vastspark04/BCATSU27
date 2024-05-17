using System.Collections.Generic;
using UnityEngine;

public class RunwayHeadingLabel : MonoBehaviour
{
	public Mesh[] meshes;

	public MeshFilter digit1;

	public MeshFilter digit2;

	public List<VTText> signTexts;

	public void Apply()
	{
		int num = Mathf.RoundToInt(VectorUtils.Bearing(base.transform.position, base.transform.position + base.transform.forward) / 10f);
		if (num == 0)
		{
			num = 36;
		}
		int num2 = Mathf.FloorToInt((float)num / 10f);
		int num3 = num - num2 * 10;
		digit1.mesh = meshes[num2];
		digit2.mesh = meshes[num3];
		if (signTexts == null)
		{
			return;
		}
		string text = num.ToString("00");
		foreach (VTText signText in signTexts)
		{
			signText.text = text;
			signText.ApplyText();
		}
	}
}
