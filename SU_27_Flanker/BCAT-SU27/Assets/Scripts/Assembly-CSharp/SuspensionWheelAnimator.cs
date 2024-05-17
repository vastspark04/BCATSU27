using System;
using System.Collections;
using UnityEngine;

public class SuspensionWheelAnimator : MonoBehaviour
{
	public Transform wheelTransform;

	public Vector3 axis;

	public float wheelRadius;

	public RaySpringDamper suspension;

	private Quaternion origLocalRot;

	private float rotationDegrees;

	private float circumference;

	private Vector3 localAxis;

	private void OnDrawGizmosSelected()
	{
		if ((bool)wheelTransform)
		{
			_ = axis.normalized;
			Vector3 vector = Vector3.Cross(wheelTransform.TransformDirection(axis), UnityEngine.Random.onUnitSphere).normalized * wheelRadius;
			int num = 24;
			float num2 = 360f / (float)num;
			for (int i = 0; i < num; i++)
			{
				float angle = (float)i * num2;
				float angle2 = (float)(i + 1) * num2;
				Vector3 from = wheelTransform.position + Quaternion.AngleAxis(angle, axis) * vector;
				Vector3 to = wheelTransform.position + Quaternion.AngleAxis(angle2, axis) * vector;
				Gizmos.DrawLine(from, to);
			}
		}
	}

	private void Start()
	{
		origLocalRot = wheelTransform.localRotation;
		circumference = (float)Math.PI * (2f * wheelRadius);
		localAxis = origLocalRot * axis;
		suspension.OnContact.AddListener(OnSuspContact);
		if (suspension.isTouching)
		{
			StartCoroutine(UpdateRoutine());
		}
	}

	private void OnSuspContact(Vector3 v)
	{
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		while (suspension.isTouching)
		{
			if (Mathf.Abs(suspension.wheelSpeed) > 0.001f)
			{
				float num = suspension.wheelSpeed / circumference * 360f;
				rotationDegrees = Mathf.Repeat(rotationDegrees + num * Time.deltaTime, 360f);
				wheelTransform.localRotation = Quaternion.AngleAxis(rotationDegrees, localAxis) * origLocalRot;
			}
			yield return null;
		}
		while (!suspension.isTouching && Mathf.Abs(suspension.wheelSpeed) > 0.001f)
		{
			float num2 = suspension.wheelSpeed / circumference * 360f;
			rotationDegrees = Mathf.Repeat(rotationDegrees + num2 * Time.deltaTime, 360f);
			wheelTransform.localRotation = Quaternion.AngleAxis(rotationDegrees, localAxis) * origLocalRot;
			yield return null;
		}
	}
}
