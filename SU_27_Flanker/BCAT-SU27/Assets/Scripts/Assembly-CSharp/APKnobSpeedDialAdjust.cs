using UnityEngine;
using UnityEngine.UI;

public class APKnobSpeedDialAdjust : MonoBehaviour
{
	public VTOLAutoPilot autoPilot;

	public DashSpeedometer speedGauge;

	public Transform speedPointerTransform;

	public float adjustRate;

	public Text spdText;

	public Text apSpdText;

	public Battery battery;

	private bool speedEnabled;

	private MeasurementManager.SpeedModes lastSpeedMode = (MeasurementManager.SpeedModes)(-1);

	private string numFormat = "G";

	private void Awake()
	{
		speedPointerTransform.gameObject.SetActive(value: false);
		spdText.gameObject.SetActive(value: false);
		apSpdText.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if (battery.Drain(0.01f))
		{
			if (speedEnabled != autoPilot.speedMode)
			{
				speedEnabled = autoPilot.speedMode;
				speedPointerTransform.gameObject.SetActive(speedEnabled);
				apSpdText.gameObject.SetActive(speedEnabled);
				UpdateSpeedDial();
			}
			spdText.gameObject.SetActive(value: true);
			if (speedEnabled)
			{
				if (lastSpeedMode != speedGauge.measurements.airspeedMode)
				{
					lastSpeedMode = speedGauge.measurements.airspeedMode;
					UpdateSpeedDial();
				}
				apSpdText.text = Mathf.Round(speedGauge.measurements.ConvertedSpeed(autoPilot.speedToHold)).ToString(numFormat);
			}
			spdText.text = Mathf.Round(speedGauge.measurements.ConvertedSpeed(autoPilot.flightInfo.airspeed)).ToString(numFormat);
		}
		else
		{
			speedPointerTransform.gameObject.SetActive(value: false);
			spdText.gameObject.SetActive(value: false);
			apSpdText.gameObject.SetActive(value: false);
		}
	}

	private void UpdateSpeedDial()
	{
		speedPointerTransform.localRotation = speedGauge.GetHandAngle(speedGauge.measurements.ConvertedSpeed(autoPilot.speedToHold));
	}

	public void OnTwistDelta(float delta)
	{
		float max = speedGauge.maxValue / speedGauge.measurements.ConvertedSpeed(1f);
		autoPilot.speedToHold = Mathf.Clamp(autoPilot.speedToHold + delta * adjustRate, 0f, max);
		UpdateSpeedDial();
	}
}
