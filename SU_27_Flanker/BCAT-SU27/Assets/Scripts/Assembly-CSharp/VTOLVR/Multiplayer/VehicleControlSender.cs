using UnityEngine;

namespace VTOLVR.Multiplayer{

public class VehicleControlSender : FlightControlComponent
{
	public PlayerVehicleNetSync netSync;

	public override void SetPitchYawRoll(Vector3 pitchYawRoll)
	{
		netSync.SetPitchYawRoll(pitchYawRoll);
	}

	public override void SetThrottle(float throttle)
	{
		netSync.SetThrottle(throttle);
	}

	public override void SetBrakes(float brakes)
	{
		netSync.SetBrakes(brakes);
	}

	public void SetRawInputPYR(Vector3 pitchYawRoll)
	{
		netSync.SetRawInputPYR(pitchYawRoll);
	}

	public override void SetFlaps(float flaps)
	{
		base.SetFlaps(flaps);
		netSync.SetFlaps(flaps);
	}
}

}