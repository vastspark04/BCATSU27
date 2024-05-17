using System;
using UnityEngine;

public class EngineEffects : MonoBehaviour
{
	[Serializable]
	public class EngineParticleFX
	{
		public ParticleSystem particleSystem;

		public AnimationCurve emissionCurve;

		public AnimationCurve speedCurve;

		public AnimationCurve sizeCurve;

		public Gradient colorGradient;

		public bool useColorGradient;

		public bool afterburnerOnly;

		private ParticleSystem.EmissionModule psEmission;

		private ParticleSystem.MainModule psMain;

		public void Init()
		{
			psEmission = particleSystem.emission;
			psMain = particleSystem.main;
		}

		public void Evaluate(float throttle)
		{
			psEmission.rateOverTime = new ParticleSystem.MinMaxCurve(emissionCurve.Evaluate(throttle));
			psMain.startSpeed = speedCurve.Evaluate(throttle);
			psMain.startSize = sizeCurve.Evaluate(throttle);
			if (useColorGradient)
			{
				psMain.startColor = colorGradient.Evaluate(throttle);
			}
		}
	}

	[Serializable]
	public class EngineAudioFX
	{
		public bool afterburnerOnly;

		public AudioSource audioSource;

		public AnimationCurve volumeCurve;

		public AnimationCurve pitchCurve;
	}

	public EngineParticleFX[] particleEffects;

	public Transform tiltRotationTransform;

	public EngineAudioFX[] audioEffects;

	public AudioSource tiltAudioSource;

	public float controlAngle;

	public float controlRotationSpeed;

	public float interiorVolumeMult = 0.25f;

	private float throttle;

	private float lastThrottle = -1f;

	private float tiltSoundLevel;

	private bool tilting;

	private float tilt;

	private float prevTilt;

	private Quaternion tiltRotation;

	private Quaternion controlRotation;

	private Quaternion controlRotationTarget;

	private float abMult;

	private bool forceLowLOD;

	public Health health;

	private LODBase lodBase;

	private int lodFrameIdx;

	private int lodFrameInterval = 10;

	private bool partDied;

	public bool onlyUpdateOnThrottleDelta = true;

	private bool overrideDeltaUpdate;

	public float currentTilt => tilt;

	private void Awake()
	{
		controlRotation = Quaternion.identity;
		controlRotationTarget = Quaternion.identity;
		tiltRotation = Quaternion.identity;
		lodFrameIdx = UnityEngine.Random.Range(0, lodFrameInterval);
		if ((bool)GetComponentInParent<AIPilot>())
		{
			forceLowLOD = true;
		}
		if ((bool)health)
		{
			health.OnDeath.AddListener(OnDeath);
		}
		VehiclePart componentInParent = GetComponentInParent<VehiclePart>();
		if ((bool)componentInParent)
		{
			componentInParent.OnRepair.AddListener(OnRepair);
		}
	}

	private void OnDeath()
	{
		partDied = true;
	}

	private void OnRepair()
	{
		partDied = false;
	}

	private void Start()
	{
		for (int i = 0; i < audioEffects.Length; i++)
		{
			audioEffects[i].audioSource.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
		}
		for (int j = 0; j < particleEffects.Length; j++)
		{
			particleEffects[j].Init();
		}
		lodBase = GetComponentInParent<LODBase>();
	}

	public void SetThrottle(float t)
	{
		throttle = t;
	}

	public void SetOverrideDeltaUpdate(bool s)
	{
		overrideDeltaUpdate = s;
	}

	public void SetControl(float yaw, float roll)
	{
		float num = (90f - tilt) / 90f * yaw * controlAngle;
		float num2 = tilt / 90f * roll * controlAngle;
		controlRotationTarget = Quaternion.RotateTowards(controlRotationTarget, Quaternion.Euler(num + num2, 0f, 0f), controlRotationSpeed * Time.deltaTime);
	}

	public void SetAfterburner(float abMult)
	{
		this.abMult = abMult;
	}

	private void Update()
	{
		bool flag = false;
		bool flag2 = true;
		if ((bool)lodBase || forceLowLOD)
		{
			flag = forceLowLOD || lodBase.sqrDist > 1000000f;
			flag2 = lodFrameIdx == 0;
			lodFrameIdx = (lodFrameIdx + 1) % lodFrameInterval;
		}
		float num = Mathf.Lerp(interiorVolumeMult, 1f, AudioController.instance.exteriorLevel);
		if ((!onlyUpdateOnThrottleDelta || overrideDeltaUpdate || Mathf.Abs(throttle - lastThrottle) > 0.001f) && (!flag || flag2))
		{
			lastThrottle = throttle;
			for (int i = 0; i < audioEffects.Length; i++)
			{
				EngineAudioFX engineAudioFX = audioEffects[i];
				if ((bool)engineAudioFX.audioSource)
				{
					if (!engineAudioFX.audioSource.isPlaying)
					{
						engineAudioFX.audioSource.Play();
					}
					float num2 = (engineAudioFX.afterburnerOnly ? abMult : 1f);
					engineAudioFX.audioSource.volume = num2 * num * engineAudioFX.volumeCurve.Evaluate(throttle);
					engineAudioFX.audioSource.pitch = num2 * engineAudioFX.pitchCurve.Evaluate(throttle);
				}
			}
			for (int j = 0; j < particleEffects.Length; j++)
			{
				float num3 = (particleEffects[j].afterburnerOnly ? abMult : throttle);
				particleEffects[j].Evaluate(num3);
			}
			if ((bool)tiltAudioSource)
			{
				if (tilting)
				{
					tilting = false;
				}
				else
				{
					tiltSoundLevel = Mathf.MoveTowards(tiltSoundLevel, 0f, 4f * Time.deltaTime);
				}
				if (tiltSoundLevel > 0f)
				{
					if (!tiltAudioSource.isPlaying)
					{
						tiltAudioSource.Play();
					}
					tiltAudioSource.volume = tiltSoundLevel * 2f;
					tiltAudioSource.pitch = tiltSoundLevel + 0.5f;
				}
				else if (tiltAudioSource.isPlaying)
				{
					tiltAudioSource.Stop();
				}
			}
		}
		if ((bool)tiltRotationTransform)
		{
			if (flag)
			{
				controlRotation = controlRotationTarget;
				tiltRotationTransform.localRotation = controlRotation * tiltRotation;
			}
			else
			{
				controlRotation = Quaternion.Slerp(controlRotation, controlRotationTarget, 5f * Time.deltaTime);
				tiltRotationTransform.localRotation = controlRotation * tiltRotation;
			}
		}
	}

	public void SetTilt(float angle)
	{
		if (!partDied)
		{
			prevTilt = tilt;
			tilt = angle;
			if (Mathf.Abs(tilt - prevTilt) > 0f)
			{
				tilting = true;
				tiltSoundLevel = Mathf.MoveTowards(tiltSoundLevel, 1f, 4f * Time.deltaTime);
			}
			tiltRotation = Quaternion.Euler(angle, 0f, 0f);
		}
	}
}
