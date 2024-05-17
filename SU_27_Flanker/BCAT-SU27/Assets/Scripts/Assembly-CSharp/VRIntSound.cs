using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(VRInteractable))]
public class VRIntSound : MonoBehaviour
{
	public AudioClip sound;

	public AudioMixerGroup mixerGroup;

	[Range(0f, 1f)]
	public float volume = 1f;

	public float minDistance = 1f;

	public float maxDistance = 500f;

	[Range(0f, 3f)]
	public float pitch = 1f;

	public float dopplerLevel = 1f;

	public bool parentToObject = true;

	public Transform overrideParent;

	private AudioSource audioSource;

	private void Start()
	{
		audioSource = new GameObject("sound").AddComponent<AudioSource>();
		audioSource.minDistance = minDistance;
		audioSource.volume = volume;
		audioSource.maxDistance = maxDistance;
		audioSource.loop = false;
		audioSource.outputAudioMixerGroup = mixerGroup;
		audioSource.spatialBlend = 1f;
		audioSource.pitch = pitch;
		audioSource.dopplerLevel = dopplerLevel;
		if ((bool)overrideParent)
		{
			audioSource.transform.parent = overrideParent;
			audioSource.transform.position = base.transform.position;
		}
		else if (parentToObject)
		{
			audioSource.transform.parent = base.transform;
			audioSource.transform.localPosition = Vector3.zero;
		}
		else
		{
			audioSource.transform.position = base.transform.position;
		}
		GetComponent<VRInteractable>().OnInteract.AddListener(PlaySound);
	}

	private void PlaySound()
	{
		audioSource.PlayOneShot(sound);
	}

	private void OnDestroy()
	{
		if (!parentToObject && (bool)sound && (bool)audioSource && (bool)audioSource.gameObject)
		{
			Object.Destroy(audioSource.gameObject, sound.length);
		}
	}
}
