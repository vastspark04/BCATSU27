using UnityEngine;

public class TutObjFaceHeading : CustomTutorialObjective
{
	[Range(0f, 359.99f)]
	public float heading;

	public float threshold = 5f;

	private FlightInfo flightInfo;

	public override void OnStartObjective()
	{
		base.OnStartObjective();
		flightInfo = GetComponentInParent<FlightInfo>();
	}

	public override bool GetIsCompleted()
	{
		float num = flightInfo.heading + 360f;
		float num2 = heading + 360f;
		return Mathf.Abs(num - num2) < threshold;
	}
}
