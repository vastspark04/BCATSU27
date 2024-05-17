using System;
using System.Collections.Generic;
using UnityEngine;

public class LODRenderer : MonoBehaviour
{
	[Serializable]
	public class LODRendererLevel
	{
		public Renderer[] renderers;

		public float maxDist;

		private int count;

		private bool active = true;

		public float sqrDist { get; private set; }

		public void Init()
		{
			sqrDist = maxDist * maxDist;
			count = renderers.Length;
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
				if ((bool)renderers[i])
				{
					renderers[i].enabled = active;
				}
			}
		}
	}

	private class LODDistSorter : IComparer<LODRendererLevel>
	{
		public int Compare(LODRendererLevel x, LODRendererLevel y)
		{
			if (x.maxDist > y.maxDist)
			{
				return 1;
			}
			return -1;
		}
	}

	public LODBase lodBase;

	public List<LODRendererLevel> levels;

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
			Debug.LogErrorFormat("{0} LODRenderer has no LODBase.", base.gameObject.name);
		}
		LODDistSorter comparer = new LODDistSorter();
		levels.Sort(comparer);
		for (int i = 0; i < levels.Count; i++)
		{
			levels[i].Init();
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
