using UnityEngine;

[RequireComponent(typeof(Camera))]
public class IlluminateVesselsOnRender : MonoBehaviour
{
	public Light illuminatorLight;

	private EnvironmentManager.EnvironmentSetting env;

	public float sunIntensity = 0.5f;

	private float origSunIntensity;

	public bool modifySun = true;

	private void Start()
	{
		illuminatorLight.enabled = true;
		illuminatorLight.cullingMask = 0;
		if (!EnvironmentManager.instance)
		{
			base.enabled = false;
		}
		else
		{
			env = EnvironmentManager.instance.GetCurrentEnvironment();
		}
	}

	private void OnPreCull()
	{
		illuminatorLight.cullingMask = 8448;
		if (modifySun)
		{
			origSunIntensity = env.sun.intensity;
			env.sun.intensity *= sunIntensity;
		}
	}

	private void OnPostRender()
	{
		illuminatorLight.cullingMask = 0;
		if (modifySun)
		{
			env.sun.intensity = origSunIntensity;
		}
	}
}
