using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class Health : MonoBehaviour, IQSVehicleComponent
{
	public enum DamageTypes
	{
		Impact,
		Scrape
	}

	public delegate void DamageEvent(float damage, Vector3 position, DamageTypes damageType);

	public bool invincible;

	private bool awoke;

	public float startHealth = 1f;

	private float _currHealth;

	public float maxHealth = 1f;

	public float minDamage;

	public UnityEvent OnDeath;

	private Actor lastSourceActor;

	private float lastSourceActorTime;

	private DamageSync damageSync;

	private bool isMP;

	private bool killed;

	public float normalizedHealth
	{
		get
		{
			if (awoke)
			{
				return _currHealth / maxHealth;
			}
			return 1f;
		}
	}

	public float currentHealth
	{
		get
		{
			if (awoke)
			{
				return _currHealth;
			}
			return maxHealth;
		}
	}

	public Actor killedByActor { get; private set; }

	public string killMessage { get; private set; }

	public bool isDead => killed;

	public event DamageEvent OnDamage;

	public event UnityAction<float> OnNrmHealthChanged;

	private void Awake()
	{
		isMP = VTOLMPUtils.IsMultiplayer();
		_currHealth = Mathf.Clamp(startHealth, 0.01f, maxHealth);
		awoke = true;
	}

	private void Start()
	{
		if (this.OnNrmHealthChanged != null)
		{
			this.OnNrmHealthChanged(normalizedHealth);
		}
		damageSync = GetComponentInParent<DamageSync>();
	}

	public void Damage(float damage, Vector3 position, DamageTypes damageType, Actor sourceActor, string message = null, bool rpcIfRemote = true, PlayerInfo sourcePlayer = null)
	{
		if (invincible || _currHealth <= 0f || damage < minDamage)
		{
			return;
		}
		if (isMP && (bool)damageSync)
		{
			if (!damageSync.isMine)
			{
				if (rpcIfRemote)
				{
					damageSync.RemoteDamage(sourceActor, damage, damageType, this, sourcePlayer);
					return;
				}
			}
			else
			{
				damageSync.SetDamageCredit(sourcePlayer);
			}
		}
		float num = normalizedHealth;
		damage = Mathf.Abs(damage);
		_currHealth -= damage;
		if (this.OnDamage != null)
		{
			this.OnDamage(damage, position, damageType);
		}
		if ((bool)sourceActor)
		{
			lastSourceActor = sourceActor;
			lastSourceActorTime = Time.time;
		}
		if (_currHealth <= 0f && !killed)
		{
			_currHealth = 0f;
			if (Time.time - lastSourceActorTime < 15f)
			{
				killedByActor = lastSourceActor;
			}
			killMessage = message;
			if (OnDeath != null)
			{
				OnDeath.Invoke();
			}
			killed = true;
		}
		if (num != normalizedHealth && this.OnNrmHealthChanged != null)
		{
			this.OnNrmHealthChanged(normalizedHealth);
		}
	}

	public void Kill()
	{
		Damage(maxHealth, base.transform.position, DamageTypes.Impact, null);
	}

	public void Kill(string damageMessage)
	{
		Damage(maxHealth, base.transform.position, DamageTypes.Impact, null, damageMessage);
	}

	public void KillDelayed(float t)
	{
		if (base.gameObject.activeInHierarchy)
		{
			StartCoroutine(DelayedKillRoutine(t));
		}
		else
		{
			Kill();
		}
	}

	public void QS_Kill()
	{
		if (!killed)
		{
			killed = true;
			_currHealth = 0f;
			if (OnDeath != null)
			{
				OnDeath.Invoke();
			}
		}
	}

	private IEnumerator DelayedKillRoutine(float t)
	{
		yield return new WaitForSeconds(t);
		Kill();
	}

	public void Heal(float healAmt)
	{
		if (healAmt > 0f)
		{
			killed = false;
			float num = normalizedHealth;
			_currHealth = Mathf.Clamp(_currHealth + healAmt, 0f, maxHealth);
			if (num != normalizedHealth && this.OnNrmHealthChanged != null)
			{
				this.OnNrmHealthChanged(normalizedHealth);
			}
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode("Health_" + base.gameObject.name).SetValue("currentHealth", _currHealth);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		float num = normalizedHealth;
		ConfigNode node = qsNode.GetNode("Health_" + base.gameObject.name);
		if (node != null)
		{
			float num2 = (_currHealth = node.GetValue<float>("currentHealth"));
			if (num != normalizedHealth && this.OnNrmHealthChanged != null)
			{
				this.OnNrmHealthChanged(normalizedHealth);
			}
		}
	}
}
