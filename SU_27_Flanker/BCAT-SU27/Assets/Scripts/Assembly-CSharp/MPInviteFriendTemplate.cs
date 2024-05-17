using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

public class MPInviteFriendTemplate : MonoBehaviour
{
	public RawImage playerImage;

	public Text playerNameText;

	public GameObject inviteButton;

	public Friend friend;

	public Lobby l;

	public void InviteButton()
	{
		l.InviteFriend(friend.Id);
		inviteButton.SetActive(value: false);
	}
}
