using System.Collections;
using UnityEngine;

public class BDCoroutine : CustomYieldInstruction
{
	public delegate void BDCoroutineAction(BDCoroutine bcr);

	private bool running;

	private MonoBehaviour script;

	private Coroutine cr;

	public override bool keepWaiting => running;

	public event BDCoroutineAction OnCoroutineFinished;

	public event BDCoroutineAction OnCoroutineForceStopped;

	public BDCoroutine(IEnumerator coroutine, MonoBehaviour script)
	{
		this.script = script;
		cr = script.StartCoroutine(coroutine);
		running = true;
		script.StartCoroutine(WaitForFinish());
	}

	public void StopCoroutine()
	{
		running = false;
		if (script != null && cr != null)
		{
			script.StopCoroutine(cr);
			if (this.OnCoroutineForceStopped != null)
			{
				this.OnCoroutineForceStopped(this);
			}
		}
	}

	private IEnumerator WaitForFinish()
	{
		yield return cr;
		running = false;
		if (this.OnCoroutineFinished != null)
		{
			this.OnCoroutineFinished(this);
		}
	}
}
