using UnityEngine;

public class ArtilleryUnitSpawn : GroundUnitSpawn
{
	public class ArtilleryTargetFilter : IUnitFilter
	{
		public bool PassesFilter(UnitSpawner uSpawner)
		{
			if ((bool)uSpawner.prefabUnitSpawn.actor)
			{
				if (uSpawner.prefabUnitSpawn.actor.role != Actor.Roles.Ground && uSpawner.prefabUnitSpawn.actor.role != Actor.Roles.GroundArmor)
				{
					return uSpawner.prefabUnitSpawn.actor.role == Actor.Roles.Ship;
				}
				return true;
			}
			return false;
		}
	}

	public Component artilleryUnit;

	private IArtilleryUnit aUnit;

	private void Awake()
	{
		if (!artilleryUnit || !(artilleryUnit is IArtilleryUnit))
		{
			Debug.LogError("ArtilleryUnitSpawn does not have an IArtilleryUnit!!", base.gameObject);
		}
		aUnit = (IArtilleryUnit)artilleryUnit;
	}

	[VTEvent("Fire On Waypoint", "Commands the artillery unit to fire a single salvo on a waypoint position if it's in range.", new string[] { "Waypoint", "Shot Count" })]
	public void FireOnWaypoint(Waypoint wpt, [VTRangeParam(1f, 12f)][VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)] float count)
	{
		if (wpt != null && wpt.GetTransform() != null)
		{
			aUnit.FireOnPosition(new FixedPoint(wpt.GetTransform().position), Vector3.zero, Mathf.RoundToInt(count), 1);
		}
	}

	[VTEvent("Fire Salvos On Waypoint", "Commands the artillery unit to fire a number of salvos on a waypoint position if it's in range.", new string[] { "Waypoint", "Shots per salvo", "Salvos" })]
	public void FireMultiOnWaypoint(Waypoint wpt, [VTRangeParam(1f, 120f)][VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)] float shotsPerSalvo, [VTRangeParam(1f, 120f)][VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)] float salvos)
	{
		if (wpt != null && wpt.GetTransform() != null)
		{
			aUnit.FireOnPosition(new FixedPoint(wpt.GetTransform().position), Vector3.zero, Mathf.RoundToInt(shotsPerSalvo), Mathf.RoundToInt(salvos));
		}
	}

	[VTEvent("Fire Salvos On Waypoint Radius", "Commands the artillery unit to fire a number of salvos spread out over an area around a position if it's in range.", new string[] { "Waypoint", "Radius", "Shots per salvo", "Salvos" })]
	public void FireMultiOnWaypointRadius(Waypoint wpt, [VTRangeParam(1f, 5000f)] float radius, [VTRangeParam(1f, 120f)][VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)] float shotsPerSalvo, [VTRangeParam(1f, 120f)][VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)] float salvos)
	{
		if (wpt != null && wpt.GetTransform() != null)
		{
			aUnit.FireOnPositionRadius(new FixedPoint(wpt.GetTransform().position), radius, Mathf.RoundToInt(shotsPerSalvo), Mathf.RoundToInt(salvos));
		}
	}

	[VTEvent("Fire On Unit", "Commands the artillery unit to fire on a specific unit if it's in range.", new string[] { "Target", "Shots per salvo", "Salvos" })]
	public void FireOnUnit([VTTeamOptionParam(TeamOptions.OtherTeam)][VTUnitReferenceSubsParam(false)][VTActionParam(typeof(ArtilleryTargetFilter), null)] UnitReference target, [VTRangeParam(1f, 120f)][VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)] float shotsPerSalvo, [VTRangeParam(1f, 120f)][VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)] float salvos)
	{
		UnitSpawn unit = target.GetUnit();
		if ((bool)unit)
		{
			aUnit.FireOnActor(unit.actor, Mathf.RoundToInt(shotsPerSalvo), Mathf.RoundToInt(salvos));
		}
	}

	[VTEvent("Clear Fire Orders", "Clears any existing fire orders for the artillery unit.")]
	public void ClearFireOrders()
	{
		aUnit.ClearFireOrders();
	}
}
