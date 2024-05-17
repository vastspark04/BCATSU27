using System;
using System.Collections;
using UnityEngine;

public class TranslationToggle : MonoBehaviour
{
	[Serializable]
	public class TranslationTransform
	{
		public Transform transform;

		public Vector3 axis;

		public float distance;

		public float speed;

		public bool smooth;

		public float smoothRate;

		public AudioAnimator audio;

		private Vector3 defaultPosition;

		private Vector3 deployedPosition;

		private Vector3 prevPos;

		private float tSpeed;

		private float linearCurrT;

		public float currT { get; private set; }

		public void Init(bool deployedByDefault)
		{
			Vector3 vector = transform.parent.InverseTransformVector(transform.TransformVector(axis));
			defaultPosition = transform.localPosition;
			deployedPosition = transform.parent.InverseTransformPoint(transform.position + transform.TransformDirection(vector.normalized) * distance);
			if (deployedByDefault)
			{
				transform.localPosition = deployedPosition;
				currT = (linearCurrT = 1f);
			}
			else
			{
				currT = (linearCurrT = 0f);
			}
			if ((bool)audio)
			{
				audio.Evaluate(0f);
			}
			tSpeed = speed / distance;
		}

		public bool Update(float target, float deltaTime)
		{
			target = Mathf.Clamp01(target);
			float num = currT;
			linearCurrT = Mathf.MoveTowards(linearCurrT, target, tSpeed * deltaTime);
			if (smooth)
			{
				prevPos = transform.localPosition;
				currT = Mathf.Lerp(currT, linearCurrT, smoothRate * deltaTime);
			}
			else
			{
				currT = linearCurrT;
			}
			bool flag = false;
			if (Mathf.Abs(currT - target) <= 0.002f)
			{
				currT = target;
				flag = true;
			}
			transform.localPosition = Vector3.Lerp(defaultPosition, deployedPosition, currT);
			if ((bool)audio)
			{
				float t = ((!flag) ? 1 : 0);
				if (smooth && !flag)
				{
					t = Mathf.Clamp01((num - currT) / (tSpeed * deltaTime));
				}
				audio.Evaluate(t);
			}
			return flag;
		}
	}

	public TranslationTransform[] transforms;

	public bool deployedByDefault;

	private Coroutine translationRoutine;

	private bool deployed;

	public bool fixedTime;

	private void Awake()
	{
		for (int i = 0; i < transforms.Length; i++)
		{
			transforms[i].Init(deployedByDefault);
		}
		if (deployedByDefault)
		{
			deployed = true;
		}
	}

	public void SetDeployed()
	{
		if (translationRoutine != null)
		{
			StopCoroutine(translationRoutine);
		}
		deployed = true;
		translationRoutine = StartCoroutine(TranslationRoutine(1f));
	}

	public void SetDefault()
	{
		if (translationRoutine != null)
		{
			StopCoroutine(translationRoutine);
		}
		deployed = false;
		translationRoutine = StartCoroutine(TranslationRoutine(0f));
	}

	public void Toggle()
	{
		if (translationRoutine != null)
		{
			StopCoroutine(translationRoutine);
		}
		if (deployed)
		{
			translationRoutine = StartCoroutine(TranslationRoutine(0f));
		}
		else
		{
			translationRoutine = StartCoroutine(TranslationRoutine(1f));
		}
		deployed = !deployed;
	}

	private IEnumerator TranslationRoutine(float target)
	{
		bool done = false;
		int tfCount = transforms.Length;
		while (!done)
		{
			done = true;
			for (int i = 0; i < tfCount; i++)
			{
				if (!transforms[i].Update(target, fixedTime ? Time.fixedDeltaTime : Time.deltaTime))
				{
					done = false;
				}
			}
			if (fixedTime)
			{
				yield return new WaitForFixedUpdate();
			}
			else
			{
				yield return null;
			}
		}
	}
}
