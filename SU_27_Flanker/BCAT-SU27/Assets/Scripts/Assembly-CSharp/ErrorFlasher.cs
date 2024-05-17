using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ErrorFlasher : MonoBehaviour
{
	public GameObject displayObj;

	public Text text;

	public AudioSource audioSource;

	public AudioClip errorSound;

	private Coroutine errRoutine;

	private void Start()
	{
		displayObj.SetActive(value: false);
	}

	public void DisplayError(string message, float time)
	{
		if (errRoutine != null)
		{
			StopCoroutine(errRoutine);
		}
		errRoutine = StartCoroutine(ErrorRoutine(message, time));
	}

	public void HideError()
	{
		if (errRoutine != null)
		{
			StopCoroutine(errRoutine);
		}
		displayObj.SetActive(value: false);
	}

	private IEnumerator ErrorRoutine(string message, float time)
	{
		text.text = message;
		displayObj.SetActive(value: true);
		if ((bool)audioSource && (bool)errorSound)
		{
			audioSource.Stop();
			audioSource.PlayOneShot(errorSound);
		}
		for (float t = 0f; t < time; t += Time.deltaTime)
		{
			yield return null;
		}
		displayObj.SetActive(value: false);
	}
}
