using UnityEngine;
using UnityEngine.UI;

public class HUDAoAMeter : MonoBehaviour
{
	public FlightInfo flightInfo;

	public Text text;

	private void Start()
	{
		if (!flightInfo)
		{
			flightInfo = GetComponentInParent<FlightInfo>();
		}
	}

	private void Update()
	{
		float num = 0f;
		if (flightInfo.airspeed > 10f)
		{
			num = Mathf.Round(flightInfo.aoa);
		}
		text.text = num.ToString();
	}
}
