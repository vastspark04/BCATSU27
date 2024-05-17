using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Light))]
[ExecuteInEditMode]
public class NGSS_Directional : MonoBehaviour
{
	public enum ShadowMapResolution
	{
		UseQualitySettings = 0x100,
		VeryLow = 0x200,
		Low = 0x400,
		Med = 0x800,
		High = 0x1000,
		Ultra = 0x2000,
		Mega = 0x4000
	}

	[Header("MAIN SETTINGS")]
	[Tooltip("If disabled, NGSS Directional shadows replacement will be removed from Graphics settings when OnDisable is called in this component.")]
	public bool NGSS_KEEP_ONDISABLE = true;

	[Tooltip("Check this option if you don't need to update shadows variables at runtime, only once when scene loads.\nUseful to save some CPU cycles.")]
	public bool NGSS_NO_UPDATE_ON_PLAY;

	[Tooltip("Shadows resolution.\nUseQualitySettings = From Quality Settings, SuperLow = 512, Low = 1024, Med = 2048, High = 4096, Ultra = 8192, Mega = 16384.")]
	public ShadowMapResolution NGSS_SHADOWS_RESOLUTION = ShadowMapResolution.UseQualitySettings;

	[Header("BASE SAMPLING")]
	[Tooltip("Used to test blocker search and early bail out algorithms. Keep it as low as possible, might lead to white noise if too low.\nRecommended values: Mobile = 8, Consoles & VR = 16, Desktop = 24")]
	[Range(4f, 32f)]
	public int NGSS_SAMPLING_TEST = 16;

	[Tooltip("Number of samplers per pixel used for PCF and PCSS shadows algorithms.\nRecommended values: Mobile = 16, Consoles & VR = 32, Desktop Med = 48, Desktop High = 64, Desktop Ultra = 128")]
	[Range(8f, 128f)]
	public int NGSS_SAMPLING_FILTER = 48;

	[Header("SHADOW SOFTNESS")]
	[Tooltip("Overall shadows softness.")]
	[Range(0f, 3f)]
	public float NGSS_SHADOWS_SOFTNESS = 1f;

	[Header("PCSS")]
	[Tooltip("PCSS Requires inline sampling and SM3.5.\nProvides Area Light soft-shadows.\nDisable it if you are looking for PCF filtering (uniform soft-shadows) which runs with SM3.0.")]
	public bool NGSS_PCSS_ENABLED;

	[Tooltip("How soft shadows are when close to caster.")]
	[Range(0f, 2f)]
	public float NGSS_PCSS_SOFTNESS_NEAR = 0.125f;

	[Tooltip("How soft shadows are when far from caster.")]
	[Range(0f, 2f)]
	public float NGSS_PCSS_SOFTNESS_FAR = 1f;

	[Header("NOISE")]
	[Tooltip("Improve noise randomnes by aligning samplers in a screen space grid. If disabled, noise will be randomly distributed.\nRecommended when using low sampling count (less than 16 spp)")]
	public bool NGSS_SHADOWS_DITHERING = true;

	[Tooltip("If zero = no noise.\nIf one = 100% noise.\nUseful when fighting banding.")]
	[Range(0f, 1f)]
	public float NGSS_NOISE_SCALE = 1f;

	[Header("DENOISER")]
	[Tooltip("How many iterations the Denoiser algorithm should do.\nRequires NGSS Shadows Libraries to be installed and Cascaded Shadows to be enabled in the Editor Graphics Settings.")]
	[Range(1f, 4f)]
	public int NGSS_DENOISER_ITERATIONS = 2;

	[Tooltip("Overall Denoiser softness.")]
	[Range(0.05f, 1f)]
	public float NGSS_DENOISER_SOFTNESS = 0.75f;

	[Tooltip("The amount of shadow edges the Denoiser can tolerate during denoising.")]
	[Range(0.05f, 1f)]
	public float NGSS_DENOISER_EDGE_TOLERANCE = 0.5f;

	[Header("BIAS")]
	[Tooltip("This estimates receiver slope using derivatives and tries to tilt the filtering kernel along it.\nHowever, when doing it in screenspace from the depth texture can leads to shadow artifacts.\nThus it is disabled by default.")]
	public bool NGSS_RECEIVER_PLANE_BIAS;

	[Header("GLOBAL SETTINGS")]
	[Tooltip("Enable it to let NGSS_Directional control global shadows settings through this component.\nDisable it if you want to manage shadows settings through Unity Quality & Graphics Settings panel.")]
	public bool GLOBAL_SETTINGS_OVERRIDE;

	[Tooltip("Shadows projection.\nRecommeded StableFit as it helps stabilizing shadows as camera moves.")]
	public ShadowProjection GLOBAL_SHADOWS_PROJECTION = ShadowProjection.StableFit;

	[Tooltip("Sets the maximum distance at wich shadows are visible from camera.\nThis option affects your shadow distance in Quality Settings.")]
	public float GLOBAL_SHADOWS_DISTANCE = 150f;

	[Tooltip("Must be disabled on very low end hardware.\nIf enabled, Cascaded Shadows will be turned off in Graphics Settings.")]
	private bool GLOBAL_CASCADED_SHADOWS = true;

	private bool GLOBAL_CASCADED_SHADOWS_STATE;

	[Range(0f, 4f)]
	[Tooltip("Number of cascades the shadowmap will have. This option affects your cascade counts in Quality Settings.\nYou should entierly disable Cascaded Shadows (Graphics Menu) if you are targeting low-end devices.")]
	public int GLOBAL_CASCADES_COUNT = 4;

	[Range(0.01f, 0.25f)]
	[Tooltip("Used for the cascade stitching algorithm.\nCompute cascades splits distribution exponentially in a x*2^n form.\nIf 4 cascades, set this value to 0.1. If 2 cascades, set it to 0.25.\nThis option affects your cascade splits in Quality Settings.")]
	public float GLOBAL_CASCADES_SPLIT_VALUE = 0.1f;

	[Header("CASCADES")]
	[Tooltip("Blends cascades at seams intersection.\nAdditional overhead required for this option.")]
	public bool NGSS_CASCADES_BLENDING = true;

	[Tooltip("Tweak this value to adjust the blending transition between cascades.")]
	[Range(0f, 2f)]
	public float NGSS_CASCADES_BLENDING_VALUE = 1f;

	[Range(0f, 1f)]
	[Tooltip("If one, softness across cascades will be matched using splits distribution, resulting in realistic soft-ness over distance.\nIf zero the softness distribution will be based on cascade index, resulting in blurrier shadows over distance thus less realistic.")]
	public float NGSS_CASCADES_SOFTNESS_NORMALIZATION = 1f;

	private bool isSetup;

	private bool isInitialized;

	private bool isGraphicSet;

	private Light _DirLight;

	private Light DirLight
	{
		get
		{
			if (_DirLight == null)
			{
				_DirLight = GetComponent<Light>();
			}
			return _DirLight;
		}
	}

	private void OnDisable()
	{
		isInitialized = false;
		if (!NGSS_KEEP_ONDISABLE && isGraphicSet)
		{
			isGraphicSet = false;
			GraphicsSettings.SetCustomShader(BuiltinShaderType.ScreenSpaceShadows, Shader.Find("Hidden/Internal-ScreenSpaceShadows"));
			GraphicsSettings.SetShaderMode(BuiltinShaderType.ScreenSpaceShadows, BuiltinShaderMode.UseBuiltin);
		}
	}

	private void OnEnable()
	{
		if (IsNotSupported())
		{
			Debug.LogWarning("Unsupported graphics API, NGSS requires at least SM3.0 or higher and DX9 is not supported.", this);
			base.enabled = false;
		}
		else
		{
			Init();
		}
	}

	private void Init()
	{
		if (!isInitialized)
		{
			if (!isGraphicSet)
			{
				GraphicsSettings.SetShaderMode(BuiltinShaderType.ScreenSpaceShadows, BuiltinShaderMode.UseCustom);
				GraphicsSettings.SetCustomShader(BuiltinShaderType.ScreenSpaceShadows, Shader.Find("Hidden/NGSS_Directional"));
				DirLight.shadows = LightShadows.Soft;
				isGraphicSet = true;
			}
			isInitialized = true;
		}
	}

	private bool IsNotSupported()
	{
		return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2;
	}

	private void Update()
	{
		if ((Application.isPlaying && NGSS_NO_UPDATE_ON_PLAY && isSetup) || DirLight.shadows == LightShadows.None)
		{
			return;
		}
		DirLight.shadows = LightShadows.Soft;
		NGSS_SAMPLING_TEST = Mathf.Clamp(NGSS_SAMPLING_TEST, 4, NGSS_SAMPLING_FILTER);
		Shader.SetGlobalFloat("NGSS_TEST_SAMPLERS_DIR", NGSS_SAMPLING_TEST);
		Shader.SetGlobalFloat("NGSS_FILTER_SAMPLERS_DIR", NGSS_SAMPLING_FILTER);
		Shader.SetGlobalFloat("NGSS_GLOBAL_SOFTNESS", NGSS_SHADOWS_SOFTNESS * 2f / (QualitySettings.shadowDistance * 0.66f) * ((QualitySettings.shadowCascades == 2) ? 1.5f : ((QualitySettings.shadowCascades == 4) ? 1f : 0.25f)));
		Shader.SetGlobalFloat("NGSS_GLOBAL_SOFTNESS_OPTIMIZED", NGSS_SHADOWS_SOFTNESS / QualitySettings.shadowDistance);
		int num = (int)Mathf.Sqrt(NGSS_SAMPLING_FILTER);
		Shader.SetGlobalInt("NGSS_OPTIMIZED_ITERATIONS", (num % 2 == 0) ? (num + 1) : num);
		Shader.SetGlobalInt("NGSS_OPTIMIZED_SAMPLERS", NGSS_SAMPLING_FILTER);
		Shader.SetGlobalFloat("NGSS_DENOISER_SOFTNESS", Mathf.Clamp(1f - NGSS_DENOISER_SOFTNESS, 0.05f, 0.95f) * 4096f);
		Shader.SetGlobalFloat("NGSS_DENOISER_EDGE_TOLERANCE", NGSS_DENOISER_EDGE_TOLERANCE);
		Shader.SetGlobalInt("NGSS_DENOISER_ITERATIONS", NGSS_DENOISER_ITERATIONS);
		if (NGSS_RECEIVER_PLANE_BIAS)
		{
			Shader.EnableKeyword("NGSS_USE_RECEIVER_PLANE_BIAS");
		}
		else
		{
			Shader.DisableKeyword("NGSS_USE_RECEIVER_PLANE_BIAS");
		}
		if (NGSS_SHADOWS_DITHERING)
		{
			Shader.EnableKeyword("NGSS_NOISE_GRID_DIR");
		}
		else
		{
			Shader.DisableKeyword("NGSS_NOISE_GRID_DIR");
		}
		Shader.SetGlobalFloat("NGSS_BANDING_TO_NOISE_RATIO_DIR", NGSS_NOISE_SCALE);
		if (NGSS_PCSS_ENABLED)
		{
			float num2 = NGSS_PCSS_SOFTNESS_NEAR * 0.25f;
			float num3 = NGSS_PCSS_SOFTNESS_FAR * 0.25f;
			Shader.SetGlobalFloat("NGSS_PCSS_FILTER_DIR_MIN", (num2 > num3) ? num3 : num2);
			Shader.SetGlobalFloat("NGSS_PCSS_FILTER_DIR_MAX", (num3 < num2) ? num2 : num3);
			Shader.EnableKeyword("NGSS_PCSS_FILTER_DIR");
		}
		else
		{
			Shader.DisableKeyword("NGSS_PCSS_FILTER_DIR");
		}
		if (NGSS_SHADOWS_RESOLUTION == ShadowMapResolution.UseQualitySettings)
		{
			DirLight.shadowResolution = LightShadowResolution.FromQualitySettings;
		}
		else
		{
			DirLight.shadowCustomResolution = (int)NGSS_SHADOWS_RESOLUTION;
		}
		GLOBAL_CASCADES_COUNT = ((GLOBAL_CASCADES_COUNT != 1) ? ((GLOBAL_CASCADES_COUNT == 3) ? 4 : GLOBAL_CASCADES_COUNT) : 0);
		GLOBAL_SHADOWS_DISTANCE = Mathf.Clamp(GLOBAL_SHADOWS_DISTANCE, 0f, GLOBAL_SHADOWS_DISTANCE);
		if (GLOBAL_SETTINGS_OVERRIDE)
		{
			QualitySettings.shadowDistance = GLOBAL_SHADOWS_DISTANCE;
			QualitySettings.shadowProjection = GLOBAL_SHADOWS_PROJECTION;
			if (GLOBAL_CASCADES_COUNT > 1)
			{
				QualitySettings.shadowCascades = GLOBAL_CASCADES_COUNT;
				QualitySettings.shadowCascade4Split = new Vector3(GLOBAL_CASCADES_SPLIT_VALUE, GLOBAL_CASCADES_SPLIT_VALUE * 2f, GLOBAL_CASCADES_SPLIT_VALUE * 2f * 2f);
				QualitySettings.shadowCascade2Split = GLOBAL_CASCADES_SPLIT_VALUE * 2f;
			}
			else
			{
				QualitySettings.shadowCascades = 0;
			}
		}
		if (GLOBAL_CASCADES_COUNT > 1)
		{
			Shader.SetGlobalFloat("NGSS_CASCADES_SOFTNESS_NORMALIZATION", NGSS_CASCADES_SOFTNESS_NORMALIZATION);
			Shader.SetGlobalFloat("NGSS_CASCADES_COUNT", GLOBAL_CASCADES_COUNT);
			Shader.SetGlobalVector("NGSS_CASCADES_SPLITS", (QualitySettings.shadowCascades == 2) ? new Vector4(QualitySettings.shadowCascade2Split, 1f, 1f, 1f) : new Vector4(QualitySettings.shadowCascade4Split.x, QualitySettings.shadowCascade4Split.y, QualitySettings.shadowCascade4Split.z, 1f));
		}
		if (NGSS_CASCADES_BLENDING && GLOBAL_CASCADES_COUNT > 1)
		{
			Shader.EnableKeyword("NGSS_USE_CASCADE_BLENDING");
			Shader.SetGlobalFloat("NGSS_CASCADE_BLEND_DISTANCE", NGSS_CASCADES_BLENDING_VALUE * 0.125f);
		}
		else
		{
			Shader.DisableKeyword("NGSS_USE_CASCADE_BLENDING");
		}
		isSetup = true;
	}
}
