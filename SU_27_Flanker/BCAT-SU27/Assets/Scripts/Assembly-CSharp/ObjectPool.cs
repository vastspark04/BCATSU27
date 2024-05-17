using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
	public GameObject poolObject;

	public int size;

	public bool canGrow;

	public List<GameObject> pool;

	public string poolObjectName;

	public void DestroyPool()
	{
		if (pool != null)
		{
			foreach (GameObject item in pool)
			{
				if ((bool)item)
				{
					Object.Destroy(item);
				}
			}
		}
		Object.Destroy(base.gameObject);
	}

	private void Initialize()
	{
		pool = new List<GameObject>();
		for (int i = 0; i < size; i++)
		{
			GameObject gameObject = Object.Instantiate(poolObject);
			gameObject.name = poolObject.name;
			gameObject.transform.SetParent(base.transform);
			gameObject.SetActive(value: false);
			pool.Add(gameObject);
		}
	}

	public GameObject GetPooledObject()
	{
		for (int i = 0; i < size; i++)
		{
			if (!pool[i].activeSelf)
			{
				return pool[i];
			}
		}
		if (canGrow)
		{
			if (!poolObject)
			{
				Debug.LogWarning("Tried to instantiate a pool object but prefab is missing! (" + poolObjectName + ")");
			}
			GameObject gameObject = Object.Instantiate(poolObject, base.transform);
			gameObject.name = poolObject.name;
			gameObject.SetActive(value: false);
			pool.Add(gameObject);
			size = pool.Count;
			return gameObject;
		}
		return null;
	}

	public void DisableAfterDelay(GameObject obj, float t)
	{
		StartCoroutine(DisableObject(obj, t));
	}

	public void DisableAll()
	{
		if (pool == null)
		{
			return;
		}
		foreach (GameObject item in pool)
		{
			if (item.activeSelf)
			{
				item.SetActive(value: false);
			}
		}
	}

	private IEnumerator DisableObject(GameObject obj, float t)
	{
		yield return new WaitForSeconds(t);
		if ((bool)obj)
		{
			obj.SetActive(value: false);
			obj.transform.parent = base.transform;
		}
	}

	public static ObjectPool CreateObjectPool(GameObject obj, int size, bool canGrow, bool destroyOnLoad)
	{
		GameObject gameObject = new GameObject(obj.name + "Pool");
		ObjectPool objectPool = gameObject.AddComponent<ObjectPool>();
		objectPool.poolObject = obj;
		objectPool.size = size;
		objectPool.canGrow = canGrow;
		objectPool.poolObjectName = obj.name;
		if (!destroyOnLoad)
		{
			Object.DontDestroyOnLoad(gameObject);
		}
		objectPool.Initialize();
		return objectPool;
	}
}
