using UnityEngine;

public class RotationAudio : MonoBehaviour
{
	public Transform rotationTransform;

	public AudioSource audioSource;

	public AnimationCurve volumeCurve;

	public AnimationCurve pitchCurve;

	public bool fixedUpdate;

	public float lerpRate = -1f;

	private Quaternion lastPosition;

	private float speed;

	private bool effectEnabled = true;

	public bool manual;

	private float lastSpeed = float.MinValue;

	private void Start()
	{
		if (!rotationTransform)
		{
			rotationTransform = base.transform;
		}
		lastPosition = rotationTransform.localRotation;
	}

	private void Update()
	{
		if (!manual && !fixedUpdate)
		{
			UpdateAudio(Time.deltaTime);
		}
	}

	private void FixedUpdate()
	{
		if (!manual && fixedUpdate)
		{
			UpdateAudio(Time.fixedDeltaTime);
		}
	}

	private void UpdateAudio(float deltaTime)
	{
		deltaTime = Mathf.Clamp(deltaTime, 0.001f, 1f);
		if (effectEnabled)
		{
			Quaternion localRotation = rotationTransform.localRotation;
			float b = Quaternion.Angle(localRotation, lastPosition) / deltaTime;
			if (lerpRate > 0f)
			{
				speed = Mathf.Lerp(speed, b, lerpRate * deltaTime);
			}
			else
			{
				speed = b;
			}
			lastPosition = localRotation;
		}
		else if (lerpRate > 0f)
		{
			speed = Mathf.Lerp(speed, 0f, lerpRate * deltaTime);
		}
		else
		{
			speed = 0f;
		}
		UpdateAudioSpeed(speed);
	}

	public void UpdateAudioSpeed(float speed)
	{
		if (!(Mathf.Abs(speed - lastSpeed) > 0.001f))
		{
			return;
		}
		lastSpeed = speed;
		audioSource.volume = volumeCurve.Evaluate(speed);
		audioSource.pitch = pitchCurve.Evaluate(speed);
		if (audioSource.volume < 0.005f)
		{
			if (audioSource.isPlaying)
			{
				audioSource.Stop();
			}
		}
		else if (!audioSource.isPlaying)
		{
			audioSource.Play();
		}
	}

	public void DisableEffect()
	{
		effectEnabled = false;
	}

	public void EnableEffect()
	{
		lastPosition = rotationTransform.localRotation;
		effectEnabled = true;
	}
}
