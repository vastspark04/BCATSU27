using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class CockpitTeamRadioManager : MonoBehaviour
{
	public Transform voiceSourcePosition;

	public Transform opforSourcePosition;

	public float minDistance = 1f;

	public float maxDistance = 500f;

	public VTNetworkVoicePTT ptt;

	public EmissiveTextureLight indicator;

	public MFDCommsPage commPage;

	public AudioSource startStopSource;

	[Header("Multicrew")]
	public MultiUserVehicleSync muvs;

	public Transform copilotSourcePosition;

	public AudioSource copilotStartStopSource;

	private Dictionary<PlayerInfo, VTNetworkVoicePosition> voicePositions = new Dictionary<PlayerInfo, VTNetworkVoicePosition>();

	private List<SteamId> teamWhitelist = new List<SteamId>();

	private List<SteamId> copilotWhitelist = new List<SteamId>();

	private bool transmitAll;

	private bool copilotVoiceEnabled;

	private int knobState;

	private bool isMp => VTOLMPUtils.IsMultiplayer();

	private void Start()
	{
		if (!isMp)
		{
			return;
		}
		foreach (PlayerInfo connectedPlayer in VTOLMPLobbyManager.instance.connectedPlayers)
		{
			if ((bool)connectedPlayer.vehicleActor && !connectedPlayer.steamUser.IsMe)
			{
				SetupVoiceSource(connectedPlayer);
			}
		}
		if ((bool)muvs)
		{
			ptt.setVoiceRecord = false;
			muvs.OnOccupantEntered += Muvs_OnOccupantEntered;
		}
		VTOLMPSceneManager.instance.OnPlayerSpawnedInVehicle += OnPlayerSpawnedInVehicle;
		VTOLMPSceneManager.instance.OnPlayerUnspawnedVehicle += RemovePlayer;
		VTOLMPLobbyManager.OnPlayerLeft += RemovePlayer;
		VTNetworkVoice.instance.sendWhitelist = teamWhitelist;
	}

	private void Muvs_OnOccupantEntered(int seatIdx, ulong userID)
	{
		if (userID != BDSteamClient.mySteamID && muvs.IsLocalPlayerSeated())
		{
			PlayerInfo player = VTOLMPLobbyManager.GetPlayer(userID);
			if (player != null)
			{
				SetupVoiceSource(player);
			}
		}
	}

	private void OnDestroy()
	{
		if ((bool)VTNetworkVoice.instance)
		{
			VTNetworkVoice.instance.sendWhitelist = null;
		}
		if ((bool)muvs)
		{
			muvs.OnOccupantEntered -= Muvs_OnOccupantEntered;
		}
		if ((bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnPlayerSpawnedInVehicle -= OnPlayerSpawnedInVehicle;
			VTOLMPSceneManager.instance.OnPlayerUnspawnedVehicle -= RemovePlayer;
		}
		VTOLMPLobbyManager.OnPlayerLeft -= RemovePlayer;
	}

	private void RemovePlayer(PlayerInfo player)
	{
		if (player == null)
		{
			return;
		}
		if (voicePositions.TryGetValue(player, out var value))
		{
			if ((bool)value)
			{
				Object.Destroy(value.gameObject);
			}
			voicePositions.Remove(player);
		}
		teamWhitelist.Remove(player.steamUser.Id);
	}

	private void SetupVoiceSource(PlayerInfo player)
	{
		if (player.steamUser.IsMe)
		{
			return;
		}
		RemovePlayer(player);
		GameObject gameObject = new GameObject(player.pilotName + " voice");
		gameObject.SetActive(value: false);
		VTNetworkVoicePosition vTNetworkVoicePosition = gameObject.AddComponent<VTNetworkVoicePosition>();
		Teams team = VTOLMPLobbyManager.localPlayerInfo.team;
		vTNetworkVoicePosition.radioStartStop = true;
		vTNetworkVoicePosition.startStopSource = startStopSource;
		if (player.team == team)
		{
			if ((bool)muvs && muvs.IsPlayerSeated(player.steamUser.Id))
			{
				vTNetworkVoicePosition.SetMixerGroup(CommRadioManager.instance.copilotMixerGroup);
				copilotWhitelist.Add(player.steamUser.Id);
				teamWhitelist.Add(player.steamUser.Id);
				gameObject.transform.parent = copilotSourcePosition;
				vTNetworkVoicePosition.startStopSource = copilotStartStopSource;
				vTNetworkVoicePosition.radioStartStop = true;
				vTNetworkVoicePosition.copilot = true;
			}
			else
			{
				vTNetworkVoicePosition.SetMixerGroup(CommRadioManager.instance.mixerGroup);
				teamWhitelist.Add(player.steamUser.Id);
				gameObject.transform.parent = voiceSourcePosition;
			}
		}
		else
		{
			vTNetworkVoicePosition.SetMixerGroup(CommRadioManager.instance.opforMixerGroup);
			gameObject.transform.parent = opforSourcePosition;
		}
		gameObject.transform.localPosition = Vector3.zero;
		vTNetworkVoicePosition.useParentEntity = false;
		vTNetworkVoicePosition.overrideUserId = player.steamUser.Id;
		vTNetworkVoicePosition.minDistance = minDistance;
		vTNetworkVoicePosition.maxDistance = maxDistance;
		vTNetworkVoicePosition._3DBlend = 1f;
		voicePositions.Add(player, vTNetworkVoicePosition);
		gameObject.SetActive(value: true);
	}

	private void OnPlayerSpawnedInVehicle(PlayerInfo player)
	{
		SetupVoiceSource(player);
	}

	public void BeginTransmitAll()
	{
		if (isMp)
		{
			transmitAll = true;
		}
	}

	public void EndTransmitAll()
	{
		if (isMp)
		{
			transmitAll = false;
		}
	}

	public void SetCopilotVoiceEnabled(int k)
	{
		copilotVoiceEnabled = k > 0;
	}

	public void SetOutputChannel(int k)
	{
		if (k == 0)
		{
			EndTransmitAll();
		}
		else
		{
			BeginTransmitAll();
		}
	}

	public void SetKnob(int val)
	{
		knobState = val;
		switch (val)
		{
		case 0:
			ptt.enabled = false;
			if (isMp)
			{
				VTNetworkVoice.instance.SetVoiceRecord(r: false);
			}
			if ((bool)indicator)
			{
				indicator.SetStatus(0);
			}
			commPage.SetRecognition(0);
			break;
		case 1:
			ptt.enabled = true;
			if (isMp && ptt.setVoiceRecord)
			{
				VTNetworkVoice.instance.SetVoiceRecord(r: false);
			}
			commPage.SetRecognition(0);
			break;
		case 2:
			ptt.enabled = false;
			if (isMp)
			{
				VTNetworkVoice.instance.SetVoiceRecord(r: true);
			}
			commPage.SetRecognition(1);
			break;
		}
	}

	private void Update()
	{
		if (knobState > 0)
		{
			if ((bool)indicator)
			{
				if (isMp && (bool)VTNetworkVoice.instance)
				{
					if (VTNetworkVoice.instance.isVoiceRecording)
					{
						indicator.SetStatus(1);
						if (VTNetworkVoice.VoiceInputLevel > 0.01f || !VTOLMPUtils.IsMultiplayer())
						{
							indicator.SetColor(Color.green);
						}
						else
						{
							indicator.SetColor(Color.yellow);
						}
					}
					else
					{
						indicator.SetStatus(0);
					}
				}
				else
				{
					bool flag = ptt.isVoiceOn || knobState == 2;
					indicator.SetStatus(flag ? 1 : 0);
				}
			}
			if (knobState == 1)
			{
				commPage.SetRecognition(ptt.isVoiceOn ? 1 : 0);
				if (!isMp)
				{
					return;
				}
				if ((bool)muvs)
				{
					if (ptt.isVoiceOn)
					{
						VTNetworkVoice.instance.SetVoiceRecord(r: true);
						VTNetworkVoice.instance.sendWhitelist = (transmitAll ? null : teamWhitelist);
						return;
					}
					VTNetworkVoice.instance.SetVoiceRecord(copilotVoiceEnabled);
					if (copilotVoiceEnabled)
					{
						VTNetworkVoice.instance.sendWhitelist = copilotWhitelist;
					}
				}
				else
				{
					if (!ptt.setVoiceRecord)
					{
						VTNetworkVoice.instance.SetVoiceRecord(ptt.isVoiceOn);
					}
					VTNetworkVoice.instance.sendWhitelist = (transmitAll ? null : teamWhitelist);
				}
			}
			else if (knobState == 2 && isMp)
			{
				VTNetworkVoice.instance.SetVoiceRecord(r: true);
				VTNetworkVoice.instance.sendWhitelist = (transmitAll ? null : teamWhitelist);
			}
		}
		else if (isMp && (bool)VTNetworkVoice.instance && (bool)muvs && muvs.IsLocalPlayerSeated())
		{
			VTNetworkVoice.instance.SetVoiceRecord(copilotVoiceEnabled);
			if (copilotVoiceEnabled)
			{
				VTNetworkVoice.instance.sendWhitelist = copilotWhitelist;
			}
		}
	}
}

}