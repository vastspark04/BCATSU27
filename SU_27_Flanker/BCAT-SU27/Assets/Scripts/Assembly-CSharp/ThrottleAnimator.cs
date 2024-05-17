using UnityEngine;

public class ThrottleAnimator : MonoBehaviour
{
	public Vector3 axis;

	public float maxDeflection;

	public Transform throttleTransform;

	public void SetThrottle(float t)
	{
		throttleTransform.localEulerAngles = axis.normalized * maxDeflection * t;
	}
}
