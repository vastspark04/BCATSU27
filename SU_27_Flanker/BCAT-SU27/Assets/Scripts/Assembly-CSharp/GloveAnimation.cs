using UnityEngine;
using Valve.VR;

public class GloveAnimation : MonoBehaviour
{
	public enum Poses
	{
		Idle,
		Point,
		Joystick,
		Pinch,
		Throttle,
		Eject,
		Knob,
		JetThrottle,
		OpenHand
	}

	public Animator animator;

	public Transform parentTransform;

	private Transform lockTransform;

	public Transform fingerTipTf;

	public Transform knobCenterTf;

	public Transform knobCenterSmallTf;

	public IKTwo ik;

	private int joyTriggerID;

	private int joyControlID;

	private int collectiveTiltID;

	private int twistPushButtonID;

	private Transform buttonTransform;

	private Vector3 origLp;

	private Quaternion origRot;

	private int interactPose = -1;

	private int userPose;

	private int finalPose;

	private PoseBounds pb;

	private Transform leverTransform;

	private Quaternion leverGrabRelativeRot;

	private VRHandController controller;

	private bool started;

	private int poseAnimID;

	private int gestureAnimID;

	private bool thumbStickMode;

	private Transform knobTransform;

	private Vector3 knobHandUp;

	private bool smallKnob;

	private VRJoystick joystick;

	[Header("SteamVR Input 2 Compatibility")]
	private VTSteamVRController steamVRController;

	private SteamVR_Behaviour_Pose steamVRPose;

	private float skeletalLerpRate = 12f;

	private int curlAnimID;

	private int thumbLayer;

	private int indexLayer;

	private int middleLayer;

	private int ringLayer;

	private int pinkyLayer;

	private float[] fingerWeights = new float[5];

	private float[] fingerCurls = new float[5];

	[Header("Forearm Fix")]
	public LookRotationReference[] forearmLooks;

	public bool remoteControl;

	private bool remoteGestureDirty;

	private int remoteGesture;

	private float remoteThumb;

	private float remoteIndex;

	private float remoteMiddle;

	private float remoteRing;

	private float remotePinky;

	private bool remoteSkeletal;

	public float skeletonNaturalThumbCurl = 0.5f;

	private bool wasLocked;

	private Vector3 lockedOffset;

	private Quaternion lockedRotationOffset;

	private bool hoverPose;

	private bool touchingJoyThumb;

	private bool joyButtonDown;

	private VRThrottle throttle;

	public bool skeletonAnim { get; private set; }

	public int currentGesture { get; private set; }

	private bool doSkeletal
	{
		get
		{
			if ((bool)steamVRController)
			{
				return skeletonAnim;
			}
			return false;
		}
	}

	public float thumbCurl => fingerCurls[0];

	public float indexCurl => fingerCurls[1];

	public float middleCurl => fingerCurls[2];

	public float ringCurl => fingerCurls[3];

	public float pinkyCurl => fingerCurls[4];

	public void SetOriginTransform(Transform tf)
	{
		origLp = tf.localPosition;
		origRot = tf.localRotation;
	}

	private void Start()
	{
		origLp = parentTransform.localPosition;
		origRot = parentTransform.localRotation;
		controller = GetComponentInParent<VRHandController>();
		started = true;
		poseAnimID = Animator.StringToHash("pose");
		gestureAnimID = Animator.StringToHash("gesture");
		joyTriggerID = Animator.StringToHash("joyTrigger");
		joyControlID = Animator.StringToHash("joyControl");
		collectiveTiltID = Animator.StringToHash("tiltControl");
		twistPushButtonID = Animator.StringToHash("twistPushButton");
		thumbStickMode = GameSettings.IsThumbstickMode();
		if (!GameSettings.VR_SDK_IS_OCULUS)
		{
			steamVRPose = GetComponentInParent<SteamVR_Behaviour_Pose>();
			if ((bool)steamVRPose)
			{
				steamVRPose.onTransformUpdated.AddListener(OnSteamVRPoseUpdated);
			}
			steamVRController = GetComponentInParent<VTSteamVRController>();
			curlAnimID = Animator.StringToHash("curl");
			thumbLayer = animator.GetLayerIndex("skeletalThumb");
			indexLayer = animator.GetLayerIndex("skeletalIndex");
			middleLayer = animator.GetLayerIndex("skeletalMiddle");
			ringLayer = animator.GetLayerIndex("skeletalRing");
			pinkyLayer = animator.GetLayerIndex("skeletalPinky");
		}
		skeletonAnim = GameSettings.CurrentSettings.GetBoolSetting("SKELETON_FINGERS");
		GameSettings.OnAppliedSettings += GameSettings_OnAppliedSettings;
	}

	private void OnDestroy()
	{
		GameSettings.OnAppliedSettings -= GameSettings_OnAppliedSettings;
	}

	private void GameSettings_OnAppliedSettings(GameSettings s)
	{
		skeletonAnim = s.GetBoolSetting("SKELETON_FINGERS");
		if (!skeletonAnim && (bool)steamVRController)
		{
			UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.thumb, activeInteractable: false, thumbLayer, 0f, immediate: true);
			UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.middle, activeInteractable: false, middleLayer, 0f, immediate: true);
			UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.ring, activeInteractable: false, ringLayer, 0f, immediate: true);
			UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.pinky, activeInteractable: false, pinkyLayer, 0f, immediate: true);
			UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.index, activeInteractable: false, indexLayer, 0f, immediate: true);
		}
	}

	private void OnSteamVRPoseUpdated(SteamVR_Behaviour_Pose pose, SteamVR_Input_Sources source)
	{
		UpdateLockedPositions();
	}

	private void OnEnable()
	{
		if (started)
		{
			animator.SetInteger(poseAnimID, finalPose);
		}
	}

	public void SetRemoteGesture(int gesture)
	{
		if (gesture != remoteGesture)
		{
			remoteGesture = gesture;
			remoteGestureDirty = true;
		}
	}

	public void SetRemoteSkeletonFingers(float thumb, float index, float middle, float ring, float pinky)
	{
		remoteSkeletal = true;
		remoteThumb = thumb;
		remoteIndex = index;
		remoteMiddle = middle;
		remoteRing = ring;
		remotePinky = pinky;
	}

	private void Update()
	{
		if (remoteControl)
		{
			if (remoteGestureDirty)
			{
				animator.SetInteger(gestureAnimID, remoteGesture);
				remoteGestureDirty = false;
			}
		}
		else if (finalPose == 0 && !doSkeletal && !controller.activeInteractable && !controller.hoverInteractable && controller.gripPressed && ((thumbStickMode && controller.stickAxis.sqrMagnitude > 0.25f) || (!thumbStickMode && controller.stickPressed)))
		{
			Vector2 stickAxis = controller.stickAxis;
			float num = Vector2.Angle(Vector2.up, stickAxis) * Mathf.Sign(Vector2.Dot(stickAxis, Vector2.right));
			if (controller.isLeft)
			{
				num *= -1f;
			}
			if (num < 10f && num > -45f)
			{
				animator.SetInteger(gestureAnimID, 2);
				currentGesture = 2;
			}
			else if (num > 10f && num < 45f)
			{
				animator.SetInteger(gestureAnimID, 5);
				currentGesture = 5;
			}
			else if (num > 135f || num < -135f)
			{
				animator.SetInteger(gestureAnimID, 1);
				currentGesture = 1;
			}
			else if (num > -135f && num < -45f)
			{
				animator.SetInteger(gestureAnimID, 3);
				currentGesture = 3;
			}
			else if (num > 45f && num < 135f)
			{
				animator.SetInteger(gestureAnimID, 4);
				currentGesture = 4;
			}
		}
		else
		{
			animator.SetInteger(gestureAnimID, 0);
			currentGesture = 0;
		}
	}

	private void LateUpdate()
	{
		bool pbOverrideSkeletal = false;
		int num = 0;
		if (interactPose > 0)
		{
			num = interactPose;
			if (hoverPose)
			{
				pbOverrideSkeletal = true;
			}
		}
		else if ((bool)pb && pb.enabled && pb.gameObject.activeInHierarchy)
		{
			num = (int)pb.pose;
			pbOverrideSkeletal = true;
		}
		else
		{
			num = userPose;
		}
		if (num != finalPose)
		{
			finalPose = num;
			animator.SetInteger(poseAnimID, finalPose);
		}
		if (!steamVRPose)
		{
			UpdateLockedPositions();
		}
		if ((bool)joystick)
		{
			animator.SetBool(joyTriggerID, joystick.triggerAxis > 0.9f);
		}
		UpdateSkeletalFingers(pbOverrideSkeletal);
		UpdateLockedPositions();
	}

	private void UpdateSkeletalFingers(bool pbOverrideSkeletal)
	{
		if (remoteSkeletal || doSkeletal)
		{
			bool flag = !remoteControl && controller.activeInteractable != null;
			float weight = -1f;
			if (pbOverrideSkeletal)
			{
				weight = 0f;
			}
			UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.middle, flag, middleLayer, weight);
			UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.ring, flag, ringLayer, weight);
			UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.pinky, flag, pinkyLayer, weight);
			if (remoteControl || (!leverTransform && !knobTransform && !joystick && (flag || (!controller.hoverInteractable && !pb))))
			{
				UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.index, flag, indexLayer, weight);
			}
			else
			{
				UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.index, activeInteractable: false, indexLayer, 0f);
			}
			if (remoteControl || (!leverTransform && !knobTransform && (!joystick || ((bool)joystick && !touchingJoyThumb)) && !throttle))
			{
				UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.thumb, flag, thumbLayer, weight);
			}
			else
			{
				UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum.thumb, activeInteractable: false, thumbLayer, 0f);
			}
		}
	}

	private void UpdateSkeletalFinger(SteamVR_Skeleton_FingerIndexEnum finger, bool activeInteractable, int layer, float weight = -1f, bool immediate = false)
	{
		float num = (remoteControl ? (finger switch
		{
			SteamVR_Skeleton_FingerIndexEnum.index => remoteIndex, 
			SteamVR_Skeleton_FingerIndexEnum.middle => remoteMiddle, 
			SteamVR_Skeleton_FingerIndexEnum.ring => remoteRing, 
			SteamVR_Skeleton_FingerIndexEnum.pinky => remotePinky, 
			_ => remoteThumb, 
		}) : ((finger != SteamVR_Skeleton_FingerIndexEnum.index) ? steamVRController.GetFingerCurl(finger) : controller.triggerAxis));
		if (weight < 0f)
		{
			weight = 1f;
			if (activeInteractable)
			{
				weight = 1f - num;
			}
		}
		if (!activeInteractable && finger == SteamVR_Skeleton_FingerIndexEnum.thumb)
		{
			bool flag = true;
			for (int i = 1; i < 5 && flag; i++)
			{
				if (fingerCurls[i] < 0.9f)
				{
					flag = false;
				}
			}
			if (!flag)
			{
				num = Mathf.Max(num, skeletonNaturalThumbCurl);
			}
		}
		float normalizedTime = (fingerCurls[(int)finger] = Mathf.Lerp(fingerCurls[(int)finger], num, skeletalLerpRate * Time.deltaTime));
		animator.Play(curlAnimID, layer, normalizedTime);
		float weight2 = ((!immediate) ? (fingerWeights[(int)finger] = Mathf.Lerp(fingerWeights[(int)finger], weight, skeletalLerpRate * Time.deltaTime)) : (fingerWeights[(int)finger] = weight));
		animator.SetLayerWeight(layer, weight2);
	}

	private void UpdateLockedPositions()
	{
		if ((bool)lockTransform)
		{
			if (!wasLocked)
			{
				wasLocked = true;
				lockedOffset = lockTransform.InverseTransformPoint(parentTransform.position);
				lockedRotationOffset = Quaternion.LookRotation(lockTransform.InverseTransformDirection(parentTransform.forward), lockTransform.InverseTransformDirection(parentTransform.up));
			}
			parentTransform.position = lockTransform.TransformPoint(lockedOffset);
			parentTransform.rotation = Quaternion.LookRotation(lockTransform.TransformDirection(lockedRotationOffset * Vector3.forward), lockTransform.TransformDirection(lockedRotationOffset * Vector3.up));
			lockedOffset = Vector3.Lerp(lockedOffset, Vector3.zero, 15f * Time.deltaTime);
			lockedRotationOffset = Quaternion.Lerp(lockedRotationOffset, Quaternion.identity, 15f * Time.deltaTime);
		}
		else if ((bool)leverTransform)
		{
			parentTransform.rotation = controller.transform.parent.rotation * leverGrabRelativeRot;
			Vector3 vector = parentTransform.position - fingerTipTf.position;
			parentTransform.position = leverTransform.position + vector;
		}
		else if ((bool)buttonTransform)
		{
			Vector3 vector2 = parentTransform.position - fingerTipTf.position;
			parentTransform.position = buttonTransform.position + vector2;
		}
		else
		{
			wasLocked = false;
			parentTransform.localPosition = Vector3.Lerp(parentTransform.localPosition, origLp, 10f * Time.deltaTime);
			parentTransform.localRotation = Quaternion.Slerp(parentTransform.localRotation, origRot, 10f * Time.deltaTime);
		}
		if ((bool)knobTransform)
		{
			Transform transform = (knobCenterTf ? knobCenterTf : fingerTipTf);
			if (smallKnob && (bool)knobCenterSmallTf)
			{
				transform = knobCenterSmallTf;
			}
			Vector3 vector3 = parentTransform.position - transform.position;
			parentTransform.position = knobTransform.position + vector3;
			parentTransform.rotation = Quaternion.LookRotation(parentTransform.forward, knobTransform.TransformDirection(knobHandUp));
		}
		if (forearmLooks == null)
		{
			return;
		}
		for (int i = 0; i < forearmLooks.Length; i++)
		{
			LookRotationReference lookRotationReference = forearmLooks[i];
			if ((bool)lookRotationReference && lookRotationReference.enabled)
			{
				lookRotationReference.UpdateLook();
			}
		}
	}

	public void SetLeverTransform(Transform tf)
	{
		leverTransform = tf;
		leverGrabRelativeRot = Quaternion.Inverse(controller.transform.parent.rotation) * parentTransform.rotation;
	}

	public void SetKnobTransform(Transform knobTransform, Transform lockTransform, bool small)
	{
		Vector3 vector = ((small && (bool)knobCenterSmallTf) ? (parentTransform.position - knobCenterSmallTf.position) : ((!knobCenterTf) ? (parentTransform.position - fingerTipTf.position) : (parentTransform.position - knobCenterTf.position)));
		parentTransform.position = knobTransform.position + vector;
		lockTransform.position = parentTransform.position;
		lockTransform.rotation = parentTransform.rotation;
		this.knobTransform = knobTransform;
		smallKnob = small;
		knobHandUp = knobTransform.InverseTransformDirection(parentTransform.up);
		knobHandUp.y = 0f;
	}

	public void SetJoystick(VRJoystick joy)
	{
		if ((bool)joystick)
		{
			RemoveJoystickActions();
		}
		joystick = joy;
		AddJoystickActions();
	}

	public void PlayTwistPushButton()
	{
		animator.SetTrigger(twistPushButtonID);
	}

	public void ClearInteractPose()
	{
		interactPose = -1;
		lockTransform = null;
		leverTransform = null;
		knobTransform = null;
		RemoveJoystickActions();
		joystick = null;
		animator.SetBool(joyTriggerID, value: false);
		animator.SetInteger(joyControlID, 0);
		joyButtonDown = false;
		animator.SetInteger(collectiveTiltID, 0);
		if ((bool)throttle)
		{
			if (throttle.rotationMode)
			{
				RemoveCollectiveActions();
			}
			else
			{
				RemoveJetThrottleActions();
			}
		}
		throttle = null;
	}

	public void PressButton(Transform buttonTransform, bool pressAndHold)
	{
		if ((bool)fingerTipTf)
		{
			Vector3 vector = parentTransform.position - fingerTipTf.position;
			parentTransform.position = buttonTransform.position + vector;
			if (pressAndHold)
			{
				this.buttonTransform = buttonTransform;
			}
		}
	}

	public void UnPressButton()
	{
		if ((bool)buttonTransform)
		{
			buttonTransform = null;
		}
	}

	public void SetLockTransform(Transform tf)
	{
		lockTransform = tf;
	}

	public void SetPoseInteractable(Poses pose)
	{
		hoverPose = false;
		interactPose = (int)pose;
	}

	public void SetPoseHover(Poses pose)
	{
		hoverPose = true;
		interactPose = (int)pose;
	}

	public void SetBoundsPose(PoseBounds poseBounds)
	{
		if ((bool)pb)
		{
			pb.ClearGlove(this);
		}
		pb = poseBounds;
	}

	public void ClearBoundsPose(PoseBounds poseBounds)
	{
		if (pb == poseBounds)
		{
			pb = null;
		}
	}

	private void AddJoystickActions()
	{
		if ((bool)joystick)
		{
			joystick.OnSetThumbstick.AddListener(OnJoyThumbAxis);
			joystick.OnResetThumbstick.AddListener(OnResetJoyAxis);
			joystick.OnThumbstickButtonDown.AddListener(OnJoyButtonDown);
			joystick.OnThumbstickButtonUp.AddListener(OnJoyButtonUp);
			joystick.OnThumbstickTouch.AddListener(OnJoyThumbTouch);
			joystick.OnThumbstuckUntouch.AddListener(OnJoyThumbUntouch);
		}
	}

	private void RemoveJoystickActions()
	{
		if ((bool)joystick)
		{
			joystick.OnSetThumbstick.RemoveListener(OnJoyThumbAxis);
			joystick.OnResetThumbstick.RemoveListener(OnResetJoyAxis);
			joystick.OnThumbstickButtonDown.RemoveListener(OnJoyButtonDown);
			joystick.OnThumbstickButtonUp.RemoveListener(OnJoyButtonUp);
			joystick.OnThumbstickTouch.RemoveListener(OnJoyThumbTouch);
			joystick.OnThumbstuckUntouch.RemoveListener(OnJoyThumbUntouch);
		}
		touchingJoyThumb = false;
	}

	private void OnJoyThumbTouch()
	{
		touchingJoyThumb = true;
	}

	private void OnJoyThumbUntouch()
	{
		touchingJoyThumb = false;
	}

	private void OnJoyThumbAxis(Vector3 axis)
	{
		if (!joyButtonDown)
		{
			int value = 0;
			if ((double)axis.sqrMagnitude > 0.01)
			{
				value = ((!(Mathf.Abs(axis.y) > Mathf.Abs(axis.x))) ? ((axis.x < 0f) ? 3 : 4) : ((axis.y > 0f) ? 1 : 2));
			}
			animator.SetInteger(joyControlID, value);
		}
	}

	private void OnResetJoyAxis()
	{
		animator.SetInteger(joyControlID, 0);
	}

	private void OnJoyButtonDown()
	{
		joyButtonDown = true;
		animator.SetInteger(joyControlID, 5);
	}

	private void OnJoyButtonUp()
	{
		joyButtonDown = false;
		animator.SetInteger(joyControlID, 0);
	}

	public void SetThrottle(VRThrottle t)
	{
		throttle = t;
		if (t.rotationMode)
		{
			AddCollectiveActions();
		}
		else
		{
			AddJetThrottleActions();
		}
	}

	private void AddCollectiveActions()
	{
		if ((bool)throttle)
		{
			throttle.OnSetThumbstick.AddListener(OnSetCollectiveThumbstick);
			throttle.OnResetThumbstick.AddListener(OnResetCollectiveThumbstick);
			throttle.OnTriggerAxis.AddListener(OnSetThrottleTrigger);
		}
	}

	private void RemoveCollectiveActions()
	{
		if ((bool)throttle)
		{
			throttle.OnSetThumbstick.RemoveListener(OnSetCollectiveThumbstick);
			throttle.OnResetThumbstick.RemoveListener(OnResetCollectiveThumbstick);
			throttle.OnTriggerAxis.RemoveListener(OnSetThrottleTrigger);
		}
	}

	private void AddJetThrottleActions()
	{
		if ((bool)throttle)
		{
			throttle.OnTriggerAxis.AddListener(OnSetThrottleTrigger);
		}
	}

	private void RemoveJetThrottleActions()
	{
		if ((bool)throttle)
		{
			throttle.OnTriggerAxis.RemoveListener(OnSetThrottleTrigger);
		}
	}

	private void OnSetCollectiveThumbstick(Vector3 axis)
	{
		if (axis.y > 0.1f)
		{
			animator.SetInteger(collectiveTiltID, 1);
		}
		else if (axis.y < -0.1f)
		{
			animator.SetInteger(collectiveTiltID, 2);
		}
		else
		{
			animator.SetInteger(collectiveTiltID, 0);
		}
	}

	private void OnResetCollectiveThumbstick()
	{
		animator.SetInteger(collectiveTiltID, 0);
	}

	private void OnSetThrottleTrigger(float t)
	{
		if (t > 0.25f)
		{
			animator.SetBool(joyTriggerID, value: true);
		}
		else
		{
			animator.SetBool(joyTriggerID, value: false);
		}
	}
}
