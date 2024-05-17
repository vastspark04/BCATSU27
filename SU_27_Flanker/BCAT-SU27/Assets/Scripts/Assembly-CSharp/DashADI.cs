using UnityEngine;

public class DashADI : MonoBehaviour
{
	public FlightInfo flightInfo;

	public Battery battery;

	public GameObject displayObj;

	public Transform rollTf;

	public Transform pitchTf;

	public float pitchFactor;

	private void Update()
	{
		if (!battery || battery.Drain(0.01f * Time.deltaTime))
		{
			if ((bool)displayObj)
			{
				displayObj.SetActive(value: true);
			}
			rollTf.transform.localRotation = Quaternion.Euler(0f, 0f, flightInfo.roll);
			pitchTf.localPosition = new Vector3(0f, pitchFactor * flightInfo.pitch, 0f);
		}
		else if ((bool)displayObj)
		{
			displayObj.SetActive(value: false);
		}
	}
}
