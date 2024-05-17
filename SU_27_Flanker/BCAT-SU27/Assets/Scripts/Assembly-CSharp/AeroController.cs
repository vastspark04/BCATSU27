using System;
using System.Collections;
using UnityEngine;

public class AeroController : FlightControlComponent
{
	[Serializable]
	public class ControlSurfaceTransform
	{
		public Transform transform;

		public Vector3 axis;

		public float maxDeflection;

		public float actuatorSpeed;

		public float pitchFactor;

		public float yawFactor;

		public float rollFactor;

		public float brakeFactor;

		public float flapsFactor;

		public float AoAFactor;

		public bool oneDirectional;

		public float trim;

		public float aoaLimit = -1f;

		private Quaternion posRot;

		private Quaternion negRot;

		private bool useAoA;

		private bool noTransform;

		public void Init()
		{
			if (!transform)
			{
				noTransform = true;
			}
			Quaternion localRotation = transform.localRotation;
			Vector3 vector = transform.parent.InverseTransformDirection(transform.TransformDirection(axis));
			posRot = Quaternion.AngleAxis(maxDeflection, vector) * localRotation;
			negRot = Quaternion.AngleAxis(0f - maxDeflection, vector) * localRotation;
			if (Mathf.Abs(AoAFactor) > 0.1f)
			{
				useAoA = true;
			}
		}

		public void Update(Vector3 pitchYawRoll, float brake, float flaps, float AoA, float airspeed, float deltaTime)
		{
			if (noTransform)
			{
				return;
			}
			float num = trim + pitchYawRoll.x * pitchFactor + pitchYawRoll.y * yawFactor + pitchYawRoll.z * rollFactor + flaps * flapsFactor;
			if (airspeed > 0f)
			{
				num += brake * brakeFactor;
			}
			if (useAoA)
			{
				num += AoAFactor * AoA / maxDeflection;
			}
			num = ((!oneDirectional) ? Mathf.Clamp(num, -1f, 1f) : Mathf.Clamp01(num));
			if (aoaLimit > 0f)
			{
				float num2 = AoA / maxDeflection;
				float num3 = aoaLimit / maxDeflection;
				float min = num2 - num3;
				float max = num2 + num3;
				num = Mathf.Clamp(num, min, max);
			}
			num = (num + 1f) / 2f;
			Quaternion to = Quaternion.Slerp(negRot, posRot, num);
			if ((bool)transform)
			{
				Quaternion localRotation = Quaternion.RotateTowards(transform.localRotation, to, actuatorSpeed * deltaTime);
				if (!float.IsNaN(localRotation.x))
				{
					transform.localRotation = localRotation;
				}
			}
			else
			{
				noTransform = true;
			}
		}
	}

	public LODBase lodBase;

	public Battery battery;

	private FlightInfo flightInfo;

	public Vector3 input;

	public Vector3 trim;

	public float brake;

	public float brakeSpeed;

	private float _brake;

	public float brakeAirspeedMin = 15f;

	public float flaps;

	public float flapSpeed;

	private float _flaps;

	private int surfacesCount;

	public ControlSurfaceTransform[] controlSurfaces;

	public UpdateModes updateMode = UpdateModes.Dynamic;

	private Rigidbody rb;

	private bool checkLod;

	private bool doingLowUpdate;

	private Coroutine lowUpdateRoutine;

	private void Start()
	{
		if (controlSurfaces == null)
		{
			base.enabled = false;
			return;
		}
		flightInfo = GetComponentInParent<FlightInfo>();
		rb = GetComponentInParent<Rigidbody>();
		if (!lodBase)
		{
			lodBase = GetComponentInParent<LODBase>();
		}
		if ((bool)lodBase)
		{
			checkLod = true;
		}
		surfacesCount = controlSurfaces.Length;
		for (int i = 0; i < surfacesCount; i++)
		{
			controlSurfaces[i].Init();
		}
	}

	public void SetRandomInputs()
	{
		input = UnityEngine.Random.insideUnitSphere;
	}

	private void Update()
	{
		if (updateMode == UpdateModes.Dynamic)
		{
			DoUpdate(Time.deltaTime);
		}
	}

	private void FixedUpdate()
	{
		if (updateMode == UpdateModes.Fixed)
		{
			DoUpdate(Time.fixedDeltaTime);
		}
	}

	private IEnumerator LowPriorityUpdateRoutine()
	{
		doingLowUpdate = true;
		int frameInterval = 4;
		float deltaTime = Time.fixedDeltaTime;
		WaitForFixedUpdate fixedWait = new WaitForFixedUpdate();
		while (base.enabled)
		{
			if (updateMode == UpdateModes.Dynamic)
			{
				DoUpdate(deltaTime, inLowUpdateRoutine: true);
				deltaTime = 0f;
				for (int j = 0; j < frameInterval; j++)
				{
					deltaTime += Time.deltaTime;
					yield return null;
				}
			}
			else
			{
				DoUpdate(Time.fixedDeltaTime * (float)frameInterval, inLowUpdateRoutine: true);
				for (int j = 0; j < frameInterval; j++)
				{
					yield return fixedWait;
				}
			}
		}
	}

	private void DoUpdate(float deltaTime, bool inLowUpdateRoutine = false)
	{
		if (!inLowUpdateRoutine && checkLod)
		{
			if ((bool)rb && rb.isKinematic && lodBase.sqrDist > 40000f)
			{
				if (!doingLowUpdate)
				{
					lowUpdateRoutine = StartCoroutine(LowPriorityUpdateRoutine());
				}
				return;
			}
			if (doingLowUpdate && lowUpdateRoutine != null)
			{
				StopCoroutine(lowUpdateRoutine);
			}
			doingLowUpdate = false;
		}
		input.x = Mathf.Clamp(input.x, -1f, 1f);
		input.y = Mathf.Clamp(input.y, -1f, 1f);
		input.z = Mathf.Clamp(input.z, -1f, 1f);
		if (!battery || battery.Drain(0.01f * deltaTime))
		{
			brake = Mathf.Clamp01(brake);
			_brake = Mathf.MoveTowards(_brake, brake, brakeSpeed * deltaTime);
			flaps = Mathf.Clamp01(flaps);
			_flaps = Mathf.MoveTowards(_flaps, flaps, flapSpeed * deltaTime);
			float aoA = 0f;
			if ((!flightInfo.isLanded && flightInfo.airspeed > 10f) || (flightInfo.isLanded && flightInfo.surfaceSpeed > 10f))
			{
				Vector3 toDirection = Quaternion.Inverse(rb.rotation) * rb.velocity;
				toDirection.x = 0f;
				aoA = VectorUtils.SignedAngle(Vector3.forward, toDirection, Vector3.up);
			}
			Vector3 pitchYawRoll = input + trim;
			for (int i = 0; i < controlSurfaces.Length; i++)
			{
				controlSurfaces[i].Update(pitchYawRoll, _brake, _flaps, aoA, flightInfo.surfaceSpeed - brakeAirspeedMin, deltaTime);
			}
		}
	}

	public override void SetPitchYawRoll(Vector3 pitchYawRoll)
	{
		input = pitchYawRoll;
	}

	public override void SetFlaps(float flaps)
	{
		this.flaps = flaps;
	}

	public override void SetBrakes(float brakes)
	{
		brake = brakes;
	}

	public override void SetTrim(Vector3 trimPitchYawRoll)
	{
		trim = trimPitchYawRoll;
	}
}
