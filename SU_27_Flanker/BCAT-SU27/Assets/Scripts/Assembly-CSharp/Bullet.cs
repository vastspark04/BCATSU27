using System;
using UnityEngine;
using VTOLVR.Multiplayer;

public class Bullet : MonoBehaviour, IFloatingOriginShiftable
{
	public delegate void BulletFiredDelegate(Ray ray, float speed);

	[Serializable]
	public struct BulletInfo
	{
		public float speed;

		public float tracerWidth;

		public float rayWidth;

		public float dispersion;

		public float damage;

		public float detonationRange;

		public Color color;

		public float maxLifetime;

		public float lifetimeVariance;

		public float projectileMass;

		public float totalMass;
	}

	public class BulletPoolDestructor : MonoBehaviour
	{
		private void OnDestroy()
		{
			poolCreated = false;
		}
	}

	public LineRenderer lr;

	private Vector3 velocity;

	private float tracerWidth;

	private float randomWidthScale;

	private float rayWidth;

	private float damage;

	private float detonationRange;

	public float startTime;

	private bool skipFrame;

	private bool dontRic;

	private float speed;

	private float lifeTime;

	private float fov;

	private float widthFactor;

	private bool bulletHitAudio = true;

	private Vector3 hitPoint;

	private Vector3 hitNormal;

	private Transform myTransform;

	private Actor sourceActor;

	private int layerMask = 1025;

	private bool bActive;

	public Color color;

	private float mass;

	private bool longInitial;

	private FixedPoint initialPoint;

	private const float bulletSpeedLength = 0.0243f;

	private PlayerInfo sourcePlayer;

	private const string DMG_BULLET_MESSAGE = "Bullet Impact";

	private Collider directHitCol;

	private static ObjectPool bulletPool;

	private static bool poolCreated;

	private void Awake()
	{
		myTransform = base.transform;
	}

	private void Start()
	{
		float num3 = (lr.startWidth = (lr.endWidth = tracerWidth));
	}

	private void OnEnable()
	{
		bActive = true;
		FloatingOrigin.instance.AddShiftable(this);
	}

	private void OnDisable()
	{
		bActive = false;
		FloatingOrigin.instance.RemoveShiftable(this);
	}

	private void OnOriginShift(Vector3 offset)
	{
		if (bActive)
		{
			myTransform.position += offset;
		}
	}

	public void OnFloatingOriginShift(Vector3 offset)
	{
		myTransform.position += offset;
	}

	public void Fire(Vector3 origin, Vector3 direction, float speed, float width, float dispersion, float rayWidth, float damage, float detonationRange, Vector3 inheritVelocity, Color color, float lifeTime, Actor sourceActor, float bulletMass, BulletFiredDelegate fEvent)
	{
		mass = bulletMass;
		direction = VectorUtils.WeightedDirectionDeviation(direction, dispersion);
		velocity = direction * speed;
		velocity += inheritVelocity;
		myTransform.position = origin;
		tracerWidth = width;
		randomWidthScale = UnityEngine.Random.Range(0.5f, 1f);
		fEvent?.Invoke(new Ray(origin, velocity), velocity.magnitude);
		fov = VRHead.instance.fieldOfView;
		widthFactor = tracerWidth * randomWidthScale * fov / 180f * 0.35f;
		lr.widthMultiplier = tracerWidth;
		myTransform.rotation = Quaternion.LookRotation(velocity);
		this.damage = damage;
		this.detonationRange = detonationRange;
		this.rayWidth = rayWidth;
		this.speed = speed;
		this.lifeTime = lifeTime;
		this.color = color;
		lr.endColor = color;
		Color startColor = color;
		startColor.a = 0f;
		lr.startColor = startColor;
		Vector3 position = new Vector3(0f, 0f, speed * 0.0243f);
		lr.SetPosition(1, position);
		lr.SetPosition(0, Vector3.zero);
		startTime = Time.time;
		skipFrame = true;
		longInitial = false;
		if (detonationRange >= 9f)
		{
			dontRic = true;
		}
		else
		{
			dontRic = false;
		}
		this.sourceActor = sourceActor;
		if (VTOLMPUtils.IsMultiplayer())
		{
			if ((bool)sourceActor)
			{
				if ((bool)sourceActor.unitSpawn && sourceActor.unitSpawn is MultiplayerSpawn)
				{
					sourcePlayer = VTOLMPSceneManager.instance.GetPlayer(sourceActor);
				}
			}
			else
			{
				sourcePlayer = null;
			}
		}
		else
		{
			sourcePlayer = null;
		}
		base.gameObject.SetActive(value: true);
	}

	public void FireWithInitial(Vector3 origin, Vector3 direction, float speed, float width, float dispersion, float rayWidth, float damage, float detonationRange, Vector3 inheritVelocity, Color color, float lifeTime, Actor sourceActor, float bulletMass, Vector3 initialOrigin)
	{
		mass = bulletMass;
		direction = VectorUtils.WeightedDirectionDeviation(direction, dispersion);
		velocity = direction * speed;
		velocity += inheritVelocity;
		myTransform.position = origin;
		tracerWidth = width;
		randomWidthScale = UnityEngine.Random.Range(0.5f, 1f);
		fov = VRHead.instance.fieldOfView;
		widthFactor = tracerWidth * randomWidthScale * fov / 180f * 0.35f;
		lr.widthMultiplier = tracerWidth;
		myTransform.rotation = Quaternion.LookRotation(velocity);
		this.damage = damage;
		this.detonationRange = detonationRange;
		this.rayWidth = rayWidth;
		this.speed = speed;
		this.lifeTime = lifeTime;
		this.color = color;
		lr.endColor = color;
		Color startColor = color;
		startColor.a = 0f;
		lr.startColor = startColor;
		Vector3 position = new Vector3(0f, 0f, speed * 0.0243f);
		longInitial = true;
		lr.SetPosition(1, position);
		lr.SetPosition(0, base.transform.InverseTransformPoint(initialOrigin));
		initialPoint = new FixedPoint(initialOrigin);
		startTime = Time.time;
		skipFrame = true;
		if (detonationRange >= 9f)
		{
			dontRic = true;
		}
		else
		{
			dontRic = false;
		}
		this.sourceActor = sourceActor;
		if (VTOLMPUtils.IsMultiplayer())
		{
			if ((bool)sourceActor && (bool)sourceActor.unitSpawn && sourceActor.unitSpawn is MultiplayerSpawn)
			{
				sourcePlayer = VTOLMPSceneManager.instance.GetPlayer(sourceActor);
			}
		}
		else
		{
			sourcePlayer = null;
		}
		base.gameObject.SetActive(value: true);
	}

	private void LateUpdate()
	{
		UpdateBullet(Time.deltaTime);
	}

	private void UpdateBullet(float deltaTime)
	{
		bulletHitAudio = true;
		if (skipFrame)
		{
			skipFrame = false;
			return;
		}
		Vector3 vector = myTransform.position;
		Vector3 vector2 = vector;
		vector2 += velocity * deltaTime;
		bool flag = false;
		if (longInitial)
		{
			vector = initialPoint.point + deltaTime / 2f * velocity;
			lr.SetPosition(0, Vector3.zero);
			longInitial = false;
		}
		flag = ((!(rayWidth <= 0f)) ? Physics.SphereCast(vector, rayWidth / 2f, vector2 - vector, out var hitInfo, speed * deltaTime, layerMask) : Physics.Linecast(vector, vector2, out hitInfo, layerMask));
		Hitbox hitbox = null;
		if (flag)
		{
			hitbox = hitInfo.collider.GetComponent<Hitbox>();
			if ((bool)hitbox)
			{
				if ((bool)hitbox.actor && hitbox.actor == sourceActor)
				{
					flag = false;
				}
				else if (hitbox.hitProbability < 1f && UnityEngine.Random.Range(0f, 1f) > hitbox.hitProbability)
				{
					flag = false;
				}
			}
		}
		if (flag)
		{
			if (mass > 0f)
			{
				Rigidbody rigidbody = hitInfo.rigidbody;
				if ((bool)rigidbody && !rigidbody.isKinematic)
				{
					Vector3 vector3 = velocity - rigidbody.GetPointVelocity(hitInfo.point);
					rigidbody.AddForceAtPosition(vector3 * mass / rigidbody.mass, hitInfo.point, ForceMode.VelocityChange);
				}
			}
			vector2 = hitInfo.point - velocity * Time.deltaTime;
			lr.SetPosition(1, myTransform.InverseTransformPoint(hitInfo.point));
			float num = Vector3.Angle(hitInfo.normal, -velocity);
			bool num2 = !dontRic && RicochetOnPart(num, speed);
			hitPoint = hitInfo.point;
			hitNormal = hitInfo.normal;
			if (!num2)
			{
				if (detonationRange < 9f)
				{
					if ((bool)hitbox)
					{
						hitbox.Damage(damage, hitInfo.point, Health.DamageTypes.Impact, sourceActor, "Bullet Impact", sourcePlayer);
					}
				}
				else
				{
					directHitCol = hitInfo.collider;
				}
				KillBullet();
				return;
			}
			widthFactor *= 0.75f;
			vector2 = hitInfo.point;
			velocity = Vector3.Reflect(velocity, hitInfo.normal);
			velocity = Mathf.Clamp01(num / 150f) * velocity * 0.45f;
			Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
			velocity = Vector3.RotateTowards(velocity, onUnitSphere, UnityEngine.Random.Range(0f, 100f - num) * ((float)Math.PI / 180f), 0f);
			speed = velocity.magnitude;
			lr.SetPosition(1, new Vector3(0f, 0f, speed * Time.deltaTime * 1.62f));
			bulletHitAudio = false;
			BulletHitManager.instance.CreateBulletHit(hitInfo.point, hitInfo.normal, bulletHitAudio);
			dontRic = true;
		}
		else
		{
			float height = WaterPhysics.instance.height;
			if (vector2.y < height && vector.y > height)
			{
				BulletHitManager.instance.CreateSplash(vector2, velocity);
				hitPoint = vector2;
				hitNormal = myTransform.forward;
				KillBullet();
			}
			else if (Time.time - startTime > lifeTime)
			{
				hitPoint = vector2;
				hitNormal = myTransform.forward;
				KillBullet();
			}
		}
		myTransform.position = vector2;
		float num3 = Mathf.Pow(Mathf.Clamp((vector2 - VRHead.position).sqrMagnitude, 25f, 36000000f), 0.28f);
		float widthMultiplier = widthFactor * num3;
		lr.widthMultiplier = widthMultiplier;
		velocity += Physics.gravity * deltaTime;
		myTransform.localRotation = Quaternion.LookRotation(velocity);
	}

	private void KillBullet()
	{
		if (detonationRange > 0f)
		{
			ExplosionManager.instance.CreateDamageExplosion(hitPoint + hitNormal * 0.1f, detonationRange, damage, sourceActor, velocity, directHitCol, debugMode: false, sourcePlayer);
			if (detonationRange >= 9f)
			{
				bulletHitAudio = false;
				ExplosionManager.instance.CreateExplosionEffect(ExplosionManager.ExplosionTypes.Small, hitPoint, hitNormal);
			}
		}
		directHitCol = null;
		if (!dontRic)
		{
			BulletHitManager.instance.CreateBulletHit(hitPoint, hitNormal, bulletHitAudio);
		}
		base.gameObject.SetActive(value: false);
	}

	private bool RicochetOnPart(float angleFromNormal, float impactVel)
	{
		float num = Mathf.Pow(angleFromNormal - 5f, 2f) * 4f / impactVel;
		if (UnityEngine.Random.Range(0f, 100f) < num)
		{
			return true;
		}
		return false;
	}

	public static void FireBullet(Vector3 origin, Vector3 direction, float speed, float width, float dispersion, float rayWidth, float damage, Vector3 inheritVelocity, Color color, Actor sourceActor, float detonationRange = 0f, float lifeTime = 6f, float lifetimeVariance = 0f, float bulletMass = 5E-05f, BulletFiredDelegate fEvent = null)
	{
		if (!poolCreated)
		{
			CreatePool();
		}
		bulletPool.GetPooledObject().GetComponent<Bullet>().Fire(origin, direction, speed, width, dispersion, rayWidth, damage, detonationRange, inheritVelocity, color, lifeTime + UnityEngine.Random.Range(0f - lifetimeVariance, lifetimeVariance), sourceActor, bulletMass, fEvent);
	}

	public static void FireBullet(Vector3 origin, Vector3 direction, BulletInfo info, Vector3 inheritVelocity, Actor sourceActor, BulletFiredDelegate fEvent = null)
	{
		FireBullet(origin, direction, info.speed, info.tracerWidth, info.dispersion, info.rayWidth, info.damage, inheritVelocity, info.color, sourceActor, info.detonationRange, info.maxLifetime, info.lifetimeVariance, info.projectileMass, fEvent);
	}

	public static void FireBullet(Vector3 origin, Vector3 direction, BulletInfo info, Vector3 inheritVelocity, Actor sourceActor)
	{
		FireBullet(origin, direction, info.speed, info.tracerWidth, info.dispersion, info.rayWidth, info.damage, inheritVelocity, info.color, sourceActor, info.detonationRange, info.maxLifetime, info.lifetimeVariance, info.projectileMass);
	}

	public static void FireBulletWithOrigin(Vector3 origin, Vector3 direction, float speed, float width, float dispersion, float rayWidth, float damage, Vector3 inheritVelocity, Color color, Actor sourceActor, Vector3 initialOrigin, float detonationRange = 0f, float lifeTime = 6f, float lifetimeVariance = 0f, float bulletMass = 5E-05f)
	{
		if (!poolCreated)
		{
			CreatePool();
		}
		bulletPool.GetPooledObject().GetComponent<Bullet>().FireWithInitial(origin, direction, speed, width, dispersion, rayWidth, damage, detonationRange, inheritVelocity, color, lifeTime + UnityEngine.Random.Range(0f - lifetimeVariance, lifetimeVariance), sourceActor, bulletMass, initialOrigin);
	}

	public static void CreatePool()
	{
		if (!poolCreated)
		{
			bulletPool = ObjectPool.CreateObjectPool((GameObject)Resources.Load("Weapons/BulletPrefab"), 500, canGrow: true, destroyOnLoad: true);
			bulletPool.gameObject.AddComponent<BulletPoolDestructor>();
			poolCreated = true;
		}
	}

	public static void DisableFiredBullets()
	{
		if ((bool)bulletPool)
		{
			bulletPool.DisableAll();
		}
	}

	public static void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode("BULLETS");
		qsNode.AddNode(configNode);
		foreach (GameObject item in bulletPool.pool)
		{
			if (item.activeSelf)
			{
				ConfigNode configNode2 = new ConfigNode("B");
				configNode.AddNode(configNode2);
				Bullet component = item.GetComponent<Bullet>();
				configNode2.SetValue("vel", component.velocity);
				configNode2.SetValue("pos", VTMapManager.WorldToGlobalPoint(component.transform.position));
				configNode2.SetValue("width", component.tracerWidth);
				configNode2.SetValue("color", component.color);
				configNode2.SetValue("rayWidth", component.rayWidth);
				configNode2.SetValue("timeLeft", component.lifeTime - (Time.time - component.startTime));
				configNode2.SetValue("dmg", component.damage);
				configNode2.SetValue("detRange", component.detonationRange);
				if ((bool)component.sourceActor)
				{
					configNode2.AddNode(QuicksaveManager.SaveActorIdentifierToNode(component.sourceActor, "sourceActor"));
				}
			}
		}
	}

	public static void OnQuickload(ConfigNode qsNode)
	{
		foreach (GameObject item in bulletPool.pool)
		{
			item.SetActive(value: false);
		}
		if (!qsNode.HasNode("BULLETS"))
		{
			return;
		}
		foreach (ConfigNode node in qsNode.GetNode("BULLETS").GetNodes("B"))
		{
			Vector3 value = node.GetValue<Vector3>("vel");
			Vector3 origin = VTMapManager.GlobalToWorldPoint(node.GetValue<Vector3D>("pos"));
			float value2 = node.GetValue<float>("width");
			Color value3 = node.GetValue<Color>("color");
			float value4 = node.GetValue<float>("rayWidth");
			float value5 = node.GetValue<float>("timeLeft");
			float value6 = node.GetValue<float>("dmg");
			float value7 = node.GetValue<float>("detRange");
			Actor actor = null;
			actor = QuicksaveManager.RetrieveActorFromNode(node.GetNode("sourceActor"));
			FireBullet(origin, value, value.magnitude, value2, 0f, value4, value6, Vector3.zero, value3, actor, value7, value5);
		}
	}
}
