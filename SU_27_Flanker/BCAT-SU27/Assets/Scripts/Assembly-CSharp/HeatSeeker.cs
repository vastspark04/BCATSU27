using System;
using System.Collections.Generic;
using UnityEngine;

public class HeatSeeker : MonoBehaviour, IQSMissileComponent, IFloatingOriginShiftable
{
	public enum SeekerModes
	{
		Caged,
		Uncaged,
		VerticalScan,
		HeadTrack,
		HardLock
	}

	public struct HeatTarget
	{
		public Vector3 position;

		public float heatScore;

		public float baseHeat;

		public Vector3 velocity;

		public HeatTarget(Vector3 position, float heatScore, float baseHeat, Vector3 velocity)
		{
			this.position = position;
			this.heatScore = heatScore;
			this.baseHeat = baseHeat;
			this.velocity = velocity;
		}
	}

	public bool seekerEnabled;

	private Vector3 lastTargetPosition;

	private Vector3 localSeekerDirection = Vector3.forward;

	public float gimbalFOV = 140f;

	public float seekerFOV = 4f;

	public float seekerScanTrackRate = 10f;

	public float seekerTrackRate = 30f;

	public float sensitivity = 4000f;

	public float minThreshold;

	[Tooltip("Counter-countermeasure.  Higher = less likeley to track flares.")]
	public float ccm = 1f;

	private const int VIS_CHECK_FRAME_INTERVAL = 8;

	public AudioSource seekerAudio;

	[Range(0f, 1f)]
	public float seekerAudioVol = 1f;

	public AudioSource lockToneAudio;

	[Range(0f, 1f)]
	public float lockAudioVol = 1f;

	[HideInInspector]
	public Transform headTransform;

	[HideInInspector]
	public Transform vssReferenceTransform;

	private Transform fwdOverrideTransform;

	private Vector3 uncagedScanDir;

	public float uncagedScanSpeed = 360f;

	private float verticalScanAngle;

	public float verticalScanSweepAngleLimit = 30f;

	private int verticalScanDir = 1;

	public float verticalScanSweepSpeed = 60f;

	public bool debugSeeker;

	public LockingRadar lockingRadar;

	private Transform myTransform;

	private Vector3 lastParentForward;

	private Vector3 localFwd = Vector3.forward;

	private bool addedShifter;

	private int visibilityCheckFrame;

	private List<HeatEmitter> visibleEmitters = new List<HeatEmitter>();

	private int visEmittersCount;

	private Camera debugCam;

	public Vector3 targetPosition { get; private set; }

	public Vector3 targetVelocity { get; private set; }

	public float seekerLock { get; private set; }

	public SeekerModes seekerMode { get; private set; }

	public Actor likelyTargetActor { get; private set; }

	public bool manualUncage { get; set; }

	public void SetSeekerMode(SeekerModes mode)
	{
		switch (mode)
		{
		case SeekerModes.HardLock:
			return;
		case SeekerModes.HeadTrack:
			if (!headTransform)
			{
				return;
			}
			break;
		}
		seekerMode = mode;
	}

	public void ToggleSeekerMode()
	{
		switch (seekerMode)
		{
		case SeekerModes.Caged:
			seekerMode = SeekerModes.Uncaged;
			break;
		case SeekerModes.Uncaged:
			seekerMode = SeekerModes.VerticalScan;
			break;
		case SeekerModes.VerticalScan:
			if ((bool)headTransform)
			{
				seekerMode = SeekerModes.HeadTrack;
			}
			else
			{
				seekerMode = SeekerModes.Caged;
			}
			break;
		case SeekerModes.HeadTrack:
			seekerMode = SeekerModes.Caged;
			break;
		}
	}

	public void SetHardLock()
	{
		seekerMode = SeekerModes.HardLock;
		if (debugSeeker)
		{
			SetupDebugCam();
		}
	}

	public void SetForwardOverrideTransform(Transform tf)
	{
		fwdOverrideTransform = tf;
		localFwd = base.transform.parent.InverseTransformDirection(fwdOverrideTransform.forward);
		uncagedScanDir = Quaternion.AngleAxis(seekerFOV * 0.15f, Vector3.right) * localFwd;
	}

	private void Awake()
	{
		myTransform = base.transform;
		uncagedScanDir = Quaternion.AngleAxis(seekerFOV * 0.15f, Vector3.right) * Vector3.forward;
	}

	private void Start()
	{
		if ((bool)seekerAudio)
		{
			seekerAudio.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
		}
		if ((bool)lockToneAudio)
		{
			lockToneAudio.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
		}
		visibilityCheckFrame = UnityEngine.Random.Range(0, 8);
	}

	private void OnDisable()
	{
		if ((bool)FloatingOrigin.instance && addedShifter)
		{
			FloatingOrigin.instance.RemoveShiftable(this);
			addedShifter = false;
		}
		seekerLock = 0f;
	}

	public void OnFloatingOriginShift(Vector3 offset)
	{
		lastTargetPosition += offset;
		targetPosition += offset;
	}

	private void LateUpdate()
	{
		if (seekerEnabled)
		{
			if (!addedShifter)
			{
				FloatingOrigin.instance.AddShiftable(this);
				addedShifter = true;
			}
			TrackHeat();
			float num = 0f;
			if ((bool)lockToneAudio && lockToneAudio.enabled)
			{
				if (!lockToneAudio.isPlaying)
				{
					lockToneAudio.Play();
				}
				float num2 = Mathf.Clamp01(seekerLock * 2f - 1f);
				lockToneAudio.volume = Mathf.MoveTowards(lockToneAudio.volume, num2 * num2 * lockAudioVol, 2f * Time.deltaTime);
				num = lockToneAudio.volume;
			}
			if ((bool)seekerAudio && seekerAudio.enabled)
			{
				if (!seekerAudio.isPlaying)
				{
					seekerAudio.Play();
				}
				seekerAudio.volume = Mathf.MoveTowards(seekerAudio.volume, (Mathf.Clamp(seekerLock, 0.5f, 1f) - num) * seekerAudioVol, Time.deltaTime);
				seekerAudio.pitch = Mathf.MoveTowards(seekerAudio.pitch, 1f + seekerLock, 3f * Time.deltaTime);
			}
		}
		else
		{
			if (addedShifter)
			{
				FloatingOrigin.instance.RemoveShiftable(this);
				addedShifter = false;
			}
			seekerLock = 0f;
			localSeekerDirection = Vector3.forward;
			myTransform.localRotation = Quaternion.identity;
			if ((bool)seekerAudio)
			{
				seekerAudio.volume = 0f;
			}
			if ((bool)lockToneAudio)
			{
				lockToneAudio.volume = 0f;
			}
		}
	}

	public void EnableAudio()
	{
		if ((bool)seekerAudio)
		{
			seekerAudio.enabled = true;
		}
		if ((bool)lockToneAudio)
		{
			lockToneAudio.enabled = true;
		}
	}

	public void DisableAudio()
	{
		if ((bool)seekerAudio)
		{
			seekerAudio.enabled = false;
		}
		if ((bool)lockToneAudio)
		{
			lockToneAudio.enabled = false;
		}
	}

	private void RotateSeekerHead(bool trackingTarget)
	{
		if (!trackingTarget)
		{
			seekerLock = 0f;
		}
		if (seekerMode == SeekerModes.Caged)
		{
			localSeekerDirection = localFwd;
		}
		else if (seekerMode == SeekerModes.Uncaged)
		{
			bool flag = (bool)lockingRadar && lockingRadar.IsLocked();
			if (!trackingTarget || manualUncage || flag)
			{
				if (flag)
				{
					Vector3 b = myTransform.parent.InverseTransformDirection(lockingRadar.currentLock.actor.position - myTransform.position);
					localSeekerDirection = Vector3.Slerp(localSeekerDirection, b, seekerTrackRate);
				}
				else
				{
					localSeekerDirection = uncagedScanDir;
					uncagedScanDir = Quaternion.AngleAxis(uncagedScanSpeed * Time.deltaTime, localFwd) * uncagedScanDir;
				}
			}
		}
		else if (seekerMode == SeekerModes.VerticalScan)
		{
			if (!trackingTarget || manualUncage)
			{
				Vector3 axis = Vector3.right;
				if ((bool)vssReferenceTransform)
				{
					axis = myTransform.parent.InverseTransformDirection(vssReferenceTransform.right);
				}
				localSeekerDirection = Quaternion.AngleAxis(0f - verticalScanAngle, axis) * localFwd;
				if (verticalScanAngle > verticalScanSweepAngleLimit)
				{
					verticalScanDir = -1;
				}
				else if (verticalScanAngle < 0f)
				{
					verticalScanDir = 1;
				}
				verticalScanAngle += (float)verticalScanDir * verticalScanSweepSpeed * Time.deltaTime;
			}
		}
		else if (seekerMode == SeekerModes.HardLock)
		{
			if (!trackingTarget)
			{
				Vector3 target = lastTargetPosition - myTransform.position;
				target = Vector3.RotateTowards(myTransform.parent.forward, target, gimbalFOV * ((float)Math.PI / 180f) / 2f, 0f);
				localSeekerDirection = myTransform.parent.InverseTransformDirection(target);
				lastTargetPosition += targetVelocity * Time.deltaTime;
			}
		}
		else if (seekerMode == SeekerModes.HeadTrack && (bool)headTransform)
		{
			localSeekerDirection = myTransform.parent.InverseTransformDirection(headTransform.forward);
		}
		float num = ((seekerMode == SeekerModes.HardLock || seekerMode == SeekerModes.HeadTrack || (seekerMode == SeekerModes.VerticalScan && !trackingTarget)) ? seekerTrackRate : seekerScanTrackRate);
		localSeekerDirection = Vector3.RotateTowards(Vector3.forward, localSeekerDirection, (float)Math.PI / 180f * gimbalFOV / 2f, 0f);
		if (localSeekerDirection == Vector3.zero)
		{
			localSeekerDirection = Vector3.forward;
		}
		Vector3 vector = myTransform.parent.InverseTransformDirection(myTransform.up);
		if (localSeekerDirection == vector)
		{
			vector = Vector3.forward;
		}
		myTransform.localRotation = Quaternion.Slerp(myTransform.localRotation, Quaternion.LookRotation(localSeekerDirection, vector), num * Time.deltaTime);
	}

	public void RemoteSetHardLock(Vector3 targetPosition)
	{
		Vector3 target = targetPosition - myTransform.position;
		target = Vector3.RotateTowards(myTransform.parent.forward, target, gimbalFOV * ((float)Math.PI / 180f) / 2f, 0f);
		localSeekerDirection = myTransform.parent.InverseTransformDirection(target);
		lastTargetPosition = targetPosition;
		SetHardLock();
	}

	private void TrackHeat()
	{
		Vector3 to;
		if (seekerMode == SeekerModes.HardLock)
		{
			myTransform.parent.rotation = Quaternion.LookRotation(myTransform.parent.forward);
			to = lastTargetPosition + targetVelocity * Time.deltaTime - myTransform.position;
		}
		else
		{
			to = myTransform.forward;
		}
		if (HeatEmitter.emitters.Count == 0)
		{
			RotateSeekerHead(trackingTarget: false);
			return;
		}
		float num = 0f;
		likelyTargetActor = null;
		Vector3 zero = Vector3.zero;
		float num2 = 0f;
		Vector3 zero2 = Vector3.zero;
		float num3 = seekerFOV / 2f;
		float num4 = sensitivity * sensitivity;
		bool flag = visibilityCheckFrame == 0;
		visibilityCheckFrame = (visibilityCheckFrame + 1) % 8;
		List<HeatEmitter> list = (flag ? HeatEmitter.emitters : visibleEmitters);
		int num5 = (flag ? list.Count : visEmittersCount);
		if (flag)
		{
			visEmittersCount = 0;
		}
		for (int i = 0; i < num5; i++)
		{
			HeatEmitter heatEmitter = list[i];
			if (!heatEmitter)
			{
				continue;
			}
			Vector3 position = heatEmitter.transform.position;
			Vector3 from = position - myTransform.position;
			float num6 = 0f;
			float num7 = 0f;
			if (!(heatEmitter.heat > 0f) || !((num6 = from.sqrMagnitude) > 10f) || !((num7 = Vector3.Angle(from, to)) < num3) || (flag && Physics.Linecast(myTransform.position, position, 1)))
			{
				continue;
			}
			float num8 = heatEmitter.heat * num4 / num6;
			if (heatEmitter.fwdAspect)
			{
				num8 *= Vector3.Dot(heatEmitter.transform.forward, -from.normalized);
			}
			num8 /= Mathf.Clamp(4f * num7 / num3, 0.25f, 4f);
			if (heatEmitter.isCountermeasure)
			{
				num8 /= ccm;
			}
			if (!(num8 < minThreshold))
			{
				if (heatEmitter.actor != null && num8 > num)
				{
					num = num8;
					likelyTargetActor = heatEmitter.actor;
				}
				zero += num8 * position;
				zero2 += num8 * heatEmitter.velocity;
				num2 += num8;
				if (flag)
				{
					visibleEmitters.AddOrSet(heatEmitter, visEmittersCount);
					visEmittersCount++;
				}
			}
		}
		if (visEmittersCount == 0)
		{
			if (seekerMode != SeekerModes.HardLock)
			{
				lastTargetPosition = myTransform.position + myTransform.parent.forward * 1000f;
			}
			RotateSeekerHead(trackingTarget: false);
			return;
		}
		Vector3 vector = zero / num2;
		Vector3 vector2 = zero2 / num2;
		if (float.IsNaN(vector.x) || float.IsNaN(vector2.x))
		{
			if (seekerMode != SeekerModes.HardLock)
			{
				lastTargetPosition = myTransform.position + myTransform.parent.forward * 1000f;
			}
			RotateSeekerHead(trackingTarget: false);
			return;
		}
		lastTargetPosition = vector;
		targetPosition = vector;
		targetVelocity = vector2;
		Vector3 target = vector - myTransform.position;
		target = Vector3.RotateTowards(myTransform.parent.forward, target, gimbalFOV * ((float)Math.PI / 180f) / 2f, 0f);
		localSeekerDirection = myTransform.parent.InverseTransformDirection(target);
		RotateSeekerHead(trackingTarget: true);
		seekerLock = Mathf.Clamp01(1f - Vector3.Angle(myTransform.forward, target) / seekerFOV);
		lastParentForward = myTransform.parent.forward;
	}

	private void SetupDebugCam()
	{
		if (!debugCam)
		{
			Camera camera = new GameObject().AddComponent<Camera>();
			camera.transform.parent = base.transform;
			camera.transform.localPosition = Vector3.zero;
			camera.transform.localRotation = Quaternion.identity;
			camera.fieldOfView = seekerFOV * 1.5f;
			camera.depth = 10f;
			camera.stereoTargetEye = StereoTargetEyeMask.None;
			camera.nearClipPlane = 1f;
			camera.farClipPlane = 15000f;
			camera.transform.rotation = Quaternion.LookRotation(camera.transform.forward);
			camera.rect = new Rect(0f, 0f, 0.5f, 0.5f);
			debugCam = camera;
		}
	}

	public void OnQuicksavedMissile(ConfigNode qsNode, float elapsedTime)
	{
		ConfigNode configNode = new ConfigNode("HeatSeeker");
		qsNode.AddNode(configNode);
		Vector3D value = VTMapManager.WorldToGlobalPoint(targetPosition);
		Vector3D value2 = VTMapManager.WorldToGlobalPoint(lastTargetPosition);
		configNode.SetValue("targetGPos", value);
		configNode.SetValue("lastTargetGPos", value2);
		configNode.SetValue("seekerLock", seekerLock);
	}

	public void OnQuickloadedMissile(ConfigNode qsNode, float elapsedTime)
	{
		string text = "HeatSeeker";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			Vector3D value = node.GetValue<Vector3D>("targetGPos");
			Vector3D value2 = node.GetValue<Vector3D>("lastTargetGPos");
			targetPosition = VTMapManager.GlobalToWorldPoint(value);
			lastTargetPosition = VTMapManager.GlobalToWorldPoint(value2);
			seekerLock = node.GetValue<float>("seekerLock");
			SetHardLock();
		}
	}
}
