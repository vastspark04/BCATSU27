using UnityEngine;
using UnityEngine.Rendering;

public class CameraAmbientLight : MonoBehaviour
{
	public Color sky;

	public Color equator;

	public Color ground;

	private Color origSky;

	private Color origEquator;

	private Color origGround;

	private AmbientMode origMode;

	private void OnPreRender()
	{
		origMode = RenderSettings.ambientMode;
		if (origMode == AmbientMode.Trilight)
		{
			origSky = RenderSettings.ambientSkyColor;
			origEquator = RenderSettings.ambientEquatorColor;
			origGround = RenderSettings.ambientGroundColor;
		}
		RenderSettings.ambientMode = AmbientMode.Trilight;
		RenderSettings.ambientSkyColor = sky;
		RenderSettings.ambientEquatorColor = equator;
		RenderSettings.ambientGroundColor = ground;
	}

	private void OnPostRender()
	{
		RenderSettings.ambientMode = origMode;
		if (origMode == AmbientMode.Trilight)
		{
			RenderSettings.ambientSkyColor = origSky;
			RenderSettings.ambientEquatorColor = origEquator;
			RenderSettings.ambientGroundColor = origGround;
		}
	}
}
