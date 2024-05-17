using System;
using System.Collections;
using System.Collections.Generic;
using OC;
using UnityEngine;
using UnityEngine.Rendering;

public class EnvironmentManager : MonoBehaviour
{
	[Serializable]
	public class EnvironmentSetting
	{
		public string name;

		public Light sun;

		public Cubemap skyCubemap;

		public Cubemap fogCubemap;

		public Cubemap highAltCubemap;

		public float cubeRotation;

		public int fogMipLevel;

		public float ambientIntensity = 1f;

		public float lightFlareSizeMult = 1f;

		public float cityLightBrightness;

		public List<GameObject> environmentObjects;

		public bool useCloudColors;

		public Color cloudShadowColor;

		public Color cloudLightColor;

		public Color cityLODColor = Color.white;
	}

	private static EnvironmentManager _instance;

	public ReflectionProbe probe;

	public bool realtimeReflectionBake;

	public List<EnvironmentSetting> options;

	public string currentEnvironment;

	public float _SkyAltColorDiv = 10000f;

	public float _SkyAltVertDiv = 100000f;

	public float _SphericalClipFactor = 0.75f;

	public static EnvironmentManager instance
	{
		get
		{
			return _instance;
		}
		private set
		{
			_instance = value;
		}
	}

	public static float CurrentSunBrightness
	{
		get
		{
			if ((bool)_instance)
			{
				return _instance.GetCurrentEnvironment().sun.intensity;
			}
			return 0.5f;
		}
	}

	public static float CurrentLightFlareSizeMult
	{
		get
		{
			if ((bool)_instance)
			{
				return _instance.GetCurrentEnvironment().lightFlareSizeMult;
			}
			return 1f;
		}
	}

	public Texture currentSkyTexture { get; private set; }

	public Texture currentSkyTextureHigh { get; private set; }

	public Texture currentFogTexture { get; private set; }

	public event Action<EnvironmentSetting> OnEnvironmentChanged;

	[ContextMenu("Apply Spherical Clip Factor")]
	public void ApplySphericalClipFactor()
	{
		Shader.SetGlobalFloat("_SphericalClipFactor", _SphericalClipFactor);
	}

	public EnvironmentSetting GetCurrentEnvironment()
	{
		foreach (EnvironmentSetting option in options)
		{
			if (option.name == currentEnvironment)
			{
				return option;
			}
		}
		return null;
	}

	private void Awake()
	{
		instance = this;
		ApplySphericalClipFactor();
		Shader.EnableKeyword("TERRAIN_BOOLS");
	}

	private void Start()
	{
		SetCurrent();
		if (realtimeReflectionBake)
		{
			StartCoroutine(RealtimeUpdateRoutine());
		}
	}

	private void SetEnvironment(string environmentName)
	{
		ApplySphericalClipFactor();
		currentEnvironment = environmentName;
		Shader.SetGlobalFloat("_SkyAltVertDiv", _SkyAltVertDiv);
		Shader.SetGlobalFloat("_SkyAltColorDiv", _SkyAltColorDiv);
		Shader.SetGlobalFloat("_CurrentLightFlareSizeMult", CurrentLightFlareSizeMult);
		Shader.SetGlobalFloat("_CityLightBrightness", GetCurrentEnvironment().cityLightBrightness);
		if (VTResources.useOverCloud)
		{
			if (environmentName == "morning")
			{
				OverCloud.timeOfDay.time = 5.0;
			}
			else if (environmentName == "night")
			{
				OverCloud.timeOfDay.time = 21.0;
			}
			else
			{
				OverCloud.timeOfDay.time = 10.0;
			}
			return;
		}
		foreach (EnvironmentSetting option in options)
		{
			if (option.name == environmentName)
			{
				RenderSettings.ambientIntensity = option.ambientIntensity;
				foreach (GameObject environmentObject in option.environmentObjects)
				{
					environmentObject.SetActive(value: true);
				}
				if ((bool)option.fogCubemap)
				{
					currentFogTexture = option.fogCubemap;
				}
				else
				{
					currentFogTexture = option.skyCubemap;
				}
				Shader.SetGlobalTexture("_GlobalFogCube", currentFogTexture);
				currentSkyTexture = option.skyCubemap;
				currentSkyTextureHigh = option.highAltCubemap;
				Shader.SetGlobalTexture("_GlobalSkyCube", option.skyCubemap);
				Shader.SetGlobalTexture("_GlobalSkyCubeHigh", option.highAltCubemap);
				Shader.SetGlobalTexture("_GlobalFogCubeHigh", option.highAltCubemap);
				Shader.SetGlobalInt("_GlobalFogMip", option.fogMipLevel);
				Shader.SetGlobalFloat("_GlobalCubeRotation", option.cubeRotation);
				Shader.SetGlobalFloat("_SinGlobalCubeRotation", Mathf.Sin(option.cubeRotation * ((float)Math.PI / 180f)));
				Shader.SetGlobalFloat("_CosGlobalCubeRotation", Mathf.Cos(option.cubeRotation * ((float)Math.PI / 180f)));
				Vector3 forward = option.sun.transform.forward;
				Vector4 value = new Vector4(forward.x, forward.y, forward.z, 0f);
				Shader.SetGlobalVector("_GlobalSunDir", value);
				Shader.SetGlobalColor("_GlobalSunColor", option.sun.color * option.sun.intensity);
				if ((bool)probe)
				{
					if (realtimeReflectionBake)
					{
						probe.mode = ReflectionProbeMode.Realtime;
						probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
					}
					else
					{
						probe.customBakedTexture = option.skyCubemap;
						probe.mode = ReflectionProbeMode.Custom;
					}
				}
				continue;
			}
			foreach (GameObject environmentObject2 in option.environmentObjects)
			{
				environmentObject2.SetActive(value: false);
			}
		}
		this.OnEnvironmentChanged?.Invoke(GetCurrentEnvironment());
	}

	private IEnumerator RealtimeUpdateRoutine()
	{
		yield return null;
		while (base.enabled)
		{
			probe.transform.position = VRHead.position;
			yield return null;
		}
	}

	[ContextMenu("Set Current Environment")]
	public void SetCurrent()
	{
		SetEnvironment(currentEnvironment);
	}

	[ContextMenu("Reset HUD Brightness")]
	public void ResetHUDBrightness()
	{
		Shader.SetGlobalFloat("_HUDBrightness", 1f);
	}

	public static CampaignScenario.EnvironmentOption[] GetGlobalEnvOptions()
	{
		return new CampaignScenario.EnvironmentOption[3]
		{
			new CampaignScenario.EnvironmentOption("Morning", "morning"),
			new CampaignScenario.EnvironmentOption("Day", "day"),
			new CampaignScenario.EnvironmentOption("Night", "night")
		};
	}
}
