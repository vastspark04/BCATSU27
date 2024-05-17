using Rewired;
using UnityEngine;

public class VehicleInputManager : MonoBehaviour
{
	private bool useLeftStickRudder;

	private bool useHardwareRudder;

	private Vector3 vrJoyPYR;

	private Vector3 throttleThumbstick;

	private float hardwareRudder;

	private float hardwareBrakeR;

	private float hardwareBrakeL;

	private bool wheelBrakesBound;

	private float virtualBrakes;

	public FlightControlComponent[] pyrOutputs;

	public FlightControlComponent[] wheelSteerOutputs;

	public TiltController tiltController;

	public ThrottleSOISwitcher soiSwitcher;

	public bool thumbRudderAllowed = true;

	private Player rwPlayer;

	private void Start()
	{
		useLeftStickRudder = thumbRudderAllowed && GameSettings.CurrentSettings.GetBoolSetting("THUMB_RUDDER");
		useHardwareRudder = GameSettings.CurrentSettings.GetBoolSetting("HARDWARE_RUDDER");
		if (ReInput.isReady)
		{
			rwPlayer = ReInput.players.GetPlayer(0);
			wheelBrakesBound = IsWheelbrakeBound();
		}
	}

	private bool IsWheelbrakeBound()
	{
		if (ReInput.isReady)
		{
			if (rwPlayer.controllers.maps.GetFirstElementMapWithAction(5, skipDisabledMaps: true) == null)
			{
				return rwPlayer.controllers.maps.GetFirstElementMapWithAction(5, skipDisabledMaps: true) != null;
			}
			return true;
		}
		return false;
	}

	private void Update()
	{
		Vector3 pitchYawRoll = vrJoyPYR;
		float brakeL = virtualBrakes;
		float brakeR = virtualBrakes;
		if (useHardwareRudder)
		{
			UpdateHardwareRudder();
			pitchYawRoll.y = hardwareRudder;
			if (wheelBrakesBound)
			{
				brakeL = hardwareBrakeL;
				brakeR = hardwareBrakeR;
			}
		}
		else if (useLeftStickRudder)
		{
			pitchYawRoll.y = throttleThumbstick.x;
		}
		for (int i = 0; i < pyrOutputs.Length; i++)
		{
			pyrOutputs[i].SetPitchYawRoll(pitchYawRoll);
		}
		for (int j = 0; j < wheelSteerOutputs.Length; j++)
		{
			wheelSteerOutputs[j].SetWheelSteer(pitchYawRoll.y);
			if (wheelSteerOutputs[j] is WheelsController)
			{
				WheelsController obj = (WheelsController)wheelSteerOutputs[j];
				obj.SetBrakeL(brakeL);
				obj.SetBrakeR(brakeR);
			}
		}
		if ((bool)tiltController)
		{
			if (useLeftStickRudder)
			{
				if (Mathf.Abs(throttleThumbstick.x) < 0.25f && Mathf.Abs(throttleThumbstick.y) > 0.5f)
				{
					tiltController.PadInputScaled(new Vector3(0f, (throttleThumbstick.y - Mathf.Sign(throttleThumbstick.y) * 0.5f) * 2f, 0f));
				}
				else
				{
					tiltController.PadInputScaled(Vector3.zero);
				}
			}
			else
			{
				tiltController.PadInputScaled(new Vector3(0f, throttleThumbstick.y, 0f));
			}
		}
		if ((bool)soiSwitcher && !useLeftStickRudder)
		{
			soiSwitcher.OnSetThumbstick(throttleThumbstick);
		}
	}

	private void UpdateHardwareRudder()
	{
		if (ReInput.isReady)
		{
			hardwareRudder = rwPlayer.GetAxis(0);
			hardwareBrakeR = rwPlayer.VTRWGetAxis(6);
			hardwareBrakeL = rwPlayer.VTRWGetAxis(5);
		}
	}

	public void SetJoystickPYR(Vector3 pyr)
	{
		vrJoyPYR = pyr;
	}

	public void SetThrottleThumbstick(Vector3 ts)
	{
		throttleThumbstick = ts;
	}

	public void SetVirtualBrakes(float b)
	{
		virtualBrakes = b;
	}
}
