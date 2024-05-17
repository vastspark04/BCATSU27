using System;
using UnityEngine;

public class Wing : MonoBehaviour, IParentRBDependent
{
	[Serializable]
	public class WingSoundEffect
	{
		public AudioSource audioSource;

		public AnimationCurve volumeForce;

		public AnimationCurve volumeSpeed;

		public AnimationCurve pitchForce;

		public AnimationCurve pitchSpeed;

		public float interiorMult = 0.45f;

		public void Update(float force, float speed)
		{
			force = Mathf.Abs(force);
			float num = volumeForce.Evaluate(force) * volumeSpeed.Evaluate(speed);
			if (num > 0f)
			{
				if (!audioSource.isPlaying)
				{
					audioSource.Play();
				}
				audioSource.volume = num;
				if ((bool)AudioController.instance && AudioController.instance.exteriorLevel < 1f)
				{
					audioSource.volume *= Mathf.Lerp(interiorMult, 1f, AudioController.instance.exteriorLevel);
				}
				audioSource.pitch = pitchForce.Evaluate(force) * pitchSpeed.Evaluate(speed);
			}
			else if (audioSource.isPlaying)
			{
				audioSource.Stop();
			}
		}
	}

	public const float MIN_WING_AIRSPEED = 20f;

	public const float MIN_WING_AIRSPEED_SQR = 400f;

	public bool usePointVelocity = true;

	public float liftCoefficient;

	public float dragCoefficient;

	public float liftArea;

	public bool useManualOffset;

	public Vector3 manualOffset;

	public Rigidbody rb;

	private Transform wingTransform;

	public WingSoundEffect[] soundEffects;

	private bool hasSoundEffects;

	public Aerodynamics aeroProfile;

	public bool useBuffet;

	private float buffetRand;

	private float liftConstant;

	private float dragConstant;

	private Vector3 localOffset;

	private bool dragCamShake;

	private float dragShakeFactor = 1f;

	private CamRigRotationInterpolator camRigShaker;

	private const bool SWEEP_CALC = true;

	public bool debugSweep;

	private Vector3 rotorVelocity;

	public bool usePhaseLag;

	public float phaseLagAngle;

	public Transform phaseLagAxis;

	private WingMaster master;

	public Color gizmoColor = new Color(1f, 0.5f, 0.25f, 0.25f);

	private float currDynamicSweep;

	[HideInInspector]
	public Vector3 dragVector;

	[HideInInspector]
	public Vector3 liftVector;

	[HideInInspector]
	public float sweepDragMul;

	[HideInInspector]
	public float currAoA;

	private WindMaster windMaster;

	public bool cullAtMinAirspeed = true;

	private int shakeDir = 1;

	public float currentLiftForce { get; private set; }

	public float currentDragForce { get; private set; }

	public float currentTotalForceMagnitude => currentLiftForce + currentDragForce;

	public bool useRotorVelocity { get; private set; }

	public float wingAirspeed { get; private set; }

	public void SetRotorVelocity(Vector3 s)
	{
		useRotorVelocity = true;
		rotorVelocity = s;
		if ((bool)master)
		{
			master.alwaysEnabled = true;
		}
	}

	private void Awake()
	{
		if (!rb)
		{
			rb = GetComponentInParent<Rigidbody>();
		}
		wingTransform = base.transform;
		hasSoundEffects = soundEffects != null && soundEffects.Length != 0;
		liftConstant = 0.5f * liftCoefficient * liftArea;
		dragConstant = -0.5f * dragCoefficient * liftArea;
		if (useBuffet)
		{
			buffetRand = UnityEngine.Random.Range(0f, 200f);
		}
		if (dragCamShake)
		{
			camRigShaker = rb.GetComponentInChildren<CamRigRotationInterpolator>();
			if (!camRigShaker)
			{
				dragCamShake = false;
			}
		}
	}

	public void SetLiftArea(float a)
	{
		liftArea = a;
		liftConstant = 0.5f * liftCoefficient * liftArea;
		dragConstant = -0.5f * dragCoefficient * liftArea;
	}

	private void Start()
	{
		if (!useManualOffset)
		{
			manualOffset = rb.transform.InverseTransformPoint(wingTransform.position) - rb.centerOfMass;
		}
		else
		{
			localOffset = base.transform.InverseTransformPoint(rb.worldCenterOfMass + rb.transform.TransformVector(manualOffset));
		}
		if (aeroProfile == null)
		{
			aeroProfile = AerodynamicsController.fetch.wingAero;
		}
		master = rb.GetComponent<WingMaster>();
		if (!master)
		{
			master = rb.gameObject.AddComponent<WingMaster>();
		}
	}

	private void OnDrawGizmos()
	{
		if (!rb)
		{
			rb = GetComponentInParent<Rigidbody>();
		}
		if (!rb)
		{
			return;
		}
		Vector3 worldCenterOfMass = rb.worldCenterOfMass;
		float num = Mathf.Sqrt(liftArea);
		float x = num * 2f;
		float z = num * 0.5f;
		Transform transform = (wingTransform ? wingTransform : base.transform);
		Vector3 position = (useManualOffset ? (worldCenterOfMass + rb.transform.TransformVector(manualOffset)) : transform.position);
		position = transform.InverseTransformPoint(position);
		Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
		Gizmos.color = gizmoColor;
		Gizmos.DrawCube(position, new Vector3(x, 0.25f, z));
		Gizmos.matrix = Matrix4x4.identity;
		if (debugSweep)
		{
			wingTransform = base.transform;
			if (!Application.isPlaying)
			{
				CalculateSweep(Vector3.forward);
			}
			else
			{
				_ = currDynamicSweep;
			}
		}
	}

	private float CalculateSweep(Vector3 vel)
	{
		Vector3 vector = Vector3.ProjectOnPlane(vel, wingTransform.up);
		Vector3 to = Vector3.Project(vector, wingTransform.forward);
		return Vector3.Angle(vector, to);
	}

	private void FixedUpdate()
	{
		if (rb.isKinematic)
		{
			return;
		}
		Vector3 vector = rb.worldCenterOfMass + rb.transform.TransformVector(manualOffset);
		Vector3 up = wingTransform.up;
		Vector3 vector2 = ((!usePointVelocity) ? rb.velocity : rb.GetPointVelocity(vector));
		if (useRotorVelocity)
		{
			vector2 += rotorVelocity;
			vector2 += CancelRotVel(vector);
		}
		if (WindVolumes.windEnabled)
		{
			if (!windMaster)
			{
				windMaster = rb.gameObject.GetComponent<WindMaster>();
				if (!windMaster)
				{
					windMaster = rb.gameObject.AddComponent<WindMaster>();
				}
			}
			vector2 -= windMaster.wind;
		}
		float sqrMagnitude = vector2.sqrMagnitude;
		float speed = (wingAirspeed = Mathf.Sqrt(sqrMagnitude));
		if (!cullAtMinAirspeed || sqrMagnitude > 400f)
		{
			float num2 = AerodynamicsController.fetch.AtmosDensityAtPosition(vector);
			float num3 = (currAoA = Vector3.Angle(up, vector2) - 90f);
			float time = Mathf.Abs(num3);
			float num4 = num2 * sqrMagnitude;
			float num5 = (aeroProfile.mirroredCurves ? aeroProfile.liftCurve.Evaluate(time) : aeroProfile.liftCurve.Evaluate(num3));
			float num6 = liftConstant * num4 * Mathf.Sign(num3) * num5;
			float num7 = (aeroProfile.mirroredCurves ? aeroProfile.dragCurve.Evaluate(time) : aeroProfile.dragCurve.Evaluate(num3));
			float num8 = dragConstant * num4 * num7;
			float sweep = CalculateSweep(vector2);
			if (debugSweep)
			{
				currDynamicSweep = sweep;
			}
			num8 *= AerodynamicsController.fetch.DragMultiplierAtSpeedAndSweep(speed, WaterPhysics.GetAltitude(vector), sweep);
			Vector3 vector3 = (aeroProfile.perpLiftVector ? Vector3.Cross(vector2, Vector3.Cross(up, vector2)) : up);
			dragVector = num8 * vector2.normalized;
			liftVector = num6 * vector3.normalized;
			Vector3 force = dragVector + liftVector;
			if (usePhaseLag)
			{
				Vector3 vector4 = rb.worldCenterOfMass + (phaseLagAxis.position - rb.transform.TransformPoint(rb.centerOfMass));
				Vector3 vector5 = vector - vector4;
				Quaternion quaternion = Quaternion.AngleAxis(phaseLagAngle, phaseLagAxis.forward);
				vector = vector4 + quaternion * vector5;
				force = quaternion * (liftVector + dragVector);
			}
			currentLiftForce = num6;
			currentDragForce = num8;
			if (useBuffet)
			{
				Vector3 vector6 = VectorUtils.FullRangePerlinNoise(buffetRand, Time.time * sqrMagnitude * aeroProfile.buffetTimeFactor) * aeroProfile.buffetCurve.Evaluate(num3) * aeroProfile.buffetMagnitude * sqrMagnitude * liftArea * up;
				force += vector6;
			}
			rb.AddForceAtPosition(force, vector);
			if (hasSoundEffects)
			{
				float force2 = num6 + 2f * num8;
				for (int i = 0; i < soundEffects.Length; i++)
				{
					soundEffects[i].Update(force2, speed);
				}
			}
			if (dragCamShake && (bool)camRigShaker)
			{
				Vector3 force3 = (float)shakeDir * dragShakeFactor * dragVector;
				shakeDir *= -1;
				CamRigRotationInterpolator.ShakeAll(force3);
			}
		}
		else
		{
			dragVector = Vector3.zero;
			liftVector = Vector3.zero;
		}
	}

	private Vector3 CancelRotVel(Vector3 wingPos)
	{
		return -(rb.GetPointVelocity(wingPos) - rb.velocity);
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		this.rb = rb;
		if (!rb)
		{
			return;
		}
		if ((bool)master)
		{
			master.RemoveWing(this);
			master = null;
		}
		master = rb.GetComponent<WingMaster>();
		if (!master)
		{
			rb.gameObject.AddComponent<WingMaster>();
		}
		else
		{
			master.AddWing(this);
		}
		wingTransform = base.transform;
		if (useManualOffset)
		{
			manualOffset = rb.transform.InverseTransformPoint(base.transform.TransformPoint(localOffset)) - rb.centerOfMass;
		}
		else
		{
			manualOffset = rb.transform.InverseTransformPoint(wingTransform.position) - rb.centerOfMass;
		}
		windMaster = rb.gameObject.GetComponent<WindMaster>();
		if (!windMaster)
		{
			windMaster = rb.gameObject.AddComponent<WindMaster>();
		}
		if (dragCamShake)
		{
			camRigShaker = rb.GetComponentInChildren<CamRigRotationInterpolator>();
			if (!camRigShaker)
			{
				dragCamShake = false;
			}
		}
	}
}
