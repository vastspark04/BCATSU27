using System.Collections;
using UnityEngine;

public class VTOLCannon : HPEquippable, IMassObject, IGDSCompatible
{
	public float weaponMass;

	public float ammoUnitMass;

	public float bulletMass;

	public float RPM;

	public float speed;

	public float tracerWidth;

	public float dispersion;

	public Color bulletColor;

	public Transform fireTransform;

	public Transform rotationTransform;

	public Vector3 rotationAxis;

	public float maxRotationSpeed;

	public float spinUpTime;

	public float spinDownTime;

	public Light muzzleFlashLight;

	public ParticleSystem flashEmitter;

	private ParticleSystem.EmissionModule flashEmission;

	public ParticleSystem sparkEmitter;

	private ParticleSystem.EmissionModule sparkEmission;

	public ParticleSystem ejectEmitter;

	private ParticleSystem.EmissionModule ejectEmission;

	public AudioSource windupAudioSource;

	public AnimationCurve windupVolumeCurve;

	public AnimationCurve windupPitchCurve;

	public AudioSource fireAudioSource;

	public AudioClip fireStopSound;

	public Transform ejectTransform;

	public int maxAmmo = 800;

	public int ammo = 800;

	public float damage = 5f;

	public float radius = 1f;

	private float currRotationSpeed;

	private float timeLastFired;

	private float spinupRate;

	private float spindownRate;

	private bool firing;

	private Vector3 impactPoint;

	private Rigidbody rb;

	public bool isSpinningUp { get; private set; }

	public bool isFiring { get; private set; }

	private new void Start()
	{
		rb = GetComponentInParent<Rigidbody>();
		spinupRate = maxRotationSpeed / spinUpTime;
		spindownRate = maxRotationSpeed / spinDownTime;
		flashEmission = flashEmitter.emission;
		sparkEmission = sparkEmitter.emission;
		ejectEmission = ejectEmitter.emission;
		isSpinningUp = false;
		isFiring = false;
	}

	private void Fire()
	{
		currRotationSpeed = Mathf.MoveTowards(currRotationSpeed, (ammo > 0) ? maxRotationSpeed : (maxRotationSpeed * 0.75f), spinupRate * Time.deltaTime);
		isSpinningUp = true;
		if (!windupAudioSource.isPlaying)
		{
			windupAudioSource.Play();
		}
		if (Time.time - timeLastFired > 60f / RPM && currRotationSpeed == maxRotationSpeed && ammo > 0)
		{
			isFiring = true;
			timeLastFired = Time.time;
			Bullet.FireBullet(fireTransform.position + rb.velocity * Time.fixedDeltaTime, fireTransform.forward, speed, tracerWidth, dispersion, 0.15f, damage, rb.velocity, bulletColor, base.weaponManager.actor, radius);
			if (Random.Range(0, 100) < 65)
			{
				muzzleFlashLight.enabled = true;
				flashEmission.enabled = true;
				sparkEmission.enabled = true;
				StartCoroutine(TurnOffFlashAfterFrame());
			}
			if (!ejectEmission.enabled)
			{
				fireAudioSource.Stop();
				fireAudioSource.Play();
				ejectEmission.enabled = true;
			}
			EjectedShell.Eject(ejectTransform.position, ejectTransform.rotation, 7f, 1f, rb.velocity);
			ammo--;
		}
	}

	private void Update()
	{
		if (firing)
		{
			Fire();
		}
		if (isFiring && (Time.time - timeLastFired > 60f / RPM + Time.deltaTime || ammo == 0) && fireAudioSource.isPlaying)
		{
			fireAudioSource.Stop();
			fireAudioSource.PlayOneShot(fireStopSound);
			isFiring = false;
			ejectEmission.enabled = false;
		}
		rotationTransform.localRotation = Quaternion.AngleAxis(currRotationSpeed * Time.deltaTime, rotationAxis) * rotationTransform.localRotation;
		currRotationSpeed = Mathf.MoveTowards(currRotationSpeed, 0f, spindownRate * Time.deltaTime);
		if (currRotationSpeed > 0f && !isFiring)
		{
			isSpinningUp = false;
			if (!windupAudioSource.isPlaying)
			{
				windupAudioSource.Play();
			}
			float time = currRotationSpeed / maxRotationSpeed;
			windupAudioSource.volume = windupVolumeCurve.Evaluate(time);
			windupAudioSource.pitch = windupPitchCurve.Evaluate(time);
		}
		else if (!isFiring && windupAudioSource.isPlaying)
		{
			windupAudioSource.Stop();
		}
		if (base.itemActivated)
		{
			CalculateImpact();
		}
	}

	private IEnumerator TurnOffFlashAfterFrame()
	{
		yield return null;
		muzzleFlashLight.enabled = false;
		flashEmission.enabled = false;
		sparkEmission.enabled = false;
	}

	public override float GetWeaponDamage()
	{
		return damage;
	}

	public override int GetCount()
	{
		return ammo;
	}

	public override int GetMaxCount()
	{
		return maxAmmo;
	}

	public override Vector3 GetAimPoint()
	{
		return impactPoint;
	}

	public override void OnStartFire()
	{
		firing = true;
	}

	public override void OnStopFire()
	{
		firing = false;
	}

	private void CalculateImpact()
	{
		Vector3 vector = rb.velocity + fireTransform.forward * speed;
		float num = 0f;
		Vector3 vector2 = fireTransform.position;
		float num2 = 0.2f;
		while (num < 3800f)
		{
			Vector3 vector3 = vector2 + vector * num2;
			if (Physics.Linecast(vector2, vector3, out var hitInfo, 1))
			{
				impactPoint = hitInfo.point;
				break;
			}
			vector += Physics.gravity * num2;
			vector2 = vector3;
			num += speed * num2;
			impactPoint = vector3;
		}
	}

	public float GetMass()
	{
		return (float)ammo * ammoUnitMass + weaponMass;
	}

	public float GetMuzzleVelocity()
	{
		return speed;
	}

	public Transform GetFireTransform()
	{
		return fireTransform;
	}
}
