using UnityEngine;

public class FlightAssist : FlightControlComponent
{
	public Battery battery;

	public float powerDrain = 1f;

	public float pitchAngVel;

	public float yawAngVel;

	public float rollAngVel;

	public AnimationCurve pitchInputCurve;

	public AnimationCurve yawInputCurve;

	public AnimationCurve rollInputCurve;

	private Rigidbody rb;

	private Vector3 stick;

	private Vector3 _assistedStick;

	public bool assistEnabled = true;

	public PID pitchPID;

	public PID yawPID;

	public PID rollPID;

	public bool pitch = true;

	public bool yaw = true;

	public bool roll = true;

	public bool pitchGLimiter;

	public float gLimit = 9f;

	public float yawStabilityFactor = 0.2f;

	public float yawStabilityDamper = 1f;

	public float alphaRudderRollFactor = 0.1f;

	public bool useSpeedInputCurve;

	public AnimationCurve speedInputCurve;

	public FlightInfo flightInfo;

	public FlightControlComponent[] outputs;

	[Header("Take Off Trim")]
	public bool takeOffTrim;

	public float takeOffTrimPitch = -0.35f;

	public float takeOffTrimFadeRate = 0.2f;

	public float takeOffTrimPitchTarget = 10f;

	public float takeOffTrimMaxSpeed = 92f;

	private float takeOffTrimAmt;

	private bool takeOffTrimProgramEnd;

	[Header("Auto Trim")]
	public bool autoTrimPitch = true;

	public AnimationCurve pitchTrimCurve;

	public bool controlFlaps;

	private float _flaps;

	private WindMaster windMaster;

	private float _brakes;

	public bool bypassBrakes;

	[HideInInspector]
	public float debug_gLimitedAngVel;

	[HideInInspector]
	public Vector3 debug_stickAngVel;

	[HideInInspector]
	public float debug_actualPitchVel;

	public Vector3 assistedStick => _assistedStick;

	public bool isLimitingGs { get; private set; }

	public override void SetBrakes(float brakes)
	{
		_brakes = brakes;
	}

	private void Start()
	{
		rb = GetComponentInParent<Rigidbody>();
		if (Input.GetKey(KeyCode.Q))
		{
			assistEnabled = false;
		}
	}

	public override void SetPitchYawRoll(Vector3 pitchYawRoll)
	{
		stick = pitchYawRoll;
	}

	private void FixedUpdate()
	{
		_assistedStick = stick;
		Vector3 zero = Vector3.zero;
		Vector3 vector = new Vector3(Mathf.Sign(stick.x) * pitchInputCurve.Evaluate(Mathf.Abs(stick.x)), Mathf.Sign(stick.y) * yawInputCurve.Evaluate(Mathf.Abs(stick.y)), Mathf.Sign(stick.z) * rollInputCurve.Evaluate(Mathf.Abs(stick.z)));
		vector.x += takeOffTrimAmt;
		bool flag = false;
		if (takeOffTrim)
		{
			if (takeOffTrimProgramEnd)
			{
				flag = true;
				if (flightInfo.isLanded && flightInfo.airspeed < 10f)
				{
					takeOffTrimProgramEnd = false;
				}
			}
			else if (flightInfo.airspeed > 10f && flightInfo.airspeed < takeOffTrimMaxSpeed && flightInfo.pitch < takeOffTrimPitchTarget)
			{
				takeOffTrimAmt = Mathf.Lerp(takeOffTrimPitch / 3f, takeOffTrimPitch, Mathf.Clamp01(flightInfo.surfaceSpeed / 50f));
			}
			else
			{
				if (!flightInfo.isLanded)
				{
					Debug.Log("FlightAssist take-off trim program end. Airspeed: " + flightInfo.airspeed + ", pitch: " + flightInfo.pitch);
					takeOffTrimProgramEnd = true;
				}
				flag = true;
			}
		}
		else
		{
			flag = true;
		}
		if (flag)
		{
			takeOffTrimAmt = Mathf.MoveTowards(takeOffTrimAmt, 0f, takeOffTrimFadeRate * Time.fixedDeltaTime);
		}
		if (assistEnabled && (!battery || battery.Drain(powerDrain * Time.fixedDeltaTime)))
		{
			Vector3 vector2 = rb.transform.InverseTransformVector(rb.angularVelocity);
			Vector3 vector3 = new Vector3(vector.x * pitchAngVel, vector.y * yawAngVel, vector.z * rollAngVel);
			isLimitingGs = false;
			if (pitchGLimiter)
			{
				float num = GLimitedAngVel();
				if (Mathf.Abs(vector3.x) > num)
				{
					isLimitingGs = true;
					vector3.x = Mathf.Clamp(vector3.x, 0f - num, num);
				}
				debug_gLimitedAngVel = num;
				debug_stickAngVel = vector3;
				debug_actualPitchVel = vector2.x;
			}
			bool i = !flightInfo.isLanded;
			Vector3 vector4 = new Vector3(pitchPID.Evaluate(vector2.x, vector3.x, _p: true, i), yawPID.Evaluate(vector2.y, vector3.y * 0.85f), rollPID.Evaluate(vector2.z, vector3.z, _p: true, i));
			if (pitch)
			{
				_assistedStick.x = vector4.x;
			}
			else
			{
				_assistedStick.x = vector.x;
			}
			if (yaw)
			{
				_assistedStick.y = vector4.y;
			}
			else
			{
				_assistedStick.y = vector.y;
			}
			if (roll)
			{
				_assistedStick.z = vector4.z;
			}
			else
			{
				_assistedStick.z = vector.z;
			}
			_assistedStick.x = Mathf.Clamp(_assistedStick.x, -1f, 1f);
			_assistedStick.y = Mathf.Clamp(_assistedStick.y, -1f, 1f);
			_assistedStick.z = Mathf.Clamp(_assistedStick.z, -1f, 1f);
			if (useSpeedInputCurve)
			{
				_assistedStick *= speedInputCurve.Evaluate(flightInfo.airspeed);
			}
			if (yaw)
			{
				Vector3 velocity = rb.velocity;
				if (WindVolumes.windEnabled)
				{
					windMaster = rb.gameObject.GetComponent<WindMaster>();
					if (!windMaster)
					{
						windMaster = rb.gameObject.AddComponent<WindMaster>();
					}
					velocity -= windMaster.wind;
				}
				float num2 = Mathf.Clamp01(0.9f - Mathf.Abs(stick.y * 1.5f));
				float num3 = Mathf.Clamp01((flightInfo.airspeed - 40f) / 80f);
				float num4 = yawStabilityDamper * vector2.y;
				float num5 = yawStabilityFactor * Vector3.Dot(base.transform.right, Vector3.ProjectOnPlane(velocity, base.transform.up).normalized);
				float num6 = Mathf.Abs(assistedStick.z);
				float num7 = (num5 - num4) * (num2 * num3 + num6);
				float num8 = 0f - Vector3.Dot(base.transform.up, Vector3.ProjectOnPlane(velocity, base.transform.right).normalized);
				float num9 = num3 * alphaRudderRollFactor * num8 * assistedStick.z;
				_assistedStick.y = Mathf.Clamp(_assistedStick.y + num7 + num9, -1f, 1f);
			}
			if (autoTrimPitch)
			{
				zero.x = pitchTrimCurve.Evaluate(flightInfo.airspeed);
			}
		}
		else
		{
			_assistedStick = vector;
			isLimitingGs = false;
		}
		for (int j = 0; j < outputs.Length; j++)
		{
			outputs[j].SetTrim(zero);
			outputs[j].SetPitchYawRoll(_assistedStick);
			if (controlFlaps)
			{
				outputs[j].SetFlaps(_flaps);
			}
			if (bypassBrakes)
			{
				outputs[j].SetBrakes(_brakes);
			}
		}
	}

	private float GLimitedAngVel()
	{
		return (gLimit - Vector3.Dot(base.transform.up, Vector3.up)) * 9.81f / flightInfo.airspeed;
	}

	public void ToggleAssist()
	{
		assistEnabled = !assistEnabled;
	}

	public void SetMasterAssist(int st)
	{
		assistEnabled = st > 0;
	}

	public void TogglePitch()
	{
		pitch = !pitch;
	}

	public void SetPitchSAS(int state)
	{
		pitch = state != 0;
	}

	public void ToggleYaw()
	{
		yaw = !yaw;
	}

	public void SetYawSAS(int state)
	{
		yaw = state != 0;
	}

	public void ToggleRoll()
	{
		roll = !roll;
	}

	public void SetRollSAS(int state)
	{
		roll = state != 0;
	}

	public void SetTakeoffTrim(int state)
	{
		takeOffTrim = state != 0;
	}

	public void TogglePitchTrim()
	{
		autoTrimPitch = !autoTrimPitch;
	}

	public void SetPitchAutoTrim(int state)
	{
		autoTrimPitch = state != 0;
	}

	public void DisableAssist()
	{
		assistEnabled = false;
	}

	public void SetGLimiter(int state)
	{
		pitchGLimiter = state != 0;
	}

	public override void SetFlaps(float flaps)
	{
		_flaps = flaps;
	}
}
