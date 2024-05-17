using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VTOLVR.Multiplayer{

public class MPInviteFriendMenu : MonoBehaviour
{
	public ScrollRect scrollRect;

	public GameObject template;

	public UnityEvent OnClose;

	private List<GameObject> displayObjs = new List<GameObject>();

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		foreach (GameObject displayObj in displayObjs)
		{
			Object.Destroy(displayObj);
		}
		displayObjs.Clear();
		float num = ((RectTransform)template.transform).rect.height * template.transform.localScale.y;
		template.SetActive(value: false);
		int num2 = 0;
		foreach (Friend friend in SteamFriends.GetFriends())
		{
			if (friend.IsOnline && friend.IsPlayingThisGame)
			{
				GameObject gameObject = Object.Instantiate(template, scrollRect.content);
				gameObject.transform.localPosition = new Vector3(0f, (float)(-num2) * num, 0f);
				gameObject.SetActive(value: true);
				MPInviteFriendTemplate component = gameObject.GetComponent<MPInviteFriendTemplate>();
				component.friend = friend;
				component.l = VTOLMPLobbyManager.currentLobby;
				component.playerNameText.text = friend.Name;
				displayObjs.Add(gameObject);
				StartCoroutine(ImageRtn(component, friend));
				num2++;
			}
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num2 * num);
		scrollRect.ClampVertical();
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		OnClose?.Invoke();
	}

	private IEnumerator ImageRtn(MPInviteFriendTemplate s, Friend friend)
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
}

}