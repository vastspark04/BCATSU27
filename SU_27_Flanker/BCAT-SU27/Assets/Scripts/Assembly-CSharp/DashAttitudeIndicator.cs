using UnityEngine;

public class DashAttitudeIndicator : MonoBehaviour
{
	public FlightInfo flightInfo;

	public Transform rollTf;

	public Transform pitchTf;

	public float lerpRate;

	private void Update()
	{
		Quaternion b = Quaternion.Euler(0f, 0f, 0f - flightInfo.roll);
		Quaternion b2 = Quaternion.Euler(flightInfo.pitch, 0f, 0f);
		rollTf.localRotation = Quaternion.Slerp(rollTf.localRotation, b, lerpRate * Time.deltaTime);
		pitchTf.localRotation = Quaternion.Slerp(pitchTf.localRotation, b2, lerpRate * Time.deltaTime);
	}
}
