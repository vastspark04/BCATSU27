using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace OC{

[ExecuteInEditMode]
[RequireComponent(typeof(ReflectionProbe))]
public class OverCloudReflectionProbe : MonoBehaviour
{
	[Serializable]
	public enum UpdateMode
	{
		OnSkyChanged,
		OnEnable,
		Realtime,
		ScriptOnly
	}

	[Serializable]
	public enum SpreadMode
	{
		_7Frames,
		_1Frame
	}

	private static Quaternion[] orientations = new Quaternion[6]
	{
		Quaternion.LookRotation(Vector3.right, Vector3.down),
		Quaternion.LookRotation(Vector3.left, Vector3.down),
		Quaternion.LookRotation(Vector3.up, Vector3.forward),
		Quaternion.LookRotation(Vector3.down, Vector3.back),
		Quaternion.LookRotation(Vector3.forward, Vector3.down),
		Quaternion.LookRotation(Vector3.back, Vector3.down)
	};

	private static Quaternion[] orientationsFlipped = new Quaternion[6]
	{
		Quaternion.LookRotation(Vector3.right, Vector3.up),
		Quaternion.LookRotation(Vector3.left, Vector3.up),
		Quaternion.LookRotation(Vector3.up, Vector3.back),
		Quaternion.LookRotation(Vector3.down, Vector3.forward),
		Quaternion.LookRotation(Vector3.forward, Vector3.up),
		Quaternion.LookRotation(Vector3.back, Vector3.up)
	};

	private Camera m_Camera;

	private OverCloudCamera m_OverCloudCamera;

	private ReflectionProbe m_Probe;

	private RenderTexture m_Result;

	private RenderTexture m_CubeMap;

	private Material m_TransferMaterial;

	private int m_CurrentFace = -1;

	[Tooltip("If and when the reflection probe should be updated. If set to ScriptOnly, the reflection probe will not render unless RenderProbe is manually called.")]
	public UpdateMode updateMode;

	[Tooltip("How many frames to spread the reflection probe update over. 1 frame will make the result available immediately after calling RenderProbe. 7 frames will spread the render work over the first 6 frames, and calculate mip maps on the 7th.")]
	public SpreadMode spreadMode;

	[Header("General")]
	[Tooltip("The level of downsampling to use when rendering the volumetric clouds and volumetric lighting. This enables you to render the effects at 1/2, 1/4 or 1/8 resolution and can give you a big performance boost in exchange for fidelity.")]
	public DownSampleFactor downsampleFactor = DownSampleFactor.Half;

	[Header("Volumetric Clouds")]
	[Tooltip("Toggle the rendering of the volumetric clouds.")]
	public bool renderVolumetricClouds = true;

	[Tooltip("Toggle the rendering of the 2D fallback cloud plane for the volumetric clouds.")]
	public bool render2DFallback = true;

	[Tooltip("The number of samples to use when ray-marching the lighting for the volumetric clouds. A higher value will look nicer at the cost of performance.")]
	public SampleCount lightSampleCount;

	[Tooltip("Use the high-resolution 3D noise for the light ray-marching for the volumetric clouds, which is normally only used for the alpha.")]
	public bool highQualityClouds;

	[Tooltip("Downsample the 2D clouds along with the volumetric ones. Can save performance at the cost of fidelity, especially around the horizon.")]
	public bool downsample2DClouds;

	[Header("Atmosphere")]
	[Tooltip("Toggle the rendering of atmospheric scattering and fog.")]
	public bool renderAtmosphere = true;

	[Tooltip("Enable the scattering mask (god rays).")]
	public bool renderScatteringMask;

	[Tooltip("Include the cascaded shadow map in the scattering mask.")]
	public bool includeCascadedShadows = true;

	[Tooltip("How many samples the scattering mask should use when rendering. More results in higher quality but slower rendering.")]
	public SampleCount scatteringMaskSamples = SampleCount.Normal;

	[Header("Weather")]
	[Tooltip("Enable the rain height mask.")]
	public bool renderRainMask;

	[Header("Camera Settings")]
	[Tooltip("Enable rendering of shadows in the reflection probe (shadows need to be enabled in quality settings also).")]
	public bool enableShadows;

	[Header("Misc")]
	[Tooltip("Print debug information in the console.")]
	public bool debug;

	[Header("Cubemap Saving")]
	[Tooltip("The file path to store the cubemap .exr when saving (the filename will be OverCloudReflectionProbe.exr).")]
	public string filePath = "";

	private bool m_FlippedRendering;

	public bool hasFinishedRendering { get; private set; }

	private void OnEnable()
	{
		Initialize();
		RenderProbe();
	}

	private void OnValidate()
	{
		Reset();
	}

	private void Initialize()
	{
		if (!m_Camera)
		{
			GameObject gameObject = new GameObject("ReflectionCamera");
			gameObject.transform.parent = base.transform;
			gameObject.hideFlags = HideFlags.HideAndDontSave;
			m_Camera = gameObject.AddComponent<Camera>();
			m_Camera.enabled = false;
			m_Camera.tag = "Untagged";
			m_OverCloudCamera = m_Camera.gameObject.AddComponent<OverCloudCamera>();
		}
		m_Probe = GetComponent<ReflectionProbe>();
		RenderTextureDescriptor desc = new RenderTextureDescriptor(m_Probe.resolution, m_Probe.resolution, RenderTextureFormat.DefaultHDR);
		desc.useMipMap = true;
		desc.autoGenerateMips = false;
		desc.depthBufferBits = 0;
		m_Result = new RenderTexture(desc);
		m_Result.dimension = TextureDimension.Cube;
		m_Result.antiAliasing = 1;
		m_Result.filterMode = FilterMode.Trilinear;
		m_CubeMap = new RenderTexture(desc);
		m_CubeMap.dimension = TextureDimension.Cube;
		m_CubeMap.antiAliasing = 1;
		m_CubeMap.filterMode = FilterMode.Trilinear;
		Graphics.Blit(Texture2D.whiteTexture, m_Result);
		Graphics.Blit(Texture2D.whiteTexture, m_CubeMap);
		m_Result.GenerateMips();
		m_CubeMap.GenerateMips();
		m_Probe.mode = ReflectionProbeMode.Custom;
		m_Probe.customBakedTexture = m_CubeMap;
		m_TransferMaterial = new Material(Shader.Find("Hidden/OverCloud/Utilities"));
	}

	public void RenderProbe()
	{
		hasFinishedRendering = false;
		m_CurrentFace = -1;
		if (spreadMode == SpreadMode._1Frame)
		{
			RenderUpdate();
		}
	}

	private void OnDisable()
	{
		m_Probe.mode = ReflectionProbeMode.Baked;
	}

	private void Update()
	{
		if (Application.isPlaying)
		{
			RenderUpdate();
		}
	}

	private void RenderUpdate()
	{
		if (m_CubeMap.width != m_Probe.resolution)
		{
			Initialize();
			Reset();
		}
		if (m_CurrentFace > 6)
		{
			if (updateMode != 0 || !OverCloud.skyChanged)
			{
				return;
			}
			m_CurrentFace = -1;
		}
		if (m_CurrentFace < 0)
		{
			Reset();
		}
		if (!m_Camera)
		{
			Initialize();
		}
		while (m_CurrentFace < 6)
		{
			hasFinishedRendering = false;
			if (debug)
			{
				Debug.Log("OverCloudReflectionProbe: Updating face " + (m_CurrentFace + 1) + ".");
			}
			UpdateCamera();
			Shader.EnableKeyword("OVERCLOUD_REFLECTION");
			ShadowQuality shadows = QualitySettings.shadows;
			if (!enableShadows)
			{
				QualitySettings.shadows = ShadowQuality.Disable;
			}
			RenderTexture temporary = RenderTexture.GetTemporary(new RenderTextureDescriptor(m_CubeMap.width, m_CubeMap.height, m_CubeMap.format, 16)
			{
				useMipMap = false
			});
			m_Camera.targetTexture = temporary;
			m_Camera.Render();
			QualitySettings.shadows = shadows;
			Shader.DisableKeyword("OVERCLOUD_REFLECTION");
			RenderTexture active = RenderTexture.active;
			Graphics.SetRenderTarget(face: (CubemapFace)m_CurrentFace, rt: m_CubeMap, mipLevel: 0);
			m_TransferMaterial.SetTexture("_MainTex", temporary);
			m_TransferMaterial.SetInt("_Flip", (!m_FlippedRendering) ? 1 : 0);
			Graphics.Blit(temporary, m_TransferMaterial, 2);
			RenderTexture.ReleaseTemporary(temporary);
			RenderTexture.active = active;
			m_CurrentFace++;
			if (spreadMode == SpreadMode._7Frames)
			{
				return;
			}
		}
		if (debug)
		{
			Debug.Log("OverCloudReflectionProbe: Generating mip maps.");
		}
		m_CubeMap.GenerateMips();
		if (updateMode == UpdateMode.Realtime)
		{
			m_CurrentFace = -1;
		}
		else
		{
			m_CurrentFace++;
		}
		hasFinishedRendering = true;
	}

	private void Reset()
	{
		m_CurrentFace = 0;
	}

	private void UpdateCamera()
	{
		m_Camera.transform.position = base.transform.position;
		if (m_FlippedRendering)
		{
			m_Camera.transform.rotation = orientationsFlipped[m_CurrentFace];
		}
		else
		{
			m_Camera.transform.rotation = orientations[m_CurrentFace];
		}
		m_Camera.cameraType = CameraType.Reflection;
		m_Camera.fieldOfView = 90f;
		m_Camera.cameraType = CameraType.Reflection;
		m_Camera.farClipPlane = m_Probe.farClipPlane;
		m_Camera.nearClipPlane = m_Probe.nearClipPlane;
		m_Camera.cullingMask = m_Probe.cullingMask;
		m_Camera.clearFlags = (CameraClearFlags)m_Probe.clearFlags;
		m_Camera.backgroundColor = m_Probe.backgroundColor;
		m_Camera.allowHDR = m_Probe.hdr;
		m_Camera.allowMSAA = false;
		m_OverCloudCamera.renderVolumetricClouds = renderVolumetricClouds;
		m_OverCloudCamera.render2DFallback = render2DFallback;
		m_OverCloudCamera.renderAtmosphere = renderAtmosphere;
		m_OverCloudCamera.renderScatteringMask = renderScatteringMask;
		m_OverCloudCamera.includeCascadedShadows = includeCascadedShadows;
		m_OverCloudCamera.scatteringMaskSamples = scatteringMaskSamples;
		m_OverCloudCamera.renderRainMask = renderRainMask;
		m_OverCloudCamera.downsampleFactor = downsampleFactor;
		m_OverCloudCamera.lightSampleCount = lightSampleCount;
		m_OverCloudCamera.highQualityClouds = highQualityClouds;
		m_OverCloudCamera.downsample2DClouds = downsample2DClouds;
	}

	public void SaveCubemap()
	{
		SpreadMode spreadMode = this.spreadMode;
		RenderTexture active = RenderTexture.active;
		if (filePath == "")
		{
			filePath = Application.dataPath + "/";
		}
		this.spreadMode = SpreadMode._1Frame;
		m_CurrentFace = -1;
		m_FlippedRendering = true;
		RenderUpdate();
		m_FlippedRendering = false;
		Texture2D texture2D = new Texture2D(m_CubeMap.width * 6, m_CubeMap.height, TextureFormat.RGBAHalf, mipChain: false, linear: false);
		Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.PositiveX);
		Rect source = new Rect(0f, 0f, m_CubeMap.width, m_CubeMap.height);
		_ = m_CubeMap.width;
		texture2D.ReadPixels(source, 0, 0);
		Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.NegativeX);
		texture2D.ReadPixels(new Rect(0f, 0f, m_CubeMap.width, m_CubeMap.height), m_CubeMap.width, 0);
		Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.PositiveY);
		texture2D.ReadPixels(new Rect(0f, 0f, m_CubeMap.width, m_CubeMap.height), m_CubeMap.width * 2, 0);
		Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.NegativeY);
		texture2D.ReadPixels(new Rect(0f, 0f, m_CubeMap.width, m_CubeMap.height), m_CubeMap.width * 3, 0);
		Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.PositiveZ);
		texture2D.ReadPixels(new Rect(0f, 0f, m_CubeMap.width, m_CubeMap.height), m_CubeMap.width * 4, 0);
		Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.NegativeZ);
		texture2D.ReadPixels(new Rect(0f, 0f, m_CubeMap.width, m_CubeMap.height), m_CubeMap.width * 5, 0);
		File.WriteAllBytes(bytes: texture2D.EncodeToEXR(), path: filePath + "OverCloudReflectionProbe.exr");
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(texture2D);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(texture2D);
		}
		Debug.Log("Cubemap saved to " + filePath);
		RenderProbe();
		RenderTexture.active = active;
		this.spreadMode = spreadMode;
	}
}
}