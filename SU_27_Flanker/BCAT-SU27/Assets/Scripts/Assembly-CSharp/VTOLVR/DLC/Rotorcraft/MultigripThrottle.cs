using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class MultigripThrottle : MonoBehaviour
{
	public VRThrottle throttleA;

	public VRThrottle throttleB;

	public FloatEvent OnSetThrottle;

	public void SetThrottleA(float t)
	{
		OnSetThrottle?.Invoke(t);
		throttleB.RemoteSetThrottleNoEvents(t);
	}

	public void SetThrottleB(float t)
	{
		OnSetThrottle?.Invoke(t);
		throttleA.RemoteSetThrottleNoEvents(t);
	}
}

}