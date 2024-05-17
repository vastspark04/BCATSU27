using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineTest : MonoBehaviour
{
	private Coroutine cr;

	private void Start()
	{
		cr = StartCoroutine(ContinueDebugMessagesRoutine());
	}

	private void OnDisable()
	{
		if (cr != null)
		{
			StopCoroutine(cr);
		}
	}

	private IEnumerator TestRoutine()
	{
		MonoBehaviour.print("Test start");
		yield return new BDCoroutine(Wait(), this);
		MonoBehaviour.print("Test End");
	}

	private IEnumerator DelayedKillUnity(List<Coroutine> crList)
	{
		yield return new WaitForSeconds(3f);
		foreach (Coroutine cr in crList)
		{
			StopCoroutine(cr);
		}
	}

	private IEnumerator DelayedKill(BDCoroutine cr)
	{
		yield return new WaitForSeconds(3f);
		cr.StopCoroutine();
	}

	private IEnumerator Wait()
	{
		int i = 0;
		while (i < 6)
		{
			Debug.Log(i++);
			yield return new WaitForSeconds(1f);
		}
	}

	private IEnumerator ContinueDebugMessagesRoutine()
	{
		while (true)
		{
			Debug.Log(Time.frameCount);
			yield return new WaitForSeconds(1f);
		}
	}
}
