using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class DamageSync : VTNetSyncRPCOnly
{
	public Actor actor;

	public Health[] healths;

	private Dictionary<Health, int> healthIndices;

	private PlayerInfo killCredit;

	private float timeKillCredited;

	private Dictionary<PlayerInfo, float> assistCredits = new Dictionary<PlayerInfo, float>();

	private VehicleMaster vm;

	private bool isPlayer;

	private const float maxTimeSinceDamage = 60f;

	private const float maxTimeSinceAssist = 30f;

	private bool killsCredited;

	protected override void Awake()
	{
		base.Awake();
		if (!actor)
		{
			actor = GetComponent<Actor>();
		}
		vm = GetComponent<VehicleMaster>();
		healthIndices = new Dictionary<Health, int>();
		for (int i = 0; i < healths.Length; i++)
		{
			healthIndices.Add(healths[i], i);
		}
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
		GiveKillCredits();
	}

	private void GiveKillCredits()
	{
		if (!VTNetworkManager.hasInstance || VTNetworkManager.instance.netState == VTNetworkManager.NetStates.None || killsCredited || !VTOLMPSceneManager.instance || !VTOLMPUtils.IsMultiplayer() || !base.isMine || ((bool)vm && (bool)vm.currentRearmingPoint) || !actor || actor.role == Actor.Roles.Missile)
		{
			return;
		}
		if (VTNetworkManager.verboseLogs)
		{
			Debug.Log(base.gameObject.name + " is giving kill credit to " + ((killCredit != null) ? killCredit.pilotName : "null"));
		}
		killsCredited = true;
		if (killCredit != null && Time.time - timeKillCredited < 60f)
		{
			VTOLMPSceneManager.instance.GiveKillCredit(killCredit, actor);
			foreach (KeyValuePair<PlayerInfo, float> assistCredit in assistCredits)
			{
				if (assistCredit.Key != killCredit && timeKillCredited - assistCredit.Value < 30f && assistCredit.Key.team != actor.team)
				{
					VTOLMPSceneManager.instance.GiveAssistCredit(assistCredit.Key);
				}
			}
		}
		else
		{
			string msg = ((!actor.killedByActor) ? (actor.actorName + " was killed.") : (actor.actorName + " was killed by " + actor.killedByActor.actorName + "."));
			VTOLMPLobbyManager.SendLogMessage(msg);
		}
		if (isPlayer)
		{
			PlayerInfo player = VTOLMPSceneManager.instance.GetPlayer(actor);
			VTOLMPSceneManager.instance.GiveDeathCredit(player);
		}
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
		if (!actor)
		{
			actor = GetComponent<Actor>();
		}
		if (base.isMine && (bool)actor)
		{
			actor.OnThisActorKilled -= Actor_OnThisActorKilled;
			actor.OnThisActorKilled += Actor_OnThisActorKilled;
			if (actor.isPlayer)
			{
				actor.GetComponentInChildren<EjectionSeat>(includeInactive: true).OnEject.AddListener(OnEject);
				isPlayer = true;
			}
		}
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (!base.isMine)
		{
			return;
		}
		Refresh(0uL);
		VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		for (int i = 0; i < healths.Length; i++)
		{
			int hIdx = i;
			healths[i].OnDeath.AddListener(delegate
			{
				SendRPCKill(hIdx);
			});
		}
	}

	private void Actor_OnThisActorKilled()
	{
		GiveKillCredits();
	}

	private void OnEject()
	{
		GiveKillCredits();
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		Refresh(obj);
	}

	private void Refresh(ulong target = 0uL)
	{
		if (!base.isMine)
		{
			return;
		}
		for (int i = 0; i < healths.Length; i++)
		{
			if (healths[i].isDead)
			{
				if (target == 0L)
				{
					SendRPC("RPC_KillLate", i);
				}
				else
				{
					SendDirectedRPC(target, "RPC_KillLate", i);
				}
			}
		}
	}

	[VTRPC]
	private void RPC_KillLate(int hIdx)
	{
		if (healths != null)
		{
			if (hIdx >= 0 && hIdx < healths.Length)
			{
				if ((bool)healths[hIdx])
				{
					healths[hIdx].QS_Kill();
				}
				else
				{
					Debug.LogError($"RPC_KillLate: healths[{hIdx}] was null! ({UIUtils.GetHierarchyString(base.gameObject)}");
				}
			}
			else
			{
				Debug.LogError($"RPC_KillLate: hIdx was out of range! hIdx={hIdx}, healths.Length={healths.Length}");
			}
		}
		else
		{
			Debug.LogError("RPC_KillLate: healths was nulL!");
		}
	}

	public void RemoteDamage(Actor sourceActor, float dmg, Health.DamageTypes dmgType, Health h, PlayerInfo sourcePlayer)
	{
		if (healthIndices.TryGetValue(h, out var value))
		{
			int actorIdentifier = VTNetUtils.GetActorIdentifier(sourceActor);
			ulong num = 0uL;
			if (sourcePlayer != null)
			{
				num = sourcePlayer.steamUser.Id.Value;
			}
			SendRPC("RPC_RemoteDmg", actorIdentifier, dmg, (int)dmgType, value, num);
		}
	}

	[VTRPC]
	private void RPC_RemoteDmg(int actorId, float dmg, int dmgType, int hIdx, ulong sourcePlayerId)
	{
		if (base.isMine && (bool)healths[hIdx])
		{
			Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorId);
			PlayerInfo playerInfo = ((sourcePlayerId != 0L) ? VTOLMPLobbyManager.GetPlayer(sourcePlayerId) : null);
			if (sourcePlayerId != 0L)
			{
				SetDamageCredit(playerInfo);
			}
			healths[hIdx].Damage(dmg, base.transform.position, (Health.DamageTypes)dmgType, actorFromIdentifier, "Networked damage", rpcIfRemote: false, playerInfo);
		}
	}

	private void SendRPCKill(int hIdx)
	{
		SendRPC("RPC_Kill", hIdx);
	}

	[VTRPC]
	private void RPC_Kill(int hIdx)
	{
		Debug.Log($"{base.gameObject.name}.RPC_Kill({hIdx}) from {new Friend(VTNetworkManager.currentRPCInfo.senderId).Name}");
		if (hIdx >= 0 && hIdx < healths.Length)
		{
			if ((bool)healths[hIdx])
			{
				healths[hIdx].Damage(healths[hIdx].maxHealth + 1f, healths[hIdx].transform.position, Health.DamageTypes.Impact, null, null, rpcIfRemote: false);
			}
		}
		else
		{
			Debug.LogError($" -- hIdx was out of range!  (hIdx == {hIdx}, Length == {healths.Length})");
		}
	}

	public void SetDamageCredit(PlayerInfo p)
	{
		if (p != null)
		{
			if (killCredit != p)
			{
				Debug.Log(base.gameObject.name + " was damaged by " + p.pilotName);
			}
			killCredit = p;
			if (assistCredits.ContainsKey(p))
			{
				assistCredits[p] = Time.time;
			}
			else
			{
				assistCredits.Add(p, Time.time);
			}
			timeKillCredited = Time.time;
		}
	}
}

}