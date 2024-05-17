using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

public class TacticalSituationController : MonoBehaviour
{
	public class TSTargetInfo
	{
		public int tscIdx;

		public int dataIdx;

		public float detectionTime = -1f;

		public bool lost;

		private FixedPoint _point;

		public Vector3 point
		{
			get
			{
				return GetPoint();
			}
			set
			{
				SetPoint(value);
			}
		}

		protected virtual void SetPoint(Vector3 worldPoint)
		{
			_point = new FixedPoint(worldPoint);
		}

		protected virtual Vector3 GetPoint()
		{
			return _point.point;
		}
	}

	public class TSActorTargetInfo : TSTargetInfo
	{
		public Actor actor;

		public Vector3 velocity;

		public bool radar;

		public bool stationary;

		public Vector3 estimatedPosition => base.point + velocity * (Time.time - detectionTime);
	}

	public class TSGPSTargetInfo : TSTargetInfo
	{
		public GPSTargetGroup gpsGroup;

		public GPSTarget gpsData
		{
			get
			{
				if (gpsGroup != null && gpsGroup.targets != null && dataIdx >= 0 && dataIdx < gpsGroup.targets.Count)
				{
					return gpsGroup.targets[dataIdx];
				}
				return null;
			}
		}

		protected override Vector3 GetPoint()
		{
			return gpsData?.worldPosition ?? Vector3.zero;
		}
	}

	public delegate void TSActorInfoDelegate(TSTargetInfo info);

	private class MissileTWSTrack
	{
		public Missile missile;

		public Actor target;
	}

	public enum TargetStates
	{
		Known,
		Uncertain,
		Lost
	}

	private struct QueuedActorUpdate
	{
		public Actor actor;

		public Vector3 position;

		public Vector3 velocity;

		public float timeDetected;
	}

	public Radar radar;

	public LockingRadar lockingRadar;

	public OpticalTargeter opticalTargeter;

	public VisualTargetFinder visualTargetFinder;

	public WeaponManager weaponManager;

	public ModuleRWR rwr;

	[Header("Runtime")]
	public bool autoRadar = true;

	public List<TSTargetInfo> infos = new List<TSTargetInfo>();

	private Dictionary<int, int> actorID_to_infoIdx = new Dictionary<int, int>();

	private Dictionary<GPSTargetGroup, List<TSGPSTargetInfo>> gpsInfos = new Dictionary<GPSTargetGroup, List<TSGPSTargetInfo>>();

	private List<MissileTWSTrack> twsTracks = new List<MissileTWSTrack>();

	private int selectedIdx = -1;

	private bool autoPointLock;

	private Missile processedRadarMissile;

	public float targetViewPersistTime = 30f;

	public float targetUncertaintyTime = 5f;

	private Queue<QueuedActorUpdate> dataLinkedRadarDataQueue = new Queue<QueuedActorUpdate>();

	private bool hasSetupDatalink;

	private bool hasQuickloaded;

	private ConfigNode tscQsNode;

	public bool readyAfterQL
	{
		get
		{
			if (QuicksaveManager.isQuickload)
			{
				return hasQuickloaded;
			}
			return true;
		}
	}

	public event TSActorInfoDelegate OnRegisteredInfo;

	public event Action<TSTargetInfo> OnUnitSelected;

	public event Action OnUnitDeselected;

	public event Action<RadarLockData> OnAutoRadarLocked;

	public event Action OnAutoRadarUnlocked;

	public int SelectedIndex()
	{
		return selectedIdx;
	}

	public Actor GetCurrentSelectionActor()
	{
		TSTargetInfo currentSelectionInfo = GetCurrentSelectionInfo();
		if (currentSelectionInfo != null && currentSelectionInfo is TSActorTargetInfo)
		{
			return ((TSActorTargetInfo)currentSelectionInfo).actor;
		}
		return null;
	}

	public TSTargetInfo GetCurrentSelectionInfo()
	{
		if (selectedIdx >= 0 && selectedIdx < infos.Count)
		{
			return infos[selectedIdx];
		}
		return null;
	}

	public TSGPSTargetInfo GetGPSInfo(GPSTargetGroup grp, int tgtIdx)
	{
		if (gpsInfos.TryGetValue(grp, out var value) && tgtIdx >= 0 && tgtIdx < value.Count)
		{
			return value[tgtIdx];
		}
		return null;
	}

	public void SelectTarget(int idx)
	{
		if (idx < 0)
		{
			Deselect();
			return;
		}
		selectedIdx = idx;
		TSTargetInfo tSTargetInfo = infos[idx];
		if (tSTargetInfo is TSGPSTargetInfo)
		{
			TSGPSTargetInfo tSGPSTargetInfo = (TSGPSTargetInfo)tSTargetInfo;
			weaponManager.gpsSystem.SelectTarget(tSGPSTargetInfo.gpsGroup, tSGPSTargetInfo.dataIdx);
		}
		if (this.OnUnitSelected != null)
		{
			this.OnUnitSelected(infos[idx]);
		}
	}

	public void SelectUnit(Actor a)
	{
		if (actorID_to_infoIdx.TryGetValue(a.actorID, out var value))
		{
			SelectTarget(value);
		}
	}

	public TSActorTargetInfo GetActorInfo(Actor a)
	{
		if (actorID_to_infoIdx.TryGetValue(a.actorID, out var value))
		{
			return (TSActorTargetInfo)infos[value];
		}
		return null;
	}

	public void Deselect()
	{
		if (selectedIdx != -1)
		{
			selectedIdx = -1;
			if (this.OnUnitDeselected != null)
			{
				this.OnUnitDeselected();
			}
		}
	}

	private void RegisterActor(Actor a, Vector3 position, Vector3 velocity, bool radar = false)
	{
		RegisterActor(a, position, velocity, Time.time, radar);
	}

	private void RegisterActor(Actor a, Vector3 position, Vector3 velocity, float timeDetected, bool radar = false)
	{
		if ((QuicksaveManager.isQuickload && !hasQuickloaded) || a == FlightSceneManager.instance.playerActor || !a.opticalTargetable)
		{
			return;
		}
		if ((bool)a.parentActor)
		{
			RegisterActor(a.parentActor, position + (a.parentActor.position - a.position), velocity, timeDetected, radar);
			return;
		}
		int actorID = a.actorID;
		if (actorID_to_infoIdx.TryGetValue(actorID, out var value))
		{
			UpdateInfo(value, position, velocity, timeDetected, radar);
			return;
		}
		value = infos.Count;
		TSActorTargetInfo tSActorTargetInfo = new TSActorTargetInfo
		{
			actor = a,
			dataIdx = value,
			tscIdx = value
		};
		if ((a.role == Actor.Roles.Ground || a.role == Actor.Roles.GroundArmor) && !a.gameObject.GetComponentImplementing<GroundUnitMover>())
		{
			tSActorTargetInfo.stationary = true;
		}
		infos.Add(tSActorTargetInfo);
		actorID_to_infoIdx.Add(actorID, value);
		UpdateInfo(value, position, velocity, timeDetected, radar);
		if (this.OnRegisteredInfo != null)
		{
			this.OnRegisteredInfo(tSActorTargetInfo);
		}
	}

	private void UpdateInfo(int idx, Vector3 position, Vector3 velocity, float timeDetected, bool radar)
	{
		if (idx < 0 || idx >= infos.Count)
		{
			Debug.LogErrorFormat("Tried to update TSC info on invalid index. idx={0}, count={1}", idx, infos.Count);
			return;
		}
		TSTargetInfo tSTargetInfo = infos[idx];
		if (tSTargetInfo.detectionTime >= timeDetected)
		{
			return;
		}
		if (tSTargetInfo is TSActorTargetInfo)
		{
			TSActorTargetInfo tSActorTargetInfo = (TSActorTargetInfo)tSTargetInfo;
			if (velocity == Vector3.zero && tSActorTargetInfo.detectionTime > 0.1f)
			{
				float num = timeDetected - tSActorTargetInfo.detectionTime;
				if (!(num > 0.1f))
				{
					return;
				}
				tSActorTargetInfo.velocity = (position - tSActorTargetInfo.point) / num;
			}
			else
			{
				tSActorTargetInfo.velocity = velocity;
			}
			tSActorTargetInfo.point = position;
			tSActorTargetInfo.detectionTime = timeDetected;
			if (radar)
			{
				tSActorTargetInfo.radar = true;
			}
		}
		else if (tSTargetInfo is TSGPSTargetInfo)
		{
			TSGPSTargetInfo tSGPSTargetInfo = (TSGPSTargetInfo)tSTargetInfo;
			if (tSGPSTargetInfo.gpsData != null)
			{
				tSGPSTargetInfo.point = tSGPSTargetInfo.gpsData.worldPosition;
			}
		}
	}

	private void OnEnable()
	{
		StartCoroutine(CollectFromTGP());
		StartCoroutine(CollectFromVisualTargetFinder());
		StartCoroutine(CollectFromKnownAllies());
		if (VTOLMPUtils.IsMultiplayer())
		{
			VTOLMPDataLinkManager.instance.OnDataLinkReceived += Instance_OnDataLinkReceived;
		}
	}

	private void OnDisable()
	{
		if ((bool)VTOLMPSceneManager.instance)
		{
			VTOLMPDataLinkManager.instance.OnDataLinkReceived -= Instance_OnDataLinkReceived;
		}
	}

	private void Instance_OnDataLinkReceived(Actor a)
	{
		if ((bool)a)
		{
			RegisterActor(a, a.position, a.velocity);
		}
	}

	private void Start()
	{
		radar.OnDetectedActor += RadarDetected;
		rwr.OnDetectPing += Rwr_OnDetectPing;
		weaponManager.gpsSystem.onGPSTargetsChanged.AddListener(OnGPSTargetsChanged);
		weaponManager.OnFiredMissile += WeaponManager_OnFiredMissile;
		OnGPSTargetsChanged();
	}

	private void WeaponManager_OnFiredMissile(Missile obj)
	{
		if ((bool)obj.datalinkOnlyActor)
		{
			twsTracks.Add(new MissileTWSTrack
			{
				missile = obj,
				target = obj.datalinkOnlyActor
			});
		}
	}

	private void OnGPSTargetsChanged()
	{
		if (QuicksaveManager.isQuickload && !hasQuickloaded)
		{
			return;
		}
		GPSTargetSystem gpsSystem = weaponManager.gpsSystem;
		if (gpsSystem.currentGroup == null || gpsSystem.currentGroup.targets.Count <= 0)
		{
			return;
		}
		if (!gpsInfos.TryGetValue(gpsSystem.currentGroup, out var value))
		{
			value = new List<TSGPSTargetInfo>();
			gpsInfos.Add(gpsSystem.currentGroup, value);
		}
		while (value.Count < gpsSystem.currentGroup.targets.Count)
		{
			TSGPSTargetInfo tSGPSTargetInfo = new TSGPSTargetInfo();
			tSGPSTargetInfo.dataIdx = value.Count;
			tSGPSTargetInfo.gpsGroup = gpsSystem.currentGroup;
			tSGPSTargetInfo.point = gpsSystem.currentGroup.targets[tSGPSTargetInfo.dataIdx].worldPosition;
			tSGPSTargetInfo.tscIdx = infos.Count;
			value.Add(tSGPSTargetInfo);
			infos.Add(tSGPSTargetInfo);
			if (this.OnRegisteredInfo != null)
			{
				this.OnRegisteredInfo(tSGPSTargetInfo);
			}
		}
	}

	private float NetworkTime(float localTime)
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			return localTime + (VTNetworkManager.GetNetworkTimestamp() - Time.time);
		}
		return localTime;
	}

	private void Rwr_OnDetectPing(ModuleRWR.RWRContact contact)
	{
		contact.radarActor.UpdateKnownPosition(weaponManager.actor, mpBroadcast: true, NetworkTime(contact.GetTimeDetected()));
		RegisterActor(contact.radarActor, contact.radarActor.position, Vector3.zero, Time.time, radar: true);
	}

	public void SetAutoPointLock(bool l)
	{
		autoPointLock = l;
		if (!l && (bool)lockingRadar && lockingRadar.IsLocked())
		{
			lockingRadar.Unlock();
			if (this.OnAutoRadarUnlocked != null)
			{
				this.OnAutoRadarUnlocked();
			}
		}
	}

	private void Update()
	{
		if (autoRadar)
		{
			TSTargetInfo currentSelectionInfo = GetCurrentSelectionInfo();
			if (currentSelectionInfo != null && currentSelectionInfo is TSActorTargetInfo && ((TSActorTargetInfo)currentSelectionInfo).actor != null)
			{
				TSActorTargetInfo tSActorTargetInfo = (TSActorTargetInfo)currentSelectionInfo;
				if (tSActorTargetInfo.actor.finalCombatRole == Actor.Roles.Air || tSActorTargetInfo.actor.finalCombatRole == Actor.Roles.Missile)
				{
					if (!lockingRadar.IsLocked() || lockingRadar.currentLock.actor != tSActorTargetInfo.actor)
					{
						if (lockingRadar.IsLocked())
						{
							lockingRadar.Unlock();
							if (this.OnAutoRadarUnlocked != null)
							{
								this.OnAutoRadarUnlocked();
							}
						}
						if (autoPointLock)
						{
							lockingRadar.GetLock(tSActorTargetInfo.actor);
							if (lockingRadar.IsLocked() && this.OnAutoRadarLocked != null)
							{
								this.OnAutoRadarLocked(lockingRadar.currentLock);
							}
						}
					}
				}
				else if (lockingRadar.IsLocked())
				{
					lockingRadar.Unlock();
					if (this.OnAutoRadarUnlocked != null)
					{
						this.OnAutoRadarUnlocked();
					}
				}
			}
			else if (lockingRadar.IsLocked())
			{
				lockingRadar.Unlock();
				if (this.OnAutoRadarUnlocked != null)
				{
					this.OnAutoRadarUnlocked();
				}
			}
			bool flag = false;
			for (int i = 0; i < twsTracks.Count; i++)
			{
				MissileTWSTrack missileTWSTrack = twsTracks[i];
				if (!missileTWSTrack.missile || !missileTWSTrack.target)
				{
					flag = true;
					continue;
				}
				TSActorTargetInfo actorInfo = GetActorInfo(missileTWSTrack.target);
				if (actorInfo != null)
				{
					lockingRadar.UpdateTWSLock(actorInfo.actor, actorInfo.estimatedPosition, actorInfo.velocity);
				}
			}
			if (flag)
			{
				twsTracks.RemoveAll((MissileTWSTrack x) => !x.target || !x.missile);
			}
		}
		if (lockingRadar.IsLocked())
		{
			RegisterActor(lockingRadar.currentLock.actor, lockingRadar.currentLock.actor.position, lockingRadar.currentLock.actor.velocity);
		}
	}

	private IEnumerator MissileRadarTrackRoutine(Missile m, TSActorTargetInfo tgtInfo)
	{
		while (GetTargetState(tgtInfo) == TargetStates.Known && !m.isPitbull)
		{
			lockingRadar.UpdateTWSLock(tgtInfo.actor, tgtInfo.point, tgtInfo.velocity);
			yield return null;
		}
		lockingRadar.RemoveTWSLock(tgtInfo.actor);
	}

	public TargetStates GetTargetState(TSTargetInfo base_info)
	{
		if (base_info.lost)
		{
			return TargetStates.Lost;
		}
		if (base_info is TSActorTargetInfo)
		{
			TSActorTargetInfo tSActorTargetInfo = (TSActorTargetInfo)base_info;
			if ((bool)tSActorTargetInfo.actor && tSActorTargetInfo.actor.alive && tSActorTargetInfo.actor.gameObject.activeSelf)
			{
				float num = Time.time - tSActorTargetInfo.detectionTime;
				if (num < targetViewPersistTime || tSActorTargetInfo.stationary)
				{
					return TargetStates.Known;
				}
				if (num < targetViewPersistTime + targetUncertaintyTime)
				{
					return TargetStates.Uncertain;
				}
				return TargetStates.Lost;
			}
			return TargetStates.Lost;
		}
		if (base_info is TSGPSTargetInfo)
		{
			TSGPSTargetInfo tSGPSTargetInfo = (TSGPSTargetInfo)base_info;
			if (tSGPSTargetInfo.gpsData != null && weaponManager.gpsSystem.currentGroup == tSGPSTargetInfo.gpsGroup)
			{
				return TargetStates.Known;
			}
		}
		return TargetStates.Lost;
	}

	private void RadarDetected(Actor a)
	{
		if ((bool)a && a.alive)
		{
			RegisterActor(a, a.position, a.velocity);
		}
	}

	private void DataLinkedRadarDetected(Actor a)
	{
		if (a != null)
		{
			dataLinkedRadarDataQueue.Enqueue(new QueuedActorUpdate
			{
				actor = a,
				position = a.position,
				velocity = a.velocity,
				timeDetected = Time.time
			});
		}
	}

	private IEnumerator CollectFromTGP()
	{
		yield return null;
		while (base.enabled)
		{
			if ((bool)opticalTargeter.lockedActor)
			{
				RegisterActor(opticalTargeter.lockedActor, opticalTargeter.lockedActor.position, opticalTargeter.lockedActor.velocity);
			}
			for (int i = 0; i < 3; i++)
			{
				yield return null;
			}
		}
	}

	private IEnumerator CollectFromVisualTargetFinder()
	{
		yield return null;
		List<Actor> unitBuffer = new List<Actor>();
		while (base.enabled)
		{
			unitBuffer.Clear();
			for (int j = 0; j < visualTargetFinder.targetsSeen.Count; j++)
			{
				unitBuffer.Add(visualTargetFinder.targetsSeen[j]);
			}
			yield return null;
			for (int i = 0; i < unitBuffer.Count; i++)
			{
				Actor actor = unitBuffer[i];
				if ((bool)actor && actor.alive)
				{
					RegisterActor(actor, actor.position, actor.velocity);
				}
				yield return null;
			}
		}
	}

	private IEnumerator CollectFromKnownAllies()
	{
		yield return null;
		bool isMP = VTOLMPUtils.IsMultiplayer();
		Teams myTeam = weaponManager.actor.team;
		if (!hasSetupDatalink)
		{
			hasSetupDatalink = true;
			List<AIAircraftSpawn> datalinkAircraft = new List<AIAircraftSpawn>();
			while (VTScenario.current == null || VTScenario.current.units == null || VTScenario.current.units.alliedUnits == null)
			{
				yield return null;
			}
			foreach (UnitSpawner value in ((myTeam == Teams.Allied) ? VTScenario.current.units.alliedUnits : VTScenario.current.units.enemyUnits).Values)
			{
				if ((bool)value.spawnedUnit && value.spawnedUnit is AIAircraftSpawn)
				{
					AIAircraftSpawn aIAircraftSpawn = (AIAircraftSpawn)value.spawnedUnit;
					if (aIAircraftSpawn.tsdDataLinkAlways)
					{
						datalinkAircraft.Add(aIAircraftSpawn);
					}
				}
			}
			if (!isMP)
			{
				while (VTScenario.current.units.GetPlayerSpawner() == null)
				{
					yield return null;
				}
				PlayerSpawn playerSpawn = (PlayerSpawn)VTScenario.current.units.GetPlayerSpawner().spawnedUnit;
				while (playerSpawn == null)
				{
					yield return null;
					playerSpawn = (PlayerSpawn)VTScenario.current.units.GetPlayerSpawner().spawnedUnit;
				}
				if (playerSpawn.unitGroup != null)
				{
					foreach (int unitID in playerSpawn.unitGroup.unitIDs)
					{
						UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
						if ((bool)unit.spawnedUnit && unit.spawnedUnit is AIAircraftSpawn && unit.spawnedUnit.actor.hasRadar)
						{
							datalinkAircraft.Add((AIAircraftSpawn)unit.spawnedUnit);
						}
					}
				}
			}
			for (int j = 0; j < datalinkAircraft.Count; j++)
			{
				AIAircraftSpawn aIAircraftSpawn2 = datalinkAircraft[j];
				if (!aIAircraftSpawn2.unitSpawner.spawned || !aIAircraftSpawn2.actor.alive)
				{
					continue;
				}
				List<Radar> radars = aIAircraftSpawn2.actor.GetRadars();
				for (int k = 0; k < radars.Count; k++)
				{
					Radar radar = radars[k];
					if ((bool)radar)
					{
						radar.OnDetectedActor += DataLinkedRadarDetected;
					}
				}
			}
		}
		StartCoroutine(ReceiveRadarDataLinkRoutine());
		yield return null;
		List<Actor> unitBuffer = new List<Actor>();
		while (base.enabled)
		{
			unitBuffer.Clear();
			List<Actor> list = ((myTeam == Teams.Allied) ? TargetManager.instance.alliedUnits : TargetManager.instance.enemyUnits);
			for (int l = 0; l < list.Count; l++)
			{
				unitBuffer.Add(list[l]);
			}
			yield return null;
			for (int i = 0; i < unitBuffer.Count; i++)
			{
				Actor actor = unitBuffer[i];
				if ((bool)actor && actor.alive && actor.discovered)
				{
					RegisterActor(actor, actor.position, actor.velocity);
				}
				yield return null;
			}
		}
	}

	private IEnumerator ReceiveRadarDataLinkRoutine()
	{
		yield return null;
		WaitForSeconds twoSec = new WaitForSeconds(2f);
		while (base.enabled)
		{
			while (dataLinkedRadarDataQueue.Count == 0)
			{
				yield return twoSec;
			}
			while (dataLinkedRadarDataQueue.Count > 0)
			{
				QueuedActorUpdate queuedActorUpdate = dataLinkedRadarDataQueue.Dequeue();
				RegisterActor(queuedActorUpdate.actor, queuedActorUpdate.position, queuedActorUpdate.velocity, queuedActorUpdate.timeDetected);
			}
		}
	}

	public void Quicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode("TSC");
		configNode.SetValue("selectedIdx", selectedIdx);
		foreach (TSTargetInfo info in infos)
		{
			if (info == null)
			{
				continue;
			}
			ConfigNode configNode2 = new ConfigNode("info");
			configNode2.SetValue("dataIdx", info.dataIdx);
			configNode2.SetValue("tscIdx", info.tscIdx);
			configNode2.SetValue("detectionTimeElapsed", Time.time - info.detectionTime);
			configNode2.SetValue("point", new FixedPoint(info.point));
			configNode2.SetValue("lost", value: false);
			bool value = false;
			if (info is TSActorTargetInfo)
			{
				TSActorTargetInfo tSActorTargetInfo = (TSActorTargetInfo)info;
				if ((bool)tSActorTargetInfo.actor)
				{
					configNode2.AddNode(QuicksaveManager.SaveActorIdentifierToNode(tSActorTargetInfo.actor, "actor"));
					configNode2.SetValue("radar", tSActorTargetInfo.radar);
					configNode2.SetValue("velocity", tSActorTargetInfo.velocity);
				}
				else
				{
					configNode2.SetValue("lost", value: true);
					Debug.Log("- failed to save a TSC info for an actor (actor was null)");
				}
			}
			else if (info is TSGPSTargetInfo)
			{
				value = true;
				TSGPSTargetInfo tSGPSTargetInfo = (TSGPSTargetInfo)info;
				if (tSGPSTargetInfo.gpsGroup != null)
				{
					configNode2.SetValue("gpsGroupName", tSGPSTargetInfo.gpsGroup.groupName);
				}
				else
				{
					Debug.Log("- failed to save TSC info for a gps target (group is null)");
					configNode2.SetValue("lost", value: true);
				}
			}
			else
			{
				configNode2.SetValue("lost", value: true);
			}
			configNode2.SetValue("isGPS", value);
			configNode.AddNode(configNode2);
		}
	}

	public void Quickload(ConfigNode qsNode)
	{
		tscQsNode = qsNode;
		QuicksaveManager.instance.OnQuickloadedMissiles += QLAfterMissiles;
	}

	private void OnDestroy()
	{
		if ((bool)QuicksaveManager.instance)
		{
			QuicksaveManager.instance.OnQuickloadedMissiles -= QLAfterMissiles;
		}
	}

	private void QLAfterMissiles(ConfigNode dummy)
	{
		QuicksaveManager.instance.OnQuickloadedMissiles -= QLAfterMissiles;
		hasQuickloaded = true;
		Debug.Log("Quickloading TSC");
		ConfigNode configNode = tscQsNode;
		try
		{
			ConfigNode node = configNode.GetNode("TSC");
			if (node != null)
			{
				List<TSTargetInfo> list = new List<TSTargetInfo>();
				foreach (ConfigNode node2 in node.GetNodes("info"))
				{
					int value = node2.GetValue<int>("dataIdx");
					int value2 = node2.GetValue<int>("tscIdx");
					float detectionTime = Time.time - node2.GetValue<float>("detectionTimeElapsed");
					FixedPoint value3 = node2.GetValue<FixedPoint>("point");
					bool value4 = node2.GetValue<bool>("isGPS");
					if (node2.GetValue<bool>("lost"))
					{
						TSTargetInfo tSTargetInfo = new TSTargetInfo();
						tSTargetInfo.lost = true;
						tSTargetInfo.dataIdx = value;
						tSTargetInfo.tscIdx = value2;
						list.Add(tSTargetInfo);
						continue;
					}
					if (value4)
					{
						string value5 = node2.GetValue("gpsGroupName");
						TSGPSTargetInfo tSGPSTargetInfo = new TSGPSTargetInfo();
						tSGPSTargetInfo.dataIdx = value;
						tSGPSTargetInfo.tscIdx = value2;
						tSGPSTargetInfo.point = value3.point;
						tSGPSTargetInfo.detectionTime = detectionTime;
						if (weaponManager.gpsSystem.targetGroups.TryGetValue(value5, out var value6))
						{
							tSGPSTargetInfo.gpsGroup = value6;
						}
						list.Add(tSGPSTargetInfo);
						continue;
					}
					Actor actor = QuicksaveManager.RetrieveActorFromNode(node2.GetNode("actor"));
					bool value7 = node2.GetValue<bool>("radar");
					Vector3 value8 = node2.GetValue<Vector3>("velocity");
					TSActorTargetInfo tSActorTargetInfo = new TSActorTargetInfo();
					tSActorTargetInfo.actor = actor;
					tSActorTargetInfo.dataIdx = value;
					tSActorTargetInfo.detectionTime = detectionTime;
					tSActorTargetInfo.tscIdx = value2;
					tSActorTargetInfo.point = value3.point;
					tSActorTargetInfo.radar = value7;
					tSActorTargetInfo.velocity = value8;
					if ((bool)actor)
					{
						if (!actorID_to_infoIdx.ContainsKey(actor.actorID))
						{
							actorID_to_infoIdx.Add(actor.actorID, tSActorTargetInfo.tscIdx);
						}
						else
						{
							Actor a2 = null;
							foreach (TSTargetInfo item in list)
							{
								if (item is TSActorTargetInfo)
								{
									Actor actor2 = ((TSActorTargetInfo)item).actor;
									if ((bool)actor2 && actor2.actorID == actor.actorID)
									{
										a2 = ((TSActorTargetInfo)item).actor;
									}
								}
							}
							Debug.LogErrorFormat("Tried to add an actor to actorID_to_infoIdx but the dictionary had an existing entry with the same actorID ({0}) ExistingActor:{1} AddingActor{2}", actor.actorID, a2.DebugName(), actor.DebugName());
							QuicksaveManager.instance.IndicateError();
						}
					}
					list.Add(tSActorTargetInfo);
				}
				list.Sort((TSTargetInfo a, TSTargetInfo b) => a.tscIdx.CompareTo(b.tscIdx));
				Debug.Log("Quickloading TSC Data: ");
				for (int i = 0; i < list.Count; i++)
				{
					TSTargetInfo tSTargetInfo2 = list[i];
					if (tSTargetInfo2.tscIdx == i)
					{
						infos.Add(tSTargetInfo2);
						if (tSTargetInfo2 is TSGPSTargetInfo)
						{
							TSGPSTargetInfo tSGPSTargetInfo2 = (TSGPSTargetInfo)tSTargetInfo2;
							if (tSGPSTargetInfo2.gpsGroup != null)
							{
								if (gpsInfos.TryGetValue(tSGPSTargetInfo2.gpsGroup, out var value9))
								{
									value9.Add(tSGPSTargetInfo2);
								}
								else
								{
									value9 = new List<TSGPSTargetInfo>();
									value9.Add(tSGPSTargetInfo2);
									gpsInfos.Add(tSGPSTargetInfo2.gpsGroup, value9);
								}
							}
						}
						if (this.OnRegisteredInfo != null)
						{
							this.OnRegisteredInfo(tSTargetInfo2);
						}
					}
					else
					{
						Debug.LogError("Mismatched tsc info idx on quickload...");
					}
				}
				SelectTarget(node.GetValue<int>("selectedIdx"));
			}
			else
			{
				Debug.Log("qsNode did not have TSC node");
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("QS Error loading TSC: \n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
	}
}
