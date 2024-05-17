using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointManager : MonoBehaviour
{
	public delegate void OnSetGPSWaypointDelegate(GPSTargetGroup g, int index);

	public static WaypointManager instance;

	public List<Transform> waypoints = new List<Transform>();

	private int waypointIndex = -1;

	private bool isUsingGPSwaypoints;

	private Transform _currWpt;

	private ObjectPool wptTfPool;

	public Transform fuelWaypoint;

	public Transform rtbWaypoint;

	public Transform bullseye;

	public List<AirbaseNavNode> taxiNodes;

	public Transform currentWaypoint
	{
		get
		{
			if ((bool)_currWpt && !_currWpt.gameObject.activeSelf)
			{
				_currWpt = null;
			}
			return _currWpt;
		}
		set
		{
			if (_currWpt != value)
			{
				_currWpt = value;
				if ((bool)_currWpt)
				{
					Debug.Log("SetWaypoint (currWaypoint = " + _currWpt.gameObject.name + ")");
					Actor componentInParent = _currWpt.GetComponentInParent<Actor>();
					if ((bool)componentInParent)
					{
						this.OnSetActorWaypoint?.Invoke(componentInParent);
					}
					this.OnSetUnknownWaypoint?.Invoke(new FixedPoint(_currWpt.position));
				}
				else
				{
					Debug.Log("SetWaypoint (currWaypoint = null)");
					this.OnSetWaypoint?.Invoke(null);
				}
			}
			ClearGPSWaypoints();
		}
	}

	public event Action<FixedPoint> OnSetUnknownWaypoint;

	public event Action<Waypoint> OnSetWaypoint;

	public event Action<Actor> OnSetActorWaypoint;

	public event OnSetGPSWaypointDelegate OnSetGPSWaypoint;

	public void SetWaypoint(Waypoint waypoint)
	{
		if (waypoint != null)
		{
			Debug.Log("SetWaypoint(Waypoint " + waypoint.name + ")");
			_currWpt = waypoint.GetTransform();
			if (waypoint is UnitWaypoint)
			{
				UnitWaypoint unitWaypoint = (UnitWaypoint)waypoint;
				this.OnSetActorWaypoint?.Invoke(unitWaypoint.unitSpawner.spawnedUnit.actor);
			}
			else
			{
				this.OnSetWaypoint?.Invoke(waypoint);
			}
		}
		else
		{
			Debug.Log("SetWaypoint(Waypoint null)");
			_currWpt = null;
			this.OnSetWaypoint?.Invoke(waypoint);
		}
	}

	public void RemoteSetWaypoint(Waypoint waypoint)
	{
		if (waypoint != null)
		{
			Debug.Log("RemoteSetWaypoint(Waypoint " + waypoint.name + ")");
			_currWpt = waypoint.GetTransform();
		}
		else
		{
			Debug.Log("RemoteSetWaypoint(Waypoint null)");
			_currWpt = null;
		}
	}

	public void RemoteSetWaypoint(Actor actor)
	{
		if ((bool)actor)
		{
			Debug.Log("RemoteSetWaypoint(Actor " + actor.actorName + ")");
			_currWpt = actor.transform;
		}
		else
		{
			Debug.Log("RemoteSetWaypoint(Actor null)");
			currentWaypoint = null;
		}
	}

	public void RemoteSetWaypoint(FixedPoint fixedPoint)
	{
		Debug.Log($"RemoteSetWaypoint(FixedPoint {fixedPoint.globalPoint})");
		SetWaypointGlobalPos(fixedPoint.globalPoint);
	}

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		GameObject gameObject = new GameObject("Waypoint");
		gameObject.AddComponent<FloatingOriginTransform>();
		wptTfPool = ObjectPool.CreateObjectPool(gameObject, 5, canGrow: true, destroyOnLoad: true);
		gameObject.SetActive(value: false);
		FlightSceneManager.instance.OnExitScene += Instance_OnExitScene;
		QuicksaveManager.instance.OnQuicksave += OnQuicksave;
		QuicksaveManager.instance.OnQuickload += OnQuickload;
	}

	private void OnQuicksave(ConfigNode configNode)
	{
		try
		{
			ConfigNode configNode2 = configNode.AddNode("WaypointManager");
			configNode2.SetValue("isUsingGPSwaypoints", isUsingGPSwaypoints);
			if (isUsingGPSwaypoints)
			{
				List<Vector3D> list = new List<Vector3D>();
				foreach (Transform waypoint in waypoints)
				{
					list.Add(VTMapManager.WorldToGlobalPoint(waypoint.position));
				}
				configNode2.SetValue("gpsPoints", list);
				configNode2.SetValue("waypointIndex", waypointIndex);
			}
			else if (_currWpt != null)
			{
				Actor componentInParent = _currWpt.GetComponentInParent<Actor>();
				if ((bool)componentInParent)
				{
					configNode2.AddNode(QuicksaveManager.SaveActorIdentifierToNode(componentInParent, "waypointActor"));
				}
				else
				{
					configNode2.SetValue("currWptPoint", VTMapManager.WorldToGlobalPoint(_currWpt.position));
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Waypoint manager had an error quicksaving!\n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
	}

	private void OnQuickload(ConfigNode configNode)
	{
		Debug.Log("WaypointManager quickloading...");
		StartCoroutine(QLRoutine(configNode));
	}

	private IEnumerator QLRoutine(ConfigNode configNode)
	{
		while (!FlightSceneManager.instance.playerActor)
		{
			yield return null;
		}
		try
		{
			Debug.Log("WaypointManager waited for player actor.");
			ConfigNode node = configNode.GetNode("WaypointManager");
			if (node == null)
			{
				yield break;
			}
			if (node.GetValue<bool>("isUsingGPSwaypoints"))
			{
				List<Vector3D> value = node.GetValue<List<Vector3D>>("gpsPoints");
				GPSTargetGroup gPSTargetGroup = new GPSTargetGroup("QL ", 0);
				gPSTargetGroup.isPath = value.Count > 0;
				foreach (Vector3D item in value)
				{
					gPSTargetGroup.AddTarget(new GPSTarget(VTMapManager.GlobalToWorldPoint(item), "qll", 0));
				}
				gPSTargetGroup.currentTargetIdx = node.GetValue<int>("waypointIndex");
				SetWaypointGPS(gPSTargetGroup);
				Debug.Log("WaypointManager quickloaded GPS waypoint.");
			}
			else if (node.HasNode("waypointActor"))
			{
				Debug.Log("WaypointManager attempting to quickload actor waypoint.");
				Actor actor = QuicksaveManager.RetrieveActorFromNode(node.GetNode("waypointActor"));
				if ((bool)actor)
				{
					currentWaypoint = actor.transform;
					Debug.Log("WaypointManager quickloaded actor waypoint.");
				}
			}
			else if (node.HasValue("currWptPoint"))
			{
				SetWaypointGlobalPos(node.GetValue<Vector3D>("currWptPoint"));
				Debug.Log("WaypointManager quickloaded global point waypoint.");
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Waypoint manager had an error quickloading!\n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
	}

	private void Instance_OnExitScene()
	{
		SetWaypoint(null);
		taxiNodes = null;
		ClearGPSWaypoints();
	}

	private void ClearGPSWaypoints()
	{
		if (!isUsingGPSwaypoints)
		{
			return;
		}
		isUsingGPSwaypoints = false;
		if (waypoints != null)
		{
			for (int i = 0; i < waypoints.Count; i++)
			{
				waypoints[i].gameObject.SetActive(value: false);
			}
			waypoints.Clear();
		}
		waypointIndex = -1;
	}

	public void SetWaypointGPS(GPSTargetGroup gpsGroup, bool sendEvt = true)
	{
		Debug.Log("SetWaypointGPS()");
		ClearGPSWaypoints();
		if (gpsGroup.isPath)
		{
			for (int i = 0; i < gpsGroup.targets.Count; i++)
			{
				waypoints.Add(wptTfPool.GetPooledObject().transform);
				waypoints[i].gameObject.SetActive(value: true);
				waypoints[i].position = gpsGroup.targets[i].worldPosition;
			}
			waypointIndex = gpsGroup.currentTargetIdx;
			if (sendEvt)
			{
				this.OnSetGPSWaypoint?.Invoke(gpsGroup, waypointIndex);
			}
		}
		else
		{
			waypoints.Clear();
			waypointIndex = 0;
			GameObject pooledObject = wptTfPool.GetPooledObject();
			pooledObject.SetActive(value: true);
			waypoints.Add(pooledObject.transform);
			waypoints[0].position = gpsGroup.currentTarget.worldPosition;
			if (sendEvt)
			{
				this.OnSetGPSWaypoint?.Invoke(gpsGroup, gpsGroup.currentTargetIdx);
			}
		}
		_currWpt = waypoints[waypointIndex];
		isUsingGPSwaypoints = true;
	}

	private void SetWaypointGlobalPos(Vector3D globalPos)
	{
		Debug.Log($"SetWaypointGlobalPos({globalPos})");
		GPSTargetGroup gPSTargetGroup = new GPSTargetGroup("QL ", 1);
		gPSTargetGroup.AddTarget(new GPSTarget(VTMapManager.GlobalToWorldPoint(globalPos), "qlp", 0));
		gPSTargetGroup.currentTargetIdx = 0;
		SetWaypointGPS(gPSTargetGroup);
	}

	public Transform GetPreviousGPSPoint()
	{
		if (isUsingGPSwaypoints && waypointIndex > 0)
		{
			return waypoints[waypointIndex - 1];
		}
		return null;
	}

	private void NextWaypointOnPath()
	{
		waypointIndex++;
		if (waypointIndex >= waypoints.Count)
		{
			ClearGPSWaypoints();
			_currWpt = null;
		}
		else
		{
			_currWpt = waypoints[waypointIndex];
		}
	}

	private void Update()
	{
		if ((bool)FlightSceneManager.instance.playerActor)
		{
			if (isUsingGPSwaypoints && (bool)_currWpt && (FlightSceneManager.instance.playerActor.position - _currWpt.position).sqrMagnitude < 640000f)
			{
				NextWaypointOnPath();
			}
			if (taxiNodes != null && taxiNodes.Count > 0 && ((FlightSceneManager.instance.playerActor.position - taxiNodes[taxiNodes.Count - 1].transform.position).sqrMagnitude < 900f || (!FlightSceneManager.instance.playerActor.flightInfo.isLanded && FlightSceneManager.instance.playerActor.flightInfo.radarAltitude > 20f)))
			{
				taxiNodes = null;
			}
			if ((bool)_currWpt && !_currWpt.gameObject.activeInHierarchy)
			{
				SetWaypoint(null);
			}
		}
	}

	public void ClearWaypoint()
	{
		currentWaypoint = null;
	}

	public void SetFuelWaypoint()
	{
		if ((bool)fuelWaypoint && fuelWaypoint.gameObject.activeInHierarchy)
		{
			currentWaypoint = fuelWaypoint;
		}
		else
		{
			SetWaypoint(null);
		}
	}

	public void SetRTBWaypoint()
	{
		currentWaypoint = rtbWaypoint;
	}

	public void GetBullsBRA(Vector3 target, out float bearing, out float range, out float altitude)
	{
		if ((bool)bullseye)
		{
			bearing = VectorUtils.Bearing(bullseye.position, target);
			range = (target - bullseye.position).magnitude;
			altitude = WaterPhysics.GetAltitude(target);
		}
		else
		{
			bearing = 0f;
			range = 0f;
			altitude = 0f;
		}
	}
}
