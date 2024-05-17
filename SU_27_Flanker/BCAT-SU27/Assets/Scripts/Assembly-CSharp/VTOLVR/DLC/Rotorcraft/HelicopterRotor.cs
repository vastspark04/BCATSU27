using System;
using UnityEngine;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class HelicopterRotor : FlightControlComponent, IQSVehicleComponent
{
	[Serializable]
	public class RotorBlade
	{
		public Transform referenceTransform;

		public Wing wing;

		public float collectiveFactor;

		public float cyclicPitchFactor;

		public float pedalYawFactor;

		public float cyclicRollFactor;

		public float maxDeflection;

		private Vector3 tLocalDir;

		public float radius { get; set; }

		public Vector3 tangentialDirection
		{
			get
			{
				return referenceTransform.TransformDirection(tLocalDir);
			}
			set
			{
				tLocalDir = referenceTransform.InverseTransformDirection(value);
			}
		}

		public Vector3 localRotAxis { get; set; }

		public Quaternion defaultRot { get; set; }

		public float undamagedWingArea { get; set; }
	}

	public TurbineDriveshaft inputShaft;

	public Rigidbody rb;

	public MinMax collectiveRange = new MinMax(-1f, 1f);

	public bool zeroSpring;

	public PID zeroSpringPid;

	public float zeroSpringMaxSpeed = 50f;

	public float maxZeroSpringRate = 1f;

	public float maxVisualSpeed = -1f;

	public bool useMaxVisualCurve;

	public AnimationCurve maxVisualCurve;

	public float torqueMassRatio;

	public bool doDiscTilt;

	public Transform[] discTiltRefTransforms;

	public Transform outputDiscTiltTf;

	public bool doCollision;

	public float minCollisionRPM = 30f;

	public int collisionCasts = 1;

	public float collisionTorqueFactor = 0.1f;

	public float[] collisionRadii;

	public float damageTorqueDragFactor = 1f;

	public RotorBlade[] blades;

	public Transform rotationTransform;

	public Vector3 rotationAxis;

	private Vector3 localAxis;

	public RotationAudio[] rotationAudios;

	public AnimationCurve collectiveSpeedPitchTrim;

	public bool useCSPT;

	public MultiUserVehicleSync muvs;

	private bool initializedWings;

	private float rotationVelocity;

	private float rotorAngle = 180f;

	public RotationToggle foldToggle;

	public VRLever foldSwitchLever;

	public RotorBrake rotorBrake;

	private int foldSwitch;

	public bool showYawTrim;

	public bool showPitchRollTrim;

	public Vector3 pyrTrim;

	private Vector3 inputPYR;

	private float inputCollective;

	public float shiftTrimRate = 0.2f;

	public HUDJoyIndicator trimIndicator;

	private float dragTorque;

	private Vector3 localDiscAxis = Vector3.up;

	[Range(-180f, 180f)]
	public float flapCycleOffset;

	public bool rotateFlapCalcToVel;

	[Header("Induced Flow")]
	public bool doInducedFlow;

	public bool inducedFlowGizmos;

	public float inducedFlowRateFactor = 0.25f;

	public float inducedFlowRadiusFactor = 0.7f;

	public float inducedFlowLagRate = 1f;

	public float inducedFlowFadeRate = 1f;

	public float inducedFlowChangeRate = 5f;

	public FlightInfo flightInfo;

	public AnimationCurve groundEffectCurve;

	private Vector3 inducedFlowP;

	public float flapVelFactor = 0.1f;

	public float collisionRadius => collisionRadii[Mathf.Clamp(damageLevel, 0, collisionRadii.Length - 1)];

	public int damageLevel { get; private set; }

	private string nodeName => "Rotor_" + base.gameObject.name;

	public event Action<int> OnDamageLevel;

	private void InitializeWings()
	{
		if (!initializedWings)
		{
			initializedWings = true;
			localAxis = rotationTransform.parent.InverseTransformDirection(rotationTransform.TransformDirection(rotationAxis));
			Vector3 vector = rotationTransform.parent.TransformDirection(localAxis);
			for (int i = 0; i < blades.Length; i++)
			{
				RotorBlade rotorBlade = blades[i];
				rotorBlade.referenceTransform = base.transform;
				rotorBlade.radius = Vector3.ProjectOnPlane(rotorBlade.wing.transform.position - rotationTransform.position, vector).magnitude;
				rotorBlade.tangentialDirection = Vector3.Cross(vector, rotorBlade.wing.transform.position - rotationTransform.position).normalized;
				rotorBlade.defaultRot = rotorBlade.wing.transform.localRotation;
				rotorBlade.localRotAxis = rotorBlade.wing.transform.parent.InverseTransformDirection(rotorBlade.wing.transform.right);
				rotorBlade.undamagedWingArea = rotorBlade.wing.liftArea;
			}
		}
	}

	private void Start()
	{
		if (rotationAudios != null)
		{
			RotationAudio[] array = rotationAudios;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].manual = true;
			}
		}
		if (zeroSpring)
		{
			zeroSpringPid.circular = true;
		}
		InitializeWings();
		if (doCollision)
		{
			VehiclePart componentInParent = GetComponentInParent<VehiclePart>();
			if ((bool)componentInParent)
			{
				componentInParent.OnRepair.AddListener(OnRepair);
			}
		}
	}

	private void Update()
	{
		bool flag = !rb.isKinematic;
		bool flag2 = !VTOLMPUtils.IsMultiplayer() || !muvs || muvs.IsLocalPlayerSeated();
		float num = inputShaft.outputRPM * 6f;
		bool flag3 = zeroSpring && num < zeroSpringMaxSpeed && inputShaft.rotationAcceleration <= 0f;
		if (!flag3)
		{
			rotationVelocity = num;
		}
		if (flag2 && (bool)rotorBrake && (bool)foldSwitchLever)
		{
			if (!foldToggle.battery.Drain(0.001f * Time.deltaTime))
			{
				foldSwitchLever.LockTo(foldSwitchLever.currentState);
			}
			else if (rotorBrake.IsBraking() && Mathf.Abs(rotationVelocity) < 3f && Mathf.Abs(rotorAngle - 180f) < 3f)
			{
				foldSwitchLever.Unlock();
			}
			else
			{
				foldSwitchLever.LockTo(0);
			}
		}
		float num2 = rotationVelocity;
		if (!flag3 && damageLevel == 0)
		{
			if (useMaxVisualCurve)
			{
				num2 = maxVisualCurve.Evaluate(rotationVelocity);
			}
			else if (maxVisualSpeed > 0f)
			{
				num2 = Mathf.Min(rotationVelocity, maxVisualSpeed);
			}
		}
		rotorAngle = Mathf.Repeat(rotorAngle + num2 * Time.deltaTime, 360f);
		rotationTransform.localRotation = Quaternion.Euler(0f, 0f - rotorAngle, 0f);
		if (rotationAudios != null)
		{
			for (int i = 0; i < rotationAudios.Length; i++)
			{
				rotationAudios[i].UpdateAudioSpeed(inputShaft.outputRPM);
			}
		}
		if (flag2 && (bool)trimIndicator)
		{
			if (showYawTrim)
			{
				trimIndicator.SetTrimYaw(pyrTrim.y);
			}
			if (showPitchRollTrim)
			{
				trimIndicator.SetTrimPitchRoll(pyrTrim.x, pyrTrim.z);
			}
		}
		if (flag && doCollision && inputShaft.outputRPM > minCollisionRPM)
		{
			UpdateCollision();
		}
	}

	public void SetFold(int f)
	{
		foldSwitch = f;
		if (Mathf.Abs(rotationVelocity) < 4f)
		{
			foldToggle.SetState(f);
		}
	}

	private void UpdateCollision()
	{
		float num = UnityEngine.Random.Range(0f, 180f);
		float num2 = collisionRadius / 6f / ((float)Math.PI * 2f * collisionRadius) * 360f;
		for (int i = 0; i < collisionCasts; i++)
		{
			float num3 = num + (float)i * num2;
			float rot = num3 + 180f;
			if (CollisionCast(num3))
			{
				break;
			}
			CollisionCast(rot);
		}
	}

	private bool CollisionCast(float rot)
	{
		Quaternion quaternion = Quaternion.AngleAxis(rot, rotationTransform.parent.up);
		Vector3 vector = rotationTransform.position + quaternion * rotationTransform.parent.forward * collisionRadius;
		Vector3 vector2 = quaternion * rotationTransform.parent.right;
		Vector3 vector3 = vector + vector2 * collisionRadius / 6f;
		bool flag = false;
		Vector3 vector4 = Vector3.zero;
		RaycastHit hitInfo;
		if (vector3.y < WaterPhysics.waterHeight)
		{
			flag = true;
			vector4 = vector3;
			vector4.y = WaterPhysics.waterHeight;
		}
		else if (Physics.Linecast(vector, vector3, out hitInfo, 1))
		{
			flag = true;
			vector4 = hitInfo.point;
		}
		if (flag)
		{
			DamageRotor();
			ExplosionManager.instance.CreateExplosionEffect(ExplosionManager.ExplosionTypes.DebrisPoof, vector4, vector - vector4);
			float num = collisionTorqueFactor * inputShaft.outputRPM / Time.fixedDeltaTime;
			inputShaft.AddResistanceTorque(num);
			rb.AddTorque(num * torqueMassRatio * rotationTransform.up);
			return true;
		}
		return false;
	}

	[ContextMenu("Damage Rotor")]
	public void DamageRotor()
	{
		if (!initializedWings)
		{
			InitializeWings();
		}
		damageLevel++;
		RotorBlade[] array = blades;
		foreach (RotorBlade rotorBlade in array)
		{
			rotorBlade.wing.SetLiftArea(rotorBlade.undamagedWingArea * (collisionRadius / collisionRadii[0]));
		}
		this.OnDamageLevel?.Invoke(damageLevel);
	}

	public void DamageRotor(int damageIdx)
	{
		if (!initializedWings)
		{
			InitializeWings();
		}
		damageLevel = damageIdx;
		float num = 1f / (float)(damageLevel + 1);
		RotorBlade[] array = blades;
		foreach (RotorBlade rotorBlade in array)
		{
			rotorBlade.wing.SetLiftArea(rotorBlade.undamagedWingArea * num);
		}
		this.OnDamageLevel?.Invoke(damageLevel);
	}

	private void OnRepair()
	{
		if (!initializedWings)
		{
			InitializeWings();
		}
		damageLevel = 0;
		RotorBlade[] array = blades;
		foreach (RotorBlade rotorBlade in array)
		{
			rotorBlade.wing.SetLiftArea(rotorBlade.undamagedWingArea);
		}
		this.OnDamageLevel?.Invoke(damageLevel);
	}

	public override void SetPitchYawRoll(Vector3 pyr)
	{
		inputPYR = pyr;
	}

	public Vector3 CurrentPYR()
	{
		return inputPYR + pyrTrim;
	}

	public float CurrentCollective()
	{
		return inputCollective;
	}

	public void SetCollective(float c)
	{
		inputCollective = c;
	}

	public void SetCollectiveFullRange(float c)
	{
		inputCollective = collectiveRange.Lerp(c);
	}

	public void ShiftTrimRPx(Vector3 rpx)
	{
		pyrTrim += new Vector3(rpx.y, 0f, 0f - rpx.x) * shiftTrimRate * Time.deltaTime;
		ClampTrim();
	}

	public void ShifitTrimYaw(Vector3 yxx)
	{
		pyrTrim += new Vector3(0f, yxx.x, 0f) * shiftTrimRate * Time.deltaTime;
		ClampTrim();
	}

	private void ClampTrim()
	{
		pyrTrim = new Vector3(Mathf.Clamp(pyrTrim.x, -1f, 1f), Mathf.Clamp(pyrTrim.y, -1f, 1f), Mathf.Clamp(pyrTrim.z, -1f, 1f));
	}

	public void SetYawTrim(float t)
	{
		pyrTrim.y = t;
		ClampTrim();
	}

	public void SetPitchTrim(float p)
	{
		pyrTrim.x = p;
		ClampTrim();
	}

	public void SetRollTrim(float r)
	{
		pyrTrim.z = r;
		ClampTrim();
	}

	public float GetDragTorque()
	{
		return dragTorque;
	}

	private void FixedUpdate()
	{
		bool num = !rb.isKinematic;
		float num2 = inputShaft.outputRPM * 6f;
		if (zeroSpring && num2 < zeroSpringMaxSpeed && inputShaft.rotationAcceleration <= 0f)
		{
			zeroSpringPid.updateMode = UpdateModes.Fixed;
			float num3 = Mathf.Clamp(zeroSpringPid.Evaluate(rotorAngle, 180f), 0f - maxZeroSpringRate, maxZeroSpringRate);
			rotationVelocity += num3 * Time.fixedDeltaTime;
		}
		float num4 = inputShaft.outputRPM * 0.10472f;
		Vector3 zero = Vector3.zero;
		if (num && useCSPT)
		{
			zero.x += collectiveSpeedPitchTrim.Evaluate(inputCollective * rb.velocity.magnitude);
		}
		if (num && doDiscTilt)
		{
			Transform[] array = discTiltRefTransforms;
			Vector3 vector = -(new Plane(array[0].position, array[1].position, array[2].position).normal + new Plane(array[2].position, array[3].position, array[0].position).normal);
			localDiscAxis = base.transform.InverseTransformDirection(vector);
			Debug.DrawLine(rotationTransform.position, rotationTransform.position + vector);
			if ((bool)outputDiscTiltTf)
			{
				outputDiscTiltTf.rotation = Quaternion.LookRotation(vector);
			}
		}
		Vector3 inducedFlow = Vector3.zero;
		if (num && doInducedFlow)
		{
			inducedFlow = CalculateInducedFlow();
		}
		if (!num)
		{
			return;
		}
		dragTorque = 0f;
		for (int i = 0; i < blades.Length; i++)
		{
			RotorBlade rotorBlade = blades[i];
			float num5 = num4 * rotorBlade.radius;
			Vector3 vector2 = rotorBlade.tangentialDirection * num5;
			float num6 = (vector2 + rb.velocity).magnitude * rb.velocity.magnitude;
			vector2 += CalculateFlapVelocity(rotorBlade.wing.transform.position, num6 * inputCollective);
			if (doInducedFlow)
			{
				vector2 += CalculateInducedFlowAtPosition(rotorBlade.wing.transform.position, inducedFlow);
			}
			rotorBlade.wing.SetRotorVelocity(vector2);
			dragTorque += Vector3.Dot(rotorBlade.wing.dragVector + rotorBlade.wing.liftVector, -rotorBlade.tangentialDirection) * 1000f * rotorBlade.radius;
			Vector3 vector3 = inputPYR + pyrTrim;
			vector3 += zero;
			Quaternion localRotation = Quaternion.AngleAxis(Mathf.Clamp(0f + rotorBlade.cyclicPitchFactor * vector3.x + rotorBlade.pedalYawFactor * vector3.y + rotorBlade.cyclicRollFactor * vector3.z + rotorBlade.collectiveFactor * inputCollective, -1f, 1f) * rotorBlade.maxDeflection, rotorBlade.localRotAxis) * rotorBlade.defaultRot;
			rotorBlade.wing.transform.localRotation = localRotation;
		}
		if (doCollision && damageLevel > 0)
		{
			float num7 = damageTorqueDragFactor * (float)damageLevel * inputShaft.outputRPM;
			dragTorque += num7;
		}
		inputShaft.AddResistanceTorque(dragTorque);
		rb.AddTorque(inputShaft.transmission.inputTorque * torqueMassRatio * rotationTransform.up);
	}

	private Vector3 CalculateFlapVelocity(Vector3 position, float tanSpeed)
	{
		float num = tanSpeed * flapVelFactor;
		Vector3 to = Vector3.ProjectOnPlane(position - base.transform.position, base.transform.up);
		Vector3 from = base.transform.forward;
		if (rotateFlapCalcToVel)
		{
			from = Vector3.ProjectOnPlane(rb.velocity, base.transform.up);
		}
		return Mathf.Cos((Vector3.SignedAngle(from, to, -base.transform.up) + 90f + flapCycleOffset) * ((float)Math.PI / 180f)) * num * rotationTransform.up;
	}

	private Vector3 CalculateInducedFlow()
	{
		float radarAltitude = flightInfo.radarAltitude;
		float num = groundEffectCurve.Evaluate(radarAltitude);
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < blades.Length; i++)
		{
			zero += blades[i].wing.liftVector + blades[i].wing.dragVector;
		}
		float num2 = AerodynamicsController.fetch.AtmosDensityAtPositionMetric(base.transform.position);
		float num3 = (float)Math.PI * collisionRadius * collisionRadius;
		Vector3 target = Mathf.Sqrt(zero.magnitude * 1000f / Mathf.Max(0.001f, 2f * num2 * num3)) * inducedFlowRateFactor * num * zero.normalized;
		inducedFlowP = Vector3.MoveTowards(inducedFlowP, target, inducedFlowChangeRate * Time.fixedDeltaTime);
		return inducedFlowP;
	}

	private Vector3 CalculateInducedFlowAtPosition(Vector3 position, Vector3 inducedFlow)
	{
		Vector3 vector = rotationTransform.position - rb.velocity * inducedFlowLagRate;
		float num = Mathf.Max(0f, (position - vector).magnitude - collisionRadius * inducedFlowRadiusFactor);
		inducedFlow *= 1f - Mathf.Pow(Mathf.Clamp01(inducedFlowFadeRate * num), 0.5f);
		return inducedFlow;
	}

	private void OnDrawGizmos()
	{
		if (flapVelFactor != 0f && collisionRadii != null && collisionRadii.Length != 0)
		{
			float num = Mathf.Max(0.1f, collisionRadii[0]);
			Vector3 vector = rotationTransform.forward * num;
			float num2 = 280f * 0.10472f * num;
			for (int i = 0; i < 360; i += 15)
			{
				for (float num3 = 0.2f; num3 <= 1f; num3 = Mathf.MoveTowards(num3, 1.1f, 0.2f))
				{
					Vector3 vector2 = rotationTransform.position + Quaternion.AngleAxis(i, rotationTransform.up) * vector * num3;
					Vector3 vector3 = CalculateFlapVelocity(vector2, num2 * num3) * 8f;
					Gizmos.DrawLine(vector2, vector2 + vector3 * 0.4f);
					Gizmos.DrawLine(vector2 + vector3 * 0.3f + (vector2 - rotationTransform.position).normalized * (vector3.magnitude * 0.1f), vector2 + vector3 * 0.4f);
				}
			}
		}
		if (!doInducedFlow || !inducedFlowGizmos)
		{
			return;
		}
		Vector3 inducedFlow = 10f * base.transform.up;
		if (Application.isPlaying)
		{
			inducedFlow = CalculateInducedFlow();
		}
		Vector3 position = base.transform.position;
		position.x = Mathf.FloorToInt(position.x);
		position.z = Mathf.FloorToInt(position.z);
		for (int j = -20; j < 20; j++)
		{
			for (int k = -20; k < 20; k++)
			{
				Vector3 vector4 = position + Vector3.forward * k + Vector3.right * j;
				Vector3 vector5 = CalculateInducedFlowAtPosition(vector4, inducedFlow);
				Gizmos.color = Color.green;
				Gizmos.DrawLine(vector4, vector4 - vector5);
			}
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode(nodeName);
		if (doCollision)
		{
			configNode.SetValue("damageLevel", damageLevel);
		}
		configNode.SetValue("pyrTrim", pyrTrim);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(nodeName);
		int target = 0;
		if (doCollision)
		{
			ConfigNodeUtils.TryParseValue(node, "damageLevel", ref target);
			DamageRotor(target);
		}
		pyrTrim = node.GetValue<Vector3>("pyrTrim");
	}
}

}