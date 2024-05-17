using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundPathGenerator : MonoBehaviour
{
	public class GroundPathRequest
	{
		public List<FixedPoint> points = new List<FixedPoint>();

		public bool ready;

		public List<FixedPoint> problemAreas = new List<FixedPoint>();
	}

	private static GroundPathGenerator fetch;

	public const float PROBLEM_AREA_MULT = 1f;

	public static GroundPathRequest GetGroundPath(Vector3 start, Vector3 destination, float maxSlope, float distInterval)
	{
		if (!fetch)
		{
			fetch = new GameObject("GroundPathGenerator").AddComponent<GroundPathGenerator>();
		}
		return fetch._GetGroundPath(start, destination, maxSlope, distInterval);
	}

	private GroundPathRequest _GetGroundPath(Vector3 start, Vector3 destination, float maxSlope, float distInterval)
	{
		GroundPathRequest groundPathRequest = new GroundPathRequest();
		Vector3 worldPosition = GroundPosAtPoint(start);
		new FixedPoint(worldPosition);
		FixedPoint destPos = new FixedPoint(GroundPosAtPoint(destination));
		FixedPoint currPos = new FixedPoint(worldPosition);
		Vector3 dir = destination - start;
		dir.y = 0f;
		dir.Normalize();
		StartCoroutine(GetPathRoutine(currPos, destPos, maxSlope, dir, distInterval, groundPathRequest));
		return groundPathRequest;
	}

	private IEnumerator GetPathRoutine(FixedPoint currPos, FixedPoint destPos, float maxSlope, Vector3 dir, float distInterval, GroundPathRequest request, int depth = 0)
	{
		bool searching = true;
		bool reachedDest = false;
		List<FixedPoint> newPoints = new List<FixedPoint>();
		_ = distInterval;
		_ = distInterval;
		while (searching)
		{
			Vector3 target = destPos.point - currPos.point;
			if (target.sqrMagnitude < distInterval * distInterval)
			{
				searching = false;
				reachedDest = true;
				continue;
			}
			target = Vector3.RotateTowards(dir, target, (float)Math.PI / 4f, 0f);
			target.Normalize();
			Vector3 vector = GroundPosAtPoint(currPos.point + target * distInterval, out var normal);
			if (!SlopeIsOkay(normal, maxSlope) || IsProblemArea(vector, request, distInterval))
			{
				vector = GroundPosAtPoint(currPos.point + dir.normalized * distInterval, out normal);
				if (!SlopeIsOkay(normal, maxSlope) || IsProblemArea(vector, request, distInterval))
				{
					float num = ((depth == 0) ? 179f : 89f);
					for (float num2 = 15f; num2 < num; num2 += 15f)
					{
						for (int i = -1; i <= 1; i += 2)
						{
							Vector3 vector2 = Quaternion.AngleAxis(num2 * (float)i, Vector3.up) * dir;
							Vector3 normal2;
							Vector3 vector3 = GroundPosAtPoint(currPos.point + vector2.normalized * distInterval, out normal2);
							if (!SlopeIsOkay(normal2, maxSlope))
							{
								Debug.DrawLine(currPos.point, vector3, Color.red);
								request.problemAreas.Add(new FixedPoint(vector3));
							}
							else if (!IsProblemArea(vector3, request, distInterval) && request.ready)
							{
								request.points.InsertRange(0, newPoints);
								if (depth > 0)
								{
									Debug.Log("broke at depth: " + depth);
									yield break;
								}
								i = 2;
								num2 = num + 1f;
							}
						}
					}
					request.problemAreas.Add(new FixedPoint(vector));
					Debug.DrawLine(currPos.point, currPos.point + 150f * Vector3.up, Color.cyan);
					yield break;
				}
				Debug.DrawLine(currPos.point, currPos.point + 100f * Vector3.up, Color.green);
				currPos.point = vector;
				newPoints.Add(new FixedPoint(vector));
				yield return null;
				if (request.ready)
				{
					request.points.InsertRange(0, newPoints);
					searching = false;
				}
				if (depth > 0)
				{
					Debug.Log("broke at depth: " + depth);
					yield break;
				}
			}
			else
			{
				Debug.DrawLine(currPos.point, currPos.point + 100f * Vector3.up, Color.blue);
				currPos.point = vector;
				dir = target;
				newPoints.Add(new FixedPoint(vector));
				yield return null;
			}
		}
		if (reachedDest)
		{
			request.points.AddRange(newPoints);
			request.ready = true;
		}
		if (!request.ready || depth != 0)
		{
			yield break;
		}
		Debug.Log("Simplifying");
		int curr = 0;
		while (curr < request.points.Count - 2)
		{
			FixedPoint next = request.points[curr + 1];
			Vector3 pos = request.points[curr].point;
			Debug.DrawLine(pos, pos + 100f * Vector3.up, Color.cyan);
			bool keep = false;
			while (!keep)
			{
				if ((GroundPosAtPoint(pos, out var normal3) - next.point).sqrMagnitude < 0.25f * distInterval * distInterval)
				{
					request.points.RemoveAt(curr + 1);
					keep = true;
				}
				if (!SlopeIsOkay(normal3, maxSlope))
				{
					keep = true;
					curr++;
				}
				else
				{
					pos = Vector3.MoveTowards(pos, next.point, distInterval);
					Debug.DrawLine(pos, pos + 100f * Vector3.up, Color.cyan);
					yield return null;
				}
			}
			yield return null;
		}
	}

	private bool IsProblemArea(Vector3 pt, GroundPathRequest request, float distInterval)
	{
		for (int i = 0; i < request.problemAreas.Count; i++)
		{
			if ((request.problemAreas[i].point - pt).sqrMagnitude < 1f * distInterval * distInterval)
			{
				return true;
			}
		}
		return false;
	}

	private bool SlopeIsOkay(Vector3 normal, float maxSlope)
	{
		return Vector3.Angle(normal, Vector3.up) < maxSlope;
	}

	private Vector3 GroundPosAtPoint(Vector3 pt)
	{
		float num = 2000f;
		float y = num / 2f;
		if (Physics.Raycast(pt + new Vector3(0f, y, 0f), Vector3.down, out var hitInfo, num, 1))
		{
			return hitInfo.point;
		}
		return pt;
	}

	private Vector3 GroundPosAtPoint(Vector3 pt, out Vector3 normal)
	{
		float num = 2000f;
		float y = num / 2f;
		if (Physics.Raycast(pt + new Vector3(0f, y, 0f), Vector3.down, out var hitInfo, num, 1))
		{
			normal = hitInfo.normal;
			return hitInfo.point;
		}
		normal = Vector3.up;
		return pt;
	}
}
