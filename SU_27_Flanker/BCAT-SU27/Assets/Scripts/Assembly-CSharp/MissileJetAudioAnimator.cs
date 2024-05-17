using System.Collections;
using UnityEngine;

public class MissileJetAudioAnimator : AudioAnimator
{
	public float spoolUpRate;

	public float spoolDownRate;

	private Missile m;

	private void Start()
	{
		m = GetComponent<Missile>();
	}

	public void OnStartEngine()
	{
		StartCoroutine(EngineStartupRoutine());
	}

	public void OnStopEngine()
	{
		StartCoroutine(EngineShutdownRoutine());
	}

	private IEnumerator EngineStartupRoutine()
	{
		float t = 0f;
		while (t < 1f)
		{
			t = Mathf.MoveTowards(t, 1f, spoolUpRate * Time.deltaTime);
			Evaluate(t);
			yield return null;
		}
	}

	private IEnumerator EngineShutdownRoutine()
	{
		float t = 1f;
		while (t > 0f)
		{
			t = Mathf.MoveTowards(t, 0f, spoolDownRate * Time.deltaTime);
			Evaluate(t);
			yield return null;
		}
	}
}
