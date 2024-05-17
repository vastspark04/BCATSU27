using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class Rocket : MonoBehaviour
{
	public static List<Rocket> allFiredRockets = new List<Rocket>();

	public float damage;

	public float radius;

	public ExplosionManager.ExplosionTypes explosionType;

	public float thrust;

	public float thrustDecayFactor;

	public float mass;

	public float thrustTime;

	public float initialKickVel;

	public float maxLifeTime = 15f;

	public Transform exhaustTransform;

	public Light exhaustLight;

	private ParticleSystem[] exhaustEmitters;

	public float inaccuracy;

	public AudioSource audioSource;

	private Vector3 velocity;

	public Rigidbody launcherRB;

	private bool fired;

	private Vector3 prevPos;

	private float firedTime;

	private Vector3 explosionNormal = Vector3.up;

	private Actor sourceActor;

	public PlayerInfo sourcePlayer;

	private Transform myTransform;

	private Collider directHitCol;

	private bool detonated;

	public event UnityAction<Rocket> OnDetonated;

	public static void DestroyAllFiredRockets()
	{
		foreach (Rocket allFiredRocket in allFiredRockets)
		{
			if ((bool)allFiredRocket && (bool)allFiredRocket.gameObject)
			{
				Object.Destroy(allFiredRocket.gameObject);
			}
		}
	}

	public float GetTimeFired()
	{
		return firedTime;
	}

	public Vector3 GetVelocity()
	{
		return velocity;
	}

	private void Awake()
	{
		myTransform = base.transform;
		exhaustEmitters = exhaustTransform.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < exhaustEmitters.Length; i++)
		{
			if (exhaustEmitters[i].main.simulationSpace == ParticleSystemSimulationSpace.World && !exhaustEmitters[i].GetComponent<FOParticleShifter>())
			{
				exhaustEmitters[i].gameObject.AddComponent<FOParticleShifter>();
			}
			exhaustEmitters[i].gameObject.SetActive(value: false);
		}
		if ((bool)exhaustLight)
		{
			exhaustLight.enabled = false;
		}
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		prevPos += offset;
		myTransform.position += offset;
	}

	public void Fire(Actor sourceActor)
	{
		if (!fired)
		{
			this.sourceActor = sourceActor;
			fired = true;
			FloatingOrigin.instance.AddQueuedFixedUpdateAction(FixedUpdateFire);
			allFiredRockets.Add(this);
		}
	}

	public void ResumeFire(Actor sourceActor, Vector3 startVel, float elapsedTime)
	{
		if (!fired)
		{
			this.sourceActor = sourceActor;
			fired = true;
			allFiredRockets.Add(this);
			FloatingOrigin.instance.AddQueuedFixedUpdateAction(FixedUpdateResume);
			firedTime = Time.time - elapsedTime;
			velocity = startVel;
		}
	}

	private void FixedUpdateFire()
	{
		fired = true;
		prevPos = myTransform.position;
		myTransform.parent = null;
		FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
		if ((bool)launcherRB)
		{
			velocity = launcherRB.GetPointVelocity(myTransform.position);
		}
		velocity += initialKickVel * myTransform.forward;
		velocity += inaccuracy * Random.insideUnitSphere;
		StartCoroutine(ThrustRoutine(Time.time));
		firedTime = Time.time;
		StartCoroutine(PhysicsRoutine());
		StartCoroutine(InterpolationRoutine());
	}

	private void FixedUpdateResume()
	{
		prevPos = myTransform.position;
		myTransform.parent = null;
		FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
		StartCoroutine(ThrustRoutine(firedTime));
		StartCoroutine(PhysicsRoutine());
		StartCoroutine(InterpolationRoutine());
	}

	private IEnumerator PhysicsRoutine()
	{
		while (fired)
		{
			float num = Time.time - firedTime;
			int num2 = 1;
			if (num > 0.25f)
			{
				num2 |= 0x400;
			}
			if (Physics.Linecast(prevPos, myTransform.position + velocity * Time.fixedDeltaTime, out var hitInfo, num2))
			{
				myTransform.position = hitInfo.point - velocity.normalized * 0.1f;
				explosionNormal = hitInfo.normal;
				directHitCol = hitInfo.collider;
				Detonate();
			}
			else if (num > maxLifeTime)
			{
				Detonate();
			}
			else if ((bool)WaterPhysics.instance && myTransform.position.y < WaterPhysics.instance.height)
			{
				BulletHitManager.instance.CreateSplash(myTransform.position, velocity);
				Detonate();
			}
			if (velocity.sqrMagnitude > 0f)
			{
				Quaternion rotation = Quaternion.RotateTowards(myTransform.rotation, Quaternion.LookRotation(velocity, myTransform.up), GetRotationDelta(Time.time - firedTime, myTransform.position, velocity, Time.fixedDeltaTime));
				if (!float.IsNaN(rotation.x))
				{
					myTransform.rotation = rotation;
				}
			}
			velocity += Physics.gravity * Time.fixedDeltaTime;
			prevPos = myTransform.position;
			yield return new WaitForFixedUpdate();
			myTransform.position = prevPos + velocity * Time.fixedDeltaTime;
		}
	}

	private IEnumerator InterpolationRoutine()
	{
		while (fired)
		{
			myTransform.position += velocity * Time.deltaTime;
			yield return null;
		}
	}

	private IEnumerator ThrustRoutine(float startTime)
	{
		if ((bool)audioSource)
		{
			audioSource.Play();
		}
		for (int i = 0; i < exhaustEmitters.Length; i++)
		{
			exhaustEmitters[i].gameObject.SetActive(value: true);
			exhaustEmitters[i].SetEmission(emit: true);
		}
		if ((bool)exhaustLight)
		{
			exhaustLight.enabled = true;
		}
		for (float num = startTime; num < Time.time; num += 0.1f)
		{
			thrust -= thrustDecayFactor * thrust * 0.1f;
		}
		while (Time.time - startTime < thrustTime)
		{
			velocity += Time.fixedDeltaTime * thrust / mass * myTransform.forward;
			thrust -= thrustDecayFactor * thrust * Time.fixedDeltaTime;
			yield return new WaitForFixedUpdate();
		}
		exhaustEmitters.SetEmission(emit: false);
		if ((bool)exhaustLight)
		{
			exhaustLight.enabled = false;
		}
		if ((bool)audioSource)
		{
			audioSource.Stop();
		}
	}

	public void Detonate()
	{
		if (!detonated)
		{
			detonated = true;
			if (this.OnDetonated != null)
			{
				this.OnDetonated(this);
			}
			exhaustTransform.parent = null;
			exhaustTransform.gameObject.AddComponent<FloatingOriginTransform>().shiftParticles = false;
			float longestLife = exhaustEmitters.GetLongestLife();
			exhaustEmitters.SetEmission(emit: false);
			if ((bool)audioSource)
			{
				audioSource.Stop();
			}
			Object.Destroy(exhaustTransform.gameObject, longestLife);
			Object.Destroy(base.gameObject);
			ExplosionManager.instance.CreateExplosionEffect(explosionType, myTransform.position, explosionNormal);
			ExplosionManager.instance.CreateDamageExplosion(myTransform.position, radius, damage, sourceActor, velocity, directHitCol, debugMode: false, sourcePlayer);
		}
	}

	private static float GetAtmosMultiplier(Vector3 position, Vector3 velocity)
	{
		return Mathf.Clamp01(2.5f * AerodynamicsController.fetch.AtmosDensityAtPosition(position) * velocity.magnitude / 100f);
	}

	public static float GetRotationDelta(float time, Vector3 position, Vector3 velocity, float deltaTime)
	{
		return GetAtmosMultiplier(position, velocity) * time * 25f * deltaTime;
	}

	private void OnDestroy()
	{
		if (fired && allFiredRockets != null)
		{
			allFiredRockets.Remove(this);
			FloatingOrigin.instance.OnOriginShift -= FloatingOrigin_instance_OnOriginShift;
		}
	}
}
