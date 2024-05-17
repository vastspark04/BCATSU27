using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiSurfaceCruiseGuidance : MissileGuidanceUnit
{
	public float terminalDist = 1000f;

	public float minAltitude = 50f;

	public float maxDescendAngle = 20f;

	private LinearPathD path;

	private float terminalT = 1f;

	private FixedPoint guidedPoint;

	public float followPtLeadDistance = 500f;

	public bool terminalOpticalGuidance = true;

	public float terminalOpticalViewFOV = 6f;

	public PID altitudeControlPID = new PID(12f, 0f, -16f, -1f, 0f);

	private GPSTargetGroup qs_targetGroup;

	private GPSTarget qs_singleTarget;

	private bool finishedStartRun;

	private Actor tgtActor;

	protected override void OnBeginGuidance()
	{
		base.OnBeginGuidance();
		StartCoroutine(GuidanceRoutine());
	}

	public override Vector3 GetGuidedPoint()
	{
		return guidedPoint.point;
	}

	public void SetTarget(GPSTarget tgt)
	{
		path = null;
		qs_singleTarget = tgt;
		Vector3 position = base.transform.position;
		position.y = Mathf.Max(position.y, tgt.worldPosition.y + minAltitude);
		Vector3 worldPosition = tgt.worldPosition;
		worldPosition.y = base.transform.position.y;
		Vector3 worldPoint = Vector3.Lerp(position, worldPosition, 0.5f);
		Vector3D vector3D = VTMapManager.WorldToGlobalPoint(position);
		Vector3D vector3D2 = VTMapManager.WorldToGlobalPoint(worldPoint);
		Vector3D vector3D3 = VTMapManager.WorldToGlobalPoint(tgt.worldPosition);
		LinearPathD tempPath = new LinearPathD(new Vector3D[3] { vector3D, vector3D2, vector3D3 });
		StartCoroutine(GeneratePathRoutine(tempPath));
	}

	public void SetTarget(GPSTargetGroup grp)
	{
		if (!grp.isPath || grp.currentTargetIdx == grp.targets.Count - 1)
		{
			SetTarget(grp.targets[grp.currentTargetIdx]);
			return;
		}
		qs_targetGroup = grp;
		path = null;
		Vector3D[] array = new Vector3D[grp.targets.Count - grp.currentTargetIdx];
		int num = grp.currentTargetIdx;
		int num2 = 0;
		while (num < grp.targets.Count)
		{
			array[num2] = VTMapManager.WorldToGlobalPoint(grp.targets[num].worldPosition);
			num++;
			num2++;
		}
		LinearPathD tempPath = new LinearPathD(array);
		StartCoroutine(GeneratePathRoutine(tempPath));
	}

	private void CalculateTerminalT()
	{
		terminalT = 1f - terminalDist / path.length;
	}

	private IEnumerator GeneratePathRoutine(LinearPathD tempPath)
	{
		float maxAngle = 10f;
		List<Vector3D> points = new List<Vector3D>();
		float t = 0f;
		float tInterval = 75f / tempPath.length;
		int maxCastsPerFrame = 3;
		int casts = 0;
		int currIdx = 0;
		while (t < 1f)
		{
			Vector3 worldPoint = GetWorldPoint(tempPath, t);
			Vector3 surfacePoint = GetSurfacePoint(worldPoint);
			if (worldPoint.y - surfacePoint.y < minAltitude)
			{
				worldPoint.y = surfacePoint.y + minAltitude;
			}
			Vector3D vector3D = VTMapManager.WorldToGlobalPoint(worldPoint);
			points.Add(vector3D);
			if (currIdx > 1)
			{
				Vector3 toVector = (points[currIdx - 1] - points[currIdx - 2]).toVector3;
				Vector3 toVector2 = (vector3D - points[currIdx - 1]).toVector3;
				if (Vector3.Angle(toVector, toVector2) > maxAngle)
				{
					if (toVector2.y < toVector.y)
					{
						Vector3 vector = toVector2;
						vector.y = 0f;
						Vector3 current = toVector;
						current.y = 0f;
						Vector3 vector2 = Vector3.RotateTowards(current, toVector2, maxAngle * ((float)Math.PI / 180f), 0f);
						vector3D = (points[currIdx] = points[currIdx - 1] + vector2);
					}
					else
					{
						for (int num = currIdx; num > 1; num--)
						{
							int index = num;
							int index2 = num - 1;
							int index3 = num - 2;
							Vector3 toVector3 = (points[index2] - points[index]).toVector3;
							Vector3 toVector4 = (points[index3] - points[index2]).toVector3;
							if (Vector3.Angle(toVector3, toVector4) > maxAngle && toVector3.y < 0f)
							{
								Vector3D value = points[index2];
								value.y = Vector3D.LerpD(points[index3].y, points[index].y, 0.699999988079071);
								points[index2] = value;
							}
							else
							{
								num = 0;
							}
						}
					}
				}
			}
			t += tInterval;
			currIdx++;
			casts++;
			if (casts >= maxCastsPerFrame)
			{
				casts = 0;
				yield return null;
			}
		}
		points[points.Count - 1] = VTMapManager.WorldToGlobalPoint(GetSurfacePoint(VTMapManager.GlobalToWorldPoint(tempPath.GetPoint(1f))));
		path = new LinearPathD(points.ToArray());
		CalculateTerminalT();
	}

	private Vector3 ApplyAltitudeMaintenance(Vector3 tgtPos)
	{
		Vector3 current = tgtPos - base.transform.position;
		current.y = 0f;
		float altitude = WaterPhysics.GetAltitude(base.transform.position);
		Vector3 vector = base.transform.position + current.normalized * 1000f;
		float target = Mathf.Max(minAltitude, WaterPhysics.GetAltitude(tgtPos));
		Vector3 target2 = vector + altitudeControlPID.Evaluate(altitude, target) * Vector3.up - base.transform.position;
		target2 = Vector3.RotateTowards(current, target2, maxDescendAngle * ((float)Math.PI / 180f), 0f);
		return base.transform.position + target2;
	}

	private IEnumerator GuidanceRoutine()
	{
		Vector3 origFwd = base.transform.forward;
		Debug.Log("Beginning ASSM guidance.  Waiting for path to be calculated.");
		while (path == null)
		{
			guidedPoint = new FixedPoint(base.transform.position + origFwd * 100f);
			guidedPoint.point = ApplyAltitudeMaintenance(guidedPoint.point);
			yield return null;
		}
		Debug.Log("ASSM Path calculated.  Begining route guidance.");
		if (!finishedStartRun)
		{
			FixedPoint startPt = new FixedPoint(VTMapManager.GlobalToWorldPoint(path.GetPoint(0f)));
			while ((base.transform.position - startPt.point).sqrMagnitude > 2000f && Vector3.Dot(base.transform.forward, startPt.point - base.transform.position) > 0f)
			{
				guidedPoint = startPt;
				yield return null;
			}
			finishedStartRun = true;
		}
		while (base.missile.hasTarget)
		{
			float currT;
			Vector3 vector = GetFollowPoint(base.transform.position, followPtLeadDistance, out currT);
			if (currT > terminalT)
			{
				if (tgtActor == null)
				{
					if (terminalOpticalGuidance)
					{
						tgtActor = TargetManager.instance.GetOpticalTargetFromView(base.missile.actor, terminalDist, 22, 20f, base.transform.position, vector - base.transform.position, terminalOpticalViewFOV);
						if ((bool)tgtActor)
						{
							Debug.Log("ASSM Acquired target in terminal phase: " + tgtActor.actorName);
						}
					}
					vector = Missile.BallisticPoint(VTMapManager.GlobalToWorldPoint(path.GetPoint(1f)), base.transform.position, base.missile.rb.velocity.magnitude);
				}
				else
				{
					vector = Missile.BallisticPoint(Missile.GetLeadPoint(tgtActor.position, tgtActor.velocity, base.transform.position, base.missile.rb.velocity), base.transform.position, base.missile.rb.velocity.magnitude);
				}
			}
			Vector3 vector2 = vector - base.transform.position;
			if (Vector3.Dot(vector2, base.transform.forward) < 0f)
			{
				Vector3 forward = base.transform.forward;
				forward.y = 0f;
				Vector3 target = vector2;
				target.y = 0f;
				vector2 = Vector3.RotateTowards(forward, target, (float)Math.PI / 4f, 1f).normalized * 100f;
				vector = base.transform.position + vector2;
			}
			guidedPoint = new FixedPoint(vector);
			yield return null;
		}
	}

	private Vector3 GetSurfacePoint(Vector3 worldPt)
	{
		if ((bool)VTMapGenerator.fetch)
		{
			return VTMapGenerator.fetch.GetSurfacePos(worldPt);
		}
		worldPt.y = WaterPhysics.instance.height;
		if (Physics.Raycast(worldPt + new Vector3(0f, 10000f, 0f), Vector3.down, out var hitInfo, 10000f, 1))
		{
			return hitInfo.point;
		}
		return worldPt;
	}

	private Vector3 GetWorldPoint(LinearPathD path, float t)
	{
		return VTMapManager.GlobalToWorldPoint(path.GetPoint(t));
	}

	private Vector3 GetFollowPoint(Vector3 worldPos, float leadDistance, out float currT)
	{
		Vector3D position = VTMapManager.WorldToGlobalPoint(worldPos);
		float num = (currT = path.GetClosestTime(position, 4));
		float num2 = leadDistance / path.length;
		return VTMapManager.GlobalToWorldPoint(path.GetPoint(num + num2));
	}

	public override void SaveToQuicksaveNode(ConfigNode qsNode)
	{
		base.SaveToQuicksaveNode(qsNode);
		ConfigNode configNode = qsNode.AddNode("AntiSurfaceCruiseGuidance");
		if (!base.guidanceEnabled || path == null)
		{
			if (qs_singleTarget != null)
			{
				configNode.SetValue("qs_singleTargetPoint", new FixedPoint(qs_singleTarget.worldPosition));
			}
			else if (qs_targetGroup != null)
			{
				configNode.AddNode(qs_targetGroup.SaveToConfigNode("qs_targetGroup"));
			}
		}
		else
		{
			configNode.AddNode(path.SaveToConfigNode("path"));
			configNode.SetValue("terminalT", terminalT);
		}
	}

	public override void LoadFromQuicksaveNode(Missile m, ConfigNode qsNode)
	{
		base.LoadFromQuicksaveNode(m, qsNode);
		string text = "AntiSurfaceCruiseGuidance";
		if (!qsNode.HasNode(text))
		{
			return;
		}
		ConfigNode node = qsNode.GetNode(text);
		if (!base.guidanceEnabled || !node.HasNode("path"))
		{
			if (node.HasValue("qs_singleTargetPoint"))
			{
				GPSTarget target = new GPSTarget(node.GetValue<FixedPoint>("qs_singleTargetPoint").point, "QST", 0);
				SetTarget(target);
			}
			else if (node.HasNode("qs_targetGroup"))
			{
				GPSTargetGroup target2 = GPSTargetGroup.LoadFromConfigNode(node.GetNode("qs_targetGroup"));
				SetTarget(target2);
			}
		}
		else
		{
			path = LinearPathD.LoadFromConfigNode(node.GetNode("path"));
			terminalT = node.GetValue<float>("terminalT");
		}
		if (base.guidanceEnabled)
		{
			StartCoroutine(GuidanceRoutine());
		}
	}
}
