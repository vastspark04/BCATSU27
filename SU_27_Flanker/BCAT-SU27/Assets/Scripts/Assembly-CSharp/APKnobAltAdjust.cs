using UnityEngine;
using UnityEngine.UI;

public class APKnobAltAdjust : MonoBehaviour
{
	public VTOLAutoPilot autoPilot;

	public Text altSetText;

	public MeasurementManager measurements;

	public float adjustRate;

	private bool apAltEnabled;

	private float rawAltHold;

	private void Awake()
	{
		altSetText.gameObject.SetActive(value: false);
		measurements.OnChangedAltitudeMode += Measurements_OnChangedAltitudeMode;
	}

	private void Measurements_OnChangedAltitudeMode()
	{
		UpdateAltText();
	}

	private void Update()
	{
		if (autoPilot.altitudeHold != apAltEnabled)
		{
			apAltEnabled = autoPilot.altitudeHold;
			altSetText.gameObject.SetActive(apAltEnabled);
			rawAltHold = autoPilot.altitudeToHold;
			UpdateAltText();
		}
	}

	private void UpdateAltText()
	{
		altSetText.text = Mathf.RoundToInt(measurements.ConvertedAltitude(autoPilot.altitudeToHold)).ToString();
	}

	public void OnTwistDelta(float delta)
	{
		if (apAltEnabled)
		{
			float num = adjustRate * delta;
			rawAltHold += num;
			rawAltHold = Mathf.Max(rawAltHold, 15f);
			float num2 = measurements.ConvertedAltitude(rawAltHold);
			float num3 = ((!(num > 0f)) ? (Mathf.Floor(num2 / 10f) * 10f) : (Mathf.Ceil(num2 / 10f) * 10f));
			num3 /= measurements.ConvertedAltitude(1f);
			autoPilot.altitudeToHold = num3;
			UpdateAltText();
		}
	}
}
