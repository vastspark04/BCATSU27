using UnityEngine;

public class Winch : MonoBehaviour
{
	public Rigidbody parentRB;

	public float maxLength;

	public float spoolRate;

	public float hookMass;

	public float hookDragArea;

	public Transform hookTransform;

	private FloatingOriginTransform hookFOT;

	private SimpleDrag hookDrag;

	private ConfigurableJoint hookToVesselJoint;

	private ConfigurableJoint hookToObjectJoint;

	private Rigidbody hookRB;

	private Rigidbody objectRB;

	private float currentLength;

	public LineRenderer ropeLR;

	private void Start()
	{
		SetupRope();
	}

	private void SeparateHook()
	{
		hookTransform.parent = null;
		hookRB = hookTransform.gameObject.AddComponent<Rigidbody>();
		hookRB.interpolation = RigidbodyInterpolation.Interpolate;
		hookRB.velocity = parentRB.GetPointVelocity(hookTransform.position);
		hookRB.mass = hookMass;
		hookFOT = hookTransform.gameObject.AddComponent<FloatingOriginTransform>();
		hookFOT.SetRigidbody(hookRB);
		if (!hookDrag)
		{
			hookDrag = hookTransform.gameObject.AddComponent<SimpleDrag>();
			hookDrag.SetDragArea(hookDragArea);
			hookDrag.SetParentRigidbody(hookRB);
		}
		hookToVesselJoint = parentRB.gameObject.AddComponent<ConfigurableJoint>();
		ConfigurableJoint configurableJoint = hookToVesselJoint;
		ConfigurableJoint configurableJoint2 = hookToVesselJoint;
		ConfigurableJointMotion configurableJointMotion2 = (hookToVesselJoint.zMotion = ConfigurableJointMotion.Limited);
		ConfigurableJointMotion configurableJointMotion5 = (configurableJoint.xMotion = (configurableJoint2.yMotion = configurableJointMotion2));
		hookToVesselJoint.linearLimit = HookLimit(1f);
		hookToVesselJoint.projectionMode = JointProjectionMode.PositionAndRotation;
		hookToVesselJoint.anchor = parentRB.transform.InverseTransformPoint(base.transform.position);
		hookToVesselJoint.connectedBody = hookRB;
		hookToVesselJoint.connectedAnchor = Vector3.zero;
	}

	private void StowHook()
	{
		Object.Destroy(hookRB);
		Object.Destroy(hookFOT);
		Object.Destroy(hookToVesselJoint);
		hookTransform.parent = base.transform;
		hookTransform.localPosition = Vector3.zero;
		hookTransform.localRotation = Quaternion.identity;
	}

	private SoftJointLimit HookLimit(float length)
	{
		SoftJointLimit result = default(SoftJointLimit);
		result.limit = (currentLength = length);
		result.bounciness = 0f;
		return result;
	}

	public void Extend()
	{
		if ((bool)hookToVesselJoint)
		{
			hookToVesselJoint.linearLimit = HookLimit(Mathf.Min(maxLength, currentLength + spoolRate * Time.deltaTime));
		}
		else
		{
			SeparateHook();
		}
	}

	public void Retract()
	{
		if ((bool)hookToVesselJoint)
		{
			float a = (objectRB ? 0.5f : 0f);
			hookToVesselJoint.linearLimit = HookLimit(Mathf.Max(a, currentLength - spoolRate * Time.deltaTime));
			if (!objectRB && currentLength < 0.01f)
			{
				StowHook();
			}
		}
	}

	private void SetupRope()
	{
		ropeLR.positionCount = 2;
	}

	private void Update()
	{
		UpdateRope();
		if (Input.GetKey(KeyCode.H))
		{
			Extend();
		}
		else if (Input.GetKey(KeyCode.J))
		{
			Retract();
		}
	}

	private void FixedUpdate()
	{
		UpdateHook();
	}

	private void UpdateRope()
	{
		if ((bool)hookRB)
		{
			ropeLR.enabled = true;
			ropeLR.SetPosition(0, base.transform.position);
			ropeLR.SetPosition(1, hookTransform.position);
		}
		else
		{
			ropeLR.enabled = false;
		}
	}

	private void UpdateHook()
	{
		if ((bool)hookRB && !objectRB && Physics.Linecast(hookRB.position, hookRB.position + hookRB.velocity * Time.fixedDeltaTime, out var hitInfo, 256))
		{
			WinchableObject component = hitInfo.collider.GetComponent<WinchableObject>();
			if ((bool)component)
			{
				HookObject(component);
			}
		}
	}

	private void HookObject(WinchableObject wo)
	{
		objectRB = wo.rb;
		hookToObjectJoint = hookTransform.gameObject.AddComponent<ConfigurableJoint>();
		ConfigurableJoint configurableJoint = hookToObjectJoint;
		ConfigurableJoint configurableJoint2 = hookToObjectJoint;
		ConfigurableJointMotion configurableJointMotion2 = (hookToObjectJoint.zMotion = ConfigurableJointMotion.Locked);
		ConfigurableJointMotion configurableJointMotion5 = (configurableJoint.xMotion = (configurableJoint2.yMotion = configurableJointMotion2));
		ConfigurableJoint configurableJoint3 = hookToObjectJoint;
		ConfigurableJoint configurableJoint4 = hookToObjectJoint;
		configurableJointMotion2 = (hookToObjectJoint.angularZMotion = ConfigurableJointMotion.Locked);
		configurableJointMotion5 = (configurableJoint3.angularXMotion = (configurableJoint4.angularYMotion = configurableJointMotion2));
		hookToObjectJoint.projectionMode = JointProjectionMode.PositionAndRotation;
		hookToObjectJoint.connectedBody = objectRB;
		hookToObjectJoint.connectedAnchor = objectRB.transform.InverseTransformPoint(hookTransform.position);
		hookToVesselJoint.connectedBody = objectRB;
		hookToVesselJoint.connectedAnchor = objectRB.transform.InverseTransformPoint(hookTransform.position);
	}

	private void UnhookObject()
	{
		if ((bool)objectRB)
		{
			if ((bool)hookToObjectJoint)
			{
				Object.Destroy(hookToObjectJoint);
			}
			hookToVesselJoint.connectedBody = hookRB;
			hookToVesselJoint.connectedAnchor = Vector3.zero;
		}
	}
}
