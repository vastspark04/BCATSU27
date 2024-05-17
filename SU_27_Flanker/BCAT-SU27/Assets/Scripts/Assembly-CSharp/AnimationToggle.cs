using System.Collections;
using UnityEngine;

public class AnimationToggle : MonoBehaviour
{
	public Animator animator;

	public float time = 1f;

	public string animName;

	public int layer;

	private int animID;

	private bool deployed;

	private float t;

	private Coroutine animRoutine;

	public float GetT()
	{
		return t;
	}

	private void Awake()
	{
		animID = Animator.StringToHash(animName);
	}

	public void Toggle()
	{
		if (deployed)
		{
			Retract();
		}
		else
		{
			Deploy();
		}
	}

	public void Deploy()
	{
		if (!deployed)
		{
			if (animRoutine != null)
			{
				StopCoroutine(animRoutine);
			}
			animRoutine = StartCoroutine(AnimRoutine(1f));
			deployed = true;
		}
	}

	public void Retract()
	{
		if (deployed)
		{
			if (animRoutine != null)
			{
				StopCoroutine(animRoutine);
			}
			animRoutine = StartCoroutine(AnimRoutine(0f));
			deployed = false;
		}
	}

	private IEnumerator AnimRoutine(float tgt)
	{
		animator.speed = 0f;
		float delta = 1f / time;
		while (t != tgt)
		{
			t = Mathf.MoveTowards(t, tgt, delta * Time.deltaTime);
			animator.Play(animID, layer, t);
			yield return null;
		}
		animator.speed = 0f;
	}
}
