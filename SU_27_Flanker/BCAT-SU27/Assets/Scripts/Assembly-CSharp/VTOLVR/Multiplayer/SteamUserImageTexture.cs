using System.Collections;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class SteamUserImageTexture : MonoBehaviour
{
	public enum AssignmentModes
	{
		Mine,
		NetEntity,
		Scripted
	}

	public AssignmentModes mode;

	public string texturePropertyName = "_MainTex";

	public string colorPropertyName = "_Color";

	public MeshRenderer[] meshes;

	public SkinnedMeshRenderer[] skinnedMeshes;

	public RawImage[] images;

	public int materialIndex = -1;

	private bool hideSteamImages;

	private Coroutine gtRtn;

	private void Awake()
	{
		GameSettings.TryGetGameSettingValue<bool>("HIDE_STEAM_IMAGES", out hideSteamImages);
		if (!VTOLMPUtils.IsMultiplayer())
		{
			hideSteamImages = true;
		}
		if (meshes != null)
		{
			MeshRenderer[] array = meshes;
			foreach (MeshRenderer meshRenderer in array)
			{
				if ((bool)meshRenderer)
				{
					meshRenderer.enabled = false;
				}
			}
		}
		if (images == null)
		{
			return;
		}
		RawImage[] array2 = images;
		foreach (RawImage rawImage in array2)
		{
			if ((bool)rawImage)
			{
				rawImage.enabled = false;
			}
		}
	}

	private IEnumerator Start()
	{
		if (mode == AssignmentModes.Mine)
		{
			StartCoroutine(GetTexture(SteamClient.SteamId));
		}
		else if (mode == AssignmentModes.NetEntity)
		{
			VTNetEntity netEnt = GetComponentInParent<VTNetEntity>();
			if ((bool)netEnt)
			{
				while ((ulong)netEnt.owner.Id == 0L)
				{
					yield return null;
				}
				StartCoroutine(GetTexture(netEnt.owner.Id));
			}
		}
		else
		{
			_ = mode;
			_ = 2;
		}
	}

	public void SetSteamID(SteamId id)
	{
		if (mode != AssignmentModes.Scripted)
		{
			Debug.LogError("SetSteamID was called on a SteamUserImageTexture when it was not in Scripted mode!");
			return;
		}
		if (gtRtn != null)
		{
			StopCoroutine(gtRtn);
		}
		gtRtn = StartCoroutine(GetTexture(id));
	}

	private IEnumerator GetTexture(SteamId id)
	{
		if (hideSteamImages)
		{
			yield break;
		}
		Task<Texture2D> s_imgTask = VTOLMPLobbyManager.GetUserImage(id);
		while (!s_imgTask.IsCompleted)
		{
			yield return null;
		}
		if (!s_imgTask.Result)
		{
			yield break;
		}
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetTexture(texturePropertyName, s_imgTask.Result);
		materialPropertyBlock.SetColor(colorPropertyName, Color.white);
		if (meshes != null)
		{
			MeshRenderer[] array = meshes;
			foreach (MeshRenderer meshRenderer in array)
			{
				if ((bool)meshRenderer)
				{
					meshRenderer.enabled = true;
					meshRenderer.SetPropertyBlock(materialPropertyBlock);
				}
			}
		}
		if (skinnedMeshes != null)
		{
			SkinnedMeshRenderer[] array2 = skinnedMeshes;
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in array2)
			{
				if ((bool)skinnedMeshRenderer)
				{
					skinnedMeshRenderer.enabled = true;
					if (materialIndex >= 0)
					{
						skinnedMeshRenderer.SetPropertyBlock(materialPropertyBlock, materialIndex);
					}
					else
					{
						skinnedMeshRenderer.SetPropertyBlock(materialPropertyBlock);
					}
				}
			}
		}
		if (images == null)
		{
			yield break;
		}
		RawImage[] array3 = images;
		foreach (RawImage rawImage in array3)
		{
			if ((bool)rawImage)
			{
				rawImage.enabled = true;
				rawImage.texture = s_imgTask.Result;
			}
		}
	}
}

}