using System;
using System.Collections;
using UnityEngine;

public class GunCycleAnimation : MonoBehaviour
{
	[Serializable]
	public class CycleTransform
	{
		public Transform transform;

		public Vector3 axis = Vector3.forward;

		public float distance;

		public float startNormTime;

		public float midNormTime = 0.2f;

		public float endNormTime = 1f;

		private Vector3 startPoint;

		private Vector3 endPoint;

		public void Initialize()
		{
			startPoint = transform.localPosition;
			Vector3 vector = transform.parent.InverseTransformDirection(transform.TransformDirection(axis));
			endPoint = startPoint + vector * distance;
		}

		public void Update(float normTime)
		{
			if (normTime <= startNormTime || normTime >= endNormTime)
			{
				transform.localPosition = startPoint;
			}
			else if (normTime < midNormTime)
			{
				float t = Mathf.InverseLerp(startNormTime, midNormTime, normTime);
				transform.localPosition = Vector3.Lerp(startPoint, endPoint, t);
			}
			else
			{
				float t2 = Mathf.InverseLerp(midNormTime, endNormTime, normTime);
				transform.localPosition = Vector3.Lerp(endPoint, startPoint, t2);
			}
		}
	}

	public Gun gun;

	public CycleTransform[] transforms;

	private void Start()
	{
		gun.OnFired += Gun_OnFired;
		for (int i = 0; i < transforms.Length; i++)
		{
			transforms[i].Initialize();
		}
	}

	private void Gun_OnFired()
	{
		StartCoroutine(CycleRoutine());
	}

	private IEnumerator CycleRoutine()
	{
		float normTime = 0f;
		while (normTime < 1f)
		{
			for (int i = 0; i < transforms.Length; i++)
			{
				transforms[i].Update(normTime);
			}
			float num = Time.deltaTime * gun.rpm / 60f;
			normTime += num;
			yield return null;
		}
		for (int j = 0; j < transforms.Length; j++)
		{
			transforms[j].Update(0f);
		}
	}
}
