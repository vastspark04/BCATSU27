using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRMotionCapture : MonoBehaviour
{
	[Serializable]
	public struct CaptureFrame
	{
		public Vector3 position;

		public Quaternion rotation;

		public float time;

		public CaptureFrame(Transform tf, float time, Transform referenceTf)
		{
			position = referenceTf.InverseTransformPoint(tf.position);
			rotation = Quaternion.Inverse(referenceTf.rotation) * tf.rotation;
			this.time = time;
		}
	}

	public bool playOnStart;

	public bool loop;

	public VRMotionRecording recordingObject;

	public Transform headTransform;

	public Transform leftHandTransform;

	public Transform rightHandTransform;

	public Transform referenceTransform;

	public float timeInterval = 0.2f;

	private bool recording;

	private Coroutine playRtn;

	private void OnEnable()
	{
		if (playOnStart)
		{
			PlayRecording();
		}
	}

	public void StartRecording()
	{
		if (!recording)
		{
			StartCoroutine(RecordRoutine());
		}
	}

	public void StopRecording()
	{
		recording = false;
	}

	public void ToggleRecording()
	{
		if (recording)
		{
			StopRecording();
		}
		else
		{
			StartRecording();
		}
	}

	public void PlayRecording()
	{
		if (playRtn != null)
		{
			StopCoroutine(playRtn);
		}
		playRtn = StartCoroutine(PlayRoutine());
	}

	public void StopPlaying()
	{
		if (playRtn != null)
		{
			StopCoroutine(playRtn);
		}
	}

	private IEnumerator PlayRoutine()
	{
		yield return null;
		float length = recordingObject.GetTimeLength();
		float t = 0f;
		recordingObject.SetTime(0f, headTransform, leftHandTransform, rightHandTransform, referenceTransform, 9999999f);
		float lerpRate = 15f;
		while (t < length)
		{
			recordingObject.SetTime(t, headTransform, leftHandTransform, rightHandTransform, referenceTransform, lerpRate);
			t += Time.deltaTime;
			yield return null;
			lerpRate = Mathf.MoveTowards(lerpRate, 15f, 5f * Time.deltaTime);
			if (loop && t >= length)
			{
				t = 0f;
				lerpRate = 1f;
			}
		}
	}

	private IEnumerator RecordRoutine()
	{
		recording = true;
		WaitForSeconds wait = new WaitForSeconds(timeInterval);
		List<CaptureFrame> headFrames = new List<CaptureFrame>();
		List<CaptureFrame> lhFrames = new List<CaptureFrame>();
		List<CaptureFrame> rhFrames = new List<CaptureFrame>();
		float t = 0f;
		while (recording)
		{
			headFrames.Add(new CaptureFrame(headTransform, t, referenceTransform));
			lhFrames.Add(new CaptureFrame(leftHandTransform, t, referenceTransform));
			rhFrames.Add(new CaptureFrame(rightHandTransform, t, referenceTransform));
			t += timeInterval;
			yield return wait;
		}
		recordingObject.headAnimation = new VRMotionTransformCurve();
		recordingObject.leftHandAnimation = new VRMotionTransformCurve();
		recordingObject.rightHandAnimation = new VRMotionTransformCurve();
		for (int i = 0; i < headFrames.Count; i++)
		{
			recordingObject.headAnimation.frames.Add(headFrames[i]);
			recordingObject.leftHandAnimation.frames.Add(lhFrames[i]);
			recordingObject.rightHandAnimation.frames.Add(rhFrames[i]);
		}
	}
}
