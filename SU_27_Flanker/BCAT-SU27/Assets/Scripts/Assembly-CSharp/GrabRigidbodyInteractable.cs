using UnityEngine;

public class GrabRigidbodyInteractable : MonoBehaviour
{
	private Rigidbody rb;

	private Transform controllerTf;

	private bool grabbed;

	private Vector3 localPos;

	private Quaternion localRot;

	private Vector3 velocity;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		VRInteractable component = GetComponent<VRInteractable>();
		component.OnStartInteraction += Ir_OnStartInteraction;
		component.OnStopInteraction += Ir_OnStopInteraction;
	}

	private void LateUpdate()
	{
		if (grabbed)
		{
			rb.MovePosition(controllerTf.TransformPoint(localPos));
			rb.MoveRotation(controllerTf.transform.rotation * localRot);
			velocity = rb.velocity;
		}
	}

	private void Ir_OnStartInteraction(VRHandController controller)
	{
		rb.isKinematic = true;
		controllerTf = controller.transform;
		localPos = controllerTf.InverseTransformPoint(rb.position);
		localRot = Quaternion.Inverse(controllerTf.rotation) * rb.rotation;
		grabbed = true;
	}

	private void Ir_OnStopInteraction(VRHandController controller)
	{
		rb.isKinematic = false;
		rb.velocity = velocity;
		grabbed = false;
	}
}
