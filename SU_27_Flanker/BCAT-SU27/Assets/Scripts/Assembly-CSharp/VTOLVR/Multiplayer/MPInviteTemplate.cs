using System;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.Multiplayer{

public class MPInviteTemplate : MonoBehaviour
{
	public RawImage playerImage;

	public Text playerNameText;

	public Friend friend;

	public Lobby lobby;

	public GameObject displayObj;

	public Action<Lobby, Friend> onAccept;

	public void Accept()
	{
		displayObj.SetActive(value: false);
		onAccept?.Invoke(lobby, friend);
	}

	public void Deny()
	{
		displayObj.SetActive(value: false);
	}
}

}