using System.Collections.Generic;
using UnityEngine;

namespace VTNetworking{

public class VTNetSceneManager : MonoBehaviour
{
	public VTNetEntity[] sceneEntities;

	private int sceneEntitiesInitCount;

	public static VTNetSceneManager instance { get; private set; }

	public bool sceneEntitiesReady { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	public int GetIndex(VTNetEntity ent)
	{
		for (int i = 0; i < sceneEntities.Length; i++)
		{
			if (ent == sceneEntities[i])
			{
				return i;
			}
		}
		return -1;
	}

	public void InitClientSceneEntity(int idx, int entID, List<int> nsIds)
	{
		VTNetEntity vTNetEntity = instance.sceneEntities[idx];
		if (!vTNetEntity.wasRemoteInitialized)
		{
			vTNetEntity.wasRemoteInitialized = true;
			Debug.LogFormat("Initializing scene entity on client side.  Idx:{0}, entityID:{1}", idx, entID);
			vTNetEntity.entityID = entID;
			for (int i = 0; i < nsIds.Count; i++)
			{
				vTNetEntity.netSyncs[i].id = nsIds[i];
				VTNetworkManager.instance.RegisterRemoteNetSync(vTNetEntity.netSyncs[i]);
			}
			sceneEntitiesInitCount++;
			if (sceneEntitiesInitCount == sceneEntities.Length)
			{
				sceneEntitiesReady = true;
			}
		}
	}

	public void InitHostSceneEntity(int idx)
	{
		sceneEntitiesInitCount++;
		if (sceneEntitiesInitCount == sceneEntities.Length)
		{
			sceneEntitiesReady = true;
		}
	}
}

}