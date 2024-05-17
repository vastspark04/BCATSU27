using System.Collections;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class GunTurretAISync : VTNetSyncRPCOnly
{
	public GunTurretAI ai;

	public Actor actor;

	private Actor targetActor;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
	}

	private void OnEnable()
	{
		StartCoroutine(EnableRoutine());
	}

	private IEnumerator EnableRoutine()
	{
		while (!wasRegistered)
		{
			yield return null;
		}
		if (base.isMine)
		{
			if ((bool)ai.turret)
			{
				StartCoroutine(LocalUpdateRoutine());
			}
			yield break;
		}
		ai.enabled = false;
		if ((bool)ai.turret)
		{
			StartCoroutine(RemoteUpdateRoutine());
		}
	}

	private IEnumerator LocalUpdateRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(0.3f);
		while (base.enabled && actor.alive)
		{
			if (ai.engagingActor != targetActor)
			{
				targetActor = ai.engagingActor;
				SendRPC("RPC_Target", VTNetUtils.GetActorIdentifier(targetActor));
			}
			yield return wait;
		}
	}

	[VTRPC]
	private void RPC_Target(int actorId)
	{
		Debug.Log($"RPC_Target({actorId})");
		Actor actor = (targetActor = VTNetUtils.GetActorFromIdentifier(actorId));
		Debug.Log(" - target is " + (actor ? actor.actorName : "null"));
	}

	private IEnumerator RemoteUpdateRoutine()
	{
		while (base.enabled && actor.alive)
		{
			if ((bool)targetActor)
			{
				ai.turret.AimToTarget(ai.gun.GetCalculatedTargetPosition(targetActor));
			}
			else
			{
				ai.turret.ReturnTurret();
			}
			yield return null;
		}
	}

	private void OnValidate()
	{
		if (!actor)
		{
			actor = GetComponent<Actor>();
		}
		if (!ai)
		{
			ai = GetComponent<GunTurretAI>();
		}
	}
}

}