using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class RotorAffectedMissileLauncher : MonoBehaviour
{
	private HelicopterRotor rotor;

	private HPEquipMissileLauncher mlEq;

	private void Awake()
	{
		mlEq = GetComponent<HPEquipMissileLauncher>();
		if ((bool)mlEq)
		{
			if ((bool)mlEq.weaponManager)
			{
				OnEquip();
			}
			else
			{
				mlEq.OnEquipped += OnEquip;
			}
		}
		mlEq.ml.OnFiredMissileIdx += Ml_OnFiredMissileIdx;
	}

	private void Ml_OnFiredMissileIdx(int idx)
	{
		RotorAffectedParticles[] componentsInChildren = mlEq.ml.missiles[idx].GetComponentsInChildren<RotorAffectedParticles>();
		foreach (RotorAffectedParticles rotorAffectedParticles in componentsInChildren)
		{
			if ((bool)rotorAffectedParticles)
			{
				rotorAffectedParticles.Begin(rotor);
			}
		}
	}

	private void OnEquip()
	{
		rotor = mlEq.weaponManager.GetComponentInChildren<HelicopterRotor>();
	}
}

}