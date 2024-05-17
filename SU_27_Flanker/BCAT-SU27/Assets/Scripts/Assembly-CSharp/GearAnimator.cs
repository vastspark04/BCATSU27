using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GearAnimator : MonoBehaviour, IQSVehicleComponent
{
	public enum GearStates
	{
		Extended,
		Retracted,
		Moving
	}

	[Serializable]
	public class AnimatedGearLeg
	{
		public RaySpringDamper suspension;

		public Transform suspensionTransform;

		public Transform footTransform;

		public Transform retractedFootTransform;

		public float normStartExtend;

		public float normEndExtend = 1f;

		public float retractedSuspensionLength = -1f;

		private float extendedSuspensionLength;

		public bool vectorSlerp = true;

		private Quaternion footRotation;

		private Vector3 suspensionPosition;

		private Vector3 retractPos;

		private float localNormLength;

		private bool initialized;

		public void Initialize()
		{
			if (!initialized)
			{
				initialized = true;
				if (!suspensionTransform)
				{
					suspensionTransform = suspension.transform;
				}
				suspensionPosition = suspensionTransform.localPosition;
				footRotation = footTransform.localRotation;
				retractPos = footTransform.parent.InverseTransformPoint(retractedFootTransform.position + suspension.suspensionDistance * retractedFootTransform.up);
				extendedSuspensionLength = suspension.suspensionDistance;
				localNormLength = normEndExtend - normStartExtend;
			}
		}

		public void UpdateAnimation(float normTime)
		{
			if (!initialized)
			{
				Initialize();
			}
			float t = (normTime - normStartExtend) / localNormLength;
			if (vectorSlerp)
			{
				suspensionTransform.localPosition = Vector3.Slerp(retractPos, suspensionPosition, t);
			}
			else
			{
				suspensionTransform.localPosition = Vector3.Lerp(retractPos, suspensionPosition, t);
			}
			if (vectorSlerp)
			{
				footTransform.localRotation = Quaternion.Slerp(retractedFootTransform.localRotation, footRotation, t);
			}
			else
			{
				footTransform.localRotation = Quaternion.Lerp(retractedFootTransform.localRotation, footRotation, t);
			}
			suspensionTransform.localRotation = footTransform.localRotation;
			if (retractedSuspensionLength > 0f)
			{
				suspension.suspensionDistance = Mathf.Lerp(retractedSuspensionLength, extendedSuspensionLength, t);
			}
		}

		public void UpdateFootPosition()
		{
			Vector3 vector = suspension.point - suspension.transform.position;
			vector = Vector3.ClampMagnitude(vector, suspension.suspensionDistance);
			footTransform.position = suspensionTransform.position + vector;
		}
	}

	public Battery battery;

	public float powerDrain;

	public AnimatedGearLeg[] gearLegs;

	public float transitionTime = 2f;

	public UnityEvent OnOpen;

	public UnityEvent OnOpened;

	public UnityEvent OnClose;

	public UnityEvent OnClosed;

	public SimpleDrag dragComponent;

	public float deployedDragArea;

	public Animator gearDoorAnimator;

	public string gearExtendStateName;

	public int gearExtendStateLayer = -1;

	private int gearExtendStateHash;

	public EmissiveTextureLight statusLight;

	private GearStates fState;

	private GearStates _tState;

	private Coroutine coroutine;

	private float normTime = 1f;

	private bool started;

	private Coroutine footUpdateRoutine;

	public bool remoteOnly { get; set; }

	public GearStates state
	{
		get
		{
			return fState;
		}
		set
		{
			if (fState != value)
			{
				fState = value;
				this.OnSetFinalState?.Invoke(fState);
			}
		}
	}

	public GearStates targetState
	{
		get
		{
			return _tState;
		}
		private set
		{
			if (_tState != value)
			{
				_tState = value;
				this.OnSetTargetState?.Invoke(value);
			}
		}
	}

	public event Action<GearStates> OnSetFinalState;

	public event Action<GearStates> OnSetTargetState;

	public event Action<int> OnSetStateImmediate;

	private void Awake()
	{
		GearStates gearStates3 = (state = (targetState = GearStates.Extended));
		if ((bool)dragComponent)
		{
			dragComponent.area = deployedDragArea;
		}
		gearExtendStateHash = Animator.StringToHash(gearExtendStateName);
	}

	private void Start()
	{
		if (!started)
		{
			started = true;
			if ((bool)statusLight)
			{
				statusLight.SetColor(Color.green);
			}
			for (int i = 0; i < gearLegs.Length; i++)
			{
				gearLegs[i].Initialize();
			}
		}
	}

	private void OnEnable()
	{
		if ((bool)gearDoorAnimator)
		{
			UpdateAnimatorPosition((state == GearStates.Extended) ? 1 : 0);
		}
		EnsureFootUpdateRoutine();
	}

	public void Toggle()
	{
		if (state != GearStates.Moving)
		{
			if (state == GearStates.Extended)
			{
				Retract();
			}
			else
			{
				Extend();
			}
		}
	}

	public void Extend(float speedMult = 1f, bool requireBatt = true)
	{
		if (remoteOnly)
		{
			return;
		}
		targetState = GearStates.Extended;
		if (state != 0)
		{
			if (coroutine != null)
			{
				StopCoroutine(coroutine);
			}
			if (base.gameObject.activeInHierarchy)
			{
				coroutine = StartCoroutine(ExtendRoutine(speedMult, requireBatt));
			}
		}
		EnsureFootUpdateRoutine();
	}

	public void Retract(float speedMult = 1f, bool requireBatt = true)
	{
		if (remoteOnly)
		{
			return;
		}
		targetState = GearStates.Retracted;
		if (state != GearStates.Retracted)
		{
			if (coroutine != null)
			{
				StopCoroutine(coroutine);
			}
			if (base.gameObject.activeInHierarchy)
			{
				coroutine = StartCoroutine(RetractRoutine(speedMult, requireBatt));
			}
		}
	}

	public void SetExtend(int e)
	{
		if (!remoteOnly)
		{
			if (e == 0)
			{
				Retract();
			}
			else
			{
				Extend();
			}
		}
	}

	public void ExtendImmediate()
	{
		if (!remoteOnly)
		{
			Extend(100f, requireBatt: false);
			this.OnSetStateImmediate?.Invoke(1);
		}
	}

	private IEnumerator ExtendRoutine(float speedMult, bool requireBatt = true)
	{
		if (normTime == 0f && OnOpen != null)
		{
			OnOpen.Invoke();
		}
		if ((bool)statusLight)
		{
			statusLight.SetColor(Color.yellow);
		}
		for (int i = 0; i < gearLegs.Length; i++)
		{
			gearLegs[i].suspension.enabled = true;
		}
		state = GearStates.Moving;
		EnsureFootUpdateRoutine();
		while (normTime < 1f)
		{
			for (int j = 0; j < gearLegs.Length; j++)
			{
				gearLegs[j].UpdateAnimation(normTime);
			}
			UpdateAnimatorPosition(normTime);
			normTime += speedMult * Time.deltaTime / transitionTime;
			normTime = Mathf.Clamp01(normTime);
			if ((bool)dragComponent)
			{
				dragComponent.area = normTime * deployedDragArea;
			}
			if (requireBatt && (bool)battery)
			{
				while (!battery.Drain(powerDrain * Time.deltaTime))
				{
					yield return null;
				}
			}
			yield return null;
		}
		normTime = 1f;
		for (int k = 0; k < gearLegs.Length; k++)
		{
			gearLegs[k].UpdateAnimation(normTime);
		}
		UpdateAnimatorPosition(normTime);
		if (OnOpened != null)
		{
			OnOpened.Invoke();
		}
		state = GearStates.Extended;
		EnsureFootUpdateRoutine();
		if ((bool)statusLight)
		{
			statusLight.SetColor(Color.green);
		}
	}

	private IEnumerator RetractRoutine(float speedMult, bool requireBatt = true)
	{
		if (normTime == 1f && OnClose != null)
		{
			OnClose.Invoke();
		}
		if ((bool)statusLight)
		{
			statusLight.SetColor(Color.yellow);
		}
		state = GearStates.Moving;
		EnsureFootUpdateRoutine();
		while (normTime > 0f)
		{
			for (int i = 0; i < gearLegs.Length; i++)
			{
				gearLegs[i].UpdateAnimation(normTime);
			}
			UpdateAnimatorPosition(normTime);
			normTime = Mathf.MoveTowards(normTime, 0f, speedMult * Time.deltaTime / transitionTime);
			if ((bool)dragComponent)
			{
				dragComponent.area = normTime * deployedDragArea;
			}
			if ((bool)battery && requireBatt)
			{
				while (!battery.Drain(powerDrain * Time.deltaTime))
				{
					yield return null;
				}
			}
			yield return null;
		}
		normTime = 0f;
		for (int j = 0; j < gearLegs.Length; j++)
		{
			gearLegs[j].UpdateAnimation(normTime);
			gearLegs[j].suspension.enabled = false;
			if (gearLegs[j].retractedSuspensionLength > 0f)
			{
				gearLegs[j].footTransform.position = gearLegs[j].suspension.transform.TransformPoint(new Vector3(0f, 0f - gearLegs[j].retractedSuspensionLength, 0f));
			}
			else
			{
				gearLegs[j].footTransform.position = gearLegs[j].retractedFootTransform.position;
			}
		}
		UpdateAnimatorPosition(normTime);
		state = GearStates.Retracted;
		if ((bool)dragComponent)
		{
			dragComponent.area = 0f;
		}
		if (OnClosed != null)
		{
			OnClosed.Invoke();
		}
		if ((bool)statusLight)
		{
			statusLight.SetColor(Color.black);
		}
		UpdateFootPositions();
	}

	public void RetractImmediate()
	{
		if (!remoteOnly)
		{
			Retract(100f, requireBatt: false);
			this.OnSetStateImmediate?.Invoke(0);
		}
	}

	private void UpdateAnimatorPosition(float normTime)
	{
		if ((bool)gearDoorAnimator)
		{
			gearDoorAnimator.Play(gearExtendStateHash, gearExtendStateLayer, Mathf.Min(normTime, 0.999f));
		}
	}

	private void EnsureFootUpdateRoutine()
	{
		if (footUpdateRoutine != null)
		{
			StopCoroutine(footUpdateRoutine);
		}
		if (base.gameObject.activeInHierarchy)
		{
			footUpdateRoutine = StartCoroutine(FootUpdateRoutine());
		}
	}

	private IEnumerator FootUpdateRoutine()
	{
		WaitForEndOfFrame eof = new WaitForEndOfFrame();
		while (state != GearStates.Retracted)
		{
			yield return eof;
			UpdateFootPositions();
		}
		float t = Time.time;
		while (Time.time - t < 2f)
		{
			yield return eof;
			UpdateFootPositions();
		}
	}

	private void UpdateFootPositions()
	{
		for (int i = 0; i < gearLegs.Length; i++)
		{
			gearLegs[i].UpdateFootPosition();
		}
	}

	public GearStates GetCurrentState()
	{
		return state;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_GearAnimator");
		bool value = state == GearStates.Retracted;
		configNode.SetValue("retracted", value);
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = base.gameObject.name + "_GearAnimator";
		if (qsNode.HasNode(text) && ConfigNodeUtils.ParseBool(qsNode.GetNode(text).GetValue("retracted")))
		{
			RetractImmediate();
		}
	}
}
