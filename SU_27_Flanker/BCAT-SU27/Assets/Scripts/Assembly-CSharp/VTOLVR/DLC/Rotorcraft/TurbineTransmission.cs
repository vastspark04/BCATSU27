using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class TurbineTransmission : MonoBehaviour, IQSVehicleComponent
{
	public ModuleEngine[] engines;

	public float minTorqueResistance;

	public float rpmFriction;

	public float driveMass;

	public float rpmDifferentialRatio;

	public float rpmDifferentialResistanceFactor;

	public AnimationCurve resistanceRPMCurve;

	public float outputTorqueRatio = 1f;

	public float staticFrictionRPMFade = 10000f;

	public float backblowTorqueRatio = 0.5f;

	private float driveshaftRPM;

	private float debug_appliedResistance;

	private float debug_outputTorque;

	private float _r;

	public RotationAudio rotationAudio;

	private float lastDriveMass;

	public float outputRPM => driveshaftRPM;

	private string nodeName => "TurbineTransmission_" + base.gameObject.name;

	public float inputTorque { get; private set; }

	private void Start()
	{
		lastDriveMass = driveMass;
		if ((bool)rotationAudio)
		{
			rotationAudio.manual = true;
		}
	}

	private void FixedUpdate()
	{
		float num = 0f;
		if (driveMass != lastDriveMass)
		{
			float num2 = (driveshaftRPM = lastDriveMass * driveshaftRPM / driveMass);
			lastDriveMass = driveMass;
		}
		for (int i = 0; i < engines.Length; i++)
		{
			if (!engines[i].failed)
			{
				float num3 = (engines[i].outputRPM - driveshaftRPM / rpmDifferentialRatio) * rpmDifferentialResistanceFactor * resistanceRPMCurve.Evaluate(engines[i].outputRPM);
				if (num3 < 0f)
				{
					num3 *= backblowTorqueRatio;
				}
				engines[i].AddResistanceTorque(num3);
				num += Mathf.Max(0f, num3 * outputTorqueRatio);
			}
		}
		inputTorque = num;
		num -= rpmFriction * driveshaftRPM;
		num -= _r;
		debug_appliedResistance = _r;
		_r = 0f;
		num = (debug_outputTorque = num - minTorqueResistance * Mathf.Clamp01((staticFrictionRPMFade - driveshaftRPM) / staticFrictionRPMFade));
		float num4 = driveshaftRPM * 0.10472f;
		num4 += num / driveMass * Time.fixedDeltaTime;
		driveshaftRPM = num4 / 0.10472f;
		driveshaftRPM = Mathf.Max(0f, driveshaftRPM);
		if ((bool)rotationAudio)
		{
			rotationAudio.UpdateAudioSpeed(driveshaftRPM);
		}
	}

	public void RemoteSetRPM(float rpm)
	{
		driveshaftRPM = rpm;
	}

	public void AddResistanceTorque(float t)
	{
		_r += t;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode(nodeName).SetValue("driveshaftRPM", driveshaftRPM);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNodeUtils.TryParseValue(qsNode.GetNode(nodeName), "driveshaftRPM", ref driveshaftRPM);
	}
}

}