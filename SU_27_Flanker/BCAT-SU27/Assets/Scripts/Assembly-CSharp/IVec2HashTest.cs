using System.Collections.Generic;
using UnityEngine;

public class IVec2HashTest : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.H))
		{
			RunHashTest();
		}
	}

	private void RunHashTest()
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		int num = 500;
		for (int i = -num; i <= num; i++)
		{
			for (int j = -num; j <= num; j++)
			{
				int hashCode = new IntVector2(i, j).GetHashCode();
				if (dictionary.ContainsKey(hashCode))
				{
					dictionary[hashCode]++;
				}
				else
				{
					dictionary.Add(hashCode, 1);
				}
			}
		}
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		float num7 = 0f;
		foreach (int key in dictionary.Keys)
		{
			if (dictionary[key] > num3)
			{
				num3 = dictionary[key];
				num2 = key;
			}
			if (key > num4)
			{
				num4 = key;
			}
			if (key < num5)
			{
				num5 = key;
			}
			num7 += (float)dictionary[key];
			num6++;
		}
		float num8 = num7 / (float)num6;
		int num9 = 0;
		foreach (int key2 in dictionary.Keys)
		{
			if (dictionary[key2] == num3)
			{
				num9++;
			}
		}
		Debug.LogFormat("The max hash is {0} which has {1} collisions. The hash range is {2}. {3} hashes have {1} collisions. The avg collisions is {4}", num2, num3, new IntVector2(num5, num4), num9, num8);
	}
}
