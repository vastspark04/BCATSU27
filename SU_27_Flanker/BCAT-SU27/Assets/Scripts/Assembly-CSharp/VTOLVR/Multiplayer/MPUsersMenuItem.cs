using UnityEngine;
using UnityEngine.UI;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class MPUsersMenuItem : MonoBehaviour
{
	public MPUsersMenu menu;

	public SteamUserImageTexture userImage;

	public Text pilotName;

	public Text steamName;

	public Text voteKicks;

	public Text voteBans;

	public GameObject muteButton;

	public GameObject unmuteButton;

	public GameObject[] otherUserObjects;

	public GameObject[] hideForHost;

	private PlayerInfo user;

	public void SetupForUser(PlayerInfo user)
	{
		this.user = user;
		userImage.SetSteamID(user.steamUser.Id);
		pilotName.text = user.pilotName;
		steamName.text = user.steamUser.Name;
		if (user.steamUser.IsMe)
		{
			otherUserObjects.SetActive(active: false);
		}
		else if ((ulong)user.steamUser.Id == (ulong)VTOLMPLobbyManager.instance.currentLobbyHost)
		{
			otherUserObjects.SetActive(active: true);
			hideForHost.SetActive(active: false);
		}
		else
		{
			otherUserObjects.SetActive(active: true);
			voteKicks.text = user.voteKicks.ToString();
			voteBans.text = user.voteBans.ToString();
		}
	}

	private void UpdateMuteButton()
	{
		bool flag = VTNetworkVoice.mutes.Contains(user.steamUser.Id.Value);
		muteButton.SetActive(!flag);
		unmuteButton.SetActive(flag);
	}

	public void Kick()
	{
		menu.KickButton(user);
	}

	public void Ban()
	{
		menu.BanButton(user);
	}

	public void ToggleMute()
	{
		if (VTNetworkVoice.mutes.Contains(user.steamUser.Id.Value))
		{
			VTNetworkVoice.mutes.Remove(user.steamUser.Id.Value);
		}
		else
		{
			VTNetworkVoice.mutes.Add(user.steamUser.Id.Value);
		}
		UpdateMuteButton();
	}
}

}