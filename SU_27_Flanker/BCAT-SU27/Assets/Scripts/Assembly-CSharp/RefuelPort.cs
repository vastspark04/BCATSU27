using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class RefuelPort : MonoBehaviour
{
	public Actor actor;

	public FuelTank fuelTank;

	public float refuelRate;

	public AudioSource refuelAudioSource;

	public AudioSource refuelLoopAudioSource;

	public AudioClip refuelStartSound;

	public AudioClip refuelLoopSound;

	public AudioClip refuelEndSound;

	public AudioClip refuelFailSound;

	public bool isPlayer;

	public UnityEvent OnOpen;

	public UnityEvent OnClose;

	public bool open { get; private set; }

	public bool isRemote { get; private set; }

	public bool remoteNeedsFuel { get; set; }

	public RefuelPlane currentRefuelPlane { get; private set; }

	public event Action<int> OnSetState;

	public void SetToRemote()
	{
		isRemote = true;
	}

	private void Start()
	{
		TargetManager.instance.refuelPorts.Add(this);
		if (!actor)
		{
			actor = GetComponentInParent<Actor>();
		}
	}

	private void OnDestroy()
	{
		if ((bool)TargetManager.instance && TargetManager.instance.refuelPorts != null)
		{
			TargetManager.instance.refuelPorts.Remove(this);
		}
	}

	public void StartRefuel(RefuelPlane rp)
	{
		currentRefuelPlane = rp;
		if ((bool)refuelAudioSource)
		{
			refuelAudioSource.PlayOneShot(refuelStartSound);
		}
		if ((bool)refuelLoopAudioSource)
		{
			refuelLoopAudioSource.clip = refuelLoopSound;
			refuelLoopAudioSource.Play();
		}
		if (isPlayer)
		{
			StartCoroutine(DelayedRadioMessage(0.68f, "refuelContact"));
		}
	}

	public void EndRefuel()
	{
		currentRefuelPlane = null;
		if ((bool)refuelLoopAudioSource)
		{
			refuelLoopAudioSource.Stop();
		}
		if ((bool)refuelAudioSource)
		{
			refuelAudioSource.PlayOneShot(refuelEndSound);
		}
		if (isPlayer)
		{
			StartCoroutine(DelayedRadioMessage(0.9f, "refuelComplete"));
		}
	}

	public void FailRefuel()
	{
		currentRefuelPlane = null;
		if ((bool)refuelLoopAudioSource)
		{
			refuelLoopAudioSource.Stop();
		}
		if ((bool)refuelAudioSource)
		{
			refuelAudioSource.PlayOneShot(refuelFailSound);
		}
		if (isPlayer)
		{
			StartCoroutine(DelayedRadioMessage(0.9f, "refuelFail"));
		}
	}

	private IEnumerator DelayedRadioMessage(float delay, string message)
	{
		yield return new WaitForSeconds(delay);
		CommRadioManager.instance.PlayMessage(message);
	}

	public bool Refuel()
	{
		return fuelTank.AddFuel(refuelRate * Time.deltaTime);
	}

	public void Open()
	{
		open = true;
		if (OnOpen != null)
		{
			OnOpen.Invoke();
		}
		this.OnSetState?.Invoke(1);
	}

	public void Close()
	{
		open = false;
		if (OnClose != null)
		{
			OnClose.Invoke();
		}
		this.OnSetState?.Invoke(0);
	}

	public void SetOpenState(int state)
	{
		if (state == 1)
		{
			if (!open)
			{
				Open();
			}
		}
		else if (open)
		{
			Close();
		}
	}
}
