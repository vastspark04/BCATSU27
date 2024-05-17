using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFollowAlgoTest : MonoBehaviour
{
	public float altitude;

	public float distInterval;

	public float maxAngle;

	public float targetDist;

	public float testTimeInterval = 1f;

	public Transform testStartTf;

	private Vector3 debugPos;

	private List<Vector3> points = new List<Vector3>();

	private Coroutine testR;

	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			if (testR != null)
			{
				StopCoroutine(testR);
			}
			testR = StartCoroutine(TestRoutine2());
		}
	}

	private void OnDrawGizmos()
	{
		for (int i = 0; i < points.Count - 1; i++)
		{
			Gizmos.color = Color.white;
			Gizmos.DrawLine(points[i], points[i + 1]);
			Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
			Gizmos.DrawSphere(debugPos, 1f);
		}
	}

	private IEnumerator TestRoutine()
	{
		WaitForSeconds testWait = new WaitForSeconds(testTimeInterval);
		points.Clear();
		Vector3 currPos = (debugPos = testStartTf.position);
		int currIdx = 0;
		while (Vector3.Distance(currPos, testStartTf.position) < targetDist)
		{
			if (Physics.Raycast(currPos + 500f * Vector3.up, Vector3.down, out var hitInfo, 1000f, 1))
			{
				currPos = (debugPos = hitInfo.point + new Vector3(0f, altitude, 0f));
				points.Add(currPos);
				yield return testWait;
				if (currIdx > 1)
				{
					Vector3 vector = points[currIdx - 1] - points[currIdx - 2];
					Vector3 vector2 = currPos - points[currIdx - 1];
					if (Vector3.Angle(vector, vector2) > maxAngle)
					{
						if (vector2.y < vector.y)
						{
							Vector3 vector3 = vector2;
							vector3.y = 0f;
							new Plane(-vector3, currPos);
							Vector3 current = vector;
							current.y = 0f;
							Vector3 vector4 = Vector3.RotateTowards(current, vector2, maxAngle * ((float)Math.PI / 180f), 0f);
							currPos = points[currIdx - 1] + vector4;
							points[currIdx] = currPos;
							debugPos = currPos;
							yield return testWait;
						}
						else
						{
							for (int i = currIdx; i > 1; i--)
							{
								int aIdx = i;
								int bIdx = i - 1;
								int cIdx = i - 2;
								debugPos = points[bIdx];
								yield return testWait;
								Vector3 from = points[bIdx] - points[aIdx];
								Vector3 to = points[cIdx] - points[bIdx];
								if (Vector3.Angle(from, to) > maxAngle && from.y < to.y)
								{
									Vector3 value = points[bIdx];
									value.y = Mathf.Lerp(points[cIdx].y, points[aIdx].y, 0.7f);
									points[bIdx] = value;
								}
								else
								{
									i = 0;
								}
								yield return testWait;
							}
						}
					}
				}
			}
			else
			{
				currPos.y = altitude;
				debugPos = currPos;
				points.Add(currPos);
				yield return testWait;
			}
			currPos += testStartTf.forward * distInterval;
			currIdx++;
		}
	}

	private IEnumerator TestRoutine2()
	{
		points.Clear();
		Vector3 currPos = testStartTf.position;
		int currIdx = 0;
		while (Vector3.Distance(currPos, testStartTf.position) < targetDist)
		{
			if (Physics.Raycast(currPos + 500f * Vector3.up, Vector3.down, out var hitInfo, 1000f, 1))
			{
				currPos = hitInfo.point + new Vector3(0f, altitude, 0f);
				points.Add(currPos);
				if (currIdx > 1)
				{
					Vector3 vector = points[currIdx - 1] - points[currIdx - 2];
					Vector3 vector2 = currPos - points[currIdx - 1];
					if (Vector3.Angle(vector, vector2) > maxAngle)
					{
						if (vector2.y < vector.y)
						{
							Vector3 vector3 = vector2;
							vector3.y = 0f;
							new Plane(-vector3, currPos);
							Vector3 current = vector;
							current.y = 0f;
							Vector3 vector4 = Vector3.RotateTowards(current, vector2, maxAngle * ((float)Math.PI / 180f), 0f);
							currPos = points[currIdx - 1] + vector4;
							points[currIdx] = currPos;
							yield return null;
						}
						else
						{
							for (int i = currIdx; i > 1; i--)
							{
								int index = i;
								int index2 = i - 1;
								int index3 = i - 2;
								Vector3 from = points[index2] - points[index];
								Vector3 to = points[index3] - points[index2];
								if (Vector3.Angle(from, to) > maxAngle && from.y < 0f)
								{
									Vector3 value = points[index2];
									value.y = Mathf.Lerp(points[index3].y, points[index].y, 0.7f);
									points[index2] = value;
								}
								else
								{
									i = 0;
								}
								yield return null;
							}
						}
					}
				}
			}
			else
			{
				currPos.y = altitude;
				points.Add(currPos);
				yield return null;
			}
			currPos += testStartTf.forward * distInterval;
			currIdx++;
		}
	}
}
