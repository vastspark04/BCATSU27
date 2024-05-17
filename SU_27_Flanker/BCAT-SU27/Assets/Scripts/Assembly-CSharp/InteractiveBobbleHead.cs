using System;
using UnityEngine;

public class InteractiveBobbleHead : MonoBehaviour
{
	[Serializable]
	public class HandCollider
	{
		public Transform transform;

		public Vector3 center;

		public float radius;

		public Vector3 lastLocalPos { get; set; }

		public Vector3 velocity { get; set; }
	}

	public Rigidbody parentRb;

	[Header("Head Physics")]
	public PID3 positionalSpring;

	public PID rotationalSpring;

	public Vector3 headLocalTarget;

	public Vector3 maxHeadOffset;

	public Transform headTransform;

	public float springTorqueMul = 1f;

	public float inertiaFactor = 1f;

	public float maxRotationOffset = 45f;

	[Header("Head Collider")]
	public float headColliderRadius;

	public Vector3 headColliderCenter;

	[Header("Hand Collision")]
	public HandCollider[] handColliders;

	public float handImpactMultiplier = 1f;

	public float handFrictionTorqueMul = 1f;

	[Header("Cockpit Shake")]
	public float cShakeMultiplier = 20f;

	private CamRigRotationInterpolator cShaker;

	private bool hasUpdated;

	private float lastHandUpdateTime;

	private Vector3 headAngVel;

	private float maxAngVel = 7f;

	private Vector3 headVelocity;

	private Vector3 lastRBVel;

	private void Start()
	{
		if (!GameSettings.CurrentSettings.GetBoolSetting("SHOW_BOBBLEHEAD"))
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		cShaker = base.transform.root.GetComponentInChildren<CamRigRotationInterpolator>(includeInactive: true);
		InitHands();
	}

	private void Update()
	{
		hasUpdated = true;
	}

	private void FixedUpdate()
	{
		UpdateRBPhysics();
		UpdateHands();
		UpdateHeadPhysics();
	}

	private void InitHands()
	{
		HandCollider[] array = handColliders;
		foreach (HandCollider handCollider in array)
		{
			if ((bool)handCollider.transform)
			{
				handCollider.lastLocalPos = LocalPos(handCollider.transform.position);
			}
		}
		lastHandUpdateTime = Time.fixedTime;
	}

	private void UpdateHands()
	{
		if (!hasUpdated)
		{
			return;
		}
		float num = Time.fixedTime - lastHandUpdateTime;
		int num2 = handColliders.Length;
		for (int i = 0; i < num2; i++)
		{
			HandCollider handCollider = handColliders[i];
			if ((bool)handCollider.transform)
			{
				Vector3 vector = LocalPos(handCollider.transform.position);
				handCollider.velocity = (vector - handCollider.lastLocalPos) / num;
				handCollider.lastLocalPos = vector;
				DoCollision(handCollider);
			}
		}
		hasUpdated = false;
		lastHandUpdateTime = Time.fixedTime;
	}

	private void AddImpulse(Vector3 impulse, Vector3 localPosition)
	{
		headVelocity += impulse;
		Vector3 vector = LocalPos(headTransform.TransformPoint(headColliderCenter));
		Vector3 vector2 = localPosition - vector;
		Vector3 vector3 = Vector3.Cross(Vector3.ProjectOnPlane(impulse, vector2), -vector2);
		headAngVel += vector3 * springTorqueMul;
	}

	private void DoCollision(HandCollider h)
	{
		Vector3 vector = LocalPos(h.transform.TransformPoint(h.center));
		Vector3 vector2 = LocalPos(headTransform.TransformPoint(headColliderCenter));
		float num = headColliderRadius + h.radius;
		Vector3 vector3 = vector2 - vector;
		if (vector3.sqrMagnitude < num * num)
		{
			Vector3 vector4 = h.velocity - headVelocity;
			if (Vector3.Dot(vector4, vector3) > 0f)
			{
				Vector3 impulse = vector4 * handImpactMultiplier;
				AddImpulse(impulse, vector - vector3.normalized * headColliderRadius);
				Vector3 vector5 = vector + vector3.normalized * num - vector2;
				Vector3 headPos = headTransform.localPosition + vector5;
				headPos = LimitHeadMovement(headPos);
				Vector3 vector6 = Vector3.Cross(Vector3.ProjectOnPlane(vector4, vector3), vector3) * handFrictionTorqueMul;
				headAngVel += vector6;
				headTransform.localPosition = headPos;
			}
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		if (handColliders != null)
		{
			HandCollider[] array = handColliders;
			foreach (HandCollider handCollider in array)
			{
				if ((bool)handCollider.transform)
				{
					Gizmos.DrawWireSphere(handCollider.transform.TransformPoint(handCollider.center), handCollider.radius);
				}
			}
		}
		if ((bool)headTransform)
		{
			Gizmos.DrawWireSphere(headTransform.TransformPoint(headColliderCenter), headColliderRadius);
		}
	}

	private Vector3 LocalPos(Vector3 worldPosition)
	{
		return base.transform.InverseTransformPoint(worldPosition);
	}

	private void UpdateHeadPhysics()
	{
		Vector3 localPosition = headTransform.localPosition;
		Vector3 vector = positionalSpring.Evaluate(localPosition, headLocalTarget);
		AddImpulse(vector * Time.fixedDeltaTime, Vector3.zero);
		localPosition += headVelocity * Time.fixedDeltaTime;
		localPosition = LimitHeadMovement(localPosition);
		headAngVel = Vector3.ClampMagnitude(headAngVel, maxAngVel);
		Vector3 vector2 = -Vector3.Cross(Vector3.forward, headTransform.localRotation * Vector3.forward);
		headAngVel += vector2 * rotationalSpring.kp * Time.fixedDeltaTime;
		Vector3 vector3 = -Vector3.Cross(Vector3.up, headTransform.localRotation * Vector3.up);
		headAngVel += vector3 * rotationalSpring.kp * Time.fixedDeltaTime;
		Vector3 vector4 = -Vector3.Cross(Vector3.right, headTransform.localRotation * Vector3.right);
		headAngVel += vector4 * rotationalSpring.kp * Time.fixedDeltaTime;
		headAngVel += headAngVel * rotationalSpring.kd * Time.fixedDeltaTime;
		headTransform.localRotation = Quaternion.AngleAxis(headAngVel.magnitude * 57.29578f * Time.fixedDeltaTime, headAngVel) * headTransform.localRotation;
		Vector3 upwards = Vector3.RotateTowards(Vector3.up, headTransform.localRotation * Vector3.up, maxRotationOffset * ((float)Math.PI / 180f), 0f);
		Vector3 forward = Vector3.RotateTowards(Vector3.forward, headTransform.localRotation * Vector3.forward, maxRotationOffset * ((float)Math.PI / 180f), 0f);
		headTransform.localRotation = Quaternion.LookRotation(forward, upwards);
		headTransform.localPosition = localPosition;
	}

	private Vector3 LimitHeadMovement(Vector3 headPos)
	{
		if (headPos.x > maxHeadOffset.x)
		{
			headPos.x = maxHeadOffset.x;
			headVelocity.x = Mathf.Min(headVelocity.x, 0f);
		}
		else if (headPos.x < 0f - maxHeadOffset.x)
		{
			headPos.x = 0f - maxHeadOffset.x;
			headVelocity.x = Mathf.Max(headVelocity.x, 0f);
		}
		if (headPos.y > maxHeadOffset.y)
		{
			headPos.y = maxHeadOffset.y;
			headVelocity.y = Mathf.Min(headVelocity.y, 0f);
		}
		else if (headPos.y < 0f - maxHeadOffset.y)
		{
			headPos.y = 0f - maxHeadOffset.y;
			headVelocity.y = Mathf.Max(headVelocity.y, 0f);
		}
		if (headPos.z > maxHeadOffset.z)
		{
			headPos.z = maxHeadOffset.z;
			headVelocity.z = Mathf.Min(headVelocity.z, 0f);
		}
		else if (headPos.z < 0f - maxHeadOffset.z)
		{
			headPos.z = 0f - maxHeadOffset.z;
			headVelocity.z = Mathf.Max(headVelocity.z, 0f);
		}
		return headPos;
	}

	private void UpdateRBPhysics()
	{
		if ((bool)parentRb)
		{
			Vector3 pointVelocity = parentRb.GetPointVelocity(headTransform.position);
			if ((bool)cShaker)
			{
				pointVelocity += cShaker.transform.TransformVector(cShaker.GetCurrentShakeVelocity()) * cShakeMultiplier;
			}
			Vector3 vector = (pointVelocity - lastRBVel) / Time.fixedDeltaTime;
			vector = base.transform.InverseTransformVector(vector);
			lastRBVel = pointVelocity;
			Vector3 vector2 = base.transform.InverseTransformVector(new Vector3(0f, -9.81f, 0f));
			AddImpulse(inertiaFactor * Time.fixedDeltaTime * (-vector + vector2), LocalPos(headTransform.TransformPoint(headColliderCenter)));
		}
	}
}
