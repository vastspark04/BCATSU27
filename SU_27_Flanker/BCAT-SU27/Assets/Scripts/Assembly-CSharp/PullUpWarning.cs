using UnityEngine;

public class PullUpWarning : GenericCommonFlightWarning
{
	public GearAnimator landingGear;

	public float maxG = 6f;

	private float minAlt;

	private void Awake()
	{
		warning = FlightWarnings.CommonWarnings.PullUp;
		minAlt = VTOLVRConstants.WARN_PULL_UP_MIN_ALT;
	}

	protected override void Update()
	{
		base.Update();
		if (!flightInfo.isLanded && landingGear.state == GearAnimator.GearStates.Retracted && flightInfo.verticalSpeed < 0f)
		{
			float airspeed = flightInfo.airspeed;
			float num = maxG * 9.81f;
			float num2 = airspeed * airspeed / num;
			float num3 = Vector3.Dot(flightInfo.rb.velocity.normalized, Vector3.down);
			float num4 = Mathf.Max(num2 * num3 + minAlt, minAlt);
			doWarning = flightInfo.radarAltitude < num4;
		}
		else
		{
			doWarning = false;
		}
	}
}
