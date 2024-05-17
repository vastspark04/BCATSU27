using System;
using UnityEngine;

public class ConnectedThrottles : MonoBehaviour
{
	public VRThrottle[] throttles;

	private int masterThrottleIdx = -1;

	private bool[] grabbed;

	public int CurrentMasterIdx => masterThrottleIdx;

	public event Action<int> OnSetMasterIdx;

	public event Action<int> OnLocalGrabbedThrottle;

	public event Action<int> OnLocalReleasedThrottle;

	private void Start()
	{
		grabbed = new bool[throttles.Length];
		for (int i = 0; i < throttles.Length; i++)
		{
			int tIdx = i;
			VRInteractable component = throttles[i].GetComponent<VRInteractable>();
			component.OnInteract.AddListener(delegate
			{
				OnGrabbedThrottle(tIdx);
				this.OnLocalGrabbedThrottle?.Invoke(tIdx);
			});
			component.OnStopInteract.AddListener(delegate
			{
				OnReleasedThrottle(tIdx);
				this.OnLocalReleasedThrottle?.Invoke(tIdx);
			});
			throttles[tIdx].sendEvents = false;
		}
	}

	private void OnGrabbedThrottle(int tIdx)
	{
		if (masterThrottleIdx < 0)
		{
			masterThrottleIdx = tIdx;
			throttles[tIdx].sendEvents = true;
		}
		grabbed[tIdx] = true;
	}

	private void OnReleasedThrottle(int tIdx)
	{
		grabbed[tIdx] = false;
		throttles[tIdx].sendEvents = false;
		if (masterThrottleIdx != tIdx)
		{
			return;
		}
		masterThrottleIdx = -1;
		for (int i = 0; i < grabbed.Length; i++)
		{
			if (grabbed[i])
			{
				masterThrottleIdx = i;
				throttles[i].sendEvents = true;
				break;
			}
		}
		this.OnSetMasterIdx?.Invoke(masterThrottleIdx);
	}

	private void LateUpdate()
	{
		if (masterThrottleIdx < 0)
		{
			return;
		}
		for (int i = 0; i < throttles.Length; i++)
		{
			if (i != masterThrottleIdx)
			{
				throttles[i].RemoteSetThrottle(throttles[masterThrottleIdx].currentThrottle);
			}
		}
	}

	public void OverrideSetThrottles(float t)
	{
		for (int i = 0; i < throttles.Length; i++)
		{
			_ = throttles[i].sendEvents;
			throttles[i].sendEvents = true;
			throttles[i].RemoteSetThrottle(t);
			throttles[i].sendEvents = false;
		}
	}

	public void OverrideSetMaster(int tIdx)
	{
		if (masterThrottleIdx > 0 && masterThrottleIdx != tIdx)
		{
			throttles[masterThrottleIdx].sendEvents = false;
		}
		if (masterThrottleIdx != tIdx)
		{
			masterThrottleIdx = tIdx;
			if (tIdx >= 0)
			{
				throttles[tIdx].sendEvents = true;
			}
		}
	}

	public void RemoteGrab(int tIdx)
	{
		OnGrabbedThrottle(tIdx);
	}

	public void RemoteRelease(int tIdx)
	{
		OnReleasedThrottle(tIdx);
	}
}
