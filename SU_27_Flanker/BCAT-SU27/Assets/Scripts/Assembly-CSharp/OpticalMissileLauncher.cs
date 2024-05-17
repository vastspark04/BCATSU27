using System.Collections;
using UnityEngine;

public class OpticalMissileLauncher : MissileLauncher
{
	public AudioSource lockAudioSource;

	public float seekerRate = 8f;

	[Range(0f, 1f)]
	public float boresightFOVFraction = 0.25f;

	private Transform lockTransform;

	private bool lockInRange;

	private bool tracking;

	private Vector3 localAimPoint;

	private bool weaponEnabled;

	private Coroutine updateRoutine;

	public bool targetLocked { get; private set; }

	public Vector3 aimPoint => base.transform.TransformPoint(localAimPoint);

	public OpticalTargeter targeter { get; private set; }

	private Vector3 fwdLocalAimPos => new Vector3(0f, 0f, 8000f);

	public int GetAmmoCount()
	{
		return base.missileCount;
	}

	public Vector3 GetAimPoint()
	{
		return aimPoint;
	}

	protected override void RemoteFire()
	{
		InvokeRemoteFiredEvent(targeter.lockedActor);
	}

	public bool TryFireMissile()
	{
		if (base.missileCount > 0 && lockInRange && (missiles[base.missileIdx].opticalLOAL || targetLocked))
		{
			missiles[base.missileIdx].SetOpticalTarget(lockTransform, targeter.lockedActor, targeter);
			FireMissile();
			lockInRange = false;
			tracking = false;
			localAimPoint = fwdLocalAimPos;
			if ((bool)lockAudioSource)
			{
				lockAudioSource.pitch = 1f;
			}
			return true;
		}
		return false;
	}

	public override void RemoteFireOn(Actor tgt)
	{
		Debug.Log("OpticalMissileLauncher.RemoteFireOn" + (tgt ? tgt.actorName : "null"));
		missiles[base.missileIdx].SetOpticalTarget(lockTransform, tgt, targeter);
		FireMissile();
	}

	public void OnEnableWeapon()
	{
		weaponEnabled = true;
		updateRoutine = StartCoroutine(UpdateRoutine());
	}

	private void OnEnable()
	{
		if (weaponEnabled)
		{
			updateRoutine = StartCoroutine(UpdateRoutine());
		}
	}

	private void OnDisable()
	{
		if ((bool)lockAudioSource)
		{
			lockAudioSource.Stop();
			lockAudioSource.volume = 0f;
			lockAudioSource.pitch = 1f;
		}
	}

	public void SetTargeter(OpticalTargeter t)
	{
		targeter = t;
		if ((bool)targeter)
		{
			lockTransform = targeter.lockTransform;
		}
	}

	public void OnDisableWeapon()
	{
		if (updateRoutine != null)
		{
			StopCoroutine(updateRoutine);
		}
		if ((bool)lockAudioSource)
		{
			lockAudioSource.Stop();
			lockAudioSource.volume = 0f;
			lockAudioSource.pitch = 1f;
		}
		tracking = false;
		lockInRange = false;
		targetLocked = false;
		weaponEnabled = false;
		localAimPoint = fwdLocalAimPos;
	}

	public int GetReticleIndex()
	{
		return 2;
	}

	private IEnumerator UpdateRoutine()
	{
		while (missiles == null || missiles.Length == 0)
		{
			yield return null;
		}
		while (weaponEnabled)
		{
			if ((bool)targeter && targeter.locked && !targeter.lockedSky && (bool)lockTransform && base.missileCount > 0 && lockTransform.gameObject.activeInHierarchy)
			{
				Missile missile = missiles[base.missileIdx];
				if ((bool)missile && Vector3.Angle(lockTransform.position - missile.transform.position, missile.transform.forward) < missile.opticalFOV / 2f * boresightFOVFraction)
				{
					localAimPoint = Vector3.Lerp(localAimPoint, base.transform.InverseTransformPoint(lockTransform.position), seekerRate * Time.deltaTime);
					if (!tracking)
					{
						tracking = true;
						localAimPoint = fwdLocalAimPos;
					}
					lockInRange = true;
					if (!targeter.laserOccluded && Vector3.Angle(aimPoint - base.transform.position, lockTransform.position - base.transform.position) < 1f)
					{
						targetLocked = true;
					}
					else
					{
						targetLocked = false;
					}
				}
				else
				{
					localAimPoint = Vector3.Lerp(localAimPoint, fwdLocalAimPos, seekerRate * Time.deltaTime);
					lockInRange = false;
					tracking = false;
					targetLocked = false;
				}
			}
			else
			{
				localAimPoint = fwdLocalAimPos;
				lockInRange = false;
				tracking = false;
				targetLocked = false;
			}
			if ((bool)lockAudioSource)
			{
				if (tracking)
				{
					if (!lockAudioSource.isPlaying)
					{
						lockAudioSource.Play();
					}
					float pitch = ((!targetLocked) ? 1 : 2);
					lockAudioSource.pitch = pitch;
					lockAudioSource.volume = Mathf.Lerp(lockAudioSource.volume, 1f, 10f * Time.deltaTime);
				}
				else if (lockAudioSource.isPlaying)
				{
					lockAudioSource.Stop();
					lockAudioSource.volume = 0f;
					lockAudioSource.pitch = 1f;
				}
			}
			yield return null;
		}
	}
}
