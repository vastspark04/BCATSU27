using UnityEngine;

public class TranslationAudio : MonoBehaviour
{
	public Transform translationTransform;

	public AudioSource audioSource;

	public AnimationCurve volumeCurve;

	public AnimationCurve pitchCurve;

	public bool fixedUpdate;

	public float lerpRate = -1f;

	private Vector3 lastPosition;

	private float speed;

	private bool effectEnabled = true;

	private void Start()
	{
		if (!translationTransform)
		{
			translationTransform = base.transform;
		}
		lastPosition = translationTransform.localPosition;
	}

	private void Update()
	{
		if (!fixedUpdate)
		{
			UpdateAudio(Time.deltaTime);
		}
	}

	private void FixedUpdate()
	{
		if (fixedUpdate)
		{
			UpdateAudio(Time.fixedDeltaTime);
		}
	}

	private void UpdateAudio(float deltaTime)
	{
		deltaTime = Mathf.Clamp(deltaTime, 0.001f, 1f);
		if (effectEnabled)
		{
			Vector3 localPosition = translationTransform.localPosition;
			float b = (localPosition - lastPosition).magnitude / deltaTime;
			if (lerpRate > 0f)
			{
				speed = Mathf.Lerp(speed, b, lerpRate * deltaTime);
			}
			else
			{
				speed = b;
			}
			lastPosition = localPosition;
		}
		else if (lerpRate > 0f)
		{
			speed = Mathf.Lerp(speed, 0f, lerpRate * deltaTime);
		}
		else
		{
			speed = 0f;
		}
		float num = volumeCurve.Evaluate(speed);
		if (num > 0.01f)
		{
			if (!audioSource.isPlaying)
			{
				audioSource.Play();
			}
			audioSource.volume = num;
			audioSource.pitch = pitchCurve.Evaluate(speed);
		}
		else if (audioSource.isPlaying)
		{
			audioSource.Stop();
		}
	}

	public void DisableEffect()
	{
		effectEnabled = false;
	}

	public void EnableEffect()
	{
		lastPosition = translationTransform.localPosition;
		effectEnabled = true;
	}
}
