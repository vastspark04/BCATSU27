using System.Collections;
using Steamworks;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class AWACSRadioSync : VTNetSyncRPCOnly
{
	public AIAWACSSpawn awacs;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			awacs.OnDetectedActor += Awacs_OnDetectedActor;
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
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
		if (base.isMine)
		{
			int aWACSVoiceIndex = VTResources.GetAWACSVoiceIndex(awacs.awacsVoiceProfile);
			SendRPC("RPC_SetVoice", aWACSVoiceIndex);
		}
	}

	[VTRPC]
	private void RPC_SetVoice(int voiceIdx)
	{
		awacs.awacsVoiceProfile = VTResources.GetAWACSVoice(voiceIdx);
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		foreach (Actor knownHostile in awacs.GetKnownHostiles())
		{
			if ((bool)knownHostile)
			{
				SendDirectedRPC(obj, "RPC_RefreshA", VTNetUtils.GetActorIdentifier(knownHostile));
			}
		}
	}

	private void Awacs_OnDetectedActor(Actor a)
	{
		if ((bool)a)
		{
			SendRPC("RPC_AWACSDet", VTNetUtils.GetActorIdentifier(a));
		}
	}

	[VTRPC]
	private void RPC_AWACSDet(int actorId)
	{
		Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorId);
		if ((bool)actorFromIdentifier)
		{
			awacs.AwacsDetectedActor(actorFromIdentifier);
		}
	}

	[VTRPC]
	private void RPC_RefreshA(int actorId)
	{
		Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorId);
		if ((bool)actorFromIdentifier)
		{
			awacs.AwacsDetectedActor(actorFromIdentifier, callPopups: false);
		}
	}
}

}