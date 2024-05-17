using System;
using System.Collections;
using UnityEngine;

public class ModuleEngine : FlightControlComponent, IParentRBDependent, IQSVehicleComponent
{
	private const float GLOBAL_ENGINE_HEAT_MULT = 0.4f;

	public bool engineEnabled = true;

	public FuelTank fuelTank;

	public Transform thrustTransform;

	public EngineEffects engineEffects;

	public Rigidbody rb;

	public KinematicPlane kPlane;

	public AudioSource toggleABaudio;

	public bool afterburner;

	public bool autoAB;

	public float autoABThreshold = 0.75f;

	[Space]
	public bool includeInTWR = true;

	[Space]
	public Battery battery;

	[Tooltip("Whether to use the custom thrust point.")]
	public bool overrideThrustPos;

	[Tooltip("Point relative to rigidbody's CoM where force is applied.")]
	public Vector3 thrustPos;

	private Vector3 localThrustPoint;

	[Header("Specifications")]
	public bool useCommonSpecs;

	public EngineSpecifications specs;

	public float startupTime = 3f;

	public float maxThrust;

	public float fuelDrain;

	public float spoolRate;

	public bool lerpSpool = true;

	public float idleThrottle = 0.03f;

	[Header("Afterburner")]
	public float abThrustMult;

	public float abSpoolMult;

	public float abDrainMult;

	public bool progressiveAB;

	public float afterburnerRate;

	[Header("Thrust Effects")]
	public bool useSpeedCurve;

	public SOCurve speedCurve;

	public bool useAtmosCurve;

	[Tooltip("Thrust multiplier per atmospheric pressure (atm)")]
	public SOCurve atmosCurve;

	[Header("Electrical")]
	public float startupDrain;

	public float alternatorChargeRate;

	[Header("Heat")]
	public float thrustHeatMult = 25f;

	public float abHeatAdd = 1f;

	[Header("Flight Warnings")]
	public FlightWarnings fw;

	public FlightWarnings.CommonWarnings engineWarning;

	public bool doWarnings;

	private FlightInfo _fi;

	private bool gotFi;

	private float throttle;

	private double finalThrottleD;

	private bool wasAb;

	private HeatEmitter heat;

	[Header("TorquePhysics")]
	public bool useTorquePhysics;

	public AnimationCurve torqueCurve;

	public float maxRPM;

	public float engineResistance;

	public float driveMass;

	public bool clampRPM;

	private float resistanceTorque;

	private float tq_finalThrottle;

	private int switchPosition;

	private Coroutine startupRoutine;

	private bool switchPosWarned;

	private float switchPosTimer;

	private float waterFailCounter;

	private float fuelReceived;

	private double fuelRequest;

	public bool enableEngineOnRepair;

	private FlightInfo flightInfo
	{
		get
		{
			if (!gotFi)
			{
				_fi = base.transform.root.GetComponentInChildren<FlightInfo>(includeInactive: true);
				gotFi = true;
			}
			return _fi;
		}
	}

	public float inputThrottle => throttle;

	public float finalThrottle => (float)finalThrottleD;

	public float displayedRPM { get; private set; }

	public float abMult { get; private set; }

	public bool failed { get; private set; }

	public float outputRPM => finalThrottle * maxRPM;

	public float appliedTorque => torqueCurve.Evaluate(outputRPM) * inputThrottle;

	public bool startingUp { get; private set; }

	public bool shuttingDown { get; private set; }

	public bool startedUp { get; private set; }

	public float finalThrust { get; private set; }

	public event Action<int> OnEngineStateImmediate;

	public event Action<int> OnEngineState;

	[ContextMenu("Save to Specifications Object")]
	public void SaveToEngineSpecs()
	{
		if ((bool)specs)
		{
			specs.startupTime = startupTime;
			specs.maxThrust = maxThrust;
			specs.fuelDrain = fuelDrain;
			specs.abThrustMult = abThrustMult;
			specs.abDrainMult = abDrainMult;
			specs.abSpoolMult = abSpoolMult;
			specs.spoolRate = spoolRate;
			specs.lerpSpool = lerpSpool;
			specs.idleThrottle = idleThrottle;
			specs.useSpeedCurve = useSpeedCurve;
			specs.speedCurve = speedCurve;
			specs.useAtmosCurve = useAtmosCurve;
			specs.atmosCurve = atmosCurve;
			specs.afterburnerRate = afterburnerRate;
			specs.startupDrain = startupDrain;
			specs.alternatorChargeRate = alternatorChargeRate;
		}
	}

	[ContextMenu("Load from Specifications Object")]
	public void LoadFromEngineSpecs()
	{
		if ((bool)specs)
		{
			startupTime = specs.startupTime;
			maxThrust = specs.maxThrust;
			fuelDrain = specs.fuelDrain;
			abThrustMult = specs.abThrustMult;
			abDrainMult = specs.abDrainMult;
			abSpoolMult = specs.abSpoolMult;
			spoolRate = specs.spoolRate;
			lerpSpool = specs.lerpSpool;
			idleThrottle = specs.idleThrottle;
			useSpeedCurve = specs.useSpeedCurve;
			speedCurve = specs.speedCurve;
			useAtmosCurve = specs.useAtmosCurve;
			atmosCurve = specs.atmosCurve;
			afterburnerRate = specs.afterburnerRate;
			startupDrain = specs.startupDrain;
			alternatorChargeRate = specs.alternatorChargeRate;
		}
	}

	public void AddResistanceTorque(float t)
	{
		resistanceTorque += t;
	}

	private void UpdateTorquePhysics(float inputThrottle)
	{
		float num = outputRPM;
		float num2 = torqueCurve.Evaluate(num);
		float num3 = engineResistance * num;
		num2 *= inputThrottle;
		num2 -= num3;
		num2 -= resistanceTorque;
		resistanceTorque = 0f;
		num = (num * 0.10472f + num2 / driveMass * Time.fixedDeltaTime) / 0.10472f;
		tq_finalThrottle = Mathf.Max(0f, num / maxRPM);
		if (clampRPM)
		{
			tq_finalThrottle = Mathf.Clamp01(tq_finalThrottle);
		}
	}

	public void Torque_RemoteSetRPM(float rpm)
	{
		finalThrottleD = rpm / maxRPM;
		tq_finalThrottle = Mathf.Max(0f, rpm / maxRPM);
	}

	private void Awake()
	{
		heat = GetComponent<HeatEmitter>();
		if ((bool)specs && useCommonSpecs)
		{
			LoadFromEngineSpecs();
		}
		if (useAtmosCurve && !atmosCurve)
		{
			Debug.LogError("Engine missing atmospheric curve.\n" + UIUtils.GetHierarchyString(base.gameObject), base.gameObject);
			useAtmosCurve = false;
		}
		if (useSpeedCurve && !speedCurve)
		{
			Debug.LogError("Engine missing speed curve.\n" + UIUtils.GetHierarchyString(base.gameObject), base.gameObject);
			useSpeedCurve = false;
		}
		VehiclePart componentInParent = GetComponentInParent<VehiclePart>();
		if ((bool)componentInParent)
		{
			componentInParent.OnRepair.AddListener(FullyRepairEngine);
		}
	}

	private void Start()
	{
		if (engineEnabled)
		{
			startedUp = true;
			throttle = idleThrottle;
		}
		else
		{
			startedUp = false;
			throttle = 0f;
		}
		UpdateThrustPoint();
	}

	[ContextMenu("Fail Engine")]
	public void FailEngine()
	{
		if (!failed)
		{
			Debug.LogError("Failed engine " + UIUtils.GetHierarchyString(base.gameObject), base.gameObject);
		}
		failed = true;
		if (engineEnabled)
		{
			ToggleEngine();
		}
		if (doWarnings && (bool)fw)
		{
			fw.AddCommonWarningContinuous(engineWarning);
		}
	}

	private void ResetSwitchWarning()
	{
		switchPosWarned = false;
		switchPosTimer = 0f;
	}

	public void StartImmediate()
	{
		if (startupRoutine != null)
		{
			StopCoroutine(startupRoutine);
			startingUp = false;
		}
		Debug.Log($"{GetInstanceID()} ModuleEngine Starting engine immediately");
		engineEnabled = true;
		startedUp = true;
		throttle = idleThrottle;
		this.OnEngineStateImmediate?.Invoke(1);
		if (doWarnings && (bool)fw)
		{
			fw.RemoveCommonWarning(engineWarning);
			ResetSwitchWarning();
		}
	}

	public void StopImmediate()
	{
		if (startupRoutine != null)
		{
			StopCoroutine(startupRoutine);
			startingUp = false;
		}
		Debug.Log($"{GetInstanceID()} ModuleEngine Stopping engine immediately");
		engineEnabled = false;
		startedUp = false;
		throttle = 0f;
		this.OnEngineStateImmediate?.Invoke(0);
	}

	public void UpdateThrustPoint()
	{
		if ((bool)rb)
		{
			if (overrideThrustPos)
			{
				localThrustPoint = base.transform.InverseTransformPoint(rb.transform.TransformPoint(thrustPos + rb.centerOfMass));
			}
			else
			{
				localThrustPoint = base.transform.InverseTransformPoint(thrustTransform.position);
			}
		}
	}

	public void SetUseOverrideThrustPos(bool use)
	{
		overrideThrustPos = use;
	}

	public void SetPower(int p)
	{
		Debug.Log($"{GetInstanceID()} ModuleEngine.SetPower({p})");
		switchPosition = p;
		if (p == 0)
		{
			if (engineEnabled)
			{
				ToggleEngine();
			}
			if (doWarnings && (bool)fw)
			{
				fw.RemoveCommonWarning(engineWarning);
				ResetSwitchWarning();
			}
		}
		else
		{
			if (engineEnabled)
			{
				return;
			}
			if (fuelTank.fuel < fuelDrain * Time.deltaTime || ((bool)battery && !battery.Drain(0.01f * Time.deltaTime)))
			{
				if (doWarnings && (bool)fw)
				{
					fw.AddCommonWarningContinuous(engineWarning);
				}
			}
			else
			{
				ToggleEngine();
			}
		}
	}

	public void SetPowerImmediate(int p)
	{
		if (p > 0)
		{
			StartImmediate();
			return;
		}
		StopImmediate();
		if (doWarnings && (bool)fw)
		{
			fw.RemoveCommonWarning(engineWarning);
			ResetSwitchWarning();
		}
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)rb && (bool)thrustTransform)
		{
			Gizmos.color = Color.red;
			UpdateThrustPoint();
			Vector3 vector = base.transform.TransformPoint(localThrustPoint);
			Gizmos.DrawLine(vector, vector + 2f * thrustTransform.forward);
		}
	}

	private IEnumerator StartupRoutine()
	{
		startingUp = true;
		startedUp = false;
		while (finalThrottle < idleThrottle * 0.9f)
		{
			float num = 1f;
			if ((bool)battery && !battery.Drain(startupDrain * Time.deltaTime))
			{
				num = -1f;
			}
			throttle += num * idleThrottle * Time.deltaTime / startupTime;
			throttle = Mathf.Clamp01(throttle);
			displayedRPM = throttle / idleThrottle * 0.3f;
			if (num < 0f)
			{
				ToggleEngine();
				if (doWarnings && (bool)fw)
				{
					fw.AddCommonWarningContinuous(engineWarning);
				}
				yield break;
			}
			yield return null;
		}
		throttle = idleThrottle * 0.9f;
		float t = Time.time;
		while (Time.time - t < 2f)
		{
			if ((bool)battery)
			{
				battery.Charge(alternatorChargeRate * finalThrottle * Time.deltaTime);
				if (!battery.Drain(startupDrain * Time.deltaTime))
				{
					ToggleEngine();
					if (doWarnings && (bool)fw)
					{
						fw.AddCommonWarningContinuous(engineWarning);
					}
					yield break;
				}
			}
			displayedRPM = Mathf.Lerp(displayedRPM, 0.25f, 2f * Time.deltaTime);
			yield return null;
		}
		throttle = idleThrottle;
		startingUp = false;
		startedUp = true;
		if (doWarnings && (bool)fw)
		{
			fw.RemoveCommonWarning(engineWarning);
			ResetSwitchWarning();
		}
	}

	private double Lerp(double a, double b, double t)
	{
		if (t < 0.0)
		{
			return a;
		}
		if (t > 1.0)
		{
			return b;
		}
		return a + (b - a) * t;
	}

	private double MoveTowards(double a, double b, double delta)
	{
		if (delta < 0.0)
		{
			return a;
		}
		if (b > a)
		{
			if (a + delta > b)
			{
				return b;
			}
			return a + delta;
		}
		if (a - delta < b)
		{
			return b;
		}
		return a - delta;
	}

	private void Update()
	{
		if (doWarnings)
		{
			bool flag = battery.currentCharge / battery.maxCharge > 0.05f;
			if (!switchPosWarned && !engineEnabled && switchPosition > 0 && flag)
			{
				switchPosTimer += Time.deltaTime;
				if (switchPosTimer > 2f)
				{
					switchPosWarned = true;
					fw.AddCommonWarningContinuous(engineWarning);
				}
			}
			if (switchPosWarned && (!flag || switchPosition == 0 || (switchPosition > 0 && engineEnabled)))
			{
				switchPosWarned = false;
				switchPosTimer = 0f;
			}
		}
		if (failed || !engineEnabled)
		{
			throttle = 0f;
			afterburner = false;
			if (engineEnabled)
			{
				ToggleEngine();
			}
		}
		else
		{
			if (engineEnabled && !startedUp && !startingUp)
			{
				startupRoutine = StartCoroutine(StartupRoutine());
			}
			if (startingUp)
			{
				afterburner = false;
			}
			if (!engineEnabled)
			{
				throttle = 0f;
			}
		}
		if (!failed && thrustTransform.position.y < WaterPhysics.waterHeight)
		{
			waterFailCounter += Mathf.Min(0.011f, Time.deltaTime);
			if (waterFailCounter > 1f)
			{
				FailEngine();
			}
		}
		if (autoAB)
		{
			afterburner = throttle > autoABThreshold;
		}
		if (progressiveAB)
		{
			abMult = Mathf.Lerp(abMult, Mathf.Clamp01((throttle - autoABThreshold) / (1f - autoABThreshold)), afterburnerRate * Time.deltaTime);
		}
		else
		{
			abMult = Mathf.Lerp(abMult, afterburner ? 1 : 0, afterburnerRate * Time.deltaTime);
		}
		double num = 1f + abMult * (abSpoolMult - 1f);
		if (useTorquePhysics)
		{
			finalThrottleD = tq_finalThrottle;
		}
		else if (lerpSpool)
		{
			finalThrottleD = Lerp(finalThrottleD, throttle, num * (double)spoolRate * (double)Time.deltaTime);
		}
		else
		{
			finalThrottleD = MoveTowards(finalThrottle, throttle, num * (double)spoolRate * (double)Time.deltaTime);
		}
		if (!startingUp)
		{
			displayedRPM = Mathf.Lerp(displayedRPM, Mathf.Lerp((startedUp && !useTorquePhysics) ? 0.125f : 0f, 1f, finalThrottle), 2f * Time.deltaTime);
			if (engineEnabled && (bool)battery)
			{
				battery.Charge(alternatorChargeRate * Time.deltaTime * finalThrottle);
			}
		}
		if ((bool)engineEffects)
		{
			engineEffects.SetThrottle(finalThrottle);
			engineEffects.SetAfterburner(abMult);
			engineEffects.SetOverrideDeltaUpdate(startingUp || shuttingDown);
		}
		if (afterburner != wasAb)
		{
			wasAb = afterburner;
			if ((bool)toggleABaudio)
			{
				toggleABaudio.Stop();
				toggleABaudio.Play();
			}
		}
		if (progressiveAB && (bool)toggleABaudio)
		{
			toggleABaudio.volume = abMult;
		}
		double num4;
		if (engineEnabled && fuelTank.fuel > 0f)
		{
			float num2 = Mathf.Clamp(Time.deltaTime, 0.001f, 1f);
			double num3 = 1f + abMult * (abDrainMult - 1f);
			if (useTorquePhysics)
			{
				fuelRequest = num3 * (double)fuelDrain * (finalThrottleD * (double)inputThrottle);
			}
			else
			{
				fuelRequest = num3 * (double)fuelDrain * (double)finalThrottle;
			}
			num4 = (double)(1f + abMult * (abThrustMult - 1f)) * (double)fuelReceived * finalThrottleD * (double)maxThrust;
			if (useSpeedCurve)
			{
				num4 *= (double)speedCurve.Evaluate(flightInfo.airspeed);
			}
			if (useAtmosCurve)
			{
				num4 *= (double)atmosCurve.Evaluate(AerodynamicsController.fetch.AtmosPressureAtPosition(thrustTransform.position));
			}
			if ((bool)heat)
			{
				float num5 = thrustHeatMult * (1f + abHeatAdd * abMult);
				heat.AddHeat((float)num4 * num5 * 0.4f * num2);
			}
			shuttingDown = false;
		}
		else
		{
			num4 = 0.0;
			shuttingDown = finalThrottle > 0f;
		}
		finalThrust = (float)num4;
	}

	private void FixedUpdate()
	{
		if (engineEnabled && (fuelTank.fuel > 0f || fuelDrain <= 0f))
		{
			fuelReceived = fuelTank.RequestFuel(fuelRequest * (double)Time.fixedDeltaTime);
			Vector3 position = base.transform.TransformPoint(localThrustPoint);
			if ((bool)rb && !rb.isKinematic)
			{
				rb.AddForceAtPosition((0f - finalThrust) * thrustTransform.forward, position);
			}
			else if ((bool)kPlane)
			{
				kPlane.AddForce((0f - finalThrust) * thrustTransform.forward);
			}
			if (useTorquePhysics && startedUp)
			{
				UpdateTorquePhysics(throttle);
			}
		}
		else
		{
			if (engineEnabled)
			{
				ToggleEngine();
			}
			if (useTorquePhysics)
			{
				UpdateTorquePhysics(0f);
			}
		}
	}

	public float GetAffectedMaxThrust()
	{
		float num = maxThrust * abThrustMult;
		if (useSpeedCurve)
		{
			num *= speedCurve.Evaluate(flightInfo.airspeed);
		}
		if (useAtmosCurve)
		{
			num *= atmosCurve.Evaluate(AerodynamicsController.fetch.AtmosPressureAtPosition(thrustTransform.position));
		}
		return num;
	}

	public float GetLandingThrust()
	{
		float num = maxThrust * abThrustMult;
		if (useSpeedCurve)
		{
			num *= speedCurve.Evaluate(0f);
		}
		if (useAtmosCurve)
		{
			Vector3 worldPos = base.transform.position - new Vector3(0f, flightInfo.radarAltitude, 0f);
			num *= atmosCurve.Evaluate(AerodynamicsController.fetch.AtmosPressureAtPosition(worldPos));
		}
		return num;
	}

	public override void SetThrottle(float t)
	{
		if (startedUp)
		{
			throttle = Mathf.Clamp(t, idleThrottle, 1f);
		}
	}

	public void SetFinalThrottle(float t)
	{
		if (startedUp)
		{
			SetThrottle(t);
			finalThrottleD = t;
		}
	}

	public void ToggleEngine()
	{
		if (!engineEnabled && failed)
		{
			return;
		}
		Debug.Log($"{GetInstanceID()} ModuleEngine.ToggleEngine() to {!base.enabled}");
		engineEnabled = !engineEnabled;
		if (!engineEnabled)
		{
			if (startupRoutine != null)
			{
				StopCoroutine(startupRoutine);
			}
			startedUp = false;
			startingUp = false;
		}
		this.OnEngineState?.Invoke(engineEnabled ? 1 : 0);
	}

	public void FullyRepairEngine()
	{
		failed = false;
		if (enableEngineOnRepair)
		{
			StartImmediate();
		}
		StartCoroutine(DelayedThrustPointFix());
	}

	private IEnumerator DelayedThrustPointFix()
	{
		yield return null;
		yield return new WaitForFixedUpdate();
		UpdateThrustPoint();
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		this.rb = rb;
		UpdateThrustPoint();
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_ModuleEngine");
		configNode.SetValue("engineEnabled", engineEnabled);
		configNode.SetValue("finalThrottle", finalThrottle);
		configNode.SetValue("failed", failed);
		configNode.SetValue("startingUp", startingUp);
		if ((bool)heat)
		{
			configNode.SetValue("heat", heat.heat);
		}
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = base.gameObject.name + "_ModuleEngine";
		if (!qsNode.HasNode(text))
		{
			return;
		}
		ConfigNode node = qsNode.GetNode(text);
		if (ConfigNodeUtils.ParseBool(node.GetValue("engineEnabled")))
		{
			if (node.GetValue<bool>("startingUp"))
			{
				engineEnabled = false;
				SetPower(1);
			}
			else
			{
				StartImmediate();
			}
			finalThrottleD = ConfigNodeUtils.ParseFloat(node.GetValue("finalThrottle"));
		}
		if ((bool)heat)
		{
			heat.AddHeat(node.GetValue<float>("heat"));
		}
		if (node.GetValue<bool>("failed"))
		{
			FailEngine();
		}
	}
}
