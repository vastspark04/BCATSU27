using System;
using UnityEngine;
using VTOLVR.DLC.Rotorcraft;

public class JetWashFX : MonoBehaviour
{
	[Serializable]
	public class JetWashParticleSystem
	{
		public ParticleSystem ps;

		public AnimationCurve emissionCurve;

		public AnimationCurve speedCurve;

		public bool abOnly;

		private ParticleSystem.EmissionModule em;

		private ParticleSystem.MainModule main;

		private float longestLife;

		private float timeEmitted;

		public void Init()
		{
			main = ps.main;
			em = ps.emission;
			longestLife = main.startLifetime.constantMax;
			Update(1f, 0f);
		}

		public void Update(float normDist, float throttle)
		{
			float time = normDist * throttle;
			float num = emissionCurve.Evaluate(time);
			if (num < 0.2f)
			{
				if (Time.time - timeEmitted < longestLife)
				{
					em.enabled = false;
				}
				else
				{
					ps.gameObject.SetActive(value: false);
				}
			}
			else
			{
				ps.gameObject.SetActive(value: true);
				em.enabled = true;
				em.rateOverTime = new ParticleSystem.MinMaxCurve(num);
				main.startSpeedMultiplier = speedCurve.Evaluate(time);
				timeEmitted = Time.time;
			}
		}
	}

	public ModuleEngine engine;

	public Transform thrustTransform;

	public float maxDist = 1f;

	public JetWashParticleSystem[] particleSystems;

	public JetWashParticleSystem[] waterParticleSystems;

	private bool disabledOutOfRange;

	[Header("Rotor Wash")]
	public HelicopterRotor rotor;

	public float rotorMaxRPM;

	private void Awake()
	{
		for (int i = 0; i < particleSystems.Length; i++)
		{
			particleSystems[i].Init();
		}
		for (int j = 0; j < waterParticleSystems.Length; j++)
		{
			waterParticleSystems[j].Init();
		}
		if (!thrustTransform && (bool)engine)
		{
			thrustTransform = engine.thrustTransform;
		}
	}

	private void LateUpdate()
	{
		if ((VRHead.position - base.transform.position).sqrMagnitude < 25000000f || ((bool)FlybyCameraMFDPage.instance && FlybyCameraMFDPage.instance.isCamEnabled && (FlybyCameraMFDPage.instance.flybyCam.transform.position - base.transform.position).sqrMagnitude < 100000000f))
		{
			disabledOutOfRange = false;
			float normDist = 0f;
			Ray ray = new Ray(thrustTransform.position, thrustTransform.forward);
			bool flag = false;
			if (flag = Physics.Raycast(ray, out var hitInfo, maxDist, 1, QueryTriggerInteraction.Ignore))
			{
				if ((bool)WaterPhysics.instance && hitInfo.point.y < WaterPhysics.instance.height)
				{
					flag = false;
				}
				else
				{
					normDist = 1f - Mathf.Clamp01(hitInfo.distance / maxDist);
					base.transform.position = hitInfo.point;
					base.transform.rotation = Quaternion.LookRotation(hitInfo.normal, Vector3.forward);
				}
			}
			float throttle = 0f;
			if ((bool)engine)
			{
				throttle = engine.abMult;
			}
			float throttle2 = 0f;
			if ((bool)engine)
			{
				throttle2 = engine.finalThrottle;
			}
			else if ((bool)rotor)
			{
				throttle2 = rotor.inputShaft.outputRPM / rotorMaxRPM;
			}
			for (int i = 0; i < particleSystems.Length; i++)
			{
				if (particleSystems[i].abOnly)
				{
					particleSystems[i].Update(normDist, throttle);
				}
				else
				{
					particleSystems[i].Update(normDist, throttle2);
				}
			}
			if (waterParticleSystems == null || !WaterPhysics.instance)
			{
				return;
			}
			float normDist2 = 0f;
			if (!flag && thrustTransform.position.y < WaterPhysics.instance.height + maxDist)
			{
				float enter = 0f;
				if (WaterPhysics.instance.waterPlane.Raycast(ray, out enter))
				{
					normDist2 = 1f - Mathf.Clamp01(enter / maxDist);
					if (enter < maxDist)
					{
						base.transform.position = ray.GetPoint(enter);
						base.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
					}
				}
			}
			for (int j = 0; j < waterParticleSystems.Length; j++)
			{
				if (waterParticleSystems[j].abOnly)
				{
					waterParticleSystems[j].Update(normDist2, throttle);
				}
				else
				{
					waterParticleSystems[j].Update(normDist2, throttle2);
				}
			}
		}
		else
		{
			if (disabledOutOfRange)
			{
				return;
			}
			disabledOutOfRange = true;
			JetWashParticleSystem[] array;
			if (waterParticleSystems != null)
			{
				array = waterParticleSystems;
				for (int k = 0; k < array.Length; k++)
				{
					array[k].Update(0f, 0f);
				}
			}
			array = particleSystems;
			for (int k = 0; k < array.Length; k++)
			{
				array[k].Update(0f, 0f);
			}
		}
	}
}
