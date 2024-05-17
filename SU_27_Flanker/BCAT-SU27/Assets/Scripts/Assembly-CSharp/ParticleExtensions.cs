using UnityEngine;

public static class ParticleExtensions
{
	private static ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[1];

	public static void SetEmission(this ParticleSystem[] pSystems, bool emit)
	{
		for (int i = 0; i < pSystems.Length; i++)
		{
			if ((bool)pSystems[i])
			{
				pSystems[i].SetEmission(emit);
			}
		}
	}

	public static void SetEmissionAndActive(this ParticleSystem[] pSystems, bool emit)
	{
		for (int i = 0; i < pSystems.Length; i++)
		{
			if ((bool)pSystems[i])
			{
				pSystems[i].SetEmission(emit);
				pSystems[i].gameObject.SetActive(emit);
			}
		}
	}

	public static void SetEmissionRateMultiplier(this ParticleSystem[] pSystems, float multiplier)
	{
		for (int i = 0; i < pSystems.Length; i++)
		{
			if ((bool)pSystems[i])
			{
				pSystems[i].SetEmissionRateMultiplier(multiplier);
			}
		}
	}

	public static void SetEmission(this ParticleSystem pSystem, bool emit)
	{
		ParticleSystem.EmissionModule emission = pSystem.emission;
		emission.enabled = emit;
	}

	public static void SetEmissionRate(this ParticleSystem pSystem, float emissionRate)
	{
		ParticleSystem.EmissionModule emission = pSystem.emission;
		emission.rateOverTime = emissionRate;
	}

	public static void SetEmissionRateMultiplier(this ParticleSystem pSystem, float multiplier)
	{
		ParticleSystem.EmissionModule emission = pSystem.emission;
		emission.rateOverTimeMultiplier = multiplier;
	}

	public static void Play(this ParticleSystem[] pSystems)
	{
		for (int i = 0; i < pSystems.Length; i++)
		{
			pSystems[i].Play();
		}
	}

	public static void FireBurst(this ParticleSystem pSystem)
	{
		pSystem.emission.GetBursts(bursts);
		pSystem.Emit(Random.Range(bursts[0].minCount, bursts[0].maxCount));
	}

	public static void FireBurst(this ParticleSystem[] pSystems)
	{
		for (int i = 0; i < pSystems.Length; i++)
		{
			pSystems[i].FireBurst();
		}
	}

	public static float GetLongestLife(this ParticleSystem[] pSystems)
	{
		float num = 0f;
		for (int i = 0; i < pSystems.Length; i++)
		{
			if ((bool)pSystems[i] && num < pSystems[i].main.startLifetime.constantMax)
			{
				num = pSystems[i].main.startLifetime.constantMax;
			}
		}
		return num;
	}
}
