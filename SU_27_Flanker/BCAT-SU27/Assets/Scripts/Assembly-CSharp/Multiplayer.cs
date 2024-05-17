using System;
using UnityEngine;

public class Multiplayer : MonoBehaviour
{
	public bool doMultiplayer;

	private void Start()
	{
		if (doMultiplayer)
		{
			DoMultiplayer();
		}
	}

	private void DoMultiplayer()
	{
		throw new NotImplementedException("TODO: Multiplayer");
	}
}
