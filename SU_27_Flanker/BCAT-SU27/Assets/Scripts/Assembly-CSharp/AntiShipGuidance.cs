using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiShipGuidance : MissileGuidanceUnit
{
	public enum ASMTerminalBehaviors
	{
		Direct,
		SeaSkim,
		SSEvasive,
		Popup
	}

	public Radar searchRadar;

	public LockingRadar lockingRadar;

	public ASMTerminalBehaviors terminalBehavior;

	public float estimatedRange = 70000f;

	[Header("Flight Behavior")]
	public float maxClimbAngle;

	public float maxDescendAngle;

	public float waypointSteerMult = -1f;

	private float originalSteerMult;

	[Header("Target Search")]
	[Tooltip("We will consider targets which are within this radius from the final waypoint")]
	public float maxSearchRadius = 2500f;

	[Tooltip("If we found a bunch of targets near the GPS but not close enough for a certain decision, we pick the closest one when we fly this distance away from it")]
	public float decisionDistance = 3000f;

	[Tooltip("If we find a target within this distance to the final waypoint, we select it immediately")]
	public float targetCertaintyRadius = 100f;

	[Header("Sea Skimming")]
	public float finalRadius;

	public float approachRadius;

	public float approachAltitude = 25f;

	[Header("Popup")]
	public float popupRadius = 1200f;

	public float popupMagnitude = 1f;

	[Header("Evasive")]
	public float evasiveRadius;

	public float evasiveRate;

	public float evasiveMagnitude;

	[Header("Components")]
	public MissileDetector rwr;

	public ModuleRWR mRWR;

	private float noiseRand;

	public PID altitudeControlPID;

	[HideInInspector]
	private bool allowSearch = true;

	private bool foundTarget;

	private float totalDistance;

	private bool distTesting;

	private bool deployClearanceComplete;

	private bool waypointRoutineComplete;

	private GPSTargetGroup qs_targetGroup;

	private GPSTarget qs_singleTarget;

	private LinearPathD path;

	private float terminalT = 1f;

	private float overWaterT = 0.9f;

	private FixedPoint guidedPoint;

	private bool finishedStartRun;

	private Vector3 origFwd;

	private bool isSearching;

	private Actor targetActor;

	private bool foundIncomingMissile;

	public float terrainHeight;

	private void Awake()
	{
		noiseRand = UnityEngine.Random.Range(0f, 100f);
		altitudeControlPID.updateMode = UpdateModes.Dynamic;
		searchRadar.radarEnabled = false;
		searchRadar.detectAircraft = false;
		searchRadar.detectShips = true;
	}

	protected override void OnBeginGuidance()
	{
		base.OnBeginGuidance();
		base.missile.explosionType = ExplosionManager.ExplosionTypes.Aerial;
		originalSteerMult = base.missile.steerMult;
		if (waypointSteerMult < 0f)
		{
			waypointSteerMult = base.missile.steerMult;
		}
		if ((bool)searchRadar)
		{
			searchRadar.myActor = base.missile.actor;
			if (base.missile.actor.team == Teams.Enemy)
			{
				searchRadar.teamsToDetect = Radar.DetectionTeams.Allied;
			}
			else if (base.missile.actor.team == Teams.Allied)
			{
				searchRadar.teamsToDetect = Radar.DetectionTeams.Enemy;
			}
		}
		StartCoroutine(ASMProgramRoutine());
		StartCoroutine(DistanceTestRoutine());
	}

	private void OnDisable()
	{
		if (distTesting)
		{
			Debug.Log("ASM total distance: " + totalDistance + "m");
		}
	}

	private IEnumerator DistanceTestRoutine()
	{
		distTesting = true;
		while (!base.missile.rb)
		{
			yield return null;
		}
		while (base.enabled)
		{
			yield return new WaitForFixedUpdate();
			totalDistance += base.missile.rb.velocity.magnitude * Time.fixedDeltaTime;
		}
	}

	private IEnumerator ASMProgramRoutine()
	{
		if (!deployClearanceComplete)
		{
			yield return StartCoroutine(DeployClearanceRoutine());
			deployClearanceComplete = true;
		}
		if (!waypointRoutineComplete)
		{
			yield return StartCoroutine(NewWaypointRoutine());
			waypointRoutineComplete = true;
		}
		FixedPoint lastTgtPoint = default(FixedPoint);
		if ((bool)targetActor)
		{
			lastTgtPoint.point = targetActor.position;
		}
		while (base.guidanceEnabled)
		{
			if (!targetActor || !targetActor.alive)
			{
				if (!isSearching)
				{
					Debug.Log("ASM lost it's target.  Attempting to acquire a new one!");
					base.missile.guidanceMode = Missile.GuidanceModes.GPS;
					base.missile.explosionType = ExplosionManager.ExplosionTypes.Aerial;
					foundTarget = false;
					StartCoroutine(TargetAcquireRoutine());
				}
				while (isSearching)
				{
					Vector3 vector = Vector3.ProjectOnPlane(lastTgtPoint.point - base.transform.position, Vector3.up);
					Vector3 point = base.transform.position + vector.normalized * 1000f;
					point += altitudeControlPID.Evaluate(WaterPhysics.GetAltitude(base.transform.position), approachAltitude) * Vector3.up;
					guidedPoint.point = point;
					yield return null;
				}
			}
			else
			{
				lastTgtPoint.point = targetActor.position;
			}
			if (terminalBehavior == ASMTerminalBehaviors.Direct)
			{
				guidedPoint.point = Missile.BallisticLeadTargetPoint(base.missile.estTargetPos, base.missile.estTargetVel, base.missile.rb.position, base.missile.rb.velocity, base.missile.rb.velocity.magnitude, base.missile.leadTimeMultiplier, base.missile.maxBallisticOffset, base.missile.maxLeadTime);
			}
			else
			{
				Vector3 current = base.missile.estTargetPos - base.transform.position;
				current.y = 0f;
				float magnitude = current.magnitude;
				float altitude = WaterPhysics.GetAltitude(base.transform.position);
				if (magnitude < finalRadius && altitude > approachAltitude + 10f)
				{
					Vector3 estTargetPos = base.missile.estTargetPos;
					estTargetPos.y = WaterPhysics.instance.height + 1f;
					guidedPoint.point = Missile.BallisticLeadTargetPoint(estTargetPos, base.missile.estTargetVel, base.missile.rb.position, base.missile.rb.velocity, base.missile.rb.velocity.magnitude, base.missile.leadTimeMultiplier, 100f, base.missile.maxLeadTime);
				}
				else if (magnitude < approachRadius)
				{
					Vector3 vector2 = base.missile.estTargetPos;
					float num = approachAltitude;
					if (magnitude < finalRadius)
					{
						num = 7f;
					}
					if (terminalBehavior == ASMTerminalBehaviors.Popup && magnitude < popupRadius)
					{
						vector2 += Mathf.Max(0f, popupMagnitude * (magnitude - 800f)) * Vector3.up;
					}
					else
					{
						vector2 = base.transform.position + current.normalized * 1000f;
						vector2 += altitudeControlPID.Evaluate(altitude, num) * Vector3.up;
						Vector3 target = vector2 - base.transform.position;
						target = Vector3.RotateTowards(current, target, maxDescendAngle * ((float)Math.PI / 180f), 0f);
						vector2 = base.transform.position + target;
					}
					if (terminalBehavior == ASMTerminalBehaviors.SSEvasive && (magnitude < evasiveRadius || foundIncomingMissile || ((bool)rwr && rwr.missileDetected) || ((bool)mRWR && (mRWR.isLocked || mRWR.isMissileLocked))))
					{
						vector2 = base.transform.position + (vector2 - base.transform.position).normalized * 1000f;
						float num2 = Triangle(noiseRand + Time.time * evasiveRate) * evasiveMagnitude;
						float num3 = 1f / Mathf.Max(1f, Mathf.Abs(altitude - num));
						Vector3 rhs = current.normalized * Mathf.Clamp(magnitude - 500f, 0f, 1500f);
						Vector3 vector3 = Vector3.Cross(Vector3.up * num3, rhs);
						vector2 = vector2 + num2 * vector3 + 17f * Mathf.Abs(num2) * Vector3.up;
					}
					guidedPoint.point = vector2;
				}
				else
				{
					guidedPoint.point = Missile.BallisticPoint(base.missile.estTargetPos - current.normalized * approachRadius + approachAltitude * Vector3.up, base.missile.transform.position, base.missile.rb.velocity.magnitude);
				}
			}
			yield return null;
		}
	}

	private IEnumerator DeployClearanceRoutine()
	{
		base.missile.steerMult = waypointSteerMult;
		while ((bool)base.missile.launcherRB)
		{
			Vector3 vector = base.missile.launcherRB.position - base.missile.rb.position;
			vector.y = 0f;
			if (vector.sqrMagnitude > 10000f)
			{
				break;
			}
			Vector3 right = base.missile.launcherRB.transform.right;
			right.y = 0f;
			Vector3 vector2 = Vector3.Project(-vector, right);
			guidedPoint.point = base.transform.position + 1000f * base.transform.forward + vector2.normalized * 500f;
			yield return null;
		}
	}

	public void SetTarget(GPSTargetGroup grp)
	{
		if (!grp.isPath || grp.targets.Count == 1 || grp.currentTargetIdx == grp.targets.Count - 1)
		{
			SetTarget(grp.currentTarget);
			return;
		}
		qs_targetGroup = grp;
		Debug.Log("Setting ASM target with " + (grp.targets.Count - grp.currentTargetIdx) + " waypoints.");
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
		origFwd = base.transform.forward;
		StartCoroutine(GeneratePathRoutine(tempPath));
	}

	public void SetTarget(GPSTarget tgt)
	{
		qs_singleTarget = tgt;
		Debug.Log("Setting ASM target with single waypoint.");
		path = null;
		Vector3 position = base.transform.position;
		position.y = Mathf.Max(position.y, tgt.worldPosition.y + 100f);
		Vector3 worldPosition = tgt.worldPosition;
		worldPosition.y = base.transform.position.y;
		Vector3 worldPoint = Vector3.Lerp(position, worldPosition, 0.5f);
		Plane plane = new Plane(Vector3.down, position);
		Ray ray = new Ray(tgt.worldPosition, Vector3.RotateTowards(Vector3.up, position - tgt.worldPosition, 1.3089969f, 1f));
		if (plane.Raycast(ray, out var enter))
		{
			Vector3 point = ray.GetPoint(enter);
			if (Vector3.Dot(point - position, tgt.worldPosition - position) > 0f)
			{
				worldPoint = point;
			}
		}
		Vector3D vector3D = VTMapManager.WorldToGlobalPoint(position);
		Vector3D vector3D2 = VTMapManager.WorldToGlobalPoint(worldPoint);
		Vector3D vector3D3 = VTMapManager.WorldToGlobalPoint(tgt.worldPosition);
		LinearPathD tempPath = new LinearPathD(new Vector3D[3] { vector3D, vector3D2, vector3D3 });
		StartCoroutine(GeneratePathRoutine(tempPath));
	}

	private float CalculateTerminalT(float dist)
	{
		return 1f - dist / path.length;
	}

	private Vector3 GetSurfacePoint(Vector3 worldPt)
	{
		if ((bool)VTMapGenerator.fetch && !VTMapGenerator.fetch.IsChunkColliderEnabled(worldPt))
		{
			return VTMapGenerator.fetch.GetSurfacePos(worldPt);
		}
		worldPt.y = WaterPhysics.instance.height;
		Hitbox component;
		if (Physics.Raycast(worldPt + new Vector3(0f, 10000f, 0f), Vector3.down, out var hitInfo, 10000f, 1) && (!(component = hitInfo.collider.GetComponent<Hitbox>()) || component.actor.role != Actor.Roles.Ship))
		{
			Vector3 point = hitInfo.point;
			point.y = Mathf.Max(point.y, WaterPhysics.instance.height);
			return point;
		}
		worldPt.y = WaterPhysics.instance.height;
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

	private IEnumerator GeneratePathRoutine(LinearPathD tempPath)
	{
		float maxAngle = 10f;
		List<Vector3D> points = new List<Vector3D>();
		float t = 0f;
		float tInterval = 75f / tempPath.length;
		int maxCastsPerFrame = 3;
		int casts = 0;
		int currIdx = 0;
		float minAltitude = 100f;
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
		terminalT = CalculateTerminalT(approachRadius);
		for (float w = 1f; w >= 0f; w -= tInterval)
		{
			overWaterT = w;
			if (WaterPhysics.GetAltitude(GetSurfacePoint(VTMapManager.GlobalToWorldPoint(path.GetPoint(w)))) > 1f)
			{
				w = -1f;
			}
			yield return null;
		}
	}

	private Vector3 ApplyAltitudeMaintenance(Vector3 tgtPos)
	{
		Vector3 current = tgtPos - base.transform.position;
		current.y = 0f;
		float altitude = WaterPhysics.GetAltitude(base.transform.position);
		Vector3 vector = base.transform.position + current.normalized * 1000f;
		float target = Mathf.Max(approachAltitude, WaterPhysics.GetAltitude(tgtPos));
		Vector3 target2 = vector + altitudeControlPID.Evaluate(altitude, target) * Vector3.up - base.transform.position;
		target2 = Vector3.RotateTowards(current, target2, maxDescendAngle * ((float)Math.PI / 180f), 0f);
		return base.transform.position + target2;
	}

	private IEnumerator NewWaypointRoutine()
	{
		float followPtLeadDistance = 300f;
		Debug.Log("Beginning ASSM guidance.  Waiting for path to be calculated.");
		while (path == null)
		{
			guidedPoint = new FixedPoint(base.transform.position + origFwd * 100f);
			yield return null;
		}
		Debug.Log("ASSM Path calculated.  Begining route guidance.");
		if (!finishedStartRun)
		{
			FixedPoint startPt = new FixedPoint(VTMapManager.GlobalToWorldPoint(path.GetPoint(0f)));
			while ((base.transform.position - startPt.point).sqrMagnitude > 16000000f && Vector3.Dot(base.transform.forward, startPt.point - base.transform.position) > 0f)
			{
				Vector3 point = startPt.point;
				point = ApplyAltitudeMaintenance(point);
				guidedPoint = new FixedPoint(point);
				yield return null;
			}
			finishedStartRun = true;
		}
		while ((bool)base.missile)
		{
			float currT;
			Vector3 vector = GetFollowPoint(base.transform.position, followPtLeadDistance, out currT);
			if (currT > terminalT)
			{
				if (!isSearching)
				{
					StartCoroutine(TargetAcquireRoutine());
				}
				if (foundTarget && currT > overWaterT)
				{
					break;
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

	private IEnumerator TargetAcquireRoutine()
	{
		if (isSearching)
		{
			yield break;
		}
		isSearching = true;
		if (!searchRadar)
		{
			yield break;
		}
		searchRadar.radarEnabled = true;
		StartCoroutine(SearchForThreatsRoutine());
		FixedPoint gpPoint = new FixedPoint(GetSurfacePoint(GetWorldPoint(path, 1f)));
		GPSTarget gPSTarget = new GPSTarget(gpPoint.point, "tgt", 0);
		base.missile.SetGPSTarget(gPSTarget);
		new WaitForSeconds(0.25f);
		float closestSqrDist = maxSearchRadius * maxSearchRadius;
		while (allowSearch && !foundTarget)
		{
			foreach (Actor detectedUnit in searchRadar.detectedUnits)
			{
				if (!detectedUnit || detectedUnit.finalCombatRole != Actor.Roles.Ship || !detectedUnit.alive)
				{
					continue;
				}
				Debug.DrawLine(detectedUnit.position, detectedUnit.position + new Vector3(0f, 40f, 0f), Color.red);
				float sqrMagnitude = (detectedUnit.position - gpPoint.point).sqrMagnitude;
				Debug.DrawLine(detectedUnit.position + new Vector3(0f, 25f, 0f), gpPoint.point + new Vector3(0f, 25f, 0f), Color.cyan);
				Vector3 vector = detectedUnit.position - detectedUnit.velocity * (Time.time - base.missile.timeFired);
				float sqrMagnitude2 = (gpPoint.point - vector).sqrMagnitude;
				Debug.DrawLine(vector + new Vector3(0f, 30f, 0f), gpPoint.point + new Vector3(0f, 30f, 0f), Color.magenta);
				sqrMagnitude = Mathf.Min(sqrMagnitude, sqrMagnitude2);
				if (sqrMagnitude < closestSqrDist)
				{
					targetActor = detectedUnit;
					closestSqrDist = sqrMagnitude;
					if (sqrMagnitude < targetCertaintyRadius * targetCertaintyRadius)
					{
						Debug.Log("ASM found target within certainty radius (" + targetActor.DebugName() + ")");
						foundTarget = true;
					}
				}
			}
			if ((bool)targetActor)
			{
				Debug.DrawLine(base.transform.position, targetActor.position + new Vector3(0f, 40f, 0f), Color.green);
			}
			if ((bool)targetActor)
			{
				float sqrMagnitude3 = (base.transform.position - targetActor.position).sqrMagnitude;
				if (sqrMagnitude3 < decisionDistance * decisionDistance)
				{
					Debug.Log($"ASM triggered decisionDistance on target {targetActor.DebugName()} ({Mathf.Sqrt(sqrMagnitude3)}m)");
					foundTarget = true;
				}
			}
			yield return null;
		}
		if ((bool)targetActor)
		{
			base.missile.explosionType = ExplosionManager.ExplosionTypes.Massive;
			isSearching = false;
			WaitForSeconds wait = new WaitForSeconds(0.2f);
			while ((bool)targetActor)
			{
				foreach (Actor detectedUnit2 in searchRadar.detectedUnits)
				{
					if (detectedUnit2 == targetActor)
					{
						float num = Vector3.Dot(base.missile.rb.velocity - targetActor.velocity, (targetActor.position - base.transform.position).normalized);
						float num2 = (targetActor.position - base.transform.position).magnitude / num;
						gPSTarget = new GPSTarget(targetActor.position + num2 * targetActor.velocity, "tgt", 0);
						base.missile.SetGPSTarget(gPSTarget);
						break;
					}
				}
				yield return wait;
			}
		}
		else
		{
			base.missile.explodeDamage = 0f;
			base.missile.Detonate();
		}
	}

	private IEnumerator SearchForThreatsRoutine()
	{
		if (!searchRadar || !searchRadar.detectMissiles)
		{
			yield break;
		}
		while (!searchRadar.radarEnabled)
		{
			yield return null;
		}
		while (base.enabled && searchRadar.radarEnabled)
		{
			for (int i = 0; i < searchRadar.detectedUnits.Count; i++)
			{
				Actor actor = searchRadar.detectedUnits[i];
				if ((bool)actor && actor.finalCombatRole == Actor.Roles.Missile && Vector3.Dot(rhs: (actor.velocity - base.missile.rb.velocity).normalized, lhs: (base.transform.position - actor.position).normalized) > 0.9f)
				{
					foundIncomingMissile = true;
					yield break;
				}
			}
			yield return new WaitForSeconds(1f);
		}
	}

	public override Vector3 GetGuidedPoint()
	{
		return guidedPoint.point;
	}

	private IEnumerator TerrainAvoidanceRoutine()
	{
		float aheadMult = 4f;
		int maxMemCount = (int)(90f * aheadMult);
		float[] heights = new float[maxMemCount];
		int hIdx = 0;
		while (base.enabled)
		{
			int num = Mathf.FloorToInt(Mathf.Min(90f * aheadMult, aheadMult / Mathf.Max(Time.deltaTime, 0.011f)));
			Vector3 velocity = base.missile.rb.velocity;
			velocity.y = 0f;
			Ray ray = new Ray(base.transform.position + velocity * aheadMult + 1000f * Vector3.up, Vector3.down);
			RaycastHit hitInfo;
			if ((bool)VTMapGenerator.fetch && !VTMapGenerator.fetch.IsChunkColliderEnabled(ray.origin))
			{
				float heightmapAltitude = VTMapGenerator.fetch.GetHeightmapAltitude(ray.origin);
				heights[hIdx] = Mathf.Max(0f, heightmapAltitude);
			}
			else if (Physics.Raycast(ray, out hitInfo, WaterPhysics.GetAltitude(base.transform.position) + 1000f, 1, QueryTriggerInteraction.Ignore))
			{
				heights[hIdx] = WaterPhysics.GetAltitude(hitInfo.point);
			}
			else
			{
				heights[hIdx] = 0f;
			}
			hIdx = (hIdx + 1) % maxMemCount;
			float num2 = 0f;
			int num3 = hIdx;
			for (int i = 0; i < num; i++)
			{
				num3--;
				if (num3 < 0)
				{
					num3 = maxMemCount - 1;
				}
				num2 = Mathf.Max(num2, heights[num3]);
			}
			num2 = (terrainHeight = num2 * 1.25f);
			yield return null;
		}
	}

	private float Triangle(float t)
	{
		t += 0.5f;
		float num = Mathf.Repeat(t, 1f);
		if (Mathf.FloorToInt(t) % 2 != 0)
		{
			num = 1f - num;
		}
		return (num - 0.5f) * 2f;
	}

	public override void SaveToQuicksaveNode(ConfigNode qsNode)
	{
		base.SaveToQuicksaveNode(qsNode);
		ConfigNode configNode = qsNode.AddNode("AntiShipGuidance");
		if (path != null)
		{
			configNode.AddNode(path.SaveToConfigNode("path"));
		}
		else if (qs_targetGroup != null)
		{
			configNode.AddNode(qs_targetGroup.SaveToConfigNode("qs_targetGroup"));
		}
		else if (qs_singleTarget != null)
		{
			configNode.SetValue("qs_singleTargetPoint", new FixedPoint(qs_singleTarget.worldPosition));
		}
		if (base.guidanceEnabled)
		{
			configNode.SetValue("deployClearanceComplete", deployClearanceComplete);
			configNode.SetValue("waypointRoutineComplete", waypointRoutineComplete);
			configNode.SetValue("noiseRand", noiseRand);
			configNode.SetValue("isSearching", isSearching);
			configNode.SetValue("foundTarget", foundTarget);
			configNode.SetValue("originalSteerMult", originalSteerMult);
			configNode.SetValue("missile_steerMult", base.missile.steerMult);
			configNode.SetValue("foundIncomingMissile", foundIncomingMissile);
			configNode.SetValue("terminalT", terminalT);
		}
	}

	public override void LoadFromQuicksaveNode(Missile m, ConfigNode qsNode)
	{
		base.LoadFromQuicksaveNode(m, qsNode);
		string text = "AntiShipGuidance";
		if (!qsNode.HasNode(text))
		{
			return;
		}
		Debug.Log("Quickloading ASM");
		ConfigNode node = qsNode.GetNode(text);
		if (node.HasNode("path"))
		{
			path = LinearPathD.LoadFromConfigNode(node.GetNode("path"));
		}
		else if (node.HasNode("qs_targetGroup"))
		{
			GPSTargetGroup target = GPSTargetGroup.LoadFromConfigNode(node.GetNode("qs_targetGroup"));
			SetTarget(target);
		}
		else if (node.HasValue("qs_singleTargetPoint"))
		{
			GPSTarget target2 = new GPSTarget(node.GetValue<FixedPoint>("qs_singleTargetPoint").point, "QS_", 0);
			SetTarget(target2);
		}
		if (!base.guidanceEnabled)
		{
			return;
		}
		terminalT = node.GetValue<float>("terminalT");
		noiseRand = node.GetValue<float>("noiseRand");
		originalSteerMult = node.GetValue<float>("originalSteerMult");
		base.missile.steerMult = node.GetValue<float>("missile_steerMult");
		foundIncomingMissile = node.GetValue<bool>("foundIncomingMissile");
		deployClearanceComplete = node.GetValue<bool>("deployClearanceComplete");
		Debug.Log(" - deployClearanceComplete " + deployClearanceComplete);
		waypointRoutineComplete = node.GetValue<bool>("waypointRoutineComplete");
		Debug.Log(" - waypointRoutineComplete " + waypointRoutineComplete);
		bool value = node.GetValue<bool>("isSearching");
		bool flag = (foundTarget = node.GetValue<bool>("foundTarget"));
		Debug.Log(" - _isSearching " + value);
		Debug.Log(" - _foundTarget " + flag);
		StartCoroutine(ASMProgramRoutine());
		if (value)
		{
			searchRadar.radarEnabled = true;
			if (!flag)
			{
				isSearching = false;
				StartCoroutine(TargetAcquireRoutine());
			}
		}
	}
}
