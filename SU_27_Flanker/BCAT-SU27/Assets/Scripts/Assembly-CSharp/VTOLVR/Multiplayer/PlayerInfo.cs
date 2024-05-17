using System;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class PlayerInfo
{
	public Friend steamUser;

	public string pilotName;

	public bool chosenTeam;

	public Teams team;

	private int _slotIdx = -1;

	public bool isReady;

	public GameObject multicrewAvatar;

	public int voteKicks;

	public int voteBans;

	private int _vEntId = -1;

	public int selectedSlot
	{
		get
		{
			return _slotIdx;
		}
		set
		{
			_slotIdx = value;
			if (value < 0)
			{
				isReady = false;
			}
		}
	}

	public GameObject vehicleObject { get; private set; }

	public Actor vehicleActor { get; private set; }

	public int vehicleEntityID
	{
		get
		{
			return _vEntId;
		}
		set
		{
			Debug.Log($"Set vehicleEntityID {value} for {steamUser.Name} ({pilotName})");
			VTNetEntity entity = VTNetworkManager.instance.GetEntity(value);
			if ((bool)entity)
			{
				vehicleObject = entity.gameObject;
				vehicleActor = vehicleObject.GetComponent<Actor>();
				if ((bool)vehicleActor)
				{
					Debug.Log(" - Got actor: " + vehicleActor.name);
				}
				else
				{
					Debug.LogError(" - vehicleObject did not have an Actor!");
				}
				vehicleActor.SetTeam(team);
				MultiplayerSpawn mPSpawn = VTOLMPSceneManager.instance.GetMPSpawn(team, selectedSlot);
				if (!mPSpawn)
				{
					Debug.LogError($" - team {team} slot {selectedSlot} did not have an mpSpawn!");
				}
				mPSpawn.actor = vehicleActor;
				vehicleActor.unitSpawn = mPSpawn;
				if (!PilotSaveManager.currentCampaign)
				{
					Debug.LogError(" - currentCampaign is null!");
				}
				Texture2D texture2D = ((team == Teams.Allied) ? PilotSaveManager.currentCampaign.campaignLivery : PilotSaveManager.currentCampaign.campaignLiveryOpFor);
				if (PilotSaveManager.currentCampaign.perVehicleLiveries != null)
				{
					Debug.Log("Checking per-vehicle liveries");
					Campaign.PerVehicleLiveries[] perVehicleLiveries = PilotSaveManager.currentCampaign.perVehicleLiveries;
					foreach (Campaign.PerVehicleLiveries perVehicleLiveries2 in perVehicleLiveries)
					{
						if (perVehicleLiveries2.vehicleName == mPSpawn.VehicleName())
						{
							Debug.Log(" - per-vehicle livery found for " + perVehicleLiveries2.vehicleName);
							texture2D = ((team == Teams.Allied) ? perVehicleLiveries2.alliedLivery : perVehicleLiveries2.enemyLivery);
						}
					}
				}
				if ((bool)texture2D)
				{
					AircraftLiveryApplicator component = vehicleActor.GetComponent<AircraftLiveryApplicator>();
					if ((bool)component)
					{
						component.ApplyLivery(texture2D);
					}
				}
				_vEntId = value;
				if (!VTOLMPSceneManager.instance)
				{
					Debug.LogError(" - VTOLMPSceneManager.instance is null!");
				}
				VTOLMPSceneManager.instance.ReportPlayerSpawnedInVehicle(this);
				Debug.LogError("TODO: Callsign designation");
				vehicleActor.designation = VTOLMPSceneManager.instance.GetSlot(this).designation;
				if (this == VTOLMPLobbyManager.localPlayerInfo)
				{
					return;
				}
				if (VTOLMPLobbyManager.localPlayerInfo.chosenTeam)
				{
					if (VTOLMPLobbyManager.localPlayerInfo.team == team)
					{
						vehicleActor.DiscoverActor();
					}
				}
				else
				{
					vehicleActor.DiscoverOnLocalPlayerTeamChosen();
				}
			}
			else
			{
				Debug.Log(" - did not find entity.");
				vehicleObject = null;
				vehicleActor = null;
			}
		}
	}

	public static event Action OnKickBanVotesUpdated;

	public void ReportKickBanUpdated()
	{
		PlayerInfo.OnKickBanVotesUpdated?.Invoke();
	}

	public override string ToString()
	{
		return $"{steamUser.Id.Value},{pilotName},{team},{selectedSlot},{chosenTeam},{voteKicks},{voteBans}";
	}

	public void UpdateFromString(string s)
	{
		string[] array = s.Split(',');
		steamUser = new Friend(ulong.Parse(array[0]));
		pilotName = array[1];
		selectedSlot = int.Parse(array[3]);
		if (!steamUser.IsMe)
		{
			chosenTeam = bool.Parse(array[4]);
			team = (Teams)Enum.Parse(typeof(Teams), array[2]);
		}
		int num = int.Parse(array[5]);
		int num2 = int.Parse(array[6]);
		if (num != voteKicks)
		{
			voteKicks = num;
			PlayerInfo.OnKickBanVotesUpdated?.Invoke();
		}
		if (num2 != voteBans)
		{
			voteBans = num2;
			PlayerInfo.OnKickBanVotesUpdated?.Invoke();
		}
	}
}

}