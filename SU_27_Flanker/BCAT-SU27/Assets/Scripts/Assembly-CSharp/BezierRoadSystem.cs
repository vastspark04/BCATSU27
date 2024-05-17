using System.Collections.Generic;
using UnityEngine;

public class BezierRoadSystem
{
	public struct RoadSnapInfo
	{
		public bool snapped;

		public Vector3 worldSnapPoint;

		public BezierRoadSegment snappedSegmentSplit;

		public BezierRoadSegment snappedSegmentEnd;

		public BezierRoadIntersection snappedIntersection;

		public BezierRoadSegment[] snappedSegmentsInsert;

		public Vector3 snapTangent;

		public override string ToString()
		{
			if (snapped)
			{
				return string.Format("Snap info: snappedSegmentSplit({0}), snappedSegmentEnd({1}), snappedIntersection({2}), snappedSegmentsInsert({3})", (snappedSegmentSplit != null) ? snappedSegmentSplit.id.ToString() : "none", (snappedSegmentEnd != null) ? snappedSegmentEnd.id.ToString() : "none", (snappedIntersection != null) ? snappedIntersection.id.ToString() : "none", (snappedSegmentsInsert != null) ? $"{snappedSegmentsInsert[0].id}, {snappedSegmentsInsert[1].id}" : "none");
			}
			return "No snap info.";
		}
	}

	private struct SegmentMatchup
	{
		public int segmentID;

		public int prevSegment;

		public int nextSegment;

		public int startIntersection;

		public int endIntersection;
	}

	public class BezierRoadChunk
	{
		public IntVector2 gridPos;

		public List<BezierRoadSegment> segments = new List<BezierRoadSegment>();
	}

	public class BezierRoadSegment
	{
		public int id;

		public int type;

		public bool bridge;

		public float length;

		private int si_id = -1;

		private int ei_id = -1;

		private int ns_id = -1;

		private int ps_id = -1;

		public BezierCurveD curve;

		public BezierRoadChunk chunk;

		public BezierRoadSystem roadSystem { get; private set; }

		public BezierRoadIntersection startIntersection
		{
			get
			{
				return roadSystem.GetIntersection(si_id);
			}
			set
			{
				if (value == null)
				{
					si_id = -1;
				}
				else
				{
					si_id = value.id;
				}
			}
		}

		public BezierRoadIntersection endIntersection
		{
			get
			{
				return roadSystem.GetIntersection(ei_id);
			}
			set
			{
				if (value == null)
				{
					ei_id = -1;
				}
				else
				{
					ei_id = value.id;
				}
			}
		}

		public BezierRoadSegment nextSegment
		{
			get
			{
				return roadSystem.GetSegment(ns_id);
			}
			set
			{
				if (value == null)
				{
					ns_id = -1;
				}
				else
				{
					ns_id = value.id;
				}
			}
		}

		public BezierRoadSegment prevSegment
		{
			get
			{
				return roadSystem.GetSegment(ps_id);
			}
			set
			{
				if (value == null)
				{
					ps_id = -1;
				}
				else
				{
					ps_id = value.id;
				}
			}
		}

		public void UpdateLength()
		{
			length = (float)curve.EstimateLength(10);
		}

		public BezierRoadSegment(BezierRoadSystem system, int id, BezierCurveD curve)
		{
			roadSystem = system;
			this.id = id;
			this.curve = curve;
		}

		public Vector3 GetWorldPoint(float t)
		{
			return VTMapManager.GlobalToWorldPoint(curve.GetPoint(t));
		}

		public BezierRoadSegment SplitAtGlobalPoint(Vector3D globalPos, float gapDist, int postSegID)
		{
			float closestTime = curve.GetClosestTime(globalPos, 6);
			BezierCurveD[] array = curve.SplitCurve(closestTime, gapDist);
			curve = array[0];
			UpdateLength();
			BezierRoadSegment bezierRoadSegment = new BezierRoadSegment(roadSystem, postSegID, array[1]);
			roadSystem.allSegments.Add(bezierRoadSegment.id, bezierRoadSegment);
			bezierRoadSegment.UpdateLength();
			bezierRoadSegment.type = type;
			bezierRoadSegment.bridge = bridge;
			if (nextSegment != null)
			{
				bezierRoadSegment.nextSegment = nextSegment;
				if (nextSegment.prevSegment == this)
				{
					nextSegment.prevSegment = bezierRoadSegment;
				}
				else if (nextSegment.nextSegment == this)
				{
					nextSegment.nextSegment = bezierRoadSegment;
				}
			}
			if (endIntersection != null)
			{
				bezierRoadSegment.endIntersection = endIntersection;
				endIntersection.attachedSegments.Remove(this);
				endIntersection.attachedSegments.Add(bezierRoadSegment);
				endIntersection.SortSegmentsClockwise();
			}
			nextSegment = null;
			endIntersection = null;
			return bezierRoadSegment;
		}
	}

	public class BezierRoadIntersection
	{
		private struct SegmentAngleInfo
		{
			public float angle;

			public BezierRoadSegment segment;
		}

		public int id;

		public List<BezierRoadSegment> attachedSegments = new List<BezierRoadSegment>();

		public void SortSegmentsClockwise()
		{
			List<SegmentAngleInfo> list = new List<SegmentAngleInfo>();
			int count = attachedSegments.Count;
			for (int i = 0; i < count; i++)
			{
				BezierRoadSegment segment = attachedSegments[i];
				Vector3 segmentRadialDir = GetSegmentRadialDir(i);
				segmentRadialDir.y = 0f;
				float angle = VectorUtils.SignedAngle(Vector3.forward, segmentRadialDir, Vector3.right);
				list.Add(new SegmentAngleInfo
				{
					angle = angle,
					segment = segment
				});
				Vector3 vector = VTMapManager.GlobalToWorldPoint(GetIntersectionPoint());
				Debug.DrawLine(vector, vector + segmentRadialDir * 10f, Color.Lerp(Color.red, Color.green, (float)i / (float)(count - 1)));
			}
			list.Sort(SegmentAngleComparison);
			attachedSegments.Clear();
			for (int j = 0; j < count; j++)
			{
				attachedSegments.Add(list[j].segment);
			}
		}

		private int SegmentAngleComparison(SegmentAngleInfo a, SegmentAngleInfo b)
		{
			return a.angle.CompareTo(b.angle);
		}

		public Vector3 GetSegmentRadialDir(int idx)
		{
			BezierRoadSegment bezierRoadSegment = attachedSegments[idx];
			if (bezierRoadSegment.startIntersection == this)
			{
				return bezierRoadSegment.curve.GetTangent(0f);
			}
			return -bezierRoadSegment.curve.GetTangent(1f);
		}

		public BezierRoadIntersection(int id)
		{
			this.id = id;
		}

		public BezierRoadIntersection(int id, params BezierRoadSegment[] segments)
		{
			this.id = id;
			for (int i = 0; i < segments.Length; i++)
			{
				attachedSegments.Add(segments[i]);
			}
		}

		public Vector3D GetIntersectionPoint()
		{
			Vector3D zero = Vector3D.zero;
			foreach (BezierRoadSegment attachedSegment in attachedSegments)
			{
				Vector3D vector3D = ((attachedSegment.endIntersection != this) ? attachedSegment.curve.GetPoint(0f) : attachedSegment.curve.GetPoint(1f));
				zero += vector3D;
			}
			return zero / attachedSegments.Count;
		}
	}

	private const float SEGMENT_LENGTH = 300f;

	private const float MAX_BRIDGE_SEGMENT_LENGTH = 2500f;

	public Dictionary<IntVector2, BezierRoadChunk> roadChunks = new Dictionary<IntVector2, BezierRoadChunk>();

	public List<IntVector2> dirtyChunks = new List<IntVector2>();

	public Dictionary<int, BezierRoadSegment> allSegments = new Dictionary<int, BezierRoadSegment>();

	public Dictionary<int, BezierRoadIntersection> allIntersections = new Dictionary<int, BezierRoadIntersection>();

	private int nextSegmentID;

	private int nextIntersectionID;

	private const double ROAD_SNAP_DIST = 20.0;

	private const float ROAD_END_SNAP_THRESH = 0.15f;

	private const float ROAD_SNAP_INTERVALS = 10f;

	private const string NODE_NAME = "BezierRoads";

	private const string CHUNK_NODE_NAME = "Chunk";

	private const string SEGMENT_NODE_NAME = "Segment";

	private const string INTERSECTION_NODE_NAME = "Intersection";

	public RoadSnapInfo AddNewRoad(BezierCurveD inCurve, RoadSnapInfo startSnapInfo, RoadSnapInfo endSnapInfo, int roadType)
	{
		return AddNewRoad(inCurve, startSnapInfo.snappedSegmentEnd, endSnapInfo.snappedSegmentEnd, startSnapInfo.snappedSegmentSplit, endSnapInfo.snappedSegmentSplit, startSnapInfo.snappedIntersection, endSnapInfo.snappedIntersection, startSnapInfo.snappedSegmentsInsert, endSnapInfo.snappedSegmentsInsert, roadType);
	}

	public RoadSnapInfo AddNewRoad(BezierCurveD inCurve, BezierRoadSegment fromSegment, BezierRoadSegment toSegment, BezierRoadSegment fromSegmentSplit, BezierRoadSegment toSegmentSplit, BezierRoadIntersection fromIntersection, BezierRoadIntersection toIntersection, BezierRoadSegment[] insertBtwnSegsStart, BezierRoadSegment[] insertBtwnSegsEnd, int roadType)
	{
		bool flag = CheckIsCompleteBridge(inCurve);
		float num = (float)inCurve.EstimateLength(20);
		int subdivisions = Mathf.Max(1, Mathf.RoundToInt(num / (flag ? 2500f : 300f)));
		BezierCurveD[] array = inCurve.Subdivide(subdivisions);
		BezierRoadSegment[] array2 = new BezierRoadSegment[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			BezierRoadSegment bezierRoadSegment = (array2[i] = new BezierRoadSegment(this, nextSegmentID++, array[i]));
			bezierRoadSegment.type = roadType;
			bezierRoadSegment.UpdateLength();
			allSegments.Add(bezierRoadSegment.id, bezierRoadSegment);
			if (i > 0)
			{
				BezierRoadSegment bezierRoadSegment3 = (bezierRoadSegment.prevSegment = array2[i - 1]);
				bezierRoadSegment3.nextSegment = bezierRoadSegment;
			}
			bezierRoadSegment.bridge = CheckIsBridge(bezierRoadSegment);
			AssignSegmentToChunk(bezierRoadSegment);
		}
		BezierRoadSegment bezierRoadSegment4 = array2[0];
		BezierRoadSegment bezierRoadSegment5 = array2[array2.Length - 1];
		AttachToSegmentEnd(bezierRoadSegment4, fromSegment, 0f);
		AttachToSegmentEnd(bezierRoadSegment5, toSegment, 1f);
		AttachToSegmentSplit(bezierRoadSegment4, fromSegmentSplit, 0f, 0f);
		BezierRoadIntersection bezierRoadIntersection = AttachToSegmentSplit(bezierRoadSegment5, toSegmentSplit, 1f, 0f);
		InsertSegment(bezierRoadSegment4, insertBtwnSegsStart, end: false);
		BezierRoadIntersection bezierRoadIntersection2 = InsertSegment(bezierRoadSegment5, insertBtwnSegsEnd, end: true);
		if (fromIntersection != null)
		{
			bezierRoadSegment4.startIntersection = fromIntersection;
			fromIntersection.attachedSegments.Add(bezierRoadSegment4);
			fromIntersection.SortSegmentsClockwise();
			foreach (BezierRoadSegment attachedSegment in fromIntersection.attachedSegments)
			{
				MarkChunkDirty(attachedSegment.chunk);
			}
		}
		if (toIntersection != null)
		{
			bezierRoadSegment5.endIntersection = toIntersection;
			toIntersection.attachedSegments.Add(bezierRoadSegment5);
			toIntersection.SortSegmentsClockwise();
			foreach (BezierRoadSegment attachedSegment2 in toIntersection.attachedSegments)
			{
				MarkChunkDirty(attachedSegment2.chunk);
			}
		}
		RoadSnapInfo result = default(RoadSnapInfo);
		result.snapped = true;
		result.worldSnapPoint = bezierRoadSegment5.GetWorldPoint(1f);
		result.snapTangent = bezierRoadSegment5.curve.GetTangent(1f);
		if (bezierRoadIntersection != null)
		{
			result.snappedIntersection = bezierRoadIntersection;
		}
		else if (bezierRoadIntersection2 != null)
		{
			result.snappedIntersection = bezierRoadIntersection;
		}
		else if (toSegment != null)
		{
			result.snappedSegmentsInsert = new BezierRoadSegment[2] { bezierRoadSegment5, toSegment };
		}
		else
		{
			result.snappedSegmentEnd = bezierRoadSegment5;
		}
		return result;
	}

	public bool CheckIsBridge(BezierCurveD curve)
	{
		int num = 5;
		for (int i = 0; i <= num; i++)
		{
			float t = (float)i / (float)num;
			Vector3 vector = VTMapManager.GlobalToWorldPoint(curve.GetPoint(t));
			if (Physics.Raycast(vector + 2000f * Vector3.up, Vector3.down, out var hitInfo, 4000f, 1) && vector.y > hitInfo.point.y + 5f)
			{
				return true;
			}
		}
		return false;
	}

	public bool CheckIsCompleteBridge(BezierCurveD curve)
	{
		int num = 10;
		for (int i = 0; i <= num; i++)
		{
			float t = (float)i / (float)num;
			Vector3 vector = VTMapManager.GlobalToWorldPoint(curve.GetPoint(t));
			if (!Physics.Raycast(vector + 2000f * Vector3.up, Vector3.down, out var hitInfo, 4000f, 1))
			{
				continue;
			}
			if (vector.y > hitInfo.point.y + 5f)
			{
				if (i == 0 || i == num)
				{
					return false;
				}
			}
			else if (i > 0 && i < num)
			{
				return false;
			}
		}
		return true;
	}

	private bool CheckIsBridge(BezierRoadSegment seg)
	{
		return CheckIsBridge(seg.curve);
	}

	private BezierRoadIntersection InsertSegment(BezierRoadSegment newSegment, BezierRoadSegment[] insertSegments, bool end)
	{
		if (insertSegments == null)
		{
			return null;
		}
		BezierRoadSegment bezierRoadSegment = insertSegments[0];
		BezierRoadSegment bezierRoadSegment2 = insertSegments[1];
		BezierRoadIntersection bezierRoadIntersection = new BezierRoadIntersection(nextIntersectionID++, bezierRoadSegment, bezierRoadSegment2, newSegment);
		allIntersections.Add(bezierRoadIntersection.id, bezierRoadIntersection);
		if (end)
		{
			newSegment.endIntersection = bezierRoadIntersection;
		}
		else
		{
			newSegment.startIntersection = bezierRoadIntersection;
		}
		if (bezierRoadSegment.nextSegment == bezierRoadSegment2)
		{
			bezierRoadSegment.nextSegment = null;
			bezierRoadSegment.endIntersection = bezierRoadIntersection;
		}
		else
		{
			bezierRoadSegment.prevSegment = null;
			bezierRoadSegment.startIntersection = bezierRoadIntersection;
		}
		if (bezierRoadSegment2.nextSegment == bezierRoadSegment)
		{
			bezierRoadSegment2.nextSegment = null;
			bezierRoadSegment2.endIntersection = bezierRoadIntersection;
		}
		else
		{
			bezierRoadSegment2.prevSegment = null;
			bezierRoadSegment2.startIntersection = bezierRoadIntersection;
		}
		bezierRoadIntersection.SortSegmentsClockwise();
		foreach (BezierRoadSegment attachedSegment in bezierRoadIntersection.attachedSegments)
		{
			MarkChunkDirty(attachedSegment.chunk);
		}
		return bezierRoadIntersection;
	}

	private BezierRoadIntersection AttachToSegmentSplit(BezierRoadSegment newSegment, BezierRoadSegment segmentSplit, float t, float gapDist)
	{
		if (segmentSplit != null)
		{
			Vector3D point = newSegment.curve.GetPoint(t);
			BezierRoadSegment bezierRoadSegment = segmentSplit.SplitAtGlobalPoint(point, gapDist, nextSegmentID++);
			AssignSegmentToChunk(bezierRoadSegment);
			if (bezierRoadSegment.endIntersection != null)
			{
				foreach (BezierRoadSegment attachedSegment in bezierRoadSegment.endIntersection.attachedSegments)
				{
					MarkChunkDirty(attachedSegment.chunk);
				}
			}
			BezierRoadIntersection bezierRoadIntersection = new BezierRoadIntersection(nextIntersectionID++, segmentSplit, bezierRoadSegment, newSegment);
			allIntersections.Add(bezierRoadIntersection.id, bezierRoadIntersection);
			segmentSplit.endIntersection = bezierRoadIntersection;
			bezierRoadSegment.startIntersection = bezierRoadIntersection;
			if (t < 0.5f)
			{
				newSegment.startIntersection = bezierRoadIntersection;
			}
			else
			{
				newSegment.endIntersection = bezierRoadIntersection;
			}
			bezierRoadIntersection.SortSegmentsClockwise();
			{
				foreach (BezierRoadSegment attachedSegment2 in bezierRoadIntersection.attachedSegments)
				{
					MarkChunkDirty(attachedSegment2.chunk);
				}
				return bezierRoadIntersection;
			}
		}
		return null;
	}

	private void AttachToSegmentEnd(BezierRoadSegment newSegment, BezierRoadSegment existingSegment, float t)
	{
		if (existingSegment != null)
		{
			if (t > 0.5f)
			{
				newSegment.nextSegment = existingSegment;
			}
			else
			{
				newSegment.prevSegment = existingSegment;
			}
			if (existingSegment.curve.GetClosestTime(newSegment.curve.GetPoint(t), 4) > 0.5f)
			{
				existingSegment.nextSegment = newSegment;
			}
			else
			{
				existingSegment.prevSegment = newSegment;
			}
			MarkChunkDirty(existingSegment.chunk);
		}
	}

	private void MarkChunkDirty(BezierRoadChunk chunk)
	{
		if (!dirtyChunks.Contains(chunk.gridPos))
		{
			dirtyChunks.Add(chunk.gridPos);
		}
	}

	private void AssignSegmentToChunk(BezierRoadSegment seg)
	{
		IntVector2 intVector = VTMapGenerator.fetch.ChunkGridAtPos(seg.GetWorldPoint(0f));
		if (seg.chunk != null)
		{
			seg.chunk.segments.Remove(seg);
			MarkChunkDirty(seg.chunk);
			seg.chunk = null;
		}
		if (roadChunks.TryGetValue(intVector, out var value))
		{
			value.segments.Add(seg);
			seg.chunk = value;
			MarkChunkDirty(value);
			return;
		}
		BezierRoadChunk bezierRoadChunk = new BezierRoadChunk();
		bezierRoadChunk.gridPos = intVector;
		bezierRoadChunk.segments.Add(seg);
		seg.chunk = bezierRoadChunk;
		roadChunks.Add(intVector, bezierRoadChunk);
		MarkChunkDirty(bezierRoadChunk);
	}

	public void DeleteSegment(BezierRoadSegment segment)
	{
		if (segment.endIntersection != null)
		{
			RemoveSegmentFromIntersection(segment, segment.endIntersection);
		}
		else if (segment.nextSegment != null)
		{
			if (segment.nextSegment.nextSegment == segment)
			{
				segment.nextSegment.nextSegment = null;
			}
			else
			{
				segment.nextSegment.prevSegment = null;
			}
			MarkChunkDirty(segment.nextSegment.chunk);
		}
		if (segment.startIntersection != null)
		{
			RemoveSegmentFromIntersection(segment, segment.startIntersection);
		}
		else if (segment.prevSegment != null)
		{
			if (segment.prevSegment.nextSegment == segment)
			{
				segment.prevSegment.nextSegment = null;
			}
			else
			{
				segment.prevSegment.prevSegment = null;
			}
			MarkChunkDirty(segment.prevSegment.chunk);
		}
		MarkChunkDirty(segment.chunk);
		allSegments.Remove(segment.id);
		segment.chunk.segments.Remove(segment);
		if (segment.chunk.segments.Count == 0)
		{
			roadChunks.Remove(segment.chunk.gridPos);
		}
	}

	private void RemoveSegmentFromIntersection(BezierRoadSegment segment, BezierRoadIntersection intersection)
	{
		intersection.attachedSegments.RemoveAll((BezierRoadSegment x) => x.id == segment.id);
		if (intersection.attachedSegments.Count == 2)
		{
			BezierRoadSegment bezierRoadSegment = intersection.attachedSegments[0];
			BezierRoadSegment bezierRoadSegment2 = intersection.attachedSegments[1];
			if (bezierRoadSegment.endIntersection == intersection)
			{
				bezierRoadSegment.endIntersection = null;
				bezierRoadSegment.nextSegment = bezierRoadSegment2;
			}
			else
			{
				bezierRoadSegment.startIntersection = null;
				bezierRoadSegment.prevSegment = bezierRoadSegment2;
			}
			if (bezierRoadSegment2.endIntersection == intersection)
			{
				bezierRoadSegment2.endIntersection = null;
				bezierRoadSegment2.nextSegment = bezierRoadSegment;
			}
			else
			{
				bezierRoadSegment2.startIntersection = null;
				bezierRoadSegment2.prevSegment = bezierRoadSegment;
			}
			allIntersections.Remove(intersection.id);
			MarkChunkDirty(bezierRoadSegment.chunk);
			MarkChunkDirty(bezierRoadSegment2.chunk);
			return;
		}
		intersection.SortSegmentsClockwise();
		foreach (BezierRoadSegment attachedSegment in intersection.attachedSegments)
		{
			MarkChunkDirty(attachedSegment.chunk);
		}
	}

	public BezierRoadSegment GetClosestSegment(Vector3 worldCursorPoint)
	{
		IntVector2 intVector = VTMapGenerator.fetch.ChunkGridAtPos(worldCursorPoint);
		Vector3D vector3D = VTMapManager.WorldToGlobalPoint(worldCursorPoint);
		BezierRoadSegment result = null;
		double num = 400.0;
		for (int i = intVector.x - 1; i <= intVector.x + 1; i++)
		{
			for (int j = intVector.y - 1; j <= intVector.y + 1; j++)
			{
				if (!roadChunks.TryGetValue(new IntVector2(i, j), out var value))
				{
					continue;
				}
				for (int k = 0; k < value.segments.Count; k++)
				{
					BezierRoadSegment bezierRoadSegment = value.segments[k];
					vector3D.y = bezierRoadSegment.curve.midPt.y;
					float closestTime = bezierRoadSegment.curve.GetClosestTime(vector3D, 4);
					Vector3D point = bezierRoadSegment.curve.GetPoint(closestTime);
					vector3D.y = point.y;
					double sqrMagnitude = (point - vector3D).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						result = bezierRoadSegment;
					}
				}
			}
		}
		return result;
	}

	public BezierRoadSegment GetClosestSegment(Vector3 worldCursorPoint, Ray ray)
	{
		IntVector2 intVector = VTMapGenerator.fetch.ChunkGridAtPos(worldCursorPoint);
		BezierRoadSegment result = null;
		double num = 400.0;
		for (int i = intVector.x - 1; i <= intVector.x + 1; i++)
		{
			for (int j = intVector.y - 1; j <= intVector.y + 1; j++)
			{
				if (!roadChunks.TryGetValue(new IntVector2(i, j), out var value))
				{
					continue;
				}
				for (int k = 0; k < value.segments.Count; k++)
				{
					BezierRoadSegment bezierRoadSegment = value.segments[k];
					for (int l = 0; l < 10; l++)
					{
						float t = (float)l / 9f;
						Vector3D point = bezierRoadSegment.curve.GetPoint(t);
						new Plane(-ray.direction, VTMapManager.GlobalToWorldPoint(point)).Raycast(ray, out var enter);
						Vector3D vector3D = VTMapManager.WorldToGlobalPoint(ray.GetPoint(enter));
						double sqrMagnitude = (point - vector3D).sqrMagnitude;
						if (sqrMagnitude < num)
						{
							num = sqrMagnitude;
							result = bezierRoadSegment;
						}
					}
				}
			}
		}
		return result;
	}

	public RoadSnapInfo SnapRoadPoint(Vector3 worldCursorPoint, Ray ray)
	{
		IntVector2 intVector = VTMapGenerator.fetch.ChunkGridAtPos(worldCursorPoint);
		BezierRoadSegment bezierRoadSegment = null;
		BezierRoadIntersection bezierRoadIntersection = null;
		BezierRoadSegment bezierRoadSegment2 = null;
		double num = 400.0;
		float num2 = -1f;
		for (int i = intVector.x - 1; i <= intVector.x + 1; i++)
		{
			for (int j = intVector.y - 1; j <= intVector.y + 1; j++)
			{
				if (!roadChunks.TryGetValue(new IntVector2(i, j), out var value))
				{
					continue;
				}
				for (int k = 0; k < value.segments.Count; k++)
				{
					BezierRoadSegment bezierRoadSegment3 = value.segments[k];
					for (int l = 0; (float)l < 10f; l++)
					{
						float num3 = (float)l / 9f;
						Vector3D point = bezierRoadSegment3.curve.GetPoint(num3);
						new Plane(-ray.direction, VTMapManager.GlobalToWorldPoint(point)).Raycast(ray, out var enter);
						Vector3D vector3D = VTMapManager.WorldToGlobalPoint(ray.GetPoint(enter));
						double sqrMagnitude = (point - vector3D).sqrMagnitude;
						if (!(sqrMagnitude < num))
						{
							continue;
						}
						bezierRoadIntersection = null;
						bezierRoadSegment2 = null;
						if (num3 > 0.85f)
						{
							if (bezierRoadSegment3.endIntersection != null)
							{
								bezierRoadIntersection = bezierRoadSegment3.endIntersection;
							}
							else if (bezierRoadSegment3.nextSegment != null)
							{
								bezierRoadSegment2 = bezierRoadSegment3.nextSegment;
							}
							num3 = 1f;
						}
						else if (num3 < 0.15f)
						{
							if (bezierRoadSegment3.startIntersection != null)
							{
								bezierRoadIntersection = bezierRoadSegment3.startIntersection;
							}
							else if (bezierRoadSegment3.prevSegment != null)
							{
								bezierRoadSegment2 = bezierRoadSegment3.prevSegment;
							}
							num3 = 0f;
						}
						num = sqrMagnitude;
						num2 = num3;
						bezierRoadSegment = bezierRoadSegment3;
					}
				}
			}
		}
		RoadSnapInfo result = default(RoadSnapInfo);
		result.snapped = false;
		if (bezierRoadSegment != null)
		{
			result.snapped = true;
			if (bezierRoadIntersection != null)
			{
				result.snappedIntersection = bezierRoadIntersection;
			}
			else if (bezierRoadSegment2 != null)
			{
				result.snappedSegmentsInsert = new BezierRoadSegment[2] { bezierRoadSegment, bezierRoadSegment2 };
				Vector3 tangent;
				Vector3 b;
				if (bezierRoadSegment.nextSegment == bezierRoadSegment2)
				{
					tangent = bezierRoadSegment.curve.GetTangent(1f);
					b = ((bezierRoadSegment2.prevSegment != bezierRoadSegment) ? (-bezierRoadSegment2.curve.GetTangent(1f)) : bezierRoadSegment2.curve.GetTangent(0f));
				}
				else
				{
					tangent = bezierRoadSegment.curve.GetTangent(0f);
					b = ((bezierRoadSegment2.nextSegment != bezierRoadSegment) ? (-bezierRoadSegment2.curve.GetTangent(0f)) : bezierRoadSegment2.curve.GetTangent(1f));
				}
				result.snapTangent = Vector3.Lerp(tangent, b, 0.5f).normalized;
			}
			else
			{
				if (num2 > 0.999f || num2 < 0.001f)
				{
					result.snappedSegmentEnd = bezierRoadSegment;
				}
				else
				{
					result.snappedSegmentSplit = bezierRoadSegment;
				}
				result.snapTangent = bezierRoadSegment.curve.GetTangent(num2);
			}
			result.worldSnapPoint = bezierRoadSegment.GetWorldPoint(num2);
		}
		return result;
	}

	public void SaveToConfigNode(ConfigNode saveNode)
	{
		ConfigNode configNode = new ConfigNode("BezierRoads");
		saveNode.AddNode(configNode);
		foreach (BezierRoadChunk value in roadChunks.Values)
		{
			ConfigNode configNode2 = new ConfigNode("Chunk");
			configNode.AddNode(configNode2);
			configNode2.SetValue("grid", value.gridPos);
			foreach (BezierRoadSegment segment in value.segments)
			{
				ConfigNode configNode3 = new ConfigNode("Segment");
				configNode2.AddNode(configNode3);
				configNode3.SetValue("id", segment.id);
				configNode3.SetValue("type", segment.type);
				configNode3.SetValue("bridge", segment.bridge);
				configNode3.SetValue("length", segment.length);
				configNode3.SetValue("s", segment.curve.startPt);
				configNode3.SetValue("m", segment.curve.midPt);
				configNode3.SetValue("e", segment.curve.endPt);
				if (segment.startIntersection != null)
				{
					configNode3.SetValue("si", segment.startIntersection.id);
				}
				if (segment.endIntersection != null)
				{
					configNode3.SetValue("ei", segment.endIntersection.id);
				}
				if (segment.prevSegment != null)
				{
					configNode3.SetValue("ps", segment.prevSegment.id);
				}
				if (segment.nextSegment != null)
				{
					configNode3.SetValue("ns", segment.nextSegment.id);
				}
			}
		}
		foreach (BezierRoadIntersection value2 in allIntersections.Values)
		{
			ConfigNode configNode4 = new ConfigNode("Intersection");
			configNode.AddNode(configNode4);
			configNode4.SetValue("id", value2.id);
			List<int> list = new List<int>();
			foreach (BezierRoadSegment attachedSegment in value2.attachedSegments)
			{
				list.Add(attachedSegment.id);
			}
			configNode4.SetValue("segments", ConfigNodeUtils.WriteList(list));
		}
	}

	public void LoadFromConfigNode(ConfigNode saveNode, int mapSize)
	{
		if (!saveNode.HasNode("BezierRoads"))
		{
			return;
		}
		ConfigNode node = saveNode.GetNode("BezierRoads");
		List<SegmentMatchup> list = new List<SegmentMatchup>();
		foreach (ConfigNode node2 in node.GetNodes("Chunk"))
		{
			IntVector2 value = node2.GetValue<IntVector2>("grid");
			if (value.x < 0 || value.x >= mapSize || value.y < 0 || value.y >= mapSize)
			{
				continue;
			}
			BezierRoadChunk bezierRoadChunk = new BezierRoadChunk();
			bezierRoadChunk.gridPos = value;
			roadChunks.Add(bezierRoadChunk.gridPos, bezierRoadChunk);
			foreach (ConfigNode node3 in node2.GetNodes("Segment"))
			{
				int value2 = node3.GetValue<int>("id");
				int value3 = node3.GetValue<int>("type");
				bool value4 = node3.GetValue<bool>("bridge");
				float value5 = node3.GetValue<float>("length");
				Vector3D value6 = node3.GetValue<Vector3D>("s");
				Vector3D value7 = node3.GetValue<Vector3D>("m");
				Vector3D value8 = node3.GetValue<Vector3D>("e");
				BezierCurveD curve = new BezierCurveD(value6, value7, value8);
				BezierRoadSegment bezierRoadSegment = new BezierRoadSegment(this, value2, curve)
				{
					type = value3,
					bridge = value4,
					length = value5
				};
				allSegments.Add(value2, bezierRoadSegment);
				bezierRoadSegment.chunk = bezierRoadChunk;
				bezierRoadChunk.segments.Add(bezierRoadSegment);
				int target = -1;
				int target2 = -1;
				int target3 = -1;
				int target4 = -1;
				ConfigNodeUtils.TryParseValue(node3, "ps", ref target);
				ConfigNodeUtils.TryParseValue(node3, "ns", ref target2);
				ConfigNodeUtils.TryParseValue(node3, "si", ref target3);
				ConfigNodeUtils.TryParseValue(node3, "ei", ref target4);
				list.Add(new SegmentMatchup
				{
					segmentID = value2,
					prevSegment = target,
					nextSegment = target2,
					startIntersection = target3,
					endIntersection = target4
				});
				nextSegmentID = Mathf.Max(nextSegmentID, value2 + 1);
			}
			MarkChunkDirty(bezierRoadChunk);
		}
		foreach (ConfigNode node4 in node.GetNodes("Intersection"))
		{
			int value9 = node4.GetValue<int>("id");
			BezierRoadIntersection value10 = new BezierRoadIntersection(value9);
			allIntersections.Add(value9, value10);
			nextIntersectionID = Mathf.Max(nextIntersectionID, value9 + 1);
		}
		foreach (SegmentMatchup item in list)
		{
			BezierRoadSegment bezierRoadSegment2 = allSegments[item.segmentID];
			if (item.prevSegment >= 0 && allSegments.TryGetValue(item.prevSegment, out var value11))
			{
				bezierRoadSegment2.prevSegment = value11;
			}
			if (item.nextSegment >= 0 && allSegments.TryGetValue(item.nextSegment, out value11))
			{
				bezierRoadSegment2.nextSegment = value11;
			}
			if (item.startIntersection >= 0 && allIntersections.TryGetValue(item.startIntersection, out var value12))
			{
				bezierRoadSegment2.startIntersection = value12;
				value12.attachedSegments.Add(bezierRoadSegment2);
			}
			if (item.endIntersection >= 0 && allIntersections.TryGetValue(item.endIntersection, out value12))
			{
				bezierRoadSegment2.endIntersection = value12;
				value12.attachedSegments.Add(bezierRoadSegment2);
			}
		}
		foreach (BezierRoadIntersection value13 in allIntersections.Values)
		{
			value13.SortSegmentsClockwise();
		}
	}

	public BezierRoadIntersection GetIntersection(int id)
	{
		if (id < 0)
		{
			return null;
		}
		if (allIntersections.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public BezierRoadSegment GetSegment(int id)
	{
		if (id < 0)
		{
			return null;
		}
		if (allSegments.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}
}
