using UnityEngine;

public class RudderFootAnimator : FlightControlComponent
{
	public Vector3 axis;

	public float distance;

	public Transform leftFoot;

	private Vector3 leftFootStartPos;

	public Transform rightFoot;

	private Vector3 rightFootStartPos;

	private Vector3 localAxis;

	public float lerpRate = 15f;

	private Vector3 pitchYawRoll;

	private void OnDrawGizmosSelected()
	{
		if ((bool)rightFoot)
		{
			Gizmos.color = Color.green;
			Vector3 to = rightFoot.TransformPoint(axis);
			Gizmos.DrawLine(rightFoot.transform.position, to);
		}
	}

	private void Start()
	{
		leftFootStartPos = leftFoot.transform.localPosition;
		rightFootStartPos = rightFoot.transform.localPosition;
		localAxis = rightFoot.parent.InverseTransformDirection(rightFoot.TransformDirection(axis));
	}

	public override void SetPitchYawRoll(Vector3 pitchYawRoll)
	{
		this.pitchYawRoll = pitchYawRoll;
	}

	private void Update()
	{
		float num = pitchYawRoll.y * distance;
		rightFoot.localPosition = Vector3.Lerp(rightFoot.localPosition, rightFootStartPos + num * localAxis, lerpRate * Time.deltaTime);
		leftFoot.localPosition = Vector3.Lerp(leftFoot.localPosition, leftFootStartPos - num * localAxis, lerpRate * Time.deltaTime);
	}
}
