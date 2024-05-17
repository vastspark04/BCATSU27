using System;
using UnityEngine;

public class NavLightController : MonoBehaviour
{
	public ObjectPowerUnit[] powerUnits;

	public int state { get; private set; }

	public event Action<int> OnSetPower;

	public void SetPower(int p)
	{
		for (int i = 0; i < powerUnits.Length; i++)
		{
			if ((bool)powerUnits[i])
			{
				powerUnits[i].SetConnection(p);
			}
		}
		state = p;
		this.OnSetPower?.Invoke(p);
	}
}
