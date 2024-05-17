using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class EjectionSeat : MonoBehaviour
{
	public Rigidbody seatRB;

	public Rigidbody canopyRB;

	public float seatEjectDelay = 0.35f;

	public GameObject canopyObject;

	public GameObject cameraRig;

	public AudioClip canopyPopSound;

	public AudioClip ejectSound;

	public GameObject seatParticleObj;

	private ParticleSystem[] seatParticles;

	public GameObject canopyParticleObj;

	private ParticleSystem[] canopyParticles;

	public GameObject pilotModel;

	public UnityEvent OnJettisonCanopy;

	public UnityEvent OnEject;

	public UnityEvent OnFireChute;

	public UnityEvent OnCutChute;

	private ShipController shipController;

	private Actor actor;

	private bool seatEjected;

	private bool collided;

	private MovingPlatform movingPlatform;

	private List<GameObject> cleanupObjects = new List<GameObject>();

	private bool isMine;

	private GameObject seatObject;

	private FloatingOriginShifter oS;

	public bool ejected { get; private set; }

	private void Start()
	{
		ejected = false;
		shipController = GetComponentInParent<ShipController>();
		if ((bool)seatParticleObj)
		{
			seatParticles = seatParticleObj.GetComponentsInChildren<ParticleSystem>();
		}
		if ((bool)canopyParticleObj)
		{
			canopyParticles = canopyParticleObj.GetComponentsInChildren<ParticleSystem>();
		}
		actor = GetComponentInParent<Actor>();
	}

	public void Eject()
	{
		if (!ejected)
		{
			FlightSceneManager.instance.ReportHasEjected();
			ejected = true;
			StartCoroutine(EjectRoutine());
			FlightLogger.Log($"{actor.actorName} ejected.");
		}
	}

	private IEnumerator ChuteRoutine()
	{
		Vector3 vel = seatRB.velocity;
		while (vel.sqrMagnitude > 1f || !collided)
		{
			vel = seatRB.velocity;
			if (base.transform.position.y < WaterPhysics.instance.height)
			{
				collided = true;
			}
			else if (collided && (bool)movingPlatform)
			{
				vel = Vector3.zero;
			}
			yield return null;
		}
		if (OnCutChute != null)
		{
			OnCutChute.Invoke();
		}
		shipController.GetComponentInChildren<TempPilotDetacher>().DetachPilot();
	}

	private void OnCollisionEnter(Collision col)
	{
		if (seatEjected)
		{
			movingPlatform = col.collider.GetComponent<MovingPlatform>();
			collided = true;
		}
	}

	private IEnumerator EjectRoutine()
	{
		isMine = true;
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += RemoveObjectsOnExitScene;
		}
		seatObject = base.gameObject;
		if ((bool)canopyObject)
		{
			canopyObject.GetComponentInChildren<Collider>().enabled = true;
		}
		seatObject.GetComponentInChildren<Collider>().enabled = true;
		oS = FloatingOriginShifter.instance;
		FloatingOrigin.instance.AddQueuedFixedUpdateAction(FixedUpdateDelayedCanopyJett);
		yield return new WaitForSeconds(seatEjectDelay);
		FloatingOrigin.instance.AddQueuedFixedUpdateAction(FixedUpdateDelayedEject);
		StartCoroutine(DestroyWithActorRoutine());
		while (!seatEjected)
		{
			yield return null;
		}
		float t = Time.time;
		yield return new WaitForSeconds(0.5f);
		seatRB.GetComponentInChildren<Collider>().gameObject.layer = 0;
		while (seatRB.velocity.y > -6f && Time.time - t < 2.25f && WaterPhysics.GetAltitude(base.transform.position) > 2000f)
		{
			yield return null;
		}
		if (OnFireChute != null)
		{
			OnFireChute.Invoke();
		}
		StartCoroutine(ChuteRoutine());
		if (!VTOLMPUtils.IsMultiplayer())
		{
			EndMission.AddFailText("Pilot ejected.");
		}
	}

	private IEnumerator DestroyWithActorRoutine()
	{
		while ((bool)actor)
		{
			yield return null;
		}
		Object.Destroy(base.gameObject);
	}

	private void RemoveObjectsOnExitScene()
	{
		if (cleanupObjects != null)
		{
			foreach (GameObject cleanupObject in cleanupObjects)
			{
				if ((bool)cleanupObject)
				{
					Object.Destroy(cleanupObject);
				}
			}
		}
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= RemoveObjectsOnExitScene;
		}
	}

	private void FixedUpdateDelayedCanopyJett()
	{
		if ((bool)canopyObject)
		{
			canopyObject.transform.parent = null;
			cleanupObjects.Add(canopyObject);
			if (!canopyRB)
			{
				canopyRB = canopyObject.GetComponent<Rigidbody>();
				if (!canopyRB)
				{
					canopyRB = canopyObject.AddComponent<Rigidbody>();
					canopyRB.mass = 0.1f;
					canopyRB.angularDrag = 0.05f;
				}
			}
			IParentRBDependent[] componentsInChildrenImplementing = canopyObject.GetComponentsInChildrenImplementing<IParentRBDependent>(includeInactive: true);
			foreach (IParentRBDependent parentRBDependent in componentsInChildrenImplementing)
			{
				parentRBDependent.SetParentRigidbody(canopyRB);
				if (parentRBDependent is MonoBehaviour)
				{
					((MonoBehaviour)parentRBDependent).enabled = true;
				}
			}
			if (!canopyObject.GetComponent<FloatingOriginTransform>())
			{
				canopyObject.AddComponent<FloatingOriginTransform>().SetRigidbody(canopyRB);
			}
			canopyRB.isKinematic = false;
			canopyRB.velocity = shipController.rb.GetPointVelocity(canopyRB.transform.position);
			canopyRB.velocity += 45f * canopyRB.transform.forward;
			canopyRB.angularVelocity = Random.insideUnitSphere * 5f;
			canopyRB.interpolation = RigidbodyInterpolation.Interpolate;
		}
		seatObject.GetComponent<AudioSource>().PlayOneShot(canopyPopSound);
		if (canopyParticles != null)
		{
			StartCoroutine(ParticleBurst(canopyParticles, 0.4f));
		}
		if (OnJettisonCanopy != null)
		{
			OnJettisonCanopy.Invoke();
		}
		AudioController.instance.SetExteriorOpening("eject", 1f);
	}

	private void OnDestroy()
	{
		if ((bool)AudioController.instance && isMine)
		{
			AudioController.instance.SetExteriorOpening("eject", 0f);
		}
	}

	private void FixedUpdateDelayedEject()
	{
		seatEjected = true;
		BlackoutEffect componentInChildren = VRHead.instance.GetComponentInChildren<BlackoutEffect>();
		if ((bool)componentInChildren)
		{
			componentInChildren.rb = seatRB;
			componentInChildren.useFlightInfo = false;
		}
		seatObject.transform.parent = null;
		seatRB.position = seatObject.transform.position;
		cleanupObjects.Add(seatObject);
		seatObject.AddComponent<FloatingOriginShifter>().threshold = oS.threshold;
		oS.enabled = false;
		seatObject.AddComponent<FloatingOriginTransform>();
		seatRB.isKinematic = false;
		seatRB.interpolation = RigidbodyInterpolation.Interpolate;
		seatRB.collisionDetectionMode = CollisionDetectionMode.Continuous;
		seatRB.velocity = shipController.rb.GetPointVelocity(seatRB.transform.position);
		IParentRBDependent[] componentsInChildrenImplementing = seatObject.GetComponentsInChildrenImplementing<IParentRBDependent>();
		for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
		{
			componentsInChildrenImplementing[i].SetParentRigidbody(seatRB);
		}
		StartCoroutine(SeatThrustRoutine());
		seatRB.angularVelocity = 0.15f * Random.insideUnitSphere;
		foreach (VRHandController controller in VRHandController.controllers)
		{
			if ((bool)controller.activeInteractable && controller.activeInteractable.transform.root != seatObject.transform)
			{
				controller.ReleaseFromInteractable();
			}
		}
		seatObject.GetComponent<AudioSource>().PlayOneShot(ejectSound);
		if (OnEject != null)
		{
			OnEject.Invoke();
		}
		if (seatParticles != null)
		{
			StartCoroutine(ParticleBurst(seatParticles, 0.75f));
		}
	}

	private IEnumerator SeatThrustRoutine()
	{
		float speed = 0f;
		float tgtSpeed = 32f;
		float num = 32f;
		float accel = num * 9.81f;
		Vector3 relativeForce = accel * Vector3.up;
		while (speed < tgtSpeed)
		{
			speed += accel * Time.fixedDeltaTime;
			seatRB.AddRelativeForce(relativeForce, ForceMode.Acceleration);
			yield return new WaitForFixedUpdate();
		}
	}

	private IEnumerator ParticleBurst(ParticleSystem[] ps, float time)
	{
		ps.SetEmission(emit: true);
		yield return new WaitForSeconds(time);
		ps.SetEmission(emit: false);
	}
}
