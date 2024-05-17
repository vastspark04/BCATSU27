using System.Collections.Generic;
using UnityEngine;

public class ShipGroup : MonoBehaviour
{
	public List<ShipMover> ships = new List<ShipMover>();

	private ShipMover leadShip;

	private float maxSpeed;

	private Transform waypointTransform;

	private void Awake()
	{
		waypointTransform = new GameObject("ShipGroupWaypoint").transform;
		waypointTransform.gameObject.AddComponent<FloatingOriginTransform>();
	}

	private void OnDestroy()
	{
		if ((bool)waypointTransform && Application.isPlaying)
		{
			Object.Destroy(waypointTransform.gameObject);
		}
	}

	public void SetLeader(ShipMover ship)
	{
		Waypoint waypoint = null;
		FollowPath followPath = null;
		if ((bool)leadShip)
		{
			leadShip.isLeader = false;
			waypoint = leadShip.currWpt;
			followPath = leadShip.currPath;
		}
		leadShip = ship;
		leadShip.isLeader = true;
		leadShip.formationMaxSpeed = maxSpeed;
		leadShip.formationLeader = null;
		leadShip.formationIdx = 0;
		List<ShipMover> list = new List<ShipMover>();
		list.Add(leadShip);
		ships.Remove(leadShip);
		AddShipsInClosestPosition(ships, list);
		ships = list;
		UpdateFormationIndices();
		if (waypoint != null)
		{
			MoveToPosition(waypoint);
		}
		else if ((bool)followPath)
		{
			MovePath(followPath);
		}
	}

	private void AddShipsInClosestPosition(List<ShipMover> shipPool, List<ShipMover> newShips)
	{
		int count = shipPool.Count;
		int num = 0;
		int num2 = 0;
		while (num < count)
		{
			Vector3 vector = leadShip.transform.TransformPoint(LocalFormationPos(num2));
			ShipMover item = null;
			float num3 = float.MaxValue;
			foreach (ShipMover item2 in shipPool)
			{
				float sqrMagnitude = (item2.transform.position - vector).sqrMagnitude;
				if (sqrMagnitude < num3)
				{
					num3 = sqrMagnitude;
					item = item2;
				}
			}
			newShips.Add(item);
			shipPool.Remove(item);
			num++;
			num2++;
		}
	}

	private void UpdateFormationIndices()
	{
		int num = 0;
		foreach (ShipMover ship in ships)
		{
			if (ship != leadShip)
			{
				ship.formationIdx = num;
				num++;
			}
		}
	}

	public void AddShip(ShipMover ship)
	{
		ships.Add(ship);
		ship.shipGroup = this;
		SetLeader(ships[0]);
		UpdateMaxSpeed();
	}

	public void RemoveShip(ShipMover ship)
	{
		ships.Remove(ship);
		ship.shipGroup = null;
		if (ships.Count > 0)
		{
			SetLeader(ships[0]);
			UpdateMaxSpeed();
		}
	}

	public void MoveToPosition(Waypoint wpt)
	{
		if (!leadShip)
		{
			return;
		}
		if (wpt != null)
		{
			waypointTransform.position = wpt.worldPosition;
			leadShip.MoveTo(wpt);
		}
		leadShip.formationMaxSpeed = maxSpeed;
		leadShip.isLeader = true;
		foreach (ShipMover ship in ships)
		{
			if (ship != leadShip)
			{
				ship.formationLeader = leadShip;
				ship.isLeader = false;
			}
		}
	}

	public void MovePath(FollowPath path)
	{
		MoveToPosition(null);
		if ((bool)leadShip)
		{
			leadShip.MovePath(path);
		}
	}

	public void SetFormationPosition(ShipMover ship, Transform formationTransform)
	{
		int formationIdx = ship.formationIdx;
		Vector3 vector2 = (formationTransform.localPosition = LocalFormationPos(formationIdx));
	}

	private Vector3 LocalFormationPos(int idx)
	{
		int num = ((idx % 2 != 0) ? 1 : (-1));
		Vector3 forward = Vector3.forward;
		Vector3 right = Vector3.right;
		int num2 = Mathf.CeilToInt(((float)idx + 1f) / 2f);
		float num3 = 300f;
		float num4 = (float)num2 * num3;
		float num5 = 300f;
		float num6 = (float)num2 * num5;
		return (0f - num4) * forward + num6 * (float)num * right;
	}

	private void UpdateMaxSpeed()
	{
		maxSpeed = float.MaxValue;
		for (int i = 0; i < ships.Count; i++)
		{
			maxSpeed = Mathf.Min(maxSpeed, ships[i].maxSpeed);
		}
		maxSpeed *= 0.9f;
		if ((bool)leadShip)
		{
			leadShip.formationMaxSpeed = maxSpeed;
		}
	}
}
