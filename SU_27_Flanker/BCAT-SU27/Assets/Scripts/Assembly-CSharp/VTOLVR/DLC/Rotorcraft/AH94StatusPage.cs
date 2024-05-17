using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class AH94StatusPage : MonoBehaviour
{
	public ModuleEngine[] engines;

	public Transform[] engineRPMBars;

	public HelicopterRotor rotor;

	public Transform rotorBar;

	public float rotorRPMScale;

	public TurbineTransmission transmission;

	public Transform torqueBar;

	public float torqueScale;

	private void Update()
	{
		for (int i = 0; i < engines.Length; i++)
		{
			engineRPMBars[i].localScale = new Vector3(engines[i].displayedRPM, 1f, 1f);
		}
		rotorBar.localScale = new Vector3(rotor.inputShaft.outputRPM / rotorRPMScale, 1f, 1f);
		torqueBar.localScale = new Vector3(transmission.inputTorque / torqueScale, 1f, 1f);
	}
}

}