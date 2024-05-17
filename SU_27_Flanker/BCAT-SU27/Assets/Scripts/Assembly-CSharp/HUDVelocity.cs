using UnityEngine;
using UnityEngine.UI;

public class HUDVelocity : MonoBehaviour
{
	public Text unitLabel;

	private Text text;

	private FlightInfo flightInfo;

	private MeasurementManager measurements;

	public bool overrideUnits;

	public MeasurementManager.SpeedModes overrideUnit;

	private MeasurementManager.SpeedModes originalUnit;

	private bool hasUnitLabel;

	private void Start()
	{
		text = GetComponent<Text>();
		measurements = GetComponentInParent<MeasurementManager>();
		flightInfo = GetComponentInParent<FlightInfo>();
		if ((bool)unitLabel)
		{
			hasUnitLabel = true;
		}
	}

	private void Update()
	{
		if (overrideUnits)
		{
			originalUnit = measurements.airspeedMode;
			measurements.airspeedMode = overrideUnit;
		}
		text.text = measurements.FormattedSpeed(flightInfo.airspeed);
		if (hasUnitLabel)
		{
			unitLabel.text = measurements.SpeedLabel();
		}
		if (overrideUnits)
		{
			measurements.airspeedMode = originalUnit;
		}
	}
}
