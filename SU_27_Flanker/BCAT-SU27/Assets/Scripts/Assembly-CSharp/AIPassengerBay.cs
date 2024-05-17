using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIPassengerBay : MonoBehaviour, IQSVehicleComponent
{
	public AIPilot pilot;

	public int capacity;

	public float loadRadius;

	public Transform[] seatTransforms;

	public Transform[] unloadTransforms;

	private List<Soldier> loadedSoldiers;

	private List<Soldier> expectedUnits = new List<Soldier>();

	private bool unloadingWhenAvailable;

	private Transform unloadingWhenAvailableAtTf;

	private const string QS_SOLDIER_NAME = "SOLDIER";

	private string QS_NODE_NAME => "AIPassengerBay_" + base.gameObject.name;

	private void Awake()
	{
		loadedSoldiers = new List<Soldier>(capacity);
	}

	private void Start()
	{
		pilot.actor.health.OnDeath.AddListener(KillPassengersOnDeath);
	}

	private void KillPassengersOnDeath()
	{
		foreach (Soldier loadedSoldier in loadedSoldiers)
		{
			if ((bool)loadedSoldier && (bool)loadedSoldier.actor && (bool)loadedSoldier.actor.health)
			{
				loadedSoldier.actor.health.Damage(loadedSoldier.actor.health.maxHealth + 1f, base.transform.position, Health.DamageTypes.Impact, pilot.actor.health.killedByActor, "Died in passenger bay.");
			}
		}
	}

	public bool IsFull()
	{
		return loadedSoldiers.Count >= capacity;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawWireSphere(base.transform.position, loadRadius);
	}

	public void ExpectPickupUnit(Soldier s)
	{
		if (!expectedUnits.Contains(s))
		{
			expectedUnits.Add(s);
		}
	}

	public bool IsExpectingUnits()
	{
		bool flag = false;
		for (int i = 0; i < expectedUnits.Count; i++)
		{
			if (expectedUnits[i] == null)
			{
				flag = true;
			}
			else if (!expectedUnits[i].actor.alive)
			{
				flag = true;
			}
		}
		if (flag)
		{
			expectedUnits.RemoveAll((Soldier x) => x == null || !x.actor.alive);
		}
		return expectedUnits.Count > 0;
	}

	public void LoadSoldier(Soldier s)
	{
		if (loadedSoldiers.Count >= capacity)
		{
			Debug.Log("AIPassengerBay tried to load a soldier but it's full.");
			return;
		}
		s.gameObject.SetActive(value: false);
		loadedSoldiers.Add(s);
		s.mover.ClearSurfaceObj();
		expectedUnits.Remove(s);
		if (IsFull())
		{
			expectedUnits.Clear();
		}
	}

	public void UnloadSoldier(Soldier s, Vector3 targetPosition)
	{
		if (!loadedSoldiers.Contains(s))
		{
			return;
		}
		Transform transform = null;
		float num = float.MaxValue;
		for (int i = 0; i < unloadTransforms.Length; i++)
		{
			float sqrMagnitude = (unloadTransforms[i].position - targetPosition).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				transform = unloadTransforms[i];
			}
		}
		s.transform.position = transform.position;
		s.gameObject.SetActive(value: true);
		loadedSoldiers.Remove(s);
	}

	public void UnloadAllSoldiersWhenAvailable(Transform rallyWaypoint)
	{
		StartCoroutine(UnloadAllSoldiersWhenAvailableRoutine(rallyWaypoint));
	}

	private IEnumerator UnloadAllSoldiersWhenAvailableRoutine(Transform rallyWaypoint)
	{
		unloadingWhenAvailable = true;
		unloadingWhenAvailableAtTf = rallyWaypoint;
		while (pilot.isAlive && !pilot.autoPilot.flightInfo.isLanded)
		{
			yield return null;
		}
		foreach (Soldier loadedSoldier in loadedSoldiers)
		{
			if ((bool)loadedSoldier.mover.squad && loadedSoldier.mover.squad.leaderMover == null)
			{
				loadedSoldier.mover.squad.MoveTo(rallyWaypoint);
			}
		}
		if (pilot.autoPilot.flightInfo.isLanded)
		{
			Debug.Log("Finally beginning unload of soldiers from " + pilot.aiSpawn.unitSpawner.GetUIDisplayName());
			while (loadedSoldiers.Count > 0 && pilot.autoPilot.flightInfo.isLanded)
			{
				yield return new WaitForSeconds(1f);
				loadedSoldiers[0].DismountAIBayImmediate(rallyWaypoint);
			}
		}
		unloadingWhenAvailable = false;
		unloadingWhenAvailableAtTf = null;
	}

	private void LateUpdate()
	{
		int count = loadedSoldiers.Count;
		for (int i = 0; i < count; i++)
		{
			loadedSoldiers[i].gameObject.SetActive(value: false);
			loadedSoldiers[i].transform.position = base.transform.position;
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode(QS_NODE_NAME);
		foreach (Soldier loadedSoldier in loadedSoldiers)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(loadedSoldier.actor, "SOLDIER"));
		}
		ConfigNode configNode2 = configNode.AddNode("expectingUnits");
		foreach (Soldier expectedUnit in expectedUnits)
		{
			configNode2.AddNode(QuicksaveManager.SaveActorIdentifierToNode(expectedUnit.actor, "eUnit"));
		}
		configNode.SetValue("unloadingWhenAvailable", unloadingWhenAvailable);
		if (unloadingWhenAvailable)
		{
			configNode.SetValue("unloadingWhenAvailableWp", VTSConfigUtils.WriteObject(VTScenario.current.waypoints.GetWaypoint(unloadingWhenAvailableAtTf)));
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(QS_NODE_NAME);
		if (node == null)
		{
			return;
		}
		foreach (ConfigNode node2 in node.GetNodes("SOLDIER"))
		{
			Actor actor = QuicksaveManager.RetrieveActorFromNode(node2);
			if ((bool)actor)
			{
				Soldier component = actor.GetComponent<Soldier>();
				if ((bool)component)
				{
					component.BoardAIBayImmediate(this);
					continue;
				}
				Debug.LogError("AIPassengerBay quickloaded an actor with no soldier component!");
				QuicksaveManager.instance.IndicateError();
			}
			else
			{
				Debug.LogError("AIPassengerBay quickload failed to retrieve the actor of a loaded soldier!");
				QuicksaveManager.instance.IndicateError();
			}
		}
		foreach (ConfigNode node3 in node.GetNode("expectingUnits").GetNodes("eUnit"))
		{
			Actor actor2 = QuicksaveManager.RetrieveActorFromNode(node3);
			expectedUnits.Add(actor2.GetComponent<Soldier>());
		}
		unloadingWhenAvailable = node.GetValue<bool>("unloadingWhenAvailable");
		if (unloadingWhenAvailable)
		{
			Waypoint waypoint = VTSConfigUtils.ParseObject<Waypoint>(node.GetValue("unloadingWhenAvailableWp"));
			UnloadAllSoldiersWhenAvailable(waypoint.GetTransform());
		}
	}
}
