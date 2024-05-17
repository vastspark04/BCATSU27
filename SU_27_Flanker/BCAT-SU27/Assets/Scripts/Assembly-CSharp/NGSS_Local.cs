using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Light))]
[ExecuteInEditMode]
public class NGSS_Local : MonoBehaviour
{
	public enum ShadowMapResolution
	{
		UseQualitySettings = 0x100,
		VeryLow = 0x200,
		Low = 0x400,
		Med = 0x800,
		High = 0x1000,
		Ultra = 0x2000
	}

	[Tooltip("Check this option to disable this component from receiving updates calls at runtime or when you hit play in Editor.\nUseful when you have lot of lights in your scene and you don't want that many update calls.")]
	public bool NGSS_DISABLE_ON_PLAY;

	[Tooltip("Check this option if you don't need to update shadows variables at runtime, only once when scene loads.\nUseful when you have lot of lights in your scene and you don't want that many update calls.")]
	public bool NGSS_NO_UPDATE_ON_PLAY;

	[Tooltip("If enabled, this component will manage GLOBAL SETTINGS for all Local shadows.\nEnable this option only in one of your scene local lights to avoid multiple lights fighting for global tweaks.\nLOCAL SETTINGS are not affected by this option.")]
	public bool NGSS_MANAGE_GLOBAL_SETTINGS;

	[Header("GLOBAL SETTINGS")]
	[Tooltip("PCSS Requires inline sampling and SM3.5.\nProvides Area Light soft-shadows.\nDisable it if you are looking for PCF filtering (uniform soft-shadows) which runs with SM3.0.")]
	public bool NGSS_PCSS_ENABLED = true;

	[Tooltip("How soft shadows are when close to caster. Low values means sharper shadows.")]
	[Range(0f, 2f)]
	public float NGSS_PCSS_SOFTNESS_NEAR;

	[Tooltip("How soft shadows are when far from caster. Low values means sharper shadows.")]
	[Range(0f, 2f)]
	public float NGSS_PCSS_SOFTNESS_FAR = 1f;

	[Tooltip("Value to fix blocker search bias artefacts. Be careful with extreme values, can lead to false self-shadowing.")]
	[Range(0f, 1f)]
	public float NGSS_PCSS_BLOCKER_BIAS;

	[Space]
	[Tooltip("Used to test blocker search and early bail out algorithms. Keep it as low as possible, might lead to noise artifacts if too low.\nRecommended values: Mobile = 8, Consoles & VR = 16, Desktop = 24")]
	[Range(4f, 32f)]
	public int NGSS_SAMPLING_TEST = 16;

	[Tooltip("Number of samplers per pixel used for PCF and PCSS shadows algorithms.\nRecommended values: Mobile = 12, Consoles & VR = 24, Desktop Med = 32, Desktop High = 48, Desktop Ultra = 64")]
	[Range(4f, 64f)]
	public int NGSS_SAMPLING_FILTER = 32;

	[Space]
	[Tooltip("If zero = no noise.\nIf one = 100% noise.\nUseful when fighting banding.")]
	[Range(0f, 1f)]
	public float NGSS_NOISE_SCALE = 1f;

	[Space]
	[Tooltip("Number of samplers per pixel used for PCF and PCSS shadows algorithms.\nRecommended values: Mobile = 12, Consoles & VR = 24, Desktop Med = 32, Desktop High = 48, Desktop Ultra = 64")]
	[Range(0f, 1f)]
	public float NGSS_SHADOWS_OPACITY = 1f;

	[Header("LOCAL SETTINGS")]
	[Tooltip("Defines the Penumbra size of this shadows.")]
	[Range(0f, 1f)]
	public float NGSS_SHADOWS_SOFTNESS = 1f;

	[Tooltip("If 1 noise will be aligned in a grid pattern (dithering). If 0, noise will be more randomly distributed (white noise).\nDithering is recommended when using low sampling count (less than 16 spp)")]
	[Range(0f, 1f)]
	public float NGSS_SHADOWS_DITHERING = 1f;

	[Tooltip("Shadows resolution.\nUseQualitySettings = From Quality Settings, SuperLow = 512, Low = 1024, Med = 2048, High = 4096, Ultra = 8192.")]
	public ShadowMapResolution NGSS_SHADOWS_RESOLUTION = ShadowMapResolution.UseQualitySettings;

	private bool isInitialized;

	private Light _LocalLight;

	private Light LocalLight
	{
		get
		{
			if (_LocalLight == null)
			{
				_LocalLight = GetComponent<Light>();
			}
			return _LocalLight;
		}
	}

	private void OnDisable()
	{
		isInitialized = false;
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
			LocalLight.shadows = ((!NGSS_PCSS_ENABLED) ? LightShadows.Hard : LightShadows.Soft);
			Shader.SetGlobalFloat("NGSS_PCSS_FILTER_LOCAL_MIN", NGSS_PCSS_SOFTNESS_NEAR);
			Shader.SetGlobalFloat("NGSS_PCSS_FILTER_LOCAL_MAX", NGSS_PCSS_SOFTNESS_FAR);
			SetProperties(NGSS_MANAGE_GLOBAL_SETTINGS);
			isInitialized = true;
		}
	}

	private bool IsNotSupported()
	{
		return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2;
	}

	private void Update()
	{
		if (LocalLight.shadows == LightShadows.None)
		{
			return;
		}
		if (Application.isPlaying)
		{
			if (NGSS_DISABLE_ON_PLAY)
			{
				base.enabled = false;
				return;
			}
			if (NGSS_NO_UPDATE_ON_PLAY)
			{
				return;
			}
		}
		SetProperties(NGSS_MANAGE_GLOBAL_SETTINGS);
	}

	private void SetProperties(bool setLocalAndGlobalProperties)
	{
		LocalLight.shadowStrength = NGSS_SHADOWS_SOFTNESS;
		if (NGSS_SHADOWS_RESOLUTION == ShadowMapResolution.UseQualitySettings)
		{
			LocalLight.shadowResolution = LightShadowResolution.FromQualitySettings;
		}
		else
		{
			LocalLight.shadowCustomResolution = (int)NGSS_SHADOWS_RESOLUTION;
		}
		if (setLocalAndGlobalProperties)
		{
			NGSS_SAMPLING_TEST = Mathf.Clamp(NGSS_SAMPLING_TEST, 4, NGSS_SAMPLING_FILTER);
			Shader.SetGlobalFloat("NGSS_TEST_SAMPLERS", NGSS_SAMPLING_TEST);
			LocalLight.shadows = ((!NGSS_PCSS_ENABLED) ? LightShadows.Hard : LightShadows.Soft);
			Shader.SetGlobalFloat("NGSS_PCSS_FILTER_LOCAL_MIN", NGSS_PCSS_SOFTNESS_NEAR);
			Shader.SetGlobalFloat("NGSS_PCSS_FILTER_LOCAL_MAX", NGSS_PCSS_SOFTNESS_FAR);
			Shader.SetGlobalFloat("NGSS_PCSS_LOCAL_BLOCKER_BIAS", NGSS_PCSS_BLOCKER_BIAS * 0.01f);
			Shader.SetGlobalFloat("NGSS_SHADOWS_DITHERING", NGSS_SHADOWS_DITHERING);
			Shader.SetGlobalFloat("NGSS_FILTER_SAMPLERS", NGSS_SAMPLING_FILTER);
			Shader.SetGlobalFloat("NGSS_BANDING_TO_NOISE_RATIO", NGSS_NOISE_SCALE);
			Shader.SetGlobalFloat("NGSS_GLOBAL_OPACITY", 1f - NGSS_SHADOWS_OPACITY);
		}
	}
}
