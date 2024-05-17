using UnityEngine;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class AH94HUDCopilotInfo : MonoBehaviour
{
	public Text[] nameTexts;

	public Text[] statusTexts;

	public MultiUserVehicleSync muvs;

	private int wpnSeatIdx;

	private int tgpSeatIdx;

	private int flightSeatIdx;

	private void OnEnable()
	{
		Text[] array = nameTexts;
		foreach (Text text in array)
		{
			if ((bool)text)
			{
				text.text = string.Empty;
			}
		}
		array = statusTexts;
		foreach (Text text2 in array)
		{
			if ((bool)text2)
			{
				text2.text = string.Empty;
			}
		}
		muvs.OnOccupantEntered += Muvs_OnOccupantEntered;
		muvs.OnOccupantLeft += Muvs_OnOccupantLeft;
		muvs.OnPilotTakeTGP += Muvs_OnPilotTakeTGP;
		muvs.OnSetWeaponControllerId += Muvs_OnSetWeaponControllerId;
		muvs.OnSetControlOwnerID += Muvs_OnSetControlOwnerID;
		Muvs_OnSetControlOwnerID(muvs.controlOwner);
		Muvs_OnPilotTakeTGP(muvs.tgpControllerId);
		Muvs_OnSetWeaponControllerId(muvs.weaponControllerId);
		for (int j = 0; j < muvs.seatCount; j++)
		{
			ulong occupantID = muvs.GetOccupantID(j);
			if (occupantID != 0)
			{
				Muvs_OnOccupantEntered(j, occupantID);
			}
			else
			{
				Muvs_OnOccupantLeft(j, occupantID);
			}
		}
	}

	private void OnDisable()
	{
		if ((bool)muvs)
		{
			muvs.OnOccupantEntered -= Muvs_OnOccupantEntered;
			muvs.OnOccupantLeft -= Muvs_OnOccupantLeft;
			muvs.OnPilotTakeTGP -= Muvs_OnPilotTakeTGP;
			muvs.OnSetWeaponControllerId -= Muvs_OnSetWeaponControllerId;
			muvs.OnSetControlOwnerID -= Muvs_OnSetControlOwnerID;
		}
	}

	private void Muvs_OnSetControlOwnerID(ulong obj)
	{
		flightSeatIdx = muvs.UserSeatIdx(obj);
		UpdateStatuses();
	}

	private void Muvs_OnSetWeaponControllerId(ulong obj)
	{
		wpnSeatIdx = muvs.UserSeatIdx(obj);
		UpdateStatuses();
	}

	private void Muvs_OnPilotTakeTGP(ulong obj)
	{
		tgpSeatIdx = muvs.UserSeatIdx(obj);
		UpdateStatuses();
	}

	private void Muvs_OnOccupantLeft(int seatIdx, ulong userID)
	{
		nameTexts[seatIdx].text = string.Empty;
	}

	private void Muvs_OnOccupantEntered(int seatIdx, ulong userID)
	{
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(userID);
		string text = ((player == null) ? string.Empty : player.pilotName);
		if ((bool)nameTexts[seatIdx])
		{
			nameTexts[seatIdx].text = text;
		}
	}

	private void UpdateStatuses()
	{
		for (int i = 0; i < statusTexts.Length; i++)
		{
			if ((bool)statusTexts[i])
			{
				string text = string.Empty;
				if (flightSeatIdx == i)
				{
					text += "FLY ";
				}
				if (wpnSeatIdx == i && muvs.wm.isMasterArmed)
				{
					text += "WPN ";
				}
				if (tgpSeatIdx == i && muvs.tgpPage.powered)
				{
					text += "TADS ";
				}
				statusTexts[i].text = text;
			}
		}
	}
}

}