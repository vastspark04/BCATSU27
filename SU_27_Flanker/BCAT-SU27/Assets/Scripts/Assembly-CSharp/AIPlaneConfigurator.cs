using UnityEngine;

public class AIPlaneConfigurator : MonoBehaviour
{
	public AIPlaneConfig config;

	public AIPilot aiPilot;

	public AutoPilot autoPilot;

	public KinematicPlane kPlane;

	public Health health;

	public Tailhook tailhook;

	[ContextMenu("Apply Config")]
	public void ApplyConfig()
	{
		OnValidate();
		health.maxHealth = config.health;
		health.startHealth = config.health;
		health.minDamage = config.minDamage;
		aiPilot.allowEvasiveManeuvers = config.allowEvasiveManeuvers;
		aiPilot.landingGlideSlope = config.landingGlideSlope;
		aiPilot.landingSpeed = config.landingSpeed;
		aiPilot.landingStartDistance = config.landingStartDistance;
		aiPilot.gunAirMaxRange = config.gunAirMaxRange;
		aiPilot.gunGroundMaxRange = config.gunGroundMaxRange;
		aiPilot.gunRunAngle = config.gunRunAngle;
		aiPilot.gunRunStartAltitude = config.gunRunStartAltitude;
		aiPilot.gunRunMinAltitude = config.gunRunMinAltitude;
		aiPilot.gunRunSpeed = config.gunRunSpeed;
		aiPilot.missileEvadeDistance = config.missileEvadeDistance;
		aiPilot.landingHorizPID = new PID(config.landingHorizPID);
		aiPilot.landingVertPID = new PID(config.landingVertPID);
		aiPilot.landingHorizPID.updateMode = UpdateModes.Dynamic;
		aiPilot.landingVertPID.updateMode = UpdateModes.Dynamic;
		autoPilot.maxOutputSpeed = config.maxOutputSpeed;
		autoPilot.maxThrottleSpeed = config.maxThrottleSpeed;
		autoPilot.pitchPID = new PID(config.pitchPID);
		autoPilot.yawPID = new PID(config.yawPID);
		autoPilot.rollPID = new PID(config.rollPID);
		autoPilot.throttlePID = new PID(config.throttlePID);
		autoPilot.angularRollFactor = config.angularRollFactor;
		autoPilot.maxBank = config.maxBank;
		autoPilot.rollDotPower = config.rollDotPower;
		autoPilot.wheelSteerPID = new PID(config.wheelSteerPID);
		autoPilot.wheelSteerPID.updateMode = UpdateModes.Dynamic;
		kPlane.maxAoAcurve = config.maxAoaCurve;
		kPlane.maxGCurve = config.maxGCurve;
		kPlane.rollRateCurve = config.rollRateCurve;
		kPlane.lerpCurve = config.lerpCurve;
		kPlane.flapsMultiplier = config.flapsMultiplier;
		kPlane.pitchLerpMult = config.pitchLerpMult;
		kPlane.rollLerpMult = config.rollLerpMult;
		kPlane.yawGMult = config.yawGMult;
		kPlane.yawAoAMult = config.yawAoaMult;
		kPlane.dragArea = config.dragArea;
		kPlane.brakeDrag = config.brakeDrag;
		if ((bool)tailhook)
		{
			tailhook.hookForce = config.hookForce;
			tailhook.minDistForce = config.minDistForce;
			tailhook.maxDistForce = config.maxDistForce;
		}
	}

	private void Awake()
	{
		ApplyConfig();
	}

	private void OnValidate()
	{
		if (!aiPilot)
		{
			aiPilot = GetComponent<AIPilot>();
		}
		if (!autoPilot)
		{
			autoPilot = GetComponent<AutoPilot>();
		}
		if (!kPlane)
		{
			kPlane = GetComponent<KinematicPlane>();
		}
		if (!health)
		{
			health = GetComponent<Health>();
		}
		if (!tailhook)
		{
			tailhook = GetComponentInChildren<Tailhook>();
		}
	}
}
