using System;
using System.Collections;
using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class RotorBrake : MonoBehaviour, IQSVehicleComponent
{
	public float brakeTorque;

	public TurbineDriveshaft shaft;

	public RotationToggle rotorFolder;

	public VRLever foldDependentSwitch;

	public VRThrottle[] powerLevers;

	public VRLever[] switches;

	private bool brake;

	private Coroutine brakeRoutine;

	public event Action<bool> OnSetBrake;

	private void Awake()
	{
		VRThrottle[] array = powerLevers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnSetThrottle.AddListener(OnSetPowerLever);
		}
	}

	private void OnSetPowerLever(float t)
	{
		VRLever[] array;
		if (t > 0.32f)
		{
			array = switches;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].LockTo(0);
			}
			return;
		}
		array = switches;
		foreach (VRLever vRLever in array)
		{
			if (vRLever.currentState == 0 && vRLever.lockedToState >= 0)
			{
				vRLever.Unlock();
			}
		}
	}

	public void SetBrake(int b)
	{
		bool flag = b > 0;
		if (flag == brake)
		{
			return;
		}
		brake = flag;
		if (brake)
		{
			if (brakeRoutine != null)
			{
				StopCoroutine(brakeRoutine);
			}
			brakeRoutine = StartCoroutine(BrakeRoutine());
			this.OnSetBrake?.Invoke(flag);
		}
		UpdatePowerLever();
	}

	public void RemoteSetBrake(bool _brake)
	{
		if (_brake == brake)
		{
			return;
		}
		brake = _brake;
		if (brake)
		{
			if (brakeRoutine != null)
			{
				StopCoroutine(brakeRoutine);
			}
			brakeRoutine = StartCoroutine(BrakeRoutine());
		}
		UpdatePowerLever();
	}

	private void UpdatePowerLever()
	{
		VRThrottle[] array = powerLevers;
		foreach (VRThrottle vRThrottle in array)
		{
			if (brake)
			{
				vRThrottle.minThrottle = vRThrottle.abGateThreshold - 0.01f;
			}
			else
			{
				vRThrottle.minThrottle = 0f;
			}
		}
	}

	private void OnEnable()
	{
		if (brake)
		{
			if (brakeRoutine != null)
			{
				StopCoroutine(brakeRoutine);
			}
			brakeRoutine = StartCoroutine(BrakeRoutine());
		}
		UpdatePowerLever();
	}

	public bool IsBraking()
	{
		return brake;
	}

	private IEnumerator BrakeRoutine()
	{
		WaitForFixedUpdate fixedWait = new WaitForFixedUpdate();
		while (brake)
		{
			shaft.AddResistanceTorque(brakeTorque);
			if ((bool)foldDependentSwitch)
			{
				if (!AllRotorsLockedDeployed() || !rotorFolder.battery.Drain(0.001f * Time.fixedDeltaTime))
				{
					foldDependentSwitch.LockTo(1);
				}
				else
				{
					foldDependentSwitch.Unlock();
				}
			}
			yield return fixedWait;
		}
	}

	private bool AllRotorsLockedDeployed()
	{
		RotationToggle.RotationToggleTransform[] transforms = rotorFolder.transforms;
		for (int i = 0; i < transforms.Length; i++)
		{
			if (transforms[i].currentT > 0f)
			{
				return false;
			}
		}
		return true;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode("RotorBrake").SetValue("brake", brake);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("RotorBrake");
		bool target = brake;
		ConfigNodeUtils.TryParseValue(node, "brake", ref target);
		RemoteSetBrake(target);
	}
}

}