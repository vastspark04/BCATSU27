using UnityEngine;
using UnityEngine.Events;

public class RaySpringDamper : MonoBehaviour, IParentRBDependent
{
	public enum RayTypes
	{
		Ray,
		Sphere
	}

	public delegate void WheelSkidDelegate(float skidAmount);

	public Rigidbody rb;

	public LayerMask layerMask;

	public bool raycastWhileKinematic;

	[Header("Suspension")]
	public RayTypes rayType;

	public float sphereRadius = 1f;

	public float springConstant;

	public float springDamper;

	public float suspensionDistance;

	public float minDistance;

	public float preload;

	public float maxRelV = -1f;

	public Vector3 origin;

	private bool touching;

	public float friction;

	public SOCurve frictionCurve;

	[Header("Wheel")]
	public bool wheel;

	public float frictionLateral;

	public float brakeForce;

	public float brakePedal;

	public float minBrakePedal;

	public float maxWheelForce = -1f;

	public bool brakeLock;

	public Vector3Event OnContact;

	public FloatEvent OnImpact;

	public UnityEvent OnLiftOff;

	private float previousDistance;

	private Vector3 nrm;

	private FixedPoint rayHitPoint;

	private Vector3 surfaceRelV;

	private Ray ray;

	private float distance;

	private Transform myTransform;

	public bool sampleCityStreets;

	private Collider lastCollider;

	private MovingPlatform lastMovingPlatform;

	[Header("Brake Anchor")]
	public bool brakeAnchor;

	public float brakeAnchorSpeedThreshold = 0.1f;

	private Transform anchorTransform;

	private Vector3 anchorLocalPos;

	public PID3 anchorPID = new PID3(5f, 1f, 0.5f, 1f);

	private bool brakeAnchored;

	private bool rearmAnchor;

	private float timeAnchorPaused;

	private float anchorPauseDuration;

	private bool wasAnchorPaused;

	public bool isTouching
	{
		get
		{
			if (base.enabled)
			{
				return touching;
			}
			return false;
		}
	}

	public float wheelSpeed { get; private set; }

	public Vector3 normal => nrm;

	public Vector3 point
	{
		get
		{
			if (base.enabled && (raycastWhileKinematic || !rb.isKinematic))
			{
				return rayHitPoint.point;
			}
			return myTransform.TransformPoint(origin) - myTransform.up * suspensionDistance;
		}
	}

	public Vector3 surfaceVelocity => surfaceRelV;

	public WheelSurfaceMaterial surfaceMaterial { get; private set; }

	public Vector3 currentWheelForce { get; private set; }

	public Collider touchingCollider => lastCollider;

	public MovingPlatform touchingPlatform => lastMovingPlatform;

	private bool isAnchorPaused => Time.time - timeAnchorPaused < anchorPauseDuration;

	public event WheelSkidDelegate OnWheelSkid;

	public void ToggleBrakeLock()
	{
		brakeLock = !brakeLock;
	}

	private void Awake()
	{
		myTransform = base.transform;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Vector3 vector = base.transform.TransformPoint(origin);
		if (rayType == RayTypes.Ray)
		{
			Gizmos.DrawLine(vector, vector - suspensionDistance * base.transform.up);
			Gizmos.DrawCube(vector - suspensionDistance * base.transform.up, new Vector3(1f, 0.01f, 1f));
		}
		else
		{
			Gizmos.DrawLine(vector + sphereRadius * Vector3.right, vector + sphereRadius * Vector3.right - suspensionDistance * base.transform.up);
			Gizmos.DrawLine(vector - sphereRadius * Vector3.right, vector - sphereRadius * Vector3.right - suspensionDistance * base.transform.up);
			Gizmos.DrawWireSphere(vector - suspensionDistance * base.transform.up, sphereRadius);
		}
		if (minDistance > 0f)
		{
			Gizmos.DrawCube(base.transform.position - minDistance * base.transform.up, 0.1f * Vector3.one);
		}
		if (Application.isPlaying)
		{
			Gizmos.color = Color.red - new Color(0f, 0f, 0f, 0.5f);
			Gizmos.DrawCube(point, new Vector3(2f, 0.05f, 2f));
		}
	}

	private void Start()
	{
		if (!rb)
		{
			rb = GetComponentInParent<Rigidbody>();
			if (!rb)
			{
				Debug.LogWarning("RaySpringDamper: No rigidbody found in parent.");
				base.enabled = false;
			}
		}
		previousDistance = suspensionDistance;
		ray = new Ray(base.transform.TransformPoint(origin), -base.transform.up);
	}

	private void FixedUpdate()
	{
		SuspensionUpdate();
	}

	private void SuspensionUpdate()
	{
		if (raycastWhileKinematic || !rb.isKinematic)
		{
			ray = new Ray(myTransform.TransformPoint(origin), -myTransform.up);
			float num = Mathf.Max(0f, 0f - rb.velocity.y) * Time.fixedDeltaTime;
			if ((rayType != 0) ? Physics.SphereCast(ray, sphereRadius, out var hitInfo, suspensionDistance + num, layerMask.value) : Physics.Raycast(ray, out hitInfo, suspensionDistance + num, layerMask.value))
			{
				rayHitPoint.point = hitInfo.point;
				distance = hitInfo.distance;
				nrm = hitInfo.normal;
				WheelSurfaceMaterial material = null;
				bool flag = WheelSurface.TryGetMaterial(hitInfo.collider, out material);
				if (flag && sampleCityStreets && VTMapManager.IsPositionOverCityStreet(hitInfo.point))
				{
					flag = false;
					material = null;
				}
				if (flag)
				{
					Vector3D vector3D = VTMapManager.WorldToGlobalPoint(hitInfo.point) * material.bumpScale;
					distance += VectorUtils.FullRangePerlinNoise((float)vector3D.x, (float)vector3D.z) * material.bumpiness;
					surfaceMaterial = material;
				}
				else
				{
					surfaceMaterial = null;
				}
				float num2 = (distance - previousDistance) / Time.fixedDeltaTime;
				previousDistance = distance;
				if (!touching)
				{
					touching = true;
					if (OnContact != null)
					{
						OnContact.Invoke(point);
					}
					if (OnImpact != null)
					{
						OnImpact.Invoke(num2);
					}
				}
				if (maxRelV > 0f)
				{
					num2 = Mathf.Clamp(num2, 0f - maxRelV, maxRelV);
				}
				Vector3 zero = Vector3.zero;
				MovingPlatform movingPlatform;
				if (hitInfo.collider == lastCollider)
				{
					movingPlatform = lastMovingPlatform;
				}
				else
				{
					lastCollider = hitInfo.collider;
					movingPlatform = (lastMovingPlatform = lastCollider.GetComponent<MovingPlatform>());
				}
				surfaceRelV = rb.GetPointVelocity(point);
				if ((bool)movingPlatform)
				{
					zero = movingPlatform.GetVelocity(point);
					surfaceRelV -= zero;
					zero = Vector3.ProjectOnPlane(zero, nrm);
				}
				if (!rb.isKinematic)
				{
					float num3 = springConstant * (preload + suspensionDistance - Mathf.Min(distance, suspensionDistance)) - springDamper * num2;
					Vector3 vector = normal;
					rb.AddForceAtPosition(Vector3.Project(num3 * myTransform.up, vector), ray.origin);
					if ((bool)hitInfo.collider.attachedRigidbody)
					{
						hitInfo.collider.attachedRigidbody.AddForceAtPosition(num3 * -vector, hitInfo.point);
					}
					if (minDistance > 0f && distance < minDistance)
					{
						Vector3 vector2 = Vector3.Project(rb.velocity, myTransform.up);
						rb.velocity -= vector2;
					}
					if (!(friction > 0f))
					{
						return;
					}
					Vector3 vector3 = surfaceRelV;
					vector3 = Vector3.ProjectOnPlane(vector3, nrm);
					_ = vector3.magnitude;
					if (!wheel)
					{
						float num4 = 1f;
						_ = frictionCurve != null;
						Vector3 force = -vector3.normalized * Mathf.Clamp01(vector3.magnitude) * num4 * friction * num3;
						rb.AddForceAtPosition(force, point);
						return;
					}
					Vector3 vector4 = Vector3.Project(vector3, Vector3.ProjectOnPlane(myTransform.right, normal).normalized);
					Vector3 normalized = Vector3.ProjectOnPlane(myTransform.forward, normal).normalized;
					Vector3 vector5 = Vector3.Project(vector3, normalized);
					float num5 = 1f;
					float num6 = 1f;
					if (frictionCurve != null)
					{
						num6 = frictionCurve.Evaluate(vector5.magnitude);
					}
					float magnitude = vector5.magnitude;
					wheelSpeed = Vector3.Dot(vector3, normalized);
					float magnitude2 = vector4.magnitude;
					Vector3 vector6 = -vector4.normalized * Mathf.Clamp01(magnitude2) * num5 * frictionLateral * num3;
					float num7 = (brakeLock ? 1f : Mathf.Clamp(brakePedal, minBrakePedal, 1f));
					float num8 = num7;
					if (flag)
					{
						num7 *= material.traction;
						vector6 *= material.traction;
					}
					Vector3 vector7 = -vector5.normalized * Mathf.Clamp01(magnitude) * num6 * friction * ((magnitude > 0.5f) ? brakeForce : 1f) * num3 * num7;
					Vector3 vector8 = vector6 + vector7;
					if (maxWheelForce > 0f)
					{
						vector8 = Vector3.ClampMagnitude(vector8, maxWheelForce);
					}
					if (flag)
					{
						vector8 -= material.resistance * vector3;
					}
					else if (this.OnWheelSkid != null)
					{
						float num9 = Mathf.Max(0f, vector7.magnitude - maxWheelForce);
						this.OnWheelSkid(magnitude2 + num9);
					}
					currentWheelForce = vector8;
					rb.AddForceAtPosition(vector8, point);
					if (brakeAnchor)
					{
						if (num8 > 0.999f && !brakeAnchored && Mathf.Abs(wheelSpeed) < brakeAnchorSpeedThreshold)
						{
							CreateBrakeAnchor();
						}
						if (num8 < 0.95f && brakeAnchored)
						{
							ReleaseBrakeAnchor();
						}
						if (brakeAnchored)
						{
							UpdateBrakeAnchor();
						}
					}
					else if (brakeAnchored)
					{
						ReleaseBrakeAnchor();
					}
				}
				else if (wheel)
				{
					Vector3 vector9 = surfaceRelV;
					vector9 = Vector3.ProjectOnPlane(vector9, nrm);
					Vector3 normalized2 = Vector3.ProjectOnPlane(myTransform.forward, normal).normalized;
					wheelSpeed = Vector3.Dot(vector9, normalized2);
				}
				return;
			}
			if (touching)
			{
				if (OnLiftOff != null)
				{
					OnLiftOff.Invoke();
				}
				this.OnWheelSkid?.Invoke(0f);
				lastMovingPlatform = null;
			}
			touching = false;
			previousDistance = suspensionDistance;
			rayHitPoint.point = ray.GetPoint(suspensionDistance);
			surfaceRelV = rb.velocity;
			lastCollider = null;
			surfaceMaterial = null;
			currentWheelForce = Vector3.zero;
			if (wheel)
			{
				wheelSpeed = Mathf.Lerp(wheelSpeed, 0f, 2f * Time.fixedDeltaTime);
			}
			if (brakeAnchor)
			{
				ReleaseBrakeAnchor();
			}
		}
		else
		{
			currentWheelForce = Vector3.zero;
		}
	}

	private void CreateBrakeAnchor()
	{
		if (!brakeAnchored && (bool)lastCollider)
		{
			anchorTransform = lastCollider.transform;
			brakeAnchored = true;
			anchorLocalPos = anchorTransform.InverseTransformPoint(point);
			anchorPID.ResetIntegrator();
		}
	}

	private void ReleaseBrakeAnchor()
	{
		if (!rearmAnchor)
		{
			brakeAnchored = false;
			anchorPID.ResetIntegrator();
		}
	}

	private void UpdateBrakeAnchor()
	{
		if (!anchorTransform || !brakeAnchored || Mathf.Abs(wheelSpeed) > brakeAnchorSpeedThreshold * 1.5f)
		{
			ReleaseBrakeAnchor();
			return;
		}
		if (isAnchorPaused)
		{
			wasAnchorPaused = true;
		}
		else if (wasAnchorPaused)
		{
			wasAnchorPaused = false;
			ReleaseBrakeAnchor();
			CreateBrakeAnchor();
		}
		Vector3 vector = Vector3.ProjectOnPlane(anchorLocalPos - anchorTransform.InverseTransformPoint(point), anchorTransform.InverseTransformDirection(normal));
		vector = Vector3.ClampMagnitude(vector, 1f);
		anchorPID.updateMode = UpdateModes.Fixed;
		anchorPID.kp = rb.mass * VTOLVRConstants.PHYS_BRAKE_ANCHOR_P_FACTOR;
		anchorPID.kd = rb.mass * VTOLVRConstants.PHYS_BRAKE_ANCHOR_D_FACTOR;
		anchorPID.ki = 1f;
		anchorPID.iMax = 1f;
		Vector3 vector2 = anchorPID.Evaluate(-vector, Vector3.zero);
		vector2 = anchorTransform.TransformVector(vector2);
		rb.AddForceAtPosition(vector2, point);
	}

	public void SetRearmAnchor()
	{
		brakeAnchor = true;
		rearmAnchor = true;
	}

	public void ReleaseRearmAnchor()
	{
		rearmAnchor = false;
	}

	public void PauseAnchor(float time)
	{
		timeAnchorPaused = Time.time;
		anchorPauseDuration = time;
		wasAnchorPaused = true;
	}

	private void LateUpdate()
	{
		ray = new Ray(base.transform.TransformPoint(origin), -base.transform.up);
		if (!touching)
		{
			rayHitPoint.point = ray.GetPoint(suspensionDistance);
		}
		else
		{
			rayHitPoint.point = ray.GetPoint(distance);
		}
	}

	private void OnDisable()
	{
		wheelSpeed = 0f;
		ReleaseBrakeAnchor();
	}

	public void SetBrakePedal(float t)
	{
		brakePedal = t;
	}

	public void SetParentRigidbody(Rigidbody newRB)
	{
		float num = -1f;
		if ((bool)rb)
		{
			num = rb.mass;
		}
		rb = newRB;
		if (!newRB)
		{
			Debug.LogError("Set parent rigidbody of RaySpringDamper to null. Disabling.");
			base.enabled = false;
		}
		else if (num > 0f)
		{
			float num2 = newRB.mass / num;
			if (maxWheelForce > 0f)
			{
				maxWheelForce *= num2;
			}
			springConstant *= num2;
			springDamper *= num2;
		}
	}
}
