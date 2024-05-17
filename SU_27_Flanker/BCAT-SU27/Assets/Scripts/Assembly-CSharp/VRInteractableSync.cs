using System;
using VTNetworking;

public class VRInteractableSync : VTNetSyncRPCOnly
{
	public VRInteractable vrInt;

	public bool exclusive;

	private bool isRightCon;

	private ulong exclusiveUser;

	public event Action<ulong> OnSetExclusiveUser;

	protected override void Awake()
	{
		base.Awake();
		vrInt.OnInteract.AddListener(OnInteract);
		vrInt.OnStopInteract.AddListener(OnStopInteract);
	}

	private void OnInteract()
	{
		if (exclusive && exclusiveUser != 0L && exclusiveUser != BDSteamClient.mySteamID)
		{
			vrInt.activeController.ReleaseFromInteractable();
			return;
		}
		isRightCon = !vrInt.activeController.isLeft;
		SendInteractRPC(isRightCon, interacting: true);
		if (exclusive && base.isMine)
		{
			if (exclusiveUser == 0L)
			{
				ulong num = (exclusiveUser = BDSteamClient.mySteamID);
				SendRPC("SetExclusiveUser", num);
				this.OnSetExclusiveUser?.Invoke(exclusiveUser);
			}
			else if (exclusiveUser != BDSteamClient.mySteamID && (bool)vrInt.activeController)
			{
				vrInt.activeController.ReleaseFromInteractable();
			}
		}
	}

	private void OnStopInteract()
	{
		SendInteractRPC(isRightCon, interacting: false);
		if (exclusive && exclusiveUser == BDSteamClient.mySteamID)
		{
			exclusiveUser = 0uL;
			SendRPC("SetExclusiveUser", 0);
			this.OnSetExclusiveUser?.Invoke(exclusiveUser);
		}
	}

	private void SendInteractRPC(bool isRight, bool interacting)
	{
		int num = 0;
		if (isRight)
		{
			num |= 1;
		}
		if (interacting)
		{
			num |= 2;
		}
		SendRPC("SetInteract", num, BDSteamClient.mySteamID);
	}

	public ulong GetCurrentExclusiveUser()
	{
		return exclusiveUser;
	}

	[VTRPC]
	private void SetInteract(int iCode, ulong id)
	{
		if (exclusive && base.isMine)
		{
			if (exclusiveUser == 0L)
			{
				exclusiveUser = id;
				SendRPC("SetExclusiveUser", id);
				this.OnSetExclusiveUser?.Invoke(exclusiveUser);
			}
			else if (exclusiveUser != id)
			{
				SendRPC("UnInteract", id);
			}
		}
		bool rightHand = (iCode & 1) == 1;
		bool flag = (iCode & 2) == 2;
		PlayerModelSync playerModel = PlayerModelSync.GetPlayerModel(id);
		if ((bool)playerModel)
		{
			playerModel.SetRemoteInteractable(rightHand, flag ? vrInt : null);
		}
	}

	[VTRPC]
	private void SetExclusiveUser(ulong id)
	{
		exclusiveUser = id;
		this.OnSetExclusiveUser?.Invoke(exclusiveUser);
	}

	[VTRPC]
	private void UnInteract(ulong id)
	{
		if (id == BDSteamClient.mySteamID && (bool)vrInt.activeController)
		{
			vrInt.activeController.ReleaseFromInteractable();
		}
	}
}
