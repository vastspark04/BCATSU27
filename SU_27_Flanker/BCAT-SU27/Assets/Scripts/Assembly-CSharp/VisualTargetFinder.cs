using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualTargetFinder : MonoBehaviour, ITargetPreferences
{
	public float visionRadius;

	public float minEngageRadius;

	public float targetScanInterval;

	public bool detectSameTeam;

	public bool hitboxOccluded = true;

	public Actor attackingTarget;

	public Actor.ActorRolesSelection targetsToFind;

	public List<Actor> targetsSeen = new List<Actor>();

	public bool useFOV;

	public Transform fovReference;

	public float fov = 90f;

	private Actor actor;

	private List<Actor> nonTargets = new List<Actor>();

	private List<Actor> priorityTargets = new List<Actor>();

	private void Awake()
	{
		actor = GetComponentInParent<Actor>();
	}

	private void OnEnable()
	{
		StartCoroutine(TargetScanRoutine());
	}

	private void OnDisable()
	{
		attackingTarget = null;
	}

	private IEnumerator TargetScanRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(targetScanInterval);
		yield return new WaitForSeconds(Random.Range(0f, targetScanInterval));
		while (base.enabled)
		{
			if (!TargetManager.instance)
			{
				yield return null;
			}
			while (!actor.alive)
			{
				yield return wait;
			}
			if (useFOV)
			{
				if (!fovReference)
				{
					base.enabled = false;
					break;
				}
				attackingTarget = TargetManager.instance.GetOpticalTargetFromView(actor, visionRadius, targetsToFind.bitmask, minEngageRadius, fovReference.position, fovReference.forward, fov, random: true, allActors: false, targetsSeen, updateDetection: true);
			}
			else
			{
				attackingTarget = TargetManager.instance.GetRandomVisualTarget(actor, visionRadius, targetsToFind.bitmask, base.transform.position, ref targetsSeen, nonTargets, priorityTargets, minEngageRadius, detectSameTeam, hitboxOccluded);
			}
			yield return wait;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position, minEngageRadius);
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(base.transform.position, visionRadius);
	}

	public void SetNonTargets(UnitReferenceList list)
	{
		nonTargets.Clear();
		foreach (UnitReference unit in list.units)
		{
			Actor actor = unit.GetActor();
			if ((bool)actor)
			{
				nonTargets.Add(actor);
			}
			targetsSeen.Remove(actor);
		}
		if ((bool)attackingTarget && nonTargets.Contains(attackingTarget))
		{
			attackingTarget = null;
		}
	}

	public void AddNonTargets(UnitReferenceList list)
	{
		foreach (UnitReference unit in list.units)
		{
			Actor actor = unit.GetActor();
			if ((bool)actor)
			{
				nonTargets.Add(actor);
			}
			targetsSeen.Remove(actor);
		}
		if ((bool)attackingTarget && nonTargets.Contains(attackingTarget))
		{
			attackingTarget = null;
		}
	}

	public void RemoveNonTargets(UnitReferenceList list)
	{
		foreach (UnitReference unit in list.units)
		{
			if ((bool)unit.GetActor())
			{
				nonTargets.Remove(unit.GetActor());
			}
		}
	}

	public void ClearNonTargets()
	{
		nonTargets.Clear();
	}

	public void SetPriorityTargets(UnitReferenceList list)
	{
		priorityTargets.Clear();
		foreach (UnitReference unit in list.units)
		{
			if ((bool)unit.GetActor())
			{
				priorityTargets.Add(unit.GetActor());
			}
		}
	}

	public void AddPriorityTargets(UnitReferenceList list)
	{
		foreach (UnitReference unit in list.units)
		{
			if ((bool)unit.GetActor())
			{
				priorityTargets.Add(unit.GetActor());
			}
		}
	}

	public void RemovePriorityTargets(UnitReferenceList list)
	{
		foreach (UnitReference unit in list.units)
		{
			if ((bool)unit.GetActor())
			{
				priorityTargets.Remove(unit.GetActor());
			}
		}
	}

	public void ClearPriorityTargets()
	{
		priorityTargets.Clear();
	}
}
