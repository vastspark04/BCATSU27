using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRDoor : MonoBehaviour
{
	[Serializable]
	public class VRDoorSounds
	{
		public AudioClip slamSound;

		public AudioSource slamSource;

		public AnimationCurve slamVolumeCurve;

		public AudioClip latchedSound;

		public AudioSource latchSource;
	}

	public List<VRDoor> attachedHandles;

	public Transform doorTransform;

	public float maxDoorAngle;

	public float unhingedSpringAngle = 5f;

	public string openingName = "vrdoor";

	private bool createdFName;

	private string _fName;

	public bool reverseDirection;

	public bool openOnSpawn_sp;

	public bool openOnSpawn_mp;

	public int muvsSeatIdx;

	[Tooltip("When rearming, coDoors are checked to see if any should be opened automatically.")]
	public List<VRDoor> coDoors;

	private float radius;

	private IKTwo armIk;

	private float currAngle;

	private bool affectAudio = true;

	private bool unlocked;

	private bool latched = true;

	public VRDoorSounds sounds;

	private Vector3 relStartPos;

	private Vector3 iOffset;

	private float startAngle;

	private VRHandController controller;

	private bool grabbed;

	private Coroutine grabbedRoutine;

	private float debug_armLimAngle;

	private float debug_a;

	private float debug_b;

	private float debug_c;

	private float debug_cosT;

	private Vector3 closedDoorLocalPos;

	public bool doLimitByArmLength;

	public float armLengthAdjustment = -0.1f;

	public bool rightSideReference;

	private string finalOpeningName
	{
		get
		{
			if (!createdFName)
			{
				createdFName = true;
				int num = GetInstanceID();
				foreach (VRDoor attachedHandle in attachedHandles)
				{
					int instanceID = attachedHandle.GetInstanceID();
					if (instanceID > num)
					{
						num = instanceID;
					}
				}
				_fName = $"{openingName}_{num}";
			}
			return _fName;
		}
	}

	public float currentAngle => currAngle;

	public bool isLatched => latched;

	public event Action<float> OnSetNormalizedState;

	public event Action<bool> OnLatched;

	public void SetAffectAudio(bool a)
	{
		if (!string.IsNullOrEmpty(openingName) && a != affectAudio)
		{
			affectAudio = a;
			if (affectAudio)
			{
				AudioController.instance.SetExteriorOpening(finalOpeningName, Mathf.Clamp01(currentAngle / 25f));
			}
			else
			{
				AudioController.instance.SetExteriorOpening(finalOpeningName, 0f);
			}
		}
	}

	public void SetUnlocked(int s)
	{
		unlocked = s > 0;
		if (unlocked && latched)
		{
			latched = false;
			this.OnLatched?.Invoke(obj: false);
		}
	}

	private void Awake()
	{
		VRInteractable component = GetComponent<VRInteractable>();
		component.OnStartInteraction += VrInt_OnStartInteraction;
		component.OnStopInteraction += VrInt_OnStopInteraction;
		radius = (doorTransform.position - base.transform.position).magnitude;
		closedDoorLocalPos = doorTransform.parent.InverseTransformPoint(base.transform.position);
	}

	private void VrInt_OnStartInteraction(VRHandController controller)
	{
		this.controller = controller;
		relStartPos = doorTransform.parent.InverseTransformPoint(controller.transform.position);
		relStartPos.y = 0f;
		grabbed = true;
		iOffset = base.transform.InverseTransformPoint(controller.transform.position);
		if (reverseDirection)
		{
			currAngle = Mathf.Repeat(0f - doorTransform.localEulerAngles.y, 360f);
		}
		else
		{
			currAngle = Mathf.Repeat(doorTransform.localEulerAngles.y, 360f);
		}
		if (currAngle > 1f && latched)
		{
			latched = false;
			this.OnLatched?.Invoke(obj: false);
		}
		foreach (VRDoor attachedHandle in attachedHandles)
		{
			attachedHandle.grabbed = true;
		}
		if (grabbedRoutine != null)
		{
			StopCoroutine(grabbedRoutine);
		}
		grabbedRoutine = StartCoroutine(GrabbedRoutine());
		armIk = controller.gloveAnimation.ik;
	}

	private void VrInt_OnStopInteraction(VRHandController controller)
	{
		grabbed = false;
		foreach (VRDoor attachedHandle in attachedHandles)
		{
			attachedHandle.grabbed = false;
			attachedHandle.latched = latched;
		}
		armIk = null;
	}

	private IEnumerator GrabbedRoutine()
	{
		float newAngle = currAngle;
		while (grabbed)
		{
			float num = newAngle;
			if (!latched)
			{
				Vector3 vector = base.transform.TransformPoint(iOffset);
				Vector3 vector2 = Vector3.Lerp(vector, controller.transform.position, 0.1f);
				Vector3 vector3 = vector2 - vector;
				vector3 = Vector3.ClampMagnitude(vector3, 0.07f);
				vector2 = vector + vector3;
				Vector3 toDirection = doorTransform.parent.InverseTransformPoint(vector2);
				toDirection.y = 0f;
				Vector3 vector4 = Vector3.Cross(Vector3.up, relStartPos);
				if (reverseDirection)
				{
					vector4 = -vector4;
				}
				float num2 = VectorUtils.SignedAngle(relStartPos, toDirection, vector4);
				float max = maxDoorAngle;
				if ((bool)armIk && doLimitByArmLength)
				{
					max = Mathf.Min(maxDoorAngle, GetArmLimitedAngle());
				}
				newAngle = Mathf.Clamp(currAngle + num2, 0f, max);
				this.OnSetNormalizedState?.Invoke(newAngle / maxDoorAngle);
				doorTransform.localRotation = Quaternion.Euler(0f, reverseDirection ? (0f - newAngle) : newAngle, 0f);
			}
			if (newAngle == 0f && !unlocked && !latched)
			{
				sounds.latchSource.PlayOneShot(sounds.latchedSound);
				latched = true;
				this.OnLatched?.Invoke(obj: true);
			}
			if (newAngle == 0f && newAngle != num)
			{
				float time = num / Time.deltaTime;
				sounds.slamSource.volume = sounds.slamVolumeCurve.Evaluate(time);
				sounds.slamSource.PlayOneShot(sounds.slamSound);
			}
			if (affectAudio && !string.IsNullOrEmpty(openingName))
			{
				AudioController.instance.SetExteriorOpening(finalOpeningName, Mathf.Clamp01(newAngle / 25f));
			}
			for (int i = 0; i < attachedHandles.Count; i++)
			{
				attachedHandles[i].currAngle = newAngle;
			}
			yield return null;
		}
		currAngle = newAngle;
		while (!grabbed && !latched && newAngle < unhingedSpringAngle)
		{
			newAngle = (currAngle = Mathf.Lerp(newAngle, unhingedSpringAngle, 5f * Time.deltaTime));
			doorTransform.localRotation = Quaternion.Euler(0f, reverseDirection ? (0f - newAngle) : newAngle, 0f);
			if (affectAudio && !string.IsNullOrEmpty(openingName))
			{
				AudioController.instance.SetExteriorOpening(finalOpeningName, Mathf.Clamp01(newAngle / 25f));
			}
			for (int j = 0; j < attachedHandles.Count; j++)
			{
				attachedHandles[j].currAngle = newAngle;
			}
			this.OnSetNormalizedState?.Invoke(newAngle / maxDoorAngle);
			yield return null;
		}
	}

	public void RemoteSetState(float s, bool sendEvent = true)
	{
		s = Mathf.Clamp01(s);
		float num = (currAngle = s * maxDoorAngle);
		doorTransform.localRotation = Quaternion.Euler(0f, reverseDirection ? (0f - num) : num, 0f);
		if (affectAudio && !string.IsNullOrEmpty(openingName))
		{
			AudioController.instance.SetExteriorOpening(finalOpeningName, Mathf.Clamp01(num / 25f));
		}
		if (!unlocked)
		{
			bool flag = s == 0f;
			if (flag != latched)
			{
				latched = flag;
				this.OnLatched?.Invoke(latched);
			}
		}
		foreach (VRDoor attachedHandle in attachedHandles)
		{
			attachedHandle.latched = latched;
			attachedHandle.currAngle = currAngle;
		}
		if (!sendEvent)
		{
			return;
		}
		this.OnSetNormalizedState?.Invoke(currAngle / maxDoorAngle);
		if (!grabbed && !latched)
		{
			if (grabbedRoutine != null)
			{
				StopCoroutine(grabbedRoutine);
			}
			grabbedRoutine = StartCoroutine(GrabbedRoutine());
		}
	}

	private float GetArmLimitedAngle()
	{
		Transform transform = armIk.transform;
		float num = armIk.totalLength + armLengthAdjustment;
		float magnitude = (transform.position - doorTransform.parent.position).magnitude;
		float num2 = radius;
		int num3 = ((!rightSideReference) ? 1 : (-1));
		float num4 = VectorUtils.SignedAngle(doorTransform.parent.forward, Vector3.ProjectOnPlane(transform.position - doorTransform.position, doorTransform.parent.up), doorTransform.parent.right * num3);
		debug_a = num2;
		debug_b = num;
		debug_c = magnitude;
		float num5 = Mathf.Acos(debug_cosT = (num2 * num2 + magnitude * magnitude - num * num) / (2f * num2 * magnitude));
		float num6 = 57.29578f * num5;
		return debug_armLimAngle = num6 + num4;
	}

	public static float SignedAngle(Vector3 fromDirection, Vector3 toDirection, Vector3 referenceRight)
	{
		fromDirection.Normalize();
		toDirection.Normalize();
		float num = Vector3.Angle(fromDirection, toDirection);
		return Mathf.Sign(Vector3.Dot(toDirection - fromDirection, referenceRight)) * num;
	}
}
