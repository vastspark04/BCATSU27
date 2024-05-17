using System.Collections;
using UnityEngine;

public class GroundUnitMover : MonoBehaviour, IQSVehicleComponent
{
	public enum Behaviors
	{
		Path,
		Parked,
		StayInRadius,
		Follow,
		RailPath
	}

	private bool gotActor;

	private Actor _a;

	public Behaviors behavior;

	public Vector3 railPathOffset;

	public float moveSpeed;

	public float moveAccel;

	public float height;

	public float maxClimb = 1f;

	private float randomTOffset;

	public float moveWaitTime = 1f;

	private float followPointLeadDistance = 2f;

	[HideInInspector]
	public bool parkWhenInRallyRadius;

	public Transform followTransform;

	private FixedPoint fRallyPos;

	public Transform rallyTransform;

	public float rallyRadius;

	public FollowPath path;

	private Coroutine behaviorRoutine;

	private Coroutine moveRoutine;

	private float deccelDist;

	private FixedPoint movingToPos;

	public bool move = true;

	[Tooltip("Always uses raycasts to check surface height")]
	public bool overrideSurfaceMode;

	public float collisionDetectRadius = 1f;

	[Header("Unit group stuff")]
	public int positionInGroup;

	public GroundSquad squad;

	public float unitSize = 5f;

	private Vector3 _frmFwd = Vector3.forward;

	public float currTOnPath;

	private Transform myTransform;

	private VTMapGenerator.VTTerrainChunk currentTerrainChunk;

	private bool moveFormation;

	private bool isRemote;

	private FixedPoint remoteTarget;

	private int nonMovingRaycastInterval = 8;

	private int nonMovingRaycastIdx;

	private MovingPlatform mPlat;

	private Collider _oncol;

	private MovingPlatform surfaceMovingPlatform;

	private GameObject _sObj;

	private Coroutine railPathRoutine;

	private RaycastHit[] surfaceRayHits = new RaycastHit[10];

	private float railMoveTMax;

	private float railMoveT;

	private bool isRailMoving;

	private FollowPath _lastFp;

	private bool readyToMove;

	private bool quickloadedPath;

	public Actor actor
	{
		get
		{
			if (!gotActor)
			{
				_a = GetComponent<Actor>();
				gotActor = true;
			}
			return _a;
		}
		set
		{
			_a = value;
		}
	}

	private float finalMoveSpeed
	{
		get
		{
			if ((bool)squad)
			{
				if (positionInGroup == 0)
				{
					return squad.slowestSpeed * 0.95f;
				}
				return moveSpeed * 1.2f;
			}
			return moveSpeed;
		}
	}

	public Vector3 rallyPosition
	{
		get
		{
			return fRallyPos.point;
		}
		set
		{
			fRallyPos.point = value;
		}
	}

	public Vector3 velocity { get; private set; }

	public Vector3 surfaceNormal { get; private set; }

	private Vector3 worldMovingToPos
	{
		get
		{
			return movingToPos.point;
		}
		set
		{
			movingToPos.point = value;
		}
	}

	public Vector3 formationForward
	{
		get
		{
			if (velocity.sqrMagnitude > 0.1f)
			{
				_frmFwd = velocity;
				_frmFwd.y = 0f;
				_frmFwd.Normalize();
			}
			return _frmFwd;
		}
	}

	public Collider onCollider
	{
		get
		{
			return _oncol;
		}
		private set
		{
			_oncol = value;
			if ((bool)value)
			{
				mPlat = _oncol.GetComponent<MovingPlatform>();
			}
			else
			{
				mPlat = null;
			}
		}
	}

	private GameObject surfaceObj
	{
		get
		{
			return _sObj;
		}
		set
		{
			if (_sObj != value)
			{
				_sObj = value;
				if ((bool)_sObj)
				{
					surfaceMovingPlatform = value.GetComponent<MovingPlatform>();
				}
				else
				{
					surfaceMovingPlatform = null;
				}
			}
		}
	}

	public bool railComplete { get; private set; }

	private void Awake()
	{
		myTransform = base.transform;
		if (!actor)
		{
			actor = GetComponentInParent<Actor>();
		}
	}

	private void Start()
	{
		if (moveWaitTime > 0f)
		{
			randomTOffset = Random.Range(0f, 1f);
		}
		if ((bool)VTMapGenerator.fetch)
		{
			currentTerrainChunk = VTMapGenerator.fetch.GetTerrainChunk(base.transform.position);
			if (currentTerrainChunk != null)
			{
				currentTerrainChunk.groundMovers.Add(this);
				VTMapGenerator.fetch.SetChunkLOD(currentTerrainChunk.grid, 0);
				VTMapGenerator.fetch.BakeCollider(currentTerrainChunk.grid);
			}
		}
	}

	private void UpdateTerrainChunk(Vector3 rayPos)
	{
		if (!VTMapGenerator.fetch)
		{
			return;
		}
		IntVector2 intVector = VTMapGenerator.fetch.ChunkGridAtPos(rayPos);
		if (currentTerrainChunk == null || intVector != currentTerrainChunk.grid)
		{
			if (currentTerrainChunk != null)
			{
				currentTerrainChunk.groundMovers.Remove(this);
			}
			currentTerrainChunk = VTMapGenerator.fetch.GetTerrainChunk(intVector);
			if (currentTerrainChunk != null)
			{
				currentTerrainChunk.groundMovers.Add(this);
				VTMapGenerator.fetch.SetChunkLOD(intVector, 0);
				VTMapGenerator.fetch.BakeCollider(currentTerrainChunk.grid);
				if ((bool)currentTerrainChunk.collider)
				{
					currentTerrainChunk.collider.enabled = true;
				}
			}
		}
		if (currentTerrainChunk != null && (bool)currentTerrainChunk.collider && !currentTerrainChunk.collider.enabled)
		{
			currentTerrainChunk.collider.enabled = true;
		}
	}

	public void RefreshBehaviorRoutines()
	{
		if (behaviorRoutine != null)
		{
			StopCoroutine(behaviorRoutine);
			behaviorRoutine = null;
		}
		if (moveRoutine != null)
		{
			StopCoroutine(moveRoutine);
			moveRoutine = null;
		}
	}

	public void MoveToWaypoint(Transform wptTf)
	{
		RefreshBehaviorRoutines();
		rallyTransform = wptTf;
		behavior = Behaviors.StayInRadius;
	}

	public void BeginMovingFormation()
	{
		moveFormation = true;
	}

	public void SetRemoteTarget(Vector3 targetPos)
	{
		isRemote = true;
		remoteTarget.point = targetPos;
	}

	private void Update()
	{
		if (!readyToMove)
		{
			return;
		}
		if (move)
		{
			deccelDist = finalMoveSpeed * finalMoveSpeed / (2f * moveAccel);
			if (isRemote)
			{
				MoveToConstantly(remoteTarget.point);
			}
			else if ((bool)squad && squad.leaderMover != this)
			{
				if (squad.leaderMover.behavior != Behaviors.Parked)
				{
					moveFormation = true;
				}
				if (behaviorRoutine != null)
				{
					StopCoroutine(behaviorRoutine);
				}
				if (moveRoutine != null)
				{
					StopCoroutine(moveRoutine);
				}
				if (moveFormation)
				{
					Vector3 formationPosition = squad.GetFormationPosition(positionInGroup);
					float magnitude = squad.leaderMover.velocity.magnitude;
					if (magnitude > 1f || (formationPosition - base.transform.position).sqrMagnitude > (squad.leaderMover.transform.position - formationPosition).sqrMagnitude)
					{
						MoveToConstantly(formationPosition, magnitude, squad.leaderMover.velocity);
					}
					else
					{
						velocity = Vector3.MoveTowards(velocity, Vector3.zero, moveAccel);
					}
				}
			}
			else
			{
				switch (behavior)
				{
				case Behaviors.StayInRadius:
					if ((bool)rallyTransform)
					{
						StayInRadius(rallyTransform.position, rallyRadius);
					}
					else
					{
						StayInRadius(rallyPosition, rallyRadius);
					}
					break;
				case Behaviors.Path:
					if (!path)
					{
						behavior = Behaviors.Parked;
						Debug.Log("GroundUnitMover has no path");
					}
					else
					{
						FollowPath(path, 0.02f);
					}
					break;
				case Behaviors.Follow:
					if (!followTransform)
					{
						behavior = Behaviors.Parked;
					}
					else
					{
						FollowTransform();
					}
					break;
				case Behaviors.RailPath:
					if (!path)
					{
						behavior = Behaviors.Parked;
						Debug.Log("GroundUnitMover has no path");
					}
					else
					{
						RailPath();
					}
					break;
				case Behaviors.Parked:
					velocity = Vector3.zero;
					break;
				}
			}
		}
		if (behavior != Behaviors.RailPath)
		{
			if (railPathRoutine != null)
			{
				StopCoroutine(railPathRoutine);
			}
			railComplete = false;
			isRailMoving = false;
			Vector3 position = myTransform.position;
			if (AIHelper.instance.surfaceMode == AIHelper.SurfaceModes.Flat && !overrideSurfaceMode)
			{
				position.y = AIHelper.instance.GetSurfaceHeight(position) + height;
			}
			position += velocity * Time.deltaTime;
			UpdateTerrainChunk(position);
			if ((velocity.sqrMagnitude > 0.1f || nonMovingRaycastIdx == 0) && Physics.Raycast(new Ray(position + (5f - height) * Vector3.up, Vector3.down), out var hitInfo, 20f, 1))
			{
				position.y = hitInfo.point.y + height;
				surfaceNormal = hitInfo.normal;
				onCollider = hitInfo.collider;
			}
			nonMovingRaycastIdx = (nonMovingRaycastIdx + 1) % nonMovingRaycastInterval;
			myTransform.position = position;
		}
	}

	public void ClearSurfaceObj()
	{
		surfaceObj = null;
	}

	public virtual void FixedUpdate()
	{
		if (!readyToMove)
		{
			return;
		}
		if (velocity.sqrMagnitude > 0f && behavior != Behaviors.RailPath)
		{
			if (move && AIHelper.instance.surfaceMode == AIHelper.SurfaceModes.Flat && !overrideSurfaceMode)
			{
				surfaceNormal = Vector3.up;
			}
			if (!move)
			{
				if (behaviorRoutine != null)
				{
					StopCoroutine(behaviorRoutine);
					behaviorRoutine = null;
				}
				if (moveRoutine != null)
				{
					StopCoroutine(moveRoutine);
					moveRoutine = null;
				}
				Stop();
			}
		}
		Vector3 customVelocity = velocity;
		if ((bool)mPlat)
		{
			Vector3 vector = mPlat.GetVelocity(myTransform.position);
			myTransform.position += vector * Time.fixedDeltaTime;
			customVelocity += vector;
		}
		actor.SetCustomVelocity(customVelocity);
	}

	private void RailPath()
	{
		if (behaviorRoutine != null)
		{
			StopCoroutine(behaviorRoutine);
		}
		if (!isRailMoving)
		{
			railPathRoutine = StartCoroutine(RailPathRoutine());
		}
	}

	private IEnumerator RailPathRoutine()
	{
		if (isRailMoving)
		{
			Debug.Log(base.gameObject.name + " is already rail pathing but RailPathRoutine was started again!");
			yield break;
		}
		isRailMoving = true;
		Debug.Log("Starting rail path routine (" + base.gameObject.name + ") Path length: " + path.GetApproximateLength() + "m");
		if (quickloadedPath)
		{
			quickloadedPath = false;
			Debug.Log("  -  Resuming path at t=" + currTOnPath);
		}
		else
		{
			currTOnPath = path.GetClosestTime(myTransform.position, 6);
			Vector3 worldPoint = path.GetWorldPoint(currTOnPath);
			Vector3 worldTangent = path.GetWorldTangent(currTOnPath);
			Vector3 vector = Vector3.Cross(Vector3.up, worldTangent);
			float magnitude = Vector3.Project(myTransform.position - worldPoint, vector).magnitude;
			magnitude *= Mathf.Sign(Vector3.Dot(myTransform.position - worldPoint, vector));
			railPathOffset = new Vector3(magnitude, 0f, 0f);
		}
		float accelT = moveAccel / path.GetApproximateLength();
		float deccelT = 1f - deccelDist / path.GetApproximateLength();
		float speedT = velocity.magnitude / path.GetApproximateLength();
		Vector3 worldTangent2 = path.GetWorldTangent(currTOnPath);
		Vector3.Cross(Vector3.up, worldTangent2);
		bool hasBegunTransition = false;
		Vector3 transitionOffset = Vector3.zero;
		FixedPoint prevPos = new FixedPoint(path.GetWorldPoint(currTOnPath));
		FixedPoint worldActualPoint = new FixedPoint(myTransform.position);
		while (currTOnPath < 1f)
		{
			float speedTMax = (railMoveTMax = finalMoveSpeed / path.GetApproximateLength());
			float target = speedTMax * 0.1f;
			railMoveT = speedT;
			if (move)
			{
				speedT = ((path.loop || !(currTOnPath > deccelT)) ? Mathf.MoveTowards(speedT, speedTMax, accelT * Time.deltaTime) : Mathf.MoveTowards(speedT, target, accelT * Time.deltaTime));
			}
			else
			{
				speedT = Mathf.MoveTowards(speedT, 0f, accelT * Time.deltaTime);
				yield return null;
			}
			currTOnPath += speedT * Time.deltaTime;
			railPathOffset = Vector3.MoveTowards(railPathOffset, Vector3.zero, moveSpeed * Time.deltaTime);
			if (speedT > 0f)
			{
				Vector3 worldPoint2 = path.GetWorldPoint(currTOnPath);
				worldTangent2 = path.GetWorldTangent(currTOnPath);
				velocity = finalMoveSpeed * (speedT / speedTMax) * worldTangent2;
				Vector3 vector2 = Vector3.Cross(Vector3.up, worldTangent2);
				Vector3 vector3 = worldPoint2 + railPathOffset.z * worldTangent2 + railPathOffset.x * vector2;
				vector3.y = worldActualPoint.point.y;
				float num = 5f;
				Ray ray = new Ray(vector3 + num * Vector3.up, Vector3.down);
				UpdateTerrainChunk(ray.origin);
				if (!VTMapGenerator.fetch || VTMapGenerator.fetch.IsChunkColliderEnabled(ray.origin))
				{
					RaycastHit hitInfo = default(RaycastHit);
					bool flag = false;
					int num2 = 0;
					while (!flag && num2 < 5)
					{
						flag = Physics.Raycast(ray, out hitInfo, num * 2f, 1);
						num *= 2f;
						ray = new Ray(vector3 + num * Vector3.up, Vector3.down);
						num2++;
					}
					if (flag)
					{
						RaycastHit raycastHit = hitInfo;
						surfaceObj = raycastHit.collider.gameObject;
						vector3.y = raycastHit.point.y;
						surfaceNormal = raycastHit.normal;
						onCollider = raycastHit.collider;
					}
				}
				Vector3 vector4 = vector3 + height * Vector3.up;
				if ((vector4 - worldActualPoint.point).sqrMagnitude > float.Epsilon)
				{
					velocity = finalMoveSpeed * (speedT / speedTMax) * (vector4 - worldActualPoint.point).normalized;
				}
				if (!hasBegunTransition)
				{
					hasBegunTransition = true;
					transitionOffset = myTransform.position - vector4;
				}
				else
				{
					transitionOffset = Vector3.MoveTowards(transitionOffset, Vector3.zero, finalMoveSpeed * Time.deltaTime);
				}
				myTransform.position = vector4 + transitionOffset;
				worldActualPoint.point = vector4;
				prevPos.point = worldPoint2;
			}
			else
			{
				velocity = Vector3.zero;
			}
			yield return null;
			if (currTOnPath >= 1f && path.loop)
			{
				currTOnPath = 0f;
			}
		}
		currTOnPath = 1f;
		railComplete = true;
		behavior = Behaviors.Parked;
	}

	[ContextMenu("Set Rally to Current Pos")]
	public void SetRallyToCurrentPos()
	{
	}

	private void FollowTransform()
	{
		StayInRadius(followTransform.position, rallyRadius);
	}

	private void FollowPath(FollowPath followPath, float radius)
	{
		if (behaviorRoutine != null)
		{
			StopCoroutine(behaviorRoutine);
		}
		if (moveRoutine != null)
		{
			StopCoroutine(moveRoutine);
		}
		Vector3 vector;
		if (followPath != _lastFp && !quickloadedPath)
		{
			_lastFp = followPath;
			vector = followPath.GetFollowPoint(base.transform.position, followPointLeadDistance + deccelDist + radius * 2f, out currTOnPath);
		}
		else
		{
			if (quickloadedPath)
			{
				quickloadedPath = false;
				_lastFp = path;
			}
			vector = followPath.GetWorldPoint(currTOnPath);
			Vector3 vector2 = vector - base.transform.position;
			vector2.y = 0f;
			if (vector2.sqrMagnitude < deccelDist * deccelDist)
			{
				float num = (followPointLeadDistance + deccelDist + radius * 2f) / followPath.GetApproximateLength();
				currTOnPath += num;
				vector = followPath.GetWorldPoint(currTOnPath);
				behavior = Behaviors.RailPath;
				return;
			}
		}
		if (currTOnPath > 0.999f)
		{
			if (!followPath.loop)
			{
				behavior = Behaviors.Parked;
				return;
			}
			currTOnPath = 0f;
		}
		MoveToConstantly(vector);
	}

	private void StayInRadius(Vector3 center, float radius)
	{
		if (behaviorRoutine == null)
		{
			behaviorRoutine = StartCoroutine(StayInRadiusRoutine(center, radius));
		}
	}

	private IEnumerator StayInRadiusRoutine(Vector3 center, float radius)
	{
		FixedPoint centerPt = new FixedPoint(center);
		Vector3 worldPosition = center + Vector3.ProjectOnPlane(Random.insideUnitSphere * radius, Vector3.up);
		FixedPoint fixedTargetPos = new FixedPoint(worldPosition);
		if (moveRoutine != null)
		{
			StopCoroutine(moveRoutine);
		}
		while ((fixedTargetPos.point - base.transform.position).sqrMagnitude > deccelDist * deccelDist)
		{
			MoveToConstantly(fixedTargetPos.point);
			yield return null;
		}
		if (moveWaitTime > 0.1f)
		{
			yield return StartCoroutine(WaitRoutine(moveWaitTime + randomTOffset));
		}
		behaviorRoutine = null;
		if (parkWhenInRallyRadius && (centerPt.point - base.transform.position).sqrMagnitude < radius * radius)
		{
			parkWhenInRallyRadius = false;
			Debug.Log("Parked when got in rally radius.");
			Debug.DrawLine(centerPt.point, centerPt.point + 100f * Vector3.up, Color.magenta, 10f);
			behavior = Behaviors.Parked;
		}
	}

	private void MoveToConstantly(Vector3 worldPosition, float leaderSpeed = 0f, Vector3 leaderVel = default(Vector3))
	{
		Vector3 target = Vector3.zero;
		float magnitude = velocity.magnitude;
		float num = Mathf.Clamp(finalMoveSpeed - leaderSpeed, 1f, finalMoveSpeed);
		float num2 = num * num / (2f * moveAccel);
		if (move)
		{
			worldMovingToPos = worldPosition;
			Vector3 vector = worldPosition;
			vector.y = 0f;
			float num3 = Vector3.SqrMagnitude(vector - PlanarPos());
			if (num3 > num2 || leaderSpeed > 0f)
			{
				Vector3 vector2 = vector - PlanarPos();
				vector2 = Vector3.ProjectOnPlane(vector2, surfaceNormal).normalized;
				target = vector2 * finalMoveSpeed;
				bool flag = false;
				int num4 = 0;
				float num5 = 5f;
				RaycastHit hitInfo = default(RaycastHit);
				Vector3 vector3 = myTransform.position + vector2 * finalMoveSpeed * Time.fixedDeltaTime;
				UpdateTerrainChunk(vector3);
				while (!flag && num4 < 3)
				{
					flag = Physics.Raycast(new Ray(vector3 + new Vector3(0f, num5, 0f), Vector3.down), out hitInfo, num5 * 2f, 1);
					num5 *= 2f;
					num4++;
				}
				if (flag)
				{
					_ = hitInfo.point + height * Vector3.up;
					onCollider = hitInfo.collider;
					target = Vector3.ClampMagnitude((vector - PlanarPos()) / num2 * finalMoveSpeed, finalMoveSpeed);
					if (leaderSpeed > 0f)
					{
						target = Vector3.Lerp(target, leaderVel, 1f - Mathf.Clamp01(num3 / (num2 * num2)));
					}
					target = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(target, hitInfo.normal), finalMoveSpeed);
					surfaceObj = hitInfo.collider.gameObject;
					surfaceNormal = hitInfo.normal;
				}
				else
				{
					target = Vector3.zero;
				}
			}
			else if (leaderSpeed > 0f)
			{
				target = leaderVel;
			}
		}
		velocity = Vector3.MoveTowards(velocity, target, moveAccel * Time.deltaTime * (1f + Mathf.Clamp01(magnitude / moveSpeed)));
	}

	private IEnumerator WaitRoutine(float time)
	{
		float waitStart = Time.time;
		while (Time.time - waitStart < time)
		{
			Stop();
			yield return new WaitForFixedUpdate();
		}
	}

	private void Stop()
	{
		velocity = Vector3.MoveTowards(velocity, Vector3.zero, moveAccel * Time.fixedDeltaTime);
	}

	private Vector3 PlanarPos()
	{
		return new Vector3(myTransform.position.x, 0f, myTransform.position.z);
	}

	public void FullStop()
	{
		StopAllCoroutines();
		behaviorRoutine = null;
		moveRoutine = null;
		if (railPathRoutine != null)
		{
			StopCoroutine(railPathRoutine);
		}
		behavior = Behaviors.Parked;
		move = false;
	}

	public void SetVelocity(Vector3 v)
	{
		velocity = v;
	}

	private void OnDestroy()
	{
		if (currentTerrainChunk != null)
		{
			currentTerrainChunk.groundMovers.Remove(this);
		}
	}

	public void SetWorldRallyPosition(Vector3 position)
	{
		rallyPosition = position;
	}

	private void OnEnable()
	{
		StartCoroutine(OnEnableRoutine());
		nonMovingRaycastIdx = Random.Range(0, nonMovingRaycastInterval);
	}

	private IEnumerator OnEnableRoutine()
	{
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		yield return null;
		if (AIHelper.instance.surfaceMode == AIHelper.SurfaceModes.Terrain || overrideSurfaceMode)
		{
			RaycastHit hit = default(RaycastHit);
			bool hasHit = false;
			Vector3 pos = base.transform.position;
			while (!hasHit)
			{
				pos = base.transform.position;
				pos.y = WaterPhysics.instance.height + 10000f;
				bool flag;
				hasHit = (flag = Physics.Raycast(pos, Vector3.down, out hit, 10000f, 1));
				if (flag)
				{
					pos.y = hit.point.y;
					surfaceObj = hit.collider.gameObject;
				}
				else
				{
					yield return null;
				}
				if (hasHit && hit.point.y < WaterPhysics.instance.height)
				{
					yield return null;
					hasHit = false;
				}
			}
			OnPlaceOnTerrain(pos, hit.normal);
		}
		if (behaviorRoutine != null)
		{
			StopCoroutine(behaviorRoutine);
			behaviorRoutine = null;
		}
		if (moveRoutine != null)
		{
			StopCoroutine(moveRoutine);
			moveRoutine = null;
		}
		if (railPathRoutine != null)
		{
			StopCoroutine(railPathRoutine);
		}
		railPathRoutine = null;
		readyToMove = true;
	}

	private void OnDisable()
	{
		readyToMove = false;
	}

	[ContextMenu("AlignToSurface")]
	public void AlignToSurface()
	{
		if (Physics.Raycast(base.transform.position + 100f * Vector3.up, Vector3.down, out var hitInfo, 500f, 1, QueryTriggerInteraction.Ignore))
		{
			OnPlaceOnTerrain(hitInfo.point, hitInfo.normal);
		}
	}

	protected virtual void OnPlaceOnTerrain(Vector3 point, Vector3 normal)
	{
		Transform transform = base.transform;
		Vector3 forward = Vector3.Cross(Vector3.up, -transform.right);
		transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
		transform.position = point + height * Vector3.up;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_GroundUnitMover");
		configNode.SetValue("behavior", behavior);
		configNode.SetValue("currTOnPath", currTOnPath);
		configNode.SetValue("scenarioPath", path ? path.scenarioPathID : (-1));
		configNode.SetValue("move", move);
		configNode.SetValue("velocity", velocity);
		configNode.SetValue("railPathOffset", railPathOffset);
		if ((bool)rallyTransform)
		{
			Waypoint waypoint = VTScenario.current.waypoints.GetWaypoint(rallyTransform);
			if (waypoint != null)
			{
				configNode.SetValue("rallyTfID", waypoint.id);
			}
		}
		configNode.SetValue("rallyGlobalPos", fRallyPos);
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = base.gameObject.name + "_GroundUnitMover";
		if (!qsNode.HasNode(text))
		{
			return;
		}
		ConfigNode node = qsNode.GetNode(text);
		behavior = node.GetValue<Behaviors>("behavior");
		currTOnPath = node.GetValue<float>("currTOnPath");
		int value = node.GetValue<int>("scenarioPath");
		if (value >= 0)
		{
			path = VTScenario.current.paths.GetPath(value);
			if (behavior == Behaviors.Path || behavior == Behaviors.RailPath)
			{
				quickloadedPath = true;
			}
		}
		move = node.GetValue<bool>("move");
		velocity = node.GetValue<Vector3>("velocity");
		railPathOffset = node.GetValue<Vector3>("railPathOffset");
		if (node.HasValue("rallyTfID"))
		{
			int value2 = node.GetValue<int>("rallyTfID");
			rallyTransform = VTScenario.current.waypoints.GetWaypoint(value2).GetTransform();
		}
		fRallyPos = node.GetValue<FixedPoint>("rallyGlobalPos");
	}
}
