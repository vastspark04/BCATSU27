using UnityEngine;
using VTOLVR.Multiplayer;

public class Hitbox : MonoBehaviour
{
	public Health health;

	public float subtractiveArmor;

	public bool setDefaultLayerOnDeath;

	public bool disableOnDeath;

	public Actor actor;

	public float hitProbability = 1f;

	private void Awake()
	{
		health.OnDeath.AddListener(Health_OnDeath);
	}

	private void Start()
	{
		if (!actor)
		{
			actor = GetComponentInParent<Actor>();
		}
	}

	private void Health_OnDeath()
	{
		if (setDefaultLayerOnDeath)
		{
			base.gameObject.layer = 0;
		}
		if (disableOnDeath)
		{
			GetComponent<Collider>().enabled = false;
		}
	}

	public void Damage(float damage, Vector3 position, Health.DamageTypes damageType, Actor sourceActor, string damageMessage, PlayerInfo sourcePlayer = null)
	{
		damage -= subtractiveArmor;
		if (damage > 0f)
		{
			health.Damage(damage, position, damageType, sourceActor, damageMessage, rpcIfRemote: true, sourcePlayer);
		}
	}

	[ContextMenu("GetComponents From Parent")]
	public void GetComponentsFromParent()
	{
		health = GetComponentInParent<Health>();
		actor = GetComponentInParent<Actor>();
	}
}
