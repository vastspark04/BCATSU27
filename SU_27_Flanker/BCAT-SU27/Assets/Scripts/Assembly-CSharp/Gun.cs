using System;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour, IMassObject, IParentRBDependent
{
	[Serializable]
	public class GunAudioProfile
	{
		public AudioSource audioSource;

		public AudioClip firingSound;

		public AudioClip stopFiringSound;
	}

	public float gunMass;

	public float rpm;

	public Bullet.BulletInfo bulletInfo;

	public Transform[] fireTransforms;

	public int maxAmmo;

	public int currentAmmo;

	private int fireTransformIdx;

	private ParticleSystem[][] firePsystems;

	public AudioSource fireAudioSource;

	public AudioClip fireAudioClip;

	public bool loopingAudio;

	public AudioClip fireStopClip;

	public GunAudioProfile[] audioProfiles;

	[Range(0f, 1f)]
	public float muzzleFlashChance = 0.65f;

	private bool firing;

	private float lastFireTime;

	private float fireInterval;

	private bool playedEndClip = true;

	[HideInInspector]
	public Actor actor;

	public BoolEvent OnSetFire;

	[Space]
	public Transform ejectTransform;

	private bool hasEjectTf;

	private ParticleSystem[] ejectEmitters;

	public GunBarrelRotator barrelRotator;

	private bool hasRotator;

	[Tooltip("If greater than zero, multiplies recoil calculated by velocity and projectile mass.")]
	public float recoilFactor = -1f;

	private Rigidbody parentRb;

	private bool hasParentRb;

	[Header("Networking")]
	public bool isLocal = true;

	private float interpRofOverlap;

	private Quaternion lastFireDirection;

	public bool isFiring => firing;

	public event Action OnFired;

	public event Bullet.BulletFiredDelegate OnFiredBullet;

	public void SetParentRigidbody(Rigidbody r)
	{
		parentRb = r;
		if ((bool)parentRb)
		{
			hasParentRb = true;
		}
		else
		{
			hasParentRb = false;
		}
	}

	private void Awake()
	{
		actor = GetComponentInParent<Actor>();
		firePsystems = new ParticleSystem[fireTransforms.Length][];
		for (int i = 0; i < fireTransforms.Length; i++)
		{
			firePsystems[i] = fireTransforms[i].GetComponentsInChildren<ParticleSystem>();
		}
		fireInterval = 60f / rpm;
		currentAmmo = Mathf.Min(currentAmmo, maxAmmo);
		if (audioProfiles == null || audioProfiles.Length == 0)
		{
			GunAudioProfile gunAudioProfile = new GunAudioProfile();
			gunAudioProfile.audioSource = fireAudioSource;
			gunAudioProfile.firingSound = fireAudioClip;
			gunAudioProfile.stopFiringSound = fireStopClip;
			audioProfiles = new GunAudioProfile[1] { gunAudioProfile };
		}
		if (loopingAudio)
		{
			for (int j = 0; j < audioProfiles.Length; j++)
			{
				audioProfiles[j].audioSource.clip = audioProfiles[j].firingSound;
				audioProfiles[j].audioSource.loop = true;
			}
		}
		if ((bool)ejectTransform)
		{
			hasEjectTf = true;
			ejectEmitters = ejectTransform.GetComponentsInChildren<ParticleSystem>();
		}
		if ((bool)barrelRotator)
		{
			hasRotator = true;
		}
	}

	private IEnumerator FiringRoutine()
	{
		while (firing)
		{
			if (currentAmmo > 0)
			{
				if (Time.time - lastFireTime >= fireInterval)
				{
					if (rpm > 4000f)
					{
						float num;
						for (num = interpRofOverlap; num < Time.deltaTime; num += fireInterval)
						{
							FireBullet(num);
						}
						lastFireTime = Time.time;
						interpRofOverlap = num - Time.deltaTime;
					}
					else
					{
						FireBullet();
						lastFireTime = Time.time;
					}
				}
			}
			else
			{
				SetFire(fire: false);
			}
			yield return null;
		}
		if (loopingAudio && !playedEndClip)
		{
			playedEndClip = true;
			for (int i = 0; i < audioProfiles.Length; i++)
			{
				audioProfiles[i].audioSource.Stop();
				audioProfiles[i].audioSource.PlayOneShot(audioProfiles[i].stopFiringSound);
			}
		}
	}

	private void UpdateEjectEmitters()
	{
		if (hasEjectTf)
		{
			if (firing && currentAmmo <= 0)
			{
				ejectEmitters.SetEmission(emit: false);
			}
			else
			{
				ejectEmitters.SetEmission(firing);
			}
		}
	}

	private void FireBullet(float timeDelay = 0f)
	{
		if (currentAmmo <= 0 || (hasRotator && barrelRotator.currSpeed < barrelRotator.minFiringSpeed))
		{
			return;
		}
		Transform transform = fireTransforms[fireTransformIdx];
		Vector3 vector = transform.forward;
		if (rpm > 4000f)
		{
			float num = ((Time.deltaTime == 0f) ? Time.fixedDeltaTime : Time.deltaTime);
			vector = Quaternion.Lerp(lastFireDirection, transform.rotation, timeDelay / num) * Vector3.forward;
			lastFireDirection = transform.rotation;
		}
		if (isLocal)
		{
			Bullet.FireBullet(transform.position + bulletInfo.speed * timeDelay * vector, vector, bulletInfo, actor.velocity, actor, this.OnFiredBullet);
			currentAmmo--;
		}
		StartCoroutine(MuzzleFlashRoutine());
		if (!loopingAudio)
		{
			for (int i = 0; i < audioProfiles.Length; i++)
			{
				audioProfiles[i].audioSource.Stop();
				audioProfiles[i].audioSource.PlayOneShot(audioProfiles[i].firingSound);
			}
		}
		else if (playedEndClip)
		{
			playedEndClip = false;
			for (int j = 0; j < audioProfiles.Length; j++)
			{
				audioProfiles[j].audioSource.Stop();
				audioProfiles[j].audioSource.Play();
			}
		}
		if (hasEjectTf)
		{
			EjectedShell.Eject(ejectTransform.position, ejectTransform.rotation, 7f, 1f, actor.velocity);
		}
		if (hasParentRb && recoilFactor > 0f)
		{
			float num2 = bulletInfo.projectileMass * bulletInfo.speed * recoilFactor;
			parentRb.AddForceAtPosition(num2 * -transform.forward, transform.position, ForceMode.Impulse);
		}
		if (this.OnFired != null)
		{
			this.OnFired();
		}
		fireTransformIdx = (fireTransformIdx + 1) % fireTransforms.Length;
	}

	public void SetFire(bool fire)
	{
		if (firing != fire)
		{
			firing = fire;
			if (OnSetFire != null)
			{
				OnSetFire.Invoke(fire);
			}
			if (fire)
			{
				interpRofOverlap = 0f;
				lastFireDirection = fireTransforms[0].rotation;
				StartCoroutine(FiringRoutine());
			}
			UpdateEjectEmitters();
		}
	}

	private void OnDisable()
	{
		SetFire(fire: false);
		if (loopingAudio)
		{
			for (int i = 0; i < audioProfiles.Length; i++)
			{
				audioProfiles[i].audioSource.Stop();
			}
			playedEndClip = true;
		}
	}

	public Vector3 GetCalculatedTargetPosition(Actor target, bool calcAccel = false)
	{
		Vector3 targetAccel = ((calcAccel && (bool)target.flightInfo) ? target.flightInfo.acceleration : Vector3.zero);
		return GetCalculatedTargetPosition(target.position, target.velocity, targetAccel);
	}

	public Vector3 GetCalculatedTargetPosition(Vector3 targetPos, Vector3 targetVel)
	{
		return GetCalculatedTargetPosition(targetPos, targetVel, Vector3.zero);
	}

	public Vector3 GetCalculatedTargetPosition(Vector3 targetPos, Vector3 targetVel, Vector3 targetAccel)
	{
		Vector3 vector = targetPos + targetVel * Time.deltaTime - fireTransforms[0].position;
		Vector3 vector2 = targetVel - actor.velocity;
		float num = VectorUtils.CalculateLeadTime(vector, vector2, bulletInfo.speed);
		Vector3 vector3 = fireTransforms[0].position + vector + vector2 * num;
		float num2 = 0.5f * num * num;
		return vector3 - num2 * Physics.gravity + num2 * targetAccel;
	}

	public bool GetBallisticDirection(Vector3 targetPosition, Vector3 targetVelocity, out Vector3 direction, bool direct = true)
	{
		targetPosition += targetVelocity * Time.deltaTime;
		return VectorUtils.BallisticDirection(targetPosition, fireTransforms[0].position, bulletInfo.speed, direct, out direction);
	}

	private IEnumerator MuzzleFlashRoutine()
	{
		if (!(muzzleFlashChance < 1f) || !((float)UnityEngine.Random.Range(0, 100) < muzzleFlashChance * 100f))
		{
			ParticleSystem[] ps = firePsystems[fireTransformIdx];
			ps.SetEmission(emit: true);
			yield return null;
			yield return null;
			ps.SetEmission(emit: false);
		}
	}

	public float GetMass()
	{
		return gunMass + (float)currentAmmo * bulletInfo.totalMass;
	}
}
