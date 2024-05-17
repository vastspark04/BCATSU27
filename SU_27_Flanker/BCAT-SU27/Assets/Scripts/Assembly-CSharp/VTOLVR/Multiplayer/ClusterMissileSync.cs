using VTNetworking;

namespace VTOLVR.Multiplayer{

public class ClusterMissileSync : VTNetSyncRPCOnly
{
	public ClusterMissile cMissile;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			cMissile.OnSubLaunch += CMissile_OnSubLaunch;
			cMissile.OnFiredSubmissile += CMissile_OnFiredSubmissile;
		}
		else
		{
			cMissile.MP_SetRemote();
		}
	}

	private void CMissile_OnFiredSubmissile(Actor tgt)
	{
		SendRPC("RPC_SubMissile", VTNetUtils.GetActorIdentifier(tgt));
	}

	[VTRPC]
	private void RPC_SubMissile(int tgtActorId)
	{
		Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(tgtActorId);
		if ((bool)actorFromIdentifier)
		{
			cMissile.RemoteFireSubmissile(actorFromIdentifier);
		}
	}

	private void CMissile_OnSubLaunch()
	{
		SendRPC("RPC_SubLaunch");
	}

	[VTRPC]
	private void RPC_SubLaunch()
	{
		cMissile.RemoteSubLaunch();
	}
}

}