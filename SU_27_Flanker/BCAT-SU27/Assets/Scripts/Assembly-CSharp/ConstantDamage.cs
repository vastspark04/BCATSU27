using System.Collections;
using UnityEngine;

public class ConstantDamage : MonoBehaviour
{
	public Health target;

	public Actor sourceActor;

	public float damageRate;

	public string damageMessage;

	private void OnEnable()
	{
		StartCoroutine(DamageRoutine());
	}

	private IEnumerator DamageRoutine()
	{
		while (base.enabled && target.normalizedHealth > 0f)
		{
			target.Damage(damageRate * Time.fixedDeltaTime, base.transform.position, Health.DamageTypes.Impact, sourceActor, damageMessage);
			yield return new WaitForFixedUpdate();
		}
	}
}
