using System.Collections;
using UnityEngine;

public class GearBogeyAnimator : MonoBehaviour
{
	public RaySpringDamper suspension;

	public float naturalRot;

	public float springbackRate;

	private Coroutine cRoutine;

	private void Awake()
	{
		suspension.OnContact.AddListener(OnContact);
		suspension.OnLiftOff.AddListener(OnLiftoff);
	}

	private void OnEnable()
	{
		if (suspension.isTouching)
		{
			OnContact(suspension.point);
		}
		else
		{
			OnLiftoff();
		}
	}

	private void OnContact(Vector3 pt)
	{
		if (cRoutine != null)
		{
			StopCoroutine(cRoutine);
		}
		cRoutine = StartCoroutine(ContactRoutine());
	}

	private void OnLiftoff()
	{
		if (cRoutine != null)
		{
			StopCoroutine(cRoutine);
		}
		cRoutine = StartCoroutine(LiftoffRoutine());
	}

	private IEnumerator LiftoffRoutine()
	{
		float dot = -1f;
		while (dot < 0.9999f)
		{
			Vector3 vector = Quaternion.AngleAxis(naturalRot, Vector3.right) * Vector3.forward;
			base.transform.localRotation = Quaternion.RotateTowards(base.transform.localRotation, Quaternion.LookRotation(vector), springbackRate * Time.deltaTime);
			dot = Vector3.Dot(base.transform.localRotation * Vector3.forward, vector);
			yield return null;
		}
		base.transform.localRotation = Quaternion.LookRotation(Quaternion.AngleAxis(naturalRot, Vector3.right) * Vector3.forward);
	}

	private IEnumerator ContactRoutine()
	{
		while (base.enabled)
		{
			Vector3 forward = base.transform.parent.InverseTransformDirection(suspension.normal);
			forward.x = 0f;
			base.transform.localRotation = Quaternion.LookRotation(forward);
			yield return null;
		}
	}
}
