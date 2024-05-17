using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace VTOLVR.Multiplayer{

public class MPInviteReceivedNotification : MonoBehaviour
{
	public GameObject template;

	public GameObject clearButton;

	public AudioSource notificationAudio;

	public AudioClip notifSound;

	public float notifRemainTime = 2f;

	private VRPointInteractableCanvas vpic;

	private List<GameObject> notifObjects = new List<GameObject>();

	private void Awake()
	{
		template.SetActive(value: false);
		if ((bool)clearButton)
		{
			clearButton.SetActive(value: false);
		}
	}

	private void OnEnable()
	{
		SteamMatchmaking.OnLobbyInvite += SteamMatchmaking_OnLobbyInvite;
	}

	private void SteamMatchmaking_OnLobbyInvite(Friend arg1, Lobby arg2)
	{
		if (PilotSaveManager.current != null)
		{
			Debug.Log("OnLobbyInvite()");
			StartCoroutine(NotificationRoutine(arg1, arg2));
		}
	}

	private void OnDisable()
	{
		SteamMatchmaking.OnLobbyInvite -= SteamMatchmaking_OnLobbyInvite;
	}

	public void ClearButton()
	{
		foreach (GameObject notifObject in notifObjects)
		{
			UnityEngine.Object.Destroy(notifObject.gameObject);
		}
		notifObjects.Clear();
		if ((bool)clearButton)
		{
			clearButton.SetActive(value: false);
		}
	}

	private IEnumerator ImgRoutine(MPInviteTemplate s, Friend friend)
	{
		Task<Texture2D> imgTask = VTOLMPLobbyManager.GetUserImage(friend.Id);
		while ((bool)s && !imgTask.IsCompleted)
		{
			yield return null;
		}
		if ((bool)s)
		{
			s.playerImage.texture = imgTask.Result;
		}
	}

	private IEnumerator NotificationRoutine(Friend friend, Lobby lobby)
	{
		GameObject obj = UnityEngine.Object.Instantiate(template, template.transform.parent);
		MPInviteTemplate component = obj.GetComponent<MPInviteTemplate>();
		component.lobby = lobby;
		component.friend = friend;
		component.playerNameText.text = friend.Name;
		component.onAccept = (Action<Lobby, Friend>)Delegate.Combine(component.onAccept, new Action<Lobby, Friend>(AcceptedInvite));
		if ((bool)component.playerImage)
		{
			StartCoroutine(ImgRoutine(component, friend));
		}
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
			notificationAudio.PlayOneShot(notifSound);
		}
		if ((bool)clearButton)
		{
			clearButton.SetActive(value: true);
		}
		if (vpic == null)
		{
			vpic = GetComponentInParent<VRPointInteractableCanvas>();
		}
		if ((bool)vpic)
		{
			vpic.RefreshInteractables();
		}
		float t = Time.time;
		while (Time.time - t < notifRemainTime)
		{
			yield return null;
		}
		if ((bool)obj)
		{
			notifObjects.Remove(obj);
			UnityEngine.Object.Destroy(obj);
			if (notifObjects.Count == 0 && (bool)clearButton)
			{
				clearButton.SetActive(value: false);
			}
		}
	}

	private void AcceptedInvite(Lobby lobby, Friend friend)
	{
		ClearButton();
		VTMPMainMenu.FriendLobbyJoinRequested(lobby, friend.Id);
	}
}

}