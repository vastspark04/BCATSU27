using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VTOLVR.Multiplayer{

public class MPUsersMenu : MonoBehaviour
{
	public ScrollRect scrollRect;

	public GameObject userTemplate;

	public VTConfirmationDialogue confirmDialogue;

	private List<GameObject> listObjs = new List<GameObject>();

	public UnityEvent OnMenuClosed;

	private void OnEnable()
	{
		userTemplate.SetActive(value: false);
		StartCoroutine(EnableRoutine());
	}

	private IEnumerator EnableRoutine()
	{
		while (!VTOLMPLobbyManager.instance)
		{
			yield return null;
		}
		UpdateList();
		VTOLMPLobbyManager.instance.OnConnectedPlayerListUpdated += UpdateList;
		PlayerInfo.OnKickBanVotesUpdated += UpdateList;
	}

	private void OnDisable()
	{
		if ((bool)VTOLMPLobbyManager.instance)
		{
			VTOLMPLobbyManager.instance.OnConnectedPlayerListUpdated -= UpdateList;
		}
		PlayerInfo.OnKickBanVotesUpdated -= UpdateList;
	}

	public void OpenMenu()
	{
		base.gameObject.SetActive(value: true);
	}

	private void UpdateList()
	{
		foreach (GameObject listObj in listObjs)
		{
			Object.Destroy(listObj);
		}
		listObjs.Clear();
		float num = ((RectTransform)userTemplate.transform).rect.height * userTemplate.transform.localScale.y;
		int num2 = 0;
		foreach (PlayerInfo connectedPlayer in VTOLMPLobbyManager.instance.connectedPlayers)
		{
			GameObject gameObject = Object.Instantiate(userTemplate, scrollRect.content);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-num2) * num, 0f);
			gameObject.SetActive(value: true);
			MPUsersMenuItem component = gameObject.GetComponent<MPUsersMenuItem>();
			component.menu = this;
			component.SetupForUser(connectedPlayer);
			listObjs.Add(gameObject);
			num2++;
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num2 * num);
	}

	public void CloseMenu()
	{
		base.gameObject.SetActive(value: false);
		OnMenuClosed?.Invoke();
	}

	public void KickButton(PlayerInfo user)
	{
		if (VTOLMPLobbyManager.isLobbyHost)
		{
			confirmDialogue.DisplayConfirmation("Kick", $"Kick user {user.pilotName} from the lobby? ({user.voteKicks} votes)", delegate
			{
				VTOLMPSceneManager.instance.KickMember(user.steamUser.Id);
			}, null);
			return;
		}
		float kickbanCooldownLeft = VTOLMPSceneManager.instance.kickbanCooldownLeft;
		if (kickbanCooldownLeft > 0f)
		{
			confirmDialogue.DisplayConfirmation("Please Wait", $"Please wait {kickbanCooldownLeft} seconds before voting again.", null, null);
			return;
		}
		confirmDialogue.DisplayConfirmation("Vote Kick", "Vote to kick " + user.pilotName + " from the lobby? This cannot be undone.", delegate
		{
			VTOLMPSceneManager.instance.VoteKick(user.steamUser.Id);
		}, null);
	}

	public void BanButton(PlayerInfo user)
	{
		if (VTOLMPLobbyManager.isLobbyHost)
		{
			confirmDialogue.DisplayConfirmation("Ban", $"Ban user {user.pilotName} from the lobby? ({user.voteBans} votes)", delegate
			{
				VTOLMPSceneManager.instance.BanMember(user.steamUser.Id);
			}, null);
			return;
		}
		float kickbanCooldownLeft = VTOLMPSceneManager.instance.kickbanCooldownLeft;
		if (kickbanCooldownLeft > 0f)
		{
			confirmDialogue.DisplayConfirmation("Please Wait", $"Please wait {kickbanCooldownLeft} seconds before voting again.", null, null);
			return;
		}
		confirmDialogue.DisplayConfirmation("Vote Ban", "Vote to ban " + user.pilotName + " from the lobby? This cannot be undone.", delegate
		{
			VTOLMPSceneManager.instance.VoteBan(user.steamUser.Id);
		}, null);
	}
}

}