using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class ExteriorLightSync : VTNetSyncRPCOnly
{
	public NavLightController navLights;

	public StrobeLightController strobeLights;

	public ObjectPowerUnit landingLights;

	public FormationGlowController formationLights;

	public Battery battery;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			if ((bool)navLights)
			{
				navLights.OnSetPower += NavLights_OnSetPower;
			}
			if ((bool)strobeLights)
			{
				strobeLights.OnSetPower += StrobeLights_OnSetPower;
			}
			if ((bool)landingLights)
			{
				landingLights.OnPowerSwitched += LandingLights_OnPowerSwitched;
			}
			if ((bool)formationLights)
			{
				formationLights.OnSetState += FormationLights_OnSetState;
			}
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
			Refresh(0uL);
		}
		else if ((bool)battery)
		{
			battery.SetToRemote();
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
		if (base.isMine && (bool)battery)
		{
			SendRPC("RPC_Battery", BatteryAlive() ? 1 : 0);
			StartCoroutine(BatteryCheckRoutine());
		}
	}

	private void FormationLights_OnSetState(int on)
	{
		SendRPC("RPC_Formation", on);
	}

	[VTRPC]
	private void RPC_Formation(int on)
	{
		if ((bool)formationLights)
		{
			formationLights.SetStatus(on);
		}
	}

	private void OnDestroy()
	{
		if ((bool)VTNetworkManager.instance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		Refresh(obj);
	}

	private void Refresh(ulong target = 0uL)
	{
		if (base.isMine)
		{
			if ((bool)battery)
			{
				SendDirectedRPC(target, "RPC_Battery", BatteryAlive() ? 1 : 0);
			}
			if ((bool)landingLights)
			{
				SendDirectedRPC(target, "RPC_Landing", landingLights.isConnected ? 1 : 0);
			}
			if ((bool)strobeLights)
			{
				SendDirectedRPC(target, "RPC_Strobe", strobeLights.state);
			}
			if ((bool)navLights)
			{
				SendDirectedRPC(target, "RPC_Nav", navLights.state);
			}
			if ((bool)formationLights)
			{
				SendDirectedRPC(target, "RPC_Formation", formationLights.currentState);
			}
		}
	}

	private IEnumerator BatteryCheckRoutine()
	{
		int state = (BatteryAlive() ? 1 : 0);
		WaitForSeconds wait = new WaitForSeconds(1f);
		while (base.enabled)
		{
			int num = (BatteryAlive() ? 1 : 0);
			if (num != state)
			{
				SendRPC("RPC_Battery", num);
				state = num;
			}
			yield return wait;
		}
	}

	private bool BatteryAlive()
	{
		return battery.Drain(0.001f * Time.deltaTime);
	}

	private void LandingLights_OnPowerSwitched(int obj)
	{
		SendRPC("RPC_Landing", obj);
	}

	[VTRPC]
	private void RPC_Landing(int p)
	{
		landingLights.SetConnection(p);
	}

	private void StrobeLights_OnSetPower(int obj)
	{
		SendRPC("RPC_Strobe", obj);
	}

	[VTRPC]
	private void RPC_Strobe(int p)
	{
		strobeLights.SetStrobePower(p);
	}

	private void NavLights_OnSetPower(int obj)
	{
		SendRPC("RPC_Nav", obj);
	}

	[VTRPC]
	private void RPC_Nav(int p)
	{
		navLights.SetPower(p);
	}

	[VTRPC]
	private void RPC_Battery(int c)
	{
		battery.SetConnection(c);
	}
}

}