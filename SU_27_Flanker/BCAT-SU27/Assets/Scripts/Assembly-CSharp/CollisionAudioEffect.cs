using UnityEngine;

public class CollisionAudioEffect : MonoBehaviour
{
	public AudioSource audioSource;

	public AudioClip[] sounds;

	public float speedFactor;

	public float minPitch;

	public float maxPitch;

	private void OnCollisionEnter(Collision col)
	{
		audioSource.Stop();
		audioSource.volume = col.relativeVelocity.magnitude * speedFactor;
		audioSource.pitch = Random.Range(minPitch, maxPitch);
		audioSource.PlayOneShot(sounds[Random.Range(0, sounds.Length)]);
	}
}
