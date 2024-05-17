using System;
using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class TargetManager : MonoBehaviour
{
	public struct ThreatScanResults
	{
		public Actor actor;

		public bool firingGun;

		public bool firingMissile;

		public Vector3 shootingDirection;

		public bool isMissile;
	}

	public static TargetManager instance;

	public List<Actor> allActors = new List<Actor>();

	public List<Actor> alliedUnits = new List<Actor>();

	public List<Actor> enemyUnits = new List<Actor>();

	public List<Actor> detectedByAllies = new List<Actor>();

	public List<Actor> detectedByEnemies = new List<Actor>();

	[HideInInspector]
	public List<RefuelPort> refuelPorts = new List<RefuelPort>();

	private static bool showVersion = true;

	private List<Actor.GetActorInfo> visualTargetBuffer = new List<Actor.GetActorInfo>();

	private void Awake()
	{
		instance = this;
	}

	public void RegisterActor(Actor a)
	{
		allActors.Add(a);
		if (a.team == Teams.Allied)
		{
			alliedUnits.Add(a);
		}
		else
		{
			enemyUnits.Add(a);
		}
	}

	public void UnregisterActor(Actor a)
	{
		allActors.Remove(a);
		alliedUnits.Remove(a);
		enemyUnits.Remove(a);
		detectedByAllies.Remove(a);
		detectedByEnemies.Remove(a);
	}

	private static bool IsGroundedAirTarget(Actor a)
	{
		Actor.Roles roles = a.role;
		if (roles == Actor.Roles.None && a.overrideCombatTarget)
		{
			roles = a.overriddenCombatRole;
		}
		if (roles == Actor.Roles.Air)
		{
			return a.flightInfo.isLanded;
		}
		return false;
	}

	public Actor GetRandomVisualTarget(Actor targeter, float radius, int roleMask, Vector3 origin, ref List<Actor> seenTargets, List<Actor> nonTargets, List<Actor> priorityTargets, float minRadius = 0f, bool detectSameTeam = false, bool hitboxOccluded = true)
	{
		seenTargets.Clear();
		bool flag = false;
		if (2 == (roleMask & 2) && 8 != (roleMask & 8))
		{
			flag = true;
		}
		List<Actor.GetActorInfo> list = visualTargetBuffer;
		Actor.GetActorsInRadius(origin, radius, targeter.team, (!detectSameTeam) ? TeamOptions.OtherTeam : TeamOptions.BothTeams, list);
		Teams teams = Teams.Allied;
		if (VTOLMPUtils.IsMultiplayer())
		{
			teams = VTOLMPLobbyManager.localPlayerInfo.team;
		}
		if (list.Count == 0)
		{
			return null;
		}
		bool flag2 = false;
		Actor actor = null;
		int num = UnityEngine.Random.Range(0, list.Count - 1);
		int num2 = 0;
		while (num2 < list.Count)
		{
			Actor actor2 = list[num].actor;
			if ((!detectSameTeam || !(actor2 == targeter)) && (nonTargets == null || !nonTargets.Contains(actor2)))
			{
				Actor.Roles roles = actor2.role;
				if (roles == Actor.Roles.None && actor2.overrideCombatTarget)
				{
					roles = actor2.overriddenCombatRole;
				}
				if (actor2.alive && roles != 0 && (roles == (Actor.Roles)((uint)roleMask & (uint)roles) || (flag && IsGroundedAirTarget(actor2))))
				{
					float sqrDist = list[num].sqrDist;
					if (CheckTargetVisibility(targeter.team, actor2, radius, minRadius, origin, teamCheck: true, sqrDist, hitboxOccluded))
					{
						bool flag3 = false;
						if ((!actor || (!flag2 && priorityTargets != null && (flag3 = priorityTargets.Contains(actor2)))) && actor2.team != targeter.team)
						{
							actor = actor2;
							flag2 = flag3;
						}
						seenTargets.Add(actor2);
						if (targeter.team == teams)
						{
							actor2.DiscoverActor();
						}
						actor2.DetectActor(targeter.team, targeter);
					}
				}
			}
			num2++;
			num = (num + 1) % list.Count;
		}
		return actor;
	}

	public Actor GetOpticalTargetFromView(Actor targeter, float radius, int roleMask, float minRadius, Vector3 origin, Vector3 direction, float fov, bool random = false, bool allActors = false, List<Actor> ref_targetsSeen = null, bool updateDetection = false, Teams backupTargeterTeam = Teams.Allied, float visualSizeRequirement = -1f, bool raycastVisibilityOnly = false)
	{
		Teams teams = backupTargeterTeam;
		if ((bool)targeter)
		{
			teams = targeter.team;
		}
		Teams teams2 = Teams.Allied;
		if (VTOLMPUtils.IsMultiplayer())
		{
			teams2 = VTOLMPLobbyManager.localPlayerInfo.team;
		}
		ref_targetsSeen?.Clear();
		List<Actor.GetActorInfo> list = visualTargetBuffer;
		Actor.GetActorsInRadius(origin, radius, teams, (!allActors) ? TeamOptions.OtherTeam : TeamOptions.BothTeams, list);
		bool flag = false;
		if (2 == (roleMask & 2) && 8 != (roleMask & 8))
		{
			flag = true;
		}
		if (list.Count == 0)
		{
			return null;
		}
		direction.Normalize();
		float num = radius * radius;
		float num2 = minRadius * minRadius;
		Actor actor = null;
		float num3 = fov / 2f;
		float num4 = Mathf.Cos(num3 * ((float)Math.PI / 180f));
		float num5 = Mathf.Cos((float)Math.PI / 180f * num3 / 3f);
		if (random)
		{
			int num6 = UnityEngine.Random.Range(0, list.Count - 1);
			int num7 = 0;
			while (num7 < list.Count)
			{
				Actor actor2 = list[num6].actor;
				if ((bool)actor2 && actor2.opticalTargetable)
				{
					Actor.Roles roles = actor2.role;
					if (roles == Actor.Roles.None && actor2.overrideCombatTarget)
					{
						roles = actor2.overriddenCombatRole;
					}
					if (roles == (Actor.Roles)((uint)roleMask & (uint)roles) || (flag && IsGroundedAirTarget(actor2)))
					{
						float sqrDist = list[num6].sqrDist;
						if (sqrDist < num && sqrDist > num2)
						{
							float num8 = Vector3.Dot(direction, (actor2.position - origin) / Mathf.Sqrt(sqrDist));
							float num9 = ((roles == Actor.Roles.Missile) ? num5 : num4);
							if (num8 > num9 && CheckTargetVisibility(teams, actor2, radius, minRadius, origin, !allActors, sqrDist))
							{
								ref_targetsSeen?.Add(actor2);
								if (updateDetection)
								{
									if (teams == teams2)
									{
										actor2.DiscoverActor();
									}
									actor2.DetectActor(teams, targeter);
								}
								if (!actor)
								{
									actor = actor2;
									if (ref_targetsSeen == null)
									{
										break;
									}
								}
							}
						}
					}
				}
				num7++;
				num6 = (num6 + 1) % list.Count;
			}
		}
		else
		{
			float num10 = num4;
			for (int i = 0; i < list.Count; i++)
			{
				Actor actor3 = list[i].actor;
				if (!actor3 || !actor3.opticalTargetable)
				{
					continue;
				}
				Actor.Roles roles2 = actor3.role;
				if (roles2 == Actor.Roles.None && actor3.overrideCombatTarget)
				{
					roles2 = actor3.overriddenCombatRole;
				}
				if (roles2 != (Actor.Roles)((uint)roleMask & (uint)roles2))
				{
					continue;
				}
				float sqrDist2 = list[i].sqrDist;
				if (!(sqrDist2 < num) || !(sqrDist2 > num2))
				{
					continue;
				}
				float num11 = Vector3.Dot(direction, (actor3.position - origin) / Mathf.Sqrt(sqrDist2));
				if ((roles2 == Actor.Roles.Missile && num11 < num5) || !CheckTargetVisibility(teams, actor3, radius, minRadius, origin, !allActors, sqrDist2, hitboxOccluded: true, raycastVisibilityOnly))
				{
					continue;
				}
				if (visualSizeRequirement > 0f)
				{
					Vector3 normalized = Vector3.Cross(Vector3.up, actor3.position - origin).normalized;
					if (Vector3.Angle(actor3.position + normalized * actor3.physicalRadius - origin, actor3.position - normalized * actor3.physicalRadius - origin) / fov < visualSizeRequirement)
					{
						continue;
					}
				}
				if (num11 > num10)
				{
					num10 = num11;
					actor = actor3;
				}
				ref_targetsSeen?.Add(actor3);
				if (updateDetection)
				{
					if (teams == teams2)
					{
						actor3.DiscoverActor();
					}
					actor3.DetectActor(teams, targeter);
				}
			}
		}
		return actor;
	}

	public void GetAllOpticalTargetsInView(Actor targeter, float fov, float minRadius, float maxRadius, int roleMask, Vector3 origin, Vector3 direction, List<Actor> outList, bool allActors = false, bool occlusionCheck = true)
	{
		List<Actor.GetActorInfo> list = visualTargetBuffer;
		Teams teams = Teams.Allied;
		if ((bool)targeter)
		{
			teams = targeter.team;
		}
		Actor.GetActorsInRadius(origin, maxRadius, teams, (!allActors) ? TeamOptions.OtherTeam : TeamOptions.BothTeams, list);
		bool flag = false;
		if (2 == (roleMask & 2) && 8 != (roleMask & 8))
		{
			flag = true;
		}
		outList.Clear();
		if (list.Count == 0)
		{
			return;
		}
		direction.Normalize();
		float num = Mathf.Cos(fov / 2f * ((float)Math.PI / 180f));
		float num2 = minRadius * minRadius;
		foreach (Actor.GetActorInfo item in list)
		{
			Actor actor = item.actor;
			if (!actor)
			{
				continue;
			}
			Actor.Roles roles = actor.role;
			if (roles == Actor.Roles.None && actor.overrideCombatTarget)
			{
				roles = actor.overriddenCombatRole;
			}
			if (roles != (Actor.Roles)((uint)roleMask & (uint)roles) && (!flag || !IsGroundedAirTarget(actor)))
			{
				continue;
			}
			Vector3 rhs = (actor.position - origin) / Mathf.Sqrt(item.sqrDist);
			if (!(Vector3.Dot(direction, rhs) < num))
			{
				float sqrDist = item.sqrDist;
				if (!(sqrDist < num2) && (!occlusionCheck || CheckTargetVisibility(teams, actor, maxRadius, minRadius, origin, teamCheck: false, sqrDist)))
				{
					outList.Add(actor);
				}
			}
		}
	}

	public bool CheckTargetVisibility(Teams myTeam, Actor other, float visionRadius, float minRadius, Vector3 origin, bool teamCheck = true, float sqrDist = -1f, bool hitboxOccluded = true, bool raycastOnly = false)
	{
		Vector3 position = other.position;
		if (sqrDist < 0f)
		{
			sqrDist = (position - origin).sqrMagnitude;
		}
		if (sqrDist > visionRadius * visionRadius || sqrDist < minRadius * minRadius)
		{
			return false;
		}
		int num = 1;
		if (hitboxOccluded)
		{
			num |= 0x400;
		}
		if (Physics.Linecast(origin, position, out var hitInfo, num))
		{
			Actor componentInParent = hitInfo.collider.gameObject.GetComponentInParent<Actor>();
			if ((bool)componentInParent)
			{
				if (teamCheck)
				{
					if (componentInParent.team == myTeam)
					{
						return false;
					}
					return true;
				}
				if (componentInParent == other)
				{
					return true;
				}
				return false;
			}
			if (!raycastOnly)
			{
				if ((hitInfo.point - other.position).magnitude - other.physicalRadius < 5f)
				{
					return true;
				}
				return false;
			}
			return false;
		}
		return true;
	}

	public ThreatScanResults AirThreatScan(Actor scanner, Vector3 direction, float radius, float dotLimit, ModuleRWR rwr)
	{
		ThreatScanResults result = default(ThreatScanResults);
		float num = radius * radius;
		List<Actor.GetActorInfo> list = visualTargetBuffer;
		Actor.GetActorsInRadius(scanner.position, radius, scanner.team, TeamOptions.OtherTeam, list);
		bool flag = false;
		Actor actor = null;
		float num2 = num;
		foreach (Actor.GetActorInfo item in list)
		{
			Actor actor2 = item.actor;
			Vector3 lhs = actor2.position - scanner.position;
			if (Vector3.Dot(lhs, direction) < dotLimit)
			{
				continue;
			}
			float sqrDist = item.sqrDist;
			bool flag2 = ((bool)actor2.weaponManager && actor2.weaponManager.isFiring) || ((bool)actor2.gunTurretAI && actor2.gunTurretAI.isFiring);
			if ((!flag || flag2) && (flag != flag2 || !(sqrDist > num2)))
			{
				if (flag2)
				{
					flag = true;
				}
				if (actor2.role != Actor.Roles.Missile || ((!rwr || actor2.GetMissile().radarLock == null || (rwr.isLocked && rwr.IsLockedBy(actor2.GetMissile().actor))) && !(Vector3.Dot(-(actor2.velocity - scanner.velocity).normalized, lhs.normalized) < 0.9975641f)))
				{
					num2 = sqrDist;
					actor = actor2;
				}
			}
		}
		if ((bool)actor)
		{
			result.actor = actor;
			if (actor.role == Actor.Roles.Missile)
			{
				result.isMissile = true;
			}
			else if ((bool)actor.weaponManager)
			{
				if (actor.weaponManager.equippedGun && actor.weaponManager.isFiring)
				{
					result.firingGun = true;
					result.shootingDirection = actor.transform.forward;
				}
				else if ((bool)actor.weaponManager.lastFiredMissile)
				{
					result.firingMissile = true;
				}
			}
			else if ((bool)actor.gunTurretAI && actor.gunTurretAI.isFiring)
			{
				result.firingGun = true;
				result.shootingDirection = actor.gunTurretAI.gun.fireTransforms[0].forward;
			}
		}
		return result;
	}
}
