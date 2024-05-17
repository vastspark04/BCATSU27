using UnityEngine;
using UnityStandardAssets.ImageEffects;

[ExecuteAlways]
public class ScreenMaskedColorRamp : ImageEffectBase
{
	public Texture textureRamp;

	public Texture maskTex;

	[Range(-1f, 1f)]
	public float rampOffset;

	[Range(0f, 1f)]
	public float effectScale;

	private int rampTexID;

	private int rampOffsetID;

	private int maskTexID;

	private int effectScaleID;

	private int uvXScaleID;

	private void Awake()
	{
		rampTexID = Shader.PropertyToID("_RampTex");
		rampOffsetID = Shader.PropertyToID("_RampOffset");
		maskTexID = Shader.PropertyToID("_MaskTex");
		effectScaleID = Shader.PropertyToID("_EffectScale");
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		
	}
}
