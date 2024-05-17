using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VTTextToTMP : MonoBehaviour
{
	public VTText[] vtTexts;

	public List<TextMeshPro> appliedTmps;

	public float scale = 11f;

	public TMP_FontAsset fontAsset;

	[ContextMenu("Get VTTexts")]
	public void GetVTTexts()
	{
		vtTexts = GetComponentsInChildren<VTText>();
	}

	[ContextMenu("Apply")]
	public void Apply()
	{
		Clear();
		VTText[] array = vtTexts;
		foreach (VTText vTText in array)
		{
			GameObject obj = new GameObject(vTText.gameObject.name + "_TMPro");
			obj.transform.parent = vTText.transform.parent;
			obj.transform.localPosition = vTText.transform.localPosition;
			obj.transform.localRotation = vTText.transform.localRotation;
			obj.transform.localScale = scale * vTText.transform.localScale.x * Vector3.one;
			TextMeshPro textMeshPro = obj.AddComponent<TextMeshPro>();
			textMeshPro.text = vTText.text.ToUpper();
			textMeshPro.fontSize = vTText.fontSize;
			textMeshPro.font = fontAsset;
			textMeshPro.UpdateFontAsset();
			textMeshPro.SetAllDirty();
			textMeshPro.color = vTText.color;
			appliedTmps.Add(textMeshPro);
		}
	}

	[ContextMenu("Clear")]
	public void Clear()
	{
		if (appliedTmps != null)
		{
			foreach (TextMeshPro appliedTmp in appliedTmps)
			{
				if ((bool)appliedTmp)
				{
					Object.DestroyImmediate(appliedTmp.gameObject);
				}
			}
		}
		else
		{
			appliedTmps = new List<TextMeshPro>();
		}
		appliedTmps.Clear();
	}

	[ContextMenu("Hide VTTs")]
	public void HideVTTs()
	{
		VTText[] array = vtTexts;
		foreach (VTText vTText in array)
		{
			if ((bool)vTText)
			{
				vTText.enabled = false;
				vTText.GetComponent<MeshRenderer>().enabled = false;
			}
		}
	}

	[ContextMenu("Unhide VTTs")]
	public void UnhideVTTs()
	{
		VTText[] array = vtTexts;
		foreach (VTText vTText in array)
		{
			if ((bool)vTText)
			{
				vTText.enabled = true;
				vTText.GetComponent<MeshRenderer>().enabled = true;
			}
		}
	}
}
