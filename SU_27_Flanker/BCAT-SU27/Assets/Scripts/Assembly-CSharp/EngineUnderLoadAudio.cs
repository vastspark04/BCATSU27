using System;
using UnityEngine;

public class EngineUnderLoadAudio : MonoBehaviour
{
	[Serializable]
	public class AudioProfile
	{
		public AudioSource audioSource;

		public AnimationCurve rpmVolumeCurve;

		public AnimationCurve rpmPitchCurve;

		public AnimationCurve throttleCurve;

		public float lerpRate = 5f;

		private bool wasOff = true;

		public void Evaluate(ModuleEngine e)
		{
			float num = rpmVolumeCurve.Evaluate(e.outputRPM) * throttleCurve.Evaluate(e.inputThrottle);
			if (num > 0f)
			{
				if (!audioSource.isPlaying)
				{
					audioSource.Play();
				}
				if (wasOff)
				{
					wasOff = false;
					audioSource.pitch = rpmPitchCurve.Evaluate(e.outputRPM);
				}
				else
				{
					audioSource.pitch = Mathf.Lerp(audioSource.pitch, rpmPitchCurve.Evaluate(e.outputRPM), lerpRate * Time.deltaTime);
				}
				audioSource.volume = Mathf.Lerp(audioSource.volume, num, lerpRate * Time.deltaTime);
			}
			else
			{
				wasOff = true;
				if (audioSource.isPlaying)
				{
					audioSource.volume = 0f;
					audioSource.Stop();
				}
			}
		}
	}

	public ModuleEngine engine;

	public AudioProfile[] profiles;

	private void Update()
	{
		for (int i = 0; i < profiles.Length; i++)
		{
			profiles[i].Evaluate(engine);
		}
	}
}
