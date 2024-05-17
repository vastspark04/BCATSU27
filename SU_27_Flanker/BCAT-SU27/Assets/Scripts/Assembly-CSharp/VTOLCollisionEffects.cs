using UnityEngine;

public class VTOLCollisionEffects : MonoBehaviour
{
	public AudioSource collideAudioSource;

	public AudioSource scrapeAudioSource;

	public AudioClip[] collideSounds;

	public AudioClip scrapeLoop;

	private bool stopScrapeNext;

	private bool stopScrapeNow;

	public AnimationCurve scrapeSpeedPitch;

	public AnimationCurve scrapeSpeedVolume;

	public float impactDamageFactor;

	public float scrapeDamageFactor;

	private Health health;

	private Rigidbody rb;

	private float startTime;

	private Hitbox[] hitboxes;

	private const float MAX_IMPACT_DAMAGE = 300f;

	private const float MAX_SCRAPE_DPS = 300f;

	private void Awake()
	{
		startTime = Time.time;
	}

	private void Start()
	{
		rb = GetComponentInParent<Rigidbody>();
		this.health = GetComponent<Health>();
		scrapeAudioSource.clip = scrapeLoop;
		hitboxes = GetComponentsInChildren<Hitbox>();
		Health[] componentsInChildren = GetComponentsInChildren<Health>();
		foreach (Health health in componentsInChildren)
		{
			if (!health.GetComponent<Missile>())
			{
				health.OnDamage += Health_OnDamage;
			}
		}
	}

	private void Health_OnDamage(float damage, Vector3 position, Health.DamageTypes damageType)
	{
		if (damageType == Health.DamageTypes.Impact)
		{
			collideAudioSource.transform.position = position;
			collideAudioSource.pitch = Random.Range(0.8f, 1.2f);
			collideAudioSource.volume = damage / (10f * impactDamageFactor);
			collideAudioSource.PlayOneShot(RandomSound());
		}
	}

	private void FixedUpdate()
	{
		if (stopScrapeNext)
		{
			stopScrapeNext = false;
			stopScrapeNow = true;
		}
		else if (stopScrapeNow)
		{
			scrapeAudioSource.Stop();
			scrapeAudioSource.volume = 0f;
			stopScrapeNow = false;
		}
	}

	private void OnCollisionStay(Collision col)
	{
		if (base.enabled && col.gameObject.layer != 16 && !(Time.time - startTime < 5f))
		{
			Vector3 pointVelocity = rb.GetPointVelocity(col.contacts[0].point);
			if ((bool)col.rigidbody)
			{
				pointVelocity -= col.rigidbody.GetPointVelocity(col.contacts[0].point);
			}
			float magnitude = pointVelocity.magnitude;
			if (!scrapeAudioSource.isPlaying)
			{
				scrapeAudioSource.Play();
			}
			scrapeAudioSource.volume = Mathf.Lerp(scrapeAudioSource.volume, scrapeSpeedVolume.Evaluate(magnitude), 10f * Time.deltaTime) * 0.6f;
			scrapeAudioSource.pitch = scrapeSpeedPitch.Evaluate(magnitude);
			stopScrapeNow = false;
			stopScrapeNext = true;
			health.Damage(magnitude * scrapeDamageFactor * Time.fixedDeltaTime, col.contacts[0].point, Health.DamageTypes.Scrape, null, "scraping");
		}
	}

	private void OnCollisionEnter(Collision col)
	{
		if (!base.enabled || col.gameObject.layer == 16 || Time.time - startTime < 5f)
		{
			return;
		}
		float magnitude = col.impulse.magnitude;
		string text = $"({col.contacts[0].thisCollider.gameObject.name} collided with {col.contacts[0].otherCollider.gameObject.name})";
		float damage = Mathf.Min(magnitude * impactDamageFactor, 300f);
		Vector3 point = col.contacts[0].point;
		Hitbox hitbox = null;
		float num = float.MaxValue;
		for (int i = 0; i < hitboxes.Length; i++)
		{
			if ((bool)hitboxes[i] && hitboxes[i].health.normalizedHealth > 0f)
			{
				float sqrMagnitude = (hitboxes[i].transform.position - point).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					hitbox = hitboxes[i];
					num = sqrMagnitude;
				}
			}
		}
		if ((bool)hitbox)
		{
			hitbox.Damage(damage, point, Health.DamageTypes.Impact, null, text);
		}
		else
		{
			health.Damage(damage, point, Health.DamageTypes.Impact, null, text);
		}
		if (!col.rigidbody)
		{
			return;
		}
		Hitbox[] componentsInChildren = col.rigidbody.GetComponentsInChildren<Hitbox>();
		float num2 = float.MaxValue;
		Hitbox hitbox2 = null;
		Hitbox[] array = componentsInChildren;
		foreach (Hitbox hitbox3 in array)
		{
			float sqrMagnitude2 = (hitbox3.transform.position - point).sqrMagnitude;
			if (sqrMagnitude2 < num2)
			{
				hitbox2 = hitbox3;
				num2 = sqrMagnitude2;
			}
		}
		if (hitbox2 != null && (!hitbox || hitbox2.actor != hitbox.actor))
		{
			hitbox2.Damage(damage, point, Health.DamageTypes.Impact, null, "Collision");
		}
	}

	private AudioClip RandomSound()
	{
		int num = Random.Range(0, collideSounds.Length - 1);
		return collideSounds[num];
	}
}
