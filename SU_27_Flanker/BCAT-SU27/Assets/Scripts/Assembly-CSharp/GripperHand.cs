using System.Collections.Generic;
using UnityEngine;

public class GripperHand : MonoBehaviour
{
	public List<GripperHandDigit> digits;

	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.G))
		{
			Grab();
		}
	}

	public void Grab()
	{
		foreach (GripperHandDigit digit in digits)
		{
			digit.Grab();
		}
	}
}
