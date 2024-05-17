using System.Collections;
using UnityEngine;
using VTOLVR.Multiplayer;

public class PlayerFlightLogger : MonoBehaviour
{
	private Actor actor;

	private FlightInfo flightInfo;

	private bool recording;

	private bool isLanded = true;

	private float landedChangeTime;

	private void Start()
	{
		actor = GetComponent<Actor>();
		flightInfo = GetComponent<FlightInfo>();
	}

	private void OnEnable()
	{
		StartCoroutine(StartupRoutine());
		if (VTOLMPUtils.IsMultiplayer())
		{
			VTOLMPLobbyManager.OnLogMessage += FlightLogger.Log;
		}
	}

	private void OnDisable()
	{
		VTOLMPLobbyManager.OnLogMessage -= FlightLogger.Log;
	}

	private IEnumerator StartupRoutine()
	{
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		yield return new WaitForSeconds(3f);
		if ((bool)flightInfo)
		{
			isLanded = flightInfo.isLanded;
			recording = true;
		}
	}

	private void Update()
	{
		if (recording && flightInfo.isLanded != isLanded && Time.time - landedChangeTime > 3f)
		{
			isLanded = flightInfo.isLanded;
			landedChangeTime = Time.time;
			if (isLanded)
			{
				FlightLogger.Log($"{actor.actorName} landed at {actor.location}.");
			}
			else
			{
				FlightLogger.Log($"{actor.actorName} took off from {actor.location}.");
			}
		}
	}
}
