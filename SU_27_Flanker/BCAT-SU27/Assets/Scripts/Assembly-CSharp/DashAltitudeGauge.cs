using UnityEngine;
using UnityEngine.UI;

public class DashAltitudeGauge : DashGauge
{
	public FlightInfo flightInfo;

	public MeasurementManager measurements;

	public Text hundredsText;

	protected override float GetMeteredValue()
	{
		float num = measurements.ConvertedAltitude(flightInfo.altitudeASL);
		float num2 = Mathf.Floor(num / 100f);
		hundredsText.text = num2.ToString("000");
		return Mathf.Repeat(num / 1000f, 1f);
	}
}
