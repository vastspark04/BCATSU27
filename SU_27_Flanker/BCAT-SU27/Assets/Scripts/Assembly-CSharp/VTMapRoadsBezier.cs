using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VTMapRoadsBezier : MonoBehaviour
{
	private struct RoadVertex
	{
		public Vector3 vertex;

		public Vector3 normal;

		public Vector2 uv;
	}

	private struct IntersectionMeshInfo
	{
		public VTTerrainMesh vtMesh;

		public VTTerrainMesh masterMesh;

		public int vtIndexOffset;

		public int[] vertIndices;

		public Transform transform;
	}

	private class ChunkRoadMeshes
	{
		public IntVector2 chunkGrid;

		public List<MeshFilter> uMeshes = new List<MeshFilter>();

		public Dictionary<int, VTTerrainMesh> segmentMeshes = new Dictionary<int, VTTerrainMesh>();
	}

	public static VTMapRoadsBezier instance;

	public BezierRoadProfile[] roadProfiles;

	public BezierRoadAdapters adapters;

	private VTMapCustom cMap;

	private Dictionary<IntVector2, ChunkRoadMeshes> chunkMeshes = new Dictionary<IntVector2, ChunkRoadMeshes>();

	private Stack<VTTerrainMesh> vtMeshPool = new Stack<VTTerrainMesh>();

	private Stack<MeshFilter> uMeshPool = new Stack<MeshFilter>();

	private FlightSceneManager.FlightSceneLoadItem loadItem;

	private List<BezierRoadSystem.BezierRoadIntersection> intersectionsToMerge = new List<BezierRoadSystem.BezierRoadIntersection>();

	private List<ChunkRoadMeshes> cMeshesToApply = new List<ChunkRoadMeshes>();

	private const float END_PIECE_T = 0.1f;

	private const float ADAPTER_T = 0.2f;

	private int createdMeshes;

	private List<Mesh> createdMeshesList = new List<Mesh>();

	private List<VTTMapTrees.TreeChunk> removalTreeChunks = new List<VTTMapTrees.TreeChunk>(5);

	private BezierRoadSystem roadSystem
	{
		get
		{
			if ((bool)cMap)
			{
				return cMap.roadSystem;
			}
			return null;
		}
	}

	private void OnDestroy()
	{
		DestroyMeshes();
	}

	private void DestroyMeshes()
	{
		int num = 0;
		if (chunkMeshes != null)
		{
			foreach (ChunkRoadMeshes value in chunkMeshes.Values)
			{
				if (value == null)
				{
					continue;
				}
				if (value.uMeshes != null)
				{
					value.uMeshes = null;
				}
				if (value.segmentMeshes == null)
				{
					continue;
				}
				foreach (VTTerrainMesh value2 in value.segmentMeshes.Values)
				{
					value2.Clear();
				}
				value.segmentMeshes = null;
			}
		}
		if (vtMeshPool != null)
		{
			while (vtMeshPool.Count > 0)
			{
				vtMeshPool.Pop().Clear();
			}
			vtMeshPool = null;
		}
		if (uMeshPool != null)
		{
			uMeshPool.Clear();
		}
		if (createdMeshesList != null)
		{
			foreach (Mesh createdMeshes in createdMeshesList)
			{
				if ((bool)createdMeshes)
				{
					UnityEngine.Object.DestroyImmediate(createdMeshes);
					num++;
				}
			}
		}
		Debug.LogFormat("VTMapRoadsBezier destroyed {0} out of {1} meshes", num, this.createdMeshes);
		this.createdMeshes = 0;
	}

	[ContextMenu("Regenerate All")]
	public void RegenerateAll()
	{
		if (roadSystem == null)
		{
			return;
		}
		foreach (IntVector2 key in roadSystem.roadChunks.Keys)
		{
			ReturnChunkToPool(key);
		}
		foreach (IntVector2 key2 in roadSystem.roadChunks.Keys)
		{
			GenerateChunk(VTMapGenerator.fetch.GetTerrainChunk(key2));
		}
		foreach (BezierRoadSystem.BezierRoadIntersection item in intersectionsToMerge)
		{
			MergeIntersectionVerts(item);
		}
		intersectionsToMerge.Clear();
		foreach (ChunkRoadMeshes item2 in cMeshesToApply)
		{
			ApplyMeshes(item2);
		}
		cMeshesToApply.Clear();
	}

	private IEnumerator RegenerateAllAsync()
	{
		Debug.Log("Regenerating roads Async");
		foreach (IntVector2 key in roadSystem.roadChunks.Keys)
		{
			ReturnChunkToPool(key);
		}
		roadSystem.dirtyChunks.Clear();
		foreach (IntVector2 c in roadSystem.roadChunks.Keys)
		{
			VTMapGenerator.VTTerrainChunk chunk = null;
			while (chunk == null)
			{
				chunk = VTMapGenerator.fetch.GetTerrainChunk(c);
				if (chunk != null)
				{
					while (chunk.lodObjects == null || chunk.lodObjects[0] == null || chunk.terrainMeshes == null || chunk.terrainMeshes[0] == null)
					{
						yield return null;
					}
					if (!chunk.colliderBaked)
					{
						VTMapGenerator.fetch.BakeCollider(chunk.grid);
						yield return null;
					}
					GenerateChunk(chunk);
					yield return null;
					if (loadItem != null)
					{
						loadItem.currentValue += 1f;
					}
				}
				else
				{
					yield return null;
				}
			}
		}
		foreach (BezierRoadSystem.BezierRoadIntersection item in intersectionsToMerge)
		{
			MergeIntersectionVerts(item);
		}
		intersectionsToMerge.Clear();
		foreach (ChunkRoadMeshes item2 in cMeshesToApply)
		{
			if (VTMapManager.nextLaunchMode != VTMapManager.MapLaunchModes.MapEditor)
			{
				CombineMeshes(item2);
			}
			ApplyMeshes(item2);
			yield return null;
		}
		cMeshesToApply.Clear();
	}

	private void Awake()
	{
		instance = this;
		VTCustomMapManager.OnLoadedMap += VTCustomMapManager_OnLoadedMap;
		BezierRoadProfile[] array = roadProfiles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].EnsureVTMeshes();
		}
	}

	private void Start()
	{
		if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.MapEditor)
		{
			StartCoroutine(EditorReconstructionRoutine());
		}
		else
		{
			StartCoroutine(ScenarioStartRoutine());
		}
	}

	private void ReturnChunkToPool(IntVector2 chunkGrid)
	{
		if (!chunkMeshes.TryGetValue(chunkGrid, out var value))
		{
			return;
		}
		foreach (VTTerrainMesh value2 in value.segmentMeshes.Values)
		{
			value2.Clear();
			vtMeshPool.Push(value2);
		}
		foreach (MeshFilter uMesh in value.uMeshes)
		{
			uMesh.gameObject.SetActive(value: false);
			uMeshPool.Push(uMesh);
		}
		value.segmentMeshes.Clear();
		value.uMeshes.Clear();
	}

	private IEnumerator ScenarioStartRoutine()
	{
		while (roadSystem == null)
		{
			yield return null;
		}
		loadItem = FlightSceneManager.instance.AddLoadItem();
		loadItem.maxValue = roadSystem.roadChunks.Count;
		loadItem.done = false;
		while (VTMapGenerator.fetch.IsGenerating())
		{
			yield return null;
		}
		yield return StartCoroutine(RegenerateAllAsync());
		loadItem.done = true;
	}

	private IEnumerator EditorReconstructionRoutine()
	{
		while (roadSystem == null)
		{
			yield return null;
		}
		while (VTMapGenerator.fetch.IsGenerating())
		{
			yield return null;
		}
		yield return StartCoroutine(RegenerateAllAsync());
		VTMapGenerator.fetch.OnChunkRecalculated += MapGenerator_OnChunkRecalculated;
		while (base.enabled)
		{
			if (roadSystem.dirtyChunks.Count > 0)
			{
				for (int i = 0; i < roadSystem.dirtyChunks.Count; i++)
				{
					ReturnChunkToPool(roadSystem.dirtyChunks[i]);
				}
				for (int j = 0; j < roadSystem.dirtyChunks.Count; j++)
				{
					GenerateChunk(VTMapGenerator.fetch.GetTerrainChunk(roadSystem.dirtyChunks[j]));
				}
				roadSystem.dirtyChunks.Clear();
				foreach (BezierRoadSystem.BezierRoadIntersection item in intersectionsToMerge)
				{
					MergeIntersectionVerts(item);
				}
				intersectionsToMerge.Clear();
				foreach (ChunkRoadMeshes item2 in cMeshesToApply)
				{
					ApplyMeshes(item2);
				}
				cMeshesToApply.Clear();
			}
			yield return null;
		}
	}

	public void PauseRecalcCallback()
	{
		VTMapGenerator.fetch.OnChunkRecalculated -= MapGenerator_OnChunkRecalculated;
	}

	public void UnpauseRecalcCallback()
	{
		VTMapGenerator.fetch.OnChunkRecalculated -= MapGenerator_OnChunkRecalculated;
		VTMapGenerator.fetch.OnChunkRecalculated += MapGenerator_OnChunkRecalculated;
	}

	private void MapGenerator_OnChunkRecalculated(VTMapGenerator.VTTerrainChunk chunk)
	{
		if (!chunkMeshes.ContainsKey(chunk.grid))
		{
			return;
		}
		ReturnChunkToPool(chunk.grid);
		GenerateChunk(chunk);
		foreach (BezierRoadSystem.BezierRoadIntersection item in intersectionsToMerge)
		{
			MergeIntersectionVerts(item);
		}
		intersectionsToMerge.Clear();
		foreach (ChunkRoadMeshes item2 in cMeshesToApply)
		{
			ApplyMeshes(item2);
		}
		cMeshesToApply.Clear();
	}

	private void VTCustomMapManager_OnLoadedMap(VTMapCustom map)
	{
		cMap = map;
	}

	private void GenerateChunk(VTMapGenerator.VTTerrainChunk chunk)
	{
		if (chunk == null)
		{
			return;
		}
		IntVector2 grid = chunk.grid;
		VTTMapTrees.TreeChunkGroup treeGroup = VTTMapTrees.fetch.GetTreeGroup(grid);
		if (treeGroup != null)
		{
			foreach (VTTMapTrees.TreeChunk chunk2 in treeGroup.chunks)
			{
				chunk2.treeRemovers.Clear();
			}
		}
		if (roadSystem.roadChunks.TryGetValue(grid, out var value))
		{
			if (!chunkMeshes.TryGetValue(grid, out var value2))
			{
				value2 = new ChunkRoadMeshes();
				value2.chunkGrid = grid;
				chunkMeshes.Add(grid, value2);
			}
			Transform terrainTf = chunk.lodObjects[0].transform;
			for (int i = 0; i < value.segments.Count; i++)
			{
				VTTerrainMesh vTTerrainMesh = ((vtMeshPool.Count <= 0) ? new VTTerrainMesh() : vtMeshPool.Pop());
				BezierRoadSystem.BezierRoadSegment bezierRoadSegment = value.segments[i];
				if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.MapEditor)
				{
					ResurfaceRoadSegment(bezierRoadSegment);
				}
				GenerateSegmentMesh(vTTerrainMesh, bezierRoadSegment, terrainTf);
				value2.segmentMeshes.Add(bezierRoadSegment.id, vTTerrainMesh);
				if (bezierRoadSegment.startIntersection != null && !intersectionsToMerge.Contains(bezierRoadSegment.startIntersection))
				{
					intersectionsToMerge.Add(bezierRoadSegment.startIntersection);
				}
				if (bezierRoadSegment.endIntersection != null && !intersectionsToMerge.Contains(bezierRoadSegment.endIntersection))
				{
					intersectionsToMerge.Add(bezierRoadSegment.endIntersection);
				}
				if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.MapEditor)
				{
					RemoveTrees(bezierRoadSegment);
				}
				else
				{
					RemoveTrees(bezierRoadSegment, permanent: true);
				}
			}
			cMeshesToApply.Add(value2);
		}
		else
		{
			if (!chunkMeshes.TryGetValue(grid, out var value3))
			{
				return;
			}
			foreach (MeshFilter uMesh in value3.uMeshes)
			{
				UnityEngine.Object.Destroy(uMesh.gameObject);
			}
			value3.uMeshes.Clear();
			value3.segmentMeshes.Clear();
			chunkMeshes.Remove(value3.chunkGrid);
		}
	}

	private void ResurfaceRoadSegment(BezierRoadSystem.BezierRoadSegment seg)
	{
		if (!seg.bridge)
		{
			BezierCurveD curve = seg.curve;
			if (seg.prevSegment == null || !seg.prevSegment.bridge)
			{
				curve.startPt = GlobalSurfacePoint(curve.startPt);
			}
			if (seg.nextSegment == null || !seg.nextSegment.bridge)
			{
				curve.endPt = GlobalSurfacePoint(curve.endPt);
			}
			curve.midPt = GlobalSurfacePoint(curve.midPt);
			seg.curve = curve;
		}
	}

	private Vector3D GlobalSurfacePoint(Vector3D gp)
	{
		if ((bool)VTMapGenerator.fetch)
		{
			Vector3 vector = VTMapManager.GlobalToWorldPoint(gp);
			VTMapGenerator.fetch.GetTerrainChunk(vector);
			if (Physics.Raycast(new Ray(vector + new Vector3(0f, 10000f, 0f), Vector3.down), out var hitInfo, 20000f, 1))
			{
				return VTMapManager.WorldToGlobalPoint(hitInfo.point);
			}
			return gp;
		}
		return gp;
	}

	private void CombineMeshes(ChunkRoadMeshes cMeshes)
	{
		if (cMeshes.segmentMeshes.Count < 2)
		{
			return;
		}
		VTTerrainMesh vTTerrainMesh = new VTTerrainMesh();
		vTTerrainMesh.Clear();
		foreach (VTTerrainMesh value in cMeshes.segmentMeshes.Values)
		{
			int vertCount = vTTerrainMesh.vertCount;
			VTTerrainMesh vTTerrainMesh2 = value;
			for (int i = 0; i < vTTerrainMesh2.vertCount; i++)
			{
				vTTerrainMesh.verts.Add(vTTerrainMesh2.verts[i]);
				vTTerrainMesh.uvs.Add(vTTerrainMesh2.uvs[i]);
				vTTerrainMesh.normals.Add(vTTerrainMesh2.normals[i]);
			}
			for (int j = 0; j < vTTerrainMesh2.triangles.Count; j++)
			{
				vTTerrainMesh.triangles.Add(vTTerrainMesh2.triangles[j] + vertCount);
			}
		}
		cMeshes.segmentMeshes.Clear();
		cMeshes.segmentMeshes.Add(-1, vTTerrainMesh);
	}

	private void GenerateSegmentMesh(VTTerrainMesh vtMesh, BezierRoadSystem.BezierRoadSegment segment, Transform terrainTf)
	{
		BezierCurveD curve = segment.curve;
		BezierRoadProfile bezierRoadProfile = roadProfiles[segment.type];
		bool flag = false;
		bool flag2 = false;
		VTTerrainMesh vTTerrainMesh;
		if (segment.bridge)
		{
			vTTerrainMesh = bezierRoadProfile.GetBridgeMesh(segment.length);
		}
		else
		{
			vTTerrainMesh = bezierRoadProfile.GetSegmentMesh(segment.length);
			if (segment.nextSegment != null && segment.nextSegment.bridge)
			{
				flag = true;
			}
			if (segment.prevSegment != null && segment.prevSegment.bridge)
			{
				flag2 = true;
			}
		}
		bool flag3 = false;
		bool flag4 = false;
		float num = 0f;
		float num2 = 1f;
		float minT = 0f;
		float maxT = 1f;
		if (segment.endIntersection != null)
		{
			num2 = 1f - GetIntersectionTSpan(segment, segment.endIntersection);
			maxT = num2;
		}
		else if (segment.nextSegment == null || flag)
		{
			num2 = 0.9f;
		}
		else if (segment.nextSegment != null && segment.nextSegment.type < segment.type)
		{
			num2 = 0.8f;
			maxT = num2;
			flag3 = true;
		}
		if (segment.startIntersection != null)
		{
			num = GetIntersectionTSpan(segment, segment.startIntersection);
			minT = num;
		}
		else if (segment.prevSegment == null || flag2)
		{
			num = 0.1f;
		}
		else if (segment.prevSegment != null && segment.prevSegment.type < segment.type)
		{
			num = 0.2f;
			minT = num;
			flag4 = true;
		}
		Vector3 vector = curve.GetTangent(0f);
		if (segment.prevSegment != null)
		{
			Vector3 b = ((segment.prevSegment.nextSegment != segment) ? (-segment.prevSegment.curve.GetTangent(0f)) : segment.prevSegment.curve.GetTangent(1f));
			vector = Vector3.Slerp(vector, b, 0.5f);
		}
		Vector3 vector2 = curve.GetTangent(1f);
		if (segment.nextSegment != null)
		{
			Vector3 b2 = ((segment.nextSegment.prevSegment != segment) ? (-segment.nextSegment.curve.GetTangent(1f)) : segment.nextSegment.curve.GetTangent(0f));
			vector2 = Vector3.Slerp(vector2, b2, 0.5f);
		}
		float value = 2f * bezierRoadProfile.radius;
		value = Mathf.Clamp(value, 0f, segment.length * 0.25f);
		value = Mathf.Lerp(value, segment.length * 0.25f, Mathf.Abs(Mathf.Cos(Vector3.Angle(vector2, vector) * ((float)Math.PI / 180f))));
		Vector3D startTanPt = curve.startPt + vector * value;
		Vector3D endTanPt = curve.endPt - vector2 * value;
		BezierCurveD5 bezierCurveD = new BezierCurveD5(curve.startPt, startTanPt, curve.midPt, endTanPt, curve.endPt);
		for (int i = 0; i < vTTerrainMesh.vertCount; i++)
		{
			Vector3 vert = vTTerrainMesh.verts[i];
			Vector3 norm = vTTerrainMesh.normals[i];
			RoadVertex roadVertex = ApplyRoadVertTransformation(vert, norm, vTTerrainMesh.uvs[i], segment.length, reverseT: false, bezierCurveD, terrainTf, bezierRoadProfile.radius, num, num2);
			vtMesh.verts.Add(roadVertex.vertex);
			vtMesh.normals.Add(roadVertex.normal);
			vtMesh.uvs.Add(roadVertex.uv);
		}
		for (int j = 0; j < vTTerrainMesh.triangles.Count; j++)
		{
			vtMesh.triangles.Add(vTTerrainMesh.triangles[j]);
		}
		float dist = 0.1f * segment.length;
		VTTerrainMesh masterMesh = (segment.bridge ? bezierRoadProfile.bridgeEndVTMesh : bezierRoadProfile.segmentEndVTMesh);
		if (segment.nextSegment == null && segment.endIntersection == null)
		{
			AppendEndPiece(vtMesh, bezierRoadProfile, terrainTf, bezierCurveD, dist, 0.9f, 1f, reverseT: false, masterMesh);
		}
		if (segment.prevSegment == null && segment.startIntersection == null)
		{
			AppendEndPiece(vtMesh, bezierRoadProfile, terrainTf, bezierCurveD, dist, 0f, 0.1f, reverseT: true, masterMesh);
		}
		if (flag)
		{
			AppendEndPiece(vtMesh, bezierRoadProfile, terrainTf, bezierCurveD, dist, 0.9f, 1f, reverseT: false, bezierRoadProfile.bridgeAdapterVTMesh);
		}
		if (flag2)
		{
			AppendEndPiece(vtMesh, bezierRoadProfile, terrainTf, bezierCurveD, dist, 0f, 0.1f, reverseT: true, bezierRoadProfile.bridgeAdapterVTMesh);
		}
		if (flag3)
		{
			bool reverse;
			VTTerrainMesh adapter = adapters.GetAdapter(segment.type, segment.nextSegment.type, segment.bridge, out reverse);
			if (adapter != null)
			{
				AppendEndPiece(vtMesh, bezierRoadProfile, terrainTf, bezierCurveD, segment.length * 0.2f, 0.8f, 1f, reverse, adapter);
			}
		}
		if (flag4)
		{
			bool reverse2;
			VTTerrainMesh adapter2 = adapters.GetAdapter(segment.prevSegment.type, segment.type, segment.bridge, out reverse2);
			if (adapter2 != null)
			{
				AppendEndPiece(vtMesh, bezierRoadProfile, terrainTf, bezierCurveD, segment.length * 0.2f, 0f, 0.2f, reverse2, adapter2);
			}
		}
		ApplyBridgeSupports(segment, vtMesh, bezierRoadProfile, bezierCurveD, terrainTf, minT, maxT);
		if (segment.startIntersection != null)
		{
			AppendIntersection(segment, vtMesh, bezierRoadProfile, terrainTf, segment.startIntersection, bezierCurveD, end: false, num);
		}
		if (segment.endIntersection != null)
		{
			AppendIntersection(segment, vtMesh, bezierRoadProfile, terrainTf, segment.endIntersection, bezierCurveD, end: true, 1f - num2);
		}
	}

	private void ApplyBridgeSupports(BezierRoadSystem.BezierRoadSegment segment, VTTerrainMesh masterMesh, BezierRoadProfile roadProfile, BezierCurveD5 curve, Transform terrainTf, float minT, float maxT)
	{
		if (!segment.bridge)
		{
			return;
		}
		float bridgeHeight;
		float bridgeLength = GetBridgeLength(segment, out bridgeHeight);
		float segmentLength = segment.length * (maxT - minT);
		VTTerrainMesh bridgeSupportMesh = roadProfile.GetBridgeSupportMesh(bridgeLength, segmentLength, bridgeHeight);
		if (bridgeSupportMesh != null)
		{
			int vertCount = masterMesh.vertCount;
			for (int i = 0; i < bridgeSupportMesh.vertCount; i++)
			{
				Vector3 vert = bridgeSupportMesh.verts[i];
				Vector3 norm = bridgeSupportMesh.normals[i];
				Vector2 uv = bridgeSupportMesh.uvs[i];
				RoadVertex roadVertex = ApplyRoadVertTransformation(vert, norm, uv, segment.length, reverseT: false, curve, terrainTf, roadProfile.radius, minT, maxT, transformUVs: false);
				masterMesh.verts.Add(roadVertex.vertex);
				masterMesh.normals.Add(roadVertex.normal);
				masterMesh.uvs.Add(roadVertex.uv);
			}
			for (int j = 0; j < bridgeSupportMesh.triangles.Count; j++)
			{
				masterMesh.triangles.Add(bridgeSupportMesh.triangles[j] + vertCount);
			}
		}
	}

	private float GetBridgeLength(BezierRoadSystem.BezierRoadSegment segment, out float bridgeHeight)
	{
		BezierRoadSystem.BezierRoadSegment bezierRoadSegment = null;
		if ((segment.nextSegment == null || !segment.nextSegment.bridge) && (segment.prevSegment == null || !segment.prevSegment.bridge))
		{
			bezierRoadSegment = segment;
		}
		BezierRoadSystem.BezierRoadSegment bezierRoadSegment2 = segment;
		bool flag = true;
		while (bezierRoadSegment == null)
		{
			if (flag)
			{
				if (bezierRoadSegment2.nextSegment == null || !bezierRoadSegment2.nextSegment.bridge || bezierRoadSegment2.nextSegment == segment)
				{
					bezierRoadSegment = bezierRoadSegment2;
					continue;
				}
				flag = bezierRoadSegment2.nextSegment.nextSegment != bezierRoadSegment2;
				bezierRoadSegment2 = bezierRoadSegment2.nextSegment;
			}
			else if (bezierRoadSegment2.prevSegment == null || !bezierRoadSegment2.prevSegment.bridge || bezierRoadSegment2.prevSegment == segment)
			{
				bezierRoadSegment = bezierRoadSegment2;
			}
			else
			{
				flag = bezierRoadSegment2.prevSegment.nextSegment != bezierRoadSegment2;
				bezierRoadSegment2 = bezierRoadSegment2.prevSegment;
			}
		}
		bool flag2 = false;
		bezierRoadSegment2 = bezierRoadSegment;
		flag = !flag;
		float num = 0f;
		float num2 = 0f;
		while (!flag2)
		{
			num += bezierRoadSegment2.length;
			Vector3 worldPoint = bezierRoadSegment2.GetWorldPoint(0.5f);
			num2 = Mathf.Max(WaterPhysics.GetAltitude(worldPoint) - VTMapGenerator.fetch.GetHeightmapAltitude(worldPoint), num2);
			if (flag)
			{
				if (bezierRoadSegment2.nextSegment == null || !bezierRoadSegment2.nextSegment.bridge || bezierRoadSegment2.nextSegment == bezierRoadSegment)
				{
					flag2 = true;
					continue;
				}
				flag = bezierRoadSegment2.nextSegment.nextSegment != bezierRoadSegment2;
				bezierRoadSegment2 = bezierRoadSegment2.nextSegment;
			}
			else if (bezierRoadSegment2.prevSegment == null || !bezierRoadSegment2.prevSegment.bridge || bezierRoadSegment2.prevSegment == bezierRoadSegment)
			{
				flag2 = true;
			}
			else
			{
				flag = bezierRoadSegment2.prevSegment.nextSegment != bezierRoadSegment2;
				bezierRoadSegment2 = bezierRoadSegment2.prevSegment;
			}
		}
		bridgeHeight = num2;
		return num;
	}

	private float GetBridgeLength(float currLength, BezierRoadSystem.BezierRoadSegment currentSeg, BezierRoadSystem.BezierRoadSegment prevSeg, BezierRoadSystem.BezierRoadSegment startSeg, ref float bridgeHeight)
	{
		if (currentSeg.bridge)
		{
			currLength = currentSeg.length;
			if (currentSeg.nextSegment != null && currentSeg.nextSegment != prevSeg && currentSeg.nextSegment != startSeg)
			{
				currLength += GetBridgeLength(currLength, currentSeg.nextSegment, currentSeg, startSeg, ref bridgeHeight);
			}
			else if (currentSeg.prevSegment != null && currentSeg.prevSegment != prevSeg && currentSeg.prevSegment != startSeg)
			{
				currLength += GetBridgeLength(currLength, currentSeg.prevSegment, currentSeg, startSeg, ref bridgeHeight);
			}
			Vector3 worldPoint = currentSeg.GetWorldPoint(0.5f);
			float b = WaterPhysics.GetAltitude(worldPoint) - VTMapGenerator.fetch.GetHeightmapAltitude(worldPoint);
			bridgeHeight = Mathf.Max(bridgeHeight, b);
		}
		return currLength;
	}

	private void AppendEndPiece(VTTerrainMesh vtMesh, BezierRoadProfile roadProfile, Transform terrainTf, ICurve curve, float dist, float minT, float maxT, bool reverseT, VTTerrainMesh masterMesh)
	{
		int vertCount = vtMesh.vertCount;
		for (int i = 0; i < masterMesh.vertCount; i++)
		{
			Vector3 vert = masterMesh.verts[i];
			Vector3 norm = masterMesh.normals[i];
			Vector3 vector = masterMesh.uvs[i];
			RoadVertex roadVertex = ApplyRoadVertTransformation(vert, norm, vector, dist, reverseT, curve, terrainTf, roadProfile.radius, minT, maxT, transformUVs: false);
			vtMesh.verts.Add(roadVertex.vertex);
			vtMesh.normals.Add(roadVertex.normal);
			vtMesh.uvs.Add(roadVertex.uv);
		}
		for (int j = 0; j < masterMesh.triangles.Count; j++)
		{
			vtMesh.triangles.Add(masterMesh.triangles[j] + vertCount);
		}
	}

	private RoadVertex ApplyRoadVertTransformation(Vector3 vert, Vector3 norm, Vector2 uv, float dist, bool reverseT, ICurve bezierCurve, Transform terrainTf, float radius, float minT, float maxT, bool transformUVs = true)
	{
		if (reverseT)
		{
			Quaternion quaternion = Quaternion.Euler(0f, 180f, 0f);
			vert = quaternion * vert;
			vert.z += 1f;
			norm = quaternion * norm;
		}
		float t = Mathf.Lerp(minT, maxT, vert.z);
		Vector3 vector = VTMapManager.GlobalToWorldPoint(bezierCurve.GetPoint(t));
		Vector3 vector2 = terrainTf.InverseTransformPoint(vector);
		Vector3 vector3 = terrainTf.InverseTransformDirection(bezierCurve.GetTangent(t));
		Vector3 vector4 = Vector3.Cross(Vector3.forward, vector3);
		Vector3 position = vector2 + vert.x * vector4 + vert.y * Vector3.forward;
		Vector3 vector5 = terrainTf.TransformPoint(position);
		Vector3 vector6 = terrainTf.TransformDirection(vector4);
		bool flag = false;
		if (vert.y > 0f)
		{
			float y = vector.y;
			for (int i = -1; i <= 1; i++)
			{
				Vector3 vector7 = SurfaceWorldPoint(vector + i * vector6 * radius);
				if (vector7.y > y)
				{
					y = vector7.y;
					flag = true;
				}
			}
			if (flag)
			{
				vector5.y = y + vert.y;
			}
		}
		if (!flag || vert.y < -40f)
		{
			Vector3 vector8 = SurfaceWorldPoint(vector5);
			if (vector8.y > vector.y || vert.y < -40f)
			{
				vector5 = vector8 + (vert.y + (float)((vert.y < -40f) ? 40 : 0)) * Vector3.up;
			}
		}
		position = terrainTf.InverseTransformPoint(vector5);
		Vector3 normal = norm;
		if (vector3 != Vector3.zero)
		{
			normal = Quaternion.LookRotation(vector3, Vector3.Cross(vector3, vector4)) * norm;
		}
		Vector2 uv2 = uv;
		if (transformUVs)
		{
			uv2.x *= dist;
		}
		RoadVertex result = default(RoadVertex);
		result.vertex = position;
		result.normal = normal;
		result.uv = uv2;
		return result;
	}

	private float GetIntersectionTSpan(BezierRoadSystem.BezierRoadSegment seg, BezierRoadSystem.BezierRoadIntersection intersection)
	{
		float num = GetIntersectionRadius(intersection) / seg.length;
		float radius = roadProfiles[seg.type].radius;
		int count = intersection.attachedSegments.Count;
		int num2 = intersection.attachedSegments.IndexOf(seg);
		int num3 = (num2 + 1) % count;
		int num4 = (num2 + count - 1) % count;
		Vector3 segmentRadialDir = intersection.GetSegmentRadialDir(num2);
		Vector3 segmentRadialDir2 = intersection.GetSegmentRadialDir(num3);
		Vector3 segmentRadialDir3 = intersection.GetSegmentRadialDir(num4);
		segmentRadialDir.y = (segmentRadialDir2.y = (segmentRadialDir3.y = 0f));
		segmentRadialDir2 = Quaternion.AngleAxis(Vector3.Angle(segmentRadialDir, segmentRadialDir2), Vector3.up) * Vector3.back;
		segmentRadialDir3 = Quaternion.AngleAxis(Vector3.Angle(segmentRadialDir, segmentRadialDir3), Vector3.down) * Vector3.back;
		Vector3 vector = -Vector3.Cross(Vector3.up, segmentRadialDir2);
		Vector3 vector2 = Vector3.Cross(Vector3.up, segmentRadialDir3);
		float radius2 = roadProfiles[intersection.attachedSegments[num3].type].radius;
		float radius3 = roadProfiles[intersection.attachedSegments[num4].type].radius;
		float num5 = 500f;
		Plane plane = new Plane(vector, new Vector3(0f, 0f, num5) + vector * radius2);
		Plane plane2 = new Plane(vector2, new Vector3(0f, 0f, num5) + vector2 * radius3);
		Ray ray = new Ray(new Vector3(0f - radius, 0f, 0f), Vector3.forward);
		Ray ray2 = new Ray(new Vector3(radius, 0f, 0f), Vector3.forward);
		if (!plane.Raycast(ray, out var enter))
		{
			enter = num5;
		}
		if (!plane2.Raycast(ray2, out var enter2))
		{
			enter2 = num5;
		}
		return (num5 - Mathf.Min(enter, enter2)) / num5 + num;
	}

	private void AppendIntersection(BezierRoadSystem.BezierRoadSegment seg, VTTerrainMesh vtMesh, BezierRoadProfile roadProfile, Transform terrainTf, BezierRoadSystem.BezierRoadIntersection intersection, ICurve curve, bool end, float intersectionTSpan)
	{
		int vertCount = vtMesh.vertCount;
		VTTerrainMesh vTTerrainMesh = (seg.bridge ? roadProfile.bridgeIntersectionVTMesh : roadProfile.intersectionVTMesh);
		float num = intersectionTSpan * seg.length;
		bool reverseT;
		float minT;
		float maxT;
		if (end)
		{
			reverseT = false;
			minT = 1f - intersectionTSpan;
			maxT = 1f;
		}
		else
		{
			reverseT = true;
			minT = 0f;
			maxT = intersectionTSpan;
		}
		if (!seg.bridge)
		{
			_ = roadProfile.leftIntersectionVerts;
		}
		else
		{
			_ = roadProfile.leftBridgeIntersectionVerts;
		}
		if (!seg.bridge)
		{
			_ = roadProfile.rightIntersectionVerts;
		}
		else
		{
			_ = roadProfile.rightBridgeIntersectionVerts;
		}
		float num2 = 1f;
		float num3 = 1f;
		float radius = roadProfiles[seg.type].radius;
		int count = intersection.attachedSegments.Count;
		int num4 = intersection.attachedSegments.IndexOf(seg);
		int num5 = (num4 + 1) % count;
		int num6 = (num4 + count - 1) % count;
		Vector3 segmentRadialDir = intersection.GetSegmentRadialDir(num4);
		Vector3 segmentRadialDir2 = intersection.GetSegmentRadialDir(num5);
		Vector3 segmentRadialDir3 = intersection.GetSegmentRadialDir(num6);
		segmentRadialDir.y = (segmentRadialDir2.y = (segmentRadialDir3.y = 0f));
		segmentRadialDir2 = Quaternion.AngleAxis(Vector3.Angle(segmentRadialDir, segmentRadialDir2), Vector3.up) * Vector3.back;
		segmentRadialDir3 = Quaternion.AngleAxis(Vector3.Angle(segmentRadialDir, segmentRadialDir3), Vector3.down) * Vector3.back;
		Vector3 vector = -Vector3.Cross(Vector3.up, segmentRadialDir2);
		Vector3 vector2 = Vector3.Cross(Vector3.up, segmentRadialDir3);
		float radius2 = roadProfiles[intersection.attachedSegments[num5].type].radius;
		float radius3 = roadProfiles[intersection.attachedSegments[num6].type].radius;
		float num7 = num;
		Plane plane = new Plane(vector, new Vector3(0f, 0f, num7) + vector * radius2);
		Plane plane2 = new Plane(vector2, new Vector3(0f, 0f, num7) + vector2 * radius3);
		Ray ray = new Ray(new Vector3(0f - radius, 0f, 0f), Vector3.forward);
		Ray ray2 = new Ray(new Vector3(radius, 0f, 0f), Vector3.forward);
		if (plane.Raycast(ray, out var enter))
		{
			num2 = enter / num7;
		}
		if (plane2.Raycast(ray2, out var enter2))
		{
			num3 = enter2 / num7;
		}
		for (int i = 0; i < vTTerrainMesh.verts.Count; i++)
		{
			Vector3 vert = vTTerrainMesh.verts[i];
			Vector3 norm = vTTerrainMesh.normals[i];
			Vector2 uv = vTTerrainMesh.uvs[i];
			if (vert.x > 0f)
			{
				vert.z *= num3;
			}
			else
			{
				vert.z *= num2;
			}
			RoadVertex roadVertex = ApplyRoadVertTransformation(vert, norm, uv, num * 2f, reverseT, curve, terrainTf, roadProfile.radius, minT, maxT, transformUVs: false);
			vtMesh.verts.Add(roadVertex.vertex);
			vtMesh.normals.Add(roadVertex.normal);
			vtMesh.uvs.Add(roadVertex.uv);
		}
		for (int j = 0; j < vTTerrainMesh.triangles.Count; j++)
		{
			vtMesh.triangles.Add(vTTerrainMesh.triangles[j] + vertCount);
		}
	}

	private float GetIntersectionRadius(BezierRoadSystem.BezierRoadIntersection intersection)
	{
		float num = 0f;
		foreach (BezierRoadSystem.BezierRoadSegment attachedSegment in intersection.attachedSegments)
		{
			float radius = roadProfiles[attachedSegment.type].radius;
			num = Mathf.Max(num, radius);
		}
		return num * 3f;
	}

	private void ApplyMeshes(ChunkRoadMeshes cMeshes)
	{
		int num = 0;
		foreach (KeyValuePair<int, VTTerrainMesh> segmentMesh in cMeshes.segmentMeshes)
		{
			string text = "Segment " + segmentMesh.Key;
			Mesh mesh;
			MeshRenderer meshRenderer;
			GameObject gameObject;
			if (uMeshPool.Count > 0)
			{
				MeshFilter meshFilter = uMeshPool.Pop();
				mesh = meshFilter.sharedMesh;
				meshRenderer = meshFilter.GetComponent<MeshRenderer>();
				gameObject = meshRenderer.gameObject;
				gameObject.name = text;
				cMeshes.uMeshes.Add(meshFilter);
				gameObject.SetActive(value: true);
			}
			else
			{
				mesh = new Mesh();
				createdMeshesList.Add(mesh);
				createdMeshes++;
				gameObject = new GameObject(text);
				MeshFilter meshFilter2 = gameObject.AddComponent<MeshFilter>();
				meshFilter2.sharedMesh = mesh;
				meshRenderer = gameObject.AddComponent<MeshRenderer>();
				cMeshes.uMeshes.Add(meshFilter2);
			}
			gameObject.transform.parent = VTMapGenerator.fetch.GetTerrainChunk(cMeshes.chunkGrid).lodObjects[0].transform;
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.localRotation = Quaternion.identity;
			meshRenderer.material = roadProfiles[0].roadMaterial;
			mesh.name = text;
			segmentMesh.Value.ApplyToMesh(mesh);
			if (VTMapManager.nextLaunchMode != VTMapManager.MapLaunchModes.MapEditor)
			{
				MeshCollider meshCollider = new GameObject("RoadCollider").AddComponent<MeshCollider>();
				meshCollider.transform.parent = gameObject.transform.parent.parent;
				meshCollider.transform.position = gameObject.transform.position;
				meshCollider.transform.rotation = gameObject.transform.rotation;
				meshCollider.sharedMesh = mesh;
			}
			num++;
		}
	}

	private void MergeIntersectionVerts(BezierRoadSystem.BezierRoadIntersection intersection)
	{
		List<VTTerrainMesh> list = new List<VTTerrainMesh>();
		int count = intersection.attachedSegments.Count;
		for (int i = 0; i < count; i++)
		{
			BezierRoadSystem.BezierRoadSegment bezierRoadSegment = intersection.attachedSegments[i];
			VTTerrainMesh value = null;
			if (chunkMeshes.TryGetValue(bezierRoadSegment.chunk.gridPos, out var value2))
			{
				value2.segmentMeshes.TryGetValue(bezierRoadSegment.id, out value);
			}
			list.Add(value);
		}
		bool bridge = intersection.attachedSegments[0].bridge;
		Vector3 vector = VTMapManager.GlobalToWorldPoint(intersection.GetIntersectionPoint());
		if (!bridge)
		{
			vector = SurfaceWorldPoint(vector);
		}
		float num = 0f;
		if (!bridge)
		{
			num = GetIntersectionVertUpshift(intersection);
		}
		for (int j = 0; j < count; j++)
		{
			int index = j;
			int index2 = (j + 1) % count;
			BezierRoadSystem.BezierRoadSegment bezierRoadSegment2 = intersection.attachedSegments[index];
			BezierRoadSystem.BezierRoadSegment bezierRoadSegment3 = intersection.attachedSegments[index2];
			VTTerrainMesh vTTerrainMesh = list[index];
			VTTerrainMesh vTTerrainMesh2 = list[index2];
			if (vTTerrainMesh == null || vTTerrainMesh2 == null)
			{
				continue;
			}
			BezierRoadProfile bezierRoadProfile = roadProfiles[bezierRoadSegment2.type];
			BezierRoadProfile bezierRoadProfile2 = roadProfiles[bezierRoadSegment3.type];
			VTTerrainMesh vTTerrainMesh3 = (bezierRoadSegment2.bridge ? bezierRoadProfile.bridgeIntersectionVTMesh : bezierRoadProfile.intersectionVTMesh);
			int num2 = vTTerrainMesh.vertCount - vTTerrainMesh3.vertCount;
			if (bezierRoadSegment2.endIntersection != null && bezierRoadSegment2.startIntersection != null && bezierRoadSegment2.startIntersection == intersection)
			{
				num2 -= vTTerrainMesh3.vertCount;
			}
			VTTerrainMesh vTTerrainMesh4 = (bezierRoadSegment3.bridge ? bezierRoadProfile2.bridgeIntersectionVTMesh : bezierRoadProfile2.intersectionVTMesh);
			int num3 = vTTerrainMesh2.vertCount - vTTerrainMesh4.vertCount;
			if (bezierRoadSegment3.endIntersection != null && bezierRoadSegment3.startIntersection != null && bezierRoadSegment3.startIntersection == intersection)
			{
				num3 -= vTTerrainMesh4.vertCount;
			}
			IntersectionMeshInfo intersectionMeshInfo = default(IntersectionMeshInfo);
			intersectionMeshInfo.vtMesh = vTTerrainMesh;
			intersectionMeshInfo.masterMesh = vTTerrainMesh3;
			intersectionMeshInfo.vtIndexOffset = num2;
			intersectionMeshInfo.vertIndices = (bezierRoadSegment2.bridge ? bezierRoadProfile.leftBridgeIntersectionVerts : bezierRoadProfile.leftIntersectionVerts);
			intersectionMeshInfo.transform = VTMapGenerator.fetch.GetTerrainChunk(bezierRoadSegment2.chunk.gridPos).lodObjects[0].transform;
			IntersectionMeshInfo leftInfo = intersectionMeshInfo;
			intersectionMeshInfo = default(IntersectionMeshInfo);
			intersectionMeshInfo.vtMesh = vTTerrainMesh2;
			intersectionMeshInfo.masterMesh = vTTerrainMesh4;
			intersectionMeshInfo.vtIndexOffset = num3;
			intersectionMeshInfo.vertIndices = (bezierRoadSegment3.bridge ? bezierRoadProfile2.rightBridgeIntersectionVerts : bezierRoadProfile2.rightIntersectionVerts);
			intersectionMeshInfo.transform = VTMapGenerator.fetch.GetTerrainChunk(bezierRoadSegment3.chunk.gridPos).lodObjects[0].transform;
			IntersectionMeshInfo rightInfo = intersectionMeshInfo;
			MergeIntersectionVerts(leftInfo, rightInfo);
			foreach (int item in bezierRoadSegment2.bridge ? bezierRoadProfile.bridgeIntersectionVertIdx : bezierRoadProfile.intersectionVertIdx)
			{
				Vector3 vector2 = (vTTerrainMesh3.verts[item].y + num) * Vector3.up;
				leftInfo.vtMesh.verts[item + leftInfo.vtIndexOffset] = leftInfo.transform.InverseTransformPoint(vector + vector2);
			}
			foreach (int item2 in bezierRoadSegment3.bridge ? bezierRoadProfile2.bridgeIntersectionVertIdx : bezierRoadProfile2.intersectionVertIdx)
			{
				Vector3 vector3 = (vTTerrainMesh4.verts[item2].y + num) * Vector3.up;
				rightInfo.vtMesh.verts[item2 + rightInfo.vtIndexOffset] = rightInfo.transform.InverseTransformPoint(vector + vector3);
			}
		}
	}

	private float GetIntersectionVertUpshift(BezierRoadSystem.BezierRoadIntersection intersection)
	{
		Vector3 vector = SurfaceWorldPoint(VTMapManager.GlobalToWorldPoint(intersection.GetIntersectionPoint()));
		float num = 0f;
		for (int i = 0; i < intersection.attachedSegments.Count; i++)
		{
			float radius = roadProfiles[intersection.attachedSegments[i].type].radius;
			Vector3 segmentRadialDir = intersection.GetSegmentRadialDir(i);
			Vector3 vector2 = Vector3.Cross(Vector3.up, segmentRadialDir);
			for (int j = -1; j <= 1; j += 2)
			{
				num = Mathf.Max(SurfaceWorldPoint(vector + j * vector2 * radius).y - vector.y, num);
			}
		}
		return num;
	}

	private void MergeIntersectionVerts(IntersectionMeshInfo leftInfo, IntersectionMeshInfo rightInfo)
	{
		if (leftInfo.vertIndices.Length != rightInfo.vertIndices.Length)
		{
			Debug.LogError("Mismatched vertex indices on road intersection mesh.");
			return;
		}
		Quaternion.Euler(0f, 180f, 0f);
		for (int i = 0; i < leftInfo.vertIndices.Length; i++)
		{
			int num = leftInfo.vertIndices[i];
			_ = leftInfo.masterMesh.verts[num];
			_ = leftInfo.masterMesh.normals[num];
			int num2 = rightInfo.vertIndices[i];
			int index = num + leftInfo.vtIndexOffset;
			int index2 = num2 + rightInfo.vtIndexOffset;
			Vector3 a = leftInfo.transform.TransformPoint(leftInfo.vtMesh.verts[index]);
			Vector3 b = rightInfo.transform.TransformPoint(rightInfo.vtMesh.verts[index2]);
			Vector3 position = Vector3.Lerp(a, b, 0.5f);
			leftInfo.vtMesh.verts[index] = leftInfo.transform.InverseTransformPoint(position);
			rightInfo.vtMesh.verts[index2] = rightInfo.transform.InverseTransformPoint(position);
		}
	}

	private void RemoveTrees(BezierRoadSystem.BezierRoadSegment segment, bool permanent = false)
	{
		float num = roadProfiles[segment.type].radius * 2f;
		int num2 = Mathf.CeilToInt(segment.length / num);
		List<IntVector2> list = new List<IntVector2>();
		for (int i = 0; i < num2; i++)
		{
			float t = (float)i / (float)(num2 - 1);
			Vector3 worldPoint = segment.GetWorldPoint(t);
			VTTMapTrees.fetch.GetChunks(removalTreeChunks, worldPoint, num);
			foreach (VTTMapTrees.TreeChunk removalTreeChunk in removalTreeChunks)
			{
				Vector3 vector = removalTreeChunk.treeObj.transform.InverseTransformPoint(worldPoint);
				if (permanent)
				{
					float num3 = num * num;
					for (int num4 = removalTreeChunk.treePoints.Count - 1; num4 >= 0; num4--)
					{
						if ((vector - removalTreeChunk.treePoints[num4]).sqrMagnitude < num3)
						{
							removalTreeChunk.treePoints.RemoveAt(num4);
							removalTreeChunk.treeScales.RemoveAt(num4);
							removalTreeChunk.colliderSizes.RemoveAt(num4);
						}
					}
				}
				else
				{
					VTTMapTrees.TreeRemover item = new VTTMapTrees.TreeRemover(vector, num);
					removalTreeChunk.treeRemovers.Add(item);
				}
				IntVector2 item2 = VTMapGenerator.fetch.ChunkGridAtPos(worldPoint);
				if (!list.Contains(item2))
				{
					list.Add(item2);
				}
			}
		}
		foreach (IntVector2 item3 in list)
		{
			VTTMapTrees.fetch.DespawnMeshPoolChunk(item3);
		}
	}

	private Vector3 SurfaceWorldPoint(Vector3 point)
	{
		VTMapGenerator.VTTerrainChunk terrainChunk = VTMapGenerator.fetch.GetTerrainChunk(point);
		if (terrainChunk != null && terrainChunk.colliderBaked)
		{
			if (terrainChunk.collider.Raycast(new Ray(point + new Vector3(0f, 2000f, 0f), Vector3.down), out var hitInfo, 4000f))
			{
				return hitInfo.point;
			}
			return point;
		}
		return VTMapGenerator.fetch.SurfacePoint(point);
	}

	private Vector3D SurfaceGlobalPoint(Vector3 point)
	{
		return VTMapManager.WorldToGlobalPoint(SurfaceWorldPoint(point));
	}
}
