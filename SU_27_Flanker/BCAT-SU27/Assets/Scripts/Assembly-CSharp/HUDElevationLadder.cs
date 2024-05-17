using UnityEngine;
using UnityEngine.UI;

public class HUDElevationLadder : MonoBehaviour
{
	public Transform ladderTransform;

	public CollimatedHUDUI collimatedHud;

	public Transform vesselTransform;

	public Transform headTransform;

	public FlightInfo flightInfo;

	public GameObject climbTemplate;

	public GameObject descendTemplate;

	public Transform horizonTransform;

	private Transform myTransform;

	private void Start()
	{
		myTransform = base.transform;
		ConstructLadder();
	}

	private void ConstructLadder()
	{
		float depth = collimatedHud.depth;
		Vector3 vector = collimatedHud.GetScaleFactor() * Vector3.one;
		for (int i = 5; i <= 90; i += 5)
		{
			GameObject gameObject = Object.Instantiate(climbTemplate, ladderTransform);
			gameObject.GetComponentInChildren<Text>().text = i.ToString();
			gameObject.transform.localRotation = Quaternion.Euler(-i, 0f, 0f);
			gameObject.transform.localPosition = gameObject.transform.localRotation * new Vector3(0f, 0f, depth);
			gameObject.transform.localScale = vector * gameObject.transform.localScale.x;
		}
		for (int j = 5; j <= 90; j += 5)
		{
			GameObject gameObject2 = Object.Instantiate(descendTemplate, ladderTransform);
			gameObject2.GetComponentInChildren<Text>().text = j.ToString();
			gameObject2.transform.localRotation = Quaternion.Euler(j, 0f, 0f);
			gameObject2.transform.localPosition = gameObject2.transform.localRotation * new Vector3(0f, 0f, depth);
			gameObject2.transform.localScale = vector * gameObject2.transform.localScale.x;
		}
		horizonTransform.localScale *= vector.x;
		horizonTransform.localPosition = new Vector3(0f, 0f, depth);
		ladderTransform.localScale = 1f / vector.x * Vector3.one;
		climbTemplate.SetActive(value: false);
		descendTemplate.SetActive(value: false);
	}

	private void LateUpdate()
	{
		headTransform = VRHead.instance.transform;
		if ((bool)headTransform)
		{
			myTransform.position = headTransform.position;
			float x;
			if (flightInfo.airspeed > 50f)
			{
				Vector3 velocity = flightInfo.rb.velocity;
				myTransform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
				Vector3 fromDirection = velocity;
				fromDirection.y = 0f;
				x = VectorUtils.SignedAngle(fromDirection, velocity, Vector3.up);
			}
			else
			{
				myTransform.rotation = Quaternion.LookRotation(vesselTransform.forward, Vector3.up);
				x = flightInfo.pitch;
			}
			ladderTransform.localEulerAngles = new Vector3(x, 0f, 0f);
		}
	}
}
