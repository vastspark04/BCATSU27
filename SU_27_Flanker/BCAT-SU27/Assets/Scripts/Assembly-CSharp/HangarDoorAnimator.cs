using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HangarDoorAnimator : MonoBehaviour
{
	public Actor[] attachedActors;

	public TranslationToggle doorToggle;

	public AnimationToggle animToggle;

	public Transform triggerTf;

	public float radius;

	public AirbaseNavNode parkingNode;

	public bool isFullyOpen
	{
		get
		{
			if ((bool)doorToggle)
			{
				return Mathf.Abs(1f - doorToggle.transforms[0].currT) <= 0.01f;
			}
			if ((bool)animToggle)
			{
				return Mathf.Abs(1f - animToggle.GetT()) <= 0.01f;
			}
			return false;
		}
	}

	private void OnEnable()
	{
		StartCoroutine(DoorRoutine());
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)triggerTf)
		{
			Gizmos.DrawWireSphere(triggerTf.position, radius);
		}
	}

	private bool IsAttachedActor(Actor a)
	{
		if (attachedActors != null)
		{
			for (int i = 0; i < attachedActors.Length; i++)
			{
				if (a == attachedActors[i])
				{
					return true;
				}
			}
		}
		return false;
	}

	private IEnumerator DoorRoutine()
	{
		while (!TargetManager.instance)
		{
			yield return null;
		}
		float sqrRad = radius * radius;
		yield return new WaitForSeconds(Random.Range(0f, 3f));
		while (base.enabled)
		{
			bool open = false;
			if ((bool)parkingNode && parkingNode.parkingOccupiedBy != null)
			{
				open = true;
			}
			else
			{
				List<Actor> aList = TargetManager.instance.allActors;
				Actor actor = ((attachedActors != null && attachedActors.Length != 0) ? attachedActors[0] : null);
				if (actor != null)
				{
					aList = ((actor.team == Teams.Allied) ? TargetManager.instance.alliedUnits : TargetManager.instance.enemyUnits);
				}
				for (int i = 0; i < aList.Count; i++)
				{
					if (open)
					{
						break;
					}
					Actor actor2 = aList[i];
					if ((bool)actor2 && !IsAttachedActor(actor2) && (actor2.position - triggerTf.position).sqrMagnitude < sqrRad)
					{
						open = true;
					}
					yield return null;
				}
			}
			if (open)
			{
				Open();
			}
			else
			{
				Close();
			}
			yield return new WaitForSeconds(1f);
		}
	}

	private void Open()
	{
		if ((bool)doorToggle)
		{
			doorToggle.SetDeployed();
		}
		if ((bool)animToggle)
		{
			animToggle.Deploy();
		}
	}

	private void Close()
	{
		if ((bool)doorToggle)
		{
			doorToggle.SetDefault();
		}
		if ((bool)animToggle)
		{
			animToggle.Retract();
		}
	}
}
