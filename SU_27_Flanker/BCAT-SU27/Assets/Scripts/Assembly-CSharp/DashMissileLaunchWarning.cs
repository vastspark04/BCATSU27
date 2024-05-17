using System.Collections;
using UnityEngine;

public class DashMissileLaunchWarning : MonoBehaviour
{
	public float blinkTime = 5f;

	public float blinkInterval = 0.2f;

	private Coroutine warningRoutine;

	public UIImageToggle imgToggle;

	public bool useCommonWarnings = true;

	public AudioSource warningAudioSource;

	public Battery battery;

	private void Awake()
	{
		if (!imgToggle)
		{
			imgToggle = GetComponent<UIImageToggle>();
		}
	}

	private void Start()
	{
		if (useCommonWarnings)
		{
			FlightWarnings componentInParent = GetComponentInParent<FlightWarnings>();
			if ((bool)warningAudioSource)
			{
				warningAudioSource.clip = componentInParent.GetCommonWarningClip(FlightWarnings.CommonWarnings.MissileLaunch);
			}
			if (!battery)
			{
				battery = componentInParent.battery;
			}
		}
	}

	public void LaunchWarning()
	{
		if (!battery || battery.Drain(0.01f * Time.deltaTime))
		{
			if (warningRoutine != null)
			{
				StopCoroutine(warningRoutine);
			}
			warningRoutine = StartCoroutine(WarningRoutine());
		}
	}

	private IEnumerator WarningRoutine()
	{
		if ((bool)warningAudioSource)
		{
			warningAudioSource.Play();
		}
		float t = Time.time;
		while (Time.time - t < blinkTime)
		{
			imgToggle.imageEnabled = true;
			yield return new WaitForSeconds(blinkInterval);
			imgToggle.imageEnabled = false;
			yield return new WaitForSeconds(blinkInterval);
			if ((bool)battery && !battery.Drain(0.01f * Time.deltaTime))
			{
				break;
			}
		}
		imgToggle.imageEnabled = false;
		if ((bool)warningAudioSource)
		{
			warningAudioSource.Stop();
		}
	}
}
