using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.CrashReportHandler;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using Valve.VR;
using VTNetworking;

public class VRHandController : MonoBehaviour
{
	public delegate void VRHandControllerDelegate(VRHandController ctrlr);

	public delegate void VRHandControllerAxisDelegate(VRHandController ctlr, float axis);

	public delegate void VRHandControllerVectorDelegate(VRHandController ctrlr, Vector2 vector);

	public static List<VRHandController> controllers = new List<VRHandController>();

	private VTSteamVRController steamVRInputController;

	private RiftTouchController riftController;

	public bool isLeft;

	public GloveAnimation gloveAnimation;

	[Header("VRInteractable")]
	public Transform interactionTransform;

	public VRInteractable hoverInteractable;

	public VRInteractable overrideHoverInteractable;

	public VRInteractable activeInteractable;

	[Header("Control statuses")]
	public bool triggerClicked;

	public bool triggerStageOnePressed;

	public float triggerAxis;

	private const float TRIGGER_STAGE_THRESHOLD = 0.15f;

	private const float TRIGGER_STAGE_UNCLICK_THRESHOLD = 0.05f;

	private const float TRIGGER_RIFT_CLICK_THRESHOLD = 0.9f;

	public bool gripPressed;

	public bool gripForcePressed;

	public bool thumbButtonPressed;

	public bool secondaryThumbButtonPressed;

	private bool i_stickPressed;

	private bool wasStickPressed;

	public Vector3 velocity;

	public Vector3 angularVelocity;

	private bool isOculus;

	private bool isLegacySteamVR = true;

	private bool isIndex;

	private bool registered;

	private float totalVibePower;

	private bool ovrVibeActive;

	public Vector2 stickAxis { get; private set; }

	public bool stickTouched { get; private set; }

	public bool stickPressed { get; private set; }

	public bool stickPressDown { get; private set; }

	public bool stickPressUp { get; private set; }

	public event VRHandControllerDelegate OnTriggerClicked;

	public event VRHandControllerDelegate OnTriggerClickReleased;

	public event VRHandControllerDelegate OnTriggerStageOnePressed;

	public event VRHandControllerDelegate OnTriggerStageOneReleased;

	public event VRHandControllerAxisDelegate OnTriggerAxis;

	public event VRHandControllerDelegate OnGripPressed;

	public event VRHandControllerDelegate OnGripReleased;

	public event VRHandControllerDelegate OnGripForcePressed;

	public event VRHandControllerDelegate OnGripForceReleased;

	public event VRHandControllerDelegate OnThumbButtonPressed;

	public event VRHandControllerDelegate OnThumbButtonReleased;

	public event VRHandControllerDelegate OnSecondaryThumbButtonPressed;

	public event VRHandControllerDelegate OnSecondaryThumbButtonReleased;

	public event VRHandControllerDelegate OnStickTouched;

	public event VRHandControllerDelegate OnStickUntouched;

	public event VRHandControllerVectorDelegate OnStickAxis;

	public event VRHandControllerDelegate OnStickPressed;

	public event VRHandControllerDelegate OnStickUnpressed;

	public static event VRHandControllerDelegate OnAddController;

	public static event VRHandControllerDelegate OnRemoveController;

	private void Awake()
	{
		isOculus = GameSettings.VR_SDK_IS_OCULUS;
		riftController = GetComponent<RiftTouchController>();
		steamVRInputController = GetComponent<VTSteamVRController>();
		riftController.enabled = isOculus;
		if (isOculus)
		{
			riftController.OnTriggerAxis += RiftController_OnTriggerAxis;
			riftController.OnThumbButtonPressed += RiftController_OnThumbButtonPressed;
			riftController.OnThumbButtonReleased += RiftController_OnThumbButtonReleased;
			riftController.OnSecondaryThumbButtonPressed += RiftController_OnSecondaryThumbButtonPressed;
			riftController.OnSecondaryThumbButtonReleased += RiftController_OnSecondaryThumbButtonReleased;
			riftController.OnGripPressed += RiftController_OnGripPressed;
			riftController.OnGripReleased += RiftController_OnGripReleased;
			riftController.OnStickTouched += RiftController_OnStickTouched;
			riftController.OnStickUntouched += RiftController_OnStickUntouched;
			riftController.OnStickAxis += RiftController_OnStickAxis;
			riftController.OnStickPressed += RiftController_OnStickPressed;
			riftController.OnStickUnpressed += RiftController_OnStickUnpressed;
		}
		else if ((bool)steamVRInputController)
		{
			isLegacySteamVR = false;
			steamVRInputController.TriggerClicked += SteamVRController_TriggerClicked;
			steamVRInputController.TriggerUnclicked += SteamVRController_TriggerUnclicked;
			steamVRInputController.MenuButtonClicked += SteamVRController_MenuButtonClicked;
			steamVRInputController.MenuButtonUnclicked += SteamVRController_MenuButtonUnclicked;
			steamVRInputController.Gripped += SteamVRController_Gripped;
			steamVRInputController.Ungripped += SteamVRController_Ungripped;
			steamVRInputController.PadTouched += SteamVRController_PadTouched;
			steamVRInputController.PadUntouched += SteamVRController_PadUntouched;
			steamVRInputController.PadClicked += SteamVRController_PadClicked;
			steamVRInputController.PadUnclicked += SteamVRController_PadUnclicked;
			GetComponent<SteamVR_Behaviour_Pose>().enabled = false;
			TrackedPoseDriver trackedPoseDriver = base.gameObject.AddComponent<TrackedPoseDriver>();
			trackedPoseDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRController, isLeft ? TrackedPoseDriver.TrackedPose.LeftPose : TrackedPoseDriver.TrackedPose.RightPose);
			trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.Update;
		}
	}

	private void OnSteamVRDeviceIndexChanged(SteamVR_Behaviour_Pose p, SteamVR_Input_Sources s, int idx)
	{
		if (idx < 0)
		{
			steamVRInputController.OnGripForce -= OnIndexGripForce;
			isIndex = false;
			return;
		}
		int deviceIndex = p.GetDeviceIndex();
		string text = (isLeft ? "LeftHandDevice" : "RightHandDevice");
		StringBuilder stringBuilder = new StringBuilder(64);
		ETrackedPropertyError pError = ETrackedPropertyError.TrackedProp_Success;
		OpenVR.System.GetStringTrackedDeviceProperty((uint)deviceIndex, ETrackedDeviceProperty.Prop_ControllerType_String, stringBuilder, 64u, ref pError);
		string text2 = stringBuilder.ToString().ToLower();
		stringBuilder.Clear();
		OpenVR.System.GetStringTrackedDeviceProperty((uint)deviceIndex, ETrackedDeviceProperty.Prop_InputProfilePath_String, stringBuilder, 64u, ref pError);
		string text3 = stringBuilder.ToString().ToLower();
		CrashReportHandler.SetUserMetadata(text, text2);
		Debug.Log($"{text}:{deviceIndex}:{text2} inputProfile:{text3}");
		if (text2.Contains("knuckles") || text2.Contains("index"))
		{
			Debug.Log("Index controller detected");
			isIndex = true;
			steamVRInputController.OnGripForce -= OnIndexGripForce;
			steamVRInputController.OnGripForce += OnIndexGripForce;
			GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.Index;
		}
		else
		{
			isIndex = false;
			steamVRInputController.OnGripForce -= OnIndexGripForce;
			if (text2.Contains("oculus") || text2.Contains("miramar"))
			{
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.RiftTouch;
			}
			else if (text2.Contains("hpmotioncontroller") || text3.Contains("hpmotioncontroller") || text2.Contains("openvr controller(windowsmr: 0x045e/0x066a/0/"))
			{
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.WMRStick;
			}
			else if (text2.Contains("holographic_controller"))
			{
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.WMRTouchpad;
			}
			else if (text2.Contains("vive_cosmos_controller"))
			{
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.ViveCosmos;
			}
			else if (text2.Contains("vive_controller"))
			{
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.ViveWands;
			}
			else
			{
				VTNetworkManager.SendAsyncException("Unknown controller type detected: " + text2);
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.Unknown;
			}
		}
		CrashReportHandler.SetUserMetadata("VR_CONTROLLER_STYLE", GameSettings.VR_CONTROLLER_STYLE.ToString());
		Debug.Log("VR_CONTROLLER_STYLE == " + GameSettings.VR_CONTROLLER_STYLE);
	}

	private void OnDestroy()
	{
		if (isOculus)
		{
			riftController.OnTriggerAxis -= RiftController_OnTriggerAxis;
			riftController.OnThumbButtonPressed -= RiftController_OnThumbButtonPressed;
			riftController.OnThumbButtonReleased -= RiftController_OnThumbButtonReleased;
			riftController.OnSecondaryThumbButtonPressed -= RiftController_OnSecondaryThumbButtonPressed;
			riftController.OnSecondaryThumbButtonReleased -= RiftController_OnSecondaryThumbButtonReleased;
			riftController.OnGripPressed -= RiftController_OnGripPressed;
			riftController.OnGripReleased -= RiftController_OnGripReleased;
			riftController.OnStickTouched -= RiftController_OnStickTouched;
			riftController.OnStickUntouched -= RiftController_OnStickUntouched;
			riftController.OnStickAxis -= RiftController_OnStickAxis;
			riftController.OnStickPressed -= RiftController_OnStickPressed;
			riftController.OnStickUnpressed -= RiftController_OnStickUnpressed;
		}
		else if ((bool)steamVRInputController)
		{
			steamVRInputController.TriggerClicked -= SteamVRController_TriggerClicked;
			steamVRInputController.TriggerUnclicked -= SteamVRController_TriggerUnclicked;
			steamVRInputController.MenuButtonClicked -= SteamVRController_MenuButtonClicked;
			steamVRInputController.MenuButtonUnclicked -= SteamVRController_MenuButtonUnclicked;
			steamVRInputController.Gripped -= SteamVRController_Gripped;
			steamVRInputController.Ungripped -= SteamVRController_Ungripped;
			steamVRInputController.PadTouched -= SteamVRController_PadTouched;
			steamVRInputController.PadUntouched -= SteamVRController_PadUntouched;
			steamVRInputController.PadClicked -= SteamVRController_PadClicked;
			steamVRInputController.PadUnclicked -= SteamVRController_PadUnclicked;
			if (isIndex)
			{
				steamVRInputController.OnGripForce += OnIndexGripForce;
			}
		}
	}

	private void OnEnable()
	{
		if (controllers == null)
		{
			controllers = new List<VRHandController>();
		}
		if (!interactionTransform)
		{
			interactionTransform = new GameObject("interactionTf").transform;
			interactionTransform.parent = base.transform;
			interactionTransform.localPosition = Vector3.zero;
		}
		StartCoroutine(EnableRoutine());
	}

	private IEnumerator EnableRoutine()
	{
		yield return null;
		if (!registered)
		{
			controllers.Add(this);
			ControllerEventHandler.RegisterController(this);
			if (VRHandController.OnAddController != null)
			{
				VRHandController.OnAddController(this);
			}
			registered = true;
		}
		if (!steamVRInputController)
		{
			yield break;
		}
		XRNode node = (isLeft ? XRNode.LeftHand : XRNode.RightHand);
		InputDevice device = InputDevices.GetDeviceAtXRNode(node);
		while (string.IsNullOrEmpty(device.name))
		{
			device = InputDevices.GetDeviceAtXRNode(node);
			yield return null;
		}
		string text = device.name.ToLower();
		Debug.Log($"{node} controller deviceName = {text}");
		if (text.Contains("knuckles") || text.Contains("index"))
		{
			Debug.Log("Index controller detected");
			isIndex = true;
			steamVRInputController.OnGripForce -= OnIndexGripForce;
			steamVRInputController.OnGripForce += OnIndexGripForce;
			GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.Index;
		}
		else
		{
			isIndex = false;
			steamVRInputController.OnGripForce -= OnIndexGripForce;
			if (text.Contains("oculus") || text.Contains("miramar"))
			{
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.RiftTouch;
			}
			else if (text.Contains("hpmotioncontroller") || text.Contains("openvr controller(windowsmr: 0x045e/0x066a/0/"))
			{
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.WMRStick;
			}
			else if (text.Contains("holographic_controller"))
			{
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.WMRTouchpad;
			}
			else if (text.Contains("vive_cosmos_controller"))
			{
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.ViveCosmos;
			}
			else if (text.Contains("vive_controller"))
			{
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.ViveWands;
			}
			else
			{
				GameSettings.VR_CONTROLLER_STYLE = ControllerStyles.Unknown;
			}
		}
		CrashReportHandler.SetUserMetadata("VR_CONTROLLER_STYLE", GameSettings.VR_CONTROLLER_STYLE.ToString());
		Debug.Log("VR_CONTROLLER_STYLE == " + GameSettings.VR_CONTROLLER_STYLE);
	}

	private void OnDisable()
	{
		if (registered)
		{
			if (controllers != null)
			{
				controllers.Remove(this);
			}
			ControllerEventHandler.UnregisterController(this);
			if (VRHandController.OnRemoveController != null)
			{
				VRHandController.OnRemoveController(this);
			}
			registered = false;
		}
		totalVibePower = 0f;
	}

	private void Update()
	{
		if (isOculus)
		{
			velocity = riftController.velocity;
			angularVelocity = riftController.angularVelocity;
		}
		else
		{
			velocity = steamVRInputController.GetVelocity();
			angularVelocity = steamVRInputController.GetAngularVelocity();
			triggerAxis = steamVRInputController.GetTriggerAxis();
			if (this.OnTriggerAxis != null)
			{
				this.OnTriggerAxis(this, triggerAxis);
			}
			if (steamVRInputController.GetTriggerClickDown())
			{
				triggerStageOnePressed = true;
				this.OnTriggerStageOnePressed?.Invoke(this);
			}
			else if (steamVRInputController.GetTriggerClickUp())
			{
				triggerStageOnePressed = false;
				this.OnTriggerStageOneReleased?.Invoke(this);
			}
			stickAxis = steamVRInputController.GetStickAxis();
			if (this.OnStickAxis != null)
			{
				this.OnStickAxis(this, stickAxis);
			}
		}
		stickPressDown = false;
		stickPressUp = false;
		if (wasStickPressed != i_stickPressed)
		{
			bool flag2 = (stickPressed = i_stickPressed);
			wasStickPressed = flag2;
			if (stickPressed)
			{
				stickPressDown = true;
			}
			else
			{
				stickPressUp = true;
			}
		}
		UpdateHaptics();
	}

	private void RiftController_OnStickUnpressed()
	{
		i_stickPressed = false;
		if (this.OnStickUnpressed != null)
		{
			this.OnStickUnpressed(this);
		}
	}

	private void RiftController_OnStickPressed()
	{
		i_stickPressed = true;
		if (this.OnStickPressed != null)
		{
			this.OnStickPressed(this);
		}
	}

	private void RiftController_OnStickAxis(Vector2 axis)
	{
		stickAxis = axis;
		if (this.OnStickAxis != null)
		{
			this.OnStickAxis(this, axis);
		}
	}

	private void RiftController_OnStickUntouched()
	{
		stickTouched = false;
		if (this.OnStickUntouched != null)
		{
			this.OnStickUntouched(this);
		}
	}

	private void RiftController_OnStickTouched()
	{
		stickTouched = true;
		if (this.OnStickTouched != null)
		{
			this.OnStickTouched(this);
		}
	}

	private void RiftController_OnGripReleased()
	{
		gripForcePressed = false;
		gripPressed = false;
		if (this.OnGripForceReleased != null)
		{
			this.OnGripForceReleased(this);
		}
		if (this.OnGripReleased != null)
		{
			this.OnGripReleased(this);
		}
	}

	private void RiftController_OnGripPressed()
	{
		gripPressed = true;
		gripForcePressed = true;
		if (this.OnGripPressed != null)
		{
			this.OnGripPressed(this);
		}
		if (this.OnGripForcePressed != null)
		{
			this.OnGripForcePressed(this);
		}
	}

	private void RiftController_OnSecondaryThumbButtonReleased()
	{
		secondaryThumbButtonPressed = false;
		if (this.OnSecondaryThumbButtonReleased != null)
		{
			this.OnSecondaryThumbButtonReleased(this);
		}
	}

	private void RiftController_OnSecondaryThumbButtonPressed()
	{
		secondaryThumbButtonPressed = true;
		if (this.OnSecondaryThumbButtonPressed != null)
		{
			this.OnSecondaryThumbButtonPressed(this);
		}
	}

	private void RiftController_OnThumbButtonReleased()
	{
		thumbButtonPressed = false;
		if (this.OnThumbButtonReleased != null)
		{
			this.OnThumbButtonReleased(this);
		}
	}

	private void RiftController_OnThumbButtonPressed()
	{
		thumbButtonPressed = true;
		if (this.OnThumbButtonPressed != null)
		{
			this.OnThumbButtonPressed(this);
		}
	}

	private void RiftController_OnTriggerAxis(float axis)
	{
		triggerAxis = axis;
		if (axis > 0.15f)
		{
			bool num = !triggerStageOnePressed;
			triggerStageOnePressed = true;
			if (num && this.OnTriggerStageOnePressed != null)
			{
				this.OnTriggerStageOnePressed(this);
			}
		}
		else
		{
			bool num2 = triggerStageOnePressed;
			triggerStageOnePressed = false;
			if (num2 && this.OnTriggerStageOneReleased != null)
			{
				this.OnTriggerStageOneReleased(this);
			}
		}
		if (axis > 0.9f)
		{
			bool num3 = !triggerClicked;
			triggerClicked = true;
			if (num3 && this.OnTriggerClicked != null)
			{
				this.OnTriggerClicked(this);
			}
		}
		else
		{
			bool num4 = triggerClicked;
			triggerClicked = false;
			if (num4 && this.OnTriggerClickReleased != null)
			{
				this.OnTriggerClickReleased(this);
			}
		}
		if (this.OnTriggerAxis != null)
		{
			this.OnTriggerAxis(this, axis);
		}
	}

	private void OnIndexGripForce(float gripForce)
	{
		if (gripForce > 0.8f && !gripForcePressed)
		{
			gripForcePressed = true;
			if (this.OnGripForcePressed != null)
			{
				this.OnGripForcePressed(this);
			}
		}
		else if (gripForce < 0.2f && gripForcePressed)
		{
			gripForcePressed = false;
			if (this.OnGripForceReleased != null)
			{
				this.OnGripForceReleased(this);
			}
		}
	}

	private void SteamVRController_PadUnclicked()
	{
		i_stickPressed = false;
		if (this.OnStickUnpressed != null)
		{
			this.OnStickUnpressed(this);
		}
	}

	private void SteamVRController_PadClicked()
	{
		i_stickPressed = true;
		if (this.OnStickPressed != null)
		{
			this.OnStickPressed(this);
		}
	}

	private void SteamVRController_PadUntouched()
	{
		stickTouched = false;
		if (this.OnStickUntouched != null)
		{
			this.OnStickUntouched(this);
		}
	}

	private void SteamVRController_PadTouched()
	{
		stickTouched = true;
		if (this.OnStickTouched != null)
		{
			this.OnStickTouched(this);
		}
	}

	private void SteamVRController_Ungripped()
	{
		if (!isIndex)
		{
			gripForcePressed = false;
		}
		gripPressed = false;
		if (!isIndex && this.OnGripForceReleased != null)
		{
			this.OnGripForceReleased(this);
		}
		if (this.OnGripReleased != null)
		{
			this.OnGripReleased(this);
		}
	}

	private void SteamVRController_Gripped()
	{
		gripPressed = true;
		if (!isIndex)
		{
			gripForcePressed = true;
		}
		if (this.OnGripPressed != null)
		{
			this.OnGripPressed(this);
		}
		if (!isIndex && this.OnGripForcePressed != null)
		{
			this.OnGripForcePressed(this);
		}
	}

	private void SteamVRController_MenuButtonUnclicked()
	{
		thumbButtonPressed = false;
		if (this.OnThumbButtonReleased != null)
		{
			this.OnThumbButtonReleased(this);
		}
	}

	private void SteamVRController_MenuButtonClicked()
	{
		thumbButtonPressed = true;
		if (this.OnThumbButtonPressed != null)
		{
			this.OnThumbButtonPressed(this);
		}
	}

	private void SteamVRController_TriggerUnclicked()
	{
		triggerClicked = false;
		if (this.OnTriggerClickReleased != null)
		{
			this.OnTriggerClickReleased(this);
		}
	}

	private void SteamVRController_TriggerClicked()
	{
		triggerClicked = true;
		if (this.OnTriggerClicked != null)
		{
			this.OnTriggerClicked(this);
		}
	}

	public bool GetTriggerClickDown()
	{
		if (isOculus)
		{
			return riftController.GetTriggerClickDown();
		}
		return steamVRInputController.GetTriggerClickDown();
	}

	public bool GetTriggerClickUp()
	{
		if (isOculus)
		{
			return riftController.GetTriggerClickUp();
		}
		return steamVRInputController.GetTriggerClickUp();
	}

	public bool GetStickPressDown()
	{
		if (isOculus)
		{
			return riftController.GetStickPressDown();
		}
		return steamVRInputController.GetStickPressDown();
	}

	public bool GetStickPressUp()
	{
		if (isOculus)
		{
			return riftController.GetStickPressUp();
		}
		return steamVRInputController.GetStickPressUp();
	}

	public bool GetThumbButtonDown()
	{
		if (isOculus)
		{
			return riftController.GetThumbButtonDown();
		}
		return steamVRInputController.GetThumbButtonDown();
	}

	public bool GetThumbButtonUp()
	{
		if (isOculus)
		{
			return riftController.GetThumbButtonUp();
		}
		return steamVRInputController.GetThumbButtonUp();
	}

	public bool GetSecondButtonDown()
	{
		if (isOculus)
		{
			return false;
		}
		return steamVRInputController.GetSecondButtonDown();
	}

	public bool GetSecondButtonUp()
	{
		if (isOculus)
		{
			return false;
		}
		return steamVRInputController.GetSecondButtonUp();
	}

	public void ReleaseFromInteractable()
	{
		if (activeInteractable != null)
		{
			activeInteractable.StopInteraction();
		}
		if (hoverInteractable != null)
		{
			hoverInteractable.UnHover();
		}
	}

	public void HapticPulse(float power)
	{
		if (isOculus)
		{
			Vibrate(power, 0.01f);
			return;
		}
		ushort microSecDuration = (ushort)Mathf.RoundToInt(Mathf.Clamp01(power) * 3500f);
		switch (GameSettings.VR_CONTROLLER_STYLE)
		{
		case ControllerStyles.RiftTouch:
			if (GameSettings.isQuest2)
			{
				power *= power;
			}
			steamVRInputController.Vibe(power);
			break;
		case ControllerStyles.WMRTouchpad:
		case ControllerStyles.WMRStick:
			steamVRInputController.Vibe(power * power);
			break;
		default:
			steamVRInputController.Vibe(microSecDuration);
			break;
		}
	}

	public void Vibrate(float power, float time)
	{
		StartCoroutine(VibrateRoutine(power, time));
	}

	private IEnumerator VibrateRoutine(float power, float time)
	{
		totalVibePower += power;
		yield return new WaitForSeconds(time);
		totalVibePower -= power;
	}

	private void UpdateHaptics()
	{
		if (totalVibePower > 0.001f && Time.timeScale > 0f)
		{
			if (isOculus)
			{
				ovrVibeActive = true;
				OVRInput.SetControllerVibration(1f, Mathf.Clamp01(totalVibePower), riftController.GetOVRController());
			}
			else
			{
				HapticPulse(totalVibePower);
			}
		}
		else if (ovrVibeActive)
		{
			ovrVibeActive = false;
			OVRInput.SetControllerVibration(0f, 0f, riftController.GetOVRController());
		}
	}
}
