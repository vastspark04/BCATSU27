using System;
using UnityEngine;

namespace VTOLVR.Multiplayer{

public class PerSeatObjects : MonoBehaviour
{
	[Serializable]
	public class SeatObjs
	{
		public GameObject[] objects;
	}

	[Serializable]
	public class PerSeatPosition
	{
		public GameObject gObj;

		public Transform[] positions;
	}

	public MultiUserVehicleSync muvs;

	public GameObject[] hideInSP;

	public GameObject[] hideInMP;

	public SeatObjs[] seatExclusiveMPObjects;

	public PerSeatPosition[] perSeatPositions;

	private void Awake()
	{
		bool flag = VTOLMPUtils.IsMultiplayer();
		hideInSP.SetActive(flag);
		hideInMP.SetActive(!flag);
		if (!flag)
		{
			return;
		}
		if (muvs.IsLocalPlayerSeated())
		{
			OnEnterSeat(muvs.LocalPlayerSeatIdx());
			return;
		}
		SeatObjs[] array = seatExclusiveMPObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].objects.SetActive(active: false);
		}
		muvs.OnOccupantEntered += Muvs_OnOccupantEntered;
	}

	private void Muvs_OnOccupantEntered(int seatIdx, ulong userID)
	{
		if (userID == BDSteamClient.mySteamID)
		{
			OnEnterSeat(seatIdx);
		}
	}

	private void OnEnterSeat(int seatIdx)
	{
		for (int i = 0; i < seatExclusiveMPObjects.Length; i++)
		{
			seatExclusiveMPObjects[i].objects.SetActive(i == seatIdx);
		}
		for (int j = 0; j < perSeatPositions.Length; j++)
		{
			PerSeatPosition perSeatPosition = perSeatPositions[j];
			perSeatPosition.gObj.transform.position = perSeatPosition.positions[seatIdx].position;
		}
	}
}

}