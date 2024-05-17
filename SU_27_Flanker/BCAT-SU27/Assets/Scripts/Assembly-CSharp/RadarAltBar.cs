using UnityEngine;
using VTOLVR.DLC.Rotorcraft;

public class RadarAltBar : MonoBehaviour, IPilotReceiverHandler
{
	public Transform barTransform;

	public float barVelScale;

	public float maxScale;

	public FlightInfo flightInfo;

	public void OnPilotReceiver(AH94PilotReceiver receiver)
	{
		flightInfo = receiver.flightInfo;
	}

	private void Update()
	{
		float num = Mathf.Max(0f, flightInfo.radarAltitude) * barVelScale;
		if (num < maxScale)
		{
			barTransform.gameObject.SetActive(value: true);
			barTransform.localScale = new Vector3(1f, num, 1f);
		}
		else
		{
			barTransform.gameObject.SetActive(value: false);
		}
	}
}
