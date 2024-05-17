using UnityEngine;
using UnityEngine.UI;

public class HUDHeadingLadder : MonoBehaviour
{
	public FlightInfo flightInfo;

	public Transform ladderTransform;

	public float zeroHeading = 90f;

	public float degreesToPixels = 26.2f;

	public Text hdgText;

	private void Start()
	{
		if (!flightInfo)
		{
			flightInfo = GetComponentInParent<FlightInfo>();
		}
		if (!ladderTransform)
		{
			ladderTransform = base.transform;
		}
	}

	private void Update()
	{
		Vector3 localPosition = ladderTransform.localPosition;
		float num = (localPosition.x = (0f - (flightInfo.heading - zeroHeading)) * degreesToPixels);
		ladderTransform.localPosition = localPosition;
		hdgText.text = Mathf.Round(flightInfo.heading).ToString();
	}
}
