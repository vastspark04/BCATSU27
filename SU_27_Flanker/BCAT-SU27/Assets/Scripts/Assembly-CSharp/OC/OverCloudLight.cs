using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OC{

[ExecuteInEditMode]
[RequireComponent(typeof(Light))]
public class OverCloudLight : MonoBehaviour
{
	[Serializable]
	public enum Type
	{
		Point,
		Sun,
		Moon
	}

	private Light _light;

	[Tooltip("The light type (sun or moon).")]
	[SerializeField]
	private Type m_Type;

	[Tooltip("A gradient which describes the color of the light over time. Sort of. In actuality, it uses the elevation of the light source as the input for the gradient evaluation. This means that the color value at the location 0% will be used when the light is facing straight upwards, 50% will be used when the light is exactly on the horizon and 100% will be used when the light is pointing straight downwards.")]
	[SerializeField]
	private Gradient m_ColorOverTime;

	[Tooltip("A multiplier to apply on top of the evaluated color.")]
	public float multiplier = 1f;

	private CommandBuffer m_CascadeBuffer;

	private CommandBuffer m_ShadowsBuffer;

	private Material m_ShadowMaterial;

	private bool m_BufferInitialized;

	private bool m_ShadowBufferInitialized;

	public static List<OverCloudLight> lights { get; private set; }

	public bool hasActiveLight
	{
		get
		{
			if (light.enabled && light.intensity > Mathf.Epsilon)
			{
				return light.color != Color.black;
			}
			return false;
		}
	}

	public float pointRadius => light.range;

	public Color pointColor => light.color * light.intensity;

	public Light light
	{
		get
		{
			if (!_light)
			{
				_light = GetComponent<Light>();
			}
			return _light;
		}
	}

	public Type type => m_Type;

	private void OnEnable()
	{
		if (lights == null)
		{
			lights = new List<OverCloudLight>();
		}
		lights.Add(this);
	}

	private void OnDisable()
	{
		lights.Remove(this);
		ClearBuffers();
	}

	private void InitializeBuffers()
	{
		if (!m_BufferInitialized)
		{
			if (m_CascadeBuffer == null)
			{
				m_CascadeBuffer = new CommandBuffer();
				m_CascadeBuffer.name = "CascadeShadowCopy";
				m_CascadeBuffer.SetGlobalTexture("_CascadeShadowMapTexture", new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));
				m_CascadeBuffer.SetGlobalFloat("_CascadeShadowMapPresent", 1f);
			}
			light.AddCommandBuffer(LightEvent.AfterShadowMap, m_CascadeBuffer);
			m_BufferInitialized = true;
		}
	}

	private void InitializeShadowBuffer()
	{
		if (m_ShadowBufferInitialized)
		{
			return;
		}
		if (m_ShadowsBuffer == null)
		{
			m_ShadowsBuffer = new CommandBuffer();
			m_ShadowsBuffer.name = "CloudShadows";
			if (!m_ShadowMaterial)
			{
				m_ShadowMaterial = new Material(Shader.Find("Hidden/OverCloud/Atmosphere"));
			}
			m_ShadowsBuffer.SetGlobalVector("_LightShadowData", new Vector4(light.shadowStrength, 0f, 0f, light.shadowNearPlane));
			m_ShadowsBuffer.DrawMesh(OverCloud.quad, Matrix4x4.identity, m_ShadowMaterial, 0, 5);
		}
		light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, m_ShadowsBuffer);
		m_ShadowBufferInitialized = true;
	}

	private void ClearBuffers()
	{
		if (m_BufferInitialized)
		{
			if (m_CascadeBuffer != null)
			{
				light.RemoveCommandBuffer(LightEvent.AfterShadowMap, m_CascadeBuffer);
				m_CascadeBuffer = null;
			}
			m_BufferInitialized = false;
		}
	}

	private void ClearShadowBuffer()
	{
		if (m_ShadowBufferInitialized)
		{
			if (m_ShadowsBuffer != null)
			{
				light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, m_ShadowsBuffer);
				m_ShadowsBuffer = null;
			}
			m_ShadowBufferInitialized = false;
		}
	}

	public void UpdateBuffers()
	{
		if (!m_BufferInitialized)
		{
			InitializeBuffers();
		}
		if (!m_ShadowBufferInitialized)
		{
			InitializeShadowBuffer();
		}
	}

	private void Update()
	{
		if ((bool)OverCloud.instance)
		{
			if (m_Type != 0)
			{
				float time = Vector3.Dot(light.transform.forward, Vector3.down) * 0.5f + 0.5f;
				Type type = m_Type;
				if (type == Type.Sun || type != Type.Moon)
				{
					light.color = Color.Lerp(m_ColorOverTime.Evaluate(time), OverCloud.atmosphere.solarEclipseColor, OverCloud.solarEclipse) * multiplier;
				}
				else
				{
					light.color = Color.Lerp(m_ColorOverTime.Evaluate(time), OverCloud.atmosphere.lunarEclipseColor, OverCloud.lunarEclipse) * multiplier * OverCloud.moonFade;
				}
				light.enabled = light == OverCloud.dominantLight && (light.color.r != 0f || light.color.g != 0f || light.color.b != 0f);
				if (light.enabled && !m_BufferInitialized)
				{
					InitializeBuffers();
				}
				else if (!light.enabled && m_BufferInitialized)
				{
					ClearBuffers();
				}
				if (light.enabled && OverCloud.lighting.cloudShadows.mode == CloudShadowsMode.Injected && !m_ShadowBufferInitialized)
				{
					InitializeShadowBuffer();
				}
				else if ((!light.enabled || OverCloud.lighting.cloudShadows.mode != 0) && m_ShadowBufferInitialized)
				{
					ClearShadowBuffer();
				}
			}
		}
		else if (m_BufferInitialized)
		{
			ClearBuffers();
			ClearShadowBuffer();
		}
	}
}}
