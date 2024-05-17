using System.Collections.Generic;
using UnityEngine;

public class GroundSquad : MonoBehaviour
{
	public enum GroundFormations
	{
		Grid,
		Vee,
		EchelonRight,
		EchelonLeft,
		Spread,
		Column
	}

	private enum SquadCommands
	{
		None,
		MoveToWpt,
		MovePath
	}

	private Soldier leaderSoldier;

	private GroundUnitMover _leaderMover;

	private List<GroundUnitMover> units = new List<GroundUnitMover>();

	private List<Soldier> bayLoadedSoldiers = new List<Soldier>();

	private bool positionsDirty = true;

	public GroundFormations formationType;

	private float largestSize;

	private float formationMargin = 2f;

	private SquadCommands squadCommand;

	private Transform commandWpt;

	private FollowPath commandPath;

	public GroundUnitMover leaderMover
	{
		get
		{
			return _leaderMover;
		}
		set
		{
			if (_leaderMover != value)
			{
				_leaderMover = value;
				if (value != null)
				{
					leaderSoldier = value.GetComponent<Soldier>();
				}
			}
		}
	}

	public float slowestSpeed { get; private set; }

	public void SetBeginFormationMovement()
	{
		foreach (GroundUnitMover unit in units)
		{
			if ((bool)unit)
			{
				unit.BeginMovingFormation();
			}
		}
	}

	public void RegisterUnit(GroundUnitMover m, bool updateLeader = true)
	{
		if (!units.Contains(m))
		{
			units.Add(m);
			m.positionInGroup = units.Count - 1;
			UpdateSizeAndSpeed();
		}
		m.squad = this;
		if (updateLeader)
		{
			UpdateLeader();
		}
	}

	public void UnregisterUnit(GroundUnitMover m, bool updateLeader = true)
	{
		if (units.Contains(m))
		{
			m.positionInGroup = -1;
			units.Remove(m);
			m.squad = null;
			if (leaderMover == m)
			{
				leaderMover = null;
			}
			UpdateSizeAndSpeed();
		}
		if (updateLeader)
		{
			UpdateLeader();
		}
	}

	public void UpdateSizeAndSpeed()
	{
		largestSize = 0f;
		slowestSpeed = float.MaxValue;
		for (int i = 0; i < units.Count; i++)
		{
			if ((bool)units[i])
			{
				largestSize = Mathf.Max(largestSize, units[i].unitSize);
				slowestSpeed = Mathf.Min(slowestSpeed, units[i].moveSpeed);
			}
		}
	}

	private void Update()
	{
		UpdateLeader();
		UpdatePositions();
	}

	private bool LeaderIsLoadedInBay()
	{
		if ((bool)leaderMover)
		{
			Soldier soldier = leaderSoldier;
			if ((bool)soldier && (soldier.isLoadedInBay || soldier.isLoadedInAIBay))
			{
				return true;
			}
		}
		return false;
	}

	private bool UnitIsLoadedInBay(GroundUnitMover m)
	{
		Soldier component = m.GetComponent<Soldier>();
		if ((bool)component)
		{
			if (!component.isLoadedInAIBay)
			{
				return component.isLoadedInBay;
			}
			return true;
		}
		return false;
	}

	private void UpdateLeader()
	{
		for (int num = bayLoadedSoldiers.Count - 1; num >= 0; num--)
		{
			Soldier soldier = bayLoadedSoldiers[num];
			if (soldier.actor.alive && !soldier.isLoadedInAIBay && !soldier.isLoadedInBay)
			{
				units.Add(soldier.mover);
				soldier.mover.positionInGroup = units.Count - 1;
				UpdateSizeAndSpeed();
				bayLoadedSoldiers.RemoveAt(num);
			}
		}
		if (!(leaderMover == null) && leaderMover.actor.alive && !LeaderIsLoadedInBay())
		{
			return;
		}
		if (LeaderIsLoadedInBay())
		{
			bayLoadedSoldiers.Add(leaderSoldier);
			units.Remove(leaderSoldier.mover);
			leaderSoldier.mover.positionInGroup = -1;
			UpdateSizeAndSpeed();
		}
		units.RemoveAll((GroundUnitMover x) => x == null || !x.actor.alive);
		if (units.Count > 0)
		{
			leaderMover = units[0];
			if (squadCommand == SquadCommands.MovePath)
			{
				MovePath(commandPath);
			}
			else if (squadCommand == SquadCommands.MoveToWpt)
			{
				MoveTo(commandWpt);
			}
			else if (squadCommand == SquadCommands.None)
			{
				StopAll();
			}
		}
		else
		{
			leaderMover = null;
		}
		positionsDirty = true;
	}

	private void UpdatePositions()
	{
		if (!positionsDirty)
		{
			return;
		}
		largestSize = 0f;
		for (int i = 0; i < units.Count; i++)
		{
			if ((bool)units[i])
			{
				units[i].positionInGroup = i;
				largestSize = Mathf.Max(largestSize, units[i].unitSize);
			}
		}
		positionsDirty = false;
	}

	public Vector3 GetFormationPosition(int grpPos)
	{
		switch (formationType)
		{
		case GroundFormations.Grid:
			return GridPosition(grpPos);
		case GroundFormations.Vee:
			return VeePosition(grpPos);
		case GroundFormations.EchelonRight:
			return EchelonPosition(grpPos, 1);
		case GroundFormations.EchelonLeft:
			return EchelonPosition(grpPos, -1);
		case GroundFormations.Spread:
			return SpreadPosition(grpPos);
		case GroundFormations.Column:
			return ColumnPosition(grpPos);
		default:
			Debug.LogError("Unhandled GroundSquad formationType: " + formationType);
			return Vector3.zero;
		}
	}

	private Vector3 GridPosition(int grpPos)
	{
		if (units == null)
		{
			Debug.LogError("No units for GridPosition");
		}
		if (leaderMover == null)
		{
			Debug.LogError("No leader mover for GridPosition");
		}
		int num = Mathf.RoundToInt(Mathf.Sqrt(units.Count));
		int num2 = num / 2;
		if (grpPos - num2 <= 0)
		{
			grpPos--;
		}
		Vector3 formationForward = leaderMover.formationForward;
		Vector3 vector = Vector3.Cross(Vector3.up, formationForward);
		float num3 = largestSize + formationMargin;
		Vector3 vector2 = leaderMover.transform.position + (0f - num3) * (float)num2 * vector;
		int num4 = grpPos / num;
		int num5 = grpPos % num;
		return vector2 + (float)num5 * num3 * vector - (float)num4 * num3 * formationForward;
	}

	private Vector3 EchelonPosition(int grpPos, int side)
	{
		Vector3 formationForward = leaderMover.formationForward;
		Vector3 vector = Vector3.Cross(Vector3.up, formationForward);
		float num = largestSize + formationMargin;
		return leaderMover.transform.position + (float)(side * grpPos) * num * (vector - formationForward);
	}

	private Vector3 SpreadPosition(int grpPos)
	{
		int num = units.Count / 2;
		if (grpPos - num <= 0)
		{
			grpPos--;
		}
		Vector3 formationForward = leaderMover.formationForward;
		Vector3 vector = Vector3.Cross(Vector3.up, formationForward);
		float num2 = largestSize + formationMargin;
		return leaderMover.transform.position + (0f - num2) * (float)num * vector + num2 * vector * grpPos;
	}

	private Vector3 VeePosition(int grpPos)
	{
		grpPos++;
		int num = ((grpPos % 2 == 0) ? 1 : (-1));
		int num2 = 1 + grpPos / 2;
		Vector3 formationForward = leaderMover.formationForward;
		Vector3 vector = Vector3.Cross(Vector3.up, formationForward);
		float num3 = largestSize + formationMargin;
		return leaderMover.transform.position + (float)(num * (num2 - 1)) * num3 * vector + -num2 * formationForward;
	}

	private Vector3 ColumnPosition(int grpPos)
	{
		float num = largestSize + formationMargin;
		Vector3 vector = Vector3.zero;
		bool flag = false;
		if ((leaderMover.behavior == GroundUnitMover.Behaviors.Path || leaderMover.behavior == GroundUnitMover.Behaviors.RailPath) && (bool)leaderMover.path)
		{
			float num2 = num / leaderMover.path.GetApproximateLength();
			float num3 = leaderMover.currTOnPath - (float)grpPos * num2;
			if (num3 > 0f)
			{
				vector = leaderMover.path.GetWorldPoint(num3);
				flag = true;
			}
		}
		if (!flag)
		{
			Vector3 formationForward = leaderMover.formationForward;
			vector = leaderMover.transform.position - num * (float)grpPos * formationForward;
		}
		Transform transform = units[grpPos].transform;
		if ((VRHead.position - transform.position).sqrMagnitude > 1.225E+09f)
		{
			transform.position = vector;
		}
		return vector;
	}

	public void MoveTo(Transform wpt)
	{
		Debug.Log("ground squad set to MoveTo");
		squadCommand = SquadCommands.MoveToWpt;
		commandWpt = wpt;
		if ((bool)leaderMover)
		{
			UpdateSizeAndSpeed();
			leaderMover.behavior = GroundUnitMover.Behaviors.StayInRadius;
			leaderMover.parkWhenInRallyRadius = true;
			leaderMover.rallyTransform = wpt;
			leaderMover.RefreshBehaviorRoutines();
		}
	}

	public void MovePath(FollowPath path)
	{
		Debug.Log("ground squad set to MovePath");
		squadCommand = SquadCommands.MovePath;
		commandPath = path;
		if ((bool)leaderMover)
		{
			UpdateSizeAndSpeed();
			leaderMover.behavior = GroundUnitMover.Behaviors.Path;
			leaderMover.path = path;
			leaderMover.RefreshBehaviorRoutines();
		}
	}

	public void StopAll()
	{
		squadCommand = SquadCommands.None;
		foreach (GroundUnitMover unit in units)
		{
			if ((bool)unit)
			{
				unit.behavior = GroundUnitMover.Behaviors.Parked;
				unit.RefreshBehaviorRoutines();
			}
		}
	}
}
