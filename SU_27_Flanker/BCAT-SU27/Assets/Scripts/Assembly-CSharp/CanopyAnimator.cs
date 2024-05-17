using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

public class CanopyAnimator : MonoBehaviour, IQSVehicleComponent
{
	private enum AnimStates
	{
		Stopped,
		Opening,
		Closing
	}

	public Transform canopyTf;

	public FlightInfo flightInfo;

	public Animator animator;

	public string animName = "canopy";

	public float animTime = 4f;

	private bool gotAnimID;

	private int animID;

	private float nrmTime;

	private Coroutine animCoroutine;

	private bool broken;

	public AudioSource canopyBreakAudioSource;

	public AudioClip canopyBreakSound;

	public float nrmBreakTime = 0.4f;

	private AnimStates animState;

	private bool remote;

	public VRInteractable[] handleInteractables;

	private VehiclePart vPart;

	private bool mp_isMine;

	private Transform canopyParent;

	private Vector3 canopyLocalPos;

	private Quaternion canopyLocalRot;

	private Rigidbody initialRB;

	private List<MonoBehaviour> disabledRBDs = new List<MonoBehaviour>();

	public bool isBroken => broken;

	public int targetState { get; private set; }

	public event Action<int> OnSetState;

	public event Action OnBroken;

	public void SetToRemote()
	{
		remote = true;
	}

	private void Start()
	{
		RecordCanopyInitials();
		GetAnimID();
		vPart = GetComponentInParent<VehiclePart>();
		vPart.OnRepair.AddListener(OnRepair);
		if (VTOLMPUtils.IsMultiplayer())
		{
			VTNetEntity componentInParent = GetComponentInParent<VTNetEntity>();
			if ((bool)componentInParent)
			{
				StartCoroutine(CheckIsMine(componentInParent));
			}
		}
	}

	private IEnumerator CheckIsMine(VTNetEntity netEnt)
	{
		while (netEnt.ownerID == 0L)
		{
			yield return null;
		}
		mp_isMine = netEnt.isMine;
	}

	private void GetAnimID()
	{
		if (!gotAnimID)
		{
			animID = Animator.StringToHash(animName);
			gotAnimID = true;
		}
	}

	private void OnDestroy()
	{
		if ((bool)canopyTf)
		{
			UnityEngine.Object.Destroy(canopyTf.gameObject);
		}
		if ((bool)AudioController.instance && mp_isMine)
		{
			AudioController.instance.SetExteriorOpening("canopy", 0f);
		}
	}

	private IEnumerator AnimRoutine(bool open)
	{
		GetAnimID();
		animState = (open ? AnimStates.Opening : AnimStates.Closing);
		_ = open;
		float tgtTime = (open ? 1f : 0f);
		float interval = 1f / animTime;
		while (nrmTime != tgtTime)
		{
			if (broken)
			{
				animState = AnimStates.Stopped;
				yield break;
			}
			nrmTime = Mathf.MoveTowards(nrmTime, tgtTime, interval * Time.deltaTime);
			if (!remote)
			{
				AudioController.instance.SetExteriorOpening("canopy", nrmTime);
			}
			animator.Play(animID, 0, nrmTime);
			yield return null;
		}
		animState = AnimStates.Stopped;
		if (open || handleInteractables == null)
		{
			yield break;
		}
		VRInteractable[] array = handleInteractables;
		foreach (VRInteractable vRInteractable in array)
		{
			if ((bool)vRInteractable)
			{
				vRInteractable.enabled = true;
			}
		}
	}

	public void SetCanopyState(int st)
	{
		if (st > 0)
		{
			Open();
		}
		else
		{
			Close();
		}
		this.OnSetState?.Invoke(st);
	}

	private void Open()
	{
		if (animCoroutine != null)
		{
			StopCoroutine(animCoroutine);
		}
		if (broken)
		{
			return;
		}
		targetState = 1;
		animCoroutine = StartCoroutine(AnimRoutine(open: true));
		if (handleInteractables == null)
		{
			return;
		}
		VRInteractable[] array = handleInteractables;
		foreach (VRInteractable vRInteractable in array)
		{
			if ((bool)vRInteractable)
			{
				if ((bool)vRInteractable.activeController)
				{
					vRInteractable.activeController.ReleaseFromInteractable();
				}
				vRInteractable.enabled = false;
			}
		}
	}

	private void Close()
	{
		if (animCoroutine != null)
		{
			StopCoroutine(animCoroutine);
		}
		if (!broken)
		{
			targetState = 0;
			animCoroutine = StartCoroutine(AnimRoutine(open: false));
		}
	}

	public void SetCanopyImmediate(bool open)
	{
		if (animCoroutine != null)
		{
			StopCoroutine(animCoroutine);
		}
		targetState = (open ? 1 : 0);
		nrmTime = targetState;
		animator.Play(animID, 0, targetState);
		if (!remote)
		{
			AudioController.instance.SetExteriorOpening("canopy", targetState);
		}
		animState = AnimStates.Stopped;
		if (handleInteractables != null)
		{
			VRInteractable[] array = handleInteractables;
			foreach (VRInteractable vRInteractable in array)
			{
				if (!vRInteractable)
				{
					continue;
				}
				if (open)
				{
					if ((bool)vRInteractable.activeController)
					{
						vRInteractable.activeController.ReleaseFromInteractable();
					}
					vRInteractable.enabled = false;
				}
				else
				{
					vRInteractable.enabled = true;
				}
			}
		}
		this.OnSetState?.Invoke(targetState);
	}

	private void Update()
	{
		if (!broken && nrmTime > nrmBreakTime && flightInfo.airspeed > 100f)
		{
			BreakCanopy();
		}
	}

	public void BreakCanopy()
	{
		if (!broken)
		{
			if (!remote)
			{
				AudioController.instance.SetExteriorOpening("canopy", 1f);
			}
			if ((bool)canopyBreakAudioSource)
			{
				canopyBreakAudioSource.PlayOneShot(canopyBreakSound);
			}
			FloatingOrigin.instance.AddQueuedFixedUpdateAction(BreakCanopyFinal);
		}
	}

	private void BreakCanopyFinal()
	{
		if (broken)
		{
			return;
		}
		vPart.health.Damage(vPart.health.maxHealth + 1f, canopyTf.position, Health.DamageTypes.Impact, null, "Canopy broke due to being open at speed");
		broken = true;
		canopyTf.parent = null;
		FloatingOriginTransform floatingOriginTransform = canopyTf.gameObject.AddComponent<FloatingOriginTransform>();
		Rigidbody rigidbody = canopyTf.gameObject.AddComponent<Rigidbody>();
		rigidbody.isKinematic = false;
		rigidbody.mass = 0.1f;
		rigidbody.angularDrag = 0.05f;
		rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		rigidbody.velocity = flightInfo.rb.GetPointVelocity(canopyTf.position);
		floatingOriginTransform.SetRigidbody(rigidbody);
		disabledRBDs.Clear();
		IParentRBDependent[] componentsInChildrenImplementing = canopyTf.gameObject.GetComponentsInChildrenImplementing<IParentRBDependent>(includeInactive: true);
		foreach (IParentRBDependent parentRBDependent in componentsInChildrenImplementing)
		{
			parentRBDependent.SetParentRigidbody(rigidbody);
			if (parentRBDependent is MonoBehaviour)
			{
				MonoBehaviour monoBehaviour = (MonoBehaviour)parentRBDependent;
				if (!monoBehaviour.enabled)
				{
					monoBehaviour.enabled = true;
					disabledRBDs.Add(monoBehaviour);
					Debug.LogFormat("Canopy break enabled script: {0} ({1})", monoBehaviour.GetType().ToString(), monoBehaviour.gameObject.name);
				}
			}
		}
		this.OnBroken?.Invoke();
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode(base.gameObject.name + "_CanopyAnimator");
		configNode.SetValue("nrmTime", nrmTime);
		configNode.SetValue("animState", animState);
		configNode.SetValue("broken", broken);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(base.gameObject.name + "_CanopyAnimator");
		if (node == null)
		{
			return;
		}
		nrmTime = node.GetValue<float>("nrmTime");
		animState = node.GetValue<AnimStates>("animState");
		if (animState == AnimStates.Opening)
		{
			Open();
		}
		else if (animState == AnimStates.Closing)
		{
			Close();
		}
		else
		{
			GetAnimID();
			if (!remote)
			{
				AudioController.instance.SetExteriorOpening("canopy", nrmTime);
			}
			animator.Play(animID, 0, nrmTime);
		}
		if (node.GetValue<bool>("broken"))
		{
			if (!remote)
			{
				AudioController.instance.SetExteriorOpening("brokenCanopy", 1f);
			}
			BreakCanopyFinal();
		}
	}

	private void RecordCanopyInitials()
	{
		canopyParent = canopyTf.parent;
		canopyLocalPos = canopyTf.localPosition;
		canopyLocalRot = canopyTf.localRotation;
		initialRB = GetComponentInParent<Rigidbody>();
	}

	private void OnRepair()
	{
		if (!broken)
		{
			return;
		}
		UnityEngine.Object.Destroy(canopyTf.GetComponent<FloatingOriginTransform>());
		UnityEngine.Object.Destroy(canopyTf.GetComponent<Rigidbody>());
		canopyTf.parent = canopyParent;
		canopyTf.localPosition = canopyLocalPos;
		canopyTf.localRotation = canopyLocalRot;
		IParentRBDependent[] componentsInChildrenImplementing = canopyTf.gameObject.GetComponentsInChildrenImplementing<IParentRBDependent>(includeInactive: true);
		for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
		{
			componentsInChildrenImplementing[i].SetParentRigidbody(initialRB);
		}
		foreach (MonoBehaviour disabledRBD in disabledRBDs)
		{
			disabledRBD.enabled = false;
		}
		broken = false;
	}
}
