using UnityEngine;

public class AeroSpeedTrimmer : MonoBehaviour
{
	public FlightInfo flightInfo;

	public AeroController aeroController;

	public int[] indices;

	public AnimationCurve speedTrimCurve;

	private float lastAirspeed = -1f;

	private void Update()
	{
		if (Mathf.Abs(flightInfo.airspeed - lastAirspeed) > 0.01f)
		{
			lastAirspeed = flightInfo.airspeed;
			float trim = speedTrimCurve.Evaluate(flightInfo.airspeed);
			for (int i = 0; i < indices.Length; i++)
			{
				int num = indices[i];
				aeroController.controlSurfaces[num].trim = trim;
			}
		}
	}
}
