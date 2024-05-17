using UnityEngine;

[ExecuteInEditMode]
public class IKTwo : MonoBehaviour
{
	public Transform rootTransform;

	public Transform upperSegmentTransform;

	public Transform lowerSegmentTransform;

	public Transform targetTransform;

	public Transform kneeHintTransform;

	public bool limitTargetDistance = true;

	public bool warnTargetDistance;

	private float lowerSegmentLength;

	private float upperSegmentLength;

	public float overrideUpperLength = -1f;

	public float overrideLowerLength = -1f;

	public float overrideMinLength = -1f;

	public float overrideMaxLength = -1f;

	public float totalLength;

	private float minLength;

	public bool blenderBoneFix;

	private Quaternion boneFixRotation;

	public bool updateInEditMode;

	private bool started;

	private void Awake()
	{
		if (Application.isPlaying || (updateInEditMode && (bool)rootTransform && (bool)upperSegmentTransform && (bool)lowerSegmentTransform && (bool)targetTransform && (bool)kneeHintTransform))
		{
			UpdateLengths();
			boneFixRotation = Quaternion.Euler(-90f, 180f, 0f);
			started = true;
		}
	}

	[ContextMenu("Update Lengths")]
	private void UpdateLengths()
	{
		lowerSegmentLength = Vector3.Distance(lowerSegmentTransform.position, upperSegmentTransform.position);
		upperSegmentLength = Vector3.Distance(upperSegmentTransform.position, rootTransform.position);
		if (overrideUpperLength > 0f)
		{
			upperSegmentLength = overrideUpperLength;
		}
		if (overrideLowerLength > 0f)
		{
			lowerSegmentLength = overrideLowerLength;
		}
		if (overrideMaxLength > 0f)
		{
			totalLength = overrideMaxLength;
		}
		else
		{
			totalLength = lowerSegmentLength + upperSegmentLength;
		}
		if (overrideMinLength > 0f)
		{
			minLength = overrideMinLength;
		}
		else
		{
			minLength = Mathf.Clamp(Mathf.Abs(lowerSegmentLength - upperSegmentLength), 0.01f, totalLength);
		}
	}

	private void OnValidate()
	{
		if (updateInEditMode)
		{
			UpdateLengths();
		}
	}

	private void LateUpdate()
	{
		if (!Application.isPlaying)
		{
			if (!updateInEditMode)
			{
				started = false;
				return;
			}
			if (!rootTransform || !upperSegmentTransform || !lowerSegmentTransform || !targetTransform || !kneeHintTransform)
			{
				return;
			}
			if (!started)
			{
				Awake();
			}
		}
		UpdateIK();
	}

	private void UpdateIK()
	{
		if (!targetTransform)
		{
			return;
		}
		Vector3 vector = targetTransform.position;
		float num = Vector3.Distance(targetTransform.position, rootTransform.position);
		if (num > totalLength * 0.99f)
		{
			num = 0.99f * totalLength;
			vector = rootTransform.position + num * (targetTransform.position - rootTransform.position).normalized;
			if (limitTargetDistance)
			{
				targetTransform.position = vector;
			}
			if (warnTargetDistance)
			{
				Debug.LogWarning("IK target distance is beyond total segment length.");
			}
		}
		else if (num < minLength)
		{
			num = minLength;
			vector = rootTransform.position + minLength * (targetTransform.position - rootTransform.position).normalized;
			if (limitTargetDistance)
			{
				targetTransform.position = vector;
			}
			if (warnTargetDistance)
			{
				Debug.LogWarning("IK target distance is less than minimum segment length.");
			}
		}
		Vector3 vector2 = Vector3.Cross(kneeHintTransform.position - vector, rootTransform.position - vector);
		Vector3 vector3 = Vector3.Cross(-vector2, rootTransform.position - vector);
		lowerSegmentTransform.position = vector;
		lowerSegmentTransform.LookAt(lowerSegmentTransform.position + vector3, vector2);
		float num2 = Mathf.Acos((num * num + lowerSegmentLength * lowerSegmentLength - upperSegmentLength * upperSegmentLength) / (2f * num * lowerSegmentLength));
		float num3 = 90f - num2 * 57.29578f;
		if (!float.IsNaN(num3))
		{
			Vector3 vector4 = rootTransform.position - lowerSegmentTransform.position;
			if (vector4 != Vector3.zero && vector2 != Vector3.zero && vector4.normalized != vector2.normalized)
			{
				lowerSegmentTransform.rotation = Quaternion.Lerp(lowerSegmentTransform.rotation, Quaternion.LookRotation(vector4, vector2), num3 / 90f);
			}
		}
		if (blenderBoneFix)
		{
			lowerSegmentTransform.rotation *= boneFixRotation;
			upperSegmentTransform.position = lowerSegmentTransform.position + lowerSegmentTransform.up * lowerSegmentLength;
			upperSegmentTransform.LookAt(rootTransform.position, lowerSegmentTransform.forward);
			upperSegmentTransform.rotation *= boneFixRotation;
		}
		else
		{
			upperSegmentTransform.position = lowerSegmentTransform.position + lowerSegmentTransform.forward * lowerSegmentLength;
			upperSegmentTransform.LookAt(rootTransform.position, lowerSegmentTransform.up);
		}
	}

	public void SetTargetTf(Transform t)
	{
		targetTransform = t;
	}
}
