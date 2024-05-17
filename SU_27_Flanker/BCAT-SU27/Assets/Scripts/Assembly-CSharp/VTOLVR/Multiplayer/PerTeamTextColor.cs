using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class PerTeamTextColor : MonoBehaviour
{
	public Actor actor;

	public VTNetEntity netEnt;

	public Color friendlyColor;

	public Color hostileColor;

	public Text text;

	public PlayerNameText playerNameText;

	private bool isMP;

	private void Awake()
	{
		if (!netEnt)
		{
			netEnt = GetComponentInParent<VTNetEntity>();
		}
		if (!actor)
		{
			actor = GetComponentInParent<Actor>();
		}
		isMP = VTOLMPUtils.IsMultiplayer();
		UpdateColor();
		if ((bool)actor)
		{
			actor.OnSetTeam += Actor_OnSetTeam;
		}
		if (isMP)
		{
			VTOLMPSceneManager.instance.OnPlayerSelectedTeam += Instance_OnPlayerSelectedTeam;
		}
	}

	private void Actor_OnSetTeam(Teams obj)
	{
		UpdateColor();
	}

	private IEnumerator Start()
	{
		if (!netEnt)
		{
			yield break;
		}
		while (netEnt.ownerID == 0L)
		{
			yield return null;
		}
		if (!actor)
		{
			PlayerInfo player;
			while ((player = VTOLMPLobbyManager.GetPlayer(netEnt.ownerID)) == null || !player.chosenTeam)
			{
				yield return null;
			}
		}
		UpdateColor();
	}

	private void OnDestroy()
	{
		if ((bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnPlayerSelectedTeam -= Instance_OnPlayerSelectedTeam;
			if ((bool)actor)
			{
				actor.OnSetTeam -= Actor_OnSetTeam;
			}
		}
	}

	private void Instance_OnPlayerSelectedTeam(PlayerInfo obj)
	{
		UpdateColor();
	}

	private void UpdateColor()
	{
		if (!text)
		{
			return;
		}
		Color color;
		if (isMP)
		{
			Teams teams = Teams.Allied;
			PlayerInfo player;
			if ((bool)netEnt && netEnt.ownerID != 0 && (player = VTOLMPLobbyManager.GetPlayer(netEnt.ownerID)) != null)
			{
				teams = player.team;
			}
			else if ((bool)actor)
			{
				teams = actor.team;
			}
			color = ((teams == VTOLMPLobbyManager.localPlayerInfo.team) ? friendlyColor : hostileColor);
		}
		else
		{
			color = ((actor.team == Teams.Allied) ? friendlyColor : hostileColor);
		}
		text.color = color;
		if ((bool)playerNameText)
		{
			playerNameText.RefreshRTName();
		}
	}
}

}