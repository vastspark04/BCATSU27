using UnityEngine;
using UnityEngine.UI;

public class HUDGearWarning : MonoBehaviour
{
	public FlightInfo flightInfo;

	public GearAnimator gearAnimator;

	public Text warnText;

	public float warnInterval;

	public FlightWarnings flightWarnings;

	public bool useCommonWarnings = true;

	public FlightWarnings.FlightWarning flightWarning;

	private float timeWarned;

	private bool wasWarn;

	private bool landingWarning;

	private bool takeOffWarning;

	private bool warnCleared;

	private GearAnimator.GearStates lastState = GearAnimator.GearStates.Moving;

	private void Start()
	{
		flightWarnings.OnClearedWarnings.AddListener(OnCleared);
	}

	private void OnCleared()
	{
		warnCleared = true;
	}

	private void Update()
	{
		landingWarning = gearAnimator.state == GearAnimator.GearStates.Retracted && flightInfo.radarAltitude < 500f && flightInfo.airspeed < 120f && flightInfo.verticalSpeed < 0f;
		takeOffWarning = gearAnimator.state == GearAnimator.GearStates.Extended && !flightInfo.isLanded && flightInfo.airspeed > 90f && flightInfo.verticalSpeed > 0f && flightInfo.radarAltitude > 50f;
		if ((bool)flightWarnings.battery && !flightWarnings.battery.Drain(0.01f * Time.deltaTime))
		{
			warnCleared = false;
			lastState = GearAnimator.GearStates.Moving;
		}
		if (lastState != gearAnimator.state || flightInfo.isLanded)
		{
			warnCleared = false;
			lastState = gearAnimator.state;
		}
		if ((landingWarning || takeOffWarning) && !warnCleared)
		{
			if (Time.time - timeWarned > warnInterval)
			{
				timeWarned = Time.time;
			}
			if (Time.time - timeWarned < warnInterval * 0.75f)
			{
				warnText.enabled = true;
			}
			else
			{
				warnText.enabled = false;
			}
			if (!wasWarn)
			{
				if (useCommonWarnings)
				{
					flightWarnings.AddCommonWarningContinuous(FlightWarnings.CommonWarnings.LandingGear);
				}
				else
				{
					flightWarnings.AddContinuousWarning(flightWarning);
				}
				wasWarn = true;
			}
			return;
		}
		if (wasWarn)
		{
			if (useCommonWarnings)
			{
				flightWarnings.RemoveCommonWarning(FlightWarnings.CommonWarnings.LandingGear);
			}
			else
			{
				flightWarnings.RemoveContinuousWarning(flightWarning);
			}
			wasWarn = false;
		}
		warnText.enabled = false;
	}
}
