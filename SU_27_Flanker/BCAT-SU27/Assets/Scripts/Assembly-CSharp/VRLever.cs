using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(VRInteractable))]
public class VRLever : MonoBehaviour, IQSVehicleComponent
{
	public bool quicksavePersistent = true;

	public Transform leverTransform;

	public float angleLimit;

	public int initialState;

	public bool debugStates;

	public int states = 2;

	public bool springTo;

	public int springToIdx;

	public IntEvent OnSetState;

	public bool springy;

	public bool fireEventOnStart;

	private VRInteractable vrint;

	private bool grabbed;

	private int state;

	private Quaternion[] rotations;

	private VRHandController ctrlr;

	private float angleInterval;

	private Vector3 interactionOffset;

	private Coroutine returnRoutine;

	private Vector3 interactionSwitchOffset;

	public VRLever[] connectedLevers;

	public int lockedToState { get; private set; } = -1;


	public int currentState => state;

	public void LockTo(int state)
	{
		if (currentState != state)
		{
			RemoteSetState(state);
		}
		lockedToState = state;
	}

	public void Unlock()
	{
		lockedToState = -1;
	}

	private void Awake()
	{
		vrint = GetComponent<VRInteractable>();
		vrint.OnStartInteraction += Vrint_OnStartInteraction;
		vrint.OnStopInteraction += Vrint_OnStopInteraction;
		state = initialState;
		angleInterval = 2f * angleLimit / (float)(states - 1);
		rotations = new Quaternion[states];
		float num = angleLimit;
		rotations[0] = Quaternion.Euler(angleLimit, 0f, 0f);
		for (int i = 1; i < states; i++)
		{
			num -= angleInterval;
			rotations[i] = Quaternion.Euler(num, 0f, 0f);
		}
		leverTransform.localRotation = rotations[state];
	}

	private void Start()
	{
		if (fireEventOnStart)
		{
			SetState(state);
		}
		ReturnSpring();
	}

	private void ReturnSpring()
	{
		if (base.gameObject.activeInHierarchy && returnRoutine == null)
		{
			returnRoutine = StartCoroutine(SpringReturnRoutine());
		}
	}

	private void Vrint_OnStopInteraction(VRHandController controller)
	{
		grabbed = false;
		if ((bool)controller.gloveAnimation)
		{
			controller.gloveAnimation.ClearInteractPose();
		}
		if (springTo && state != springToIdx)
		{
			RemoteSetState(springToIdx);
		}
	}

	private void Vrint_OnStartInteraction(VRHandController controller)
	{
		grabbed = true;
		ctrlr = controller;
		if ((bool)ctrlr.gloveAnimation)
		{
			ctrlr.gloveAnimation.SetLeverTransform(base.transform);
			ctrlr.gloveAnimation.SetPoseInteractable(GloveAnimation.Poses.Pinch);
		}
		interactionOffset = leverTransform.parent.InverseTransformVector(base.transform.position - ctrlr.transform.position);
		StartCoroutine(GrabbedRoutine());
	}

	private IEnumerator GrabbedRoutine()
	{
		while (grabbed)
		{
			Vector3 vector = leverTransform.parent.InverseTransformPoint(ctrlr.transform.position) + interactionOffset;
			vector.z = Mathf.Abs(vector.z);
			Vector3 vector2 = Vector3.ProjectOnPlane(vector, Vector3.right);
			int num;
			if (springy)
			{
				Quaternion quaternion = Quaternion.RotateTowards(Quaternion.identity, Quaternion.LookRotation(vector2), angleLimit);
				if (lockedToState >= 0)
				{
					Vector3 current = rotations[lockedToState] * Vector3.forward;
					quaternion = Quaternion.RotateTowards(Quaternion.identity, Quaternion.LookRotation(Vector3.RotateTowards(current, vector2, (float)Math.PI / 180f * angleInterval / 2f, 0f)), angleLimit);
					Quaternion b = Quaternion.Lerp(rotations[lockedToState], quaternion, 0.65f);
					leverTransform.localRotation = Quaternion.Lerp(leverTransform.localRotation, b, 20f * Time.deltaTime);
					num = lockedToState;
				}
				else
				{
					Quaternion b2 = Quaternion.Lerp(rotations[state], quaternion, 0.65f);
					leverTransform.localRotation = Quaternion.Lerp(leverTransform.localRotation, b2, 20f * Time.deltaTime);
					num = StateFromRotation(quaternion);
				}
			}
			else
			{
				if (lockedToState >= 0)
				{
					vector2 = Vector3.RotateTowards(rotations[lockedToState] * Vector3.forward, vector2, (float)Math.PI / 180f * angleInterval / 3f, 0f);
				}
				leverTransform.localRotation = Quaternion.RotateTowards(Quaternion.identity, Quaternion.LookRotation(vector2), angleLimit);
				num = StateFromRotation(leverTransform.localRotation);
			}
			if (num != state)
			{
				SetState(num);
			}
			yield return null;
		}
		ReturnSpring();
	}

	private IEnumerator SpringReturnRoutine()
	{
		if (rotations != null)
		{
			while (!grabbed && leverTransform.localRotation != rotations[state])
			{
				leverTransform.localRotation = Quaternion.Lerp(leverTransform.localRotation, rotations[state], 10f * Time.deltaTime);
				yield return null;
			}
			returnRoutine = null;
		}
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
		if (connectedLevers != null)
		{
			VRLever[] array = connectedLevers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].RemoteSetStateNoEvents(st);
			}
		}
	}

	public void SetCurrentState(int st)
	{
		state = st;
		initialState = st;
	}

	public void RemoteSetState(int st)
	{
		if ((bool)ctrlr)
		{
			ctrlr.ReleaseFromInteractable();
		}
		SetState(st);
		ReturnSpring();
		if (springTo && state != springToIdx)
		{
			RemoteSetState(springToIdx);
		}
	}

	public void RemoteSetStateNoEvents(int st)
	{
		if ((bool)ctrlr)
		{
			ctrlr.ReleaseFromInteractable();
		}
		state = st;
		ReturnSpring();
	}

	private int StateFromRotation(Quaternion leverRotation)
	{
		return Mathf.RoundToInt(Quaternion.Angle(rotations[0], leverRotation) / angleInterval);
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		if (quicksavePersistent)
		{
			ConfigNode configNode = new ConfigNode(base.gameObject.name + "_VRLever");
			configNode.SetValue("state", state);
			qsNode.AddNode(configNode);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		if (quicksavePersistent)
		{
			string text = base.gameObject.name + "_VRLever";
			if (qsNode.HasNode(text))
			{
				int st = ConfigNodeUtils.ParseInt(qsNode.GetNode(text).GetValue("state"));
				RemoteSetState(st);
			}
		}
	}
}
