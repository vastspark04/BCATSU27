using UnityEngine;

public class HUDOpticalLandingSystem : MonoBehaviour
{
	public Tailhook hook;

	public GameObject displayObj;

	public Transform currBallTf;

	public GameObject goodBallTf;

	public float intervalHeight;

	public float persistTime = 1f;

	private OpticalLandingSystem.OLSData data;

	private float timeRecieved;

	private void Start()
	{
		hook.OnReceivedOLSData += Hook_OnReceivedOLSData;
		displayObj.SetActive(value: false);
	}

	private void Hook_OnReceivedOLSData(OpticalLandingSystem.OLSData obj)
	{
		timeRecieved = Time.time;
		data = obj;
	}

	private void Update()
	{
		if (Time.time - timeRecieved < persistTime)
		{
			displayObj.SetActive(value: true);
			currBallTf.localPosition = new Vector3(0f, (float)(-data.ball) * intervalHeight, 0f);
			goodBallTf.gameObject.SetActive(data.ball == 0);
		}
		else
		{
			displayObj.SetActive(value: false);
		}
	}
}
