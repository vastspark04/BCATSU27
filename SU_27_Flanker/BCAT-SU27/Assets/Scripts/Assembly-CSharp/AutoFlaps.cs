using UnityEngine;

public class AutoFlaps : MonoBehaviour
{
	public FlightInfo flightInfo;

	public AeroController aeroController;

	public AnimationCurve speedFlapsCurve;

	public FlightControlComponent[] outputs;

	private void Update()
	{
		float flaps = speedFlapsCurve.Evaluate(flightInfo.airspeed);
		aeroController.SetFlaps(flaps);
		if (outputs != null)
		{
			for (int i = 0; i < outputs.Length; i++)
			{
				outputs[i].SetFlaps(flaps);
			}
		}
	}
}
