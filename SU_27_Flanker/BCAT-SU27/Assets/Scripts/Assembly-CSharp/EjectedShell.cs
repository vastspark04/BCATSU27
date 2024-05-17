using UnityEngine;

public class EjectedShell : MonoBehaviour
{
	public const float lifeTime = 4f;

	public Rigidbody rb;

	private float ejectTime;

	private static ObjectPool shellPool;

	private static bool initialized;

	public void Eject(Vector3 velocity)
	{
		rb.angularVelocity = Random.insideUnitCircle * 5f;
		rb.velocity = velocity;
		ejectTime = Time.time;
		base.gameObject.SetActive(value: true);
	}

	private void Update()
	{
		if (Time.time - ejectTime > 4f)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	private void FixedUpdate()
	{
		Vector3 force = -rb.velocity.normalized * rb.velocity.sqrMagnitude * 5E-05f;
		rb.AddForce(force);
	}

	public static void Eject(Vector3 position, Quaternion rotation, float speed, float scale, Vector3 sourceVelocity)
	{
		if (!initialized)
		{
			CreatePool();
		}
		speed = Random.Range(0.9f, 1.1f) * speed;
		rotation = Quaternion.RotateTowards(rotation, Random.rotation, 3f);
		GameObject pooledObject = shellPool.GetPooledObject();
		pooledObject.transform.position = position;
		pooledObject.transform.rotation = rotation;
		pooledObject.transform.localScale = scale * Vector3.one;
		pooledObject.GetComponent<EjectedShell>().Eject(speed * pooledObject.transform.up + sourceVelocity);
	}

	public static void CreatePool()
	{
		if (!initialized)
		{
			initialized = true;
			shellPool = ObjectPool.CreateObjectPool((GameObject)Resources.Load("Weapons/ShellPrefab"), 30, canGrow: true, destroyOnLoad: false);
		}
	}
}
