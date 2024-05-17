using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VTMapEdStructurePrefab : VTMapEdPrefab
{
	[Header("Structure Prefab")]
	public bool doFlatten;

	public Bounds flattenRect;

	public float flattenRadius;

	public bool lockAltitude;

	public float lockedAltitude;

	public GameObject[] groundColorModels;

	private IntVector2 grid;

	private Vector3 tSpacePos;

	private Matrix4x4 terrainToLocalMatrix;

	private List<Mesh> meshesToDestroy = new List<Mesh>();

	private void Start()
	{
		if ((bool)VTMapGenerator.fetch)
		{
			VTMapGenerator fetch = VTMapGenerator.fetch;
			GameObject[] array = groundColorModels;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].GetComponent<MeshRenderer>().sharedMaterial = fetch.biomeProfiles[(int)fetch.biome].terrainMaterial;
			}
		}
		if (groundColorModels != null && groundColorModels.Length != 0)
		{
			StartCoroutine(ApplyColorsRoutine());
		}
	}

	public override void OnPlacedInMap(VTMapEdPlacementInfo info)
	{
		base.OnPlacedInMap(info);
		grid = info.terrainChunk.grid;
		tSpacePos = info.terrainChunk.lodObjects[0].transform.InverseTransformPoint(base.transform.position);
		terrainToLocalMatrix = base.transform.worldToLocalMatrix * info.terrainChunk.lodObjects[0].transform.localToWorldMatrix;
		if (groundColorModels != null && groundColorModels.Length != 0)
		{
			StartCoroutine(ApplyColorsRoutine());
		}
	}

	private void OnDrawGizmosSelected()
	{
		new Vector3(flattenRect.center.x, 0f, flattenRect.center.y);
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, Vector3.one);
		Vector3 size = flattenRect.size;
		size.y = 0f;
		Gizmos.DrawWireCube(flattenRect.center, size);
		Gizmos.color = Color.red;
		Gizmos.DrawLine(flattenRect.center + new Vector3(0f, 0f, flattenRect.extents.z), flattenRect.center + new Vector3(0f, 0f, flattenRadius + flattenRect.extents.z));
		Gizmos.DrawLine(flattenRect.center - new Vector3(0f, 0f, flattenRect.extents.z), flattenRect.center - new Vector3(0f, 0f, flattenRadius + flattenRect.extents.z));
		Gizmos.DrawLine(flattenRect.center + new Vector3(flattenRect.extents.x, 0f, 0f), flattenRect.center + new Vector3(flattenRadius + flattenRect.extents.x, 0f, 0f));
		Gizmos.DrawLine(flattenRect.center - new Vector3(flattenRect.extents.x, 0f, 0f), flattenRect.center - new Vector3(flattenRadius + flattenRect.extents.x, 0f, 0f));
		Gizmos.matrix = Matrix4x4.identity;
	}

	public override List<VTTerrainMod> GetTerrainMods()
	{
		List<VTTerrainMod> terrainMods = base.GetTerrainMods();
		if (doFlatten)
		{
			VTTModFlatRect vTTModFlatRect = new VTTModFlatRect();
			vTTModFlatRect.transform = base.transform;
			vTTModFlatRect.terrainToLocalMatrix = terrainToLocalMatrix;
			Bounds bounds = flattenRect;
			Vector3 center = bounds.center;
			center.y = base.transform.position.y - VTMapManager.GlobalToWorldPoint(Vector3D.zero).y;
			bounds.center = center;
			vTTModFlatRect.bounds = bounds;
			vTTModFlatRect.radius = flattenRadius;
			vTTModFlatRect.tSpacePos = tSpacePos;
			vTTModFlatRect.gridSpace = grid;
			terrainMods.Add(vTTModFlatRect);
		}
		return terrainMods;
	}

	protected override void OnSavedToNode(ConfigNode node)
	{
		base.OnSavedToNode(node);
		node.SetValue("grid", ConfigNodeUtils.WriteIntVector2(grid));
		node.SetValue("tSpacePos", ConfigNodeUtils.WriteVector3(tSpacePos));
		node.SetValue("terrainToLocalMatrix", ConfigNodeUtils.WriteMatrix(terrainToLocalMatrix));
	}

	protected override void OnLoadedFromNode(ConfigNode node)
	{
		base.OnLoadedFromNode(node);
		grid = ConfigNodeUtils.ParseIntVector2(node.GetValue("grid"));
		tSpacePos = ConfigNodeUtils.ParseVector3(node.GetValue("tSpacePos"));
		terrainToLocalMatrix = ConfigNodeUtils.ParseMatrix(node.GetValue("terrainToLocalMatrix"));
	}

	protected override Bounds CreateLocalPlacementBounds()
	{
		Bounds result = base.CreateLocalPlacementBounds();
		if (doFlatten)
		{
			Vector3 size = result.size;
			size.x = flattenRect.size.x + flattenRadius * 2f;
			size.z = flattenRect.size.z + flattenRadius * 2f;
			result.size = size;
		}
		return result;
	}

	private IEnumerator ApplyColorsRoutine()
	{
		yield return null;
		while (VTMapGenerator.fetch.IsGenerating())
		{
			yield return null;
		}
		yield return null;
		ApplyGroundColors();
	}

	private void OnDestroy()
	{
		foreach (Mesh item in meshesToDestroy)
		{
			Object.Destroy(item);
		}
		meshesToDestroy.Clear();
	}

	private void ApplyGroundColors()
	{
		GameObject[] array = groundColorModels;
		foreach (GameObject gameObject in array)
		{
			gameObject.GetComponent<MeshRenderer>();
			MeshFilter component = gameObject.GetComponent<MeshFilter>();
			Mesh mesh = component.mesh;
			meshesToDestroy.Add(mesh);
			Vector3[] vertices = mesh.vertices;
			List<Color> list = new List<Color>();
			List<Vector3> list2 = new List<Vector3>();
			for (int j = 0; j < vertices.Length; j++)
			{
				Vector3 pos = gameObject.transform.TransformPoint(vertices[j]);
				list.Add(VTMapGenerator.fetch.GetTerrainColor(pos, out var worldNormal));
				worldNormal = component.transform.InverseTransformDirection(worldNormal);
				list2.Add(worldNormal);
			}
			mesh.SetNormals(list2);
			mesh.SetColors(list);
			component.sharedMesh = mesh;
		}
	}
}
