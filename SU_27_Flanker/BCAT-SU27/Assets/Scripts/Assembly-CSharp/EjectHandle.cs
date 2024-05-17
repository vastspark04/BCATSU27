using UnityEngine;
using UnityEngine.Events;

public class EjectHandle : MonoBehaviour
{
	public Transform handleTransform;

	public VRInteractable interactable;

	private bool ejected;

	private Vector3 startPos;

	private bool grabbed;

	private Transform controllerTransform;

	private VRHandController controller;

	private float lastZVibe;

	public float zVibeInterval = 0.02f;

	public float zThresh = 0.185f;

	public UnityEvent OnHandlePull;

	private void Start()
	{
		if (!interactable)
		{
			interactable = GetComponent<VRInteractable>();
		}
		interactable.OnStartInteraction += Interactable_OnStartInteraction;
		interactable.OnStopInteraction += Interactable_OnStopInteraction;
	}

	private void Interactable_OnStopInteraction(VRHandController controller)
	{
		grabbed = false;
	}

	private void Interactable_OnStartInteraction(VRHandController controller)
	{
		grabbed = true;
		startPos = handleTransform.parent.InverseTransformPoint(controller.transform.position);
		controllerTransform = controller.transform;
		this.controller = controller;
	}

	private void Update()
	{
		if (grabbed)
		{
			float z = (handleTransform.parent.InverseTransformPoint(controllerTransform.position) - startPos).z;
			z = Mathf.Clamp(z, 0f, zThresh);
			Vector3 localPosition = handleTransform.localPosition;
			localPosition.z = z;
			handleTransform.localPosition = localPosition;
			if (Mathf.Abs(lastZVibe - z) > zVibeInterval)
			{
				controller.Vibrate(z / zThresh, 0.04f);
				lastZVibe = z;
			}
			if (!ejected && z >= zThresh * 0.99f)
			{
				ejected = true;
				if (OnHandlePull != null)
				{
					OnHandlePull.Invoke();
				}
				controller.Vibrate(1f, 1f);
			}
		}
		else
		{
			handleTransform.localPosition = Vector3.Lerp(handleTransform.localPosition, Vector3.zero, 10f * Time.deltaTime);
		}
	}
}
