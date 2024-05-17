using UnityEngine;

[CreateAssetMenu]
public class VRMotionRecording : ScriptableObject
{
	public VRMotionTransformCurve headAnimation;

	public VRMotionTransformCurve leftHandAnimation;

	public VRMotionTransformCurve rightHandAnimation;

	public float GetTimeLength()
	{
		return headAnimation.frames[headAnimation.frames.Count - 1].time;
	}

	public void SetTime(float t, Transform headTf, Transform lhTf, Transform rhTf, Transform referenceTf, float lerpRate = 15f)
	{
		headAnimation.ApplyTransform(t, headTf, referenceTf, lerpRate);
		leftHandAnimation.ApplyTransform(t, lhTf, referenceTf, lerpRate);
		rightHandAnimation.ApplyTransform(t, rhTf, referenceTf, lerpRate);
	}
}
