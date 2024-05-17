using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class DamageEngineOnRotorCollision : MonoBehaviour
{
	public HelicopterRotor rotor;

	public Health[] engineHealths;

	public MinMax damageRange;

	public float[] resistanceLevels;

	private void Start()
	{
		if (QuicksaveManager.isQuickload)
		{
			QuicksaveManager.instance.OnQuickloadLate += Instance_OnQuickloadLate;
			return;
		}
		rotor.OnDamageLevel += Rotor_OnDamageLevel;
		Health component = rotor.GetComponent<Health>();
		if ((bool)component)
		{
			component.OnDamage += H_OnDamage;
		}
	}

	private void Instance_OnQuickloadLate(ConfigNode configNode)
	{
		rotor.OnDamageLevel += Rotor_OnDamageLevel;
		Health component = rotor.GetComponent<Health>();
		if ((bool)component)
		{
			component.OnDamage += H_OnDamage;
		}
		QuicksaveManager.instance.OnQuickloadLate -= Instance_OnQuickloadLate;
	}

	private void OnDestroy()
	{
		if ((bool)QuicksaveManager.instance)
		{
			QuicksaveManager.instance.OnQuickloadLate -= Instance_OnQuickloadLate;
		}
	}

	private void H_OnDamage(float damage, Vector3 position, Health.DamageTypes damageType)
	{
		rotor.DamageRotor();
	}

	private void Rotor_OnDamageLevel(int obj)
	{
		if (obj != 0)
		{
			for (int i = 0; i < engineHealths.Length; i++)
			{
				engineHealths[i].Damage(damageRange.Random(), engineHealths[i].transform.position, Health.DamageTypes.Impact, null);
			}
		}
	}

	private void FixedUpdate()
	{
		if (rotor.damageLevel > 0)
		{
			int num = Mathf.Clamp(rotor.damageLevel, 0, resistanceLevels.Length - 1);
			rotor.inputShaft.AddResistanceTorque(resistanceLevels[num]);
		}
	}
}

}