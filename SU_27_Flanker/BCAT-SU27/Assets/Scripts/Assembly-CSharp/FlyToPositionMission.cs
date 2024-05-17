using UnityEngine;
using VTOLVR.Multiplayer;

public class FlyToPositionMission : MissionObjective
{
	public Transform targetTransform;

	public float radius;

	public bool planarRadius;

	public UnitSpawner targetUnit;

	private float unavailableUnitTimer;

	private void OnDrawGizmosSelected()
	{
		if ((bool)targetTransform)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(base.transform.position, targetTransform.position);
			Gizmos.DrawWireSphere(targetTransform.position, radius);
		}
	}

	private void Update()
	{
		if (!base.started || base.objectiveFinished)
		{
			return;
		}
		if (!targetTransform)
		{
			Debug.Log("Target transform for " + objectiveName + " is destroyed! Failing objective.");
			if (base.isPlayersMission)
			{
				EndMission.AddFailText($"{objectiveName} {VTLStaticStrings.mission_targetDestroyed}");
			}
			FailObjective();
		}
		else
		{
			if (QuicksaveManager.isQuickload && !PlayerSpawn.qLoadPlayerComplete)
			{
				return;
			}
			bool flag = false;
			if ((bool)targetUnit)
			{
				if (!targetUnit.spawned)
				{
					unavailableUnitTimer += Time.deltaTime;
					if (unavailableUnitTimer > 5f)
					{
						Debug.Log("Objective to join a unit that is not spawned! Failing objective.");
						FailObjective();
						flag = true;
						if (base.isPlayersMission)
						{
							EndMission.AddFailText(objectiveName + " target not spawned.");
						}
					}
				}
				else if (!targetUnit.spawnedUnit.actor.alive)
				{
					unavailableUnitTimer += Time.deltaTime;
					if (unavailableUnitTimer > 5f)
					{
						Debug.Log("Objective to join a unit that is dead! Failing objective.");
						FailObjective();
						flag = true;
						if (base.isPlayersMission)
						{
							EndMission.AddFailText($"{objectiveName} {VTLStaticStrings.mission_targetDestroyed}");
						}
					}
				}
			}
			if (flag)
			{
				return;
			}
			if (VTOLMPUtils.IsMultiplayer())
			{
				if (!VTOLMPLobbyManager.isLobbyHost)
				{
					return;
				}
				for (int i = 0; i < VTOLMPLobbyManager.instance.connectedPlayers.Count; i++)
				{
					PlayerInfo playerInfo = VTOLMPLobbyManager.instance.connectedPlayers[i];
					if (playerInfo.team != team || !playerInfo.vehicleActor || !playerInfo.vehicleActor.alive)
					{
						continue;
					}
					Vector3 position = playerInfo.vehicleActor.position;
					Vector3 position2 = targetTransform.position;
					if (planarRadius)
					{
						position.y = (position2.y = 0f);
					}
					if ((position - position2).sqrMagnitude < radius * radius)
					{
						CompleteObjective();
						if (base.isPlayersMission)
						{
							EndMission.AddText($"{objectiveName} {VTLStaticStrings.mission_completed}", red: false);
						}
					}
				}
			}
			else
			{
				if (!FlightSceneManager.instance.playerActor)
				{
					return;
				}
				Vector3 position3 = FlightSceneManager.instance.playerActor.position;
				Vector3 position4 = targetTransform.position;
				if (planarRadius)
				{
					position3.y = (position4.y = 0f);
				}
				if ((position3 - position4).sqrMagnitude < radius * radius)
				{
					CompleteObjective();
					if (base.isPlayersMission)
					{
						EndMission.AddText($"{objectiveName} {VTLStaticStrings.mission_completed}", red: false);
					}
				}
			}
		}
	}
}
