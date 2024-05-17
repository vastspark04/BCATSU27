using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DirectionalAudioSource : MonoBehaviour
{
	public float minMinDist;

	public float maxMinDist;

	public float minMaxDist;

	public float maxMaxDist;

	public float dotExp = 1f;

	private AudioSource audioSource;

	private Transform myTransform;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
		myTransform = base.transform;
	}

	private void LateUpdate()
	{
		float num = Vector3.Dot((AudioListenerPosition.GetAudioListenerPosition() - myTransform.position).normalized, myTransform.forward);
		num = (num + 1f) / 2f;
		num = Mathf.Sign(num) * Mathf.Pow(num, dotExp);
		audioSource.minDistance = Mathf.Lerp(minMinDist, maxMinDist, num);
		audioSource.maxDistance = Mathf.Lerp(minMaxDist, maxMaxDist, num);
	}
}
