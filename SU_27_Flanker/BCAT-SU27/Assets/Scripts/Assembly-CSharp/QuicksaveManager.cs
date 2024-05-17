using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.CrashReportHandler;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class QuicksaveManager : MonoBehaviour
{
	public enum QSModes
	{
		Anywhere,
		Rearm_Only,
		None
	}

	public delegate void QuicksaveDelegate(ConfigNode configNode);

	private static ConfigNode quicksaveNode;

	private List<FlightLogger.LogEntry> flightLog = new List<FlightLogger.LogEntry>();

	private static bool _isQl;

	private static bool _hasQuickloadedMissiles;

	public static QuicksaveManager instance { get; private set; }

	public static float quicksaveMET { get; private set; }

	public static bool quickloadAvailable => quicksaveNode != null;

	public static bool quickloading { get; private set; }

	public int savesUsed { get; private set; }

	public static bool isQuickload
	{
		get
		{
			return _isQl;
		}
		set
		{
			_isQl = value;
			CrashReportHandler.SetUserMetadata("isQuickload", value.ToString());
		}
	}

	public bool indicatedError { get; private set; }

	public bool canQuickload => quicksaveNode != null;

	public static bool hasQuickloadedMissiles => _hasQuickloadedMissiles;

	public event QuicksaveDelegate OnQuicksave;

	public event QuicksaveDelegate OnQuickloadEarly;

	public event QuicksaveDelegate OnQuickload;

	public event QuicksaveDelegate OnQuickloadLate;

	public event QuicksaveDelegate OnQuickloadedMissiles;

	public event UnityAction OnIndicatedError;

	public void IndicateError()
	{
		if (this.OnIndicatedError != null)
		{
			this.OnIndicatedError();
		}
	}

	public bool Quicksave()
	{
		if (!CheckQsEligibility())
		{
			return false;
		}
		indicatedError = false;
		quicksaveNode = new ConfigNode();
		VTScenario.current.QuicksaveScenario(quicksaveNode);
		if (this.OnQuicksave != null)
		{
			Debug.Log("Quicksaving...");
			this.OnQuicksave(quicksaveNode);
			quicksaveNode.SaveToFile(Path.Combine(PilotSaveManager.saveDataPath, "quicksave.cfg"));
		}
		QuicksaveBGM();
		flightLog.Clear();
		foreach (FlightLogger.LogEntry item in FlightLogger.GetLog())
		{
			flightLog.Add(item);
		}
		Missile.QuicksaveMissiles();
		quicksaveMET = FlightSceneManager.instance.missionElapsedTime;
		savesUsed++;
		return true;
	}

	private void QuicksaveBGM()
	{
		if (BGMManager.isPlaying)
		{
			Debug.LogFormat("Quicksaving BGM: {0} loop:{1} t:{2}", BGMManager.currentID, BGMManager.isLoopingBGM, BGMManager.currentTime);
			ConfigNode configNode = quicksaveNode.AddNode("BGMManager");
			configNode.SetValue("currentID", BGMManager.currentID);
			configNode.SetValue("isLoopingBGM", BGMManager.isLoopingBGM);
			configNode.SetValue("currentTime", BGMManager.currentTime);
		}
	}

	private void QuickloadBGM()
	{
		ConfigNode node = quicksaveNode.GetNode("BGMManager");
		if (node != null)
		{
			string value = node.GetValue("currentID");
			bool value2 = node.GetValue<bool>("isLoopingBGM");
			float value3 = node.GetValue<float>("currentTime");
			VTScenario.current.systemActions.ResumeBGM(value, value2, value3);
		}
		else
		{
			BGMManager.FadeOut();
		}
	}

	public bool CheckScenarioQsLimits()
	{
		if (VTScenario.current.qsMode == QSModes.None)
		{
			return false;
		}
		if (VTScenario.current.qsMode == QSModes.Rearm_Only && !CheckIsInRearmZone())
		{
			return false;
		}
		if (VTScenario.current.qsLimit > 0 && instance.savesUsed >= VTScenario.current.qsLimit)
		{
			return false;
		}
		return true;
	}

	private bool CheckIsInRearmZone()
	{
		List<ReArmingPoint> reArmingPoints = ReArmingPoint.reArmingPoints;
		for (int i = 0; i < reArmingPoints.Count; i++)
		{
			ReArmingPoint reArmingPoint = reArmingPoints[i];
			if (reArmingPoint.team == Teams.Allied && (reArmingPoint.transform.position - FlightSceneManager.instance.playerActor.position).sqrMagnitude < reArmingPoint.radius * reArmingPoint.radius)
			{
				return true;
			}
		}
		return false;
	}

	public bool CheckQsEligibility()
	{
		if (!FlightSceneManager.instance.playerActor || !FlightSceneManager.instance.playerActor.alive || FlightSceneManager.instance.playerHasEjected || VTOLMPUtils.IsMultiplayer())
		{
			return false;
		}
		if (!PilotSaveManager.currentVehicle.quicksaveReady && GameStartup.version.releaseType == GameVersion.ReleaseTypes.Public)
		{
			return false;
		}
		return true;
	}

	private void OnDestroy()
	{
		quicksaveNode = null;
	}

	public void Quickload()
	{
		indicatedError = false;
		if (quicksaveNode != null)
		{
			FlightSceneManager.instance.ReloadScene();
			quickloading = true;
			StartCoroutine(QuickloadRoutine());
		}
	}

	private void Awake()
	{
		instance = this;
		isQuickload = false;
		indicatedError = false;
		savesUsed = 0;
	}

	private void Start()
	{
		quicksaveNode = null;
		OnQuicksave += Bullet.OnQuicksave;
		OnQuickload += Bullet.OnQuickload;
	}

	private IEnumerator QuickloadRoutine()
	{
		_hasQuickloadedMissiles = false;
		yield return null;
		isQuickload = true;
		while (!VTMapManager.fetch || !VTMapManager.fetch.scenarioReady || !FlightSceneManager.instance || FlightSceneManager.instance.switchingScene)
		{
			yield return null;
		}
		if (this.OnQuickloadEarly != null)
		{
			Debug.Log("Quickloading (early)...");
			this.OnQuickloadEarly(quicksaveNode);
		}
		VTScenario.current.QuickloadScenario(quicksaveNode);
		if (this.OnQuickload != null)
		{
			Debug.Log("Quickloading...");
			this.OnQuickload(quicksaveNode);
		}
		if (this.OnQuickloadLate != null)
		{
			Debug.Log("Quickloading (late)...");
			this.OnQuickloadLate(quicksaveNode);
		}
		foreach (FlightLogger.LogEntry item in flightLog)
		{
			FlightLogger.Relog(item);
		}
		while (!PlayerSpawn.qLoadPlayerComplete)
		{
			yield return null;
		}
		FloatingOrigin.instance.AddQueuedFixedUpdateAction(FixedUpdateQuickloadFinalization);
	}

	private void FixedUpdateQuickloadFinalization()
	{
		Debug.Log("Finalizing quickload.");
		VTScenario.current.FinalQuicksaveResume();
		Missile.QuickloadMissiles();
		_hasQuickloadedMissiles = true;
		this.OnQuickloadedMissiles?.Invoke(quicksaveNode);
		quickloading = false;
		ScreenFader.FadeIn(0.5f);
		QuickloadBGM();
	}

	public static ConfigNode SaveActorIdentifierToNode(Actor a, string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		SaveActorIdentifier(a, out var id, out var globalPos, out var subUnitID);
		configNode.SetValue("actorID", id);
		configNode.SetValue("globalPos", globalPos);
		configNode.SetValue("subUnitID", subUnitID);
		configNode.SetValue("m_actorID", a ? a.actorID : (-1));
		return configNode;
	}

	public static Actor RetrieveActorFromNode(ConfigNode node)
	{
		if (node == null)
		{
			return null;
		}
		int value = node.GetValue<int>("actorID");
		Vector3D value2 = node.GetValue<Vector3D>("globalPos");
		int value3 = node.GetValue<int>("subUnitID");
		int value4 = node.GetValue<int>("m_actorID");
		Actor result = RetrieveActor(value, value2, value3, value4);
		if (value == -3 && value4 >= 0)
		{
			if (!_hasQuickloadedMissiles)
			{
				Debug.LogError("Tried to retrieve a missile actor before missiles were fully quickloaded!");
			}
			Missile quicksavedMissile = Missile.GetQuicksavedMissile(value4);
			if ((bool)quicksavedMissile)
			{
				result = quicksavedMissile.actor;
			}
		}
		return result;
	}

	public static void SaveActorIdentifier(Actor a, out int id, out Vector3D globalPos, out int subUnitID)
	{
		subUnitID = -1;
		if (!a)
		{
			id = -1;
			globalPos = Vector3D.zero;
			return;
		}
		globalPos = VTMapManager.WorldToGlobalPoint(a.position);
		if ((bool)a.unitSpawn)
		{
			id = a.unitSpawn.unitID;
			return;
		}
		if (a.role == Actor.Roles.Missile)
		{
			id = -3;
			return;
		}
		UnitSpawn parentActorSpawn = GetParentActorSpawn(a);
		if ((bool)parentActorSpawn)
		{
			id = parentActorSpawn.unitID;
			subUnitID = ((AIUnitSpawn)parentActorSpawn).subUnits.IndexOf(a);
		}
		else
		{
			id = -2;
		}
	}

	public static Actor RetrieveActor(int id, Vector3D globalPos, int subUnitID, int m_actorID = -1)
	{
		switch (id)
		{
		case -1:
			return null;
		case -2:
		{
			if (m_actorID >= 0)
			{
				for (int i = 0; i < TargetManager.instance.allActors.Count; i++)
				{
					Actor actor = TargetManager.instance.allActors[i];
					if ((bool)actor && actor.actorID == m_actorID)
					{
						return actor;
					}
				}
			}
			Vector3 vector = VTMapManager.GlobalToWorldPoint(globalPos);
			Actor actor2 = null;
			float num = 250000f;
			for (int j = 0; j < TargetManager.instance.allActors.Count; j++)
			{
				Actor actor3 = TargetManager.instance.allActors[j];
				if ((bool)actor3)
				{
					float sqrMagnitude = (actor3.position - vector).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						actor2 = actor3;
						num = sqrMagnitude;
					}
					if (m_actorID >= 0 && m_actorID == actor3.actorID)
					{
						return actor3;
					}
				}
			}
			Debug.LogErrorFormat(" - could not retrieve a QS actor reference from actorID, used proximity: {0}", actor2.DebugName());
			return actor2;
		}
		case -3:
			return null;
		default:
		{
			UnitSpawn spawnedUnit = VTScenario.current.units.GetUnit(id).spawnedUnit;
			if (subUnitID >= 0)
			{
				return ((AIUnitSpawn)spawnedUnit).subUnits[subUnitID];
			}
			return spawnedUnit.actor;
		}
		}
	}

	public static UnitSpawn GetParentActorSpawn(Actor a)
	{
		Actor item = a;
		while ((bool)a.parentActor)
		{
			a = a.parentActor;
			if ((bool)a.unitSpawn && a.unitSpawn is AIUnitSpawn)
			{
				AIUnitSpawn aIUnitSpawn = (AIUnitSpawn)a.unitSpawn;
				if (aIUnitSpawn.subUnits != null && aIUnitSpawn.subUnits.Contains(item))
				{
					return a.unitSpawn;
				}
			}
		}
		return null;
	}
}
