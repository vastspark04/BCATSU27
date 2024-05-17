using UnityEngine;

public class GearImpactAudio : MonoBehaviour
{
	public AudioSource audioSource;

	public AudioClip impactClip;

	public AnimationCurve volumeCurve;

	public void OnImpact(float speed)
	{
		audioSource.volume = volumeCurve.Evaluate(speed);
		audioSource.PlayOneShot(impactClip);
	}
}
