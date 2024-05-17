using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class TurbineDriveshaft : MonoBehaviour
{
	public TurbineTransmission transmission;

	public float gearRatio;

	private float appliedResistanceTorque;

	private float debug_lastAppliedResistance;

	private float lastAbsRPM;

	public float outputRPM { get; private set; }

	public float rotationAcceleration { get; private set; }

	private void FixedUpdate()
	{
		outputRPM = transmission.outputRPM * gearRatio;
		float num = Mathf.Abs(outputRPM);
		rotationAcceleration = (num - lastAbsRPM) / Time.fixedDeltaTime;
		lastAbsRPM = num;
		transmission.AddResistanceTorque(appliedResistanceTorque * gearRatio);
		debug_lastAppliedResistance = appliedResistanceTorque;
		appliedResistanceTorque = 0f;
	}

	public void AddResistanceTorque(float t)
	{
		appliedResistanceTorque += t;
	}
}

}