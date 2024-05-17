using System;
using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class WeaponManagerSync : VTNetSyncRPCOnly
{
	public WeaponManager wm;

	public CountermeasureManager cmm;

	public MultiUserVehicleSync muvs;

	public ExternalOptionalHardpoints extPylons;

	public bool doRemoteSync;

	private bool hasEquipped;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if ((bool)wm && doRemoteSync)
		{
			wm.SetRemoteWmSync(this);
		}
		if (base.isMine)
		{
			if ((bool)cmm)
			{
				cmm.OnFiredCM += Cmm_OnFiredCM;
			}
		}
		else if ((bool)cmm)
		{
			foreach (Countermeasure countermeasure in cmm.countermeasures)
			{
				countermeasure.SetRemote();
			}
		}
		if ((bool)muvs)
		{
			muvs.OnOccupantEntered += Muvs_OnOccupantEntered;
			muvs.OnOccupantLeft += Muvs_OnOccupantLeft;
			if (muvs.IsLocalPlayerSeated())
			{
				cmm.OnToggledCM -= Cmm_OnToggledCM;
				cmm.OnToggledCM += Cmm_OnToggledCM;
			}
		}
	}

	private void Muvs_OnOccupantLeft(int seatIdx, ulong userID)
	{
		if (userID == BDSteamClient.mySteamID)
		{
			cmm.OnToggledCM -= Cmm_OnToggledCM;
		}
	}

	private void Muvs_OnOccupantEntered(int seatIdx, ulong userID)
	{
		if (userID == BDSteamClient.mySteamID)
		{
			cmm.OnToggledCM -= Cmm_OnToggledCM;
			cmm.OnToggledCM += Cmm_OnToggledCM;
		}
		if (base.isMine)
		{
			for (int i = 0; i < cmm.countermeasures.Count; i++)
			{
				Cmm_OnToggledCM(i, cmm.countermeasures[i].enabled);
			}
		}
	}

	private void Cmm_OnToggledCM(int cmIdx, bool _enabled)
	{
		int num = (_enabled ? 2 : 0) | cmIdx;
		muvs.SendRPCToCopilots(this, "RPC_SetCM", num);
	}

	[VTRPC]
	private void RPC_SetCM(int enabled_cmIdx)
	{
		bool flag = (enabled_cmIdx & 2) == 2;
		int num = enabled_cmIdx & 1;
		if (cmm.countermeasures[num].enabled != flag)
		{
			cmm.OnToggledCM -= Cmm_OnToggledCM;
			cmm.SetCM(num, flag ? 1 : 0);
			cmm.OnToggledCM += Cmm_OnToggledCM;
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
		if (base.isMine && (bool)wm)
		{
			VTNetworkManager.instance.OnNewClientConnected -= SendAttachEqsToNewClient;
			VTNetworkManager.instance.OnNewClientConnected += SendAttachEqsToNewClient;
		}
	}

	private void OnDisable()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= SendAttachEqsToNewClient;
		}
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= SendAttachEqsToNewClient;
		}
	}

	private void SendAttachEqsToNewClient(SteamId obj)
	{
		if (!base.gameObject)
		{
			Debug.LogError("Somehow 'SendAttachEqsToNewClient' was called but we don't have a gameobject.");
			if (VTNetworkManager.hasInstance)
			{
				VTNetworkManager.instance.OnNewClientConnected -= SendAttachEqsToNewClient;
			}
			return;
		}
		if (!wm)
		{
			Debug.LogError("Called RPC_AttachEq but no wm (" + UIUtils.GetHierarchyString(base.gameObject) + ")", base.gameObject);
		}
		if (!hasEquipped)
		{
			return;
		}
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if (!equip)
			{
				continue;
			}
			VTNetEntity vTNetEntity = null;
			try
			{
				vTNetEntity = equip.gameObject.GetComponent<VTNetEntity>();
			}
			catch (NullReferenceException arg)
			{
				Debug.LogError($"Somehow we have an eq that doesn't have a game object. \n{arg}");
			}
			finally
			{
				if ((bool)vTNetEntity)
				{
					SendDirectedRPC(obj, "RPC_AttachEq", vTNetEntity.entityID, i);
				}
			}
		}
	}

	private void Wm_OnWeaponUnequippedHPIdx(int arg0)
	{
		throw new NotImplementedException();
	}

	private void Cmm_OnFiredCM()
	{
		int num = 0;
		for (int i = 0; i < cmm.countermeasures.Count; i++)
		{
			if (cmm.countermeasures[i].enabled)
			{
				num |= 1 << i;
			}
		}
		SendRPC("RPC_CM", num);
	}

	[VTRPC]
	private void RPC_CM(int cmMask)
	{
		if (!muvs)
		{
			for (int i = 0; i < cmm.countermeasures.Count; i++)
			{
				bool flag = (cmMask & (1 << i)) == 1 << i;
				if ((bool)cmm.countermeasures[i] != flag)
				{
					cmm.ToggleCM(i);
				}
			}
		}
		cmm.FireSingleCM();
	}

	public void NetEquipWeapons(Loadout l, bool additive = false)
	{
		if ((bool)wm)
		{
			hasEquipped = true;
			StartCoroutine(NetEqRoutine(l, additive));
		}
	}

	private IEnumerator NetEqRoutine(Loadout l, bool additive)
	{
		string[] hps = l.hpLoadout;
		MassUpdater component = wm.GetComponent<MassUpdater>();
		for (int j = 0; j < wm.equipCount; j++)
		{
			HPEquippable equip = wm.GetEquip(j);
			if (equip != null && (!additive || !string.IsNullOrEmpty(hps[j])))
			{
				IMassObject[] componentsInChildren = equip.GetComponentsInChildren<IMassObject>();
				foreach (IMassObject o in componentsInChildren)
				{
					component.RemoveMassObject(o);
				}
				equip.OnUnequip();
				wm.InvokeUnequipEvent(j);
				VTNetworkManager.NetDestroyObject(equip.gameObject);
				equip.transform.parent = null;
			}
		}
		for (int i = 0; i < wm.hardpointTransforms.Length && i < hps.Length; i++)
		{
			if (!string.IsNullOrEmpty(hps[i]))
			{
				string resourcePath = wm.resourcePath + "/" + hps[i];
				VTNetworkManager.NetInstantiateRequest req = VTNetworkManager.NetInstantiate(resourcePath, base.transform.position, base.transform.rotation);
				while (!req.isReady)
				{
					yield return null;
				}
				GameObject obj = req.obj;
				obj.transform.parent = wm.hardpointTransforms[i];
				obj.name = hps[i];
				obj.transform.localRotation = Quaternion.identity;
				obj.transform.localPosition = Vector3.zero;
				obj.transform.localScale = Vector3.one;
				obj.SetActive(value: true);
				SendRPC("RPC_AttachEq", obj.GetComponent<VTNetEntity>().entityID, i);
			}
		}
		if (l.cmLoadout != null)
		{
			CountermeasureManager componentInChildren = wm.GetComponentInChildren<CountermeasureManager>();
			if ((bool)componentInChildren)
			{
				for (int m = 0; m < componentInChildren.countermeasures.Count && m < l.cmLoadout.Length; m++)
				{
					componentInChildren.countermeasures[m].count = Mathf.Clamp(l.cmLoadout[m], 0, componentInChildren.countermeasures[m].maxCount);
					componentInChildren.countermeasures[m].UpdateCountText();
				}
			}
		}
		wm.ReattachWeapons();
		FuelTank component2 = wm.GetComponent<FuelTank>();
		component2.startingFuel = l.normalizedFuel * component2.maxFuel;
		component2.SetNormFuel(l.normalizedFuel);
	}

	public void NetClearWeapons()
	{
		if (!base.isMine || !wm)
		{
			return;
		}
		for (int i = 0; i < wm.equipCount; i++)
		{
			HPEquippable equip = wm.GetEquip(i);
			if ((bool)equip)
			{
				equip.transform.parent = null;
				VTNetworkManager.NetDestroyObject(equip.gameObject);
			}
		}
	}

	[VTRPC]
	private void RPC_AttachEq(int eqEntityId, int hpIdx)
	{
		Debug.Log($"RPC_AttachEq({eqEntityId}, {hpIdx})");
		VTNetEntity entity = VTNetworkManager.instance.GetEntity(eqEntityId);
		if (entity != null)
		{
			if ((bool)wm.GetEquip(hpIdx))
			{
				if (wm.GetEquip(hpIdx).gameObject == entity.gameObject)
				{
					return;
				}
				Debug.LogError("Tried to attach an equip on a remote WM when it already had an equip in that slot! (may be harmless)");
				wm.GetEquip(hpIdx).transform.parent = null;
			}
			Transform parent = wm.hardpointTransforms[hpIdx];
			entity.transform.parent = parent;
			entity.transform.localPosition = Vector3.zero;
			entity.transform.localRotation = Quaternion.identity;
			wm.ReattachWeapons();
			if ((bool)extPylons)
			{
				extPylons.Refresh();
			}
		}
		else
		{
			Debug.Log(" - eqEnt was null");
		}
	}

	public void RemoteStartFire()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_SF");
	}

	public void RemoteEndFire()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_EF");
	}

	public void RemoteWpnSwitchDown()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_WSDown");
	}

	public void RemoteWpnSwitchUp()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_WSUp");
	}

	[VTRPC]
	private void RPC_WSDown()
	{
		if (base.isMine)
		{
			wm.UserCycleActiveWeapon();
		}
	}

	[VTRPC]
	private void RPC_WSUp()
	{
		if (base.isMine)
		{
			wm.UserReleaseCycleActiveWeaponButton();
		}
	}

	[VTRPC]
	private void RPC_SF()
	{
		if (base.isMine)
		{
			wm.StartFire();
		}
	}

	[VTRPC]
	private void RPC_EF()
	{
		if (base.isMine)
		{
			wm.EndFire();
		}
	}
}

}