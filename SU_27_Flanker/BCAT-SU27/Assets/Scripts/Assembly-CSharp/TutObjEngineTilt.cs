using UnityEngine;

public class TutObjEngineTilt : CustomTutorialObjective
{
	public TiltController tiltController;

	public float tilt;

	public float threshold = 5f;

	public override bool GetIsCompleted()
	{
		return Mathf.Abs(tiltController.currentTilt - tilt) < threshold;
	}
}
