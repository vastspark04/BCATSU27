using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VRMotionTransformCurve
{
	public List<VRMotionCapture.CaptureFrame> frames = new List<VRMotionCapture.CaptureFrame>();

	private Curve3D positionCurve;

	private AnimationCurve rotCurveX;

	private AnimationCurve rotCurveY;

	private AnimationCurve rotCurveZ;

	private AnimationCurve rotCurveW;

	public void ApplyTransform(float t, Transform tf, Transform referenceTf, float lerpRate)
	{
		if (positionCurve == null)
		{
			SetupCurves();
		}
		Vector3 point = positionCurve.GetPoint(t);
		Quaternion quaternion = new Quaternion(rotCurveX.Evaluate(t), rotCurveY.Evaluate(t), rotCurveZ.Evaluate(t), rotCurveW.Evaluate(t));
		tf.position = Vector3.Lerp(tf.position, referenceTf.TransformPoint(point), lerpRate * Time.deltaTime);
		tf.rotation = Quaternion.Slerp(tf.rotation, referenceTf.rotation * quaternion, lerpRate * Time.deltaTime);
	}

	private void SetupCurves()
	{
		positionCurve = new Curve3D();
		rotCurveX = new AnimationCurve();
		rotCurveY = new AnimationCurve();
		rotCurveZ = new AnimationCurve();
		rotCurveW = new AnimationCurve();
		Vector3[] array = new Vector3[frames.Count];
		float[] array2 = new float[frames.Count];
		for (int i = 0; i < array.Length; i++)
		{
			float time = frames[i].time;
			array[i] = frames[i].position;
			array2[i] = time;
			Quaternion rotation = frames[i].rotation;
			rotCurveX.AddKey(new Keyframe(time, rotation.x));
			rotCurveY.AddKey(new Keyframe(time, rotation.y));
			rotCurveZ.AddKey(new Keyframe(time, rotation.z));
			rotCurveW.AddKey(new Keyframe(time, rotation.w));
		}
		positionCurve.SetPoints(array, array2);
	}
}
