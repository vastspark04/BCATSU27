using Steamworks;
using Steamworks.Data;
using UnityEngine;
using VTOLVR.Multiplayer;

public class BDSteamClient : MonoBehaviour
{
	private static BDSteamClient instance;

	private bool isSingleton;

	public static ulong mySteamID;

	public static uint APP_ID => 667970u;

	private void Awake()
	{
		base.transform.parent = null;
		Object.DontDestroyOnLoad(base.gameObject);
	}

	private void OnEnable()
	{
		if ((bool)instance && !isSingleton)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		instance = this;
		isSingleton = true;
		Debug.Log("Initializing Steam Client");
		SteamClient.Init(APP_ID);
		mySteamID = SteamClient.SteamId;
		SteamFriends.OnGameLobbyJoinRequested += SteamFriends_OnGameLobbyJoinRequested;
	}

	private void SteamFriends_OnGameLobbyJoinRequested(Lobby arg1, SteamId arg2)
	{
		Debug.Log($"OnGameLobbyJoinRequested({arg1.Id}, {arg2.Value})");
		VTMPMainMenu.FriendLobbyJoinRequested(arg1, arg2);
	}

	private void OnDisable()
	{
		if (isSingleton)
		{
			Debug.Log("SteamClient Shutdown");
			SteamClient.Shutdown();
		}
	}
}
