using System.Collections;
using UnityEngine;

public class PixelSphereFader : MonoBehaviour
{
	public float fadeRate;

	public float currentValue;

	public string propertyName = "_AlphaOffset";

	private int propertyId;

	private MaterialPropertyBlock propertyBlock;

	private Renderer r;

	private Coroutine fadeRoutine;

	private void Awake()
	{
		propertyId = Shader.PropertyToID(propertyName);
		propertyBlock = new MaterialPropertyBlock();
		r = GetComponent<MeshRenderer>();
		SetValue(currentValue);
	}

	public void FadeToTransparent()
	{
		if (fadeRoutine != null)
		{
			StopCoroutine(fadeRoutine);
		}
		fadeRoutine = StartCoroutine(FadeRoutine(-1f));
	}

	public void FadeToBlack()
	{
		if (fadeRoutine != null)
		{
			StopCoroutine(fadeRoutine);
		}
		fadeRoutine = StartCoroutine(FadeRoutine(0f));
	}

	private IEnumerator FadeRoutine(float target)
	{
		r.enabled = true;
		while (currentValue != target)
		{
			currentValue = Mathf.MoveTowards(currentValue, target, fadeRate * Time.deltaTime);
			SetValue(currentValue);
			yield return null;
		}
		if (currentValue == -1f)
		{
			r.enabled = false;
		}
	}

	private void SetValue(float t)
	{
		currentValue = t;
		propertyBlock.SetFloat(propertyId, t);
		r.SetPropertyBlock(propertyBlock);
	}

	public void Blink()
	{
		SetValue(0f);
		FadeToTransparent();
	}
}
