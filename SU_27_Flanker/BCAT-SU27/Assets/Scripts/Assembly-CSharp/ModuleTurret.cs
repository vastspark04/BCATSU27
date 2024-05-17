using System;
using System.Collections;
using UnityEngine;

public class ModuleTurret : MonoBehaviour
{
	public Transform pitchTransform;

	public Transform yawTransform;

	public Transform referenceTransform;

	public float yawSpeedDPS;

	public float pitchSpeedDPS;

	public float maxPitch;

	public float minPitch;

	public float yawRange;

	public bool smoothRotation;

	public float smoothMultiplier = 10f;

	private bool returningTurret;

	private float pitchTargetOffset;

	private float yawTargetOffset;

	public float maxAudioPitch = 0.5f;

	public float minAudioPitch;

	public float maxVolume = 1f;

	public float minVolume;

	public AudioClip soundClip;

	private AudioSource audioSource;

	private bool hasAudio;

	private bool audioRoutinesStarted;

	private float audioRotationRate;

	private float targetAudioRotationRate;

	private Vector3 lastTurretDirection;

	private float maxAudioRotRate;

	public TurretManager turretManager;

	public int pitchPriority;

	public int yawPriority;

	public float targetingThreshold = 1f;

	private bool inPitchRange;

	private bool inYawRange;

	public bool useDeltaTime;

	public AnimationCurve minPitchCurve;

	public bool useMinPitchCurve;

	public AnimationCurve maxPitchCurve;

	public bool useMaxPitchCurve;

	private bool returnedTurret;

	private Coroutine returnRoutine;

	public float currentYawAngle { get; private set; }

	private float deltaTime
	{
		get
		{
			if (!useDeltaTime)
			{
				return Time.fixedDeltaTime;
			}
			return Time.deltaTime;
		}
	}

	public event Action<Vector3> OnAimToTarget;

	public event Action OnReturningTurret;

	private void Start()
	{
		if (!yawTransform || !pitchTransform)
		{
			base.enabled = false;
			Debug.LogError("ModuleTurret is missing pitch or yaw transform");
			return;
		}
		if (!referenceTransform)
		{
			referenceTransform = pitchTransform;
		}
		if ((bool)soundClip && (yawSpeedDPS != 0f || pitchSpeedDPS != 0f))
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
			audioSource.clip = soundClip;
			audioSource.loop = true;
			audioSource.dopplerLevel = 0f;
			audioSource.minDistance = 0.5f;
			audioSource.maxDistance = 150f;
			audioSource.Play();
			audioSource.volume = 0f;
			audioSource.pitch = 0f;
			audioSource.priority = 9999;
			audioSource.spatialBlend = 1f;
			lastTurretDirection = yawTransform.parent.InverseTransformDirection(pitchTransform.forward);
			maxAudioRotRate = Mathf.Min(yawSpeedDPS, pitchSpeedDPS);
			hasAudio = true;
		}
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)yawTransform && (bool)pitchTransform)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(yawTransform.position, yawTransform.position + 10f * (Quaternion.AngleAxis(yawRange / 2f, yawTransform.parent.up) * yawTransform.parent.forward));
			Gizmos.DrawLine(yawTransform.position, yawTransform.position + 10f * (Quaternion.AngleAxis(yawRange / 2f, -yawTransform.parent.up) * yawTransform.parent.forward));
			Gizmos.color = Color.blue;
			if (useMaxPitchCurve)
			{
				DrawPitchCurve(maxPitchCurve);
			}
			else
			{
				Gizmos.DrawLine(pitchTransform.position, pitchTransform.position + 10f * (Quaternion.AngleAxis(maxPitch, -pitchTransform.parent.right) * pitchTransform.parent.forward));
			}
			Gizmos.color = Color.yellow;
			if (useMinPitchCurve)
			{
				DrawPitchCurve(minPitchCurve);
			}
			else
			{
				Gizmos.DrawLine(pitchTransform.position, pitchTransform.position + 10f * (Quaternion.AngleAxis(minPitch, -pitchTransform.parent.right) * pitchTransform.parent.forward));
			}
			Gizmos.color = Color.red;
			Gizmos.DrawLine(pitchTransform.position, pitchTransform.position + 30f * pitchTransform.forward);
		}
	}

	private void DrawPitchCurve(AnimationCurve curve)
	{
		Vector3 from = Vector3.zero;
		for (int i = 0; i <= 360; i += 2)
		{
			float num = i;
			Vector3 vector = Quaternion.AngleAxis(num, Vector3.up) * Vector3.forward;
			Vector3 position = Quaternion.AngleAxis(curve.Evaluate(num), -Vector3.Cross(Vector3.up, vector)) * vector;
			position = pitchTransform.TransformPoint(position);
			if (i > 0)
			{
				Gizmos.DrawLine(from, position);
				if (i % 12 == 0)
				{
					Gizmos.DrawLine(position, pitchTransform.position);
				}
			}
			from = position;
		}
	}

	private IEnumerator AudioFixedUpdateRoutine()
	{
		while (base.enabled)
		{
			audioRotationRate = Mathf.Lerp(audioRotationRate, targetAudioRotationRate, 20f * Time.fixedDeltaTime);
			audioRotationRate = Mathf.Clamp01(audioRotationRate);
			if (audioRotationRate < 0.05f)
			{
				audioSource.volume = 0f;
			}
			else
			{
				audioSource.volume = Mathf.Clamp(2f * audioRotationRate, minVolume, maxVolume);
				audioSource.pitch = Mathf.Clamp(audioRotationRate, minAudioPitch, maxAudioPitch);
			}
			Vector3 from = yawTransform.parent.InverseTransformDirection(pitchTransform.forward);
			float num = Mathf.Clamp01(Vector3.Angle(from, lastTurretDirection) / Time.fixedDeltaTime / maxAudioRotRate);
			lastTurretDirection = from;
			targetAudioRotationRate = num;
			yield return new WaitForFixedUpdate();
		}
	}

	private IEnumerator AudioUpdateRoutine()
	{
		while (base.enabled)
		{
			if (audioRotationRate > 0.05f)
			{
				if (!audioSource.isPlaying)
				{
					audioSource.Play();
				}
			}
			else if (audioSource.isPlaying)
			{
				audioSource.Stop();
			}
			yield return null;
		}
	}

	private void OnDisable()
	{
		audioRoutinesStarted = false;
	}

	public void AimToTargetImmediate(Vector3 targetPosition)
	{
		float num = pitchSpeedDPS;
		float num2 = yawSpeedDPS;
		pitchSpeedDPS = float.MaxValue;
		yawSpeedDPS = float.MaxValue;
		AimToTarget(targetPosition);
		pitchSpeedDPS = num;
		yawSpeedDPS = num2;
		returnedTurret = false;
	}

	public void AimToTarget(Vector3 targetPosition, bool pitch = true, bool yaw = true, bool useManager = true)
	{
		if (returningTurret)
		{
			if (returnRoutine != null)
			{
				StopCoroutine(returnRoutine);
			}
			returningTurret = false;
		}
		returnedTurret = false;
		if (useManager && (bool)turretManager)
		{
			turretManager.AimToTarget(this, targetPosition, pitchPriority, yawPriority);
		}
		else if ((bool)yawTransform)
		{
			if (!referenceTransform)
			{
				referenceTransform = pitchTransform;
			}
			pitchTargetOffset = pitchTransform.InverseTransformPoint(referenceTransform.position).y;
			yawTargetOffset = yawTransform.InverseTransformPoint(referenceTransform.position).x;
			Vector3 vector = Vector3.ProjectOnPlane(yawTransform.parent.InverseTransformPoint(targetPosition - yawTargetOffset * pitchTransform.right), Vector3.up);
			float value = VectorUtils.SignedAngle(Vector3.forward, vector, Vector3.right);
			value = Mathf.Clamp(value, (0f - yawRange) / 2f, yawRange / 2f);
			Quaternion localRotation = yawTransform.localRotation;
			yawTransform.localRotation = Quaternion.Euler(0f, value, 0f);
			Vector3 vector2 = pitchTransform.parent.InverseTransformPoint(targetPosition - pitchTargetOffset * pitchTransform.up);
			yawTransform.localRotation = localRotation;
			vector2.z = Mathf.Abs(vector2.z);
			Vector3 vector3 = Vector3.ProjectOnPlane(vector2, Vector3.right);
			float value2 = VectorUtils.SignedAngle(Vector3.forward, vector3, Vector3.up);
			value2 = Mathf.Clamp(value2, MinPitch(currentYawAngle), MaxPitch(currentYawAngle));
			float num = Vector3.Angle(yawTransform.parent.InverseTransformDirection(yawTransform.forward), vector);
			inYawRange = !yaw || num < targetingThreshold;
			float num2 = Mathf.Sign(Vector3.Dot(yawTransform.localRotation * Vector3.forward, Vector3.right));
			float num3 = Vector3.Angle(pitchTransform.parent.InverseTransformDirection(pitchTransform.forward), vector3);
			inPitchRange = !pitch || num3 < targetingThreshold;
			float num4 = ((num > 0f) ? Mathf.Clamp01(num3 / num * (yawSpeedDPS / pitchSpeedDPS)) : 1f);
			float num5 = ((num3 > 0f) ? Mathf.Clamp01(num / num3 * (pitchSpeedDPS / yawSpeedDPS)) : 1f);
			float num6;
			float num7;
			if (smoothRotation)
			{
				num6 = Mathf.Clamp(num * smoothMultiplier, 1f, yawSpeedDPS) * deltaTime;
				num7 = Mathf.Clamp(num3 * smoothMultiplier, 1f, pitchSpeedDPS) * deltaTime;
			}
			else
			{
				num6 = yawSpeedDPS * deltaTime;
				num7 = pitchSpeedDPS * deltaTime;
			}
			num6 *= num5;
			num7 *= num4;
			if (yawRange < 360f && Mathf.Abs(value) > 90f && num2 != Mathf.Sign(value))
			{
				value = 5f * Mathf.Sign(value);
			}
			if (yaw)
			{
				yawTransform.localRotation = Quaternion.RotateTowards(yawTransform.localRotation, Quaternion.Euler(0f, value, 0f), num6);
				currentYawAngle = VectorUtils.SignedAngle(yawTransform.parent.forward, yawTransform.forward, yawTransform.parent.right);
			}
			if (pitch)
			{
				pitchTransform.localRotation = Quaternion.RotateTowards(pitchTransform.localRotation, Quaternion.Euler(0f - value2, 0f, 0f), num7);
			}
			if (hasAudio && !audioRoutinesStarted)
			{
				audioRoutinesStarted = true;
				StartCoroutine(AudioFixedUpdateRoutine());
				StartCoroutine(AudioUpdateRoutine());
			}
			this.OnAimToTarget?.Invoke(targetPosition);
		}
	}

	public bool ReturnTurret()
	{
		if (!yawTransform)
		{
			return false;
		}
		if (returnedTurret)
		{
			return true;
		}
		float num = Vector3.Angle(yawTransform.forward, yawTransform.parent.forward);
		float num2 = Vector3.Angle(pitchTransform.forward, yawTransform.forward);
		float num3;
		float num4;
		if (smoothRotation)
		{
			num3 = Mathf.Clamp(num * smoothMultiplier, 1f, yawSpeedDPS) * deltaTime;
			num4 = Mathf.Clamp(num2 * smoothMultiplier, 1f, pitchSpeedDPS) * deltaTime;
		}
		else
		{
			num3 = yawSpeedDPS * deltaTime;
			num4 = pitchSpeedDPS * deltaTime;
		}
		float num5 = ((num > 0f) ? Mathf.Clamp01(num2 / num * (yawSpeedDPS / pitchSpeedDPS)) : 1f);
		float num6 = ((num2 > 0f) ? Mathf.Clamp01(num / num2 * (pitchSpeedDPS / yawSpeedDPS)) : 1f);
		num3 *= num6;
		num4 *= num5;
		yawTransform.localRotation = Quaternion.RotateTowards(yawTransform.localRotation, Quaternion.identity, num3);
		pitchTransform.localRotation = Quaternion.RotateTowards(pitchTransform.localRotation, Quaternion.identity, num4);
		this.OnReturningTurret?.Invoke();
		if (yawTransform.localRotation == Quaternion.identity && pitchTransform.localRotation == Quaternion.identity)
		{
			returnedTurret = true;
			return true;
		}
		return false;
	}

	public void ReturnTurretOneshot()
	{
		if (!returningTurret)
		{
			returnedTurret = false;
			returnRoutine = StartCoroutine(ReturnRoutine());
			returningTurret = true;
		}
	}

	private IEnumerator ReturnRoutine()
	{
		while (!ReturnTurret())
		{
			if (useDeltaTime)
			{
				yield return null;
			}
			else
			{
				yield return new WaitForFixedUpdate();
			}
		}
		returningTurret = false;
	}

	public bool TargetInRange(Vector3 targetPosition, float maxDistance)
	{
		if (!pitchTransform)
		{
			return false;
		}
		bool num = inYawRange && inPitchRange;
		bool flag = (targetPosition - pitchTransform.position).sqrMagnitude < maxDistance * maxDistance;
		return num && flag;
	}

	public bool TargetInGimbalRange(Vector3 targetPosition)
	{
		Vector3 vector = yawTransform.parent.InverseTransformPoint(targetPosition);
		vector.y = 0f;
		float num = VectorUtils.SignedAngle(Vector3.forward, vector, Vector3.right);
		if (Mathf.Abs(num) > yawRange / 2f)
		{
			return false;
		}
		Quaternion localRotation = yawTransform.localRotation;
		yawTransform.localRotation = Quaternion.LookRotation(vector);
		bool result = true;
		Vector3 toDirection = yawTransform.InverseTransformPoint(targetPosition);
		toDirection.x = 0f;
		float num2 = VectorUtils.SignedAngle(Vector3.forward, toDirection, Vector3.up);
		if (num2 > MaxPitch(num) || num2 < MinPitch(num))
		{
			result = false;
		}
		yawTransform.localRotation = localRotation;
		return result;
	}

	public void SetReferenceTransform(Transform t)
	{
		referenceTransform = t;
		pitchTargetOffset = pitchTransform.InverseTransformPoint(referenceTransform.position).y;
		yawTargetOffset = yawTransform.InverseTransformPoint(referenceTransform.position).x;
	}

	private float MinPitch(float yawAngle)
	{
		if (useMinPitchCurve)
		{
			return minPitchCurve.Evaluate(Mathf.Repeat(yawAngle, 360f));
		}
		return minPitch;
	}

	private float MaxPitch(float yawAngle)
	{
		if (useMaxPitchCurve)
		{
			return maxPitchCurve.Evaluate(Mathf.Repeat(yawAngle, 360f));
		}
		return maxPitch;
	}
}
