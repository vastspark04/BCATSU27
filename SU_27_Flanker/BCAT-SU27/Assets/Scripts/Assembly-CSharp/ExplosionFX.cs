using UnityEngine;

public class ExplosionFX : MonoBehaviour
{
	public AudioSource audioSource;

	public ParticleSystem[] particleSystems;

	public float overrideLifetime = -1f;

	public MinMax pitchRange = new MinMax(0.8f, 1.2f);

	private void Awake()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += OnExitScene;
		}
	}

	private void OnExitScene()
	{
		Object.Destroy(base.gameObject);
	}

	private void OnDestroy()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= OnExitScene;
		}
	}

	private void Start()
	{
		if ((bool)audioSource)
		{
			audioSource.pitch = pitchRange.Random();
			audioSource.Play();
		}
		if (overrideLifetime > 0f)
		{
			Object.Destroy(base.gameObject, overrideLifetime);
			return;
		}
		float num = 0f;
		for (int i = 0; i < particleSystems.Length; i++)
		{
			if (particleSystems[i].main.startLifetime.constant > num)
			{
				num = particleSystems[i].main.startLifetime.constant;
			}
		}
		if ((bool)audioSource && audioSource.clip.length > num)
		{
			num = audioSource.clip.length;
		}
		Object.Destroy(base.gameObject, num);
	}
}
