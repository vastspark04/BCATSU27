using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VTOLVR.Multiplayer{

public class EnterPlayerNotification : MonoBehaviour
{
	private enum NotifTypes
	{
		Join,
		Leave,
		VoteKick,
		VoteBan
	}

	public GameObject joinedTemplate;

	public GameObject leftTemplate;

	public GameObject clearButton;

	public AudioSource notificationAudio;

	public AudioClip joinedSound;

	public AudioClip leftSound;

	public AudioClip voteSound;

	public float notifRemainTime = 2f;

	private List<GameObject> notifObjects = new List<GameObject>();

	private void Awake()
	{
		joinedTemplate.SetActive(value: false);
		leftTemplate.SetActive(value: false);
		if ((bool)clearButton)
		{
			clearButton.SetActive(value: false);
		}
		if (!VTOLMPUtils.IsMultiplayer())
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void OnEnable()
	{
		VTOLMPSceneManager.instance.OnPlayerSelectedTeam += VTOLMPLobbyManager_OnNewPlayerJoined;
		VTOLMPLobbyManager.OnPlayerLeft += VTOLMPLobbyManager_OnPlayerLeft;
		VTOLMPLobbyManager.instance.OnVoteKick += Instance_OnVoteKick;
		VTOLMPLobbyManager.instance.OnVoteBan += Instance_OnVoteBan;
	}

	private void Instance_OnVoteBan(PlayerInfo target, PlayerInfo voter)
	{
		StartCoroutine(NotificationRoutine(NotifTypes.VoteBan, target, voter));
	}

	private void Instance_OnVoteKick(PlayerInfo target, PlayerInfo voter)
	{
		StartCoroutine(NotificationRoutine(NotifTypes.VoteKick, target, voter));
	}

	private void OnDisable()
	{
		if ((bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnPlayerSelectedTeam -= VTOLMPLobbyManager_OnNewPlayerJoined;
			VTOLMPLobbyManager.instance.OnVoteKick -= Instance_OnVoteKick;
			VTOLMPLobbyManager.instance.OnVoteBan -= Instance_OnVoteBan;
		}
		VTOLMPLobbyManager.OnNewPlayerJoined -= VTOLMPLobbyManager_OnNewPlayerJoined;
		VTOLMPLobbyManager.OnPlayerLeft -= VTOLMPLobbyManager_OnPlayerLeft;
	}

	private void VTOLMPLobbyManager_OnPlayerLeft(PlayerInfo obj)
	{
		StartCoroutine(NotificationRoutine(NotifTypes.Leave, obj));
	}

	private void VTOLMPLobbyManager_OnNewPlayerJoined(PlayerInfo obj)
	{
		StartCoroutine(NotificationRoutine(NotifTypes.Join, obj));
	}

	public void ClearButton()
	{
		foreach (GameObject notifObject in notifObjects)
		{
			Object.Destroy(notifObject.gameObject);
		}
		notifObjects.Clear();
		if ((bool)clearButton)
		{
			clearButton.SetActive(value: false);
		}
	}

	private IEnumerator NotificationRoutine(NotifTypes type, PlayerInfo player, PlayerInfo voter = null)
	{
		GameObject gameObject = ((type != NotifTypes.Leave) ? joinedTemplate : leftTemplate);
		GameObject obj = Object.Instantiate(gameObject, gameObject.transform.parent);
		EnterPlayerTemplate s = obj.GetComponent<EnterPlayerTemplate>();
		s.playerNameText.text = player.pilotName;
		if ((bool)s.playerImage)
		{
			Task<Texture2D> imgTask = VTOLMPLobbyManager.GetUserImage(player.steamUser.Id);
			while (!imgTask.IsCompleted)
			{
				yield return null;
			}
			s.playerImage.texture = imgTask.Result;
		}
		_ = Vector3.zero;
		obj.transform.localPosition = Vector3.zero;
		float y = ((RectTransform)obj.transform).rect.height * obj.transform.localScale.y;
		foreach (GameObject notifObject in notifObjects)
		{
			notifObject.transform.localPosition += new Vector3(0f, y, 0f);
		}
		obj.SetActive(value: true);
		notifObjects.Add(obj);
		if ((bool)notificationAudio)
		{
			switch (type)
			{
			case NotifTypes.Join:
				notificationAudio.PlayOneShot(joinedSound);
				break;
			case NotifTypes.Leave:
				notificationAudio.PlayOneShot(leftSound);
				break;
			default:
				notificationAudio.PlayOneShot(voteSound);
				break;
			}
		}
		if (type == NotifTypes.Join && (bool)s.infoText)
		{
			s.infoText.text = "has joined " + ((player.team == Teams.Allied) ? "Team A" : "Team B");
		}
		switch (type)
		{
		case NotifTypes.VoteBan:
			s.playerNameText.text = "Ban " + player.pilotName;
			if (voter != null)
			{
				s.infoText.text = $"{voter.pilotName} voted to ban {player.pilotName} ({player.voteBans})";
			}
			else
			{
				s.infoText.text = $"A player voted to ban {player.pilotName} ({player.voteBans})";
			}
			break;
		case NotifTypes.VoteKick:
			s.playerNameText.text = "Kick " + player.pilotName;
			if (voter != null)
			{
				s.infoText.text = $"{voter.pilotName} voted to kick {player.pilotName} ({player.voteKicks})";
			}
			else
			{
				s.infoText.text = $"A player voted to kick {player.pilotName} ({player.voteBans})";
			}
			break;
		}
		if ((bool)clearButton)
		{
			clearButton.SetActive(value: true);
		}
		float t = Time.time;
		while (Time.time - t < notifRemainTime)
		{
			yield return null;
		}
		if ((bool)obj)
		{
			notifObjects.Remove(obj);
			Object.Destroy(obj);
			if (notifObjects.Count == 0 && (bool)clearButton)
			{
				clearButton.SetActive(value: false);
			}
		}
	}
}

}