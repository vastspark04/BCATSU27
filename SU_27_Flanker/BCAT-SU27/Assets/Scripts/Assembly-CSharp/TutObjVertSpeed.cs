using UnityEngine;

public class TutObjVertSpeed : CustomTutorialObjective
{
	public FlightInfo flightInfo;

	public float duration;

	public float speed;

	public float threshold;

	private float accumulatedTime;

	public override bool GetIsCompleted()
	{
		if (Mathf.Abs(flightInfo.verticalSpeed - speed) < threshold)
		{
			accumulatedTime += Time.deltaTime;
		}
		else
		{
			accumulatedTime = 0f;
		}
		return accumulatedTime > duration;
	}
}
