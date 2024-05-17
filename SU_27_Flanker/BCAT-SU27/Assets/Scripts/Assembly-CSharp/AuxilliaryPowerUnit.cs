using System;
using UnityEngine;

public class AuxilliaryPowerUnit : ElectronicComponent, IQSVehicleComponent
{
	public FuelTank fuelTank;

	public float fuelDrainRate;

	public float drainRate;

	public float chargeRate;

	public float spoolRate;

	private float throttle;

	private bool powerEnabled;

	public AudioAnimator[] audioEffects;

	private bool destroyed;

	private bool isRemote;

	public float rpm => throttle;

	public bool isPowerEnabled => powerEnabled;

	public event Action<int> OnSetState;

	public void SetToRemote()
	{
		isRemote = true;
	}

	private void FixedUpdate()
	{
		if (powerEnabled && !destroyed && DrainElectricity(drainRate * Time.fixedDeltaTime))
		{
			if (fuelTank.RequestFuel(fuelDrainRate * Time.fixedDeltaTime) > 0.5f)
			{
				throttle = Mathf.MoveTowards(throttle, 1f, spoolRate * Time.fixedDeltaTime);
			}
			else
			{
				throttle = Mathf.MoveTowards(throttle, 0f, spoolRate * Time.fixedDeltaTime);
				if (!isRemote && throttle < 0.001f)
				{
					Shutdown();
				}
			}
			battery.Charge(chargeRate * throttle * Time.fixedDeltaTime);
		}
		else
		{
			throttle = Mathf.MoveTowards(throttle, 0f, spoolRate * Time.fixedDeltaTime);
		}
		for (int i = 0; i < audioEffects.Length; i++)
		{
			audioEffects[i].Evaluate(throttle);
		}
	}

	public void DestroyUnit()
	{
		destroyed = true;
		if (powerEnabled)
		{
			Shutdown();
		}
		FlightWarnings componentInParent = GetComponentInParent<FlightWarnings>();
		if ((bool)componentInParent)
		{
			componentInParent.AddCommonWarningContinuous(FlightWarnings.CommonWarnings.APUFailure);
		}
	}

	public void RepairUnit()
	{
		destroyed = false;
	}

	public void Shutdown()
	{
		powerEnabled = false;
		this.OnSetState?.Invoke(0);
	}

	public void PowerUp()
	{
		powerEnabled = true;
		this.OnSetState?.Invoke(1);
	}

	public void SetPower3Way(int p)
	{
		switch (p)
		{
		case 2:
			if (!powerEnabled && battery.connected)
			{
				PowerUp();
			}
			break;
		case 0:
			if (powerEnabled)
			{
				Shutdown();
			}
			break;
		}
	}

	public void SetPower(int p)
	{
		if (p == 0)
		{
			if (powerEnabled)
			{
				Shutdown();
			}
		}
		else if (!powerEnabled)
		{
			PowerUp();
		}
	}

	public void RemoteSetPower(bool p)
	{
		powerEnabled = p;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_APU");
		configNode.SetValue("powerEnabled", powerEnabled);
		configNode.SetValue("throttle", throttle);
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = base.gameObject.name + "_APU";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			powerEnabled = node.GetValue<bool>("powerEnabled");
			throttle = node.GetValue<float>("throttle");
		}
	}
}
