using UnityEngine;

public class HUDJoyIndicator : FlightControlComponent
{
	public Transform indicatorTf;

	public Transform prTrimIndicator;

	public Transform yawTrimIndicator;

	public Transform rudderTf;

	public float maxOffset = 40f;

	public GameObject throttleDisplayObject;

	public Transform throttleTransform;

	public Transform hoverThrottleTransform;

	public GameObject hoverDisplayObject;

	private bool hoverAltMode;

	private float throttle;

	public GameObject airbrakeDisplay;

	public Transform airbrakeTf;

	private Vector3 trimPitchYawRoll;

	public override void SetTrim(Vector3 trimPitchYawRoll)
	{
		this.trimPitchYawRoll = trimPitchYawRoll;
		UpdateTrimIndicators();
	}

	private void Start()
	{
		UpdateTrimIndicators();
	}

	private void UpdateTrimIndicators()
	{
		if ((bool)prTrimIndicator)
		{
			Vector3 localPosition = maxOffset * new Vector3(0f - trimPitchYawRoll.z, trimPitchYawRoll.x, 0f);
			prTrimIndicator.localPosition = localPosition;
		}
		if ((bool)yawTrimIndicator)
		{
			Vector3 localPosition2 = yawTrimIndicator.localPosition;
			localPosition2.x = maxOffset * trimPitchYawRoll.y;
			yawTrimIndicator.localPosition = localPosition2;
		}
	}

	public void SetTrimPitchRoll(float pitch, float roll)
	{
		trimPitchYawRoll.x = pitch;
		trimPitchYawRoll.z = roll;
		UpdateTrimIndicators();
	}

	public void SetTrimYaw(float yaw)
	{
		trimPitchYawRoll.y = yaw;
		UpdateTrimIndicators();
	}

	public override void SetPitchYawRoll(Vector3 pitchYawRoll)
	{
		Vector3 localPosition = maxOffset * new Vector3(0f - pitchYawRoll.z, pitchYawRoll.x, 0f);
		indicatorTf.localPosition = localPosition;
		Vector3 localPosition2 = rudderTf.localPosition;
		localPosition2.x = maxOffset * pitchYawRoll.y;
		rudderTf.localPosition = localPosition2;
	}

	public override void SetThrottle(float throttle)
	{
		this.throttle = throttle;
		if (hoverAltMode)
		{
			Vector3 localScale = hoverThrottleTransform.localScale;
			float num = (localScale.y = Mathf.Abs(throttle - 0.5f));
			hoverThrottleTransform.localScale = localScale;
			float z = 0f;
			if (throttle < 0.5f)
			{
				z = 180f;
			}
			hoverThrottleTransform.localEulerAngles = new Vector3(0f, 0f, z);
		}
		else
		{
			Vector3 localScale2 = throttleTransform.localScale;
			localScale2.y = throttle;
			throttleTransform.localScale = localScale2;
		}
	}

	public override void SetBrakes(float brakes)
	{
		if (brakes > 0.04f)
		{
			airbrakeDisplay.SetActive(value: true);
			Vector3 localScale = airbrakeTf.localScale;
			localScale.y = brakes;
			airbrakeTf.localScale = localScale;
		}
		else
		{
			airbrakeDisplay.SetActive(value: false);
		}
	}

	public void SetToHoverMode()
	{
		throttleDisplayObject.SetActive(value: false);
		hoverDisplayObject.SetActive(value: true);
		hoverAltMode = true;
		SetThrottle(throttle);
	}

	public void SetToThrottleMode()
	{
		throttleDisplayObject.SetActive(value: true);
		hoverDisplayObject.SetActive(value: false);
		hoverAltMode = false;
		SetThrottle(throttle);
	}
}
