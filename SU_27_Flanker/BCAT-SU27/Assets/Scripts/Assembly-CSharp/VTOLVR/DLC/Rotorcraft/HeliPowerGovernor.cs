using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class HeliPowerGovernor : MonoBehaviour
{
	public ModuleEngine[] engines;

	public TurbineStarterMotor[] starters;

	public TurbineTransmission transmission;

	public MinMax rpmGovernorLimits = new MinMax(17000f, 24000f);

	public float idleRPM = 800f;

	public float throttleIdleNotch = 0.3f;

	public float transmissionTargetRPM = 40000f;

	public AnimationCurve throttleStartupCurve;

	public PID[] throttleGovernors;

	public float throttleLerpRate = 6f;

	public float minThrottle = 0.1f;

	public PID[] rpmGovernors;

	private float[] throttles;

	private float throttleLimit;

	public float currentThrottleLimit => throttleLimit;

	private void Awake()
	{
		throttles = new float[engines.Length];
	}

	private void FixedUpdate()
	{
		for (int i = 0; i < throttleGovernors.Length; i++)
		{
			float num = Mathf.Clamp01(throttleLimit / throttleIdleNotch);
			float b = rpmGovernorLimits.Lerp(Mathf.Clamp01(rpmGovernors[i].Evaluate(transmission.outputRPM, transmissionTargetRPM)));
			b = Mathf.Lerp(Mathf.Lerp(0f, idleRPM, num), b, Mathf.Clamp01(throttleLimit - throttleIdleNotch) / (1f - throttleIdleNotch));
			float b2 = Mathf.Clamp01(throttleGovernors[i].Evaluate(engines[i].outputRPM, b));
			b2 = Mathf.Max(minThrottle * num, b2);
			if (starters[i].motorEnabled)
			{
				b2 = num;
			}
			b2 *= throttleStartupCurve.Evaluate(engines[i].outputRPM);
			b2 = Mathf.Clamp01(b2);
			throttles[i] = Mathf.Lerp(throttles[i], b2, throttleLerpRate * Time.fixedDeltaTime);
			engines[i].SetThrottle(throttles[i]);
		}
	}

	public void SetThrottleLimit(float tLimit)
	{
		throttleLimit = tLimit;
	}
}

}