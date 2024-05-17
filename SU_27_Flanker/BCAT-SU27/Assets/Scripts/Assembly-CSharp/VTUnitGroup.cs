using System;
using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class VTUnitGroup
{
	public enum GroupTypes
	{
		Ground,
		Sea,
		Air,
		Unassigned
	}

	public delegate void UnitGroupDelegate(UnitSpawner unit, UnitGroup group);

	public class UnitGroup : IHasTeam
	{
		public class UnitGroupActions : IHasTeam
		{
			protected delegate bool UnitConditionDelegate(UnitSpawner u);

			public UnitGroup unitGroup;

			public Teams GetTeam()
			{
				return unitGroup.team;
			}

			[VTEvent("Spawn All", "Spawn all unspawned units in the group.")]
			public void SpawnAll()
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if ((bool)unit && !unit.spawned)
					{
						unit.SpawnUnit();
					}
				}
			}

			[VTEvent("Set Invincible", "Set the invincibility of each AI unit in the group.")]
			public void SetInvincible(bool i)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if ((bool)unit && unit.spawnedUnit is AIUnitSpawn)
					{
						((AIUnitSpawn)unit.spawnedUnit).SetInvincible(i);
					}
				}
			}

			[VTEvent("Kill All", "Kill all spawned AI units in the group.")]
			public void KillAll()
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if ((bool)unit && unit.spawned && unit.spawnedUnit is AIUnitSpawn && unit.spawnedUnit.actor.alive)
					{
						((AIUnitSpawn)unit.spawnedUnit).DestroySelf();
					}
				}
			}

			[SCCUnitProperty("All Alive", true)]
			public bool SCC_AllAlive()
			{
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if ((bool)unit && (bool)unit.spawnedUnit)
					{
						if (unit.spawnedUnit is PlayerSpawn)
						{
							if (!FlightSceneManager.instance.playerActor.alive)
							{
								return false;
							}
						}
						else if (unit.spawnedUnit is AIUnitSpawn)
						{
							AIUnitSpawn aIUnitSpawn = (AIUnitSpawn)unit.spawnedUnit;
							if ((bool)aIUnitSpawn.actor && !aIUnitSpawn.actor.alive)
							{
								return false;
							}
						}
						continue;
					}
					return false;
				}
				return true;
			}

			[SCCUnitProperty("Num Alive", new string[] { "Comparison", "Count" }, false)]
			public bool SCC_NumAlive(IntComparisons comparison, [VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)][VTRangeParam(0f, 100f)] float count)
			{
				int num = Mathf.RoundToInt(count);
				int num2 = 0;
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if ((bool)unit && (bool)unit.spawnedUnit)
					{
						if (unit.spawnedUnit is PlayerSpawn)
						{
							if (FlightSceneManager.instance.playerActor.alive)
							{
								num2++;
							}
						}
						else
						{
							if (!(unit.spawnedUnit is AIUnitSpawn))
							{
								continue;
							}
							AIUnitSpawn aIUnitSpawn = (AIUnitSpawn)unit.spawnedUnit;
							if ((bool)aIUnitSpawn.actor)
							{
								if (aIUnitSpawn.actor.alive)
								{
									num2++;
								}
							}
							else
							{
								num2++;
							}
						}
						continue;
					}
					return false;
				}
				return comparison switch
				{
					IntComparisons.Equals => num2 == num, 
					IntComparisons.Greater_Than => num2 > num, 
					IntComparisons.Less_Than => num2 < num, 
					_ => false, 
				};
			}

			[SCCUnitProperty("Any Near Waypoint", new string[] { "Waypoint", "Radius" }, true)]
			public bool SCC_AnyNearWaypoint(Waypoint wpt, [VTRangeParam(10f, 200000f)] float radius)
			{
				float num = radius * radius;
				for (int i = 0; i < unitGroup.unitIDs.Count; i++)
				{
					int unitID = unitGroup.unitIDs[i];
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if ((bool)unit && unit.spawned && (bool)unit.spawnedUnit && (bool)unit.spawnedUnit.actor && unit.spawnedUnit.actor.alive && wpt.GetTransform() != null)
					{
						Vector3 vector = unit.spawnedUnit.actor.position - wpt.worldPosition;
						vector.y = 0f;
						if (vector.sqrMagnitude < num)
						{
							return true;
						}
					}
				}
				return false;
			}

			[SCCUnitProperty("Any Unit Detected", new string[] { "By Team" }, true)]
			public bool SCC_AnyUnitDetected(Teams team)
			{
				for (int i = 0; i < unitGroup.unitIDs.Count; i++)
				{
					int unitID = unitGroup.unitIDs[i];
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if ((bool)unit && (bool)unit.spawnedUnit && (bool)unit.spawnedUnit.actor && unit.spawnedUnit.actor.alive && ((team == Teams.Allied) ? unit.spawnedUnit.actor.detectedByAllied : unit.spawnedUnit.actor.detectedByEnemy))
					{
						return true;
					}
				}
				return false;
			}

			[SCCUnitProperty("All Units Detected", new string[] { "By Team" }, true)]
			public bool SCC_AllUnitDetected(Teams team)
			{
				for (int i = 0; i < unitGroup.unitIDs.Count; i++)
				{
					int unitID = unitGroup.unitIDs[i];
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if (!unit || !unit.spawnedUnit || !unit.spawnedUnit.actor || !unit.spawnedUnit.actor.alive || !((team == Teams.Allied) ? unit.spawnedUnit.actor.detectedByAllied : unit.spawnedUnit.actor.detectedByEnemy))
					{
						return false;
					}
				}
				return true;
			}

			protected int GetCountWhere(UnitConditionDelegate d)
			{
				int num = 0;
				for (int i = 0; i < unitGroup.unitIDs.Count; i++)
				{
					if (d(VTScenario.current.units.GetUnit(unitGroup.unitIDs[i])))
					{
						num++;
					}
				}
				return num;
			}
		}

		public class UnitGroupActionsTargetPrefs : UnitGroupActions
		{
			[VTEvent("Set Non-targets", "Set the list of units this group will not attack.", new string[] { "Non-targets" })]
			public void SetNonTargets(UnitReferenceListOtherSubs nonTargets)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if (unit != null && (bool)unit.spawnedUnit && unit.spawnedUnit is AIUnitSpawn)
					{
						((AIUnitSpawn)unit.spawnedUnit).SetNonTargets(nonTargets);
					}
				}
			}

			[VTEvent("Add Non-targets", "Add units to the list of units this group will not attack.", new string[] { "Non-targets" })]
			public void AddNonTargets(UnitReferenceListOtherSubs nonTargets)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if (unit != null && (bool)unit.spawnedUnit && unit.spawnedUnit is AIUnitSpawn)
					{
						((AIUnitSpawn)unit.spawnedUnit).AddNonTargets(nonTargets);
					}
				}
			}

			[VTEvent("Remove Non-targets", "Remove units from to the list of units this group will not attack.", new string[] { "Non-targets" })]
			public void RemoveNonTargets(UnitReferenceListOtherSubs nonTargets)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if (unit != null && (bool)unit.spawnedUnit && unit.spawnedUnit is AIUnitSpawn)
					{
						((AIUnitSpawn)unit.spawnedUnit).RemoveNonTargets(nonTargets);
					}
				}
			}

			[VTEvent("Clear Non-targets", "Clear the list of units this group will not attack.")]
			public void ClearNonTargets()
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if (unit != null && (bool)unit.spawnedUnit && unit.spawnedUnit is AIUnitSpawn)
					{
						((AIUnitSpawn)unit.spawnedUnit).ClearNonTargets();
					}
				}
			}

			[VTEvent("Set Priority Targets", "Set the list of units this group will prioritize when attacking.", new string[] { "Targets" })]
			public void SetPriorityTargets(UnitReferenceListOtherSubs targets)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if (unit != null && (bool)unit.spawnedUnit && unit.spawnedUnit is AIUnitSpawn)
					{
						((AIUnitSpawn)unit.spawnedUnit).SetPriorityTargets(targets);
					}
				}
			}

			[VTEvent("Add Priority Targets", "Add units to the list of units this group will prioritize when attacking.", new string[] { "Targets" })]
			public void AddPriorityTargets(UnitReferenceListOtherSubs targets)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if (unit != null && (bool)unit.spawnedUnit && unit.spawnedUnit is AIUnitSpawn)
					{
						((AIUnitSpawn)unit.spawnedUnit).AddPriorityTargets(targets);
					}
				}
			}

			[VTEvent("Remove Priority Targets", "Remove units from the list of units this group will prioritize when attacking.", new string[] { "Targets" })]
			public void RemovePriorityTargets(UnitReferenceListOtherSubs targets)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if (unit != null && (bool)unit.spawnedUnit && unit.spawnedUnit is AIUnitSpawn)
					{
						((AIUnitSpawn)unit.spawnedUnit).RemovePriorityTargets(targets);
					}
				}
			}

			[VTEvent("Clear Priority Targets", "Clear the list of units this group will prioritize when attacking.")]
			public void ClearPriorityTargets()
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if (unit != null && (bool)unit.spawnedUnit && unit.spawnedUnit is AIUnitSpawn)
					{
						((AIUnitSpawn)unit.spawnedUnit).ClearPriorityTargets();
					}
				}
			}
		}

		public class SeaGroupActions : UnitGroupActionsTargetPrefs
		{
			public ShipGroup shipGroup;

			[VTEvent("Move To", "Command the sea group to move to a waypoint.", new string[] { "Waypoint" })]
			public void MoveTo(Waypoint waypoint)
			{
				if (VTScenario.isScenarioHost)
				{
					shipGroup.MoveToPosition(waypoint);
				}
			}

			[VTEvent("Move Path", "Command the sea group to move along a path.", new string[] { "Path" })]
			public void MovePath(FollowPath path)
			{
				if (VTScenario.isScenarioHost)
				{
					shipGroup.MovePath(path);
				}
			}

			[VTEvent("Set Engage Enemies", "Set whether the entire group should engage enemies", new string[] { "Engage" })]
			public void SetEngageEnemies(bool engage)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if ((bool)unit && (bool)unit.spawnedUnit)
					{
						((AIUnitSpawn)unit.spawnedUnit).SetEngageEnemies(engage);
					}
				}
			}
		}

		public class AirGroupActions : UnitGroupActions
		{
			public AIWing wing;

			public bool IsAllied()
			{
				return unitGroup.team == Teams.Allied;
			}

			[UnitSpawnAttributeConditional("IsAllied")]
			[VTEvent("Set Player Commands", "Set whether all units in this group can be commanded by the player.", new string[] { "Mode" })]
			public void SetPlayerCommands(AIAircraftSpawn.PlayerCommandsModes mode)
			{
				foreach (AIPilot pilot in wing.pilots)
				{
					if ((bool)pilot.aiSpawn && pilot.aiSpawn.wingmanVoice)
					{
						pilot.aiSpawn.SetPlayerCommands(mode);
					}
				}
			}

			[VTEvent("Take Off", "Command the air group to take off.")]
			public void TakeOff()
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (AIPilot pilot in wing.pilots)
				{
					if ((bool)pilot && (bool)pilot.aiSpawn)
					{
						pilot.aiSpawn.TakeOff();
					}
				}
			}

			[VTEvent("Land", "Command the air group to land at a specified airfield.", new string[] { "Airfield" })]
			public void Land([VTTeamOptionParam(TeamOptions.SameTeam)] AirportReference airport)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (AIPilot pilot in wing.pilots)
				{
					if ((bool)pilot && (bool)pilot.aiSpawn && (bool)pilot.aiSpawn)
					{
						pilot.aiSpawn.Land(airport);
					}
				}
			}

			[VTEvent("Rearm", "Command the air group to land at a specified airfield, rearm, and take off again.", new string[] { "Airfield" })]
			public void RearmAt([VTTeamOptionParam(TeamOptions.SameTeam)] AirportReference airport)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				AirportManager airport2 = airport.GetAirport();
				if (!(airport2 != null))
				{
					return;
				}
				foreach (AIPilot pilot in wing.pilots)
				{
					if (pilot.autoPilot.flightInfo.isLanded)
					{
						if ((bool)pilot.aiSpawn)
						{
							Debug.Log(pilot.aiSpawn.unitSpawner.GetUIDisplayName() + " was commanded landing/rearm but is already landed!");
						}
						break;
					}
					if (airport2.team != pilot.actor.team)
					{
						Debug.Log(pilot.actor.DebugName() + " was commanded to rearm but airbase is the opposing team!");
					}
					pilot.SetRearmAfterLanding(rearm: true);
					pilot.LandAtAirport(airport2);
				}
			}

			[VTEvent("Form On Leader", "Command the air group to form up on its AI wing leader.")]
			public void FormOnLeader()
			{
				if (VTScenario.isScenarioHost)
				{
					wing.FormOnLeader();
				}
			}

			[VTEvent("Form On Pilot", "Command the air group to form up on a particular air unit.", new string[] { "Target Leader" })]
			public void FormOnPilot([VTTeamOptionParam(TeamOptions.SameTeam)][VTActionParam(typeof(PilotUnitFilter), null)] UnitReference target)
			{
				if (!VTScenario.isScenarioHost || !target.GetSpawner().spawned)
				{
					return;
				}
				UnitSpawn unit = target.GetUnit();
				if ((bool)unit)
				{
					if (unit is PlayerSpawn)
					{
						wing.FormOnPlayer();
					}
					else if (unit is AIAircraftSpawn)
					{
						AIPilot aiPilot = ((AIAircraftSpawn)unit).aiPilot;
						wing.FormOnPilot(aiPilot);
					}
				}
			}

			[VTEvent("Set Nav Speed", "Set the default navigation airspeed for the entire group (when not in combat).", new string[] { "Airspeed" })]
			public void SetNavSpeed([VTRangeParam(60f, 700f)] float speed)
			{
				if (VTScenario.isScenarioHost)
				{
					wing.SetNavSpeed(speed);
				}
			}

			[VTEvent("Set Engage Enemies", "Set whether the entire group should engage enemies", new string[] { "Engage" })]
			public void SetEngageEnemies(bool engage)
			{
				if (VTScenario.isScenarioHost)
				{
					wing.SetAutoEngageEnemies(engage);
				}
			}

			[VTEvent("Fly Nav Path", "Command the air group to fly a path in formation.", new string[] { "Path" })]
			public void FlyNavPath(FollowPath path)
			{
				if (VTScenario.isScenarioHost)
				{
					wing.FlyNavPath(path);
				}
			}

			[VTEvent("Fly Orbit", "Command the air group to orbit a waypoint in formation.", new string[] { "Waypoint", "Radius", "Altitude" })]
			public void FlyOrbit(Waypoint wpt, [VTRangeParam(1000f, 80000f)] float radius, [VTRangeParam(1500f, 10000f)] float alt)
			{
				if (VTScenario.isScenarioHost)
				{
					wing.SetOrbitRadius(radius);
					wing.SetDefaultAltitude(alt);
					wing.OrbitTransform(wpt.GetTransform());
				}
			}

			[VTEvent("Set Altitude", "Set the air group's default altitude.", new string[] { "Altitude" })]
			public void SetAltitude([VTRangeParam(1500f, 10000f)] float alt)
			{
				if (VTScenario.isScenarioHost)
				{
					wing.SetDefaultAltitude(alt);
				}
			}

			[VTEvent("Attack Target", "Order the air group to attack a specific target, regardless of detection or other threats.", new string[] { "Target" })]
			public void AttackTarget([VTTeamOptionParam(TeamOptions.OtherTeam)][VTUnitReferenceSubsParam(true)] UnitReference tgt)
			{
				if (VTScenario.isScenarioHost)
				{
					UnitSpawn unit = tgt.GetUnit();
					if ((bool)unit)
					{
						wing.OrderAllAttackTarget(unit.actor);
					}
				}
			}

			[VTEvent("Cancel Attack Tgt", "Cancel the override attack target and return to normal behavior.")]
			public void CancelAttackTarget()
			{
				if (VTScenario.isScenarioHost)
				{
					wing.CancelAllAttackOrder();
				}
			}

			[VTEvent("Set Radio Comms", "Set whether the air group will communicate with the player via radio. (Units in the same group as Player do radio comms by default)", new string[] { "Enable" })]
			public void SetRadioComms(bool radioEnabled)
			{
				if (VTScenario.isScenarioHost)
				{
					wing.doRadioComms = radioEnabled;
				}
			}

			[VTEvent("Set Radar", "Sets whether AI pilots equipped with radars in this group should turn them on or off.", new string[] { "Radar On" })]
			public void SetRadar(bool radarOn)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (AIPilot pilot in wing.pilots)
				{
					if ((bool)pilot && (bool)pilot.detectionRadar)
					{
						pilot.vt_radarEnabled = radarOn;
					}
				}
			}

			[VTEvent("Add Priority Targets", "Adds unit(s) to each AI pilots' priority targets.  They will attack this target before others when those targets are detected.", new string[] { "Targets" })]
			public void AddPriorityTargets(UnitReferenceListOtherSubs targets)
			{
				if (!VTScenario.isScenarioHost || targets == null)
				{
					return;
				}
				foreach (UnitReference unit in targets.units)
				{
					foreach (AIPilot pilot in wing.pilots)
					{
						if ((bool)pilot)
						{
							pilot.AddPriorityTarget(unit.GetActor());
						}
					}
				}
			}

			[VTEvent("Set Priority Targets", "Sets or replaces the AI pilots' priority target lists to a new list.", new string[] { "Targets" })]
			public void SetPriorityTargets(UnitReferenceListOtherSubs targets)
			{
				if (!VTScenario.isScenarioHost || targets == null)
				{
					return;
				}
				foreach (AIPilot pilot in wing.pilots)
				{
					if (!pilot)
					{
						continue;
					}
					pilot.ClearPriorityTargets();
					foreach (UnitReference unit in targets.units)
					{
						Actor actor = unit.GetActor();
						if ((bool)actor)
						{
							pilot.AddPriorityTarget(actor);
						}
					}
				}
			}

			[VTEvent("Clear Priority Targets", "Clears the AI pilots' priority target lists.")]
			public void ClearPriorityTargets()
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (AIPilot pilot in wing.pilots)
				{
					if ((bool)pilot)
					{
						pilot.ClearPriorityTargets();
					}
				}
			}

			[VTEvent("Add Non-Targets", "Adds unit(s) to each AI pilots' non-targets lists.  They will never attack these targets.", new string[] { "Targets" })]
			public void AddNonTargets(UnitReferenceListOtherSubs targets)
			{
				if (!VTScenario.isScenarioHost || targets == null)
				{
					return;
				}
				foreach (UnitReference unit in targets.units)
				{
					foreach (AIPilot pilot in wing.pilots)
					{
						if ((bool)pilot)
						{
							pilot.AddNonTarget(unit.GetActor());
						}
					}
				}
			}

			[VTEvent("Set Non-Targets", "Sets or replaces the AI pilots' non-target lists to a new list. They will never attack these targets.", new string[] { "Targets" })]
			public void SetNonTargets(UnitReferenceListOtherSubs targets)
			{
				if (!VTScenario.isScenarioHost || targets == null)
				{
					return;
				}
				foreach (AIPilot pilot in wing.pilots)
				{
					if (!pilot)
					{
						continue;
					}
					pilot.ClearNonTargets();
					if (targets == null)
					{
						continue;
					}
					foreach (UnitReference unit in targets.units)
					{
						if ((bool)unit.GetActor())
						{
							pilot.AddNonTarget(unit.GetActor());
						}
					}
				}
			}

			[VTEvent("Clear Non-Targets", "Clears the AI pilots' non-target lists.")]
			public void ClearNonTargets()
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (AIPilot pilot in wing.pilots)
				{
					if ((bool)pilot)
					{
						pilot.ClearNonTargets();
					}
				}
			}

			[VTEvent("Add Designated Targets", "Adds units to the air group AI's designated targets, which it will attack at highest priority, immediately, whether or not these targets have been detected.", new string[] { "Targets" })]
			public void AddDesignatedTargets(UnitReferenceListOtherSubs targets)
			{
				if (!VTScenario.isScenarioHost || targets == null)
				{
					return;
				}
				foreach (UnitReference unit in targets.units)
				{
					wing.AddDesignatedTarget(unit.GetActor());
				}
			}

			[VTEvent("Set Designated Targets", "Sets or replaces the air group AI's designated targets, which it will attack at highest priority, immediately, whether or not these targets have been detected.", new string[] { "Targets" })]
			public void SetDesignatedTargets(UnitReferenceListOtherSubs targets)
			{
				if (VTScenario.isScenarioHost && targets != null)
				{
					wing.ClearDesignatedTargets();
					AddDesignatedTargets(targets);
				}
			}

			[VTEvent("Clear Designated Targets", "Clears the air group AI's designated targets.")]
			public void ClearDesignatedTargets()
			{
				if (VTScenario.isScenarioHost)
				{
					wing.ClearDesignatedTargets();
				}
			}

			[SCCUnitProperty("All Airborne", true)]
			public bool AllAirborne()
			{
				int count = unitGroup.unitIDs.Count;
				return GetCountWhere((UnitSpawner u) => u.spawnedUnit.actor.alive && !u.spawnedUnit.actor.flightInfo.isLanded) == count;
			}

			[SCCUnitProperty("Num Airborne", new string[] { "Comparison", "Count" }, false)]
			public bool NumAirborne(IntComparisons comparison, [VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)][VTRangeParam(0f, 100f)] float count)
			{
				int num = Mathf.RoundToInt(count);
				int countWhere = GetCountWhere((UnitSpawner u) => u.spawnedUnit.actor.alive && !u.spawnedUnit.actor.flightInfo.isLanded);
				return comparison switch
				{
					IntComparisons.Equals => countWhere == num, 
					IntComparisons.Greater_Than => countWhere > num, 
					IntComparisons.Less_Than => countWhere < num, 
					_ => false, 
				};
			}

			[SCCUnitProperty("All Landed", true)]
			public bool AllLanded()
			{
				int count = unitGroup.unitIDs.Count;
				return GetCountWhere((UnitSpawner u) => u.spawnedUnit.actor.alive && u.spawnedUnit.actor.flightInfo.isLanded) == count;
			}
		}

		public class GroundGroupActions : UnitGroupActionsTargetPrefs
		{
			public GroundSquad squad;

			[VTEvent("Move To", "Command the group to move to a waypoint.")]
			public void MoveTo(Waypoint wpt)
			{
				if (VTScenario.isScenarioHost)
				{
					squad.MoveTo(wpt.GetTransform());
				}
			}

			[VTEvent("Move Path", "Command the group to move along a path.")]
			public void MovePath(FollowPath path)
			{
				if (VTScenario.isScenarioHost)
				{
					squad.MovePath(path);
				}
			}

			[VTEvent("Set Formation", "Set the formation shape for this group. Units will begin to move to maintain this formation if they are not already moving.")]
			public void SetFormation(GroundSquad.GroundFormations formation)
			{
				if (VTScenario.isScenarioHost)
				{
					squad.formationType = formation;
					squad.SetBeginFormationMovement();
				}
			}

			[VTEvent("Stop", "Command all units in the group to stop.")]
			public void Stop()
			{
				if (VTScenario.isScenarioHost)
				{
					squad.StopAll();
				}
			}

			[VTEvent("Set Engage Enemies", "Set whether the entire group should engage enemies", new string[] { "Engage" })]
			public void SetEngageEnemies(bool engage)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if ((bool)unit && (bool)unit.spawnedUnit)
					{
						((AIUnitSpawn)unit.spawnedUnit).SetEngageEnemies(engage);
					}
				}
			}

			[VTEvent("Set Movement Speed", "Set the movement speed of the unit group.", new string[] { "Speed" })]
			public void SetMovementSpeed(GroundUnitSpawn.MoveSpeeds moveSpeed)
			{
				if (!VTScenario.isScenarioHost)
				{
					return;
				}
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if ((bool)unit && (bool)unit.spawnedUnit && unit.spawnedUnit is GroundUnitSpawn)
					{
						((GroundUnitSpawn)unit.spawnedUnit).SetMovementSpeed(moveSpeed);
					}
				}
			}
		}

		public Teams team;

		public GroupTypes groupType = GroupTypes.Unassigned;

		public PhoneticLetters groupID;

		public List<int> unitIDs;

		public bool syncAltSpawns;

		public int syncedAltSpawnIdx = -2;

		public object groupActions;

		private List<GameObject> cleanupObjects = new List<GameObject>();

		public void DestroyObjects()
		{
			foreach (GameObject cleanupObject in cleanupObjects)
			{
				if ((bool)cleanupObject)
				{
					UnityEngine.Object.Destroy(cleanupObject);
				}
			}
		}

		public Teams GetTeam()
		{
			return team;
		}

		public int GetEventTargetID()
		{
			return (int)(((team == Teams.Allied) ? 100 : 200) + groupID);
		}

		public void BeginScenario()
		{
			if (groupType == GroupTypes.Air)
			{
				bool flag = !VTOLMPUtils.IsMultiplayer() && team == Teams.Allied && unitIDs.Contains(VTScenario.current.units.GetPlayerSpawner().unitInstanceID);
				AIWing aIWing = new GameObject(groupID.ToString() + " Wing").AddComponent<AIWing>();
				cleanupObjects.Add(aIWing.gameObject);
				aIWing.pilots = new List<AIPilot>();
				foreach (int unitID in unitIDs)
				{
					UnitSpawner unit = VTScenario.current.units.GetUnit(unitID);
					if ((bool)unit)
					{
						AIPilot component = unit.spawnedUnit.GetComponent<AIPilot>();
						if ((bool)component)
						{
							aIWing.pilots.Add(component);
						}
					}
				}
				if (flag)
				{
					AIWing.playerWing = aIWing;
					aIWing.isPlayerWing = true;
					aIWing.doRadioComms = true;
				}
				aIWing.UpdateLeader();
				aIWing.FormOnLeader();
				((AirGroupActions)groupActions).wing = aIWing;
			}
			else if (groupType == GroupTypes.Sea)
			{
				ShipGroup shipGroup = new GameObject(groupID.ToString() + " Sea Group").AddComponent<ShipGroup>();
				cleanupObjects.Add(shipGroup.gameObject);
				foreach (int unitID2 in unitIDs)
				{
					UnitSpawner unit2 = VTScenario.current.units.GetUnit(unitID2);
					if ((bool)unit2)
					{
						ShipMover component2 = unit2.spawnedUnit.GetComponent<ShipMover>();
						if ((bool)component2)
						{
							shipGroup.AddShip(component2);
						}
					}
				}
				((SeaGroupActions)groupActions).shipGroup = shipGroup;
			}
			else
			{
				if (groupType != 0)
				{
					return;
				}
				GroundSquad groundSquad = new GameObject(groupID.ToString() + " Ground Group").AddComponent<GroundSquad>();
				cleanupObjects.Add(groundSquad.gameObject);
				foreach (int unitID3 in unitIDs)
				{
					UnitSpawner unit3 = VTScenario.current.units.GetUnit(unitID3);
					if ((bool)unit3)
					{
						GroundUnitMover component3 = unit3.spawnedUnit.GetComponent<GroundUnitMover>();
						if ((bool)component3)
						{
							component3.behavior = GroundUnitMover.Behaviors.Parked;
							groundSquad.RegisterUnit(component3);
						}
						else
						{
							Debug.LogError("Unit " + unit3.GetUIDisplayName() + " is in a ground unit group but has no GroundUnitMover!");
						}
					}
				}
				((GroundGroupActions)groupActions).squad = groundSquad;
			}
		}
	}

	public Dictionary<PhoneticLetters, UnitGroup> alliedGroups = new Dictionary<PhoneticLetters, UnitGroup>();

	public Dictionary<PhoneticLetters, UnitGroup> enemyGroups = new Dictionary<PhoneticLetters, UnitGroup>();

	public const string NODE_NAME = "UNITGROUPS";

	public const string ENEMY_NODE = "ENEMY";

	public const string ALLIED_NODE = "ALLIED";

	public static event UnitGroupDelegate OnUnitAddedToGroup;

	public void AddUnitToGroup(UnitSpawner unit, PhoneticLetters groupID)
	{
		Dictionary<PhoneticLetters, UnitGroup> groupDictionary = GetGroupDictionary(unit.team);
		if (groupDictionary.ContainsKey(groupID))
		{
			groupDictionary[groupID].unitIDs.Add(unit.unitInstanceID);
			UnitGroup unitGroup = groupDictionary[groupID];
			if (unitGroup != null && unitGroup.syncAltSpawns)
			{
				int num = 0;
				foreach (int unitID in unitGroup.unitIDs)
				{
					UnitSpawner unit2 = VTScenario.current.units.GetUnit(unitID);
					num = Mathf.Max(num, unit2.alternateSpawns.Count);
				}
				foreach (int unitID2 in unitGroup.unitIDs)
				{
					UnitSpawner unit3 = VTScenario.current.units.GetUnit(unitID2);
					if (unit3.alternateSpawns.Count < num)
					{
						for (int i = unit3.alternateSpawns.Count; i < num; i++)
						{
							UnitSpawner.AlternateSpawn alternateSpawn = new UnitSpawner.AlternateSpawn();
							alternateSpawn.position = unit3.transform.position;
							alternateSpawn.rotation = unit3.transform.rotation;
							unit3.alternateSpawns.Add(alternateSpawn);
						}
					}
				}
			}
		}
		else
		{
			UnitGroup unitGroup2 = new UnitGroup();
			unitGroup2.team = unit.team;
			unitGroup2.groupType = unit.prefabUnitSpawn.groupType;
			unitGroup2.groupID = groupID;
			unitGroup2.unitIDs = new List<int>();
			unitGroup2.unitIDs.Add(unit.unitInstanceID);
			CreateGroupActionsObj(unitGroup2);
			groupDictionary.Add(groupID, unitGroup2);
		}
		VTUnitGroup.OnUnitAddedToGroup?.Invoke(unit, groupDictionary[groupID]);
	}

	public UnitGroup GetUnitGroup(Teams team, PhoneticLetters groupID)
	{
		UnitGroup value;
		if (team == Teams.Allied)
		{
			if (alliedGroups.TryGetValue(groupID, out value))
			{
				return value;
			}
		}
		else if (enemyGroups.TryGetValue(groupID, out value))
		{
			return value;
		}
		return null;
	}

	public UnitGroup GetUnitGroup(int eventTargetID)
	{
		Teams teams = ((eventTargetID >= 200) ? Teams.Enemy : Teams.Allied);
		int groupID = eventTargetID - ((teams == Teams.Allied) ? 100 : 200);
		return GetUnitGroup(teams, (PhoneticLetters)groupID);
	}

	public void RemoveUnitFromGroups(UnitSpawner unit)
	{
		Dictionary<PhoneticLetters, UnitGroup> groupDictionary = GetGroupDictionary(unit.team);
		List<PhoneticLetters> list = new List<PhoneticLetters>();
		foreach (PhoneticLetters key in groupDictionary.Keys)
		{
			UnitGroup unitGroup = groupDictionary[key];
			unitGroup.unitIDs.RemoveAll((int x) => x == unit.unitInstanceID);
			if (unitGroup.unitIDs.Count == 0)
			{
				list.Add(key);
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		foreach (PhoneticLetters item in list)
		{
			groupDictionary.Remove(item);
		}
	}

	public List<UnitGroup> GetExistingGroups(Teams team)
	{
		Dictionary<PhoneticLetters, UnitGroup> groupDictionary = GetGroupDictionary(team);
		List<UnitGroup> list = new List<UnitGroup>();
		foreach (UnitGroup value in groupDictionary.Values)
		{
			list.Add(value);
		}
		return list;
	}

	private Dictionary<PhoneticLetters, UnitGroup> GetGroupDictionary(Teams team)
	{
		if (team == Teams.Allied)
		{
			return alliedGroups;
		}
		return enemyGroups;
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		alliedGroups = new Dictionary<PhoneticLetters, UnitGroup>();
		enemyGroups = new Dictionary<PhoneticLetters, UnitGroup>();
		if (!scenarioNode.HasNode("UNITGROUPS"))
		{
			return;
		}
		ConfigNode node = scenarioNode.GetNode("UNITGROUPS");
		if (node.HasNode("ALLIED"))
		{
			ConfigNode node2 = node.GetNode("ALLIED");
			foreach (ConfigNode.ConfigValue value in node2.GetValues())
			{
				PhoneticLetters phoneticLetters = ConfigNodeUtils.ParseEnum<PhoneticLetters>(value.name);
				UnitGroup unitGroup = new UnitGroup();
				unitGroup.team = Teams.Allied;
				unitGroup.groupID = phoneticLetters;
				unitGroup.unitIDs = ConfigNodeUtils.ParseList<int>(value.value);
				unitGroup.groupType = (GroupTypes)unitGroup.unitIDs[0];
				unitGroup.unitIDs.RemoveAt(0);
				unitGroup.unitIDs.RemoveAll((int x) => VTScenario.current.units.GetUnit(x) == null);
				CreateGroupActionsObj(unitGroup);
				alliedGroups.Add(phoneticLetters, unitGroup);
				ConfigNode node3 = node2.GetNode($"{phoneticLetters}_SETTINGS");
				if (node3 != null)
				{
					ConfigNodeUtils.TryParseValue(node3, "syncAltSpawns", ref unitGroup.syncAltSpawns);
				}
			}
		}
		if (!node.HasNode("ENEMY"))
		{
			return;
		}
		ConfigNode node4 = node.GetNode("ENEMY");
		foreach (ConfigNode.ConfigValue value2 in node4.GetValues())
		{
			PhoneticLetters phoneticLetters2 = ConfigNodeUtils.ParseEnum<PhoneticLetters>(value2.name);
			UnitGroup unitGroup2 = new UnitGroup();
			unitGroup2.team = Teams.Enemy;
			unitGroup2.groupID = phoneticLetters2;
			unitGroup2.unitIDs = ConfigNodeUtils.ParseList<int>(value2.value);
			unitGroup2.groupType = (GroupTypes)unitGroup2.unitIDs[0];
			unitGroup2.unitIDs.RemoveAt(0);
			unitGroup2.unitIDs.RemoveAll((int x) => VTScenario.current.units.GetUnit(x) == null);
			CreateGroupActionsObj(unitGroup2);
			enemyGroups.Add(phoneticLetters2, unitGroup2);
			ConfigNode node5 = node4.GetNode($"{phoneticLetters2}_SETTINGS");
			if (node5 != null)
			{
				ConfigNodeUtils.TryParseValue(node5, "syncAltSpawns", ref unitGroup2.syncAltSpawns);
			}
		}
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode configNode = new ConfigNode("UNITGROUPS");
		if (alliedGroups.Count > 0)
		{
			ConfigNode configNode2 = new ConfigNode("ALLIED");
			foreach (PhoneticLetters key in alliedGroups.Keys)
			{
				try
				{
					if (alliedGroups[key].unitIDs.Count > 0)
					{
						string text = ConfigNodeUtils.WriteList(alliedGroups[key].unitIDs);
						int groupType = (int)alliedGroups[key].groupType;
						text = groupType + ";" + text;
						configNode2.SetValue(key.ToString(), text);
						configNode2.AddNode($"{key}_SETTINGS").SetValue("syncAltSpawns", alliedGroups[key].syncAltSpawns);
					}
				}
				catch (Exception message)
				{
					Debug.LogError(message);
				}
			}
			configNode.AddNode(configNode2);
		}
		if (enemyGroups.Count > 0)
		{
			ConfigNode configNode3 = new ConfigNode("ENEMY");
			foreach (PhoneticLetters key2 in enemyGroups.Keys)
			{
				try
				{
					if (enemyGroups[key2].unitIDs.Count > 0)
					{
						string text2 = ConfigNodeUtils.WriteList(enemyGroups[key2].unitIDs);
						int groupType = (int)enemyGroups[key2].groupType;
						text2 = groupType + ";" + text2;
						configNode3.SetValue(key2.ToString(), text2);
						configNode3.AddNode($"{key2}_SETTINGS").SetValue("syncAltSpawns", enemyGroups[key2].syncAltSpawns);
					}
				}
				catch (Exception message2)
				{
					Debug.LogError(message2);
				}
			}
			configNode.AddNode(configNode3);
		}
		scenarioNode.AddNode(configNode);
	}

	public void BeginScenario()
	{
		foreach (UnitGroup value in alliedGroups.Values)
		{
			value.BeginScenario();
		}
		foreach (UnitGroup value2 in enemyGroups.Values)
		{
			value2.BeginScenario();
		}
	}

	public void DestroyAll()
	{
		foreach (UnitGroup value in alliedGroups.Values)
		{
			value.DestroyObjects();
		}
		foreach (UnitGroup value2 in enemyGroups.Values)
		{
			value2.DestroyObjects();
		}
	}

	private void CreateGroupActionsObj(UnitGroup group)
	{
		switch (group.groupType)
		{
		case GroupTypes.Ground:
		{
			UnitGroup.GroundGroupActions groundGroupActions = new UnitGroup.GroundGroupActions();
			groundGroupActions.unitGroup = group;
			group.groupActions = groundGroupActions;
			break;
		}
		case GroupTypes.Sea:
		{
			UnitGroup.SeaGroupActions seaGroupActions = new UnitGroup.SeaGroupActions();
			seaGroupActions.unitGroup = group;
			group.groupActions = seaGroupActions;
			break;
		}
		case GroupTypes.Air:
		{
			UnitGroup.AirGroupActions airGroupActions = new UnitGroup.AirGroupActions();
			airGroupActions.unitGroup = group;
			group.groupActions = airGroupActions;
			break;
		}
		case GroupTypes.Unassigned:
			break;
		}
	}

	public ConfigNode QuicksaveToNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		ConfigNode node = configNode.AddNode("allied");
		ConfigNode node2 = configNode.AddNode("enemy");
		foreach (UnitGroup value in alliedGroups.Values)
		{
			SaveGroupToNode(value, node);
		}
		foreach (UnitGroup value2 in enemyGroups.Values)
		{
			SaveGroupToNode(value2, node2);
		}
		return configNode;
	}

	private void SaveGroupToNode(UnitGroup group, ConfigNode node)
	{
		ConfigNode configNode = node.AddNode(group.groupID.ToString());
		switch (group.groupType)
		{
		case GroupTypes.Ground:
		{
			GroundSquad squad = ((UnitGroup.GroundGroupActions)group.groupActions).squad;
			configNode.SetValue("formationType", squad.formationType);
			break;
		}
		case GroupTypes.Air:
			((UnitGroup.AirGroupActions)group.groupActions).wing.OnQuicksaveGroupToNode(configNode);
			break;
		case GroupTypes.Sea:
		case GroupTypes.Unassigned:
			break;
		}
	}

	public void QuickloadFromNode(ConfigNode node)
	{
		ConfigNode node2 = node.GetNode("allied");
		ConfigNode node3 = node.GetNode("enemy");
		foreach (UnitGroup value in alliedGroups.Values)
		{
			LoadGroupFromNode(value, node2);
		}
		foreach (UnitGroup value2 in enemyGroups.Values)
		{
			LoadGroupFromNode(value2, node3);
		}
	}

	private void LoadGroupFromNode(UnitGroup group, ConfigNode node)
	{
		ConfigNode node2 = node.GetNode(group.groupID.ToString());
		switch (group.groupType)
		{
		case GroupTypes.Ground:
			((UnitGroup.GroundGroupActions)group.groupActions).squad.formationType = node2.GetValue<GroundSquad.GroundFormations>("formationType");
			break;
		case GroupTypes.Air:
			((UnitGroup.AirGroupActions)group.groupActions).wing.OnQuickloadGroupFromNode(node2);
			break;
		case GroupTypes.Sea:
		case GroupTypes.Unassigned:
			break;
		}
	}
}
