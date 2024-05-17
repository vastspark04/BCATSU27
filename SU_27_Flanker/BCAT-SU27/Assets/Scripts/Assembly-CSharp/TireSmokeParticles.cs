using System.Collections;
using UnityEngine;

public class TireSmokeParticles : MonoBehaviour
{
	public AudioSource screechAudioSource;

	public AudioClip screechAudioClip;

	private ParticleSystem ps;

	public Rigidbody rb;

	private float wheelSpeed;

	private Coroutine wsRoutine;

	private void Awake()
	{
		ps = GetComponent<ParticleSystem>();
		if (!rb)
		{
			rb = GetComponentInParent<Rigidbody>();
		}
	}

	public void OnContact(Vector3 point)
	{
		float magnitude = Vector3.Project(rb.GetPointVelocity(point), rb.transform.forward).magnitude;
		float num = Mathf.Abs(magnitude - wheelSpeed);
		if (num > 10f)
		{
			ps.transform.position = point;
			ps.Emit(Mathf.RoundToInt(num / 3f));
			if (num > 20f)
			{
				screechAudioSource.PlayOneShot(screechAudioClip);
			}
		}
		wheelSpeed = magnitude;
		if (wsRoutine != null)
		{
			wsRoutine = StartCoroutine(WheelSpeedRoutine());
		}
	}

	private IEnumerator WheelSpeedRoutine()
	{
		while (wheelSpeed > 0f)
		{
			wheelSpeed = Mathf.MoveTowards(wheelSpeed, 0f, 5f * Time.deltaTime);
			yield return null;
		}
		wsRoutine = null;
	}
}
