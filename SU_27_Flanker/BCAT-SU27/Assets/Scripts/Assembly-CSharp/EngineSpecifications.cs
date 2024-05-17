using UnityEngine;

[CreateAssetMenu]
public class EngineSpecifications : ScriptableObject
{
	public float startupTime;

	public float maxThrust;

	public float fuelDrain;

	public float abThrustMult;

	public float abDrainMult;

	public float abSpoolMult;

	public float spoolRate;

	public bool lerpSpool;

	public float idleThrottle;

	public bool useSpeedCurve;

	public SOCurve speedCurve;

	public bool useAtmosCurve;

	public SOCurve atmosCurve;

	public float afterburnerRate;

	public float startupDrain;

	public float alternatorChargeRate;
}
