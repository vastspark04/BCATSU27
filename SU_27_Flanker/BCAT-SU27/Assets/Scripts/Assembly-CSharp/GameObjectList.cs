using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameObjectList
{
	public List<GameObject> objects;

	public void SetAllActive(bool active)
	{
		if (objects == null)
		{
			return;
		}
		int count = objects.Count;
		for (int i = 0; i < count; i++)
		{
			if ((bool)objects[i])
			{
				objects[i].SetActive(active);
			}
		}
	}
}
