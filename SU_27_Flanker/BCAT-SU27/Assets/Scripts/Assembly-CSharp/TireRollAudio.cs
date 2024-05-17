using UnityEngine;

public class TireRollAudio : MonoBehaviour
{
	public RaySpringDamper[] suspensions;

	public AudioSource audioSource;

	private int count;

	public AnimationCurve volumeCurve;

	public AnimationCurve pitchCurve;

	public float speedMult;

	public Rigidbody rb;

	private AudioClip origClip;

	private void Start()
	{
		audioSource.loop = true;
		count = suspensions.Length;
		origClip = audioSource.clip;
		if (!rb)
		{
			rb = GetComponentInParent<Rigidbody>();
		}
	}

	private void Update()
	{
		bool flag = false;
		float time = 0f;
		WheelSurfaceMaterial wheelSurfaceMaterial = null;
		for (int i = 0; i < count; i++)
		{
			if (suspensions[i].isTouching)
			{
				flag = true;
				time = suspensions[i].surfaceVelocity.magnitude / speedMult;
				wheelSurfaceMaterial = suspensions[i].surfaceMaterial;
				break;
			}
		}
		if (flag)
		{
			if (!audioSource.isPlaying)
			{
				audioSource.Play();
			}
			audioSource.volume = volumeCurve.Evaluate(time);
			audioSource.pitch = pitchCurve.Evaluate(time);
			AudioClip audioClip = ((!wheelSurfaceMaterial || !wheelSurfaceMaterial.rollingAudio) ? origClip : wheelSurfaceMaterial.rollingAudio);
			if (audioClip != audioSource.clip)
			{
				audioSource.clip = audioClip;
				audioSource.Play();
			}
		}
		else if (audioSource.isPlaying)
		{
			audioSource.Stop();
		}
	}
}
