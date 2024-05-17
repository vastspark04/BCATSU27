using UnityEngine;

public class FlightInfo : MonoBehaviour
{
	public Rigidbody rb;

	public Transform aoaReferenceTf;

	public Transform playerGTransform;

	public float maxRadarAlt = 2000f;

	public float radarAltOffset;

	private Vector3 lastPlayerV;

	private Vector3 lastVehicleV;

	public RaySpringDamper[] suspensions;

	public WheelsController wheelsController;

	private bool gCalculationsPaused;

	private float maxPlayerAccel;

	private Transform myTransform;

	private bool useWc;

	private int raycastFrame = -1;

	private const int raycastInterval = 4;

	private static int raycastStartFrame;

	public bool fwdSweepRadarAlt;

	private float radarSweepT;

	private const int SWEEP_ARR_LENGTH = 10;

	private float[] sweepSurfAlts = new float[10];

	private float surfAlt;

	private float sweptSurfAlt;

	private bool remote;

	private const int AVG_ACCEL_COUNT = 5;

	private Vector3[] avgAccelVelocities = new Vector3[5];

	private int avgAccelIdx;

	public float pitch { get; private set; }

	public float roll { get; private set; }

	public float altitudeASL { get; private set; }

	public float radarAltitude { get; private set; }

	public float sweptRadarAltitude { get; private set; }

	public float heading { get; private set; }

	public float airspeed { get; private set; }

	public float verticalSpeed { get; private set; }

	public Vector3 acceleration { get; private set; }

	public float aoa { get; private set; }

	public float playerGs { get; private set; }

	public float maxInstantaneousG { get; private set; }

	public float currentInstantaneousG { get; private set; }

	public Vector3 planarForward { get; private set; }

	public bool isLanded { get; private set; }

	public Vector3 surfaceVelocity { get; private set; }

	public float surfaceSpeed { get; private set; }

	public Vector3 surfaceNormal { get; private set; }

	public Collider surfaceCollider { get; private set; }

	public Vector3 averagedAccel { get; private set; }

	public Vector3 pilotAccel { get; private set; }

	private void Awake()
	{
		myTransform = base.transform;
		if (!rb)
		{
			rb = GetComponentInParent<Rigidbody>();
		}
		if (!aoaReferenceTf)
		{
			aoaReferenceTf = rb.transform;
		}
		Actor component = GetComponent<Actor>();
		if ((bool)component)
		{
			component.flightInfo = this;
		}
		useWc = wheelsController != null;
		if (!playerGTransform)
		{
			playerGTransform = base.transform;
		}
	}

	private void Update()
	{
		UpdateCalculations();
	}

	private void UpdateCalculations()
	{
		altitudeASL = WaterPhysics.GetAltitude(myTransform.position);
		Vector3 forward = myTransform.forward;
		forward.y = 0f;
		planarForward = forward.normalized;
		pitch = VectorUtils.SignedAngle(forward, myTransform.forward, Vector3.up);
		Vector3 toDirection = Vector3.ProjectOnPlane(myTransform.up, forward);
		roll = VectorUtils.SignedAngle(Vector3.up, toDirection, Vector3.Cross(Vector3.up, forward));
		heading = VectorUtils.SignedAngle(Vector3.forward, forward, Vector3.right);
		if (heading < 0f)
		{
			heading += 360f;
		}
		if (raycastFrame == -1 || raycastFrame == Time.frameCount % 4)
		{
			if (raycastFrame == -1)
			{
				raycastFrame = raycastStartFrame;
				raycastStartFrame = (raycastStartFrame + 1) % 4;
			}
			Vector3 position = myTransform.position;
			if (fwdSweepRadarAlt)
			{
				position += 4f * radarSweepT * rb.velocity;
				radarSweepT = Mathf.Repeat(radarSweepT + Time.deltaTime, 1f);
			}
			surfAlt = GetSurfaceAlt(position);
			if (fwdSweepRadarAlt)
			{
				int num = Mathf.RoundToInt(radarSweepT * 9f);
				sweepSurfAlts[num] = surfAlt;
				float num2 = 0f;
				for (int i = 0; i < 10; i++)
				{
					num2 = Mathf.Max(surfAlt, sweepSurfAlts[i]);
				}
				sweptSurfAlt = num2;
			}
			else
			{
				sweptSurfAlt = surfAlt;
			}
		}
		radarAltitude = radarAltOffset + altitudeASL - surfAlt;
		sweptRadarAltitude = radarAltOffset + altitudeASL - sweptSurfAlt;
		surfaceVelocity = rb.velocity;
		if (remote)
		{
			return;
		}
		isLanded = false;
		if (useWc)
		{
			isLanded = wheelsController.landed;
			if (isLanded)
			{
				surfaceVelocity = wheelsController.surfaceVelocity;
			}
		}
		else
		{
			if (suspensions == null)
			{
				return;
			}
			for (int j = 0; j < suspensions.Length; j++)
			{
				if (suspensions[j] != null && suspensions[j].isTouching)
				{
					surfaceVelocity = suspensions[j].surfaceVelocity;
					isLanded = true;
					break;
				}
			}
		}
	}

	public void RemoteSetIsLanded(bool l)
	{
		isLanded = l;
		remote = true;
	}

	public void SetRemote(bool r)
	{
		remote = r;
	}

	public void ForceUpdateRadarAltitude()
	{
		altitudeASL = WaterPhysics.GetAltitude(myTransform.position);
		float surfaceAlt = GetSurfaceAlt(myTransform.position);
		radarAltitude = radarAltOffset + altitudeASL - surfaceAlt;
	}

	private float GetSurfaceAlt(Vector3 rPos)
	{
		if ((bool)VTMapGenerator.fetch && !VTMapGenerator.fetch.IsPositionOutOfBounds(rPos) && !VTMapGenerator.fetch.IsChunkColliderEnabled(rPos))
		{
			float heightmapAltitude = VTMapGenerator.fetch.GetHeightmapAltitude(rPos);
			surfaceNormal = Vector3.up;
			surfaceCollider = null;
			return Mathf.Max(0f, heightmapAltitude);
		}
		if (Physics.Raycast(rPos, Vector3.down, out var hitInfo, maxRadarAlt, 1, QueryTriggerInteraction.Ignore))
		{
			float altitude = WaterPhysics.GetAltitude(hitInfo.point);
			surfaceNormal = hitInfo.normal;
			surfaceCollider = hitInfo.collider;
			return Mathf.Max(0f, altitude);
		}
		surfaceNormal = Vector3.up;
		surfaceCollider = null;
		return 0f;
	}

	public void ForceUpdateNow()
	{
		UpdateCalculations();
		FixedUpdateCalculations();
	}

	private void FixedUpdate()
	{
		FixedUpdateCalculations();
	}

	private void FixedUpdateCalculations()
	{
		airspeed = rb.velocity.magnitude;
		surfaceSpeed = surfaceVelocity.magnitude;
		verticalSpeed = rb.velocity.y;
		aoa = 0f - VectorUtils.SignedAngle(aoaReferenceTf.forward, Vector3.ProjectOnPlane(rb.velocity, aoaReferenceTf.right), aoaReferenceTf.up);
		UpdatePlayerGs();
	}

	private void UpdatePlayerGs()
	{
		if (!playerGTransform)
		{
			playerGTransform = base.transform;
		}
		if (gCalculationsPaused || remote)
		{
			Vector3 vector3 = (averagedAccel = (pilotAccel = acceleration));
			avgAccelVelocities[avgAccelIdx] = rb.velocity;
			avgAccelIdx = (avgAccelIdx + 1) % 5;
		}
		else
		{
			Vector3 pointVelocity = rb.GetPointVelocity(playerGTransform.position);
			Vector3 vector3 = (rb.velocity - lastVehicleV) / Time.fixedDeltaTime;
			pilotAccel = (pointVelocity - lastPlayerV) / Time.fixedDeltaTime;
			acceleration = vector3;
			lastPlayerV = pointVelocity;
			lastVehicleV = rb.velocity;
			avgAccelVelocities[avgAccelIdx] = rb.velocity;
			averagedAccel = Vector3.zero;
			for (int i = 0; i < 5; i++)
			{
				Vector3 vector4 = avgAccelVelocities[(avgAccelIdx + (5 - i)) % 5];
				Vector3 vector5 = avgAccelVelocities[(avgAccelIdx + (5 - (i + 1))) % 5];
				averagedAccel += (vector4 - vector5) / Time.fixedDeltaTime;
			}
			averagedAccel /= 5f;
			avgAccelIdx = (avgAccelIdx + 1) % 5;
		}
		float magnitude = pilotAccel.magnitude;
		if (magnitude > maxPlayerAccel)
		{
			maxPlayerAccel = magnitude;
			maxInstantaneousG = maxPlayerAccel / 9.81f;
		}
		Vector3 lhs = Vector3.Project(pilotAccel - Physics.gravity, playerGTransform.up);
		float num2 = (currentInstantaneousG = lhs.magnitude / 9.81f);
		float num3 = num2;
		num3 = (playerGs = num3 * Mathf.Sign(Vector3.Dot(lhs, playerGTransform.up)));
	}

	public void PauseGCalculations()
	{
		if (!gCalculationsPaused)
		{
			gCalculationsPaused = true;
			playerGs = 1f;
			acceleration = Vector3.zero;
		}
	}

	public void UnpauseGCalculations()
	{
		if (gCalculationsPaused)
		{
			gCalculationsPaused = false;
			if ((bool)playerGTransform)
			{
				lastPlayerV = rb.GetRelativePointVelocity(rb.transform.InverseTransformPoint(playerGTransform.position));
				lastVehicleV = rb.velocity;
			}
		}
	}

	public void OverrideRecordedAcceleration(Vector3 accel)
	{
		if (!gCalculationsPaused && !remote)
		{
			Debug.Log("Flight info acceleration can only be overridden when G calculations are paused!");
			return;
		}
		acceleration = accel;
		pilotAccel = accel;
	}
}
