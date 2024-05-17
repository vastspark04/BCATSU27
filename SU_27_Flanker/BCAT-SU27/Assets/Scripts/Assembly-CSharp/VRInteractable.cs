using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class VRInteractable : MonoBehaviour
{
	public enum Buttons
	{
		Grip,
		Trigger,
		Menu,
		Pad,
		Proximity,
		GripPlus
	}

	public delegate void InteractionEvent(VRHandController controller);

	public string interactableName = "Interactable";

	public string controlReferenceName;

	public Buttons button;

	public float radius;

	public bool useRect;

	public bool useRectTransform;

	public Bounds rect;

	public bool requireMotion;

	public Vector3 requiredMotion;

	public bool toggle;

	public bool tapOrHold;

	private const float tapDuration = 0.25f;

	private float tapTime;

	[HideInInspector]
	public float sqrRadius;

	private bool interacting;

	public VRHandController nearbyController;

	public UnityEvent OnInteract;

	public UnityEvent OnStopInteract;

	[Tooltip("Called every frame while object is being held")]
	public UnityEvent OnInteracting;

	public PoseBounds poseBounds;

	private int interactedOnFrame;

	public VRHandController activeController { get; private set; }

	public bool wasInteractedThisFrame => Time.frameCount - interactedOnFrame <= 3;

	public event InteractionEvent OnStartInteraction;

	public event InteractionEvent OnStopInteraction;

	public event InteractionEvent OnHover;

	public event InteractionEvent OnUnHover;

	public string GetControlReferenceName()
	{
		if (string.IsNullOrEmpty(controlReferenceName))
		{
			return interactableName;
		}
		return controlReferenceName;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
		if (useRect)
		{
			Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, base.transform.lossyScale);
			Gizmos.DrawCube(rect.center, rect.size);
			Gizmos.matrix = Matrix4x4.identity;
		}
		else
		{
			Gizmos.DrawSphere(base.transform.position, radius);
		}
	}

	private void Awake()
	{
		foreach (VRHandController controller in VRHandController.controllers)
		{
			AddControllerEvents(controller);
		}
		VRHandController.OnAddController += AddControllerEvents;
		VRHandController.OnRemoveController += RemoveControllerEvents;
		sqrRadius = radius * radius;
	}

	private void OnEnable()
	{
		if (useRectTransform)
		{
			StartCoroutine(RectTransformRoutine());
		}
	}

	private IEnumerator RectTransformRoutine()
	{
		RectTransform rectTf = (RectTransform)base.transform;
		while (base.enabled)
		{
			rect.center = rectTf.rect.center;
			Vector3 size = rectTf.rect.size;
			size.z = rect.size.z;
			rect.size = size;
			yield return null;
		}
	}

	private void Start()
	{
		ControllerEventHandler.RegisterInteractable(this);
	}

	private void AddControllerEvents(VRHandController con)
	{
		switch (button)
		{
		case Buttons.Grip:
			con.OnGripPressed += Click;
			con.OnGripReleased += UnClick;
			break;
		case Buttons.Menu:
			con.OnThumbButtonPressed += Click;
			con.OnThumbButtonPressed += UnClick;
			break;
		case Buttons.Pad:
			con.OnStickPressed += Click;
			con.OnStickUnpressed += UnClick;
			break;
		case Buttons.Trigger:
			con.OnTriggerStageOnePressed += Click;
			con.OnTriggerStageOneReleased += UnClick;
			break;
		case Buttons.GripPlus:
			con.OnGripForcePressed += Click;
			con.OnGripForceReleased += UnClick;
			break;
		case Buttons.Proximity:
			break;
		}
	}

	private void RemoveControllerEvents(VRHandController con)
	{
		switch (button)
		{
		case Buttons.Grip:
			con.OnGripPressed -= Click;
			con.OnGripReleased -= UnClick;
			break;
		case Buttons.Menu:
			con.OnThumbButtonPressed -= Click;
			con.OnThumbButtonPressed -= UnClick;
			break;
		case Buttons.Pad:
			con.OnStickPressed -= Click;
			con.OnStickUnpressed -= UnClick;
			break;
		case Buttons.Trigger:
			con.OnTriggerStageOnePressed -= Click;
			con.OnTriggerStageOneReleased -= UnClick;
			break;
		case Buttons.GripPlus:
			con.OnGripForcePressed -= Click;
			con.OnGripForceReleased -= UnClick;
			break;
		case Buttons.Proximity:
			break;
		}
	}

	private void OnDestroy()
	{
		foreach (VRHandController controller in VRHandController.controllers)
		{
			RemoveControllerEvents(controller);
		}
		ControllerEventHandler.UnegisterInteractable(this);
		VRHandController.OnAddController -= AddControllerEvents;
		VRHandController.OnRemoveController -= RemoveControllerEvents;
	}

	private void UnClick(VRHandController controller)
	{
		if (!(this == null) && controller == activeController)
		{
			if (tapOrHold && Time.time - tapTime > 0.25f)
			{
				toggle = false;
			}
			if (interacting && !toggle)
			{
				controller.activeInteractable = null;
				StopInteraction();
			}
		}
	}

	private void Click(VRHandController controller)
	{
		if (!(this == null) && base.enabled && !ControllerEventHandler.eventsPaused && base.gameObject.activeInHierarchy)
		{
			if (GameSettings.VR_CONTROLLER_STYLE == ControllerStyles.Index)
			{
				tapOrHold = false;
				toggle = false;
			}
			tapTime = Time.time;
			if (tapOrHold && !interacting)
			{
				toggle = true;
			}
			if (interacting && toggle && (bool)controller && controller == activeController)
			{
				controller.activeInteractable = null;
				StopInteraction();
			}
			else if (!interacting && (bool)controller && controller.activeInteractable == null && controller.hoverInteractable == this && (!requireMotion || ValidateMotion(controller)))
			{
				activeController = controller;
				controller.activeInteractable = this;
				StartInteraction();
			}
		}
	}

	public void StartInteraction()
	{
		interacting = true;
		interactedOnFrame = Time.frameCount;
		if (this.OnStartInteraction != null)
		{
			this.OnStartInteraction(activeController);
		}
		if (OnInteract != null)
		{
			OnInteract.Invoke();
		}
		if (base.gameObject.activeInHierarchy && OnInteracting != null)
		{
			StartCoroutine(WhileInteractingRoutine());
		}
	}

	private IEnumerator WhileInteractingRoutine()
	{
		while (interacting)
		{
			OnInteracting.Invoke();
			yield return null;
		}
	}

	public void StopInteraction()
	{
		if ((bool)activeController)
		{
			activeController.activeInteractable = null;
			interacting = false;
			if (this.OnStopInteraction != null)
			{
				this.OnStopInteraction(activeController);
			}
			if (OnStopInteract != null)
			{
				OnStopInteract.Invoke();
			}
			activeController = null;
		}
	}

	public void Hover(VRHandController c)
	{
		nearbyController = c;
		if (this.OnHover != null)
		{
			this.OnHover(c);
		}
		c.Vibrate(0.6f, 0.04f);
	}

	public void UnHover()
	{
		if ((bool)nearbyController && this.OnUnHover != null)
		{
			this.OnUnHover(nearbyController);
		}
		if (button == Buttons.Proximity && activeController == nearbyController)
		{
			UnClick(nearbyController);
		}
		nearbyController = null;
	}

	private bool ValidateMotion(VRHandController c)
	{
		Vector3 velocity = c.velocity;
		velocity = c.transform.parent.TransformVector(velocity);
		Vector3 lhs = Vector3.Project(base.transform.InverseTransformVector(velocity), requiredMotion);
		if (Mathf.Sign(Vector3.Dot(lhs, requiredMotion)) * lhs.magnitude >= requiredMotion.magnitude)
		{
			return true;
		}
		return false;
	}
}
