using UnityEngine;

public class Loadout
{
	public string[] hpLoadout;

	public int[] cmLoadout;

	private float _normalizedFuel;

	public float normalizedFuel
	{
		get
		{
			return _normalizedFuel;
		}
		set
		{
			_normalizedFuel = Mathf.Clamp01(value);
		}
	}
}
