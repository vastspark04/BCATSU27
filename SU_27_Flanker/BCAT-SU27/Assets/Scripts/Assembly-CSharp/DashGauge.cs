using System.Collections;
using UnityEngine;

public abstract class DashGauge : MonoBehaviour
{
	public Battery battery;

	public Transform dialHand;

	public Vector3 axis;

	public float arcAngle;

	public float maxValue = 1f;

	public float lerpRate = 8f;

	public bool loop;

	public float gizmoRadius = 0.022f;

	public float gizmoHeight = 0.006f;

	public bool doCalibration = true;

	public float calibrationSpeed = 1.5f;

	private bool calibrating;

	private bool calibrated;

	private Coroutine calibrationRoutine;

	private float displayedT;

	private void Update()
	{
		if (!battery || battery.Drain(0.01f * Time.deltaTime))
		{
			if (calibrated)
			{
				float value = GetMeteredValue() / maxValue;
				if (loop)
				{
					displayedT = value;
				}
				else
				{
					displayedT = Mathf.Lerp(displayedT, Mathf.Clamp01(value), lerpRate * Time.deltaTime);
				}
			}
			else if (!calibrating)
			{
				calibrationRoutine = StartCoroutine(CalibrationRoutine());
			}
		}
		else
		{
			displayedT = Mathf.Lerp(displayedT, 0f, lerpRate * Time.deltaTime);
			if (calibrating)
			{
				if (calibrationRoutine != null)
				{
					StopCoroutine(calibrationRoutine);
				}
				calibrating = false;
			}
			calibrated = false;
		}
		if (loop)
		{
			Quaternion b = Quaternion.AngleAxis(arcAngle * displayedT, axis);
			dialHand.localRotation = Quaternion.Slerp(dialHand.localRotation, b, lerpRate * Time.deltaTime);
		}
		else
		{
			dialHand.localRotation = Quaternion.AngleAxis(arcAngle * displayedT, axis);
		}
	}

	public Quaternion GetHandAngle(float value)
	{
		float num = value / maxValue;
		float num2 = ((!loop) ? Mathf.Clamp01(num) : num);
		return Quaternion.AngleAxis(arcAngle * num2, axis);
	}

	private IEnumerator CalibrationRoutine()
	{
		if (!doCalibration)
		{
			calibrated = true;
			yield break;
		}
		calibrating = true;
		float t = 0f;
		bool goingUp = true;
		while (!battery || battery.Drain(0.01f * Time.deltaTime))
		{
			if (goingUp)
			{
				if (t > 1.25f)
				{
					goingUp = false;
				}
				t = Mathf.MoveTowards(t, 1.3f, calibrationSpeed * Time.deltaTime);
			}
			else
			{
				if (t < 0f)
				{
					calibrating = false;
					calibrated = true;
					yield break;
				}
				t = Mathf.MoveTowards(t, -1f, calibrationSpeed * Time.deltaTime);
			}
			displayedT = Mathf.Lerp(displayedT, Mathf.Clamp01(t), 20f * Time.deltaTime);
			yield return null;
		}
		calibrating = false;
	}

	protected virtual float GetMeteredValue()
	{
		return 0f;
	}

	private void OnDrawGizmosSelected()
	{
		if (axis.sqrMagnitude > 0f && (bool)dialHand)
		{
			Vector3 forward = Vector3.forward;
			Vector3 direction = forward;
			float num = 0.02f;
			Vector3 vector = dialHand.parent.TransformDirection(axis) * gizmoHeight;
			for (float num2 = num; num2 < 1f; num2 += num)
			{
				Gizmos.color = Color.Lerp(Color.red, Color.green, num2);
				forward = Quaternion.AngleAxis(num2 * arcAngle, axis) * Vector3.forward;
				Vector3 from = dialHand.position + vector + dialHand.parent.TransformDirection(direction) * gizmoRadius;
				Vector3 vector2 = dialHand.position + vector + dialHand.parent.TransformDirection(forward) * gizmoRadius;
				Gizmos.DrawLine(from, vector2);
				Gizmos.color -= new Color(0f, 0f, 0f, 0.9f);
				Gizmos.DrawLine(vector2, dialHand.position);
				direction = forward;
			}
		}
	}
}
