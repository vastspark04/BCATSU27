using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class RotorFlapCalculator : MonoBehaviour
{
	public TurbineTransmission transmission;

	public HeliWingFlapper[] flappers;

	public float baseDriveshaftMass;

	public float radiusAdjustFactor;

	public float totalFlap { get; set; }

	private void Update()
	{
		totalFlap = 0f;
		for (int i = 0; i < flappers.Length; i++)
		{
			totalFlap += Mathf.Abs(flappers[i].currentFlap);
		}
		transmission.driveMass = baseDriveshaftMass - totalFlap * radiusAdjustFactor;
	}
}

}