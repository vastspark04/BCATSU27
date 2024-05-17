using System;
using System.Collections;
using System.Collections.Generic;
using OC;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using VTOLVR.Multiplayer;

public class FlightSceneManager : MonoBehaviour
{
	public class FlightReadyContingent
	{
		public bool ready;
	}

	public class FlightSceneLoadItem
	{
		public bool done;

		public float currentValue;

		public float maxValue = 1f;
	}

	private Actor _pActor;

	private List<FlightReadyContingent> flightReadyContingents = new List<FlightReadyContingent>();

	private float missionStartTime;

	private List<FlightSceneLoadItem> loadItems = new List<FlightSceneLoadItem>();

	public static FlightSceneManager instance { get; private set; }

	public Actor playerActor
	{
		get
		{
			return _pActor;
		}
		set
		{
			_pActor = value;
			if (_pActor != null)
			{
				playerVehicleMaster = _pActor.GetComponent<VehicleMaster>();
			}
			else
			{
				playerVehicleMaster = null;
			}
		}
	}

	public VehicleMaster playerVehicleMaster { get; private set; }

	public bool switchingScene { get; private set; }

	public static bool isFlightReady { get; private set; }

	public bool playerHasEjected { get; private set; }

	public Transform particleCustomSimSpace { get; private set; }

	public float missionElapsedTime
	{
		get
		{
			if (isFlightReady)
			{
				return Time.time - missionStartTime;
			}
			return 0f;
		}
	}

	public event UnityAction OnExitScene;

	public void ReportHasEjected()
	{
		playerHasEjected = true;
	}

	private void Awake()
	{
		instance = this;
		VTLStaticStrings.ApplyLocalization();
		MeshCombiner2.DestroyAllCombinedMeshes();
		GameSettings.EnsureSettings();
		isFlightReady = false;
		OnExitScene += FlightSceneManager_OnExitScene;
		particleCustomSimSpace = new GameObject("ParticleCustomSimulationSpace").transform;
	}

	private void FlightSceneManager_OnExitScene()
	{
		playerHasEjected = false;
		BGMManager.ReleaseBGMCommDucker();
	}

	private void Start()
	{
		StartCoroutine(FlightReadyRoutine());
		StartCoroutine(LoadFallbackRoutine());
		if ((bool)QuicksaveManager.instance)
		{
			QuicksaveManager.instance.OnQuicksave += OnQuicksave;
			QuicksaveManager.instance.OnQuickload += OnQuickload;
		}
		if (VTResources.useOverCloud)
		{
			OverCloud.ResetOrigin();
			if ((bool)WaterPhysics.instance)
			{
				OverCloud.MoveOrigin(new Vector3(0f, 0f - WaterPhysics.instance.height, 0f));
			}
			else
			{
				OverCloud.MoveOrigin(Vector3.zero);
			}
			if ((bool)FloatingOrigin.instance)
			{
				FloatingOrigin.instance.OnOriginShift += OverCloudShift;
			}
		}
		if ((bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.AddTransform(particleCustomSimSpace);
		}
	}

	private void Update()
	{
		if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Scenario)
		{
			if (!VTOLMPUtils.IsMultiplayer() && Input.GetKeyDown(KeyCode.End))
			{
				ReloadScene();
			}
			else if (Input.GetKeyDown(KeyCode.Delete))
			{
				ReturnToBriefingOrExitScene();
			}
		}
	}

	private void OverCloudShift(Vector3 oShift)
	{
		OverCloud.MoveOrigin(-oShift);
	}

	private void OnQuicksave(ConfigNode configNode)
	{
		configNode.SetValue("missionElapsedTime", missionElapsedTime);
	}

	private void OnQuickload(ConfigNode configNode)
	{
		try
		{
			missionStartTime = Time.time - configNode.GetValue<float>("missionElapsedTime");
			Debug.Log("Setting mission start time (Quickload)");
		}
		catch (Exception ex)
		{
			Debug.LogError("Error when quickloading FlightSceneManager\n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
	}

	private IEnumerator LoadFallbackRoutine()
	{
		for (int i = 0; i < 5; i++)
		{
			yield return null;
		}
		if (loadItems.Count == 0)
		{
			FlightSceneLoadItem flightSceneLoadItem = AddLoadItem();
			flightSceneLoadItem.currentValue = flightSceneLoadItem.maxValue;
			flightSceneLoadItem.done = true;
		}
	}

	private void OnDestroy()
	{
		isFlightReady = false;
		if ((bool)FloatingOrigin.instance && (bool)particleCustomSimSpace)
		{
			FloatingOrigin.instance.RemoveTransform(particleCustomSimSpace);
		}
	}

	private IEnumerator FlightReadyRoutine()
	{
		yield return null;
		while (!isFlightReady)
		{
			isFlightReady = true;
			foreach (FlightReadyContingent flightReadyContingent in flightReadyContingents)
			{
				if (!flightReadyContingent.ready)
				{
					isFlightReady = false;
					break;
				}
			}
			yield return null;
		}
		if (VTMapManager.nextLaunchMode != VTMapManager.MapLaunchModes.Scenario)
		{
			yield break;
		}
		if ((bool)VTMapManager.fetch)
		{
			while (!VTMapManager.fetch.scenarioReady)
			{
				yield return null;
			}
		}
		else
		{
			Debug.Log("Setting mission start time (FlightReadyRoutine)");
			missionStartTime = Time.time;
		}
	}

	public void SetMissionStartTimeNow()
	{
		missionStartTime = Time.time;
	}

	[ContextMenu("Override Set Ready")]
	public void OverrideSetReady()
	{
		isFlightReady = true;
	}

	public void ReloadScene()
	{
		if (switchingScene)
		{
			return;
		}
		QuicksaveManager.isQuickload = false;
		switchingScene = true;
		if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Scenario)
		{
			StartCoroutine(InstantScenarioRestartRoutine());
			return;
		}
		BGMManager.FadeOut();
		InvokeExitScene();
		if (PilotSaveManager.current != null)
		{
			PilotSaveManager.SavePilotsToFile();
		}
		StartCoroutine(ReloadSceneRoutine());
	}

	private IEnumerator InstantScenarioRestartRoutine()
	{
		Debug.Log("Restarting scenario.");
		BGMManager.FadeOut();
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black, 2f);
		yield return new WaitForSeconds(2.5f);
		InvokeExitScene();
		if (PilotSaveManager.current != null)
		{
			PilotSaveManager.SavePilotsToFile();
		}
		VTMapManager.fetch.RestartCurrentScenario();
		Debug.Log("Combined mesh count: " + MeshCombiner2.GetCombinedMeshesCount());
		ControllerEventHandler.UnpauseEvents();
		switchingScene = false;
		GC.Collect();
		while (!VTMapManager.fetch.scenarioReady)
		{
			yield return null;
		}
		if (!QuicksaveManager.isQuickload)
		{
			Debug.Log("Setting mission start time (InstantScenarioRestartRoutine)");
			missionStartTime = Time.time;
		}
	}

	private IEnumerator ReloadSceneRoutine()
	{
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black, 0.45f);
		yield return new WaitForSecondsRealtime(0.5f);
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		ControllerEventHandler.UnpauseEvents();
	}

	public void ExitScene()
	{
		if (!switchingScene)
		{
			switchingScene = true;
			StartCoroutine(ReturnToMainRoutine());
		}
	}

	public void ReturnToBriefingOrExitScene()
	{
		if (VTOLMPUtils.IsMultiplayer() && playerActor != null)
		{
			StartCoroutine(ReturnToBriefingRoutine());
		}
		else
		{
			ExitScene();
		}
	}

	private IEnumerator ReturnToBriefingRoutine()
	{
		Debug.Log("Returning to briefing room.");
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black);
		yield return new WaitForSeconds(1.5f);
		GameObject vehicleObject = VTOLMPLobbyManager.localPlayerInfo.vehicleObject;
		if ((bool)vehicleObject)
		{
			vehicleObject.GetComponent<PlayerVehicleSetup>().SavePersistentData();
			PilotSaveManager.SavePilotsToFile();
		}
		VTOLMPSceneManager.instance.ReturnToBriefingRoom();
		ControllerEventHandler.UnpauseEvents();
	}

	public void InvokeExitScene()
	{
		this.OnExitScene?.Invoke();
	}

	private IEnumerator ReturnToMainRoutine()
	{
		BGMManager.FadeOut();
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black, 2f);
		yield return new WaitForSeconds(2.5f);
		InvokeExitScene();
		if (PilotSaveManager.current != null)
		{
			PilotSaveManager.SavePilotsToFile();
		}
		if (VTOLMPUtils.IsMultiplayer())
		{
			VTOLMPSceneManager.instance.DisconnectToMainMenu();
		}
		else if (VTScenarioEditor.returnToEditor)
		{
			VRUtils.DisableVR();
			VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Editor;
			VTResources.LaunchMapForScenario(VTScenario.currentScenarioInfo, skipLoading: false);
		}
		else if (PilotSaveManager.current != null)
		{
			SceneManager.LoadScene("ReadyRoom");
		}
		else
		{
			SceneManager.LoadScene(0);
		}
		ControllerEventHandler.UnpauseEvents();
		GC.Collect();
	}

	public void SetPlayerFormationType(int t)
	{
		playerActor.GetComponent<AirFormationLeader>().SetFormationType(t);
	}

	public FlightSceneLoadItem AddLoadItem()
	{
		FlightSceneLoadItem flightSceneLoadItem = new FlightSceneLoadItem();
		loadItems.Add(flightSceneLoadItem);
		return flightSceneLoadItem;
	}

	public bool SceneLoadFinished()
	{
		if (loadItems.Count == 0)
		{
			return false;
		}
		foreach (FlightSceneLoadItem loadItem in loadItems)
		{
			if (!loadItem.done)
			{
				return false;
			}
		}
		return true;
	}

	public float SceneLoadPercent()
	{
		if (loadItems.Count == 0)
		{
			return 0f;
		}
		float num = 0f;
		float num2 = 0f;
		bool flag = true;
		foreach (FlightSceneLoadItem loadItem in loadItems)
		{
			if (!loadItem.done)
			{
				flag = false;
			}
			num += loadItem.currentValue;
			num2 += loadItem.maxValue;
		}
		if (flag)
		{
			return 1f;
		}
		if (num2 <= 0f)
		{
			return 0f;
		}
		return num / num2 * 0.9f;
	}

	public void AddReadyContingent(FlightReadyContingent c)
	{
		flightReadyContingents.Add(c);
	}
}
