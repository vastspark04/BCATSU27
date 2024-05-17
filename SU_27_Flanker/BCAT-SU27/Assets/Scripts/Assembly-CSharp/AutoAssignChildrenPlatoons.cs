using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AutoAssignChildrenPlatoons : MonoBehaviour
{
	private Dictionary<IntVector2, GridPlatoon> platoons;

	public LevelBuilderCreator levelBuilderCreator;

	public bool apply;

	public bool unapply;

	private void Update()
	{
		if (apply)
		{
			apply = false;
			Apply();
		}
		if (unapply)
		{
			unapply = false;
			UnApply();
		}
	}

	public void Apply()
	{
		if (!levelBuilderCreator)
		{
			return;
		}
		platoons = new Dictionary<IntVector2, GridPlatoon>();
		List<Transform> list = new List<Transform>();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			list.Add(base.transform.GetChild(i));
		}
		for (int j = 0; j < list.Count; j++)
		{
			IntVector2 editCenterPos = levelBuilderCreator.editCenterPos;
			IntVector2 intVector = levelBuilderCreator.levelBuilder.PositionToGrid(list[j].position) + editCenterPos;
			if (platoons.ContainsKey(intVector))
			{
				list[j].parent = platoons[intVector].transform;
				continue;
			}
			GameObject gameObject = new GameObject($"AutoGridPlatoon {intVector.ToString()}");
			GridPlatoon gridPlatoon = gameObject.AddComponent<GridPlatoon>();
			gridPlatoon.spawnInGrid = intVector;
			gameObject.transform.parent = base.transform;
			gameObject.transform.position = levelBuilderCreator.levelBuilder.GridToPosition(intVector - editCenterPos);
			list[j].parent = gameObject.transform;
			platoons.Add(intVector, gridPlatoon);
		}
	}

	public void UnApply()
	{
		GameObject gameObject = new GameObject();
		List<Transform> list = new List<Transform>();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			list.Add(base.transform.GetChild(i));
		}
		for (int j = 0; j < list.Count; j++)
		{
			if (list[j].name.Contains("Auto"))
			{
				Transform[] componentsInChildren = list[j].GetComponentsInChildren<Transform>();
				foreach (Transform transform in componentsInChildren)
				{
					if (transform.IsChildOf(list[j]))
					{
						transform.parent = list[j].parent;
					}
				}
			}
			list[j].parent = gameObject.transform;
		}
		Object.DestroyImmediate(gameObject);
	}
}
