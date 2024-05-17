using Steamworks;
using UnityEngine;

public class WebLinkButton : MonoBehaviour
{
	public string url;

	public void Open()
	{
		Application.OpenURL(url);
		if (SteamClient.IsValid)
		{
			SteamFriends.OpenWebOverlay(url);
		}
	}
}
