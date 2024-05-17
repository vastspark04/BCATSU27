using System.Collections;
using UnityEngine;

public class HUDBrightnessControl : MonoBehaviour
{
	public float maxBrightness = 1f;

	public float minBrightness = 0.1f;

	public float lerpRate = 5f;

	private float brightness = 1f;

	private float lerpedBrightness;

	private bool colorDirty;

	public void SetBrightness(float t)
	{
		brightness = Mathf.Lerp(minBrightness, maxBrightness, t);
		if (!colorDirty)
		{
			colorDirty = true;
			StartCoroutine(UpdateRoutine());
		}
	}

	private IEnumerator UpdateRoutine()
	{
		while (colorDirty)
		{
			lerpedBrightness = Mathf.Lerp(lerpedBrightness, brightness, lerpRate * Time.deltaTime);
			Shader.SetGlobalFloat("_HUDBrightness", lerpedBrightness);
			if (Mathf.Abs(brightness - lerpedBrightness) < 0.001f)
			{
				colorDirty = false;
			}
			yield return null;
		}
	}
}
