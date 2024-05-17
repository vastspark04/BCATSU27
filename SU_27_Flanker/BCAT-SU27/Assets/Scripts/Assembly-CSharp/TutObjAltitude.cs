using UnityEngine;

public class TutObjAltitude : CustomTutorialObjective
{
	public FlightInfo flightInfo;

	public bool radarAlt;

	public float altitude;

	public float threshold = 50f;

	public override bool GetIsCompleted()
	{
		return Mathf.Abs((radarAlt ? flightInfo.radarAltitude : flightInfo.altitudeASL) - altitude) < threshold;
	}
}
