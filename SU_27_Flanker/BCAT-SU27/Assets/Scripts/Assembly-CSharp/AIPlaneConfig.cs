using UnityEngine;

[CreateAssetMenu]
public class AIPlaneConfig : ScriptableObject
{
	[Header("Health Config")]
	public float health;

	public float minDamage;

	[Header("AIPilot Config")]
	public bool allowEvasiveManeuvers;

	public float landingGlideSlope;

	public float landingSpeed;

	public float landingStartDistance;

	public float gunAirMaxRange;

	public float gunGroundMaxRange;

	[Range(25f, 80f)]
	public float gunRunAngle;

	public float gunRunStartAltitude;

	public float gunRunMinAltitude;

	public float gunRunSpeed;

	public PID.PIDConfig landingHorizPID;

	public PID.PIDConfig landingVertPID;

	public float missileEvadeDistance;

	[Header("Autopilot Config")]
	public float maxOutputSpeed = -1f;

	public float maxThrottleSpeed = -1f;

	public PID.PIDConfig pitchPID;

	public PID.PIDConfig yawPID;

	public PID.PIDConfig rollPID;

	public PID.PIDConfig throttlePID;

	public float angularRollFactor;

	public float maxBank = 190f;

	public float rollDotPower = 15f;

	public PID.PIDConfig wheelSteerPID;

	[Header("KPlane Config")]
	public AnimationCurve maxAoaCurve;

	public AnimationCurve maxGCurve;

	public AnimationCurve rollRateCurve;

	public AnimationCurve lerpCurve;

	public float flapsMultiplier;

	public float pitchLerpMult;

	public float rollLerpMult;

	public float yawGMult;

	public float yawAoaMult;

	public float dragArea;

	public float brakeDrag;

	[Header("Tailhook Config")]
	public float hookForce;

	public float minDistForce = 10f;

	public float maxDistForce = 60f;
}
