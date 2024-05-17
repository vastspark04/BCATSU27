using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft.Test{

public class TestEngineTorque : MonoBehaviour
{
	public ModuleEngine[] engines;

	public float maxResistanceTorque;

	public TurbineTransmission transmission;

	public HelicopterRotor[] rotors;

	public bool useGovernators;

	public PID[] throttleGovernators;

	public MinMax rpmGovernatorLimits;

	public PID[] rpmGovernators;

	public bool testCollective;

	private float collective;

	private float[] throttles;

	private float tBrake;

	public float maxTBrake;

	private float resistance;

	private void Start()
	{
		if (throttles == null)
		{
			throttles = new float[engines.Length];
		}
	}

	private void FixedUpdate()
	{
		transmission.AddResistanceTorque(tBrake);
		if (useGovernators)
		{
			for (int i = 0; i < throttleGovernators.Length; i++)
			{
				float target = rpmGovernatorLimits.Lerp(Mathf.Clamp01(rpmGovernators[i].Evaluate(transmission.outputRPM, 40000f)));
				throttles[i] = Mathf.Clamp01(throttleGovernators[i].Evaluate(engines[i].outputRPM, target));
			}
		}
	}

	private void OnGUI()
	{
		float num = 10f;
		num += 40f;
		for (int i = 0; i < engines.Length; i++)
		{
			ModuleEngine moduleEngine = engines[i];
			GUI.Label(new Rect(10f, num, 500f, 20f), $"Throttle: {Mathf.Round(throttles[i] * 100f)}%");
			num += 20f;
			throttles[i] = GUI.HorizontalSlider(new Rect(10f, num, 500f, 20f), throttles[i], 0f, 1f);
			moduleEngine.SetThrottle(throttles[i]);
			num += 20f;
			GUI.Label(new Rect(10f, num, 500f, 20f), $"RPM: {moduleEngine.outputRPM}");
			num += 30f;
		}
		num += 40f;
		GUI.Label(new Rect(10f, num, 500f, 20f), $"Transmission RPM: {transmission.outputRPM}");
		num += 40f;
		GUI.Label(new Rect(10f, num, 500f, 20f), $"Transmission Brake: {tBrake}");
		num += 20f;
		tBrake = GUI.HorizontalSlider(new Rect(10f, num, 500f, 20f), tBrake, 0f, maxTBrake);
		if (testCollective)
		{
			num += 40f;
			GUI.Label(new Rect(10f, num, 500f, 20f), $"Collective: {collective}");
			num += 20f;
			collective = GUI.HorizontalSlider(new Rect(10f, num, 500f, 20f), collective, -1f, 1f);
			HelicopterRotor[] array = rotors;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].SetCollective(collective);
			}
		}
	}
}
}