using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(VRInteractable))]
public class VRThrottle : MonoBehaviour, IQSVehicleComponent
{
	public enum ThrottleTypes
	{
		Throttle,
		HeliPower,
		Collective
	}

	public Transform throttleTransform;

	public bool rotationMode;

	public float maxOffset;

	public float defaultThrottle;

	public float throttleLimiter = 1f;

	public float minThrottle;

	public float throttleVibeInterval = 0.025f;

	private float lastVibeT;

	public float throttleClickInterval = 0.05f;

	private float lastClickT;

	public AudioClip throttleClickSound;

	public AudioSource audioSource;

	public bool sendEvents = true;

	public bool alwaysSendButtonEvents;

	public FloatEvent OnSetThrottle;

	public Vector3Event OnThumbstickClicked;

	public UnityEvent OnStickPressDown;

	public UnityEvent OnStickPressUp;

	public UnityEvent OnStickPressed;

	public ThrottleTypes throttleType;

	public UnityEvent OnMenuButtonDown;

	public UnityEvent OnMenuButtonUp;

	public FloatEvent OnTriggerAxis;

	public UnityEvent OnTriggerDown;

	public UnityEvent OnTriggerUp;

	public Vector3Event OnSetThumbstick;

	public float thumbStickDeadzone = 0.03f;

	private float tsDeadZoneSqr;

	public UnityEvent OnResetThumbstick;

	private bool hasResetThumbstick = true;

	public float triggerDeadZone = 0.05f;

	private VRHandController controller;

	private bool grabbed;

	private bool triggerIsDown;

	private bool mButtonIsDown;

	private Vector3 ctrlerOffset;

	private bool thumbStickMode;

	public bool abGate;

	public float abGateThreshold = 0.74f;

	public float abPostGateWidth = 0.1f;

	public bool invertOutput;

	public AudioSource gateAudioSource;

	public AudioClip gateABSound;

	public AudioClip gateMilSound;

	private float timePressedTouchpad;

	private float thumbpadPressThresh = 0.2f;

	private float hapticFactor = 1f;

	private bool remoteOnly;

	private VRInteractable i;

	public bool applyDefaultNoEvents;

	private Transform playAreaTransform;

	private Vector3 playAreaRelZeroPos;

	private bool holdingThumbstickButton;

	private float throttle;

	public bool smoothThrottle;

	public float smoothRate = 7f;

	private float f_smoothThrottle;

	private float smoothRemoteT;

	private bool belowGate = true;

	private float gateAnimThrottle;

	public VRInteractable interactable => i;

	public bool skipApplyDefaults { get; set; }

	public float currentThrottle => throttle;

	public bool IsTriggerPressed()
	{
		return triggerIsDown;
	}

	public void SetRemoteOnly(bool r)
	{
		remoteOnly = r;
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)throttleTransform && !rotationMode)
		{
			Gizmos.DrawLine(throttleTransform.position, throttleTransform.position + maxOffset * throttleTransform.forward);
		}
	}

	private void Awake()
	{
		i = GetComponent<VRInteractable>();
		if (!skipApplyDefaults)
		{
			if (applyDefaultNoEvents)
			{
				RemoteSetThrottleNoEvents(defaultThrottle);
			}
			else
			{
				RemoteSetThrottle(defaultThrottle);
			}
		}
	}

	private void Start()
	{
		i.OnStartInteraction += I_OnStartInteraction;
		i.OnStopInteraction += I_OnStopInteraction;
		i.tapOrHold = GameSettings.CurrentSettings.GetBoolSetting("TAP_TOGGLE_GRIP");
		i.toggle = i.tapOrHold;
		thumbStickMode = GameSettings.IsThumbstickMode();
		hapticFactor = GameSettings.CurrentSettings.GetFloatSetting("CONTROL_HAPTICS") / 100f;
		tsDeadZoneSqr = thumbStickDeadzone * thumbStickDeadzone;
	}

	private void I_OnStartInteraction(VRHandController controller)
	{
		this.controller = controller;
		if (rotationMode)
		{
			Vector3 position = throttleTransform.InverseTransformPoint(base.transform.position);
			position.y = 0f;
			ctrlerOffset = throttleTransform.parent.InverseTransformVector(throttleTransform.TransformPoint(position) - controller.transform.position);
		}
		else
		{
			ctrlerOffset = throttleTransform.localPosition - throttleTransform.parent.InverseTransformPoint(controller.transform.position);
		}
		playAreaTransform = controller.transform.parent.parent;
		playAreaRelZeroPos = playAreaTransform.InverseTransformPoint(throttleTransform.parent.position);
		grabbed = true;
		if ((bool)controller.gloveAnimation)
		{
			controller.gloveAnimation.SetThrottle(this);
		}
	}

	private void I_OnStopInteraction(VRHandController controller)
	{
		this.controller = null;
		grabbed = false;
		if ((sendEvents && !remoteOnly) || alwaysSendButtonEvents)
		{
			if (OnTriggerAxis != null)
			{
				OnTriggerAxis.Invoke(0f);
			}
			if (triggerIsDown && OnTriggerUp != null)
			{
				OnTriggerUp.Invoke();
			}
			if (mButtonIsDown && OnMenuButtonUp != null)
			{
				OnMenuButtonUp.Invoke();
			}
			if (!hasResetThumbstick)
			{
				hasResetThumbstick = true;
				OnSetThumbstick?.Invoke(Vector3.zero);
				OnResetThumbstick?.Invoke();
			}
		}
		triggerIsDown = false;
		mButtonIsDown = false;
	}

	public void DisableEvents()
	{
		sendEvents = false;
	}

	public void RemoteSetThrottle(float t)
	{
		throttle = t;
		if (smoothThrottle)
		{
			f_smoothThrottle = t;
		}
		belowGate = t < abGateThreshold + 0.01f;
		gateAnimThrottle = t;
		UpdateThrottle(t);
		if (!remoteOnly)
		{
			UpdateThrottleAnim(t);
		}
		if ((bool)audioSource && Mathf.Abs(throttle - lastClickT) > throttleClickInterval)
		{
			lastClickT = throttle;
			audioSource.volume = throttle;
			audioSource.PlayOneShot(throttleClickSound);
		}
	}

	public void RemoteSetThrottleForceEvents(float t)
	{
		bool flag = sendEvents;
		sendEvents = true;
		RemoteSetThrottle(t);
		sendEvents = flag;
	}

	public void RemoteSetThrottleNoEvents(float t)
	{
		throttle = t;
		if (smoothThrottle)
		{
			f_smoothThrottle = t;
		}
		belowGate = t < abGateThreshold + 0.01f;
		gateAnimThrottle = t;
		if (!remoteOnly)
		{
			UpdateThrottleAnim(t);
		}
	}

	public void RemoteAdjustThrottle(float amt)
	{
		throttle = Mathf.Clamp01(throttle + amt);
		UpdateThrottle(throttle);
		UpdateThrottleAnim(throttle);
		if ((bool)audioSource && Mathf.Abs(throttle - lastClickT) > throttleClickInterval)
		{
			lastClickT = throttle;
			audioSource.volume = throttle;
			audioSource.PlayOneShot(throttleClickSound);
		}
	}

	private void UpdateThrottleAnim(float throttle)
	{
		if (rotationMode)
		{
			throttleTransform.localRotation = Quaternion.AngleAxis(throttle * maxOffset, -Vector3.right);
		}
		else
		{
			throttleTransform.localPosition = throttle * maxOffset * Vector3.forward;
		}
	}

	private void UpdateThrottle(float throttle)
	{
		if (sendEvents)
		{
			if (invertOutput)
			{
				OnSetThrottle?.Invoke(1f - throttle);
			}
			else
			{
				OnSetThrottle?.Invoke(throttle);
			}
		}
	}

	private void UpdateButtonEvents()
	{
		if ((!sendEvents && !alwaysSendButtonEvents) || !controller)
		{
			return;
		}
		if (controller.GetThumbButtonDown())
		{
			mButtonIsDown = true;
			if (OnMenuButtonDown != null)
			{
				OnMenuButtonDown.Invoke();
			}
		}
		if (controller.GetThumbButtonUp())
		{
			mButtonIsDown = false;
			if (OnMenuButtonUp != null)
			{
				OnMenuButtonUp.Invoke();
			}
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
				if (OnSetThumbstick != null)
				{
					OnSetThumbstick.Invoke(Vector3.zero);
				}
				if (OnResetThumbstick != null)
				{
					OnResetThumbstick.Invoke();
				}
				hasResetThumbstick = true;
			}
			if (controller.stickPressDown)
			{
				OnStickPressDown?.Invoke();
			}
			if (controller.stickPressUp)
			{
				OnStickPressUp?.Invoke();
			}
			if (controller.stickPressed)
			{
				OnStickPressed?.Invoke();
			}
		}
		else
		{
			if (controller.GetStickPressDown())
			{
				Vector3 vector = controller.stickAxis;
				if (vector.x > -0.35f && vector.x < 0.35f && vector.y < 0.35f && vector.y > -0.35f)
				{
					OnStickPressDown?.Invoke();
					holdingThumbstickButton = true;
				}
			}
			if (controller.GetStickPressUp())
			{
				if (holdingThumbstickButton)
				{
					holdingThumbstickButton = false;
					OnStickPressUp?.Invoke();
				}
				else
				{
					if (OnSetThumbstick != null)
					{
						OnSetThumbstick.Invoke(Vector3.zero);
					}
					if (OnResetThumbstick != null)
					{
						OnResetThumbstick.Invoke();
					}
					hasResetThumbstick = true;
				}
			}
			else if (controller.stickPressed)
			{
				if (holdingThumbstickButton)
				{
					OnStickPressed?.Invoke();
				}
				else
				{
					Vector3 arg = controller.stickAxis;
					hasResetThumbstick = false;
					if (OnSetThumbstick != null)
					{
						OnSetThumbstick.Invoke(arg);
					}
				}
			}
		}
		if (controller.GetTriggerClickDown())
		{
			triggerIsDown = true;
			if (OnTriggerDown != null)
			{
				OnTriggerDown.Invoke();
			}
		}
		else if (controller.GetTriggerClickUp())
		{
			triggerIsDown = false;
			if (OnTriggerUp != null)
			{
				OnTriggerUp.Invoke();
			}
		}
		if (OnTriggerAxis != null)
		{
			float triggerAxis = controller.triggerAxis;
			if (triggerAxis > triggerDeadZone)
			{
				triggerAxis = (triggerAxis - triggerDeadZone) * (1f / (1f - triggerDeadZone));
				OnTriggerAxis.Invoke(triggerAxis);
			}
			else
			{
				OnTriggerAxis.Invoke(0f);
			}
		}
	}

	private void Update()
	{
		if (smoothThrottle)
		{
			f_smoothThrottle = Mathf.Lerp(f_smoothThrottle, throttle, smoothRate * Time.deltaTime);
		}
		if (!remoteOnly)
		{
			smoothRemoteT = throttle;
		}
		if (grabbed)
		{
			if (!remoteOnly)
			{
				if (rotationMode)
				{
					Vector3 to = throttleTransform.parent.InverseTransformPoint(controller.transform.position) + ctrlerOffset;
					to += throttleTransform.parent.InverseTransformVector(playAreaTransform.TransformVector(playAreaTransform.InverseTransformPoint(throttleTransform.parent.position) - playAreaRelZeroPos));
					to.x = 0f;
					to.y = Mathf.Max(0f, to.y);
					float value = Vector3.Angle(Vector3.forward, to);
					value = Mathf.Clamp(value, 0f, maxOffset);
					throttle = value / maxOffset;
					throttle = Mathf.Clamp(throttle, minThrottle, Mathf.Clamp01(throttleLimiter));
				}
				else
				{
					float num = Mathf.Clamp((throttleTransform.parent.InverseTransformPoint(controller.transform.position) + ctrlerOffset + throttleTransform.parent.InverseTransformVector(playAreaTransform.TransformVector(playAreaTransform.InverseTransformPoint(throttleTransform.parent.position) - playAreaRelZeroPos))).z, 0f, maxOffset);
					throttle = num / maxOffset;
					throttle = Mathf.Clamp(throttle, minThrottle, Mathf.Clamp01(throttleLimiter));
				}
				if (abGate)
				{
					bool flag = false;
					if (throttle > abGateThreshold && throttle < 1f - abPostGateWidth)
					{
						gateAnimThrottle = Mathf.Lerp(gateAnimThrottle, Mathf.Lerp(abGateThreshold, throttle, 0.25f), 25f * Time.deltaTime);
						throttle = abGateThreshold;
						UpdateThrottle(throttle);
						UpdateThrottleAnim(gateAnimThrottle);
						flag = true;
					}
					else if (throttle >= 1f - abPostGateWidth)
					{
						throttle = 1f;
						gateAnimThrottle = Mathf.Lerp(gateAnimThrottle, 1f, 25f * Time.deltaTime);
						UpdateThrottleAnim(gateAnimThrottle);
						UpdateThrottle(throttle);
						flag = true;
					}
					else
					{
						gateAnimThrottle = throttle;
					}
					if (throttle > abGateThreshold + 0.01f)
					{
						if (belowGate)
						{
							belowGate = false;
							if ((bool)gateAudioSource)
							{
								gateAudioSource.PlayOneShot(gateABSound);
							}
							controller.HapticPulse(hapticFactor);
						}
					}
					else if (!belowGate)
					{
						belowGate = true;
						if ((bool)gateAudioSource)
						{
							gateAudioSource.PlayOneShot(gateMilSound);
						}
						controller.HapticPulse(hapticFactor);
					}
					if (flag)
					{
						UpdateButtonEvents();
						return;
					}
				}
				float num2 = throttle;
				if (smoothThrottle)
				{
					num2 = f_smoothThrottle;
				}
				if (Mathf.Abs(throttle - lastVibeT) > throttleVibeInterval)
				{
					lastVibeT = throttle;
					if ((bool)controller)
					{
						controller.HapticPulse(throttle * hapticFactor);
					}
				}
				if ((bool)audioSource && Mathf.Abs(throttle - lastClickT) > throttleClickInterval)
				{
					lastClickT = throttle;
					audioSource.volume = throttle;
					audioSource.PlayOneShot(throttleClickSound);
				}
				UpdateThrottleAnim(throttle);
				UpdateThrottle(num2);
				UpdateButtonEvents();
			}
			if (remoteOnly && alwaysSendButtonEvents)
			{
				UpdateButtonEvents();
			}
		}
		else if (!remoteOnly && abGate)
		{
			if (throttle >= 1f - abPostGateWidth)
			{
				gateAnimThrottle = Mathf.Lerp(gateAnimThrottle, 1f, 25f * Time.deltaTime);
				UpdateThrottleAnim(gateAnimThrottle);
			}
			else if (gateAnimThrottle > abGateThreshold)
			{
				gateAnimThrottle = Mathf.Lerp(gateAnimThrottle, abGateThreshold, 15f * Time.deltaTime);
				UpdateThrottleAnim(gateAnimThrottle);
			}
		}
		if (remoteOnly)
		{
			smoothRemoteT = Mathf.Lerp(smoothRemoteT, throttle, 8f * Time.deltaTime);
			UpdateThrottleAnim(smoothRemoteT);
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_VRThrottle");
		configNode.SetValue("throttle", throttle);
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = base.gameObject.name + "_VRThrottle";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			RemoteSetThrottleForceEvents(ConfigNodeUtils.ParseFloat(node.GetValue("throttle")));
		}
	}
}
