using UnityEngine;
using UnityEngine.Events;

public class VRJoystick : MonoBehaviour
{
	public enum ControlModes
	{
		Rotation,
		Position
	}

	public ControlModes controlMode;

	public Vector3 pyrStickLimits = new Vector3(30f, 30f, 30f);

	public float sensitivity = 1f;

	public Transform frameOfReference;

	private VRHandController controller;

	private bool grabbed;

	public bool returnToZeroWhenReleased = true;

	private Vector3 stick;

	private Vector3 stickStartForward;

	private Vector3 rudderStartUp;

	public Vector3Event OnSetStick;

	public Vector3Event OnSetThumbstick;

	public float thumbStickDeadzone = 0.03f;

	private float tsDeadZoneSqr;

	public UnityEvent OnResetThumbstick;

	private bool hasResetThumbstick = true;

	public UnityEvent OnThumbstickButtonDown;

	public UnityEvent OnThumbstickButtonUp;

	public UnityEvent OnThumbstickButton;

	public UnityEvent OnThumbstickTouch;

	public UnityEvent OnThumbstuckUntouch;

	private bool stickTouching;

	public UnityEvent OnMenuButtonDown;

	public UnityEvent OnMenuButtonUp;

	public UnityEvent OnSecondButtonDown;

	public UnityEvent OnSecondButtonUp;

	public FloatEvent OnTriggerAxis;

	public UnityEvent OnTriggerDown;

	public UnityEvent OnTriggerUp;

	public FloatEvent OnSetSteer;

	public bool sendEvents = true;

	public bool alwaysSendTriggerEvents;

	public bool alwaysSendMenuButtonEvents;

	private Vector3 ctrlerOffset;

	public Transform pitchTransform;

	public Transform rollTransform;

	public Transform yawTransform;

	private bool thumbStickMode;

	private bool thumbRudder;

	private bool hardwareRudder;

	public bool debug;

	public bool doVibration = true;

	private Transform playAreaTransform;

	private bool playAreaRelative = true;

	private Vector3 playAreaRelPitchPos;

	private float hapticFactor = 1f;

	private float _triggerAxis;

	private bool _triggerPressed;

	private bool remoteOnly;

	private Vector3 finalPyrLimits => pyrStickLimits / sensitivity;

	public Vector3 CurrentStick => stick;

	public bool holdingThumbstickButton { get; private set; }

	public bool isMenuButtonPressed { get; private set; }

	public bool isSecondButtonPressed { get; private set; }

	public float triggerAxis
	{
		get
		{
			if (grabbed)
			{
				return _triggerAxis;
			}
			return 0f;
		}
	}

	public bool isTriggerPressed => _triggerPressed;

	private void Start()
	{
		VRInteractable component = GetComponent<VRInteractable>();
		component.OnStartInteraction += I_OnStartInteraction;
		component.OnStopInteraction += I_OnStopInteraction;
		component.tapOrHold = GameSettings.CurrentSettings.GetBoolSetting("TAP_TOGGLE_GRIP");
		component.toggle = component.tapOrHold;
		thumbStickMode = GameSettings.IsThumbstickMode();
		bool flag = true;
		VehicleInputManager componentInParent = GetComponentInParent<VehicleInputManager>();
		if ((bool)componentInParent)
		{
			flag = componentInParent.thumbRudderAllowed;
		}
		thumbRudder = flag && GameSettings.CurrentSettings.GetBoolSetting("THUMB_RUDDER");
		hardwareRudder = GameSettings.CurrentSettings.GetBoolSetting("HARDWARE_RUDDER");
		hapticFactor = GameSettings.CurrentSettings.GetFloatSetting("CONTROL_HAPTICS") / 100f;
		tsDeadZoneSqr = thumbStickDeadzone * thumbStickDeadzone;
	}

	private void I_OnStartInteraction(VRHandController controller)
	{
		this.controller = controller;
		if (controlMode == ControlModes.Rotation)
		{
			stickStartForward = controller.transform.InverseTransformDirection(pitchTransform.parent.up);
		}
		else if (controlMode == ControlModes.Position)
		{
			Vector3 vector = pitchTransform.parent.TransformPoint(new Vector3(0f, pitchTransform.parent.InverseTransformPoint(base.transform.position).y, 0f));
			ctrlerOffset = pitchTransform.parent.InverseTransformVector(vector - controller.transform.position);
			if (playAreaRelative)
			{
				playAreaTransform = controller.transform.parent.parent;
				playAreaRelPitchPos = playAreaTransform.InverseTransformPoint(pitchTransform.position);
			}
		}
		rudderStartUp = controller.transform.InverseTransformDirection(pitchTransform.parent.forward);
		grabbed = true;
		if ((bool)controller.gloveAnimation)
		{
			controller.gloveAnimation.SetJoystick(this);
		}
	}

	private void I_OnStopInteraction(VRHandController controller)
	{
		this.controller = null;
		grabbed = false;
		if (_triggerAxis > 0f)
		{
			_triggerAxis = 0f;
			OnTriggerAxis?.Invoke(_triggerAxis);
		}
		if (isTriggerPressed)
		{
			_triggerPressed = false;
			OnTriggerUp?.Invoke();
		}
		if (holdingThumbstickButton)
		{
			holdingThumbstickButton = false;
			OnThumbstickButtonUp?.Invoke();
		}
		if (stickTouching)
		{
			stickTouching = false;
			OnThumbstuckUntouch?.Invoke();
		}
		if (!hasResetThumbstick)
		{
			hasResetThumbstick = true;
			OnResetThumbstick?.Invoke();
		}
		if (isSecondButtonPressed)
		{
			isSecondButtonPressed = false;
			OnSecondButtonUp?.Invoke();
		}
		if (isMenuButtonPressed)
		{
			isMenuButtonPressed = false;
			OnMenuButtonUp?.Invoke();
		}
	}

	public void DisableEvents()
	{
		sendEvents = false;
	}

	private void Update()
	{
		if (grabbed)
		{
			if (!remoteOnly)
			{
				Vector3 vector = stick;
				Vector3 vector2 = pitchTransform.parent.InverseTransformDirection(controller.transform.TransformDirection(stickStartForward));
				Vector3 vector3 = pitchTransform.parent.InverseTransformDirection(controller.transform.TransformDirection(rudderStartUp));
				if (controlMode == ControlModes.Rotation)
				{
					if (!frameOfReference)
					{
						frameOfReference = base.transform;
					}
					controller.transform.parent.InverseTransformVector(frameOfReference.forward);
					controller.transform.parent.InverseTransformVector(frameOfReference.right);
					stick.z = Mathf.Sign(Vector3.Dot(Vector3.Cross(Vector3.up, vector2), Vector3.forward)) * Vector3.Angle(Vector3.up, Vector3.ProjectOnPlane(vector2, Vector3.forward));
					stick.x = Mathf.Sign(Vector3.Dot(Vector3.Cross(Vector3.up, vector2), Vector3.right)) * Vector3.Angle(Vector3.up, Vector3.ProjectOnPlane(vector2, Vector3.right));
				}
				else if (controlMode == ControlModes.Position)
				{
					Vector3 vector4 = ((!playAreaRelative) ? (pitchTransform.parent.InverseTransformPoint(controller.transform.position) + ctrlerOffset) : (Quaternion.FromToRotation(Vector3.forward, pitchTransform.parent.InverseTransformDirection(playAreaTransform.forward)) * (playAreaTransform.InverseTransformPoint(controller.transform.position) - playAreaRelPitchPos) + ctrlerOffset));
					Vector3 vector5 = vector4;
					vector5.x = 0f;
					Vector3 vector6 = vector4;
					vector6.z = 0f;
					stick.x = Mathf.Sign(Vector3.Dot(vector5, Vector3.forward)) * Vector3.Angle(vector5, Vector3.up);
					stick.z = (0f - Mathf.Sign(Vector3.Dot(vector6, Vector3.right))) * Vector3.Angle(vector6, Vector3.up);
				}
				stick.y = Mathf.Sign(Vector3.Dot(Vector3.Cross(Vector3.forward, vector3), Vector3.up)) * Vector3.Angle(Vector3.forward, Vector3.ProjectOnPlane(vector3, Vector3.up));
				stick.x = Mathf.Clamp(stick.x / finalPyrLimits.x, -1f, 1f);
				stick.y = Mathf.Clamp(stick.y / finalPyrLimits.y, -1f, 1f);
				stick.z = Mathf.Clamp(stick.z / finalPyrLimits.z, -1f, 1f);
				if (thumbRudder)
				{
					stick.y = 0f;
				}
				if (doVibration)
				{
					float num = Mathf.Clamp(hapticFactor * 5500f * (vector - stick).magnitude, 0f, 3500f) / 3500f;
					if (num > 0.0014f)
					{
						controller.HapticPulse(num);
					}
				}
				if (sendEvents)
				{
					if (OnSetStick != null)
					{
						OnSetStick.Invoke(stick);
					}
					if (OnSetSteer != null)
					{
						OnSetSteer.Invoke(stick.y);
					}
					if (thumbStickMode)
					{
						Vector2 stickAxis = controller.stickAxis;
						if (stickAxis.sqrMagnitude > tsDeadZoneSqr)
						{
							stickAxis = Vector2.MoveTowards(stickAxis, Vector2.zero, thumbStickDeadzone);
							stickAxis *= 1f / (1f - thumbStickDeadzone);
							if (OnSetThumbstick != null)
							{
								OnSetThumbstick.Invoke(stickAxis);
							}
							hasResetThumbstick = false;
						}
						else if (!hasResetThumbstick)
						{
							if (OnResetThumbstick != null)
							{
								OnResetThumbstick.Invoke();
							}
							hasResetThumbstick = true;
						}
						if (controller.GetStickPressDown())
						{
							if (OnThumbstickButtonDown != null)
							{
								OnThumbstickButtonDown.Invoke();
							}
						}
						else if (controller.GetStickPressUp() && OnThumbstickButtonUp != null)
						{
							OnThumbstickButtonUp.Invoke();
						}
						if (controller.stickPressed && OnThumbstickButton != null)
						{
							OnThumbstickButton.Invoke();
						}
						if (controller.stickTouched != stickTouching)
						{
							stickTouching = controller.stickTouched;
							if (stickTouching)
							{
								if (OnThumbstickTouch != null)
								{
									OnThumbstickTouch.Invoke();
								}
							}
							else if (OnThumbstuckUntouch != null)
							{
								OnThumbstuckUntouch.Invoke();
							}
						}
					}
					else
					{
						if (controller.GetStickPressDown())
						{
							Vector3 vector7 = controller.stickAxis;
							if (vector7.x > -0.35f && vector7.x < 0.35f && vector7.y < 0.35f && vector7.y > -0.35f)
							{
								if (OnThumbstickButtonDown != null)
								{
									OnThumbstickButtonDown.Invoke();
								}
								holdingThumbstickButton = true;
							}
							if (OnThumbstickTouch != null)
							{
								OnThumbstickTouch.Invoke();
							}
						}
						if (controller.GetStickPressUp())
						{
							if (holdingThumbstickButton)
							{
								holdingThumbstickButton = false;
								if (OnThumbstickButtonUp != null)
								{
									OnThumbstickButtonUp.Invoke();
								}
							}
							else if (OnResetThumbstick != null)
							{
								OnResetThumbstick.Invoke();
							}
							if (OnThumbstuckUntouch != null)
							{
								OnThumbstuckUntouch.Invoke();
							}
						}
						else if (controller.stickPressed)
						{
							if (holdingThumbstickButton)
							{
								if (OnThumbstickButton != null)
								{
									OnThumbstickButton.Invoke();
								}
							}
							else
							{
								Vector3 arg = controller.stickAxis;
								if (OnSetThumbstick != null)
								{
									OnSetThumbstick.Invoke(arg);
								}
							}
						}
					}
				}
			}
			if ((sendEvents && !remoteOnly) || alwaysSendTriggerEvents)
			{
				if (OnTriggerAxis != null)
				{
					_triggerAxis = controller.triggerAxis;
					OnTriggerAxis.Invoke(_triggerAxis);
				}
				if (controller.GetTriggerClickDown())
				{
					_triggerPressed = true;
					if (OnTriggerDown != null)
					{
						OnTriggerDown.Invoke();
					}
				}
				else if (controller.GetTriggerClickUp())
				{
					_triggerPressed = false;
					if (OnTriggerUp != null)
					{
						OnTriggerUp.Invoke();
					}
				}
			}
			if ((sendEvents && !remoteOnly) || alwaysSendMenuButtonEvents)
			{
				if (controller.GetThumbButtonDown())
				{
					isMenuButtonPressed = true;
					if (OnMenuButtonDown != null)
					{
						OnMenuButtonDown.Invoke();
					}
				}
				else if (controller.GetThumbButtonUp())
				{
					isMenuButtonPressed = false;
					if (OnMenuButtonUp != null)
					{
						OnMenuButtonUp.Invoke();
					}
				}
				if (controller.GetSecondButtonDown())
				{
					isSecondButtonPressed = true;
					OnSecondButtonDown?.Invoke();
				}
				else if (controller.GetSecondButtonUp())
				{
					isSecondButtonPressed = false;
					OnSecondButtonUp?.Invoke();
				}
			}
		}
		else if (debug)
		{
			stick = Vector3.zero;
			if (Input.GetKey(KeyCode.W))
			{
				stick.x += 1f;
			}
			if (Input.GetKey(KeyCode.S))
			{
				stick.x -= 1f;
			}
			if (Input.GetKey(KeyCode.D))
			{
				stick.y += 1f;
			}
			if (Input.GetKey(KeyCode.A))
			{
				stick.y -= 1f;
			}
			if (Input.GetKey(KeyCode.E))
			{
				stick.z -= 1f;
			}
			if (Input.GetKey(KeyCode.Q))
			{
				stick.z += 1f;
			}
			if (OnSetStick != null)
			{
				OnSetStick.Invoke(stick);
			}
			if (OnSetSteer != null)
			{
				OnSetSteer.Invoke(stick.y);
			}
		}
		else
		{
			if (!remoteOnly && returnToZeroWhenReleased)
			{
				stick = Vector3.Lerp(stick, Vector3.zero, 20f * Time.deltaTime);
			}
			if (sendEvents)
			{
				if (OnSetStick != null)
				{
					OnSetStick.Invoke(stick);
				}
				if (OnSetSteer != null)
				{
					OnSetSteer.Invoke(stick.y);
				}
			}
		}
		SetStickAnimation();
	}

	private void SetStickAnimation()
	{
		pitchTransform.localRotation = Quaternion.Euler(new Vector3(stick.x, 0f, 0f) * finalPyrLimits.x);
		rollTransform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, stick.z) * finalPyrLimits.z);
		if (!thumbRudder && !hardwareRudder)
		{
			yawTransform.localRotation = Quaternion.Euler(new Vector3(0f, stick.y, 0f) * finalPyrLimits.y);
		}
	}

	public void RemoteSetStick(Vector3 pyr)
	{
		stick = pyr;
	}

	public void SetRemoteOnly(bool r)
	{
		remoteOnly = r;
	}
}
