using System;
using UnityEngine;

public class MultiSlotVehicleManager : MonoBehaviour
{
	[Serializable]
	public class PlayerSlot
	{
		public GameObject localPlayerObj;

		public GameObject localNPCObj;

		public Transform spawnTransform;

		public string netEntResourcePath;
	}

	public PlayerSlot[] slots;
}
