using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VTNetworking;
using VTOLVR.Multiplayer;

public class VTMapManager : MonoBehaviour
{
	public enum MapLaunchModes
	{
		Editor,
		Scenario,
		MapEditor
	}

	public static MapLaunchModes nextLaunchMode = MapLaunchModes.Scenario;

	public VTMap map;

	public List<AirportManager> airports = new List<AirportManager>();

	private List<AirportManager> origAirports;

	public GameObject scenarioEditorObject;

	public GameObject vtMapEditorObject;

	private const double eqMetersPerDegree = 111319.9;

	public Texture2D fallbackHeightmap;

	public float fallbackHeightmapTotalSize;

	public Vector2 fallbackHeightmapOffset;

	public bool mpScenarioStart;

	public static VTMapManager fetch { get; private set; }

	public bool scenarioReady { get; private set; }

	private void OnEnable()
	{
		fetch = this;
	}

	private void Awake()
	{
		fetch = this;
		scenarioReady = false;
		origAirports = new List<AirportManager>();
		foreach (AirportManager airport in airports)
		{
			origAirports.Add(airport);
		}
	}

	private void Start()
	{
		if (nextLaunchMode == MapLaunchModes.Scenario)
		{
			if ((bool)scenarioEditorObject)
			{
				UnityEngine.Object.Destroy(scenarioEditorObject);
			}
			if ((bool)vtMapEditorObject)
			{
				UnityEngine.Object.Destroy(vtMapEditorObject);
			}
			StartCoroutine(ScenarioStartRoutine());
		}
		else if (nextLaunchMode == MapLaunchModes.Editor)
		{
			if ((bool)vtMapEditorObject)
			{
				UnityEngine.Object.Destroy(vtMapEditorObject);
			}
			if ((bool)scenarioEditorObject)
			{
				if (XRSettings.enabled)
				{
					VRUtils.DisableVR();
					FlightSceneManager.instance.ReloadScene();
				}
				else
				{
					StartCoroutine(StartEditorRoutine());
				}
			}
		}
		else
		{
			if (nextLaunchMode != MapLaunchModes.MapEditor)
			{
				return;
			}
			if ((bool)scenarioEditorObject)
			{
				UnityEngine.Object.Destroy(scenarioEditorObject);
			}
			if ((bool)scenarioEditorObject)
			{
				if (XRSettings.enabled)
				{
					VRUtils.DisableVR();
					FlightSceneManager.instance.ReloadScene();
				}
				else
				{
					StartCoroutine(StartMapEditorRoutine());
				}
			}
		}
	}

	public void RestartCurrentScenario()
	{
		if (nextLaunchMode == MapLaunchModes.Scenario && VTScenario.current != null)
		{
			Bullet.DisableFiredBullets();
			Missile.DestroyAllFiredMissiles();
			Rocket.DestroyAllFiredRockets();
			UnitIconManager.instance.UnregisterAll();
			FlightLogger.ClearLog();
			MissionManager.instance.ClearMissionsForRestart();
			if ((bool)FlightSceneManager.instance.playerActor)
			{
				UnityEngine.Object.Destroy(FlightSceneManager.instance.playerActor.gameObject);
			}
			VTScenario.current.DestroyAllScenarioObjects();
			scenarioReady = false;
			StartCoroutine(ScenarioStartRoutine());
		}
	}

	private IEnumerator StartMapEditorRoutine()
	{
		while (!FlightSceneManager.instance || !FlightSceneManager.instance.SceneLoadFinished())
		{
			yield return null;
		}
		vtMapEditorObject.SetActive(value: true);
	}

	private IEnumerator StartEditorRoutine()
	{
		while (!FlightSceneManager.instance || !FlightSceneManager.instance.SceneLoadFinished())
		{
			yield return null;
		}
		scenarioEditorObject.SetActive(value: true);
	}

	private IEnumerator ScenarioStartRoutine()
	{
		VTScenarioInfo scenarioInfo = VTScenario.currentScenarioInfo;
		if (scenarioInfo == null)
		{
			yield break;
		}
		EndMission.Initialize();
		if (scenarioInfo.mpPlayerCount > 0)
		{
			StartCoroutine(MPScenarioStartRoutine());
			yield break;
		}
		CampaignScenario currentScenario = PilotSaveManager.currentScenario;
		string text = currentScenario.environmentName;
		if ((bool)EnvironmentManager.instance)
		{
			if (currentScenario.envIdx >= 0)
			{
				text = currentScenario.envOptions[currentScenario.envIdx].envName;
			}
			if (!string.IsNullOrEmpty(text))
			{
				EnvironmentManager.instance.currentEnvironment = text;
				EnvironmentManager.instance.SetCurrent();
			}
		}
		while (!FlightSceneManager.isFlightReady || !FlightSceneManager.instance.SceneLoadFinished())
		{
			yield return null;
		}
		yield return null;
		BGMManager.FadeOut();
		VTScenario scenario = (VTScenario.current = new VTScenario());
		scenario.LoadFromInfo(scenarioInfo);
		CommRadioManager.ShuffleVoiceProfiles();
		FloatingOrigin.instance.ShiftOrigin(scenario.units.GetPlayerSpawner().transform.position);
		yield return new WaitForFixedUpdate();
		yield return null;
		if (!QuicksaveManager.isQuickload)
		{
			Debug.Log("Setting mission start time (VTMapManager before *.BeginScenario())");
			FlightSceneManager.instance.SetMissionStartTimeNow();
		}
		scenario.globalValues.BeginScenario();
		scenario.staticObjects.BeginScenario();
		foreach (UnitSpawner value in scenario.units.units.Values)
		{
			value.BeginScenario();
		}
		foreach (UnitSpawner value2 in scenario.units.units.Values)
		{
			if (!(value2.prefabUnitSpawn is AIUnitSpawn) || ((AIUnitSpawn)value2.spawnedUnit).spawnOnStart)
			{
				value2.SpawnUnit();
			}
		}
		scenario.groups.BeginScenario();
		foreach (VTTimedEventGroup allGroup in scenario.timedEventGroups.GetAllGroups())
		{
			allGroup.BeginScenario();
		}
		foreach (VTObjective objective in scenario.objectives.GetObjectives(Teams.Allied))
		{
			objective.BeginScenario();
		}
		scenario.sequencedEvents.BeginScenario();
		scenario.bases.BeginScenario();
		scenario.triggerEvents.BeginScenario();
		WaypointManager.instance.rtbWaypoint = scenario.GetRTBWaypoint();
		WaypointManager.instance.fuelWaypoint = scenario.GetRefuelWaypoint();
		WaypointManager.instance.bullseye = scenario.waypoints.bullseyeTransform;
		scenarioReady = true;
		Debug.Log("VTMapManager scenarioReady!");
		MissionManager.instance.RestartMissions();
	}

	private IEnumerator MPScenarioStartRoutine()
	{
		VTScenarioInfo scenarioInfo = VTScenario.currentScenarioInfo;
		CampaignScenario currentScenario = PilotSaveManager.currentScenario;
		string text = currentScenario.environmentName;
		if ((bool)EnvironmentManager.instance)
		{
			if (currentScenario.envIdx >= 0)
			{
				text = currentScenario.envOptions[currentScenario.envIdx].envName;
			}
			if (!string.IsNullOrEmpty(text))
			{
				EnvironmentManager.instance.currentEnvironment = text;
				EnvironmentManager.instance.SetCurrent();
			}
		}
		while (!FlightSceneManager.isFlightReady || !FlightSceneManager.instance.SceneLoadFinished())
		{
			yield return null;
		}
		yield return null;
		BGMManager.FadeOut();
		VTScenario scenario = (VTScenario.current = new VTScenario());
		scenario.LoadFromInfo(scenarioInfo);
		CommRadioManager.ShuffleVoiceProfiles();
		while (VTNetworkManager.instance.connectionState != VTNetworkManager.ConnectionStates.Connected)
		{
			yield return null;
		}
		foreach (UnitSpawner value2 in scenario.units.units.Values)
		{
			if (value2.prefabUnitSpawn is MultiplayerSpawn)
			{
				value2.BeginScenario();
			}
		}
		Debug.Log("Waiting for alt-spawn seed.");
		while (VTOLMPSceneManager.instance.altSpawnSeed < 0)
		{
			yield return null;
		}
		Debug.Log($"- Got alt-spawn seed of {VTOLMPSceneManager.instance.altSpawnSeed}");
		foreach (UnitSpawner value3 in scenario.units.units.Values)
		{
			if (VTOLMPLobbyManager.isLobbyHost)
			{
				value3.BeginScenario();
			}
		}
		while (!mpScenarioStart)
		{
			yield return null;
		}
		if (VTOLMPLobbyManager.isLobbyHost)
		{
			FlightSceneManager.instance.SetMissionStartTimeNow();
			scenario.globalValues.BeginScenario();
			scenario.staticObjects.BeginScenario();
			foreach (KeyValuePair<int, UnitSpawner> unit in scenario.units.units)
			{
				Debug.Log($"MP Spawning {unit.Key}");
				UnitSpawner value = unit.Value;
				if (!(value.prefabUnitSpawn is AIUnitSpawn) || ((AIUnitSpawn)value.spawnedUnit).spawnOnStart)
				{
					value.SpawnUnit();
				}
			}
			scenario.groups.BeginScenario();
			foreach (VTTimedEventGroup allGroup in scenario.timedEventGroups.GetAllGroups())
			{
				allGroup.BeginScenario();
			}
			foreach (VTObjective objective in scenario.objectives.GetObjectives(Teams.Allied))
			{
				objective.BeginScenario();
			}
			foreach (VTObjective objective2 in scenario.objectives.GetObjectives(Teams.Enemy))
			{
				objective2.BeginScenario();
			}
			scenario.sequencedEvents.BeginScenario();
			scenario.bases.BeginScenario();
			scenario.triggerEvents.BeginScenario();
		}
		else
		{
			bool prespawnReady = false;
			while (!prespawnReady)
			{
				prespawnReady = true;
				foreach (UnitSpawner value4 in scenario.units.units.Values)
				{
					if (!(value4.prefabUnitSpawn is MultiplayerSpawn) && !value4.isMPReady)
					{
						prespawnReady = false;
						break;
					}
				}
				yield return null;
			}
			scenario.globalValues.BeginScenario();
			scenario.staticObjects.BeginScenario();
			foreach (VTTimedEventGroup allGroup2 in scenario.timedEventGroups.GetAllGroups())
			{
				allGroup2.BeginScenario();
			}
			foreach (VTObjective objective3 in scenario.objectives.GetObjectives(Teams.Allied))
			{
				objective3.BeginScenario();
			}
			foreach (VTObjective objective4 in scenario.objectives.GetObjectives(Teams.Enemy))
			{
				objective4.BeginScenario();
			}
			foreach (UnitSpawner value5 in scenario.units.units.Values)
			{
				if (value5.prefabUnitSpawn is MultiplayerSpawn)
				{
					value5.SpawnUnit();
				}
			}
			scenario.groups.BeginScenario();
			scenario.sequencedEvents.BeginScenario();
			scenario.bases.BeginScenario();
			scenario.triggerEvents.BeginScenario();
		}
		Debug.Log("Setting team waypoints in MPScenarioStartRoutine");
		WaypointManager.instance.rtbWaypoint = scenario.GetRTBWaypoint();
		WaypointManager.instance.fuelWaypoint = scenario.GetRefuelWaypoint();
		WaypointManager.instance.bullseye = scenario.waypoints.bullseyeTransform;
		if (VTOLMPLobbyManager.localPlayerInfo.team == Teams.Enemy)
		{
			WaypointManager.instance.rtbWaypoint = scenario.GetRTBWaypoint(Teams.Enemy);
			WaypointManager.instance.fuelWaypoint = scenario.GetRefuelWaypoint(Teams.Enemy);
			WaypointManager.instance.bullseye = scenario.waypoints.bullseyeBTransform;
		}
		scenarioReady = true;
		Debug.Log("VTMapManager scenarioReady!");
		MissionManager.instance.RestartMissions();
		VTOLMPSceneManager.instance.ReportScenarioStarted();
	}

	public static Vector3 GlobalToWorldPoint(Vector3D globalPoint)
	{
		return (globalPoint - FloatingOrigin.accumOffset).toVector3;
	}

	public static Vector3D WorldToGlobalPoint(Vector3 worldPoint)
	{
		return new Vector3D(worldPoint) + FloatingOrigin.accumOffset;
	}

	public Vector3D WorldPositionToGPSCoords(Vector3 worldPoint)
	{
		Vector3D vector3D = WorldToGlobalPoint(worldPoint);
		double z = worldPoint.y - WaterPhysics.instance.height;
		double num = vector3D.z / 111319.9;
		double num2 = Math.Abs(Math.Cos(num * 0.01745329238474369) * 111319.9);
		double num3 = 0.0;
		if (num2 > 0.0)
		{
			num3 = vector3D.x / num2;
		}
		double num4 = num3;
		if ((bool)map)
		{
			num += (double)map.mapLatitude;
			num4 += (double)map.mapLongitude;
		}
		return new Vector3D(num, num4, z);
	}

	public Vector3 GPSCoordsToWorldPoint(Vector3D gpsCoords)
	{
		double num = gpsCoords.x - (double)map.mapLatitude;
		num *= 111319.9;
		double num2 = Math.Abs(Math.Cos(gpsCoords.x * 0.01745329238474369) * 111319.9);
		Vector3 result = GlobalToWorldPoint(new Vector3D((gpsCoords.y - (double)map.mapLongitude) * num2, 0.0, num));
		result.y = (float)gpsCoords.z + WaterPhysics.instance.height;
		return result;
	}

	public static string FormattedCoordinate(double coord)
	{
		double value = (double)Math.Sign(coord) * Math.Floor(Math.Abs(coord));
		return string.Concat(str2: (100.0 * (Math.Abs(coord) - Math.Abs(value))).ToString("0.000"), str0: value.ToString("0"), str1: " ");
	}

	public static string FormattedCoordsMinSec(double coord)
	{
		int num = (int)Math.Floor(coord);
		double num2 = (coord - (double)num) * 60.0;
		int num3 = (int)Math.Floor(num2);
		return string.Format("{0}°{1}'{2}\"", num, num3, ((num2 - (double)num3) * 60.0).ToString("0.000"));
	}

	public static void GPSToDegreesMinutesSeconds(double coord, out int latDegrees, out int latMinutes, out int latSec)
	{
		latDegrees = (int)Math.Floor(coord);
		double num = coord - (double)latDegrees;
		num *= 60.0;
		latMinutes = (int)Math.Floor(num);
		num -= (double)latMinutes;
		latSec = Mathf.RoundToInt((float)num * 60f);
	}

	public static string FormattedGeoPos(Vector3D geoPos, bool altitude)
	{
		string empty = string.Empty;
		string text = FormattedCoordinate(geoPos.x);
		empty = empty + "N:" + text;
		string text2 = FormattedCoordinate(geoPos.y);
		empty = empty + " E:" + text2;
		if (altitude)
		{
			empty = empty + " ASL:" + geoPos.z.ToString("0.000");
		}
		return empty;
	}

	public static bool IsPositionOverCityStreet(Vector3 worldPos)
	{
		if (!VTMapGenerator.fetch)
		{
			return false;
		}
		float num = 20f;
		float num2 = 153.6f;
		float num3 = 2f * num2;
		float num4 = 0.6f;
		Vector3D vector3D = WorldToGlobalPoint(worldPos);
		float num5 = Mathf.Repeat((float)vector3D.x, num3);
		float num6 = Mathf.Repeat((float)vector3D.z, num3);
		float num7 = 0.01f;
		if ((num5 < num + num4 && num5 > num4) || (num6 > num3 - num - num4 && num6 < num3 - num4))
		{
			int num8 = Mathf.FloorToInt((float)vector3D.x / num2);
			int num9 = Mathf.FloorToInt((float)vector3D.z / num2);
			BDTexture hmBdt = VTMapGenerator.fetch.hmBdt;
			if (num8 >= 0 && num8 < hmBdt.width - 1 && num9 >= 0 && num9 < hmBdt.height - 1 && hmBdt.GetPixel(num8, num9).g > num7 && hmBdt.GetPixel(num8 + 1, num9 + 1).g > num7 && hmBdt.GetPixel(num8, num9 + 1).g > num7)
			{
				return hmBdt.GetPixel(num8 + 1, num9).g > num7;
			}
		}
		return false;
	}
}
