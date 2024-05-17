using System.Collections;
using UnityEngine;

public class CSVPushback : MonoBehaviour
{
	public FollowPath pushbackPath;

	public Transform pbTransform;

	public Transform thresholdTransform;

	public TruckTrailer pushbackTrailer;

	public Transform pushbackCar;

	public TruckTrailer resetTrailer;

	public Transform resetCar;

	public float pushbackSpeed = 16f;

	public float resetSpeed = 16f;

	public float carAccel = 2f;

	public bool isReset { get; private set; }

	public bool isHooked { get; private set; }

	private void SetToReset()
	{
		pushbackTrailer.enabled = false;
		resetCar.transform.localPosition = pushbackTrailer.transform.localPosition;
		resetCar.transform.rotation = Quaternion.LookRotation(-pushbackTrailer.transform.forward, pushbackTrailer.transform.up);
		while (pushbackTrailer.transform.childCount > 0)
		{
			pushbackTrailer.transform.GetChild(0).SetParent(resetCar, worldPositionStays: true);
		}
		resetTrailer.transform.localPosition = pbTransform.localPosition;
		resetTrailer.transform.rotation = Quaternion.LookRotation(-pbTransform.forward, pbTransform.up);
		while (pbTransform.childCount > 0)
		{
			pbTransform.GetChild(0).SetParent(resetTrailer.transform, worldPositionStays: true);
		}
		resetTrailer.enabled = true;
	}

	private void SetToPushback()
	{
		resetTrailer.enabled = false;
		pushbackCar.transform.localPosition = resetTrailer.transform.localPosition;
		pushbackCar.transform.rotation = Quaternion.LookRotation(-resetTrailer.transform.forward, resetTrailer.transform.up);
		while (resetTrailer.transform.childCount > 0)
		{
			resetTrailer.transform.GetChild(0).SetParent(pushbackCar, worldPositionStays: true);
		}
		pushbackTrailer.transform.localPosition = resetCar.localPosition;
		pushbackTrailer.transform.rotation = Quaternion.LookRotation(-resetCar.forward, resetCar.up);
		while (resetCar.childCount > 0)
		{
			resetCar.GetChild(0).SetParent(pushbackTrailer.transform, worldPositionStays: true);
		}
		pushbackTrailer.enabled = true;
	}

	private void Awake()
	{
		isReset = true;
	}

	public bool NeedsPushback(Transform hookTransform)
	{
		return Vector3.Dot(hookTransform.position - thresholdTransform.position, thresholdTransform.forward) > 0f;
	}

	public void Pushback(Transform hookTransform)
	{
		StartCoroutine(PushbackRoutine(hookTransform));
	}

	private IEnumerator PushbackRoutine(Transform hookTf)
	{
		float t = pushbackPath.GetClosestTimeWorld(pbTransform.position);
		float speed = 0f;
		isReset = false;
		Vector3 posOffset = Vector3.zero;
		while (t < 1f && !isHooked)
		{
			speed = Mathf.MoveTowards(speed, pushbackSpeed, carAccel * Time.deltaTime);
			float sqrMagnitude = Vector3.ProjectOnPlane(hookTf.position - pbTransform.position, Vector3.up).sqrMagnitude;
			if (sqrMagnitude < 1f)
			{
				posOffset = pbTransform.InverseTransformVector(pbTransform.position - hookTf.position);
				isHooked = true;
			}
			else if (sqrMagnitude < 16f)
			{
				Vector3 target = pbTransform.parent.InverseTransformPoint(hookTf.position);
				target.y = 0f;
				pbTransform.localPosition = Vector3.MoveTowards(pbTransform.localPosition, target, speed * Time.deltaTime);
			}
			else
			{
				float num = speed / pushbackPath.GetApproximateLength();
				t += num * Time.deltaTime;
				pbTransform.position = pushbackPath.GetWorldPoint(t);
				pbTransform.rotation = Quaternion.LookRotation(pushbackPath.GetWorldTangent(t));
			}
			yield return null;
		}
		while (isHooked)
		{
			pbTransform.position = hookTf.position + pbTransform.TransformVector(posOffset);
			yield return null;
		}
	}

	public void ResetPushback()
	{
		isHooked = false;
		StartCoroutine(ResetRoutine());
	}

	private IEnumerator ResetRoutine()
	{
		SetToReset();
		float t = pushbackPath.GetClosestTimeWorld(resetCar.position);
		float speed = 0f;
		while (t > 0f)
		{
			speed = Mathf.MoveTowards(speed, resetSpeed, carAccel * Time.deltaTime);
			float num = speed / pushbackPath.GetApproximateLength();
			t = Mathf.MoveTowards(t, 0f, num * Time.deltaTime);
			resetCar.position = pushbackPath.GetWorldPoint(t);
			resetCar.rotation = Quaternion.LookRotation(-pushbackPath.GetWorldTangent(t));
			yield return null;
		}
		isReset = true;
		SetToPushback();
	}
}
