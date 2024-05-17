using System;
using System.Collections;
using UnityEngine;

public class CMFlare : MonoBehaviour
{
	public float heatEmission;

	public HeatEmitter heatEmitter;

	public float gravFactor;

	public Vector3 velocity;

	public float drag = 0.2f;

	public ParticleSystem[] ps;

	public ParticleSystem smokeSystem;

	[HideInInspector]
	public float flareLife = 7f;

	private float finalEmission;

	private Vector3 rotAxis;

	private float rotRate;

	public float smokeForce = 3f;

	public MinMax rotationRate = new MinMax(60f, 180f);

	public float timeFired;

	public event Action<CMFlare> OnDecayed;

	private void OnEnable()
	{
		if (!heatEmitter)
		{
			heatEmitter = GetComponent<HeatEmitter>();
		}
		rotAxis = UnityEngine.Random.onUnitSphere;
		rotRate = rotationRate.Random();
		timeFired = Time.time;
		StartCoroutine(LifeRoutine());
		StartCoroutine(SmokeRoutine());
	}

	private IEnumerator LifeRoutine()
	{
		finalEmission = heatEmission;
		ps.SetEmission(emit: true);
		yield return new WaitForSeconds(flareLife);
		float longestLife = ps.GetLongestLife();
		ps.SetEmission(emit: false);
		finalEmission = 0f;
		yield return new WaitForSeconds(longestLife);
		if (this.OnDecayed != null)
		{
			this.OnDecayed(this);
		}
		base.gameObject.SetActive(value: false);
	}

	private IEnumerator SmokeRoutine()
	{
		float speed = velocity.magnitude * 0.015f;
		ParticleSystem.MainModule mainModule = smokeSystem.main;
		ParticleSystem.LimitVelocityOverLifetimeModule dampModule = smokeSystem.limitVelocityOverLifetime;
		dampModule.dampen = 0.05f;
		ParticleSystem.ForceOverLifetimeModule forceMod = smokeSystem.forceOverLifetime;
		while (base.enabled)
		{
			mainModule.startSpeed = speed;
			speed = Mathf.Lerp(speed, 0f, 2f * Time.deltaTime);
			Vector3 vector = smokeForce * base.transform.forward;
			forceMod.x = vector.x;
			forceMod.y = vector.y;
			forceMod.z = vector.z;
			dampModule.dampen = Mathf.Lerp(dampModule.dampen, 0.1f, 8f * Time.deltaTime);
			yield return null;
		}
	}

	private void Update()
	{
		velocity += Time.deltaTime * gravFactor * Physics.gravity;
		velocity -= Time.deltaTime * drag * velocity;
		heatEmitter.velocity = velocity;
		heatEmitter.AddHeat(finalEmission * Time.deltaTime);
		base.transform.position += velocity * Time.deltaTime;
		base.transform.rotation = Quaternion.AngleAxis(rotRate * Time.deltaTime, rotAxis) * base.transform.rotation;
	}
}
