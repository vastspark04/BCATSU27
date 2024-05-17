using UnityEngine;

public class AudioAnimator : MonoBehaviour
{
	public AudioSource audioSource;

	public AnimationCurve pitchCurve;

	public AnimationCurve volumeCurve;

	public void Evaluate(float t)
	{
		audioSource.volume = volumeCurve.Evaluate(t);
		if (audioSource.volume < 0.02f)
		{
			if (audioSource.isPlaying)
			{
				audioSource.Stop();
			}
			return;
		}
		if (!audioSource.isPlaying)
		{
			audioSource.Play();
		}
		audioSource.pitch = pitchCurve.Evaluate(t);
	}
}
