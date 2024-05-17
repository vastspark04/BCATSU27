using UnityEngine;

public class LandingZoneMission : MissionObjective
{
	public string locationName;

	private FlightInfo playerFlightInfo;

	[HideInInspector]
	public bool incompleteSetup;

	public override void OnBeginMission()
	{
		playerFlightInfo = FlightSceneManager.instance.playerActor.GetComponent<FlightInfo>();
		if (!playerFlightInfo)
		{
			Debug.LogError("Landing zone mission " + objectiveName + " did not find the playerFlightInfo.");
		}
	}

	private void Update()
	{
		if (!base.started || base.objectiveFinished || !playerFlightInfo)
		{
			return;
		}
		if (incompleteSetup)
		{
			Debug.LogError("Landing mission was missing a waypoint.  Failing.");
			FailObjective();
			return;
		}
		Actor playerActor = FlightSceneManager.instance.playerActor;
		if (playerFlightInfo.isLanded && playerFlightInfo.surfaceSpeed < 1f && playerActor.location == locationName)
		{
			Debug.Log("LandingZoneMission complete!");
			CompleteObjective();
			if (base.isPlayersMission)
			{
				EndMission.AddText($"{objectiveName} {VTLStaticStrings.mission_completed}", red: false);
			}
		}
	}
}
