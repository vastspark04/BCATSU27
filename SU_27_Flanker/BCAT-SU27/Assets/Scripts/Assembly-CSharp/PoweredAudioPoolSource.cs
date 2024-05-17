using UnityEngine;

public class PoweredAudioPoolSource : MonoBehaviour
{
	public Battery battery;

	public AudioClip poweredSound;

	public AudioClip unpoweredSound;

	public float volume = 1f;

	public MinMax pitchRange = new MinMax(1f, 1f);

	public float minDist = 1f;

	public float maxDist = 500f;

	public bool exterior;

	public int priority = 128;

	public void PlaySound()
	{
		AudioClip clip = ((!battery.Drain(0.01f * Time.deltaTime)) ? unpoweredSound : poweredSound);
		AudioController.instance.PlayOneShot(clip, base.transform.position, pitchRange.Random(), volume, minDist, maxDist, exterior, priority, base.transform);
	}
}
