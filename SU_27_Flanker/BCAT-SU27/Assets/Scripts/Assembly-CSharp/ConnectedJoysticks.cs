using System;
using UnityEngine;

public class ConnectedJoysticks : MonoBehaviour
{
	public VRJoystick[] joysticks;

	private int masterJoyIdx = -1;

	private bool[] grabbed;

	public int CurrentMasterIdx => masterJoyIdx;

	public event Action<int> OnSetMasterIdx;

	public event Action<int> OnLocalGrabbedStick;

	public event Action<int> OnLocalReleasedStick;

	public bool IsGrabbed(int ctrlIdx)
	{
		return grabbed[ctrlIdx];
	}

	private void Start()
	{
		grabbed = new bool[joysticks.Length];
		for (int i = 0; i < joysticks.Length; i++)
		{
			int tIdx = i;
			VRInteractable component = joysticks[i].GetComponent<VRInteractable>();
			component.OnInteract.AddListener(delegate
			{
				OnGrabbedStick(tIdx);
				this.OnLocalGrabbedStick?.Invoke(tIdx);
			});
			component.OnStopInteract.AddListener(delegate
			{
				OnReleasedStick(tIdx);
				this.OnLocalReleasedStick?.Invoke(tIdx);
			});
			joysticks[tIdx].sendEvents = false;
		}
	}

	private void OnGrabbedStick(int tIdx)
	{
		if (masterJoyIdx < 0)
		{
			masterJoyIdx = tIdx;
			joysticks[tIdx].sendEvents = true;
			this.OnSetMasterIdx?.Invoke(masterJoyIdx);
		}
		grabbed[tIdx] = true;
	}

	private void OnReleasedStick(int tIdx)
	{
		grabbed[tIdx] = false;
		joysticks[tIdx].sendEvents = false;
		VRJoystick vRJoystick = joysticks[tIdx];
		if (masterJoyIdx != tIdx)
		{
			return;
		}
		masterJoyIdx = -1;
		for (int i = 0; i < grabbed.Length; i++)
		{
			if (grabbed[i])
			{
				masterJoyIdx = i;
				joysticks[i].sendEvents = true;
				break;
			}
		}
		this.OnSetMasterIdx?.Invoke(masterJoyIdx);
		if (masterJoyIdx == -1 && vRJoystick.returnToZeroWhenReleased)
		{
			vRJoystick.OnSetStick?.Invoke(Vector3.zero);
		}
	}

	private void LateUpdate()
	{
		if (masterJoyIdx < 0)
		{
			return;
		}
		for (int i = 0; i < joysticks.Length; i++)
		{
			if (i != masterJoyIdx)
			{
				joysticks[i].RemoteSetStick(joysticks[masterJoyIdx].CurrentStick);
			}
		}
	}

	public void OverrideSetMaster(int tIdx)
	{
		if (masterJoyIdx > 0 && masterJoyIdx != tIdx)
		{
			joysticks[masterJoyIdx].sendEvents = false;
		}
		if (masterJoyIdx != tIdx)
		{
			masterJoyIdx = tIdx;
			if (tIdx >= 0)
			{
				joysticks[tIdx].sendEvents = true;
			}
		}
	}

	public void RemoteGrab(int tIdx)
	{
		OnGrabbedStick(tIdx);
	}

	public void RemoteRelease(int tIdx)
	{
		OnReleasedStick(tIdx);
	}
}
