using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class AirPathfindingTest : MonoBehaviour
{
	private class PathfindingHandle
	{
		private object lockObj = new object();

		private bool _done;

		private int _currLength;

		public List<Vector3D> result;

		public bool done
		{
			get
			{
				lock (lockObj)
				{
					return _done;
				}
			}
			set
			{
				lock (lockObj)
				{
					_done = value;
				}
			}
		}

		public int currLength
		{
			get
			{
				lock (lockObj)
				{
					return _currLength;
				}
			}
			set
			{
				lock (lockObj)
				{
					_currLength = value;
				}
			}
		}
	}

	private struct PathfindRequest
	{
		public Vector3D ptA;

		public Vector3D ptB;

		public PathfindingHandle handle;

		public BDTexture heightmap;

		public float gridSize;

		public float chunkSize;

		public float maxHeight;
	}

	public Transform pointA;

	public Transform pointB;

	private int testLength;

	private bool testing;

	private PathfindingHandle currHandle;

	private const float MOVE_STEP = 300f;

	private const float ALT_FLOOR = 100f;

	[ContextMenu("Run Test")]
	public void RunTest()
	{
		if (!testing)
		{
			StartCoroutine(TestRoutine());
		}
	}

	private IEnumerator TestRoutine()
	{
		testing = true;
		Debug.Log("Beginning pathfinding.");
		float t = Time.realtimeSinceStartup;
		PathfindingHandle handle = (currHandle = new PathfindingHandle());
		PathfindRequest pathfindRequest = default(PathfindRequest);
		pathfindRequest.handle = handle;
		pathfindRequest.ptA = VTMapManager.WorldToGlobalPoint(pointA.position);
		pathfindRequest.ptB = VTMapManager.WorldToGlobalPoint(pointB.position);
		pathfindRequest.heightmap = VTCustomMapManager.instance.mapGenerator.hmBdt;
		pathfindRequest.gridSize = VTCustomMapManager.instance.mapGenerator.gridSize;
		pathfindRequest.chunkSize = VTCustomMapManager.instance.mapGenerator.chunkSize;
		pathfindRequest.maxHeight = VTCustomMapManager.instance.mapGenerator.hm_maxHeight;
		PathfindRequest pathfindRequest2 = pathfindRequest;
		ThreadPool.QueueUserWorkItem(Pathfind, pathfindRequest2);
		while (!handle.done)
		{
			testLength = handle.currLength;
			yield return null;
		}
		Debug.Log("Pathfinding took " + (Time.realtimeSinceStartup - t) + " seconds.");
		List<Vector3D> result = handle.result;
		if (result.Count == 0)
		{
			Debug.Log("Pathfinding failed!");
		}
		else
		{
			for (int i = 0; i < result.Count - 1; i++)
			{
				Vector3 vector = VTMapManager.GlobalToWorldPoint(result[i]);
				Vector3 end = VTMapManager.GlobalToWorldPoint(result[i + 1]);
				Debug.DrawLine(vector, end, Color.magenta);
				Debug.DrawLine(vector, vector + Vector3.up, Color.red);
			}
		}
		testing = false;
		currHandle = null;
	}

	private void OnDestroy()
	{
		if (currHandle != null)
		{
			currHandle.done = true;
		}
	}

	[ContextMenu("Stop Test")]
	public void StopTest()
	{
		if (testing && currHandle != null)
		{
			currHandle.done = true;
		}
	}

	private void Pathfind(object r)
	{
		PathfindRequest r2 = (PathfindRequest)r;
		r2.handle.result = new List<Vector3D>();
		try
		{
			if (!PathfindNonRecurs(r2))
			{
				r2.handle.result = new List<Vector3D>(1);
			}
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
		r2.handle.done = true;
	}

	private bool PathfindNonRecurs(PathfindRequest r)
	{
		float num = 10f;
		Vector3D vector3D = r.ptA;
		int num2 = 0;
		List<int> list = new List<int>();
		list.Add(0);
		r.handle.result.Add(vector3D);
		while (!r.handle.done)
		{
			int num3 = num2;
			num2++;
			r.handle.result.AddOrSet(vector3D, num3);
			if ((vector3D - r.ptB).sqrMagnitude < 1.0)
			{
				r.handle.currLength = num2;
				r.handle.result.AddOrSet(r.ptB, num2 - 1);
				return true;
			}
			Vector3 vector = ((num3 <= 0) ? (r.ptB - r.ptA).normalized.toVector3 : (r.handle.result[num3] - r.handle.result[num3 - 1]).normalized.toVector3);
			float num4 = 300f;
			if ((r.ptB - vector3D).sqrMagnitude < 90000.0)
			{
				num4 = (float)(r.ptB - vector3D).magnitude;
			}
			bool flag = false;
			if (list[num3] == 0)
			{
				Vector3 toVector = (r.ptB - vector3D).normalized.toVector3;
				Vector3 normalized = Vector3.RotateTowards(vector, toVector, num * ((float)Math.PI / 180f), 0f).normalized;
				Vector3D vector3D2 = vector3D + new Vector3D(normalized * num4);
				float height = GetHeight(vector3D2, r);
				if (vector3D2.y < (double)height)
				{
					num2--;
				}
				else
				{
					vector3D = vector3D2;
				}
			}
			else if (list[num3] == 1)
			{
				Vector3D vector3D3 = vector3D + new Vector3D(vector * num4);
				float height2 = GetHeight(vector3D3, r);
				if (vector3D3.y < (double)height2)
				{
					num2--;
				}
				else
				{
					vector3D = vector3D3;
				}
			}
			else if (list[num3] == 2)
			{
				Vector3 vector2 = Quaternion.AngleAxis(num, Vector3.Cross(Vector3.down, vector)) * vector;
				float num5 = Vector3.Angle(vector2, Vector3.ProjectOnPlane(vector2, Vector3.up));
				Vector3D vector3D4 = vector3D + new Vector3D(vector2 * num4);
				float height3 = GetHeight(vector3D4, r);
				if (vector3D4.y < (double)height3 || num5 > 15f)
				{
					num2--;
				}
				else
				{
					vector3D = vector3D4;
				}
			}
			else if (list[num3] == 3)
			{
				Vector3 vector3 = Quaternion.AngleAxis(num, Vector3.down) * vector;
				Vector3D vector3D5 = vector3D + new Vector3D(vector3 * num4);
				float height4 = GetHeight(vector3D5, r);
				if (vector3D5.y < (double)height4)
				{
					num2--;
				}
				else
				{
					vector3D = vector3D5;
				}
			}
			else if (list[num3] == 4)
			{
				Vector3 vector4 = Quaternion.AngleAxis(num, Vector3.up) * vector;
				Vector3D vector3D6 = vector3D + new Vector3D(vector4 * num4);
				float height5 = GetHeight(vector3D6, r);
				if (vector3D6.y < (double)height5)
				{
					flag = true;
				}
				else
				{
					vector3D = vector3D6;
				}
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				num2 -= 2;
				r.handle.currLength = num2;
				r.handle.result.RemoveAt(num3);
				list.RemoveAt(num3);
				list[num3 - 1]++;
				if (num3 > 0)
				{
					vector3D = r.handle.result[num3 - 1];
					r.handle.result.RemoveAt(num3 - 1);
				}
				if (num3 == 0)
				{
					return false;
				}
			}
			else
			{
				list[num3]++;
				if (num2 > num3)
				{
					list.AddOrSet(0, num2);
					continue;
				}
				vector3D = r.handle.result[num3];
				r.handle.result.RemoveAt(num3);
			}
		}
		return false;
	}

	private bool PathfindRecurs(PathfindRequest r, Vector3D currPos, Vector3 currDir, ref int currIdx)
	{
		if (r.handle.done)
		{
			return false;
		}
		int num = currIdx;
		currIdx++;
		if ((currPos - r.ptB).sqrMagnitude < 1.0)
		{
			r.handle.currLength = currIdx;
			r.handle.result.AddOrSet(r.ptB, currIdx - 1);
			return true;
		}
		r.handle.result.AddOrSet(currPos, currIdx - 1);
		currDir.y = 0f;
		currDir = currDir.normalized;
		Vector3 toVector = (r.ptB - currPos).normalized.toVector3;
		Vector3 normalized = Vector3.RotateTowards(currDir, toVector, 0.17453292f, 0f).normalized;
		float num2 = 300f;
		if ((r.ptB - currPos).sqrMagnitude < 90000.0)
		{
			num2 = (float)(r.ptB - currPos).magnitude;
		}
		Vector3D vector3D = currPos + new Vector3D(normalized * num2);
		float height = GetHeight(vector3D, r);
		if (vector3D.y > (double)height && PathfindRecurs(r, vector3D, normalized, ref currIdx))
		{
			return true;
		}
		vector3D = currPos + new Vector3D(currDir * num2);
		height = GetHeight(vector3D, r);
		if (vector3D.y > (double)height && PathfindRecurs(r, vector3D, currDir, ref currIdx))
		{
			return true;
		}
		normalized = Quaternion.AngleAxis(10f, Vector3.Cross(Vector3.down, currDir)) * currDir;
		vector3D = currPos + new Vector3D(normalized * num2);
		if (vector3D.y > (double)height && PathfindRecurs(r, vector3D, normalized, ref currIdx))
		{
			return true;
		}
		normalized = Quaternion.AngleAxis(10f, Vector3.down) * currDir;
		vector3D = currPos + new Vector3D(normalized * num2);
		height = GetHeight(vector3D, r);
		if (vector3D.y > (double)height && PathfindRecurs(r, vector3D, normalized, ref currIdx))
		{
			return true;
		}
		normalized = Quaternion.AngleAxis(10f, Vector3.up) * currDir;
		vector3D = currPos + new Vector3D(normalized * num2);
		height = GetHeight(vector3D, r);
		if (vector3D.y > (double)height && PathfindRecurs(r, vector3D, normalized, ref currIdx))
		{
			return true;
		}
		r.handle.result.RemoveRange(num, r.handle.result.Count - num);
		currIdx = num;
		r.handle.currLength = currIdx;
		return false;
	}

	private float GetHeight(Vector3D pt, PathfindRequest r)
	{
		float num = 200f;
		float height = GetHeight(pt + num * Vector3.right, r.heightmap, r.gridSize, r.chunkSize, r.maxHeight);
		float height2 = GetHeight(pt + num * Vector3.forward, r.heightmap, r.gridSize, r.chunkSize, r.maxHeight);
		float height3 = GetHeight(pt + num * Vector3.back, r.heightmap, r.gridSize, r.chunkSize, r.maxHeight);
		float height4 = GetHeight(pt + num * Vector3.left, r.heightmap, r.gridSize, r.chunkSize, r.maxHeight);
		float height5 = GetHeight(pt, r.heightmap, r.gridSize, r.chunkSize, r.maxHeight);
		return (height + height2 + height3 + height4 + height5) / 5f;
	}

	private float GetHeight(Vector3D pt, BDTexture heightmap, float gridSize, float chunkSize, float hm_maxHeight)
	{
		Vector2 vector = new Vector2((float)pt.x, (float)pt.z);
		Vector2 worldUV = vector;
		float num = gridSize * chunkSize;
		vector /= num;
		float r = heightmap.GetColorUV(vector.x, vector.y).r;
		float hmHeight = Mathf.Lerp(-80f, hm_maxHeight, r);
		float oobHeight = -80f;
		return Mathf.Max(0f, VTTHeightMap.GetBlendedHeight(hmHeight, oobHeight, gridSize, chunkSize, hm_maxHeight, worldUV)) + 100f;
	}
}
