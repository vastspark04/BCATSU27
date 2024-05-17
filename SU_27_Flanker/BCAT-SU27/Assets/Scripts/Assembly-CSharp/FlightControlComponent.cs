using UnityEngine;

public abstract class FlightControlComponent : MonoBehaviour
{
	public virtual void SetPitchYawRoll(Vector3 pitchYawRoll)
	{
	}

	public virtual void SetTrim(Vector3 trimPitchYawRoll)
	{
	}

	public virtual void SetThrottle(float throttle)
	{
	}

	public virtual void SetBrakes(float brakes)
	{
	}

	public virtual void SetFlaps(float flaps)
	{
	}

	public virtual void SetGear(bool gearDown)
	{
	}

	public virtual void SetWheelSteer(float steer)
	{
	}
}
