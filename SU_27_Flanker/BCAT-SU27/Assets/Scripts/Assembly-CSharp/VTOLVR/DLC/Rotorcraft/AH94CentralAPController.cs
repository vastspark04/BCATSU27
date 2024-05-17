using System.Collections;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class AH94CentralAPController : VTNetSyncRPCOnly
{
	public VTOLAutoPilot vtolAp;

	public MultiUserVehicleSync muvs;

	public UIImageToggle[] hoverIndicator;

	public UIImageToggle[] navIndicator;

	public UIImageToggle[] headingIndicator;

	public UIImageToggle[] altitudeIndicator;

	private bool isMultiplayer => wasRegistered;

	private bool mpCanSetAP
	{
		get
		{
			if ((bool)muvs && muvs.OwnerIsLocked())
			{
				return muvs.IsControlOwner();
			}
			return true;
		}
	}

	protected override void Awake()
	{
		vtolAp.OnAltitudeHold += VtolAp_OnAltitudeHold;
		vtolAp.OnHeadingHold += VtolAp_OnHeadingHold;
		vtolAp.OnHoverMode += VtolAp_OnHoverMode;
		vtolAp.OnNavMode += VtolAp_OnNavMode;
	}

	private void OnEnable()
	{
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		while (!wasRegistered)
		{
			yield return null;
		}
		WaitForSeconds sendWait = new WaitForSeconds(VTNetworkManager.CurrentSendInterval);
		while (base.enabled)
		{
			if (muvs.IsControlOwner() && (vtolAp.headingHold || vtolAp.hoverMode))
			{
				SendHeadingToHold();
			}
			yield return sendWait;
		}
	}

	private void VtolAp_OnNavMode(bool apEnabled)
	{
		UIImageToggle[] array = navIndicator;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].imageEnabled = apEnabled;
		}
	}

	private void VtolAp_OnHoverMode(bool apEnabled)
	{
		UIImageToggle[] array = hoverIndicator;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].imageEnabled = apEnabled;
		}
	}

	private void VtolAp_OnHeadingHold(bool apEnabled)
	{
		UIImageToggle[] array = headingIndicator;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].imageEnabled = apEnabled;
		}
	}

	private void VtolAp_OnAltitudeHold(bool apEnabled)
	{
		Debug.Log($"OnAltHold({apEnabled})");
		UIImageToggle[] array = altitudeIndicator;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].imageEnabled = apEnabled;
		}
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if ((bool)muvs)
		{
			muvs.OnOccupantEntered += Muvs_OnOccupantEntered;
			vtolAp.OnTriggeredAltDisable += SendAltSync;
			vtolAp.OnTriggeredHeadingDisable += SendHeadingSync;
			vtolAp.OnTriggeredHoverDisable += SendHoverSync;
			vtolAp.OnTriggeredNavDisable += SendNavSync;
		}
	}

	private void SendHoverSync()
	{
		muvs.SendRPCToCopilots(this, "RPC_Hov", vtolAp.hoverMode ? 1 : 0);
	}

	private void SendNavSync()
	{
		muvs.SendRPCToCopilots(this, "RPC_Nav", vtolAp.navMode ? 1 : 0);
	}

	private void SendHeadingSync()
	{
		muvs.SendRPCToCopilots(this, "RPC_Head", vtolAp.headingHold ? 1 : 0);
	}

	private void SendAltSync()
	{
		muvs.SendRPCToCopilots(this, "RPC_Alt", vtolAp.altitudeHold ? 1 : 0);
	}

	private void SendHeadingToHold()
	{
		muvs.SendRPCToCopilots(this, "RPC_HdgToHold", vtolAp.headingToHold);
	}

	[VTRPC]
	private void RPC_HdgToHold(float h)
	{
		vtolAp.headingToHold = h;
	}

	private void Muvs_OnOccupantEntered(int seatIdx, ulong userID)
	{
		if (userID != BDSteamClient.mySteamID && ((base.isMine && !muvs.OwnerIsLocked()) || muvs.IsControlOwner()))
		{
			SendNavSync();
			SendHoverSync();
			SendHeadingSync();
			SendAltSync();
		}
	}

	[VTRPC]
	private void RPC_Nav(int e)
	{
		bool flag = e > 0;
		if (vtolAp.navMode != flag)
		{
			ToggleNavMode();
		}
	}

	[VTRPC]
	private void RPC_Hov(int e)
	{
		bool flag = e > 0;
		if (vtolAp.hoverMode != flag)
		{
			vtolAp.ToggleHoverMode();
		}
	}

	[VTRPC]
	private void RPC_Head(int e)
	{
		bool flag = e > 0;
		if (vtolAp.headingHold != flag)
		{
			vtolAp.ToggleHeadingHold();
		}
	}

	[VTRPC]
	private void RPC_Alt(int e)
	{
		bool flag = e > 0;
		if (vtolAp.altitudeHold != flag)
		{
			vtolAp.ToggleAltitudeHold();
		}
	}

	public void ToggleNavMode()
	{
		if (!isMultiplayer || mpCanSetAP)
		{
			vtolAp.ToggleNav();
			if (isMultiplayer)
			{
				SendNavSync();
			}
		}
	}

	public void ToggleHoverMode()
	{
		if (!isMultiplayer || mpCanSetAP)
		{
			vtolAp.ToggleHoverMode();
			if (isMultiplayer)
			{
				SendHoverSync();
			}
		}
	}

	public void ToggleHeadingHold()
	{
		if (!isMultiplayer || mpCanSetAP)
		{
			vtolAp.ToggleHeadingHold();
			if (isMultiplayer)
			{
				SendHeadingSync();
			}
		}
	}

	public void ToggleAltitudeHold()
	{
		if (!isMultiplayer || mpCanSetAP)
		{
			vtolAp.ToggleAltitudeHold();
			if (isMultiplayer)
			{
				SendAltSync();
			}
		}
	}

	public void AllAPOff()
	{
		if (!isMultiplayer || mpCanSetAP)
		{
			vtolAp.AllOff();
			if (isMultiplayer)
			{
				SendNavSync();
				SendHoverSync();
				SendHeadingSync();
				SendAltSync();
			}
		}
	}
}

}