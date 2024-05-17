using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OC{

[ExecuteInEditMode]
public class OverCloudFogLight : MonoBehaviour
{
	public static List<OverCloudFogLight> fogLights;

	private Light _light;

	[SerializeField]
	[Tooltip("The sphere mesh used to render the fog light. Don't change this!")]
	public Mesh m_Mesh;

	[SerializeField]
	[Tooltip("The material used to render the fog light. Don't change this!")]
	public Material m_Material;

	[Range(0f, 1f)]
	[Tooltip("Intensity multiplier for the effect.")]
	public float intensity = 1f;

	[Range(0f, 1f)]
	[Tooltip("Minimum fog density used for the effect. Can be used to force the effect to show up even when there is no fog.")]
	public float minimumDensity;

	[Range(0f, 1f)]
	[Tooltip("Controls the falloff of the effect from the camera.")]
	public float attenuationFactor = 1f;

	[Range(4f, 128f)]
	public int raymarchSteps = 16;

	private MaterialPropertyBlock m_Prop;

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

	private void OnEnable()
	{
		if (fogLights == null)
		{
			fogLights = new List<OverCloudFogLight>();
		}
		fogLights.Add(this);
	}

	private void OnDisable()
	{
		fogLights.Remove(this);
	}

	private void UpdateProperties()
	{
		m_Prop.SetColor("_Color", light.color * light.intensity);
		m_Prop.SetVector("_Center", base.transform.position);
		m_Prop.SetVector("_Params", new Vector4(light.range, 1f / light.range, intensity, attenuationFactor));
		m_Prop.SetVector("_Params2", new Vector4(Mathf.Pow(minimumDensity, 16f), 0f, 0f, 0f));
		if (light.type == LightType.Spot)
		{
			_ = light.transform.forward;
			m_Prop.SetVector("_SpotParams", light.transform.forward);
			float num = 1f - Mathf.Cos(light.spotAngle * ((float)Math.PI / 180f) * 0.5f);
			m_Prop.SetVector("_SpotParams2", new Vector3(1f - num, 1f / num, 0f));
		}
		m_Prop.SetVector("_RaymarchSteps", new Vector2(raymarchSteps, 1f / (float)raymarchSteps));
	}

	public void BufferRender(CommandBuffer buffer)
	{
		if ((bool)light)
		{
			if (m_Prop == null)
			{
				m_Prop = new MaterialPropertyBlock();
			}
			int num = -1;
			if (light.type == LightType.Point)
			{
				num = 0;
			}
			else if (light.type == LightType.Spot)
			{
				num = 1;
			}
			if (num >= 0 && (bool)m_Mesh && (bool)m_Material)
			{
				UpdateProperties();
				buffer.DrawMesh(m_Mesh, Matrix4x4.TRS(base.transform.position, base.transform.rotation, Vector3.one * light.range), m_Material, 0, num, m_Prop);
			}
		}
	}
}
}