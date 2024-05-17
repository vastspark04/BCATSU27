using UnityEngine;

namespace VTOLVR.Multiplayer{

public class AIPilotControlSender : FlightControlComponent
{
	public AIPilotSync aiSync;

	public override void SetPitchYawRoll(Vector3 pitchYawRoll)
	{
		aiSync.SetPitchYawRoll(pitchYawRoll);
	}

	public override void SetBrakes(float brakes)
	{
		aiSync.SetBrakes(brakes);
	}

	public override void SetFlaps(float flaps)
	{
		aiSync.SetFlaps(flaps);
	}

	public override void SetThrottle(float throttle)
	{
		aiSync.SetThrottle(throttle);
	}
}

}