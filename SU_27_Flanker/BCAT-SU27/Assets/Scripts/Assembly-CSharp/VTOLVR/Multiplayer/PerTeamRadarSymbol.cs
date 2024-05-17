using System.Collections;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class PerTeamRadarSymbol : MonoBehaviour
{
	public Radar radar;

	public VTNetEntity netEnt;

	public string teamASymbol;

	public string teamBSymbol;

	private IEnumerator Start()
	{
		while (netEnt.ownerID == 0L)
		{
			yield return null;
		}
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(netEnt.ownerID);
		if (player != null)
		{
			radar.radarSymbol = ((player.team == Teams.Allied) ? teamASymbol : teamBSymbol);
		}
	}
}

}