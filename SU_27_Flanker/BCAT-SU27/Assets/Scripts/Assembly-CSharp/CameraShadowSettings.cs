using UnityEngine;

public class CameraShadowSettings : MonoBehaviour
{
	public Vector3 cascades;

	public float shadowDistance;

	private Vector3 origCascades;

	private float origShadowDistance;

	private bool appliedSettings;

	private bool useSettings;

	private void OnEnable()
	{
		useSettings = true;
	}

	private void OnDisable()
	{
		useSettings = false;
	}

	private void OnPreRender()
	{
		if (useSettings)
		{
			appliedSettings = true;
			origCascades = QualitySettings.shadowCascade4Split;
			QualitySettings.shadowCascade4Split = cascades;
			origShadowDistance = QualitySettings.shadowDistance;
			QualitySettings.shadowDistance = shadowDistance;
		}
	}

	private void OnPostRender()
	{
		if (appliedSettings)
		{
			QualitySettings.shadowCascade4Split = origCascades;
			QualitySettings.shadowDistance = origShadowDistance;
			appliedSettings = false;
		}
	}
}
