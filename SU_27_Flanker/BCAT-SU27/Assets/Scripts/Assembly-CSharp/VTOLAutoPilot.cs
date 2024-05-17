using System;
using UnityEngine;
using VTOLVR.DLC.Rotorcraft;

public class VTOLAutoPilot : FlightControlComponent, IQSVehicleComponent
{
	public delegate void APSettingDelegate(bool apEnabled);

	public Battery battery;

	public float componentElectricDrain;

	public FlightInfo flightInfo;

	public AutoPilot autoPilot;

	public FlightControlComponent[] bypassOutputs;

	public FlightControlComponent[] finalThrottleOutputs;

	public HelicopterRotor[] rotorOutputs;

	public TiltController tiltController;

	public UIImageToggle hoverIndicator;

	public UIImageToggle navIndicator;

	public UIImageToggle headingIndicator;

	public UIImageToggle altitudeIndicator;

	public UIImageToggle speedIndicator;

	public HUDJoyIndicator hudJoyIndicator;

	private FlightWarnings flightWarnings;

	private float bypassABThreshold;

	private Rigidbody rb;

	public float speedToHold;

	private float bankToHold;

	public float headingToHold;

	public float altitudeToHold;

	public float maxAPBank = 45f;

	public float maxAPRollRate = 20f;

	[Space]
	[Header("Altitude")]
	public PID altitudePitchPID;

	public PID altitudeClimbPID;

	public float minAltHoldSpeed = 30f;

	[Tooltip("Negative (min) is pitching up")]
	public MinMax altitudePitchStickMinMax = new MinMax(-0.25f, 0.2f);

	[Header("Heading")]
	public PID headingTurnPID;

	public PID headingRollPID;

	[Header("Hover")]
	public bool squareHoverInputs = true;

	public float maxHoverBank = 35f;

	public PID hoverPitchPID;

	public PID hoverRollPID;

	public PID hoverThrottlePID;

	public bool hoverAccountTWR = true;

	public float baselineHoverThrottle = 0.35f;

	public bool hoverHoldYaw;

	public PID hoverHoldYawPID;

	private Vector3 hoverHoldYawDir;

	public float hoverControlSpeed = 15f;

	public float maxTargetAltOffset = 5f;

	public float hoverYawRate = 40f;

	public bool hoverHoldGPS;

	public PID3 hoverHoldGPSPID;

	private FixedPoint hoverHoldPoint;

	private bool doOvrdYaw;

	private Vector3 overrideYawDir;

	public PID overrideYawPID;

	public Vector3 targetZeroVel = Vector3.zero;

	public float hoverMaxThrottle = 1f;

	public bool hoverControlsAB = true;

	[Header("Nav")]
	public float navInputLimiter = 0.7f;

	public PID navAltitudePID;

	public ModuleEngine[] engines;

	private float totalMaxThrust;

	private Vector3 stickPYR;

	private float throttleInput;

	private int isAB = -1;

	private float abSwitchTime;

	public float minAbSwitchDuration = 1f;

	public bool disableAutoAB = true;

	private float smoothRollTarget;

	[Header("FTF Hover Mode")]
	public float minFTFTWR = 1.1f;

	public float minFTFRollTWR = 1.05f;

	public float ftfMaxPitch = 5f;

	public float ftfMaxRoll = 35f;

	public bool ftfHoverMode;

	public PID ftfHoverAltitudePID;

	public AnimationCurve ftfTransitionSpeedCurve;

	public AnimationCurve ftfPitchTransotionSpeedCurve;

	public float maxFtfHoverSpeed;

	public FloatEvent OnSetFTFThrottle;

	public Wing[] wings;

	public AnimationCurve stdFlightAoACurve;

	[Header("Speed")]
	public bool speedAPMovesThrottle;

	public VRThrottle throttle;

	private Vector3 debug_altHoldVectorTarget;

	private Vector3 debug_altHoldVectorCurr;

	private float apThrottle;

	private bool warnDisable;

	public bool headingHold { get; private set; }

	public bool altitudeHold { get; private set; }

	public bool hoverMode { get; private set; }

	public bool navMode { get; private set; }

	public bool speedMode { get; private set; }

	public event APSettingDelegate OnHeadingHold;

	public event APSettingDelegate OnAltitudeHold;

	public event APSettingDelegate OnHoverMode;

	public event APSettingDelegate OnNavMode;

	public event Action OnTriggeredNavDisable;

	public event Action OnTriggeredHeadingDisable;

	public event Action OnTriggeredHoverDisable;

	public event Action OnTriggeredAltDisable;

	public void SetOverrideHoverYaw(Vector3 dir)
	{
		overrideYawDir = dir;
		doOvrdYaw = true;
	}

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		totalMaxThrust = 0f;
		for (int i = 0; i < engines.Length; i++)
		{
			totalMaxThrust += engines[i].maxThrust;
			if (disableAutoAB)
			{
				engines[i].autoAB = false;
			}
			bypassABThreshold = engines[i].autoABThreshold;
		}
		flightWarnings = GetComponentInParent<FlightWarnings>();
	}

	private void Start()
	{
		altitudePitchPID.updateMode = UpdateModes.Fixed;
		altitudeClimbPID.updateMode = UpdateModes.Fixed;
		headingTurnPID.circular = (headingRollPID.circular = true);
		headingTurnPID.updateMode = (headingRollPID.updateMode = UpdateModes.Dynamic);
		hoverPitchPID.updateMode = UpdateModes.Fixed;
		hoverRollPID.updateMode = UpdateModes.Fixed;
		hoverThrottlePID.updateMode = UpdateModes.Fixed;
		navAltitudePID.updateMode = UpdateModes.Fixed;
		overrideYawPID.updateMode = UpdateModes.Fixed;
	}

	private void UpdateHoverMode()
	{
		if (ftfHoverMode)
		{
			UpdateFTFHover();
			return;
		}
		Vector3 vector = rb.rotation * Vector3.forward;
		Vector3 vector2 = rb.rotation * Vector3.forward;
		float overrideRudder = stickPYR.y;
		if (doOvrdYaw)
		{
			doOvrdYaw = false;
			overrideYawDir = base.transform.InverseTransformDirection(overrideYawDir);
			overrideYawDir.y = 0f;
			float current = VectorUtils.SignedAngle(Vector3.forward, overrideYawDir, Vector3.right);
			overrideRudder = 0f - overrideYawPID.Evaluate(current, 0f);
		}
		if (hoverHoldYaw)
		{
			hoverHoldYawDir = VectorUtils.BearingVector(headingToHold);
			hoverHoldYawDir = Quaternion.AngleAxis(hoverYawRate * stickPYR.y * Time.deltaTime, Vector3.up) * hoverHoldYawDir;
			Vector3 toDirection = autoPilot.referenceTransform.InverseTransformDirection(hoverHoldYawDir);
			toDirection.y = 0f;
			headingToHold = VectorUtils.Bearing(hoverHoldYawDir);
			float current2 = VectorUtils.SignedAngle(Vector3.forward, toDirection, Vector3.right);
			overrideRudder = 0f - hoverHoldYawPID.Evaluate(current2, 0f);
		}
		vector.y = 0f;
		vector2.y = 0f;
		vector2.Normalize();
		Vector3 normalized = Vector3.Cross(Vector3.up, vector2).normalized;
		float num = Vector3.Dot(rb.velocity, normalized);
		float num2 = Vector3.Dot(rb.velocity, vector2);
		Vector3 vector3 = stickPYR;
		if (squareHoverInputs)
		{
			vector3.x *= Mathf.Abs(stickPYR.x);
			vector3.y *= Mathf.Abs(stickPYR.y);
			vector3.z *= Mathf.Abs(stickPYR.z);
		}
		if (hoverHoldGPS)
		{
			Vector3 vector4 = ((0f - vector3.z) * normalized + vector3.x * vector2) * hoverControlSpeed;
			hoverHoldPoint.point += vector4 * Time.fixedDeltaTime;
			Vector3 vector5 = hoverHoldPoint.point - autoPilot.referenceTransform.position;
			vector5 = Vector3.ClampMagnitude(vector5, 5f);
			vector5.y = 0f;
			hoverHoldPoint.point = autoPilot.referenceTransform.position + vector5;
			targetZeroVel = hoverHoldGPSPID.Evaluate(Vector3.zero, vector5);
			vector3.x = 0f;
			vector3.z = 0f;
		}
		Vector3 vector6 = new Vector3(Vector3.Dot(targetZeroVel, normalized), 0f, Vector3.Dot(targetZeroVel, vector2));
		float angle = Mathf.Clamp(hoverPitchPID.Evaluate(num2 - vector6.z, vector3.x * hoverControlSpeed), 0f - maxHoverBank, maxHoverBank);
		Vector3 overrideRollTarget = Quaternion.AngleAxis(Mathf.Clamp(hoverRollPID.Evaluate(0f - (num - vector6.x), vector3.z * hoverControlSpeed), 0f - maxHoverBank, maxHoverBank), vector2) * Vector3.up;
		Vector3 vector7 = Quaternion.AngleAxis(angle, normalized) * vector;
		autoPilot.SetOverrideRudder(overrideRudder);
		autoPilot.SetOverrideRollTarget(overrideRollTarget);
		autoPilot.steerMode = AutoPilot.SteerModes.Aim;
		autoPilot.targetPosition = rb.position + vector7 * 100f;
		if (altitudeHold)
		{
			float num3 = 2f * (throttleInput - 0.5f);
			num3 *= Mathf.Abs(num3);
			float num4 = baselineHoverThrottle;
			if (hoverAccountTWR)
			{
				float num5 = totalMaxThrust / (rb.mass * 9.81f) * Vector3.Dot(Vector3.up, base.transform.up);
				num4 = 1f / num5;
			}
			altitudeToHold += num3 * 20f * Time.fixedDeltaTime;
			altitudeToHold = Mathf.Clamp(altitudeToHold, flightInfo.altitudeASL - maxTargetAltOffset, flightInfo.altitudeASL + maxTargetAltOffset);
			float target = Mathf.Clamp(2f * (altitudeToHold - flightInfo.altitudeASL), -20f, 20f);
			float num6 = 0f;
			if (num3 < 0f && flightInfo.isLanded)
			{
				num6 = ((!hoverAccountTWR) ? baselineHoverThrottle : 0f);
				altitudeToHold = flightInfo.altitudeASL;
			}
			else
			{
				num6 = num4 + hoverThrottlePID.Evaluate(flightInfo.verticalSpeed, target);
			}
			bool flag = false;
			bool flag3;
			if (minAbSwitchDuration > 0f)
			{
				bool flag2 = isAB == 1;
				flag3 = (flag2 ? (num6 > 0.75f) : (num6 > 1.25f));
				if ((flag3 != flag2 || isAB < 0) && Time.time - abSwitchTime > minAbSwitchDuration)
				{
					isAB = (flag3 ? 1 : 0);
					abSwitchTime = Time.time;
					flag = true;
				}
				else
				{
					flag3 = isAB > 0;
				}
			}
			else
			{
				flag3 = num6 > bypassABThreshold;
				flag = true;
			}
			float value = Mathf.Min(hoverMaxThrottle, num6);
			for (int i = 0; i < engines.Length; i++)
			{
				engines[i].SetThrottle(value);
				if (hoverControlsAB && flag)
				{
					engines[i].afterburner = flag3;
				}
			}
			for (int j = 0; j < finalThrottleOutputs.Length; j++)
			{
				finalThrottleOutputs[j].SetThrottle(value);
			}
			for (int k = 0; k < rotorOutputs.Length; k++)
			{
				rotorOutputs[k].SetCollectiveFullRange(Mathf.Clamp01(value));
			}
		}
		else
		{
			BypassThrottle();
		}
		if ((bool)tiltController && tiltController.currentTilt > 5f)
		{
			if (altitudeHold)
			{
				ToggleAltitudeHold();
				this.OnTriggeredAltDisable?.Invoke();
			}
			ToggleHoverMode();
			this.OnTriggeredHoverDisable?.Invoke();
			WarnAutopilotDisable();
		}
		for (int l = 0; l < rotorOutputs.Length; l++)
		{
			rotorOutputs[l].pyrTrim = Vector3.MoveTowards(rotorOutputs[l].pyrTrim, Vector3.zero, 0.5f * Time.deltaTime);
		}
	}

	private void UpdateFTFHover()
	{
		Vector3 normalized = Vector3.ProjectOnPlane(base.transform.forward, Vector3.up).normalized;
		Vector3 axis = Vector3.Cross(Vector3.up, normalized);
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < engines.Length; i++)
		{
			num2 += engines[i].maxThrust;
			num += engines[i].maxThrust * engines[i].abThrustMult;
		}
		float num3 = num;
		num = Mathf.Lerp(num2, num, Mathf.Cos(tiltController.currentTilt * ((float)Math.PI / 180f)));
		float num4 = 0f;
		for (int j = 0; j < wings.Length; j++)
		{
			num4 += Vector3.Dot(wings[j].liftVector, Vector3.up);
		}
		float num5 = rb.mass * 9.81f - num4;
		float num6 = num3 / (rb.mass * 9.81f);
		float num7 = num * Vector3.Dot(Vector3.up, base.transform.up) / num5;
		float num8 = 1f / num7;
		if (num6 < minFTFTWR)
		{
			ToggleHoverMode();
			this.OnTriggeredHoverDisable?.Invoke();
			WarnAutopilotDisable();
			return;
		}
		float num9 = ftfMaxRoll;
		float num10 = Mathf.Pow(stickPYR.z, 2f) * Mathf.Sign(stickPYR.z);
		Vector3 overrideRollTarget = Quaternion.AngleAxis(Mathf.Lerp(0f - flightInfo.roll, num10 * num9, 0.4f), normalized) * Vector3.up;
		altitudeToHold += (0f - stickPYR.x) * Time.deltaTime * hoverControlSpeed;
		altitudeToHold = Mathf.Clamp(altitudeToHold, flightInfo.altitudeASL - maxTargetAltOffset, flightInfo.altitudeASL + maxTargetAltOffset);
		if (stickPYR.x < -0.01f && altitudeToHold < flightInfo.altitudeASL)
		{
			altitudeToHold = flightInfo.altitudeASL;
		}
		else if (stickPYR.x > 0.01f && altitudeToHold > flightInfo.altitudeASL)
		{
			altitudeToHold = flightInfo.altitudeASL;
		}
		float target = ftfHoverAltitudePID.Evaluate(flightInfo.altitudeASL, altitudeToHold);
		float a = Mathf.Clamp(num8 + hoverThrottlePID.Evaluate(flightInfo.verticalSpeed, target), 0f, 1f);
		a = Mathf.Lerp(a, 1f, 0f - stickPYR.x);
		if (flightInfo.isLanded && stickPYR.x > -0.05f)
		{
			a = 0f;
		}
		float num11 = throttleInput * 2f - 1f;
		float max = Mathf.Lerp(ftfMaxPitch, 0f, flightInfo.airspeed / 40f);
		Vector3 a2 = Quaternion.AngleAxis(Mathf.Clamp(ftfMaxPitch * num11, 0f - ftfMaxPitch, max), axis) * normalized;
		float a3 = Mathf.Clamp(num11 / 0.74f * 25f, 0f, 90f);
		float b = Mathf.Clamp(throttleInput, num8, 1f);
		Vector3 b2 = Quaternion.AngleAxis(maxHoverBank * stickPYR.x, axis) * (Quaternion.AngleAxis(0f - stdFlightAoACurve.Evaluate(flightInfo.airspeed), base.transform.right) * rb.velocity).normalized;
		float b3 = Mathf.Lerp(0f, 90f, throttleInput * 2f);
		float t = ftfTransitionSpeedCurve.Evaluate(flightInfo.airspeed);
		float value = Mathf.Lerp(a, b, t);
		Vector3 vector = Vector3.Slerp(a2, b2, ftfPitchTransotionSpeedCurve.Evaluate(flightInfo.airspeed));
		float y = Mathf.Clamp(Mathf.Lerp(a3, b3, t) - tiltController.currentTilt, -1f, 1f);
		tiltController.PadInputScaled(new Vector3(0f, y, 0f));
		autoPilot.targetPosition = autoPilot.referenceTransform.position + vector * 100f;
		autoPilot.SetOverrideRudder(stickPYR.y);
		autoPilot.SetOverrideRollTarget(overrideRollTarget);
		float arg = Mathf.Clamp01(value) * 0.74f;
		for (int k = 0; k < engines.Length; k++)
		{
			engines[k].SetThrottle(arg);
		}
		if (OnSetFTFThrottle != null)
		{
			OnSetFTFThrottle.Invoke(arg);
		}
		for (int l = 0; l < finalThrottleOutputs.Length; l++)
		{
			finalThrottleOutputs[l].SetThrottle(arg);
		}
		if (flightInfo.airspeed > maxFtfHoverSpeed && tiltController.currentTilt > 89.9f && throttleInput > 0.7f)
		{
			WarnAutopilotDisable();
			ToggleHoverMode();
			this.OnTriggeredHoverDisable?.Invoke();
		}
	}

	private void FixedUpdate()
	{
		if (hoverMode || navMode || speedMode || altitudeHold || headingHold)
		{
			CheckBattery();
			CheckLanded();
		}
		Vector3 pitchYawRoll = stickPYR;
		if (hoverMode)
		{
			UpdateHoverMode();
		}
		else
		{
			if (speedMode)
			{
				float max = engines[0].autoABThreshold - 0.001f;
				apThrottle = autoPilot.throttlePID.Evaluate(flightInfo.airspeed, speedToHold);
				apThrottle = Mathf.Clamp(apThrottle, 0f, max);
				for (int i = 0; i < engines.Length; i++)
				{
					engines[i].SetThrottle(apThrottle);
					engines[i].afterburner = false;
				}
				for (int j = 0; j < finalThrottleOutputs.Length; j++)
				{
					finalThrottleOutputs[j].SetThrottle(apThrottle);
				}
			}
			else
			{
				BypassThrottle();
			}
			if (navMode)
			{
				if ((bool)WaypointManager.instance.currentWaypoint)
				{
					Vector3 position = WaypointManager.instance.currentWaypoint.position;
					headingToHold = VectorUtils.Bearing(rb.position, position);
					if (!altitudeHold)
					{
						float a = position.y - WaterPhysics.instance.height;
						a = (altitudeToHold = Mathf.Max(a, 1500f));
					}
					if (Mathf.Abs(stickPYR.x) > 0.35f || Mathf.Abs(stickPYR.z) > 0.35f)
					{
						ToggleNav();
						this.OnTriggeredNavDisable?.Invoke();
						WarnAutopilotDisable();
					}
				}
				else
				{
					ToggleNav();
					this.OnTriggeredNavDisable?.Invoke();
					WarnAutopilotDisable();
				}
			}
			if (altitudeHold || navMode)
			{
				if ((altitudeHold && (Mathf.Abs(stickPYR.x) > 0.35f || Mathf.Abs(flightInfo.roll) > 80f)) || flightInfo.airspeed < minAltHoldSpeed)
				{
					ToggleAltitudeHold();
					this.OnTriggeredAltDisable?.Invoke();
					WarnAutopilotDisable();
				}
				else
				{
					Vector3 velocity = rb.velocity;
					velocity.y = 0f;
					VectorUtils.SignedAngle(velocity, rb.velocity, Vector3.up);
					float current = rb.position.y - WaterPhysics.instance.height;
					float target = altitudeToHold;
					float num = altitudeClimbPID.Evaluate(current, target, _p: true, _i: true, _d: false);
					num += altitudeClimbPID.kd * flightInfo.verticalSpeed;
					num = Mathf.Clamp(num, -25f, 25f);
					num += flightInfo.aoa * 0.75f;
					float current2 = VectorUtils.SignedAngle(Vector3.ProjectOnPlane(base.transform.forward, Vector3.up), base.transform.forward, Vector3.up);
					float num2 = altitudePitchPID.Evaluate(current2, num);
					pitchYawRoll.x = Mathf.Lerp(Mathf.Clamp(0f - num2, altitudePitchStickMinMax.min, altitudePitchStickMinMax.max), pitchYawRoll.x, Mathf.Abs(pitchYawRoll.x));
					if (!headingHold && !navMode)
					{
						if (Mathf.Abs(stickPYR.z) > 0.03f)
						{
							bankToHold = flightInfo.roll;
						}
						float num3 = headingRollPID.Evaluate(flightInfo.roll, bankToHold);
						pitchYawRoll.z = Mathf.Lerp(Mathf.Clamp(0f - num3, -0.5f, 0.5f), pitchYawRoll.z, 2f * Mathf.Abs(pitchYawRoll.z));
					}
				}
			}
			if (headingHold || navMode)
			{
				if (headingHold && Mathf.Abs(pitchYawRoll.z) > 0.35f)
				{
					ToggleHeadingHold();
					this.OnTriggeredHeadingDisable?.Invoke();
					WarnAutopilotDisable();
				}
				else
				{
					float value = headingTurnPID.Evaluate(flightInfo.heading, headingToHold);
					value = Mathf.Clamp(value, 0f - maxAPBank, maxAPBank);
					smoothRollTarget = Mathf.MoveTowards(smoothRollTarget, value, maxAPRollRate * Time.fixedDeltaTime);
					float num4 = headingRollPID.Evaluate(flightInfo.roll, smoothRollTarget);
					num4 = Mathf.Clamp(0f - num4, -0.5f, 0.5f);
					pitchYawRoll.z = Mathf.Lerp(num4, pitchYawRoll.z, Mathf.Abs(pitchYawRoll.z));
					bankToHold = flightInfo.roll;
				}
			}
			for (int k = 0; k < bypassOutputs.Length; k++)
			{
				bypassOutputs[k].SetPitchYawRoll(pitchYawRoll);
				bypassOutputs[k].SetThrottle(throttleInput);
			}
		}
		if (warnDisable && (bool)flightWarnings)
		{
			flightWarnings.AddCommonWarning(FlightWarnings.CommonWarnings.AutopilotOff);
			warnDisable = false;
		}
	}

	private void LateUpdate()
	{
		if (speedAPMovesThrottle && speedMode && !hoverMode)
		{
			if ((bool)throttle.interactable.activeController)
			{
				WarnAutopilotDisable();
				ToggleSpeedHold();
			}
			else
			{
				throttle.RemoteSetThrottle(Mathf.Lerp(throttle.currentThrottle, apThrottle, 5f * Time.deltaTime));
			}
		}
	}

	private void CheckBattery()
	{
		if ((bool)battery && !battery.Drain(componentElectricDrain * Time.fixedDeltaTime))
		{
			AllOff();
		}
	}

	private void CheckLanded()
	{
		if (flightInfo.isLanded)
		{
			if (speedMode)
			{
				ToggleSpeedHold();
			}
			if (navMode)
			{
				ToggleNav();
				this.OnTriggeredNavDisable?.Invoke();
			}
			if (altitudeHold && !hoverMode)
			{
				ToggleAltitudeHold();
				this.OnTriggeredAltDisable?.Invoke();
			}
			if (headingHold)
			{
				ToggleHeadingHold();
				this.OnTriggeredHeadingDisable?.Invoke();
			}
		}
	}

	private void BypassThrottle()
	{
		isAB = -1;
		for (int i = 0; i < engines.Length; i++)
		{
			engines[i].SetThrottle(throttleInput);
			engines[i].afterburner = throttleInput > bypassABThreshold;
		}
		for (int j = 0; j < finalThrottleOutputs.Length; j++)
		{
			finalThrottleOutputs[j].SetThrottle(throttleInput);
		}
		for (int k = 0; k < rotorOutputs.Length; k++)
		{
			rotorOutputs[k].SetCollectiveFullRange(throttleInput);
		}
	}

	private void WarnAutopilotDisable()
	{
		warnDisable = true;
	}

	public override void SetPitchYawRoll(Vector3 pitchYawRoll)
	{
		stickPYR = pitchYawRoll;
	}

	public override void SetThrottle(float throttle)
	{
		throttleInput = throttle;
	}

	public void ToggleSpeedHold()
	{
		if ((speedMode || !battery || battery.Drain(0.01f * Time.deltaTime)) && !hoverMode)
		{
			speedMode = !speedMode;
			if ((bool)speedIndicator)
			{
				speedIndicator.imageEnabled = speedMode;
			}
			if (speedMode)
			{
				speedToHold = flightInfo.airspeed;
			}
		}
	}

	public void ToggleHeadingHold()
	{
		if ((headingHold || !battery || battery.Drain(0.01f * Time.deltaTime)) && (!hoverMode || headingHold))
		{
			if (navMode)
			{
				ToggleNav();
				this.OnTriggeredNavDisable?.Invoke();
			}
			headingHold = !headingHold;
			if (headingHold)
			{
				headingHold = true;
				headingToHold = flightInfo.heading;
				smoothRollTarget = flightInfo.roll;
			}
			if ((bool)headingIndicator)
			{
				headingIndicator.imageEnabled = headingHold;
			}
			this.OnHeadingHold?.Invoke(headingHold);
		}
	}

	public void ToggleAltitudeHold()
	{
		if (!altitudeHold && (bool)battery && !battery.Drain(0.01f * Time.deltaTime))
		{
			return;
		}
		altitudeHold = !altitudeHold;
		if (altitudeHold)
		{
			altitudeToHold = flightInfo.altitudeASL;
			bankToHold = flightInfo.roll;
			if ((bool)hudJoyIndicator && hoverMode)
			{
				hudJoyIndicator.SetToHoverMode();
			}
		}
		else if ((bool)hudJoyIndicator && hoverMode)
		{
			hudJoyIndicator.SetToThrottleMode();
		}
		if ((bool)altitudeIndicator)
		{
			altitudeIndicator.imageEnabled = altitudeHold;
		}
		this.OnAltitudeHold?.Invoke(altitudeHold);
	}

	public void ToggleHoverMode()
	{
		if ((!hoverMode && (bool)battery && !battery.Drain(0.01f * Time.deltaTime)) || (!hoverMode && ((!ftfHoverMode && (bool)tiltController && tiltController.currentTilt > 5f) || navMode)))
		{
			return;
		}
		hoverMode = !hoverMode;
		if (hoverMode)
		{
			hoverMode = true;
			if (hoverHoldGPS)
			{
				hoverHoldPoint.point = autoPilot.referenceTransform.position;
			}
			if (hoverHoldYaw)
			{
				headingToHold = VectorUtils.Bearing(autoPilot.referenceTransform.forward);
				hoverHoldYawDir = autoPilot.referenceTransform.forward;
				hoverHoldYawDir.y = 0f;
			}
			if (speedMode)
			{
				ToggleSpeedHold();
			}
			if (headingHold)
			{
				ToggleHeadingHold();
				this.OnTriggeredHeadingDisable?.Invoke();
			}
			altitudeToHold = flightInfo.altitudeASL;
			autoPilot.enabled = true;
			if ((bool)hudJoyIndicator && altitudeHold)
			{
				hudJoyIndicator.SetToHoverMode();
			}
		}
		else
		{
			autoPilot.enabled = false;
			if ((bool)hudJoyIndicator)
			{
				hudJoyIndicator.SetToThrottleMode();
			}
		}
		if ((bool)hoverIndicator)
		{
			hoverIndicator.imageEnabled = hoverMode;
		}
		this.OnHoverMode?.Invoke(hoverMode);
	}

	public void AllOff()
	{
		if (hoverMode)
		{
			ToggleHoverMode();
		}
		if (headingHold)
		{
			ToggleHeadingHold();
		}
		if (altitudeHold)
		{
			ToggleAltitudeHold();
		}
		if (navMode)
		{
			ToggleNav();
		}
		if (speedMode)
		{
			ToggleSpeedHold();
		}
	}

	public void ToggleNav()
	{
		if (!navMode)
		{
			if (!WaypointManager.instance.currentWaypoint || ((bool)battery && !battery.Drain(0.01f * Time.deltaTime)) || flightInfo.airspeed < 50f)
			{
				return;
			}
			if (hoverMode)
			{
				ToggleHoverMode();
				this.OnTriggeredHoverDisable?.Invoke();
			}
			if (headingHold)
			{
				ToggleHeadingHold();
				this.OnTriggeredHeadingDisable?.Invoke();
			}
		}
		navMode = !navMode;
		if (navMode)
		{
			smoothRollTarget = flightInfo.roll;
		}
		if ((bool)navIndicator)
		{
			navIndicator.imageEnabled = navMode;
		}
		this.OnNavMode?.Invoke(navMode);
	}

	public void SetThrottleThumbInput(Vector3 axes)
	{
		stickPYR.y = axes.x;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode("VTOLAutoPilot");
		configNode.SetValue("navMode", navMode);
		configNode.SetValue("hoverMode", hoverMode);
		configNode.SetValue("headingHold", headingHold);
		configNode.SetValue("headingToHold", headingToHold);
		configNode.SetValue("altitudeHold", altitudeHold);
		configNode.SetValue("altitudeToHold", altitudeToHold);
		configNode.SetValue("speedMode", speedMode);
		configNode.SetValue("speedToHold", speedToHold);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("VTOLAutoPilot");
		if (node != null)
		{
			bool value = node.GetValue<bool>("navMode");
			bool value2 = node.GetValue<bool>("hoverMode");
			bool value3 = node.GetValue<bool>("headingHold");
			bool value4 = node.GetValue<bool>("altitudeHold");
			bool value5 = node.GetValue<bool>("speedMode");
			if (value)
			{
				ToggleNav();
			}
			if (value2)
			{
				ToggleHoverMode();
			}
			if (value3)
			{
				ToggleHeadingHold();
				headingToHold = node.GetValue<float>("headingToHold");
			}
			if (value4)
			{
				ToggleAltitudeHold();
				altitudeToHold = node.GetValue<float>("altitudeToHold");
			}
			if (value5)
			{
				ToggleSpeedHold();
				speedToHold = node.GetValue<float>("speedToHold");
			}
		}
	}
}
