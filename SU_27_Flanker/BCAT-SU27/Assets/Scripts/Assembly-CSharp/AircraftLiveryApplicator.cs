using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class AircraftLiveryApplicator : MonoBehaviour
{
	public List<Material> materials;

	public Texture2D overrideLivery;

	private void Start()
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			return;
		}
		if ((bool)PilotSaveManager.currentCampaign)
		{
			if ((bool)PilotSaveManager.currentCampaign.campaignLivery)
			{
				ApplyLivery(PilotSaveManager.currentCampaign.campaignLivery);
			}
		}
		else
		{
			Debug.Log("AircraftLiveryApplicator: Current campaign has no livery.");
		}
	}

	public void ApplyLivery(Texture2D texture)
	{
		Debug.Log("AircraftLiveryApplicator: Applying livery: " + texture);
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetTexture("_Livery", texture);
		MeshRenderer[] componentsInChildren = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			Material[] sharedMaterials = meshRenderer.sharedMaterials;
			bool flag = false;
			foreach (Material material2 in materials)
			{
				Material[] array = sharedMaterials;
				foreach (Material material in array)
				{
					if ((bool)material && material.name == material2.name)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (flag)
			{
				meshRenderer.SetPropertyBlock(materialPropertyBlock);
			}
		}
	}

	[ContextMenu("Apply Override")]
	public void ApplyOverride()
	{
		if ((bool)overrideLivery)
		{
			ApplyLivery(overrideLivery);
		}
	}
}
