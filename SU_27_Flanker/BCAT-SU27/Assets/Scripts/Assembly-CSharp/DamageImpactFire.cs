using System.Collections;
using UnityEngine;

public class DamageImpactFire : MonoBehaviour
{
	private Health health;

	public MinMax damageRange;

	public GameObject firePrefab;

	public float fireLifetime = -1f;

	private void Awake()
	{
		health = GetComponentInParent<Health>();
		health.OnDamage += Health_OnDamage;
	}

	private void Health_OnDamage(float damage, Vector3 position, Health.DamageTypes damageType)
	{
		if (damage > damageRange.min && damage < damageRange.max)
		{
			GameObject gameObject = Object.Instantiate(firePrefab, base.transform);
			gameObject.transform.position = position;
			HeatFire component = gameObject.GetComponent<HeatFire>();
			if ((bool)component)
			{
				component.target = base.transform;
				component.localPosition = gameObject.transform.localPosition;
				gameObject.transform.parent = null;
			}
			if (fireLifetime > 0f)
			{
				StartCoroutine(FireDestroyRoutine(gameObject));
			}
		}
	}

	private IEnumerator FireDestroyRoutine(GameObject fireObj)
	{
		yield return new WaitForSeconds(fireLifetime);
		ParticleSystem[] componentsInChildren = fireObj.GetComponentsInChildren<ParticleSystem>();
		float longestLife = componentsInChildren.GetLongestLife();
		componentsInChildren.SetEmission(emit: false);
		yield return new WaitForSeconds(longestLife);
		Object.Destroy(fireObj);
	}
}
