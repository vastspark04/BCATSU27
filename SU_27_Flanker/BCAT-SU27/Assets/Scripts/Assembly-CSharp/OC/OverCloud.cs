using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace OC{

[ExecuteInEditMode]
public class OverCloud : MonoBehaviour
{
	public delegate void OverCloudEventHandler();

	[Serializable]
	public class Components
	{
		[Tooltip("The directional light representing the sun.")]
		public Light sun;

		[Tooltip("The directional light representing the moon. OverCloud will automatically fade this out when the sun is active.")]
		public Light moon;

		[Tooltip("The material used for rendering the volumetric clouds. Recommended to not modify.")]
		public Material cloudMaterial;

		[Tooltip("The material used for rendering the skybox. Recommended to not modify.")]
		public Material skyMaterial;
	}

	[Serializable]
	public class VolumetricClouds
	{
		[Serializable]
		public class NoiseSettings
		{
			[Tooltip("The tile rate of the first 3D noise pass.")]
			[Range(0f, 0.01f)]
			public float noiseTiling_A = 0.00035f;

			[Tooltip("The intensity of the first 3D noise pass.")]
			[Range(0f, 1f)]
			public float noiseIntensity_A = 1f;

			[Tooltip("The tile rate of the second 3D noise pass.")]
			[Range(0f, 0.01f)]
			public float noiseTiling_B = 0.0015f;

			[Tooltip("The intensity of the second 3D noise pass.")]
			[Range(0f, 1f)]
			public float noiseIntensity_B = 0.5f;

			[Tooltip("The placement (along the cloud plane height) of the density peak.")]
			[SerializeField]
			[Range(0.001f, 0.999f)]
			public float shapeCenter = 0.3f;

			[Tooltip("Increase the density at the base of the clouds by a set amount.")]
			[SerializeField]
			[Range(0f, 8f)]
			public float baseDensityIncrease = 2f;

			[Tooltip("The amount of noise erosion to apply to the clouds.")]
			[SerializeField]
			[Range(0f, 2f)]
			public float erosion = 1.1f;

			[Tooltip("The lower edge when smooth-stepping the alpha value per-particle.")]
			[SerializeField]
			[Range(0f, 1f)]
			public float alphaEdgeLower = 0.015f;

			[Tooltip("The upper edge when smooth-stepping the alpha value per-particle.")]
			[SerializeField]
			[Range(0f, 1f)]
			public float alphaEdgeUpper = 0.25f;

			[Tooltip("Adds additional scrolling to the 3D noise, making the volumetric clouds appear more turbulent.")]
			[SerializeField]
			[Range(-1f, 1f)]
			public float turbulence = 0.5f;

			[Tooltip("Adds vertical scrolling to the 3D noise.")]
			[SerializeField]
			[Range(-1f, 1f)]
			public float riseFactor = 0.25f;
		}

		[Serializable]
		public class NoiseGeneration
		{
			public _3DNoiseResolution resolution;

			public CloudNoiseGen.NoiseSettings perlin;

			public CloudNoiseGen.NoiseSettings worley;
		}

		[Tooltip("The radius of the high quality pass of the volumetric cloud plane. A larger radius will require larger particles and a higher compositor resolution to look the same.")]
		[Range(50f, 64000f)]
		public float cloudPlaneRadius = 16000f;

		[Tooltip("The resolution of the compostior. Affects volumetric cloud quality, shadows and cloud AO.")]
		public CompositorResolution compositorResolution = CompositorResolution._1024x1024;

		[Tooltip("The amount of blur to apply to the compositor texture after rendering it. A value of 0 will probably give you visible artifacts when the clouds move. Also affects cloud shadows and AO.")]
		[Range(0f, 1f)]
		public float compositorBlur = 0.2f;

		[Tooltip("The noise texture used to render the compositor.")]
		public Texture2D noiseTexture;

		[Tooltip("The scale of the small noise pass when rendering the compositor.")]
		[Range(0f, 1f)]
		public float noiseScale = 0.5f;

		[Tooltip("The scale of the large noise pass when rendering the compositor.")]
		[Range(0f, 1f)]
		public float noiseMacroScale = 0.2f;

		[Tooltip("How many particles the volumetric cloud mesh is made up of. Recommended to leave at the maximum value.")]
		[Range(1000f, 16000f)]
		public int particleCount = 16000;

		[Tooltip("The radius multiplier for the lower quality volumetric cloud pass, which is rendered before the high-quality pass. Actual lod radius = Cloud Plane Radius * Lod Radius Multiplier.")]
		[Range(2f, 8f)]
		public float lodRadiusMultiplier = 4f;

		[Tooltip("Size multiplier for the low quality pass particles. Actual lod particle size = Radius Max * Lod size.")]
		[Range(1f, 8f)]
		public float lodParticleSize = 2.5f;

		[SerializeField]
		private NoiseSettings m_NoiseSettings = new NoiseSettings();

		[SerializeField]
		private NoiseGeneration m_NoiseGeneration = new NoiseGeneration();

		public NoiseSettings noiseSettings => m_NoiseSettings;

		public NoiseGeneration noiseGeneration => m_NoiseGeneration;
	}

	[Serializable]
	public class CloudPlane
	{
		public string name;

		public Texture2D texture;

		public Color color = Color.white;

		public float scale = 200000f;

		public float detailScale = 10000f;

		public float height = 10000f;

		[Range(0f, 1f)]
		public float opacity = 1f;

		public float lightPenetration = 0.5f;

		public float lightAbsorption = 1f;

		public float windTimescale = 1f;

		public CloudPlane(string name)
		{
			this.name = name;
		}

		public CloudPlane(CloudPlane obj)
		{
			name = obj.name + "_copy";
			texture = obj.texture;
			scale = obj.scale;
			detailScale = obj.detailScale;
			height = obj.height;
			opacity = obj.opacity;
			lightPenetration = obj.lightPenetration;
			lightAbsorption = obj.lightAbsorption;
			windTimescale = obj.windTimescale;
		}
	}

	[Serializable]
	public class Atmosphere
	{
		[Serializable]
		public class Precomputation
		{
			[Tooltip("The compute shader used to generate the scattering lookup tables. Don't change this!.")]
			public ComputeShader shader;

			[Tooltip("The size of the planet, measured in earth radii (6 371 km).")]
			public float planetScale = 1f;

			[Tooltip("The height of the atmosphere, measured in earth atmosphere heights (60 km).")]
			public float heightScale = 1f;

			[Tooltip("The Mie density.")]
			public float mie = 1f;

			[Tooltip("The Rayleigh density.")]
			public float rayleigh = 1f;

			[Tooltip("The ozone density.")]
			public float ozone = 1f;

			[Tooltip("The G term of the Mie scattering phase function.")]
			public float phase = 0.8f;
		}

		[Serializable]
		public class ScatteringMaskSettings
		{
			[Tooltip("The range (ratio of the volumetric cloud plane radius) of the scattering mask effect.")]
			[Range(0f, 1f)]
			public float range = 0.15f;

			[Tooltip("Any height below the “floor” will skip scattering mask rendering. Recommended to leave at the lowest height of your scene.")]
			public float floor;

			[Tooltip("The softness of the scattering mask.")]
			[Range(0f, 1f)]
			public float softness = 0.5f;

			[Tooltip("The intensity of the scattering mask.")]
			[Range(0f, 1f)]
			public float intensity = 1f;
		}

		[Tooltip("When checked, OverCloud will override the scene’s skybox material with the OverCloud skybox material. Unless you really, really want your own skybox it is recommended to leave this checked if you want the atmospheric scattering to match the skybox.")]
		public bool overrideSkyboxMaterial = true;

		[Tooltip("The exposure level of the atmospheric scattering. This has been tweaked to appear natural in Unity if left at 1.")]
		public float exposure = 1f;

		[Tooltip("The density of the atmosphere. Setting this to a value other than 1 will break the physically-based result, but it can be useful if the atmosphere in the scene appears too dense.")]
		[Range(0f, 8f)]
		public float density = 1f;

		[Tooltip("Controls how quickly the scene will fade into the skybox (if at all).")]
		[Range(0f, 1f)]
		public float farClipFade = 1f;

		[SerializeField]
		private Precomputation m_Precomputation = new Precomputation();

		[Tooltip("The color of the sun in the sky. This is different from the color of the sun directional light, in that it should always stay the same as it represents the physical color of the sun.")]
		public Color actualSunColor = Color.white;

		[Tooltip("The size of the sun in the sky. A value of 1 = physical size of the sun on earth, however for most cases you probably want to increase it.")]
		public float sunSize = 5f;

		[Tooltip("The intensity of the sun in the sky. Useful parameter to tweak when authoring bloom.")]
		public float sunIntensity = 100f;

		[Tooltip("A multiplicative color to apply to the sun during a solar eclipse. This is useful if you don’t want a solar eclipse to block out all light. Actual solar eclipse color = sun color * solar eclipse color. Set to white to disable solar eclipses entirely.")]
		public Color solarEclipseColor = new Color(0.06f, 0.06f, 0.06f, 1f);

		[Tooltip("The texture used to render the moon celestial body in the sky.")]
		public Cubemap moonAlbedo;

		[Tooltip("The color of the moon in the sky.")]
		public Color actualMoonColor = new Color(0.025f, 0.29f, 0.45f, 1f);

		[Tooltip("The size of the moon in the sky. A value of 1 = physical size of the moon on earth, however for most cases you probably want to increase it.")]
		public float moonSize = 4.5f;

		[Tooltip("The intensity of the moon in the sky.")]
		public float moonIntensity = 15f;

		[Tooltip("A multiplicative color to apply to the moon during a lunar eclipse. Actual lunar eclipse color = moon color * lunar eclipse color. Set to white to disable lunar eclipses entirely.")]
		public Color lunarEclipseColor = new Color(0.15f, 0.017f, 0f, 1f);

		[Tooltip("The color of earth's surface in the skybox.")]
		public Color earthColor = new Color(0.015f, 0.017f, 0.02f, 1f);

		[Tooltip("The intensity of the sun mie scattering effect.")]
		[Range(0f, 8f)]
		public float mieScatteringIntensity = 1f;

		[Tooltip("The G parameter of the Heyney-Greenstein phase function when calculating the Mie scattering. Essentially it controls the “width” of the effect.")]
		[Range(0f, 1f)]
		public float mieScatteringPhase = 0.9f;

		[Tooltip("Same as above, but used specifically for the fog lighting.")]
		[Range(0f, 1f)]
		public float mieScatteringFogPhase = 0.7f;

		[Tooltip("A distance fade to apply to the Mie scattering effect. This fade is used when the scattering mask is disabled.")]
		[Range(0f, 1f)]
		public float mieScatteringDistanceFadeA = 0.6f;

		[Tooltip("A distance fade to apply to the Mie scattering effect. This fade is used when the scattering mask is enabled.")]
		[Range(0f, 1f)]
		public float mieScatteringDistanceFadeB = 0.1f;

		[Tooltip("The amount of night-time scattering to apply to the world and skybox. This is a non-physically based effect, but it adds a lot of atmosphere to night-time scenes. The color of the night scattering is based on the moon color.")]
		[Range(0f, 4f)]
		public float nightScattering = 1f;

		[Tooltip("The space cubemap.")]
		public Cubemap starsCubemap;

		[Tooltip("The intensity of the space cubemap in the skybox.")]
		[Range(0f, 1f)]
		public float starsIntensity = 1f;

		[SerializeField]
		private ScatteringMaskSettings m_ScatteringMask = new ScatteringMaskSettings();

		public Precomputation precomputation => m_Precomputation;

		public ScatteringMaskSettings scatteringMask => m_ScatteringMask;
	}

	[Serializable]
	public class Lighting
	{
		[Serializable]
		public class CloudLighting
		{
			[Tooltip("The color of the clouds.")]
			public Color albedo = Color.white;

			[Tooltip("The color of rain clouds.")]
			public Color precipitationAlbedo = Color.white;

			[Tooltip("The eccentricity of the lighting. A value of 0 will light the clouds based solely on the phase function. A value of 1 will ensure the energy value for each pixel is at least that of the light.")]
			[Range(0f, 1f)]
			public float eccentricity = 0.5f;

			[Tooltip("The intensity of the silver lining effect around the light source.")]
			[Range(0f, 4f)]
			public float silverIntensity = 1f;

			[Tooltip("The spread of the silver lining effect.")]
			[Range(0f, 4f)]
			public float silverSpread = 0.2f;

			[Tooltip("A multiplier for the direct lighting of the clouds.")]
			[Range(0f, 4f)]
			public float direct = 1f;

			[Tooltip("Increasing this value will increase the (direct) lighting absorption of the clouds.")]
			[Range(0f, 1f)]
			public float directAbsorption = 1f;

			[Tooltip("A multiplier for the indirect lighting of the clouds.")]
			[Range(0f, 4f)]
			public float indirect = 1f;

			[Tooltip("Increasing this value will increase the (indirect) lighting absorption of the clouds.")]
			[Range(0f, 1f)]
			public float indirectAbsorption = 1f;

			[Tooltip("Interpolate between a softer density sample for the indirect lighting.")]
			[Range(0f, 1f)]
			public float indirectSoftness = 0.5f;

			[Tooltip("A multiplier for the ambient lighting of the clouds.")]
			[Range(0f, 4f)]
			public float ambient = 1f;

			[Tooltip("Increasing this value will increase the (ambient) lighting absorption of the clouds.")]
			[Range(0f, 4f)]
			public float ambientAbsorption = 1f;

			[Tooltip("Can be used to desaturate the ambient lighting contribution. Mostly used if the clouds appear too blue.")]
			[Range(0f, 1f)]
			public float ambientDesaturation = 0.5f;

			[Tooltip("The width of the \"sugared powder\" effect (darkened edges when facing away from the light source).")]
			[Range(0f, 1f)]
			public float powderSize = 0.2f;

			[Tooltip("The intensity of the \"sugared powder\" effect (darkened edges when facing away from the light source).")]
			[Range(0f, 1f)]
			public float powderIntensity = 0.4f;

			private ColorParameter _OC_CloudAlbedo;

			private ColorParameter _OC_CloudPrecipitationAlbedo;

			private Vector4Parameter _OC_CloudParams1;

			private Vector4Parameter _OC_CloudParams2;

			private Vector4Parameter _OC_CloudParams3;

			public CloudLighting()
			{
				_OC_CloudAlbedo = new ColorParameter("_OC_CloudAlbedo");
				_OC_CloudPrecipitationAlbedo = new ColorParameter("_OC_CloudPrecipitationAlbedo");
				_OC_CloudParams1 = new Vector4Parameter("_OC_CloudParams1");
				_OC_CloudParams2 = new Vector4Parameter("_OC_CloudParams2");
				_OC_CloudParams3 = new Vector4Parameter("_OC_CloudParams3");
			}

			public void UpdateShaderProperties()
			{
				_OC_CloudAlbedo.value = albedo;
				_OC_CloudPrecipitationAlbedo.value = precipitationAlbedo;
				_OC_CloudParams1.value = new Vector4(eccentricity, silverIntensity, silverSpread, direct);
				_OC_CloudParams2.value = new Vector4(indirect, ambient, Mathf.Pow(directAbsorption, 8f), Mathf.Pow(indirectAbsorption, 8f));
				_OC_CloudParams3.value = new Vector4(indirectSoftness, Mathf.Pow(ambientAbsorption, 8f), powderSize, powderIntensity);
			}
		}

		[Serializable]
		public class Ambient
		{
			[Tooltip("The sky color of the ambient gradient over time (OverCloud samples these color gradients based on the elevation of the sun, not the hour of the day).")]
			public Gradient sky;

			[Tooltip("The equator color of the ambient gradient over time.")]
			public Gradient equator;

			[Tooltip("The equator color of the ambient gradient over time.")]
			public Gradient ground;

			[Tooltip("How much the color of the moon influences the ambient lighting during a lunar eclipse.")]
			[Range(0f, 1f)]
			public float lunarEclipseLightingInfluence = 0.2f;

			[Tooltip("The intensity of the ambient lighting.")]
			[Range(0f, 4f)]
			public float multiplier = 1.75f;
		}

		[Serializable]
		public class CloudShadows
		{
			[Tooltip("Whether to render cloud shadows or not. If unchecked, cloud shadows will not appear, no matter the intensity.")]
			public bool enabled = true;

			[Tooltip("Whether to automatically inject cloud shadows in the screenspace shadows mask or not. For deferred rendering, it is better to swap out the deferred shader. Please see the documentation for info on how to do this.")]
			public CloudShadowsMode mode;

			[Tooltip("The resolution of the cloud shadows buffer.")]
			public ShadowsResolution resolution = ShadowsResolution._512x512;

			[Tooltip("The relative size of the compositor covered by the cloud shadows. This value is rounded to a value which will maintain the texel ratio between the cloud shadows and the compostior texture to prevent shadows from shimmering when the camera moves.")]
			[Range(0f, 1f)]
			public float coverage = 0.25f;

			[Tooltip("How much blur to apply to the volumetric cloud shadows.")]
			[Range(0f, 1f)]
			public float blur = 0.25f;

			[Tooltip("A tiling texture which is used to refine the edges of the cloud shadows, making them appear higher-resolution.")]
			public Texture2D edgeTexture;

			[Tooltip("The tile factor of the cloud shadows edge texture.")]
			[Range(0f, 1f)]
			public float edgeTextureScale = 0.35f;

			[Tooltip("The intensity of the cloud shadows edge refinement.")]
			[Range(0f, 1f)]
			public float edgeTextureIntensity = 0.5f;

			[Tooltip("A sharpen factor applied after blurring and refining the cloud shadows.")]
			[Range(0f, 1f)]
			public float sharpen;
		}

		[Serializable]
		public class CloudAmbientOcclusion
		{
			[Tooltip("The intensity of the cloud ambient occlusion effect.")]
			[Range(0f, 8f)]
			public float intensity = 1.5f;

			[Tooltip("How far down below the cloud layer the cloud ambient occlusion will extend before fading out completely.")]
			public float heightFalloff = 5000f;
		}

		[SerializeField]
		private CloudLighting m_CloudLighting;

		[SerializeField]
		private Ambient m_Ambient = new Ambient();

		[SerializeField]
		private CloudShadows m_CloudShadows = new CloudShadows();

		[SerializeField]
		private CloudAmbientOcclusion m_CloudAmbientOcclusion = new CloudAmbientOcclusion();

		public CloudLighting cloudLighting => m_CloudLighting;

		public Ambient ambient => m_Ambient;

		public CloudShadows cloudShadows => m_CloudShadows;

		public CloudAmbientOcclusion cloudAmbientOcclusion => m_CloudAmbientOcclusion;
	}

	[Serializable]
	public class Weather
	{
		[Serializable]
		public class Rain
		{
			[Tooltip("The resolution of the world-space mask volume.")]
			public RainMaskResolution maskResolution = RainMaskResolution._1024x1024;

			[Tooltip("A layermask specifying which objects should be rendered into the rain mask.")]
			public LayerMask maskLayers = 1;

			[Tooltip("The world-space radius of the rain mask coverage.")]
			public float maskRadius = 20f;

			[Tooltip("The height falloff of the rain mask. A higher value will give a smoother fade, but will increase the height of when objects start to occlude surfaces beneath them.")]
			public float maskFalloff = 1f;

			[Tooltip("Apply an optional blur to the rain mask. If set to 0, will skip the blur pass, which is slightly faster.")]
			[Range(0f, 4f)]
			public float maskBlur = 0.25f;

			[Tooltip("A textured used to add some local noise to the rain mask sampling.")]
			public Texture maskOffsetTexture;

			[Tooltip("The amount of noise to apply to the rain mask sampling.")]
			[Range(0f, 2f)]
			public float maskOffset = 10f;

			[Tooltip("(Deferred rendering only) How much to darken wet surfaces.")]
			[Range(0f, 1f)]
			public float albedoDarken = 0.35f;

			[Tooltip("(Deferred rendering only) How much to decrease the roughness of wet surfaces.")]
			[Range(0f, 1f)]
			public float roughnessDecrease = 0.75f;

			[Tooltip("The texture used to drive the rain ripples effect. Should probably never be changed.")]
			public Texture rippleTexture;

			[Tooltip("The texture used to drive the vertical rain flow effect.")]
			public Texture flowTexture;

			[Tooltip("(Deferred rendering only) The intensity of the rain ripple effect.")]
			[Range(0f, 1f)]
			public float rippleIntensity = 1f;

			[Tooltip("(Deferred rendering only) The scale of the rain ripple effect.")]
			[Range(0f, 1f)]
			public float rippleScale = 1f;

			[Tooltip("(Deferred rendering only) The timescale of the rain ripple effect.")]
			[Range(0f, 1f)]
			public float rippleTimescale = 0.3f;

			[Tooltip("(Deferred rendering only) The intensity of the rain flow effect.")]
			[Range(0f, 1f)]
			public float flowIntensity = 1f;

			[Tooltip("(Deferred rendering only) The scale of the rain flow effect.")]
			[Range(0f, 1f)]
			public float flowScale = 1f;

			[Tooltip("(Deferred rendering only) The timescale of the rain ripple effect.")]
			[Range(0f, 1f)]
			public float flowTimescale = 0.4f;
		}

		[Serializable]
		public class LightningSettings
		{
			[Tooltip("The lightning effect GameObject. This is re-enabled when a lightning strike occurs, so your script should use OnEnable as a play function.")]
			public GameObject gameObject;

			[Tooltip("The minimum distance at which lightning strikes will appear from the camera.")]
			public float distanceMin = 1000f;

			[Tooltip("The maximum distance at which lightning strikes will appear from the camera.")]
			public float distanceMax = 10000f;

			[Tooltip("Bias lightning strikes towards being in front of the camera. 0 = No bias. 1 = Always right in front.")]
			[Range(0f, 1f)]
			public float cameraBias = 0.75f;

			[Tooltip("Lightning strikes will only occur where the cloud density is higher than this value.")]
			[Range(0f, 1f)]
			public float minimumDensity = 0.75f;

			[Tooltip("The minimum amount of time between lightning strikes.")]
			public float intervalMin = 4f;

			[Tooltip("The maximum amount of time between lightning strikes.")]
			public float intervalMax = 20f;

			[Tooltip("The odds that another strike will occur right after another.")]
			[Range(0f, 1f)]
			public float restrikeChance = 0.15f;

			[Tooltip("Enable lightning effects in the editor (your lightning script also needs to support playing in the editor).")]
			public bool enableInEditor = true;
		}

		public float windTime;

		[Tooltip("The timescale for the wind. Should probably be left at 1 unless you want the appearance of time moving at an increased rate.")]
		[Range(0f, 100f)]
		public float windTimescale = 1f;

		[SerializeField]
		private Rain m_Rain = new Rain();

		[SerializeField]
		private LightningSettings m_Lightning = new LightningSettings();

		public Rain rain => m_Rain;

		public LightningSettings lightning => m_Lightning;
	}

	[Serializable]
	public class TimeOfDay
	{
		[Tooltip("When checked, will override the sun and moon positions with the ones calculated from the current latitude, longitude, date and time.")]
		public bool enable;

		[Tooltip("When checked, the moon will be positioned in the sky according to its physical position at that time. Uncheck if you'd like to have a fixed moon in the sky.")]
		public bool affectsMoon = true;

		[Tooltip("When checked, will override the date and time with that of the local computer.")]
		public bool useLocalTime;

		[Tooltip("Whether to move time forwards automatically or not. Date will be moved forwards automatically when time goes back down to 0.")]
		public bool play = true;

		[Tooltip("Enable the Play feature in the editor.")]
		public bool playInEditor;

		[Tooltip("The latitude coordinate of the camera.")]
		[Range(-90f, 90f)]
		public float latitude;

		[Tooltip("The longitude coordinate of the camera.")]
		[Range(-180f, 180f)]
		public float longitude;

		[Tooltip("The year.")]
		public int year = 1992;

		[Tooltip("The month.")]
		[Range(1f, 12f)]
		public int month = 1;

		[Tooltip("The day.")]
		public int day = 8;

		[Tooltip("The time (in hours, meaning a value of 0.5 is equal to 30 minutes, etc).")]
		[Range(0f, 24f)]
		public double time = 12.0;

		[Tooltip("The speed at which the time of day moves when Play is enabled. A value of 1 is realtime.")]
		public float playSpeed = 10f;

		public float dayNumber => (float)((double)(float)(367 * year - 7 * (year + (month + 9) / 12) / 4 + 275 * month / 9 + day - 730530) + time / 24.0);

		public int daysInMonth => DateTime.DaysInMonth(year, month);

		public int hour => Mathf.FloorToInt((float)time);

		public int minute => (int)(time * 1440.0) % 60;

		public int second => (int)(time * 86400.0) % 60;

		public void Advance()
		{
			time += (double)Time.deltaTime * 1.1574074074074073E-05 * (double)playSpeed;
			if (time > 24.0)
			{
				day++;
				time -= 24.0;
			}
			if (day > daysInMonth)
			{
				month++;
				day = 0;
			}
			if (month > 12)
			{
				year++;
				month = 1;
			}
		}
	}

	private static Mesh s_Quad;

	public bool showDrawerCloud;

	public bool showDrawerNoiseGenerator;

	public bool showDrawerCirrus;

	public bool showDrawerAtmosphere;

	public bool showDrawerTimeOfDay;

	public bool showDrawerCloudCompositorTexture;

	public bool showDrawerPhase;

	public bool showDrawer3DNoise;

	public bool showDrawerFog;

	public bool showDrawerScatteringMask;

	public bool showDrawerRendering;

	public bool showDrawerWeather;

	public bool showDrawerWeatherPresets;

	public bool showDrawerCustomFloats;

	public bool showDrawerLighting;

	public int drawerSelectedCustomFloat;

	public int drawerSelectedCloudPlane;

	public static Vector3 currentOriginOffset;

	public static RenderTexture cloudRT = null;

	public static RenderTexture cloudDepthRT = null;

	public static RenderTexture scatteringMaskRT = null;

	public static RenderTexture volumeRT = null;

	[SerializeField]
	private Components m_Components;

	[SerializeField]
	private VolumetricClouds m_VolumetricClouds;

	[SerializeField]
	private CloudPlane[] m_CloudPlanes;

	[SerializeField]
	private Atmosphere m_Atmosphere;

	[SerializeField]
	private Lighting m_Lighting;

	[SerializeField]
	private Weather m_Weather;

	[SerializeField]
	private TimeOfDay m_TimeOfDay;

	[Tooltip("The name of the current active weather preset.")]
	public string activePreset = "None";

	[Tooltip("The time in seconds for the weather to fully fade to a new preset.")]
	public float fadeDuration = 10f;

	[Tooltip("Same as above, but when the game is not running.")]
	public float editorFadeDuration = 10f;

	[SerializeField]
	private CustomFloat[] m_CustomFloats;

	[SerializeField]
	private WeatherPreset[] m_Presets;

	[SerializeField]
	private WeatherPreset m_CurrentPreset;

	private AtmosphereModel m_AtmosphereModel;

	private OverCloudLight m_OverCloudSun;

	private OverCloudLight m_OverCloudMoon;

	private GameObject m_CloudObject;

	private GameObject m_LodObject;

	private MeshFilter m_Filter;

	private MeshFilter m_LodFilter;

	private MeshRenderer m_Renderer;

	private MeshRenderer m_LodRenderer;

	private MaterialPropertyBlock m_PropBlock;

	private MaterialPropertyBlock m_LodPropBlock;

	private Dictionary<Camera, CommandBuffer> m_CameraBuffers = new Dictionary<Camera, CommandBuffer>();

	private Dictionary<Camera, CommandBuffer> m_CameraPreBuffers = new Dictionary<Camera, CommandBuffer>();

	private Dictionary<Camera, CommandBuffer> m_CameraPostBuffers = new Dictionary<Camera, CommandBuffer>();

	private Dictionary<Camera, CommandBuffer> m_VolumeBuffers = new Dictionary<Camera, CommandBuffer>();

	private Dictionary<Camera, CommandBuffer> m_OcclusionBuffers = new Dictionary<Camera, CommandBuffer>();

	private Dictionary<Camera, CommandBuffer> m_WetnessBuffers = new Dictionary<Camera, CommandBuffer>();

	private Dictionary<Light, CommandBuffer> m_ShadowBuffers = new Dictionary<Light, CommandBuffer>();

	private Camera m_RainCamera;

	private Dictionary<Camera, RenderTexture> m_DownsampledDepthRTs;

	private Dictionary<Camera, RenderTexture> m_CloudRTs;

	private Dictionary<Camera, RenderTexture> m_CloudDepthRTs;

	private Dictionary<Camera, RenderTexture> m_ScatteringMasks;

	private Dictionary<Camera, RenderTexture> m_VolumeRTs;

	private RenderTexture m_CompositorRT;

	private RenderTexture m_CloudShadowsRT;

	private RenderTexture m_RainMask;

	private RenderTexture m_RainRippleRT;

	private Texture3D m_3DNoise;

	private RenderTexture[] m_3DNoiseSlice = new RenderTexture[3];

	private Material m_CompositorMat;

	private Material m_UtilitiesMat;

	private Material m_DownsampleDepthMat;

	private Material m_UpsampleMat;

	private Material m_ClearMat;

	private Material m_ScatteringMaskRTMat;

	private Material m_AtmosphereMat;

	private Material m_SeparableBlurMat;

	private Material m_RainRippleMat;

	private CompositorResolution m_LastCompositorRes;

	private WeatherPreset m_PrevPreset;

	private WeatherPreset m_TargetPreset;

	private WeatherPreset m_LastFramePreset;

	private TimeOfDay m_LastFrameTimeOfDay;

	private Atmosphere.Precomputation m_LastAtmosphere;

	private Vector3 m_LastPos = new Vector3(0f, -99999f, 0f);

	private Vector3 m_LastLodPos = new Vector3(0f, -99999f, 0f);

	private Rect m_WorldExtents;

	private float m_FadeTimer;

	private float m_LastRadius;

	private float m_LastLodMultiplier;

	private float m_LightningTimer;

	private bool m_LightningRestrike;

	private float m_LST;

	private readonly RenderTargetIdentifier[] m_OcclusionMRT = new RenderTargetIdentifier[2]
	{
		BuiltinRenderTextureType.GBuffer0,
		BuiltinRenderTextureType.CameraTarget
	};

	private readonly RenderTargetIdentifier[] m_WetnessMRT = new RenderTargetIdentifier[3]
	{
		BuiltinRenderTextureType.GBuffer0,
		BuiltinRenderTextureType.GBuffer1,
		BuiltinRenderTextureType.CameraTarget
	};

	private static TextureParameter _OC_NoiseTex = new TextureParameter("_OC_NoiseTex");

	private static Vector2Parameter _OC_NoiseScale = new Vector2Parameter("_OC_NoiseScale");

	private static FloatParameter _OC_Timescale = new FloatParameter("_OC_Timescale");

	private static TextureParameter _OC_3DNoiseTex = new TextureParameter("_OC_3DNoiseTex");

	private static Vector4Parameter _OC_NoiseParams1 = new Vector4Parameter("_OC_NoiseParams1");

	private static Vector2Parameter _OC_NoiseParams2 = new Vector2Parameter("_OC_NoiseParams2");

	private static FloatParameter _OC_Precipitation = new FloatParameter("_OC_Precipitation");

	private static Vector2Parameter _OC_CloudOcclusionParams = new Vector2Parameter("_OC_CloudOcclusionParams");

	private static Vector4Parameter _OC_ShapeParams = new Vector4Parameter("_OC_ShapeParams");

	private static FloatParameter _OC_NoiseErosion = new FloatParameter("_OC_NoiseErosion");

	private static Vector2Parameter _OC_AlphaEdgeParams = new Vector2Parameter("_OC_AlphaEdgeParams");

	private static FloatParameter _OC_CloudAltitude = new FloatParameter("_OC_CloudAltitude");

	private static FloatParameter _OC_CloudPlaneRadius = new FloatParameter("_OC_CloudPlaneRadius");

	private static FloatParameter _OC_CloudHeight = new FloatParameter("_OC_CloudHeight");

	private static FloatParameter _OC_CloudHeightInv = new FloatParameter("_OC_CloudHeightInv");

	private static FloatParameter _OC_NightScattering = new FloatParameter("_OC_NightScattering");

	private static Vector4Parameter _OC_MieScatteringParams = new Vector4Parameter("_OC_MieScatteringParams");

	private static FloatParameter _SkySunSize = new FloatParameter("_SkySunSize");

	private static FloatParameter _SkyMoonSize = new FloatParameter("_SkyMoonSize");

	private static FloatParameter _SkySunIntensity = new FloatParameter("_SkySunIntensity");

	private static FloatParameter _SkyMoonIntensity = new FloatParameter("_SkyMoonIntensity");

	private static TextureParameter _SkyMoonCubemap = new TextureParameter("_SkyMoonCubemap");

	private static TextureParameter _SkyStarsCubemap = new TextureParameter("_SkyStarsCubemap");

	private static FloatParameter _SkyStarsIntensity = new FloatParameter("_SkyStarsIntensity");

	private static Vector4Parameter _SkySolarEclipse = new Vector4Parameter("_SkySolarEclipse");

	private static Vector4Parameter _SkyLunarEclipse = new Vector4Parameter("_SkyLunarEclipse");

	private static FloatParameter _LunarEclipseLightingInfluence = new FloatParameter("_LunarEclipseLightingInfluence");

	private static FloatParameter _OC_GlobalWindMultiplier = new FloatParameter("_OC_GlobalWindMultiplier");

	private static Vector4Parameter _OC_GlobalWetnessParams = new Vector4Parameter("_OC_GlobalWetnessParams");

	private static Vector4Parameter _OC_GlobalRainParams = new Vector4Parameter("_OC_GlobalRainParams");

	private static Vector2Parameter _OC_GlobalRainParams2 = new Vector2Parameter("_OC_GlobalRainParams2");

	private static Vector4Parameter _OC_Cloudiness = new Vector4Parameter("_OC_Cloudiness");

	private static Vector2Parameter _OC_CloudSharpness = new Vector2Parameter("_OC_CloudSharpness");

	private static Vector2Parameter _OC_CloudDensity = new Vector2Parameter("_OC_CloudDensity");

	private static Vector2Parameter _OC_CloudShadowsParams = new Vector2Parameter("_OC_CloudShadowsParams");

	private static FloatParameter _OC_CloudShadowsSharpen = new FloatParameter("_OC_CloudShadowsSharpen");

	private static TextureParameter _OC_CloudShadowsEdgeTex = new TextureParameter("_OC_CloudShadowsEdgeTex");

	private static Vector4Parameter _OC_CloudShadowsEdgeTexParams = new Vector4Parameter("_OC_CloudShadowsEdgeTexParams");

	private static FloatParameter _OC_ScatteringMaskSoftness = new FloatParameter("_OC_ScatteringMaskSoftness");

	private static FloatParameter _OC_ScatteringMaskFloor = new FloatParameter("_OC_ScatteringMaskFloor");

	private static Vector4Parameter _OC_FogParams = new Vector4Parameter("_OC_FogParams");

	private static FloatParameter _OC_FogBlend = new FloatParameter("_OC_FogBlend");

	private static ColorParameter _OC_FogColor = new ColorParameter("_OC_FogColor");

	private static FloatParameter _OC_FogHeight = new FloatParameter("_OC_FogHeight");

	private static Vector2Parameter _OC_FogFalloffParams = new Vector2Parameter("_OC_FogFalloffParams");

	private static FloatParameter _OC_AtmosphereExposure = new FloatParameter("_OC_AtmosphereExposure");

	private static FloatParameter _OC_AtmosphereDensity = new FloatParameter("_OC_AtmosphereDensity");

	private static FloatParameter _OC_AtmosphereFarClipFade = new FloatParameter("_OC_AtmosphereFarClipFade");

	private static Vector3Parameter _OC_CurrentSunColor = new Vector3Parameter("_OC_CurrentSunColor");

	private static Vector3Parameter _OC_CurrentMoonColor = new Vector3Parameter("_OC_CurrentMoonColor");

	private static Vector3Parameter _OC_LightDir = new Vector3Parameter("_OC_LightDir");

	private static FloatParameter _OC_LightDirYInv = new FloatParameter("_OC_LightDirYInv");

	private static Vector3Parameter _OC_LightColor = new Vector3Parameter("_OC_LightColor");

	private static Vector3Parameter _OC_ActualSunDir = new Vector3Parameter("_OC_ActualSunDir");

	private static ColorParameter _OC_ActualSunColor = new ColorParameter("_OC_ActualSunColor");

	private static Vector3Parameter _OC_ActualMoonDir = new Vector3Parameter("_OC_ActualMoonDir");

	private static ColorParameter _OC_ActualMoonColor = new ColorParameter("_OC_ActualMoonColor");

	private ColorParameter _OC_EarthColor = new ColorParameter("_OC_EarthColor");

	private int t0;

	private int t1;

	private int t2;

	private int t3;

	private int t4;

	private int t5;

	private Vector3[] vertices;

	private int[] triangles;

	private float[] distances;

	public static Mesh quad
	{
		get
		{
			if (s_Quad != null)
			{
				return s_Quad;
			}
			Vector3[] array = new Vector3[4]
			{
				new Vector3(-1f, -1f, 0f),
				new Vector3(1f, 1f, 0f),
				new Vector3(1f, -1f, 0f),
				new Vector3(-1f, 1f, 0f)
			};
			Vector2[] uv = new Vector2[4]
			{
				new Vector2(0f, 0f),
				new Vector2(1f, 1f),
				new Vector2(1f, 0f),
				new Vector2(0f, 1f)
			};
			int[] array2 = new int[6] { 0, 1, 2, 1, 0, 3 };
			s_Quad = new Mesh
			{
				vertices = array,
				uv = uv,
				triangles = array2
			};
			s_Quad.RecalculateNormals();
			s_Quad.RecalculateBounds();
			return s_Quad;
		}
	}

	public static OverCloud instance { get; private set; }

	public static Light dominantLight { get; private set; }

	public static OverCloudLight dominantOverCloudLight { get; private set; }

	public static int bufferWidth { get; private set; }

	public static int bufferHeight { get; private set; }

	public static int bufferWidthDS { get; private set; }

	public static int bufferHeightDS { get; private set; }

	public static float solarEclipse
	{
		get
		{
			if ((bool)instance && (bool)components.sun && (bool)components.moon)
			{
				float num = 0.0002f;
				return Mathf.Clamp01((Vector3.Dot(components.sun.transform.forward, components.moon.transform.forward) * 0.5f + 0.5f - (1f - num)) / num);
			}
			return 0f;
		}
	}

	public static float lunarEclipse
	{
		get
		{
			if ((bool)instance && (bool)components.sun && (bool)components.moon)
			{
				float num = 0.0002f;
				return Mathf.Clamp01((Vector3.Dot(-components.sun.transform.forward, components.moon.transform.forward) * 0.5f + 0.5f - (1f - num)) / num);
			}
			return 0f;
		}
	}

	public static float moonFade { get; private set; }

	public static RenderTexture compositorTexture => instance.m_CompositorRT;

	public RenderTexture[] _3DNoiseSlice => m_3DNoiseSlice;

	public static WeatherPreset current
	{
		get
		{
			if (!instance)
			{
				return null;
			}
			return instance.m_CurrentPreset;
		}
	}

	public static bool skyChanged { get; private set; }

	public static float adjustedCloudPlaneAltitude => ((current != null) ? current.cloudPlaneAltitude : 0f) - currentOriginOffset.y;

	public static Components components => instance.m_Components;

	public static VolumetricClouds volumetricClouds => instance.m_VolumetricClouds;

	public static CloudPlane[] cloudPlanes => instance.m_CloudPlanes;

	public static Atmosphere atmosphere => instance.m_Atmosphere;

	public static Lighting lighting => instance.m_Lighting;

	public static Weather weather => instance.m_Weather;

	public static TimeOfDay timeOfDay => instance.m_TimeOfDay;

	public int customFloatsCount => m_CustomFloats.Length;

	public static event OverCloudEventHandler beforeCameraUpdate;

	public static event OverCloudEventHandler afterCameraUpdate;

	public static event OverCloudEventHandler beforeRender;

	public static event OverCloudEventHandler afterRender;

	public static event OverCloudEventHandler beforeShaderParametersUpdate;

	public static event OverCloudEventHandler afterShaderParametersUpdate;

	private void Awake()
	{
		if ((bool)instance && instance != this)
		{
			Debug.LogError("Multiple OverCloud instances found");
		}
		instance = this;
		m_CameraBuffers = new Dictionary<Camera, CommandBuffer>();
		m_CameraPreBuffers = new Dictionary<Camera, CommandBuffer>();
		m_CameraPostBuffers = new Dictionary<Camera, CommandBuffer>();
		m_OcclusionBuffers = new Dictionary<Camera, CommandBuffer>();
		m_WetnessBuffers = new Dictionary<Camera, CommandBuffer>();
		m_VolumeBuffers = new Dictionary<Camera, CommandBuffer>();
		m_WorldExtents = new Rect(new Vector2(0f, 0f), Vector2.one * volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier * 2f);
		InitializeAtmosphere();
	}

	private void TryLoadShader(string name, out Material material)
	{
		material = new Material(Shader.Find(name));
		if (!material)
		{
			Debug.LogError("Unable to load shader " + name + ", (file accidentally deleted?).");
		}
	}

	private void OnEnable()
	{
		if ((bool)instance && instance != this)
		{
			Debug.LogError("Multiple OverCloud instances found");
		}
		instance = this;
		if (m_AtmosphereModel == null || !m_AtmosphereModel.initialized)
		{
			InitializeAtmosphere();
		}
		TryLoadShader("Hidden/OverCloud/Compositor", out m_CompositorMat);
		TryLoadShader("Hidden/OverCloud/Utilities", out m_UtilitiesMat);
		TryLoadShader("Hidden/OverCloud/DownsampleDepth", out m_DownsampleDepthMat);
		TryLoadShader("Hidden/OverCloud/DepthUpsampling", out m_UpsampleMat);
		TryLoadShader("Hidden/OverCloud/ScatteringMask", out m_ScatteringMaskRTMat);
		TryLoadShader("Hidden/OverCloud/Clear", out m_ClearMat);
		TryLoadShader("Hidden/OverCloud/Atmosphere", out m_AtmosphereMat);
		TryLoadShader("Hidden/SeparableBlur", out m_SeparableBlurMat);
		UpdateShaderProperties();
		CheckComponents();
		InitializeMeshes();
		if (m_3DNoise == null)
		{
			InitializeNoise();
		}
		if (components.cloudMaterial != null)
		{
			m_Renderer.sharedMaterial = components.cloudMaterial;
			m_LodRenderer.sharedMaterial = components.cloudMaterial;
		}
		m_Renderer.enabled = true;
		m_LodRenderer.enabled = true;
		InitializeCompositor();
		InitializeWeather();
		if (m_DownsampledDepthRTs == null)
		{
			m_DownsampledDepthRTs = new Dictionary<Camera, RenderTexture>();
		}
		if (m_CloudRTs == null)
		{
			m_CloudRTs = new Dictionary<Camera, RenderTexture>();
		}
		if (m_CloudDepthRTs == null)
		{
			m_CloudDepthRTs = new Dictionary<Camera, RenderTexture>();
		}
		if (m_ScatteringMasks == null)
		{
			m_ScatteringMasks = new Dictionary<Camera, RenderTexture>();
		}
		if (m_VolumeRTs == null)
		{
			m_VolumeRTs = new Dictionary<Camera, RenderTexture>();
		}
		FindTargetPreset();
		m_CurrentPreset = new WeatherPreset(m_TargetPreset);
		m_PrevPreset = new WeatherPreset(m_CurrentPreset);
		weather.windTime = 0f;
		if (!Application.isPlaying)
		{
			ResetOrigin();
		}
	}

	private void OnDisable()
	{
		if ((bool)m_Renderer)
		{
			m_Renderer.enabled = false;
		}
		if ((bool)m_LodRenderer)
		{
			m_LodRenderer.enabled = false;
		}
		foreach (KeyValuePair<Camera, CommandBuffer> cameraBuffer in m_CameraBuffers)
		{
			if ((bool)cameraBuffer.Key)
			{
				cameraBuffer.Key.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, cameraBuffer.Value);
			}
		}
		foreach (KeyValuePair<Camera, CommandBuffer> cameraPreBuffer in m_CameraPreBuffers)
		{
			if ((bool)cameraPreBuffer.Key)
			{
				cameraPreBuffer.Key.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, cameraPreBuffer.Value);
			}
		}
		foreach (KeyValuePair<Camera, CommandBuffer> cameraPreBuffer2 in m_CameraPreBuffers)
		{
			if ((bool)cameraPreBuffer2.Key)
			{
				cameraPreBuffer2.Key.RemoveCommandBuffer(CameraEvent.AfterLighting, cameraPreBuffer2.Value);
			}
		}
		foreach (KeyValuePair<Camera, CommandBuffer> cameraPostBuffer in m_CameraPostBuffers)
		{
			if ((bool)cameraPostBuffer.Key)
			{
				cameraPostBuffer.Key.RemoveCommandBuffer(CameraEvent.AfterEverything, cameraPostBuffer.Value);
			}
		}
		foreach (KeyValuePair<Camera, CommandBuffer> volumeBuffer in m_VolumeBuffers)
		{
			if ((bool)volumeBuffer.Key)
			{
				volumeBuffer.Key.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, volumeBuffer.Value);
			}
		}
		foreach (KeyValuePair<Camera, CommandBuffer> occlusionBuffer in m_OcclusionBuffers)
		{
			if ((bool)occlusionBuffer.Key)
			{
				occlusionBuffer.Key.RemoveCommandBuffer(CameraEvent.BeforeReflections, occlusionBuffer.Value);
			}
		}
		foreach (KeyValuePair<Camera, CommandBuffer> wetnessBuffer in m_WetnessBuffers)
		{
			if ((bool)wetnessBuffer.Key)
			{
				wetnessBuffer.Key.RemoveCommandBuffer(CameraEvent.BeforeReflections, wetnessBuffer.Value);
			}
		}
		foreach (KeyValuePair<Light, CommandBuffer> shadowBuffer in m_ShadowBuffers)
		{
			if ((bool)shadowBuffer.Key)
			{
				shadowBuffer.Key.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, shadowBuffer.Value);
			}
		}
		m_CameraBuffers.Clear();
		m_CameraPreBuffers.Clear();
		m_CameraPostBuffers.Clear();
		m_VolumeBuffers.Clear();
		m_OcclusionBuffers.Clear();
		m_WetnessBuffers.Clear();
		m_ShadowBuffers.Clear();
	}

	private void FindTargetPreset()
	{
		m_TargetPreset = null;
		WeatherPreset[] presets = m_Presets;
		foreach (WeatherPreset weatherPreset in presets)
		{
			if (weatherPreset.name == activePreset)
			{
				m_TargetPreset = weatherPreset;
				break;
			}
		}
	}

	private void InitializeCompositor()
	{
		RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor((int)volumetricClouds.compositorResolution, (int)volumetricClouds.compositorResolution, RenderTextureFormat.ARGB32, 0);
		renderTextureDescriptor.useMipMap = true;
		renderTextureDescriptor.autoGenerateMips = true;
		renderTextureDescriptor.sRGB = false;
		m_CompositorRT = new RenderTexture(renderTextureDescriptor.width, renderTextureDescriptor.height, renderTextureDescriptor.depthBufferBits, renderTextureDescriptor.colorFormat, RenderTextureReadWrite.Linear);
		m_CompositorRT.filterMode = FilterMode.Bilinear;
		m_LastCompositorRes = volumetricClouds.compositorResolution;
	}

	private void InitializeAtmosphere()
	{
		if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
		{
			if (m_AtmosphereModel == null)
			{
				m_AtmosphereModel = new AtmosphereModel();
			}
			m_AtmosphereModel.m_compute = atmosphere.precomputation.shader;
			m_AtmosphereModel.planetScale = atmosphere.precomputation.planetScale;
			m_AtmosphereModel.heightScale = atmosphere.precomputation.heightScale;
			m_AtmosphereModel.Initialize(atmosphere.precomputation);
		}
	}

	private void InitializeWeather()
	{
		RenderTextureDescriptor desc = new RenderTextureDescriptor(512, 512, RenderTextureFormat.ARGB32, 0);
		desc.autoGenerateMips = true;
		desc.useMipMap = true;
		m_RainRippleRT = new RenderTexture(desc);
		m_RainRippleRT.wrapMode = TextureWrapMode.Repeat;
		m_RainRippleRT.filterMode = FilterMode.Bilinear;
		TryLoadShader("Hidden/OverCloud/RippleNormals", out m_RainRippleMat);
		m_LightningTimer = UnityEngine.Random.Range(weather.lightning.intervalMin, weather.lightning.intervalMax);
		if (!m_RainCamera)
		{
			GameObject gameObject = new GameObject("Rain Camera");
			gameObject.hideFlags = HideFlags.HideAndDontSave;
			m_RainCamera = gameObject.AddComponent<Camera>();
			m_RainCamera.enabled = false;
		}
	}

	public static void MoveOrigin(Vector3 offset)
	{
		currentOriginOffset += offset;
		instance.m_LastPos += offset;
		instance.m_LastLodPos += offset;
		Shader.SetGlobalVector("_OverCloudOriginOffset", currentOriginOffset);
	}

	public static void ResetOrigin()
	{
		if ((bool)instance)
		{
			instance.m_LastPos -= currentOriginOffset;
			instance.m_LastLodPos -= currentOriginOffset;
		}
		currentOriginOffset = Vector3.zero;
		Shader.SetGlobalVector("_OverCloudOriginOffset", currentOriginOffset);
	}

	private void OnValidate()
	{
		if (!instance)
		{
			return;
		}
		if (volumetricClouds.compositorResolution != m_LastCompositorRes)
		{
			InitializeCompositor();
		}
		timeOfDay.day = Mathf.Clamp(timeOfDay.day, 1, timeOfDay.daysInMonth);
		weather.rain.maskFalloff = Mathf.Max(weather.rain.maskFalloff, 0.01f);
		if (timeOfDay.useLocalTime)
		{
			UpdateTime();
		}
		UpdateOrbital();
		m_LastPos = Vector3.one * 999999f;
		m_LastLodPos = Vector3.one * 999999f;
		UpdateShaderProperties();
		skyChanged = true;
		float alphaEdgeLower = Mathf.Min(volumetricClouds.noiseSettings.alphaEdgeLower, volumetricClouds.noiseSettings.alphaEdgeUpper);
		float alphaEdgeUpper = Mathf.Max(volumetricClouds.noiseSettings.alphaEdgeLower, volumetricClouds.noiseSettings.alphaEdgeUpper);
		volumetricClouds.noiseSettings.alphaEdgeLower = alphaEdgeLower;
		volumetricClouds.noiseSettings.alphaEdgeUpper = alphaEdgeUpper;
		float num = Mathf.Min((float)lighting.cloudShadows.resolution / (float)volumetricClouds.compositorResolution, 1f);
		lighting.cloudShadows.coverage = Mathf.Max(lighting.cloudShadows.coverage, 0.01f);
		if (lighting.cloudShadows.coverage >= num)
		{
			lighting.cloudShadows.coverage = num;
			return;
		}
		float num2 = num;
		float num3 = num2;
		while (num2 > lighting.cloudShadows.coverage)
		{
			num3 = num2;
			num2 *= 0.5f;
		}
		if (num3 - lighting.cloudShadows.coverage < lighting.cloudShadows.coverage - num2)
		{
			lighting.cloudShadows.coverage = num3;
		}
		else
		{
			lighting.cloudShadows.coverage = num2;
		}
	}

	private void CheckComponents()
	{
		bool flag = false;
		if (!m_CloudObject)
		{
			m_CloudObject = (base.transform.Find("CloudObject") ? base.transform.Find("CloudObject").gameObject : null);
			if (!m_CloudObject)
			{
				m_CloudObject = new GameObject("CloudObject");
				m_CloudObject.hideFlags = HideFlags.HideAndDontSave;
				m_CloudObject.transform.SetParent(base.transform);
			}
			flag = true;
		}
		if (!m_Renderer)
		{
			m_Renderer = m_CloudObject.GetComponent<MeshRenderer>();
			if (!m_Renderer)
			{
				m_Renderer = m_CloudObject.AddComponent<MeshRenderer>();
			}
			m_Renderer.sharedMaterial = components.cloudMaterial;
			m_Renderer.shadowCastingMode = ShadowCastingMode.Off;
			flag = true;
		}
		if (!m_Filter)
		{
			m_Filter = m_CloudObject.GetComponent<MeshFilter>();
			if (!m_Filter)
			{
				m_Filter = m_CloudObject.AddComponent<MeshFilter>();
			}
			flag = true;
		}
		if (!m_LodObject)
		{
			m_LodObject = (base.transform.Find("CloudLOD") ? base.transform.Find("CloudLOD").gameObject : null);
			if (!m_LodObject)
			{
				m_LodObject = new GameObject("CloudLOD");
				m_LodObject.hideFlags = HideFlags.HideAndDontSave;
				m_LodObject.transform.SetParent(base.transform);
				m_LodObject.transform.localPosition = Vector3.zero;
				m_LodObject.transform.localRotation = Quaternion.identity;
			}
			flag = true;
		}
		if (!m_LodRenderer)
		{
			m_LodRenderer = m_LodObject.GetComponent<MeshRenderer>();
			if (!m_LodRenderer)
			{
				m_LodRenderer = m_LodObject.AddComponent<MeshRenderer>();
			}
			m_LodRenderer.sharedMaterial = components.cloudMaterial;
			m_LodRenderer.shadowCastingMode = ShadowCastingMode.Off;
			flag = true;
		}
		if (!m_LodFilter)
		{
			m_LodFilter = m_LodObject.GetComponent<MeshFilter>();
			if (!m_LodFilter)
			{
				m_LodFilter = m_LodObject.AddComponent<MeshFilter>();
			}
			flag = true;
		}
		if (flag)
		{
			InitializeMeshes();
		}
		if (!m_OverCloudSun && (bool)components.sun)
		{
			m_OverCloudSun = components.sun.GetComponent<OverCloudLight>();
		}
		if (!m_OverCloudMoon && (bool)components.moon)
		{
			m_OverCloudMoon = components.moon.GetComponent<OverCloudLight>();
		}
	}

	private void InitializeMeshes()
	{
		InitializeMesh(m_Filter, m_Renderer, m_PropBlock);
		InitializeMesh(m_LodFilter, m_LodRenderer, m_LodPropBlock, volumetricClouds.lodRadiusMultiplier);
	}

	private void InitializeMesh(MeshFilter filter, MeshRenderer renderer, MaterialPropertyBlock propBlock, float radiusMultiplier = 1f)
	{
		float num = volumetricClouds.cloudPlaneRadius * radiusMultiplier;
		int num2 = (int)Mathf.Floor(Mathf.Sqrt(volumetricClouds.particleCount));
		float num3 = num * 2f / (float)num2;
		int num4 = num2 * num2;
		int num5 = num4 * 4;
		int num6 = num4 * 2 * 3;
		Vector3[] array = new Vector3[num5];
		int num7 = 0;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				Vector3 zero = Vector3.zero;
				zero.x = (float)i / (float)num2 * num * 2f - num + num3 * 0.5f;
				zero.z = (float)j / (float)num2 * num * 2f - num + num3 * 0.5f;
				array[num7] = zero;
				array[num7 + 1] = zero;
				array[num7 + 2] = zero;
				array[num7 + 3] = zero;
				num7 += 4;
			}
		}
		Vector2[] array2 = new Vector2[num5];
		Vector2[] uv = new Vector2[num5];
		for (num7 = 0; num7 < num4; num7++)
		{
			array2[num7 * 4] = new Vector2(0f, 0f);
			array2[num7 * 4 + 1] = new Vector2(1f, 0f);
			array2[num7 * 4 + 2] = new Vector2(0f, 1f);
			array2[num7 * 4 + 3] = new Vector2(1f, 1f);
		}
		Vector3[] array3 = new Vector3[num5];
		for (num7 = 0; num7 < num4; num7++)
		{
			array3[num7 * 4] = Vector3.up;
			array3[num7 * 4 + 1] = Vector3.up;
			array3[num7 * 4 + 2] = Vector3.up;
			array3[num7 * 4 + 3] = Vector3.up;
		}
		int[] array4 = new int[num6];
		for (num7 = 0; num7 < num4; num7++)
		{
			array4[num7 * 6] = num7 * 4;
			array4[num7 * 6 + 1] = num7 * 4 + 1;
			array4[num7 * 6 + 2] = num7 * 4 + 2;
			array4[num7 * 6 + 3] = num7 * 4 + 1;
			array4[num7 * 6 + 4] = num7 * 4 + 3;
			array4[num7 * 6 + 5] = num7 * 4 + 2;
		}
		Color[] array5 = new Color[num5];
		for (num7 = 0; num7 < num4; num7++)
		{
			Color color = new Color(1f, 1f, 1f, 1f);
			color.a = UnityEngine.Random.Range(0f, 1f);
			array5[num7 * 4] = color;
			array5[num7 * 4 + 1] = color;
			array5[num7 * 4 + 2] = color;
			array5[num7 * 4 + 3] = color;
		}
		Mesh mesh = new Mesh();
		mesh.name = "OverCloud_Mesh";
		mesh.vertices = array;
		mesh.uv = array2;
		mesh.uv2 = uv;
		mesh.normals = array3;
		mesh.triangles = array4;
		mesh.colors = array5;
		mesh.RecalculateBounds();
		Bounds bounds = mesh.bounds;
		Vector3 extents = bounds.extents;
		extents.y = 10000f;
		bounds.extents = extents;
		mesh.bounds = bounds;
		filter.sharedMesh = mesh;
		if (propBlock == null)
		{
			propBlock = new MaterialPropertyBlock();
		}
		renderer.GetPropertyBlock(propBlock);
		propBlock.SetFloat("_RandomRange", num3);
		propBlock.SetFloat("_Radius", num);
		propBlock.SetFloat("_Altitude", adjustedCloudPlaneAltitude);
		renderer.SetPropertyBlock(propBlock);
		filter.sharedMesh = SortTriangles(filter.sharedMesh);
	}

	private void UpdateRaymarchingMatrices(Camera camera)
	{
		bool isValid = InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid;
		if (!(camera.stereoTargetEye != 0 && Application.isPlaying && XRSettings.enabled && isValid) || 2 == 0 || XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
		{
			Matrix4x4 cameraToWorldMatrix = camera.cameraToWorldMatrix;
			Matrix4x4 inverse = GL.GetGPUProjectionMatrix(camera.projectionMatrix, renderIntoTexture: true).inverse;
			inverse[1, 1] *= -1f;
			Shader.SetGlobalMatrix("_WorldFromView", cameraToWorldMatrix);
			Shader.SetGlobalMatrix("_ViewFromScreen", inverse);
			return;
		}
		Matrix4x4 inverse2 = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse;
		Matrix4x4 inverse3 = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;
		Matrix4x4 stereoProjectionMatrix = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
		Matrix4x4 stereoProjectionMatrix2 = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
		Matrix4x4 inverse4 = GL.GetGPUProjectionMatrix(stereoProjectionMatrix, renderIntoTexture: true).inverse;
		Matrix4x4 inverse5 = GL.GetGPUProjectionMatrix(stereoProjectionMatrix2, renderIntoTexture: true).inverse;
		inverse4[1, 1] *= -1f;
		inverse5[1, 1] *= -1f;
		Shader.SetGlobalMatrix("_LeftWorldFromView", inverse2);
		Shader.SetGlobalMatrix("_RightWorldFromView", inverse3);
		Shader.SetGlobalMatrix("_LeftViewFromScreen", inverse4);
		Shader.SetGlobalMatrix("_RightViewFromScreen", inverse5);
	}

	public static void Render(Camera camera, bool renderVolumetricClouds, bool render2DFallback, bool renderAtmosphere, bool renderScatteringMask, bool includeCascadedShadows, bool downsample2DClouds, SampleCount scatteringMaskSamples, bool renderRainMask, DownSampleFactor downsampleFactor, SampleCount lightSampleCount, bool highQualityClouds)
	{
		if ((bool)camera && (bool)instance)
		{
			instance.mRender(camera, renderVolumetricClouds, render2DFallback, renderAtmosphere, renderScatteringMask, includeCascadedShadows, downsample2DClouds, scatteringMaskSamples, renderRainMask, downsampleFactor, lightSampleCount, highQualityClouds);
		}
	}

	private void mRender(Camera camera, bool renderVolumetricClouds, bool render2DFallback, bool renderAtmosphere, bool renderScatteringMask, bool includeCascadedShadows, bool downsample2DClouds, SampleCount scatteringMaskSamples, bool renderRainMask, DownSampleFactor downsampleFactor, SampleCount lightSampleCount, bool highQualityClouds)
	{
		if (OverCloud.beforeRender != null)
		{
			OverCloud.beforeRender();
		}
		if (!lighting.cloudShadows.enabled && !includeCascadedShadows)
		{
			renderScatteringMask = false;
		}
		if ((bool)components.skyMaterial && atmosphere.overrideSkyboxMaterial)
		{
			RenderSettings.skybox = components.skyMaterial;
		}
		if ((bool)components.sun)
		{
			RenderSettings.sun = components.sun;
		}
		if (renderRainMask && m_CurrentPreset.precipitation > Mathf.Epsilon)
		{
			RenderTexture active = RenderTexture.active;
			int maskResolution = (int)weather.rain.maskResolution;
			float num = weather.rain.maskRadius * 2f;
			float num2 = num / (float)maskResolution * 9f;
			Vector3 position = camera.transform.position;
			position.x = Mathf.Round(position.x / num2) * num2;
			position.z = Mathf.Round(position.z / num2) * num2;
			position.y = adjustedCloudPlaneAltitude;
			m_RainCamera.transform.position = position;
			m_RainCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
			Shader.SetGlobalVector("_OC_RainMaskPosition", m_RainCamera.transform.position);
			Shader.SetGlobalVector("_OC_RainMaskRadius", new Vector3(weather.rain.maskRadius, 1f / weather.rain.maskRadius, 1f / num));
			Shader.SetGlobalFloat("_OC_RainMaskFalloff", 1f / weather.rain.maskFalloff);
			Shader.SetGlobalVector("_OC_RainMaskTexel", new Vector4(maskResolution, 1f / (float)maskResolution, weather.rain.maskOffsetTexture.width, 1f / (float)weather.rain.maskOffsetTexture.width));
			Shader.SetGlobalFloat("_OC_RainMaskOffset", weather.rain.maskOffset);
			if (!m_RainMask || m_RainMask.width != maskResolution || m_RainMask.height != maskResolution)
			{
				m_RainMask = new RenderTexture(maskResolution, maskResolution, 16, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
			}
			m_RainMask.filterMode = FilterMode.Bilinear;
			m_RainCamera.orthographic = true;
			m_RainCamera.orthographicSize = weather.rain.maskRadius;
			m_RainCamera.aspect = 1f;
			m_RainCamera.clearFlags = CameraClearFlags.Color;
			m_RainCamera.backgroundColor = Color.clear;
			m_RainCamera.cullingMask = weather.rain.maskLayers;
			m_RainCamera.allowMSAA = false;
			m_RainCamera.farClipPlane = adjustedCloudPlaneAltitude;
			m_RainCamera.nearClipPlane = 1f;
			m_RainCamera.renderingPath = RenderingPath.Forward;
			CommandBuffer commandBuffer = new CommandBuffer();
			Shader.SetGlobalTexture("_OC_RainMaskOffsetTex", weather.rain.maskOffsetTexture);
			commandBuffer.Blit(null, m_RainMask, m_UtilitiesMat, 3);
			m_RainCamera.AddCommandBuffer(CameraEvent.AfterDepthTexture, commandBuffer);
			m_RainCamera.depthTextureMode = DepthTextureMode.Depth;
			ShadowQuality shadows = QualitySettings.shadows;
			QualitySettings.shadows = ShadowQuality.Disable;
			m_RainCamera.Render();
			QualitySettings.shadows = shadows;
			m_RainCamera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, commandBuffer);
			if (weather.rain.maskBlur > Mathf.Epsilon)
			{
				RenderTexture temporary = RenderTexture.GetTemporary(m_RainMask.descriptor);
				Shader.SetGlobalVector("_PixelSize", new Vector2(1f / (float)m_RainMask.width, 1f / (float)m_RainMask.height));
				Shader.SetGlobalFloat("_BlurAmount", weather.rain.maskBlur);
				Graphics.Blit(m_RainMask, temporary, m_SeparableBlurMat, 0);
				Graphics.Blit(temporary, m_RainMask, m_SeparableBlurMat, 1);
				RenderTexture.ReleaseTemporary(temporary);
			}
			Shader.SetGlobalTexture("_OC_RainMask", m_RainMask);
			RenderTexture.active = active;
		}
		else
		{
			Shader.SetGlobalTexture("_OC_RainMask", Texture2D.whiteTexture);
		}
		UpdateRaymarchingMatrices(camera);
		bool isValid = InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid;
		VRTextureUsage vRTextureUsage = ((camera.stereoTargetEye != 0 && Application.isPlaying && XRSettings.enabled && isValid) ? VRTextureUsage.TwoEyes : VRTextureUsage.None);
		Shader.SetGlobalFloat("_OC_FarClipInv", 1f / (camera.farClipPlane * 0.65f));
		_OC_MieScatteringParams.value = new Vector4(atmosphere.mieScatteringIntensity * 0.1f, atmosphere.mieScatteringPhase, atmosphere.mieScatteringFogPhase, (renderScatteringMask && includeCascadedShadows) ? Mathf.Pow(1f - atmosphere.mieScatteringDistanceFadeB, 8f) : Mathf.Pow(1f - atmosphere.mieScatteringDistanceFadeA, 8f));
		if (camera.actualRenderingPath == RenderingPath.Forward && camera.depthTextureMode == DepthTextureMode.None)
		{
			camera.depthTextureMode = DepthTextureMode.Depth;
		}
		bufferWidth = camera.pixelWidth * ((vRTextureUsage != VRTextureUsage.TwoEyes) ? 1 : 2);
		bufferHeight = camera.pixelHeight;
		bufferWidthDS = bufferWidth / (int)downsampleFactor;
		bufferHeightDS = bufferHeight / (int)downsampleFactor;
		if (bufferWidthDS % 2 != 0)
		{
			bufferWidthDS++;
		}
		UpdateShaderProperties();
		m_Renderer.enabled = false;
		m_LodRenderer.enabled = false;
		CommandBuffer commandBuffer2 = null;
		if (m_CameraPreBuffers.ContainsKey(camera))
		{
			commandBuffer2 = m_CameraPreBuffers[camera];
			commandBuffer2.Clear();
		}
		else
		{
			commandBuffer2 = new CommandBuffer();
			commandBuffer2.name = "CloudBufPre";
			m_CameraPreBuffers.Add(camera, commandBuffer2);
			camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, commandBuffer2);
			camera.AddCommandBuffer(CameraEvent.AfterLighting, commandBuffer2);
		}
		CommandBuffer commandBuffer3 = null;
		if (m_CameraBuffers.ContainsKey(camera))
		{
			commandBuffer3 = m_CameraBuffers[camera];
			commandBuffer3.Clear();
		}
		else
		{
			commandBuffer3 = new CommandBuffer();
			commandBuffer3.name = "CloudBuf";
			m_CameraBuffers.Add(camera, commandBuffer3);
			camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, commandBuffer3);
		}
		CommandBuffer commandBuffer4 = null;
		if (m_CameraPostBuffers.ContainsKey(camera))
		{
			commandBuffer4 = m_CameraPostBuffers[camera];
			commandBuffer4.Clear();
		}
		else
		{
			commandBuffer4 = new CommandBuffer();
			commandBuffer4.name = "CloudBufPost";
			m_CameraPostBuffers.Add(camera, commandBuffer3);
			camera.AddCommandBuffer(CameraEvent.AfterEverything, commandBuffer4);
		}
		commandBuffer2.EnableShaderKeyword("OVERCLOUD_ENABLED");
		if (renderAtmosphere)
		{
			commandBuffer2.EnableShaderKeyword("OVERCLOUD_ATMOSPHERE_ENABLED");
		}
		else
		{
			commandBuffer2.DisableShaderKeyword("OVERCLOUD_ATMOSPHERE_ENABLED");
		}
		if (downsample2DClouds)
		{
			commandBuffer2.EnableShaderKeyword("DOWNSAMPLE_2D_CLOUDS");
		}
		else
		{
			commandBuffer2.DisableShaderKeyword("DOWNSAMPLE_2D_CLOUDS");
		}
		if (renderRainMask)
		{
			commandBuffer2.EnableShaderKeyword("RAIN_MASK_ENABLED");
		}
		else
		{
			commandBuffer2.DisableShaderKeyword("RAIN_MASK_ENABLED");
		}
		if (RenderSettings.skybox == components.skyMaterial || atmosphere.overrideSkyboxMaterial)
		{
			commandBuffer2.EnableShaderKeyword("OVERCLOUD_SKY_ENABLED");
		}
		else
		{
			commandBuffer2.DisableShaderKeyword("OVERCLOUD_SKY_ENABLED");
		}
		switch (downsampleFactor)
		{
		case DownSampleFactor.Full:
			m_UpsampleMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 0f);
			break;
		case DownSampleFactor.Half:
			m_UpsampleMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 0.5f);
			break;
		case DownSampleFactor.Quarter:
			m_UpsampleMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 1f);
			break;
		case DownSampleFactor.Eight:
			m_UpsampleMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 2f);
			break;
		}
		commandBuffer2.SetGlobalVector("_PixelSize", new Vector4(bufferWidth, bufferHeight, 1f / (float)bufferWidth, 1f / (float)bufferHeight));
		commandBuffer2.SetGlobalVector("_PixelSizeDS", new Vector4(bufferWidthDS, bufferHeightDS, 1f / (float)bufferWidthDS, 1f / (float)bufferHeightDS));
		switch (downsampleFactor)
		{
		case DownSampleFactor.Full:
			m_DownsampleDepthMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 0f);
			break;
		case DownSampleFactor.Half:
			m_DownsampleDepthMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 0.5f);
			break;
		case DownSampleFactor.Quarter:
			m_DownsampleDepthMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 1f);
			break;
		case DownSampleFactor.Eight:
			m_DownsampleDepthMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 2f);
			break;
		}
		RenderTextureDescriptor desc = new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.RFloat, 0);
		desc.sRGB = false;
		desc.vrUsage = vRTextureUsage;
		if (!m_DownsampledDepthRTs.ContainsKey(camera))
		{
			m_DownsampledDepthRTs.Add(camera, new RenderTexture(desc));
		}
		if (m_DownsampledDepthRTs[camera] == null || m_DownsampledDepthRTs[camera].width != bufferWidthDS || m_DownsampledDepthRTs[camera].height != bufferHeightDS)
		{
			if (m_DownsampledDepthRTs[camera] != null)
			{
				m_DownsampledDepthRTs[camera].DiscardContents();
				m_DownsampledDepthRTs[camera].Release();
			}
			m_DownsampledDepthRTs[camera] = new RenderTexture(desc);
		}
		RenderTexture renderTexture = m_DownsampledDepthRTs[camera];
		renderTexture.filterMode = FilterMode.Point;
		commandBuffer2.SetRenderTarget(renderTexture);
		commandBuffer2.ClearRenderTarget(clearDepth: true, clearColor: true, new Color(0f, 0f, 0f, 0f));
		commandBuffer2.Blit(null, renderTexture, m_DownsampleDepthMat);
		commandBuffer2.SetGlobalTexture("_CameraDepthLowRes", renderTexture);
		if (renderAtmosphere && renderVolumetricClouds && renderScatteringMask && (bool)dominantOverCloudLight)
		{
			desc = new RenderTextureDescriptor(bufferWidth, bufferHeight, RenderTextureFormat.RG32, 0);
			desc.vrUsage = vRTextureUsage;
			if (!m_ScatteringMasks.ContainsKey(camera))
			{
				m_ScatteringMasks.Add(camera, new RenderTexture(desc));
			}
			if (m_ScatteringMasks[camera] == null || m_ScatteringMasks[camera].width != desc.width || m_ScatteringMasks[camera].height != desc.height)
			{
				m_ScatteringMasks[camera] = new RenderTexture(desc);
			}
			scatteringMaskRT = m_ScatteringMasks[camera];
			RenderTextureDescriptor desc2 = new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.RGHalf, 0);
			desc2.vrUsage = vRTextureUsage;
			int num3 = Shader.PropertyToID("_ScatteringMaskDS");
			commandBuffer2.GetTemporaryRT(num3, desc2);
			float num4 = Mathf.Max(volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier * atmosphere.scatteringMask.range, 10f);
			Shader.SetGlobalVector("_OC_ScatteringMaskRadius", new Vector2(num4, 1f / num4));
			m_ScatteringMaskRTMat.SetFloat("_Intensity", atmosphere.scatteringMask.intensity);
			m_ScatteringMaskRTMat.SetFloat("_Floor", atmosphere.scatteringMask.floor);
			m_ScatteringMaskRTMat.SetVector("_Random", UnityEngine.Random.insideUnitCircle);
			m_ScatteringMaskRTMat.SetVector("_ShadowDistance", new Vector2(QualitySettings.shadowDistance, 1f / QualitySettings.shadowDistance));
			bool flag = includeCascadedShadows && QualitySettings.shadows != ShadowQuality.Disable;
			flag = (bool)dominantLight && flag && dominantLight.shadows != 0 && dominantLight.isActiveAndEnabled;
			m_ScatteringMaskRTMat.SetFloat("_CascadedShadowsEnabled", flag ? 1f : 0f);
			switch (scatteringMaskSamples)
			{
			case SampleCount.Low:
				m_ScatteringMaskRTMat.EnableKeyword("SAMPLE_COUNT_LOW");
				m_ScatteringMaskRTMat.DisableKeyword("SAMPLE_COUNT_MEDIUM");
				break;
			case SampleCount.Normal:
				m_ScatteringMaskRTMat.DisableKeyword("SAMPLE_COUNT_LOW");
				m_ScatteringMaskRTMat.EnableKeyword("SAMPLE_COUNT_MEDIUM");
				break;
			case SampleCount.High:
				m_ScatteringMaskRTMat.DisableKeyword("SAMPLE_COUNT_LOW");
				m_ScatteringMaskRTMat.DisableKeyword("SAMPLE_COUNT_MEDIUM");
				break;
			}
			commandBuffer2.Blit(null, num3, m_ScatteringMaskRTMat);
			int num5 = Shader.PropertyToID("_BlurResult");
			commandBuffer2.GetTemporaryRT(num5, desc2);
			commandBuffer2.SetGlobalFloat("_DepthThreshold", 0.1f);
			commandBuffer2.SetGlobalVector("_PixelSize", new Vector2(1f / (float)bufferWidthDS, 1f / (float)bufferHeightDS));
			commandBuffer2.SetGlobalFloat("_BlurAmount", 1f);
			commandBuffer2.Blit(num3, num5, m_SeparableBlurMat, 2);
			commandBuffer2.Blit(num5, num3, m_SeparableBlurMat, 3);
			commandBuffer2.ReleaseTemporaryRT(num5);
			commandBuffer2.Blit(num3, scatteringMaskRT, m_UpsampleMat, 0);
			commandBuffer2.ReleaseTemporaryRT(num3);
			commandBuffer2.SetGlobalTexture("_OC_ScatteringMask", scatteringMaskRT);
		}
		else
		{
			commandBuffer2.SetGlobalTexture("_OC_ScatteringMask", Texture2D.whiteTexture);
		}
		if (renderAtmosphere)
		{
			int num6 = Shader.PropertyToID("_BackBuffer");
			if (XRSettings.enabled && camera.stereoEnabled)
			{
				desc = XRSettings.eyeTextureDesc;
				desc.colorFormat = (camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
				commandBuffer2.GetTemporaryRT(num6, desc);
			}
			else
			{
				commandBuffer2.GetTemporaryRT(num6, new RenderTextureDescriptor(bufferWidth, bufferHeight, camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default, 0));
			}
			commandBuffer2.SetRenderTarget(num6);
			commandBuffer2.SetGlobalTexture("_BlitTex", BuiltinRenderTextureType.CameraTarget);
			commandBuffer2.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 7);
			commandBuffer2.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
			commandBuffer2.SetGlobalTexture("_BackBuffer", num6);
			commandBuffer2.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 0);
			commandBuffer2.ReleaseTemporaryRT(num6);
		}
		CommandBuffer commandBuffer5 = null;
		if (m_OcclusionBuffers.ContainsKey(camera))
		{
			commandBuffer5 = m_OcclusionBuffers[camera];
			if (camera.renderingPath != RenderingPath.DeferredShading || lighting.cloudAmbientOcclusion.intensity <= Mathf.Epsilon)
			{
				camera.RemoveCommandBuffer(CameraEvent.BeforeReflections, commandBuffer5);
				m_OcclusionBuffers.Remove(camera);
			}
		}
		else if (camera.renderingPath == RenderingPath.DeferredShading && lighting.cloudAmbientOcclusion.intensity > Mathf.Epsilon)
		{
			commandBuffer5 = new CommandBuffer();
			commandBuffer5.name = "OverCloudAmbientOcclusion";
			m_OcclusionBuffers.Add(camera, commandBuffer5);
			commandBuffer5.SetRenderTarget(m_OcclusionMRT, BuiltinRenderTextureType.CameraTarget);
			commandBuffer5.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 2);
			camera.AddCommandBuffer(CameraEvent.BeforeReflections, commandBuffer5);
		}
		CommandBuffer commandBuffer6 = null;
		if (m_WetnessBuffers.ContainsKey(camera))
		{
			commandBuffer6 = m_WetnessBuffers[camera];
			commandBuffer6.Clear();
			if (camera.renderingPath != RenderingPath.DeferredShading)
			{
				camera.RemoveCommandBuffer(CameraEvent.BeforeReflections, commandBuffer6);
				m_WetnessBuffers.Remove(camera);
			}
		}
		else if (camera.renderingPath == RenderingPath.DeferredShading)
		{
			commandBuffer6 = new CommandBuffer();
			commandBuffer6.name = "OverCloudWetness";
			m_WetnessBuffers.Add(camera, commandBuffer6);
			camera.AddCommandBuffer(CameraEvent.BeforeReflections, commandBuffer6);
		}
		if (commandBuffer6 != null)
		{
			if (renderRainMask)
			{
				commandBuffer6.EnableShaderKeyword("RAIN_MASK_ENABLED");
			}
			else
			{
				commandBuffer6.DisableShaderKeyword("RAIN_MASK_ENABLED");
			}
			commandBuffer6.SetGlobalTexture("_GBuffer2", BuiltinRenderTextureType.GBuffer2);
			commandBuffer6.SetRenderTarget(m_WetnessMRT, BuiltinRenderTextureType.CameraTarget);
			commandBuffer6.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 3);
			desc = new RenderTextureDescriptor(bufferWidth, bufferHeight, RenderTextureFormat.ARGB2101010, 0);
			desc.vrUsage = vRTextureUsage;
			int num7 = Shader.PropertyToID("_GBuffer2Copy");
			commandBuffer6.GetTemporaryRT(num7, desc);
			commandBuffer6.SetGlobalTexture("_GBuffer2Copy", num7);
			commandBuffer6.Blit(BuiltinRenderTextureType.GBuffer2, num7);
			commandBuffer6.SetGlobalTexture("_OC_RainFlowTex", weather.rain.flowTexture);
			commandBuffer6.SetRenderTarget(BuiltinRenderTextureType.GBuffer2, BuiltinRenderTextureType.CameraTarget);
			commandBuffer6.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 4);
			commandBuffer6.ReleaseTemporaryRT(num7);
		}
		if (!m_CloudRTs.ContainsKey(camera))
		{
			desc = new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.ARGBHalf, 0);
			desc.vrUsage = vRTextureUsage;
			m_CloudRTs.Add(camera, new RenderTexture(desc));
		}
		if (!m_CloudDepthRTs.ContainsKey(camera))
		{
			desc = new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.ARGBFloat, 0);
			desc.vrUsage = vRTextureUsage;
			m_CloudDepthRTs.Add(camera, new RenderTexture(desc));
		}
		if (m_CloudRTs[camera] == null || m_CloudRTs[camera].width != bufferWidthDS || m_CloudRTs[camera].height != bufferHeightDS)
		{
			if (m_CloudRTs[camera] != null)
			{
				m_CloudRTs[camera].DiscardContents();
				m_CloudRTs[camera].Release();
			}
			desc = new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.ARGBHalf, 0);
			desc.vrUsage = vRTextureUsage;
			m_CloudRTs[camera] = new RenderTexture(desc);
		}
		if (m_CloudDepthRTs[camera] == null || m_CloudDepthRTs[camera].width != bufferWidthDS || m_CloudDepthRTs[camera].height != bufferHeightDS)
		{
			if (m_CloudDepthRTs[camera] != null)
			{
				m_CloudDepthRTs[camera].DiscardContents();
				m_CloudDepthRTs[camera].Release();
			}
			desc = new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.ARGBHalf, 0);
			desc.vrUsage = vRTextureUsage;
			m_CloudDepthRTs[camera] = new RenderTexture(desc);
		}
		cloudRT = m_CloudRTs[camera];
		cloudDepthRT = m_CloudDepthRTs[camera];
		commandBuffer3.SetRenderTarget(cloudRT);
		commandBuffer3.ClearRenderTarget(clearDepth: true, clearColor: true, new Color(0f, 0f, 0f, 0f));
		commandBuffer3.SetGlobalFloat("_FarZ", camera.farClipPlane);
		commandBuffer3.Blit(null, cloudDepthRT, m_ClearMat, 1);
		CloudPlane[] array = cloudPlanes.OrderBy((CloudPlane o) => Mathf.Abs(camera.transform.position.y + currentOriginOffset.y - o.height)).Reverse().ToArray();
		float num8 = camera.transform.position.y + currentOriginOffset.y;
		CloudPlane[] array2 = array;
		foreach (CloudPlane cloudPlane in array2)
		{
			if ((!(cloudPlane.height > current.cloudPlaneAltitude) || !(num8 > cloudPlane.height)) && (!(cloudPlane.height < current.cloudPlaneAltitude) || !(num8 < cloudPlane.height)))
			{
				commandBuffer3.SetGlobalTexture("_CloudPlaneTex", cloudPlane.texture);
				commandBuffer3.SetGlobalVector("_CloudPlaneParams1", new Vector4(1f / cloudPlane.scale, 1f / (cloudPlane.detailScale / cloudPlane.scale), cloudPlane.height, cloudPlane.opacity));
				commandBuffer3.SetGlobalVector("_CloudPlaneParams2", new Vector3(cloudPlane.lightPenetration, cloudPlane.lightAbsorption, cloudPlane.windTimescale));
				commandBuffer3.SetGlobalVector("_CloudPlaneColor", cloudPlane.color);
				if (num8 > cloudPlane.height)
				{
					commandBuffer3.SetGlobalFloat("_AboveCloudPlane", 1f);
				}
				else
				{
					commandBuffer3.SetGlobalFloat("_AboveCloudPlane", 0f);
				}
				if (downsample2DClouds)
				{
					commandBuffer3.SetRenderTarget(cloudRT);
				}
				else
				{
					commandBuffer3.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
				}
				commandBuffer3.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 1);
			}
		}
		if (render2DFallback)
		{
			commandBuffer3.SetGlobalFloat("_RenderingVolumetricClouds", renderVolumetricClouds ? 1 : 0);
			if (downsample2DClouds)
			{
				commandBuffer3.SetRenderTarget(cloudRT);
			}
			else
			{
				commandBuffer3.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
			}
			commandBuffer3.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 6);
		}
		if (renderVolumetricClouds)
		{
			if (highQualityClouds)
			{
				components.cloudMaterial.EnableKeyword("HQ_LIGHT_SAMPLING");
			}
			else
			{
				components.cloudMaterial.DisableKeyword("HQ_LIGHT_SAMPLING");
			}
			switch (lightSampleCount)
			{
			case SampleCount.Low:
				components.cloudMaterial.EnableKeyword("SAMPLE_COUNT_LOW");
				components.cloudMaterial.DisableKeyword("SAMPLE_COUNT_NORMAL");
				break;
			default:
				components.cloudMaterial.DisableKeyword("SAMPLE_COUNT_LOW");
				components.cloudMaterial.EnableKeyword("SAMPLE_COUNT_NORMAL");
				break;
			case SampleCount.High:
				components.cloudMaterial.DisableKeyword("SAMPLE_COUNT_LOW");
				components.cloudMaterial.DisableKeyword("SAMPLE_COUNT_NORMAL");
				break;
			}
			if (highQualityClouds)
			{
				components.cloudMaterial.EnableKeyword("HQ_LIGHT_SAMPLING");
			}
			else
			{
				components.cloudMaterial.DisableKeyword("HQ_LIGHT_SAMPLING");
			}
			RenderTargetIdentifier[] array3 = new RenderTargetIdentifier[2] { cloudRT, cloudDepthRT };
			commandBuffer3.SetRenderTarget(array3, array3[0]);
			commandBuffer3.EnableShaderKeyword("LOD_CLOUDS");
			commandBuffer3.DrawRenderer(m_LodRenderer, components.cloudMaterial, 0, 0);
			commandBuffer3.DisableShaderKeyword("LOD_CLOUDS");
			commandBuffer3.DrawRenderer(m_Renderer, components.cloudMaterial, 0, 0);
			if (!downsample2DClouds)
			{
				commandBuffer3.SetGlobalTexture("_OverCloudDepthTex", cloudDepthRT);
				commandBuffer3.SetGlobalTexture("_OverCloudTex", cloudRT);
				commandBuffer3.Blit(cloudRT, BuiltinRenderTextureType.CameraTarget, m_UpsampleMat, 1);
			}
		}
		array2 = array;
		foreach (CloudPlane cloudPlane2 in array2)
		{
			if ((cloudPlane2.height > current.cloudPlaneAltitude && num8 > cloudPlane2.height) || (cloudPlane2.height < current.cloudPlaneAltitude && num8 < cloudPlane2.height))
			{
				commandBuffer3.SetGlobalTexture("_CloudPlaneTex", cloudPlane2.texture);
				commandBuffer3.SetGlobalVector("_CloudPlaneParams1", new Vector4(1f / cloudPlane2.scale, 1f / (cloudPlane2.detailScale / cloudPlane2.scale), cloudPlane2.height, cloudPlane2.opacity));
				commandBuffer3.SetGlobalVector("_CloudPlaneParams2", new Vector3(cloudPlane2.lightPenetration, cloudPlane2.lightAbsorption, cloudPlane2.windTimescale));
				commandBuffer3.SetGlobalVector("_CloudPlaneColor", cloudPlane2.color);
				if (num8 > cloudPlane2.height)
				{
					commandBuffer3.SetGlobalFloat("_AboveCloudPlane", 1f);
				}
				else
				{
					commandBuffer3.SetGlobalFloat("_AboveCloudPlane", 0f);
				}
				if (downsample2DClouds)
				{
					commandBuffer3.SetRenderTarget(cloudRT);
				}
				else
				{
					commandBuffer3.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
				}
				commandBuffer3.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 1);
			}
		}
		if (downsample2DClouds)
		{
			commandBuffer3.SetGlobalTexture("_OverCloudDepthTex", cloudDepthRT);
			commandBuffer3.SetGlobalTexture("_OverCloudTex", cloudRT);
			commandBuffer3.Blit(cloudRT, BuiltinRenderTextureType.CameraTarget, m_UpsampleMat, 1);
		}
		CommandBuffer commandBuffer7 = null;
		if (m_VolumeBuffers.ContainsKey(camera))
		{
			commandBuffer7 = m_VolumeBuffers[camera];
			commandBuffer7.Clear();
		}
		else
		{
			commandBuffer7 = new CommandBuffer();
			commandBuffer7.name = "VolumeLightBuffer";
			m_VolumeBuffers.Add(camera, commandBuffer7);
			camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer7);
		}
		if (OverCloudFogLight.fogLights != null && OverCloudFogLight.fogLights.Count > 0)
		{
			desc = new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.DefaultHDR, 0);
			desc.vrUsage = vRTextureUsage;
			int num9 = Shader.PropertyToID("_VolumeLightRT");
			commandBuffer7.GetTemporaryRT(num9, desc);
			commandBuffer7.SetGlobalColor("_ClearColor", Color.black);
			commandBuffer7.Blit(null, num9, m_ClearMat, 0);
			commandBuffer7.SetRenderTarget(num9);
			foreach (OverCloudFogLight fogLight in OverCloudFogLight.fogLights)
			{
				fogLight.BufferRender(commandBuffer7);
			}
			int num10 = Shader.PropertyToID("_BlurResult");
			commandBuffer7.GetTemporaryRT(num10, desc);
			commandBuffer7.SetGlobalFloat("_DepthThreshold", 0.1f);
			commandBuffer7.SetGlobalVector("_PixelSize", new Vector2(1f / (float)bufferWidthDS, 1f / (float)bufferHeightDS));
			commandBuffer7.SetGlobalFloat("_BlurAmount", 1f);
			commandBuffer7.Blit(num9, num10, m_SeparableBlurMat, 2);
			commandBuffer7.Blit(num10, num9, m_SeparableBlurMat, 3);
			commandBuffer7.Blit(num9, BuiltinRenderTextureType.CameraTarget, m_UpsampleMat, 2);
			commandBuffer7.ReleaseTemporaryRT(num9);
			commandBuffer7.ReleaseTemporaryRT(num10);
		}
		if (OverCloud.afterRender != null)
		{
			OverCloud.afterRender();
		}
	}

	public static void CleanUp()
	{
		cloudRT = null;
		cloudDepthRT = null;
		scatteringMaskRT = null;
		volumeRT = null;
		Shader.SetGlobalTexture("_OC_ScatteringMask", Texture2D.whiteTexture);
	}

	public static void CameraUpdate(Camera camera)
	{
		if ((bool)camera && (bool)instance)
		{
			if (OverCloud.beforeCameraUpdate != null)
			{
				OverCloud.beforeCameraUpdate();
			}
			instance.CheckComponents();
			PositionCloudVolume(camera);
			skyChanged = false;
			instance.UpdateWeather(camera);
			instance.UpdateTime();
			instance.UpdateLighting();
			instance.UpdateOrbital();
			if (OverCloud.afterCameraUpdate != null)
			{
				OverCloud.afterCameraUpdate();
			}
		}
	}

	public static void PositionCloudVolume(Camera camera)
	{
		if ((bool)instance && instance.enabled)
		{
			instance.UpdatePosition(camera);
			instance.UpdateCompositor(camera);
			instance.UpdateCloudShadows();
			instance.UpdatePointLight(camera);
		}
	}

	private void UpdateCloudShadows()
	{
		if (!lighting.cloudShadows.enabled || current.cloudShadowsDensity * current.cloudShadowsOpacity * current.cloudiness * current.macroCloudiness < Mathf.Epsilon)
		{
			Shader.SetGlobalTexture("_OC_CloudShadowsTex", Texture2D.blackTexture);
			return;
		}
		if (!m_CloudShadowsRT || m_CloudShadowsRT.width != (int)lighting.cloudShadows.resolution)
		{
			m_CloudShadowsRT = new RenderTexture((int)lighting.cloudShadows.resolution, (int)lighting.cloudShadows.resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		}
		Rect worldExtents = m_WorldExtents;
		Vector2 center = worldExtents.center;
		worldExtents.center = Vector2.zero;
		worldExtents.size *= lighting.cloudShadows.coverage;
		worldExtents.center = center;
		Shader.SetGlobalVector("_OC_CloudShadowExtentsMinMax", new Vector4(worldExtents.min.x, worldExtents.min.y, worldExtents.max.x, worldExtents.max.y));
		Shader.SetGlobalVector("_OC_CloudShadowExtents", new Vector4(worldExtents.width, worldExtents.height, 1f / worldExtents.width, 1f / worldExtents.height));
		Graphics.Blit(null, m_CloudShadowsRT, m_UtilitiesMat, 0);
		if (lighting.cloudShadows.blur > Mathf.Epsilon)
		{
			RenderTexture temporary = RenderTexture.GetTemporary(m_CloudShadowsRT.width, m_CloudShadowsRT.height, 0, m_CloudShadowsRT.format, RenderTextureReadWrite.Linear);
			Shader.SetGlobalVector("_PixelSize", new Vector2(1f / (float)temporary.width, 1f / (float)temporary.height));
			Shader.SetGlobalFloat("_BlurAmount", lighting.cloudShadows.blur);
			Graphics.Blit(m_CloudShadowsRT, temporary, m_SeparableBlurMat, 0);
			Graphics.Blit(temporary, m_CloudShadowsRT, m_SeparableBlurMat, 1);
			RenderTexture.ReleaseTemporary(temporary);
		}
		Shader.SetGlobalTexture("_OC_CloudShadowsTex", m_CloudShadowsRT);
	}

	private void UpdateLighting()
	{
		if (!components.sun)
		{
			return;
		}
		moonFade = 1f;
		if ((bool)components.sun)
		{
			float time = Vector3.Dot(components.sun.transform.forward, Vector3.down) * 0.5f + 0.5f;
			RenderSettings.ambientSkyColor = lighting.ambient.sky.Evaluate(time) * lighting.ambient.multiplier;
			RenderSettings.ambientEquatorColor = lighting.ambient.equator.Evaluate(time) * lighting.ambient.multiplier;
			RenderSettings.ambientGroundColor = lighting.ambient.ground.Evaluate(time) * lighting.ambient.multiplier;
			RenderSettings.ambientSkyColor *= Color.Lerp(Color.white, atmosphere.lunarEclipseColor, lunarEclipse * lighting.ambient.lunarEclipseLightingInfluence);
			RenderSettings.ambientEquatorColor *= Color.Lerp(Color.white, atmosphere.lunarEclipseColor, lunarEclipse * lighting.ambient.lunarEclipseLightingInfluence);
			RenderSettings.ambientGroundColor *= Color.Lerp(Color.white, atmosphere.lunarEclipseColor, lunarEclipse * lighting.ambient.lunarEclipseLightingInfluence);
			RenderSettings.ambientSkyColor *= Color.Lerp(Color.white, atmosphere.solarEclipseColor, solarEclipse);
			RenderSettings.ambientEquatorColor *= Color.Lerp(Color.white, atmosphere.solarEclipseColor, solarEclipse);
			RenderSettings.ambientGroundColor *= Color.Lerp(Color.white, atmosphere.solarEclipseColor, solarEclipse);
			if ((bool)components.moon)
			{
				moonFade = Mathf.Min(Mathf.Max(components.sun.transform.forward.y, 0f), 1f);
			}
		}
		Color linear = RenderSettings.ambientSkyColor.linear;
		Color.RGBToHSV(linear, out var H, out var _, out var V);
		Shader.SetGlobalColor("_OC_AmbientColor", Color.Lerp(linear, Color.HSVToRGB(H, 0f, V), lighting.cloudLighting.ambientDesaturation));
	}

	private void UpdatePointLight(Camera camera)
	{
		if (OverCloudLight.lights != null && OverCloudLight.lights.Count > 0)
		{
			OverCloudLight overCloudLight = null;
			float num = float.PositiveInfinity;
			for (int i = 0; i < OverCloudLight.lights.Count; i++)
			{
				if (OverCloudLight.lights[i].hasActiveLight && OverCloudLight.lights[i].type == OverCloudLight.Type.Point)
				{
					float num2 = Vector3.Distance(camera.transform.position, OverCloudLight.lights[i].transform.position);
					if (num2 < num)
					{
						overCloudLight = OverCloudLight.lights[i];
						num = num2;
					}
				}
			}
			if (!overCloudLight)
			{
				Shader.DisableKeyword("OVERCLOUD_POINTLIGHT_ENABLED");
				return;
			}
			Shader.EnableKeyword("OVERCLOUD_POINTLIGHT_ENABLED");
			Vector3 position = overCloudLight.transform.position;
			Shader.SetGlobalVector("_OC_PointLightPosRadius", new Vector4(position.x, position.y, position.z, overCloudLight.pointRadius));
			Shader.SetGlobalVector("_OC_PointLightColor", overCloudLight.pointColor);
		}
		else
		{
			Shader.DisableKeyword("OVERCLOUD_POINTLIGHT_ENABLED");
		}
	}

	private void UpdateShaderProperties()
	{
		if (current == null)
		{
			return;
		}
		if (OverCloud.beforeShaderParametersUpdate != null)
		{
			OverCloud.beforeShaderParametersUpdate();
		}
		Shader.SetGlobalVector("_OverCloudOriginOffset", currentOriginOffset);
		if (m_LastAtmosphere == null || m_LastAtmosphere.planetScale != atmosphere.precomputation.planetScale || m_LastAtmosphere.heightScale != atmosphere.precomputation.heightScale || m_LastAtmosphere.mie != atmosphere.precomputation.mie || m_LastAtmosphere.rayleigh != atmosphere.precomputation.rayleigh || m_LastAtmosphere.ozone != atmosphere.precomputation.ozone || m_LastAtmosphere.phase != atmosphere.precomputation.phase)
		{
			InitializeAtmosphere();
			if (m_LastAtmosphere == null)
			{
				m_LastAtmosphere = new Atmosphere.Precomputation();
			}
			m_LastAtmosphere.planetScale = atmosphere.precomputation.planetScale;
			m_LastAtmosphere.heightScale = atmosphere.precomputation.heightScale;
			m_LastAtmosphere.mie = atmosphere.precomputation.mie;
			m_LastAtmosphere.rayleigh = atmosphere.precomputation.rayleigh;
			m_LastAtmosphere.ozone = atmosphere.precomputation.ozone;
			m_LastAtmosphere.phase = atmosphere.precomputation.phase;
		}
		if ((bool)volumetricClouds.noiseTexture)
		{
			_OC_NoiseTex.value = volumetricClouds.noiseTexture;
		}
		else
		{
			Debug.LogError("OverCloud noise texture not set.");
		}
		_OC_NoiseScale.value = new Vector2(Mathf.Pow(volumetricClouds.noiseScale, 4f), Mathf.Pow(volumetricClouds.noiseMacroScale, 4f)) * 0.001f;
		_OC_Timescale.value = weather.windTimescale;
		_OC_3DNoiseTex.value = m_3DNoise;
		_OC_NoiseParams1.value = new Vector4(volumetricClouds.noiseSettings.noiseTiling_A, volumetricClouds.noiseSettings.noiseIntensity_A, volumetricClouds.noiseSettings.noiseTiling_B, volumetricClouds.noiseSettings.noiseIntensity_B);
		_OC_NoiseParams2.value = new Vector2(volumetricClouds.noiseSettings.turbulence, volumetricClouds.noiseSettings.riseFactor);
		_OC_Precipitation.value = m_CurrentPreset.precipitation;
		_OC_CloudOcclusionParams.value = new Vector2(lighting.cloudAmbientOcclusion.intensity, 1f / lighting.cloudAmbientOcclusion.heightFalloff);
		_OC_ShapeParams.value = new Vector4(volumetricClouds.noiseSettings.shapeCenter, 1f / volumetricClouds.noiseSettings.shapeCenter, 1f / (1f - volumetricClouds.noiseSettings.shapeCenter), volumetricClouds.noiseSettings.baseDensityIncrease);
		_OC_NoiseErosion.value = volumetricClouds.noiseSettings.erosion;
		_OC_AlphaEdgeParams.value = new Vector2(volumetricClouds.noiseSettings.alphaEdgeLower, volumetricClouds.noiseSettings.alphaEdgeUpper);
		_OC_CloudAltitude.value = adjustedCloudPlaneAltitude;
		_OC_CloudPlaneRadius.value = volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier;
		_OC_CloudHeight.value = current.cloudPlaneHeight;
		_OC_CloudHeightInv.value = 1f / current.cloudPlaneHeight;
		_OC_CloudShadowsSharpen.value = lighting.cloudShadows.sharpen;
		_OC_CloudShadowsEdgeTex.value = lighting.cloudShadows.edgeTexture;
		_OC_CloudShadowsEdgeTexParams.value = new Vector4(lighting.cloudShadows.coverage, 1f / (volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier * 2f * lighting.cloudShadows.coverage), lighting.cloudShadows.edgeTextureIntensity, lighting.cloudShadows.coverage / lighting.cloudShadows.edgeTextureScale);
		lighting.cloudLighting.UpdateShaderProperties();
		_OC_NightScattering.value = atmosphere.nightScattering;
		_SkySunSize.value = atmosphere.sunSize;
		_SkyMoonSize.value = atmosphere.moonSize;
		_SkySunIntensity.value = atmosphere.sunIntensity;
		_SkyMoonIntensity.value = atmosphere.moonIntensity;
		_SkyMoonCubemap.value = atmosphere.moonAlbedo;
		_SkyStarsCubemap.value = atmosphere.starsCubemap;
		_SkyStarsIntensity.value = atmosphere.starsIntensity;
		_SkySolarEclipse.value = new Vector4(atmosphere.solarEclipseColor.r, atmosphere.solarEclipseColor.g, atmosphere.solarEclipseColor.b, solarEclipse);
		_SkyLunarEclipse.value = new Vector4(atmosphere.lunarEclipseColor.r, atmosphere.lunarEclipseColor.g, atmosphere.lunarEclipseColor.b, lunarEclipse);
		_LunarEclipseLightingInfluence.value = lighting.ambient.lunarEclipseLightingInfluence;
		_OC_EarthColor.value = atmosphere.earthColor;
		_OC_GlobalWindMultiplier.value = m_CurrentPreset.windMultiplier;
		_OC_GlobalWetnessParams.value = new Vector4(m_CurrentPreset.wetnessRemap, weather.rain.albedoDarken, weather.rain.roughnessDecrease, 0f);
		_OC_GlobalRainParams.value = new Vector4(weather.rain.rippleIntensity, weather.rain.rippleScale, weather.rain.flowIntensity, weather.rain.flowScale);
		_OC_GlobalRainParams2.value = new Vector2(weather.rain.rippleTimescale * 100f, weather.rain.flowTimescale * 2f);
		float x = Mathf.Min(m_CurrentPreset.cloudiness * 2f, 1f);
		float y = Mathf.Max(m_CurrentPreset.cloudiness * 2f - 1f, 0f);
		float z = Mathf.Min(m_CurrentPreset.macroCloudiness * 2f, 1f);
		float w = Mathf.Max(m_CurrentPreset.macroCloudiness * 2f - 1f, 0f);
		_OC_Cloudiness.value = new Vector4(x, y, z, w);
		_OC_CloudSharpness.value = new Vector2(m_CurrentPreset.sharpness, m_CurrentPreset.macroSharpness);
		_OC_CloudDensity.value = new Vector2(m_CurrentPreset.opticalDensity, m_CurrentPreset.lightingDensity);
		_OC_CloudShadowsParams.value = new Vector2(m_CurrentPreset.cloudShadowsDensity, m_CurrentPreset.cloudShadowsOpacity);
		_OC_ScatteringMaskSoftness.value = atmosphere.scatteringMask.softness;
		_OC_ScatteringMaskFloor.value = atmosphere.scatteringMask.floor;
		_OC_FogParams.value = new Vector4(Mathf.Pow(m_CurrentPreset.fogDensity, 16f), m_CurrentPreset.fogDirectIntensity, m_CurrentPreset.fogAmbientIntensity, m_CurrentPreset.fogShadow);
		_OC_FogBlend.value = 1f / Mathf.Pow(_OC_FogParams.value.x, m_CurrentPreset.fogBlend);
		_OC_FogColor.value = m_CurrentPreset.fogAlbedo;
		_OC_FogHeight.value = m_CurrentPreset.fogHeight * current.cloudPlaneAltitude;
		_OC_FogFalloffParams.value = new Vector2(m_CurrentPreset.fogFalloff, 1f / m_CurrentPreset.fogFalloff);
		_OC_AtmosphereExposure.value = atmosphere.exposure * 0.0003f;
		_OC_AtmosphereDensity.value = atmosphere.density;
		_OC_AtmosphereFarClipFade.value = atmosphere.farClipFade;
		Vector3 value = Vector3.zero;
		Vector3 value2 = Vector3.zero;
		if ((bool)components.sun)
		{
			value = new Vector3(components.sun.color.r, components.sun.color.g, components.sun.color.b) * components.sun.intensity;
			_OC_CurrentSunColor.value = value;
		}
		if ((bool)components.moon)
		{
			value2 = new Vector3(components.moon.color.r, components.moon.color.g, components.moon.color.b) * components.moon.intensity;
			_OC_CurrentMoonColor.value = value2;
		}
		if ((bool)components.sun && components.sun.gameObject.activeInHierarchy && components.sun.intensity > 0.001f && components.sun.color.r + components.sun.color.g + components.sun.color.b > 0.001f)
		{
			_OC_LightDir.value = components.sun.transform.forward;
			_OC_LightDirYInv.value = 1f / components.sun.transform.forward.y;
			_OC_LightColor.value = value;
			dominantLight = components.sun;
			if ((bool)m_OverCloudSun)
			{
				dominantOverCloudLight = m_OverCloudSun;
			}
			else
			{
				dominantOverCloudLight = null;
			}
		}
		else if ((bool)components.moon && components.moon.gameObject.activeInHierarchy)
		{
			_OC_LightDir.value = components.moon.transform.forward;
			_OC_LightDirYInv.value = 1f / components.moon.transform.forward.y;
			_OC_LightColor.value = value2;
			dominantLight = components.moon;
			if ((bool)m_OverCloudMoon)
			{
				dominantOverCloudLight = m_OverCloudMoon;
			}
			else
			{
				dominantOverCloudLight = null;
			}
		}
		else
		{
			_OC_LightColor.value = Vector3.zero;
			dominantLight = null;
			dominantOverCloudLight = null;
		}
		if ((bool)components.sun)
		{
			_OC_ActualSunDir.value = components.sun.transform.forward;
			_OC_ActualSunColor.value = atmosphere.actualSunColor;
		}
		else
		{
			_OC_ActualSunColor.value = Color.clear;
		}
		if ((bool)components.moon)
		{
			_OC_ActualMoonDir.value = components.moon.transform.forward;
			if ((bool)components.sun)
			{
				_OC_ActualMoonColor.value = new Color(atmosphere.actualMoonColor.r, atmosphere.actualMoonColor.g, atmosphere.actualMoonColor.b, 1f - (Vector3.Dot(components.moon.transform.forward, components.sun.transform.forward) * 0.5f + 0.5f));
			}
			else
			{
				_OC_ActualMoonColor.value = atmosphere.actualMoonColor;
			}
		}
		else
		{
			_OC_ActualMoonColor.value = Color.clear;
		}
		for (int i = 0; i < m_CustomFloats.Length; i++)
		{
			if (m_CustomFloats[i].shaderParameter != "")
			{
				Shader.SetGlobalFloat(m_CustomFloats[i].shaderParameter, m_CurrentPreset.customFloats[i]);
			}
		}
		if (OverCloud.afterShaderParametersUpdate != null)
		{
			OverCloud.afterShaderParametersUpdate();
		}
	}

	private void UpdateWeather(Camera camera)
	{
		weather.windTime += Time.deltaTime * m_CurrentPreset.windMultiplier * weather.windTimescale * 10f;
		Shader.SetGlobalFloat("_OC_GlobalWindTime", weather.windTime);
		if (m_LastFramePreset == null)
		{
			m_LastFramePreset = new WeatherPreset(m_CurrentPreset);
		}
		else
		{
			m_LastFramePreset.Lerp(m_LastFramePreset, m_CurrentPreset, 1f);
		}
		if (m_TargetPreset == null || m_TargetPreset.name != activePreset)
		{
			FindTargetPreset();
			m_PrevPreset = new WeatherPreset(m_CurrentPreset);
			m_FadeTimer = 0f;
		}
		if (m_TargetPreset != null)
		{
			m_FadeTimer = Mathf.Clamp01(m_FadeTimer + Time.deltaTime / (Application.isPlaying ? fadeDuration : editorFadeDuration));
			m_CurrentPreset.Lerp(m_PrevPreset, m_TargetPreset, m_FadeTimer);
		}
		if (m_LastFramePreset.cloudiness != m_CurrentPreset.cloudiness || m_LastFramePreset.macroCloudiness != m_CurrentPreset.macroCloudiness || m_LastFramePreset.sharpness != m_CurrentPreset.sharpness || m_LastFramePreset.macroSharpness != m_CurrentPreset.macroSharpness || m_LastFramePreset.opticalDensity != m_CurrentPreset.opticalDensity || m_LastFramePreset.lightingDensity != m_CurrentPreset.lightingDensity || m_LastFramePreset.cloudShadowsDensity != m_CurrentPreset.cloudShadowsDensity || m_LastFramePreset.cloudShadowsOpacity != m_CurrentPreset.cloudShadowsOpacity || m_LastFramePreset.precipitation != m_CurrentPreset.precipitation || m_LastFramePreset.fogDensity != m_CurrentPreset.fogDensity || m_LastFramePreset.fogAlbedo != m_CurrentPreset.fogAlbedo || m_LastFramePreset.fogDirectIntensity != m_CurrentPreset.fogDirectIntensity || m_LastFramePreset.fogAmbientIntensity != m_CurrentPreset.fogAmbientIntensity || m_LastFramePreset.fogHeight != m_CurrentPreset.fogHeight)
		{
			skyChanged = true;
		}
		if ((bool)weather.lightning.gameObject)
		{
			m_LightningTimer -= Time.deltaTime;
			if (m_LightningTimer <= 0f || (m_LightningRestrike && !weather.lightning.gameObject.activeInHierarchy))
			{
				if ((UnityEngine.Random.Range(0f, 1f) <= m_CurrentPreset.lightningChance || (m_LightningRestrike && !weather.lightning.gameObject.activeInHierarchy)) && (bool)weather.lightning.gameObject && (Application.isPlaying || weather.lightning.enableInEditor))
				{
					Vector3 position = camera.transform.position;
					position.y = adjustedCloudPlaneAltitude;
					Vector3 insideUnitSphere = UnityEngine.Random.insideUnitSphere;
					insideUnitSphere.y = 0f;
					insideUnitSphere.Normalize();
					Vector3 forward = camera.transform.forward;
					forward.y = 0f;
					forward.Normalize();
					insideUnitSphere = Vector3.Lerp(insideUnitSphere, forward, weather.lightning.cameraBias);
					position += insideUnitSphere * UnityEngine.Random.Range(weather.lightning.distanceMin, weather.lightning.distanceMax);
					if (GetDensity2D(position) > weather.lightning.minimumDensity)
					{
						weather.lightning.gameObject.transform.position = position;
						weather.lightning.gameObject.transform.rotation = Quaternion.LookRotation(-insideUnitSphere, Vector3.up);
						weather.lightning.gameObject.SetActive(value: false);
						weather.lightning.gameObject.SetActive(value: true);
						m_LightningTimer = UnityEngine.Random.Range(weather.lightning.intervalMin, weather.lightning.intervalMax);
						m_LightningRestrike = UnityEngine.Random.Range(0f, 1f) <= weather.lightning.restrikeChance * m_CurrentPreset.lightningChance;
					}
				}
				else
				{
					m_LightningTimer = UnityEngine.Random.Range(weather.lightning.intervalMin, weather.lightning.intervalMax);
				}
			}
		}
		m_RainRippleMat.SetFloat("_TimeScale", 40f);
		m_RainRippleMat.SetFloat("_Intensity", 1f);
		Graphics.Blit(weather.rain.rippleTexture, m_RainRippleRT, m_RainRippleMat);
		Shader.SetGlobalTexture("_OC_RainRippleTex", m_RainRippleRT);
	}

	private void UpdateCompositor(Camera camera)
	{
		Vector3 position = camera.transform.position;
		float num = volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier * 2f;
		float num2 = num / (float)volumetricClouds.compositorResolution;
		position += currentOriginOffset;
		position.x = Mathf.Round(position.x / num2) * num2;
		position.z = Mathf.Round(position.z / num2) * num2;
		position -= currentOriginOffset;
		m_WorldExtents = new Rect(new Vector2(position.x, position.z), Vector2.one * num);
		m_WorldExtents.center -= m_WorldExtents.size * 0.5f;
		Shader.SetGlobalVector("_OC_CloudWorldExtentsMinMax", new Vector4(m_WorldExtents.min.x, m_WorldExtents.min.y, m_WorldExtents.max.x, m_WorldExtents.max.y));
		Shader.SetGlobalVector("_OC_CloudWorldExtents", new Vector4(m_WorldExtents.width, m_WorldExtents.height, 1f / m_WorldExtents.width, 1f / m_WorldExtents.height));
		Shader.SetGlobalVector("_OC_CloudWorldPos", new Vector3(position.x, 0f, position.z));
		Graphics.Blit(null, m_CompositorRT, m_CompositorMat);
		RenderTexture temporary = RenderTexture.GetTemporary(m_CompositorRT.width, m_CompositorRT.height, 0, m_CompositorRT.format, RenderTextureReadWrite.Linear);
		Shader.SetGlobalVector("_PixelSize", new Vector2(1f / (float)temporary.width, 1f / (float)temporary.height));
		Shader.SetGlobalFloat("_BlurAmount", volumetricClouds.compositorBlur);
		Graphics.Blit(m_CompositorRT, temporary, m_SeparableBlurMat, 4);
		Graphics.Blit(temporary, m_CompositorRT, m_SeparableBlurMat, 5);
		RenderTexture.ReleaseTemporary(temporary);
		Shader.SetGlobalTexture("_OC_CompositorTex", m_CompositorRT);
	}

	private void UpdatePosition(Camera camera)
	{
		int num = (int)Mathf.Floor(Mathf.Sqrt(volumetricClouds.particleCount));
		float num2 = volumetricClouds.cloudPlaneRadius * 2f / (float)num;
		Vector3 vector = new Vector3(camera.transform.position.x, 0f, camera.transform.position.z);
		vector += new Vector3(currentOriginOffset.x, 0f, currentOriginOffset.z);
		float num3 = volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier * 2f / (float)num;
		Vector3 vector2 = vector;
		Shader.SetGlobalVector("_OC_CellSpan", new Vector2(num2, num3));
		vector.x = Mathf.Round(vector.x / num2) * num2;
		vector.z = Mathf.Round(vector.z / num2) * num2;
		vector -= new Vector3(currentOriginOffset.x, 0f, currentOriginOffset.z);
		vector.y += adjustedCloudPlaneAltitude;
		m_CloudObject.transform.position = vector;
		m_CloudObject.transform.rotation = Quaternion.identity;
		vector2.x = Mathf.Round(vector2.x / num3) * num3;
		vector2.z = Mathf.Round(vector2.z / num3) * num3;
		vector2 -= new Vector3(currentOriginOffset.x, 0f, currentOriginOffset.z);
		vector2.y += adjustedCloudPlaneAltitude;
		m_LodObject.transform.position = vector2;
		m_LodObject.transform.rotation = Quaternion.identity;
		if (!m_Filter.sharedMesh || m_Filter.sharedMesh.vertexCount != num * num * 4 || Mathf.Abs(volumetricClouds.cloudPlaneRadius - m_LastRadius) > Mathf.Epsilon || Mathf.Abs(volumetricClouds.lodRadiusMultiplier - m_LastLodMultiplier) > Mathf.Epsilon)
		{
			InitializeMeshes();
		}
		m_LastLodMultiplier = volumetricClouds.lodRadiusMultiplier;
		m_LastRadius = volumetricClouds.cloudPlaneRadius;
		if (Vector3.Distance(vector, m_LastPos) > Mathf.Epsilon)
		{
			if (m_PropBlock == null)
			{
				m_PropBlock = new MaterialPropertyBlock();
			}
			m_Renderer.GetPropertyBlock(m_PropBlock);
			m_PropBlock.SetVector("_RandomRange", new Vector2(num2, num2));
			m_PropBlock.SetFloat("_NearRadius", 0f);
			m_PropBlock.SetFloat("_Radius", volumetricClouds.cloudPlaneRadius);
			m_PropBlock.SetFloat("_RadiusMax", current.cloudPlaneHeight);
			m_PropBlock.SetVector("_ParticleScale", new Vector2(1f, 1f));
			m_PropBlock.SetVector("_CloudPosition", m_CloudObject.transform.position);
			m_PropBlock.SetVector("_CloudExtents", Vector3.one * 1f / volumetricClouds.cloudPlaneRadius);
			m_Renderer.SetPropertyBlock(m_PropBlock);
			m_LastPos = vector;
		}
		if (Vector3.Distance(vector2, m_LastLodPos) > Mathf.Epsilon)
		{
			if (m_LodPropBlock == null)
			{
				m_LodPropBlock = new MaterialPropertyBlock();
			}
			m_LodRenderer.GetPropertyBlock(m_LodPropBlock);
			m_LodPropBlock.SetVector("_RandomRange", new Vector2(num3, num2));
			m_LodPropBlock.SetFloat("_NearRadius", volumetricClouds.cloudPlaneRadius);
			m_LodPropBlock.SetFloat("_Radius", volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier);
			m_LodPropBlock.SetFloat("_RadiusMax", current.cloudPlaneHeight);
			m_LodPropBlock.SetVector("_ParticleScale", new Vector2(volumetricClouds.lodParticleSize, 1f / volumetricClouds.lodParticleSize));
			m_LodPropBlock.SetVector("_CloudPosition", m_CloudObject.transform.position);
			m_LodPropBlock.SetVector("_CloudExtents", Vector3.one * 1f / (volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier));
			m_LodRenderer.SetPropertyBlock(m_LodPropBlock);
			m_LastLodPos = vector2;
		}
	}

	private void AzimuthialCoordiante(float RA, float Decl, out float phi, out float theta)
	{
		float f = m_LST - RA;
		float num = Mathf.Cos(f) * Mathf.Cos(Decl);
		float num2 = Mathf.Sin(f) * Mathf.Cos(Decl);
		float num3 = Mathf.Sin(Decl);
		float x = num * Mathf.Sin(timeOfDay.latitude * ((float)Math.PI / 180f)) - num3 * Mathf.Cos(timeOfDay.latitude * ((float)Math.PI / 180f));
		float y = num2;
		float f2 = num * Mathf.Cos(timeOfDay.latitude * ((float)Math.PI / 180f)) + num3 * Mathf.Sin(timeOfDay.latitude * ((float)Math.PI / 180f));
		float num4 = Mathf.Atan2(y, x) + (float)Math.PI;
		float num5 = Mathf.Asin(f2);
		phi = num4;
		theta = (float)Math.PI / 2f - num5;
	}

	private Vector3 CartesianCoordinate(float phi, float theta)
	{
		float num = Mathf.Cos(phi);
		float num2 = Mathf.Sin(phi);
		float y = Mathf.Cos(theta);
		float num3 = Mathf.Sin(theta);
		Vector3 result = default(Vector3);
		result.x = num2 * num3;
		result.y = y;
		result.z = num * num3;
		return result;
	}

	private void UpdateTime()
	{
		if (m_LastFrameTimeOfDay == null)
		{
			m_LastFrameTimeOfDay = new TimeOfDay();
		}
		m_LastFrameTimeOfDay.latitude = timeOfDay.latitude;
		m_LastFrameTimeOfDay.longitude = timeOfDay.longitude;
		m_LastFrameTimeOfDay.year = timeOfDay.year;
		m_LastFrameTimeOfDay.month = timeOfDay.month;
		m_LastFrameTimeOfDay.day = timeOfDay.day;
		m_LastFrameTimeOfDay.time = timeOfDay.time;
		bool flag = timeOfDay.play && (Application.isPlaying || timeOfDay.playInEditor);
		if (timeOfDay.useLocalTime)
		{
			DateTime now = DateTime.Now;
			timeOfDay.year = now.Year;
			timeOfDay.month = now.Month;
			timeOfDay.day = now.Day;
			timeOfDay.time = (float)now.Hour + (float)now.Minute / 1440f + (float)now.Millisecond / 86400000f;
		}
		else if (flag)
		{
			timeOfDay.Advance();
		}
		if (m_LastFrameTimeOfDay.latitude != timeOfDay.latitude || m_LastFrameTimeOfDay.longitude != timeOfDay.longitude || m_LastFrameTimeOfDay.year != timeOfDay.year || m_LastFrameTimeOfDay.month != timeOfDay.month || m_LastFrameTimeOfDay.day != timeOfDay.day || m_LastFrameTimeOfDay.time != timeOfDay.time)
		{
			skyChanged = true;
		}
	}

	private void UpdateOrbital()
	{
		if (timeOfDay.enable)
		{
			float dayNumber = timeOfDay.dayNumber;
			float f = (23.4393f - 3.563E-07f * dayNumber) * ((float)Math.PI / 180f);
			if ((bool)components.sun)
			{
				float num = (282.9404f + 4.70935E-05f * dayNumber) * ((float)Math.PI / 180f);
				float num2 = 0.016709f - 1.151E-09f * dayNumber;
				float num3 = (356.047f + 0.98560023f * dayNumber) * ((float)Math.PI / 180f);
				float f2 = num3 + num2 * Mathf.Sin(num3) * (1f + num2 * Mathf.Cos(num3));
				float num4 = 1f * (Mathf.Cos(f2) - num2);
				float num5 = 1f * (Mathf.Sqrt(1f - num2 * num2) * Mathf.Sin(f2));
				float num6 = Mathf.Atan2(num5, num4);
				float num7 = Mathf.Sqrt(num4 * num4 + num5 * num5);
				float num8 = num6 + num;
				float num9 = (float)((double)(num8 * 57.29578f + 180f) + timeOfDay.time * 15.0);
				m_LST = (num9 + timeOfDay.longitude) * ((float)Math.PI / 180f);
				float num10 = num7 * Mathf.Cos(num8);
				float num11 = num7 * Mathf.Sin(num8);
				float x = num10;
				float y = num11 * Mathf.Cos(f);
				float f3 = num11 * Mathf.Sin(f);
				float rA = Mathf.Atan2(y, x);
				float decl = Mathf.Asin(f3);
				AzimuthialCoordiante(rA, decl, out var phi, out var theta);
				components.sun.transform.forward = CartesianCoordinate(phi, theta) * -1f;
			}
			if ((bool)components.moon && timeOfDay.affectsMoon)
			{
				float f4 = (125.1228f - 0.05295381f * dayNumber) * ((float)Math.PI / 180f);
				float f5 = 0.08980417f;
				float num12 = (318.0634f + 0.16435732f * dayNumber) * ((float)Math.PI / 180f);
				float num13 = 0.0549f;
				float num14 = (115.3654f + 13.064993f * dayNumber) * ((float)Math.PI / 180f);
				float f6 = num14 + num13 * Mathf.Sin(num14) * (1f + num13 * Mathf.Cos(num14));
				float num15 = 60.2666f * (Mathf.Cos(f6) - num13);
				float num16 = 60.2666f * (Mathf.Sqrt(1f - num13 * num13) * Mathf.Sin(f6));
				float num17 = Mathf.Atan2(num16, num15);
				float num18 = Mathf.Sqrt(num15 * num15 + num16 * num16);
				float num19 = num18 * (Mathf.Cos(f4) * Mathf.Cos(num17 + num12) - Mathf.Sin(f4) * Mathf.Sin(num17 + num12) * Mathf.Cos(f5));
				float num20 = num18 * (Mathf.Sin(f4) * Mathf.Cos(num17 + num12) + Mathf.Cos(f4) * Mathf.Sin(num17 + num12) * Mathf.Cos(f5));
				float num21 = num18 * (Mathf.Sin(num17 + num12) * Mathf.Sin(f5));
				float num22 = num19;
				float num23 = num20 * Mathf.Cos(f) - num21 * Mathf.Sin(f);
				float y2 = num20 * Mathf.Sin(f) + num21 * Mathf.Cos(f);
				float rA2 = Mathf.Atan2(num23, num22);
				float decl2 = Mathf.Atan2(y2, Mathf.Sqrt(num22 * num22 + num23 * num23));
				AzimuthialCoordiante(rA2, decl2, out var phi2, out var theta2);
				components.moon.transform.forward = CartesianCoordinate(phi2, theta2) * -1f;
			}
		}
	}

	public static void SetWeatherPreset(string preset)
	{
		SetWeatherPreset(preset, instance.fadeDuration);
	}

	public static void SetWeatherPreset(string preset, float fadeDuration)
	{
		instance.fadeDuration = fadeDuration;
		if ((bool)instance)
		{
			instance.activePreset = preset;
		}
	}

	private void QuicksortTriangles(int left, int right)
	{
		int i = left;
		int num = right;
		float num2 = distances[(i + num) / 2];
		while (i <= num)
		{
			for (; distances[i] < num2; i++)
			{
			}
			while (distances[num] > num2)
			{
				num--;
			}
			if (i <= num)
			{
				float num3 = distances[i];
				distances[i] = distances[num];
				distances[num] = num3;
				t0 = triangles[i * 6];
				t1 = triangles[i * 6 + 1];
				t2 = triangles[i * 6 + 2];
				t3 = triangles[i * 6 + 3];
				t4 = triangles[i * 6 + 4];
				t5 = triangles[i * 6 + 5];
				triangles[i * 6] = triangles[num * 6];
				triangles[i * 6 + 1] = triangles[num * 6 + 1];
				triangles[i * 6 + 2] = triangles[num * 6 + 2];
				triangles[i * 6 + 3] = triangles[num * 6 + 3];
				triangles[i * 6 + 4] = triangles[num * 6 + 4];
				triangles[i * 6 + 5] = triangles[num * 6 + 5];
				triangles[num * 6] = t0;
				triangles[num * 6 + 1] = t1;
				triangles[num * 6 + 2] = t2;
				triangles[num * 6 + 3] = t3;
				triangles[num * 6 + 4] = t4;
				triangles[num * 6 + 5] = t5;
				i++;
				num--;
			}
		}
		if (left < num)
		{
			QuicksortTriangles(left, num);
		}
		if (i < right)
		{
			QuicksortTriangles(i, right);
		}
	}

	public Mesh SortTriangles(Mesh mesh)
	{
		Vector3 zero = Vector3.zero;
		vertices = mesh.vertices;
		triangles = mesh.triangles;
		distances = new float[mesh.vertices.Length / 4];
		int num = 0;
		for (int i = 0; i < triangles.Length; i += 6)
		{
			distances[num] = 0f - Vector3.Distance(zero, (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]] + vertices[triangles[i + 3]] + vertices[triangles[i + 4]] + vertices[triangles[i + 5]]) * 0.16666f);
			num++;
		}
		QuicksortTriangles(0, distances.Length - 1);
		mesh.triangles = triangles;
		return mesh;
	}

	private static float smoothstep(float edge0, float edge1, float x)
	{
		float num = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
		return num * num * (3f - 2f * num);
	}

	private static float GetDensity2D(Vector3 worldPos)
	{
		worldPos += currentOriginOffset;
		Vector2 vector = (new Vector2(worldPos.x, worldPos.z) + new Vector2(1f, 0f) * weather.windTime) * _OC_NoiseScale.value.x;
		float r = volumetricClouds.noiseTexture.GetPixelBilinear(vector.x, vector.y).r;
		r = Mathf.Max(r - (1f - _OC_Cloudiness.value.x), 0f);
		r = Mathf.Lerp(_OC_Cloudiness.value.y, 1f, r);
		r = smoothstep(_OC_CloudSharpness.value.x * 0.499f, 1f - _OC_CloudSharpness.value.x * 0.499f, r);
		vector = (new Vector2(worldPos.x, worldPos.z) + new Vector2(1f, 0f) * weather.windTime) * _OC_NoiseScale.value.y;
		float g = volumetricClouds.noiseTexture.GetPixelBilinear(vector.x, vector.y).g;
		g = Mathf.Max(g - (1f - _OC_Cloudiness.value.z), 0f);
		g = Mathf.Lerp(_OC_Cloudiness.value.w, 1f, g);
		g = smoothstep(_OC_CloudSharpness.value.y * 0.499f, 1f - _OC_CloudSharpness.value.y * 0.499f, g);
		r *= g;
		r = smoothstep(0.1f, 0.5f, r);
		return r * Mathf.Min(current.opticalDensity, 1f);
	}

	private static float GetDensity3D(Vector3 worldPos, out float density2D)
	{
		density2D = smoothstep(volumetricClouds.noiseSettings.alphaEdgeLower, volumetricClouds.noiseSettings.alphaEdgeUpper, GetDensity2D(worldPos));
		float result = 0f;
		if (density2D > Mathf.Epsilon && IsInsideCloudVolume(worldPos))
		{
			float num = worldPos.y - (_OC_CloudAltitude.value - _OC_CloudHeight.value);
			num *= _OC_CloudHeightInv.value * 0.5f;
			num = ((!(num < _OC_ShapeParams.value.x)) ? (1f - (num - _OC_ShapeParams.value.x) * _OC_ShapeParams.value.z * 1.5f) : (num * _OC_ShapeParams.value.y));
			result = density2D * num;
		}
		return result;
	}

	private static float GetDensity3D(Vector3 worldPos)
	{
		float density2D;
		return GetDensity3D(worldPos, out density2D);
	}

	private static float Radius(float density)
	{
		return current.cloudPlaneHeight * density;
	}

	public static CloudDensity GetDensity(Vector3 worldPos)
	{
		CloudDensity result = default(CloudDensity);
		if (!instance)
		{
			return result;
		}
		float density2D;
		float num = (result.density = GetDensity3D(worldPos, out density2D));
		result.coverage = density2D;
		result.rain = density2D * ((worldPos.y < current.cloudPlaneAltitude) ? 1f : num) * instance.m_CurrentPreset.precipitation;
		return result;
	}

	public static bool IsAboveCloudVolume(Vector3 position)
	{
		return position.y > adjustedCloudPlaneAltitude + current.cloudPlaneHeight;
	}

	public static bool IsBelowCloudVolume(Vector3 position)
	{
		return position.y < adjustedCloudPlaneAltitude - current.cloudPlaneHeight;
	}

	public static bool IsInsideCloudVolume(Vector3 position)
	{
		return Mathf.Abs(position.y - adjustedCloudPlaneAltitude) < current.cloudPlaneHeight;
	}

	public static float CloudVisibility(Vector3 p0, Vector3 p1, int sampleCount = 32)
	{
		float num = adjustedCloudPlaneAltitude + current.cloudPlaneHeight * 0.5f;
		float num2 = adjustedCloudPlaneAltitude - current.cloudPlaneHeight * 0.5f;
		if ((p0.y < num2 && p1.y < num2) || (p0.y > num && p1.y > num))
		{
			return 0f;
		}
		if (p0.y > num || p0.y < num2)
		{
			Vector3 normalized = (p1 - p0).normalized;
			if (p0.y > num)
			{
				p0 += normalized * ((p0.y - num) / Mathf.Abs(normalized.y));
			}
			else
			{
				p0 -= normalized * ((p0.y - num2) / Mathf.Abs(normalized.y));
			}
		}
		if (p1.y > num || p1.y < num2)
		{
			Vector3 normalized2 = (p0 - p1).normalized;
			if (p1.y > num)
			{
				p1 += normalized2 * ((p1.y - num) / Mathf.Abs(normalized2.y));
			}
			else
			{
				p1 -= normalized2 * ((p1.y - num2) / Mathf.Abs(normalized2.y));
			}
		}
		float num3 = Vector3.Distance(p0, p1) / (current.cloudPlaneHeight * 0.5f) / (float)sampleCount;
		float num4 = 0f;
		for (int i = 0; i < sampleCount; i++)
		{
			float x = GetDensity3D(Vector3.Lerp(p0, p1, (float)i / (float)(sampleCount - 1))) * _OC_CloudDensity.value.x;
			num4 += smoothstep(_OC_AlphaEdgeParams.value.x, _OC_AlphaEdgeParams.value.y, x);
		}
		num4 *= num3;
		return Mathf.Clamp01(num4);
	}

	public static float GetCustomFloat(string name)
	{
		return current.GetCustomFloat(name);
	}

	public int GetCustomFloatIndex(string name)
	{
		for (int i = 0; i < m_CustomFloats.Length; i++)
		{
			if (m_CustomFloats[i].name == name)
			{
				return i;
			}
		}
		return -1;
	}

	public string GetCustomFloatName(int index)
	{
		return m_CustomFloats[index].name;
	}

	public void SetCustomFloatName(int index, string name)
	{
		m_CustomFloats[index].name = name;
	}

	public string GetCustomFloatShaderParameter(int index)
	{
		return m_CustomFloats[index].shaderParameter;
	}

	public void SetCustomFloatShaderParameter(int index, string shaderParameter)
	{
		m_CustomFloats[index].shaderParameter = shaderParameter;
	}

	public void AddCustomFloat()
	{
		CustomFloat item = default(CustomFloat);
		item.name = "MyCustomFloat";
		item.shaderParameter = "_MyCustomFloat";
		List<CustomFloat> list = new List<CustomFloat>(m_CustomFloats);
		list.Add(item);
		m_CustomFloats = list.ToArray();
		m_CurrentPreset.AddCustomFloat();
		m_PrevPreset.AddCustomFloat();
		m_LastFramePreset.AddCustomFloat();
		for (int i = 0; i < m_Presets.Length; i++)
		{
			m_Presets[i].AddCustomFloat();
		}
		drawerSelectedCustomFloat = m_CustomFloats.Length - 1;
	}

	public void DeleteCustomFloat()
	{
		if (drawerSelectedCustomFloat >= 0 && drawerSelectedCustomFloat <= m_CustomFloats.Length)
		{
			List<CustomFloat> list = new List<CustomFloat>(m_CustomFloats);
			list.RemoveAt(drawerSelectedCustomFloat);
			m_CustomFloats = list.ToArray();
			m_CurrentPreset.DeleteCustomFloat(drawerSelectedCustomFloat);
			m_PrevPreset.DeleteCustomFloat(drawerSelectedCustomFloat);
			m_LastFramePreset.DeleteCustomFloat(drawerSelectedCustomFloat);
			for (int i = 0; i < m_Presets.Length; i++)
			{
				m_Presets[i].DeleteCustomFloat(drawerSelectedCustomFloat);
			}
			if (m_CustomFloats.Length < 1)
			{
				drawerSelectedCustomFloat = -1;
			}
			else
			{
				drawerSelectedCustomFloat = Mathf.Max(drawerSelectedCustomFloat - 1, 0);
			}
		}
	}

	public void AddCloudPlane()
	{
		CloudPlane cloudPlane = null;
		if (drawerSelectedCloudPlane > -1 && drawerSelectedCloudPlane < m_CloudPlanes.Length)
		{
			cloudPlane = m_CloudPlanes[drawerSelectedCloudPlane];
		}
		List<CloudPlane> list = new List<CloudPlane>(m_CloudPlanes);
		if (cloudPlane != null)
		{
			list.Add(new CloudPlane(cloudPlane));
		}
		else
		{
			list.Add(new CloudPlane("2D Cloud Plane"));
		}
		m_CloudPlanes = list.ToArray();
		drawerSelectedCloudPlane = m_CloudPlanes.Length - 1;
	}

	public void DeleteCloudPlane()
	{
		List<CloudPlane> list = new List<CloudPlane>(m_CloudPlanes);
		list.RemoveAt(drawerSelectedCloudPlane);
		m_CloudPlanes = list.ToArray();
		drawerSelectedCloudPlane = m_CloudPlanes.Length - 1;
	}

	public static void ForceSkyChanged()
	{
		skyChanged = true;
	}

	public void AddWeatherPreset()
	{
		List<WeatherPreset> list = new List<WeatherPreset>(m_Presets);
		WeatherPreset weatherPreset = new WeatherPreset("New Weather Preset");
		weatherPreset.customFloats = new float[m_CustomFloats.Length];
		for (int i = 0; i < m_CustomFloats.Length; i++)
		{
			weatherPreset.customFloats[i] = 0f;
		}
		list.Add(weatherPreset);
		m_Presets = list.ToArray();
	}

	public void InitializeNoise(bool forceRegenerate = false)
	{
		CloudNoiseGen.perlin = volumetricClouds.noiseGeneration.perlin;
		CloudNoiseGen.worley = volumetricClouds.noiseGeneration.worley;
		if (CloudNoiseGen.InitializeNoise(ref m_3DNoise, "OverCloud", (int)volumetricClouds.noiseGeneration.resolution, forceRegenerate ? CloudNoiseGen.Mode.ForceGenerate : CloudNoiseGen.Mode.LoadAvailableElseGenerate))
		{
			Shader.SetGlobalTexture("_OC_3DNoiseTex", m_3DNoise);
		}
		else
		{
			Debug.LogError("Fatal: Failed to load/initialize 3D noise texture.");
		}
	}

	public void UpdateNoisePreview()
	{
	}
}}
