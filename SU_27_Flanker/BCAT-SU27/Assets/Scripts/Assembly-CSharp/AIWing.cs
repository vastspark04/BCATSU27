using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIWing : MonoBehaviour
{
	private struct PilotCandidate
	{
		public AIPilot pilot;

		public int points;

		public PilotCandidate(AIPilot pilot, int points)
		{
			this.pilot = pilot;
			this.points = points;
		}
	}

	public enum DetectionMethods
	{
		Visual,
		Radar,
		RWR
	}

	[Serializable]
	public class AIWingTarget
	{
		public Actor actor;

		public Actor detectedBy;

		public float timeFound;

		public int numEngaging;

		public int missilesOnTarget;

		public DetectionMethods detectionMethod;
	}

	public static AIWing playerWing;

	public bool isPlayerWing;

	public List<AIPilot> pilots = new List<AIPilot>();

	private AIPilot leader;

	public int maxMissilePerTarget = 1;

	private List<AIWingTarget> targets = new List<AIWingTarget>();

	private List<Actor> targetsList = new List<Actor>();

	private bool wingActive = true;

	private AIPilot.CommandStates commandState;

	private Transform orbitTransform;

	private FollowPath navPath;

	private bool _doRadioComms;

	private List<Actor> designatedTargets = new List<Actor>();

	private static List<Actor> gtvBuffer = new List<Actor>();

	public bool doRadioComms
	{
		get
		{
			return _doRadioComms;
		}
		set
		{
			_doRadioComms = value;
			for (int i = 0; i < pilots.Count; i++)
			{
				AIPilot aIPilot = pilots[i];
				if ((bool)aIPilot)
				{
					if ((bool)aIPilot.aiSpawn && aIPilot.aiSpawn is AIAWACSSpawn)
					{
						((AIAWACSSpawn)aIPilot.aiSpawn).commsEnabled = _doRadioComms;
					}
					else
					{
						aIPilot.doRadioComms = value;
					}
				}
			}
		}
	}

	private void OnEnable()
	{
		StartCoroutine(RefreshTargetsRoutine());
		StartCoroutine(CollectTargetsRoutine());
	}

	private void Start()
	{
		if (pilots == null || pilots.Count == 0)
		{
			Debug.LogWarning("AIWing was started with no pilots.");
			return;
		}
		if (isPlayerWing)
		{
			playerWing = this;
		}
		leader = pilots[0];
		foreach (AIPilot pilot in pilots)
		{
			pilot.aiWing = this;
		}
	}

	public void UpdateLeader()
	{
		if (leader == null || !leader.isAlive)
		{
			pilots.RemoveAll((AIPilot x) => x == null || !x.isAlive);
			AIPilot aIPilot = leader;
			if (pilots.Count > 0)
			{
				SetLeader(0);
				wingActive = true;
			}
			if ((bool)aIPilot)
			{
				for (int i = 0; i < pilots.Count; i++)
				{
					if (pilots[i] != leader && pilots[i].formationLeader == aIPilot.formationComponent)
					{
						pilots[i].formationLeader = leader.formationComponent;
					}
				}
			}
			if (leader == null)
			{
				wingActive = false;
				return;
			}
			if (leader.commandState == AIPilot.CommandStates.Orbit || leader.commandState == AIPilot.CommandStates.Navigation)
			{
				FormOnPilot(leader);
			}
			if (commandState == AIPilot.CommandStates.Orbit && orbitTransform != null)
			{
				OrbitTransform(orbitTransform);
			}
			else if (commandState == AIPilot.CommandStates.Navigation && navPath != null)
			{
				FlyNavPath(navPath);
			}
		}
		base.transform.position = leader.transform.position;
	}

	private void Update()
	{
		if (wingActive)
		{
			UpdateLeader();
		}
	}

	private void SetLeader(int idx)
	{
		if (pilots.Count < 1)
		{
			leader = null;
			return;
		}
		leader = pilots[idx];
		for (int i = 0; i < pilots.Count; i++)
		{
			if (leader.formationLeader == pilots[i].formationComponent)
			{
				leader.formationLeader = null;
			}
		}
	}

	public void FormOnPlayer(bool checkPlayerCommand = false)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			if (!checkPlayerCommand || pilots[i].allowPlayerCommands)
			{
				pilots[i].FormOnPlayer();
			}
		}
	}

	public void FormOnLeader()
	{
		if (isPlayerWing)
		{
			FormOnPlayer();
			return;
		}
		for (int i = 0; i < pilots.Count; i++)
		{
			if (pilots[i] != leader)
			{
				pilots[i].FormOnPilot(leader);
			}
		}
	}

	public void OrbitTransform(Transform orbitTf, bool checkPlayerCommand = false)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			if (!checkPlayerCommand || pilots[i].allowPlayerCommands)
			{
				if (pilots[i] == leader)
				{
					pilots[i].OrbitTransform(orbitTf);
					continue;
				}
				pilots[i].FormOnPilot(leader);
				pilots[i].orbitTransform = orbitTf;
			}
		}
		commandState = AIPilot.CommandStates.Orbit;
		orbitTransform = orbitTf;
	}

	public void SetOrbitRadius(float radius)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			pilots[i].orbitRadius = radius;
		}
	}

	public void SetFallbackOrbitTransform(Transform tf)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			pilots[i].SetFallbackOrbitTransform(tf);
		}
	}

	public void SetForceReturnRadius(float radius)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			pilots[i].forceReturnDistance = radius;
		}
	}

	public void FlyNavPath(FollowPath path)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			if (pilots[i] == leader)
			{
				pilots[i].FlyNavPath(path);
				continue;
			}
			pilots[i].FormOnPilot(leader);
			pilots[i].navPath = path;
		}
		commandState = AIPilot.CommandStates.Navigation;
		navPath = path;
	}

	public void LandAtAirport(AirportManager airport)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			pilots[i].LandAtAirport(airport);
		}
	}

	public void FormOnPilot(AIPilot pilot)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			pilots[i].FormOnPilot(pilot);
		}
	}

	public void SetAutoEngageEnemies(bool autoEngage)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			if ((bool)pilots[i].aiSpawn)
			{
				pilots[i].aiSpawn.SetEngageEnemies(autoEngage);
			}
			else
			{
				pilots[i].SetEngageEnemies(autoEngage);
			}
		}
	}

	public void SetNavSpeed(float speed)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			pilots[i].navSpeed = speed;
		}
	}

	public void SetDefaultAltitude(float altitude)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			pilots[i].defaultAltitude = altitude;
		}
	}

	public void OrderAllAttackTarget(Actor tgt)
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			pilots[i].OrderAttackTarget(tgt);
		}
	}

	public void CancelAllAttackOrder()
	{
		for (int i = 0; i < pilots.Count; i++)
		{
			pilots[i].CancelAttackOrder();
		}
	}

	public void ReportTarget(Actor detectedBy, Actor target, DetectionMethods detectionMethod)
	{
		if (!target)
		{
			return;
		}
		for (int i = 0; i < targets.Count; i++)
		{
			if (targets[i].actor == target)
			{
				targets[i].timeFound = Time.time;
				if (targets[i].detectionMethod == DetectionMethods.RWR)
				{
					targets[i].detectionMethod = detectionMethod;
				}
				return;
			}
		}
		AIWingTarget aIWingTarget = new AIWingTarget();
		aIWingTarget.detectedBy = detectedBy;
		aIWingTarget.actor = target;
		aIWingTarget.timeFound = Time.time;
		aIWingTarget.detectionMethod = detectionMethod;
		targets.Add(aIWingTarget);
		targetsList.Add(target);
	}

	private IEnumerator RefreshTargetsRoutine()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 3f));
		while (base.enabled)
		{
			yield return new WaitForSeconds(3f);
			targets.RemoveAll((AIWingTarget x) => x.actor == null || (x.numEngaging < 1 && Time.time - x.timeFound > 60f) || !x.actor.alive);
			targetsList.Clear();
			for (int i = 0; i < targets.Count; i++)
			{
				targetsList.Add(targets[i].actor);
			}
		}
	}

	private IEnumerator CollectTargetsRoutine()
	{
		while (base.enabled)
		{
			yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.5f));
			for (int i = 0; i < pilots.Count; i++)
			{
				if ((bool)pilots[i].targetFinder && (bool)pilots[i].targetFinder.attackingTarget)
				{
					foreach (Actor item in pilots[i].targetFinder.targetsSeen)
					{
						ReportTarget(pilots[i].actor, item, DetectionMethods.Visual);
					}
				}
				if ((bool)pilots[i].detectionRadar && pilots[i].detectionRadar.radarEnabled)
				{
					foreach (Actor detectedUnit in pilots[i].detectionRadar.detectedUnits)
					{
						ReportTarget(pilots[i].actor, detectedUnit, DetectionMethods.Radar);
					}
				}
				if ((bool)pilots[i].wm && (bool)pilots[i].wm.opticalTargeter && (bool)pilots[i].wm.opticalTargeter.lockedActor)
				{
					ReportTarget(pilots[i].actor, pilots[i].wm.opticalTargeter.lockedActor, DetectionMethods.Visual);
				}
				if (!pilots[i].moduleRWR)
				{
					continue;
				}
				for (int j = 0; j < pilots[i].moduleRWR.maxContacts; j++)
				{
					ModuleRWR.RWRContact rWRContact = pilots[i].moduleRWR.contacts[j];
					if (rWRContact.active && rWRContact.radarActor.team != pilots[i].actor.team)
					{
						ReportTarget(pilots[i].actor, rWRContact.radarActor, DetectionMethods.RWR);
					}
				}
			}
		}
	}

	public void AddDesignatedTarget(Actor a)
	{
		if (a != null && !designatedTargets.Contains(a))
		{
			designatedTargets.Add(a);
		}
	}

	public void ClearDesignatedTargets()
	{
		designatedTargets.Clear();
	}

	public Actor RequestDesignatedTarget(AIPilot requester)
	{
		designatedTargets.RemoveAll((Actor x) => x == null || !x.alive);
		return RequestTarget(requester, designatedTargets, null, isTgtList: false, allowUndetected: true);
	}

	public Actor RequestTarget(AIPilot requester, List<Actor> priorityTargets, List<Actor> nonTargets)
	{
		if (priorityTargets != null)
		{
			Actor actor = RequestTarget(requester, priorityTargets, nonTargets, isTgtList: false);
			if ((bool)actor)
			{
				return actor;
			}
		}
		return RequestTarget(requester, targetsList, nonTargets, isTgtList: true, allowUndetected: true);
	}

	private Actor RequestTarget(AIPilot requester, List<Actor> actorList, List<Actor> nonTargets, bool isTgtList, bool allowUndetected = false)
	{
		Actor actor = null;
		Actor actor2 = null;
		Actor actor3 = null;
		Actor actor4 = null;
		int num = 99;
		Actor actor5 = null;
		float num2 = -1f;
		Actor actor6 = null;
		float num3 = float.MaxValue;
		int num4 = UnityEngine.Random.Range(0, actorList.Count);
		for (int i = 0; i < actorList.Count; i++, num4 = (num4 + 1) % actorList.Count)
		{
			Actor actor7 = actorList[num4];
			if (actor7 == null || !actor7 || !actor7.alive || !actor7.gameObject.activeInHierarchy || actor7.team == requester.actor.team)
			{
				continue;
			}
			AIWingTarget aIWingTarget = null;
			if (isTgtList)
			{
				aIWingTarget = targets[num4];
			}
			else
			{
				if (!allowUndetected)
				{
					if (requester.actor.team == Teams.Allied)
					{
						if (!actor7.detectedByAllied)
						{
							continue;
						}
					}
					else if (!actor7.detectedByEnemy)
					{
						continue;
					}
				}
				int num5 = targetsList.IndexOf(actor7);
				if (num5 >= 0)
				{
					aIWingTarget = targets[num5];
				}
			}
			if (actor7.finalCombatRole == Actor.Roles.Missile || (actor7.finalCombatRole != Actor.Roles.Air && requester.combatRole == AIPilot.CombatRoles.Fighter) || (actor7.finalCombatRole == Actor.Roles.Air && requester.combatRole == AIPilot.CombatRoles.Bomber) || (actor7.finalCombatRole == Actor.Roles.Air && !requester.wm.availableWeaponTypes.aam && !requester.wm.availableWeaponTypes.gun) || (actor7.finalCombatRole == Actor.Roles.Ship && !requester.wm.availableWeaponTypes.antiShip && (!actor7.hasRadar || !requester.wm.availableWeaponTypes.antirad) && !requester.wm.availableWeaponTypes.bomb) || ((actor7.finalCombatRole == Actor.Roles.Ground || actor7.finalCombatRole == Actor.Roles.GroundArmor) && !requester.wm.availableWeaponTypes.agm && !requester.wm.availableWeaponTypes.bomb && !requester.wm.availableWeaponTypes.rocket && !requester.wm.availableWeaponTypes.gun) || (nonTargets != null && AIPilot.IsNonTarget(nonTargets, actor7)) || actor7.finalCombatRole == Actor.Roles.None)
			{
				continue;
			}
			Vector3 vector = actor7.position - requester.transform.position;
			float sqrMagnitude = vector.sqrMagnitude;
			if (isTgtList && aIWingTarget != null && aIWingTarget.detectionMethod == DetectionMethods.RWR && sqrMagnitude > Mathf.Pow(requester.targetFinder.visionRadius * 2f, 2f))
			{
				if (actor7.finalCombatRole == Actor.Roles.Air)
				{
					if (requester.combatRole == AIPilot.CombatRoles.Fighter)
					{
						if (sqrMagnitude > Mathf.Pow(requester.wm.maxAntiAirRange + 10000f, 2f))
						{
							continue;
						}
					}
					else if (requester.combatRole != AIPilot.CombatRoles.FighterAttack || sqrMagnitude > Mathf.Pow(requester.wm.maxAntiAirRange + 5000f, 2f))
					{
						continue;
					}
				}
				else if (requester.combatRole == AIPilot.CombatRoles.Fighter || !requester.wm.availableWeaponTypes.antirad || sqrMagnitude > Mathf.Pow(requester.wm.maxAntiRadRange + 5000f, 2f))
				{
					continue;
				}
			}
			if (sqrMagnitude < num3 && CheckRolePriority(actor7, actor6, requester))
			{
				num3 = sqrMagnitude;
				actor6 = actor7;
			}
			float num6 = Vector3.Dot(vector.normalized, requester.transform.forward);
			if (num6 > num2 && CheckRolePriority(actor7, actor5, requester))
			{
				num2 = num6;
				actor5 = actor7;
			}
			if (aIWingTarget != null && aIWingTarget.numEngaging < num && CheckRolePriority(actor7, actor4, requester))
			{
				num = aIWingTarget.numEngaging;
				actor4 = actor7;
			}
			if (actor7.currentlyTargetingActor != null)
			{
				if (actor3 == null)
				{
					actor3 = actor7;
				}
				if (actor2 == null)
				{
					for (int j = 0; j < pilots.Count; j++)
					{
						if (pilots[j].actor == actor7.currentlyTargetingActor)
						{
							actor2 = actor7;
							j = pilots.Count;
						}
					}
					if (actor2 == null && isPlayerWing && actor7.currentlyTargetingActor == FlightSceneManager.instance.playerActor)
					{
						actor2 = actor7;
					}
				}
				if (actor != null && actor7.currentlyTargetingActor == requester.actor)
				{
					actor = actor7;
				}
			}
			else
			{
				if (!(actor7 == FlightSceneManager.instance.playerActor))
				{
					continue;
				}
				WeaponManager weaponManager = actor7.weaponManager;
				if (!weaponManager)
				{
					continue;
				}
				Actor actor8 = null;
				if ((bool)weaponManager.lockingRadar && weaponManager.lockingRadar.IsLocked())
				{
					actor8 = weaponManager.lockingRadar.currentLock.actor;
				}
				if (!actor8)
				{
					continue;
				}
				if (actor3 == null)
				{
					actor3 = actor7;
				}
				if (actor2 == null)
				{
					for (int k = 0; k < pilots.Count; k++)
					{
						if (pilots[k].actor == actor8)
						{
							actor2 = actor7;
							k = pilots.Count;
						}
					}
				}
				if (actor != null && actor8 == requester.actor)
				{
					actor = actor7;
				}
			}
		}
		Actor actor9 = null;
		actor9 = actor6;
		if (CheckRolePriority(actor5, actor9, requester))
		{
			actor9 = actor5;
		}
		if (CheckRolePriority(actor4, actor9, requester))
		{
			actor9 = actor4;
		}
		if (CheckRolePriority(actor3, actor9, requester))
		{
			actor9 = actor3;
		}
		if (CheckRolePriority(actor2, actor9, requester))
		{
			actor9 = actor2;
		}
		if (CheckRolePriority(actor, actor9, requester))
		{
			actor9 = actor;
		}
		if (actor9 != null)
		{
			return actor9;
		}
		return null;
	}

	private bool CheckRolePriority(Actor potentialTarget, Actor previousTarget, AIPilot requester)
	{
		if (previousTarget == null)
		{
			return true;
		}
		if (potentialTarget == null)
		{
			return false;
		}
		if (requester.combatRole == AIPilot.CombatRoles.Attack)
		{
			if (potentialTarget.finalCombatRole != Actor.Roles.Air)
			{
				return true;
			}
			if (previousTarget.finalCombatRole == Actor.Roles.Air)
			{
				return true;
			}
			return false;
		}
		if (requester.combatRole == AIPilot.CombatRoles.FighterAttack)
		{
			if (potentialTarget.finalCombatRole == Actor.Roles.Air)
			{
				return true;
			}
			if (previousTarget.finalCombatRole != Actor.Roles.Air)
			{
				return true;
			}
			return false;
		}
		return true;
	}

	public void ReportEngageTarget(Actor target)
	{
		for (int i = 0; i < targets.Count; i++)
		{
			AIWingTarget aIWingTarget = targets[i];
			if (aIWingTarget.actor == target)
			{
				aIWingTarget.numEngaging++;
				aIWingTarget.timeFound = Time.time;
				targets[i] = aIWingTarget;
				break;
			}
		}
	}

	public void ReportDisengageTarget(Actor target)
	{
		for (int i = 0; i < targets.Count; i++)
		{
			AIWingTarget aIWingTarget = targets[i];
			if (aIWingTarget.actor == target)
			{
				aIWingTarget.numEngaging = Mathf.Max(0, aIWingTarget.numEngaging - 1);
				targets[i] = aIWingTarget;
				break;
			}
		}
	}

	public void ReportMissileOnTarget(Actor firedBy, Actor target, Missile m)
	{
		if (target == null && m == null)
		{
			return;
		}
		ReportTarget(firedBy, target, DetectionMethods.Visual);
		foreach (AIWingTarget target2 in targets)
		{
			if (target2.actor == target)
			{
				StartCoroutine(MissileOnTargetRoutine(target2, m));
				break;
			}
		}
	}

	private IEnumerator MissileOnTargetRoutine(AIWingTarget target, Missile m)
	{
		target.missilesOnTarget++;
		yield return null;
		while ((bool)m && !m.fired)
		{
			yield return null;
		}
		yield return new WaitForSeconds(1f);
		while ((bool)m && (m.hasTarget || m.guidanceMode == Missile.GuidanceModes.Bomb))
		{
			yield return null;
		}
		target.missilesOnTarget--;
	}

	public int GetNumMissilesOnTarget(Actor target)
	{
		foreach (AIWingTarget target2 in targets)
		{
			if (target2.actor == target)
			{
				return target2.missilesOnTarget;
			}
		}
		return 0;
	}

	public int AttackTarget(Actor target, List<AIPilot> overridePilotList = null)
	{
		Debug.LogFormat("Player commanded wingmen to attack {0}", target.DebugName());
		List<PilotCandidate> list = new List<PilotCandidate>();
		if (overridePilotList == null)
		{
			overridePilotList = pilots;
		}
		foreach (AIPilot overridePilot in overridePilotList)
		{
			if (!overridePilot || !overridePilot.actor)
			{
				continue;
			}
			if (!overridePilot.actor.alive)
			{
				Debug.LogFormat("- Deny: {0} is dead", overridePilot.actor.DebugName());
				continue;
			}
			if (!overridePilot.allowPlayerCommands)
			{
				Debug.LogFormat("- Deny: {0} is not allowed to be commanded", overridePilot.actor.DebugName());
				continue;
			}
			if (!overridePilot.wm)
			{
				Debug.LogFormat("- Deny: {0} has no weapon manager", overridePilot.actor.DebugName());
				continue;
			}
			if (overridePilot.autoPilot.flightInfo.isLanded)
			{
				Debug.LogFormat("- Deny: {0} is not airborne", overridePilot.actor.DebugName());
				continue;
			}
			if (overridePilot.commandState == AIPilot.CommandStates.AirRefuel && (bool)overridePilot.fuelTank && overridePilot.fuelTank.fuelFraction < 0.5f)
			{
				Debug.LogFormat("- Deny: {0} is low fuel and refueling", overridePilot.actor.DebugName());
				continue;
			}
			if (overridePilot.commandState == AIPilot.CommandStates.Override)
			{
				Debug.LogFormat("- Deny: {0} is in an override commandState", overridePilot.actor.DebugName());
				continue;
			}
			int num = 0;
			if (target.finalCombatRole == Actor.Roles.Air)
			{
				if (overridePilot.wm.availableWeaponTypes.aam)
				{
					num += 100;
				}
				if (overridePilot.wm.availableWeaponTypes.gun)
				{
					num += 50;
				}
				if (num > 0)
				{
					if (overridePilot.combatRole == AIPilot.CombatRoles.Fighter)
					{
						num += 100;
					}
					else if (overridePilot.combatRole == AIPilot.CombatRoles.FighterAttack)
					{
						num += 75;
					}
					else if (overridePilot.combatRole == AIPilot.CombatRoles.Attack)
					{
						num += 25;
					}
				}
			}
			else if (target.finalCombatRole == Actor.Roles.Ship)
			{
				if (overridePilot.wm.availableWeaponTypes.antiShip)
				{
					num += 100;
				}
				if (target.hasRadar && overridePilot.wm.availableWeaponTypes.antirad)
				{
					num += 50;
				}
				if (overridePilot.wm.availableWeaponTypes.bomb)
				{
					float num2 = -1f;
					foreach (HPEquippable combinedEquip in overridePilot.wm.GetCombinedEquips())
					{
						if (combinedEquip is HPEquipBombRack || combinedEquip is HPEquipLaserBombRack || combinedEquip is HPEquipGPSBombRack)
						{
							MissileLauncher ml = ((HPEquipMissileLauncher)combinedEquip).ml;
							if ((bool)ml && ml.missileCount > 0)
							{
								num2 = Mathf.Max(num2, ml.missilePrefab.GetComponent<Missile>().explodeDamage);
							}
						}
					}
					if (num2 > target.healthMinDamage)
					{
						num += Mathf.CeilToInt(Mathf.Min(num2 - target.healthMinDamage, 75f));
					}
				}
			}
			else if (target.finalCombatRole == Actor.Roles.Ground)
			{
				if (overridePilot.wm.availableWeaponTypes.agm)
				{
					num += 100;
				}
				if (overridePilot.wm.availableWeaponTypes.rocket)
				{
					num += 75;
				}
				if (overridePilot.wm.availableWeaponTypes.bomb)
				{
					num += 50;
				}
				if (overridePilot.wm.availableWeaponTypes.gun)
				{
					num += 30;
				}
				if (target.hasRadar && overridePilot.wm.availableWeaponTypes.antirad)
				{
					num += 250;
				}
				if (num > 0)
				{
					if (overridePilot.combatRole == AIPilot.CombatRoles.Attack)
					{
						num += 100;
					}
					else if (overridePilot.combatRole == AIPilot.CombatRoles.FighterAttack)
					{
						num += 25;
					}
					else if (overridePilot.combatRole == AIPilot.CombatRoles.Bomber)
					{
						num += 50;
					}
				}
			}
			else if (target.finalCombatRole == Actor.Roles.GroundArmor)
			{
				if (overridePilot.wm.availableWeaponTypes.agm)
				{
					num += 75;
				}
				if (overridePilot.wm.availableWeaponTypes.rocket)
				{
					num += 75;
				}
				if (overridePilot.wm.availableWeaponTypes.bomb)
				{
					num += 75;
					if (target.healthMinDamage > 50f)
					{
						num += 50;
					}
				}
				if (overridePilot.wm.availableWeaponTypes.gun && target.healthMinDamage < 5f)
				{
					num += 50;
				}
				if (target.hasRadar && overridePilot.wm.availableWeaponTypes.antirad)
				{
					num += 250;
				}
				if (num > 0)
				{
					if (overridePilot.combatRole == AIPilot.CombatRoles.Attack)
					{
						num += 100;
					}
					else if (overridePilot.combatRole == AIPilot.CombatRoles.FighterAttack)
					{
						num += 75;
					}
					else if (overridePilot.combatRole == AIPilot.CombatRoles.Bomber)
					{
						num += 100;
					}
				}
			}
			float num3 = Vector3.Distance(overridePilot.actor.position, target.position);
			if (num3 < 5000f)
			{
				num += 50;
			}
			else if (num3 < 10000f)
			{
				num += 25;
			}
			if (overridePilot.hasOverrideAttackTarget)
			{
				num -= 50;
			}
			else if (num > 0 && (overridePilot.commandState == AIPilot.CommandStates.FollowLeader || overridePilot.commandState == AIPilot.CommandStates.Orbit))
			{
				num += 50;
			}
			if (num > 0)
			{
				Debug.LogFormat("- Potential: {0} has {1} points", overridePilot.actor.DebugName(), num);
				list.Add(new PilotCandidate(overridePilot, num));
			}
			else
			{
				Debug.LogFormat("- Deny: {0} has {1} points", overridePilot.actor.DebugName(), num);
			}
		}
		AIPilot aIPilot = null;
		int num4 = 0;
		foreach (PilotCandidate item in list)
		{
			if (item.points > num4)
			{
				aIPilot = item.pilot;
				num4 = item.points;
			}
		}
		if ((bool)aIPilot)
		{
			Debug.LogFormat("- ACCEPTED: {0} is attacking {1}", aIPilot.actor.DebugName(), target.DebugName());
			aIPilot.OrderAttackTarget(target);
			return overridePilotList.IndexOf(aIPilot);
		}
		return -1;
	}

	public void OnQuicksaveGroupToNode(ConfigNode node)
	{
		foreach (Actor designatedTarget in designatedTargets)
		{
			if ((bool)designatedTarget && designatedTarget.alive)
			{
				node.AddNode(QuicksaveManager.SaveActorIdentifierToNode(designatedTarget, "D_TARGET"));
			}
		}
	}

	public void OnQuickloadGroupFromNode(ConfigNode node)
	{
		foreach (ConfigNode node2 in node.GetNodes("D_TARGET"))
		{
			Actor actor = QuicksaveManager.RetrieveActorFromNode(node2);
			if ((bool)actor)
			{
				AddDesignatedTarget(actor);
			}
		}
	}

	public void GetGroundAttackVectors(Vector3 attackerPosition, Vector3 targetPosition, float attackRange, List<Actor> designatedTargets, List<Actor> rwrActors, out Vector3 ingress, out Vector3 egress)
	{
		List<Actor>[] enemies = ((rwrActors == null) ? new List<Actor>[2] { targetsList, designatedTargets } : new List<Actor>[3] { targetsList, designatedTargets, rwrActors });
		GetGroundAttackVectors(attackerPosition, targetPosition, attackRange, enemies, out ingress, out egress);
	}

	private static Vector3 PlanarVector(Vector3 v)
	{
		v.y = 0f;
		return v;
	}

	public static void GetGroundAttackVectors(Vector3 attackerPosition, Vector3 targetPosition, float attackRange, List<Actor>[] enemies, out Vector3 ingress, out Vector3 egress)
	{
		float num = 10f;
		float num2 = 45f;
		float num3 = VectorUtils.Bearing(attackerPosition, targetPosition);
		float bearing = num3;
		float num4 = float.MaxValue;
		float bearing2 = num3;
		float num5 = float.MaxValue;
		_ = (targetPosition - attackerPosition).magnitude;
		gtvBuffer.Clear();
		for (int i = 0; i < enemies.Length; i++)
		{
			foreach (Actor item in enemies[i])
			{
				if ((bool)item && item.gameObject.activeInHierarchy && !gtvBuffer.Contains(item))
				{
					gtvBuffer.Add(item);
				}
			}
		}
		for (float num6 = 0f; num6 <= 90f; num6 += num)
		{
			for (int j = -1; j <= 1; j += 2)
			{
				float num7 = num3 + (float)j * num6;
				Vector3 vector = VectorUtils.BearingVector(num7);
				Vector3 vector2 = targetPosition - vector * attackRange;
				float num8 = 0f;
				float num9 = 0f;
				for (int k = 0; k < gtvBuffer.Count; k++)
				{
					Actor actor = gtvBuffer[k];
					if ((bool)actor && actor.alive)
					{
						float num10 = Vector3.Angle(vector, PlanarVector(vector2 - actor.position));
						if (num10 < num2)
						{
							num8 += num2 - num10;
						}
						float num11 = Vector3.Angle(vector, PlanarVector(actor.position - vector2));
						if (num11 < num2)
						{
							num9 += num2 - num11;
						}
					}
				}
				if (num8 < num4)
				{
					num4 = num8;
					bearing = num7;
				}
				if (num9 < num5)
				{
					num5 = num9;
					bearing2 = num7;
				}
			}
		}
		ingress = VectorUtils.BearingVector(bearing);
		egress = VectorUtils.BearingVector(bearing2);
	}
}
