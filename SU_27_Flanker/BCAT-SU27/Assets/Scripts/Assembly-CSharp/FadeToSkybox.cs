using System;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Rendering/Fade To Skybox")]
public class FadeToSkybox : MonoBehaviour
{
	[SerializeField]
	private bool _useRadialDistance;

	[SerializeField]
	private float _startDistance;

	[SerializeField]
	private Shader _fogShader;

	private Material _fogMaterial;

	public bool useRadialDistance
	{
		get
		{
			return _useRadialDistance;
		}
		set
		{
			_useRadialDistance = value;
		}
	}

	public float startDistance
	{
		get
		{
			return _startDistance;
		}
		set
		{
			_startDistance = value;
		}
	}

	public static bool CheckSkybox()
	{
		Material skybox = RenderSettings.skybox;
		if (skybox != null && skybox.HasProperty("_Tex") && skybox.HasProperty("_Tint") && skybox.HasProperty("_Exposure"))
		{
			return skybox.HasProperty("_Rotation");
		}
		return false;
	}

	private void Setup()
	{
		if (_fogMaterial == null)
		{
			_fogMaterial = new Material(_fogShader);
			_fogMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
	}

	private void SanitizeParameters()
	{
		_startDistance = Mathf.Max(_startDistance, 0f);
	}

	private void Start()
	{
		Setup();
	}

	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!CheckSkybox())
		{
			Graphics.Blit(source, destination);
			return;
		}
		SanitizeParameters();
		Setup();
		_fogMaterial.SetFloat("_DistanceOffset", _startDistance);
		switch (RenderSettings.fogMode)
		{
		case FogMode.Linear:
		{
			float fogStartDistance = RenderSettings.fogStartDistance;
			float fogEndDistance = RenderSettings.fogEndDistance;
			float num = 1f / Mathf.Max(fogEndDistance - fogStartDistance, 1E-06f);
			_fogMaterial.SetFloat("_LinearGrad", 0f - num);
			_fogMaterial.SetFloat("_LinearOffs", fogEndDistance * num);
			_fogMaterial.DisableKeyword("FOG_EXP");
			_fogMaterial.DisableKeyword("FOG_EXP2");
			break;
		}
		case FogMode.Exponential:
		{
			float fogDensity2 = RenderSettings.fogDensity;
			_fogMaterial.SetFloat("_Density", 1.442695f * fogDensity2);
			_fogMaterial.EnableKeyword("FOG_EXP");
			_fogMaterial.DisableKeyword("FOG_EXP2");
			break;
		}
		default:
		{
			float fogDensity = RenderSettings.fogDensity;
			_fogMaterial.SetFloat("_Density", 1.2011224f * fogDensity);
			_fogMaterial.DisableKeyword("FOG_EXP");
			_fogMaterial.EnableKeyword("FOG_EXP2");
			break;
		}
		}
		if (_useRadialDistance)
		{
			_fogMaterial.EnableKeyword("RADIAL_DIST");
		}
		else
		{
			_fogMaterial.DisableKeyword("RADIAL_DIST");
		}
		Material skybox = RenderSettings.skybox;
		_fogMaterial.SetTexture("_SkyCubemap", skybox.GetTexture("_Tex"));
		_fogMaterial.SetColor("_SkyTint", skybox.GetColor("_Tint"));
		_fogMaterial.SetFloat("_SkyExposure", skybox.GetFloat("_Exposure"));
		_fogMaterial.SetFloat("_SkyRotation", skybox.GetFloat("_Rotation"));
		Camera component = GetComponent<Camera>();
		Transform obj = component.transform;
		float nearClipPlane = component.nearClipPlane;
		float farClipPlane = component.farClipPlane;
		float num2 = Mathf.Tan(component.fieldOfView * ((float)Math.PI / 180f) / 2f);
		Vector3 vector = obj.right * nearClipPlane * num2 * component.aspect;
		Vector3 vector2 = obj.up * nearClipPlane * num2;
		Vector3 vector3 = obj.forward * nearClipPlane - vector + vector2;
		Vector3 vector4 = obj.forward * nearClipPlane + vector + vector2;
		Vector3 vector5 = obj.forward * nearClipPlane + vector - vector2;
		Vector3 vector6 = obj.forward * nearClipPlane - vector - vector2;
		float num3 = vector3.magnitude * farClipPlane / nearClipPlane;
		RenderTexture.active = destination;
		_fogMaterial.SetTexture("_MainTex", source);
		_fogMaterial.SetPass(0);
		GL.PushMatrix();
		GL.LoadOrtho();
		GL.Begin(7);
		GL.MultiTexCoord2(0, 0f, 0f);
		GL.MultiTexCoord(1, vector6.normalized * num3);
		GL.Vertex3(0f, 0f, 0.1f);
		GL.MultiTexCoord2(0, 1f, 0f);
		GL.MultiTexCoord(1, vector5.normalized * num3);
		GL.Vertex3(1f, 0f, 0.1f);
		GL.MultiTexCoord2(0, 1f, 1f);
		GL.MultiTexCoord(1, vector4.normalized * num3);
		GL.Vertex3(1f, 1f, 0.1f);
		GL.MultiTexCoord2(0, 0f, 1f);
		GL.MultiTexCoord(1, vector3.normalized * num3);
		GL.Vertex3(0f, 1f, 0.1f);
		GL.End();
		GL.PopMatrix();
	}

	private static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr)
	{
		RenderTexture.active = dest;
		fxMaterial.SetTexture("_MainTex", source);
		GL.PushMatrix();
		GL.LoadOrtho();
		fxMaterial.SetPass(passNr);
		GL.Begin(7);
		GL.MultiTexCoord2(0, 0f, 0f);
		GL.Vertex3(0f, 0f, 3f);
		GL.MultiTexCoord2(0, 1f, 0f);
		GL.Vertex3(1f, 0f, 2f);
		GL.MultiTexCoord2(0, 1f, 1f);
		GL.Vertex3(1f, 1f, 1f);
		GL.MultiTexCoord2(0, 0f, 1f);
		GL.Vertex3(0f, 1f, 0f);
		GL.End();
		GL.PopMatrix();
	}
}
