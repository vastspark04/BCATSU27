using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VTTextMaterialBatcher : MonoBehaviour
{
	private class BatchableObject
	{
		public Color baseColor;

		public Color emissColor;

		public Material material;
	}

	private bool hasBatched;

	private List<BatchableObject> batches;

	private void Start()
	{
		BatchIllums();
	}

	private void BatchIllums()
	{
		if (hasBatched)
		{
			return;
		}
		List<VTText> list = new List<VTText>();
		VTText[] componentsInChildren = GetComponentsInChildren<VTText>();
		VTText[] array = componentsInChildren;
		foreach (VTText vTText in array)
		{
			if (!(vTText != null))
			{
				continue;
			}
			if ((object)vTText != null)
			{
				VTText vTText2 = vTText;
				if ((bool)vTText2)
				{
					MeshRenderer component = vTText2.GetComponent<MeshRenderer>();
					if (!component || !component.sharedMaterial)
					{
						StartCoroutine(BatchDelayed(vTText2));
						return;
					}
				}
			}
			list.Add(vTText);
		}
		hasBatched = true;
		batches = new List<BatchableObject>();
		int nameID = Shader.PropertyToID("_Color");
		MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
		array = componentsInChildren;
		foreach (VTText vTText3 in array)
		{
			if ((object)vTText3 == null)
			{
				continue;
			}
			VTText vTText4 = vTText3;
			if (!vTText4)
			{
				continue;
			}
			list.Remove(vTText3);
			bool flag = false;
			foreach (BatchableObject batch in batches)
			{
				if (CompareColors(vTText4, batch))
				{
					flag = true;
					vTText4.GetComponent<MeshRenderer>().sharedMaterial = batch.material;
					break;
				}
			}
			if (!flag)
			{
				Material sharedMaterial = vTText4.GetComponent<MeshRenderer>().sharedMaterial;
				if (!sharedMaterial)
				{
					Debug.LogError("Tried to batch VTText but it had no material: " + UIUtils.GetHierarchyString(vTText4.gameObject));
					continue;
				}
				sharedMaterial.SetColor(nameID, vTText4.color);
				BatchableObject item = new BatchableObject
				{
					baseColor = vTText4.color,
					emissColor = vTText4.emission,
					material = sharedMaterial
				};
				batches.Add(item);
			}
			vTText4.GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);
		}
		Debug.LogFormat("VTTextMaterialBatcher batched {0} texts into {1} materials.", componentsInChildren.Length, batches.Count);
		componentsInChildren = list.ToArray();
	}

	private IEnumerator BatchDelayed(VTText waitOn)
	{
		string msg3 = "BatchIllums: No VTText to wait on...?";
		if ((bool)waitOn)
		{
			msg3 = "Batch Illums waiting on " + UIUtils.GetHierarchyString(waitOn.gameObject);
			Debug.Log(msg3);
			while ((bool)waitOn && (!waitOn.GetComponent<MeshRenderer>() || !waitOn.GetComponent<MeshRenderer>().sharedMaterial))
			{
				yield return null;
			}
			msg3 = ((!waitOn) ? (msg3 + "\nVTText we waited on suddenly disappeared!") : (msg3 + "\nSuccessfully waited for mesh renderer and shared material."));
			Debug.Log(msg3);
			BatchIllums();
		}
		else
		{
			Debug.LogError(msg3);
		}
	}

	private bool CompareColors(VTText a, BatchableObject b)
	{
		float num = Mathf.Pow(0.02f, 2f);
		Vector3 vector = new Vector3(a.color.r, a.color.g, a.color.b);
		Vector3 vector2 = new Vector3(b.baseColor.r, b.baseColor.g, b.baseColor.b);
		if ((vector - vector2).sqrMagnitude < num)
		{
			Vector3 vector3 = new Vector3(a.emission.r, a.emission.g, a.emission.b);
			Vector3 vector4 = new Vector3(b.emissColor.r, b.emissColor.g, b.emissColor.b);
			if ((vector3 - vector4).sqrMagnitude < num)
			{
				return true;
			}
		}
		return false;
	}
}
