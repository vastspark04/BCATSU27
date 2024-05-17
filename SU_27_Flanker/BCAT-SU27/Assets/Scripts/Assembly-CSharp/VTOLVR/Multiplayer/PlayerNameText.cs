using System.Collections;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class PlayerNameText : MonoBehaviour
{
	public enum NameModes
	{
		Pilot,
		Steam
	}

	public VTNetEntity netEntity;

	public Text text;

	public VTText vtText;

	public bool abbreviate;

	public bool showForSelf;

	[Header("Render to Texture")]
	public GameObject renderObject;

	public Camera rtCam;

	public RawImage rtRawImage;

	private RenderTexture rt;

	public int rtHeight = 512;

	public int rtWidth = 512;

	public AnimationCurve alphaDistanceCurve;

	public Image colorImage;

	public ParticleSystemRenderer psr;

	public Color baseParticleColor = Color.white;

	private int psrTintID;

	[Header("Apply to Decal")]
	public string decalPropertyName = "_DecalTex";

	public MeshRenderer[] decalRenderers;

	public NameModes nameMode;

	public bool enabledWithoutIcons;

	private string customName;

	private MaterialPropertyBlock psrProps;

	private void Awake()
	{
		if ((bool)text)
		{
			text.enabled = false;
		}
		if ((bool)vtText)
		{
			vtText.GetComponent<MeshRenderer>().enabled = false;
		}
		if ((bool)psr)
		{
			psrTintID = Shader.PropertyToID("_TintColor");
			if (GameSettings.TryGetGameSettingValue<float>("NAMETAG_SIZE", out var val))
			{
				psr.minParticleSize = val;
			}
		}
	}

	private void OnEnable()
	{
		if (!VTOLMPUtils.IsMultiplayer())
		{
			base.gameObject.SetActive(value: false);
			base.enabled = false;
		}
		else
		{
			StartCoroutine(EnableRoutine());
		}
	}

	private IEnumerator EnableRoutine()
	{
		yield return null;
		while ((bool)netEntity && netEntity.ownerID == 0L)
		{
			yield return null;
		}
		AutoSetName();
	}

	private void AutoSetName()
	{
		if ((bool)netEntity && netEntity.owner.IsMe && !showForSelf)
		{
			if ((bool)renderObject)
			{
				renderObject.SetActive(value: false);
			}
			base.gameObject.SetActive(value: false);
			return;
		}
		if ((bool)this.text)
		{
			this.text.enabled = true;
		}
		if ((bool)vtText)
		{
			vtText.GetComponent<MeshRenderer>().enabled = true;
		}
		if (!string.IsNullOrEmpty(customName))
		{
			SetName(customName);
		}
		else
		{
			Friend friend = (netEntity ? netEntity.owner : new Friend(BDSteamClient.mySteamID));
			string text;
			if (nameMode == NameModes.Steam)
			{
				if (!friend.IsMe && (string.IsNullOrEmpty(friend.Name) || friend.Name.ToLower().Contains("[unknown]")))
				{
					SteamFriends.OnPersonaStateChange -= SteamFriends_OnPersonaStateChange;
					SteamFriends.OnPersonaStateChange += SteamFriends_OnPersonaStateChange;
				}
				text = ((!abbreviate) ? friend.Name : friend.Name);
			}
			else
			{
				PlayerInfo player = VTOLMPLobbyManager.GetPlayer(friend.Id);
				text = ((player == null) ? "?ERROR?" : player.pilotName);
			}
			if ((bool)this.text)
			{
				this.text.text = text;
			}
			if ((bool)vtText)
			{
				vtText.text = text;
				vtText.ApplyText();
			}
			_ = (bool)colorImage;
			RefreshRTName();
		}
		if (((bool)rtCam || (bool)this.text) && ((bool)rtRawImage || (bool)psr || (bool)this.text))
		{
			StartCoroutine(AlphaDistCurveRoutine());
		}
	}

	private void SteamFriends_OnPersonaStateChange(Friend obj)
	{
		Debug.Log("PlayerNameText: Got a persona update for " + obj.Name);
		AutoSetName();
	}

	public void RefreshRTName()
	{
		if ((bool)rtCam)
		{
			UpdateRT();
		}
	}

	public void SetName(string newName)
	{
		customName = newName;
		_ = abbreviate;
		if ((bool)text)
		{
			text.text = newName;
		}
		if ((bool)vtText)
		{
			vtText.text = newName;
			vtText.ApplyText();
		}
		_ = (bool)colorImage;
		RefreshRTName();
	}

	private IEnumerator AlphaDistCurveRoutine()
	{
		while (base.enabled)
		{
			float a = 0f;
			if (VTOLMPSceneManager.unitIcons || enabledWithoutIcons)
			{
				float magnitude = (VRHead.position - base.transform.position).magnitude;
				a = alphaDistanceCurve.Evaluate(magnitude);
			}
			if ((bool)rtRawImage)
			{
				rtRawImage.color = new Color(1f, 1f, 1f, a);
			}
			if ((bool)psr)
			{
				if (psrProps == null)
				{
					psrProps = new MaterialPropertyBlock();
				}
				Color value = baseParticleColor;
				value.a = a;
				psrProps.SetColor(psrTintID, value);
				psr.SetPropertyBlock(psrProps);
			}
			else if ((bool)text)
			{
				Color color = text.color;
				color.a = a;
				text.color = color;
			}
			yield return null;
		}
	}

	private void UpdateRT()
	{
		if (rt == null)
		{
			rt = new RenderTexture(rtWidth, rtHeight, 16);
			rt.depth = 0;
			rt.anisoLevel = 2;
			rt.antiAliasing = 2;
			rt.useMipMap = true;
			rtCam.targetTexture = rt;
			if ((bool)rtRawImage)
			{
				rtRawImage.texture = rt;
			}
			if ((bool)psr)
			{
				if (psrProps == null)
				{
					psrProps = new MaterialPropertyBlock();
				}
				psrProps.SetTexture("_MainTex", rt);
				psr.SetPropertyBlock(psrProps);
			}
		}
		if ((bool)rtRawImage)
		{
			rtRawImage.gameObject.SetActive(value: false);
		}
		if ((bool)psr)
		{
			psr.enabled = false;
		}
		_ = base.transform.localScale;
		renderObject.SetActive(value: true);
		rtCam.Render();
		renderObject.SetActive(value: false);
		if ((bool)rtRawImage)
		{
			rtRawImage.gameObject.SetActive(value: true);
		}
		if ((bool)psr)
		{
			psr.enabled = true;
		}
		if (decalRenderers == null || decalRenderers.Length == 0)
		{
			return;
		}
		MeshRenderer[] array = decalRenderers;
		foreach (MeshRenderer meshRenderer in array)
		{
			if ((bool)meshRenderer)
			{
				MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
				meshRenderer.GetPropertyBlock(materialPropertyBlock, 1);
				materialPropertyBlock.SetTexture(decalPropertyName, rt);
				meshRenderer.SetPropertyBlock(materialPropertyBlock, 1);
			}
		}
	}

	private void OnDestroy()
	{
		if ((bool)rt)
		{
			rt.DiscardContents();
			rt.Release();
			Object.DestroyImmediate(rt);
		}
		SteamFriends.OnPersonaStateChange -= SteamFriends_OnPersonaStateChange;
	}

	private void OnDisable()
	{
		SteamFriends.OnPersonaStateChange -= SteamFriends_OnPersonaStateChange;
	}
}

}