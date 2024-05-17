using System;
using System.Collections.Generic;
using UnityEngine;

public class FuelTank : MonoBehaviour, IMassObject, IQSVehicleComponent
{
	public float baseMass;

	public float startingFuel;

	public float maxFuel;

	public float fuelDensity;

	private double currentFuel;

	private double totalFuelD;

	private double _fuelDrain;

	private double lastFuelLevel;

	public List<FuelTank> subFuelTanks = new List<FuelTank>();

	private double _fuelDrainBuffer;

	private float lastTimeDrainUpdate;

	private float drainUpdateInterval = 0.3f;

	public bool remoteOnly { get; set; }

	public float totalFuel { get; private set; }

	public float fuelDrain => (float)_fuelDrain;

	public float fuel => (float)currentFuel;

	public float fuelFraction => (float)currentFuel / maxFuel;

	public void GetSubFuelInfo(out float subCurrFuel, out float subMaxFuel)
	{
		float num = 0f;
		float num2 = 0f;
		if (subFuelTanks != null)
		{
			for (int i = 0; i < subFuelTanks.Count; i++)
			{
				num += subFuelTanks[i].fuel;
				num2 = Mathf.Max(num2, 0f);
				num2 += subFuelTanks[i].maxFuel;
			}
		}
		subCurrFuel = num;
		subMaxFuel = num2;
	}

	private void Awake()
	{
		currentFuel = Mathf.Min(startingFuel, maxFuel);
	}

	public float RequestFuel(float deltaFuelF)
	{
		return RequestFuel((double)deltaFuelF);
	}

	public float RequestFuel(double deltaFuel)
	{
		if (deltaFuel == 0.0)
		{
			return 1f;
		}
		deltaFuel = Math.Abs(deltaFuel);
		_fuelDrainBuffer += deltaFuel;
		if (remoteOnly)
		{
			return 1f;
		}
		totalFuel = (float)currentFuel;
		totalFuelD = currentFuel;
		if (subFuelTanks.Count > 0)
		{
			int num = 0;
			double num2 = 0.0;
			foreach (FuelTank subFuelTank in subFuelTanks)
			{
				if (subFuelTank.currentFuel > 0.0)
				{
					totalFuel += (float)subFuelTank.currentFuel;
					totalFuelD += subFuelTank.currentFuel;
					num++;
					num2 += subFuelTank.currentFuel;
				}
			}
			if (num > 0)
			{
				double num3 = 0.0;
				double num4 = deltaFuel / (double)num;
				foreach (FuelTank subFuelTank2 in subFuelTanks)
				{
					num3 += (double)subFuelTank2.RequestFuel(num4) * num4;
				}
				return (float)(num3 / deltaFuel);
			}
		}
		double num5 = currentFuel;
		currentFuel = Math.Max(0.0, num5 - deltaFuel);
		return (float)((num5 - currentFuel) / deltaFuel);
	}

	public void DetachFueltank(FuelTank f)
	{
		subFuelTanks.Remove(f);
	}

	public bool AddFuel(float deltaFuel)
	{
		deltaFuel = Mathf.Abs(deltaFuel);
		double num = currentFuel;
		currentFuel = Math.Min(maxFuel, currentFuel + (double)deltaFuel);
		_fuelDrainBuffer -= currentFuel - num;
		return currentFuel >= (double)maxFuel;
	}

	public void SetNormFuel(float f)
	{
		currentFuel = maxFuel * Mathf.Clamp01(f);
	}

	private void FixedUpdate()
	{
		_fuelDrain = _fuelDrainBuffer / (double)Time.fixedDeltaTime;
		_fuelDrain = Math.Max(_fuelDrain, 0.0);
		_fuelDrainBuffer = 0.0;
	}

	public float GetMass()
	{
		return baseMass + (float)currentFuel * fuelDensity;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_FuelTank");
		configNode.SetValue("normFuel", fuelFraction);
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = base.gameObject.name + "_FuelTank";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			SetNormFuel(node.GetValue<float>("normFuel"));
		}
	}
}
