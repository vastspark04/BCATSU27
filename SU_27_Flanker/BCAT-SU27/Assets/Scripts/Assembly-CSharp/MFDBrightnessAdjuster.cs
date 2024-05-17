using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MFDBrightnessAdjuster : MonoBehaviour
{
	public float minBrightness = 0.1f;

	public float maxBrightness = 1f;

	public Image[] images;

	public float lerpRate = 7f;

	private const float LERP_THRESHOLD = 0.001f;

	private float currentAlpha;

	private float targetAlpha;

	private Coroutine lerpRoutine;

	public void SetBrightness(float t)
	{
		float num = (targetAlpha = 1f - Mathf.Lerp(minBrightness, maxBrightness, t));
		if (lerpRoutine == null)
		{
			lerpRoutine = StartCoroutine(TintLerpRoutine());
		}
	}

	private IEnumerator TintLerpRoutine()
	{
		while (Mathf.Abs(currentAlpha - targetAlpha) > 0.001f)
		{
			currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, lerpRate * Time.deltaTime);
			for (int i = 0; i < images.Length; i++)
			{
				Color color = images[i].color;
				color.a = currentAlpha;
				images[i].color = color;
			}
			yield return null;
		}
		lerpRoutine = null;
	}
}
