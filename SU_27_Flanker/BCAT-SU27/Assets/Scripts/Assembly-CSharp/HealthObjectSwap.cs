using System;
using System.Collections.Generic;
using UnityEngine;

public class HealthObjectSwap : MonoBehaviour
{
	[Serializable]
	public class ObjectLevel
	{
		[Range(0f, 1f)]
		public float maxHealth;

		public GameObject[] objects;
	}

	public Health health;

	public List<ObjectLevel> objectLevels;

	private int lastLvl;

	private void Awake()
	{
		if (!health)
		{
			health = GetComponent<Health>();
		}
		health.OnNrmHealthChanged += Health_OnNrmHealthChanged;
		objectLevels.Sort((ObjectLevel a, ObjectLevel b) => b.maxHealth.CompareTo(a.maxHealth));
		Health_OnNrmHealthChanged(health.normalizedHealth);
	}

	private void Health_OnNrmHealthChanged(float nrmHealth)
	{
		int num = -1;
		for (int i = 0; i < objectLevels.Count; i++)
		{
			if (nrmHealth <= objectLevels[i].maxHealth)
			{
				num = i;
			}
		}
		if (lastLvl == num)
		{
			return;
		}
		lastLvl = num;
		for (int j = 0; j < objectLevels.Count; j++)
		{
			if (j != num)
			{
				objectLevels[j].objects.SetActive(active: false);
			}
		}
		objectLevels[num].objects.SetActive(active: true);
	}
}
