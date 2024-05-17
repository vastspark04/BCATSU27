using UnityEngine;

public class AISeaUnitSpawn : AIUnitSpawnEquippable
{
	public enum SeaUnitDefaultBehaviors
	{
		Parked,
		Move_To_Waypoint,
		Navigate_Path
	}

	public ShipMover shipMover;

	[UnitSpawn("Sea Group")]
	public VTUnitGroup.UnitGroup unitGroup;

	[UnitSpawn("Default Behavior")]
	public SeaUnitDefaultBehaviors defaultBehavior;

	[UnitSpawn("Waypoint")]
	public Waypoint defaultWaypoint;

	[UnitSpawn("Path")]
	public FollowPath defaultPath;

	[ApplyInMP]
	[UnitSpawnAttributeConditional("HasHullNumber")]
	[UnitSpawnAttributeRange("Hull Number", 0f, 999f, UnitSpawnAttributeRange.RangeTypes.Int)]
	public float hullNumber;

	private ShipHullNumber hn;

	public bool HasHullNumber()
	{
		return GetComponentInChildren<ShipHullNumber>() != null;
	}

	[VTEvent("Move To", "Command the vessel to move to a waypoint.", new string[] { "Target" })]
	public void MoveTo(Waypoint target)
	{
		shipMover.MoveTo(target);
	}

	[VTEvent("Move Path", "Command the vessel to move along a path.")]
	public void MovePath(FollowPath path)
	{
		shipMover.MovePath(path);
	}

	public override void OnPreSpawnUnit()
	{
		base.OnPreSpawnUnit();
		hn = GetComponentInChildren<ShipHullNumber>(includeInactive: true);
		if ((bool)hn)
		{
			int num = Mathf.RoundToInt(hullNumber);
			if (num == 0)
			{
				hullNumber = (num = base.unitID);
			}
			string text = num.ToString();
			if (!unitSpawner.unitName.Contains(text))
			{
				unitSpawner.unitName = $"{unitSpawner.unitName} {text}";
			}
		}
	}

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		if (defaultBehavior == SeaUnitDefaultBehaviors.Navigate_Path)
		{
			if ((bool)defaultPath)
			{
				MovePath(defaultPath);
			}
		}
		else if (defaultBehavior == SeaUnitDefaultBehaviors.Move_To_Waypoint && defaultWaypoint != null)
		{
			MoveTo(defaultWaypoint);
		}
		if ((bool)hn)
		{
			hn.SetNumber(Mathf.RoundToInt(hullNumber));
		}
		shipMover.SetPosition(unitSpawner.transform.position);
	}

	public override void Quickload(ConfigNode qsNode)
	{
		base.Quickload(qsNode);
		ShipWake[] componentsInChildren = GetComponentsInChildren<ShipWake>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].trailParticles.ResetParticles();
		}
	}
}
