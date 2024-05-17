using UnityEngine;

public class TutObjTimedPoint : CustomTutorialObjective
{
	public float duration;

	private float startTime;

	public override void OnStartObjective()
	{
		startTime = Time.time;
	}

	public override bool GetIsCompleted()
	{
		return Time.time - startTime > duration;
	}
}
