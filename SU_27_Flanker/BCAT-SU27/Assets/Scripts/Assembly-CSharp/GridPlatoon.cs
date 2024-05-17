using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GridPlatoon : MonoBehaviour
{
	public IntVector2 spawnInGrid;

	public bool releaseOnSpawn;

	public bool remainActive;

	public UnityAction OnActivate;

	public UnityAction OnDeactivate;

	private void Start()
	{
		LevelBuilder.fetch.OnSpawnGrid += LevelBuilder_fetch_OnSpawnGrid;
		LevelBuilder.fetch.OnDespawnGrid += LevelBuilder_fetch_OnDespawnGrid;
		base.transform.position = LevelBuilder.fetch.GridToPosition(spawnInGrid);
		FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
		StartCoroutine(Startup());
	}

	private void OnDestroy()
	{
		if ((bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.OnOriginShift -= FloatingOrigin_instance_OnOriginShift;
		}
		if ((bool)LevelBuilder.fetch)
		{
			LevelBuilder.fetch.OnSpawnGrid -= LevelBuilder_fetch_OnSpawnGrid;
			LevelBuilder.fetch.OnDespawnGrid -= LevelBuilder_fetch_OnDespawnGrid;
		}
	}

	private IEnumerator Startup()
	{
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		base.transform.position = LevelBuilder.fetch.GridToPosition(spawnInGrid);
		if (!LevelBuilder.fetch.IsGridActive(spawnInGrid) && !remainActive)
		{
			SetActive(val: false);
		}
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		base.transform.position = LevelBuilder.fetch.GridToPosition(spawnInGrid);
	}

	private void LevelBuilder_fetch_OnSpawnGrid(IntVector2 grid)
	{
		if (!grid.Equals(spawnInGrid))
		{
			return;
		}
		base.transform.position = LevelBuilder.fetch.GridToPosition(spawnInGrid);
		SetActive(val: true);
		if (!releaseOnSpawn)
		{
			return;
		}
		while (base.transform.childCount > 0)
		{
			Transform child = base.transform.GetChild(0);
			child.parent = null;
			FloatingOriginTransform floatingOriginTransform = child.gameObject.AddComponent<FloatingOriginTransform>();
			if ((bool)floatingOriginTransform)
			{
				Rigidbody component = child.GetComponent<Rigidbody>();
				if ((bool)component)
				{
					floatingOriginTransform.SetRigidbody(component);
				}
			}
		}
	}

	private void LevelBuilder_fetch_OnDespawnGrid(IntVector2 grid)
	{
		if (!remainActive && grid.Equals(spawnInGrid))
		{
			SetActive(val: false);
		}
	}

	private void SetActive(bool val)
	{
		if (val)
		{
			if (OnActivate != null)
			{
				OnActivate();
			}
		}
		else if (OnDeactivate != null)
		{
			OnDeactivate();
		}
		for (int i = 0; i < base.transform.childCount; i++)
		{
			base.transform.GetChild(i).gameObject.SetActive(val);
		}
	}
}
