using UnityEngine;

public class CockpitWindAudioController : MonoBehaviour, IParentRBDependent
{
	private Rigidbody rb;

	public FlightInfo flightInfo;

	public AudioSource audioSource;

	public AnimationCurve volumeCurve;

	public AnimationCurve pitchCurve;

	private void Awake()
	{
		audioSource.volume = 0f;
	}

	private void Start()
	{
		rb = flightInfo.rb;
	}

	private void Update()
	{
		float time = ((flightInfo.rb == rb) ? flightInfo.airspeed : ((!rb) ? 0f : rb.velocity.magnitude));
		float num = volumeCurve.Evaluate(time);
		if (num > 0.001f)
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

	public void SetParentRigidbody(Rigidbody rb)
	{
		this.rb = rb;
	}
}
