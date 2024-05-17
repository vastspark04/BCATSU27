using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FPSText : MonoBehaviour
{
	private Text txt;

	private void Start()
	{
		txt = GetComponent<Text>();
	}

	private void OnEnable()
	{
		StartCoroutine(FPSRoutine());
	}

	private IEnumerator FPSRoutine()
	{
		yield return null;
		int frameSamples = 10;
		while (base.enabled)
		{
			float totalTime = 0f;
			for (int i = 0; i < frameSamples; i++)
			{
				totalTime += Time.unscaledDeltaTime;
				yield return null;
			}
			float num = totalTime / (float)frameSamples;
			if (num > 0f)
			{
				txt.text = Mathf.Round(1f / num).ToString();
			}
		}
	}
}
