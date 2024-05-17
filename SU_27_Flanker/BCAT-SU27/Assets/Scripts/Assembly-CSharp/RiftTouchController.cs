using System;
using UnityEngine;

public class RiftTouchController : MonoBehaviour
{
	public Transform cameraRig;

	public GloveAnimation gloveAnim;

	public Transform gloveOriginTransform;

	public bool isLeft;

	private OVRInput.Controller ctrlr;

	private OVRInput.RawButton triggerButton;

	private OVRInput.RawAxis1D triggerAxisInput;

	private OVRInput.RawButton thumbButton;

	private OVRInput.RawButton secondaryThumbButton;

	private OVRInput.RawAxis2D stickAxisInput;

	private OVRInput.RawTouch stickTouchInput;

	private OVRInput.RawButton stickButton;

	private OVRInput.RawButton gripButton;

	[Header("Button Statuses")]
	public bool triggerPressed;

	public float triggerAxis;

	public bool thumbButtonPressed;

	public bool secondaryThumbButtonPressed;

	public Vector2 stickAxis;

	public bool stickTouched;

	public bool stickPressed;

	public bool gripPressed;

	public Vector3 velocity;

	public Vector3 angularVelocity;

	private bool triggerClickDown;

	private bool triggerClickUp;

	private bool stickButtonDown;

	private bool stickButtonUp;

	private bool thumbButtonDown;

	private bool thumbButtonUp;

	public event Action OnTriggerPressed;

	public event Action OnTriggerReleased;

	public event Action<float> OnTriggerAxis;

	public event Action OnThumbButtonPressed;

	public event Action OnThumbButtonReleased;

	public event Action OnSecondaryThumbButtonPressed;

	public event Action OnSecondaryThumbButtonReleased;

	public event Action OnStickTouched;

	public event Action OnStickUntouched;

	public event Action<Vector2> OnStickAxis;

	public event Action OnStickPressed;

	public event Action OnStickUnpressed;

	public event Action OnGripPressed;

	public event Action OnGripReleased;

	public OVRInput.Controller GetOVRController()
	{
		return ctrlr;
	}

	private void Start()
	{
		if (!GameSettings.VR_SDK_IS_OCULUS)
		{
			base.enabled = false;
			return;
		}
		if (isLeft)
		{
			ctrlr = OVRInput.Controller.LTouch;
			triggerButton = OVRInput.RawButton.LIndexTrigger;
			triggerAxisInput = OVRInput.RawAxis1D.LIndexTrigger;
			thumbButton = OVRInput.RawButton.Y;
			secondaryThumbButton = OVRInput.RawButton.X;
			stickAxisInput = OVRInput.RawAxis2D.LThumbstick;
			stickTouchInput = OVRInput.RawTouch.LThumbstick;
			stickButton = OVRInput.RawButton.LThumbstick;
			gripButton = OVRInput.RawButton.LHandTrigger;
		}
		else
		{
			ctrlr = OVRInput.Controller.RTouch;
			triggerButton = OVRInput.RawButton.RIndexTrigger;
			triggerAxisInput = OVRInput.RawAxis1D.RIndexTrigger;
			thumbButton = OVRInput.RawButton.B;
			secondaryThumbButton = OVRInput.RawButton.A;
			stickAxisInput = OVRInput.RawAxis2D.RThumbstick;
			stickTouchInput = OVRInput.RawTouch.RThumbstick;
			stickButton = OVRInput.RawButton.RThumbstick;
			gripButton = OVRInput.RawButton.RHandTrigger;
		}
		gloveAnim.transform.localPosition = gloveOriginTransform.localPosition;
		gloveAnim.transform.localRotation = gloveOriginTransform.localRotation;
		gloveAnim.SetOriginTransform(gloveOriginTransform);
	}

	private void Update()
	{
		Vector3 localControllerPosition = OVRInput.GetLocalControllerPosition(ctrlr);
		Quaternion localControllerRotation = OVRInput.GetLocalControllerRotation(ctrlr);
		base.transform.position = cameraRig.TransformPoint(localControllerPosition);
		base.transform.rotation = cameraRig.rotation * localControllerRotation;
		velocity = OVRInput.GetLocalControllerVelocity(ctrlr);
		angularVelocity = OVRInput.GetLocalControllerAngularVelocity(ctrlr);
		if (OVRInput.GetDown(triggerButton, ctrlr))
		{
			triggerClickDown = true;
			triggerPressed = true;
			if (this.OnTriggerPressed != null)
			{
				this.OnTriggerPressed();
			}
		}
		else
		{
			triggerClickDown = false;
		}
		if (OVRInput.GetUp(triggerButton, ctrlr))
		{
			triggerClickUp = true;
			triggerPressed = false;
			if (this.OnTriggerReleased != null)
			{
				this.OnTriggerReleased();
			}
		}
		else
		{
			triggerClickUp = false;
		}
		triggerAxis = OVRInput.Get(triggerAxisInput, ctrlr);
		if (this.OnTriggerAxis != null)
		{
			this.OnTriggerAxis(triggerAxis);
		}
		if (OVRInput.GetDown(thumbButton, ctrlr))
		{
			thumbButtonDown = true;
			thumbButtonPressed = true;
			if (this.OnThumbButtonPressed != null)
			{
				this.OnThumbButtonPressed();
			}
		}
		else
		{
			thumbButtonDown = false;
		}
		if (OVRInput.GetUp(thumbButton, ctrlr))
		{
			thumbButtonUp = true;
			thumbButtonPressed = false;
			if (this.OnThumbButtonReleased != null)
			{
				this.OnThumbButtonReleased();
			}
		}
		else
		{
			thumbButtonUp = false;
		}
		if (OVRInput.GetDown(secondaryThumbButton, ctrlr))
		{
			secondaryThumbButtonPressed = true;
			if (this.OnSecondaryThumbButtonPressed != null)
			{
				this.OnSecondaryThumbButtonPressed();
			}
		}
		if (OVRInput.GetUp(secondaryThumbButton, ctrlr))
		{
			secondaryThumbButtonPressed = false;
			if (this.OnSecondaryThumbButtonReleased != null)
			{
				this.OnSecondaryThumbButtonReleased();
			}
		}
		if (OVRInput.GetDown(stickTouchInput, ctrlr))
		{
			stickTouched = true;
			if (this.OnStickTouched != null)
			{
				this.OnStickTouched();
			}
		}
		if (OVRInput.GetUp(stickTouchInput, ctrlr))
		{
			stickTouched = false;
			if (this.OnStickUntouched != null)
			{
				this.OnStickUntouched();
			}
		}
		stickAxis = OVRInput.Get(stickAxisInput, ctrlr);
		if (this.OnStickAxis != null)
		{
			this.OnStickAxis(stickAxis);
		}
		if (OVRInput.GetDown(stickButton, ctrlr))
		{
			stickButtonDown = true;
			stickPressed = true;
			if (this.OnStickPressed != null)
			{
				this.OnStickPressed();
			}
		}
		else
		{
			stickButtonDown = false;
		}
		if (OVRInput.GetUp(stickButton, ctrlr))
		{
			stickButtonUp = true;
			stickPressed = false;
			if (this.OnStickUnpressed != null)
			{
				this.OnStickUnpressed();
			}
		}
		else
		{
			stickButtonUp = false;
		}
		if (OVRInput.GetDown(gripButton, ctrlr))
		{
			gripPressed = true;
			if (this.OnGripPressed != null)
			{
				this.OnGripPressed();
			}
		}
		if (OVRInput.GetUp(gripButton, ctrlr))
		{
			gripPressed = false;
			if (this.OnGripReleased != null)
			{
				this.OnGripReleased();
			}
		}
	}

	public bool GetTriggerClickDown()
	{
		return triggerClickDown;
	}

	public bool GetTriggerClickUp()
	{
		return triggerClickUp;
	}

	public bool GetStickPressDown()
	{
		return stickButtonDown;
	}

	public bool GetStickPressUp()
	{
		return stickButtonUp;
	}

	public bool GetThumbButtonDown()
	{
		return thumbButtonDown;
	}

	public bool GetThumbButtonUp()
	{
		return thumbButtonUp;
	}
}
