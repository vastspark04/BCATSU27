using System.Collections;
using System.Collections.Generic;
using VTOLVR.Multiplayer;

public class AIUnitSpawn : UnitSpawn, IHasTeam
{
	public enum InitialDetectionModes
	{
		Default,
		Force_Detected,
		Force_Undetected
	}

	public enum HealthComparisons
	{
		Greater_Than,
		Less_Than
	}

	public bool mpReady = true;

	public Health health;

	private IEngageEnemies[] engagers;

	[UnitSpawn("Engage Enemies")]
	public bool engageEnemies;

	public List<Actor> subUnits;

	[UnitSpawn("Detection Mode")]
	public InitialDetectionModes detectionMode;

	[UnitSpawn("Spawn Immediately")]
	public bool spawnOnStart = true;

	[UnitSpawn("Invincible")]
	public bool invincible;

	[UnitSpawnTooltip("Set whether this non-combat unit should be automatically engaged by enemies as if it were a combat unit.")]
	[UnitSpawnAttributeConditional("IsNonTarget")]
	[UnitSpawn("Combat Target")]
	public bool combatTarget;

	private ITargetPreferences[] targetPreferences;

	private List<Actor> nonTargets = new List<Actor>();

	private List<Actor> priorityTargets = new List<Actor>();

	public bool qsSpawned { get; set; }

	[VTEvent("Set Engage Enemies", "Set whether the unit should engage enemies.", new string[] { "Engage" })]
	public void SetEngageEnemies(bool engage)
	{
		if (actor.alive && VTScenario.isScenarioHost)
		{
			engageEnemies = engage;
			for (int i = 0; i < engagers.Length; i++)
			{
				engagers[i].SetEngageEnemies(engage);
			}
			OnSetEngageEnemies(engage);
		}
	}

	protected virtual void OnSetEngageEnemies(bool engage)
	{
	}

	public bool IsNonTarget()
	{
		return actor.role == Actor.Roles.None;
	}

	[VTEvent("Spawn Unit", "Spawn the unit if it hasn't already been spawned")]
	public void SpawnUnit()
	{
		if ((bool)unitSpawner && !unitSpawner.spawned)
		{
			unitSpawner.SpawnUnit();
		}
	}

	[VTEvent("Destroy", "This event destroys the unit immediately.")]
	public void DestroySelf()
	{
		if (VTScenario.isScenarioHost)
		{
			health.Damage(health.maxHealth + 1f, base.transform.position, Health.DamageTypes.Impact, null, "Destroyed by event action.");
		}
	}

	public override void OnPreSpawnUnit()
	{
		base.OnPreSpawnUnit();
		if (actor == null)
		{
			actor = GetComponent<Actor>();
		}
		engagers = base.gameObject.GetComponentsInChildrenImplementing<IEngageEnemies>(includeInactive: true);
		for (int i = 0; i < engagers.Length; i++)
		{
			engagers[i].SetEngageEnemies(engageEnemies);
		}
		actor.unitSpawn = this;
		if (!VTOLMPUtils.IsMultiplayer())
		{
			if (actor.team == Teams.Allied)
			{
				actor.discovered = detectionMode != InitialDetectionModes.Force_Undetected;
			}
			else
			{
				actor.discovered = detectionMode == InitialDetectionModes.Force_Detected;
			}
		}
		else
		{
			actor.discovered = false;
		}
		actor.detectionMode = detectionMode;
		if (invincible)
		{
			SetInvincible(i: true);
		}
	}

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		if (IsNonTarget())
		{
			actor.overrideCombatTarget = combatTarget;
		}
		SetEngageEnemies(engageEnemies);
		if (VTOLMPUtils.IsMultiplayer())
		{
			StartCoroutine(MPSpawnDiscoverRoutine());
		}
	}

	private IEnumerator MPSpawnDiscoverRoutine()
	{
		PlayerInfo localPlayer = VTOLMPLobbyManager.localPlayerInfo;
		while (!localPlayer.chosenTeam)
		{
			yield return null;
		}
		if (actor.team == localPlayer.team)
		{
			if (detectionMode != InitialDetectionModes.Force_Undetected)
			{
				actor.DiscoverActor();
			}
		}
		else if (detectionMode == InitialDetectionModes.Force_Detected)
		{
			actor.DiscoverActor();
		}
	}

	[VTEvent("Set Invincible", "Sets whether the unit can take damage.", new string[] { "Invincible" })]
	public void SetInvincible(bool i)
	{
		if (!VTScenario.isScenarioHost)
		{
			return;
		}
		invincible = i;
		Health[] componentsInChildren = GetComponentsInChildren<Health>();
		foreach (Health health in componentsInChildren)
		{
			if (!health.GetComponent<Missile>())
			{
				health.invincible = i;
			}
		}
	}

	private void GetTargetPreferences()
	{
		if (targetPreferences == null)
		{
			targetPreferences = base.gameObject.GetComponentsInChildrenImplementing<ITargetPreferences>();
		}
	}

	[UnitSpawnAttributeConditional("IsNonAirTargetPreferences")]
	[VTEvent("Set Non-Targets", "Set the list of units that this unit will not attack. (Overwrites existing list)", new string[] { "Non-targets" })]
	public void SetNonTargets(UnitReferenceListOtherSubs nonTargets)
	{
		GetTargetPreferences();
		this.nonTargets.Clear();
		foreach (UnitReference unit in nonTargets.units)
		{
			if ((bool)unit.GetActor())
			{
				this.nonTargets.Add(unit.GetActor());
			}
		}
		ITargetPreferences[] array = targetPreferences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetNonTargets(nonTargets);
		}
	}

	[UnitSpawnAttributeConditional("IsNonAirTargetPreferences")]
	[VTEvent("Add Non-Targets", "Add units to the list of units that this unit will not attack.", new string[] { "Non-targets" })]
	public void AddNonTargets(UnitReferenceListOtherSubs nonTargets)
	{
		GetTargetPreferences();
		foreach (UnitReference unit in nonTargets.units)
		{
			if ((bool)unit.GetActor())
			{
				this.nonTargets.Add(unit.GetActor());
			}
		}
		ITargetPreferences[] array = targetPreferences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AddNonTargets(nonTargets);
		}
	}

	[UnitSpawnAttributeConditional("IsNonAirTargetPreferences")]
	[VTEvent("Remove Non-Targets", "Remove units from the list of units that this unit will not attack.", new string[] { "Non-targets" })]
	public void RemoveNonTargets(UnitReferenceListOtherSubs nonTargets)
	{
		GetTargetPreferences();
		foreach (UnitReference unit in nonTargets.units)
		{
			if ((bool)unit.GetActor())
			{
				this.nonTargets.Remove(unit.GetActor());
			}
		}
		ITargetPreferences[] array = targetPreferences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].RemoveNonTargets(nonTargets);
		}
	}

	[UnitSpawnAttributeConditional("IsNonAirTargetPreferences")]
	[VTEvent("Clear Non-Targets", "Clear the list of units that this unit will not attack.")]
	public void ClearNonTargets()
	{
		GetTargetPreferences();
		nonTargets.Clear();
		ITargetPreferences[] array = targetPreferences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ClearNonTargets();
		}
	}

	[UnitSpawnAttributeConditional("IsNonAirTargetPreferences")]
	[VTEvent("Set Priority Targets", "Set the list of units that this unit will prioritize when finding a target. (Overwrites existing list)", new string[] { "Targets" })]
	public void SetPriorityTargets(UnitReferenceListOtherSubs targets)
	{
		GetTargetPreferences();
		priorityTargets.Clear();
		foreach (UnitReference unit in targets.units)
		{
			if ((bool)unit.GetActor())
			{
				priorityTargets.Add(unit.GetActor());
			}
		}
		ITargetPreferences[] array = targetPreferences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetPriorityTargets(targets);
		}
	}

	[UnitSpawnAttributeConditional("IsNonAirTargetPreferences")]
	[VTEvent("Add Priority Targets", "Add units to the list of units that this unit will prioritize when finding a target", new string[] { "Targets" })]
	public void AddPriorityTargets(UnitReferenceListOtherSubs targets)
	{
		GetTargetPreferences();
		foreach (UnitReference unit in targets.units)
		{
			if ((bool)unit.GetActor())
			{
				priorityTargets.Add(unit.GetActor());
			}
		}
		ITargetPreferences[] array = targetPreferences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AddPriorityTargets(targets);
		}
	}

	[UnitSpawnAttributeConditional("IsNonAirTargetPreferences")]
	[VTEvent("Remove Priority Targets", "Remove units from the list of units that this unit will prioritize when finding a target", new string[] { "Targets" })]
	public void RemovePriorityTargets(UnitReferenceListOtherSubs targets)
	{
		GetTargetPreferences();
		foreach (UnitReference unit in targets.units)
		{
			if ((bool)unit.GetActor())
			{
				priorityTargets.Remove(unit.GetActor());
			}
		}
		ITargetPreferences[] array = targetPreferences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].RemovePriorityTargets(targets);
		}
	}

	[UnitSpawnAttributeConditional("IsNonAirTargetPreferences")]
	[VTEvent("Clear Priority Targets", "Clear the list of units that this unit will prioritize when finding a target")]
	public void ClearPriorityTargets()
	{
		GetTargetPreferences();
		priorityTargets.Clear();
		ITargetPreferences[] array = targetPreferences;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ClearPriorityTargets();
		}
	}

	public bool IsNonAirTargetPreferences()
	{
		if (this is AIAircraftSpawn)
		{
			return false;
		}
		GetTargetPreferences();
		return targetPreferences.Length != 0;
	}

	[SCCUnitProperty("Alive", true)]
	public bool SC_IsAlive()
	{
		if ((bool)actor)
		{
			return actor.alive;
		}
		return false;
	}

	[SCCUnitProperty("Health Percent", new string[] { "Comparison", "Value" }, false)]
	public bool SC_HealthLevel(HealthComparisons comparison, [VTRangeParam(0f, 100f)] float percent)
	{
		float num = percent / 100f;
		if ((bool)health)
		{
			if (comparison == HealthComparisons.Greater_Than)
			{
				return health.normalizedHealth > num;
			}
			return health.normalizedHealth < num;
		}
		return false;
	}

	[SCCUnitProperty("Near Waypoint", new string[] { "Waypoint", "Radius" }, true)]
	public bool SCC_NearWaypoint(Waypoint wpt, [VTRangeParam(10f, 200000f)] float radius)
	{
		if ((bool)actor && actor.alive && actor.gameObject.activeInHierarchy && wpt.GetTransform() != null)
		{
			return (actor.position - wpt.worldPosition).sqrMagnitude < radius * radius;
		}
		return false;
	}

	[SCCUnitProperty("Detected by", new string[] { "Team" }, true)]
	public bool SC_DetectedBy(Teams d_team)
	{
		if (d_team == Teams.Allied)
		{
			return actor.detectedByAllied;
		}
		return actor.detectedByEnemy;
	}

	public Teams GetTeam()
	{
		return actor.team;
	}

	public override void Quicksave(ConfigNode qsNode)
	{
		base.Quicksave(qsNode);
		qsNode.SetValue("engageEnemies", engageEnemies);
		qsNode.SetValue("invincible", invincible);
		for (int i = 0; i < subUnits.Count; i++)
		{
			ConfigNode configNode = qsNode.AddNode("subUnit");
			configNode.SetValue("idx", i);
			configNode.SetValue("alive", subUnits[i].alive);
		}
		GetTargetPreferences();
		if (targetPreferences.Length == 0)
		{
			return;
		}
		ConfigNode configNode2 = qsNode.AddNode("NON_TARGETS");
		foreach (Actor nonTarget in nonTargets)
		{
			configNode2.AddNode(QuicksaveManager.SaveActorIdentifierToNode(nonTarget, "NON_TARGET"));
		}
		ConfigNode configNode3 = qsNode.AddNode("PRIORITY_TARGETS");
		foreach (Actor priorityTarget in priorityTargets)
		{
			configNode3.AddNode(QuicksaveManager.SaveActorIdentifierToNode(priorityTarget, "PRIORITY_TARGET"));
		}
	}

	public override void Quickload(ConfigNode qsNode)
	{
		base.Quickload(qsNode);
		bool value = qsNode.GetValue<bool>("engageEnemies");
		SetEngageEnemies(value);
		bool value2 = qsNode.GetValue<bool>("invincible");
		SetInvincible(value2);
		if (subUnits.Count > 0)
		{
			foreach (ConfigNode node in qsNode.GetNodes("subUnit"))
			{
				int value3 = node.GetValue<int>("idx");
				if (value3 < subUnits.Count)
				{
					Actor actor = subUnits[value3];
					if (!node.GetValue<bool>("alive") && (bool)actor.health)
					{
						actor.health.QS_Kill();
					}
				}
			}
		}
		if (!qsNode.HasNode("NON_TARGETS"))
		{
			return;
		}
		GetTargetPreferences();
		UnitReferenceListOtherSubs unitReferenceListOtherSubs = new UnitReferenceListOtherSubs();
		foreach (ConfigNode node2 in qsNode.GetNode("NON_TARGETS").GetNodes("NON_TARGET"))
		{
			Actor actor2 = QuicksaveManager.RetrieveActorFromNode(node2);
			if ((bool)actor2 && (bool)actor2.unitSpawn)
			{
				unitReferenceListOtherSubs.units.Add(new UnitReference(actor2.unitSpawn.unitID));
			}
		}
		UnitReferenceListOtherSubs unitReferenceListOtherSubs2 = new UnitReferenceListOtherSubs();
		foreach (ConfigNode node3 in qsNode.GetNode("PRIORITY_TARGETS").GetNodes("PRIORITY_TARGET"))
		{
			Actor actor3 = QuicksaveManager.RetrieveActorFromNode(node3);
			if ((bool)actor3 && (bool)actor3.unitSpawn)
			{
				unitReferenceListOtherSubs2.units.Add(new UnitReference(actor3.unitSpawn.unitID));
			}
		}
		SetNonTargets(unitReferenceListOtherSubs);
		SetPriorityTargets(unitReferenceListOtherSubs2);
	}
}
