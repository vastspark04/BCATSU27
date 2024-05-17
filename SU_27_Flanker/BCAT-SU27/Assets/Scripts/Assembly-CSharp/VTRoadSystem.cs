using System;
using System.Collections.Generic;
using UnityEngine;

public class VTRoadSystem
{
	public delegate void ClusterDelegate(RoadCluster[] roadClusters);

	public delegate void SegmentDelegate(RoadSegment seg);

	public struct StartPointInfo
	{
		public Statuses status;

		public bool snapped;

		public Vector3 snapPos;
	}

	public enum Statuses
	{
		Good,
		Invalid,
		Too_Steep,
		Intersection,
		Too_Sharp,
		Invalid_Connection
	}

	public struct PlacementStatus
	{
		public Statuses status;

		public bool bridge;
	}

	public class RoadCluster
	{
		public bool isDirty;

		public IntVector2 grid;

		public List<RoadSegment> segments = new List<RoadSegment>();

		public List<RoadIntersection> intersections = new List<RoadIntersection>();
	}

	public class RoadVertex
	{
		public Vector3D globalPoint;

		public Vector3 tangent;

		public float height;

		public Color color;

		public Vector3 normal;

		public const string NODE_NAME = "Vert";

		public Vector3 worldPos
		{
			get
			{
				return VTMapManager.GlobalToWorldPoint(globalPoint);
			}
			set
			{
				globalPoint = VTMapManager.WorldToGlobalPoint(value);
			}
		}

		public RoadVertex()
		{
		}

		public RoadVertex(Vector3 worldPos, Vector3 tangent, float height, Color color, Vector3 normal)
		{
			globalPoint = VTMapManager.WorldToGlobalPoint(worldPos);
			this.tangent = tangent;
			this.height = height;
			this.color = color;
			this.normal = normal;
		}

		public RoadVertex(Vector3D globalPoint, Vector3 tangent, float height, Color color, Vector3 normal)
		{
			this.globalPoint = globalPoint;
			this.tangent = tangent;
			this.height = height;
			this.color = color;
			this.normal = normal;
		}

		public ConfigNode WriteToConfigNode()
		{
			ConfigNode configNode = new ConfigNode("Vert");
			configNode.SetValue("globalPoint", ConfigNodeUtils.WriteVector3D(globalPoint));
			configNode.SetValue("tangent", ConfigNodeUtils.WriteVector3(tangent));
			configNode.SetValue("height", height);
			configNode.SetValue("color", ConfigNodeUtils.WriteColor(color));
			configNode.SetValue("normal", ConfigNodeUtils.WriteVector3(normal));
			return configNode;
		}

		public static RoadVertex LoadFromVertexNode(ConfigNode node)
		{
			return new RoadVertex
			{
				globalPoint = ConfigNodeUtils.ParseVector3D(node.GetValue("globalPoint")),
				tangent = ConfigNodeUtils.ParseVector3(node.GetValue("tangent")),
				height = ConfigNodeUtils.ParseFloat(node.GetValue("height")),
				color = ConfigNodeUtils.ParseColor(node.GetValue("color")),
				normal = ConfigNodeUtils.ParseVector3(node.GetValue("normal"))
			};
		}
	}

	public class RoadSegment : IEquatable<RoadSegment>
	{
		public string roadSet;

		public bool bridge;

		public VTRoadSystem roadSystem;

		private IntVector2 clusterGrid;

		public List<RoadVertex> roadVerts;

		public RoadSegment nextSegment;

		public RoadSegment previousSegment;

		public RoadIntersection startIntersection;

		public RoadIntersection endIntersection;

		public RoadCluster cluster
		{
			get
			{
				return roadSystem.roadClusters[clusterGrid];
			}
			set
			{
				clusterGrid = value.grid;
			}
		}

		public int id { get; private set; }

		public RoadVertex startVertex => roadVerts[0];

		public RoadVertex endVertex => roadVerts[roadVerts.Count - 1];

		public bool Equals(RoadSegment other)
		{
			return id == other.id;
		}

		public RoadSegment(int id, VTRoadSystem roadSystem, bool bridge)
		{
			this.roadSystem = roadSystem;
			this.id = id;
			this.bridge = bridge;
			roadVerts = new List<RoadVertex>();
		}
	}

	public class RoadIntersection : IEquatable<RoadIntersection>
	{
		public VTRoadSystem roadSystem;

		private IntVector2 clusterGrid;

		public string roadSet;

		public List<RoadSegment> attachedRoads;

		public RoadCluster cluster
		{
			get
			{
				return roadSystem.roadClusters[clusterGrid];
			}
			set
			{
				clusterGrid = value.grid;
			}
		}

		public int id { get; private set; }

		public Vector3 worldPosition
		{
			get
			{
				if (attachedRoads.Count == 2)
				{
					return Vector3.Lerp(GetVertex(0).worldPos, GetVertex(1).worldPos, 0.5f);
				}
				if (attachedRoads.Count == 3)
				{
					return Vector3.Lerp(GetVertex(1).worldPos, GetVertex(2).worldPos, 0.5f);
				}
				if (attachedRoads.Count == 4)
				{
					return Vector3.Lerp(GetVertex(1).worldPos, GetVertex(2).worldPos, 0.5f);
				}
				return GetVertex(0).worldPos;
			}
		}

		public bool Equals(RoadIntersection other)
		{
			return id == other.id;
		}

		public RoadIntersection(int id, VTRoadSystem roadSystem)
		{
			this.roadSystem = roadSystem;
			this.id = id;
			attachedRoads = new List<RoadSegment>();
		}

		public RoadVertex GetVertex(int roadIdx)
		{
			if (roadIdx >= attachedRoads.Count)
			{
				return null;
			}
			RoadSegment roadSegment = attachedRoads[roadIdx];
			if (roadSegment.endIntersection == this)
			{
				return roadSegment.endVertex;
			}
			return roadSegment.startVertex;
		}
	}

	private struct LoadSegmentInfo
	{
		public RoadSegment segment;

		public int nextSegID;

		public int prevSegID;

		public int startIntersectID;

		public int endIntersectID;

		public LoadSegmentInfo(RoadSegment segment, ConfigNode segNode)
		{
			this.segment = segment;
			nextSegID = -1;
			if (segNode.HasValue("nextSegment"))
			{
				nextSegID = ConfigNodeUtils.ParseInt(segNode.GetValue("nextSegment"));
			}
			prevSegID = -1;
			if (segNode.HasValue("previousSegment"))
			{
				prevSegID = ConfigNodeUtils.ParseInt(segNode.GetValue("previousSegment"));
			}
			startIntersectID = -1;
			if (segNode.HasValue("startIntersection"))
			{
				startIntersectID = ConfigNodeUtils.ParseInt(segNode.GetValue("startIntersection"));
			}
			endIntersectID = -1;
			if (segNode.HasValue("endIntersection"))
			{
				endIntersectID = ConfigNodeUtils.ParseInt(segNode.GetValue("endIntersection"));
			}
		}
	}

	private struct LoadIntersectionInfo
	{
		public RoadIntersection intersection;

		public List<int> attachedRoads;

		public LoadIntersectionInfo(RoadIntersection intersection, List<int> roads)
		{
			this.intersection = intersection;
			attachedRoads = roads;
		}
	}

	public enum RoadTypes
	{
		Road,
		Bridge,
		Adapter
	}

	public const float RAY_DISTANCE = 3000f;

	private const float HALF_RAY_DISTANCE = 1500f;

	public const float MIN_SLOPE_DOT = 0.95105f;

	public const int MAX_VERT_COUNT = 64;

	private const int CLUSTER_SIZE = 2500;

	public const float SNAP_RADIUS_MULT = 2f;

	public const float MAX_VERT_ANGLE = 30f;

	private int nextID;

	private Dictionary<IntVector2, RoadCluster> roadClusters = new Dictionary<IntVector2, RoadCluster>();

	private Dictionary<int, object> allRoadObjects = new Dictionary<int, object>();

	public const string NODE_NAME = "RoadSystem";

	public event ClusterDelegate OnRebuildClusterModel;

	public event SegmentDelegate OnDeletedSegment;

	private RoadCluster GetOrCreateCluster(Vector3D globalPos)
	{
		return GetOrCreateCluster(GlobalPosToGrid(globalPos));
	}

	private RoadCluster GetOrCreateCluster(IntVector2 grid)
	{
		RoadCluster roadCluster;
		if (roadClusters.ContainsKey(grid))
		{
			roadCluster = roadClusters[grid];
		}
		else
		{
			roadCluster = new RoadCluster();
			roadCluster.grid = grid;
			roadClusters.Add(grid, roadCluster);
		}
		return roadCluster;
	}

	private RoadCluster GetCluster(Vector3D globalPoint)
	{
		IntVector2 key = GlobalPosToGrid(globalPoint);
		if (roadClusters.ContainsKey(key))
		{
			return roadClusters[key];
		}
		return null;
	}

	public void AddRoad(Vector3[] positions, RoadSet roadSet, int startSnapID, int endSnapID, bool bridge)
	{
		List<RoadSegment> list = new List<RoadSegment>();
		RoadSegment roadSegment = new RoadSegment(nextID++, this, bridge);
		allRoadObjects.Add(roadSegment.id, roadSegment);
		roadSegment.roadSet = roadSet.name;
		RoadVertex item = null;
		for (int i = 0; i < positions.Length; i++)
		{
			if (roadSegment.roadVerts.Count >= 64)
			{
				list.Add(roadSegment);
				RoadSegment roadSegment2 = roadSegment;
				roadSegment = new RoadSegment(nextID++, this, bridge);
				roadSegment.roadSet = roadSet.name;
				allRoadObjects.Add(roadSegment.id, roadSegment);
				roadSegment.roadVerts.Add(item);
				roadSegment.previousSegment = roadSegment2;
				roadSegment2.nextSegment = roadSegment;
			}
			Vector3 tangent = ((i != positions.Length - 1) ? (positions[i + 1] - positions[i]).normalized : (positions[i] - positions[i - 1]).normalized);
			GetVertData(positions[i], tangent, roadSet.mainRoad.segmentLength, roadSet.radius, out var height, out var normal, bridge);
			height += 0.5f;
			RoadVertex roadVertex = new RoadVertex(positions[i] + height * Vector3.up, tangent, 1.25f * height, GetTerrainColor(positions[i]), normal);
			roadSegment.roadVerts.Add(roadVertex);
			item = roadVertex;
		}
		list.Add(roadSegment);
		for (int j = 0; j < list.Count; j++)
		{
			RoadSegment roadSegment3 = list[j];
			IntVector2 grid = GlobalPosToGrid(roadSegment3.startVertex.globalPoint);
			RoadCluster orCreateCluster = GetOrCreateCluster(grid);
			if (j == 0)
			{
				Vector3 worldPos = roadSegment3.startVertex.worldPos;
				if (startSnapID >= 0)
				{
					object obj = allRoadObjects[startSnapID];
					if (obj is RoadSegment)
					{
						roadSegment3.startIntersection = CreateThreeWayIntersection(roadSet, roadSegment3, orCreateCluster, (RoadSegment)obj, worldPos);
					}
					else
					{
						RoadIntersection roadIntersection = (RoadIntersection)obj;
						if (roadIntersection.attachedRoads.Count == 1)
						{
							RoadSegment roadSegment4 = roadIntersection.attachedRoads[0];
							if (roadSegment4.endIntersection == roadIntersection)
							{
								roadSegment4.nextSegment = roadSegment3;
								roadSegment4.endIntersection = null;
								roadSegment3.startVertex.globalPoint = roadSegment4.endVertex.globalPoint;
							}
							else
							{
								roadSegment4.previousSegment = roadSegment3;
								roadSegment4.startIntersection = null;
								roadSegment3.startVertex.globalPoint = roadSegment4.startVertex.globalPoint;
							}
							roadSegment4.cluster.isDirty = true;
							roadIntersection.cluster.intersections.Remove(roadIntersection);
							allRoadObjects.Remove(roadIntersection.id);
							roadSegment3.previousSegment = roadSegment4;
						}
						else
						{
							roadIntersection.attachedRoads.Add(roadSegment3);
							roadSegment3.startIntersection = roadIntersection;
							if (Vector3.Dot(Vector3.Cross(Vector3.up, roadIntersection.GetVertex(1).worldPos - roadIntersection.GetVertex(2).worldPos), roadIntersection.GetVertex(0).worldPos - roadIntersection.worldPosition) > 0f)
							{
								RoadSegment value = roadIntersection.attachedRoads[0];
								roadIntersection.attachedRoads[0] = roadIntersection.attachedRoads[3];
								roadIntersection.attachedRoads[3] = value;
							}
							foreach (RoadSegment attachedRoad in roadIntersection.attachedRoads)
							{
								SetClusterDirty(attachedRoad);
							}
						}
					}
				}
				else
				{
					roadSegment3.startIntersection = CreateEndPiece(roadSet.name, roadSegment3, orCreateCluster);
				}
			}
			if (j == list.Count - 1)
			{
				Vector3 worldPos2 = roadSegment3.endVertex.worldPos;
				if (endSnapID >= 0)
				{
					object obj2 = allRoadObjects[endSnapID];
					if (obj2 is RoadSegment)
					{
						roadSegment3.endIntersection = CreateThreeWayIntersection(roadSet, roadSegment3, orCreateCluster, (RoadSegment)obj2, worldPos2);
					}
					else
					{
						RoadIntersection roadIntersection2 = (RoadIntersection)obj2;
						if (roadIntersection2.attachedRoads.Count == 1)
						{
							RoadSegment roadSegment5 = roadIntersection2.attachedRoads[0];
							if (roadSegment5.endIntersection == roadIntersection2)
							{
								roadSegment5.nextSegment = roadSegment3;
								roadSegment5.endIntersection = null;
								roadSegment3.endVertex.globalPoint = roadSegment5.endVertex.globalPoint;
							}
							else
							{
								roadSegment5.previousSegment = roadSegment3;
								roadSegment5.startIntersection = null;
								roadSegment3.endVertex.globalPoint = roadSegment5.startVertex.globalPoint;
							}
							roadSegment5.cluster.isDirty = true;
							roadIntersection2.cluster.intersections.Remove(roadIntersection2);
							allRoadObjects.Remove(roadIntersection2.id);
							roadSegment3.nextSegment = roadSegment5;
						}
						else
						{
							roadIntersection2.attachedRoads.Add(roadSegment3);
							roadSegment3.endIntersection = roadIntersection2;
							if (Vector3.Dot(Vector3.Cross(Vector3.up, roadIntersection2.GetVertex(1).worldPos - roadIntersection2.GetVertex(2).worldPos), roadIntersection2.GetVertex(0).worldPos - roadIntersection2.worldPosition) > 0f)
							{
								RoadSegment value2 = roadIntersection2.attachedRoads[0];
								roadIntersection2.attachedRoads[0] = roadIntersection2.attachedRoads[3];
								roadIntersection2.attachedRoads[3] = value2;
							}
							foreach (RoadSegment attachedRoad2 in roadIntersection2.attachedRoads)
							{
								SetClusterDirty(attachedRoad2);
							}
						}
					}
				}
				else
				{
					roadSegment3.endIntersection = CreateEndPiece(roadSet.name, roadSegment3, orCreateCluster);
				}
			}
			orCreateCluster.segments.Add(roadSegment3);
			roadSegment3.cluster = orCreateCluster;
			orCreateCluster.isDirty = true;
		}
		RebuildDirtyModels();
	}

	public void RecalculateAllHeights()
	{
		foreach (RoadCluster value in roadClusters.Values)
		{
			foreach (RoadSegment segment in value.segments)
			{
				RoadSet roadSet = VTMapRoads.instance.GetRoadSet(segment.roadSet);
				foreach (RoadVertex roadVert in segment.roadVerts)
				{
					Vector3 vector = SurfacePoint(roadVert.worldPos, 0f);
					GetVertData(vector, roadVert.tangent, roadSet.mainRoad.segmentLength, roadSet.radius, out var height, out var normal, segment.bridge);
					height += 0.5f;
					roadVert.worldPos = vector + height * Vector3.up;
					roadVert.height = 1.25f * height;
					roadVert.normal = normal;
				}
			}
		}
		RebuildAllModels();
	}

	private RoadIntersection CreateEndPiece(string roadSet, RoadSegment seg, RoadCluster cluster)
	{
		RoadIntersection roadIntersection = new RoadIntersection(nextID++, this);
		roadIntersection.roadSet = roadSet;
		roadIntersection.attachedRoads.Add(seg);
		roadIntersection.cluster = cluster;
		cluster.intersections.Add(roadIntersection);
		cluster.isDirty = true;
		allRoadObjects.Add(roadIntersection.id, roadIntersection);
		return roadIntersection;
	}

	private RoadIntersection CreateThreeWayIntersection(RoadSet roadSet, RoadSegment seg, RoadCluster cluster, RoadSegment otherSeg, Vector3 snapPos)
	{
		RoadIntersection roadIntersection = new RoadIntersection(nextID++, this);
		roadIntersection.roadSet = roadSet.name;
		roadIntersection.attachedRoads.Add(seg);
		roadIntersection.cluster = cluster;
		cluster.intersections.Add(roadIntersection);
		allRoadObjects.Add(roadIntersection.id, roadIntersection);
		RoadIntersection endIntersection = otherSeg.endIntersection;
		int num = IndexOfMidpoint(snapPos, otherSeg) + 1;
		RoadSegment roadSegment = new RoadSegment(nextID++, this, seg.bridge);
		roadSegment.roadSet = roadSet.name;
		allRoadObjects.Add(roadSegment.id, roadSegment);
		for (int i = num; i < otherSeg.roadVerts.Count; i++)
		{
			roadSegment.roadVerts.Add(otherSeg.roadVerts[i]);
		}
		RoadCluster cluster2 = GetCluster(otherSeg.startVertex.globalPoint);
		cluster2.segments.Remove(roadSegment);
		RoadCluster orCreateCluster = GetOrCreateCluster(roadSegment.startVertex.globalPoint);
		orCreateCluster.segments.Add(roadSegment);
		roadSegment.cluster = orCreateCluster;
		otherSeg.roadVerts.RemoveRange(num, otherSeg.roadVerts.Count - num);
		roadIntersection.attachedRoads.Add(otherSeg);
		roadIntersection.attachedRoads.Add(roadSegment);
		otherSeg.endIntersection = roadIntersection;
		roadSegment.startIntersection = roadIntersection;
		roadSegment.nextSegment = otherSeg.nextSegment;
		if (roadSegment.nextSegment != null)
		{
			if (roadSegment.nextSegment.previousSegment == otherSeg)
			{
				roadSegment.nextSegment.previousSegment = roadSegment;
			}
			else if (roadSegment.nextSegment.nextSegment == otherSeg)
			{
				roadSegment.nextSegment.nextSegment = roadSegment;
			}
		}
		otherSeg.nextSegment = null;
		cluster2.isDirty = true;
		orCreateCluster.isDirty = true;
		if (endIntersection != null)
		{
			roadSegment.endIntersection = endIntersection;
			for (int j = 0; j < endIntersection.attachedRoads.Count; j++)
			{
				if (endIntersection.attachedRoads[j].id == otherSeg.id)
				{
					endIntersection.attachedRoads[j] = roadSegment;
				}
			}
			cluster2.intersections.Remove(endIntersection);
			orCreateCluster.intersections.Add(endIntersection);
		}
		FixThreeWaySegmentOrder(roadIntersection);
		return roadIntersection;
	}

	private void FixThreeWaySegmentOrder(RoadIntersection intersection)
	{
		if (Vector3.Dot(Vector3.Cross(Vector3.up, intersection.GetVertex(2).worldPos - intersection.GetVertex(1).worldPos), intersection.GetVertex(0).worldPos - intersection.worldPosition) < 0f)
		{
			RoadSegment value = intersection.attachedRoads[1];
			intersection.attachedRoads[1] = intersection.attachedRoads[2];
			intersection.attachedRoads[2] = value;
		}
	}

	private int IndexOfVert(Vector3 position, RoadSegment segment)
	{
		int result = -1;
		float num = float.MaxValue;
		for (int i = 0; i < segment.roadVerts.Count; i++)
		{
			float sqrMagnitude = (position - segment.roadVerts[i].worldPos).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = i;
			}
		}
		return result;
	}

	private int IndexOfMidpoint(Vector3 position, RoadSegment segment)
	{
		int result = -1;
		float num = float.MaxValue;
		for (int i = 0; i < segment.roadVerts.Count - 1; i++)
		{
			Vector3 vector = Vector3.Lerp(segment.roadVerts[i].worldPos, segment.roadVerts[i + 1].worldPos, 0.5f);
			float sqrMagnitude = (position - vector).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = i;
			}
		}
		return result;
	}

	public void RebuildDirtyModels()
	{
		if (this.OnRebuildClusterModel == null)
		{
			return;
		}
		List<RoadCluster> list = new List<RoadCluster>(roadClusters.Count);
		foreach (RoadCluster value in roadClusters.Values)
		{
			if (value.isDirty)
			{
				list.Add(value);
				value.isDirty = false;
			}
		}
		this.OnRebuildClusterModel(list.ToArray());
	}

	public void RebuildAllModels()
	{
		if (this.OnRebuildClusterModel == null)
		{
			return;
		}
		RoadCluster[] array = new RoadCluster[roadClusters.Count];
		int num = 0;
		foreach (RoadCluster value in roadClusters.Values)
		{
			(array[num] = value).isDirty = false;
			num++;
		}
		this.OnRebuildClusterModel(array);
	}

	private static IntVector2 GlobalPosToGrid(Vector3D globalPos)
	{
		int x = Mathf.FloorToInt((float)globalPos.x / 2500f);
		int y = Mathf.FloorToInt((float)globalPos.z / 2500f);
		return new IntVector2(x, y);
	}

	private void SetClusterDirty(RoadSegment segment)
	{
		for (int i = 0; i < segment.roadVerts.Count; i++)
		{
			IntVector2 key = GlobalPosToGrid(segment.roadVerts[i].globalPoint);
			if (roadClusters.ContainsKey(key))
			{
				roadClusters[key].isDirty = true;
			}
		}
	}

	public PlacementStatus GetRoadPositions(Vector3 start, Vector3 end, ref Vector3[] positions, RoadSet roadSet, out int snappedStart, out int snappedEnd)
	{
		float magnitude = (start - end).magnitude;
		int num = Mathf.CeilToInt(magnitude / roadSet.mainRoad.segmentLength);
		PlacementStatus result = default(PlacementStatus);
		result.status = Statuses.Good;
		result.bridge = false;
		List<int> list = new List<int>();
		snappedStart = SnapToVertex(ref start, roadSet.radius);
		snappedEnd = SnapToVertex(ref end, roadSet.radius);
		Vector3 vector = start;
		Vector3 vector2 = end;
		Vector3 vector3 = Vector3.zero;
		Vector3 vector4 = Vector3.zero;
		if (snappedStart >= 0)
		{
			list.Add(snappedStart);
			object obj = allRoadObjects[snappedStart];
			if (obj is RoadSegment)
			{
				RoadSegment roadSegment = (RoadSegment)obj;
				Vector3 vector5 = (vector3 = roadSegment.roadVerts[IndexOfVert(vector, roadSegment)].tangent);
				Vector3 vector6 = Vector3.Lerp(vector, vector2, 1f / (float)num);
				vector3 = Vector3.Cross(Vector3.Project(Vector3.Cross(vector5, vector6 - vector), Vector3.up), vector5);
			}
			else
			{
				RoadIntersection roadIntersection = (RoadIntersection)obj;
				if (roadIntersection.attachedRoads.Count == 3)
				{
					vector3 = (roadIntersection.worldPosition - roadIntersection.GetVertex(0).worldPos).normalized;
				}
				else
				{
					vector3 = roadIntersection.GetVertex(0).tangent;
					if (roadIntersection.attachedRoads[0].startIntersection == roadIntersection)
					{
						vector3 *= -1f;
					}
				}
			}
		}
		if (snappedEnd >= 0)
		{
			list.Add(snappedEnd);
			object obj2 = allRoadObjects[snappedEnd];
			if (obj2 is RoadSegment)
			{
				RoadSegment roadSegment2 = (RoadSegment)obj2;
				Vector3 vector7 = (vector4 = roadSegment2.roadVerts[IndexOfVert(vector2, roadSegment2)].tangent);
				Vector3.Lerp(vector, vector2, (float)(num - 1) / (float)num);
				vector4 = Vector3.Cross(Vector3.Project(Vector3.Cross(vector7, vector - vector2), Vector3.up), vector7);
			}
			else
			{
				RoadIntersection roadIntersection2 = (RoadIntersection)obj2;
				if (roadIntersection2.attachedRoads.Count == 3)
				{
					vector4 = (roadIntersection2.worldPosition - roadIntersection2.GetVertex(0).worldPos).normalized;
				}
				else
				{
					vector4 = roadIntersection2.GetVertex(0).tangent;
					if (roadIntersection2.attachedRoads[0].startIntersection == roadIntersection2)
					{
						vector4 *= -1f;
					}
				}
			}
		}
		if (positions == null || positions.Length != num + 1)
		{
			positions = new Vector3[num + 1];
		}
		float blendFactor = ((float)num + 1f) * roadSet.mainRoad.segmentLength;
		for (int i = 0; i <= num; i++)
		{
			float t = (float)i / (float)num;
			Vector3 vert = Vector3.Lerp(start, end, t);
			if (snappedStart >= 0)
			{
				float dist = (float)i * roadSet.mainRoad.segmentLength;
				ApplyIntersectionBlend(ref vert, vector, vector3, roadSet, blendFactor, dist);
			}
			if (snappedEnd >= 0)
			{
				float dist2 = (float)(num - i) * roadSet.mainRoad.segmentLength;
				if (snappedEnd >= 0)
				{
					blendFactor = magnitude;
				}
				ApplyIntersectionBlend(ref vert, vector2, vector4, roadSet, blendFactor, dist2);
			}
			if (Physics.Raycast(vert + 1500f * Vector3.up, Vector3.down, out var hitInfo, 3000f, 1))
			{
				vert = hitInfo.point;
				if (result.status == Statuses.Good)
				{
					if (snappedStart >= 0 && snappedStart == snappedEnd)
					{
						result.status = Statuses.Invalid_Connection;
					}
					else if (i > 2 && Vector3.Angle(positions[i - 1] - positions[i - 2], vert - positions[i - 1]) > 30f)
					{
						result.status = Statuses.Too_Sharp;
					}
					else if (i == positions.Length - 1 && positions.Length >= 3 && Vector3.Angle(positions[i - 1] - positions[i - 2], vector2 - positions[i - 1]) > 30f)
					{
						result.status = Statuses.Too_Sharp;
					}
					else if (i == 2 && Vector3.Angle(vert - positions[i - 1], positions[i - 1] - vector) > 30f)
					{
						result.status = Statuses.Too_Sharp;
					}
					else if (snappedEnd > 0 && i == positions.Length - 2 && positions.Length >= 3 && Vector3.Angle(Flat(vector4), Flat(vert - vector2)) > 30f)
					{
						result.status = Statuses.Too_Sharp;
						Debug.DrawLine(vector2, vector2 + vector4 * 100f, Color.red);
						Debug.DrawLine(vector2, vector2 + (vert - vector2).normalized * 100f, Color.cyan);
					}
					else if (snappedStart > 0 && i == 1 && Vector3.Angle(Flat(vert - vector), Flat(vector3)) > 30f)
					{
						result.status = Statuses.Too_Sharp;
					}
					else if (Vector3.Dot(hitInfo.normal, Vector3.up) < 0.95105f)
					{
						result.status = Statuses.Too_Steep;
					}
					else if ((bool)WaterPhysics.instance && vert.y < WaterPhysics.instance.height)
					{
						result.status = Statuses.Invalid;
					}
				}
				positions[i] = vert;
				bool flag = (i <= 1 && snappedStart >= 0) || (i >= positions.Length - 2 && snappedEnd >= 0);
				if (result.status == Statuses.Good && i > 0 && !flag)
				{
					Vector3 a = positions[i - 1];
					Vector3 b = vert;
					if (CheckForInvalidIntersection(a, b, roadSet.radius * 2f, list))
					{
						result.status = Statuses.Intersection;
					}
				}
				continue;
			}
			result.status = Statuses.Invalid;
			return result;
		}
		if (result.status == Statuses.Too_Steep)
		{
			Vector3 vector8 = positions[0];
			Vector3 vector9 = positions[positions.Length - 1];
			Vector3 normalized = (vector9 - vector8).normalized;
			vector8 += normalized * roadSet.radius;
			vector9 -= normalized * roadSet.radius;
			if (Vector3.Dot(Vector3.Cross(normalized, Vector3.Cross(Vector3.up, normalized)).normalized, Vector3.up) > 0.95105f && !Physics.Linecast(vector8, vector9, 1))
			{
				for (int j = 1; j < positions.Length - 1; j++)
				{
					Vector3 vector10 = positions[j] - positions[0];
					vector10 = Vector3.Project(vector10, normalized);
					positions[j] = positions[0] + vector10;
				}
				result.status = Statuses.Good;
				result.bridge = true;
			}
			Debug.DrawLine(vector8, vector9, Color.red);
		}
		return result;
	}

	private Vector3 Flat(Vector3 v)
	{
		v.y = 0f;
		return v;
	}

	private void SplitSegmentAtVert(RoadSegment segment, int vertIdx)
	{
		RoadSegment roadSegment = new RoadSegment(nextID++, this, segment.bridge);
		roadSegment.roadSet = segment.roadSet;
		allRoadObjects.Add(roadSegment.id, roadSegment);
		for (int i = vertIdx; i < segment.roadVerts.Count; i++)
		{
			roadSegment.roadVerts.Add(segment.roadVerts[i]);
		}
		segment.roadVerts.RemoveRange(vertIdx + 1, segment.roadVerts.Count - vertIdx - 1);
		RoadCluster roadCluster = (roadSegment.cluster = GetOrCreateCluster(roadSegment.startVertex.globalPoint));
		roadSegment.cluster.segments.Add(roadSegment);
		roadSegment.previousSegment = segment;
		segment.nextSegment = roadSegment;
		roadSegment.endIntersection = segment.endIntersection;
		if (roadSegment.endIntersection != null)
		{
			ReplaceSegmentInIntersection(roadSegment.endIntersection, segment, roadSegment);
		}
		segment.endIntersection = null;
	}

	private void ApplyIntersectionBlend(ref Vector3 vert, Vector3 snapPos, Vector3 tangent, RoadSet roadSet, float blendFactor, float dist)
	{
		float t = 1f - Mathf.Clamp01(dist / blendFactor);
		tangent.Normalize();
		Vector3 b = snapPos + tangent * roadSet.threeWay.intersectionRadius + tangent * Mathf.Max(dist, 0f);
		vert = Vector3.Lerp(vert, b, t);
	}

	public RoadSegment GetSegmentAtPosition(Vector3 worldPos, float radius)
	{
		VTMapManager.WorldToGlobalPoint(worldPos);
		RoadSegment result = null;
		float num = radius * radius;
		IntVector2 intVector = GlobalPosToGrid(VTMapManager.WorldToGlobalPoint(worldPos));
		for (int i = intVector.x - 1; i <= intVector.x + 1; i++)
		{
			for (int j = intVector.y - 1; j <= intVector.y + 1; j++)
			{
				IntVector2 key = new IntVector2(i, j);
				if (!roadClusters.ContainsKey(key))
				{
					continue;
				}
				RoadCluster roadCluster = roadClusters[key];
				for (int k = 0; k < roadCluster.segments.Count; k++)
				{
					RoadSegment roadSegment = roadCluster.segments[k];
					for (int l = 0; l < roadSegment.roadVerts.Count; l++)
					{
						float sqrMagnitude = (roadSegment.roadVerts[l].worldPos - worldPos).sqrMagnitude;
						if (sqrMagnitude < num)
						{
							num = sqrMagnitude;
							result = roadSegment;
						}
					}
				}
			}
		}
		return result;
	}

	public void DeleteSegment(RoadSegment seg)
	{
		RoadCluster cluster = seg.cluster;
		cluster.segments.Remove(seg);
		allRoadObjects.Remove(seg.id);
		cluster.isDirty = true;
		if (seg.nextSegment != null)
		{
			if (seg.nextSegment.previousSegment == seg)
			{
				seg.nextSegment.startIntersection = CreateEndPiece(seg.nextSegment.roadSet, seg.nextSegment, GetOrCreateCluster(seg.nextSegment.startVertex.globalPoint));
				seg.nextSegment.previousSegment = null;
			}
			else if (seg.nextSegment.nextSegment == seg)
			{
				seg.nextSegment.endIntersection = CreateEndPiece(seg.nextSegment.roadSet, seg.nextSegment, GetOrCreateCluster(seg.nextSegment.startVertex.globalPoint));
				seg.nextSegment.nextSegment = null;
			}
		}
		if (seg.previousSegment != null)
		{
			if (seg.previousSegment.previousSegment == seg)
			{
				seg.previousSegment.startIntersection = CreateEndPiece(seg.previousSegment.roadSet, seg.previousSegment, GetOrCreateCluster(seg.previousSegment.startVertex.globalPoint));
				seg.previousSegment.previousSegment = null;
			}
			else if (seg.previousSegment.nextSegment == seg)
			{
				seg.previousSegment.endIntersection = CreateEndPiece(seg.previousSegment.roadSet, seg.previousSegment, GetOrCreateCluster(seg.previousSegment.startVertex.globalPoint));
				seg.previousSegment.nextSegment = null;
			}
		}
		if (seg.endIntersection != null)
		{
			if (seg.endIntersection.attachedRoads.Count == 1)
			{
				seg.endIntersection.cluster.intersections.Remove(seg.endIntersection);
				allRoadObjects.Remove(seg.endIntersection.id);
			}
			else if (seg.endIntersection.attachedRoads.Count == 3)
			{
				RemoveSegmentFromThreeWay(seg, seg.endIntersection);
			}
			else if (seg.endIntersection.attachedRoads.Count == 4)
			{
				RemoveSegmentFromFourWay(seg, seg.endIntersection);
			}
		}
		if (seg.startIntersection != null)
		{
			if (seg.startIntersection.attachedRoads.Count == 1)
			{
				seg.startIntersection.cluster.intersections.Remove(seg.startIntersection);
				allRoadObjects.Remove(seg.startIntersection.id);
			}
			else if (seg.startIntersection.attachedRoads.Count == 3)
			{
				RemoveSegmentFromThreeWay(seg, seg.startIntersection);
			}
			else if (seg.startIntersection.attachedRoads.Count == 4)
			{
				RemoveSegmentFromFourWay(seg, seg.startIntersection);
			}
		}
		if (this.OnDeletedSegment != null)
		{
			this.OnDeletedSegment(seg);
		}
		RebuildDirtyModels();
	}

	private void ReplaceSegmentInIntersection(RoadIntersection intersection, RoadSegment oldSeg, RoadSegment newSeg)
	{
		intersection.attachedRoads[intersection.attachedRoads.IndexOf(oldSeg)] = newSeg;
		intersection.cluster.isDirty = true;
		intersection.cluster.intersections.Remove(intersection);
		intersection.cluster = intersection.attachedRoads[0].cluster;
		intersection.cluster.intersections.Add(intersection);
		intersection.cluster.isDirty = true;
	}

	private void RemoveSegmentFromThreeWay(RoadSegment segment, RoadIntersection intersection)
	{
		intersection.attachedRoads.IndexOf(segment);
		intersection.attachedRoads.Remove(segment);
		intersection.cluster.intersections.Remove(intersection);
		RoadSegment roadSegment = intersection.attachedRoads[0];
		RoadSegment roadSegment2 = intersection.attachedRoads[1];
		allRoadObjects.Remove(intersection.id);
		if (roadSegment.endIntersection == intersection)
		{
			if (roadSegment2.endIntersection == intersection)
			{
				roadSegment.endIntersection = roadSegment2.startIntersection;
				roadSegment.nextSegment = roadSegment2.previousSegment;
				if (roadSegment2.previousSegment != null)
				{
					if (roadSegment2.previousSegment.nextSegment == roadSegment2)
					{
						roadSegment2.previousSegment.nextSegment = roadSegment;
					}
					else if (roadSegment2.previousSegment.previousSegment == roadSegment2)
					{
						roadSegment2.previousSegment.previousSegment = roadSegment;
					}
				}
				for (int num = roadSegment2.roadVerts.Count - 1; num >= 0; num--)
				{
					roadSegment2.roadVerts[num].tangent *= -1f;
					roadSegment.roadVerts.Add(roadSegment2.roadVerts[num]);
				}
			}
			else
			{
				roadSegment.endIntersection = roadSegment2.endIntersection;
				roadSegment.nextSegment = roadSegment2.nextSegment;
				if (roadSegment2.nextSegment != null)
				{
					if (roadSegment2.nextSegment.nextSegment == roadSegment2)
					{
						roadSegment2.nextSegment.nextSegment = roadSegment;
					}
					else if (roadSegment2.nextSegment.previousSegment == roadSegment2)
					{
						roadSegment2.nextSegment.previousSegment = roadSegment;
					}
				}
				for (int i = 0; i < roadSegment2.roadVerts.Count; i++)
				{
					roadSegment.roadVerts.Add(roadSegment2.roadVerts[i]);
				}
			}
			if (roadSegment.endIntersection != null)
			{
				ReplaceSegmentInIntersection(roadSegment.endIntersection, roadSegment2, roadSegment);
			}
		}
		else
		{
			if (roadSegment2.endIntersection == intersection)
			{
				roadSegment.startIntersection = roadSegment2.startIntersection;
				roadSegment.previousSegment = roadSegment2.previousSegment;
				if (roadSegment2.previousSegment != null)
				{
					if (roadSegment2.previousSegment.nextSegment == roadSegment2)
					{
						roadSegment2.previousSegment.nextSegment = roadSegment;
					}
					else if (roadSegment2.previousSegment.previousSegment == roadSegment2)
					{
						roadSegment2.previousSegment.previousSegment = roadSegment;
					}
				}
				for (int num2 = roadSegment2.roadVerts.Count - 1; num2 >= 0; num2--)
				{
					roadSegment.roadVerts.Insert(0, roadSegment2.roadVerts[num2]);
				}
			}
			else
			{
				roadSegment.startIntersection = roadSegment2.endIntersection;
				roadSegment.previousSegment = roadSegment2.nextSegment;
				if (roadSegment2.nextSegment != null)
				{
					if (roadSegment2.nextSegment.nextSegment == roadSegment2)
					{
						roadSegment2.nextSegment.nextSegment = roadSegment;
					}
					else if (roadSegment2.nextSegment.previousSegment == roadSegment2)
					{
						roadSegment2.nextSegment.previousSegment = roadSegment;
					}
				}
				for (int j = 0; j < roadSegment2.roadVerts.Count; j++)
				{
					roadSegment2.roadVerts[j].tangent *= -1f;
					roadSegment.roadVerts.Insert(0, roadSegment2.roadVerts[j]);
				}
			}
			roadSegment.cluster.segments.Remove(roadSegment);
			roadSegment.cluster = roadSegment2.cluster;
			roadSegment.cluster.segments.Add(roadSegment);
			if (roadSegment.startIntersection != null)
			{
				ReplaceSegmentInIntersection(roadSegment.startIntersection, roadSegment2, roadSegment);
			}
		}
		roadSegment2.cluster.segments.Remove(roadSegment2);
		allRoadObjects.Remove(roadSegment2.id);
		if (this.OnDeletedSegment != null)
		{
			this.OnDeletedSegment(roadSegment2);
		}
		roadSegment.cluster.isDirty = true;
		if (roadSegment.roadVerts.Count > 64 || roadSegment.nextSegment == roadSegment || roadSegment.previousSegment == roadSegment || (roadSegment.endIntersection != null && roadSegment.endIntersection == roadSegment.startIntersection))
		{
			SplitSegmentAtVert(roadSegment, roadSegment.roadVerts.Count / 2);
		}
	}

	private void RemoveSegmentFromFourWay(RoadSegment segment, RoadIntersection intersection)
	{
		int num = intersection.attachedRoads.IndexOf(segment);
		intersection.cluster.intersections.Remove(intersection);
		intersection.attachedRoads.Remove(segment);
		Debug.Log("Reording 3 way after removing idx : " + num);
		switch (num)
		{
		case 0:
		{
			RoadSegment value3 = intersection.attachedRoads[2];
			RoadSegment value4 = intersection.attachedRoads[0];
			RoadSegment value5 = intersection.attachedRoads[1];
			intersection.attachedRoads[0] = value3;
			intersection.attachedRoads[1] = value4;
			intersection.attachedRoads[2] = value5;
			break;
		}
		case 1:
		{
			RoadSegment value2 = intersection.attachedRoads[1];
			RoadSegment roadSegment3 = intersection.attachedRoads[0];
			RoadSegment roadSegment4 = intersection.attachedRoads[2];
			if (roadSegment3.endIntersection == intersection && roadSegment4.endIntersection == intersection)
			{
				roadSegment3 = intersection.attachedRoads[2];
				roadSegment4 = intersection.attachedRoads[0];
			}
			intersection.attachedRoads[0] = value2;
			intersection.attachedRoads[1] = roadSegment3;
			intersection.attachedRoads[2] = roadSegment4;
			break;
		}
		case 2:
		{
			RoadSegment value = intersection.attachedRoads[1];
			RoadSegment roadSegment = intersection.attachedRoads[0];
			RoadSegment roadSegment2 = intersection.attachedRoads[2];
			if (roadSegment.endIntersection == intersection && roadSegment2.endIntersection == intersection)
			{
				roadSegment = intersection.attachedRoads[2];
				roadSegment2 = intersection.attachedRoads[0];
			}
			intersection.attachedRoads[0] = value;
			intersection.attachedRoads[1] = roadSegment;
			intersection.attachedRoads[2] = roadSegment2;
			break;
		}
		default:
			_ = 3;
			break;
		}
		FixThreeWaySegmentOrder(intersection);
		RoadCluster orCreateCluster = GetOrCreateCluster(intersection.attachedRoads[0].startVertex.globalPoint);
		orCreateCluster.intersections.Add(intersection);
		intersection.cluster = orCreateCluster;
		orCreateCluster.isDirty = true;
	}

	private bool CheckForInvalidIntersection(Vector3 a, Vector3 b, float radius, List<int> ignoreSegments = null)
	{
		Vector3 normalized = (b - a).normalized;
		a -= normalized * radius;
		b += normalized * radius;
		IntVector2 intVector = GlobalPosToGrid(VTMapManager.WorldToGlobalPoint(a));
		for (int i = intVector.x - 1; i <= intVector.x + 1; i++)
		{
			for (int j = intVector.y - 1; j <= intVector.y + 1; j++)
			{
				IntVector2 key = new IntVector2(i, j);
				if (!roadClusters.ContainsKey(key))
				{
					continue;
				}
				foreach (RoadSegment segment in roadClusters[key].segments)
				{
					if (ignoreSegments != null && ignoreSegments.Contains(segment.id))
					{
						continue;
					}
					for (int k = 0; k < segment.roadVerts.Count - 1; k++)
					{
						Vector3 worldPos = segment.roadVerts[k].worldPos;
						Vector3 worldPos2 = segment.roadVerts[k + 1].worldPos;
						Vector3 normalized2 = (worldPos2 - worldPos).normalized;
						worldPos -= normalized2 * radius;
						worldPos2 += normalized2 * radius;
						if (SegmentsIntersect(a, b, worldPos, worldPos2))
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	private int SnapToVertex(ref Vector3 pt, float radius)
	{
		radius *= 2f;
		float num = radius * radius;
		IntVector2 intVector = GlobalPosToGrid(VTMapManager.WorldToGlobalPoint(pt));
		for (int i = intVector.x - 1; i <= intVector.x + 1; i++)
		{
			for (int j = intVector.y - 1; j <= intVector.y + 1; j++)
			{
				IntVector2 key = new IntVector2(i, j);
				if (!roadClusters.ContainsKey(key))
				{
					continue;
				}
				RoadCluster roadCluster = roadClusters[key];
				for (int k = 0; k < roadCluster.segments.Count; k++)
				{
					RoadSegment roadSegment = roadCluster.segments[k];
					if (roadSegment.startIntersection != null)
					{
						RoadIntersection startIntersection = roadSegment.startIntersection;
						if ((startIntersection.worldPosition - pt).sqrMagnitude < num)
						{
							if (startIntersection.attachedRoads.Count == 3)
							{
								Vector3 normalized = (startIntersection.worldPosition - startIntersection.GetVertex(0).worldPos).normalized;
								pt = startIntersection.worldPosition + normalized * (radius / 2f);
								return startIntersection.id;
							}
							if (roadSegment.startIntersection.attachedRoads.Count == 1)
							{
								pt = roadSegment.startVertex.worldPos;
								return startIntersection.id;
							}
							continue;
						}
					}
					if (roadSegment.endIntersection != null)
					{
						RoadIntersection endIntersection = roadSegment.endIntersection;
						if ((endIntersection.worldPosition - pt).sqrMagnitude < num)
						{
							if (endIntersection.attachedRoads.Count == 3)
							{
								Vector3 normalized2 = (endIntersection.worldPosition - endIntersection.GetVertex(0).worldPos).normalized;
								pt = endIntersection.worldPosition + normalized2 * (radius / 2f);
								return endIntersection.id;
							}
							if (endIntersection.attachedRoads.Count == 1)
							{
								pt = roadSegment.endVertex.worldPos;
								return endIntersection.id;
							}
							continue;
						}
					}
					for (int l = 1; l < roadSegment.roadVerts.Count - 2; l++)
					{
						Vector3 vector = Vector3.Lerp(roadSegment.roadVerts[l].worldPos, roadSegment.roadVerts[l + 1].worldPos, 0.5f);
						if ((vector - pt).sqrMagnitude < num)
						{
							pt = vector;
							return roadSegment.id;
						}
					}
				}
			}
		}
		return -1;
	}

	public StartPointInfo GetStartPointInfo(RaycastHit hit, RoadSet roadSet)
	{
		StartPointInfo result = default(StartPointInfo);
		result.status = Statuses.Good;
		Vector3 pt = hit.point;
		if (SnapToVertex(ref pt, roadSet.radius) >= 0)
		{
			result.snapped = true;
			result.snapPos = pt;
		}
		if (Vector3.Dot(hit.normal, Vector3.up) < 0.95105f)
		{
			result.status = Statuses.Too_Steep;
		}
		return result;
	}

	private static bool OnSegment(Vector3 p, Vector3 q, Vector3 r)
	{
		if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) && q.z <= Mathf.Max(p.z, r.z) && q.y >= Mathf.Min(p.z, r.z))
		{
			return true;
		}
		return false;
	}

	private static int Orientation(Vector3 p, Vector3 q, Vector3 r)
	{
		float num = (q.z - p.z) * (r.x - q.x) - (q.x - p.x) * (r.z - q.z);
		if (num == 0f)
		{
			return 0;
		}
		if (!(num > 0f))
		{
			return 2;
		}
		return 1;
	}

	public static bool SegmentsIntersect(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
	{
		int num = Orientation(p1, q1, p2);
		int num2 = Orientation(p1, q1, q2);
		int num3 = Orientation(p2, q2, p1);
		int num4 = Orientation(p2, q2, q1);
		if (num != num2 && num3 != num4)
		{
			return true;
		}
		if (num == 0 && OnSegment(p1, p2, q1))
		{
			return true;
		}
		if (num2 == 0 && OnSegment(p1, q2, q1))
		{
			return true;
		}
		if (num3 == 0 && OnSegment(p2, p1, q2))
		{
			return true;
		}
		if (num4 == 0 && OnSegment(p2, q1, q2))
		{
			return true;
		}
		return false;
	}

	private static void GetVertData(Vector3 center, Vector3 tangent, float segLength, float radius, out float height, out Vector3 normal, bool bridge)
	{
		Vector3 vector = SurfacePoint(center, 0f);
		Vector3 vector2 = Quaternion.LookRotation(tangent) * Vector3.right;
		float num = center.y - vector.y;
		normal = Vector3.up;
		if (bridge)
		{
			height = 0f;
			return;
		}
		Vector3 b = center + tangent * segLength;
		for (int i = 0; i <= 10; i++)
		{
			float t = (float)i / 10f;
			Vector3 vector3 = Vector3.Lerp(center, b, t);
			if (Physics.Raycast(vector3 + radius * vector2 + 1500f * Vector3.up, Vector3.down, out var hitInfo, 3000f, 1))
			{
				num = Mathf.Max(num, vector3.y - hitInfo.point.y);
				normal = hitInfo.normal;
			}
			if (Physics.Raycast(vector3 - radius * vector2 + 1500f * Vector3.up, Vector3.down, out hitInfo, 3000f, 1))
			{
				num = Mathf.Max(num, vector3.y - hitInfo.point.y);
				normal += hitInfo.normal;
			}
		}
		normal.Normalize();
		height = num;
	}

	private static Color GetTerrainColor(Vector3 pos)
	{
		return VTMapGenerator.fetch.GetTerrainColor(pos);
	}

	private static Vector3 SurfacePoint(Vector3 pos, float height)
	{
		if (Physics.Raycast(pos + 1500f * Vector3.up, Vector3.down, out var hitInfo, 3000f, 1))
		{
			return hitInfo.point + new Vector3(0f, height, 0f);
		}
		return pos;
	}

	private void ReassignAllClusters()
	{
		roadClusters.Clear();
		foreach (object value in allRoadObjects.Values)
		{
			if (value is RoadSegment)
			{
				RoadSegment roadSegment = (RoadSegment)value;
				RoadCluster orCreateCluster = GetOrCreateCluster(roadSegment.startVertex.globalPoint);
				orCreateCluster.segments.Add(roadSegment);
				roadSegment.cluster = orCreateCluster;
			}
			else if (value is RoadIntersection)
			{
				RoadIntersection roadIntersection = (RoadIntersection)value;
				RoadCluster orCreateCluster2 = GetOrCreateCluster(roadIntersection.attachedRoads[0].startVertex.globalPoint);
				orCreateCluster2.intersections.Add(roadIntersection);
				roadIntersection.cluster = orCreateCluster2;
			}
		}
	}

	public ConfigNode SaveToConfigNode()
	{
		Debug.Log("Saving road system to config.  Reassigning road clusters.");
		ReassignAllClusters();
		ConfigNode configNode = new ConfigNode("RoadSystem");
		foreach (RoadCluster value in roadClusters.Values)
		{
			ConfigNode configNode2 = new ConfigNode("RoadCluster");
			configNode2.SetValue("grid", ConfigNodeUtils.WriteIntVector2(value.grid));
			foreach (RoadSegment segment in value.segments)
			{
				ConfigNode configNode3 = new ConfigNode("Segment");
				configNode3.SetValue("id", segment.id);
				configNode3.SetValue("roadSet", segment.roadSet);
				if (segment.startIntersection != null)
				{
					configNode3.SetValue("startIntersection", segment.startIntersection.id);
				}
				if (segment.endIntersection != null)
				{
					configNode3.SetValue("endIntersection", segment.endIntersection.id);
				}
				if (segment.nextSegment != null)
				{
					configNode3.SetValue("nextSegment", segment.nextSegment.id);
				}
				if (segment.previousSegment != null)
				{
					configNode3.SetValue("previousSegment", segment.previousSegment.id);
				}
				foreach (RoadVertex roadVert in segment.roadVerts)
				{
					configNode3.AddNode(roadVert.WriteToConfigNode());
				}
				configNode2.AddNode(configNode3);
			}
			foreach (RoadIntersection intersection in value.intersections)
			{
				ConfigNode configNode4 = new ConfigNode("Intersection");
				configNode4.SetValue("id", intersection.id);
				configNode4.SetValue("roadSet", intersection.roadSet);
				List<int> list = new List<int>();
				foreach (RoadSegment attachedRoad in intersection.attachedRoads)
				{
					list.Add(attachedRoad.id);
				}
				configNode4.SetValue("attached", ConfigNodeUtils.WriteList(list));
				configNode2.AddNode(configNode4);
			}
			configNode.AddNode(configNode2);
		}
		return configNode;
	}

	private object GetRoadObjectOrNull(int id)
	{
		if (allRoadObjects.ContainsKey(id))
		{
			return allRoadObjects[id];
		}
		return null;
	}

	public void LoadFromConfigNode(ConfigNode roadSystemNode)
	{
		List<LoadSegmentInfo> list = new List<LoadSegmentInfo>();
		List<LoadIntersectionInfo> list2 = new List<LoadIntersectionInfo>();
		foreach (ConfigNode node in roadSystemNode.GetNodes("RoadCluster"))
		{
			RoadCluster roadCluster = new RoadCluster();
			roadCluster.grid = ConfigNodeUtils.ParseIntVector2(node.GetValue("grid"));
			foreach (ConfigNode node2 in node.GetNodes("Segment"))
			{
				int num = ConfigNodeUtils.ParseInt(node2.GetValue("id"));
				RoadSegment roadSegment = new RoadSegment(num, this, bridge: false);
				roadSegment.roadSet = node2.GetValue("roadSet");
				list.Add(new LoadSegmentInfo(roadSegment, node2));
				allRoadObjects.Add(num, roadSegment);
				roadCluster.segments.Add(roadSegment);
				roadSegment.cluster = roadCluster;
				foreach (ConfigNode node3 in node2.GetNodes("Vert"))
				{
					roadSegment.roadVerts.Add(RoadVertex.LoadFromVertexNode(node3));
				}
				nextID = Mathf.Max(nextID, num);
			}
			foreach (ConfigNode node4 in node.GetNodes("Intersection"))
			{
				int num2 = ConfigNodeUtils.ParseInt(node4.GetValue("id"));
				RoadIntersection roadIntersection = new RoadIntersection(num2, this);
				roadIntersection.roadSet = node4.GetValue("roadSet");
				List<int> roads = ConfigNodeUtils.ParseList<int>(node4.GetValue("attached"));
				allRoadObjects.Add(num2, roadIntersection);
				roadIntersection.cluster = roadCluster;
				roadCluster.intersections.Add(roadIntersection);
				list2.Add(new LoadIntersectionInfo(roadIntersection, roads));
				nextID = Mathf.Max(nextID, num2);
			}
			roadClusters.Add(roadCluster.grid, roadCluster);
		}
		foreach (LoadSegmentInfo item in list)
		{
			RoadSegment segment = item.segment;
			if (item.nextSegID >= 0)
			{
				segment.nextSegment = (RoadSegment)GetRoadObjectOrNull(item.nextSegID);
			}
			if (item.prevSegID >= 0)
			{
				segment.previousSegment = (RoadSegment)GetRoadObjectOrNull(item.prevSegID);
			}
			if (item.startIntersectID >= 0)
			{
				segment.startIntersection = (RoadIntersection)GetRoadObjectOrNull(item.startIntersectID);
			}
			if (item.endIntersectID >= 0)
			{
				segment.endIntersection = (RoadIntersection)GetRoadObjectOrNull(item.endIntersectID);
			}
		}
		foreach (LoadIntersectionInfo item2 in list2)
		{
			RoadIntersection intersection = item2.intersection;
			foreach (int attachedRoad in item2.attachedRoads)
			{
				intersection.attachedRoads.Add((RoadSegment)allRoadObjects[attachedRoad]);
			}
		}
		nextID++;
		Debug.Log("Loaded road system from config. Clusters: " + roadClusters.Count);
	}
}
