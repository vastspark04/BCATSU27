using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraFogSettings : MonoBehaviour
{
	public FogMode fogMode;

	public float density;

	public float linearStartDist;

	public float linearEndDist;

	public Texture overrideSkyTexture;

	public Texture overrideFogTexture;

	private bool origFogEnabled;

	private FogMode origFogMode;

	private float origDensity;

	private float origStartDist;

	private float origEndDist;

	public bool enableOnOvercloud;

	private int fovID;

	private Camera cam;

	private void Awake()
	{
		fovID = Shader.PropertyToID("_CameraFOV");
		cam = GetComponent<Camera>();
	}

	private void OnPreRender()
	{
		if (!cam)
		{
			cam = GetComponent<Camera>();
		}
		origFogEnabled = RenderSettings.fog;
		origFogMode = RenderSettings.fogMode;
		origDensity = RenderSettings.fogDensity;
		origStartDist = RenderSettings.fogStartDistance;
		origEndDist = RenderSettings.fogEndDistance;
		if (VTResources.useOverCloud && !enableOnOvercloud)
		{
			RenderSettings.fog = false;
		}
		else
		{
			RenderSettings.fog = true;
			RenderSettings.fogMode = fogMode;
			RenderSettings.fogDensity = density;
			RenderSettings.fogStartDistance = linearStartDist;
			RenderSettings.fogEndDistance = linearEndDist;
			if ((bool)overrideSkyTexture)
			{
				Shader.SetGlobalTexture("_GlobalSkyCube", overrideSkyTexture);
				Shader.SetGlobalTexture("_GlobalSkyCubeHigh", overrideSkyTexture);
			}
			if ((bool)overrideFogTexture)
			{
				Shader.SetGlobalTexture("_GlobalFogCube", overrideFogTexture);
			}
		}
		Shader.SetGlobalFloat(fovID, cam.fieldOfView);
	}

	private void OnPostRender()
	{
		if (VTResources.useOverCloud && !enableOnOvercloud)
		{
			return;
		}
		RenderSettings.fog = origFogEnabled;
		RenderSettings.fogMode = origFogMode;
		RenderSettings.fogDensity = origDensity;
		RenderSettings.fogStartDistance = origStartDist;
		RenderSettings.fogEndDistance = origEndDist;
		if ((bool)EnvironmentManager.instance)
		{
			if ((bool)overrideSkyTexture)
			{
				Shader.SetGlobalTexture("_GlobalSkyCube", EnvironmentManager.instance.currentSkyTexture);
				Shader.SetGlobalTexture("_GlobalSkyCubeHigh", EnvironmentManager.instance.currentSkyTextureHigh);
			}
			if ((bool)overrideFogTexture)
			{
				Shader.SetGlobalTexture("_GlobalFogCube", EnvironmentManager.instance.currentFogTexture);
			}
		}
	}
}
