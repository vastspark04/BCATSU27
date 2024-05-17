using System.Collections;
using UnityEngine;

public class NightVisionGoggles : MonoBehaviour
{
	public Light illuminatorLight;

	public Light vesselIlluminatorLight;

	public ScreenMaskedColorRamp colorEffect;

	public const string globalEffectScaleName = "_NVGEffectScale";

	private int gEffectID;

	public float offRampOffset;

	public float targetRampOffset;

	private Coroutine nvgRoutine;

	private bool illuminatorEnabled;

	private int illuminatorMask;

	private int vesselIlluminatorMask;

	public bool doIllumination = true;

	private Light sun;

	private int sunCullMask;

	private float envAmbientIntensity;

	private int uvXScaleID;

	private float rT;

	public bool nvgEnabled { get; private set; }

	public static float nvgEffectScale { get; private set; }

	private void Awake()
	{
		gEffectID = Shader.PropertyToID("_NVGEffectScale");
		SetNVGEffectScale(0f);
	}

	private void SetNVGEffectScale(float s)
	{
		nvgEffectScale = s;
		Shader.SetGlobalFloat(gEffectID, s);
	}

	private void Start()
	{
		colorEffect.rampOffset = offRampOffset;
		colorEffect.enabled = false;
		illuminatorLight.enabled = false;
		illuminatorLight.gameObject.SetActive(value: true);
		illuminatorLight.transform.parent = null;
		illuminatorMask = illuminatorLight.cullingMask;
		illuminatorLight.cullingMask = 0;
		if ((bool)vesselIlluminatorLight)
		{
			vesselIlluminatorMask = vesselIlluminatorLight.cullingMask;
			vesselIlluminatorLight.cullingMask = 0;
		}
		if (GameSettings.CurrentSettings.GetBoolSetting("FULLSCREEN_NVG"))
		{
			colorEffect.maskTex = null;
			CameraSetGlobalTexture component = colorEffect.GetComponent<CameraSetGlobalTexture>();
			if ((bool)component)
			{
				component.texture = null;
			}
		}
		nvgEnabled = false;
	}

	private void OnDestroy()
	{
		if ((bool)illuminatorLight)
		{
			Object.Destroy(illuminatorLight.gameObject);
		}
	}

	public void EnableNVG()
	{
		nvgEnabled = true;
		if (nvgRoutine != null)
		{
			StopCoroutine(nvgRoutine);
		}
		EnvironmentManager.EnvironmentSetting currentEnvironment = EnvironmentManager.instance.GetCurrentEnvironment();
		sun = currentEnvironment.sun;
		sunCullMask = sun.cullingMask;
		illuminatorLight.intensity = 4.5f * sun.intensity;
		illuminatorLight.shadows = LightShadows.Soft;
		if ((bool)vesselIlluminatorLight)
		{
			vesselIlluminatorLight.intensity = 3.5f * sun.intensity;
			vesselIlluminatorLight.shadows = LightShadows.None;
		}
		envAmbientIntensity = currentEnvironment.ambientIntensity;
		nvgRoutine = StartCoroutine(EnableNVGRoutine());
	}

	public void DisableNVG()
	{
		nvgEnabled = false;
		if (nvgRoutine != null)
		{
			StopCoroutine(nvgRoutine);
		}
		nvgRoutine = StartCoroutine(DisableNVGRoutine());
	}

	public void DisableNVGImmediate()
	{
		nvgEnabled = false;
		RenderSettings.ambientIntensity = EnvironmentManager.instance.GetCurrentEnvironment().ambientIntensity;
		colorEffect.rampOffset = offRampOffset;
		colorEffect.enabled = false;
		illuminatorEnabled = false;
		illuminatorLight.enabled = false;
		illuminatorLight.cullingMask = 0;
		SetNVGEffectScale(0f);
		if ((bool)vesselIlluminatorLight)
		{
			vesselIlluminatorLight.enabled = false;
			vesselIlluminatorLight.cullingMask = 0;
		}
	}

	public bool IsNVGVisible()
	{
		return colorEffect.enabled;
	}

	private IEnumerator EnableNVGRoutine()
	{
		colorEffect.rampOffset = offRampOffset;
		colorEffect.enabled = true;
		illuminatorEnabled = true;
		illuminatorLight.enabled = true;
		illuminatorLight.transform.rotation = sun.transform.rotation;
		if ((bool)vesselIlluminatorLight)
		{
			vesselIlluminatorLight.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
			vesselIlluminatorLight.enabled = true;
		}
		uvXScaleID = Shader.PropertyToID("_NVGUVXScale");
		float num = GameSettings.GetIPDMeters() / 0.058f;
		Shader.SetGlobalFloat(uvXScaleID, 1f / num);
		while (rT < 1f)
		{
			rT = Mathf.MoveTowards(rT, 1f, Time.deltaTime);
			colorEffect.rampOffset = Mathf.Lerp(offRampOffset, targetRampOffset, rT);
			colorEffect.effectScale = rT;
			SetNVGEffectScale(rT);
			yield return null;
		}
		colorEffect.rampOffset = targetRampOffset;
		colorEffect.effectScale = 1f;
		SetNVGEffectScale(1f);
		while (colorEffect.enabled)
		{
			num = GameSettings.GetIPDMeters() / 0.058f;
			Shader.SetGlobalFloat(uvXScaleID, 1f / num);
			yield return null;
		}
	}

	private IEnumerator DisableNVGRoutine()
	{
		while (rT > 0f)
		{
			rT = Mathf.MoveTowards(rT, 0f, Time.deltaTime);
			colorEffect.rampOffset = Mathf.Lerp(offRampOffset, targetRampOffset, rT);
			colorEffect.effectScale = rT;
			SetNVGEffectScale(rT);
			yield return null;
		}
		RenderSettings.ambientIntensity = envAmbientIntensity;
		colorEffect.rampOffset = offRampOffset;
		colorEffect.effectScale = 0f;
		SetNVGEffectScale(0f);
		colorEffect.enabled = false;
		illuminatorEnabled = false;
		illuminatorLight.enabled = false;
		illuminatorLight.cullingMask = 0;
		if ((bool)vesselIlluminatorLight)
		{
			vesselIlluminatorLight.enabled = false;
			vesselIlluminatorLight.cullingMask = 0;
		}
	}

	private void OnPreCull()
	{
		if (illuminatorEnabled && doIllumination)
		{
			EnableIlluminator();
		}
	}

	private void OnPostRender()
	{
		if (illuminatorEnabled)
		{
			DisableIlluminator();
		}
	}

	public void EnableIlluminator()
	{
		illuminatorLight.cullingMask = illuminatorMask;
		sun.cullingMask = 0;
		if ((bool)vesselIlluminatorLight)
		{
			vesselIlluminatorLight.transform.rotation = VRHead.instance.transform.rotation;
			vesselIlluminatorLight.cullingMask = vesselIlluminatorMask;
		}
		RenderSettings.ambientIntensity = 1f;
	}

	public void DisableIlluminator()
	{
		illuminatorLight.cullingMask = 0;
		if ((bool)vesselIlluminatorLight)
		{
			vesselIlluminatorLight.cullingMask = 0;
		}
		sun.cullingMask = sunCullMask;
		RenderSettings.ambientIntensity = envAmbientIntensity;
	}
}
