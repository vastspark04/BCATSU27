using System;
using System.Collections.Generic;
using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class RotorRPMObjectSwitch : MonoBehaviour
{
	[Serializable]
	public class ObjectGroup
	{
		public float maxRpm;

		public GameObject[] objects;

		public List<GameObject> damageObjects;
	}

	public HelicopterRotor rotor;

	public ObjectGroup[] objectGroups;

	private int currIdx;

	private bool damageDirty;

	private void Start()
	{
		SetIdx(0);
		rotor.OnDamageLevel += Rotor_OnDamageLevel;
	}

	private void Rotor_OnDamageLevel(int obj)
	{
		damageDirty = true;
	}

	private void Update()
	{
		int num = 0;
		float outputRPM = rotor.inputShaft.outputRPM;
		for (int i = 0; i < objectGroups.Length; i++)
		{
			if (outputRPM < objectGroups[i].maxRpm)
			{
				num = i;
				break;
			}
		}
		if (num != currIdx || damageDirty)
		{
			SetIdx(num);
		}
	}

	private void SetIdx(int idx)
	{
		currIdx = idx;
		for (int i = 0; i < objectGroups.Length; i++)
		{
			if (i == currIdx)
			{
				objectGroups[i].objects.SetActive(rotor.damageLevel == 0);
				if (rotor.damageLevel != 0)
				{
					int num = Mathf.Clamp(rotor.damageLevel - 1, 0, objectGroups[i].damageObjects.Count - 1);
					for (int j = 0; j < objectGroups[i].damageObjects.Count; j++)
					{
						objectGroups[i].damageObjects[j].SetActive(j == num);
					}
				}
			}
			else
			{
				objectGroups[i].objects.SetActive(active: false);
				for (int k = 0; k < objectGroups[i].damageObjects.Count; k++)
				{
					objectGroups[i].damageObjects[k].SetActive(value: false);
				}
			}
		}
		damageDirty = false;
	}
}

}