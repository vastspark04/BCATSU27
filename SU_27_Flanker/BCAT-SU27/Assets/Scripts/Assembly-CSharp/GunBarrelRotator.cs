using System.Collections;
using UnityEngine;

public class GunBarrelRotator : MonoBehaviour
{
	public Transform rotationTransform;

	public Vector3 axis = Vector3.forward;

	public float speed;

	public float windDownRate;

	public float windupRate = 100f;

	public float minFiringSpeed;

	public Gun gun;

	private bool firing;

	public RotationAudio rotAudio;

	private bool hasRotAudio;

	private Coroutine fireRoutine;

	public float currSpeed { get; private set; }

	private void Start()
	{
		gun.OnSetFire.AddListener(SetFire);
		axis = axis.normalized;
		if ((bool)rotAudio)
		{
			hasRotAudio = true;
		}
	}

	private void SetFire(bool fire)
	{
		if (fire == firing)
		{
			return;
		}
		firing = fire;
		if (firing)
		{
			if (fireRoutine != null)
			{
				StopCoroutine(fireRoutine);
			}
			fireRoutine = StartCoroutine(FiringRoutine());
		}
	}

	private IEnumerator FiringRoutine()
	{
		while (firing)
		{
			currSpeed = Mathf.Lerp(currSpeed, speed, windupRate * Time.deltaTime);
			Quaternion quaternion = Quaternion.AngleAxis(currSpeed * Time.deltaTime, axis);
			rotationTransform.localRotation = quaternion * rotationTransform.localRotation;
			if (hasRotAudio)
			{
				rotAudio.UpdateAudioSpeed(currSpeed / speed);
			}
			yield return null;
		}
		while (!firing && currSpeed > 0f)
		{
			currSpeed = Mathf.Lerp(currSpeed, 0f, windDownRate * Time.deltaTime);
			Quaternion quaternion2 = Quaternion.AngleAxis(currSpeed * Time.deltaTime, axis);
			rotationTransform.localRotation = quaternion2 * rotationTransform.localRotation;
			if (hasRotAudio)
			{
				rotAudio.UpdateAudioSpeed(currSpeed / speed);
			}
			yield return null;
		}
		fireRoutine = null;
	}
}
