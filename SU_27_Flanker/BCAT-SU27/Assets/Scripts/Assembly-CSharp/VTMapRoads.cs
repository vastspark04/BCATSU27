using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VTMapRoads : MonoBehaviour
{
	public class RoadSegmentModel
	{
		public VTRoadSystem.RoadSegment segment;

		public GameObject gameObject;

		public VTTerrainMesh masterMesh;

		public MeshRenderer meshRenderer;

		public MeshFilter meshFilter;

		public Transform transform => gameObject.transform;

		public RoadSegmentModel(VTRoadSystem.RoadSegment segment, Material[] materials)
		{
			this.segment = segment;
			gameObject = new GameObject($"Road {segment.id}");
			masterMesh = new VTTerrainMesh();
			masterMesh.subMeshCount = materials.Length;
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterials = materials;
			meshFilter = gameObject.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = new Mesh();
			VTMapGenerator.VTTerrainChunk terrainChunk = VTMapGenerator.fetch.GetTerrainChunk(VTMapGenerator.fetch.ChunkGridAtPos(segment.startVertex.worldPos));
			gameObject.transform.parent = terrainChunk.gridTransform;
			gameObject.transform.position = segment.startVertex.worldPos;
		}
	}

	public List<RoadSet> roadSets;

	private Dictionary<string, RoadSet> setDictionary = new Dictionary<string, RoadSet>();

	private Dictionary<int, RoadSegmentModel> segmentModels = new Dictionary<int, RoadSegmentModel>();

	public Material[] materials;

	public static VTMapRoads instance { get; private set; }

	public VTRoadSystem roadSystem { get; private set; }

	public RoadSet GetRoadSet(string id)
	{
		if (setDictionary.ContainsKey(id))
		{
			return setDictionary[id];
		}
		return null;
	}

	private void Awake()
	{
		instance = this;
		foreach (RoadSet roadSet in roadSets)
		{
			setDictionary.Add(roadSet.name, roadSet);
		}
		VTCustomMapManager.OnLoadedMap += VTCustomMapManager_OnLoadedMap;
	}

	private void OnDestroy()
	{
		VTCustomMapManager.OnLoadedMap -= VTCustomMapManager_OnLoadedMap;
	}

	private void VTCustomMapManager_OnLoadedMap(VTMapCustom map)
	{
		StartCoroutine(InitialGenerationRoutine(map));
	}

	private IEnumerator InitialGenerationRoutine(VTMapCustom map)
	{
		if (map.roadSystem != null)
		{
			roadSystem.OnRebuildClusterModel += GenerateRoadClusters;
			roadSystem.OnDeletedSegment += OnDeletedSegment;
		}
		while (!VTMapGenerator.fetch.HasFinishedInitialGeneration())
		{
			yield return null;
		}
		Debug.Log("VTMapRoads: requesting road model rebuild");
		roadSystem.RebuildAllModels();
	}

	private void OnDeletedSegment(VTRoadSystem.RoadSegment seg)
	{
		int id = seg.id;
		Object.Destroy(segmentModels[id].gameObject);
		segmentModels.Remove(id);
	}

	public void RecalculateAllHeights()
	{
		roadSystem.RecalculateAllHeights();
	}

	public void GenerateRoadClusters(VTRoadSystem.RoadCluster[] clusters)
	{
		List<int> list = new List<int>();
		List<VTRoadSystem.RoadSegment> list2 = new List<VTRoadSystem.RoadSegment>();
		List<VTRoadSystem.RoadIntersection> list3 = new List<VTRoadSystem.RoadIntersection>();
		foreach (VTRoadSystem.RoadCluster roadCluster in clusters)
		{
			foreach (VTRoadSystem.RoadSegment segment in roadCluster.segments)
			{
				list2.Add(segment);
				if (segment.endIntersection != null && !list3.Contains(segment.endIntersection))
				{
					list3.Add(segment.endIntersection);
				}
				if (segment.startIntersection != null && !list3.Contains(segment.startIntersection))
				{
					list3.Add(segment.startIntersection);
				}
			}
			foreach (VTRoadSystem.RoadIntersection intersection in roadCluster.intersections)
			{
				if (list3.Contains(intersection))
				{
					continue;
				}
				list3.Add(intersection);
				foreach (VTRoadSystem.RoadSegment attachedRoad in intersection.attachedRoads)
				{
					if (!list2.Contains(attachedRoad))
					{
						list2.Add(attachedRoad);
					}
				}
			}
		}
		foreach (VTRoadSystem.RoadSegment item in list2)
		{
			RoadSegmentModel roadSegmentModel;
			if (segmentModels.ContainsKey(item.id))
			{
				roadSegmentModel = segmentModels[item.id];
			}
			else
			{
				roadSegmentModel = new RoadSegmentModel(item, materials);
				segmentModels.Add(item.id, roadSegmentModel);
			}
			BuildSegmentModel(roadSegmentModel);
			list.Add(item.id);
			Debug.Log("Building segment " + item.id + " bridge: " + item.bridge);
			RemoveTrees(item);
		}
		WeldInterSegments(list2);
		foreach (VTRoadSystem.RoadIntersection item2 in list3)
		{
			BuildIntersectionModel(item2, setDictionary[item2.roadSet]);
			foreach (VTRoadSystem.RoadSegment attachedRoad2 in item2.attachedRoads)
			{
				if (!list.Contains(attachedRoad2.id))
				{
					list.Add(attachedRoad2.id);
				}
			}
		}
		Debug.LogFormat("Generating {0} road clusters. {1} segments, {2} intersections. {3} models to apply.", clusters.Length, list2.Count, list3.Count, list.Count);
		foreach (int item3 in list)
		{
			RoadSegmentModel roadSegmentModel2 = segmentModels[item3];
			roadSegmentModel2.masterMesh.ApplyToMesh(roadSegmentModel2.meshFilter.sharedMesh);
			if (VTMapManager.nextLaunchMode != VTMapManager.MapLaunchModes.MapEditor)
			{
				MeshCollider component = roadSegmentModel2.gameObject.GetComponent<MeshCollider>();
				if (!component)
				{
					component = roadSegmentModel2.gameObject.AddComponent<MeshCollider>();
					continue;
				}
				component.enabled = false;
				component.enabled = true;
			}
		}
	}

	private void WeldInterSegments(List<VTRoadSystem.RoadSegment> segments)
	{
		foreach (VTRoadSystem.RoadSegment segment in segments)
		{
			RoadSet roadSet = setDictionary[segment.roadSet];
			RoadSegmentModel roadSegmentModel = segmentModels[segment.id];
			RoadMeshProfile roadMeshProfile = (segment.bridge ? roadSet.bridge : roadSet.mainRoad);
			if (segment.nextSegment != null)
			{
				RoadSegmentModel roadSegmentModel2 = segmentModels[segment.nextSegment.id];
				if (segment.nextSegment.nextSegment == segment)
				{
					List<int> frontVerts = roadMeshProfile.frontVerts;
					int intersectionVertOffset = Mathf.Max(0, roadMeshProfile.segMesh.verts.Count * (roadSegmentModel.segment.roadVerts.Count - 2));
					int roadVertOffset = Mathf.Max(0, roadMeshProfile.segMesh.verts.Count * (roadSegmentModel2.segment.roadVerts.Count - 2));
					WeldSegmentModelToIntersection(frontVerts, roadSegmentModel, intersectionVertOffset, frontVerts, roadSegmentModel2, roadVertOffset, reverseRoadIndices: true);
				}
				else if (segment.nextSegment.previousSegment == segment)
				{
					List<int> frontVerts2 = roadMeshProfile.frontVerts;
					List<int> rearVerts = roadMeshProfile.rearVerts;
					int intersectionVertOffset2 = Mathf.Max(0, roadMeshProfile.segMesh.verts.Count * (roadSegmentModel.segment.roadVerts.Count - 2));
					int roadVertOffset2 = 0;
					WeldSegmentModelToIntersection(frontVerts2, roadSegmentModel, intersectionVertOffset2, rearVerts, roadSegmentModel2, roadVertOffset2, reverseRoadIndices: true);
				}
			}
			else if (segment.previousSegment != null)
			{
				RoadSegmentModel roadSegmentModel3 = segmentModels[segment.previousSegment.id];
				if (segment.previousSegment.previousSegment == segment)
				{
					List<int> rearVerts2 = roadMeshProfile.rearVerts;
					int num = 0;
					WeldSegmentModelToIntersection(rearVerts2, roadSegmentModel, num, rearVerts2, roadSegmentModel3, num, reverseRoadIndices: true);
				}
				else if (segment.previousSegment.nextSegment == segment)
				{
					List<int> rearVerts3 = roadMeshProfile.rearVerts;
					List<int> frontVerts3 = roadMeshProfile.frontVerts;
					int intersectionVertOffset3 = 0;
					int roadVertOffset3 = Mathf.Max(0, roadMeshProfile.segMesh.verts.Count * (roadSegmentModel3.segment.roadVerts.Count - 2));
					WeldSegmentModelToIntersection(rearVerts3, roadSegmentModel, intersectionVertOffset3, frontVerts3, roadSegmentModel3, roadVertOffset3, reverseRoadIndices: true);
				}
			}
		}
	}

	private void BuildIntersectionModel(VTRoadSystem.RoadIntersection intersection, RoadSet roadSet)
	{
		if (intersection.attachedRoads.Count == 1)
		{
			BuildEndPiece(intersection, roadSet);
			return;
		}
		if (intersection.attachedRoads.Count == 3)
		{
			BuildThreeWayIntersection(intersection, roadSet);
			return;
		}
		if (intersection.attachedRoads.Count == 4)
		{
			BuildFourWayIntersection(intersection, roadSet);
			return;
		}
		Debug.LogErrorFormat("Unhandled intersection with {0} attached roads!", intersection.attachedRoads.Count);
	}

	private void BuildFourWayIntersection(VTRoadSystem.RoadIntersection intersection, RoadSet roadSet)
	{
		RoadIntersectionProfile fourWay = roadSet.fourWay;
		bool flag = intersection.attachedRoads[0].endIntersection == intersection;
		bool flag2 = intersection.attachedRoads[1].endIntersection == intersection;
		bool flag3 = intersection.attachedRoads[2].endIntersection == intersection;
		bool flag4 = intersection.attachedRoads[3].endIntersection == intersection;
		VTRoadSystem.RoadVertex roadVertex = (flag ? intersection.attachedRoads[0].endVertex : intersection.attachedRoads[0].startVertex);
		VTRoadSystem.RoadVertex obj = (flag2 ? intersection.attachedRoads[1].endVertex : intersection.attachedRoads[1].startVertex);
		VTRoadSystem.RoadVertex roadVertex2 = (flag3 ? intersection.attachedRoads[2].endVertex : intersection.attachedRoads[2].startVertex);
		VTRoadSystem.RoadVertex roadVertex3 = (flag4 ? intersection.attachedRoads[3].endVertex : intersection.attachedRoads[3].startVertex);
		Vector3 normalized = (roadVertex.worldPos - roadVertex3.worldPos).normalized;
		Vector3 normalized2 = Vector3.Cross(obj.worldPos - roadVertex2.worldPos, normalized).normalized;
		normalized2 *= Mathf.Sign(normalized2.y);
		Quaternion quaternion = Quaternion.LookRotation(normalized, normalized2);
		Vector3 worldPosition = intersection.worldPosition;
		RoadSegmentModel roadSegmentModel = segmentModels[intersection.attachedRoads[0].id];
		int vertCount = roadSegmentModel.masterMesh.vertCount;
		RoadSegmentModel roadSegmentModel2 = segmentModels[intersection.attachedRoads[1].id];
		RoadSegmentModel roadSegmentModel3 = segmentModels[intersection.attachedRoads[2].id];
		RoadSegmentModel roadSegmentModel4 = segmentModels[intersection.attachedRoads[3].id];
		AppendMesh(fourWay.intersectionMesh, worldPosition, quaternion, roadSegmentModel, roadVertex.color);
		AttachIntersectionBottomsToTerrain(roadSegmentModel, fourWay, vertCount, roadVertex, quaternion);
		int roadVertOffset = (flag ? Mathf.Max(0, roadSet.mainRoad.segMesh.verts.Count * (roadSegmentModel.segment.roadVerts.Count - 2)) : 0);
		int roadVertOffset2 = (flag2 ? Mathf.Max(0, roadSet.mainRoad.segMesh.verts.Count * (roadSegmentModel2.segment.roadVerts.Count - 2)) : 0);
		int roadVertOffset3 = (flag3 ? Mathf.Max(0, roadSet.mainRoad.segMesh.verts.Count * (roadSegmentModel3.segment.roadVerts.Count - 2)) : 0);
		int roadVertOffset4 = (flag4 ? Mathf.Max(0, roadSet.mainRoad.segMesh.verts.Count * (roadSegmentModel4.segment.roadVerts.Count - 2)) : 0);
		WeldSegmentModelToIntersection(fourWay.connectionIndices[0].indices, roadSegmentModel, vertCount, flag ? roadSet.mainRoad.frontVerts : roadSet.mainRoad.rearVerts, roadSegmentModel, roadVertOffset);
		WeldSegmentModelToIntersection(fourWay.connectionIndices[1].indices, roadSegmentModel, vertCount, flag2 ? roadSet.mainRoad.frontVerts : roadSet.mainRoad.rearVerts, roadSegmentModel2, roadVertOffset2);
		WeldSegmentModelToIntersection(fourWay.connectionIndices[2].indices, roadSegmentModel, vertCount, flag3 ? roadSet.mainRoad.frontVerts : roadSet.mainRoad.rearVerts, roadSegmentModel3, roadVertOffset3);
		WeldSegmentModelToIntersection(fourWay.connectionIndices[3].indices, roadSegmentModel, vertCount, flag4 ? roadSet.mainRoad.frontVerts : roadSet.mainRoad.rearVerts, roadSegmentModel4, roadVertOffset4);
	}

	private void BuildThreeWayIntersection(VTRoadSystem.RoadIntersection intersection, RoadSet roadSet)
	{
		RoadIntersectionProfile threeWay = roadSet.threeWay;
		VTRoadSystem.RoadVertex startVertex = intersection.attachedRoads[0].startVertex;
		RoadSegmentModel roadSegmentModel = segmentModels[intersection.attachedRoads[0].id];
		bool flag = intersection.attachedRoads[1].endIntersection == intersection;
		bool flag2 = intersection.attachedRoads[2].endIntersection == intersection;
		bool flag3 = intersection.attachedRoads[0].endIntersection == intersection;
		startVertex = ((intersection.attachedRoads[0].endIntersection != intersection) ? intersection.attachedRoads[0].startVertex : intersection.attachedRoads[0].endVertex);
		Vector3 rhs = intersection.GetVertex(2).worldPos - intersection.GetVertex(1).worldPos;
		Vector3 vector = Vector3.Cross(Vector3.up, rhs) * (flag3 ? 1 : (-1));
		VTRoadSystem.RoadVertex vertex = intersection.GetVertex(1);
		RoadSegmentModel roadSegmentModel2 = segmentModels[intersection.attachedRoads[1].id];
		VTRoadSystem.RoadVertex vertex2 = intersection.GetVertex(2);
		RoadSegmentModel roadSegmentModel3 = segmentModels[intersection.attachedRoads[2].id];
		int count = roadSegmentModel.masterMesh.verts.Count;
		Vector3 worldPosition = intersection.worldPosition;
		Vector3 vector2 = vertex.normal + vertex2.normal + startVertex.normal;
		Quaternion quaternion = Quaternion.LookRotation(Vector3.ProjectOnPlane(vector * (flag3 ? 1 : (-1)), vector2), vector2);
		Debug.DrawLine(vertex.worldPos, vertex.worldPos + 10f * Vector3.up, Color.red, 100f);
		Debug.DrawLine(vertex2.worldPos, vertex2.worldPos + 10f * Vector3.up, Color.green, 100f);
		Debug.DrawLine(startVertex.worldPos, startVertex.worldPos + 10f * Vector3.up, Color.blue, 100f);
		AppendMesh(threeWay.intersectionMesh, worldPosition, quaternion, roadSegmentModel, startVertex.color);
		AttachIntersectionBottomsToTerrain(roadSegmentModel, threeWay, count, startVertex, quaternion);
		int roadVertOffset = (flag ? Mathf.Max(0, roadSet.mainRoad.segMesh.verts.Count * (roadSegmentModel2.segment.roadVerts.Count - 2)) : 0);
		WeldSegmentModelToIntersection(threeWay.connectionIndices[0].indices, roadSegmentModel, count, flag ? roadSet.mainRoad.frontVerts : roadSet.mainRoad.rearVerts, roadSegmentModel2, roadVertOffset);
		int roadVertOffset2 = (flag2 ? Mathf.Max(0, roadSet.mainRoad.segMesh.verts.Count * (roadSegmentModel3.segment.roadVerts.Count - 2)) : 0);
		WeldSegmentModelToIntersection(threeWay.connectionIndices[1].indices, roadSegmentModel, count, flag2 ? roadSet.mainRoad.frontVerts : roadSet.mainRoad.rearVerts, roadSegmentModel3, roadVertOffset2);
		int roadVertOffset3 = (flag3 ? Mathf.Max(0, roadSet.mainRoad.segMesh.verts.Count * (roadSegmentModel.segment.roadVerts.Count - 2)) : 0);
		WeldSegmentModelToIntersection(threeWay.connectionIndices[2].indices, roadSegmentModel, count, flag3 ? roadSet.mainRoad.frontVerts : roadSet.mainRoad.rearVerts, roadSegmentModel, roadVertOffset3);
	}

	private void AppendMesh(VTTerrainMesh newItemMesh, Vector3 newMeshPos, Quaternion newMeshRot, RoadSegmentModel existingModel, Color terrainColor)
	{
		VTTerrainMesh masterMesh = existingModel.masterMesh;
		int count = masterMesh.verts.Count;
		for (int i = 0; i < newItemMesh.verts.Count; i++)
		{
			Vector3 vector = newItemMesh.verts[i];
			vector = newMeshRot * vector;
			vector += newMeshPos;
			vector = existingModel.transform.InverseTransformPoint(vector);
			masterMesh.verts.Add(vector);
			masterMesh.normals.Add(existingModel.transform.InverseTransformDirection(newMeshRot * newItemMesh.normals[i]));
			masterMesh.uvs.Add(newItemMesh.uvs[i]);
			masterMesh.colors.Add(terrainColor);
		}
		for (int j = 0; j < newItemMesh.subMeshCount; j++)
		{
			for (int k = 0; k < newItemMesh.subMeshTriangles[j].Count; k++)
			{
				masterMesh.subMeshTriangles[j].Add(newItemMesh.subMeshTriangles[j][k] + count);
			}
		}
	}

	private void WeldSegmentModelToIntersection(List<int> intersectionVerts, RoadSegmentModel intersectionModel, int intersectionVertOffset, List<int> roadEndVerts, RoadSegmentModel roadModel, int roadVertOffset, bool reverseRoadIndices = false)
	{
		for (int i = 0; i < roadEndVerts.Count; i++)
		{
			int index = i;
			if (reverseRoadIndices)
			{
				index = roadEndVerts.Count - 1 - i;
			}
			int index2 = roadEndVerts[index] + roadVertOffset;
			int index3 = intersectionVerts[i] + intersectionVertOffset;
			roadModel.masterMesh.verts[index2] = roadModel.transform.InverseTransformPoint(intersectionModel.transform.TransformPoint(intersectionModel.masterMesh.verts[index3]));
		}
	}

	private void AttachIntersectionBottomsToTerrain(RoadSegmentModel model, RoadIntersectionProfile meshProfile, int vertOffset, VTRoadSystem.RoadVertex roadVert, Quaternion rot)
	{
		for (int i = 0; i < meshProfile.bottomVerts.Count; i++)
		{
			Vector3 vector = meshProfile.intersectionMesh.verts[meshProfile.bottomVerts[i]];
			int index = meshProfile.bottomVerts[i] + vertOffset;
			Vector3 position = model.transform.TransformPoint(model.masterMesh.verts[index]);
			position += new Vector3(0f, 0f - roadVert.height + vector.y, 0f);
			Vector3 vector2 = vector;
			vector2.y = 0f;
			vector2.Normalize();
			vector2 = rot * vector2;
			position += 2f * roadVert.height * vector2;
			model.masterMesh.verts[index] = model.transform.InverseTransformPoint(position);
			model.masterMesh.normals[index] = model.transform.InverseTransformDirection(roadVert.normal);
		}
	}

	private void BuildEndPiece(VTRoadSystem.RoadIntersection intersection, RoadSet roadSet)
	{
		RoadIntersectionProfile endPiece = roadSet.endPiece;
		bool flag = false;
		VTRoadSystem.RoadVertex roadVertex;
		if (intersection.attachedRoads[0].startIntersection != null && intersection.attachedRoads[0].startIntersection.id == intersection.id)
		{
			flag = true;
			roadVertex = intersection.attachedRoads[0].startVertex;
		}
		else
		{
			if (intersection.attachedRoads[0].endIntersection == null || intersection.attachedRoads[0].endIntersection.id != intersection.id)
			{
				Debug.LogErrorFormat("And end piece for Road {0} was improperly configured.", intersection.attachedRoads[0].id);
				return;
			}
			flag = false;
			roadVertex = intersection.attachedRoads[0].endVertex;
		}
		Vector3 forward = (flag ? roadVertex.tangent : (-roadVertex.tangent));
		Vector3 worldPos = roadVertex.worldPos;
		float height = roadVertex.height;
		Color color = roadVertex.color;
		Vector3 normal = roadVertex.normal;
		Quaternion quaternion = Quaternion.LookRotation(forward, normal);
		VTTerrainMesh intersectionMesh = endPiece.intersectionMesh;
		if (!segmentModels.ContainsKey(intersection.attachedRoads[0].id))
		{
			Debug.LogError("No segment model found for Road " + intersection.attachedRoads[0].id);
		}
		RoadSegmentModel roadSegmentModel = segmentModels[intersection.attachedRoads[0].id];
		VTTerrainMesh masterMesh = roadSegmentModel.masterMesh;
		int count = masterMesh.verts.Count;
		AppendMesh(intersectionMesh, worldPos, quaternion, roadSegmentModel, color);
		for (int i = 0; i < endPiece.bottomVerts.Count; i++)
		{
			Vector3 vector = intersectionMesh.verts[endPiece.bottomVerts[i]];
			int index = endPiece.bottomVerts[i] + count;
			Vector3 position = roadSegmentModel.transform.TransformPoint(masterMesh.verts[index]);
			position += new Vector3(0f, 0f - height + intersectionMesh.verts[endPiece.bottomVerts[i]].y, 0f);
			Vector3 vector2 = vector;
			vector2.y = 0f;
			vector2.Normalize();
			vector2 = quaternion * vector2;
			position += 2f * height * vector2;
			masterMesh.verts[index] = roadSegmentModel.transform.InverseTransformPoint(position);
			masterMesh.normals[index] = roadSegmentModel.transform.InverseTransformDirection(roadVertex.normal);
		}
		List<int> list = (flag ? roadSet.mainRoad.rearVerts : roadSet.mainRoad.frontVerts);
		int num = ((!flag) ? Mathf.Max(0, roadSet.mainRoad.segMesh.verts.Count * (roadSegmentModel.segment.roadVerts.Count - 2)) : 0);
		foreach (int item in list)
		{
			Vector3 vector3 = masterMesh.verts[item + num];
			Vector3 value = Vector3.zero;
			Vector3 vector4 = masterMesh.normals[item + num];
			float num2 = float.MaxValue;
			foreach (int index2 in endPiece.connectionIndices[0].indices)
			{
				Vector3 vector5 = masterMesh.verts[index2 + count];
				float sqrMagnitude = (vector5 - vector3).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					value = vector5;
					vector4 = masterMesh.normals[index2 + count];
				}
			}
			masterMesh.verts[item + num] = value;
			masterMesh.normals[item + num] = (masterMesh.normals[item] + vector4).normalized;
		}
	}

	private void BuildSegmentModel(RoadSegmentModel model)
	{
		model.masterMesh.Clear();
		model.masterMesh.subMeshCount = materials.Length;
		RoadSet roadSet = GetRoadSet(model.segment.roadSet);
		if (model.segment.bridge)
		{
			int i = 0;
			int num = model.segment.roadVerts.Count - 1;
			if (model.segment.previousSegment != null && !model.segment.previousSegment.bridge)
			{
				i = 1;
			}
			bool flag = false;
			if (model.segment.nextSegment != null && !model.segment.nextSegment.bridge)
			{
				flag = true;
				num--;
			}
			for (; i < num; i++)
			{
				VTRoadSystem.RoadVertex a = model.segment.roadVerts[i];
				VTRoadSystem.RoadVertex b = model.segment.roadVerts[i + 1];
				AppendSegment(a, b, roadSet.bridge, model.masterMesh, model.transform, bridge: true);
			}
			if (flag)
			{
				_ = model.segment.roadVerts[num];
				_ = model.segment.roadVerts[num + 1];
			}
		}
		else
		{
			for (int j = 0; j < model.segment.roadVerts.Count - 1; j++)
			{
				VTRoadSystem.RoadVertex a2 = model.segment.roadVerts[j];
				VTRoadSystem.RoadVertex b2 = model.segment.roadVerts[j + 1];
				AppendSegment(a2, b2, roadSet.mainRoad, model.masterMesh, model.transform, model.segment.bridge);
			}
		}
	}

	private static void AppendSegment(VTRoadSystem.RoadVertex a, VTRoadSystem.RoadVertex b, RoadMeshProfile roadProfile, VTTerrainMesh masterMesh, Transform modelTransform, bool bridge)
	{
		Vector3 normal = a.normal;
		Vector3 normal2 = b.normal;
		Quaternion quaternion = Quaternion.LookRotation(a.tangent, normal);
		Quaternion quaternion2 = Quaternion.LookRotation(b.tangent, normal2);
		Vector3 vector = quaternion * Vector3.right;
		VTTerrainMesh segMesh = roadProfile.segMesh;
		List<int> rearVerts = roadProfile.rearVerts;
		List<int> frontVerts = roadProfile.frontVerts;
		List<int> bottomVerts = roadProfile.bottomVerts;
		int count = masterMesh.verts.Count;
		for (int i = 0; i < segMesh.verts.Count; i++)
		{
			masterMesh.verts.Add(segMesh.verts[i]);
			masterMesh.uvs.Add(segMesh.uvs[i]);
			masterMesh.normals.Add(modelTransform.InverseTransformDirection(quaternion * segMesh.normals[i]));
			masterMesh.colors.Add(a.color);
		}
		for (int j = 0; j < segMesh.subMeshCount; j++)
		{
			for (int k = 0; k < segMesh.subMeshTriangles[j].Count; k++)
			{
				masterMesh.subMeshTriangles[j].Add(segMesh.subMeshTriangles[j][k] + count);
			}
		}
		for (int l = 0; l < rearVerts.Count; l++)
		{
			Vector3 vector2 = segMesh.verts[rearVerts[l]];
			vector2.z = 0f;
			vector2 = quaternion * vector2;
			vector2 += a.worldPos;
			vector2 = modelTransform.InverseTransformPoint(vector2);
			int index = rearVerts[l] + count;
			masterMesh.verts[index] = vector2;
			masterMesh.colors[index] = a.color;
		}
		for (int m = 0; m < frontVerts.Count; m++)
		{
			Vector3 vector3 = segMesh.verts[frontVerts[m]];
			vector3.z = 0f;
			vector3 = quaternion2 * vector3;
			vector3 += b.worldPos;
			vector3 = modelTransform.InverseTransformPoint(vector3);
			int index2 = frontVerts[m] + count;
			masterMesh.verts[index2] = vector3;
			masterMesh.colors[index2] = b.color;
		}
		for (int n = 0; n < bottomVerts.Count; n++)
		{
			int index3 = bottomVerts[n] + count;
			Vector3 position = modelTransform.TransformPoint(masterMesh.verts[index3]);
			float height = b.height;
			position += new Vector3(0f, 0f - height + segMesh.verts[bottomVerts[n]].y, 0f);
			position += 2f * height * vector * Mathf.Sign(Vector3.Dot(segMesh.verts[bottomVerts[n]], Vector3.right));
			masterMesh.verts[index3] = modelTransform.InverseTransformPoint(position);
			masterMesh.normals[index3] = modelTransform.InverseTransformDirection(a.normal);
		}
		if (count <= 0)
		{
			return;
		}
		int num = 0;
		int num2 = rearVerts.Count - 1;
		while (num < rearVerts.Count)
		{
			int index4 = frontVerts[num2] + count - segMesh.verts.Count;
			int index5 = rearVerts[num] + count;
			Vector3 vector4 = masterMesh.verts[index4];
			Vector3 vector7 = (masterMesh.verts[index4] = (masterMesh.verts[index5] = vector4));
			if (!bridge)
			{
				Vector3 normalized = (masterMesh.normals[index4] + masterMesh.normals[index5]).normalized;
				vector7 = (masterMesh.normals[index4] = (masterMesh.normals[index5] = normalized));
			}
			num++;
			num2--;
		}
	}

	private void RemoveTrees(VTRoadSystem.RoadSegment segment)
	{
		List<IntVector2> list = new List<IntVector2>();
		float num = 900f;
		foreach (VTRoadSystem.RoadVertex roadVert in segment.roadVerts)
		{
			IntVector2 intVector = VTMapGenerator.fetch.ChunkGridAtPos(roadVert.worldPos);
			Transform gridTransform = VTMapGenerator.fetch.GetTerrainChunk(intVector).gridTransform;
			VTTMapTrees.TreeChunk chunk = VTTMapTrees.fetch.GetChunk(roadVert.worldPos);
			if (chunk == null)
			{
				continue;
			}
			List<int> list2 = new List<int>();
			for (int num2 = chunk.treePoints.Count - 1; num2 >= 0; num2--)
			{
				if ((gridTransform.TransformPoint(chunk.treePoints[num2]) - roadVert.worldPos).sqrMagnitude < num)
				{
					list2.Add(num2);
				}
			}
			foreach (int item in list2)
			{
				chunk.treePoints.RemoveAt(item);
			}
			chunk.treeScales.RemoveRange(chunk.treePoints.Count, chunk.treeScales.Count - chunk.treePoints.Count);
			if (!list.Contains(intVector))
			{
				list.Add(intVector);
			}
		}
		foreach (IntVector2 item2 in list)
		{
			VTTMapTrees.fetch.DespawnMeshPoolChunk(item2);
		}
	}
}
