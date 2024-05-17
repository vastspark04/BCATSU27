using UnityEngine;

public class KinematicPlane : FlightControlComponent
{
	public AnimationCurve maxAoAcurve;

	public AnimationCurve maxGCurve;

	public AnimationCurve rollRateCurve;

	public AnimationCurve lerpCurve;

	private Vector3 input;

	private float brake;

	private float flaps;

	public float flapsMultiplier = 1.2f;

	public float wingSweep = 20f;

	public float pitchLerpMult;

	public float rollLerpMult;

	public float yawGMult;

	public float yawAoAMult;

	public float dragArea;

	public float brakeDrag;

	private float dragFac;

	private float currRollRate;

	public Rigidbody rb;

	private float lerpedPitch;

	private Vector3 externalForces;

	private bool gotActor;

	private Actor _actor;

	private Transform myTransform;

	private float referenceMass;

	private float seaLevelAtmosDens;

	public Vector3 velocity { get; private set; }

	public Actor actor
	{
		get
		{
			if (!gotActor)
			{
				_actor = GetComponent<Actor>();
				gotActor = true;
			}
			return _actor;
		}
	}

	public bool forceDynamic { get; private set; }

	private float massAdjust => 1f;

	public override void SetPitchYawRoll(Vector3 pitchYawRoll)
	{
		input = pitchYawRoll;
	}

	public override void SetBrakes(float brakes)
	{
		base.SetBrakes(brakes);
		brake = Mathf.Clamp01(brakes);
	}

	public override void SetFlaps(float flaps)
	{
		this.flaps = flaps;
	}

	private void Awake()
	{
		myTransform = base.transform;
		if (!rb)
		{
			rb = GetComponent<Rigidbody>();
		}
		actor.fixedVelocityUpdate = rb.interpolation == RigidbodyInterpolation.None;
		MassUpdater component = GetComponent<MassUpdater>();
		if ((bool)component)
		{
			referenceMass = component.baseMass;
		}
		else
		{
			referenceMass = rb.mass;
		}
	}

	private void Start()
	{
		seaLevelAtmosDens = AerodynamicsController.fetch.AtmosDensityAtPosition(WaterPhysics.instance.transform.position);
	}

	private void FixedUpdate()
	{
		if (!rb.isKinematic)
		{
			return;
		}
		input.x = Mathf.Clamp(input.x, -1f, 1f);
		input.y = Mathf.Clamp(input.y, -1f, 1f);
		input.z = Mathf.Clamp(input.z, -1f, 1f);
		float magnitude = velocity.magnitude;
		if (float.IsNaN(magnitude))
		{
			Debug.LogWarning("Kplane speed is NaN");
			velocity = 150f * myTransform.forward;
			magnitude = 150f;
			return;
		}
		if (magnitude < 1E-05f)
		{
			Debug.LogWarning("Kplane speed is too small");
			velocity = 150f * myTransform.forward;
			magnitude = 150f;
			return;
		}
		float num = lerpCurve.Evaluate(magnitude);
		float num2 = maxAoAcurve.Evaluate(magnitude);
		float num3 = maxGCurve.Evaluate(magnitude) * 9.81f * massAdjust;
		num3 += flaps * (flapsMultiplier - 1f) * num3;
		float num4 = AerodynamicsController.fetch.AtmosDensityAtPosition(myTransform.position) / seaLevelAtmosDens;
		num3 = Mathf.Lerp(num3, num3 * num4, 0.5f);
		Quaternion b = Quaternion.LookRotation(velocity, myTransform.up) * Quaternion.Euler(input.x * num2, input.y * yawAoAMult * num2, 0f);
		lerpedPitch = Mathf.Lerp(lerpedPitch, input.x, num * pitchLerpMult * Time.fixedDeltaTime);
		Quaternion quaternion = Quaternion.Slerp(rb.rotation, b, num * pitchLerpMult * Time.fixedDeltaTime);
		currRollRate = Mathf.Lerp(currRollRate, input.z * rollRateCurve.Evaluate(magnitude), num * rollLerpMult * Time.fixedDeltaTime);
		quaternion = Quaternion.AngleAxis(currRollRate * Time.fixedDeltaTime, myTransform.forward) * quaternion;
		Vector3 vector = lerpedPitch * Vector3.Slerp(Vector3.Cross(myTransform.right, velocity).normalized, -myTransform.up, 0.5f);
		Vector3 vector2 = yawGMult * input.y * myTransform.right;
		float num5 = AerodynamicsController.fetch.AtmosDensityAtPosition(myTransform.position);
		dragFac = 0.5f * (dragArea + brakeDrag * brake + flaps * dragArea * 0.75f);
		float num6 = dragFac * magnitude * num5 * AerodynamicsController.fetch.DragMultiplierAtSpeedAndSweep(magnitude, WaterPhysics.GetAltitude(myTransform.position), wingSweep);
		externalForces += -velocity * num6;
		velocity += (externalForces / rb.mass + Physics.gravity + (vector + vector2) * num3) * Time.fixedDeltaTime;
		externalForces = Vector3.zero;
		Vector3 position = rb.position + velocity * Time.fixedDeltaTime;
		if (!float.IsNaN(position.x))
		{
			rb.MovePosition(position);
			rb.MoveRotation(quaternion);
			rb.velocity = velocity;
			actor.SetCustomVelocity(velocity);
		}
	}

	public void AddForce(Vector3 force)
	{
		externalForces += force;
	}

	public void ResetForces()
	{
		externalForces = Vector3.zero;
	}

	public void SetVelocity(Vector3 v)
	{
		if ((bool)actor && rb.isKinematic)
		{
			actor.SetCustomVelocity(v);
		}
		rb.velocity = v;
		velocity = v;
	}

	public void SetSpeed(float speed)
	{
		velocity = velocity.normalized * speed;
	}

	public void SetToDynamic()
	{
		if (!rb)
		{
			rb = GetComponent<Rigidbody>();
		}
		if (rb.isKinematic)
		{
			rb.isKinematic = false;
			rb.velocity = velocity;
		}
		if ((bool)actor)
		{
			actor.customVelocity = false;
		}
	}

	public void SetToKinematic()
	{
		if (!forceDynamic)
		{
			if (!rb)
			{
				rb = GetComponent<Rigidbody>();
			}
			if (!rb.isKinematic)
			{
				ResetForces();
				velocity = rb.velocity;
				rb.isKinematic = true;
			}
			if ((bool)actor)
			{
				actor.fixedVelocityUpdate = rb.interpolation == RigidbodyInterpolation.None;
				actor.customVelocity = true;
			}
		}
	}

	public void ForceDynamic()
	{
		forceDynamic = true;
		SetToDynamic();
	}

	public float GetTurningRadius(float speed)
	{
		float num = maxGCurve.Evaluate(speed) * 9.81f;
		return speed * speed / num;
	}
}
