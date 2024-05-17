using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirbaseNavNode : MonoBehaviour
{
	public enum NodeTypes
	{
		Midpoint,
		TakeOff,
		Exit,
		Parking
	}

	public List<AirbaseNavNode> connectedNodes = new List<AirbaseNavNode>();

	public NodeTypes nodeType;

	[HideInInspector]
	public Vector3 ts_position;

	[HideInInspector]
	public string ts_name;

	[Header("TakeOff")]
	public Runway takeoffRunway;

	public float runwayLength;

	[Header("Parking")]
	public bool vtolOnly;

	public float parkingSize;

	public HangarDoorAnimator hangarDoor;

	[Header("Exit")]
	public List<AirbaseNavNode> destinationParkingNodes;

	[Header("Carrier")]
	public FollowPath carrierReturnPath;

	private List<AIPilot> pathReservedActors = new List<AIPilot>();

	private bool unreserveRunning;

	private AirbaseNavigation navigation;

	private AirportManager _airport;

	private bool gotAirport;

	public Vector3 position => base.transform.position;

	public Actor parkingOccupiedBy { get; set; }

	public bool isReservedForPath => pathReservedActors.Count > 0;

	private void OnEnable()
	{
		ts_position = base.transform.localPosition;
		if ((bool)hangarDoor && nodeType == NodeTypes.Parking)
		{
			hangarDoor.parkingNode = this;
		}
	}

	public float ReserveForPath(AIPilot a)
	{
		if (!unreserveRunning)
		{
			StartCoroutine(UnreserveUsersRoutine());
		}
		float num = -1f;
		for (int i = 0; i < pathReservedActors.Count; i++)
		{
			AIPilot aIPilot = pathReservedActors[i];
			if (!aIPilot || !aIPilot.actor.alive)
			{
				pathReservedActors.RemoveAll((AIPilot x) => !x || !x.actor.alive);
				i = 0;
				continue;
			}
			if (aIPilot == a)
			{
				if (num >= 0f)
				{
					num += 2f * a.actor.physicalRadius + VTOLVRConstants.AIRBASE_NAV_SPACING;
				}
				return num;
			}
			if (num < 0f)
			{
				num = 0f;
			}
			num += 2f * aIPilot.actor.physicalRadius + VTOLVRConstants.AIRBASE_NAV_SPACING;
		}
		pathReservedActors.Add(a);
		return num + 2f * a.actor.physicalRadius + VTOLVRConstants.AIRBASE_NAV_SPACING;
	}

	private IEnumerator UnreserveUsersRoutine()
	{
		unreserveRunning = true;
		yield return null;
		while (pathReservedActors.Count > 0)
		{
			int i = pathReservedActors.Count - 1;
			while (i >= 0 && i < pathReservedActors.Count)
			{
				AIPilot aIPilot = pathReservedActors[i];
				if (!aIPilot || !aIPilot.actor.alive || (aIPilot.taxiingToNavNode != this && (aIPilot.actor.position - base.transform.position).sqrMagnitude > aIPilot.actor.physicalRadius * aIPilot.actor.physicalRadius))
				{
					pathReservedActors.RemoveAt(i);
				}
				yield return null;
				i--;
			}
			yield return null;
		}
		unreserveRunning = false;
	}

	public void UnreserveForPath(AIPilot a)
	{
		pathReservedActors.Remove(a);
	}

	public AirportManager GetAirport()
	{
		if (!gotAirport)
		{
			_airport = GetComponentInParent<AirportManager>();
			if (!_airport)
			{
				AirbaseNavigation componentInParent = GetComponentInParent<AirbaseNavigation>();
				if ((bool)componentInParent && (bool)componentInParent.transform.parent)
				{
					_airport = componentInParent.transform.parent.GetComponentInChildren<AirportManager>();
				}
			}
			gotAirport = true;
		}
		return _airport;
	}

	public ConfigNode QuicksaveToNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		foreach (AIPilot pathReservedActor in pathReservedActors)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(pathReservedActor.actor, "pra"));
		}
		return configNode;
	}

	public void QuickloadFromNode(ConfigNode node)
	{
		pathReservedActors.Clear();
		foreach (ConfigNode node2 in node.GetNodes("pra"))
		{
			Actor actor = QuicksaveManager.RetrieveActorFromNode(node2);
			if ((bool)actor.unitSpawn && actor.unitSpawn is AIAircraftSpawn)
			{
				pathReservedActors.Add(((AIAircraftSpawn)actor.unitSpawn).aiPilot);
			}
		}
		if (!unreserveRunning)
		{
			StartCoroutine(UnreserveUsersRoutine());
		}
	}
}
