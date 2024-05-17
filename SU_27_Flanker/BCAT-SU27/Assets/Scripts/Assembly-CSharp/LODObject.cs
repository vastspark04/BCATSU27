using System;
using System.Collections.Generic;
using UnityEngine;

public class LODObject : MonoBehaviour
{
	[Serializable]
	public class LODObjectLevel
	{
		public GameObject[] gameObjects;

		public float maxDist;

		private int count;

		private bool active = true;

		public float sqrDist { get; private set; }

		public void Init()
		{
			sqrDist = maxDist * maxDist;
			count = gameObjects.Length;
			SetActive(active: false);
		}

		public void SetActive(bool active)
		{
			if (this.active == active)
			{
				return;
			}
			this.active = active;
			for (int i = 0; i < count; i++)
			{
				if ((bool)gameObjects[i])
				{
					gameObjects[i].SetActive(active);
				}
			}
		}
	}

	private class LODDistSorter : IComparer<LODObjectLevel>
	{
		public int Compare(LODObjectLevel x, LODObjectLevel y)
		{
			if (x.maxDist > y.maxDist)
			{
				return 1;
			}
			return -1;
		}
	}

	public LODBase lodBase;

	public List<LODObjectLevel> levels;

	private void Start()
	{
		if (!lodBase)
		{
			lodBase = GetComponentInParent<LODBase>();
		}
		if ((bool)lodBase)
		{
			lodBase.AddListener(OnUpdateDist);
		}
		else
		{
			Debug.LogErrorFormat("{0} LODObject has no LODBase.", base.gameObject.name);
		}
		LODDistSorter comparer = new LODDistSorter();
		levels.Sort(comparer);
		for (int i = 0; i < levels.Count; i++)
		{
			levels[i].Init();
			levels[i].SetActive(i == levels.Count - 1);
		}
	}

	private void OnUpdateDist(float sqrDist)
	{
		if (!base.gameObject.activeInHierarchy)
		{
			return;
		}
		int num = levels.Count - 1;
		levels[num].SetActive(sqrDist < levels[num].sqrDist);
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			if (sqrDist < levels[num2].sqrDist)
			{
				levels[num2].SetActive(active: true);
				levels[num2 + 1].SetActive(active: false);
			}
			else
			{
				levels[num2].SetActive(active: false);
			}
		}
	}
}
