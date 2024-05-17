using System.Collections;
using UnityEngine;

public class ShipParticleWake : MonoBehaviour
{
	public ShipMover shipMover;

	public ParticleSystem bowWake;

	public ParticleSystem rearWake;

	public float accelParticleSpeed = 40f;

	public float overrideEmissionRate = 2f;

	private void OnEnable()
	{
		if (!shipMover)
		{
			shipMover = GetComponentInParent<ShipMover>();
		}
		if (overrideEmissionRate > 0f)
		{
			if ((bool)bowWake)
			{
				bowWake.SetEmissionRate(overrideEmissionRate);
			}
			if ((bool)rearWake)
			{
				rearWake.SetEmissionRate(overrideEmissionRate);
			}
		}
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		float frameInterval = 0.2f;
		yield return new WaitForSeconds(Random.Range(0f, frameInterval));
		WaitForSeconds wait = new WaitForSeconds(frameInterval);
		while (base.enabled)
		{
			UpdateWake();
			yield return wait;
		}
	}

	private void UpdateWake()
	{
		float num = shipMover.rb.velocity.magnitude / shipMover.maxSpeed;
		num *= num;
		float num2 = Vector3.Dot(shipMover.transform.forward, shipMover.currentAccel);
		float num3 = Mathf.Max(num, Mathf.Clamp01(num2 / shipMover.maxSpeed));
		if ((bool)rearWake)
		{
			if (num3 < 0.01f)
			{
				ParticleSystem.EmissionModule emission = rearWake.emission;
				emission.enabled = false;
			}
			else
			{
				ParticleSystem.EmissionModule emission2 = rearWake.emission;
				emission2.enabled = true;
				ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = rearWake.velocityOverLifetime;
				velocityOverLifetime.y = (0f - num3) * accelParticleSpeed;
				ParticleSystem.MainModule main = rearWake.main;
				main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, num3));
			}
		}
		if ((bool)bowWake)
		{
			if (num < 0.01f)
			{
				ParticleSystem.EmissionModule emission3 = bowWake.emission;
				emission3.enabled = false;
				return;
			}
			ParticleSystem.EmissionModule emission4 = bowWake.emission;
			emission4.enabled = true;
			ParticleSystem.MainModule main2 = bowWake.main;
			main2.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, num));
		}
	}
}
