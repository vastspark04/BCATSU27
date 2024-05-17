using System;
using UnityEngine;

public class AdvancedRadarController : MonoBehaviour
{
	public LockingRadar lockingRadar;

	public Transform referenceForwardTf;

	public float maxElevationOffset = 30f;

	public Transform elevationTransform;

	public float elevationAdjustSpeed = 12f;

	public float currentElevationAdjust;

	public bool boresightMode;

	public Transform overrideLookatTransform;

	public event Action<float> OnElevationAdjusted;

	private void Start()
	{
		this.OnElevationAdjusted?.Invoke(currentElevationAdjust);
	}

	private void Update()
	{
		if (!lockingRadar)
		{
			return;
		}
		if (boresightMode)
		{
			elevationTransform.localRotation = Quaternion.identity;
			return;
		}
		if ((bool)overrideLookatTransform)
		{
			elevationTransform.rotation = Quaternion.LookRotation(overrideLookatTransform.position - elevationTransform.position, elevationTransform.parent.up);
			return;
		}
		Vector3 forward = referenceForwardTf.forward;
		forward.y = 0f;
		float num = VectorUtils.SignedAngle(forward, referenceForwardTf.forward, Vector3.up);
		float angle = currentElevationAdjust;
		if (lockingRadar.IsLocked())
		{
			Vector3 planeNormal = Vector3.Cross(Vector3.up, referenceForwardTf.forward);
			Vector3 toDirection = Vector3.ProjectOnPlane(lockingRadar.currentLock.actor.position - elevationTransform.position, planeNormal);
			angle = Mathf.Clamp(VectorUtils.SignedAngle(forward, toDirection, Vector3.up), num - maxElevationOffset, num + maxElevationOffset);
		}
		Vector3 vector = Vector3.Cross(Vector3.up, forward);
		Vector3 target = Quaternion.AngleAxis(angle, -vector) * forward;
		target = Vector3.RotateTowards(referenceForwardTf.forward, target, maxElevationOffset * ((float)Math.PI / 180f), 0f);
		elevationTransform.rotation = Quaternion.LookRotation(target, Vector3.up);
	}

	public void OnElevationInput(float input)
	{
		input = Mathf.Clamp(input, -1f, 1f);
		currentElevationAdjust = Mathf.Clamp(currentElevationAdjust + input * elevationAdjustSpeed * Time.deltaTime, 0f - maxElevationOffset, maxElevationOffset);
		this.OnElevationAdjusted?.Invoke(currentElevationAdjust);
	}
}
