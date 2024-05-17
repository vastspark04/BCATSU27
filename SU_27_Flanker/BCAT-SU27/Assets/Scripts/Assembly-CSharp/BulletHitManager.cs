using UnityEngine;

public class BulletHitManager : MonoBehaviour
{
	public static BulletHitManager instance;

	public GameObject bulletHitPrefab;

	public GameObject bulletSplashPrefab;

	public AudioClip bulletHitSound;

	private GameObject bulletHitObj;

	private GameObject splashObj;

	private ParticleSystem[] bulletHitPs;

	private ParticleSystem[] splashPs;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		Bullet.CreatePool();
		bulletHitObj = Object.Instantiate(bulletHitPrefab);
		splashObj = Object.Instantiate(bulletSplashPrefab);
		bulletHitPs = bulletHitObj.GetComponentsInChildren<ParticleSystem>();
		splashPs = splashObj.GetComponentsInChildren<ParticleSystem>();
	}

	public void CreateBulletHit(Vector3 position, Vector3 normal, bool audio)
	{
		bulletHitObj.transform.position = position;
		bulletHitObj.transform.rotation = Quaternion.LookRotation(normal);
		bulletHitPs.FireBurst();
		if (audio)
		{
			AudioController.instance.PlayOneShot(bulletHitSound, position, 1f, 1f, 2f, 1000f);
		}
	}

	public void CreateSplash(Vector3 position)
	{
		splashObj.transform.position = position;
		splashObj.transform.rotation = Quaternion.identity;
		splashPs.FireBurst();
	}

	public void CreateSplash(Vector3 position, Vector3 velocity)
	{
		Plane waterPlane = WaterPhysics.instance.waterPlane;
		Ray ray = new Ray(position - velocity, velocity);
		if (waterPlane.Raycast(ray, out var enter))
		{
			CreateSplash(ray.GetPoint(enter));
		}
	}
}
