using UnityEngine;

public abstract class ElectronicComponent : MonoBehaviour
{
	public Battery battery;

	protected bool DrainElectricity(float drainAmount)
	{
		return battery.Drain(drainAmount);
	}
}
