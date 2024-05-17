using UnityEngine;

public class TextMeshProIllumSwitcher : MonoBehaviour, ISwitchableEmissionText, ISwitchableEmission
{
	private MeshRenderer mr;

	private Color glowColor;

	private int glowColorID;

	public void SetEmission(bool e)
	{
		if (e)
		{
			mr.sharedMaterial.EnableKeyword("GLOW_ON");
		}
		else
		{
			mr.sharedMaterial.DisableKeyword("GLOW_ON");
		}
	}

	public void SetEmissionMultiplier(float e)
	{
		Color value = glowColor;
		value.a = e;
		mr.sharedMaterial.SetColor(glowColorID, value);
	}

	private void Awake()
	{
		mr = GetComponent<MeshRenderer>();
		glowColorID = Shader.PropertyToID("_GlowColor");
		glowColor = mr.sharedMaterial.GetColor(glowColorID);
	}

	private void OnDestroy()
	{
		if ((bool)mr)
		{
			mr.sharedMaterial.DisableKeyword("GLOW_ON");
		}
	}
}
