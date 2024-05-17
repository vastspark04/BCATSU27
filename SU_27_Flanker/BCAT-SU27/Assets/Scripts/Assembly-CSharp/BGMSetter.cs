using UnityEngine;

public class BGMSetter : MonoBehaviour
{
	public AudioClip track;

	public float fadeInTime;

	private void Start()
	{
		BGMManager.FadeTo(track, fadeInTime, loop: true, track.name);
	}
}
