using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class VRTwistKnobInt : MonoBehaviour, IQSVehicleComponent, IPersistentVehicleData
{
	public bool quicksavePersistent = true;

	public bool vDataPersistent;

	public Transform knobTransform;

	public float twistRange;

	public bool smallKnob;

	public int initialState;

	public int states = 2;

	public IntEvent OnSetState;

	public bool reverse;

	public bool springy;

	private VRInteractable vrint;

	private bool grabbed;

	private int state;

	private Quaternion[] rotations;

	private VRHandController ctrlr;

	private float angleInterval;

	private Vector3 startUp;

	private Transform lockTransform;

	private float value;

	private float clampedValue;

	public bool useCtrlrZ;

	[Header("Push Button")]
	public bool canPushButton;

	public UnityEvent onPushButton;

	private bool initialized;

	private Coroutine ungrabRoutine;

	public int currentState => state;

	private void Initialize()
	{
		if (!initialized)
		{
			initialized = true;
			vrint = GetComponent<VRInteractable>();
			vrint.OnStartInteraction += Vrint_OnStartInteraction;
			vrint.OnStopInteraction += Vrint_OnStopInteraction;
			lockTransform = new GameObject("knobLockTransform").transform;
			lockTransform.parent = knobTransform;
			state = initialState;
			angleInterval = twistRange / (float)(states - 1);
			rotations = new Quaternion[states];
			float num = 0f;
			rotations[0] = Quaternion.identity;
			for (int i = 1; i < states; i++)
			{
				num += angleInterval;
				rotations[i] = Quaternion.Euler(0f, num, 0f);
			}
			knobTransform.localRotation = rotations[state];
			value = (float)state / (float)(states - 1);
			if (reverse)
			{
				value = 1f - value;
			}
			SetState(state);
		}
	}

	private void Start()
	{
		Initialize();
		BeginUngrabRoutine();
	}

	private void OnDrawGizmosSelected()
	{
		if (!knobTransform || states <= 0)
		{
			return;
		}
		float num = twistRange / (float)(states - 1);
		Vector3 forward = knobTransform.forward;
		Vector3 position = knobTransform.position;
		for (int i = 0; i < states; i++)
		{
			Vector3 to = position + Quaternion.AngleAxis(num * (float)i, knobTransform.up) * forward;
			float num2 = (float)i / (float)(states - 1);
			if (reverse)
			{
				num2 = 1f - num2;
			}
			Gizmos.color = Color.Lerp(Color.green, Color.red, num2);
			Gizmos.DrawLine(position, to);
		}
	}

	private void Vrint_OnStopInteraction(VRHandController controller)
	{
		grabbed = false;
		value = clampedValue;
		if ((bool)controller.gloveAnimation && !GetComponent<VRIntGlovePoser>())
		{
			controller.gloveAnimation.ClearInteractPose();
		}
		if (canPushButton)
		{
			ctrlr.OnThumbButtonPressed -= Ctrlr_OnThumbButtonPressed;
		}
		ctrlr = null;
		BeginUngrabRoutine();
	}

	private void Vrint_OnStartInteraction(VRHandController controller)
	{
		grabbed = true;
		ctrlr = controller;
		startUp = ctrlr.transform.InverseTransformDirection(knobTransform.parent.forward);
		if ((bool)controller.gloveAnimation && !GetComponent<VRIntGlovePoser>())
		{
			controller.gloveAnimation.SetKnobTransform(knobTransform, lockTransform, smallKnob);
			controller.gloveAnimation.SetPoseInteractable(GloveAnimation.Poses.Knob);
		}
		if (canPushButton)
		{
			ctrlr.OnThumbButtonPressed += Ctrlr_OnThumbButtonPressed;
		}
		StartCoroutine(GrabbedRoutine());
	}

	private void Ctrlr_OnThumbButtonPressed(VRHandController ctrlr)
	{
		if ((bool)ctrlr.gloveAnimation)
		{
			ctrlr.gloveAnimation.PlayTwistPushButton();
		}
		if (onPushButton != null)
		{
			onPushButton.Invoke();
		}
	}

	private IEnumerator GrabbedRoutine()
	{
		while (grabbed)
		{
			Vector3 vector = knobTransform.parent.InverseTransformDirection(ctrlr.transform.TransformDirection(startUp));
			vector.y = 0f;
			float num = Vector3.Angle(vector, Vector3.forward);
			num *= Mathf.Sign(Vector3.Dot(vector, Vector3.right));
			startUp = ctrlr.transform.InverseTransformDirection(knobTransform.parent.forward);
			value += num / twistRange;
			clampedValue = Mathf.Clamp01(value);
			float num2 = clampedValue;
			Quaternion quaternion = Quaternion.Euler(0f, num2 * twistRange, 0f);
			if (springy)
			{
				Quaternion b = Quaternion.Lerp(reverse ? rotations[states - 1 - state] : rotations[state], quaternion, 0.65f);
				knobTransform.localRotation = Quaternion.Lerp(knobTransform.localRotation, b, 20f * Time.deltaTime);
			}
			else
			{
				knobTransform.localRotation = quaternion;
			}
			int num3 = StateFromRotation();
			if (num3 != state)
			{
				SetState(num3);
			}
			yield return null;
		}
		BeginUngrabRoutine();
	}

	private void BeginUngrabRoutine()
	{
		if (ungrabRoutine != null)
		{
			StopCoroutine(ungrabRoutine);
		}
		if (base.gameObject.activeInHierarchy)
		{
			ungrabRoutine = StartCoroutine(UngrabRoutine());
			return;
		}
		Initialize();
		Quaternion localRotation = (reverse ? rotations[states - 1 - state] : rotations[state]);
		knobTransform.localRotation = localRotation;
	}

	private IEnumerator UngrabRoutine()
	{
		Initialize();
		Quaternion stateRot = (reverse ? rotations[states - 1 - state] : rotations[state]);
		while (!grabbed && knobTransform.localRotation != stateRot)
		{
			stateRot = (reverse ? rotations[states - 1 - state] : rotations[state]);
			knobTransform.localRotation = Quaternion.Lerp(knobTransform.localRotation, stateRot, 10f * Time.deltaTime);
			yield return null;
		}
	}

	public void RemoteSetState(int st)
	{
		SetState(st);
		SetRotationFromState();
		if ((bool)ctrlr)
		{
			Vrint_OnStartInteraction(ctrlr);
		}
		value = (float)state / (float)(states - 1);
		if (reverse)
		{
			value = 1f - value;
		}
		BeginUngrabRoutine();
	}

	public void RemoteSetStateNoEvents(int st)
	{
		state = st;
		value = (float)state / (float)(states - 1);
		if (reverse)
		{
			value = 1f - value;
		}
		BeginUngrabRoutine();
	}

	private void SetState(int st)
	{
		state = st;
		if (OnSetState != null)
		{
			OnSetState.Invoke(st);
		}
		if ((bool)ctrlr)
		{
			ctrlr.Vibrate(1f, 0.02f);
		}
	}

	private int StateFromRotation()
	{
		if (reverse)
		{
			return Mathf.RoundToInt((1f - clampedValue) * (float)(states - 1));
		}
		return Mathf.RoundToInt(clampedValue * (float)(states - 1));
	}

	private void SetRotationFromState()
	{
		if (reverse)
		{
			knobTransform.localRotation = rotations[states - 1 - state];
		}
		else
		{
			knobTransform.localRotation = rotations[state];
		}
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		if (vDataPersistent)
		{
			string nodeName = base.gameObject.name + "_VRTwistKnobInt";
			vDataNode.AddOrGetNode(nodeName).SetValue("state", state);
		}
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		if (vDataPersistent)
		{
			Initialize();
			string text = base.gameObject.name + "_VRTwistKnobInt";
			ConfigNode node = vDataNode.GetNode(text);
			if (node != null)
			{
				RemoteSetState(initialState = node.GetValue<int>("state"));
			}
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		if (quicksavePersistent)
		{
			ConfigNode configNode = new ConfigNode(base.gameObject.name + "_VRTwistKnobInt");
			configNode.SetValue("state", state);
			qsNode.AddNode(configNode);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		if (quicksavePersistent)
		{
			Initialize();
			string text = base.gameObject.name + "_VRTwistKnobInt";
			if (qsNode.HasNode(text))
			{
				int st = ConfigNodeUtils.ParseInt(qsNode.GetNode(text).GetValue("state"));
				RemoteSetState(st);
			}
		}
	}
}
