using UnityEngine;
using UnityEngine.UI;

public class VerticalSpeedIndicator : ElectronicComponent
{
	public FlightInfo flightInfo;

	public MeasurementManager measurements;

	public Transform vsiTransform;

	public float maxDisplaySpeed;

	public float lerpRate = 5f;

	public float translationScale;

	public Text unitText;

	private float y;

	private void Start()
	{
		y = vsiTransform.localPosition.y;
		UpdateUnitText();
		measurements.OnChangedAltitudeMode += UpdateUnitText;
	}

	private void Update()
	{
		if (battery.Drain(0.01f * Time.deltaTime))
		{
			float num = measurements.ConvertedVerticalSpeed(flightInfo.verticalSpeed);
			if (measurements.altitudeMode == MeasurementManager.AltitudeModes.Feet)
			{
				num /= 100f;
			}
			num = Mathf.Clamp(num, 0f - maxDisplaySpeed, maxDisplaySpeed);
			y = Mathf.Lerp(y, num * translationScale, lerpRate * Time.deltaTime);
			vsiTransform.localPosition = new Vector3(0f, y, 0f);
		}
	}

	private void UpdateUnitText()
	{
		if (measurements.altitudeMode == MeasurementManager.AltitudeModes.Meters)
		{
			unitText.text = "m/s";
		}
		else
		{
			unitText.text = "x100 FPM";
		}
	}
}
