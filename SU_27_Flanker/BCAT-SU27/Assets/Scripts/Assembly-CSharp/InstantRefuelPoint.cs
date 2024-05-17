using System.Collections;
using UnityEngine;

public class InstantRefuelPoint : MonoBehaviour
{
	public float radius;

	private void OnDrawGizmos()
	{
		Gizmos.DrawWireSphere(base.transform.position, radius);
	}

	private void OnEnable()
	{
	}

	private IEnumerator CheckRoutine()
	{
		while (base.enabled)
		{
			yield return new WaitForSeconds(5f);
			if (!((ShipController.instance.transform.position - base.transform.position).magnitude < radius))
			{
				continue;
			}
			bool flag = false;
			if (ShipController.instance.fuelTank.fuelFraction < 0.9f)
			{
				ShipController.instance.fuelTank.AddFuel(1E+09f);
				flag = true;
			}
			VTOLCannon componentInChildren = ShipController.instance.GetComponentInChildren<VTOLCannon>();
			if (componentInChildren.ammo < componentInChildren.maxAmmo)
			{
				componentInChildren.ammo = componentInChildren.maxAmmo;
				flag = true;
			}
			RocketLauncher[] componentsInChildren = ShipController.instance.GetComponentsInChildren<RocketLauncher>();
			foreach (RocketLauncher rocketLauncher in componentsInChildren)
			{
				if (rocketLauncher.GetCount() < rocketLauncher.fireTransforms.Length)
				{
					rocketLauncher.ReloadAll();
					flag = true;
				}
			}
			MissileLauncher[] componentsInChildren2 = ShipController.instance.GetComponentsInChildren<MissileLauncher>();
			foreach (MissileLauncher missileLauncher in componentsInChildren2)
			{
				if (missileLauncher.missileCount < missileLauncher.hardpoints.Length)
				{
					missileLauncher.LoadAllMissiles();
					flag = true;
				}
			}
			Countermeasure[] componentsInChildrenImplementing = ShipController.instance.gameObject.GetComponentsInChildrenImplementing<Countermeasure>();
			foreach (Countermeasure countermeasure in componentsInChildrenImplementing)
			{
				if (countermeasure.count < countermeasure.maxCount)
				{
					countermeasure.count = countermeasure.maxCount;
					flag = true;
				}
			}
			if (ShipController.instance.currentHealth < ShipController.instance.health.maxHealth)
			{
				ShipController.instance.health.Heal(9999f);
				flag = true;
			}
			if (flag)
			{
				GetComponent<AudioSource>().Play();
			}
		}
	}
}
