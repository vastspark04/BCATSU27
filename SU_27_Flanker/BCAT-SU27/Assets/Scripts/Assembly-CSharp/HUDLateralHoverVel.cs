using UnityEngine;
using VTOLVR.DLC.Rotorcraft;

public class HUDLateralHoverVel : MonoBehaviour, IPilotReceiverHandler
{
	public GameObject displayObject;

	public Transform velRotator;

	public Transform velBar;

	public float maxSpeed;

	public float speedMultiplier;

	[Header("Optional Accel Line")]
	public Transform accelRotator;

	public Transform accelBar;

	[Header("Optional Accel Ball")]
	public Transform accelBall;

	public float maxAccel;

	public float accelMul;

	public float accelLerpRate = 10f;

	public TiltController tiltController;

	public Rigidbody rb;

	public float maxTilt = 5f;

	private FlightInfo fInfo;

	public void OnPilotReceiver(AH94PilotReceiver receiver)
	{
		fInfo = receiver.flightInfo;
		rb = fInfo.rb;
	}

	private void Start()
	{
		fInfo = GetComponentInParent<FlightInfo>();
	}

	private void Update()
	{
		if ((bool)rb && (bool)fInfo && (!tiltController || tiltController.currentTilt < maxTilt))
		{
			displayObject.SetActive(value: true);
			Vector3 velocity = rb.velocity;
			velocity.y = 0f;
			Vector3 forward = rb.transform.forward;
			forward.y = 0f;
			float num = VectorUtils.SignedAngle(forward, velocity, Vector3.Cross(Vector3.up, forward));
			velRotator.localRotation = Quaternion.Euler(0f, 0f, 0f - num);
			float y = Mathf.Clamp(velocity.magnitude, 0f, maxSpeed) * speedMultiplier;
			velBar.localScale = new Vector3(1f, y, 1f);
			if ((bool)accelBar || (bool)accelBall)
			{
				Vector3 acceleration = fInfo.acceleration;
				acceleration.y = 0f;
				if ((bool)accelBar)
				{
					VectorUtils.SignedAngle(forward, acceleration, Vector3.Cross(Vector3.up, forward));
					accelRotator.localRotation = Quaternion.Euler(0f, 0f, 0f - num);
					float y2 = Mathf.Clamp(acceleration.magnitude, 0f, maxAccel) * accelMul;
					accelBar.localScale = new Vector3(1f, y2, 1f);
				}
				if ((bool)accelBall)
				{
					Vector3 rhs = Vector3.Cross(Vector3.up, forward);
					Vector3 vector = new Vector3(Vector3.Dot(acceleration, rhs), Vector3.Dot(acceleration, forward), 0f);
					accelBall.localPosition = Vector3.Lerp(accelBall.localPosition, Vector3.ClampMagnitude(vector, maxAccel) * accelMul, accelLerpRate * Time.deltaTime);
				}
			}
		}
		else
		{
			displayObject.SetActive(value: false);
		}
	}
}
