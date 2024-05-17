using System.Collections.Generic;
using UnityEngine;

public class CarrierCatapultManager : MonoBehaviour
{
	public static List<CarrierCatapultManager> catapultManagers = new List<CarrierCatapultManager>();

	public List<CarrierCatapult> catapults = new List<CarrierCatapult>();

	private void Awake()
	{
		catapultManagers.Add(this);
	}

	private void OnDestroy()
	{
		if (catapultManagers != null)
		{
			catapultManagers.Remove(this);
		}
	}

	public static bool CheckForCatapult(CatapultHook hook, out CarrierCatapult catapult)
	{
		CarrierCatapultManager carrierCatapultManager = null;
		float num = 1000000f;
		for (int i = 0; i < catapultManagers.Count; i++)
		{
			float sqrMagnitude = (catapultManagers[i].transform.position - hook.hookForcePointTransform.position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				carrierCatapultManager = catapultManagers[i];
			}
		}
		catapult = null;
		if (carrierCatapultManager != null)
		{
			for (int j = 0; j < carrierCatapultManager.catapults.Count; j++)
			{
				CarrierCatapult carrierCatapult = carrierCatapultManager.catapults[j];
				if (carrierCatapult.catapultReady && (carrierCatapult.transform.position - hook.hookForcePointTransform.position).sqrMagnitude < carrierCatapult.sqrRadius && Vector3.Dot(hook.hookForcePointTransform.forward, carrierCatapult.catapultTransform.forward) > 0.5f && (hook.rb.velocity - carrierCatapult.parentRb.GetPointVelocity(carrierCatapult.transform.position)).sqrMagnitude < 100f)
				{
					carrierCatapultManager.catapults[j].Hook(hook);
					catapult = carrierCatapultManager.catapults[j];
					return true;
				}
			}
		}
		return false;
	}
}
