using System.Collections;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class PlayerNameAndPing : MonoBehaviour
{
	public Text nameText;

	public SteamId userId;

	public Text pingText;

	public Text killsText;

	public Text assistsText;

	public Text deathsText;

	public VTNetworkVoiceReceiveIndicator voiceIndicator;

	private bool statListened;

	private void OnEnable()
	{
		StartCoroutine(PingRoutine());
	}

	private IEnumerator PingRoutine()
	{
		while ((ulong)userId == 0L && base.enabled)
		{
			yield return null;
		}
		if ((bool)voiceIndicator)
		{
			voiceIndicator.id = userId;
		}
		if (!statListened && ((bool)killsText || (bool)assistsText || (bool)deathsText))
		{
			VTOLMPSceneManager.instance.OnStatsUpdated += Instance_OnStatsUpdated;
			Instance_OnStatsUpdated(VTOLMPSceneManager.instance.GetPlayerStats(VTOLMPLobbyManager.GetPlayer(userId)));
			statListened = true;
		}
		WaitForSeconds wait = new WaitForSeconds(1f);
		while (base.enabled)
		{
			pingText.text = VTNetworkManager.GetPing(userId).ToString();
			yield return wait;
		}
	}

	private void OnDisable()
	{
		if (statListened && (bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnStatsUpdated -= Instance_OnStatsUpdated;
			statListened = false;
		}
	}

	private void Instance_OnStatsUpdated(VTOLMPSceneManager.PlayerStats stat)
	{
		if ((ulong)stat.player.steamUser.Id == (ulong)userId)
		{
			if ((bool)killsText)
			{
				killsText.text = stat.kills.ToString();
			}
			if ((bool)assistsText)
			{
				assistsText.text = stat.assists.ToString();
			}
			if ((bool)deathsText)
			{
				deathsText.text = stat.deaths.ToString();
			}
		}
	}
}

}