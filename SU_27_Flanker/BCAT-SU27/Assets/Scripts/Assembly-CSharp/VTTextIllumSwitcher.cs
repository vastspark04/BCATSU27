using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VTTextIllumSwitcher : MonoBehaviour
{
	private class BatchableObject
	{
		public Color baseColor;

		public Color emissColor;

		public Material material;
	}

	public Battery battery;

	private ISwitchableEmissionText[] illums;

	private bool b_hasElectricity = true;

	private bool b_powered;

	private float f_brightness;

	private int vttextEmissID;

	private int illumState = -1;

	private bool hasBatched;

	private List<BatchableObject> batches;

	private void OnEnable()
	{
		if (!battery)
		{
			battery = base.transform.root.GetComponentInChildren<Battery>();
		}
		if ((bool)battery)
		{
			StartCoroutine(BattRoutine());
		}
		else
		{
			b_hasElectricity = false;
		}
	}

	private IEnumerator BattRoutine()
	{
		bool wasElec = false;
		while (base.enabled)
		{
			if (b_powered)
			{
				b_hasElectricity = battery.Drain(0.01f * Time.deltaTime);
			}
			else
			{
				b_hasElectricity = false;
			}
			if (wasElec != b_hasElectricity)
			{
				wasElec = b_hasElectricity;
				UpdateIllums();
			}
			yield return null;
		}
	}

	private void Start()
	{
		illums = GetComponentInParent<Actor>().gameObject.GetComponentsInChildrenImplementing<ISwitchableEmissionText>();
		vttextEmissID = Shader.PropertyToID("_Emission");
		BatchIllums();
	}

	public void SetPower(int st)
	{
		bool flag = (b_powered = st > 0);
		UpdateIllums();
	}

	private void UpdateIllums(bool forceUpdate = false)
	{
		int num = ((b_powered && b_hasElectricity) ? 1 : 0);
		if (!(illumState != num || forceUpdate))
		{
			return;
		}
		illumState = num;
		bool flag = num == 1;
		if (illums != null)
		{
			for (int i = 0; i < illums.Length; i++)
			{
				illums[i].SetEmissionMultiplier(f_brightness);
				illums[i].SetEmission(flag);
			}
		}
		if (hasBatched)
		{
			UpdateBatches(flag);
		}
	}

	public void SetBrightness(float t)
	{
		f_brightness = t;
		UpdateIllums(forceUpdate: true);
	}

	private void BatchIllums()
	{
		if (hasBatched)
		{
			return;
		}
		List<ISwitchableEmissionText> list = new List<ISwitchableEmissionText>();
		ISwitchableEmissionText[] array = illums;
		foreach (ISwitchableEmissionText switchableEmissionText in array)
		{
			if (switchableEmissionText == null)
			{
				continue;
			}
			if (switchableEmissionText is VTText)
			{
				VTText vTText = (VTText)switchableEmissionText;
				if ((bool)vTText)
				{
					MeshRenderer component = vTText.GetComponent<MeshRenderer>();
					if (!component || !component.sharedMaterial)
					{
						StartCoroutine(BatchDelayed(vTText));
						return;
					}
				}
			}
			list.Add(switchableEmissionText);
		}
		hasBatched = true;
		batches = new List<BatchableObject>();
		int nameID = Shader.PropertyToID("_Color");
		MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
		array = illums;
		foreach (ISwitchableEmissionText switchableEmissionText2 in array)
		{
			if (!(switchableEmissionText2 is VTText))
			{
				continue;
			}
			VTText vTText2 = (VTText)switchableEmissionText2;
			if (!vTText2)
			{
				continue;
			}
			list.Remove(switchableEmissionText2);
			bool flag = false;
			foreach (BatchableObject batch in batches)
			{
				if (CompareColors(vTText2, batch))
				{
					flag = true;
					vTText2.GetComponent<MeshRenderer>().sharedMaterial = batch.material;
					break;
				}
			}
			if (!flag)
			{
				Material sharedMaterial = vTText2.GetComponent<MeshRenderer>().sharedMaterial;
				if (!sharedMaterial)
				{
					Debug.LogError("Tried to batch VTText but it had no material: " + UIUtils.GetHierarchyString(vTText2.gameObject));
					continue;
				}
				sharedMaterial.SetColor(nameID, vTText2.color);
				BatchableObject item = new BatchableObject
				{
					baseColor = vTText2.color,
					emissColor = vTText2.emission,
					material = sharedMaterial
				};
				batches.Add(item);
			}
			vTText2.GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);
		}
		Debug.LogFormat("VTTextIllumSwitcher batched {0} texts into {1} materials.", illums.Length, batches.Count);
		illums = list.ToArray();
		illumState = -1;
		UpdateIllums();
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

	private void UpdateBatches(bool lit)
	{
		int count = batches.Count;
		for (int i = 0; i < count; i++)
		{
			BatchableObject batchableObject = batches[i];
			if ((bool)batchableObject.material)
			{
				if (lit)
				{
					batchableObject.material.SetColor(vttextEmissID, f_brightness * batchableObject.emissColor);
				}
				else
				{
					batchableObject.material.SetColor(vttextEmissID, f_brightness * Color.black);
				}
			}
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
