using System.Collections;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class BriefingAvatarSync : VTNetSyncRPCOnly
{
	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		VTOLMPSceneManager.instance.OnBriefingSeatUpdated += Instance_OnBriefingSeatUpdated;
		VTOLMPSceneManager.instance.OnPlayerSelectedTeam += Instance_OnPlayerSelectedTeam;
		StartCoroutine(Startup());
	}

	private void Instance_OnPlayerSelectedTeam(PlayerInfo p)
	{
		if ((ulong)p.steamUser.Id == (ulong)base.netEntity.owner.Id)
		{
			int seatIdx = VTOLMPSceneManager.instance.GetSeatIdx(base.netEntity.ownerID);
			if (seatIdx >= 0)
			{
				Instance_OnBriefingSeatUpdated(base.netEntity.ownerID, p.team, seatIdx);
			}
		}
	}

	private void Instance_OnSlotUpdated(VTOLMPSceneManager.VehicleSlot obj)
	{
		if (obj.player != null && (ulong)obj.player.steamUser.Id == base.netEntity.ownerID)
		{
			int seatIdx = VTOLMPSceneManager.instance.GetSeatIdx(base.netEntity.ownerID);
			if (seatIdx >= 0)
			{
				Instance_OnBriefingSeatUpdated(base.netEntity.ownerID, obj.player.team, seatIdx);
			}
		}
	}

	private void OnDestroy()
	{
		if ((bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnBriefingSeatUpdated -= Instance_OnBriefingSeatUpdated;
			VTOLMPSceneManager.instance.OnSlotUpdated -= Instance_OnSlotUpdated;
		}
		if ((bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnEnterVehicle -= Instance_OnEnterVehicle;
			VTOLMPSceneManager.instance.OnPlayerSelectedTeam -= Instance_OnPlayerSelectedTeam;
		}
	}

	private IEnumerator Startup()
	{
		PlayerInfo p;
		for (p = null; p == null; p = VTOLMPLobbyManager.GetPlayer(base.netEntity.ownerID))
		{
			yield return null;
		}
		while (!p.chosenTeam)
		{
			yield return null;
		}
		int seatIdx = VTOLMPSceneManager.instance.GetSeatIdx(base.netEntity.ownerID);
		while (seatIdx < 0)
		{
			seatIdx = VTOLMPSceneManager.instance.GetSeatIdx(base.netEntity.ownerID);
			yield return null;
		}
		if (seatIdx >= 0)
		{
			Instance_OnBriefingSeatUpdated(base.netEntity.ownerID, p.team, seatIdx);
		}
		if (base.isMine)
		{
			VTOLMPSceneManager.instance.OnEnterVehicle += Instance_OnEnterVehicle;
			EnvironmentManager.instance.ResetHUDBrightness();
		}
	}

	private void Instance_OnEnterVehicle()
	{
		Debug.Log("BriefingAvatarSync.OnEnterVehicle");
		VTNetworkManager.NetDestroyObject(base.gameObject);
	}

	private void Instance_OnBriefingSeatUpdated(ulong id, Teams team, int seatIdx)
	{
		if (id != base.netEntity.ownerID)
		{
			return;
		}
		Debug.Log($"BriefingAvatarSync OnBriefingSeatUpdated({id}, {team}, {seatIdx})");
		BriefingSpawnPoint[] array = ((team == Teams.Allied) ? VTOLMPBriefingRoom.instance.alliedSpawnTransforms : VTOLMPBriefingRoom.instance.enemySpawnTransforms);
		if (seatIdx == array.Length)
		{
			Transform transform = VTOLMPBriefingRoom.instance.alliedBriefingControllerTf.transform;
			if ((bool)VTOLMPBriefingRoom.instance.enemyBriefingControllerTf && team == Teams.Enemy)
			{
				transform = VTOLMPBriefingRoom.instance.enemyBriefingControllerTf.transform;
			}
			base.transform.position = transform.transform.position;
			base.transform.rotation = transform.transform.rotation;
		}
		else
		{
			BriefingSpawnPoint briefingSpawnPoint = array[seatIdx];
			base.transform.position = briefingSpawnPoint.transform.position;
			base.transform.rotation = briefingSpawnPoint.transform.rotation;
		}
	}
}

}