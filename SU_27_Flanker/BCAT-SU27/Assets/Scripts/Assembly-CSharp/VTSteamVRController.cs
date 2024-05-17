using System;
using UnityEngine;
using Valve.VR;

[RequireComponent(typeof(SteamVR_Behaviour_Pose))]
public class VTSteamVRController : MonoBehaviour
{
	[SerializeField]
	private SteamVR_Input_Sources inputSource;

	[SerializeField]
	private SteamVR_Action_Single triggerAxis;

	[SerializeField]
	private SteamVR_Action_Boolean triggerButton;

	[SerializeField]
	private SteamVR_Action_Vector2 stickAxis;

	[SerializeField]
	private SteamVR_Action_Boolean menuButton;

	public bool boundSecondaryButton;

	[SerializeField]
	private SteamVR_Action_Boolean secondaryButton;

	[SerializeField]
	private SteamVR_Action_Boolean padTouch;

	[SerializeField]
	private SteamVR_Action_Boolean padClick;

	[SerializeField]
	private SteamVR_Action_Boolean grip;

	[SerializeField]
	private SteamVR_Action_Single gripForce;

	[SerializeField]
	private SteamVR_Action_Skeleton skeleton;

	private SteamVR_Behaviour_Pose pose;

	[SerializeField]
	private SteamVR_Action_Vibration hapticAction;

	public float currGripForce;

	private const float TRIGGER_CLICK_THRESH = 0.75f;

	private const float TRIGGER_UNCLICK_THRESH = 0.65f;

	private bool tClicked;

	private bool tClickDown;

	private bool tClickUp;

	private bool mbClicked;

	private bool gripped;

	private bool padTouched;

	private bool padClicked;

	public SteamVR_Input_Sources InputSource => inputSource;

	public event Action TriggerClicked;

	public event Action TriggerUnclicked;

	public event Action MenuButtonClicked;

	public event Action MenuButtonUnclicked;

	public event Action SecondButtonClicked;

	public event Action SecondButtonUnclicked;

	public event Action Gripped;

	public event Action Ungripped;

	public event Action PadTouched;

	public event Action PadUntouched;

	public event Action PadClicked;

	public event Action PadUnclicked;

	public event Action<float> OnGripForce;

	public float GetTriggerAxis()
	{
		return triggerAxis[inputSource].axis;
	}

	public Vector3 GetVelocity()
	{
		return pose.GetVelocity();
	}

	public Vector3 GetAngularVelocity()
	{
		return pose.GetAngularVelocity();
	}

	public Vector2 GetStickAxis()
	{
		return stickAxis[inputSource].axis;
	}

	private void Awake()
	{
		pose = GetComponent<SteamVR_Behaviour_Pose>();
	}

	private void Update()
	{
		float num = GetTriggerAxis();
		tClickDown = false;
		tClickUp = false;
		if (tClicked)
		{
			if (triggerButton != null && triggerButton.active)
			{
				if (!triggerButton[inputSource].state)
				{
					tClicked = false;
					tClickUp = true;
					if (this.TriggerUnclicked != null)
					{
						this.TriggerUnclicked();
					}
				}
			}
			else if (num < 0.65f)
			{
				tClicked = false;
				tClickUp = true;
				if (this.TriggerUnclicked != null)
				{
					this.TriggerUnclicked();
				}
			}
		}
		else if (triggerButton != null && triggerButton.active)
		{
			if (triggerButton[inputSource].state)
			{
				tClicked = true;
				tClickDown = true;
				if (this.TriggerClicked != null)
				{
					this.TriggerClicked();
				}
			}
		}
		else if (num > 0.75f)
		{
			tClicked = true;
			tClickDown = true;
			if (this.TriggerClicked != null)
			{
				this.TriggerClicked();
			}
		}
		bool state = grip[inputSource].state;
		if (gripped != state)
		{
			gripped = state;
			if (gripped)
			{
				if (this.Gripped != null)
				{
					this.Gripped();
				}
			}
			else if (this.Ungripped != null)
			{
				this.Ungripped();
			}
		}
		if (menuButton[inputSource].stateDown && this.MenuButtonClicked != null)
		{
			this.MenuButtonClicked();
		}
		if (menuButton[inputSource].stateUp && this.MenuButtonUnclicked != null)
		{
			this.MenuButtonUnclicked();
		}
		if (boundSecondaryButton)
		{
			if (secondaryButton[inputSource].stateDown)
			{
				this.SecondButtonClicked?.Invoke();
			}
			if (secondaryButton[inputSource].stateUp)
			{
				this.SecondButtonUnclicked?.Invoke();
			}
		}
		bool state2 = padTouch[inputSource].state;
		if (padTouched != state2)
		{
			padTouched = state2;
			if (padTouched)
			{
				if (this.PadTouched != null)
				{
					this.PadTouched();
				}
			}
			else if (this.PadUntouched != null)
			{
				this.PadUntouched();
			}
		}
		bool state3 = padClick[inputSource].state;
		if (padClicked != state3)
		{
			padClicked = state3;
			if (padClicked)
			{
				if (this.PadClicked != null)
				{
					this.PadClicked();
				}
			}
			else if (this.PadUnclicked != null)
			{
				this.PadUnclicked();
			}
		}
		if (gripForce[inputSource].changed)
		{
			float obj = (currGripForce = gripForce[inputSource].axis);
			if (this.OnGripForce != null)
			{
				this.OnGripForce(obj);
			}
		}
	}

	public void Vibe(ushort microSecDuration)
	{
		float num = (float)(int)microSecDuration / 1000000f;
		if (GameSettings.VR_CONTROLLER_STYLE == ControllerStyles.RiftTouch)
		{
			num = Mathf.Min(num, 0.79f);
		}
		hapticAction.Execute(0f, num, 1f / num, 1f, inputSource);
	}

	public void Vibe(float power)
	{
		float num = Mathf.Clamp(power, 0.001f, 1.5f) * 0.01f;
		hapticAction.Execute(0f, num, 1f / num, power * 2f, inputSource);
	}

	public float GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum finger)
	{
		return skeleton.fingerCurls[(int)finger];
	}

	public bool GetThumbButtonDown()
	{
		return menuButton[inputSource].stateDown;
	}

	public bool GetThumbButtonUp()
	{
		return menuButton[inputSource].stateUp;
	}

	public bool GetSecondButtonDown()
	{
		if (boundSecondaryButton)
		{
			return secondaryButton[inputSource].stateDown;
		}
		return false;
	}

	public bool GetSecondButtonUp()
	{
		if (boundSecondaryButton)
		{
			return secondaryButton[inputSource].stateUp;
		}
		return false;
	}

	public bool GetStickPressDown()
	{
		return padClick[inputSource].stateDown;
	}

	public bool GetStickPressUp()
	{
		return padClick[inputSource].stateUp;
	}

	public bool GetTriggerClickDown()
	{
		return tClickDown;
	}

	public bool GetTriggerClickUp()
	{
		return tClickUp;
	}
}
