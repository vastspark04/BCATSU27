using UnityEngine;

namespace OC{

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
public class OverCloudCamera : MonoBehaviour
{
	[Header("General")]
	[Tooltip("The level of downsampling to use when rendering the volumetric clouds and volumetric lighting. This enables you to render the effects at 1/2, 1/4 or 1/8 resolution and can give you a big performance boost in exchange for fidelity.")]
	public DownSampleFactor downsampleFactor = DownSampleFactor.Half;

	[Header("Volumetric Clouds")]
	[Tooltip("Toggle the rendering of the volumetric clouds.")]
	public bool renderVolumetricClouds = true;

	[Tooltip("Toggle the rendering of the 2D fallback cloud plane for the volumetric clouds.")]
	public bool render2DFallback = true;

	[Tooltip("The number of samples to use when ray-marching the lighting for the volumetric clouds. A higher value will look nicer at the cost of performance.")]
	public SampleCount lightSampleCount = SampleCount.Normal;

	[Tooltip("Use the high-resolution 3D noise for the light ray-marching for the volumetric clouds, which is normally only used for the alpha.")]
	public bool highQualityClouds;

	[Tooltip("Downsample the 2D clouds along with the volumetric ones. Can save performance at the cost of fidelity, especially around the horizon.")]
	public bool downsample2DClouds;

	[Header("Atmosphere")]
	[Tooltip("Toggle the rendering of atmospheric scattering and fog.")]
	public bool renderAtmosphere = true;

	[Tooltip("Enable the scattering mask (god rays).")]
	public bool renderScatteringMask = true;

	[Tooltip("Include the cascaded shadow map in the scattering mask.")]
	public bool includeCascadedShadows = true;

	[Tooltip("How many samples the scattering mask should use when rendering. More results in higher quality but slower rendering.")]
	public SampleCount scatteringMaskSamples = SampleCount.Normal;

	[Header("Weather")]
	[Tooltip("Enable the rain height mask. This is what prevents surfaces from being wet depending on their position beneath other geometry (dynamic wetness).")]
	public bool renderRainMask;

	private Camera camera;

	private bool hasUpdated;

	private void OnEnable()
	{
		camera = GetComponent<Camera>();
	}

	private void OnDisable()
	{
	}

	private void OnPreRender()
	{
		if (base.enabled)
		{
			if (base.gameObject.tag == "MainCamera" && !hasUpdated)
			{
				OverCloud.CameraUpdate(camera);
				hasUpdated = true;
			}
			else if (renderVolumetricClouds)
			{
				OverCloud.PositionCloudVolume(camera);
			}
			OverCloud.Render(camera, renderVolumetricClouds, render2DFallback, renderAtmosphere, renderScatteringMask, includeCascadedShadows, downsample2DClouds, scatteringMaskSamples, renderRainMask, downsampleFactor, lightSampleCount, highQualityClouds);
		}
	}

	private void OnPostRender()
	{
		if (base.enabled)
		{
			Shader.DisableKeyword("OVERCLOUD_ENABLED");
			Shader.DisableKeyword("OVERCLOUD_ATMOSPHERE_ENABLED");
			Shader.DisableKeyword("DOWNSAMPLE_2D_CLOUDS");
			Shader.DisableKeyword("RAIN_MASK_ENABLED");
			Shader.DisableKeyword("OVERCLOUD_SKY_ENABLED");
			Shader.SetGlobalFloat("_CascadeShadowMapPresent", 0f);
		}
		OverCloud.CleanUp();
		hasUpdated = false;
	}

	private void EditorUpdate()
	{
		if (base.enabled)
		{
			Camera current = Camera.current;
			if ((bool)current && !(current.name != "SceneCamera") && base.gameObject.tag == "MainCamera")
			{
				OverCloud.CameraUpdate(current);
			}
		}
	}
}}
