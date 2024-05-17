using System.Collections;
using UnityEngine;

public class IntroSequence : MonoBehaviour
{
	public Rigidbody liftTransform;

	public float liftSpeed;

	public AudioSource liftAudioSource;

	public Transform leftDoor;

	public Transform rightDoor;

	private float rampUpSpeed;

	private void Start()
	{
		StartCoroutine(IntroRoutine());
	}

	private IEnumerator IntroRoutine()
	{
		yield return new WaitForSeconds(0.2f);
		liftAudioSource.Play();
		liftAudioSource.pitch = 0.3f;
		float doorSpeed2 = 0f;
		while (leftDoor.localPosition.y > -1.5f)
		{
			leftDoor.localPosition = Vector3.MoveTowards(leftDoor.localPosition, new Vector3(0f, -1.5f, 5f), doorSpeed2 * Time.deltaTime);
			rightDoor.localPosition = Vector3.MoveTowards(rightDoor.localPosition, new Vector3(0f, -1.5f, -5f), doorSpeed2 * Time.deltaTime);
			doorSpeed2 = Mathf.MoveTowards(doorSpeed2, Mathf.Clamp(Mathf.Abs(leftDoor.localPosition.y + 1.5f) * 4f, 1f, 8f), 7f * Time.deltaTime);
			liftAudioSource.pitch = doorSpeed2 / 6f;
			yield return null;
		}
		doorSpeed2 = 0f;
		liftAudioSource.pitch = 0.27653f;
		while (leftDoor.localPosition.z < 15f)
		{
			leftDoor.localPosition = Vector3.MoveTowards(leftDoor.localPosition, new Vector3(0f, -1.5f, 15f), doorSpeed2 * Time.deltaTime);
			rightDoor.localPosition = Vector3.MoveTowards(rightDoor.localPosition, new Vector3(0f, -1.5f, -15f), doorSpeed2 * Time.deltaTime);
			liftAudioSource.pitch = doorSpeed2 / 6f;
			doorSpeed2 = Mathf.MoveTowards(doorSpeed2, Mathf.Clamp(Mathf.Abs(leftDoor.localPosition.z - 15f) * 4f, 1f, 4f), 15f * Time.deltaTime);
			yield return null;
		}
		while (liftTransform.position.y < -0.5f)
		{
			float num = rampUpSpeed * Mathf.Clamp(Vector3.Distance(liftTransform.position, new Vector3(0f, 0f, 0f)), 1f, 5f);
			Vector3 position = Vector3.MoveTowards(liftTransform.position, new Vector3(0f, -0.5f, 0f), num * Time.fixedDeltaTime);
			liftTransform.MovePosition(position);
			liftAudioSource.pitch = num / 7f;
			rampUpSpeed = Mathf.Clamp01(rampUpSpeed + 0.5f * Time.fixedDeltaTime);
			yield return new WaitForFixedUpdate();
		}
		liftAudioSource.Stop();
	}
}
