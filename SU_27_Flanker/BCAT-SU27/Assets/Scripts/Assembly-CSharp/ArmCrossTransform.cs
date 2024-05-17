using UnityEngine;

public class ArmCrossTransform : MonoBehaviour
{
	public Transform forward;

	public Transform leftHand;

	public Transform rightHand;

	public float maxDist = 1f;

	public float rotMagnitude = 5f;

	private void LateUpdate()
	{
		Vector3 vector = forward.InverseTransformPoint(leftHand.position);
		Vector3 upwards = Quaternion.AngleAxis(Mathf.Clamp(forward.InverseTransformPoint(rightHand.position).y - vector.y, 0f - maxDist, maxDist) * rotMagnitude, forward.forward) * forward.up;
		base.transform.rotation = Quaternion.LookRotation(forward.forward, upwards);
	}
}
