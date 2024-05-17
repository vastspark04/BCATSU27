using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class ReadyRoomBGMFader : MonoBehaviour
{
	public AudioMixer mixer;

	public float minAtten;

	public float maxAtten;

	public float fadeRate;

	private float currAtten;

	private Coroutine fadeRoutine;

	private void Start()
	{
		SetAtten(maxAtten);
	}

	public void FadeOut()
	{
		if (fadeRoutine != null)
		{
			StopCoroutine(fadeRoutine);
		}
		fadeRoutine = StartCoroutine(FadeRoutine(minAtten));
	}

	public void FadeIn()
	{
		if (fadeRoutine != null)
		{
			StopCoroutine(fadeRoutine);
		}
		fadeRoutine = StartCoroutine(FadeRoutine(maxAtten));
	}

	private IEnumerator FadeRoutine(float tgt)
	{
		while (currAtten != tgt)
		{
			currAtten = Mathf.MoveTowards(currAtten, tgt, fadeRate * Time.unscaledDeltaTime);
			SetAtten(currAtten);
			yield return null;
		}
	}

	private void SetAtten(float atten)
	{
		currAtten = atten;
		mixer.SetFloat("ReadyRoomBGMAttenuation", atten);
	}
}
