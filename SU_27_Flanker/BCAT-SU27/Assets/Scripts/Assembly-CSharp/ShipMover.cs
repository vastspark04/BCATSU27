using System;
using System.Collections;
using UnityEngine;

public class ShipMover : MonoBehaviour, IQSVehicleComponent
{
	public Actor actor;

	private Health health;

	public float height;

	public Rigidbody rb;

	public float maxSpeed;

	public float maxTurnRate;

	public float forwardAccel;

	public float steerRate;

	private float stoppingDist;

	private float stoppingTime;

	public float startSpeed;

	private Vector3 steerDirection;

	private Vector3 _velocity;

	private Transform targetTransform;

	public ShipMover formationLeader;

	public int formationIdx;

	private Transform formationTransform;

	public bool isLeader;

	public float formationMaxSpeed;

	public bool sinkOnDeath = true;

	public float sinkDepth;

	public float sinkTime = 10f;

	public Vector3 sinkRotation;

	public ShipGroup shipGroup;

	private Vector3 formationOffset = Vector3.zero;

	private bool engineAlive = true;

	private bool shipAlive = true;

	private bool sinking;

	public FixedPoint fixedPos;

	private float debug_finalMaxSpeed;

	private PID formationPID = new PID(0.1f, 0f, -0.01f, 0f, 0f);

	private Coroutine pathRoutine;

	private Transform p2Target;

	private float p2CurrT;

	public bool debug;

	public float speed { get; private set; }

	public Vector3 velocity => _velocity;

	public Vector3 currentAccel { get; private set; }

	public Waypoint currWpt { get; private set; }

	public FollowPath currPath { get; private set; }

	public void SetPosition(Vector3 worldPos)
	{
		worldPos.y = WaterPhysics.waterHeight + height;
		fixedPos.point = worldPos;
		base.transform.position = worldPos;
	}

	private void Awake()
	{
		stoppingDist = maxSpeed * maxSpeed / (2f * forwardAccel);
		stoppingTime = Mathf.Sqrt(2f * stoppingDist / forwardAccel);
		formationTransform = new GameObject("shipFormationTf").transform;
		health = actor.GetComponent<Health>();
		if ((bool)health)
		{
			health.OnDeath.AddListener(OnDeath);
		}
	}

	private void OnDestroy()
	{
		if ((bool)formationTransform)
		{
			UnityEngine.Object.Destroy(formationTransform.gameObject);
		}
		if ((bool)p2Target)
		{
			UnityEngine.Object.Destroy(p2Target.gameObject);
		}
	}

	private void Start()
	{
		actor.customVelocity = true;
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		base.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
		Vector3 position = base.transform.position;
		position.y = WaterPhysics.instance.height + height;
		base.transform.position = position;
		if (!QuicksaveManager.isQuickload)
		{
			_velocity = startSpeed * base.transform.forward;
			steerDirection = base.transform.forward;
		}
		if ((bool)formationLeader)
		{
			formationOffset = formationLeader.transform.InverseTransformPoint(base.transform.position);
		}
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		fixedPos = new FixedPoint(base.transform.position);
	}

	[ContextMenu("Get Component References")]
	public void GetComponentReferences()
	{
		rb = GetComponent<Rigidbody>();
		actor = GetComponent<Actor>();
	}

	private void OnDeath()
	{
		if (sinkOnDeath)
		{
			StartCoroutine(SinkRoutine());
		}
		if ((bool)shipGroup)
		{
			shipGroup.RemoveShip(this);
		}
		shipAlive = false;
		engineAlive = false;
	}

	private IEnumerator SinkRoutine()
	{
		Quaternion startRot = base.transform.rotation;
		Quaternion targetRot = Quaternion.AngleAxis(sinkRotation.z, base.transform.forward) * Quaternion.AngleAxis(sinkRotation.x, base.transform.right) * Quaternion.AngleAxis(sinkRotation.y, base.transform.up) * startRot;
		_velocity.y = (0f - sinkDepth) * (1f / sinkTime);
		sinking = true;
		float t = 0f;
		while (t < 1f)
		{
			t = Mathf.MoveTowards(t, 1f, 1f / sinkTime * Time.fixedDeltaTime);
			Quaternion rot = Quaternion.Slerp(startRot, targetRot, t);
			rb.MoveRotation(rot);
			yield return new WaitForFixedUpdate();
		}
		_velocity.y = 0f;
		rb.interpolation = RigidbodyInterpolation.None;
	}

	private void FixedUpdate()
	{
		Vector3 vector = _velocity;
		if (shipAlive)
		{
			float num = maxSpeed;
			if (isLeader)
			{
				num = formationMaxSpeed;
			}
			float num2 = num;
			Transform transform = targetTransform;
			bool flag = false;
			if ((bool)shipGroup && (bool)formationLeader && !isLeader)
			{
				transform = formationTransform;
				flag = true;
				formationTransform.parent = formationLeader.transform;
				shipGroup.SetFormationPosition(this, formationTransform);
			}
			if ((bool)transform)
			{
				Vector3 position = transform.position;
				if (flag)
				{
					float magnitude = (position - base.transform.position).magnitude;
					position += formationLeader.velocity * stoppingTime;
					float magnitude2 = formationLeader.velocity.magnitude;
					formationPID.updateMode = UpdateModes.Fixed;
					num2 = magnitude2 + formationPID.Evaluate(Vector3.Dot(transform.position - base.transform.position, -formationLeader.velocity.normalized), 0f);
					num2 = Mathf.Clamp(num2, 0f, num);
					if (num2 < magnitude2 / 2f && Vector3.Dot(position - base.transform.position, formationLeader.velocity) > 0f && magnitude2 > 1f)
					{
						num2 = magnitude2 / 2f;
					}
					if (num2 > 1f)
					{
						if (engineAlive)
						{
							float num3 = magnitude / maxSpeed;
							position += formationLeader.velocity * num3;
							Vector3 vector2 = position - base.transform.position;
							vector2.y = 0f;
							vector2 = vector2.normalized;
							_velocity.y = 0f;
							speed = Mathf.MoveTowards(_velocity.magnitude, num2, forwardAccel * Time.fixedDeltaTime);
							_velocity = _velocity.normalized * speed;
							steerDirection = Vector3.RotateTowards(steerDirection, vector2, (float)Math.PI / 180f * maxTurnRate * Time.fixedDeltaTime, 0f).normalized;
							_velocity = Vector3.Slerp(_velocity, steerDirection.normalized * speed, steerRate * Time.fixedDeltaTime);
							_velocity = Vector3.ClampMagnitude(_velocity, num);
							rb.MoveRotation(Quaternion.LookRotation(_velocity, Vector3.up));
						}
						else
						{
							steerDirection = base.transform.forward;
							_velocity = Vector3.MoveTowards(_velocity, Vector3.zero, forwardAccel * Time.fixedDeltaTime);
						}
					}
					else
					{
						steerDirection = base.transform.forward;
						_velocity = Vector3.MoveTowards(_velocity, Vector3.zero, forwardAccel * Time.fixedDeltaTime);
					}
				}
				else
				{
					position.y = WaterPhysics.instance.height;
					if ((position - base.transform.position).sqrMagnitude > stoppingDist * stoppingDist)
					{
						if (engineAlive)
						{
							_velocity += forwardAccel * Time.fixedDeltaTime * base.transform.forward;
						}
						else
						{
							_velocity = Vector3.MoveTowards(_velocity, Vector3.zero, forwardAccel * Time.fixedDeltaTime);
						}
						if (_velocity.sqrMagnitude > 1f)
						{
							Vector3 target = position - base.transform.position;
							steerDirection = Vector3.RotateTowards(steerDirection, target, maxTurnRate * ((float)Math.PI / 180f) * Time.fixedDeltaTime, 0f);
							_velocity = Vector3.Slerp(_velocity, steerDirection.normalized * _velocity.magnitude, steerRate * Time.fixedDeltaTime);
							_velocity = Vector3.ClampMagnitude(_velocity, num);
							rb.MoveRotation(Quaternion.LookRotation(_velocity, Vector3.up));
						}
					}
					else
					{
						steerDirection = base.transform.forward;
						_velocity = Vector3.MoveTowards(_velocity, Vector3.zero, forwardAccel * Time.fixedDeltaTime);
					}
				}
			}
			else
			{
				steerDirection = base.transform.forward;
				_velocity = Vector3.MoveTowards(_velocity, Vector3.zero, forwardAccel * Time.fixedDeltaTime);
			}
			debug_finalMaxSpeed = num;
		}
		else
		{
			Vector3 current = _velocity;
			current.y = 0f;
			current = Vector3.MoveTowards(current, Vector3.zero, forwardAccel * Time.fixedDeltaTime);
			_velocity.x = current.x;
			_velocity.z = current.z;
		}
		actor.SetCustomVelocity(_velocity);
		rb.velocity = _velocity;
		Vector3D globalPoint = fixedPos.globalPoint + new Vector3D(_velocity * Time.fixedDeltaTime);
		if (!sinking)
		{
			globalPoint.y = -5f + height;
		}
		fixedPos.globalPoint = globalPoint;
		rb.MovePosition(fixedPos.point);
		currentAccel = (_velocity - vector) / Mathf.Max(Time.fixedDeltaTime, 0.001f);
	}

	public void MoveTo(Waypoint target)
	{
		formationLeader = null;
		StopPathRoutine();
		targetTransform = target.GetTransform();
		currWpt = target;
		currPath = null;
	}

	public void MovePath(FollowPath path)
	{
		if (!path)
		{
			Debug.LogError("ShipMover was commanded to move on a NULL path! (" + actor.actorName + ")", base.gameObject);
			return;
		}
		StopPathRoutine();
		currPath = path;
		currWpt = null;
		pathRoutine = StartCoroutine(PathRoutine2(path));
	}

	private void OnEnable()
	{
		if ((bool)currPath)
		{
			MovePath(currPath);
		}
	}

	public void StopPathRoutine()
	{
		if (pathRoutine != null)
		{
			StopCoroutine(pathRoutine);
			pathRoutine = null;
		}
		currPath = null;
	}

	private IEnumerator PathRoutine(FollowPath path)
	{
		currPath = path;
		int num = 0;
		float num2 = float.MaxValue;
		int num3 = 0;
		float num4 = float.MaxValue;
		for (int j = 0; j < path.pointTransforms.Length; j++)
		{
			float sqrMagnitude = (path.pointTransforms[j].position - base.transform.position).sqrMagnitude;
			if (sqrMagnitude < num2)
			{
				num = j;
				num2 = sqrMagnitude;
			}
			else if (sqrMagnitude < num4)
			{
				num3 = j;
				num4 = sqrMagnitude;
			}
		}
		float num5 = Vector3.Dot((path.pointTransforms[num].position - base.transform.position).normalized, base.transform.forward);
		float num6 = Vector3.Dot((path.pointTransforms[num3].position - base.transform.position).normalized, base.transform.forward);
		int num7 = ((num5 > num6) ? num : num3);
		if (path.loop && num7 == path.pointTransforms.Length - 1 && (num == 0 || num3 == 0))
		{
			num7 = 0;
		}
		float moveSqrRad = Mathf.Pow(2f * stoppingDist, 2f);
		for (int i = num7; i < path.pointTransforms.Length; i++)
		{
			targetTransform = path.pointTransforms[i];
			while ((targetTransform.position - base.transform.position).sqrMagnitude > moveSqrRad)
			{
				yield return null;
			}
			yield return null;
			if (i == path.pointTransforms.Length - 1 && path.loop)
			{
				i = 0;
			}
		}
	}

	private IEnumerator PathRoutine2(FollowPath path)
	{
		Debug.LogFormat("{0} starting ShipMover.PathRoutine2", actor.DebugName());
		WaitForSeconds wait = new WaitForSeconds(1f);
		currPath = path;
		if (p2Target == null)
		{
			p2Target = new GameObject(base.gameObject.name + " path target").transform;
			FloatingOrigin.instance.AddTransform(p2Target);
		}
		targetTransform = p2Target;
		yield return null;
		float stopT = 1f - stoppingDist / path.GetApproximateLength();
		int iterations = Mathf.RoundToInt(Mathf.Max(3f, currPath.GetApproximateLength() / 20000f));
		Debug.Log($"{actor.DebugName()} ShipMover.PathRoutine2 stopping dist: {stoppingDist}, stopT: {stopT}, iterations: {iterations}");
		p2CurrT = 0f;
		while (p2CurrT < stopT)
		{
			p2Target.position = currPath.GetFollowPoint(base.transform.position, 1.5f * stoppingDist, out p2CurrT, iterations);
			if (p2CurrT >= stopT)
			{
				if (!path.loop)
				{
					p2Target.position = path.GetWorldPoint(1f);
					Debug.LogFormat("{0} stopping ShipMover.PathRoutine2 -- reached end", actor.DebugName());
					currPath = null;
					break;
				}
				p2CurrT = 1f - stopT;
				p2Target.position = currPath.GetWorldPoint(p2CurrT);
			}
			yield return wait;
		}
	}

	public void KillEngine()
	{
		engineAlive = false;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode("ShipMover_" + base.gameObject.name);
		qsNode.AddNode(configNode);
		configNode.SetValue("fixedPos", fixedPos);
		configNode.SetValue("velocity", velocity);
		configNode.SetValue("steerDirection", steerDirection);
		if (pathRoutine != null && currPath != null)
		{
			configNode.SetValue("pathID", currPath.scenarioPathID);
		}
		else if (currWpt != null)
		{
			configNode.SetValue("wptID", currWpt.id);
		}
		configNode.SetValue("isLeader", isLeader);
		configNode.SetValue("formationMaxSpeed", formationMaxSpeed);
		if (formationLeader != null)
		{
			configNode.SetValue("formationIdx", formationIdx);
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(formationLeader.actor, "formationLeader"));
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("ShipMover_" + base.gameObject.name);
		if (node == null)
		{
			return;
		}
		_velocity = node.GetValue<Vector3>("velocity");
		steerDirection = node.GetValue<Vector3>("steerDirection");
		if (steerDirection.sqrMagnitude < 0.5f)
		{
			steerDirection = base.transform.forward;
		}
		fixedPos = node.GetValue<FixedPoint>("fixedPos");
		if (node.HasValue("pathID"))
		{
			int value = node.GetValue<int>("pathID");
			MovePath(VTScenario.current.paths.GetPath(value));
		}
		else if (node.HasValue("wptID"))
		{
			int value2 = node.GetValue<int>("wptID");
			MoveTo(VTScenario.current.waypoints.GetWaypoint(value2));
		}
		isLeader = node.GetValue<bool>("isLeader");
		formationMaxSpeed = node.GetValue<float>("formationMaxSpeed");
		ConfigNode node2 = node.GetNode("formationLeader");
		if (node2 != null)
		{
			Actor actor = QuicksaveManager.RetrieveActorFromNode(node2);
			if (actor != null)
			{
				formationLeader = actor.GetComponent<ShipMover>();
			}
		}
	}
}
