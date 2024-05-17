using System;
using UnityEngine;

public class DashSpeedometer : DashGauge
{
	[Serializable]
	public class SpeedoProfile
	{
		public MeasurementManager.SpeedModes mode;

		public GameObject displayObject;

		public float maxSpeed;
	}

	public FlightInfo flightInfo;

	public MeasurementManager measurements;

	public SpeedoProfile[] profiles;

	private MeasurementManager.SpeedModes currMode = (MeasurementManager.SpeedModes)(-1);

	private void Start()
	{
		GetMeteredValue();
	}

	protected override float GetMeteredValue()
	{
		if (currMode != measurements.airspeedMode)
		{
			currMode = measurements.airspeedMode;
			SpeedoProfile[] array = profiles;
			foreach (SpeedoProfile speedoProfile in array)
			{
				if (speedoProfile.mode == currMode)
				{
					speedoProfile.displayObject.SetActive(value: true);
					maxValue = speedoProfile.maxSpeed;
				}
				else
				{
					speedoProfile.displayObject.SetActive(value: false);
				}
			}
		}
		return measurements.ConvertedSpeed(flightInfo.airspeed);
	}
}
