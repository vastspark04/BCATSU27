using UnityEngine;

public class IRMissileIncomingWarning : MonoBehaviour
{
	public UIImageStatusLight statusLight;

	public Battery battery;

	public MissileDetector detector;

	public AudioSource audioSource;

	public float flashRate;

	private bool playing;

	private void Update()
	{
		if ((!battery || battery.Drain(0.01f * Time.deltaTime)) && detector.missileIncomingDetected)
		{
			bool flag = Mathf.Repeat(Time.time * flashRate, 1f) < 0.75f;
			statusLight.SetStatus(flag ? 1 : 0);
			if (!playing)
			{
				if ((bool)audioSource)
				{
					audioSource.Play();
				}
				playing = true;
			}
		}
		else if (playing)
		{
			playing = false;
			if ((bool)audioSource)
			{
				audioSource.Stop();
			}
			statusLight.SetStatus(0);
		}
	}
}
