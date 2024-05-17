using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VTMapEdPrefab : MonoBehaviour
{
	public const string NODE_NAME = "StaticPrefab";

	public Vector3D globalPos;

	public string prefabName;

	public string category;

	[TextArea]
	public string prefabDescription;

	public bool disableCollisionInEditor = true;

	public UnityEvent OnSpawned;

	public bool readjustHeight = true;

	private IntVector2 chunkGrid;

	[HideInInspector]
	public int id;

	protected Bounds localPlacementBounds;

	private void Awake()
	{
		if (!GetComponent<FloatingOriginTransform>())
		{
			base.gameObject.AddComponent<FloatingOriginTransform>();
		}
		localPlacementBounds = CreateLocalPlacementBounds();
		if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.MapEditor && disableCollisionInEditor)
		{
			Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].gameObject.layer = 27;
			}
		}
	}

	private void Start()
	{
		if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.MapEditor && (bool)VTMapGenerator.fetch)
		{
			VTMapGenerator.fetch.OnChunkRecalculated += Fetch_OnChunkRecalculated;
		}
	}

	private void Fetch_OnChunkRecalculated(VTMapGenerator.VTTerrainChunk chunk)
	{
		if (chunk.grid == chunkGrid && chunk.collider.Raycast(new Ray(base.transform.position + new Vector3(0f, 10000f, 0f), Vector3.down), out var hitInfo, 20000f))
		{
			base.transform.position = hitInfo.point;
			globalPos = VTMapManager.WorldToGlobalPoint(hitInfo.point);
		}
	}

	private void OnDestroy()
	{
		if ((bool)VTMapGenerator.fetch)
		{
			VTMapGenerator.fetch.OnChunkRecalculated -= Fetch_OnChunkRecalculated;
		}
	}

	public virtual void OnPlacedInMap(VTMapEdPlacementInfo info)
	{
		globalPos = info.globalPos;
		chunkGrid = info.terrainChunk.grid;
		if (OnSpawned != null)
		{
			OnSpawned.Invoke();
		}
	}

	public virtual List<VTTerrainMod> GetTerrainMods()
	{
		return new List<VTTerrainMod>();
	}

	public void SaveToConfigNode(ConfigNode parentNode)
	{
		ConfigNode configNode = new ConfigNode("StaticPrefab");
		configNode.SetValue("prefab", base.gameObject.name);
		configNode.SetValue("id", id.ToString());
		configNode.SetValue("globalPos", ConfigNodeUtils.WriteVector3D(globalPos));
		configNode.SetValue("rotation", ConfigNodeUtils.WriteVector3(base.transform.rotation.eulerAngles));
		OnSavedToNode(configNode);
		parentNode.AddNode(configNode);
	}

	public void LoadFromConfigNode(ConfigNode objNode)
	{
		id = ConfigNodeUtils.ParseInt(objNode.GetValue("id"));
		globalPos = ConfigNodeUtils.ParseVector3D(objNode.GetValue("globalPos"));
		base.transform.rotation = Quaternion.Euler(ConfigNodeUtils.ParseVector3(objNode.GetValue("rotation")));
		base.transform.position = VTMapManager.GlobalToWorldPoint(globalPos);
		chunkGrid = VTMapGenerator.fetch.ChunkGridAtPos(base.transform.position);
		OnLoadedFromNode(objNode);
		if (OnSpawned != null)
		{
			OnSpawned.Invoke();
		}
	}

	public virtual string GetDisplayName()
	{
		return $"[{id}] {prefabName}";
	}

	protected virtual void OnSavedToNode(ConfigNode node)
	{
	}

	protected virtual void OnLoadedFromNode(ConfigNode node)
	{
	}

	protected virtual Bounds CreateLocalPlacementBounds()
	{
		Bounds result = default(Bounds);
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			Vector3 center = base.transform.InverseTransformPoint(collider.bounds.center);
			Vector3 size = collider.bounds.size;
			result.Encapsulate(new Bounds(center, size));
		}
		return result;
	}

	public Bounds GetLocalPlacementBounds()
	{
		return localPlacementBounds;
	}
}
