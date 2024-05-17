using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class EngineSync : VTNetSyncRPCOnly
{
	public ModuleEngine[] engines;

	public AuxilliaryPowerUnit apu;

	public bool syncThrottle;

	public bool twoWayAPUSync;

	private float[] currThrottles;

	private float[] syncedThrottles;

	private bool awaitingRefresh;

	protected override void Awake()
	{
		base.Awake();
		if (syncThrottle)
		{
			currThrottles = new float[engines.Length];
			syncedThrottles = new float[engines.Length];
		}
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			for (int i = 0; i < engines.Length; i++)
			{
				int engineIdx = i;
				engines[i].OnEngineState += delegate(int st)
				{
					OnEngineState(engineIdx, st);
				};
				engines[i].OnEngineStateImmediate += delegate(int st)
				{
					OnEngineStateImmediate(engineIdx, st);
				};
				OnEngineStateImmediate(i, engines[i].engineEnabled ? 1 : 0);
			}
			if ((bool)apu)
			{
				apu.OnSetState += Apu_OnSetState;
			}
			Refresh(0uL);
			VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		}
		else
		{
			ModuleEngine[] array = engines;
			foreach (ModuleEngine moduleEngine in array)
			{
				moduleEngine.fuelTank.remoteOnly = true;
				Debug.Log($"({moduleEngine.GetInstanceID()} remote engine net initialized");
			}
			if (twoWayAPUSync && (bool)apu)
			{
				apu.OnSetState += Apu_OnSetState;
			}
			awaitingRefresh = true;
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
			Refresh(0uL);
		}
		else
		{
			awaitingRefresh = true;
		}
	}

	private IEnumerator SyncThrottleRoutine()
	{
		if (base.isMine)
		{
			WaitForSeconds wait = new WaitForSeconds(VTNetworkManager.CurrentSendInterval);
			while (base.enabled)
			{
				for (int i = 0; i < engines.Length; i++)
				{
					SendRPC("RPC_SyncThrottle", i, engines[i].inputThrottle);
				}
				yield return wait;
			}
			yield break;
		}
		while (base.enabled)
		{
			for (int j = 0; j < engines.Length; j++)
			{
				currThrottles[j] = Mathf.Lerp(currThrottles[j], syncedThrottles[j], 7f * Time.deltaTime);
				engines[j].SetThrottle(currThrottles[j]);
			}
			yield return null;
		}
	}

	[VTRPC]
	private void RPC_SyncThrottle(int idx, float t)
	{
		syncedThrottles[idx] = t;
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
		Refresh(obj);
	}

	private void Apu_OnSetState(int state)
	{
		SendRPC("RPC_APU", state);
	}

	[VTRPC]
	private void RPC_APU(int state)
	{
		if ((bool)apu)
		{
			apu.RemoteSetPower(state > 0);
		}
	}

	private void OnEngineState(int engineIdx, int state)
	{
		SendRPC("RPC_EngineState", engineIdx, state);
	}

	private void OnEngineStateImmediate(int engineIdx, int state)
	{
		SendRPC("RPC_EngineStateImmediate", engineIdx, state);
	}

	[VTRPC]
	private void RPC_EngineState(int idx, int state)
	{
		Debug.Log($"({engines[idx].GetInstanceID()} engine RPC_EngineState({idx}, {state}) awaitingRefresh={awaitingRefresh}");
		if (state > 0 && engines[idx].failed)
		{
			engines[idx].FullyRepairEngine();
		}
		if (awaitingRefresh)
		{
			if (state > 0)
			{
				engines[idx].StartImmediate();
			}
			else
			{
				engines[idx].StopImmediate();
			}
			awaitingRefresh = false;
		}
		else
		{
			engines[idx].SetPower(state);
		}
	}

	[VTRPC]
	private void RPC_EngineStateImmediate(int idx, int state)
	{
		Debug.Log($"(engines[idx].{GetInstanceID()} engine RPC_EngineStateImmediate({idx}, {state})");
		if (state > 0 && engines[idx].failed)
		{
			engines[idx].FullyRepairEngine();
		}
		if (state > 0)
		{
			engines[idx].StartImmediate();
		}
		else
		{
			engines[idx].StopImmediate();
		}
		awaitingRefresh = false;
	}

	private void Refresh(ulong target = 0uL)
	{
		if (!base.isMine)
		{
			return;
		}
		for (int i = 0; i < engines.Length; i++)
		{
			if (target == 0L)
			{
				SendRPC("RPC_EngineStateImmediate", i, engines[i].engineEnabled ? 1 : 0);
			}
			else
			{
				SendDirectedRPC(target, "RPC_EngineStateImmediate", i, engines[i].engineEnabled ? 1 : 0);
			}
		}
		if ((bool)apu)
		{
			if (target == 0L)
			{
				SendRPC("RPC_APU", apu.isPowerEnabled ? 1 : 0);
			}
			else
			{
				SendDirectedRPC(target, "RPC_APU", apu.isPowerEnabled ? 1 : 0);
			}
		}
	}
}

}