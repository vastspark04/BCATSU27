using System;
using System.Globalization;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.Multiplayer{

public class VTMPLobbyListItem : MonoBehaviour
{
	public VTMPMainMenu menu;

	public Text lobbyNameText;

	public Text ownerNameText;

	public Text scenarioText;

	public UIMaskedTextScroller scenarioMask;

	public Text memberCountText;

	public GameObject fullObj;

	public VRInteractable interactable;

	public Text gameStateText;

	public Text elapsedTimeText;

	public UnityEngine.Color[] gameStateColors;

	private Lobby lobby;

	private bool isPrivate;

	public void JoinButton()
	{
		if (isPrivate)
		{
			menu.TryJoinPrivateLobby(lobby);
		}
		else
		{
			menu.JoinLobby(lobby);
		}
	}

	public void UpdateForLobby(Lobby l)
	{
		lobby = l;
		lobbyNameText.text = l.GetData("lName");
		scenarioMask.Refresh();
		memberCountText.text = l.MemberCount.ToString();
		bool flag = l.MemberCount < int.Parse(l.GetData("maxP"));
		fullObj.SetActive(!flag);
		interactable.gameObject.SetActive(flag);
		string data = l.GetData("pwh");
		if (!string.IsNullOrEmpty(data) && int.TryParse(data, out var result) && result != 0)
		{
			ownerNameText.text = "Private Lobby";
			scenarioText.text = "???";
			isPrivate = true;
		}
		else
		{
			isPrivate = false;
			ownerNameText.text = l.GetData("oName");
			scenarioText.text = l.GetData("scn");
		}
		if (!gameStateText || !elapsedTimeText)
		{
			return;
		}
		if (!isPrivate)
		{
			if (Enum.TryParse<VTOLMPLobbyManager.GameStates>(l.GetData("gState"), out var result2))
			{
				gameStateText.gameObject.SetActive(value: true);
				gameStateText.text = result2.ToString();
				gameStateText.color = gameStateColors[(int)result2];
				if (result2 == VTOLMPLobbyManager.GameStates.Mission)
				{
					MissionElapsedTime(l.GetData("mUtc"), out var hours, out var minutes);
					if (hours >= 0 && minutes >= 0)
					{
						elapsedTimeText.gameObject.SetActive(value: true);
						elapsedTimeText.text = string.Format("{0}:{1}", hours, minutes.ToString("00"));
					}
					else
					{
						elapsedTimeText.gameObject.SetActive(value: false);
					}
				}
				else
				{
					elapsedTimeText.gameObject.SetActive(value: false);
				}
			}
			else
			{
				gameStateText.gameObject.SetActive(value: false);
				elapsedTimeText.gameObject.SetActive(value: false);
			}
		}
		else
		{
			gameStateText.gameObject.SetActive(value: false);
			elapsedTimeText.gameObject.SetActive(value: false);
		}
	}

	private void MissionElapsedTime(string mUtc, out int hours, out int minutes)
	{
		if (!string.IsNullOrEmpty(mUtc))
		{
			CultureInfo provider = new CultureInfo("en-US");
			if (DateTime.TryParse(mUtc, provider, DateTimeStyles.None, out var result))
			{
				TimeSpan timeSpan = DateTime.UtcNow - result;
				minutes = timeSpan.Minutes;
				hours = timeSpan.Hours;
				return;
			}
		}
		minutes = -1;
		hours = -1;
	}
}

}