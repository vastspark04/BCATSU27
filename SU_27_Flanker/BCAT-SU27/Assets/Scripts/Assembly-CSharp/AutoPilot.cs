using System;
using System.Collections.Generic;
using UnityEngine;

public class AutoPilot : MonoBehaviour, IQSVehicleComponent
{
	public enum SteerModes
	{
		Stable,
		Aim
	}

	public FlightInfo flightInfo;

	public FlightControlComponent[] outputs;

	public WheelsController wheelsController;

	public float maxOutputSpeed = -1f;

	public float maxThrottleSpeed = -1f;

	private Vector2 lastPitchRoll;

	private float lastYaw;

	private float lastThrottle;

	public PID pitchPID;

	public PID yawPID;

	public PID rollPID;

	public float rollDotPower = 15f;

	public bool useAimModePID;

	public AttitudePIDSet aimModePID;

	public float rollUpBias;

	public float maxBank = 190f;

	public float maxClimb = -1f;

	public float maxDescend = -1f;

	public Transform wheelSteerReferenceTf;

	public PID wheelSteerPID;

	public bool useWheelSteerPID;

	public List<ModuleEngine> engines;

	public PID throttlePID;

	public bool controlThrottle = true;

	public Vector3 targetPosition;

	public bool debugTsdp;

	private float _tspd;

	public SteerModes steerMode;

	public Rigidbody rb;

	public float dynamicInputFactor;

	public float inputLimiter = 1f;

	public float throttleLimiter = 1f;

	public Transform referenceTransform;

	public float angularRollFactor;

	public UpdateModes updateMode;

	public bool gentleSteer;

	private float gentleSteerRate = 8f;

	private Vector3 gentleSteerDir;

	private Vector3 lastTargetVector;

	private bool useRollOverride;

	private Vector3 overrideRollTarget;

	private bool useOverrideRudder;

	private float overrideRudder;

	public bool debug;

	private Vector3 output;

	private float outputThrottle;

	private bool overridingThrottle;

	private float overrideThrottle;

	private bool overridingBrakes;

	private float overrideBrakes;

	private float _flaps;

	private Vector3 lastVelocity;

	private float stableVectorSlerpRate = 25f;

	public float targetSpeed
	{
		get
		{
			return _tspd;
		}
		set
		{
			if (value != _tspd)
			{
				_tspd = value;
				if (debugTsdp)
				{
					Debug.Log("Set tspd: " + value);
				}
			}
		}
	}

	public Vector3 acceleration { get; private set; }

	public float currentSpeed { get; private set; }

	public float gForce { get; private set; }

	public void SetOverrideRollTarget(Vector3 rollTarget)
	{
		useRollOverride = true;
		overrideRollTarget = rollTarget;
	}

	public void SetOverrideRudder(float rudder)
	{
		useOverrideRudder = true;
		overrideRudder = rudder;
	}

	public void OverrideSetThrottle(float t)
	{
		overridingThrottle = true;
		overrideThrottle = t;
	}

	public void OverrideSetBrakes(float b)
	{
		overrideBrakes = b;
		overridingBrakes = true;
	}

	public void SetFlaps(float f)
	{
		_flaps = f;
	}

	private void Awake()
	{
		if (!flightInfo)
		{
			flightInfo = GetComponent<FlightInfo>();
			if (!flightInfo)
			{
				flightInfo = base.gameObject.AddComponent<FlightInfo>();
			}
		}
		throttlePID.updateMode = UpdateModes.Dynamic;
	}

	private void Start()
	{
		FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		targetPosition += offset;
	}

	private void FixedUpdate()
	{
		currentSpeed = flightInfo.airspeed;
		if (flightInfo.isLanded)
		{
			currentSpeed = flightInfo.surfaceSpeed;
		}
		UpdateMeasurements(Time.fixedDeltaTime);
		if (!overridingThrottle)
		{
			if (targetSpeed < 0.01f)
			{
				outputThrottle = -1f;
			}
			else
			{
				outputThrottle = throttlePID.Evaluate(currentSpeed, targetSpeed);
				outputThrottle = Mathf.Clamp(outputThrottle, -1f, throttleLimiter);
				if (maxThrottleSpeed > 0f)
				{
					lastThrottle = Mathf.MoveTowards(lastThrottle, outputThrottle, maxThrottleSpeed * Time.deltaTime);
					outputThrottle = lastThrottle;
				}
			}
		}
		if (updateMode == UpdateModes.Fixed)
		{
			UpdateAutopilot(Time.fixedDeltaTime);
		}
	}

	private void Update()
	{
		if (updateMode == UpdateModes.Dynamic)
		{
			UpdateAutopilot(Time.deltaTime);
		}
	}

	private void LateUpdate()
	{
		if (updateMode == UpdateModes.LateDynamic)
		{
			UpdateAutopilot(Time.deltaTime);
		}
	}

	private void UpdateAutopilot(float deltaTime)
	{
		if (deltaTime == 0f)
		{
			deltaTime = Time.fixedDeltaTime;
		}
		if (!referenceTransform)
		{
			referenceTransform = base.transform;
		}
		PID pID = pitchPID;
		PID pID2 = yawPID;
		PID pID3 = rollPID;
		if (useAimModePID && steerMode == SteerModes.Aim)
		{
			aimModePID.SetUpdateMode(updateMode);
			pID = aimModePID.pitchPID;
			pID2 = aimModePID.yawPID;
			pID3 = aimModePID.rollPID;
		}
		pID.updateMode = (pID2.updateMode = (pID3.updateMode = updateMode));
		if (useWheelSteerPID)
		{
			wheelSteerPID.updateMode = updateMode;
		}
		Vector3 vector = rb.velocity;
		Vector3 vector2 = ((updateMode == UpdateModes.Fixed) ? (rb.rotation * base.transform.InverseTransformDirection(referenceTransform.forward)) : referenceTransform.forward);
		Vector3 vector3 = ((updateMode == UpdateModes.Fixed) ? (rb.rotation * base.transform.InverseTransformDirection(referenceTransform.up)) : referenceTransform.up);
		Vector3 vector4 = vector3;
		Vector3 vector5;
		Vector3 vector6;
		if (updateMode != 0)
		{
			vector5 = targetPosition - referenceTransform.position;
			vector6 = referenceTransform.right;
		}
		else
		{
			vector5 = targetPosition - (rb.position + (referenceTransform.position - rb.transform.position));
			vector6 = rb.rotation * base.transform.InverseTransformDirection(referenceTransform.right);
		}
		if (gentleSteer)
		{
			gentleSteerDir = Vector3.RotateTowards(gentleSteerDir, vector5, gentleSteerRate * ((float)Math.PI / 180f) * deltaTime, float.MaxValue);
			vector5 = gentleSteerDir;
		}
		else
		{
			gentleSteerDir = vector5;
		}
		Vector3 current = new Vector3(vector5.x, 0f, vector5.z);
		if (maxDescend > 0f && vector5.y < 0f)
		{
			vector5 = Vector3.RotateTowards(current, vector5, maxDescend * ((float)Math.PI / 180f), 0f);
		}
		if (maxClimb > 0f && vector5.y > 0f)
		{
			vector5 = Vector3.RotateTowards(current, vector5, maxClimb * ((float)Math.PI / 180f), 0f);
		}
		if (steerMode == SteerModes.Aim)
		{
			vector = ((updateMode != 0) ? referenceTransform.forward : vector2);
		}
		else
		{
			vector5 = Vector3.Slerp(lastTargetVector, vector5, stableVectorSlerpRate * deltaTime);
		}
		float value = 0f;
		float num = 0f;
		float num2 = 0f;
		float wheelSteer = 0f;
		if (useOverrideRudder)
		{
			num = 0f - overrideRudder;
			useOverrideRudder = false;
			wheelSteer = 0f - num;
		}
		else if (steerMode == SteerModes.Stable && !flightInfo.isLanded)
		{
			float current2 = VectorUtils.SignedAngle(vector2, Vector3.ProjectOnPlane(rb.velocity, vector4), vector6);
			num = pID2.Evaluate(current2, 0f);
		}
		else
		{
			PID pID4 = pID2;
			Transform transform = referenceTransform;
			if (flightInfo.isLanded && (bool)wheelSteerReferenceTf && currentSpeed < 20f)
			{
				transform = wheelSteerReferenceTf;
				if (useWheelSteerPID)
				{
					pID4 = wheelSteerPID;
				}
			}
			Vector3 vector7 = targetPosition - transform.position;
			float current3 = VectorUtils.SignedAngle(transform.forward, Vector3.ProjectOnPlane(vector7, vector4), vector6);
			num = pID4.Evaluate(current3, 0f);
			wheelSteer = 0f - num;
		}
		if (!flightInfo.isLanded || flightInfo.airspeed > 10f)
		{
			Vector3 angularRollVector = Vector3.ProjectOnPlane((vector5.normalized - lastTargetVector.normalized) / deltaTime, vector) * angularRollFactor;
			Vector3 vector8;
			if (useRollOverride)
			{
				useRollOverride = false;
				vector8 = Vector3.ClampMagnitude(overrideRollTarget, 1f);
			}
			else
			{
				vector8 = RollTargetVersion2(vector5, angularRollVector, vector3);
			}
			Vector3 vector9 = Vector3.ProjectOnPlane(vector8, vector2);
			float value2 = Vector3.Dot(vector9.normalized, vector4);
			if (maxBank < 180f)
			{
				vector9 = Vector3.RotateTowards(Vector3.up, vector9, maxBank * ((float)Math.PI / 180f), float.MaxValue);
			}
			float current4 = VectorUtils.SignedAngle(vector4, vector9, vector6);
			num2 = pID3.Evaluate(current4, 0f);
			if (steerMode == SteerModes.Aim)
			{
				num2 += 0.2f * num;
			}
			Vector3 toDirection = Vector3.ProjectOnPlane(vector5, vector6);
			Vector3 vector10 = Vector3.ProjectOnPlane(vector, vector6);
			Vector3 referenceRight = Vector3.Cross(-vector6, vector10);
			float current5 = VectorUtils.SignedAngle(vector10, toDirection, referenceRight);
			value = Mathf.Pow(Mathf.Clamp01(value2), rollDotPower) * pID.Evaluate(current5, 0f);
		}
		if (overridingThrottle)
		{
			overridingThrottle = false;
			outputThrottle = overrideThrottle;
		}
		if (controlThrottle)
		{
			foreach (ModuleEngine engine in engines)
			{
				engine.SetThrottle(outputThrottle);
			}
		}
		float brakes = 0f;
		if (outputThrottle < 0f)
		{
			brakes = 0f - outputThrottle;
		}
		if (overridingBrakes)
		{
			overridingBrakes = false;
			brakes = overrideBrakes;
		}
		inputLimiter = Mathf.Clamp01(inputLimiter);
		value = Mathf.Clamp(value, 0f - inputLimiter, inputLimiter);
		num = Mathf.Clamp(num, 0f - inputLimiter, inputLimiter);
		num2 = Mathf.Clamp(num2, 0f - inputLimiter, inputLimiter);
		if (float.IsNaN(value))
		{
			Debug.LogError(base.gameObject.name + "autopilot pitch is NaN");
		}
		if (maxOutputSpeed > 0f)
		{
			lastPitchRoll = Vector2.MoveTowards(lastPitchRoll, new Vector2(value, num2), maxOutputSpeed * deltaTime);
			lastYaw = Mathf.MoveTowards(lastYaw, num, maxOutputSpeed * deltaTime);
			value = lastPitchRoll.x;
			num = lastYaw;
			num2 = lastPitchRoll.y;
		}
		if (flightInfo.isLanded && currentSpeed < 20f)
		{
			value = (num2 = 0f);
		}
		output = new Vector3(value, 0f - num, num2);
		for (int i = 0; i < outputs.Length; i++)
		{
			outputs[i].SetPitchYawRoll(output);
			outputs[i].SetBrakes(brakes);
			if (controlThrottle)
			{
				outputs[i].SetThrottle(outputThrottle);
			}
			if (!wheelsController)
			{
				if (flightInfo.isLanded)
				{
					outputs[i].SetWheelSteer(wheelSteer);
				}
				else
				{
					outputs[i].SetWheelSteer(0f);
				}
			}
			outputs[i].SetFlaps(_flaps);
		}
		if ((bool)wheelsController)
		{
			if (flightInfo.isLanded)
			{
				wheelsController.SteerTo(targetPosition);
			}
			else
			{
				wheelsController.ResetTargetSteering();
			}
		}
		lastTargetVector = vector5;
	}

	private void UpdateMeasurements(float deltaTime)
	{
		acceleration = (rb.velocity - lastVelocity) / deltaTime;
		lastVelocity = rb.velocity;
		gForce = (acceleration - Physics.gravity).magnitude / 9.81f;
	}

	private Vector3 RollTargetVersion1(Vector3 targetVector, Vector3 angularRollVector, Vector3 tfUp)
	{
		return Vector3.ClampMagnitude(targetVector, 1f) + angularRollVector + 0.02f * tfUp + rollUpBias * (125f / Mathf.Max(0.1f, currentSpeed)) * Vector3.up;
	}

	private Vector3 RollTargetVersion2(Vector3 targetVector, Vector3 angularRollVector, Vector3 tfUp)
	{
		return (targetVector.normalized - rb.velocity.normalized) * 1f + (angularRollVector + 0.01f * tfUp) + rollUpBias * (125f / Mathf.Max(0.1f, currentSpeed)) * Vector3.up;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode("AutoPilot");
		configNode.AddNode(pitchPID.SaveToNode("pitchPID"));
		configNode.AddNode(yawPID.SaveToNode("yawPID"));
		configNode.AddNode(rollPID.SaveToNode("rollPID"));
		configNode.SetValue("targetPosition", new FixedPoint(targetPosition));
		configNode.SetValue("gentleSteerDir", gentleSteerDir);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("AutoPilot");
		if (node != null)
		{
			pitchPID.LoadFromNode(node.GetNode("pitchPID"));
			yawPID.LoadFromNode(node.GetNode("yawPID"));
			rollPID.LoadFromNode(node.GetNode("rollPID"));
			gentleSteerDir = node.GetValue<Vector3>("gentleSteerDir");
			targetPosition = node.GetValue<FixedPoint>("targetPosition").point;
		}
	}
}
