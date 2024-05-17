using UnityEngine;
using VTOLVR.Multiplayer;

public class LandingZoneWaypointMission : MissionObjective
{
	public Waypoint wpt;

	public float radius;

	private FlightInfo playerFlightInfo;

	[HideInInspector]
	public bool incompleteSetup;

	private bool isMp;

	private float sqrRad;

	public override void OnBeginMission()
	{
		if (!VTOLMPUtils.IsMultiplayer())
		{
			playerFlightInfo = FlightSceneManager.instance.playerActor.GetComponent<FlightInfo>();
			if (!playerFlightInfo)
			{
				Debug.LogError("Landing zone mission " + objectiveName + " did not find the playerFlightInfo.");
			}
		}
		else
		{
			isMp = true;
		}
		sqrRad = radius * radius;
	}

	private void Update()
	{
		if (!base.started || base.objectiveFinished || (!playerFlightInfo && !isMp))
		{
			return;
		}
		if (incompleteSetup)
		{
			Debug.LogError("Landing mission was missing a waypoint.  Failing.");
			FailObjective();
			if (base.isPlayersMission)
			{
				EndMission.AddText(objectiveName + " is missing a waypoint.", red: true);
			}
			return;
		}
		if (isMp)
		{
			if (!VTOLMPLobbyManager.isLobbyHost)
			{
				return;
			}
			for (int i = 0; i < VTOLMPLobbyManager.instance.connectedPlayers.Count; i++)
			{
				PlayerInfo playerInfo = VTOLMPLobbyManager.instance.connectedPlayers[i];
				if (playerInfo.team != team || !playerInfo.vehicleActor || !playerInfo.vehicleActor.alive)
				{
					continue;
				}
				Vector3 vector = playerInfo.vehicleActor.position - wpt.worldPosition;
				vector.y = 0f;
				FlightInfo flightInfo = playerInfo.vehicleActor.flightInfo;
				if (flightInfo.isLanded && flightInfo.surfaceSpeed < 1f && vector.sqrMagnitude < sqrRad)
				{
					Debug.Log(playerInfo.pilotName + " has completed landing mission '" + objectiveName + "'!");
					CompleteObjective();
					if (base.isPlayersMission)
					{
						EndMission.AddText($"{objectiveName} {VTLStaticStrings.mission_completed}", red: false);
					}
				}
			}
			return;
		}
		Vector3 vector2 = FlightSceneManager.instance.playerActor.position - wpt.worldPosition;
		vector2.y = 0f;
		if (playerFlightInfo.isLanded && playerFlightInfo.surfaceSpeed < 1f && vector2.sqrMagnitude < sqrRad && (!QuicksaveManager.isQuickload || PlayerSpawn.qLoadPlayerComplete))
		{
			Debug.Log("LandingZoneWaypointMission complete!");
			CompleteObjective();
			if (base.isPlayersMission)
			{
				EndMission.AddText($"{objectiveName} {VTLStaticStrings.mission_completed}", red: false);
			}
		}
	}
}
