using System;
using System.Collections.Generic;
using UnityEngine;

public class VTStaticObject : MonoBehaviour
{
	[Serializable]
	public struct SpawnObject
	{
		public GameObject gObj;

		public bool releaseOnSpawn;

		public bool createFloatingOriginTf;
	}

	public const string NODE_NAME = "StaticObject";

	public string objectName;

	public string category;

	public string description;

	public bool editorOnly;

	public bool alignToSurface = true;

	public UnitSpawn.PlacementModes placementMode;

	public List<SpawnObject> spawnObjects = new List<SpawnObject>();

	private int _id;

	private Vector3D globalPos;

	private bool spawned;

	private List<GameObject> objsToDespawn = new List<GameObject>();

	private bool despawned;

	public int id => _id;

	public void MoveInEditor()
	{
		OnPlacedInEditor();
	}

	public UnitSpawner.PlacementValidityInfo TryPlaceInEditor(ScenarioEditorCamera.CursorLocations cursorPos)
	{
		UnitSpawner.PlacementValidityInfo result = default(UnitSpawner.PlacementValidityInfo);
		if (placementMode == UnitSpawn.PlacementModes.Air && cursorPos != 0)
		{
			result.isValid = false;
			result.reason = "This can only be placed in the air!";
		}
		else if (placementMode == UnitSpawn.PlacementModes.Ground && cursorPos != ScenarioEditorCamera.CursorLocations.Ground)
		{
			result.isValid = false;
			result.reason = "This can only be placed on the ground!";
		}
		else if (placementMode == UnitSpawn.PlacementModes.Water && cursorPos != ScenarioEditorCamera.CursorLocations.Water)
		{
			result.isValid = false;
			result.reason = "This can only be placed in the water!";
		}
		else if (placementMode == UnitSpawn.PlacementModes.GroundOrAir && cursorPos == ScenarioEditorCamera.CursorLocations.Water)
		{
			result.isValid = false;
			result.reason = "This can't be placed in the water!";
		}
		else
		{
			result.isValid = true;
			globalPos = VTMapManager.WorldToGlobalPoint(base.transform.position);
			OnPlacedInEditor();
		}
		return result;
	}

	public void Spawn()
	{
		if (!spawned)
		{
			spawned = true;
			if ((bool)VTMapGenerator.fetch)
			{
				VTMapGenerator.fetch.BakeColliderAtPosition(base.transform.position);
			}
			OnSpawned();
			FlightSceneManager.instance.OnExitScene += Despawn;
		}
	}

	protected virtual void OnSpawned()
	{
		if (spawnObjects == null)
		{
			return;
		}
		foreach (SpawnObject spawnObject in spawnObjects)
		{
			Transform[] componentsInChildren = spawnObject.gObj.GetComponentsInChildren<Transform>(includeInactive: true);
			foreach (Transform transform in componentsInChildren)
			{
				objsToDespawn.Add(transform.gameObject);
			}
			spawnObject.gObj.SetActive(value: true);
			if (spawnObject.releaseOnSpawn)
			{
				spawnObject.gObj.transform.parent = null;
			}
			if (spawnObject.createFloatingOriginTf)
			{
				spawnObject.gObj.AddComponent<FloatingOriginTransform>();
			}
		}
	}

	private void OnDestroy()
	{
		if (spawned && !despawned)
		{
			Despawn();
		}
	}

	private void Despawn()
	{
		if (despawned)
		{
			return;
		}
		despawned = true;
		if (objsToDespawn != null)
		{
			foreach (GameObject item in objsToDespawn)
			{
				if ((bool)item)
				{
					UnityEngine.Object.Destroy(item);
				}
			}
		}
		OnDespawned();
	}

	protected virtual void OnDespawned()
	{
	}

	protected virtual void OnPlacedInEditor()
	{
		if (!alignToSurface)
		{
			Vector3 forward = base.transform.forward;
			forward.y = 0f;
			base.transform.rotation = Quaternion.LookRotation(forward);
		}
	}

	public virtual void OnLoadedFromConfig()
	{
	}

	public void SetNewID(int newID)
	{
		_id = newID;
	}

	public void SetGlobalPosition(Vector3D gp)
	{
		globalPos = gp;
		base.transform.position = VTMapManager.GlobalToWorldPoint(gp);
	}

	public string GetUIDisplayName()
	{
		return $"{objectName} [{id}]";
	}

	public ConfigNode SaveToConfigNode()
	{
		ConfigNode configNode = new ConfigNode("StaticObject");
		configNode.SetValue("prefabID", base.gameObject.name);
		configNode.SetValue("id", id);
		configNode.SetValue("globalPos", ConfigNodeUtils.WriteVector3D(globalPos));
		configNode.SetValue("rotation", ConfigNodeUtils.WriteVector3(base.transform.rotation.eulerAngles));
		return configNode;
	}

	public static VTStaticObject LoadFromConfigNode(ConfigNode objectNode)
	{
		string value = objectNode.GetValue("prefabID");
		GameObject staticObjectPrefab = VTResources.GetStaticObjectPrefab(value);
		if ((bool)staticObjectPrefab)
		{
			GameObject obj = UnityEngine.Object.Instantiate(staticObjectPrefab);
			obj.name = value;
			VTStaticObject component = obj.GetComponent<VTStaticObject>();
			component._id = objectNode.GetValue<int>("id");
			Vector3D value2 = objectNode.GetValue<Vector3D>("globalPos");
			component.SetGlobalPosition(value2);
			Vector3 value3 = objectNode.GetValue<Vector3>("rotation");
			component.transform.rotation = Quaternion.Euler(value3);
			return component;
		}
		return null;
	}
}
