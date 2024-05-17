using System;
using System.Collections.Generic;
using UnityEngine;

namespace OC{

[Serializable]
public class WeatherPreset
{
	[Tooltip("The name of the preset.")]
	public string name;

	[Tooltip("The altitude at which the volumetric cloud plane will appear.")]
	public float cloudPlaneAltitude = 1200f;

	[Tooltip("The height of the volumetric cloud plane.")]
	[Range(0f, 1000f)]
	public float cloudPlaneHeight = 400f;

	[Tooltip("The small-scale volumetric cloudiness.")]
	[Range(0f, 1f)]
	public float cloudiness = 0.75f;

	[Tooltip("A sharpening value to apply to the small-scale volumetric cloudiness.")]
	[Range(0f, 1f)]
	public float sharpness = 0.25f;

	[Tooltip("The large-scale volumetric cloudiness.")]
	[Range(0f, 1f)]
	public float macroCloudiness = 0.75f;

	[Tooltip("A sharpening value to apply to the large-scale volumetric cloudiness.")]
	[Range(0f, 1f)]
	public float macroSharpness = 0.25f;

	[Tooltip("The density value used for calculating the alpha of the clouds.")]
	[Range(0f, 8f)]
	public float opticalDensity = 1f;

	[Tooltip("The density value used for calculating the lighting of the clouds.")]
	[Range(0f, 8f)]
	public float lightingDensity = 1f;

	[Tooltip("The density of the cloud shadows.")]
	[Range(0f, 4f)]
	public float cloudShadowsDensity = 1f;

	[Tooltip("The opacity of the cloud shadows.")]
	[Range(0f, 4f)]
	public float cloudShadowsOpacity = 1f;

	[Tooltip("The amount of precipitation (rain/snow) from the clouds.")]
	[Range(0f, 1f)]
	public float precipitation;

	[Tooltip("The odds of a lightning strike.")]
	[Range(0f, 1f)]
	public float lightningChance;

	[Tooltip("A multiplier value which is used when incrementing the wind time.")]
	public float windMultiplier;

	[Range(0f, 1f)]
	[Tooltip("A sharpening value to apply to the wetness effect below the clouds.")]
	public float wetnessRemap = 0.5f;

	[Range(0f, 1f)]
	[Tooltip("How much the albedo should be darkened by wet areas below the clouds.")]
	public float wetnessDarken = 0.5f;

	[Range(0f, 1f)]
	[Tooltip("How much the gloss should be increased by wet areas below the clouds.")]
	public float wetnessGloss = 0.75f;

	[Tooltip("The density level of the global height fog.")]
	[Range(0f, 1f)]
	public float fogDensity;

	[Tooltip("The fog/scattering blend factor. 0 = only fog. 1 = balance between the two. 2 = scattering will appear on top of fog.")]
	[Range(0f, 2f)]
	public float fogBlend = 1f;

	[Tooltip("The color of the fog.")]
	public Color fogAlbedo = new Color(0f, 0.1f, 0.2f, 1f);

	[Tooltip("How much the fog should be affected by direct lighting from the sun and moon.")]
	[Range(0f, 1f)]
	public float fogDirectIntensity = 0.25f;

	[Tooltip("How much the fog should be affected by indirect lighting from the sky.")]
	[Range(0f, 1f)]
	public float fogAmbientIntensity = 0.25f;

	[Tooltip("The intensity of the fog shadow effect.")]
	[Range(0f, 1f)]
	public float fogShadow = 1f;

	[Tooltip("The upper limit of the volumetric fog volume. Defined in cloud height factors.")]
	[Range(0f, 4f)]
	public float fogHeight = 1f;

	[Tooltip("Fog height falloff, in meters.")]
	public float fogFalloff = 1000f;

	[Tooltip("This is where custom floats show up, if any are defined.")]
	public float[] customFloats;

	public WeatherPreset(string name)
	{
		this.name = name;
	}

	public WeatherPreset(string p_name, float p_cloudPlaneAltitude, float p_cloudPlaneHeight, float p_cloudiness, float p_sharpness, float p_macroCloudiness, float p_macroSharpness, float p_opticalDensity, float p_lightingDensity, float p_cloudShadowsDensity, float p_cloudShadowsOpacity, float p_precipitation, float p_lightningChance, float p_windMultiplier, float p_wetnessRemap, float p_wetnessDarken, float p_wetnessGloss, float p_fogDensity, float p_fogBlend, Color p_fogAlbedo, float p_fogDirectIntensity, float p_fogAmbientIntensity, float p_fogShadow, float p_fogHeight, float p_fogFalloff)
	{
		name = p_name;
		cloudPlaneAltitude = p_cloudPlaneAltitude;
		cloudPlaneHeight = p_cloudPlaneHeight;
		cloudiness = p_cloudiness;
		sharpness = p_sharpness;
		macroCloudiness = p_macroCloudiness;
		macroSharpness = p_macroSharpness;
		opticalDensity = p_opticalDensity;
		lightingDensity = p_lightingDensity;
		cloudShadowsDensity = p_cloudShadowsDensity;
		cloudShadowsOpacity = p_cloudShadowsOpacity;
		precipitation = p_precipitation;
		lightningChance = p_lightningChance;
		windMultiplier = p_windMultiplier;
		wetnessRemap = p_wetnessRemap;
		wetnessDarken = p_wetnessDarken;
		wetnessGloss = p_wetnessGloss;
		fogDensity = p_fogDensity;
		fogBlend = p_fogBlend;
		fogAlbedo = p_fogAlbedo;
		fogDirectIntensity = p_fogDirectIntensity;
		fogAmbientIntensity = p_fogAmbientIntensity;
		fogShadow = p_fogShadow;
		fogHeight = p_fogHeight;
		fogFalloff = p_fogFalloff;
	}

	public WeatherPreset(WeatherPreset obj)
	{
		name = obj.name;
		cloudPlaneAltitude = obj.cloudPlaneAltitude;
		cloudPlaneHeight = obj.cloudPlaneHeight;
		cloudiness = obj.cloudiness;
		sharpness = obj.sharpness;
		macroCloudiness = obj.macroCloudiness;
		macroSharpness = obj.macroSharpness;
		opticalDensity = obj.opticalDensity;
		lightingDensity = obj.lightingDensity;
		cloudShadowsDensity = obj.cloudShadowsDensity;
		cloudShadowsOpacity = obj.cloudShadowsOpacity;
		precipitation = obj.precipitation;
		lightningChance = obj.lightningChance;
		windMultiplier = obj.windMultiplier;
		wetnessRemap = obj.wetnessRemap;
		wetnessDarken = obj.wetnessDarken;
		wetnessGloss = obj.wetnessGloss;
		fogDensity = obj.fogDensity;
		fogBlend = obj.fogBlend;
		fogAlbedo = obj.fogAlbedo;
		fogDirectIntensity = obj.fogDirectIntensity;
		fogAmbientIntensity = obj.fogAmbientIntensity;
		fogShadow = obj.fogShadow;
		fogHeight = obj.fogHeight;
		fogFalloff = obj.fogFalloff;
		customFloats = obj.customFloats;
	}

	public void Lerp(WeatherPreset a, WeatherPreset b, float t)
	{
		cloudiness = Mathf.Lerp(a.cloudiness, b.cloudiness, t);
		cloudPlaneAltitude = Mathf.Lerp(a.cloudPlaneAltitude, b.cloudPlaneAltitude, t);
		cloudPlaneHeight = Mathf.Lerp(a.cloudPlaneHeight, b.cloudPlaneHeight, t);
		sharpness = Mathf.Lerp(a.sharpness, b.sharpness, t);
		macroCloudiness = Mathf.Lerp(a.macroCloudiness, b.macroCloudiness, t);
		macroSharpness = Mathf.Lerp(a.macroSharpness, b.macroSharpness, t);
		opticalDensity = Mathf.Lerp(a.opticalDensity, b.opticalDensity, t);
		lightingDensity = Mathf.Lerp(a.lightingDensity, b.lightingDensity, t);
		cloudShadowsDensity = Mathf.Lerp(a.cloudShadowsDensity, b.cloudShadowsDensity, t);
		cloudShadowsOpacity = Mathf.Lerp(a.cloudShadowsOpacity, b.cloudShadowsOpacity, t);
		precipitation = Mathf.Lerp(a.precipitation, b.precipitation, t);
		lightningChance = Mathf.Lerp(a.lightningChance, b.lightningChance, t);
		windMultiplier = Mathf.Lerp(a.windMultiplier, b.windMultiplier, t);
		wetnessRemap = Mathf.Lerp(a.wetnessRemap, b.wetnessRemap, t);
		wetnessDarken = Mathf.Lerp(a.wetnessDarken, b.wetnessDarken, t);
		wetnessGloss = Mathf.Lerp(a.wetnessGloss, b.wetnessGloss, t);
		fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t);
		fogBlend = Mathf.Lerp(a.fogBlend, b.fogBlend, t);
		fogAlbedo = Color.Lerp(a.fogAlbedo, b.fogAlbedo, t);
		fogDirectIntensity = Mathf.Lerp(a.fogDirectIntensity, b.fogDirectIntensity, t);
		fogAmbientIntensity = Mathf.Lerp(a.fogAmbientIntensity, b.fogAmbientIntensity, t);
		fogShadow = Mathf.Lerp(a.fogShadow, b.fogShadow, t);
		fogHeight = Mathf.Lerp(a.fogHeight, b.fogHeight, t);
		fogFalloff = Mathf.Lerp(a.fogFalloff, b.fogFalloff, t);
		for (int i = 0; i < customFloats.Length; i++)
		{
			customFloats[i] = Mathf.Lerp(a.customFloats[i], b.customFloats[i], t);
		}
	}

	public void AddCustomFloat()
	{
		List<float> list = new List<float>(customFloats);
		list.Add(0f);
		customFloats = list.ToArray();
	}

	public void DeleteCustomFloat(int index)
	{
		List<float> list = new List<float>(customFloats);
		list.RemoveAt(index);
		customFloats = list.ToArray();
	}

	public float GetCustomFloat(string name)
	{
		if (customFloats == null || customFloats.Length < 1)
		{
			return 0f;
		}
		int customFloatIndex = OverCloud.instance.GetCustomFloatIndex(name);
		if (customFloatIndex > -1)
		{
			return customFloats[customFloatIndex];
		}
		return 0f;
}}
}
