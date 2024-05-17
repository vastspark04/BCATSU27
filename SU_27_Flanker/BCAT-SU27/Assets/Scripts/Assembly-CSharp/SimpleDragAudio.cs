using UnityEngine;

public class SimpleDragAudio : SimpleDrag
{
	public AudioSource audioSource;

	public AnimationCurve pitchCurve;

	public AnimationCurve volumeCurve;

	public float magnitudeFactor = 1f;

	protected override void OnApplyDrag(float dragMagnitude)
	{
		base.OnApplyDrag(dragMagnitude);
		float time = dragMagnitude * magnitudeFactor;
		float num = volumeCurve.Evaluate(time);
		if (num > 0.02f)
		{
			if (!audioSource.isPlaying)
			{
				audioSource.Play();
			}
			audioSource.volume = num;
			audioSource.pitch = pitchCurve.Evaluate(time);
		}
		else if (audioSource.isPlaying)
		{
			audioSource.Stop();
		}
	}
}
