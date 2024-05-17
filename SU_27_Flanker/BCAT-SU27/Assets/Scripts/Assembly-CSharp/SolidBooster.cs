using System.Collections;
using UnityEngine;

public class SolidBooster : MonoBehaviour, IParentRBDependent, IQSMissileComponent, IMassObject
{
	public float thrust;

	public float burnTime;

	public Transform exhaustTransform;

	private ParticleSystem[] particleSystems;

	public float extinguishRate;

	public AudioSource audioSource;

	public Light exhaustLight;

	public Rigidbody rb;

	public HeatEmitter heatEmitter;

	[Tooltip("No rigidbody necessary")]
	public bool detachBooster;

	public float detachDelay;

	public float detachTorque;

	public float boosterMass;

	private bool fired;

	private float firedTime;

	private bool finished;

	private bool detached;

	private void Awake()
	{
		if ((bool)audioSource)
		{
			audioSource.Stop();
		}
		if ((bool)exhaustLight)
		{
			exhaustLight.enabled = false;
		}
		particleSystems = exhaustTransform.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		particleSystems.SetEmissionAndActive(emit: false);
		Missile componentInParent = GetComponentInParent<Missile>();
		if ((bool)componentInParent)
		{
			componentInParent.OnDetonate.AddListener(OnDetonate);
		}
	}

	public void Fire()
	{
		if (!fired)
		{
			fired = true;
			firedTime = Time.time;
			StartCoroutine(ThrustRoutine());
		}
	}

	private IEnumerator ThrustRoutine(float elapsedTime = 0f)
	{
		particleSystems.SetEmissionAndActive(emit: true);
		if ((bool)exhaustLight)
		{
			exhaustLight.enabled = true;
		}
		if ((bool)audioSource)
		{
			audioSource.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
			audioSource.Play();
		}
		float t = Time.time;
		while (Time.time - t < burnTime - elapsedTime)
		{
			rb.AddForceAtPosition(thrust * -exhaustTransform.forward, exhaustTransform.position);
			if ((bool)heatEmitter)
			{
				heatEmitter.AddHeat(VTOLVRConstants.MISSILE_THRUST_HEAT_MULT * thrust * Time.fixedDeltaTime);
			}
			yield return new WaitForFixedUpdate();
		}
		ParticleSystem[] array = particleSystems;
		foreach (ParticleSystem ps in array)
		{
			StartCoroutine(RampDownParticles(ps));
		}
		if ((bool)exhaustLight)
		{
			StartCoroutine(LightRampDown());
		}
		if ((bool)audioSource)
		{
			audioSource.Stop();
		}
		if (detachBooster)
		{
			yield return new WaitForSeconds(detachDelay);
			FloatingOrigin.instance.AddQueuedFixedUpdateAction(Detach);
		}
		finished = true;
	}

	private void Detach()
	{
		detached = true;
		base.transform.parent = null;
		MassUpdater component = rb.GetComponent<MassUpdater>();
		if ((bool)component)
		{
			component.UpdateMassObjects();
		}
		else
		{
			rb.mass -= boosterMass;
		}
		Vector3 velocity = rb.velocity;
		rb = base.gameObject.AddComponent<Rigidbody>();
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		rb.velocity = velocity;
		rb.AddTorque(Random.insideUnitSphere * detachTorque, ForceMode.Impulse);
		if (boosterMass > 0f)
		{
			rb.mass = boosterMass;
		}
		else
		{
			rb.mass = 0.005f;
		}
		base.gameObject.AddComponent<FloatingOriginTransform>().SetRigidbody(rb);
		IParentRBDependent[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IParentRBDependent>();
		for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
		{
			componentsInChildrenImplementing[i].SetParentRigidbody(rb);
		}
		SimpleDrag componentInChildren = GetComponentInChildren<SimpleDrag>();
		if ((bool)componentInChildren)
		{
			componentInChildren.rb = rb;
			componentInChildren.enabled = true;
		}
		ParticleSystem[] array = particleSystems;
		foreach (ParticleSystem particleSystem in array)
		{
			if (particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.World)
			{
				particleSystem.transform.parent = null;
				Object.Destroy(particleSystem.gameObject, particleSystem.main.startLifetime.constant + 4f / extinguishRate);
			}
		}
		Object.Destroy(base.gameObject, 5f);
	}

	private IEnumerator RampDownParticles(ParticleSystem ps)
	{
		if (!ps)
		{
			yield break;
		}
		float lerpRate = ((ps.main.simulationSpace == ParticleSystemSimulationSpace.World) ? (extinguishRate / 4f) : extinguishRate);
		ParticleSystem.EmissionModule em = ps.emission;
		float currEmis = em.rateOverTime.constant;
		ParticleSystem.MainModule main = ps.main;
		float size = main.startSize.constant;
		while (currEmis > 1f)
		{
			if (!ps)
			{
				yield break;
			}
			currEmis = Mathf.Lerp(currEmis, 0f, lerpRate * Time.deltaTime);
			em.rateOverTime = currEmis;
			size = Mathf.Lerp(size, 0.01f, lerpRate * Time.deltaTime);
			main.startSize = size;
			yield return null;
		}
		em.enabled = false;
	}

	private IEnumerator LightRampDown()
	{
		while ((bool)exhaustLight && exhaustLight.intensity > 0f)
		{
			exhaustLight.intensity = Mathf.Lerp(exhaustLight.intensity, 0f, extinguishRate * Time.deltaTime);
			yield return null;
		}
		exhaustLight.enabled = false;
	}

	private void OnDetonate()
	{
		if (fired && Application.isPlaying)
		{
			if ((bool)exhaustTransform)
			{
				particleSystems.SetEmission(emit: false);
				exhaustTransform.parent = null;
				exhaustTransform.gameObject.AddComponent<FloatingOriginTransform>().shiftParticles = false;
				Object.Destroy(exhaustTransform.gameObject, particleSystems.GetLongestLife());
			}
			if ((bool)exhaustLight)
			{
				exhaustLight.enabled = false;
			}
		}
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		this.rb = rb;
	}

	public void OnQuicksavedMissile(ConfigNode qsNode, float elapsedTime)
	{
		if (fired && !detached)
		{
			ConfigNode configNode = new ConfigNode("SolidBooster_" + base.gameObject.name);
			qsNode.AddNode(configNode);
			configNode.SetValue("b_elapsedTime", Time.time - firedTime);
			configNode.SetValue("finished", finished);
		}
	}

	public void OnQuickloadedMissile(ConfigNode qsNode, float elapsedTime)
	{
		string text = "SolidBooster_" + base.gameObject.name;
		if (qsNode.HasNode(text))
		{
			float value = qsNode.GetNode(text).GetValue<float>("b_elapsedTime");
			firedTime = Time.time - value;
			fired = true;
			StartCoroutine(ThrustRoutine(value));
		}
	}

	public float GetMass()
	{
		return boosterMass;
	}
}
