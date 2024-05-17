using UnityEngine;

[DisallowMultipleComponent]
public class FloatingOriginTransform : MonoBehaviour
{
	public bool shiftParticles = true;

	public Rigidbody rb;

	private bool added;

	private RaySpringDamper susp;

	private void Start()
	{
		if (shiftParticles)
		{
			SetupParticleSystems();
		}
	}

	public void SetRigidbody(Rigidbody _rb)
	{
		if (added)
		{
			if ((bool)rb)
			{
				FloatingOrigin.instance.RemoveRigidbody(rb);
				added = false;
			}
			else if ((bool)_rb)
			{
				FloatingOrigin.instance.RemoveTransform(base.transform);
			}
		}
		rb = _rb;
		if (base.enabled && base.gameObject.activeInHierarchy)
		{
			if ((bool)rb)
			{
				FloatingOrigin.instance.AddRigidbody(rb);
				added = true;
			}
			else if (!added)
			{
				FloatingOrigin.instance.AddTransform(base.transform);
				added = true;
			}
		}
	}

	private void OnEnable()
	{
		if ((bool)FloatingOrigin.instance && !added)
		{
			if ((bool)rb)
			{
				FloatingOrigin.instance.AddRigidbody(rb);
			}
			else
			{
				FloatingOrigin.instance.AddTransform(base.transform);
			}
			added = true;
		}
	}

	private void OnDisable()
	{
		if ((bool)FloatingOrigin.instance && added)
		{
			FloatingOrigin.instance.RemoveRigidbody(rb);
			FloatingOrigin.instance.RemoveTransform(base.transform);
			added = false;
		}
	}

	private void OnDestroy()
	{
		if ((bool)FloatingOrigin.instance && added)
		{
			FloatingOrigin.instance.RemoveRigidbody(rb);
			FloatingOrigin.instance.RemoveTransform(base.transform);
			added = false;
		}
	}

	private void OnOriginShift(Vector3 offset)
	{
		base.transform.position += offset;
	}

	private void OnOriginShift_RB(Vector3 offset)
	{
		rb.position += offset;
	}

	private void SetupParticleSystems()
	{
		ParticleSystem[] componentsInChildren = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		foreach (ParticleSystem particleSystem in componentsInChildren)
		{
			if (particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.World && !particleSystem.gameObject.GetComponent<FOParticleShifter>())
			{
				particleSystem.gameObject.AddComponent<FOParticleShifter>();
			}
		}
	}
}
