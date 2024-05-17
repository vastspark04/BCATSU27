using UnityEngine;

public class ShipHullNumber : MonoBehaviour
{
	public VTText[] texts;

	public void SetNumber(int num)
	{
		num = Mathf.Clamp(num, 1, 999);
		VTText[] array = texts;
		foreach (VTText obj in array)
		{
			obj.text = num.ToString();
			obj.ApplyText();
		}
	}
}
