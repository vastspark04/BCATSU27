using System;
using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class TurbineStarterMotor : MonoBehaviour
{
	public ModuleEngine turbine;

	public Battery battery;

	public float electricalDrain;

	public AnimationCurve torqueCurve;

	public float maxRPM;

	public RotationAudio[] audioAnims;

	public bool motorEnabled;

	public EmissiveTextureLight indicator;

	public bool threeWaySwitch = true;

	public event Action<bool> OnMotorState;

	public void SetMotorEnabled(int s)
	{
		bool flag = motorEnabled;
		if (threeWaySwitch)
		{
			switch (s)
			{
			case 2:
				flag = true;
				break;
			case 0:
				flag = false;
				break;
			}
		}
		else
		{
			flag = s > 0;
		}
		if (flag != motorEnabled)
		{
			motorEnabled = flag;
			this.OnMotorState?.Invoke(motorEnabled);
		}
		if ((bool)indicator)
		{
			indicator.SetStatus(motorEnabled ? 1 : 0);
		}
	}

	public void RemoteSetMotor(bool e)
	{
		if (e != motorEnabled)
		{
			motorEnabled = e;
		}
		if ((bool)indicator)
		{
			indicator.SetStatus(motorEnabled ? 1 : 0);
		}
	}

	private void Start()
	{
		if (audioAnims != null)
		{
			for (int i = 0; i < audioAnims.Length; i++)
			{
				audioAnims[i].manual = true;
			}
		}
	}

	private void FixedUpdate()
	{
		if (!motorEnabled)
		{
			return;
		}
		if (turbine.outputRPM < maxRPM && battery.Drain(electricalDrain * Time.fixedDeltaTime))
		{
			turbine.AddResistanceTorque(0f - torqueCurve.Evaluate(turbine.outputRPM));
			return;
		}
		motorEnabled = false;
		if ((bool)indicator)
		{
			indicator.SetStatus(0);
		}
		this.OnMotorState?.Invoke(obj: false);
	}

	private void Update()
	{
		if (motorEnabled)
		{
			if (audioAnims != null)
			{
				for (int i = 0; i < audioAnims.Length; i++)
				{
					audioAnims[i].UpdateAudioSpeed(turbine.outputRPM);
				}
			}
		}
		else if (audioAnims != null)
		{
			for (int j = 0; j < audioAnims.Length; j++)
			{
				audioAnims[j].UpdateAudioSpeed(0f);
			}
		}
	}
}

}