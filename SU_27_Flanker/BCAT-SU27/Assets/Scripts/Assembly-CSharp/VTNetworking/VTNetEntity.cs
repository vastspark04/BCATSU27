using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace VTNetworking{

public class VTNetEntity : MonoBehaviour
{
	public List<VTNetSync> netSyncs;

	public bool isMine;

	public bool isSceneEntity;

	private bool gotSEIdx;

	private int _seIdx;

	public int entityID { get; set; }

	public ulong ownerID { get; set; }

	public Friend owner => new Friend(ownerID);

	public bool wasRemoteInitialized { get; set; }

	public int sceneEntityIdx
	{
		get
		{
			if (!isSceneEntity || !VTNetSceneManager.instance.sceneEntitiesReady)
			{
				return -1;
			}
			if (!gotSEIdx)
			{
				_seIdx = VTNetSceneManager.instance.GetIndex(this);
				gotSEIdx = true;
			}
			return _seIdx;
		}
	}

	public bool hasRegistered { get; private set; }

	private IEnumerator SceneObjectRoutine()
	{
		while (!VTNetSceneManager.instance)
		{
			yield return null;
		}
		while (VTNetworkManager.instance.connectionState != VTNetworkManager.ConnectionStates.Connected)
		{
			yield return null;
		}
		isMine = VTNetworkManager.instance.netState == VTNetworkManager.NetStates.IsHost;
		if (isMine)
		{
			VTNetworkManager.instance.RegisterSceneEntity(this);
		}
	}

	private void Awake()
	{
		if (isSceneEntity)
		{
			StartCoroutine(SceneObjectRoutine());
		}
		else if (!VTNetworkManager.hasInstance || VTNetworkManager.instance.connectionState != VTNetworkManager.ConnectionStates.Connected)
		{
			base.enabled = false;
			Object.Destroy(this);
		}
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.isActivated)
		{
			VTNetworkManager.instance.UnregisterNetEntity(this);
		}
	}

	public void RegisterSyncs()
	{
		if (hasRegistered)
		{
			return;
		}
		hasRegistered = true;
		foreach (VTNetSync netSync in netSyncs)
		{
			if (isMine)
			{
				VTNetworkManager.instance.RegisterMyNetSync(netSync);
			}
			else if ((bool)netSync)
			{
				VTNetworkManager.instance.RegisterRemoteNetSync(netSync);
			}
		}
	}

	internal void InitializeSyncs()
	{
		foreach (VTNetSync netSync in netSyncs)
		{
			if ((bool)netSync)
			{
				netSync.NetInitialize();
			}
		}
	}

	public void UnregisterSyncs()
	{
		if (!hasRegistered)
		{
			return;
		}
		foreach (VTNetSync netSync in netSyncs)
		{
			if ((bool)netSync)
			{
				if (isMine)
				{
					VTNetworkManager.instance.UnRegisterMyNetSync(netSync);
				}
				else
				{
					VTNetworkManager.instance.UnRegisterRemoteNetSync(netSync);
				}
			}
		}
		hasRegistered = false;
	}
}

}