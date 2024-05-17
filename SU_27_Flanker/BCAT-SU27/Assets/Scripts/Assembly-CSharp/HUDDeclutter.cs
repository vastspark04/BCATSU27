using System;
using UnityEngine;

public class HUDDeclutter : MonoBehaviour
{
	public delegate void DeclutterDelegate(int declutterLevel);

	[Serializable]
	public class DeclutterObjects
	{
		public GameObject[] objects;
	}

	public DeclutterObjects[] discreteObjects;

	public DeclutterObjects[] additiveObjects;

	private int _currLevel;

	public int currentLevel => _currLevel;

	public event DeclutterDelegate OnSetDeclutter;

	public void SetDeclutter(int level)
	{
		_currLevel = level;
		if (this.OnSetDeclutter != null)
		{
			this.OnSetDeclutter(level);
		}
		for (int i = 0; i < discreteObjects.Length; i++)
		{
			discreteObjects[i].objects.SetActive(active: false);
		}
		for (int j = 0; j < discreteObjects.Length; j++)
		{
			if (j == level)
			{
				discreteObjects[j].objects.SetActive(active: true);
			}
		}
		for (int k = 0; k < additiveObjects.Length; k++)
		{
			additiveObjects[k].objects.SetActive(k >= level);
		}
	}
}
