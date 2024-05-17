using System.Collections;
using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class RotorAffectedParticles : MonoBehaviour
{
	private HelicopterRotor rotor;

	public ParticleSystem ps;

	public float maxWashSpeed = 30f;

	public float maxMinWashSpeed = 10f;

	public void Begin(HelicopterRotor r)
	{
		rotor = r;
		StartCoroutine(ParticleRoutine());
	}

	private IEnumerator ParticleRoutine()
	{
		while (base.enabled && (bool)rotor)
		{
			float num = 4f;
			float num2 = Mathf.Max(1f, (rotor.transform.position - base.transform.position).magnitude - num);
			float num3 = 1f / num2;
			ParticleSystem.ForceOverLifetimeModule forceOverLifetime = ps.forceOverLifetime;
			forceOverLifetime.enabled = true;
			Vector3 vector = -rotor.transform.up * num3 * maxMinWashSpeed;
			Vector3 vector2 = -rotor.transform.up * num3 * maxWashSpeed;
			forceOverLifetime.space = ParticleSystemSimulationSpace.World;
			forceOverLifetime.x = new ParticleSystem.MinMaxCurve(vector.x, vector2.x);
			forceOverLifetime.y = new ParticleSystem.MinMaxCurve(vector.y, vector2.y);
			forceOverLifetime.z = new ParticleSystem.MinMaxCurve(vector.z, vector2.z);
			yield return null;
		}
	}
}

}