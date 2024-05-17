using System.Collections.Generic;
using UnityEngine;

public class CamRigRotationInterpolator : MonoBehaviour
{
	private static List<CamRigRotationInterpolator> instances = new List<CamRigRotationInterpolator>();

	public float lerpRate;

	public float maxGOffset = 0.25f;

	public float gFactor = 0.025f;

	public float bodyMass = 1f;

	public float bodySpring;

	public float bodyDamper;

	public Vector3 springTgtOffset;

	public MinMax yMinMax;

	public float shakeSpring;

	public float shakeDamper;

	public float shakeMultiplier = 1f;

	private Vector3 shakeOffset;

	private Vector3 shakeVelocity;

	private Transform myTransform;

	private Transform parentTransform;

	private Vector3 localVelocity = Vector3.zero;

	private Vector3 zeroPos;

	private FlightInfo flightInfo;

	public Vector3 GetCurrentShakeVelocity()
	{
		return shakeVelocity;
	}

	public void Shake(float magnitude)
	{
		shakeVelocity += magnitude * shakeMultiplier * Random.onUnitSphere;
	}

	public void Shake(Vector3 force)
	{
		shakeVelocity += shakeMultiplier * myTransform.InverseTransformVector(force);
	}

	private void Awake()
	{
		flightInfo = GetComponentInParent<FlightInfo>();
		myTransform = base.transform;
		parentTransform = base.transform.parent;
		zeroPos = myTransform.localPosition;
		if (!GameSettings.CurrentSettings.GetBoolSetting("BODY_PHYSICS"))
		{
			base.enabled = false;
		}
	}

	private void OnEnable()
	{
		instances.Add(this);
	}

	private void OnDisable()
	{
		instances.Remove(this);
	}

	public static void ShakeAll(Vector3 force)
	{
		for (int i = 0; i < instances.Count; i++)
		{
			if ((bool)instances[i])
			{
				instances[i].Shake(force);
			}
		}
	}

	private void Start()
	{
		if (!GameSettings.CurrentSettings.GetBoolSetting("BODY_PHYSICS"))
		{
			base.enabled = false;
		}
	}

	private void LateUpdate()
	{
		if (!(Time.deltaTime > 0f))
		{
			return;
		}
		int num = Mathf.CeilToInt(Time.deltaTime / 0.011f);
		float num2 = Time.deltaTime / (float)num;
		Vector3 vector = myTransform.localPosition;
		Vector3 vector2 = parentTransform.InverseTransformVector(gFactor * -flightInfo.pilotAccel);
		Vector3 vector3 = parentTransform.InverseTransformVector(Physics.gravity);
		Vector3 vector4 = zeroPos + springTgtOffset;
		for (int i = 0; i < num; i++)
		{
			Vector3 vector5 = (vector2 + (vector4 - vector) * bodySpring - localVelocity * bodyDamper) / bodyMass;
			vector5 += vector3;
			localVelocity += vector5 * num2;
			Vector3 vector6 = vector + localVelocity * num2 - zeroPos;
			if ((vector6.y > yMinMax.max && localVelocity.y > 0f) || (vector6.y < yMinMax.min && localVelocity.y < 0f))
			{
				vector6.y = Mathf.Clamp(vector6.y, yMinMax.min, yMinMax.max);
				localVelocity.y = 0f;
			}
			if (float.IsNaN(vector6.x) || float.IsNaN(shakeOffset.x))
			{
				vector6 = Vector3.zero;
				localVelocity = Vector3.zero;
				shakeVelocity = Vector3.zero;
				shakeOffset = Vector3.zero;
				i = num;
				continue;
			}
			if (vector6.magnitude > maxGOffset)
			{
				vector6 = Vector3.ClampMagnitude(vector6, maxGOffset);
				localVelocity = Vector3.ProjectOnPlane(localVelocity, vector6);
			}
			Vector3 b = zeroPos + vector6;
			vector = Vector3.Lerp(vector, b, lerpRate * num2) + shakeOffset;
			shakeOffset += shakeVelocity * num2;
			shakeVelocity += ((0f - shakeSpring) * shakeOffset + -shakeVelocity * shakeDamper) * num2;
		}
		myTransform.localPosition = vector;
	}

	public void ResetAndDisable()
	{
		myTransform.localPosition = zeroPos;
		base.enabled = false;
	}
}
