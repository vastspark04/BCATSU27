using UnityEngine;

public class NoABWhenTilt : MonoBehaviour
{
	public VRThrottle throttle;

	public TiltController tilt;

	public float tiltedLimit;

	public float tiltThreshold = 89f;

	private void Update()
	{
		if (tilt.currentTilt < tiltThreshold)
		{
			throttle.throttleLimiter = tiltedLimit;
		}
		else
		{
			throttle.throttleLimiter = 1f;
		}
	}
}
