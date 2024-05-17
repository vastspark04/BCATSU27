using UnityEngine;

public class DestructionDebris : MonoBehaviour
{
	public Health health;

	public GameObject[] enableOnDestroyed;

	public GameObject[] disableOnDestroyed;

	public GameObject[] debrisObjects;

	public MinMax debrisSpeed;

	public MinMax debrisRotation;

	private bool destroyed;

	private Vector3 localDamagePosition;

	private void Start()
	{
		health.OnDamage += Health_OnDamage;
		health.OnDeath.AddListener(OnDeath);
	}

	private void Health_OnDamage(float damage, Vector3 position, Health.DamageTypes damageType)
	{
		localDamagePosition = base.transform.InverseTransformPoint(position);
	}

	private void OnDeath()
	{
		if (destroyed)
		{
			return;
		}
		destroyed = true;
		enableOnDestroyed.SetActive(active: true);
		disableOnDestroyed.SetActive(active: false);
		GameObject[] array = debrisObjects;
		foreach (GameObject gameObject in array)
		{
			Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			rigidbody.mass = 0.5f;
			rigidbody.velocity = (gameObject.transform.position - base.transform.TransformPoint(localDamagePosition)).normalized * debrisSpeed.Random();
			rigidbody.angularVelocity = debrisRotation.Random() * Random.onUnitSphere;
			gameObject.AddComponent<FloatingOriginTransform>().SetRigidbody(rigidbody);
			Collider[] componentsInChildren = gameObject.GetComponentsInChildren<Collider>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				componentsInChildren[j].enabled = true;
			}
			IParentRBDependent[] componentsInChildrenImplementing = gameObject.GetComponentsInChildrenImplementing<IParentRBDependent>(includeInactive: true);
			for (int j = 0; j < componentsInChildrenImplementing.Length; j++)
			{
				componentsInChildrenImplementing[j].SetParentRigidbody(rigidbody);
			}
			gameObject.transform.parent = null;
			gameObject.SetActive(value: true);
		}
	}

	private void OnDestroy()
	{
		if (debrisObjects == null)
		{
			return;
		}
		GameObject[] array = debrisObjects;
		foreach (GameObject gameObject in array)
		{
			if ((bool)gameObject)
			{
				Object.Destroy(gameObject);
			}
		}
	}
}
