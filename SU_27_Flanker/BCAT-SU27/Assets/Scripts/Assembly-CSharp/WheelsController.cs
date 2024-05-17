using UnityEngine;

public class WheelsController : FlightControlComponent
{
	public RaySpringDamper[] suspensions;

	public Transform steeringTransform;

	public Vector3 steerAxis = Vector3.up;

	public float maxSteerAngle;

	public float steerRotationRate;

	public bool useInputCurve;

	public AnimationCurve inputCurve;

	public bool useSpeedCurve;

	public AnimationCurve speedCurve;

	public FlightInfo flightInfo;

	public Vector3 surfaceVelocity;

	private float steer;

	private float brake;

	public int leftBrakeIdx = 1;

	public int rightBrakeIdx = 2;

	private Quaternion steerLeftRot;

	private Quaternion steerRightRot;

	[HideInInspector]
	public GearAnimator gearAnimator;

	public bool useSteerPID;

	public PID steerPID;

	public float pidAngVelTarget;

	[Range(0f, 1f)]
	public float pidBlend;

	private Vector3 localAxis;

	private Quaternion zeroRotation;

	private bool hasGearAnimator;

	public bool remoteAutoSteer;

	private float brakeL;

	private float brakeR;

	private bool useTargetSteering;

	private Quaternion steerToTargetRot;

	public bool landed { get; private set; }

	private void Awake()
	{
		if (!steeringTransform)
		{
			base.enabled = false;
			return;
		}
		localAxis = steeringTransform.parent.InverseTransformDirection(steeringTransform.TransformDirection(steerAxis));
		zeroRotation = steeringTransform.localRotation;
		steerLeftRot = Quaternion.AngleAxis(0f - maxSteerAngle, localAxis) * steeringTransform.localRotation;
		steerRightRot = Quaternion.AngleAxis(maxSteerAngle, localAxis) * steeringTransform.localRotation;
		gearAnimator = GetComponentInChildren<GearAnimator>();
		if ((bool)gearAnimator)
		{
			hasGearAnimator = true;
		}
	}

	private void Update()
	{
		if (hasGearAnimator && gearAnimator.GetCurrentState() == GearAnimator.GearStates.Retracted)
		{
			landed = false;
			return;
		}
		float num = steer;
		if (remoteAutoSteer)
		{
			Vector3 vector = suspensions[0].surfaceVelocity;
			if (vector.sqrMagnitude < 0.1f)
			{
				useTargetSteering = false;
				num = 0f;
			}
			else
			{
				SteerTo(suspensions[0].point + vector.normalized * 20f);
			}
		}
		else
		{
			if (useInputCurve)
			{
				num = Mathf.Sign(steer) * inputCurve.Evaluate(Mathf.Abs(steer));
			}
			if (useSpeedCurve)
			{
				num *= speedCurve.Evaluate(flightInfo.surfaceSpeed);
			}
			if (useSteerPID)
			{
				if (suspensions[0].isTouching)
				{
					float y = flightInfo.transform.InverseTransformVector(flightInfo.rb.angularVelocity).y;
					float b = steerPID.Evaluate(y, pidAngVelTarget * num);
					num = Mathf.Lerp(num, b, pidBlend);
				}
				else
				{
					steerPID.ResetIntegrator();
				}
			}
		}
		float t = (num + 1f) / 2f;
		if (gearAnimator.targetState == GearAnimator.GearStates.Retracted)
		{
			t = 0.5f;
		}
		Quaternion to = Quaternion.Slerp(steerLeftRot, steerRightRot, t);
		if (useTargetSteering && gearAnimator.targetState == GearAnimator.GearStates.Extended)
		{
			to = steerToTargetRot;
		}
		float num2 = Mathf.Clamp(Time.deltaTime, 0.001f, 1f);
		Quaternion localRotation = Quaternion.RotateTowards(steeringTransform.localRotation, to, steerRotationRate * num2);
		if (!float.IsNaN(localRotation.x))
		{
			steeringTransform.localRotation = localRotation;
		}
		landed = false;
		for (int i = 0; i < suspensions.Length; i++)
		{
			if (!(suspensions[i] == null))
			{
				float b2 = 0f;
				if (i == leftBrakeIdx)
				{
					b2 = brakeL;
				}
				else if (i == rightBrakeIdx)
				{
					b2 = brakeR;
				}
				suspensions[i].brakePedal = Mathf.Max(brake, b2);
				if (suspensions[i].isTouching)
				{
					landed = true;
					surfaceVelocity = suspensions[i].surfaceVelocity;
				}
				else if (!landed)
				{
					surfaceVelocity = suspensions[i].rb.velocity;
				}
			}
		}
	}

	public override void SetBrakes(float brakes)
	{
		brake = brakes;
	}

	public void SetBrakeL(float bL)
	{
		brakeL = bL;
	}

	public void SetBrakeR(float bR)
	{
		brakeR = bR;
	}

	public override void SetWheelSteer(float steer)
	{
		this.steer = Mathf.Clamp(steer, -1f, 1f);
	}

	public void SetBrakeLock(int b)
	{
		bool brakeLock = b > 0;
		for (int i = 0; i < suspensions.Length; i++)
		{
			suspensions[i].brakeLock = brakeLock;
		}
	}

	public void SteerTo(Vector3 target)
	{
		useTargetSteering = true;
		Vector3 vector = steeringTransform.parent.InverseTransformPoint(target);
		vector = Vector3.ProjectOnPlane(vector, localAxis);
		float value = VectorUtils.SignedAngle(Vector3.forward, vector, Vector3.right);
		value = Mathf.Clamp(value, 0f - maxSteerAngle, maxSteerAngle);
		steerToTargetRot = Quaternion.AngleAxis(value, localAxis) * zeroRotation;
	}

	public void ResetTargetSteering()
	{
		useTargetSteering = false;
		steer = 0f;
	}
}
