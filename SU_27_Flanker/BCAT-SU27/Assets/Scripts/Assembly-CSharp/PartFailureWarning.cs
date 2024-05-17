using UnityEngine;

public class PartFailureWarning : MonoBehaviour
{
	public FlightWarnings fWarnings;

	public UIImageStatusLight statusLight;

	public Battery battery;

	public VehiclePart part;

	public float nrmHealthWarnLevel;

	public AudioSource audioSource;

	public float flashRate;

	private bool cleared;

	private void Start()
	{
		if ((bool)fWarnings)
		{
			fWarnings.OnClearedWarnings.AddListener(Clear);
		}
	}

	private void Update()
	{
		if ((!battery || battery.Drain(0.01f * Time.deltaTime)) && part.health.normalizedHealth <= nrmHealthWarnLevel)
		{
			if (!cleared)
			{
				bool flag = Mathf.Repeat(Time.time * flashRate, 1f) < 0.75f;
				statusLight.SetStatus(flag ? 1 : 0);
				if ((bool)audioSource && !audioSource.isPlaying)
				{
					audioSource.Play();
				}
			}
			else if ((bool)audioSource && audioSource.isPlaying)
			{
				audioSource.Stop();
				statusLight.SetStatus(0);
			}
		}
		else
		{
			if ((bool)audioSource && audioSource.isPlaying)
			{
				audioSource.Stop();
				statusLight.SetStatus(0);
			}
			cleared = false;
		}
	}

	public void Clear()
	{
		cleared = true;
	}
}
